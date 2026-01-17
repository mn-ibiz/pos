using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using System.Text;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for waste reporting and analysis.
/// </summary>
public class WasteReportService : IWasteReportService
{
    private readonly IRepository<BatchDisposal> _disposalRepository;
    private readonly IRepository<ProductBatch> _batchRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Store> _storeRepository;

    public WasteReportService(
        IRepository<BatchDisposal> disposalRepository,
        IRepository<ProductBatch> batchRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Store> storeRepository)
    {
        _disposalRepository = disposalRepository ?? throw new ArgumentNullException(nameof(disposalRepository));
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    #region Waste Summary Reports

    /// <inheritdoc />
    public async Task<WasteSummaryReportDto> GetWasteSummaryAsync(WasteReportQueryDto query)
    {
        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var products = await GetProductsForBatchesAsync(batches);
        var categories = await GetCategoriesForProductsAsync(products);
        var suppliers = await GetSuppliersForBatchesAsync(batches);

        var result = new WasteSummaryReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            StoreId = query.StoreId,
            GeneratedAt = DateTime.UtcNow
        };

        // Calculate totals
        result.TotalWasteValue = disposals.Sum(d => d.TotalValue);
        result.TotalWasteQuantity = disposals.Sum(d => d.Quantity);
        result.TotalWasteRecords = disposals.Count;
        result.UniqueProductsAffected = disposals.Select(d => batches.FirstOrDefault(b => b.Id == d.BatchId)?.ProductId).Distinct().Count();

        // Waste by category
        result.WasteByCategory = CalculateWasteByCategory(disposals, batches, products, categories);

        // Waste by supplier
        result.WasteBySupplier = CalculateWasteBySupplier(disposals, batches, suppliers);

        // Waste by reason
        result.WasteByReason = CalculateWasteByReason(disposals);

        // Top wasted products
        result.TopWastedProducts = CalculateTopWastedProducts(disposals, batches, products, 10);

        return result;
    }

    /// <inheritdoc />
    public async Task<List<WasteByCategoryDto>> GetWasteByCategoryAsync(int? storeId, DateTime fromDate, DateTime toDate)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var products = await GetProductsForBatchesAsync(batches);
        var categories = await GetCategoriesForProductsAsync(products);

        return CalculateWasteByCategory(disposals, batches, products, categories);
    }

    /// <inheritdoc />
    public async Task<List<WasteBySupplierDto>> GetWasteBySupplierAsync(int? storeId, DateTime fromDate, DateTime toDate)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var suppliers = await GetSuppliersForBatchesAsync(batches);

        return CalculateWasteBySupplier(disposals, batches, suppliers);
    }

    /// <inheritdoc />
    public async Task<List<WasteByReasonDto>> GetWasteByReasonAsync(int? storeId, DateTime fromDate, DateTime toDate)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);
        return CalculateWasteByReason(disposals);
    }

    /// <inheritdoc />
    public async Task<List<WasteByProductDto>> GetWasteByProductAsync(int? storeId, DateTime fromDate, DateTime toDate, int topCount = 20)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var products = await GetProductsForBatchesAsync(batches);

        return CalculateTopWastedProducts(disposals, batches, products, topCount);
    }

    #endregion

    #region Trend Analysis

    /// <inheritdoc />
    public async Task<List<WasteTrendDataDto>> GetWasteTrendsAsync(int? storeId, DateTime fromDate, DateTime toDate, string groupBy = "day")
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);

        var groupedData = groupBy.ToLower() switch
        {
            "week" => disposals.GroupBy(d => GetStartOfWeek(d.DisposedAt)),
            "month" => disposals.GroupBy(d => new DateTime(d.DisposedAt.Year, d.DisposedAt.Month, 1)),
            _ => disposals.GroupBy(d => d.DisposedAt.Date)
        };

        var result = groupedData
            .Select(g => new WasteTrendDataDto
            {
                Date = g.Key,
                Period = GetPeriodLabel(g.Key, groupBy),
                WasteValue = g.Sum(d => d.TotalValue),
                WasteQuantity = g.Sum(d => d.Quantity),
                WasteRecordCount = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Calculate period-over-period changes
        for (int i = 1; i < result.Count; i++)
        {
            var previousValue = result[i - 1].WasteValue;
            if (previousValue > 0)
            {
                result[i].ChangePercent = ((result[i].WasteValue - previousValue) / previousValue) * 100;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<WasteComparisonDto> GetWasteComparisonAsync(
        int? storeId,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        DateTime previousPeriodStart,
        DateTime previousPeriodEnd)
    {
        var currentQuery = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = currentPeriodStart,
            ToDate = currentPeriodEnd
        };

        var previousQuery = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = previousPeriodStart,
            ToDate = previousPeriodEnd
        };

        var currentDisposals = await GetDisposalsForQueryAsync(currentQuery);
        var previousDisposals = await GetDisposalsForQueryAsync(previousQuery);

        var currentValue = currentDisposals.Sum(d => d.TotalValue);
        var previousValue = previousDisposals.Sum(d => d.TotalValue);
        var currentQuantity = currentDisposals.Sum(d => d.Quantity);
        var previousQuantity = previousDisposals.Sum(d => d.Quantity);

        return new WasteComparisonDto
        {
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            PreviousPeriodStart = previousPeriodStart,
            PreviousPeriodEnd = previousPeriodEnd,
            CurrentPeriodValue = currentValue,
            PreviousPeriodValue = previousValue,
            CurrentPeriodQuantity = currentQuantity,
            PreviousPeriodQuantity = previousQuantity,
            ValueChangePercent = previousValue > 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0,
            QuantityChangePercent = previousQuantity > 0 ? ((decimal)(currentQuantity - previousQuantity) / previousQuantity) * 100 : 0,
            CurrentRecordCount = currentDisposals.Count,
            PreviousRecordCount = previousDisposals.Count
        };
    }

    /// <inheritdoc />
    public async Task<WasteAnalysisDto> GetWasteAnalysisAsync(int? storeId, DateTime fromDate, DateTime toDate)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var products = await GetProductsForBatchesAsync(batches);
        var categories = await GetCategoriesForProductsAsync(products);
        var suppliers = await GetSuppliersForBatchesAsync(batches);

        var result = new WasteAnalysisDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalWasteValue = disposals.Sum(d => d.TotalValue),
            TotalWasteQuantity = disposals.Sum(d => d.Quantity),
            AverageWastePerDay = 0,
            WasteByCategory = CalculateWasteByCategory(disposals, batches, products, categories),
            WasteByReason = CalculateWasteByReason(disposals),
            WasteBySupplier = CalculateWasteBySupplier(disposals, batches, suppliers),
            TopWastedProducts = CalculateTopWastedProducts(disposals, batches, products, 5),
            Insights = new List<WasteInsightDto>()
        };

        // Calculate average waste per day
        var totalDays = (toDate - fromDate).TotalDays;
        if (totalDays > 0)
        {
            result.AverageWastePerDay = result.TotalWasteValue / (decimal)totalDays;
        }

        // Generate insights
        result.Insights = GenerateWasteInsights(result);

        return result;
    }

    #endregion

    #region Dashboard

    /// <inheritdoc />
    public async Task<WasteDashboardDto> GetWasteDashboardAsync(int? storeId)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = GetStartOfWeek(now);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        var result = new WasteDashboardDto
        {
            GeneratedAt = now
        };

        // Get period summaries in parallel
        var todayTask = GetPeriodSummaryAsync(storeId, (int)(now - todayStart).TotalDays + 1);
        var weekTask = GetPeriodSummaryForDatesAsync(storeId, weekStart, now);
        var monthTask = GetPeriodSummaryForDatesAsync(storeId, monthStart, now);
        var yearTask = GetPeriodSummaryForDatesAsync(storeId, yearStart, now);

        await Task.WhenAll(todayTask, weekTask, monthTask, yearTask);

        result.Today = await todayTask;
        result.ThisWeek = await weekTask;
        result.ThisMonth = await monthTask;
        result.ThisYear = await yearTask;

        // Get top waste reasons for the month
        result.TopReasons = await GetWasteByReasonAsync(storeId, monthStart, now);

        // Get top wasted products for the month
        result.TopProducts = await GetWasteByProductAsync(storeId, monthStart, now, 5);

        // Get recent trends (last 7 days)
        result.RecentTrends = await GetWasteTrendsAsync(storeId, now.AddDays(-7), now, "day");

        return result;
    }

    /// <inheritdoc />
    public async Task<WastePeriodSummaryDto> GetPeriodSummaryAsync(int? storeId, int periodDays = 30)
    {
        var toDate = DateTime.UtcNow;
        var fromDate = toDate.AddDays(-periodDays);

        return await GetPeriodSummaryForDatesAsync(storeId, fromDate, toDate);
    }

    private async Task<WastePeriodSummaryDto> GetPeriodSummaryForDatesAsync(int? storeId, DateTime fromDate, DateTime toDate)
    {
        var query = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var disposals = await GetDisposalsForQueryAsync(query);

        // Get previous period for comparison
        var periodLength = toDate - fromDate;
        var previousFromDate = fromDate - periodLength;
        var previousToDate = fromDate;

        var previousQuery = new WasteReportQueryDto
        {
            StoreId = storeId,
            FromDate = previousFromDate,
            ToDate = previousToDate
        };

        var previousDisposals = await GetDisposalsForQueryAsync(previousQuery);

        var currentValue = disposals.Sum(d => d.TotalValue);
        var previousValue = previousDisposals.Sum(d => d.TotalValue);

        return new WastePeriodSummaryDto
        {
            Value = currentValue,
            Quantity = disposals.Sum(d => d.Quantity),
            RecordCount = disposals.Count,
            ChangePercent = previousValue > 0 ? ((currentValue - previousValue) / previousValue) * 100 : null
        };
    }

    #endregion

    #region Export

    /// <inheritdoc />
    public async Task<WasteExportDto> ExportWasteDataAsync(WasteReportQueryDto query)
    {
        var disposals = await GetDisposalsForQueryAsync(query);
        var batches = await GetBatchesForDisposalsAsync(disposals);
        var products = await GetProductsForBatchesAsync(batches);
        var categories = await GetCategoriesForProductsAsync(products);
        var suppliers = await GetSuppliersForBatchesAsync(batches);
        var stores = await _storeRepository.GetAllAsync();

        var records = new List<WasteExportRecordDto>();

        foreach (var disposal in disposals)
        {
            var batch = batches.FirstOrDefault(b => b.Id == disposal.BatchId);
            var product = batch != null ? products.FirstOrDefault(p => p.Id == batch.ProductId) : null;
            var category = product?.CategoryId != null
                ? categories.FirstOrDefault(c => c.Id == product.CategoryId)
                : null;
            var supplier = batch?.SupplierId != null
                ? suppliers.FirstOrDefault(s => s.Id == batch.SupplierId)
                : null;
            var store = stores.FirstOrDefault(s => s.Id == disposal.StoreId);

            records.Add(new WasteExportRecordDto
            {
                DisposalId = disposal.Id,
                DisposalDate = disposal.DisposedAt,
                StoreName = store?.Name ?? "Unknown",
                ProductCode = product?.Code ?? "Unknown",
                ProductName = product?.Name ?? "Unknown",
                CategoryName = category?.Name ?? "Unknown",
                SupplierName = supplier?.Name ?? "Unknown",
                BatchNumber = batch?.BatchNumber ?? "Unknown",
                ExpiryDate = batch?.ExpiryDate,
                Quantity = disposal.Quantity,
                UnitCost = disposal.UnitCost,
                TotalValue = disposal.TotalValue,
                Reason = disposal.Reason.ToString(),
                Description = disposal.Description,
                IsWitnessed = disposal.IsWitnessed,
                WitnessName = disposal.WitnessName
            });
        }

        return new WasteExportDto
        {
            ExportedAt = DateTime.UtcNow,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            TotalRecords = records.Count,
            TotalValue = records.Sum(r => r.TotalValue),
            TotalQuantity = records.Sum(r => r.Quantity),
            Records = records
        };
    }

    /// <inheritdoc />
    public async Task<string> ExportWasteDataAsCsvAsync(WasteReportQueryDto query)
    {
        var exportData = await ExportWasteDataAsync(query);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Disposal ID,Disposal Date,Store,Product Code,Product Name,Category,Supplier,Batch Number,Expiry Date,Quantity,Unit Cost,Total Value,Reason,Description,Witnessed,Witness Name");

        // Data rows
        foreach (var record in exportData.Records)
        {
            sb.AppendLine($"{record.DisposalId},{record.DisposalDate:yyyy-MM-dd HH:mm},{EscapeCsv(record.StoreName)},{EscapeCsv(record.ProductCode)},{EscapeCsv(record.ProductName)},{EscapeCsv(record.CategoryName)},{EscapeCsv(record.SupplierName)},{EscapeCsv(record.BatchNumber)},{record.ExpiryDate:yyyy-MM-dd},{record.Quantity},{record.UnitCost:F2},{record.TotalValue:F2},{record.Reason},{EscapeCsv(record.Description ?? "")},{record.IsWitnessed},{EscapeCsv(record.WitnessName ?? "")}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    #endregion

    #region Store Comparison

    /// <inheritdoc />
    public async Task<List<WasteByStoreDto>> GetWasteByStoreAsync(DateTime fromDate, DateTime toDate)
    {
        var stores = (await _storeRepository.GetAllAsync()).Where(s => s.IsActive).ToList();
        var result = new List<WasteByStoreDto>();

        foreach (var store in stores)
        {
            var query = new WasteReportQueryDto
            {
                StoreId = store.Id,
                FromDate = fromDate,
                ToDate = toDate
            };

            var disposals = await GetDisposalsForQueryAsync(query);
            var batches = await GetBatchesForDisposalsAsync(disposals);
            var products = await GetProductsForBatchesAsync(batches);
            var categories = await GetCategoriesForProductsAsync(products);

            var wasteByReason = CalculateWasteByReason(disposals);
            var wasteByCategory = CalculateWasteByCategory(disposals, batches, products, categories);

            var totalDays = (toDate - fromDate).TotalDays;
            var wasteValue = disposals.Sum(d => d.TotalValue);

            result.Add(new WasteByStoreDto
            {
                StoreId = store.Id,
                StoreName = store.Name,
                StoreCode = store.Code ?? "",
                WasteValue = wasteValue,
                WasteQuantity = disposals.Sum(d => d.Quantity),
                WasteRecordCount = disposals.Count,
                WastePercentageOfRevenue = 0, // Would need revenue data
                AverageWastePerDay = totalDays > 0 ? wasteValue / (decimal)totalDays : 0,
                TopWasteReason = wasteByReason.OrderByDescending(r => r.WasteValue).FirstOrDefault()?.Reason ?? "N/A",
                TopWasteCategory = wasteByCategory.OrderByDescending(c => c.WasteValue).FirstOrDefault()?.CategoryName ?? "N/A"
            });
        }

        return result.OrderByDescending(s => s.WasteValue).ToList();
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<BatchDisposal>> GetDisposalsForQueryAsync(WasteReportQueryDto query)
    {
        var disposals = await _disposalRepository.GetAllAsync();

        var filtered = disposals.Where(d => d.IsActive);

        if (query.StoreId.HasValue)
        {
            filtered = filtered.Where(d => d.StoreId == query.StoreId.Value);
        }

        filtered = filtered.Where(d => d.DisposedAt >= query.FromDate && d.DisposedAt <= query.ToDate);

        if (query.Reason.HasValue)
        {
            filtered = filtered.Where(d => d.Reason == query.Reason.Value);
        }

        if (query.ProductId.HasValue)
        {
            var productBatchIds = (await _batchRepository.GetAllAsync())
                .Where(b => b.ProductId == query.ProductId.Value)
                .Select(b => b.Id)
                .ToList();
            filtered = filtered.Where(d => productBatchIds.Contains(d.BatchId));
        }

        if (query.CategoryId.HasValue)
        {
            var categoryProductIds = (await _productRepository.GetAllAsync())
                .Where(p => p.CategoryId == query.CategoryId.Value)
                .Select(p => p.Id)
                .ToList();
            var categoryBatchIds = (await _batchRepository.GetAllAsync())
                .Where(b => categoryProductIds.Contains(b.ProductId))
                .Select(b => b.Id)
                .ToList();
            filtered = filtered.Where(d => categoryBatchIds.Contains(d.BatchId));
        }

        if (query.SupplierId.HasValue)
        {
            var supplierBatchIds = (await _batchRepository.GetAllAsync())
                .Where(b => b.SupplierId == query.SupplierId.Value)
                .Select(b => b.Id)
                .ToList();
            filtered = filtered.Where(d => supplierBatchIds.Contains(d.BatchId));
        }

        return filtered.ToList();
    }

    private async Task<List<ProductBatch>> GetBatchesForDisposalsAsync(List<BatchDisposal> disposals)
    {
        var batchIds = disposals.Select(d => d.BatchId).Distinct().ToList();
        var batches = await _batchRepository.GetAllAsync();
        return batches.Where(b => batchIds.Contains(b.Id)).ToList();
    }

    private async Task<List<Product>> GetProductsForBatchesAsync(List<ProductBatch> batches)
    {
        var productIds = batches.Select(b => b.ProductId).Distinct().ToList();
        var products = await _productRepository.GetAllAsync();
        return products.Where(p => productIds.Contains(p.Id)).ToList();
    }

    private async Task<List<Category>> GetCategoriesForProductsAsync(List<Product> products)
    {
        var categoryIds = products.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Where(c => categoryIds.Contains(c.Id)).ToList();
    }

    private async Task<List<Supplier>> GetSuppliersForBatchesAsync(List<ProductBatch> batches)
    {
        var supplierIds = batches.Where(b => b.SupplierId.HasValue).Select(b => b.SupplierId!.Value).Distinct().ToList();
        var suppliers = await _supplierRepository.GetAllAsync();
        return suppliers.Where(s => supplierIds.Contains(s.Id)).ToList();
    }

    private List<WasteByCategoryDto> CalculateWasteByCategory(
        List<BatchDisposal> disposals,
        List<ProductBatch> batches,
        List<Product> products,
        List<Category> categories)
    {
        var totalWaste = disposals.Sum(d => d.TotalValue);

        var groupedData = from disposal in disposals
                          join batch in batches on disposal.BatchId equals batch.Id
                          join product in products on batch.ProductId equals product.Id
                          join category in categories on product.CategoryId equals category.Id into categoryJoin
                          from category in categoryJoin.DefaultIfEmpty()
                          group disposal by new { CategoryId = category?.Id ?? 0, CategoryName = category?.Name ?? "Uncategorized" } into g
                          select new WasteByCategoryDto
                          {
                              CategoryId = g.Key.CategoryId,
                              CategoryName = g.Key.CategoryName,
                              WasteValue = g.Sum(d => d.TotalValue),
                              WasteQuantity = g.Sum(d => d.Quantity),
                              WasteRecordCount = g.Count(),
                              PercentageOfTotalWaste = totalWaste > 0 ? (g.Sum(d => d.TotalValue) / totalWaste) * 100 : 0
                          };

        return groupedData.OrderByDescending(c => c.WasteValue).ToList();
    }

    private List<WasteBySupplierDto> CalculateWasteBySupplier(
        List<BatchDisposal> disposals,
        List<ProductBatch> batches,
        List<Supplier> suppliers)
    {
        var totalWaste = disposals.Sum(d => d.TotalValue);

        var groupedData = from disposal in disposals
                          join batch in batches on disposal.BatchId equals batch.Id
                          join supplier in suppliers on batch.SupplierId equals supplier.Id into supplierJoin
                          from supplier in supplierJoin.DefaultIfEmpty()
                          group disposal by new { SupplierId = supplier?.Id ?? 0, SupplierName = supplier?.Name ?? "Unknown" } into g
                          select new WasteBySupplierDto
                          {
                              SupplierId = g.Key.SupplierId,
                              SupplierName = g.Key.SupplierName,
                              WasteValue = g.Sum(d => d.TotalValue),
                              WasteQuantity = g.Sum(d => d.Quantity),
                              WasteRecordCount = g.Count(),
                              BatchesAffected = 0, // Would need additional query
                              PercentageOfTotalWaste = totalWaste > 0 ? (g.Sum(d => d.TotalValue) / totalWaste) * 100 : 0
                          };

        return groupedData.OrderByDescending(s => s.WasteValue).ToList();
    }

    private List<WasteByReasonDto> CalculateWasteByReason(List<BatchDisposal> disposals)
    {
        var totalWaste = disposals.Sum(d => d.TotalValue);

        return disposals
            .GroupBy(d => d.Reason)
            .Select(g => new WasteByReasonDto
            {
                Reason = g.Key.ToString(),
                WasteValue = g.Sum(d => d.TotalValue),
                WasteQuantity = g.Sum(d => d.Quantity),
                WasteRecordCount = g.Count(),
                PercentageOfTotalWaste = totalWaste > 0 ? (g.Sum(d => d.TotalValue) / totalWaste) * 100 : 0
            })
            .OrderByDescending(r => r.WasteValue)
            .ToList();
    }

    private List<WasteByProductDto> CalculateTopWastedProducts(
        List<BatchDisposal> disposals,
        List<ProductBatch> batches,
        List<Product> products,
        int topCount)
    {
        var totalWaste = disposals.Sum(d => d.TotalValue);

        var groupedData = from disposal in disposals
                          join batch in batches on disposal.BatchId equals batch.Id
                          join product in products on batch.ProductId equals product.Id
                          group disposal by new { ProductId = product.Id, ProductName = product.Name, ProductCode = product.Code } into g
                          select new WasteByProductDto
                          {
                              ProductId = g.Key.ProductId,
                              ProductName = g.Key.ProductName,
                              ProductCode = g.Key.ProductCode,
                              WasteValue = g.Sum(d => d.TotalValue),
                              WasteQuantity = g.Sum(d => d.Quantity),
                              WasteRecordCount = g.Count(),
                              AverageWastePerRecord = g.Count() > 0 ? g.Sum(d => d.TotalValue) / g.Count() : 0,
                              TopReason = g.GroupBy(d => d.Reason).OrderByDescending(r => r.Sum(x => x.TotalValue)).First().Key.ToString()
                          };

        return groupedData.OrderByDescending(p => p.WasteValue).Take(topCount).ToList();
    }

    private List<WasteInsightDto> GenerateWasteInsights(WasteAnalysisDto analysis)
    {
        var insights = new List<WasteInsightDto>();

        // Insight 1: Dominant reason
        var topReason = analysis.WasteByReason.FirstOrDefault();
        if (topReason != null && topReason.PercentageOfTotalWaste > 50)
        {
            insights.Add(new WasteInsightDto
            {
                Type = "HighConcentration",
                Title = $"{topReason.Reason} accounts for majority of waste",
                Description = $"{topReason.Reason} is responsible for {topReason.PercentageOfTotalWaste:F1}% of total waste value. Consider reviewing handling procedures for this type.",
                Severity = "Warning",
                RecommendedAction = $"Review {topReason.Reason.ToLower()} prevention procedures"
            });
        }

        // Insight 2: High-value category
        var topCategory = analysis.WasteByCategory.FirstOrDefault();
        if (topCategory != null && topCategory.WasteValue > analysis.TotalWasteValue * 0.3m)
        {
            insights.Add(new WasteInsightDto
            {
                Type = "CategoryFocus",
                Title = $"{topCategory.CategoryName} has highest waste",
                Description = $"{topCategory.CategoryName} products represent {topCategory.PercentageOfTotalWaste:F1}% of waste value ({topCategory.WasteValue:C2}).",
                Severity = "Info",
                RecommendedAction = $"Review ordering and storage practices for {topCategory.CategoryName}"
            });
        }

        // Insight 3: Supplier issue
        var topSupplier = analysis.WasteBySupplier.FirstOrDefault();
        if (topSupplier != null && topSupplier.PercentageOfTotalWaste > 40)
        {
            insights.Add(new WasteInsightDto
            {
                Type = "SupplierConcern",
                Title = $"High waste from {topSupplier.SupplierName}",
                Description = $"Products from {topSupplier.SupplierName} account for {topSupplier.PercentageOfTotalWaste:F1}% of waste. Consider reviewing delivery schedules or product quality.",
                Severity = "Warning",
                RecommendedAction = $"Schedule meeting with {topSupplier.SupplierName} to discuss quality"
            });
        }

        // Insight 4: High daily average
        if (analysis.AverageWastePerDay > 100) // Arbitrary threshold
        {
            insights.Add(new WasteInsightDto
            {
                Type = "HighVolume",
                Title = "High daily waste average",
                Description = $"Average waste of {analysis.AverageWastePerDay:C2} per day detected. This may indicate systemic issues.",
                Severity = "Warning",
                RecommendedAction = "Conduct waste audit and review inventory management practices"
            });
        }

        return insights;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static string GetPeriodLabel(DateTime date, string groupBy)
    {
        return groupBy.ToLower() switch
        {
            "week" => $"Week of {date:MMM dd}",
            "month" => date.ToString("MMM yyyy"),
            _ => date.ToString("MMM dd")
        };
    }

    #endregion
}
