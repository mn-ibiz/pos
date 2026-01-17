using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for managing stock transfers between stores and warehouses.
/// </summary>
public class StockTransferService : IStockTransferService
{
    private readonly IRepository<StockTransferRequest> _requestRepository;
    private readonly IRepository<TransferRequestLine> _lineRepository;
    private readonly IRepository<StockTransferShipment> _shipmentRepository;
    private readonly IRepository<StockTransferReceipt> _receiptRepository;
    private readonly IRepository<TransferReceiptLine> _receiptLineRepository;
    private readonly IRepository<TransferReceiptIssue> _issueRepository;
    private readonly IRepository<TransferActivityLog> _activityLogRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Inventory> _inventoryRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IStockReservationService _reservationService;

    public StockTransferService(
        IRepository<StockTransferRequest> requestRepository,
        IRepository<TransferRequestLine> lineRepository,
        IRepository<StockTransferShipment> shipmentRepository,
        IRepository<StockTransferReceipt> receiptRepository,
        IRepository<TransferReceiptLine> receiptLineRepository,
        IRepository<TransferReceiptIssue> issueRepository,
        IRepository<TransferActivityLog> activityLogRepository,
        IRepository<Store> storeRepository,
        IRepository<Product> productRepository,
        IRepository<Inventory> inventoryRepository,
        IRepository<User> userRepository,
        IStockReservationService reservationService)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
        _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
        _receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        _receiptLineRepository = receiptLineRepository ?? throw new ArgumentNullException(nameof(receiptLineRepository));
        _issueRepository = issueRepository ?? throw new ArgumentNullException(nameof(issueRepository));
        _activityLogRepository = activityLogRepository ?? throw new ArgumentNullException(nameof(activityLogRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    #region Transfer Request Management

    public async Task<StockTransferRequestDto> CreateTransferRequestAsync(CreateTransferRequestDto dto, int userId)
    {
        var request = new StockTransferRequest
        {
            RequestNumber = await GenerateRequestNumberAsync(),
            RequestingStoreId = dto.RequestingStoreId,
            SourceLocationId = dto.SourceLocationId,
            SourceLocationType = dto.SourceLocationType,
            Status = TransferRequestStatus.Draft,
            Priority = dto.Priority,
            Reason = dto.Reason,
            RequestedDeliveryDate = dto.RequestedDeliveryDate,
            Notes = dto.Notes,
            IsActive = true
        };

        await _requestRepository.AddAsync(request);

        // Add lines
        foreach (var lineDto in dto.Lines)
        {
            await AddLineToRequestAsync(request.Id, lineDto);
        }

        // Update totals
        await UpdateRequestTotalsAsync(request.Id);

        await LogActivityAsync(request.Id, "Transfer request created", userId, null, TransferRequestStatus.Draft);

        return await GetTransferRequestAsync(request.Id) ?? throw new InvalidOperationException("Failed to create transfer request");
    }

    public async Task<StockTransferRequestDto?> GetTransferRequestAsync(int requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            return null;

        return await MapToRequestDtoAsync(request);
    }

    public async Task<StockTransferRequestDto?> GetTransferRequestByNumberAsync(string requestNumber)
    {
        var requests = await _requestRepository.GetAllAsync();
        var request = requests.FirstOrDefault(r => r.RequestNumber == requestNumber);
        if (request == null)
            return null;

        return await MapToRequestDtoAsync(request);
    }

    public async Task<StockTransferRequestDto> UpdateTransferRequestAsync(int requestId, UpdateTransferRequestDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {requestId} not found");

        if (request.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Only draft requests can be updated");

        request.SourceLocationId = dto.SourceLocationId;
        request.SourceLocationType = dto.SourceLocationType;
        request.Priority = dto.Priority;
        request.Reason = dto.Reason;
        request.RequestedDeliveryDate = dto.RequestedDeliveryDate;
        request.Notes = dto.Notes;

        await _requestRepository.UpdateAsync(request);
        await LogActivityAsync(requestId, "Transfer request updated", userId);

        return await GetTransferRequestAsync(requestId) ?? throw new InvalidOperationException("Failed to update transfer request");
    }

    public async Task<bool> DeleteTransferRequestAsync(int requestId, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            return false;

        if (request.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Only draft requests can be deleted");

        request.IsActive = false;
        await _requestRepository.UpdateAsync(request);
        await LogActivityAsync(requestId, "Transfer request deleted", userId);

        return true;
    }

    public async Task<TransferRequestLineDto> AddRequestLineAsync(int requestId, CreateTransferRequestLineDto dto)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {requestId} not found");

        if (request.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to non-draft requests");

        var line = await AddLineToRequestAsync(requestId, dto);
        await UpdateRequestTotalsAsync(requestId);

        return await MapToLineDtoAsync(line);
    }

    public async Task<TransferRequestLineDto> UpdateRequestLineAsync(int lineId, int quantity, string? notes)
    {
        var line = await _lineRepository.GetByIdAsync(lineId);
        if (line == null)
            throw new InvalidOperationException($"Line {lineId} not found");

        var request = await _requestRepository.GetByIdAsync(line.TransferRequestId);
        if (request?.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Cannot update lines on non-draft requests");

        line.RequestedQuantity = quantity;
        line.Notes = notes;
        line.LineTotal = quantity * line.UnitCost;

        await _lineRepository.UpdateAsync(line);
        await UpdateRequestTotalsAsync(line.TransferRequestId);

        return await MapToLineDtoAsync(line);
    }

    public async Task<bool> RemoveRequestLineAsync(int lineId)
    {
        var line = await _lineRepository.GetByIdAsync(lineId);
        if (line == null)
            return false;

        var request = await _requestRepository.GetByIdAsync(line.TransferRequestId);
        if (request?.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Cannot remove lines from non-draft requests");

        line.IsActive = false;
        await _lineRepository.UpdateAsync(line);
        await UpdateRequestTotalsAsync(line.TransferRequestId);

        return true;
    }

    public async Task<StockTransferRequestDto> SubmitRequestAsync(int requestId, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {requestId} not found");

        if (request.Status != TransferRequestStatus.Draft)
            throw new InvalidOperationException("Only draft requests can be submitted");

        var lines = await _lineRepository.GetAllAsync();
        if (!lines.Any(l => l.TransferRequestId == requestId))
            throw new InvalidOperationException("Cannot submit request with no lines");

        var previousStatus = request.Status;
        request.Status = TransferRequestStatus.Submitted;
        request.SubmittedAt = DateTime.UtcNow;
        request.SubmittedByUserId = userId;

        await _requestRepository.UpdateAsync(request);
        await LogActivityAsync(requestId, "Transfer request submitted for approval", userId, previousStatus, TransferRequestStatus.Submitted);

        return await GetTransferRequestAsync(requestId) ?? throw new InvalidOperationException("Failed to submit request");
    }

    public async Task<StockTransferRequestDto> CancelRequestAsync(int requestId, int userId, string? reason = null)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {requestId} not found");

        if (request.Status == TransferRequestStatus.Received || request.Status == TransferRequestStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel completed or already cancelled requests");

        var previousStatus = request.Status;
        request.Status = TransferRequestStatus.Cancelled;
        request.RejectionReason = reason;

        await _requestRepository.UpdateAsync(request);

        // Release any stock reservations for this request
        try
        {
            var releasedReservations = await _reservationService.ReleaseByReferenceAsync(
                ReservationType.Transfer, requestId, userId, $"Transfer cancelled: {reason ?? "No reason"}");

            if (releasedReservations.Any())
            {
                await LogActivityAsync(requestId, $"Released {releasedReservations.Count} stock reservations", userId);
            }
        }
        catch (Exception ex)
        {
            await LogActivityAsync(requestId, $"Warning: Failed to release reservations: {ex.Message}", userId);
        }

        await LogActivityAsync(requestId, $"Transfer request cancelled: {reason ?? "No reason provided"}", userId, previousStatus, TransferRequestStatus.Cancelled);

        return await GetTransferRequestAsync(requestId) ?? throw new InvalidOperationException("Failed to cancel request");
    }

    public async Task<List<TransferRequestSummaryDto>> GetTransferRequestsAsync(TransferRequestQueryDto query)
    {
        var requests = await _requestRepository.GetAllAsync();
        var filtered = requests.AsQueryable();

        if (query.RequestingStoreId.HasValue)
            filtered = filtered.Where(r => r.RequestingStoreId == query.RequestingStoreId.Value);

        if (query.SourceLocationId.HasValue)
            filtered = filtered.Where(r => r.SourceLocationId == query.SourceLocationId.Value);

        if (query.Status.HasValue)
            filtered = filtered.Where(r => r.Status == query.Status.Value);

        if (query.Priority.HasValue)
            filtered = filtered.Where(r => r.Priority == query.Priority.Value);

        if (query.FromDate.HasValue)
            filtered = filtered.Where(r => r.CreatedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            filtered = filtered.Where(r => r.CreatedAt <= query.ToDate.Value);

        if (!string.IsNullOrEmpty(query.SearchTerm))
            filtered = filtered.Where(r => r.RequestNumber.Contains(query.SearchTerm));

        var result = filtered
            .OrderByDescending(r => r.CreatedAt)
            .Skip(query.Offset ?? 0)
            .Take(query.Limit ?? 50);

        var summaries = new List<TransferRequestSummaryDto>();
        foreach (var request in result)
        {
            summaries.Add(await MapToSummaryDtoAsync(request));
        }

        return summaries;
    }

    public async Task<List<TransferRequestSummaryDto>> GetPendingApprovalsAsync(int sourceLocationId)
    {
        return await GetTransferRequestsAsync(new TransferRequestQueryDto
        {
            SourceLocationId = sourceLocationId,
            Status = TransferRequestStatus.Submitted
        });
    }

    public async Task<List<TransferRequestSummaryDto>> GetStoreRequestsAsync(int storeId, TransferRequestStatus? status = null)
    {
        return await GetTransferRequestsAsync(new TransferRequestQueryDto
        {
            RequestingStoreId = storeId,
            Status = status
        });
    }

    #endregion

    #region Source Location and Stock

    public async Task<List<SourceLocationDto>> GetSourceLocationsAsync(int requestingStoreId)
    {
        var stores = await _storeRepository.GetAllAsync();
        return stores
            .Where(s => s.Id != requestingStoreId)
            .Select(s => new SourceLocationDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                LocationType = TransferLocationType.Store,
                IsActive = s.IsActive
            })
            .ToList();
    }

    public async Task<List<SourceProductStockDto>> GetSourceStockAsync(int sourceLocationId, string? searchTerm = null)
    {
        var products = await _productRepository.GetAllAsync();
        var inventories = await _inventoryRepository.GetAllAsync();

        var sourceInventory = inventories.Where(i => i.StoreId == sourceLocationId);

        var query = from p in products
                    join i in sourceInventory on p.Id equals i.ProductId into inv
                    from inventory in inv.DefaultIfEmpty()
                    where inventory != null && inventory.CurrentStock > 0
                    select new SourceProductStockDto
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ProductSku = p.SKU ?? string.Empty,
                        AvailableQuantity = inventory?.CurrentStock ?? 0,
                        ReservedQuantity = inventory?.ReservedStock ?? 0,
                        TransferableQuantity = (inventory?.CurrentStock ?? 0) - (inventory?.ReservedStock ?? 0),
                        UnitCost = p.CostPrice,
                        ReorderLevel = inventory?.ReorderLevel
                    };

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p =>
                p.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.ProductSku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    public async Task<SourceProductStockDto?> GetProductStockAtSourceAsync(int sourceLocationId, int productId)
    {
        var stock = await GetSourceStockAsync(sourceLocationId);
        return stock.FirstOrDefault(s => s.ProductId == productId);
    }

    public async Task<List<SourceProductStockDto>> GetSuggestedTransferProductsAsync(int requestingStoreId, int sourceLocationId)
    {
        var inventories = await _inventoryRepository.GetAllAsync();
        var requestingInventory = inventories.Where(i => i.StoreId == requestingStoreId).ToList();
        var sourceStock = await GetSourceStockAsync(sourceLocationId);

        // Find products that are low at requesting store but available at source
        var lowStockProductIds = requestingInventory
            .Where(i => i.ReorderLevel.HasValue && i.CurrentStock <= i.ReorderLevel)
            .Select(i => i.ProductId)
            .ToHashSet();

        return sourceStock
            .Where(s => lowStockProductIds.Contains(s.ProductId) && s.TransferableQuantity > 0)
            .OrderByDescending(s => s.TransferableQuantity)
            .ToList();
    }

    #endregion

    #region Approval Operations

    public async Task<StockTransferRequestDto> ApproveRequestAsync(ApproveTransferRequestDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(dto.RequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {dto.RequestId} not found");

        if (request.Status != TransferRequestStatus.Submitted)
            throw new InvalidOperationException("Only submitted requests can be approved");

        var previousStatus = request.Status;
        var isPartial = false;

        // Collect reservation lines for batch creation
        var reservationLines = new List<ReservationLineDto>();

        foreach (var lineApproval in dto.Lines)
        {
            var line = await _lineRepository.GetByIdAsync(lineApproval.LineId);
            if (line != null && line.TransferRequestId == dto.RequestId)
            {
                line.ApprovedQuantity = lineApproval.ApprovedQuantity;
                line.ApprovalNotes = lineApproval.Notes;
                await _lineRepository.UpdateAsync(line);

                if (lineApproval.ApprovedQuantity < line.RequestedQuantity)
                    isPartial = true;

                // Add to reservation list if approved quantity > 0
                if (lineApproval.ApprovedQuantity > 0)
                {
                    reservationLines.Add(new ReservationLineDto
                    {
                        ProductId = line.ProductId,
                        Quantity = lineApproval.ApprovedQuantity
                    });
                }
            }
        }

        request.Status = isPartial ? TransferRequestStatus.PartiallyApproved : TransferRequestStatus.Approved;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApprovedByUserId = userId;
        request.ApprovalNotes = dto.ApprovalNotes;
        request.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;

        await UpdateRequestTotalsAsync(dto.RequestId);
        await _requestRepository.UpdateAsync(request);

        // Create stock reservations for approved items
        if (reservationLines.Any())
        {
            try
            {
                await _reservationService.CreateBatchReservationsAsync(new CreateBatchReservationsDto
                {
                    LocationId = request.SourceLocationId,
                    LocationType = request.SourceLocationType,
                    ReferenceId = request.Id,
                    ReferenceType = ReservationType.Transfer,
                    ExpirationHours = 48, // Default 48 hour reservation
                    Lines = reservationLines
                }, userId);

                await LogActivityAsync(dto.RequestId, $"Stock reserved for {reservationLines.Count} products", userId, details: $"Total items reserved: {reservationLines.Sum(l => l.Quantity)}");
            }
            catch (InvalidOperationException ex)
            {
                // Log warning but continue - stock reservation is enhancement not blocking
                await LogActivityAsync(dto.RequestId, $"Stock reservation warning: {ex.Message}", userId);
            }
        }

        await LogActivityAsync(dto.RequestId, $"Request {(isPartial ? "partially " : "")}approved", userId, previousStatus, request.Status);

        return await GetTransferRequestAsync(dto.RequestId) ?? throw new InvalidOperationException("Failed to approve request");
    }

    public async Task<StockTransferRequestDto> RejectRequestAsync(RejectTransferRequestDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(dto.RequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {dto.RequestId} not found");

        if (request.Status != TransferRequestStatus.Submitted)
            throw new InvalidOperationException("Only submitted requests can be rejected");

        var previousStatus = request.Status;
        request.Status = TransferRequestStatus.Rejected;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApprovedByUserId = userId;
        request.RejectionReason = dto.RejectionReason;

        await _requestRepository.UpdateAsync(request);
        await LogActivityAsync(dto.RequestId, $"Request rejected: {dto.RejectionReason}", userId, previousStatus, TransferRequestStatus.Rejected);

        return await GetTransferRequestAsync(dto.RequestId) ?? throw new InvalidOperationException("Failed to reject request");
    }

    public async Task<List<TransferRequestSummaryDto>> GetRequestsAwaitingApprovalAsync()
    {
        return await GetTransferRequestsAsync(new TransferRequestQueryDto
        {
            Status = TransferRequestStatus.Submitted
        });
    }

    #endregion

    #region Shipment Operations

    public async Task<StockTransferShipmentDto> CreateShipmentAsync(CreateShipmentDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(dto.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {dto.TransferRequestId} not found");

        if (request.Status != TransferRequestStatus.Approved && request.Status != TransferRequestStatus.PartiallyApproved)
            throw new InvalidOperationException("Only approved requests can be shipped");

        var shipment = new StockTransferShipment
        {
            TransferRequestId = dto.TransferRequestId,
            ShipmentNumber = await GenerateShipmentNumberAsync(),
            ExpectedArrivalDate = dto.ExpectedArrivalDate,
            Carrier = dto.Carrier,
            TrackingNumber = dto.TrackingNumber,
            VehicleDetails = dto.VehicleDetails,
            DriverName = dto.DriverName,
            DriverContact = dto.DriverContact,
            PackageCount = dto.PackageCount,
            TotalWeightKg = dto.TotalWeightKg,
            Notes = dto.Notes,
            IsActive = true
        };

        await _shipmentRepository.AddAsync(shipment);

        // Update shipped quantities on lines
        foreach (var lineDto in dto.Lines)
        {
            var line = await _lineRepository.GetByIdAsync(lineDto.RequestLineId);
            if (line != null)
            {
                line.ShippedQuantity = lineDto.ShippedQuantity;
                await _lineRepository.UpdateAsync(line);
            }
        }

        await LogActivityAsync(dto.TransferRequestId, "Shipment created", userId);

        return await MapToShipmentDtoAsync(shipment);
    }

    public async Task<StockTransferShipmentDto?> GetShipmentAsync(int shipmentId)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        return shipment == null ? null : await MapToShipmentDtoAsync(shipment);
    }

    public async Task<StockTransferShipmentDto?> GetRequestShipmentAsync(int requestId)
    {
        var shipments = await _shipmentRepository.GetAllAsync();
        var shipment = shipments.FirstOrDefault(s => s.TransferRequestId == requestId);
        return shipment == null ? null : await MapToShipmentDtoAsync(shipment);
    }

    public async Task<StockTransferShipmentDto> UpdateShipmentAsync(int shipmentId, CreateShipmentDto dto)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new InvalidOperationException($"Shipment {shipmentId} not found");

        if (shipment.ShippedAt.HasValue)
            throw new InvalidOperationException("Cannot update dispatched shipment");

        shipment.ExpectedArrivalDate = dto.ExpectedArrivalDate;
        shipment.Carrier = dto.Carrier;
        shipment.TrackingNumber = dto.TrackingNumber;
        shipment.VehicleDetails = dto.VehicleDetails;
        shipment.DriverName = dto.DriverName;
        shipment.DriverContact = dto.DriverContact;
        shipment.PackageCount = dto.PackageCount;
        shipment.TotalWeightKg = dto.TotalWeightKg;
        shipment.Notes = dto.Notes;

        await _shipmentRepository.UpdateAsync(shipment);

        return await MapToShipmentDtoAsync(shipment);
    }

    public async Task<StockTransferShipmentDto> DispatchShipmentAsync(int shipmentId, int userId)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new InvalidOperationException($"Shipment {shipmentId} not found");

        if (shipment.ShippedAt.HasValue)
            throw new InvalidOperationException("Shipment already dispatched");

        // Get request and lines for stock deduction
        var request = await _requestRepository.GetByIdAsync(shipment.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {shipment.TransferRequestId} not found");

        var lines = await _lineRepository.GetAllAsync();
        var requestLines = lines.Where(l => l.TransferRequestId == request.Id && l.IsActive).ToList();

        // Deduct stock from source location
        foreach (var line in requestLines.Where(l => l.ShippedQuantity > 0))
        {
            await DeductInventoryFromSourceAsync(request.SourceLocationId, line.ProductId, line.ShippedQuantity);
        }

        await LogActivityAsync(request.Id, $"Stock deducted from source for {requestLines.Count(l => l.ShippedQuantity > 0)} products", userId,
            details: $"Total items shipped: {requestLines.Sum(l => l.ShippedQuantity)}");

        shipment.ShippedAt = DateTime.UtcNow;
        shipment.ShippedByUserId = userId;
        await _shipmentRepository.UpdateAsync(shipment);

        // Update request status
        var previousStatus = request.Status;
        request.Status = TransferRequestStatus.InTransit;
        await _requestRepository.UpdateAsync(request);

        // Fulfill stock reservations - stock is now being shipped
        try
        {
            var fulfilledReservations = await _reservationService.FulfillByReferenceAsync(
                ReservationType.Transfer, shipment.TransferRequestId, userId);

            if (fulfilledReservations.Any())
            {
                await LogActivityAsync(shipment.TransferRequestId, $"Fulfilled {fulfilledReservations.Count} stock reservations on dispatch", userId);
            }
        }
        catch (Exception ex)
        {
            await LogActivityAsync(shipment.TransferRequestId, $"Warning: Failed to fulfill reservations: {ex.Message}", userId);
        }

        await LogActivityAsync(shipment.TransferRequestId, "Shipment dispatched", userId, previousStatus, TransferRequestStatus.InTransit);

        return await MapToShipmentDtoAsync(shipment);
    }

    public async Task<List<StockTransferShipmentDto>> GetShipmentsInTransitAsync(int? destinationStoreId = null)
    {
        var requests = await _requestRepository.GetAllAsync();
        var inTransitRequests = requests.Where(r => r.Status == TransferRequestStatus.InTransit);

        if (destinationStoreId.HasValue)
            inTransitRequests = inTransitRequests.Where(r => r.RequestingStoreId == destinationStoreId.Value);

        var result = new List<StockTransferShipmentDto>();
        foreach (var request in inTransitRequests)
        {
            var shipment = await GetRequestShipmentAsync(request.Id);
            if (shipment != null)
                result.Add(shipment);
        }

        return result;
    }

    public async Task<List<TransferRequestSummaryDto>> GetRequestsAwaitingShipmentAsync(int? sourceLocationId = null)
    {
        var query = new TransferRequestQueryDto
        {
            SourceLocationId = sourceLocationId
        };

        var requests = await _requestRepository.GetAllAsync();
        var awaitingShipment = requests.Where(r =>
            r.Status == TransferRequestStatus.Approved ||
            r.Status == TransferRequestStatus.PartiallyApproved);

        if (sourceLocationId.HasValue)
            awaitingShipment = awaitingShipment.Where(r => r.SourceLocationId == sourceLocationId.Value);

        // Filter out those that already have shipments
        var shipments = await _shipmentRepository.GetAllAsync();
        var shippedRequestIds = shipments.Select(s => s.TransferRequestId).ToHashSet();
        awaitingShipment = awaitingShipment.Where(r => !shippedRequestIds.Contains(r.Id));

        var result = new List<TransferRequestSummaryDto>();
        foreach (var request in awaitingShipment)
        {
            result.Add(await MapToSummaryDtoAsync(request));
        }

        return result;
    }

    #endregion

    #region Pick List Operations

    public async Task<PickListDto> GetPickListAsync(int requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {requestId} not found");

        if (request.Status != TransferRequestStatus.Approved &&
            request.Status != TransferRequestStatus.PartiallyApproved)
            throw new InvalidOperationException("Pick list can only be generated for approved requests");

        var stores = await _storeRepository.GetAllAsync();
        var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);
        var destinationStore = stores.FirstOrDefault(s => s.Id == request.RequestingStoreId);

        var lines = await _lineRepository.GetAllAsync();
        var requestLines = lines.Where(l => l.TransferRequestId == requestId && l.IsActive).ToList();
        var products = await _productRepository.GetAllAsync();

        var pickListLines = new List<PickListLineDto>();
        foreach (var line in requestLines.Where(l => l.ApprovedQuantity > 0))
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);
            pickListLines.Add(new PickListLineDto
            {
                RequestLineId = line.Id,
                ProductId = line.ProductId,
                ProductName = product?.Name ?? "Unknown",
                ProductSku = product?.SKU ?? string.Empty,
                Barcode = product?.Barcode,
                ApprovedQuantity = line.ApprovedQuantity ?? 0,
                PickedQuantity = line.ShippedQuantity,
                IsPicked = line.ShippedQuantity >= (line.ApprovedQuantity ?? 0),
                Notes = line.Notes
            });
        }

        var totalApproved = pickListLines.Sum(l => l.ApprovedQuantity);
        var totalPicked = pickListLines.Sum(l => l.PickedQuantity);

        // Determine pick list status based on shipped quantities
        var status = PickListStatus.Pending;
        if (totalPicked > 0 && totalPicked >= totalApproved)
            status = PickListStatus.Completed;
        else if (totalPicked > 0)
            status = PickListStatus.InProgress;

        return new PickListDto
        {
            TransferRequestId = requestId,
            RequestNumber = request.RequestNumber,
            SourceLocationName = sourceLocation?.Name ?? "Unknown",
            DestinationStoreName = destinationStore?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Status = status,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Priority = request.Priority,
            TotalItems = pickListLines.Count,
            TotalQuantity = totalApproved,
            PickedItems = pickListLines.Count(l => l.IsPicked),
            PickedQuantity = totalPicked,
            Lines = pickListLines,
            Notes = request.Notes
        };
    }

    public async Task<PickListDto> ConfirmPicksAsync(ConfirmAllPicksDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(dto.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {dto.TransferRequestId} not found");

        if (request.Status != TransferRequestStatus.Approved &&
            request.Status != TransferRequestStatus.PartiallyApproved)
            throw new InvalidOperationException("Can only confirm picks for approved requests");

        foreach (var pickDto in dto.Lines)
        {
            var line = await _lineRepository.GetByIdAsync(pickDto.RequestLineId);
            if (line != null && line.TransferRequestId == dto.TransferRequestId)
            {
                if (pickDto.PickedQuantity > (line.ApprovedQuantity ?? 0))
                    throw new InvalidOperationException($"Picked quantity cannot exceed approved quantity for line {pickDto.RequestLineId}");

                line.ShippedQuantity = pickDto.PickedQuantity;
                if (!string.IsNullOrEmpty(pickDto.Notes))
                    line.Notes = pickDto.Notes;

                await _lineRepository.UpdateAsync(line);
            }
        }

        await LogActivityAsync(dto.TransferRequestId, $"Pick confirmed for {dto.Lines.Count} items", userId,
            details: $"Total picked: {dto.Lines.Sum(l => l.PickedQuantity)}");

        return await GetPickListAsync(dto.TransferRequestId);
    }

    public async Task<List<PickListDto>> GetPendingPickListsAsync(int? sourceLocationId = null)
    {
        var requests = await _requestRepository.GetAllAsync();
        var approvedRequests = requests.Where(r =>
            r.Status == TransferRequestStatus.Approved ||
            r.Status == TransferRequestStatus.PartiallyApproved);

        if (sourceLocationId.HasValue)
            approvedRequests = approvedRequests.Where(r => r.SourceLocationId == sourceLocationId.Value);

        // Exclude requests that already have dispatched shipments
        var shipments = await _shipmentRepository.GetAllAsync();
        var dispatchedRequestIds = shipments
            .Where(s => s.ShippedAt.HasValue)
            .Select(s => s.TransferRequestId)
            .ToHashSet();

        approvedRequests = approvedRequests.Where(r => !dispatchedRequestIds.Contains(r.Id));

        var result = new List<PickListDto>();
        foreach (var request in approvedRequests.OrderBy(r => r.Priority).ThenBy(r => r.ExpectedDeliveryDate))
        {
            result.Add(await GetPickListAsync(request.Id));
        }

        return result;
    }

    #endregion

    #region Transfer Document Operations

    public async Task<TransferDocumentDto> GenerateTransferDocumentAsync(int shipmentId)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new InvalidOperationException($"Shipment {shipmentId} not found");

        var request = await _requestRepository.GetByIdAsync(shipment.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {shipment.TransferRequestId} not found");

        var stores = await _storeRepository.GetAllAsync();
        var users = await _userRepository.GetAllAsync();
        var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);
        var destinationStore = stores.FirstOrDefault(s => s.Id == request.RequestingStoreId);

        var lines = await _lineRepository.GetAllAsync();
        var requestLines = lines.Where(l => l.TransferRequestId == request.Id && l.IsActive).ToList();
        var products = await _productRepository.GetAllAsync();

        var documentLines = new List<TransferDocumentLineDto>();
        var lineNumber = 1;
        foreach (var line in requestLines.Where(l => l.ShippedQuantity > 0))
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);
            documentLines.Add(new TransferDocumentLineDto
            {
                LineNumber = lineNumber++,
                ProductId = line.ProductId,
                ProductName = product?.Name ?? "Unknown",
                ProductSku = product?.SKU ?? string.Empty,
                Barcode = product?.Barcode,
                RequestedQuantity = line.RequestedQuantity,
                ApprovedQuantity = line.ApprovedQuantity ?? 0,
                ShippedQuantity = line.ShippedQuantity,
                UnitCost = line.UnitCost,
                LineTotal = line.ShippedQuantity * line.UnitCost,
                Notes = line.Notes
            });
        }

        return new TransferDocumentDto
        {
            ShipmentId = shipment.Id,
            ShipmentNumber = shipment.ShipmentNumber,
            RequestNumber = request.RequestNumber,
            GeneratedAt = DateTime.UtcNow,

            // Source information
            SourceLocationName = sourceLocation?.Name ?? "Unknown",
            SourceAddress = sourceLocation?.Address,
            SourcePhone = sourceLocation?.Phone,

            // Destination information
            DestinationStoreName = destinationStore?.Name ?? "Unknown",
            DestinationAddress = destinationStore?.Address,
            DestinationPhone = destinationStore?.Phone,

            // Shipment details
            DispatchedAt = shipment.ShippedAt,
            DispatchedByUserName = users.FirstOrDefault(u => u.Id == shipment.ShippedByUserId)?.Username,
            ExpectedArrivalDate = shipment.ExpectedArrivalDate,
            Carrier = shipment.Carrier,
            TrackingNumber = shipment.TrackingNumber,
            VehicleDetails = shipment.VehicleDetails,
            DriverName = shipment.DriverName,
            DriverContact = shipment.DriverContact,
            PackageCount = shipment.PackageCount,
            TotalWeightKg = shipment.TotalWeightKg,

            // Line items
            TotalItems = documentLines.Count,
            TotalQuantity = documentLines.Sum(l => l.ShippedQuantity),
            TotalValue = documentLines.Sum(l => l.LineTotal),
            Lines = documentLines,

            // Additional info
            PreparedByName = users.FirstOrDefault(u => u.Id == shipment.ShippedByUserId)?.Username,
            Notes = shipment.Notes
        };
    }

    public async Task<TransferDocumentDto?> GetTransferDocumentForRequestAsync(int requestId)
    {
        var shipment = await GetRequestShipmentAsync(requestId);
        if (shipment == null)
            return null;

        return await GenerateTransferDocumentAsync(shipment.Id);
    }

    #endregion

    #region Receipt Operations

    public async Task<StockTransferReceiptDto> CreateReceiptAsync(CreateReceiptDto dto, int userId)
    {
        var request = await _requestRepository.GetByIdAsync(dto.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {dto.TransferRequestId} not found");

        if (request.Status != TransferRequestStatus.InTransit)
            throw new InvalidOperationException("Can only receive in-transit transfers");

        var receipt = new StockTransferReceipt
        {
            TransferRequestId = dto.TransferRequestId,
            ReceiptNumber = await GenerateReceiptNumberAsync(),
            ReceivedAt = DateTime.UtcNow,
            ReceivedByUserId = userId,
            Notes = dto.Notes,
            IsActive = true
        };

        await _receiptRepository.AddAsync(receipt);

        var hasIssues = false;

        foreach (var lineDto in dto.Lines)
        {
            var requestLine = await _lineRepository.GetByIdAsync(lineDto.RequestLineId);
            if (requestLine != null)
            {
                var receiptLine = new TransferReceiptLine
                {
                    TransferReceiptId = receipt.Id,
                    TransferRequestLineId = lineDto.RequestLineId,
                    ProductId = requestLine.ProductId,
                    ExpectedQuantity = requestLine.ShippedQuantity,
                    ReceivedQuantity = lineDto.ReceivedQuantity,
                    IssueQuantity = lineDto.IssueQuantity,
                    Notes = lineDto.Notes,
                    IsActive = true
                };

                await _receiptLineRepository.AddAsync(receiptLine);

                // Update request line
                requestLine.ReceivedQuantity = lineDto.ReceivedQuantity;
                requestLine.IssueQuantity = lineDto.IssueQuantity;
                await _lineRepository.UpdateAsync(requestLine);

                // Log issues
                if (lineDto.Issues != null)
                {
                    foreach (var issueDto in lineDto.Issues)
                    {
                        await LogIssueInternalAsync(receipt.Id, receiptLine.Id, issueDto);
                        hasIssues = true;
                    }
                }
            }
        }

        receipt.HasIssues = hasIssues;
        await _receiptRepository.UpdateAsync(receipt);

        await LogActivityAsync(dto.TransferRequestId, "Receipt created", userId);

        return await GetReceiptAsync(receipt.Id) ?? throw new InvalidOperationException("Failed to create receipt");
    }

    public async Task<StockTransferReceiptDto?> GetReceiptAsync(int receiptId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(receiptId);
        return receipt == null ? null : await MapToReceiptDtoAsync(receipt);
    }

    public async Task<StockTransferReceiptDto?> GetRequestReceiptAsync(int requestId)
    {
        var receipts = await _receiptRepository.GetAllAsync();
        var receipt = receipts.FirstOrDefault(r => r.TransferRequestId == requestId);
        return receipt == null ? null : await MapToReceiptDtoAsync(receipt);
    }

    public async Task<List<TransferRequestSummaryDto>> GetTransfersAwaitingReceiptAsync(int? storeId = null)
    {
        var requests = await _requestRepository.GetAllAsync();
        var inTransit = requests.Where(r => r.Status == TransferRequestStatus.InTransit);

        if (storeId.HasValue)
            inTransit = inTransit.Where(r => r.RequestingStoreId == storeId.Value);

        var result = new List<TransferRequestSummaryDto>();
        foreach (var request in inTransit)
        {
            result.Add(await MapToSummaryDtoAsync(request));
        }

        return result;
    }

    public async Task<List<PendingReceiptDto>> GetPendingReceiptsAsync(int? storeId = null)
    {
        var requests = await _requestRepository.GetAllAsync();
        var inTransit = requests.Where(r => r.Status == TransferRequestStatus.InTransit);

        if (storeId.HasValue)
            inTransit = inTransit.Where(r => r.RequestingStoreId == storeId.Value);

        var stores = await _storeRepository.GetAllAsync();
        var shipments = await _shipmentRepository.GetAllAsync();
        var lines = await _lineRepository.GetAllAsync();

        var result = new List<PendingReceiptDto>();
        foreach (var request in inTransit.OrderBy(r => r.ExpectedDeliveryDate ?? DateTime.MaxValue))
        {
            var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);
            var shipment = shipments.FirstOrDefault(s => s.TransferRequestId == request.Id);
            var requestLines = lines.Where(l => l.TransferRequestId == request.Id && l.IsActive).ToList();

            result.Add(new PendingReceiptDto
            {
                TransferRequestId = request.Id,
                RequestNumber = request.RequestNumber,
                SourceLocationName = sourceLocation?.Name ?? "Unknown",
                TotalExpectedItems = requestLines.Count,
                TotalExpectedQuantity = requestLines.Sum(l => l.ShippedQuantity),
                TotalExpectedValue = requestLines.Sum(l => l.ShippedQuantity * l.UnitCost),
                DispatchedAt = shipment?.ShippedAt,
                ExpectedArrivalDate = shipment?.ExpectedArrivalDate,
                ShipmentNumber = shipment?.ShipmentNumber,
                TrackingNumber = shipment?.TrackingNumber,
                DriverName = shipment?.DriverName,
                Priority = request.Priority
            });
        }

        return result;
    }

    public async Task<ReceivingSummaryDto> GetReceivingSummaryAsync(int receiptId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(receiptId);
        if (receipt == null)
            throw new InvalidOperationException($"Receipt {receiptId} not found");

        var request = await _requestRepository.GetByIdAsync(receipt.TransferRequestId);
        if (request == null)
            throw new InvalidOperationException($"Transfer request {receipt.TransferRequestId} not found");

        var stores = await _storeRepository.GetAllAsync();
        var users = await _userRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();
        var receiptLines = await _receiptLineRepository.GetAllAsync();
        var requestLines = await _lineRepository.GetAllAsync();
        var issues = await _issueRepository.GetAllAsync();

        var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);
        var destinationStore = stores.FirstOrDefault(s => s.Id == request.RequestingStoreId);
        var receivedByUser = users.FirstOrDefault(u => u.Id == receipt.ReceivedByUserId);

        var receiptLineItems = receiptLines.Where(rl => rl.TransferReceiptId == receiptId).ToList();
        var lineIssues = issues.Where(i => i.TransferReceiptId == receiptId).ToList();

        var varianceLines = new List<ReceivingLineVarianceDto>();
        foreach (var rl in receiptLineItems)
        {
            var product = products.FirstOrDefault(p => p.Id == rl.ProductId);
            var requestLine = requestLines.FirstOrDefault(l => l.Id == rl.TransferRequestLineId);
            var unitCost = requestLine?.UnitCost ?? 0;

            varianceLines.Add(new ReceivingLineVarianceDto
            {
                ReceiptLineId = rl.Id,
                ProductId = rl.ProductId,
                ProductName = product?.Name ?? "Unknown",
                ProductSku = product?.SKU ?? string.Empty,
                ExpectedQuantity = rl.ExpectedQuantity,
                ReceivedQuantity = rl.ReceivedQuantity,
                IssueQuantity = rl.IssueQuantity,
                UnitCost = unitCost,
                Issues = lineIssues
                    .Where(i => i.TransferReceiptLineId == rl.Id)
                    .Select(MapToIssueDto)
                    .ToList()
            });
        }

        var totalExpected = varianceLines.Sum(l => l.ExpectedQuantity);
        var totalReceived = varianceLines.Sum(l => l.ReceivedQuantity);
        var totalIssue = varianceLines.Sum(l => l.IssueQuantity);

        return new ReceivingSummaryDto
        {
            ReceiptId = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            TransferRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            SourceLocationName = sourceLocation?.Name ?? "Unknown",
            DestinationStoreName = destinationStore?.Name ?? "Unknown",
            ReceivedAt = receipt.ReceivedAt,
            ReceivedByUserName = receivedByUser?.Username,
            IsComplete = receipt.IsComplete,
            TotalExpected = totalExpected,
            TotalReceived = totalReceived,
            TotalIssueQuantity = totalIssue,
            TotalExpectedValue = varianceLines.Sum(l => l.ExpectedQuantity * l.UnitCost),
            TotalReceivedValue = varianceLines.Sum(l => l.ReceivedQuantity * l.UnitCost),
            VarianceValue = varianceLines.Sum(l => l.VarianceValue),
            Lines = varianceLines,
            UnresolvedIssueCount = lineIssues.Count(i => !i.IsResolved),
            ResolvedIssueCount = lineIssues.Count(i => i.IsResolved)
        };
    }

    public async Task<List<VarianceInvestigationDto>> GetReceiptsWithVarianceAsync(int? storeId = null)
    {
        var receipts = await _receiptRepository.GetAllAsync();
        var requests = await _requestRepository.GetAllAsync();
        var receiptLines = await _receiptLineRepository.GetAllAsync();
        var issues = await _issueRepository.GetAllAsync();
        var stores = await _storeRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();
        var requestLines = await _lineRepository.GetAllAsync();
        var users = await _userRepository.GetAllAsync();

        var result = new List<VarianceInvestigationDto>();

        foreach (var receipt in receipts.Where(r => r.IsActive))
        {
            var request = requests.FirstOrDefault(r => r.Id == receipt.TransferRequestId);
            if (request == null) continue;

            if (storeId.HasValue && request.RequestingStoreId != storeId.Value)
                continue;

            var lines = receiptLines.Where(rl => rl.TransferReceiptId == receipt.Id).ToList();
            var receiptIssues = issues.Where(i => i.TransferReceiptId == receipt.Id).ToList();

            // Calculate variance
            var totalExpected = lines.Sum(l => l.ExpectedQuantity);
            var totalReceived = lines.Sum(l => l.ReceivedQuantity);
            var variance = totalReceived - totalExpected;

            // Only include if there's variance or unresolved issues
            if (variance == 0 && !receiptIssues.Any(i => !i.IsResolved))
                continue;

            var varianceLines = new List<ReceivingLineVarianceDto>();
            foreach (var rl in lines.Where(l => l.ReceivedQuantity != l.ExpectedQuantity))
            {
                var product = products.FirstOrDefault(p => p.Id == rl.ProductId);
                var requestLine = requestLines.FirstOrDefault(l => l.Id == rl.TransferRequestLineId);
                var unitCost = requestLine?.UnitCost ?? 0;

                varianceLines.Add(new ReceivingLineVarianceDto
                {
                    ReceiptLineId = rl.Id,
                    ProductId = rl.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    ProductSku = product?.SKU ?? string.Empty,
                    ExpectedQuantity = rl.ExpectedQuantity,
                    ReceivedQuantity = rl.ReceivedQuantity,
                    IssueQuantity = rl.IssueQuantity,
                    UnitCost = unitCost,
                    Issues = receiptIssues
                        .Where(i => i.TransferReceiptLineId == rl.Id)
                        .Select(MapToIssueDto)
                        .ToList()
                });
            }

            var varianceValue = varianceLines.Sum(l => l.VarianceValue);
            var unresolvedCount = receiptIssues.Count(i => !i.IsResolved);

            // Determine investigation status
            var status = VarianceInvestigationStatus.Pending;
            if (unresolvedCount == 0 && varianceLines.Any())
                status = VarianceInvestigationStatus.Resolved;
            else if (unresolvedCount > 0)
                status = VarianceInvestigationStatus.InProgress;

            result.Add(new VarianceInvestigationDto
            {
                ReceiptId = receipt.Id,
                ReceiptNumber = receipt.ReceiptNumber,
                TransferRequestId = request.Id,
                RequestNumber = request.RequestNumber,
                ReceivedAt = receipt.ReceivedAt,
                TotalVarianceValue = varianceValue,
                TotalVarianceQuantity = variance,
                UnresolvedIssueCount = unresolvedCount,
                Status = status,
                VarianceLines = varianceLines
            });
        }

        return result.OrderByDescending(r => Math.Abs(r.TotalVarianceValue)).ToList();
    }

    public async Task<StockTransferReceiptDto> CompleteReceiptAsync(int receiptId, int userId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(receiptId);
        if (receipt == null)
            throw new InvalidOperationException($"Receipt {receiptId} not found");

        receipt.IsComplete = true;
        await _receiptRepository.UpdateAsync(receipt);

        // Update request status
        var request = await _requestRepository.GetByIdAsync(receipt.TransferRequestId);
        if (request != null)
        {
            var previousStatus = request.Status;

            // Check if fully received
            var lines = await _lineRepository.GetAllAsync();
            var requestLines = lines.Where(l => l.TransferRequestId == request.Id).ToList();
            var allReceived = requestLines.All(l => l.ReceivedQuantity >= l.ShippedQuantity);

            request.Status = allReceived ? TransferRequestStatus.Received : TransferRequestStatus.PartiallyReceived;
            await _requestRepository.UpdateAsync(request);

            // Update inventory
            foreach (var line in requestLines)
            {
                await UpdateInventoryForReceiptAsync(request.RequestingStoreId, line.ProductId, line.ReceivedQuantity);
            }

            await LogActivityAsync(request.Id, "Receipt completed, inventory updated", userId, previousStatus, request.Status);
        }

        return await GetReceiptAsync(receiptId) ?? throw new InvalidOperationException("Failed to complete receipt");
    }

    #endregion

    #region Issue Management

    public async Task<TransferReceiptIssueDto> LogIssueAsync(int receiptLineId, CreateReceiptIssueDto dto)
    {
        var receiptLine = await _receiptLineRepository.GetByIdAsync(receiptLineId);
        if (receiptLine == null)
            throw new InvalidOperationException($"Receipt line {receiptLineId} not found");

        return await LogIssueInternalAsync(receiptLine.TransferReceiptId, receiptLineId, dto);
    }

    private async Task<TransferReceiptIssueDto> LogIssueInternalAsync(int receiptId, int receiptLineId, CreateReceiptIssueDto dto)
    {
        var issue = new TransferReceiptIssue
        {
            TransferReceiptId = receiptId,
            TransferReceiptLineId = receiptLineId,
            IssueType = dto.IssueType,
            AffectedQuantity = dto.AffectedQuantity,
            Description = dto.Description,
            PhotoPath = dto.PhotoPath,
            IsActive = true
        };

        await _issueRepository.AddAsync(issue);
        return MapToIssueDtoAsync(issue);
    }

    public async Task<TransferReceiptIssueDto> ResolveIssueAsync(ResolveIssueDto dto, int userId)
    {
        var issue = await _issueRepository.GetByIdAsync(dto.IssueId);
        if (issue == null)
            throw new InvalidOperationException($"Issue {dto.IssueId} not found");

        issue.IsResolved = true;
        issue.ResolutionNotes = dto.ResolutionNotes;
        issue.ResolvedAt = DateTime.UtcNow;
        issue.ResolvedByUserId = userId;

        await _issueRepository.UpdateAsync(issue);
        return MapToIssueDtoAsync(issue);
    }

    public async Task<List<TransferReceiptIssueDto>> GetUnresolvedIssuesAsync(int? storeId = null)
    {
        var issues = await _issueRepository.GetAllAsync();
        var unresolved = issues.Where(i => !i.IsResolved);

        // If store filter needed, we'd need to join through receipts and requests
        return unresolved.Select(MapToIssueDtoAsync).ToList();
    }

    public async Task<List<TransferReceiptIssueDto>> GetReceiptIssuesAsync(int receiptId)
    {
        var issues = await _issueRepository.GetAllAsync();
        return issues.Where(i => i.TransferReceiptId == receiptId).Select(MapToIssueDtoAsync).ToList();
    }

    #endregion

    #region Activity Logging

    public async Task<List<TransferActivityLogDto>> GetActivityLogAsync(int requestId)
    {
        var logs = await _activityLogRepository.GetAllAsync();
        var users = await _userRepository.GetAllAsync();

        return logs
            .Where(l => l.TransferRequestId == requestId)
            .OrderByDescending(l => l.PerformedAt)
            .Select(l => new TransferActivityLogDto
            {
                Id = l.Id,
                TransferRequestId = l.TransferRequestId,
                Activity = l.Activity,
                PreviousStatus = l.PreviousStatus,
                NewStatus = l.NewStatus,
                PerformedByUserName = users.FirstOrDefault(u => u.Id == l.PerformedByUserId)?.Username ?? "Unknown",
                PerformedAt = l.PerformedAt,
                Details = l.Details
            })
            .ToList();
    }

    public async Task LogActivityAsync(int requestId, string activity, int userId, TransferRequestStatus? previousStatus = null, TransferRequestStatus? newStatus = null, string? details = null)
    {
        var log = new TransferActivityLog
        {
            TransferRequestId = requestId,
            Activity = activity,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow,
            Details = details,
            IsActive = true
        };

        await _activityLogRepository.AddAsync(log);
    }

    #endregion

    #region Dashboard and Reporting

    public async Task<StoreTransferDashboardDto> GetStoreDashboardAsync(int storeId)
    {
        var stores = await _storeRepository.GetAllAsync();
        var store = stores.FirstOrDefault(s => s.Id == storeId);

        var requests = await _requestRepository.GetAllAsync();
        var outgoing = requests.Where(r => r.RequestingStoreId == storeId).ToList();
        var incoming = requests.Where(r => r.SourceLocationId == storeId).ToList();

        var issues = await GetUnresolvedIssuesAsync(storeId);

        var recentOutgoing = new List<TransferRequestSummaryDto>();
        foreach (var r in outgoing.OrderByDescending(r => r.CreatedAt).Take(5))
        {
            recentOutgoing.Add(await MapToSummaryDtoAsync(r));
        }

        var recentIncoming = new List<TransferRequestSummaryDto>();
        foreach (var r in incoming.OrderByDescending(r => r.CreatedAt).Take(5))
        {
            recentIncoming.Add(await MapToSummaryDtoAsync(r));
        }

        return new StoreTransferDashboardDto
        {
            StoreId = storeId,
            StoreName = store?.Name ?? "Unknown",
            OutgoingDraftCount = outgoing.Count(r => r.Status == TransferRequestStatus.Draft),
            OutgoingSubmittedCount = outgoing.Count(r => r.Status == TransferRequestStatus.Submitted),
            OutgoingApprovedCount = outgoing.Count(r => r.Status == TransferRequestStatus.Approved || r.Status == TransferRequestStatus.PartiallyApproved),
            OutgoingInTransitCount = outgoing.Count(r => r.Status == TransferRequestStatus.InTransit),
            IncomingPendingApprovalCount = incoming.Count(r => r.Status == TransferRequestStatus.Submitted),
            IncomingApprovedCount = incoming.Count(r => r.Status == TransferRequestStatus.Approved || r.Status == TransferRequestStatus.PartiallyApproved),
            IncomingToShipCount = incoming.Count(r => (r.Status == TransferRequestStatus.Approved || r.Status == TransferRequestStatus.PartiallyApproved)),
            RecentOutgoing = recentOutgoing,
            RecentIncoming = recentIncoming,
            UnresolvedIssuesCount = issues.Count
        };
    }

    public async Task<ChainTransferDashboardDto> GetChainDashboardAsync()
    {
        var requests = await _requestRepository.GetAllAsync();
        var stores = await _storeRepository.GetAllAsync();

        var activeStatuses = new[]
        {
            TransferRequestStatus.Submitted,
            TransferRequestStatus.Approved,
            TransferRequestStatus.PartiallyApproved,
            TransferRequestStatus.InTransit
        };

        var activeRequests = requests.Where(r => activeStatuses.Contains(r.Status)).ToList();

        var recentTransfers = new List<TransferRequestSummaryDto>();
        foreach (var r in requests.OrderByDescending(r => r.CreatedAt).Take(10))
        {
            recentTransfers.Add(await MapToSummaryDtoAsync(r));
        }

        return new ChainTransferDashboardDto
        {
            TotalActiveTransfers = activeRequests.Count,
            TotalPendingApprovals = requests.Count(r => r.Status == TransferRequestStatus.Submitted),
            TotalInTransit = requests.Count(r => r.Status == TransferRequestStatus.InTransit),
            TotalPendingReceipt = requests.Count(r => r.Status == TransferRequestStatus.InTransit),
            TotalUnresolvedIssues = (await GetUnresolvedIssuesAsync()).Count,
            TotalValueInTransit = requests.Where(r => r.Status == TransferRequestStatus.InTransit).Sum(r => r.TotalEstimatedValue),
            CountByStatus = requests.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count()),
            CountByPriority = activeRequests.GroupBy(r => r.Priority).ToDictionary(g => g.Key, g => g.Count()),
            RecentTransfers = recentTransfers
        };
    }

    public async Task<TransferStatisticsDto> GetStatisticsAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var requests = await _requestRepository.GetAllAsync();

        if (storeId.HasValue)
            requests = requests.Where(r => r.RequestingStoreId == storeId.Value || r.SourceLocationId == storeId.Value);

        if (fromDate.HasValue)
            requests = requests.Where(r => r.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            requests = requests.Where(r => r.CreatedAt <= toDate.Value);

        var requestList = requests.ToList();
        var completedRequests = requestList.Where(r => r.Status == TransferRequestStatus.Received).ToList();

        var issues = await _issueRepository.GetAllAsync();

        return new TransferStatisticsDto
        {
            TotalRequests = requestList.Count,
            CompletedRequests = completedRequests.Count,
            CancelledRequests = requestList.Count(r => r.Status == TransferRequestStatus.Cancelled),
            RejectedRequests = requestList.Count(r => r.Status == TransferRequestStatus.Rejected),
            CompletionRate = requestList.Count > 0 ? (decimal)completedRequests.Count / requestList.Count * 100 : 0,
            TotalItemsTransferred = completedRequests.Sum(r => r.TotalItemsApproved),
            TotalValueTransferred = completedRequests.Sum(r => r.TotalEstimatedValue),
            TotalIssuesReported = issues.Count(),
            ResolvedIssues = issues.Count(i => i.IsResolved),
            IssueRate = completedRequests.Count > 0 ? (decimal)issues.Count() / completedRequests.Count * 100 : 0,
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    #endregion

    #region Request Number Generation

    public async Task<string> GenerateRequestNumberAsync()
    {
        var requests = await _requestRepository.GetAllAsync();
        var year = DateTime.UtcNow.Year;
        var count = requests.Count(r => r.CreatedAt.Year == year) + 1;
        return $"TR-{year}-{count:D5}";
    }

    public async Task<string> GenerateShipmentNumberAsync()
    {
        var shipments = await _shipmentRepository.GetAllAsync();
        var year = DateTime.UtcNow.Year;
        var count = shipments.Count(s => s.CreatedAt.Year == year) + 1;
        return $"SH-{year}-{count:D5}";
    }

    public async Task<string> GenerateReceiptNumberAsync()
    {
        var receipts = await _receiptRepository.GetAllAsync();
        var year = DateTime.UtcNow.Year;
        var count = receipts.Count(r => r.CreatedAt.Year == year) + 1;
        return $"RC-{year}-{count:D5}";
    }

    #endregion

    #region Private Helpers

    private async Task<TransferRequestLine> AddLineToRequestAsync(int requestId, CreateTransferRequestLineDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        var sourceStock = await GetProductStockAtSourceAsync(
            (await _requestRepository.GetByIdAsync(requestId))!.SourceLocationId,
            dto.ProductId);

        var line = new TransferRequestLine
        {
            TransferRequestId = requestId,
            ProductId = dto.ProductId,
            RequestedQuantity = dto.RequestedQuantity,
            SourceAvailableStock = sourceStock?.AvailableQuantity ?? 0,
            UnitCost = product?.CostPrice ?? 0,
            LineTotal = dto.RequestedQuantity * (product?.CostPrice ?? 0),
            Notes = dto.Notes,
            IsActive = true
        };

        await _lineRepository.AddAsync(line);
        return line;
    }

    private async Task UpdateRequestTotalsAsync(int requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null) return;

        var lines = await _lineRepository.GetAllAsync();
        var requestLines = lines.Where(l => l.TransferRequestId == requestId && l.IsActive).ToList();

        request.TotalItemsRequested = requestLines.Sum(l => l.RequestedQuantity);
        request.TotalItemsApproved = requestLines.Sum(l => l.ApprovedQuantity ?? 0);
        request.TotalEstimatedValue = requestLines.Sum(l => l.LineTotal);

        await _requestRepository.UpdateAsync(request);
    }

    private async Task UpdateInventoryForReceiptAsync(int storeId, int productId, int quantity)
    {
        var inventories = await _inventoryRepository.GetAllAsync();
        var inventory = inventories.FirstOrDefault(i => i.StoreId == storeId && i.ProductId == productId);

        if (inventory != null)
        {
            inventory.CurrentStock += quantity;
            await _inventoryRepository.UpdateAsync(inventory);
        }
    }

    private async Task DeductInventoryFromSourceAsync(int sourceLocationId, int productId, int quantity)
    {
        var inventories = await _inventoryRepository.GetAllAsync();
        var inventory = inventories.FirstOrDefault(i => i.StoreId == sourceLocationId && i.ProductId == productId);

        if (inventory != null)
        {
            if (inventory.CurrentStock < quantity)
                throw new InvalidOperationException($"Insufficient stock for product {productId} at source location. Available: {inventory.CurrentStock}, Required: {quantity}");

            inventory.CurrentStock -= quantity;

            // Also reduce reserved stock if applicable (reservations will be fulfilled separately)
            if (inventory.ReservedStock >= quantity)
                inventory.ReservedStock -= quantity;
            else
                inventory.ReservedStock = 0;

            await _inventoryRepository.UpdateAsync(inventory);
        }
    }

    private async Task<StockTransferRequestDto> MapToRequestDtoAsync(StockTransferRequest request)
    {
        var stores = await _storeRepository.GetAllAsync();
        var requestingStore = stores.FirstOrDefault(s => s.Id == request.RequestingStoreId);
        var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);
        var users = await _userRepository.GetAllAsync();

        var lines = await _lineRepository.GetAllAsync();
        var requestLines = lines.Where(l => l.TransferRequestId == request.Id && l.IsActive).ToList();

        var lineDtos = new List<TransferRequestLineDto>();
        foreach (var line in requestLines)
        {
            lineDtos.Add(await MapToLineDtoAsync(line));
        }

        var shipment = await GetRequestShipmentAsync(request.Id);

        return new StockTransferRequestDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            RequestingStoreId = request.RequestingStoreId,
            RequestingStoreName = requestingStore?.Name ?? "Unknown",
            SourceLocationId = request.SourceLocationId,
            SourceLocationName = sourceLocation?.Name ?? "Unknown",
            SourceLocationType = request.SourceLocationType,
            Status = request.Status,
            Priority = request.Priority,
            Reason = request.Reason,
            SubmittedAt = request.SubmittedAt,
            SubmittedByUserName = users.FirstOrDefault(u => u.Id == request.SubmittedByUserId)?.Username,
            ApprovedAt = request.ApprovedAt,
            ApprovedByUserName = users.FirstOrDefault(u => u.Id == request.ApprovedByUserId)?.Username,
            ApprovalNotes = request.ApprovalNotes,
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes,
            RejectionReason = request.RejectionReason,
            TotalItemsRequested = request.TotalItemsRequested,
            TotalItemsApproved = request.TotalItemsApproved,
            TotalEstimatedValue = request.TotalEstimatedValue,
            LineCount = lineDtos.Count,
            CreatedAt = request.CreatedAt,
            Lines = lineDtos,
            Shipment = shipment
        };
    }

    private async Task<TransferRequestSummaryDto> MapToSummaryDtoAsync(StockTransferRequest request)
    {
        var stores = await _storeRepository.GetAllAsync();
        var requestingStore = stores.FirstOrDefault(s => s.Id == request.RequestingStoreId);
        var sourceLocation = stores.FirstOrDefault(s => s.Id == request.SourceLocationId);

        var lines = await _lineRepository.GetAllAsync();
        var lineCount = lines.Count(l => l.TransferRequestId == request.Id && l.IsActive);

        return new TransferRequestSummaryDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            RequestingStoreName = requestingStore?.Name ?? "Unknown",
            SourceLocationName = sourceLocation?.Name ?? "Unknown",
            Status = request.Status,
            Priority = request.Priority,
            LineCount = lineCount,
            TotalItemsRequested = request.TotalItemsRequested,
            TotalEstimatedValue = request.TotalEstimatedValue,
            SubmittedAt = request.SubmittedAt,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            CreatedAt = request.CreatedAt
        };
    }

    private async Task<TransferRequestLineDto> MapToLineDtoAsync(TransferRequestLine line)
    {
        var products = await _productRepository.GetAllAsync();
        var product = products.FirstOrDefault(p => p.Id == line.ProductId);

        return new TransferRequestLineDto
        {
            Id = line.Id,
            TransferRequestId = line.TransferRequestId,
            ProductId = line.ProductId,
            ProductName = product?.Name ?? "Unknown",
            ProductSku = product?.SKU ?? string.Empty,
            RequestedQuantity = line.RequestedQuantity,
            ApprovedQuantity = line.ApprovedQuantity,
            ShippedQuantity = line.ShippedQuantity,
            ReceivedQuantity = line.ReceivedQuantity,
            IssueQuantity = line.IssueQuantity,
            SourceAvailableStock = line.SourceAvailableStock,
            UnitCost = line.UnitCost,
            LineTotal = line.LineTotal,
            Notes = line.Notes,
            ApprovalNotes = line.ApprovalNotes
        };
    }

    private async Task<StockTransferShipmentDto> MapToShipmentDtoAsync(StockTransferShipment shipment)
    {
        var users = await _userRepository.GetAllAsync();

        return new StockTransferShipmentDto
        {
            Id = shipment.Id,
            TransferRequestId = shipment.TransferRequestId,
            ShipmentNumber = shipment.ShipmentNumber,
            ShippedAt = shipment.ShippedAt,
            ShippedByUserName = users.FirstOrDefault(u => u.Id == shipment.ShippedByUserId)?.Username,
            ExpectedArrivalDate = shipment.ExpectedArrivalDate,
            ActualArrivalDate = shipment.ActualArrivalDate,
            Carrier = shipment.Carrier,
            TrackingNumber = shipment.TrackingNumber,
            VehicleDetails = shipment.VehicleDetails,
            DriverName = shipment.DriverName,
            DriverContact = shipment.DriverContact,
            PackageCount = shipment.PackageCount,
            TotalWeightKg = shipment.TotalWeightKg,
            Notes = shipment.Notes,
            IsComplete = shipment.IsComplete
        };
    }

    private async Task<StockTransferReceiptDto> MapToReceiptDtoAsync(StockTransferReceipt receipt)
    {
        var users = await _userRepository.GetAllAsync();
        var lines = await _receiptLineRepository.GetAllAsync();
        var receiptLines = lines.Where(l => l.TransferReceiptId == receipt.Id).ToList();

        var lineDtos = new List<TransferReceiptLineDto>();
        foreach (var line in receiptLines)
        {
            var products = await _productRepository.GetAllAsync();
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);

            lineDtos.Add(new TransferReceiptLineDto
            {
                Id = line.Id,
                TransferReceiptId = line.TransferReceiptId,
                TransferRequestLineId = line.TransferRequestLineId,
                ProductId = line.ProductId,
                ProductName = product?.Name ?? "Unknown",
                ProductSku = product?.SKU ?? string.Empty,
                ExpectedQuantity = line.ExpectedQuantity,
                ReceivedQuantity = line.ReceivedQuantity,
                IssueQuantity = line.IssueQuantity,
                Notes = line.Notes
            });
        }

        var issues = await GetReceiptIssuesAsync(receipt.Id);

        return new StockTransferReceiptDto
        {
            Id = receipt.Id,
            TransferRequestId = receipt.TransferRequestId,
            ReceiptNumber = receipt.ReceiptNumber,
            ReceivedAt = receipt.ReceivedAt,
            ReceivedByUserName = users.FirstOrDefault(u => u.Id == receipt.ReceivedByUserId)?.Username ?? "Unknown",
            IsComplete = receipt.IsComplete,
            HasIssues = receipt.HasIssues,
            Notes = receipt.Notes,
            TotalReceived = lineDtos.Sum(l => l.ReceivedQuantity),
            TotalIssues = lineDtos.Sum(l => l.IssueQuantity),
            Lines = lineDtos,
            Issues = issues
        };
    }

    private TransferReceiptIssueDto MapToIssueDtoAsync(TransferReceiptIssue issue)
    {
        return new TransferReceiptIssueDto
        {
            Id = issue.Id,
            TransferReceiptId = issue.TransferReceiptId,
            TransferReceiptLineId = issue.TransferReceiptLineId,
            IssueType = issue.IssueType,
            AffectedQuantity = issue.AffectedQuantity,
            Description = issue.Description,
            PhotoPath = issue.PhotoPath,
            IsResolved = issue.IsResolved,
            ResolutionNotes = issue.ResolutionNotes,
            ResolvedAt = issue.ResolvedAt
        };
    }

    #endregion
}
