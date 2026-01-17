using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for consolidated chain-wide reporting and analytics.
/// </summary>
public class ChainReportingService : IChainReportingService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Receipt> _receiptRepository;
    private readonly IRepository<ReceiptItem> _receiptItemRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Inventory> _inventoryRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly ILogger<ChainReportingService> _logger;

    public ChainReportingService(
        IRepository<Store> storeRepository,
        IRepository<Receipt> receiptRepository,
        IRepository<ReceiptItem> receiptItemRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IRepository<Inventory> inventoryRepository,
        IRepository<Payment> paymentRepository,
        ILogger<ChainReportingService> logger)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        _receiptItemRepository = receiptItemRepository ?? throw new ArgumentNullException(nameof(receiptItemRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Dashboard

    public async Task<ChainDashboardMetricsDto> GetChainDashboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting chain dashboard metrics");

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var lastWeekStart = weekStart.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);
        var allReceipts = await _receiptRepository.FindAsync(r => r.IsActive && !r.IsVoid, cancellationToken);

        var todayReceipts = allReceipts.Where(r => r.CreatedAt >= todayStart).ToList();
        var yesterdayReceipts = allReceipts.Where(r => r.CreatedAt >= yesterdayStart && r.CreatedAt < todayStart).ToList();
        var weekReceipts = allReceipts.Where(r => r.CreatedAt >= weekStart).ToList();
        var lastWeekReceipts = allReceipts.Where(r => r.CreatedAt >= lastWeekStart && r.CreatedAt < weekStart).ToList();
        var monthReceipts = allReceipts.Where(r => r.CreatedAt >= monthStart).ToList();
        var lastMonthReceipts = allReceipts.Where(r => r.CreatedAt >= lastMonthStart && r.CreatedAt < monthStart).ToList();

        var dashboard = new ChainDashboardMetricsDto
        {
            TotalSalesToday = todayReceipts.Sum(r => r.TotalAmount),
            TotalSalesYesterday = yesterdayReceipts.Sum(r => r.TotalAmount),
            TotalSalesThisWeek = weekReceipts.Sum(r => r.TotalAmount),
            TotalSalesLastWeek = lastWeekReceipts.Sum(r => r.TotalAmount),
            TotalSalesThisMonth = monthReceipts.Sum(r => r.TotalAmount),
            TotalSalesLastMonth = lastMonthReceipts.Sum(r => r.TotalAmount),
            TransactionCountToday = todayReceipts.Count,
            TransactionCountThisWeek = weekReceipts.Count,
            TransactionCountThisMonth = monthReceipts.Count,
            AverageBasketSizeToday = todayReceipts.Any() ? todayReceipts.Average(r => r.TotalAmount) : 0,
            AverageBasketSizeThisWeek = weekReceipts.Any() ? weekReceipts.Average(r => r.TotalAmount) : 0,
            AverageBasketSizeThisMonth = monthReceipts.Any() ? monthReceipts.Average(r => r.TotalAmount) : 0,
            TotalStores = stores.Count(),
            OnlineStores = stores.Count(s => s.LastSyncTime.HasValue && s.LastSyncTime.Value >= now.AddMinutes(-30)),
            LastUpdated = now
        };

        dashboard.OfflineStores = dashboard.TotalStores - dashboard.OnlineStores;

        // Calculate growth percentages
        dashboard.TodayGrowthPercent = CalculateGrowthPercent(dashboard.TotalSalesYesterday, dashboard.TotalSalesToday);
        dashboard.WeekGrowthPercent = CalculateGrowthPercent(dashboard.TotalSalesLastWeek, dashboard.TotalSalesThisWeek);
        dashboard.MonthGrowthPercent = CalculateGrowthPercent(dashboard.TotalSalesLastMonth, dashboard.TotalSalesThisMonth);

        // Get store breakdown
        dashboard.StoreBreakdown = await GetStoreBreakdownAsync(stores, todayReceipts, weekReceipts, monthReceipts, now, cancellationToken);

        // Get top products
        dashboard.TopProducts = await GetTopProductsForDashboardAsync(monthReceipts, 10, cancellationToken);

        // Get top categories
        dashboard.TopCategories = await GetTopCategoriesForDashboardAsync(monthReceipts, 5, cancellationToken);

        return dashboard;
    }

    public async Task<IEnumerable<StoreSummaryDto>> GetAllStoresSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= monthStart, cancellationToken);

        var summaries = new List<StoreSummaryDto>();
        foreach (var store in stores)
        {
            var storeReceipts = receipts.Where(r => r.StoreId == store.Id).ToList();
            var todayReceipts = storeReceipts.Where(r => r.CreatedAt >= todayStart).ToList();
            var weekReceipts = storeReceipts.Where(r => r.CreatedAt >= weekStart).ToList();

            summaries.Add(new StoreSummaryDto
            {
                StoreId = store.Id,
                StoreCode = store.StoreCode,
                StoreName = store.Name,
                Region = store.Region ?? string.Empty,
                SalesToday = todayReceipts.Sum(r => r.TotalAmount),
                SalesThisWeek = weekReceipts.Sum(r => r.TotalAmount),
                SalesThisMonth = storeReceipts.Sum(r => r.TotalAmount),
                TransactionsToday = todayReceipts.Count,
                TransactionsThisWeek = weekReceipts.Count,
                TransactionsThisMonth = storeReceipts.Count,
                AverageBasketToday = todayReceipts.Any() ? todayReceipts.Average(r => r.TotalAmount) : 0,
                AverageBasketThisWeek = weekReceipts.Any() ? weekReceipts.Average(r => r.TotalAmount) : 0,
                AverageBasketThisMonth = storeReceipts.Any() ? storeReceipts.Average(r => r.TotalAmount) : 0,
                IsOnline = store.LastSyncTime.HasValue && store.LastSyncTime.Value >= now.AddMinutes(-30),
                LastSyncTime = store.LastSyncTime
            });
        }

        return summaries.OrderByDescending(s => s.SalesToday);
    }

    public async Task<StoreSummaryDto?> GetStoreSummaryAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store == null || !store.IsActive)
            return null;

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var receipts = await _receiptRepository.FindAsync(
            r => r.StoreId == storeId && r.IsActive && !r.IsVoid && r.CreatedAt >= monthStart, cancellationToken);

        var todayReceipts = receipts.Where(r => r.CreatedAt >= todayStart).ToList();
        var weekReceipts = receipts.Where(r => r.CreatedAt >= weekStart).ToList();
        var monthReceipts = receipts.ToList();

        return new StoreSummaryDto
        {
            StoreId = store.Id,
            StoreCode = store.StoreCode,
            StoreName = store.Name,
            Region = store.Region ?? string.Empty,
            SalesToday = todayReceipts.Sum(r => r.TotalAmount),
            SalesThisWeek = weekReceipts.Sum(r => r.TotalAmount),
            SalesThisMonth = monthReceipts.Sum(r => r.TotalAmount),
            TransactionsToday = todayReceipts.Count,
            TransactionsThisWeek = weekReceipts.Count,
            TransactionsThisMonth = monthReceipts.Count,
            AverageBasketToday = todayReceipts.Any() ? todayReceipts.Average(r => r.TotalAmount) : 0,
            AverageBasketThisWeek = weekReceipts.Any() ? weekReceipts.Average(r => r.TotalAmount) : 0,
            AverageBasketThisMonth = monthReceipts.Any() ? monthReceipts.Average(r => r.TotalAmount) : 0,
            IsOnline = store.LastSyncTime.HasValue && store.LastSyncTime.Value >= now.AddMinutes(-30),
            LastSyncTime = store.LastSyncTime
        };
    }

    #endregion

    #region Store Comparison

    public async Task<StoreComparisonReportDto> GetStoreComparisonReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var fromDate = query?.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = query?.ToDate ?? DateTime.UtcNow;
        var previousFromDate = fromDate.AddDays(-(toDate - fromDate).Days);

        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);
        if (query?.StoreIds?.Any() == true)
            stores = stores.Where(s => query.StoreIds.Contains(s.Id));

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= previousFromDate && r.CreatedAt <= toDate, cancellationToken);

        var currentReceipts = receipts.Where(r => r.CreatedAt >= fromDate && r.CreatedAt <= toDate).ToList();
        var previousReceipts = receipts.Where(r => r.CreatedAt >= previousFromDate && r.CreatedAt < fromDate).ToList();

        var rankings = new List<StoreRankingDto>();
        var storeList = stores.ToList();
        var totalChainSales = currentReceipts.Sum(r => r.TotalAmount);

        foreach (var store in storeList)
        {
            var storeCurrentReceipts = currentReceipts.Where(r => r.StoreId == store.Id).ToList();
            var storePreviousReceipts = previousReceipts.Where(r => r.StoreId == store.Id).ToList();

            var currentSales = storeCurrentReceipts.Sum(r => r.TotalAmount);
            var previousSales = storePreviousReceipts.Sum(r => r.TotalAmount);

            rankings.Add(new StoreRankingDto
            {
                StoreId = store.Id,
                StoreCode = store.StoreCode,
                StoreName = store.Name,
                Region = store.Region ?? string.Empty,
                Sales = currentSales,
                PreviousPeriodSales = previousSales,
                SalesGrowthPercent = CalculateGrowthPercent(previousSales, currentSales),
                Transactions = storeCurrentReceipts.Count,
                AverageBasket = storeCurrentReceipts.Any() ? storeCurrentReceipts.Average(r => r.TotalAmount) : 0,
                ContributionPercent = totalChainSales > 0 ? (currentSales / totalChainSales) * 100 : 0,
                DiscountsGiven = storeCurrentReceipts.Sum(r => r.DiscountAmount)
            });
        }

        // Sort and assign ranks
        var sortedRankings = rankings.OrderByDescending(r => r.Sales).ToList();
        for (int i = 0; i < sortedRankings.Count; i++)
        {
            sortedRankings[i].Rank = i + 1;
        }

        // Calculate previous ranks
        var previousRankings = rankings
            .OrderByDescending(r => r.PreviousPeriodSales)
            .Select((r, i) => new { r.StoreId, Rank = i + 1 })
            .ToDictionary(x => x.StoreId, x => x.Rank);

        foreach (var ranking in sortedRankings)
        {
            ranking.PreviousRank = previousRankings.GetValueOrDefault(ranking.StoreId, 0);
        }

        return new StoreComparisonReportDto
        {
            ReportTitle = "Store Comparison Report",
            Period = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
            FromDate = fromDate,
            ToDate = toDate,
            TotalStores = storeList.Count,
            TotalChainSales = totalChainSales,
            TotalChainTransactions = currentReceipts.Count,
            AverageStoreSales = storeList.Count > 0 ? totalChainSales / storeList.Count : 0,
            Rankings = sortedRankings,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<StoreRankingDto>> GetTopPerformingStoresAsync(int topN = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var report = await GetStoreComparisonReportAsync(new ChainReportQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate
        }, cancellationToken);

        return report.Rankings.Take(topN);
    }

    public async Task<IEnumerable<StoreRankingDto>> GetUnderperformingStoresAsync(int bottomN = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var report = await GetStoreComparisonReportAsync(new ChainReportQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate
        }, cancellationToken);

        return report.Rankings.TakeLast(bottomN).Reverse();
    }

    #endregion

    #region Product Performance

    public async Task<ProductPerformanceReportDto> GetProductPerformanceReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var fromDate = query?.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = query?.ToDate ?? DateTime.UtcNow;

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= fromDate && r.CreatedAt <= toDate, cancellationToken);

        if (query?.StoreIds?.Any() == true)
            receipts = receipts.Where(r => query.StoreIds.Contains(r.StoreId ?? 0));

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var receiptItems = await _receiptItemRepository.FindAsync(
            ri => receiptIds.Contains(ri.ReceiptId) && ri.IsActive, cancellationToken);

        var products = await _productRepository.FindAsync(p => p.IsActive, cancellationToken);
        var categories = await _categoryRepository.FindAsync(c => c.IsActive, cancellationToken);
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);

        if (query?.CategoryIds?.Any() == true)
            products = products.Where(p => p.CategoryId.HasValue && query.CategoryIds.Contains(p.CategoryId.Value));

        var productPerformances = new List<ProductPerformanceDto>();
        var productDict = products.ToDictionary(p => p.Id);
        var categoryDict = categories.ToDictionary(c => c.Id);
        var storeDict = stores.ToDictionary(s => s.Id);
        var receiptDict = receipts.ToDictionary(r => r.Id);

        var groupedItems = receiptItems.GroupBy(ri => ri.ProductId);

        foreach (var group in groupedItems)
        {
            if (!productDict.TryGetValue(group.Key, out var product))
                continue;

            var items = group.ToList();
            var storeBreakdown = items
                .GroupBy(ri => receiptDict.GetValueOrDefault(ri.ReceiptId)?.StoreId ?? 0)
                .Where(g => storeDict.ContainsKey(g.Key))
                .Select(g =>
                {
                    var store = storeDict[g.Key];
                    var storeItems = g.ToList();
                    return new ProductStoreBreakdownDto
                    {
                        StoreId = g.Key,
                        StoreName = store.Name,
                        QuantitySold = (int)storeItems.Sum(i => i.Quantity),
                        Revenue = storeItems.Sum(i => i.TotalPrice),
                        AveragePrice = storeItems.Any() ? storeItems.Average(i => i.UnitPrice) : 0
                    };
                })
                .ToList();

            var totalRevenue = items.Sum(i => i.TotalPrice);
            foreach (var sb in storeBreakdown)
            {
                sb.ContributionPercent = totalRevenue > 0 ? (sb.Revenue / totalRevenue) * 100 : 0;
            }

            productPerformances.Add(new ProductPerformanceDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.CategoryId.HasValue && categoryDict.TryGetValue(product.CategoryId.Value, out var cat) ? cat.Name : "",
                TotalQuantitySold = (int)items.Sum(i => i.Quantity),
                TotalRevenue = totalRevenue,
                AveragePrice = items.Any() ? items.Average(i => i.UnitPrice) : 0,
                CostPrice = product.CostPrice ?? 0,
                GrossMargin = product.CostPrice.HasValue && product.CostPrice > 0
                    ? ((product.SellingPrice - product.CostPrice.Value) / product.SellingPrice) * 100 : 0,
                StoresSellingCount = storeBreakdown.Count,
                StoreBreakdown = storeBreakdown
            });
        }

        var sortedProducts = (query?.SortDescending ?? true)
            ? productPerformances.OrderByDescending(p => p.TotalRevenue)
            : productPerformances.OrderBy(p => p.TotalRevenue);

        if (query?.TopN.HasValue == true)
            sortedProducts = sortedProducts.Take(query.TopN.Value);

        return new ProductPerformanceReportDto
        {
            ReportTitle = "Product Performance Report",
            FromDate = fromDate,
            ToDate = toDate,
            TotalProducts = productPerformances.Count,
            TotalRevenue = productPerformances.Sum(p => p.TotalRevenue),
            TotalQuantitySold = productPerformances.Sum(p => p.TotalQuantitySold),
            Products = sortedProducts.ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<ProductPerformanceDto>> GetTopSellingProductsAsync(int topN = 20, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var report = await GetProductPerformanceReportAsync(new ChainReportQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TopN = topN,
            SortDescending = true
        }, cancellationToken);

        return report.Products;
    }

    public async Task<ProductPerformanceDto?> GetProductPerformanceByStoreAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var report = await GetProductPerformanceReportAsync(new ChainReportQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate
        }, cancellationToken);

        return report.Products.FirstOrDefault(p => p.ProductId == productId);
    }

    #endregion

    #region Category Performance

    public async Task<CategoryPerformanceReportDto> GetCategoryPerformanceReportAsync(ChainReportQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var fromDate = query?.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = query?.ToDate ?? DateTime.UtcNow;

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= fromDate && r.CreatedAt <= toDate, cancellationToken);

        if (query?.StoreIds?.Any() == true)
            receipts = receipts.Where(r => query.StoreIds.Contains(r.StoreId ?? 0));

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var receiptItems = await _receiptItemRepository.FindAsync(
            ri => receiptIds.Contains(ri.ReceiptId) && ri.IsActive, cancellationToken);

        var products = await _productRepository.FindAsync(p => p.IsActive, cancellationToken);
        var categories = await _categoryRepository.FindAsync(c => c.IsActive, cancellationToken);
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);

        var productDict = products.ToDictionary(p => p.Id);
        var categoryDict = categories.ToDictionary(c => c.Id);
        var storeDict = stores.ToDictionary(s => s.Id);
        var receiptDict = receipts.ToDictionary(r => r.Id);

        // Group items by category
        var itemsWithCategory = receiptItems
            .Where(ri => productDict.ContainsKey(ri.ProductId) && productDict[ri.ProductId].CategoryId.HasValue)
            .Select(ri => new { Item = ri, CategoryId = productDict[ri.ProductId].CategoryId!.Value });

        var groupedByCategory = itemsWithCategory.GroupBy(x => x.CategoryId);

        var categoryPerformances = new List<CategoryPerformanceDto>();
        var totalRevenue = receiptItems.Sum(ri => ri.TotalPrice);

        foreach (var group in groupedByCategory)
        {
            if (!categoryDict.TryGetValue(group.Key, out var category))
                continue;

            var items = group.Select(x => x.Item).ToList();
            var categoryProducts = products.Where(p => p.CategoryId == group.Key).ToList();

            var storeBreakdown = items
                .GroupBy(ri => receiptDict.GetValueOrDefault(ri.ReceiptId)?.StoreId ?? 0)
                .Where(g => storeDict.ContainsKey(g.Key))
                .Select(g =>
                {
                    var store = storeDict[g.Key];
                    var storeItems = g.ToList();
                    var storeRevenue = storeItems.Sum(i => i.TotalPrice);
                    return new CategoryStoreBreakdownDto
                    {
                        StoreId = g.Key,
                        StoreName = store.Name,
                        QuantitySold = (int)storeItems.Sum(i => i.Quantity),
                        Revenue = storeRevenue
                    };
                })
                .ToList();

            var categoryRevenue = items.Sum(i => i.TotalPrice);
            foreach (var sb in storeBreakdown)
            {
                sb.ContributionPercent = categoryRevenue > 0 ? (sb.Revenue / categoryRevenue) * 100 : 0;
            }

            categoryPerformances.Add(new CategoryPerformanceDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                ProductCount = categoryProducts.Count,
                TotalQuantitySold = (int)items.Sum(i => i.Quantity),
                TotalRevenue = categoryRevenue,
                AveragePrice = items.Any() ? items.Average(i => i.UnitPrice) : 0,
                ContributionPercent = totalRevenue > 0 ? (categoryRevenue / totalRevenue) * 100 : 0,
                StoreBreakdown = storeBreakdown
            });
        }

        return new CategoryPerformanceReportDto
        {
            ReportTitle = "Category Performance Report",
            FromDate = fromDate,
            ToDate = toDate,
            TotalCategories = categoryPerformances.Count,
            TotalRevenue = categoryPerformances.Sum(c => c.TotalRevenue),
            Categories = categoryPerformances.OrderByDescending(c => c.TotalRevenue).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<CategoryPerformanceDto?> GetCategoryPerformanceByStoreAsync(int categoryId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var report = await GetCategoryPerformanceReportAsync(new ChainReportQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate
        }, cancellationToken);

        return report.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
    }

    #endregion

    #region Sales Trends

    public async Task<SalesTrendReportDto> GetSalesTrendReportAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var dailyTrends = await GetDailySalesTrendsAsync(from, to, storeIds, cancellationToken);
        var hourlyPatterns = await GetHourlySalesPatternsAsync(from, to, storeIds, cancellationToken);

        var trendsList = dailyTrends.ToList();
        var patternsList = hourlyPatterns.ToList();

        var bestDay = trendsList.OrderByDescending(d => d.Sales).FirstOrDefault();
        var peakHour = patternsList.OrderByDescending(h => h.AverageSales).FirstOrDefault();

        return new SalesTrendReportDto
        {
            ReportTitle = "Sales Trend Report",
            FromDate = from,
            ToDate = to,
            DailyTrends = trendsList,
            HourlyPatterns = patternsList,
            TotalSales = trendsList.Sum(d => d.Sales),
            TotalTransactions = trendsList.Sum(d => d.Transactions),
            AverageBasket = trendsList.Any() ? trendsList.Average(d => d.AverageBasket) : 0,
            BestDaySales = bestDay?.Sales ?? 0,
            BestDayDate = bestDay?.Date,
            PeakHour = peakHour?.Hour ?? 0,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<DailySalesTrendDto>> GetDailySalesTrendsAsync(DateTime fromDate, DateTime toDate, List<int>? storeIds = null, CancellationToken cancellationToken = default)
    {
        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= fromDate && r.CreatedAt <= toDate, cancellationToken);

        if (storeIds?.Any() == true)
            receipts = receipts.Where(r => storeIds.Contains(r.StoreId ?? 0));

        var grouped = receipts.GroupBy(r => r.CreatedAt.Date);

        return grouped.Select(g => new DailySalesTrendDto
        {
            Date = g.Key,
            Sales = g.Sum(r => r.TotalAmount),
            Transactions = g.Count(),
            AverageBasket = g.Any() ? g.Average(r => r.TotalAmount) : 0
        }).OrderBy(d => d.Date);
    }

    public async Task<IEnumerable<HourlySalesPatternDto>> GetHourlySalesPatternsAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && !r.IsVoid && r.CreatedAt >= from && r.CreatedAt <= to, cancellationToken);

        if (storeIds?.Any() == true)
            receipts = receipts.Where(r => storeIds.Contains(r.StoreId ?? 0));

        var receiptList = receipts.ToList();
        var dayCount = (to - from).Days;
        if (dayCount < 1) dayCount = 1;

        var hourlyGroups = receiptList.GroupBy(r => r.CreatedAt.Hour);

        var patterns = new List<HourlySalesPatternDto>();
        for (int hour = 0; hour < 24; hour++)
        {
            var hourReceipts = hourlyGroups.FirstOrDefault(g => g.Key == hour)?.ToList() ?? new List<Receipt>();
            patterns.Add(new HourlySalesPatternDto
            {
                Hour = hour,
                TimeLabel = $"{hour:00}:00 - {hour:00}:59",
                AverageSales = hourReceipts.Any() ? hourReceipts.Sum(r => r.TotalAmount) / dayCount : 0,
                AverageTransactions = hourReceipts.Count / dayCount
            });
        }

        return patterns;
    }

    #endregion

    #region Financial Summary

    public async Task<ChainFinancialSummaryDto> GetChainFinancialSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var receipts = await _receiptRepository.FindAsync(
            r => r.IsActive && r.CreatedAt >= from && r.CreatedAt <= to, cancellationToken);

        var validReceipts = receipts.Where(r => !r.IsVoid).ToList();
        var voidedReceipts = receipts.Where(r => r.IsVoid).ToList();

        var grossSales = validReceipts.Sum(r => r.SubTotal);
        var discounts = validReceipts.Sum(r => r.DiscountAmount);
        var returns = voidedReceipts.Sum(r => r.TotalAmount);
        var netSales = grossSales - discounts - returns;

        var paymentBreakdown = await GetPaymentMethodBreakdownAsync(from, to, null, cancellationToken);
        var storeSummaries = await GetAllStoresSummaryAsync(cancellationToken);

        return new ChainFinancialSummaryDto
        {
            FromDate = from,
            ToDate = to,
            GrossSales = grossSales,
            Returns = returns,
            Discounts = discounts,
            NetSales = netSales,
            PaymentBreakdown = paymentBreakdown.ToList(),
            StoreBreakdown = storeSummaries.ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdownAsync(DateTime? fromDate = null, DateTime? toDate = null, List<int>? storeIds = null, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var payments = await _paymentRepository.FindAsync(
            p => p.IsActive && p.PaymentDate >= from && p.PaymentDate <= to, cancellationToken);

        // Filter by store if provided (would need to join with receipts)
        var paymentList = payments.ToList();
        var totalAmount = paymentList.Sum(p => p.AmountPaid);

        var grouped = paymentList.GroupBy(p => p.PaymentMethodId);

        return grouped.Select(g => new PaymentMethodBreakdownDto
        {
            PaymentMethod = $"Method {g.Key}",
            TransactionCount = g.Count(),
            TotalAmount = g.Sum(p => p.AmountPaid),
            ContributionPercent = totalAmount > 0 ? (g.Sum(p => p.AmountPaid) / totalAmount) * 100 : 0
        }).OrderByDescending(p => p.TotalAmount);
    }

    #endregion

    #region Inventory

    public async Task<ChainInventoryStatusDto> GetChainInventoryStatusAsync(CancellationToken cancellationToken = default)
    {
        var inventories = await _inventoryRepository.FindAsync(i => i.IsActive, cancellationToken);
        var products = await _productRepository.FindAsync(p => p.IsActive, cancellationToken);
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken);

        var productDict = products.ToDictionary(p => p.Id);
        var storeDict = stores.ToDictionary(s => s.Id);

        var lowStockAlerts = new List<LowStockAlertDto>();
        var storeBreakdown = new List<StoreInventorySummaryDto>();

        foreach (var store in stores)
        {
            var storeInventory = inventories.Where(i => i.StoreId == store.Id).ToList();
            var lowStock = storeInventory.Where(i => i.CurrentStock <= i.MinimumStock).ToList();
            var outOfStock = storeInventory.Where(i => i.CurrentStock <= 0).ToList();

            storeBreakdown.Add(new StoreInventorySummaryDto
            {
                StoreId = store.Id,
                StoreName = store.Name,
                TotalProducts = storeInventory.Count,
                LowStockCount = lowStock.Count,
                OutOfStockCount = outOfStock.Count,
                InventoryValue = storeInventory.Sum(i =>
                    productDict.TryGetValue(i.ProductId, out var p) ? i.CurrentStock * (p.CostPrice ?? 0) : 0)
            });

            foreach (var inv in lowStock)
            {
                if (!productDict.TryGetValue(inv.ProductId, out var product))
                    continue;

                lowStockAlerts.Add(new LowStockAlertDto
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    CurrentStock = inv.CurrentStock,
                    MinimumStock = inv.MinimumStock,
                    ReorderLevel = inv.ReorderLevel,
                    IsOutOfStock = inv.CurrentStock <= 0
                });
            }
        }

        return new ChainInventoryStatusDto
        {
            TotalProducts = products.Count(),
            LowStockProducts = lowStockAlerts.Select(a => a.ProductId).Distinct().Count(),
            OutOfStockProducts = lowStockAlerts.Count(a => a.IsOutOfStock),
            TotalInventoryValue = storeBreakdown.Sum(s => s.InventoryValue),
            StoreBreakdown = storeBreakdown,
            LowStockAlerts = lowStockAlerts.OrderBy(a => a.CurrentStock).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<LowStockAlertDto>> GetLowStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetChainInventoryStatusAsync(cancellationToken);
        return status.LowStockAlerts;
    }

    #endregion

    #region Private Helper Methods

    private static decimal CalculateGrowthPercent(decimal previous, decimal current)
    {
        if (previous == 0)
            return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private async Task<List<StoreSummaryDto>> GetStoreBreakdownAsync(
        IEnumerable<Store> stores,
        List<Receipt> todayReceipts,
        List<Receipt> weekReceipts,
        List<Receipt> monthReceipts,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var summaries = new List<StoreSummaryDto>();
        foreach (var store in stores)
        {
            var storeTodayReceipts = todayReceipts.Where(r => r.StoreId == store.Id).ToList();
            var storeWeekReceipts = weekReceipts.Where(r => r.StoreId == store.Id).ToList();
            var storeMonthReceipts = monthReceipts.Where(r => r.StoreId == store.Id).ToList();

            summaries.Add(new StoreSummaryDto
            {
                StoreId = store.Id,
                StoreCode = store.StoreCode,
                StoreName = store.Name,
                Region = store.Region ?? string.Empty,
                SalesToday = storeTodayReceipts.Sum(r => r.TotalAmount),
                SalesThisWeek = storeWeekReceipts.Sum(r => r.TotalAmount),
                SalesThisMonth = storeMonthReceipts.Sum(r => r.TotalAmount),
                TransactionsToday = storeTodayReceipts.Count,
                TransactionsThisWeek = storeWeekReceipts.Count,
                TransactionsThisMonth = storeMonthReceipts.Count,
                AverageBasketToday = storeTodayReceipts.Any() ? storeTodayReceipts.Average(r => r.TotalAmount) : 0,
                IsOnline = store.LastSyncTime.HasValue && store.LastSyncTime.Value >= now.AddMinutes(-30),
                LastSyncTime = store.LastSyncTime
            });
        }
        return summaries.OrderByDescending(s => s.SalesToday).ToList();
    }

    private async Task<List<TopProductDto>> GetTopProductsForDashboardAsync(
        List<Receipt> receipts,
        int topN,
        CancellationToken cancellationToken)
    {
        var receiptIds = receipts.Select(r => r.Id).ToList();
        var receiptItems = await _receiptItemRepository.FindAsync(
            ri => receiptIds.Contains(ri.ReceiptId) && ri.IsActive, cancellationToken);

        var products = await _productRepository.FindAsync(p => p.IsActive, cancellationToken);
        var productDict = products.ToDictionary(p => p.Id);

        return receiptItems
            .GroupBy(ri => ri.ProductId)
            .Select(g =>
            {
                productDict.TryGetValue(g.Key, out var product);
                return new TopProductDto
                {
                    ProductId = g.Key,
                    ProductCode = product?.Code ?? "",
                    ProductName = product?.Name ?? "",
                    QuantitySold = (int)g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.TotalPrice)
                };
            })
            .OrderByDescending(p => p.Revenue)
            .Take(topN)
            .ToList();
    }

    private async Task<List<TopCategoryDto>> GetTopCategoriesForDashboardAsync(
        List<Receipt> receipts,
        int topN,
        CancellationToken cancellationToken)
    {
        var receiptIds = receipts.Select(r => r.Id).ToList();
        var receiptItems = await _receiptItemRepository.FindAsync(
            ri => receiptIds.Contains(ri.ReceiptId) && ri.IsActive, cancellationToken);

        var products = await _productRepository.FindAsync(p => p.IsActive, cancellationToken);
        var categories = await _categoryRepository.FindAsync(c => c.IsActive, cancellationToken);

        var productDict = products.ToDictionary(p => p.Id);
        var categoryDict = categories.ToDictionary(c => c.Id);

        return receiptItems
            .Where(ri => productDict.ContainsKey(ri.ProductId) && productDict[ri.ProductId].CategoryId.HasValue)
            .GroupBy(ri => productDict[ri.ProductId].CategoryId!.Value)
            .Select(g =>
            {
                categoryDict.TryGetValue(g.Key, out var category);
                return new TopCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = category?.Name ?? "",
                    QuantitySold = (int)g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.TotalPrice)
                };
            })
            .OrderByDescending(c => c.Revenue)
            .Take(topN)
            .ToList();
    }

    #endregion
}
