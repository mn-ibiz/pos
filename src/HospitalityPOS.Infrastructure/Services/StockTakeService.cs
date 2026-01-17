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

    /// <inheritdoc />
    public async Task<StockTake> StartStockTakeAsync(int userId, string? notes = null, int? categoryId = null)
    {
        // Check if there's already an in-progress stock take
        var existingStockTake = await GetInProgressStockTakeAsync().ConfigureAwait(false);
        if (existingStockTake is not null)
        {
            throw new InvalidOperationException(
                $"Cannot start a new stock take while another is in progress: {existingStockTake.StockTakeNumber}");
        }

        var stockTakeNumber = await GenerateStockTakeNumberAsync().ConfigureAwait(false);

        var stockTake = new StockTake
        {
            StockTakeNumber = stockTakeNumber,
            StartedByUserId = userId,
            StartedAt = DateTime.UtcNow,
            Status = StockTakeStatus.InProgress,
            Notes = notes
        };

        // Query products with inventory tracking enabled
        var productsQuery = _context.Products
            .Include(p => p.Inventory)
            .Where(p => p.IsActive && p.TrackInventory);

        if (categoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await productsQuery.ToListAsync().ConfigureAwait(false);

        // Create stock take items for each product
        foreach (var product in products)
        {
            var currentStock = product.Inventory?.CurrentStock ?? 0;

            var item = new StockTakeItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Code,
                SystemQuantity = currentStock,
                CostPrice = product.CostPrice,
                IsCounted = false,
                IsApproved = false
            };

            stockTake.Items.Add(item);
        }

        stockTake.TotalItems = stockTake.Items.Count;

        _context.StockTakes.Add(stockTake);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock take {StockTakeNumber} started by user {UserId} with {ItemCount} items",
            stockTakeNumber, userId, stockTake.Items.Count);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake?> GetStockTakeAsync(int stockTakeId)
    {
        return await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> GetStockTakesAsync(
        StockTakeStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.StockTakes
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(st => st.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(st => st.StartedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(st => st.StartedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(st => st.StartedAt)
            .ToListAsync()
            .ConfigureAwait(false);
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

        if (item is null)
        {
            throw new InvalidOperationException($"Stock take item with ID {stockTakeItemId} not found.");
        }

        if (item.StockTake.Status != StockTakeStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot record count - stock take is in status: {item.StockTake.Status}");
        }

        // Record the count
        item.PhysicalQuantity = physicalQuantity;
        item.VarianceQuantity = physicalQuantity - item.SystemQuantity;
        item.VarianceValue = item.VarianceQuantity * item.CostPrice;
        item.IsCounted = true;
        item.CountedAt = DateTime.UtcNow;
        item.CountedByUserId = userId;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            item.Notes = notes;
        }

        // Update stock take summary
        await UpdateStockTakeSummaryAsync(item.StockTakeId).ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Debug(
            "Count recorded for item {ItemId} in stock take {StockTakeId}: System={System}, Physical={Physical}, Variance={Variance}",
            stockTakeItemId, item.StockTakeId, item.SystemQuantity, physicalQuantity, item.VarianceQuantity);

        return item;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> RecordCountsAsync(
        int stockTakeId,
        Dictionary<int, decimal> counts,
        int userId)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId)
            .ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        if (stockTake.Status != StockTakeStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot record counts - stock take is in status: {stockTake.Status}");
        }

        var updatedItems = new List<StockTakeItem>();

        foreach (var (productId, physicalQuantity) in counts)
        {
            var item = stockTake.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is null)
            {
                _logger.Warning(
                    "Product {ProductId} not found in stock take {StockTakeId}",
                    productId, stockTakeId);
                continue;
            }

            item.PhysicalQuantity = physicalQuantity;
            item.VarianceQuantity = physicalQuantity - item.SystemQuantity;
            item.VarianceValue = item.VarianceQuantity * item.CostPrice;
            item.IsCounted = true;
            item.CountedAt = DateTime.UtcNow;
            item.CountedByUserId = userId;

            updatedItems.Add(item);
        }

        // Update stock take summary
        await UpdateStockTakeSummaryAsync(stockTakeId).ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Recorded {Count} counts for stock take {StockTakeId}",
            updatedItems.Count, stockTakeId);

        return updatedItems;
    }

    /// <inheritdoc />
    public async Task<StockTake> SaveDraftAsync(int stockTakeId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        if (stockTake.Status != StockTakeStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot save draft - stock take is in status: {stockTake.Status}");
        }

        // Ensure summary is up to date
        await UpdateStockTakeSummaryAsync(stockTakeId).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Stock take {StockTakeNumber} draft saved", stockTake.StockTakeNumber);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> SubmitForApprovalAsync(int stockTakeId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        if (stockTake.Status != StockTakeStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot submit for approval - stock take is in status: {stockTake.Status}");
        }

        // Update summary before submission
        await UpdateStockTakeSummaryAsync(stockTakeId).ConfigureAwait(false);

        stockTake.Status = StockTakeStatus.PendingApproval;

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock take {StockTakeNumber} submitted for approval. Counted: {Counted}/{Total}, Variance: {Variance:C}",
            stockTake.StockTakeNumber, stockTake.CountedItems, stockTake.TotalItems, stockTake.TotalVarianceValue);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> ApproveStockTakeAsync(int stockTakeId, int approverUserId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        if (stockTake.Status != StockTakeStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Cannot approve - stock take is in status: {stockTake.Status}");
        }

        // Apply adjustments for items with variance
        var itemsWithVariance = stockTake.Items.Where(i => i.IsCounted && i.VarianceQuantity != 0).ToList();

        foreach (var item in itemsWithVariance)
        {
            // Adjust stock to match physical count
            await _inventoryService.AdjustStockAsync(
                item.ProductId,
                item.PhysicalQuantity!.Value,
                $"Stock Take: {stockTake.StockTakeNumber}",
                $"Variance: {item.VarianceQuantity:+0.###;-0.###;0}")
                .ConfigureAwait(false);

            item.IsApproved = true;
        }

        stockTake.Status = StockTakeStatus.Approved;
        stockTake.CompletedAt = DateTime.UtcNow;
        stockTake.ApprovedByUserId = approverUserId;

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock take {StockTakeNumber} approved by user {UserId}. {AdjustmentCount} adjustments applied.",
            stockTake.StockTakeNumber, approverUserId, itemsWithVariance.Count);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTake> CancelStockTakeAsync(int stockTakeId, string reason)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        if (stockTake.Status == StockTakeStatus.Approved)
        {
            throw new InvalidOperationException("Cannot cancel an approved stock take.");
        }

        stockTake.Status = StockTakeStatus.Cancelled;
        stockTake.Notes = string.IsNullOrWhiteSpace(stockTake.Notes)
            ? $"Cancelled: {reason}"
            : $"{stockTake.Notes}\nCancelled: {reason}";

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock take {StockTakeNumber} cancelled. Reason: {Reason}",
            stockTake.StockTakeNumber, reason);

        return stockTake;
    }

    /// <inheritdoc />
    public async Task<StockTakeVarianceSummary> GetVarianceSummaryAsync(int stockTakeId)
    {
        var stockTake = await GetStockTakeAsync(stockTakeId).ConfigureAwait(false);

        if (stockTake is null)
        {
            throw new InvalidOperationException($"Stock take with ID {stockTakeId} not found.");
        }

        var countedItems = stockTake.Items.Where(i => i.IsCounted).ToList();
        var shortages = countedItems.Where(i => i.VarianceQuantity < 0).ToList();
        var overages = countedItems.Where(i => i.VarianceQuantity > 0).ToList();

        return new StockTakeVarianceSummary
        {
            StockTakeId = stockTake.Id,
            StockTakeNumber = stockTake.StockTakeNumber,
            TotalItems = stockTake.TotalItems,
            CountedItems = countedItems.Count,
            ItemsWithVariance = shortages.Count + overages.Count,
            ShortageValue = shortages.Sum(i => Math.Abs(i.VarianceValue)),
            OverageValue = overages.Sum(i => i.VarianceValue),
            NetVarianceValue = countedItems.Sum(i => i.VarianceValue),
            Shortages = shortages,
            Overages = overages
        };
    }

    /// <inheritdoc />
    public async Task<string> GenerateStockTakeNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"ST-{today:yyyyMMdd}-";

        // Get the latest stock take number with this prefix
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

    /// <summary>
    /// Updates the stock take summary based on current item counts.
    /// </summary>
    private async Task UpdateStockTakeSummaryAsync(int stockTakeId)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId)
            .ConfigureAwait(false);

        if (stockTake is null) return;

        stockTake.CountedItems = stockTake.Items.Count(i => i.IsCounted);
        stockTake.ItemsWithVariance = stockTake.Items.Count(i => i.IsCounted && i.VarianceQuantity != 0);
        stockTake.TotalVarianceValue = stockTake.Items.Where(i => i.IsCounted).Sum(i => i.VarianceValue);
    }
}
