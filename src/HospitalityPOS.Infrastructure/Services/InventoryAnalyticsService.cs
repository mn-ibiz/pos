using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for inventory analytics and reporting.
/// </summary>
public class InventoryAnalyticsService : IInventoryAnalyticsService
{
    private readonly POSDbContext _context;

    public InventoryAnalyticsService(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Stock Valuation Methods

    /// <inheritdoc />
    public async Task<StockValuationConfig?> GetStockValuationConfigAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StockValuationConfigs
            .FirstOrDefaultAsync(c => c.StoreId == storeId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockValuationConfig> SaveStockValuationConfigAsync(
        StockValuationConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.StockValuationConfigs.Add(config);
        }
        else
        {
            var existing = await _context.StockValuationConfigs
                .FirstOrDefaultAsync(c => c.Id == config.Id && !c.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Stock valuation config {config.Id} not found.");

            existing.DefaultMethod = config.DefaultMethod;
            existing.AutoCalculateOnMovement = config.AutoCalculateOnMovement;
            existing.IncludeTaxInCost = config.IncludeTaxInCost;
            existing.IncludeFreightInCost = config.IncludeFreightInCost;
            existing.StandardCostUpdateFrequencyDays = config.StandardCostUpdateFrequencyDays;
            existing.UpdatedAt = DateTime.UtcNow;

            config = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    /// <inheritdoc />
    public async Task<StockValuationResult> CalculateStockValuationAsync(
        int storeId,
        StockValuationMethod method,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = asOfDate ?? DateTime.UtcNow;

        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.StoreId == storeId && !i.IsDeleted && i.QuantityOnHand > 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new StockValuationResult
        {
            StoreId = storeId,
            ValuationDate = effectiveDate,
            Method = method,
            SkuCount = inventories.Count
        };

        // Pre-fetch all required data to avoid N+1 queries
        var productIds = inventories.Select(i => i.ProductId).ToHashSet();

        // Pre-fetch stock movements for FIFO/WeightedAverage methods (single query instead of N queries)
        Dictionary<int, List<StockMovement>>? stockMovementsByProduct = null;
        if (method is StockValuationMethod.FIFO or StockValuationMethod.WeightedAverage)
        {
            var stockMovements = await _context.StockMovements
                .Where(m => m.StoreId == storeId &&
                           productIds.Contains(m.ProductId) &&
                           m.MovementType == "In" &&
                           !m.IsDeleted)
                .OrderBy(m => m.MovementDate)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            stockMovementsByProduct = stockMovements
                .GroupBy(m => m.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        foreach (var inventory in inventories)
        {
            // Calculate unit cost using pre-fetched data (no additional DB queries)
            var unitCost = CalculateUnitCostFromCache(
                inventory,
                method,
                stockMovementsByProduct?.GetValueOrDefault(inventory.ProductId));

            var totalValue = inventory.QuantityOnHand * unitCost;

            result.Products.Add(new ProductValuation
            {
                ProductId = inventory.ProductId,
                ProductName = inventory.Product?.Name ?? "Unknown",
                ProductCode = inventory.Product?.Code ?? "",
                QuantityOnHand = inventory.QuantityOnHand,
                UnitCost = unitCost,
                TotalValue = totalValue
            });

            result.TotalQuantity += inventory.QuantityOnHand;
            result.TotalValue += totalValue;
        }

        return result;
    }

    /// <summary>
    /// Calculate unit cost using pre-fetched data (no N+1 queries).
    /// </summary>
    private static decimal CalculateUnitCostFromCache(
        Inventory inventory,
        StockValuationMethod method,
        List<StockMovement>? stockMovements)
    {
        return method switch
        {
            StockValuationMethod.StandardCost => inventory.Product?.Cost ?? 0,
            StockValuationMethod.FIFO => CalculateFIFOUnitCost(inventory.QuantityOnHand, stockMovements),
            StockValuationMethod.WeightedAverage => CalculateWeightedAverageUnitCost(stockMovements),
            _ => inventory.Product?.Cost ?? 0
        };
    }

    /// <summary>
    /// Calculate FIFO unit cost from pre-fetched stock movements.
    /// </summary>
    private static decimal CalculateFIFOUnitCost(int quantityOnHand, List<StockMovement>? receipts)
    {
        if (receipts == null || receipts.Count == 0 || quantityOnHand <= 0)
            return 0;

        var remainingQty = (decimal)quantityOnHand;
        decimal totalValue = 0;

        foreach (var receipt in receipts.OrderBy(r => r.MovementDate))
        {
            if (remainingQty <= 0) break;

            var layerQty = Math.Min(remainingQty, receipt.Quantity);
            var layerCost = receipt.UnitCost ?? 0;

            totalValue += layerQty * layerCost;
            remainingQty -= layerQty;
        }

        return totalValue / quantityOnHand;
    }

    /// <summary>
    /// Calculate weighted average unit cost from pre-fetched stock movements.
    /// </summary>
    private static decimal CalculateWeightedAverageUnitCost(List<StockMovement>? receipts)
    {
        if (receipts == null || receipts.Count == 0)
            return 0;

        var totalQuantity = receipts.Sum(r => r.Quantity);
        if (totalQuantity == 0)
            return 0;

        var totalValue = receipts.Sum(r => r.Quantity * (r.UnitCost ?? 0));
        return totalValue / totalQuantity;
    }

    /// <inheritdoc />
    public async Task<StockValuationSnapshot> CreateValuationSnapshotAsync(
        int storeId,
        StockValuationMethod method,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        var valuation = await CalculateStockValuationAsync(storeId, method, null, cancellationToken)
            .ConfigureAwait(false);

        var snapshot = new StockValuationSnapshot
        {
            StoreId = storeId,
            SnapshotDate = DateTime.UtcNow,
            Method = method,
            TotalValue = valuation.TotalValue,
            TotalQuantity = valuation.TotalQuantity,
            SkuCount = valuation.SkuCount,
            Period = period,
            IsPeriodEnd = !string.IsNullOrEmpty(period),
            CreatedAt = DateTime.UtcNow
        };

        _context.StockValuationSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Save details
        foreach (var product in valuation.Products)
        {
            var detail = new StockValuationDetail
            {
                SnapshotId = snapshot.Id,
                ProductId = product.ProductId,
                QuantityOnHand = product.QuantityOnHand,
                UnitCost = product.UnitCost,
                TotalValue = product.TotalValue,
                CreatedAt = DateTime.UtcNow
            };
            _context.StockValuationDetails.Add(detail);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return snapshot;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockValuationSnapshot>> GetValuationSnapshotsAsync(
        int storeId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockValuationSnapshots
            .Where(s => s.StoreId == storeId && !s.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(s => s.SnapshotDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.SnapshotDate <= toDate.Value);

        return await query
            .OrderByDescending(s => s.SnapshotDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockValuationSnapshot?> GetValuationSnapshotDetailAsync(
        int snapshotId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StockValuationSnapshots
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == snapshotId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProductCostResult> CalculateFIFOCostAsync(
        int storeId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        // Get stock movements (receipts) in FIFO order
        var receipts = await _context.StockMovements
            .Where(m => m.StoreId == storeId &&
                       m.ProductId == productId &&
                       m.MovementType == "In" &&
                       !m.IsDeleted)
            .OrderBy(m => m.MovementDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);

        var quantityOnHand = inventory?.QuantityOnHand ?? 0;

        var result = new ProductCostResult
        {
            ProductId = productId,
            Method = StockValuationMethod.FIFO,
            QuantityOnHand = quantityOnHand
        };

        // Build cost layers (FIFO - oldest first)
        var remainingQty = quantityOnHand;
        decimal totalValue = 0;

        foreach (var receipt in receipts.OrderBy(r => r.MovementDate))
        {
            if (remainingQty <= 0) break;

            var layerQty = Math.Min(remainingQty, receipt.Quantity);
            var layerCost = receipt.UnitCost ?? 0;

            result.CostLayers.Add(new CostLayer
            {
                ReceivedDate = receipt.MovementDate,
                Quantity = layerQty,
                UnitCost = layerCost,
                ReferenceNumber = receipt.ReferenceNumber
            });

            totalValue += layerQty * layerCost;
            remainingQty -= layerQty;
        }

        result.TotalValue = totalValue;
        result.UnitCost = quantityOnHand > 0 ? totalValue / quantityOnHand : 0;

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductCostResult> CalculateWeightedAverageCostAsync(
        int storeId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);

        var quantityOnHand = inventory?.QuantityOnHand ?? 0;

        // Calculate weighted average from recent purchases
        var recentReceipts = await _context.StockMovements
            .Where(m => m.StoreId == storeId &&
                       m.ProductId == productId &&
                       m.MovementType == "In" &&
                       m.UnitCost.HasValue &&
                       !m.IsDeleted)
            .OrderByDescending(m => m.MovementDate)
            .Take(10)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal totalCost = 0;
        decimal totalQty = 0;

        foreach (var receipt in recentReceipts)
        {
            totalCost += receipt.Quantity * (receipt.UnitCost ?? 0);
            totalQty += receipt.Quantity;
        }

        var weightedAvgCost = totalQty > 0 ? totalCost / totalQty : (inventory?.Product?.Cost ?? 0);

        return new ProductCostResult
        {
            ProductId = productId,
            Method = StockValuationMethod.WeightedAverage,
            QuantityOnHand = quantityOnHand,
            UnitCost = weightedAvgCost,
            TotalValue = quantityOnHand * weightedAvgCost
        };
    }

    /// <inheritdoc />
    public async Task<StandardCostUpdateResult> UpdateStandardCostsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var result = new StandardCostUpdateResult();

        var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var product in products)
        {
            var waCost = await CalculateWeightedAverageCostAsync(storeId, product.Id, cancellationToken)
                .ConfigureAwait(false);

            if (Math.Abs(waCost.UnitCost - product.Cost) > 0.01m)
            {
                var change = new StandardCostChange
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    OldCost = product.Cost,
                    NewCost = waCost.UnitCost,
                    QuantityOnHand = waCost.QuantityOnHand,
                    ValueChange = (waCost.UnitCost - product.Cost) * waCost.QuantityOnHand
                };

                product.Cost = waCost.UnitCost;
                product.UpdatedAt = DateTime.UtcNow;

                result.Changes.Add(change);
                result.TotalValueChange += change.ValueChange;
                result.ProductsUpdated++;
            }
        }

        // Update config
        var config = await GetStockValuationConfigAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (config != null)
        {
            config.LastStandardCostCalculation = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<ValuationComparisonResult> CompareValuationMethodsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var fifo = await CalculateStockValuationAsync(storeId, StockValuationMethod.FIFO, null, cancellationToken)
            .ConfigureAwait(false);
        var wa = await CalculateStockValuationAsync(storeId, StockValuationMethod.WeightedAverage, null, cancellationToken)
            .ConfigureAwait(false);

        var result = new ValuationComparisonResult
        {
            StoreId = storeId,
            ComparisonDate = DateTime.UtcNow,
            FIFOValue = fifo.TotalValue,
            WeightedAverageValue = wa.TotalValue,
            LIFOValue = fifo.TotalValue, // Simplified - LIFO calculation similar to FIFO but reversed
            FIFOvsLIFODifference = 0,
            FIFOvsWADifference = fifo.TotalValue - wa.TotalValue
        };

        // Build product comparisons
        foreach (var fifoProduct in fifo.Products)
        {
            var waProduct = wa.Products.FirstOrDefault(p => p.ProductId == fifoProduct.ProductId);

            result.ProductComparisons.Add(new ProductValuationComparison
            {
                ProductId = fifoProduct.ProductId,
                ProductName = fifoProduct.ProductName,
                QuantityOnHand = fifoProduct.QuantityOnHand,
                FIFOValue = fifoProduct.TotalValue,
                LIFOValue = fifoProduct.TotalValue,
                WeightedAverageValue = waProduct?.TotalValue ?? 0
            });
        }

        return result;
    }

    #endregion

    #region Automatic Reorder Generation

    /// <inheritdoc />
    public async Task<ReorderRule?> GetReorderRuleAsync(
        int storeId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReorderRules
            .Include(r => r.Product)
            .Include(r => r.PreferredSupplier)
            .FirstOrDefaultAsync(r => r.StoreId == storeId && r.ProductId == productId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReorderRule>> GetReorderRulesAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReorderRules
            .Include(r => r.Product)
            .Include(r => r.PreferredSupplier)
            .Where(r => r.StoreId == storeId && !r.IsDeleted)
            .OrderBy(r => r.Product.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReorderRule> SaveReorderRuleAsync(
        ReorderRule rule,
        CancellationToken cancellationToken = default)
    {
        if (rule.Id == 0)
        {
            rule.CreatedAt = DateTime.UtcNow;
            _context.ReorderRules.Add(rule);
        }
        else
        {
            var existing = await _context.ReorderRules
                .FirstOrDefaultAsync(r => r.Id == rule.Id && !r.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Reorder rule {rule.Id} not found.");

            existing.ReorderPoint = rule.ReorderPoint;
            existing.ReorderQuantity = rule.ReorderQuantity;
            existing.MaxStockLevel = rule.MaxStockLevel;
            existing.SafetyStock = rule.SafetyStock;
            existing.LeadTimeDays = rule.LeadTimeDays;
            existing.PreferredSupplierId = rule.PreferredSupplierId;
            existing.IsAutoReorderEnabled = rule.IsAutoReorderEnabled;
            existing.ConsolidateReorders = rule.ConsolidateReorders;
            existing.MinOrderQuantity = rule.MinOrderQuantity;
            existing.OrderMultiple = rule.OrderMultiple;
            existing.UpdatedAt = DateTime.UtcNow;

            rule = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rule;
    }

    /// <inheritdoc />
    public async Task DeleteReorderRuleAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.ReorderRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (rule == null)
            throw new InvalidOperationException($"Reorder rule {ruleId} not found.");

        rule.IsDeleted = true;
        rule.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateEOQAsync(
        int storeId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        // EOQ = sqrt((2 * D * S) / H)
        // D = Annual demand
        // S = Order cost
        // H = Holding cost per unit per year

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var salesQuantity = await _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Where(ri => ri.ProductId == productId &&
                        ri.Receipt.StoreId == storeId &&
                        ri.Receipt.ReceiptDate >= thirtyDaysAgo)
            .SumAsync(ri => ri.Quantity, cancellationToken)
            .ConfigureAwait(false);

        var annualDemand = salesQuantity * 12; // Extrapolate to annual

        var product = await _context.Products.FindAsync(productId);
        var orderCost = 50m; // Assumed fixed order cost
        var holdingCostRate = 0.25m; // 25% of unit cost per year
        var holdingCost = (product?.Cost ?? 1) * holdingCostRate;

        if (annualDemand == 0 || holdingCost == 0)
            return 1;

        var eoq = (decimal)Math.Sqrt((double)(2 * annualDemand * orderCost / holdingCost));
        return Math.Max(1, Math.Round(eoq, 0));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReorderSuggestion>> GenerateReorderSuggestionsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetReorderRulesAsync(storeId, cancellationToken).ConfigureAwait(false);
        var suggestions = new List<ReorderSuggestion>();

        foreach (var rule in rules.Where(r => r.IsAutoReorderEnabled))
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == rule.ProductId, cancellationToken)
                .ConfigureAwait(false);

            var currentStock = inventory?.QuantityOnHand ?? 0;

            if (currentStock <= rule.ReorderPoint)
            {
                // Check for existing pending suggestion
                var existingSuggestion = await _context.ReorderSuggestions
                    .AnyAsync(s => s.StoreId == storeId &&
                                  s.ProductId == rule.ProductId &&
                                  s.Status == "Pending", cancellationToken)
                    .ConfigureAwait(false);

                if (!existingSuggestion)
                {
                    var orderQty = rule.ReorderQuantity;

                    // Adjust for order multiples
                    if (rule.OrderMultiple.HasValue && rule.OrderMultiple.Value > 0)
                    {
                        orderQty = Math.Ceiling(orderQty / rule.OrderMultiple.Value) * rule.OrderMultiple.Value;
                    }

                    // Ensure minimum order quantity
                    if (rule.MinOrderQuantity.HasValue)
                    {
                        orderQty = Math.Max(orderQty, rule.MinOrderQuantity.Value);
                    }

                    var product = await _context.Products.FindAsync(rule.ProductId);
                    var estimatedCost = orderQty * (product?.Cost ?? 0);

                    // Calculate days until stockout
                    var avgDailySales = rule.AverageDailySales ?? 1;
                    var daysUntilStockout = avgDailySales > 0 ? (int)(currentStock / avgDailySales) : 999;

                    var priority = daysUntilStockout switch
                    {
                        <= 3 => "Critical",
                        <= 7 => "High",
                        <= 14 => "Medium",
                        _ => "Low"
                    };

                    var suggestion = new ReorderSuggestion
                    {
                        StoreId = storeId,
                        ProductId = rule.ProductId,
                        SupplierId = rule.PreferredSupplierId,
                        CurrentStock = currentStock,
                        ReorderPoint = rule.ReorderPoint,
                        SuggestedQuantity = orderQty,
                        EstimatedCost = estimatedCost,
                        Status = "Pending",
                        Priority = priority,
                        DaysUntilStockout = daysUntilStockout,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReorderSuggestions.Add(suggestion);
                    suggestions.Add(suggestion);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return suggestions;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReorderSuggestion>> GetPendingReorderSuggestionsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReorderSuggestions
            .Include(s => s.Product)
            .Include(s => s.Supplier)
            .Where(s => s.StoreId == storeId && s.Status == "Pending" && !s.IsDeleted)
            .OrderBy(s => s.Priority == "Critical" ? 0 : s.Priority == "High" ? 1 : s.Priority == "Medium" ? 2 : 3)
            .ThenBy(s => s.DaysUntilStockout)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReorderSuggestion> ApproveReorderSuggestionAsync(
        int suggestionId,
        int userId,
        decimal? adjustedQuantity = null,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _context.ReorderSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (suggestion == null)
            throw new InvalidOperationException($"Reorder suggestion {suggestionId} not found.");

        suggestion.Status = "Approved";
        suggestion.ApprovedByUserId = userId;
        suggestion.ApprovedAt = DateTime.UtcNow;

        if (adjustedQuantity.HasValue)
        {
            suggestion.SuggestedQuantity = adjustedQuantity.Value;
            var product = await _context.Products.FindAsync(suggestion.ProductId);
            suggestion.EstimatedCost = adjustedQuantity.Value * (product?.Cost ?? 0);
        }

        suggestion.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return suggestion;
    }

    /// <inheritdoc />
    public async Task RejectReorderSuggestionAsync(
        int suggestionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _context.ReorderSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (suggestion == null)
            throw new InvalidOperationException($"Reorder suggestion {suggestionId} not found.");

        suggestion.Status = "Rejected";
        suggestion.Notes = reason;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ConvertSuggestionsResult> ConvertSuggestionsToPurchaseOrdersAsync(
        int storeId,
        IEnumerable<int>? suggestionIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReorderSuggestions
            .Include(s => s.Supplier)
            .Include(s => s.Product)
            .Where(s => s.StoreId == storeId && s.Status == "Approved" && !s.IsDeleted);

        if (suggestionIds != null && suggestionIds.Any())
            query = query.Where(s => suggestionIds.Contains(s.Id));

        var suggestions = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = new ConvertSuggestionsResult
        {
            SuggestionsProcessed = suggestions.Count
        };

        // Group by supplier
        var bySupplier = suggestions.GroupBy(s => s.SupplierId ?? 0);

        foreach (var group in bySupplier)
        {
            var supplierId = group.Key;
            if (supplierId == 0) continue;

            var po = new PurchaseOrder
            {
                StoreId = storeId,
                SupplierId = supplierId,
                OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                OrderDate = DateTime.UtcNow,
                Status = "Draft",
                TotalAmount = group.Sum(s => s.EstimatedCost),
                CreatedAt = DateTime.UtcNow
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var suggestion in group)
            {
                var poItem = new PurchaseOrderItem
                {
                    PurchaseOrderId = po.Id,
                    ProductId = suggestion.ProductId,
                    Quantity = suggestion.SuggestedQuantity,
                    UnitPrice = suggestion.Product?.Cost ?? 0,
                    TotalPrice = suggestion.EstimatedCost,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PurchaseOrderItems.Add(poItem);

                suggestion.Status = "Converted";
                suggestion.PurchaseOrderId = po.Id;
                suggestion.UpdatedAt = DateTime.UtcNow;
            }

            result.PurchaseOrderIds.Add(po.Id);
            result.PurchaseOrdersCreated++;
            result.TotalOrderValue += po.TotalAmount;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task<ReorderPointCalculationResult> CalculateReorderPointsAsync(
        int storeId,
        int lookbackDays = 90,
        CancellationToken cancellationToken = default)
    {
        var result = new ReorderPointCalculationResult();
        var startDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var product in products)
        {
            var salesData = await _context.ReceiptItems
                .Include(ri => ri.Receipt)
                .Where(ri => ri.ProductId == product.Id &&
                            ri.Receipt.StoreId == storeId &&
                            ri.Receipt.ReceiptDate >= startDate)
                .GroupBy(ri => ri.ProductId)
                .Select(g => new
                {
                    TotalQuantity = g.Sum(ri => ri.Quantity),
                    DaysWithSales = g.Select(ri => ri.Receipt.ReceiptDate.Date).Distinct().Count()
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (salesData == null || salesData.DaysWithSales == 0) continue;

            var avgDailySales = salesData.TotalQuantity / lookbackDays;
            var leadTimeDays = 7; // Default lead time
            var safetyDays = 3; // Safety buffer

            var reorderPoint = avgDailySales * (leadTimeDays + safetyDays);
            var safetyStock = avgDailySales * safetyDays;
            var eoq = await CalculateEOQAsync(storeId, product.Id, cancellationToken).ConfigureAwait(false);

            // Update or create rule
            var rule = await GetReorderRuleAsync(storeId, product.Id, cancellationToken).ConfigureAwait(false);
            if (rule == null)
            {
                rule = new ReorderRule
                {
                    StoreId = storeId,
                    ProductId = product.Id,
                    ReorderPoint = reorderPoint,
                    ReorderQuantity = eoq,
                    SafetyStock = safetyStock,
                    LeadTimeDays = leadTimeDays,
                    AverageDailySales = avgDailySales,
                    IsAutoReorderEnabled = true,
                    LastCalculatedDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ReorderRules.Add(rule);
            }
            else
            {
                rule.ReorderPoint = reorderPoint;
                rule.SafetyStock = safetyStock;
                rule.AverageDailySales = avgDailySales;
                rule.EconomicOrderQuantity = eoq;
                rule.LastCalculatedDate = DateTime.UtcNow;
                rule.UpdatedAt = DateTime.UtcNow;
            }

            result.ProductsAnalyzed++;
            result.RulesUpdated++;
            result.Details.Add(new ReorderPointDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                AverageDailySales = avgDailySales,
                LeadTimeDays = leadTimeDays,
                CalculatedReorderPoint = reorderPoint,
                RecommendedSafetyStock = safetyStock,
                RecommendedEOQ = eoq
            });
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    #endregion

    #region Shrinkage Analysis

    /// <inheritdoc />
    public async Task<ShrinkageRecord> RecordShrinkageAsync(
        ShrinkageRecord record,
        CancellationToken cancellationToken = default)
    {
        record.TotalValue = record.Quantity * record.UnitCost;
        record.CreatedAt = DateTime.UtcNow;

        _context.ShrinkageRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return record;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ShrinkageRecord>> GetShrinkageRecordsAsync(
        int storeId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ShrinkageType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShrinkageRecords
            .Include(r => r.Product)
            .Include(r => r.RecordedByUser)
            .Where(r => r.StoreId == storeId && !r.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(r => r.ShrinkageDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ShrinkageDate <= toDate.Value);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        return await query
            .OrderByDescending(r => r.ShrinkageDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ShrinkageSummary> GetShrinkageSummaryAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var records = await GetShrinkageRecordsAsync(storeId, startDate, endDate, null, cancellationToken)
            .ConfigureAwait(false);

        var recordList = records.ToList();

        var totalSales = await _context.Receipts
            .Where(r => r.StoreId == storeId &&
                       r.ReceiptDate >= startDate &&
                       r.ReceiptDate <= endDate)
            .SumAsync(r => r.TotalAmount, cancellationToken)
            .ConfigureAwait(false);

        var summary = new ShrinkageSummary
        {
            StoreId = storeId,
            StartDate = startDate,
            EndDate = endDate,
            TotalShrinkageValue = recordList.Sum(r => r.TotalValue),
            TotalShrinkageQuantity = recordList.Sum(r => r.Quantity),
            IncidentCount = recordList.Count,
            ProductsAffected = recordList.Select(r => r.ProductId).Distinct().Count(),
            ShrinkageRate = totalSales > 0 ? (recordList.Sum(r => r.TotalValue) / totalSales) * 100 : 0
        };

        // Group by type
        summary.ByType = recordList
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalValue));

        return summary;
    }

    /// <inheritdoc />
    public async Task<ShrinkageAnalysisResult> AnalyzeShrinkagePatternsAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetShrinkageSummaryAsync(storeId, startDate, endDate, cancellationToken)
            .ConfigureAwait(false);

        var records = await GetShrinkageRecordsAsync(storeId, startDate, endDate, null, cancellationToken)
            .ConfigureAwait(false);
        var recordList = records.ToList();

        var result = new ShrinkageAnalysisResult
        {
            Summary = summary
        };

        // By department
        result.ByDepartment = recordList
            .Where(r => r.DepartmentId.HasValue)
            .GroupBy(r => r.DepartmentId!.Value)
            .Select(g => new ShrinkageByDepartment
            {
                DepartmentId = g.Key,
                DepartmentName = g.First().Department?.Name ?? "Unknown",
                TotalValue = g.Sum(r => r.TotalValue),
                IncidentCount = g.Count()
            })
            .OrderByDescending(d => d.TotalValue)
            .ToList();

        // By category
        result.ByCategory = recordList
            .GroupBy(r => r.Product?.CategoryId ?? 0)
            .Select(g => new ShrinkageByCategory
            {
                CategoryId = g.Key,
                CategoryName = g.First().Product?.Category?.Name ?? "Unknown",
                TotalValue = g.Sum(r => r.TotalValue),
                IncidentCount = g.Count()
            })
            .OrderByDescending(c => c.TotalValue)
            .ToList();

        // Generate insights
        if (summary.ShrinkageRate > 2)
            result.Insights.Add($"Shrinkage rate of {summary.ShrinkageRate:N2}% is above industry average of 2%");

        var topType = summary.ByType.OrderByDescending(t => t.Value).FirstOrDefault();
        if (topType.Value > 0)
            result.Insights.Add($"Primary shrinkage type is {topType.Key} ({(topType.Value / summary.TotalShrinkageValue) * 100:N1}% of total)");

        // Generate recommendations
        if (summary.ByType.ContainsKey(ShrinkageType.Theft) && summary.ByType[ShrinkageType.Theft] > summary.TotalShrinkageValue * 0.3m)
            result.Recommendations.Add("Consider enhanced security measures due to high theft-related shrinkage");

        if (summary.ByType.ContainsKey(ShrinkageType.Expiry) && summary.ByType[ShrinkageType.Expiry] > summary.TotalShrinkageValue * 0.2m)
            result.Recommendations.Add("Review inventory management to reduce expiry-related shrinkage");

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductShrinkageSummary>> GetTopShrinkageProductsAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        int topN = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShrinkageRecords
            .Include(r => r.Product)
            .Where(r => r.StoreId == storeId &&
                       r.ShrinkageDate >= startDate &&
                       r.ShrinkageDate <= endDate &&
                       !r.IsDeleted)
            .GroupBy(r => r.ProductId)
            .Select(g => new ProductShrinkageSummary
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                ProductCode = g.First().Product.Code,
                TotalQuantityLost = g.Sum(r => r.Quantity),
                TotalValueLost = g.Sum(r => r.TotalValue),
                IncidentCount = g.Count(),
                PrimaryType = g.GroupBy(r => r.Type).OrderByDescending(t => t.Count()).First().Key
            })
            .OrderByDescending(s => s.TotalValueLost)
            .Take(topN)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ShrinkageAnalysisPeriod> SaveShrinkageAnalysisPeriodAsync(
        ShrinkageAnalysisPeriod analysis,
        CancellationToken cancellationToken = default)
    {
        analysis.CreatedAt = DateTime.UtcNow;
        _context.ShrinkageAnalysisPeriods.Add(analysis);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return analysis;
    }

    /// <inheritdoc />
    public async Task<ShrinkageTrendResult> GetShrinkageTrendAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        string groupBy = "month",
        CancellationToken cancellationToken = default)
    {
        var records = await GetShrinkageRecordsAsync(storeId, startDate, endDate, null, cancellationToken)
            .ConfigureAwait(false);

        var result = new ShrinkageTrendResult();

        var grouped = groupBy.ToLower() switch
        {
            "week" => records.GroupBy(r => new { r.ShrinkageDate.Year, Week = (r.ShrinkageDate.DayOfYear / 7) + 1 })
                            .Select(g => new { Period = $"{g.Key.Year}-W{g.Key.Week:D2}", Start = g.Min(r => r.ShrinkageDate), Records = g }),
            "month" => records.GroupBy(r => new { r.ShrinkageDate.Year, r.ShrinkageDate.Month })
                             .Select(g => new { Period = $"{g.Key.Year}-{g.Key.Month:D2}", Start = new DateTime(g.Key.Year, g.Key.Month, 1), Records = g }),
            _ => records.GroupBy(r => r.ShrinkageDate.Date)
                       .Select(g => new { Period = g.Key.ToString("yyyy-MM-dd"), Start = g.Key, Records = g })
        };

        foreach (var group in grouped.OrderBy(g => g.Start))
        {
            result.TrendPoints.Add(new ShrinkageTrendPoint
            {
                Period = group.Period,
                PeriodStart = group.Start,
                TotalValue = group.Records.Sum(r => r.TotalValue),
                IncidentCount = group.Records.Count()
            });
        }

        // Calculate overall trend
        if (result.TrendPoints.Count >= 2)
        {
            var firstHalf = result.TrendPoints.Take(result.TrendPoints.Count / 2).Average(p => p.TotalValue);
            var secondHalf = result.TrendPoints.Skip(result.TrendPoints.Count / 2).Average(p => p.TotalValue);

            result.OverallTrend = firstHalf > 0 ? ((secondHalf - firstHalf) / firstHalf) * 100 : 0;
            result.TrendDescription = result.OverallTrend switch
            {
                > 10 => "Significantly Increasing",
                > 0 => "Slightly Increasing",
                < -10 => "Significantly Decreasing",
                < 0 => "Slightly Decreasing",
                _ => "Stable"
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> ImportShrinkageFromStockTakeAsync(
        int stockTakeId,
        ShrinkageType defaultType,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var stockTake = await _context.StockTakes
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, cancellationToken)
            .ConfigureAwait(false);

        if (stockTake == null)
            throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        int count = 0;
        foreach (var item in stockTake.Items.Where(i => i.Variance < 0))
        {
            var shrinkage = new ShrinkageRecord
            {
                StoreId = stockTake.StoreId,
                ProductId = item.ProductId,
                ShrinkageDate = stockTake.StartDate,
                Quantity = Math.Abs(item.Variance),
                UnitCost = item.Product?.Cost ?? 0,
                Type = defaultType,
                SourceReference = $"StockTake-{stockTakeId}",
                SourceType = "StockTake",
                RecordedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            shrinkage.TotalValue = shrinkage.Quantity * shrinkage.UnitCost;

            _context.ShrinkageRecords.Add(shrinkage);
            count++;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    #endregion

    #region Dead Stock Identification

    /// <inheritdoc />
    public async Task<DeadStockConfig?> GetDeadStockConfigAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DeadStockConfigs
            .FirstOrDefaultAsync(c => c.StoreId == storeId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeadStockConfig> SaveDeadStockConfigAsync(
        DeadStockConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.DeadStockConfigs.Add(config);
        }
        else
        {
            var existing = await _context.DeadStockConfigs
                .FirstOrDefaultAsync(c => c.Id == config.Id && !c.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Dead stock config {config.Id} not found.");

            existing.SlowMovingDays = config.SlowMovingDays;
            existing.NonMovingDays = config.NonMovingDays;
            existing.DeadStockDays = config.DeadStockDays;
            existing.MinStockValue = config.MinStockValue;
            existing.ExcludedCategoryIds = config.ExcludedCategoryIds;
            existing.ExcludedProductIds = config.ExcludedProductIds;
            existing.AnalysisFrequencyDays = config.AnalysisFrequencyDays;
            existing.DefaultClearanceDiscountPercent = config.DefaultClearanceDiscountPercent;
            existing.SendAlerts = config.SendAlerts;
            existing.UpdatedAt = DateTime.UtcNow;

            config = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadStockItem>> IdentifyDeadStockAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var config = await GetDeadStockConfigAsync(storeId, cancellationToken).ConfigureAwait(false);
        var slowMovingDays = config?.SlowMovingDays ?? 90;
        var nonMovingDays = config?.NonMovingDays ?? 180;
        var deadStockDays = config?.DeadStockDays ?? 365;

        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.StoreId == storeId && i.QuantityOnHand > 0 && !i.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var deadStockItems = new List<DeadStockItem>();

        foreach (var inventory in inventories)
        {
            // Get last sale date
            var lastSale = await _context.ReceiptItems
                .Include(ri => ri.Receipt)
                .Where(ri => ri.ProductId == inventory.ProductId && ri.Receipt.StoreId == storeId)
                .OrderByDescending(ri => ri.Receipt.ReceiptDate)
                .Select(ri => ri.Receipt.ReceiptDate)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var daysSinceLastSale = lastSale != default
                ? (int)(DateTime.UtcNow - lastSale).TotalDays
                : 9999;

            if (daysSinceLastSale >= slowMovingDays)
            {
                var classification = daysSinceLastSale switch
                {
                    >= 365 => DeadStockClassification.DeadStock,
                    >= 180 => DeadStockClassification.NonMoving,
                    _ => DeadStockClassification.SlowMoving
                };

                var stockValue = inventory.QuantityOnHand * (inventory.Product?.Cost ?? 0);

                // Check if already identified
                var existing = await _context.DeadStockItems
                    .FirstOrDefaultAsync(d => d.StoreId == storeId && d.ProductId == inventory.ProductId && d.Status == "Identified", cancellationToken)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    var deadStockItem = new DeadStockItem
                    {
                        StoreId = storeId,
                        ProductId = inventory.ProductId,
                        Classification = classification,
                        DaysSinceLastSale = daysSinceLastSale,
                        LastSaleDate = lastSale != default ? lastSale : null,
                        QuantityOnHand = inventory.QuantityOnHand,
                        StockValue = stockValue,
                        PotentialLoss = stockValue,
                        RecommendedAction = classification switch
                        {
                            DeadStockClassification.DeadStock => "Write-off",
                            DeadStockClassification.NonMoving => "Clearance",
                            _ => "Monitor"
                        },
                        SuggestedClearancePrice = inventory.Product?.Price * (1 - (config?.DefaultClearanceDiscountPercent ?? 50) / 100),
                        Status = "Identified",
                        IdentifiedDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.DeadStockItems.Add(deadStockItem);
                    deadStockItems.Add(deadStockItem);
                }
            }
        }

        if (config != null)
        {
            config.LastAnalysisDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return deadStockItems;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeadStockItem>> GetDeadStockItemsAsync(
        int storeId,
        DeadStockClassification? classification = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DeadStockItems
            .Include(d => d.Product)
            .Where(d => d.StoreId == storeId && !d.IsDeleted);

        if (classification.HasValue)
            query = query.Where(d => d.Classification == classification.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        return await query
            .OrderByDescending(d => d.StockValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeadStockItem> UpdateDeadStockItemAsync(
        int itemId,
        string status,
        string? actionTaken = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.DeadStockItems
            .FirstOrDefaultAsync(d => d.Id == itemId && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"Dead stock item {itemId} not found.");

        item.Status = status;
        item.ActionTaken = actionTaken;
        item.ActionTakenDate = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return item;
    }

    /// <inheritdoc />
    public async Task<DeadStockSummary> GetDeadStockSummaryAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var items = await GetDeadStockItemsAsync(storeId, null, "Identified", cancellationToken)
            .ConfigureAwait(false);
        var itemList = items.ToList();

        var totalInventoryValue = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.StoreId == storeId && !i.IsDeleted)
            .SumAsync(i => i.QuantityOnHand * (i.Product!.Cost), cancellationToken)
            .ConfigureAwait(false);

        var summary = new DeadStockSummary
        {
            StoreId = storeId,
            TotalDeadStockItems = itemList.Count,
            TotalDeadStockValue = itemList.Sum(i => i.StockValue),
            TotalPotentialLoss = itemList.Sum(i => i.PotentialLoss),
            PercentOfTotalInventory = totalInventoryValue > 0 ? (itemList.Sum(i => i.StockValue) / totalInventoryValue) * 100 : 0
        };

        summary.ByClassification = itemList
            .GroupBy(i => i.Classification)
            .ToDictionary(g => g.Key, g => g.Count());

        summary.ValueByClassification = itemList
            .GroupBy(i => i.Classification)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.StockValue));

        return summary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ClearancePriceSuggestion>> CalculateClearancePricesAsync(
        int storeId,
        decimal minMarginPercent = 0,
        CancellationToken cancellationToken = default)
    {
        var deadStock = await GetDeadStockItemsAsync(storeId, null, "Identified", cancellationToken)
            .ConfigureAwait(false);

        var config = await GetDeadStockConfigAsync(storeId, cancellationToken).ConfigureAwait(false);
        var discountPercent = config?.DefaultClearanceDiscountPercent ?? 50;

        var suggestions = new List<ClearancePriceSuggestion>();

        foreach (var item in deadStock)
        {
            var product = item.Product;
            if (product == null) continue;

            var suggestedPrice = product.Price * (1 - discountPercent / 100);

            // Ensure minimum margin if specified
            if (minMarginPercent > 0)
            {
                var minPrice = product.Cost * (1 + minMarginPercent / 100);
                suggestedPrice = Math.Max(suggestedPrice, minPrice);
            }

            suggestions.Add(new ClearancePriceSuggestion
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                CurrentPrice = product.Price,
                Cost = product.Cost,
                SuggestedClearancePrice = suggestedPrice,
                DiscountPercent = ((product.Price - suggestedPrice) / product.Price) * 100,
                QuantityOnHand = item.QuantityOnHand,
                PotentialRecovery = suggestedPrice * item.QuantityOnHand,
                DaysSinceLastSale = item.DaysSinceLastSale
            });
        }

        return suggestions.OrderByDescending(s => s.PotentialRecovery);
    }

    /// <inheritdoc />
    public async Task<int> CreateClearancePromotionAsync(
        int storeId,
        IEnumerable<int> deadStockItemIds,
        decimal discountPercent,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var promotion = new CentralPromotion
        {
            Name = $"Clearance Sale - {DateTime.UtcNow:yyyyMMdd}",
            Description = "Dead stock clearance promotion",
            DiscountType = "Percentage",
            DiscountValue = discountPercent,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.CentralPromotions.Add(promotion);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Add products to promotion
        foreach (var itemId in deadStockItemIds)
        {
            var deadStockItem = await _context.DeadStockItems.FindAsync(itemId);
            if (deadStockItem != null)
            {
                var promotionProduct = new PromotionProduct
                {
                    PromotionId = promotion.Id,
                    ProductId = deadStockItem.ProductId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PromotionProducts.Add(promotionProduct);

                // Update dead stock status
                deadStockItem.Status = "UnderReview";
                deadStockItem.ActionTaken = $"Added to clearance promotion {promotion.Id}";
                deadStockItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return promotion.Id;
    }

    /// <inheritdoc />
    public async Task<InventoryTurnoverAnalysis> CalculateInventoryTurnoverAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        int? productId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        // Calculate COGS
        var cogsQuery = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.StoreId == storeId &&
                        ri.Receipt.ReceiptDate >= startDate &&
                        ri.Receipt.ReceiptDate <= endDate);

        if (productId.HasValue)
            cogsQuery = cogsQuery.Where(ri => ri.ProductId == productId.Value);

        if (categoryId.HasValue)
            cogsQuery = cogsQuery.Where(ri => ri.Product!.CategoryId == categoryId.Value);

        var cogs = await cogsQuery
            .SumAsync(ri => ri.Quantity * (ri.Product!.Cost), cancellationToken)
            .ConfigureAwait(false);

        // Calculate average inventory
        var inventoryQuery = _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.StoreId == storeId && !i.IsDeleted);

        if (productId.HasValue)
            inventoryQuery = inventoryQuery.Where(i => i.ProductId == productId.Value);

        if (categoryId.HasValue)
            inventoryQuery = inventoryQuery.Where(i => i.Product!.CategoryId == categoryId.Value);

        var avgInventory = await inventoryQuery
            .SumAsync(i => i.QuantityOnHand * i.Product!.Cost, cancellationToken)
            .ConfigureAwait(false);

        var periodDays = (endDate - startDate).Days;
        var turnoverRatio = avgInventory > 0 ? cogs / avgInventory : 0;
        var dsi = turnoverRatio > 0 ? periodDays / turnoverRatio : 0;

        var analysis = new InventoryTurnoverAnalysis
        {
            StoreId = storeId,
            ProductId = productId,
            CategoryId = categoryId,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            COGS = cogs,
            AverageInventoryValue = avgInventory,
            TurnoverRatio = turnoverRatio,
            DaysSalesOfInventory = dsi,
            PerformanceRating = turnoverRatio switch
            {
                >= 6 => "Excellent",
                >= 4 => "Good",
                >= 2 => "Average",
                _ => "Poor"
            },
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryTurnoverAnalyses.Add(analysis);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return analysis;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategoryTurnoverSummary>> GetInventoryTurnoverByCategoryAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var categories = await _context.Categories
            .Where(c => !c.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = new List<CategoryTurnoverSummary>();

        foreach (var category in categories)
        {
            var analysis = await CalculateInventoryTurnoverAsync(storeId, startDate, endDate, null, category.Id, cancellationToken)
                .ConfigureAwait(false);

            summaries.Add(new CategoryTurnoverSummary
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                COGS = analysis.COGS,
                AverageInventory = analysis.AverageInventoryValue,
                TurnoverRatio = analysis.TurnoverRatio,
                DaysSalesOfInventory = analysis.DaysSalesOfInventory,
                PerformanceRating = analysis.PerformanceRating ?? ""
            });
        }

        return summaries.OrderByDescending(s => s.TurnoverRatio);
    }

    /// <inheritdoc />
    public async Task<ABCAnalysisResult> GetABCAnalysisAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var sales = await _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.StoreId == storeId &&
                        ri.Receipt.ReceiptDate >= startDate &&
                        ri.Receipt.ReceiptDate <= endDate)
            .GroupBy(ri => ri.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product!.Name,
                ProductCode = g.First().Product!.Code,
                SalesValue = g.Sum(ri => ri.Quantity * ri.UnitPrice)
            })
            .OrderByDescending(s => s.SalesValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalSales = sales.Sum(s => s.SalesValue);
        var result = new ABCAnalysisResult
        {
            StoreId = storeId,
            AnalysisDate = DateTime.UtcNow
        };

        decimal cumulativePercent = 0;
        foreach (var item in sales)
        {
            var percentOfTotal = totalSales > 0 ? (item.SalesValue / totalSales) * 100 : 0;
            cumulativePercent += percentOfTotal;

            var classification = cumulativePercent <= 80 ? "A" : cumulativePercent <= 95 ? "B" : "C";

            var abcItem = new ABCClassificationItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductCode = item.ProductCode,
                SalesValue = item.SalesValue,
                CumulativePercentage = cumulativePercent,
                Classification = classification
            };

            switch (classification)
            {
                case "A":
                    result.AItems.Add(abcItem);
                    break;
                case "B":
                    result.BItems.Add(abcItem);
                    break;
                case "C":
                    result.CItems.Add(abcItem);
                    break;
            }
        }

        // Calculate summaries
        var totalItems = sales.Count;
        result.ASummary = new ABCClassSummary
        {
            Classification = "A",
            ItemCount = result.AItems.Count,
            PercentOfItems = totalItems > 0 ? (decimal)result.AItems.Count / totalItems * 100 : 0,
            SalesValue = result.AItems.Sum(i => i.SalesValue),
            PercentOfSales = totalSales > 0 ? result.AItems.Sum(i => i.SalesValue) / totalSales * 100 : 0
        };

        result.BSummary = new ABCClassSummary
        {
            Classification = "B",
            ItemCount = result.BItems.Count,
            PercentOfItems = totalItems > 0 ? (decimal)result.BItems.Count / totalItems * 100 : 0,
            SalesValue = result.BItems.Sum(i => i.SalesValue),
            PercentOfSales = totalSales > 0 ? result.BItems.Sum(i => i.SalesValue) / totalSales * 100 : 0
        };

        result.CSummary = new ABCClassSummary
        {
            Classification = "C",
            ItemCount = result.CItems.Count,
            PercentOfItems = totalItems > 0 ? (decimal)result.CItems.Count / totalItems * 100 : 0,
            SalesValue = result.CItems.Sum(i => i.SalesValue),
            PercentOfSales = totalSales > 0 ? result.CItems.Sum(i => i.SalesValue) / totalSales * 100 : 0
        };

        return result;
    }

    #endregion

    #region Private Methods

    private async Task<decimal> CalculateUnitCostAsync(
        int storeId,
        int productId,
        StockValuationMethod method,
        CancellationToken cancellationToken)
    {
        return method switch
        {
            StockValuationMethod.FIFO => (await CalculateFIFOCostAsync(storeId, productId, cancellationToken).ConfigureAwait(false)).UnitCost,
            StockValuationMethod.WeightedAverage => (await CalculateWeightedAverageCostAsync(storeId, productId, cancellationToken).ConfigureAwait(false)).UnitCost,
            StockValuationMethod.StandardCost => (await _context.Products.FindAsync(productId))?.Cost ?? 0,
            _ => (await _context.Products.FindAsync(productId))?.Cost ?? 0
        };
    }

    #endregion
}
