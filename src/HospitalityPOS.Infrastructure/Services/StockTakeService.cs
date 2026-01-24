using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing stock take (physical inventory) operations.
/// </summary>
public class StockTakeService : IStockTakeService
{
    private readonly POSDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StockTakeService"/> class.
    /// </summary>
    public StockTakeService(
        POSDbContext context,
        IInventoryService inventoryService,
        ISessionService sessionService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Session Management

    /// <inheritdoc />
    public async Task<StockTake> CreateStockCountAsync(CreateStockCountDto dto, int userId, CancellationToken ct = default)
    {
        // Check if there's already an in-progress stock take for this scope
        var existingQuery = _context.StockTakes.Where(st =>
            st.Status != StockTakeStatus.Posted &&
            st.Status != StockTakeStatus.Cancelled);

        if (dto.StoreId.HasValue)
        {
            existingQuery = existingQuery.Where(st => st.StoreId == dto.StoreId);
        }

        var existing = await existingQuery.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Cannot create stock count - another count is already active: {existing.StockTakeNumber}");
        }

        var stockTakeNumber = await GenerateStockTakeNumberAsync().ConfigureAwait(false);

        var stockTake = new StockTake
        {
            StockTakeNumber = stockTakeNumber,
            StoreId = dto.StoreId,
            CountType = dto.CountType,
            Status = StockTakeStatus.Draft,
            CountDate = dto.CountDate,
            StartedByUserId = userId,
            CategoryId = dto.CategoryId,
            LocationFilter = dto.LocationFilter,
            IsBlindCount = dto.IsBlindCount,
            IsDoubleBlind = dto.IsDoubleBlind,
            FreezeInventory = dto.FreezeInventory,
            ABCClassFilter = dto.ABCClassFilter,
            SpotCountProductIds = dto.SpotCountProductIds?.Any() == true
                ? JsonSerializer.Serialize(dto.SpotCountProductIds)
                : null,
            VarianceThresholdPercent = dto.VarianceThresholdPercent,
            VarianceThresholdValue = dto.VarianceThresholdValue,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // Build product query based on count type
        var productsQuery = BuildProductQuery(dto);
        var products = await productsQuery.ToListAsync(ct).ConfigureAwait(false);

        // Create stock take items
        decimal totalSystemValue = 0;
        foreach (var product in products)
        {
            var currentStock = product.Inventory?.CurrentStock ?? 0;
            var systemValue = currentStock * product.CostPrice;
            totalSystemValue += systemValue;

            var item = new StockTakeItem
            {
                ProductId = product.Id,
                ProductSku = product.Code ?? string.Empty,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                Location = product.StorageLocation,
                UnitOfMeasure = product.UnitOfMeasure,
                SystemQuantity = currentStock,
                SystemCostPrice = product.CostPrice,
                SystemValue = systemValue,
                IsCounted = false,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            stockTake.Items.Add(item);
        }

        stockTake.TotalItemsToCount = stockTake.Items.Count;
        stockTake.TotalSystemValue = totalSystemValue;

        // Assign counters if specified
        if (dto.AssignedCounterUserIds?.Any() == true)
        {
            bool isPrimary = true;
            foreach (var counterId in dto.AssignedCounterUserIds)
            {
                stockTake.Counters.Add(new StockCountCounter
                {
                    UserId = counterId,
                    IsPrimaryCounter = isPrimary,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                isPrimary = false;
            }
        }

        _context.StockTakes.Add(stockTake);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Stock count {Number} created by user {UserId} with {Count} items, type: {Type}",
            stockTakeNumber, userId, stockTake.Items.Count, dto.CountType);

        return stockTake;
    }

    private IQueryable<Product> BuildProductQuery(CreateStockCountDto dto)
    {
        var query = _context.Products
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.TrackInventory);

        if (dto.StoreId.HasValue)
        {
            // Filter by store if multi-store inventory exists
        }

        switch (dto.CountType)
        {
            case StockCountType.CategoryCount:
                if (dto.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == dto.CategoryId.Value);
                }
                break;

            case StockCountType.SpotCount:
                if (dto.SpotCountProductIds?.Any() == true)
                {
                    query = query.Where(p => dto.SpotCountProductIds.Contains(p.Id));
                }
                break;

            case StockCountType.LocationCount:
                if (!string.IsNullOrEmpty(dto.LocationFilter))
                {
                    query = query.Where(p => p.StorageLocation != null &&
                        p.StorageLocation.Contains(dto.LocationFilter));
                }
                break;

            case StockCountType.ABCClassCount:
                if (!string.IsNullOrEmpty(dto.ABCClassFilter))
                {
                    query = query.Where(p => p.ABCClass == dto.ABCClassFilter);
                }
                break;

            case StockCountType.CycleCount:
                // Cycle count might select based on last count date or value
                query = query.OrderByDescending(p => p.CostPrice * (p.Inventory != null ? p.Inventory.CurrentStock : 0))
                    .Take(100); // Top 100 by value
                break;

            case StockCountType.FullCount:
            default:
                // Include all tracked products
                break;
        }

        return query;
    }

    /// <inheritdoc />
    public async Task<StockTake> StartStockTakeAsync(int userId, string? notes = null, int? categoryId = null)
    {
        var dto = new CreateStockCountDto
        {
            CountType = categoryId.HasValue ? StockCountType.CategoryCount : StockCountType.FullCount,
            CategoryId = categoryId,
            Notes = notes,
            CountDate = DateTime.UtcNow
        };

        var stockTake = await CreateStockCountAsync(dto, userId).ConfigureAwait(false);

        // Auto-start the count (legacy behavior)
        return await StartCountingAsync(stockTake.Id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake?> GetStockTakeAsync(int stockTakeId, CancellationToken ct = default)
    {
        return await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .Include(st => st.Items)
                .ThenInclude(i => i.CountedByUser)
            .Include(st => st.Counters)
                .ThenInclude(c => c.User)
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .Include(st => st.Category)
            .Include(st => st.Store)
            .AsSplitQuery()
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> GetStockCountsAsync(StockCountFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.StockTakes
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .Include(st => st.Store)
            .AsQueryable();

        if (filter.StoreId.HasValue)
            query = query.Where(st => st.StoreId == filter.StoreId);

        if (filter.Status.HasValue)
            query = query.Where(st => st.Status == filter.Status);

        if (filter.CountType.HasValue)
            query = query.Where(st => st.CountType == filter.CountType);

        if (filter.FromDate.HasValue)
            query = query.Where(st => st.CountDate >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(st => st.CountDate <= filter.ToDate);

        if (filter.CategoryId.HasValue)
            query = query.Where(st => st.CategoryId == filter.CategoryId);

        if (filter.HasVariance.HasValue)
        {
            query = filter.HasVariance.Value
                ? query.Where(st => st.ItemsWithVariance > 0)
                : query.Where(st => st.ItemsWithVariance == 0);
        }

        return await query
            .OrderByDescending(st => st.CountDate)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> GetStockTakesAsync(
        StockTakeStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filter = new StockCountFilterDto
        {
            Status = status,
            FromDate = startDate,
            ToDate = endDate,
            Take = 100
        };
        return await GetStockCountsAsync(filter).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake?> GetInProgressStockTakeAsync()
    {
        return await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .Include(st => st.StartedByUser)
            .FirstOrDefaultAsync(st => st.Status == StockTakeStatus.InProgress)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake> StartCountingAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.Draft)
            throw new InvalidOperationException($"Cannot start counting - stock take is in status: {stockTake.Status}");

        stockTake.Status = StockTakeStatus.InProgress;
        stockTake.StartedAt = DateTime.UtcNow;

        if (stockTake.FreezeInventory)
        {
            await FreezeInventoryAsync(stockTakeId, ct).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information("Stock take {Number} counting started", stockTake.StockTakeNumber);
        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> CancelStockCountAsync(int stockTakeId, string reason, CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status == StockTakeStatus.Posted)
            throw new InvalidOperationException("Cannot cancel a posted stock take.");

        if (stockTake.FreezeInventory && stockTake.FrozenAt.HasValue && !stockTake.UnfrozenAt.HasValue)
        {
            await UnfreezeInventoryAsync(stockTakeId, ct).ConfigureAwait(false);
        }

        stockTake.Status = StockTakeStatus.Cancelled;
        stockTake.Notes = string.IsNullOrWhiteSpace(stockTake.Notes)
            ? $"Cancelled: {reason}"
            : $"{stockTake.Notes}\nCancelled: {reason}";
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information("Stock take {Number} cancelled. Reason: {Reason}", stockTake.StockTakeNumber, reason);
        return stockTake;
    }

    /// <inheritdoc />
    public Task<StockTake> CancelStockTakeAsync(int stockTakeId, string reason)
        => CancelStockCountAsync(stockTakeId, reason);

    #endregion

    #region Counting

    /// <inheritdoc />
    public async Task<StockTakeItem> RecordCountAsync(
        int stockTakeId,
        int productId,
        decimal countedQuantity,
        int userId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.InProgress)
            throw new InvalidOperationException($"Cannot record count - stock take is in status: {stockTake.Status}");

        var item = stockTake.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not found in stock take {stockTakeId}.");

        // Check if user is a secondary counter for double-blind
        if (stockTake.IsDoubleBlind && item.IsCounted && item.CountedByUserId != userId)
        {
            return await RecordSecondCountAsync(stockTakeId, productId, countedQuantity, userId, ct).ConfigureAwait(false);
        }

        item.CountedQuantity = countedQuantity;
        item.CountedValue = countedQuantity * item.SystemCostPrice;
        item.CountedByUserId = userId;
        item.CountedAt = DateTime.UtcNow;
        item.IsCounted = true;
        item.Notes = notes;
        item.UpdatedAt = DateTime.UtcNow;

        // Calculate variance
        CalculateItemVariance(item, stockTake);

        // Update stock take summary
        await UpdateStockTakeSummaryAsync(stockTakeId, ct).ConfigureAwait(false);

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Debug(
            "Count recorded: StockTake={Number}, Product={ProductId}, System={System}, Counted={Counted}, Variance={Variance}",
            stockTake.StockTakeNumber, productId, item.SystemQuantity, countedQuantity, item.VarianceQuantity);

        return item;
    }

    /// <inheritdoc />
    public async Task<StockTakeItem> RecordCountAsync(
        int stockTakeItemId,
        decimal physicalQuantity,
        int userId,
        string? notes = null)
    {
        var item = await _context.StockTakeItems
            .Include(i => i.StockTake)
            .FirstOrDefaultAsync(i => i.Id == stockTakeItemId)
            .ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Stock take item {stockTakeItemId} not found.");

        return await RecordCountAsync(
            item.StockTakeId,
            item.ProductId,
            physicalQuantity,
            userId,
            notes).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> RecordBatchCountAsync(
        int stockTakeId,
        IEnumerable<CountEntryDto> entries,
        int userId,
        CancellationToken ct = default)
    {
        var results = new List<StockTakeItem>();

        foreach (var entry in entries)
        {
            var item = await RecordCountAsync(
                stockTakeId,
                entry.ProductId,
                entry.CountedQuantity,
                userId,
                entry.Notes,
                ct).ConfigureAwait(false);
            results.Add(item);
        }

        _logger.Information("Batch count recorded: StockTake={StockTakeId}, Items={Count}", stockTakeId, results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> RecordCountsAsync(
        int stockTakeId,
        Dictionary<int, decimal> counts,
        int userId)
    {
        var entries = counts.Select(c => new CountEntryDto
        {
            ProductId = c.Key,
            CountedQuantity = c.Value
        });
        return await RecordBatchCountAsync(stockTakeId, entries, userId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTakeItem> RecordSecondCountAsync(
        int stockTakeId,
        int productId,
        decimal countedQuantity,
        int userId,
        CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (!stockTake.IsDoubleBlind)
            throw new InvalidOperationException("This stock take is not configured for double-blind counting.");

        var item = stockTake.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not found in stock take.");

        if (!item.IsCounted)
            throw new InvalidOperationException("First count must be recorded before second count.");

        if (item.CountedByUserId == userId)
            throw new InvalidOperationException("Same user cannot perform both counts in double-blind mode.");

        item.SecondCountQuantity = countedQuantity;
        item.SecondCountedByUserId = userId;
        item.SecondCountedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        // Check for mismatch
        var threshold = stockTake.VarianceThresholdPercent ?? 5m; // Default 5% tolerance
        var diff = Math.Abs((item.CountedQuantity ?? 0) - countedQuantity);
        var percentDiff = item.CountedQuantity > 0
            ? diff / item.CountedQuantity.Value * 100
            : (countedQuantity > 0 ? 100 : 0);

        item.CountMismatch = percentDiff > threshold;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Second count recorded: Product={ProductId}, First={First}, Second={Second}, Mismatch={Mismatch}",
            productId, item.CountedQuantity, countedQuantity, item.CountMismatch);

        return item;
    }

    /// <inheritdoc />
    public async Task<StockTakeItem> ResolveMismatchAsync(
        int stockTakeItemId,
        decimal resolvedQuantity,
        int userId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var item = await _context.StockTakeItems
            .Include(i => i.StockTake)
            .FirstOrDefaultAsync(i => i.Id == stockTakeItemId, ct)
            .ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Item {stockTakeItemId} not found.");

        if (!item.CountMismatch)
            throw new InvalidOperationException("Item does not have a count mismatch.");

        item.ResolvedQuantity = resolvedQuantity;
        item.ResolvedByUserId = userId;
        item.ResolvedAt = DateTime.UtcNow;
        item.VarianceNotes = notes;
        item.UpdatedAt = DateTime.UtcNow;

        // Recalculate variance with resolved quantity
        item.VarianceQuantity = resolvedQuantity - item.SystemQuantity;
        item.VarianceValue = item.VarianceQuantity * item.SystemCostPrice;
        item.VariancePercentage = item.SystemQuantity != 0
            ? item.VarianceQuantity / item.SystemQuantity * 100
            : 0;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Mismatch resolved: Item={ItemId}, Resolved={Quantity}",
            stockTakeItemId, resolvedQuantity);

        return item;
    }

    /// <inheritdoc />
    public async Task<StockTake> CompleteCountingAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete counting - status is: {stockTake.Status}");

        // Check for unresolved mismatches in double-blind mode
        if (stockTake.IsDoubleBlind)
        {
            var unresolvedMismatches = stockTake.Items.Count(i => i.CountMismatch && !i.ResolvedQuantity.HasValue);
            if (unresolvedMismatches > 0)
                throw new InvalidOperationException($"Cannot complete counting - {unresolvedMismatches} unresolved mismatches.");
        }

        stockTake.Status = StockTakeStatus.CountingComplete;
        stockTake.CompletedAt = DateTime.UtcNow;
        stockTake.UpdatedAt = DateTime.UtcNow;

        // Calculate all variances
        await CalculateVariancesAsync(stockTakeId, ct).ConfigureAwait(false);

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information("Stock take {Number} counting completed", stockTake.StockTakeNumber);
        return stockTake;
    }

    private void CalculateItemVariance(StockTakeItem item, StockTake stockTake)
    {
        var countedQty = item.FinalCountQuantity ?? item.CountedQuantity ?? 0;

        item.VarianceQuantity = countedQty - item.SystemQuantity;
        item.VarianceValue = item.VarianceQuantity * item.SystemCostPrice;
        item.VariancePercentage = item.SystemQuantity != 0
            ? item.VarianceQuantity / item.SystemQuantity * 100
            : 0;
        item.CountedValue = countedQty * item.SystemCostPrice;

        // Check threshold
        if (stockTake.VarianceThresholdPercent.HasValue || stockTake.VarianceThresholdValue.HasValue)
        {
            var exceedsPercent = stockTake.VarianceThresholdPercent.HasValue &&
                Math.Abs(item.VariancePercentage) > stockTake.VarianceThresholdPercent.Value;

            var exceedsValue = stockTake.VarianceThresholdValue.HasValue &&
                Math.Abs(item.VarianceValue) > stockTake.VarianceThresholdValue.Value;

            item.ExceedsThreshold = exceedsPercent || exceedsValue;
        }
    }

    #endregion

    #region Variance Management

    /// <inheritdoc />
    public async Task CalculateVariancesAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null) return;

        foreach (var item in stockTake.Items.Where(i => i.IsCounted))
        {
            CalculateItemVariance(item, stockTake);
        }

        await UpdateStockTakeSummaryAsync(stockTakeId, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTakeItem> SetVarianceCauseAsync(
        int stockTakeItemId,
        VarianceCause cause,
        string? notes,
        CancellationToken ct = default)
    {
        var item = await _context.StockTakeItems
            .FirstOrDefaultAsync(i => i.Id == stockTakeItemId, ct)
            .ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Item {stockTakeItemId} not found.");

        item.VarianceCause = cause;
        item.VarianceNotes = notes;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return item;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> GetVarianceItemsAsync(
        int stockTakeId,
        bool exceedsThresholdOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.StockTakeItems
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Where(i => i.StockTakeId == stockTakeId && i.IsCounted && i.VarianceQuantity != 0);

        if (exceedsThresholdOnly)
        {
            query = query.Where(i => i.ExceedsThreshold);
        }

        return await query
            .OrderByDescending(i => Math.Abs(i.VarianceValue))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTakeVarianceSummary> GetVarianceSummaryAsync(int stockTakeId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        var countedItems = stockTake.Items.Where(i => i.IsCounted).ToList();
        var shortages = countedItems.Where(i => i.VarianceQuantity < 0).ToList();
        var overages = countedItems.Where(i => i.VarianceQuantity > 0).ToList();

        var totalSystemValue = countedItems.Sum(i => i.SystemValue);
        var totalCountedValue = countedItems.Sum(i => i.CountedValue ?? 0);

        return new StockTakeVarianceSummary
        {
            StockTakeId = stockTake.Id,
            StockTakeNumber = stockTake.StockTakeNumber,
            TotalItems = stockTake.TotalItemsToCount,
            CountedItems = countedItems.Count,
            ItemsWithVariance = shortages.Count + overages.Count,
            ShortageValue = shortages.Sum(i => Math.Abs(i.VarianceValue)),
            OverageValue = overages.Sum(i => i.VarianceValue),
            NetVarianceValue = countedItems.Sum(i => i.VarianceValue),
            TotalSystemValue = totalSystemValue,
            TotalCountedValue = totalCountedValue,
            ShrinkageRate = totalSystemValue != 0
                ? Math.Abs(shortages.Sum(i => i.VarianceValue)) / totalSystemValue * 100
                : 0,
            Shortages = shortages,
            Overages = overages
        };
    }

    #endregion

    #region Approval & Posting

    /// <inheritdoc />
    public async Task<StockTake> SaveDraftAsync(int stockTakeId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (!stockTake.CanModify)
            throw new InvalidOperationException($"Cannot modify - status is: {stockTake.Status}");

        await UpdateStockTakeSummaryAsync(stockTakeId).ConfigureAwait(false);
        stockTake.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Stock take {Number} draft saved", stockTake.StockTakeNumber);
        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> SubmitForApprovalAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.InProgress && stockTake.Status != StockTakeStatus.CountingComplete)
            throw new InvalidOperationException($"Cannot submit for approval - status is: {stockTake.Status}");

        await UpdateStockTakeSummaryAsync(stockTakeId, ct).ConfigureAwait(false);

        stockTake.Status = StockTakeStatus.PendingApproval;
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Stock take {Number} submitted for approval. Variance: {Variance:C}",
            stockTake.StockTakeNumber, stockTake.TotalVarianceValue);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> ApproveStockCountAsync(
        int stockTakeId,
        int approverUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve - status is: {stockTake.Status}");

        // Check if items exceeding threshold have causes assigned
        var itemsNeedingCause = stockTake.Items
            .Where(i => i.ExceedsThreshold && i.HasVariance && !i.VarianceCause.HasValue)
            .ToList();

        if (stockTake.RequireApprovalForVariance && itemsNeedingCause.Any())
        {
            throw new InvalidOperationException(
                $"Cannot approve - {itemsNeedingCause.Count} items exceeding threshold require variance cause assignment.");
        }

        stockTake.Status = StockTakeStatus.Approved;
        stockTake.ApprovedByUserId = approverUserId;
        stockTake.ApprovedAt = DateTime.UtcNow;
        stockTake.ApprovalNotes = notes;
        stockTake.UpdatedAt = DateTime.UtcNow;

        foreach (var item in stockTake.Items.Where(i => i.IsCounted && i.HasVariance))
        {
            item.IsApproved = true;
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Stock take {Number} approved by user {UserId}",
            stockTake.StockTakeNumber, approverUserId);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> ApproveStockTakeAsync(int stockTakeId, int approverUserId)
    {
        var stockTake = await ApproveStockCountAsync(stockTakeId, approverUserId).ConfigureAwait(false);
        // Legacy behavior: also post adjustments
        return await PostAdjustmentsAsync(stockTakeId, approverUserId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake> RejectStockCountAsync(
        int stockTakeId,
        int approverUserId,
        string reason,
        CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject - status is: {stockTake.Status}");

        // Return to InProgress for corrections
        stockTake.Status = StockTakeStatus.InProgress;
        stockTake.RejectionReason = reason;
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Stock take {Number} rejected by user {UserId}. Reason: {Reason}",
            stockTake.StockTakeNumber, approverUserId, reason);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> PostAdjustmentsAsync(int stockTakeId, int userId, CancellationToken ct = default)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.Approved)
            throw new InvalidOperationException($"Cannot post - status is: {stockTake.Status}");

        var itemsToAdjust = stockTake.Items
            .Where(i => i.IsApproved && i.HasVariance && !i.AdjustmentPosted)
            .ToList();

        foreach (var item in itemsToAdjust)
        {
            var finalQty = item.FinalCountQuantity ?? item.CountedQuantity ?? 0;

            // Create stock movement
            var movement = new StockMovement
            {
                ProductId = item.ProductId,
                StoreId = stockTake.StoreId,
                MovementType = MovementType.StockTake,
                Quantity = item.VarianceQuantity,
                UnitCost = item.SystemCostPrice,
                PreviousStock = item.SystemQuantity,
                NewStock = finalQty,
                ReferenceType = "StockTake",
                ReferenceId = stockTake.Id,
                Reason = $"Stock Take: {stockTake.StockTakeNumber}",
                Notes = item.VarianceCause.HasValue ? $"Cause: {item.VarianceCause}" : null,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            // Update inventory
            await _inventoryService.AdjustStockAsync(
                item.ProductId,
                finalQty,
                $"Stock Take: {stockTake.StockTakeNumber}",
                $"Variance: {item.VarianceQuantity:+0.###;-0.###;0}")
                .ConfigureAwait(false);

            item.AdjustmentPosted = true;
            item.AdjustmentPostedAt = DateTime.UtcNow;
            item.StockMovementId = movement.Id;
        }

        if (stockTake.FreezeInventory && stockTake.FrozenAt.HasValue && !stockTake.UnfrozenAt.HasValue)
        {
            await UnfreezeInventoryAsync(stockTakeId, ct).ConfigureAwait(false);
        }

        stockTake.Status = StockTakeStatus.Posted;
        stockTake.PostedAt = DateTime.UtcNow;
        stockTake.PostedByUserId = userId;
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.Information(
            "Stock take {Number} posted. {Count} adjustments applied.",
            stockTake.StockTakeNumber, itemsToAdjust.Count);

        return stockTake;
    }

    #endregion

    #region Counter Management

    /// <inheritdoc />
    public async Task<StockCountCounter> AssignCounterAsync(
        int stockTakeId,
        int userId,
        bool isPrimary = false,
        CancellationToken ct = default)
    {
        var existing = await _context.StockCountCounters
            .FirstOrDefaultAsync(c => c.StockTakeId == stockTakeId && c.UserId == userId, ct)
            .ConfigureAwait(false);

        if (existing != null)
            return existing;

        var counter = new StockCountCounter
        {
            StockTakeId = stockTakeId,
            UserId = userId,
            IsPrimaryCounter = isPrimary,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockCountCounters.Add(counter);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return counter;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockCountCounter>> GetCountersAsync(int stockTakeId, CancellationToken ct = default)
    {
        return await _context.StockCountCounters
            .Include(c => c.User)
            .Where(c => c.StockTakeId == stockTakeId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockCountCounter> StartCounterSessionAsync(int stockTakeId, int userId, CancellationToken ct = default)
    {
        var counter = await _context.StockCountCounters
            .FirstOrDefaultAsync(c => c.StockTakeId == stockTakeId && c.UserId == userId, ct)
            .ConfigureAwait(false);

        if (counter == null)
        {
            counter = await AssignCounterAsync(stockTakeId, userId, false, ct).ConfigureAwait(false);
        }

        counter.StartedAt = DateTime.UtcNow;
        counter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return counter;
    }

    /// <inheritdoc />
    public async Task<StockCountCounter> CompleteCounterSessionAsync(int stockTakeId, int userId, CancellationToken ct = default)
    {
        var counter = await _context.StockCountCounters
            .FirstOrDefaultAsync(c => c.StockTakeId == stockTakeId && c.UserId == userId, ct)
            .ConfigureAwait(false);

        if (counter == null)
            throw new InvalidOperationException($"Counter not found for user {userId} in stock take {stockTakeId}.");

        counter.CompletedAt = DateTime.UtcNow;
        counter.ItemsCounted = await _context.StockTakeItems
            .CountAsync(i => i.StockTakeId == stockTakeId && i.CountedByUserId == userId, ct)
            .ConfigureAwait(false);
        counter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return counter;
    }

    #endregion

    #region Reporting

    /// <inheritdoc />
    public async Task<StockCountVarianceReport> GetVarianceReportAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Include(st => st.Items)
                .ThenInclude(i => i.CountedByUser)
            .Include(st => st.Store)
            .Include(st => st.StartedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        var countedItems = stockTake.Items.Where(i => i.IsCounted).ToList();
        var varianceItems = countedItems.Where(i => i.VarianceQuantity != 0).ToList();

        var report = new StockCountVarianceReport
        {
            StockTakeId = stockTake.Id,
            StockTakeNumber = stockTake.StockTakeNumber,
            StoreName = stockTake.Store?.Name,
            CountType = stockTake.CountType,
            ReportDate = DateTime.UtcNow,
            CountDate = stockTake.CountDate,
            CountStarted = stockTake.StartedAt,
            CountCompleted = stockTake.CompletedAt,
            PreparedBy = stockTake.StartedByUser?.DisplayName ?? stockTake.StartedByUser?.Username,
            TotalItemsCounted = countedItems.Count,
            ItemsWithVariance = varianceItems.Count,
            ItemVariancePercentage = countedItems.Count > 0 ? (decimal)varianceItems.Count / countedItems.Count * 100 : 0,
            TotalSystemValue = countedItems.Sum(i => i.SystemValue),
            TotalCountedValue = countedItems.Sum(i => i.CountedValue ?? 0),
            TotalVarianceValue = countedItems.Sum(i => i.VarianceValue),
            ShrinkageRate = stockTake.ShrinkagePercentage
        };

        report.TotalVariancePercentage = report.TotalSystemValue != 0
            ? report.TotalVarianceValue / report.TotalSystemValue * 100
            : 0;

        // Variance by category
        report.VarianceByCategory = varianceItems
            .GroupBy(i => new { i.Product.CategoryId, CategoryName = i.Product.Category?.Name ?? "Uncategorized" })
            .Select(g => new CategoryVarianceSummary
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                ItemsCounted = g.Count(),
                SystemValue = g.Sum(i => i.SystemValue),
                CountedValue = g.Sum(i => i.CountedValue ?? 0),
                VarianceValue = g.Sum(i => i.VarianceValue),
                VariancePercentage = g.Sum(i => i.SystemValue) != 0
                    ? g.Sum(i => i.VarianceValue) / g.Sum(i => i.SystemValue) * 100
                    : 0
            })
            .OrderByDescending(c => Math.Abs(c.VarianceValue))
            .ToList();

        // Variance by cause
        report.VarianceByCause = varianceItems
            .Where(i => i.VarianceCause.HasValue)
            .GroupBy(i => i.VarianceCause!.Value)
            .Select(g => new CauseVarianceSummary
            {
                Cause = g.Key,
                CauseName = g.Key.ToString(),
                ItemCount = g.Count(),
                TotalVarianceValue = g.Sum(i => i.VarianceValue),
                PercentOfTotalShrinkage = report.TotalVarianceValue != 0
                    ? Math.Abs(g.Sum(i => i.VarianceValue)) / Math.Abs(report.TotalVarianceValue) * 100
                    : 0
            })
            .OrderByDescending(c => Math.Abs(c.TotalVarianceValue))
            .ToList();

        // Detailed item variances
        report.ItemVariances = varianceItems
            .OrderByDescending(i => Math.Abs(i.VarianceValue))
            .Select(i => new ItemVarianceDetail
            {
                StockTakeItemId = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.ProductSku,
                ProductName = i.ProductName,
                CategoryName = i.Product.Category?.Name,
                Location = i.Location,
                SystemQuantity = i.SystemQuantity,
                CountedQuantity = i.FinalCountQuantity ?? i.CountedQuantity ?? 0,
                VarianceQuantity = i.VarianceQuantity,
                UnitCost = i.SystemCostPrice,
                VarianceValue = i.VarianceValue,
                VariancePercentage = i.VariancePercentage,
                Cause = i.VarianceCause,
                Notes = i.VarianceNotes ?? i.Notes,
                ExceedsThreshold = i.ExceedsThreshold,
                CountedBy = i.CountedByUser?.DisplayName ?? i.CountedByUser?.Username,
                CountedAt = i.CountedAt
            })
            .ToList();

        // Significant variances (exceeded threshold)
        report.SignificantVariances = report.ItemVariances
            .Where(i => i.ExceedsThreshold)
            .ToList();

        return report;
    }

    /// <inheritdoc />
    public async Task<ShrinkageAnalysisReport> GetShrinkageAnalysisAsync(
        ShrinkageReportFilterDto filter,
        CancellationToken ct = default)
    {
        var stockTakes = await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Include(st => st.Store)
            .Where(st => st.Status == StockTakeStatus.Posted)
            .Where(st => st.PostedAt >= filter.FromDate && st.PostedAt <= filter.ToDate)
            .Where(st => !filter.StoreId.HasValue || st.StoreId == filter.StoreId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var allVarianceItems = stockTakes
            .SelectMany(st => st.Items)
            .Where(i => i.IsCounted && i.VarianceQuantity != 0)
            .ToList();

        var store = filter.StoreId.HasValue
            ? await _context.Stores.FindAsync(new object[] { filter.StoreId.Value }, ct).ConfigureAwait(false)
            : null;

        var report = new ShrinkageAnalysisReport
        {
            StoreId = filter.StoreId,
            StoreName = store?.Name,
            PeriodStart = filter.FromDate,
            PeriodEnd = filter.ToDate,
            TotalInventoryValue = stockTakes.Sum(st => st.TotalSystemValue),
            TotalShrinkageValue = stockTakes.Sum(st => Math.Abs(st.Items.Where(i => i.VarianceValue < 0).Sum(i => i.VarianceValue))),
            CountsDuringPeriod = stockTakes.Count
        };

        report.ShrinkageRate = report.TotalInventoryValue != 0
            ? report.TotalShrinkageValue / report.TotalInventoryValue * 100
            : 0;

        // Monthly trend
        report.MonthlyTrend = stockTakes
            .GroupBy(st => new { st.PostedAt!.Value.Year, st.PostedAt!.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ShrinkageTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                ShrinkageValue = g.Sum(st => Math.Abs(st.Items.Where(i => i.VarianceValue < 0).Sum(i => i.VarianceValue))),
                ShrinkageRate = g.Sum(st => st.TotalSystemValue) != 0
                    ? g.Sum(st => Math.Abs(st.Items.Where(i => i.VarianceValue < 0).Sum(i => i.VarianceValue))) /
                      g.Sum(st => st.TotalSystemValue) * 100
                    : 0,
                CountsPerformed = g.Count()
            })
            .ToList();

        // By category
        report.ByCategory = allVarianceItems
            .Where(i => i.VarianceValue < 0)
            .GroupBy(i => new { i.Product.CategoryId, CategoryName = i.Product.Category?.Name ?? "Uncategorized" })
            .Select(g => new CategoryVarianceSummary
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                ItemsCounted = g.Count(),
                VarianceValue = g.Sum(i => Math.Abs(i.VarianceValue)),
                VariancePercentage = report.TotalShrinkageValue != 0
                    ? g.Sum(i => Math.Abs(i.VarianceValue)) / report.TotalShrinkageValue * 100
                    : 0
            })
            .OrderByDescending(c => c.VarianceValue)
            .ToList();

        // By cause
        report.ByCause = allVarianceItems
            .Where(i => i.VarianceCause.HasValue && i.VarianceValue < 0)
            .GroupBy(i => i.VarianceCause!.Value)
            .Select(g => new CauseVarianceSummary
            {
                Cause = g.Key,
                CauseName = g.Key.ToString(),
                ItemCount = g.Count(),
                TotalVarianceValue = g.Sum(i => Math.Abs(i.VarianceValue)),
                PercentOfTotalShrinkage = report.TotalShrinkageValue != 0
                    ? g.Sum(i => Math.Abs(i.VarianceValue)) / report.TotalShrinkageValue * 100
                    : 0
            })
            .OrderByDescending(c => c.TotalVarianceValue)
            .ToList();

        // Top shrinking items
        report.TopShrinkingItems = allVarianceItems
            .Where(i => i.VarianceValue < 0)
            .OrderByDescending(i => Math.Abs(i.VarianceValue))
            .Take(filter.TopItemsCount)
            .Select(i => new ItemVarianceDetail
            {
                ProductId = i.ProductId,
                ProductSku = i.ProductSku,
                ProductName = i.ProductName,
                CategoryName = i.Product.Category?.Name,
                VarianceQuantity = i.VarianceQuantity,
                VarianceValue = i.VarianceValue,
                Cause = i.VarianceCause
            })
            .ToList();

        // Recurring variances
        report.RecurringVariances = allVarianceItems
            .Where(i => i.VarianceValue < 0)
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => new RecurringVarianceItem
            {
                ProductId = g.Key,
                ProductSku = g.First().ProductSku,
                ProductName = g.First().ProductName,
                VarianceOccurrences = g.Count(),
                AverageVarianceQuantity = g.Average(i => i.VarianceQuantity),
                TotalVarianceValue = g.Sum(i => Math.Abs(i.VarianceValue)),
                MostCommonCause = g.Where(i => i.VarianceCause.HasValue)
                    .GroupBy(i => i.VarianceCause)
                    .OrderByDescending(cg => cg.Count())
                    .FirstOrDefault()?.Key
            })
            .OrderByDescending(r => r.VarianceOccurrences)
            .Take(20)
            .ToList();

        return report;
    }

    /// <inheritdoc />
    public async Task<HistoricalVarianceReport> GetHistoricalVarianceAsync(
        int storeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        var store = await _context.Stores.FindAsync(new object[] { storeId }, ct).ConfigureAwait(false);

        var stockTakes = await _context.StockTakes
            .Where(st => st.StoreId == storeId)
            .Where(st => st.Status == StockTakeStatus.Posted)
            .Where(st => st.PostedAt >= fromDate && st.PostedAt <= toDate)
            .OrderBy(st => st.PostedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var report = new HistoricalVarianceReport
        {
            StoreId = storeId,
            StoreName = store?.Name ?? "Unknown",
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            CountSummaries = stockTakes.Select(st => new StockCountSummary
            {
                StockTakeId = st.Id,
                StockTakeNumber = st.StockTakeNumber,
                CountDate = st.CountDate,
                CountType = st.CountType,
                ItemsCounted = st.ItemsCounted,
                ItemsWithVariance = st.ItemsWithVariance,
                TotalSystemValue = st.TotalSystemValue,
                TotalCountedValue = st.TotalCountedValue,
                TotalVarianceValue = st.TotalVarianceValue,
                ShrinkageRate = st.ShrinkagePercentage
            }).ToList(),
            Trend = stockTakes
                .GroupBy(st => new { st.PostedAt!.Value.Year, st.PostedAt!.Value.Month })
                .Select(g => new ShrinkageTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    ShrinkageValue = g.Sum(st => Math.Abs(st.TotalVarianceValue)),
                    ShrinkageRate = g.Average(st => st.ShrinkagePercentage),
                    CountsPerformed = g.Count()
                })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToList()
        };

        return report;
    }

    #endregion

    #region Export

    /// <inheritdoc />
    public async Task<byte[]> ExportCountSheetAsync(int stockTakeId, CancellationToken ct = default)
    {
        // Generate HTML and convert to PDF (simplified - use a PDF library in production)
        var html = await GenerateCountSheetHtmlAsync(stockTakeId, ct).ConfigureAwait(false);
        return Encoding.UTF8.GetBytes(html);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportVarianceReportAsync(int stockTakeId, ExportFormat format, CancellationToken ct = default)
    {
        switch (format)
        {
            case ExportFormat.Csv:
                return await GenerateVarianceCsvAsync(stockTakeId, ct).ConfigureAwait(false);
            case ExportFormat.Excel:
            case ExportFormat.Pdf:
            default:
                var html = await GenerateVarianceReportHtmlAsync(stockTakeId, ct).ConfigureAwait(false);
                return Encoding.UTF8.GetBytes(html);
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateVarianceReportHtmlAsync(int stockTakeId, CancellationToken ct = default)
    {
        var report = await GetVarianceReportAsync(stockTakeId, ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial; margin: 20px; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background: #f4f4f4; }");
        sb.AppendLine(".negative { color: red; }");
        sb.AppendLine(".positive { color: green; }");
        sb.AppendLine(".summary { background: #e3f2fd; padding: 15px; margin: 10px 0; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Stock Count Variance Report</h1>");
        sb.AppendLine($"<h3>{report.StockTakeNumber}</h3>");
        sb.AppendLine($"<p>Store: {report.StoreName ?? "All Stores"} | Count Date: {report.CountDate:d} | Report Date: {report.ReportDate:g}</p>");

        // Executive Summary
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h3>Executive Summary</h3>");
        sb.AppendLine($"<p>Total Items Counted: {report.TotalItemsCounted}</p>");
        sb.AppendLine($"<p>Items with Variance: {report.ItemsWithVariance} ({report.ItemVariancePercentage:F1}%)</p>");
        sb.AppendLine($"<p>Total System Value: {report.TotalSystemValue:C}</p>");
        sb.AppendLine($"<p>Total Counted Value: {report.TotalCountedValue:C}</p>");
        sb.AppendLine($"<p>Total Variance: <span class='{(report.TotalVarianceValue < 0 ? "negative" : "positive")}'>{report.TotalVarianceValue:C}</span></p>");
        sb.AppendLine($"<p>Shrinkage Rate: {report.ShrinkageRate:F2}%</p>");
        sb.AppendLine("</div>");

        // Variance by Category
        if (report.VarianceByCategory.Any())
        {
            sb.AppendLine("<h3>Variance by Category</h3>");
            sb.AppendLine("<table><tr><th>Category</th><th>Items</th><th>System Value</th><th>Counted Value</th><th>Variance</th><th>%</th></tr>");
            foreach (var cat in report.VarianceByCategory)
            {
                var cls = cat.VarianceValue < 0 ? "negative" : "positive";
                sb.AppendLine($"<tr><td>{cat.CategoryName}</td><td>{cat.ItemsCounted}</td><td>{cat.SystemValue:C}</td><td>{cat.CountedValue:C}</td><td class='{cls}'>{cat.VarianceValue:C}</td><td>{cat.VariancePercentage:F1}%</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Detailed Variances
        sb.AppendLine("<h3>Detailed Item Variances</h3>");
        sb.AppendLine("<table><tr><th>SKU</th><th>Product</th><th>Category</th><th>System</th><th>Counted</th><th>Variance</th><th>Value</th><th>Cause</th></tr>");
        foreach (var item in report.ItemVariances.Take(100))
        {
            var cls = item.VarianceValue < 0 ? "negative" : "positive";
            sb.AppendLine($"<tr><td>{item.ProductSku}</td><td>{item.ProductName}</td><td>{item.CategoryName}</td>");
            sb.AppendLine($"<td>{item.SystemQuantity:F2}</td><td>{item.CountedQuantity:F2}</td>");
            sb.AppendLine($"<td class='{cls}'>{item.VarianceQuantity:+0.##;-0.##;0}</td>");
            sb.AppendLine($"<td class='{cls}'>{item.VarianceValue:C}</td>");
            sb.AppendLine($"<td>{item.Cause?.ToString() ?? "-"}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private async Task<string> GenerateCountSheetHtmlAsync(int stockTakeId, CancellationToken ct)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId, ct).ConfigureAwait(false);
        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial; margin: 20px; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #000; padding: 8px; }");
        sb.AppendLine("th { background: #f4f4f4; }");
        sb.AppendLine(".count-box { width: 80px; height: 30px; border: 1px solid #000; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Stock Count Sheet</h1>");
        sb.AppendLine($"<h3>{stockTake.StockTakeNumber}</h3>");
        sb.AppendLine($"<p>Date: {stockTake.CountDate:d} | Type: {stockTake.CountType}</p>");

        sb.AppendLine("<table><tr><th>SKU</th><th>Product</th><th>Location</th>");
        if (!stockTake.IsBlindCount)
            sb.AppendLine("<th>System Qty</th>");
        sb.AppendLine("<th>Count</th><th>Notes</th></tr>");

        foreach (var item in stockTake.Items.OrderBy(i => i.ProductName))
        {
            sb.AppendLine($"<tr><td>{item.ProductCode}</td><td>{item.ProductName}</td><td>{item.Location ?? "-"}</td>");
            if (!stockTake.IsBlindCount)
                sb.AppendLine($"<td>{item.SystemQuantity:F2}</td>");
            sb.AppendLine("<td><div class='count-box'></div></td><td></td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<br/><p>Counted By: _______________ Date: ___________ Signature: _______________</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private async Task<byte[]> GenerateVarianceCsvAsync(int stockTakeId, CancellationToken ct)
    {
        var report = await GetVarianceReportAsync(stockTakeId, ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("SKU,Product,Category,Location,System Qty,Counted Qty,Variance Qty,Unit Cost,Variance Value,Cause,Notes");

        foreach (var item in report.ItemVariances)
        {
            sb.AppendLine($"\"{item.ProductSku}\",\"{item.ProductName}\",\"{item.CategoryName}\",\"{item.Location}\"," +
                $"{item.SystemQuantity},{item.CountedQuantity},{item.VarianceQuantity},{item.UnitCost},{item.VarianceValue}," +
                $"\"{item.Cause}\",\"{item.Notes?.Replace("\"", "\"\"")}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

    #region Scheduling

    /// <inheritdoc />
    public async Task<IEnumerable<StockCountSchedule>> GetSchedulesAsync(int storeId, CancellationToken ct = default)
    {
        return await _context.StockCountSchedules
            .Include(s => s.Category)
            .Where(s => s.StoreId == storeId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockCountSchedule?> GetScheduleAsync(int scheduleId, CancellationToken ct = default)
    {
        return await _context.StockCountSchedules
            .Include(s => s.Category)
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockCountSchedule> SaveScheduleAsync(StockCountScheduleDto dto, CancellationToken ct = default)
    {
        StockCountSchedule schedule;

        if (dto.Id.HasValue)
        {
            schedule = await _context.StockCountSchedules
                .FirstOrDefaultAsync(s => s.Id == dto.Id.Value, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Schedule {dto.Id} not found.");
        }
        else
        {
            schedule = new StockCountSchedule { CreatedAt = DateTime.UtcNow };
            _context.StockCountSchedules.Add(schedule);
        }

        schedule.StoreId = dto.StoreId;
        schedule.Name = dto.Name;
        schedule.CountType = dto.CountType;
        schedule.Frequency = dto.Frequency;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.DayOfMonth = dto.DayOfMonth;
        schedule.Month = dto.Month;
        schedule.TimeOfDay = dto.TimeOfDay;
        schedule.CategoryId = dto.CategoryId;
        schedule.LocationFilter = dto.LocationFilter;
        schedule.IsEnabled = dto.IsEnabled;
        schedule.SendReminder = dto.SendReminder;
        schedule.ReminderDaysBefore = dto.ReminderDaysBefore;
        schedule.DefaultAssigneeIds = dto.DefaultAssigneeIds?.Any() == true
            ? string.Join(",", dto.DefaultAssigneeIds)
            : null;
        schedule.UseBlindCount = dto.UseBlindCount;
        schedule.UseDoubleBlind = dto.UseDoubleBlind;
        schedule.Notes = dto.Notes;
        schedule.UpdatedAt = DateTime.UtcNow;

        // Calculate next run date
        schedule.NextRunDate = CalculateNextRunDate(schedule);

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return schedule;
    }

    /// <inheritdoc />
    public async Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default)
    {
        var schedule = await _context.StockCountSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            .ConfigureAwait(false);

        if (schedule != null)
        {
            _context.StockCountSchedules.Remove(schedule);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> TriggerScheduledCountsAsync(CancellationToken ct = default)
    {
        var dueSchedules = await _context.StockCountSchedules
            .Where(s => s.IsEnabled && s.NextRunDate != null && s.NextRunDate <= DateTime.UtcNow)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var createdCounts = new List<StockTake>();

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var dto = new CreateStockCountDto
                {
                    StoreId = schedule.StoreId,
                    CountType = schedule.CountType,
                    CountDate = DateTime.UtcNow,
                    CategoryId = schedule.CategoryId,
                    LocationFilter = schedule.LocationFilter,
                    IsBlindCount = schedule.UseBlindCount,
                    IsDoubleBlind = schedule.UseDoubleBlind,
                    Notes = $"Auto-generated from schedule: {schedule.Name}",
                    AssignedCounterUserIds = !string.IsNullOrEmpty(schedule.DefaultAssigneeIds)
                        ? schedule.DefaultAssigneeIds.Split(',').Select(int.Parse).ToList()
                        : null
                };

                // Use system user ID 1 for scheduled counts
                var stockTake = await CreateStockCountAsync(dto, 1, ct).ConfigureAwait(false);
                createdCounts.Add(stockTake);

                schedule.LastRunDate = DateTime.UtcNow;
                schedule.NextRunDate = CalculateNextRunDate(schedule);

                _logger.Information(
                    "Scheduled stock count created: {Number} from schedule: {Schedule}",
                    stockTake.StockTakeNumber, schedule.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create scheduled stock count for schedule: {Schedule}", schedule.Name);
            }
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return createdCounts;
    }

    private DateTime? CalculateNextRunDate(StockCountSchedule schedule)
    {
        if (!schedule.IsEnabled) return null;

        var baseDate = schedule.LastRunDate ?? DateTime.UtcNow;
        var time = schedule.TimeOfDay ?? TimeSpan.Zero;

        return schedule.Frequency switch
        {
            RecurrenceFrequency.Daily => baseDate.Date.AddDays(1).Add(time),
            RecurrenceFrequency.Weekly => GetNextWeekday(baseDate, schedule.DayOfWeek ?? 0).Add(time),
            RecurrenceFrequency.BiWeekly => GetNextWeekday(baseDate, schedule.DayOfWeek ?? 0).AddDays(7).Add(time),
            RecurrenceFrequency.Monthly => GetNextMonthDay(baseDate, schedule.DayOfMonth ?? 1).Add(time),
            RecurrenceFrequency.Quarterly => GetNextQuarterDay(baseDate, schedule.DayOfMonth ?? 1, schedule.Month ?? 1).Add(time),
            RecurrenceFrequency.Annual => GetNextAnnualDay(baseDate, schedule.Month ?? 1, schedule.DayOfMonth ?? 1).Add(time),
            _ => null
        };
    }

    private static DateTime GetNextWeekday(DateTime from, int dayOfWeek)
    {
        var daysUntil = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return from.Date.AddDays(daysUntil);
    }

    private static DateTime GetNextMonthDay(DateTime from, int dayOfMonth)
    {
        var next = new DateTime(from.Year, from.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(from.Year, from.Month)));
        if (next <= from) next = next.AddMonths(1);
        return next;
    }

    private static DateTime GetNextQuarterDay(DateTime from, int dayOfMonth, int startMonth)
    {
        var quarterMonth = ((from.Month - startMonth) / 3 + 1) * 3 + startMonth;
        if (quarterMonth > 12) quarterMonth -= 12;
        var year = quarterMonth < from.Month ? from.Year + 1 : from.Year;
        return new DateTime(year, quarterMonth, Math.Min(dayOfMonth, DateTime.DaysInMonth(year, quarterMonth)));
    }

    private static DateTime GetNextAnnualDay(DateTime from, int month, int day)
    {
        var year = from.Year;
        var next = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
        if (next <= from) next = next.AddYears(1);
        return next;
    }

    #endregion

    #region Threshold Configuration

    /// <inheritdoc />
    public async Task<IEnumerable<VarianceThreshold>> GetThresholdsAsync(int storeId, CancellationToken ct = default)
    {
        return await _context.VarianceThresholds
            .Include(t => t.Category)
            .Include(t => t.Product)
            .Where(t => t.StoreId == storeId && t.IsActive)
            .OrderBy(t => t.Priority)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<VarianceThreshold> SaveThresholdAsync(VarianceThreshold threshold, CancellationToken ct = default)
    {
        if (threshold.Id == 0)
        {
            threshold.CreatedAt = DateTime.UtcNow;
            _context.VarianceThresholds.Add(threshold);
        }
        else
        {
            threshold.UpdatedAt = DateTime.UtcNow;
            _context.VarianceThresholds.Update(threshold);
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return threshold;
    }

    /// <inheritdoc />
    public async Task DeleteThresholdAsync(int thresholdId, CancellationToken ct = default)
    {
        var threshold = await _context.VarianceThresholds
            .FirstOrDefaultAsync(t => t.Id == thresholdId, ct)
            .ConfigureAwait(false);

        if (threshold != null)
        {
            _context.VarianceThresholds.Remove(threshold);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<VarianceThreshold?> GetApplicableThresholdAsync(
        int storeId,
        int productId,
        int? categoryId,
        CancellationToken ct = default)
    {
        // Priority: Product-specific > Category-specific > Store-wide
        var threshold = await _context.VarianceThresholds
            .Where(t => t.StoreId == storeId && t.IsActive && t.ProductId == productId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (threshold != null) return threshold;

        if (categoryId.HasValue)
        {
            threshold = await _context.VarianceThresholds
                .Where(t => t.StoreId == storeId && t.IsActive && t.CategoryId == categoryId && t.ProductId == null)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (threshold != null) return threshold;
        }

        return await _context.VarianceThresholds
            .Where(t => t.StoreId == storeId && t.IsActive && t.CategoryId == null && t.ProductId == null)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    #endregion

    #region Utility

    /// <inheritdoc />
    public async Task<string> GenerateStockTakeNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SC-{year}-";

        var latestNumber = await _context.StockTakes
            .Where(st => st.StockTakeNumber.StartsWith(prefix))
            .OrderByDescending(st => st.StockTakeNumber)
            .Select(st => st.StockTakeNumber)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        int nextSequence = 1;
        if (!string.IsNullOrEmpty(latestNumber))
        {
            var sequencePart = latestNumber.Substring(prefix.Length);
            if (int.TryParse(sequencePart, out var lastSequence))
            {
                nextSequence = lastSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D3}";
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> GetItemsPendingCountAsync(
        int stockTakeId,
        int userId,
        CancellationToken ct = default)
    {
        return await _context.StockTakeItems
            .Include(i => i.Product)
            .Where(i => i.StockTakeId == stockTakeId && !i.IsCounted)
            .OrderBy(i => i.ProductName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockCountProgress> GetCountProgressAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .Include(st => st.Counters)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        var progress = new StockCountProgress
        {
            StockTakeId = stockTakeId,
            TotalItems = stockTake.TotalItemsToCount,
            ItemsCounted = stockTake.Items.Count(i => i.IsCounted),
            ItemsRemaining = stockTake.Items.Count(i => !i.IsCounted),
            ItemsWithVariance = stockTake.Items.Count(i => i.IsCounted && i.VarianceQuantity != 0),
            ItemsExceedingThreshold = stockTake.Items.Count(i => i.ExceedsThreshold),
            MismatchesRequiringResolution = stockTake.Items.Count(i => i.CountMismatch && !i.ResolvedQuantity.HasValue)
        };

        progress.ProgressPercentage = progress.TotalItems > 0
            ? Math.Round((decimal)progress.ItemsCounted / progress.TotalItems * 100, 1)
            : 0;

        progress.CounterProgress = stockTake.Counters.ToDictionary(
            c => c.UserId,
            c => new CounterProgress
            {
                UserId = c.UserId,
                UserName = c.User?.DisplayName ?? c.User?.Username ?? "Unknown",
                IsPrimary = c.IsPrimaryCounter,
                ItemsCounted = c.ItemsCounted,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,
                IsComplete = c.CompletedAt.HasValue
            });

        return progress;
    }

    /// <inheritdoc />
    public async Task FreezeInventoryAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null) return;

        stockTake.FrozenAt = DateTime.UtcNow;
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.Information("Inventory frozen for stock take {Number}", stockTake.StockTakeNumber);
    }

    /// <inheritdoc />
    public async Task UnfreezeInventoryAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null) return;

        stockTake.UnfrozenAt = DateTime.UtcNow;
        stockTake.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.Information("Inventory unfrozen for stock take {Number}", stockTake.StockTakeNumber);
    }

    /// <inheritdoc />
    public async Task<bool> IsInventoryFrozenAsync(int? storeId, CancellationToken ct = default)
    {
        return await _context.StockTakes
            .AnyAsync(st =>
                st.FreezeInventory &&
                st.FrozenAt.HasValue &&
                !st.UnfrozenAt.HasValue &&
                st.Status == StockTakeStatus.InProgress &&
                (!storeId.HasValue || st.StoreId == storeId), ct)
            .ConfigureAwait(false);
    }

    private async Task UpdateStockTakeSummaryAsync(int stockTakeId, CancellationToken ct = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake == null) return;

        var countedItems = stockTake.Items.Where(i => i.IsCounted).ToList();

        stockTake.ItemsCounted = countedItems.Count;
        stockTake.ItemsWithVariance = countedItems.Count(i => i.VarianceQuantity != 0);
        stockTake.TotalCountedValue = countedItems.Sum(i => i.CountedValue ?? 0);
        stockTake.TotalVarianceValue = countedItems.Sum(i => i.VarianceValue);

        stockTake.ShrinkagePercentage = stockTake.TotalSystemValue != 0
            ? Math.Abs(countedItems.Where(i => i.VarianceValue < 0).Sum(i => i.VarianceValue)) / stockTake.TotalSystemValue * 100
            : 0;

        stockTake.UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
