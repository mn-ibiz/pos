using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for batch and expiry tracking operations.
/// </summary>
public class ProductBatchService : IProductBatchService
{
    private readonly IRepository<ProductBatch> _batchRepository;
    private readonly IRepository<ProductBatchConfiguration> _configRepository;
    private readonly IRepository<BatchStockMovement> _movementRepository;
    private readonly IRepository<BatchDisposal> _disposalRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<GoodsReceivedNote> _grnRepository;
    private readonly IRepository<User> _userRepository;

    public ProductBatchService(
        IRepository<ProductBatch> batchRepository,
        IRepository<ProductBatchConfiguration> configRepository,
        IRepository<BatchStockMovement> movementRepository,
        IRepository<BatchDisposal> disposalRepository,
        IRepository<Product> productRepository,
        IRepository<Store> storeRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<GoodsReceivedNote> grnRepository,
        IRepository<User> userRepository)
    {
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _disposalRepository = disposalRepository ?? throw new ArgumentNullException(nameof(disposalRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _grnRepository = grnRepository ?? throw new ArgumentNullException(nameof(grnRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    #region Batch Management

    public async Task<ProductBatchDto> CreateBatchAsync(CreateProductBatchDto dto, int userId)
    {
        // Validate expiry if required
        var config = await GetConfigurationEntityAsync(dto.ProductId);
        if (config?.RequiresExpiryDate == true && !dto.ExpiryDate.HasValue)
        {
            throw new InvalidOperationException($"Expiry date is required for product {dto.ProductId}");
        }

        if (dto.ExpiryDate.HasValue && config != null)
        {
            var validation = await ValidateShelfLifeAsync(dto.ProductId, dto.ExpiryDate.Value);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Message);
            }
        }

        var batch = new ProductBatch
        {
            ProductId = dto.ProductId,
            StoreId = dto.StoreId,
            BatchNumber = dto.BatchNumber,
            ExpiryDate = dto.ExpiryDate,
            ManufactureDate = dto.ManufactureDate,
            InitialQuantity = dto.Quantity,
            CurrentQuantity = dto.Quantity,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            DisposedQuantity = 0,
            SupplierId = dto.SupplierId,
            GrnId = dto.GrnId,
            TransferReceiptId = dto.TransferReceiptId,
            ReceivedAt = DateTime.UtcNow,
            ReceivedByUserId = userId,
            Status = BatchStatus.Active,
            UnitCost = dto.UnitCost,
            Notes = dto.Notes,
            IsActive = true
        };

        await _batchRepository.AddAsync(batch);

        // Record initial receipt movement
        await RecordMovementInternalAsync(batch.Id, BatchMovementType.Receipt, dto.Quantity, 0, dto.Quantity,
            dto.GrnId.HasValue ? "GRN" : (dto.TransferReceiptId.HasValue ? "Transfer" : "Manual"),
            dto.GrnId ?? dto.TransferReceiptId ?? 0,
            null, dto.UnitCost, userId, null);

        return await MapToBatchDtoAsync(batch);
    }

    public async Task<List<ProductBatchDto>> CreateBatchesFromReceivingAsync(int grnId, List<BatchReceivingEntryDto> entries, int storeId, int supplierId, int userId)
    {
        var batches = new List<ProductBatchDto>();

        foreach (var entry in entries)
        {
            var dto = new CreateProductBatchDto
            {
                ProductId = entry.ProductId,
                StoreId = storeId,
                BatchNumber = entry.BatchNumber,
                ExpiryDate = entry.ExpiryDate,
                ManufactureDate = entry.ManufactureDate,
                Quantity = entry.Quantity,
                SupplierId = supplierId,
                GrnId = grnId,
                UnitCost = entry.UnitCost,
                Notes = entry.Notes
            };

            var batch = await CreateBatchAsync(dto, userId);
            batches.Add(batch);
        }

        return batches;
    }

    public async Task<ProductBatchDto?> GetBatchAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        return batch != null ? await MapToBatchDtoAsync(batch) : null;
    }

    public async Task<ProductBatchDto?> GetBatchByNumberAsync(string batchNumber, int productId, int storeId)
    {
        var batches = await _batchRepository.GetAllAsync();
        var batch = batches.FirstOrDefault(b => b.BatchNumber == batchNumber && b.ProductId == productId && b.StoreId == storeId);
        return batch != null ? await MapToBatchDtoAsync(batch) : null;
    }

    public async Task<List<ProductBatchDto>> GetProductBatchesAsync(int productId, int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ProductId == productId);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var result = new List<ProductBatchDto>();
        foreach (var batch in filtered.OrderByDescending(b => b.ReceivedAt))
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<List<ProductBatchDto>> GetBatchesAsync(BatchQueryDto query)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.AsEnumerable();

        if (query.ProductId.HasValue)
            filtered = filtered.Where(b => b.ProductId == query.ProductId.Value);

        if (query.StoreId.HasValue)
            filtered = filtered.Where(b => b.StoreId == query.StoreId.Value);

        if (!string.IsNullOrEmpty(query.BatchNumber))
            filtered = filtered.Where(b => b.BatchNumber.Contains(query.BatchNumber, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<BatchStatus>(query.Status, out var status))
            filtered = filtered.Where(b => b.Status == status);

        if (query.ExpiringWithinDays == true && query.DaysUntilExpiry.HasValue)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(query.DaysUntilExpiry.Value);
            filtered = filtered.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= thresholdDate && b.ExpiryDate.Value > DateTime.UtcNow);
        }

        if (query.IncludeExpired != true)
            filtered = filtered.Where(b => !b.IsExpired);

        if (query.HasAvailableStock == true)
            filtered = filtered.Where(b => b.AvailableQuantity > 0);

        if (query.ExpiryDateFrom.HasValue)
            filtered = filtered.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value >= query.ExpiryDateFrom.Value);

        if (query.ExpiryDateTo.HasValue)
            filtered = filtered.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= query.ExpiryDateTo.Value);

        if (query.ReceivedDateFrom.HasValue)
            filtered = filtered.Where(b => b.ReceivedAt >= query.ReceivedDateFrom.Value);

        if (query.ReceivedDateTo.HasValue)
            filtered = filtered.Where(b => b.ReceivedAt <= query.ReceivedDateTo.Value);

        if (query.SupplierId.HasValue)
            filtered = filtered.Where(b => b.SupplierId == query.SupplierId.Value);

        var result = new List<ProductBatchDto>();
        foreach (var batch in filtered.OrderByDescending(b => b.ReceivedAt))
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }
        return result;
    }

    public async Task<List<BatchSelectionDto>> GetAvailableBatchesAsync(int productId, int storeId, bool includeExpired = false)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ProductId == productId && b.StoreId == storeId && b.AvailableQuantity > 0);

        if (!includeExpired)
            filtered = filtered.Where(b => !b.IsExpired);

        var config = await GetConfigurationEntityAsync(productId);
        var warningDays = config?.ExpiryWarningDays ?? 30;

        return filtered.OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(b => b.ReceivedAt)
            .Select(b => new BatchSelectionDto
            {
                BatchId = b.Id,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                AvailableQuantity = b.AvailableQuantity,
                DaysUntilExpiry = b.DaysUntilExpiry,
                IsExpired = b.IsExpired,
                IsNearExpiry = b.DaysUntilExpiry.HasValue && b.DaysUntilExpiry.Value <= warningDays,
                UnitCost = b.UnitCost,
                ReceivedAt = b.ReceivedAt
            }).ToList();
    }

    public async Task<ProductBatchSummaryDto> GetProductBatchSummaryAsync(int productId, int? storeId = null)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ProductId == productId);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var batchList = filtered.ToList();
        var config = await GetConfigurationEntityAsync(productId);
        var warningDays = config?.ExpiryWarningDays ?? 30;

        return new ProductBatchSummaryDto
        {
            ProductId = productId,
            ProductName = product?.Name ?? string.Empty,
            ProductCode = product?.Code ?? string.Empty,
            TotalBatches = batchList.Count,
            ActiveBatches = batchList.Count(b => b.Status == BatchStatus.Active && !b.IsExpired),
            ExpiredBatches = batchList.Count(b => b.IsExpired),
            NearExpiryBatches = batchList.Count(b => !b.IsExpired && b.DaysUntilExpiry.HasValue && b.DaysUntilExpiry.Value <= warningDays),
            TotalQuantity = batchList.Sum(b => b.CurrentQuantity),
            AvailableQuantity = batchList.Sum(b => b.AvailableQuantity),
            TotalValue = batchList.Sum(b => b.CurrentQuantity * b.UnitCost),
            EarliestExpiry = batchList.Where(b => b.ExpiryDate.HasValue && !b.IsExpired).Min(b => b.ExpiryDate),
            LatestExpiry = batchList.Where(b => b.ExpiryDate.HasValue).Max(b => b.ExpiryDate)
        };
    }

    public async Task<ProductBatchDto> UpdateBatchStatusAsync(int batchId, BatchStatus status, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        batch.Status = status;
        await _batchRepository.UpdateAsync(batch);

        return await MapToBatchDtoAsync(batch);
    }

    #endregion

    #region Batch Configuration

    public async Task<ProductBatchConfigurationDto?> GetBatchConfigurationAsync(int productId)
    {
        var config = await GetConfigurationEntityAsync(productId);
        return config != null ? await MapToConfigDtoAsync(config) : null;
    }

    public async Task<ProductBatchConfigurationDto> SaveBatchConfigurationAsync(UpdateProductBatchConfigurationDto dto, int userId)
    {
        var existing = await GetConfigurationEntityAsync(dto.ProductId);

        if (existing != null)
        {
            existing.RequiresBatchTracking = dto.RequiresBatchTracking;
            existing.RequiresExpiryDate = dto.RequiresExpiryDate;
            existing.ExpiryWarningDays = dto.ExpiryWarningDays;
            existing.ExpiryCriticalDays = dto.ExpiryCriticalDays;
            existing.ExpiredItemAction = Enum.TryParse<ExpiryAction>(dto.ExpiredItemAction, out var expiredAction) ? expiredAction : ExpiryAction.Block;
            existing.NearExpiryAction = Enum.TryParse<ExpiryAction>(dto.NearExpiryAction, out var nearAction) ? nearAction : ExpiryAction.Warn;
            existing.UseFifo = dto.UseFifo;
            existing.UseFefo = dto.UseFefo;
            existing.TrackManufactureDate = dto.TrackManufactureDate;
            existing.MinimumShelfLifeDaysOnReceipt = dto.MinimumShelfLifeDaysOnReceipt;

            await _configRepository.UpdateAsync(existing);
            return await MapToConfigDtoAsync(existing);
        }
        else
        {
            var config = new ProductBatchConfiguration
            {
                ProductId = dto.ProductId,
                RequiresBatchTracking = dto.RequiresBatchTracking,
                RequiresExpiryDate = dto.RequiresExpiryDate,
                ExpiryWarningDays = dto.ExpiryWarningDays,
                ExpiryCriticalDays = dto.ExpiryCriticalDays,
                ExpiredItemAction = Enum.TryParse<ExpiryAction>(dto.ExpiredItemAction, out var expiredAction) ? expiredAction : ExpiryAction.Block,
                NearExpiryAction = Enum.TryParse<ExpiryAction>(dto.NearExpiryAction, out var nearAction) ? nearAction : ExpiryAction.Warn,
                UseFifo = dto.UseFifo,
                UseFefo = dto.UseFefo,
                TrackManufactureDate = dto.TrackManufactureDate,
                MinimumShelfLifeDaysOnReceipt = dto.MinimumShelfLifeDaysOnReceipt,
                IsActive = true
            };

            await _configRepository.AddAsync(config);
            return await MapToConfigDtoAsync(config);
        }
    }

    public async Task<List<ProductBatchConfigurationDto>> GetBatchTrackingProductsAsync()
    {
        var configs = await _configRepository.GetAllAsync();
        var result = new List<ProductBatchConfigurationDto>();

        foreach (var config in configs.Where(c => c.RequiresBatchTracking))
        {
            result.Add(await MapToConfigDtoAsync(config));
        }

        return result;
    }

    public async Task<bool> RequiresBatchTrackingAsync(int productId)
    {
        var config = await GetConfigurationEntityAsync(productId);
        return config?.RequiresBatchTracking ?? false;
    }

    public async Task<bool> RequiresExpiryDateAsync(int productId)
    {
        var config = await GetConfigurationEntityAsync(productId);
        return config?.RequiresExpiryDate ?? false;
    }

    #endregion

    #region Expiry Validation

    public async Task<ShelfLifeValidationDto> ValidateShelfLifeAsync(int productId, DateTime expiryDate)
    {
        var config = await GetConfigurationEntityAsync(productId);
        var actualShelfLifeDays = (int)(expiryDate - DateTime.UtcNow).TotalDays;
        var minimumRequired = config?.MinimumShelfLifeDaysOnReceipt ?? 0;

        if (minimumRequired > 0 && actualShelfLifeDays < minimumRequired)
        {
            return new ShelfLifeValidationDto
            {
                IsValid = false,
                MinimumShelfLifeDays = minimumRequired,
                ActualShelfLifeDays = actualShelfLifeDays,
                Message = $"Product requires minimum {minimumRequired} days shelf life. Received product has only {actualShelfLifeDays} days."
            };
        }

        return new ShelfLifeValidationDto
        {
            IsValid = true,
            MinimumShelfLifeDays = minimumRequired,
            ActualShelfLifeDays = actualShelfLifeDays
        };
    }

    public async Task<ExpiryValidationResultDto> ValidateBatchForSaleAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        var config = await GetConfigurationEntityAsync(batch.ProductId);

        // Check if expired
        if (batch.IsExpired)
        {
            var action = config?.ExpiredItemAction ?? ExpiryAction.Block;
            return new ExpiryValidationResultDto
            {
                IsValid = action != ExpiryAction.Block,
                RequiresWarning = action == ExpiryAction.Warn,
                IsBlocked = action == ExpiryAction.Block,
                RequiresManagerOverride = action == ExpiryAction.RequireOverride,
                Message = "This batch has expired.",
                DaysUntilExpiry = batch.DaysUntilExpiry,
                ValidationAction = action.ToString()
            };
        }

        // Check if near expiry
        var criticalDays = config?.ExpiryCriticalDays ?? 7;
        if (batch.DaysUntilExpiry.HasValue && batch.DaysUntilExpiry.Value <= criticalDays)
        {
            var action = config?.NearExpiryAction ?? ExpiryAction.Warn;
            return new ExpiryValidationResultDto
            {
                IsValid = true,
                RequiresWarning = action == ExpiryAction.Warn,
                IsBlocked = false,
                RequiresManagerOverride = action == ExpiryAction.RequireOverride,
                Message = $"This batch expires in {batch.DaysUntilExpiry} days.",
                DaysUntilExpiry = batch.DaysUntilExpiry,
                ValidationAction = action.ToString()
            };
        }

        return new ExpiryValidationResultDto
        {
            IsValid = true,
            RequiresWarning = false,
            IsBlocked = false,
            RequiresManagerOverride = false,
            DaysUntilExpiry = batch.DaysUntilExpiry
        };
    }

    public async Task<BatchAvailabilityDto> CheckBatchAvailabilityAsync(int productId, int storeId, int quantity)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        var availableBatches = await GetAvailableBatchesAsync(productId, storeId, includeExpired: false);
        var totalAvailable = availableBatches.Sum(b => b.AvailableQuantity);

        var config = await GetConfigurationEntityAsync(productId);

        // Get suggested batches using FIFO/FEFO
        var suggestedBatches = new List<BatchSelectionDto>();
        var remaining = quantity;

        var orderedBatches = config?.UseFefo == true
            ? availableBatches.OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue).ThenBy(b => b.ReceivedAt)
            : availableBatches.OrderBy(b => b.ReceivedAt);

        foreach (var batch in orderedBatches)
        {
            if (remaining <= 0) break;
            suggestedBatches.Add(batch);
            remaining -= batch.AvailableQuantity;
        }

        // Check for expiry issues in suggested batches
        ExpiryValidationResultDto? expiryValidation = null;
        var batchWithIssue = suggestedBatches.FirstOrDefault(b => b.IsExpired || b.IsNearExpiry);
        if (batchWithIssue != null)
        {
            var batchEntity = await _batchRepository.GetByIdAsync(batchWithIssue.BatchId);
            if (batchEntity != null)
            {
                expiryValidation = await ValidateBatchForSaleAsync(batchEntity.Id);
            }
        }

        return new BatchAvailabilityDto
        {
            ProductId = productId,
            ProductName = product?.Name ?? string.Empty,
            RequestedQuantity = quantity,
            AvailableQuantity = totalAvailable,
            HasSufficientStock = totalAvailable >= quantity,
            HasExpiryIssues = expiryValidation?.RequiresWarning == true || expiryValidation?.IsBlocked == true,
            AvailableBatches = availableBatches,
            SuggestedBatches = suggestedBatches,
            ExpiryValidation = expiryValidation
        };
    }

    #endregion

    #region Batch Allocation

    public async Task<BatchAllocationResultDto> AllocateBatchesAsync(AllocateBatchesRequestDto request)
    {
        var availableBatches = await GetAvailableBatchesAsync(request.ProductId, request.StoreId, request.AllowExpired);
        var allocations = new List<BatchAllocationDto>();
        var remaining = request.Quantity;

        // Order batches based on FIFO/FEFO preference
        IEnumerable<BatchSelectionDto> orderedBatches;
        if (request.UseFefo)
        {
            orderedBatches = availableBatches.OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue).ThenBy(b => b.ReceivedAt);
        }
        else if (request.UseFifo)
        {
            orderedBatches = availableBatches.OrderBy(b => b.ReceivedAt);
        }
        else
        {
            orderedBatches = availableBatches;
        }

        ExpiryValidationResultDto? expiryValidation = null;

        foreach (var batch in orderedBatches)
        {
            if (remaining <= 0) break;

            // Skip near-expiry if not allowed
            if (!request.AllowNearExpiry && batch.IsNearExpiry)
                continue;

            // Check expiry validation
            var batchValidation = await ValidateBatchForSaleAsync(batch.BatchId);
            if (batchValidation.IsBlocked)
                continue;

            if (batchValidation.RequiresWarning || batchValidation.RequiresManagerOverride)
            {
                expiryValidation ??= batchValidation;
            }

            var toAllocate = Math.Min(remaining, batch.AvailableQuantity);
            allocations.Add(new BatchAllocationDto
            {
                BatchId = batch.BatchId,
                BatchNumber = batch.BatchNumber,
                AllocatedQuantity = toAllocate,
                UnitCost = batch.UnitCost,
                ExpiryDate = batch.ExpiryDate
            });

            remaining -= toAllocate;
        }

        var totalAllocated = allocations.Sum(a => a.AllocatedQuantity);

        return new BatchAllocationResultDto
        {
            Success = remaining <= 0,
            TotalAllocated = totalAllocated,
            Shortfall = remaining > 0 ? remaining : 0,
            Allocations = allocations,
            ExpiryValidation = expiryValidation,
            Message = remaining > 0 ? $"Insufficient stock. Short by {remaining} units." : null
        };
    }

    public async Task ReserveBatchQuantityAsync(int batchId, int quantity, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        if (batch.AvailableQuantity < quantity)
            throw new InvalidOperationException($"Insufficient available quantity in batch. Available: {batch.AvailableQuantity}, Requested: {quantity}");

        var quantityBefore = batch.CurrentQuantity;
        batch.ReservedQuantity += quantity;

        await _batchRepository.UpdateAsync(batch);

        await RecordMovementInternalAsync(batchId, BatchMovementType.Reserved, -quantity, quantityBefore, batch.CurrentQuantity,
            "Reservation", 0, null, batch.UnitCost, userId, "Stock reserved for order");
    }

    public async Task ReleaseBatchQuantityAsync(int batchId, int quantity, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        var toRelease = Math.Min(quantity, batch.ReservedQuantity);
        var quantityBefore = batch.CurrentQuantity;
        batch.ReservedQuantity -= toRelease;

        await _batchRepository.UpdateAsync(batch);

        await RecordMovementInternalAsync(batchId, BatchMovementType.Released, toRelease, quantityBefore, batch.CurrentQuantity,
            "Release", 0, null, batch.UnitCost, userId, "Reserved stock released");
    }

    public async Task DeductBatchQuantityAsync(int batchId, int quantity, string referenceType, int referenceId, string? referenceNumber, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        if (batch.CurrentQuantity < quantity)
            throw new InvalidOperationException($"Insufficient quantity in batch. Current: {batch.CurrentQuantity}, Requested: {quantity}");

        var quantityBefore = batch.CurrentQuantity;
        batch.CurrentQuantity -= quantity;
        batch.SoldQuantity += quantity;

        // Release reserved if any
        if (batch.ReservedQuantity > 0)
        {
            var toRelease = Math.Min(quantity, batch.ReservedQuantity);
            batch.ReservedQuantity -= toRelease;
        }

        // Update status if low stock
        if (batch.CurrentQuantity == 0)
            batch.Status = BatchStatus.Disposed;
        else if (batch.CurrentQuantity < batch.InitialQuantity * 0.1m)
            batch.Status = BatchStatus.LowStock;

        await _batchRepository.UpdateAsync(batch);

        await RecordMovementInternalAsync(batchId, BatchMovementType.Sale, -quantity, quantityBefore, batch.CurrentQuantity,
            referenceType, referenceId, referenceNumber, batch.UnitCost, userId, null);
    }

    #endregion

    #region Batch Movements

    public async Task<BatchStockMovementDto> RecordMovementAsync(RecordBatchMovementDto dto, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId)
            ?? throw new InvalidOperationException($"Batch {dto.BatchId} not found");

        if (!Enum.TryParse<BatchMovementType>(dto.MovementType, out var movementType))
            throw new InvalidOperationException($"Invalid movement type: {dto.MovementType}");

        var quantityBefore = batch.CurrentQuantity;
        var quantityAfter = quantityBefore + dto.Quantity;

        return await RecordMovementInternalAsync(dto.BatchId, movementType, dto.Quantity, quantityBefore, quantityAfter,
            dto.ReferenceType, dto.ReferenceId, dto.ReferenceNumber, batch.UnitCost, userId, dto.Notes);
    }

    public async Task<List<BatchStockMovementDto>> GetBatchMovementsAsync(int batchId)
    {
        var movements = await _movementRepository.GetAllAsync();
        var filtered = movements.Where(m => m.BatchId == batchId).OrderByDescending(m => m.MovedAt);

        var result = new List<BatchStockMovementDto>();
        foreach (var movement in filtered)
        {
            result.Add(await MapToMovementDtoAsync(movement));
        }
        return result;
    }

    public async Task<List<BatchStockMovementDto>> GetMovementsByReferenceAsync(string referenceType, int referenceId)
    {
        var movements = await _movementRepository.GetAllAsync();
        var filtered = movements.Where(m => m.ReferenceType == referenceType && m.ReferenceId == referenceId)
            .OrderByDescending(m => m.MovedAt);

        var result = new List<BatchStockMovementDto>();
        foreach (var movement in filtered)
        {
            result.Add(await MapToMovementDtoAsync(movement));
        }
        return result;
    }

    #endregion

    #region Batch Disposal

    public async Task<BatchDisposalDto> CreateDisposalAsync(CreateBatchDisposalDto dto, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId)
            ?? throw new InvalidOperationException($"Batch {dto.BatchId} not found");

        if (batch.CurrentQuantity < dto.Quantity)
            throw new InvalidOperationException($"Insufficient quantity for disposal. Current: {batch.CurrentQuantity}, Requested: {dto.Quantity}");

        if (!Enum.TryParse<DisposalReason>(dto.Reason, out var reason))
            throw new InvalidOperationException($"Invalid disposal reason: {dto.Reason}");

        var disposal = new BatchDisposal
        {
            BatchId = dto.BatchId,
            StoreId = batch.StoreId,
            Quantity = dto.Quantity,
            Reason = reason,
            Description = dto.Description,
            DisposedAt = DateTime.UtcNow,
            ApprovedByUserId = dto.ApprovedByUserId,
            DisposedByUserId = userId,
            UnitCost = batch.UnitCost,
            IsWitnessed = dto.IsWitnessed,
            WitnessName = dto.WitnessName,
            PhotoPath = dto.PhotoPath,
            IsActive = true
        };

        await _disposalRepository.AddAsync(disposal);

        // Update batch
        var quantityBefore = batch.CurrentQuantity;
        batch.CurrentQuantity -= dto.Quantity;
        batch.DisposedQuantity += dto.Quantity;

        if (batch.CurrentQuantity == 0)
            batch.Status = BatchStatus.Disposed;

        await _batchRepository.UpdateAsync(batch);

        // Record movement
        await RecordMovementInternalAsync(dto.BatchId, BatchMovementType.Disposal, -dto.Quantity, quantityBefore, batch.CurrentQuantity,
            "Disposal", disposal.Id, null, batch.UnitCost, userId, dto.Description);

        return await MapToDisposalDtoAsync(disposal);
    }

    public async Task<List<BatchDisposalDto>> GetBatchDisposalsAsync(int batchId)
    {
        var disposals = await _disposalRepository.GetAllAsync();
        var result = new List<BatchDisposalDto>();

        foreach (var disposal in disposals.Where(d => d.BatchId == batchId).OrderByDescending(d => d.DisposedAt))
        {
            result.Add(await MapToDisposalDtoAsync(disposal));
        }

        return result;
    }

    public async Task<List<BatchDisposalDto>> GetStoreDisposalsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var disposals = await _disposalRepository.GetAllAsync();
        var filtered = disposals.Where(d => d.StoreId == storeId);

        if (fromDate.HasValue)
            filtered = filtered.Where(d => d.DisposedAt >= fromDate.Value);

        if (toDate.HasValue)
            filtered = filtered.Where(d => d.DisposedAt <= toDate.Value);

        var result = new List<BatchDisposalDto>();
        foreach (var disposal in filtered.OrderByDescending(d => d.DisposedAt))
        {
            result.Add(await MapToDisposalDtoAsync(disposal));
        }

        return result;
    }

    #endregion

    #region Expiry Monitoring

    public async Task<List<ProductBatchDto>> GetExpiringBatchesAsync(int days, int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var thresholdDate = DateTime.UtcNow.AddDays(days);

        var filtered = batches.Where(b =>
            b.ExpiryDate.HasValue &&
            b.ExpiryDate.Value > DateTime.UtcNow &&
            b.ExpiryDate.Value <= thresholdDate &&
            b.CurrentQuantity > 0);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var result = new List<ProductBatchDto>();
        foreach (var batch in filtered.OrderBy(b => b.ExpiryDate))
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }

        return result;
    }

    public async Task<List<ProductBatchDto>> GetExpiredBatchesAsync(int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();

        var filtered = batches.Where(b =>
            b.IsExpired &&
            b.CurrentQuantity > 0);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var result = new List<ProductBatchDto>();
        foreach (var batch in filtered.OrderBy(b => b.ExpiryDate))
        {
            result.Add(await MapToBatchDtoAsync(batch));
        }

        return result;
    }

    public async Task<List<ProductBatchDto>> GetCriticalExpiryBatchesAsync(int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();

        if (storeId.HasValue)
            batches = batches.Where(b => b.StoreId == storeId.Value).ToList();

        var result = new List<ProductBatchDto>();

        foreach (var batch in batches.Where(b => b.CurrentQuantity > 0 && b.ExpiryDate.HasValue))
        {
            var config = await GetConfigurationEntityAsync(batch.ProductId);
            var criticalDays = config?.ExpiryCriticalDays ?? 7;

            if (batch.DaysUntilExpiry.HasValue && batch.DaysUntilExpiry.Value <= criticalDays && !batch.IsExpired)
            {
                result.Add(await MapToBatchDtoAsync(batch));
            }
        }

        return result.OrderBy(b => b.ExpiryDate).ToList();
    }

    public async Task UpdateExpiryStatusesAsync()
    {
        var batches = await _batchRepository.GetAllAsync();

        foreach (var batch in batches.Where(b => b.Status == BatchStatus.Active || b.Status == BatchStatus.LowStock))
        {
            if (batch.IsExpired && batch.Status != BatchStatus.Expired)
            {
                batch.Status = BatchStatus.Expired;
                await _batchRepository.UpdateAsync(batch);
            }
        }
    }

    #endregion

    #region Expiry Dashboard

    public async Task<ExpiryDashboardDto> GetExpiryDashboardAsync(ExpiryDashboardQueryDto? query = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ExpiryDate.HasValue && b.CurrentQuantity > 0);

        if (query?.StoreId.HasValue == true)
            filtered = filtered.Where(b => b.StoreId == query.StoreId.Value);

        if (query?.IncludeZeroStock != true)
            filtered = filtered.Where(b => b.CurrentQuantity > 0);

        var maxDays = query?.MaxDaysAhead ?? 90;

        var expiryGroups = new List<ExpiryGroupDto>
        {
            await CreateExpiryGroupAsync(filtered, "Already Expired", int.MinValue, 0, ExpiryAlertSeverity.Expired),
            await CreateExpiryGroupAsync(filtered, "Critical (0-7 days)", 0, 7, ExpiryAlertSeverity.Critical),
            await CreateExpiryGroupAsync(filtered, "Urgent (7-14 days)", 7, 14, ExpiryAlertSeverity.Urgent),
            await CreateExpiryGroupAsync(filtered, "Warning (14-30 days)", 14, 30, ExpiryAlertSeverity.Warning),
            await CreateExpiryGroupAsync(filtered, "Upcoming (30-90 days)", 30, maxDays, ExpiryAlertSeverity.Info)
        };

        var expiredGroup = expiryGroups.First(g => g.Severity == ExpiryAlertSeverity.Expired);
        var expiringGroups = expiryGroups.Where(g => g.Severity != ExpiryAlertSeverity.Expired);

        return new ExpiryDashboardDto
        {
            TotalExpiredItems = expiredGroup.ItemCount,
            TotalExpiredValue = expiredGroup.TotalValue,
            TotalExpiringItems = expiringGroups.Sum(g => g.ItemCount),
            TotalExpiringValue = expiringGroups.Sum(g => g.TotalValue),
            TotalBatchesRequiringAction = expiryGroups.Where(g => g.Severity >= ExpiryAlertSeverity.Warning).Sum(g => g.ItemCount),
            GeneratedAt = DateTime.UtcNow,
            ExpiryGroups = expiryGroups.Where(g => g.ItemCount > 0).ToList(),
            Summary = await GetExpirySummaryAsync(query?.StoreId)
        };
    }

    public async Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int? storeId = null, ExpiryAlertSeverity? minSeverity = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ExpiryDate.HasValue && b.CurrentQuantity > 0);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var alerts = new List<ExpiryAlertDto>();

        foreach (var batch in filtered)
        {
            var severity = GetSeverity(batch.DaysUntilExpiry ?? int.MaxValue);

            if (minSeverity.HasValue && severity < minSeverity.Value)
                continue;

            var product = await _productRepository.GetByIdAsync(batch.ProductId);
            var store = await _storeRepository.GetByIdAsync(batch.StoreId);

            alerts.Add(new ExpiryAlertDto
            {
                BatchId = batch.Id,
                ProductId = batch.ProductId,
                ProductName = product?.Name ?? string.Empty,
                BatchNumber = batch.BatchNumber,
                StoreId = batch.StoreId,
                StoreName = store?.Name ?? string.Empty,
                ExpiryDate = batch.ExpiryDate!.Value,
                DaysToExpiry = batch.DaysUntilExpiry ?? 0,
                Quantity = batch.CurrentQuantity,
                Value = batch.CurrentQuantity * batch.UnitCost,
                Severity = severity,
                AlertMessage = GetAlertMessage(severity, batch.DaysUntilExpiry ?? 0, product?.Name ?? "Unknown")
            });
        }

        return alerts.OrderBy(a => a.DaysToExpiry).ToList();
    }

    public async Task<List<SuggestedAction>> GetSuggestedActionsAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        var actions = new List<SuggestedAction>();

        if (batch.IsExpired)
        {
            actions.Add(SuggestedAction.RemoveFromShelf);
            actions.Add(SuggestedAction.Dispose);
        }
        else if (batch.DaysUntilExpiry.HasValue)
        {
            if (batch.DaysUntilExpiry <= 7)
            {
                actions.Add(SuggestedAction.Markdown);
                actions.Add(SuggestedAction.PrioritizeSale);
                actions.Add(SuggestedAction.RemoveFromShelf);
            }
            else if (batch.DaysUntilExpiry <= 14)
            {
                actions.Add(SuggestedAction.Markdown);
                actions.Add(SuggestedAction.PrioritizeSale);
            }
            else if (batch.DaysUntilExpiry <= 30)
            {
                actions.Add(SuggestedAction.PrioritizeSale);
                actions.Add(SuggestedAction.Transfer);
            }
        }

        return actions;
    }

    public Task AcknowledgeAlertAsync(int batchId, int userId)
    {
        // Alert acknowledgement would typically update a separate alerts table
        // For now, this is a placeholder for future implementation
        return Task.CompletedTask;
    }

    public async Task<List<ExpiryExportDto>> GetExpiryExportDataAsync(ExpiryDashboardQueryDto? query = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ExpiryDate.HasValue && b.CurrentQuantity > 0);

        if (query?.StoreId.HasValue == true)
            filtered = filtered.Where(b => b.StoreId == query.StoreId.Value);

        var maxDays = query?.MaxDaysAhead ?? 90;
        var cutoffDate = DateTime.UtcNow.AddDays(maxDays);

        filtered = filtered.Where(b => b.ExpiryDate!.Value <= cutoffDate || b.IsExpired);

        var exportData = new List<ExpiryExportDto>();

        foreach (var batch in filtered.OrderBy(b => b.ExpiryDate))
        {
            var product = await _productRepository.GetByIdAsync(batch.ProductId);
            var store = await _storeRepository.GetByIdAsync(batch.StoreId);
            var supplier = batch.SupplierId.HasValue ? await _supplierRepository.GetByIdAsync(batch.SupplierId.Value) : null;
            var actions = await GetSuggestedActionsAsync(batch.Id);
            var severity = GetSeverity(batch.DaysUntilExpiry ?? int.MaxValue);

            exportData.Add(new ExpiryExportDto
            {
                ProductCode = product?.Code ?? string.Empty,
                ProductName = product?.Name ?? string.Empty,
                BatchNumber = batch.BatchNumber,
                StoreName = store?.Name ?? string.Empty,
                ExpiryDate = batch.ExpiryDate!.Value,
                DaysToExpiry = batch.DaysUntilExpiry ?? 0,
                Quantity = batch.CurrentQuantity,
                UnitCost = batch.UnitCost,
                TotalValue = batch.CurrentQuantity * batch.UnitCost,
                Severity = severity.ToString(),
                SuggestedActions = string.Join(", ", actions.Select(a => a.ToString())),
                SupplierName = supplier?.Name,
                ReceivedDate = batch.ReceivedAt
            });
        }

        return exportData;
    }

    public async Task<ExpiryDashboardSummaryDto> GetExpirySummaryAsync(int? storeId = null)
    {
        var batches = await _batchRepository.GetAllAsync();
        var filtered = batches.Where(b => b.ExpiryDate.HasValue && b.CurrentQuantity > 0);

        if (storeId.HasValue)
            filtered = filtered.Where(b => b.StoreId == storeId.Value);

        var batchList = filtered.ToList();

        return new ExpiryDashboardSummaryDto
        {
            ExpiredCount = batchList.Count(b => b.IsExpired),
            ExpiredValue = batchList.Where(b => b.IsExpired).Sum(b => b.CurrentQuantity * b.UnitCost),
            CriticalCount = batchList.Count(b => !b.IsExpired && b.DaysUntilExpiry >= 0 && b.DaysUntilExpiry <= 7),
            CriticalValue = batchList.Where(b => !b.IsExpired && b.DaysUntilExpiry >= 0 && b.DaysUntilExpiry <= 7).Sum(b => b.CurrentQuantity * b.UnitCost),
            UrgentCount = batchList.Count(b => b.DaysUntilExpiry > 7 && b.DaysUntilExpiry <= 14),
            UrgentValue = batchList.Where(b => b.DaysUntilExpiry > 7 && b.DaysUntilExpiry <= 14).Sum(b => b.CurrentQuantity * b.UnitCost),
            WarningCount = batchList.Count(b => b.DaysUntilExpiry > 14 && b.DaysUntilExpiry <= 30),
            WarningValue = batchList.Where(b => b.DaysUntilExpiry > 14 && b.DaysUntilExpiry <= 30).Sum(b => b.CurrentQuantity * b.UnitCost),
            InfoCount = batchList.Count(b => b.DaysUntilExpiry > 30),
            InfoValue = batchList.Where(b => b.DaysUntilExpiry > 30).Sum(b => b.CurrentQuantity * b.UnitCost)
        };
    }

    private async Task<ExpiryGroupDto> CreateExpiryGroupAsync(IEnumerable<ProductBatch> batches, string period, int daysFrom, int daysTo, ExpiryAlertSeverity severity)
    {
        IEnumerable<ProductBatch> groupBatches;

        if (daysFrom == int.MinValue)
        {
            // Expired
            groupBatches = batches.Where(b => b.IsExpired);
        }
        else
        {
            groupBatches = batches.Where(b =>
                !b.IsExpired &&
                b.DaysUntilExpiry.HasValue &&
                b.DaysUntilExpiry >= daysFrom &&
                b.DaysUntilExpiry < daysTo);
        }

        var batchList = groupBatches.ToList();
        var expiringBatches = new List<ExpiringBatchDto>();

        foreach (var batch in batchList.OrderBy(b => b.ExpiryDate))
        {
            var product = await _productRepository.GetByIdAsync(batch.ProductId);
            var store = await _storeRepository.GetByIdAsync(batch.StoreId);
            var supplier = batch.SupplierId.HasValue ? await _supplierRepository.GetByIdAsync(batch.SupplierId.Value) : null;

            expiringBatches.Add(new ExpiringBatchDto
            {
                BatchId = batch.Id,
                ProductId = batch.ProductId,
                ProductName = product?.Name ?? string.Empty,
                ProductCode = product?.Code ?? string.Empty,
                StoreId = batch.StoreId,
                StoreName = store?.Name ?? string.Empty,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate!.Value,
                RemainingQuantity = batch.CurrentQuantity,
                UnitCost = batch.UnitCost,
                DaysToExpiry = batch.DaysUntilExpiry ?? 0,
                Severity = severity,
                SuggestedActions = await GetSuggestedActionsAsync(batch.Id),
                SupplierName = supplier?.Name,
                ReceivedAt = batch.ReceivedAt
            });
        }

        return new ExpiryGroupDto
        {
            Period = period,
            DaysFrom = daysFrom == int.MinValue ? 0 : daysFrom,
            DaysTo = daysTo,
            Severity = severity,
            ItemCount = batchList.Count,
            TotalQuantity = batchList.Sum(b => b.CurrentQuantity),
            TotalValue = batchList.Sum(b => b.CurrentQuantity * b.UnitCost),
            Batches = expiringBatches
        };
    }

    private static ExpiryAlertSeverity GetSeverity(int daysUntilExpiry)
    {
        if (daysUntilExpiry < 0) return ExpiryAlertSeverity.Expired;
        if (daysUntilExpiry <= 7) return ExpiryAlertSeverity.Critical;
        if (daysUntilExpiry <= 14) return ExpiryAlertSeverity.Urgent;
        if (daysUntilExpiry <= 30) return ExpiryAlertSeverity.Warning;
        return ExpiryAlertSeverity.Info;
    }

    private static string GetAlertMessage(ExpiryAlertSeverity severity, int daysToExpiry, string productName)
    {
        return severity switch
        {
            ExpiryAlertSeverity.Expired => $"{productName} has expired and requires immediate disposal.",
            ExpiryAlertSeverity.Critical => $"{productName} expires in {daysToExpiry} days - urgent action required.",
            ExpiryAlertSeverity.Urgent => $"{productName} expires in {daysToExpiry} days - consider markdown or priority sale.",
            ExpiryAlertSeverity.Warning => $"{productName} expires in {daysToExpiry} days - monitor closely.",
            _ => $"{productName} expires in {daysToExpiry} days."
        };
    }

    #endregion

    #region Private Helpers

    private async Task<ProductBatchConfiguration?> GetConfigurationEntityAsync(int productId)
    {
        var configs = await _configRepository.GetAllAsync();
        return configs.FirstOrDefault(c => c.ProductId == productId);
    }

    private async Task<BatchStockMovementDto> RecordMovementInternalAsync(
        int batchId,
        BatchMovementType movementType,
        int quantity,
        int quantityBefore,
        int quantityAfter,
        string referenceType,
        int referenceId,
        string? referenceNumber,
        decimal unitCost,
        int userId,
        string? notes)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        var movement = new BatchStockMovement
        {
            BatchId = batchId,
            ProductId = batch.ProductId,
            StoreId = batch.StoreId,
            MovementType = movementType,
            Quantity = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            MovedAt = DateTime.UtcNow,
            MovedByUserId = userId,
            UnitCost = unitCost,
            Notes = notes,
            IsActive = true
        };

        await _movementRepository.AddAsync(movement);
        return await MapToMovementDtoAsync(movement);
    }

    private async Task<ProductBatchDto> MapToBatchDtoAsync(ProductBatch batch)
    {
        var product = await _productRepository.GetByIdAsync(batch.ProductId);
        var store = await _storeRepository.GetByIdAsync(batch.StoreId);
        var supplier = batch.SupplierId.HasValue ? await _supplierRepository.GetByIdAsync(batch.SupplierId.Value) : null;
        var grn = batch.GrnId.HasValue ? await _grnRepository.GetByIdAsync(batch.GrnId.Value) : null;
        var user = await _userRepository.GetByIdAsync(batch.ReceivedByUserId);

        return new ProductBatchDto
        {
            Id = batch.Id,
            ProductId = batch.ProductId,
            ProductName = product?.Name ?? string.Empty,
            ProductCode = product?.Code ?? string.Empty,
            StoreId = batch.StoreId,
            StoreName = store?.Name ?? string.Empty,
            BatchNumber = batch.BatchNumber,
            ExpiryDate = batch.ExpiryDate,
            ManufactureDate = batch.ManufactureDate,
            InitialQuantity = batch.InitialQuantity,
            CurrentQuantity = batch.CurrentQuantity,
            ReservedQuantity = batch.ReservedQuantity,
            SoldQuantity = batch.SoldQuantity,
            DisposedQuantity = batch.DisposedQuantity,
            SupplierId = batch.SupplierId,
            SupplierName = supplier?.Name,
            GrnId = batch.GrnId,
            GrnNumber = grn?.GRNNumber,
            TransferReceiptId = batch.TransferReceiptId,
            ReceivedAt = batch.ReceivedAt,
            ReceivedByUserId = batch.ReceivedByUserId,
            ReceivedByUserName = user?.FullName ?? string.Empty,
            Status = batch.Status.ToString(),
            UnitCost = batch.UnitCost,
            Notes = batch.Notes,
            DaysUntilExpiry = batch.DaysUntilExpiry,
            IsExpired = batch.IsExpired,
            AvailableQuantity = batch.AvailableQuantity
        };
    }

    private async Task<ProductBatchConfigurationDto> MapToConfigDtoAsync(ProductBatchConfiguration config)
    {
        var product = await _productRepository.GetByIdAsync(config.ProductId);

        return new ProductBatchConfigurationDto
        {
            Id = config.Id,
            ProductId = config.ProductId,
            ProductName = product?.Name ?? string.Empty,
            ProductCode = product?.Code ?? string.Empty,
            RequiresBatchTracking = config.RequiresBatchTracking,
            RequiresExpiryDate = config.RequiresExpiryDate,
            ExpiryWarningDays = config.ExpiryWarningDays,
            ExpiryCriticalDays = config.ExpiryCriticalDays,
            ExpiredItemAction = config.ExpiredItemAction.ToString(),
            NearExpiryAction = config.NearExpiryAction.ToString(),
            UseFifo = config.UseFifo,
            UseFefo = config.UseFefo,
            TrackManufactureDate = config.TrackManufactureDate,
            MinimumShelfLifeDaysOnReceipt = config.MinimumShelfLifeDaysOnReceipt
        };
    }

    private async Task<BatchStockMovementDto> MapToMovementDtoAsync(BatchStockMovement movement)
    {
        var batch = await _batchRepository.GetByIdAsync(movement.BatchId);
        var product = await _productRepository.GetByIdAsync(movement.ProductId);
        var store = await _storeRepository.GetByIdAsync(movement.StoreId);
        var user = movement.MovedByUserId.HasValue ? await _userRepository.GetByIdAsync(movement.MovedByUserId.Value) : null;

        return new BatchStockMovementDto
        {
            Id = movement.Id,
            BatchId = movement.BatchId,
            BatchNumber = batch?.BatchNumber ?? string.Empty,
            ProductId = movement.ProductId,
            ProductName = product?.Name ?? string.Empty,
            StoreId = movement.StoreId,
            StoreName = store?.Name ?? string.Empty,
            MovementType = movement.MovementType.ToString(),
            Quantity = movement.Quantity,
            QuantityBefore = movement.QuantityBefore,
            QuantityAfter = movement.QuantityAfter,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            ReferenceNumber = movement.ReferenceNumber,
            MovedAt = movement.MovedAt,
            MovedByUserId = movement.MovedByUserId,
            MovedByUserName = user?.FullName,
            UnitCost = movement.UnitCost,
            TotalValue = movement.TotalValue,
            Notes = movement.Notes
        };
    }

    private async Task<BatchDisposalDto> MapToDisposalDtoAsync(BatchDisposal disposal)
    {
        var batch = await _batchRepository.GetByIdAsync(disposal.BatchId);
        var product = batch != null ? await _productRepository.GetByIdAsync(batch.ProductId) : null;
        var store = await _storeRepository.GetByIdAsync(disposal.StoreId);
        var approvedBy = await _userRepository.GetByIdAsync(disposal.ApprovedByUserId);
        var disposedBy = await _userRepository.GetByIdAsync(disposal.DisposedByUserId);

        return new BatchDisposalDto
        {
            Id = disposal.Id,
            BatchId = disposal.BatchId,
            BatchNumber = batch?.BatchNumber ?? string.Empty,
            ProductId = batch?.ProductId ?? 0,
            ProductName = product?.Name ?? string.Empty,
            StoreId = disposal.StoreId,
            StoreName = store?.Name ?? string.Empty,
            Quantity = disposal.Quantity,
            Reason = disposal.Reason.ToString(),
            Description = disposal.Description,
            DisposedAt = disposal.DisposedAt,
            ApprovedByUserId = disposal.ApprovedByUserId,
            ApprovedByUserName = approvedBy?.FullName ?? string.Empty,
            DisposedByUserId = disposal.DisposedByUserId,
            DisposedByUserName = disposedBy?.FullName ?? string.Empty,
            UnitCost = disposal.UnitCost,
            TotalValue = disposal.TotalValue,
            IsWitnessed = disposal.IsWitnessed,
            WitnessName = disposal.WitnessName,
            PhotoPath = disposal.PhotoPath
        };
    }

    #endregion
}
