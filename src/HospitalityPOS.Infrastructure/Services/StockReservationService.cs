using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing stock reservations.
/// </summary>
public class StockReservationService : IStockReservationService
{
    private readonly IRepository<StockReservation> _reservationRepository;
    private readonly IRepository<Inventory> _inventoryRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<StockTransferRequest> _transferRequestRepository;

    public StockReservationService(
        IRepository<StockReservation> reservationRepository,
        IRepository<Inventory> inventoryRepository,
        IRepository<Product> productRepository,
        IRepository<Store> storeRepository,
        IRepository<StockTransferRequest> transferRequestRepository)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _transferRequestRepository = transferRequestRepository ?? throw new ArgumentNullException(nameof(transferRequestRepository));
    }

    #region Reservation Management

    public async Task<StockReservationDto> CreateReservationAsync(CreateStockReservationDto dto, int userId)
    {
        // Validate stock availability
        var canReserve = await CanReserveAsync(dto.LocationId, dto.ProductId, dto.Quantity);
        if (!canReserve)
        {
            throw new InvalidOperationException($"Insufficient stock available to reserve {dto.Quantity} units.");
        }

        var reservation = new StockReservation
        {
            LocationId = dto.LocationId,
            LocationType = dto.LocationType,
            ProductId = dto.ProductId,
            ReservedQuantity = dto.Quantity,
            ReferenceId = dto.ReferenceId,
            ReferenceType = dto.ReferenceType,
            Status = ReservationStatus.Active,
            ReservedAt = DateTime.UtcNow,
            ReservedByUserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(dto.ExpirationHours),
            Notes = dto.Notes
        };

        await _reservationRepository.AddAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return await MapToDto(reservation);
    }

    public async Task<List<StockReservationDto>> CreateBatchReservationsAsync(CreateBatchReservationsDto dto, int userId)
    {
        // Validate all lines first
        var validation = await ValidateBatchReservationAsync(dto.LocationId, dto.Lines);
        var failedLines = validation.Where(v => !v.CanReserve).ToList();

        if (failedLines.Any())
        {
            var errors = string.Join("; ", failedLines.Select(f => $"{f.ProductName}: needs {f.RequestedQuantity}, available {f.AvailableQuantity}"));
            throw new InvalidOperationException($"Cannot reserve all items: {errors}");
        }

        var reservations = new List<StockReservation>();
        var expiresAt = DateTime.UtcNow.AddHours(dto.ExpirationHours);

        foreach (var line in dto.Lines)
        {
            var reservation = new StockReservation
            {
                LocationId = dto.LocationId,
                LocationType = dto.LocationType,
                ProductId = line.ProductId,
                ReservedQuantity = line.Quantity,
                ReferenceId = dto.ReferenceId,
                ReferenceType = dto.ReferenceType,
                Status = ReservationStatus.Active,
                ReservedAt = DateTime.UtcNow,
                ReservedByUserId = userId,
                ExpiresAt = expiresAt
            };

            await _reservationRepository.AddAsync(reservation);
            reservations.Add(reservation);
        }

        await _reservationRepository.SaveChangesAsync();

        var dtos = new List<StockReservationDto>();
        foreach (var r in reservations)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<StockReservationDto?> GetReservationAsync(int reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null) return null;

        return await MapToDto(reservation);
    }

    public async Task<List<StockReservationDto>> GetReservationsByReferenceAsync(ReservationType referenceType, int referenceId)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.ReferenceType == referenceType &&
            r.ReferenceId == referenceId &&
            r.IsActive);

        var dtos = new List<StockReservationDto>();
        foreach (var r in reservations)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<List<StockReservationDto>> GetReservationsAsync(ReservationQueryDto query)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.IsActive &&
            (!query.LocationId.HasValue || r.LocationId == query.LocationId) &&
            (!query.ProductId.HasValue || r.ProductId == query.ProductId) &&
            (!query.ReferenceType.HasValue || r.ReferenceType == query.ReferenceType) &&
            (!query.ReferenceId.HasValue || r.ReferenceId == query.ReferenceId) &&
            (!query.Status.HasValue || r.Status == query.Status) &&
            (!query.FromDate.HasValue || r.ReservedAt >= query.FromDate) &&
            (!query.ToDate.HasValue || r.ReservedAt <= query.ToDate));

        if (query.IsExpired.HasValue)
        {
            reservations = query.IsExpired.Value
                ? reservations.Where(r => r.Status == ReservationStatus.Active && DateTime.UtcNow > r.ExpiresAt).ToList()
                : reservations.Where(r => r.Status == ReservationStatus.Active && DateTime.UtcNow <= r.ExpiresAt).ToList();
        }

        var orderedList = reservations
            .OrderByDescending(r => r.ReservedAt)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToList();

        var dtos = new List<StockReservationDto>();
        foreach (var r in orderedList)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<List<StockReservationDto>> GetActiveReservationsAsync(int locationId, int productId)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.LocationId == locationId &&
            r.ProductId == productId &&
            r.Status == ReservationStatus.Active &&
            r.IsActive);

        var dtos = new List<StockReservationDto>();
        foreach (var r in reservations)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<int> GetReservedQuantityAsync(int locationId, int productId)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.LocationId == locationId &&
            r.ProductId == productId &&
            r.Status == ReservationStatus.Active &&
            r.IsActive &&
            DateTime.UtcNow <= r.ExpiresAt);

        return reservations.Sum(r => r.ReservedQuantity);
    }

    public async Task<int> GetAvailableQuantityAsync(int locationId, int productId)
    {
        // Get stock on hand
        var inventory = (await _inventoryRepository.FindAsync(i =>
            i.StoreId == locationId &&
            i.ProductId == productId &&
            i.IsActive)).FirstOrDefault();

        var stockOnHand = inventory?.Quantity ?? 0;
        var reserved = await GetReservedQuantityAsync(locationId, productId);

        return Math.Max(0, stockOnHand - reserved);
    }

    #endregion

    #region Reservation Lifecycle

    public async Task<StockReservationDto> FulfillReservationAsync(int reservationId, int userId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            throw new InvalidOperationException($"Cannot fulfill reservation in status {reservation.Status}.");

        reservation.Status = ReservationStatus.Fulfilled;
        reservation.CompletedAt = DateTime.UtcNow;
        reservation.CompletedByUserId = userId;

        await _reservationRepository.UpdateAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return await MapToDto(reservation);
    }

    public async Task<List<StockReservationDto>> FulfillByReferenceAsync(ReservationType referenceType, int referenceId, int userId)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.ReferenceType == referenceType &&
            r.ReferenceId == referenceId &&
            r.Status == ReservationStatus.Active &&
            r.IsActive);

        var dtos = new List<StockReservationDto>();

        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Fulfilled;
            reservation.CompletedAt = DateTime.UtcNow;
            reservation.CompletedByUserId = userId;
            await _reservationRepository.UpdateAsync(reservation);
            dtos.Add(await MapToDto(reservation));
        }

        await _reservationRepository.SaveChangesAsync();
        return dtos;
    }

    public async Task<StockReservationDto> ReleaseReservationAsync(int reservationId, int userId, string? reason = null)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            throw new InvalidOperationException($"Cannot release reservation in status {reservation.Status}.");

        reservation.Status = ReservationStatus.Released;
        reservation.CompletedAt = DateTime.UtcNow;
        reservation.CompletedByUserId = userId;
        if (!string.IsNullOrEmpty(reason))
        {
            reservation.Notes = $"{reservation.Notes} Released: {reason}".Trim();
        }

        await _reservationRepository.UpdateAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return await MapToDto(reservation);
    }

    public async Task<List<StockReservationDto>> ReleaseByReferenceAsync(ReservationType referenceType, int referenceId, int userId, string? reason = null)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.ReferenceType == referenceType &&
            r.ReferenceId == referenceId &&
            r.Status == ReservationStatus.Active &&
            r.IsActive);

        var dtos = new List<StockReservationDto>();

        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Released;
            reservation.CompletedAt = DateTime.UtcNow;
            reservation.CompletedByUserId = userId;
            if (!string.IsNullOrEmpty(reason))
            {
                reservation.Notes = $"{reservation.Notes} Released: {reason}".Trim();
            }
            await _reservationRepository.UpdateAsync(reservation);
            dtos.Add(await MapToDto(reservation));
        }

        await _reservationRepository.SaveChangesAsync();
        return dtos;
    }

    public async Task<StockReservationDto> UpdateReservationQuantityAsync(int reservationId, int newQuantity, int userId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            throw new InvalidOperationException($"Cannot update reservation in status {reservation.Status}.");

        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        // If increasing quantity, validate availability
        if (newQuantity > reservation.ReservedQuantity)
        {
            var additionalNeeded = newQuantity - reservation.ReservedQuantity;
            var available = await GetAvailableQuantityAsync(reservation.LocationId, reservation.ProductId);
            if (available < additionalNeeded)
            {
                throw new InvalidOperationException($"Insufficient stock. Only {available} additional units available.");
            }
        }

        reservation.ReservedQuantity = newQuantity;
        await _reservationRepository.UpdateAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return await MapToDto(reservation);
    }

    public async Task<StockReservationDto> ExtendReservationAsync(int reservationId, int additionalHours, int userId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            throw new InvalidOperationException($"Cannot extend reservation in status {reservation.Status}.");

        // Extend from current time if already expired, otherwise from current expiry
        var baseTime = reservation.ExpiresAt < DateTime.UtcNow ? DateTime.UtcNow : reservation.ExpiresAt;
        reservation.ExpiresAt = baseTime.AddHours(additionalHours);

        await _reservationRepository.UpdateAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return await MapToDto(reservation);
    }

    #endregion

    #region Expiration Management

    public async Task<List<StockReservationDto>> GetExpiredReservationsAsync()
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.Status == ReservationStatus.Active &&
            r.IsActive &&
            DateTime.UtcNow > r.ExpiresAt);

        var dtos = new List<StockReservationDto>();
        foreach (var r in reservations)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<List<StockReservationDto>> GetExpiringReservationsAsync(int withinHours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(withinHours);
        var reservations = await _reservationRepository.FindAsync(r =>
            r.Status == ReservationStatus.Active &&
            r.IsActive &&
            r.ExpiresAt <= cutoff &&
            r.ExpiresAt > DateTime.UtcNow);

        var dtos = new List<StockReservationDto>();
        foreach (var r in reservations.OrderBy(r => r.ExpiresAt))
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<int> ExpireOverdueReservationsAsync()
    {
        var expired = await _reservationRepository.FindAsync(r =>
            r.Status == ReservationStatus.Active &&
            r.IsActive &&
            DateTime.UtcNow > r.ExpiresAt);

        var count = 0;
        foreach (var reservation in expired)
        {
            reservation.Status = ReservationStatus.Expired;
            reservation.CompletedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);
            count++;
        }

        if (count > 0)
        {
            await _reservationRepository.SaveChangesAsync();
        }

        return count;
    }

    public async Task<bool> HasExpiredReservationsAsync(ReservationType referenceType, int referenceId)
    {
        var reservations = await _reservationRepository.FindAsync(r =>
            r.ReferenceType == referenceType &&
            r.ReferenceId == referenceId &&
            r.IsActive);

        return reservations.Any(r =>
            r.Status == ReservationStatus.Active &&
            DateTime.UtcNow > r.ExpiresAt);
    }

    #endregion

    #region Validation

    public async Task<bool> CanReserveAsync(int locationId, int productId, int quantity)
    {
        var available = await GetAvailableQuantityAsync(locationId, productId);
        return available >= quantity;
    }

    public async Task<List<ReservationValidationResult>> ValidateBatchReservationAsync(int locationId, List<ReservationLineDto> lines)
    {
        var results = new List<ReservationValidationResult>();

        foreach (var line in lines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId);
            var inventory = (await _inventoryRepository.FindAsync(i =>
                i.StoreId == locationId &&
                i.ProductId == line.ProductId &&
                i.IsActive)).FirstOrDefault();

            var stockOnHand = inventory?.Quantity ?? 0;
            var reserved = await GetReservedQuantityAsync(locationId, line.ProductId);
            var available = Math.Max(0, stockOnHand - reserved);

            results.Add(new ReservationValidationResult
            {
                ProductId = line.ProductId,
                ProductName = product?.Name ?? "Unknown",
                RequestedQuantity = line.Quantity,
                AvailableQuantity = available,
                StockOnHand = stockOnHand,
                CurrentlyReserved = reserved,
                ValidationMessage = available < line.Quantity
                    ? $"Insufficient stock. Available: {available}, Requested: {line.Quantity}"
                    : null
            });
        }

        return results;
    }

    #endregion

    #region Summaries and Reports

    public async Task<LocationReservationSummaryDto> GetLocationSummaryAsync(int locationId)
    {
        var store = await _storeRepository.GetByIdAsync(locationId);
        var reservations = await _reservationRepository.FindAsync(r =>
            r.LocationId == locationId &&
            r.IsActive);

        var activeReservations = reservations.Where(r => r.Status == ReservationStatus.Active && DateTime.UtcNow <= r.ExpiresAt).ToList();
        var expiringWithin24h = activeReservations.Count(r => r.ExpiresAt <= DateTime.UtcNow.AddHours(24));
        var expiredCount = reservations.Count(r => r.Status == ReservationStatus.Active && DateTime.UtcNow > r.ExpiresAt);

        return new LocationReservationSummaryDto
        {
            LocationId = locationId,
            LocationName = store?.Name ?? "Unknown",
            ActiveReservations = activeReservations.Count,
            TotalQuantityReserved = activeReservations.Sum(r => r.ReservedQuantity),
            ExpiringWithin24Hours = expiringWithin24h,
            ExpiredReservations = expiredCount
        };
    }

    public async Task<ProductReservationSummaryDto> GetProductSummaryAsync(int locationId, int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        var inventory = (await _inventoryRepository.FindAsync(i =>
            i.StoreId == locationId &&
            i.ProductId == productId &&
            i.IsActive)).FirstOrDefault();

        var stockOnHand = inventory?.Quantity ?? 0;
        var reservations = await _reservationRepository.FindAsync(r =>
            r.LocationId == locationId &&
            r.ProductId == productId &&
            r.Status == ReservationStatus.Active &&
            r.IsActive &&
            DateTime.UtcNow <= r.ExpiresAt);

        var totalReserved = reservations.Sum(r => r.ReservedQuantity);
        var details = new List<ReservationDetailDto>();

        foreach (var r in reservations)
        {
            string? refNumber = null;
            if (r.ReferenceType == ReservationType.Transfer)
            {
                var transfer = await _transferRequestRepository.GetByIdAsync(r.ReferenceId);
                refNumber = transfer?.RequestNumber;
            }

            details.Add(new ReservationDetailDto
            {
                ReservationId = r.Id,
                Quantity = r.ReservedQuantity,
                ReferenceType = r.ReferenceType,
                ReferenceNumber = refNumber,
                ExpiresAt = r.ExpiresAt
            });
        }

        return new ProductReservationSummaryDto
        {
            ProductId = productId,
            ProductName = product?.Name ?? "Unknown",
            ProductSku = product?.SKU ?? string.Empty,
            TotalStockOnHand = stockOnHand,
            TotalReserved = totalReserved,
            AvailableQuantity = Math.Max(0, stockOnHand - totalReserved),
            ActiveReservations = details
        };
    }

    public async Task<List<LocationReservationSummaryDto>> GetAllLocationSummariesAsync()
    {
        var stores = await _storeRepository.FindAsync(s => s.IsActive);
        var summaries = new List<LocationReservationSummaryDto>();

        foreach (var store in stores)
        {
            summaries.Add(await GetLocationSummaryAsync(store.Id));
        }

        return summaries.OrderBy(s => s.LocationName).ToList();
    }

    #endregion

    #region Private Helpers

    private async Task<StockReservationDto> MapToDto(StockReservation reservation)
    {
        var store = await _storeRepository.GetByIdAsync(reservation.LocationId);
        var product = await _productRepository.GetByIdAsync(reservation.ProductId);

        string? referenceNumber = null;
        if (reservation.ReferenceType == ReservationType.Transfer)
        {
            var transfer = await _transferRequestRepository.GetByIdAsync(reservation.ReferenceId);
            referenceNumber = transfer?.RequestNumber;
        }

        return new StockReservationDto
        {
            Id = reservation.Id,
            LocationId = reservation.LocationId,
            LocationName = store?.Name ?? "Unknown",
            LocationType = reservation.LocationType,
            ProductId = reservation.ProductId,
            ProductName = product?.Name ?? "Unknown",
            ProductSku = product?.SKU ?? string.Empty,
            ReservedQuantity = reservation.ReservedQuantity,
            ReferenceId = reservation.ReferenceId,
            ReferenceType = reservation.ReferenceType,
            ReferenceNumber = referenceNumber,
            Status = reservation.Status,
            ReservedAt = reservation.ReservedAt,
            ReservedByUserName = string.Empty, // Would need user repository
            ExpiresAt = reservation.ExpiresAt,
            CompletedAt = reservation.CompletedAt,
            CompletedByUserName = null, // Would need user repository
            Notes = reservation.Notes
        };
    }

    #endregion
}
