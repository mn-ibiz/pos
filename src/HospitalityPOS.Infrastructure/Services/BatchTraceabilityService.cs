using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for batch traceability and recall management.
/// </summary>
public class BatchTraceabilityService : IBatchTraceabilityService
{
    private readonly IRepository<ProductBatch> _batchRepository;
    private readonly IRepository<BatchStockMovement> _movementRepository;
    private readonly IRepository<BatchRecallAlert> _recallRepository;
    private readonly IRepository<RecallAction> _recallActionRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<GoodsReceivedNote> _grnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BatchTraceabilityService(
        IRepository<ProductBatch> batchRepository,
        IRepository<BatchStockMovement> movementRepository,
        IRepository<BatchRecallAlert> recallRepository,
        IRepository<RecallAction> recallActionRepository,
        IRepository<Product> productRepository,
        IRepository<Store> storeRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<User> userRepository,
        IRepository<GoodsReceivedNote> grnRepository,
        IUnitOfWork unitOfWork)
    {
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _recallRepository = recallRepository ?? throw new ArgumentNullException(nameof(recallRepository));
        _recallActionRepository = recallActionRepository ?? throw new ArgumentNullException(nameof(recallActionRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _grnRepository = grnRepository ?? throw new ArgumentNullException(nameof(grnRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    #region Batch Search

    public async Task<List<BatchSearchResultDto>> SearchBatchesAsync(BatchSearchQueryDto query)
    {
        var batches = await _batchRepository.FindAsync(b =>
            b.IsActive &&
            (string.IsNullOrEmpty(query.BatchNumber) || b.BatchNumber.Contains(query.BatchNumber)) &&
            (!query.ProductId.HasValue || b.ProductId == query.ProductId) &&
            (!query.StoreId.HasValue || b.StoreId == query.StoreId) &&
            (!query.SupplierId.HasValue || b.SupplierId == query.SupplierId) &&
            (!query.ReceivedFrom.HasValue || b.ReceivedAt >= query.ReceivedFrom) &&
            (!query.ReceivedTo.HasValue || b.ReceivedAt <= query.ReceivedTo) &&
            (!query.ExpiryFrom.HasValue || b.ExpiryDate >= query.ExpiryFrom) &&
            (!query.ExpiryTo.HasValue || b.ExpiryDate <= query.ExpiryTo) &&
            (string.IsNullOrEmpty(query.Status) || b.Status.ToString() == query.Status) &&
            (query.IncludeExpired || b.Status != BatchStatus.Expired));

        var batchList = batches.ToList();
        if (query.Limit.HasValue)
        {
            batchList = batchList.Take(query.Limit.Value).ToList();
        }

        var result = new List<BatchSearchResultDto>();
        foreach (var batch in batchList.OrderByDescending(b => b.ReceivedAt))
        {
            var product = await _productRepository.GetByIdAsync(batch.ProductId);
            var store = await _storeRepository.GetByIdAsync(batch.StoreId);
            var supplier = batch.SupplierId.HasValue
                ? await _supplierRepository.GetByIdAsync(batch.SupplierId.Value)
                : null;

            var hasActiveRecall = await HasActiveRecallAsync(batch.Id);

            result.Add(new BatchSearchResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                ProductId = batch.ProductId,
                ProductName = product?.Name ?? "",
                ProductSKU = product?.SKU ?? "",
                StoreId = batch.StoreId,
                StoreName = store?.Name ?? "",
                SupplierName = supplier?.Name,
                ReceivedDate = batch.ReceivedAt,
                ExpiryDate = batch.ExpiryDate,
                CurrentQuantity = batch.CurrentQuantity,
                QuantitySold = batch.SoldQuantity,
                Status = batch.Status.ToString(),
                HasActiveRecall = hasActiveRecall
            });
        }

        return result;
    }

    public async Task<List<BatchSearchResultDto>> SearchByBatchNumberAsync(string batchNumber, int? productId = null)
    {
        var query = new BatchSearchQueryDto
        {
            BatchNumber = batchNumber,
            ProductId = productId
        };
        return await SearchBatchesAsync(query);
    }

    public async Task<List<BatchSearchResultDto>> GetProductBatchesAsync(int productId, bool includeExpired = true)
    {
        var query = new BatchSearchQueryDto
        {
            ProductId = productId,
            IncludeExpired = includeExpired
        };
        return await SearchBatchesAsync(query);
    }

    #endregion

    #region Traceability Report

    public async Task<BatchTraceabilityReportDto?> GetTraceabilityReportAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null) return null;

        var product = await _productRepository.GetByIdAsync(batch.ProductId);
        var store = await _storeRepository.GetByIdAsync(batch.StoreId);
        var supplier = batch.SupplierId.HasValue
            ? await _supplierRepository.GetByIdAsync(batch.SupplierId.Value)
            : null;
        var grn = batch.GrnId.HasValue
            ? await _grnRepository.GetByIdAsync(batch.GrnId.Value)
            : null;

        // Get movements
        var movements = await GetBatchMovementsAsync(batchId);

        // Calculate movement summaries
        var movementList = movements.ToList();
        var quantitySold = movementList
            .Where(m => m.MovementType == BatchMovementType.Sale.ToString())
            .Sum(m => Math.Abs(m.Quantity));
        var quantityDisposed = movementList
            .Where(m => m.MovementType == BatchMovementType.Disposal.ToString())
            .Sum(m => Math.Abs(m.Quantity));
        var quantityTransferredOut = movementList
            .Where(m => m.MovementType == BatchMovementType.TransferOut.ToString())
            .Sum(m => Math.Abs(m.Quantity));
        var quantityTransferredIn = movementList
            .Where(m => m.MovementType == BatchMovementType.TransferIn.ToString())
            .Sum(m => Math.Abs(m.Quantity));
        var quantityReturned = movementList
            .Where(m => m.MovementType == BatchMovementType.Return.ToString())
            .Sum(m => Math.Abs(m.Quantity));
        var quantityAdjusted = movementList
            .Where(m => m.MovementType == BatchMovementType.Adjustment.ToString())
            .Sum(m => m.Quantity);

        // Get sale transactions
        var saleTransactions = await GetBatchSaleTransactionsAsync(batchId);

        // Check for active recall
        var hasActiveRecall = await HasActiveRecallAsync(batchId);
        BatchRecallAlertDto? activeRecall = null;
        if (hasActiveRecall)
        {
            var recalls = await _recallRepository.FindAsync(r =>
                r.BatchId == batchId &&
                r.Status == RecallStatus.Active &&
                r.IsActive);
            var recall = recalls.FirstOrDefault();
            if (recall != null)
            {
                activeRecall = await MapRecallToDto(recall);
            }
        }

        var now = DateTime.UtcNow;
        var daysUntilExpiry = batch.ExpiryDate.HasValue
            ? (int)(batch.ExpiryDate.Value - now).TotalDays
            : (int?)null;

        return new BatchTraceabilityReportDto
        {
            BatchId = batch.Id,
            BatchNumber = batch.BatchNumber,
            ProductId = batch.ProductId,
            ProductName = product?.Name ?? "",
            ProductSKU = product?.SKU ?? "",
            StoreId = batch.StoreId,
            StoreName = store?.Name ?? "",
            SupplierId = batch.SupplierId,
            SupplierName = supplier?.Name,
            ReceivedDate = batch.ReceivedAt,
            GrnId = batch.GrnId,
            GrnNumber = grn?.GrnNumber,
            QuantityReceived = batch.InitialQuantity,
            UnitCost = batch.UnitCost,
            ManufactureDate = batch.ManufactureDate,
            CurrentQuantity = batch.CurrentQuantity,
            ReservedQuantity = batch.ReservedQuantity,
            AvailableQuantity = batch.AvailableQuantity,
            Status = batch.Status.ToString(),
            ExpiryDate = batch.ExpiryDate,
            DaysUntilExpiry = daysUntilExpiry,
            IsExpired = batch.IsExpired,
            QuantitySold = quantitySold,
            QuantityAdjusted = quantityAdjusted,
            QuantityDisposed = quantityDisposed,
            QuantityTransferredOut = quantityTransferredOut,
            QuantityTransferredIn = quantityTransferredIn,
            QuantityReturned = quantityReturned,
            TotalReceivedValue = batch.InitialQuantity * batch.UnitCost,
            TotalSoldValue = saleTransactions.Sum(s => s.LineTotal),
            TotalDisposedValue = quantityDisposed * batch.UnitCost,
            CurrentStockValue = batch.CurrentQuantity * batch.UnitCost,
            Movements = movements,
            SaleTransactions = saleTransactions,
            HasActiveRecall = hasActiveRecall,
            ActiveRecall = activeRecall
        };
    }

    public async Task<List<BatchMovementDetailDto>> GetBatchMovementsAsync(int batchId)
    {
        var movements = await _movementRepository.FindAsync(m =>
            m.BatchId == batchId && m.IsActive);

        var result = new List<BatchMovementDetailDto>();
        foreach (var movement in movements.OrderByDescending(m => m.MovedAt))
        {
            var user = movement.MovedByUserId.HasValue
                ? await _userRepository.GetByIdAsync(movement.MovedByUserId.Value)
                : null;

            result.Add(new BatchMovementDetailDto
            {
                Id = movement.Id,
                MovementDate = movement.MovedAt,
                MovementType = movement.MovementType.ToString(),
                Quantity = movement.Quantity,
                QuantityBefore = movement.QuantityBefore,
                QuantityAfter = movement.QuantityAfter,
                ReferenceType = movement.ReferenceType,
                ReferenceId = movement.ReferenceId,
                ReferenceNumber = movement.ReferenceNumber,
                Details = movement.Notes,
                UnitCost = movement.UnitCost,
                TotalValue = Math.Abs(movement.Quantity) * movement.UnitCost,
                PerformedByUserId = movement.MovedByUserId,
                PerformedByUserName = user?.Username
            });
        }

        return result;
    }

    public async Task<List<BatchSaleTransactionDto>> GetBatchSaleTransactionsAsync(int batchId)
    {
        // Get sale movements
        var saleMovements = await _movementRepository.FindAsync(m =>
            m.BatchId == batchId &&
            m.MovementType == BatchMovementType.Sale &&
            m.ReferenceType == "Receipt" &&
            m.IsActive);

        var result = new List<BatchSaleTransactionDto>();
        foreach (var movement in saleMovements.OrderByDescending(m => m.MovedAt))
        {
            var user = movement.MovedByUserId.HasValue
                ? await _userRepository.GetByIdAsync(movement.MovedByUserId.Value)
                : null;
            var store = await _storeRepository.GetByIdAsync(movement.StoreId);

            result.Add(new BatchSaleTransactionDto
            {
                ReceiptId = movement.ReferenceId,
                ReceiptNumber = movement.ReferenceNumber ?? "",
                TransactionDate = movement.MovedAt,
                QuantitySold = Math.Abs(movement.Quantity),
                UnitPrice = movement.UnitCost,
                LineTotal = Math.Abs(movement.Quantity) * movement.UnitCost,
                CashierId = movement.MovedByUserId ?? 0,
                CashierName = user?.Username ?? "",
                StoreId = movement.StoreId,
                StoreName = store?.Name ?? ""
            });
        }

        return result;
    }

    public async Task<List<BatchLocationDto>> GetBatchLocationsAsync(int batchId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null) return new List<BatchLocationDto>();

        // For single-store batches, return the batch's store
        var store = await _storeRepository.GetByIdAsync(batch.StoreId);

        // Get last movement date
        var movements = await _movementRepository.FindAsync(m =>
            m.BatchId == batchId && m.IsActive);
        var lastMovement = movements.OrderByDescending(m => m.MovedAt).FirstOrDefault();

        return new List<BatchLocationDto>
        {
            new()
            {
                StoreId = batch.StoreId,
                StoreName = store?.Name ?? "",
                Quantity = batch.CurrentQuantity,
                ReservedQuantity = batch.ReservedQuantity,
                AvailableQuantity = batch.AvailableQuantity,
                LastMovementDate = lastMovement?.MovedAt ?? batch.ReceivedAt
            }
        };
    }

    #endregion

    #region Recall Management

    public async Task<BatchRecallAlertDto> CreateRecallAlertAsync(CreateBatchRecallAlertDto dto, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId);
        if (batch == null)
        {
            throw new InvalidOperationException("Batch not found.");
        }

        var severity = Enum.TryParse<RecallSeverity>(dto.Severity, out var sev)
            ? sev : RecallSeverity.Medium;

        var recall = new BatchRecallAlert
        {
            BatchId = dto.BatchId,
            ProductId = batch.ProductId,
            BatchNumber = batch.BatchNumber,
            RecallReason = dto.RecallReason,
            Severity = severity,
            Status = RecallStatus.Active,
            IssuedAt = DateTime.UtcNow,
            IssuedByUserId = userId,
            AffectedQuantity = batch.InitialQuantity,
            QuantityRecovered = 0,
            QuantitySold = batch.SoldQuantity,
            QuantityInStock = batch.CurrentQuantity,
            ExternalReference = dto.ExternalReference,
            SupplierContactInfo = dto.SupplierContactInfo
        };

        await _recallRepository.AddAsync(recall);

        // Update batch status to Recalled
        batch.Status = BatchStatus.Recalled;
        await _batchRepository.UpdateAsync(batch);

        await _unitOfWork.SaveChangesAsync();

        return await MapRecallToDto(recall);
    }

    public async Task<BatchRecallAlertDto?> GetRecallAlertAsync(int recallId)
    {
        var recall = await _recallRepository.GetByIdAsync(recallId);
        if (recall == null) return null;

        return await MapRecallToDto(recall);
    }

    public async Task<List<BatchRecallAlertDto>> GetRecallAlertsAsync(RecallQueryDto query)
    {
        var recalls = await _recallRepository.FindAsync(r =>
            r.IsActive &&
            (!query.ProductId.HasValue || r.ProductId == query.ProductId) &&
            (string.IsNullOrEmpty(query.Status) || r.Status.ToString() == query.Status) &&
            (string.IsNullOrEmpty(query.Severity) || r.Severity.ToString() == query.Severity) &&
            (!query.FromDate.HasValue || r.IssuedAt >= query.FromDate) &&
            (!query.ToDate.HasValue || r.IssuedAt <= query.ToDate) &&
            (!query.ActiveOnly || r.Status == RecallStatus.Active));

        var result = new List<BatchRecallAlertDto>();
        foreach (var recall in recalls.OrderByDescending(r => r.IssuedAt))
        {
            result.Add(await MapRecallToDto(recall));
        }

        return result;
    }

    public async Task<BatchRecallAlertDto> UpdateRecallStatusAsync(UpdateRecallStatusDto dto, int userId)
    {
        var recall = await _recallRepository.GetByIdAsync(dto.RecallAlertId);
        if (recall == null)
        {
            throw new InvalidOperationException("Recall alert not found.");
        }

        if (Enum.TryParse<RecallStatus>(dto.Status, out var status))
        {
            recall.Status = status;

            if (status == RecallStatus.Closed || status == RecallStatus.Recovered || status == RecallStatus.Cancelled)
            {
                recall.ResolvedAt = DateTime.UtcNow;
                recall.ResolvedByUserId = userId;
            }
        }

        if (dto.QuantityRecovered.HasValue)
        {
            recall.QuantityRecovered = dto.QuantityRecovered.Value;
        }

        if (!string.IsNullOrEmpty(dto.ResolutionNotes))
        {
            recall.ResolutionNotes = dto.ResolutionNotes;
        }

        await _recallRepository.UpdateAsync(recall);
        await _unitOfWork.SaveChangesAsync();

        return await MapRecallToDto(recall);
    }

    public async Task<RecallActionDto> RecordRecallActionAsync(CreateRecallActionDto dto, int userId)
    {
        var recall = await _recallRepository.GetByIdAsync(dto.RecallAlertId);
        if (recall == null)
        {
            throw new InvalidOperationException("Recall alert not found.");
        }

        var action = new RecallAction
        {
            RecallAlertId = dto.RecallAlertId,
            ActionType = dto.ActionType,
            StoreId = dto.StoreId,
            Quantity = dto.Quantity,
            Description = dto.Description,
            ActionDate = DateTime.UtcNow,
            PerformedByUserId = userId
        };

        await _recallActionRepository.AddAsync(action);

        // Update recovered quantity if this is a recovery action
        if (dto.ActionType == "Quarantine" || dto.ActionType == "Dispose" || dto.ActionType == "Return")
        {
            recall.QuantityRecovered += dto.Quantity;
            await _recallRepository.UpdateAsync(recall);
        }

        await _unitOfWork.SaveChangesAsync();

        var user = await _userRepository.GetByIdAsync(userId);
        var store = dto.StoreId.HasValue
            ? await _storeRepository.GetByIdAsync(dto.StoreId.Value)
            : null;

        return new RecallActionDto
        {
            Id = action.Id,
            RecallAlertId = action.RecallAlertId,
            ActionType = action.ActionType,
            StoreId = action.StoreId,
            StoreName = store?.Name,
            Quantity = action.Quantity,
            Description = action.Description,
            ActionDate = action.ActionDate,
            PerformedByUserId = action.PerformedByUserId,
            PerformedByUserName = user?.Username ?? ""
        };
    }

    public async Task<List<RecallActionDto>> GetRecallActionsAsync(int recallId)
    {
        var actions = await _recallActionRepository.FindAsync(a =>
            a.RecallAlertId == recallId && a.IsActive);

        var result = new List<RecallActionDto>();
        foreach (var action in actions.OrderByDescending(a => a.ActionDate))
        {
            var user = await _userRepository.GetByIdAsync(action.PerformedByUserId);
            var store = action.StoreId.HasValue
                ? await _storeRepository.GetByIdAsync(action.StoreId.Value)
                : null;

            result.Add(new RecallActionDto
            {
                Id = action.Id,
                RecallAlertId = action.RecallAlertId,
                ActionType = action.ActionType,
                StoreId = action.StoreId,
                StoreName = store?.Name,
                Quantity = action.Quantity,
                Description = action.Description,
                ActionDate = action.ActionDate,
                PerformedByUserId = action.PerformedByUserId,
                PerformedByUserName = user?.Username ?? ""
            });
        }

        return result;
    }

    public async Task<RecallSummaryDto> GetRecallSummaryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var recalls = await _recallRepository.FindAsync(r =>
            r.IsActive &&
            (!fromDate.HasValue || r.IssuedAt >= fromDate) &&
            (!toDate.HasValue || r.IssuedAt <= toDate));

        var recallList = recalls.ToList();

        var activeRecalls = recallList.Where(r => r.Status == RecallStatus.Active).ToList();
        var closedRecalls = recallList.Where(r =>
            r.Status == RecallStatus.Closed ||
            r.Status == RecallStatus.Recovered ||
            r.Status == RecallStatus.Cancelled).ToList();

        var totalAffected = recallList.Sum(r => r.AffectedQuantity);
        var totalRecovered = recallList.Sum(r => r.QuantityRecovered);
        var totalSold = recallList.Sum(r => r.QuantitySold);

        return new RecallSummaryDto
        {
            TotalActiveRecalls = activeRecalls.Count,
            TotalClosedRecalls = closedRecalls.Count,
            TotalAffectedQuantity = totalAffected,
            TotalRecoveredQuantity = totalRecovered,
            TotalSoldBeforeRecall = totalSold,
            RecoveryRate = totalAffected > 0 ? (decimal)totalRecovered / totalAffected * 100 : 0,
            CriticalRecalls = recallList.Count(r => r.Severity == RecallSeverity.Critical && r.Status == RecallStatus.Active),
            HighRecalls = recallList.Count(r => r.Severity == RecallSeverity.High && r.Status == RecallStatus.Active),
            MediumRecalls = recallList.Count(r => r.Severity == RecallSeverity.Medium && r.Status == RecallStatus.Active),
            LowRecalls = recallList.Count(r => r.Severity == RecallSeverity.Low && r.Status == RecallStatus.Active)
        };
    }

    public async Task<List<BatchRecallAlertDto>> GetActiveRecallsForStoreAsync(int storeId)
    {
        // Get batches at this store
        var storeBatches = await _batchRepository.FindAsync(b =>
            b.StoreId == storeId && b.IsActive);
        var batchIds = storeBatches.Select(b => b.Id).ToHashSet();

        // Get active recalls for these batches
        var recalls = await _recallRepository.FindAsync(r =>
            r.IsActive &&
            r.Status == RecallStatus.Active &&
            batchIds.Contains(r.BatchId));

        var result = new List<BatchRecallAlertDto>();
        foreach (var recall in recalls)
        {
            result.Add(await MapRecallToDto(recall));
        }

        return result;
    }

    public async Task<bool> HasActiveRecallAsync(int batchId)
    {
        var recalls = await _recallRepository.FindAsync(r =>
            r.BatchId == batchId &&
            r.Status == RecallStatus.Active &&
            r.IsActive);

        return recalls.Any();
    }

    public async Task QuarantineBatchAsync(int batchId, int recallId, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null)
        {
            throw new InvalidOperationException("Batch not found.");
        }

        batch.Status = BatchStatus.Recalled;
        await _batchRepository.UpdateAsync(batch);

        // Record quarantine action
        await RecordRecallActionAsync(new CreateRecallActionDto
        {
            RecallAlertId = recallId,
            ActionType = "Quarantine",
            StoreId = batch.StoreId,
            Quantity = batch.CurrentQuantity,
            Description = $"Batch quarantined due to recall"
        }, userId);
    }

    #endregion

    #region Private Helpers

    private async Task<BatchRecallAlertDto> MapRecallToDto(BatchRecallAlert recall)
    {
        var product = await _productRepository.GetByIdAsync(recall.ProductId);
        var issuedBy = await _userRepository.GetByIdAsync(recall.IssuedByUserId);
        var resolvedBy = recall.ResolvedByUserId.HasValue
            ? await _userRepository.GetByIdAsync(recall.ResolvedByUserId.Value)
            : null;

        var actions = await GetRecallActionsAsync(recall.Id);

        return new BatchRecallAlertDto
        {
            Id = recall.Id,
            BatchId = recall.BatchId,
            BatchNumber = recall.BatchNumber,
            ProductId = recall.ProductId,
            ProductName = product?.Name ?? "",
            ProductSKU = product?.SKU ?? "",
            RecallReason = recall.RecallReason,
            Severity = recall.Severity.ToString(),
            Status = recall.Status.ToString(),
            IssuedAt = recall.IssuedAt,
            IssuedByUserId = recall.IssuedByUserId,
            IssuedByUserName = issuedBy?.Username ?? "",
            AffectedQuantity = recall.AffectedQuantity,
            QuantityRecovered = recall.QuantityRecovered,
            QuantitySold = recall.QuantitySold,
            QuantityInStock = recall.QuantityInStock,
            ExternalReference = recall.ExternalReference,
            SupplierContactInfo = recall.SupplierContactInfo,
            ResolutionNotes = recall.ResolutionNotes,
            ResolvedAt = recall.ResolvedAt,
            ResolvedByUserId = recall.ResolvedByUserId,
            ResolvedByUserName = resolvedBy?.Username,
            Actions = actions
        };
    }

    #endregion
}
