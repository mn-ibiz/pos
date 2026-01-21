using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for customer analytics including RFM analysis, CLV, and behavior analysis.
/// </summary>
public class CustomerAnalyticsService : ICustomerAnalyticsService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<CustomerAnalyticsService> _logger;

    public CustomerAnalyticsService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<CustomerAnalyticsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<CustomerAnalyticsReport> GenerateCustomerAnalyticsReportAsync(
        RFMAnalysisParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new CustomerAnalyticsReport
        {
            AnalysisDate = DateTime.UtcNow,
            DataStartDate = parameters.StartDate,
            DataEndDate = parameters.EndDate
        };

        // Get RFM scores from loyalty members
        var rfmScores = await CalculateRFMScoresAsync(parameters, cancellationToken);

        report.Customers = rfmScores;
        report.TotalCustomers = rfmScores.Count;
        report.ActiveCustomers = rfmScores.Count(c => c.DaysSinceLastPurchase <= 90);
        report.NewCustomers = rfmScores.Count(c => c.Segment == RFMSegment.NewCustomer);
        report.ChurningCustomers = rfmScores.Count(c =>
            c.Segment == RFMSegment.AtRisk ||
            c.Segment == RFMSegment.CantLoseThem ||
            c.Segment == RFMSegment.Lost);

        report.TotalRevenue = rfmScores.Sum(c => c.TotalSpent);
        report.AverageOrderValue = rfmScores.Any() ? rfmScores.Average(c => c.AverageOrderValue) : 0;
        report.AverageOrdersPerCustomer = rfmScores.Any() ? (decimal)rfmScores.Average(c => c.TotalPurchases) : 0;

        // Segment distribution
        foreach (var segment in Enum.GetValues<RFMSegment>())
        {
            var segmentCustomers = rfmScores.Where(c => c.Segment == segment).ToList();
            if (!segmentCustomers.Any()) continue;

            report.SegmentSummaries.Add(new RFMSegmentSummary
            {
                Segment = segment,
                SegmentName = segmentCustomers.First().SegmentName,
                SegmentColor = segmentCustomers.First().SegmentColor,
                CustomerCount = segmentCustomers.Count,
                CustomerPercentage = report.TotalCustomers > 0
                    ? Math.Round((decimal)segmentCustomers.Count / report.TotalCustomers * 100, 2) : 0,
                TotalRevenue = segmentCustomers.Sum(c => c.TotalSpent),
                RevenuePercentage = report.TotalRevenue > 0
                    ? Math.Round(segmentCustomers.Sum(c => c.TotalSpent) / report.TotalRevenue * 100, 2) : 0,
                AverageOrderValue = segmentCustomers.Average(c => c.AverageOrderValue),
                AverageLifetimeValue = segmentCustomers.Average(c => c.PredictedLifetimeValue),
                AverageChurnRisk = segmentCustomers.Average(c => c.ChurnRiskScore),
                RecommendedStrategy = GetSegmentStrategy(segment)
            });
        }

        report.AtRiskCustomers = rfmScores
            .Where(c => c.ChurnRiskLevel == ChurnRiskLevel.High || c.ChurnRiskLevel == ChurnRiskLevel.Critical)
            .OrderByDescending(c => c.TotalSpent)
            .Take(parameters.TopAtRiskCount)
            .ToList();

        report.HighValueCustomers = rfmScores
            .Where(c => c.Segment == RFMSegment.Champion || c.Segment == RFMSegment.Loyal)
            .OrderByDescending(c => c.PredictedLifetimeValue)
            .Take(parameters.TopHighValueCount)
            .ToList();

        report.ChurnAnalysis = new ChurnAnalysisSummary
        {
            TotalAtRisk = report.AtRiskCustomers.Count,
            AtRiskPercentage = report.TotalCustomers > 0
                ? Math.Round((decimal)report.AtRiskCustomers.Count / report.TotalCustomers * 100, 2) : 0,
            PotentialRevenueAtRisk = report.AtRiskCustomers.Sum(c => c.PredictedLifetimeValue),
            LowRiskCount = rfmScores.Count(c => c.ChurnRiskLevel == ChurnRiskLevel.Low),
            MediumRiskCount = rfmScores.Count(c => c.ChurnRiskLevel == ChurnRiskLevel.Medium),
            HighRiskCount = rfmScores.Count(c => c.ChurnRiskLevel == ChurnRiskLevel.High),
            CriticalRiskCount = rfmScores.Count(c => c.ChurnRiskLevel == ChurnRiskLevel.Critical)
        };

        if (parameters.IncludeRecommendations)
        {
            report.Recommendations = GenerateRecommendations(report);
        }

        _logger.LogInformation("Generated Customer Analytics Report: {TotalCustomers} customers, {AtRiskCount} at risk",
            report.TotalCustomers, report.AtRiskCustomers.Count);

        return report;
    }

    public async Task<List<CustomerRFMScore>> CalculateRFMScoresAsync(
        RFMAnalysisParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var analysisDate = parameters.EndDate;
        var rfmScores = new List<CustomerRFMScore>();

        // Get loyalty members with their transactions
        var members = await context.Set<Core.Entities.LoyaltyMember>()
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);

        var transactions = await context.Set<Core.Entities.LoyaltyTransaction>()
            .Where(t => t.TransactionDate >= parameters.StartDate && t.TransactionDate <= parameters.EndDate)
            .ToListAsync(cancellationToken);

        var transactionsByMember = transactions.GroupBy(t => t.LoyaltyMemberId).ToDictionary(g => g.Key, g => g.ToList());

        // Calculate monetary percentiles
        var monetaryValues = transactionsByMember
            .Select(kvp => kvp.Value.Sum(t => t.MonetaryValue))
            .OrderBy(v => v)
            .ToList();
        var monetaryPercentiles = CalculatePercentiles(monetaryValues);

        foreach (var member in members)
        {
            if (!transactionsByMember.TryGetValue(member.Id, out var memberTransactions))
            {
                memberTransactions = new List<Core.Entities.LoyaltyTransaction>();
            }

            if (!memberTransactions.Any())
            {
                // No transactions - mark as lost/hibernating
                rfmScores.Add(new CustomerRFMScore
                {
                    CustomerId = member.Id,
                    CustomerName = member.Name,
                    Email = member.Email,
                    Phone = member.PhoneNumber,
                    RecencyScore = 1,
                    FrequencyScore = 1,
                    MonetaryScore = 1,
                    Segment = RFMSegment.Lost,
                    ChurnRiskScore = 100,
                    LoyaltyPoints = (int)member.PointsBalance,
                    LoyaltyTier = member.Tier.ToString(),
                    RecommendedAction = "Win-back campaign needed"
                });
                continue;
            }

            var firstPurchase = memberTransactions.Min(t => t.TransactionDate);
            var lastPurchase = memberTransactions.Max(t => t.TransactionDate);
            var daysSinceLastPurchase = (int)(analysisDate - lastPurchase).TotalDays;
            var totalPurchases = memberTransactions.Count;
            var totalSpent = memberTransactions.Sum(t => t.MonetaryValue);

            // Calculate R score (lower recency = better = higher score)
            var recencyScore = daysSinceLastPurchase <= parameters.RecencyScore5Threshold ? 5
                : daysSinceLastPurchase <= parameters.RecencyScore4Threshold ? 4
                : daysSinceLastPurchase <= parameters.RecencyScore3Threshold ? 3
                : daysSinceLastPurchase <= parameters.RecencyScore2Threshold ? 2
                : 1;

            // Calculate F score (higher frequency = higher score)
            var frequencyScore = totalPurchases >= parameters.FrequencyScore5Threshold ? 5
                : totalPurchases >= parameters.FrequencyScore4Threshold ? 4
                : totalPurchases >= parameters.FrequencyScore3Threshold ? 3
                : totalPurchases >= parameters.FrequencyScore2Threshold ? 2
                : 1;

            // Calculate M score
            var monetaryScore = parameters.UsePercentileMonetaryScoring
                ? GetPercentileScore(totalSpent, monetaryPercentiles)
                : totalSpent switch
                {
                    >= 10000 => 5,
                    >= 5000 => 4,
                    >= 2000 => 3,
                    >= 500 => 2,
                    _ => 1
                };

            var segment = ClassifyCustomer(recencyScore, frequencyScore, monetaryScore);
            var churnRisk = CalculateChurnRisk(recencyScore, frequencyScore, monetaryScore, daysSinceLastPurchase);

            var avgDaysBetweenPurchases = totalPurchases > 1
                ? (decimal)(lastPurchase - firstPurchase).TotalDays / (totalPurchases - 1)
                : 30;

            var monthlyValue = avgDaysBetweenPurchases > 0
                ? totalSpent / totalPurchases * (30 / avgDaysBetweenPurchases)
                : 0;

            var predictedCLV = monthlyValue * 24 * (1 - churnRisk / 100);

            rfmScores.Add(new CustomerRFMScore
            {
                CustomerId = member.Id,
                CustomerName = member.Name,
                Email = member.Email,
                Phone = member.PhoneNumber,
                FirstPurchaseDate = firstPurchase,
                LastPurchaseDate = lastPurchase,
                DaysSinceLastPurchase = daysSinceLastPurchase,
                TotalPurchases = totalPurchases,
                TotalSpent = totalSpent,
                RecencyScore = recencyScore,
                FrequencyScore = frequencyScore,
                MonetaryScore = monetaryScore,
                Segment = segment,
                ChurnRiskScore = churnRisk,
                PredictedLifetimeValue = predictedCLV,
                VisitFrequencyDays = avgDaysBetweenPurchases,
                LoyaltyPoints = (int)member.PointsBalance,
                LoyaltyTier = member.Tier.ToString(),
                RecommendedAction = GetRecommendedAction(segment, churnRisk)
            });
        }

        return rfmScores;
    }

    public async Task<CustomerRFMScore?> GetCustomerRFMScoreAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today
        };

        var allScores = await CalculateRFMScoresAsync(parameters, cancellationToken);
        return allScores.FirstOrDefault(c => c.CustomerId == customerId);
    }

    public async Task<List<CustomerRFMScore>> GetCustomersBySegmentAsync(
        RFMSegment segment,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today
        };

        var allScores = await CalculateRFMScoresAsync(parameters, cancellationToken);
        var filtered = allScores.Where(c => c.Segment == segment).OrderByDescending(c => c.TotalSpent);

        return limit.HasValue ? filtered.Take(limit.Value).ToList() : filtered.ToList();
    }

    public async Task<CustomerLifetimeValueReport> GenerateCLVReportAsync(
        CLVParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var report = new CustomerLifetimeValueReport { GeneratedAt = DateTime.UtcNow };

        var rfmParams = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today,
            StoreId = parameters.StoreId
        };

        var rfmScores = await CalculateRFMScoresAsync(rfmParams, cancellationToken);
        var clvValues = rfmScores.Select(c => c.PredictedLifetimeValue).OrderBy(v => v).ToList();

        report.AverageCLV = clvValues.Any() ? clvValues.Average() : 0;
        report.MedianCLV = clvValues.Any() ? clvValues[clvValues.Count / 2] : 0;
        report.TotalPredictedCLV = clvValues.Sum();

        report.CLVDistribution = new List<CLVBracket>
        {
            CreateCLVBracket("Low", 0, 1000, clvValues),
            CreateCLVBracket("Medium", 1000, 5000, clvValues),
            CreateCLVBracket("High", 5000, 20000, clvValues),
            CreateCLVBracket("Premium", 20000, decimal.MaxValue, clvValues)
        };

        report.TopCustomers = rfmScores
            .OrderByDescending(c => c.PredictedLifetimeValue)
            .Take(parameters.TopCustomersCount)
            .Select(c => new CustomerCLVDetail
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                HistoricalValue = c.TotalSpent,
                PredictedFutureValue = c.PredictedLifetimeValue - c.TotalSpent,
                PurchaseCount = c.TotalPurchases,
                AverageOrderValue = c.AverageOrderValue,
                DaysSinceFirstPurchase = (int)(DateTime.Today - c.FirstPurchaseDate).TotalDays
            })
            .ToList();

        return report;
    }

    public async Task<CustomerCLVDetail> CalculateCustomerCLVAsync(
        int customerId,
        CLVParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var rfmScore = await GetCustomerRFMScoreAsync(customerId, cancellationToken);

        if (rfmScore == null)
            throw new ArgumentException($"Customer {customerId} not found");

        return new CustomerCLVDetail
        {
            CustomerId = rfmScore.CustomerId,
            CustomerName = rfmScore.CustomerName,
            HistoricalValue = rfmScore.TotalSpent,
            PredictedFutureValue = rfmScore.PredictedLifetimeValue - rfmScore.TotalSpent,
            PurchaseCount = rfmScore.TotalPurchases,
            AverageOrderValue = rfmScore.AverageOrderValue,
            DaysSinceFirstPurchase = (int)(DateTime.Today - rfmScore.FirstPurchaseDate).TotalDays
        };
    }

    public async Task<PurchaseFrequencyReport> GeneratePurchaseFrequencyReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new PurchaseFrequencyReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        var totalTransactions = orders.Count;

        // Day of week analysis
        var dayGroups = orders.GroupBy(o => o.CreatedAt.DayOfWeek);
        foreach (var dayGroup in dayGroups)
        {
            report.DayOfWeekPattern.Add(new DayOfWeekFrequency
            {
                DayOfWeek = dayGroup.Key,
                TransactionCount = dayGroup.Count(),
                Percentage = totalTransactions > 0
                    ? Math.Round((decimal)dayGroup.Count() / totalTransactions * 100, 2) : 0,
                AverageOrderValue = dayGroup.Any() ? dayGroup.Average(o => o.TotalAmount) : 0
            });
        }

        // Time of day analysis
        var hourGroups = orders.GroupBy(o => o.CreatedAt.Hour);
        foreach (var hourGroup in hourGroups)
        {
            report.TimeOfDayPattern.Add(new TimeOfDayFrequency
            {
                Hour = hourGroup.Key,
                TransactionCount = hourGroup.Count(),
                Percentage = totalTransactions > 0
                    ? Math.Round((decimal)hourGroup.Count() / totalTransactions * 100, 2) : 0,
                AverageOrderValue = hourGroup.Any() ? hourGroup.Average(o => o.TotalAmount) : 0
            });
        }

        return report;
    }

    public async Task<BasketAnalysisReport> GenerateBasketAnalysisReportAsync(
        BasketAnalysisParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new BasketAnalysisReport
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            GeneratedAt = DateTime.UtcNow
        };

        var orders = await context.Orders
            .Where(o => o.CreatedAt >= parameters.StartDate && o.CreatedAt <= parameters.EndDate)
            .Where(o => o.Status == OrderStatus.Served)
            .ToListAsync(cancellationToken);

        report.TotalTransactions = orders.Count;

        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product)
            .ToListAsync(cancellationToken);

        report.AverageBasketSize = orderItems.Any()
            ? Math.Round((decimal)orderItems.Sum(oi => oi.Quantity) / report.TotalTransactions, 2) : 0;
        report.AverageBasketValue = orders.Any()
            ? Math.Round(orders.Average(o => o.TotalAmount), 2) : 0;

        // Find frequently bought together pairs
        var orderProductGroups = orderItems
            .GroupBy(oi => oi.OrderId)
            .Where(g => g.Select(oi => oi.ProductId).Distinct().Count() >= 2)
            .Select(g => g.Select(oi => oi.ProductId).Distinct().ToList())
            .ToList();

        var pairCounts = new Dictionary<(int, int), int>();
        foreach (var orderProducts in orderProductGroups)
        {
            for (int i = 0; i < orderProducts.Count; i++)
            {
                for (int j = i + 1; j < orderProducts.Count; j++)
                {
                    var pair = (Math.Min(orderProducts[i], orderProducts[j]),
                               Math.Max(orderProducts[i], orderProducts[j]));
                    pairCounts[pair] = pairCounts.GetValueOrDefault(pair) + 1;
                }
            }
        }

        var allProductIds = pairCounts.Keys.SelectMany(p => new[] { p.Item1, p.Item2 }).Distinct().ToList();
        var products = await context.Products
            .Where(p => allProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        report.FrequentPairs = pairCounts
            .OrderByDescending(p => p.Value)
            .Take(parameters.TopPairsCount)
            .Select(p => new ProductPair
            {
                Product1Id = p.Key.Item1,
                Product1Name = products.GetValueOrDefault(p.Key.Item1, "Unknown"),
                Product2Id = p.Key.Item2,
                Product2Name = products.GetValueOrDefault(p.Key.Item2, "Unknown"),
                TimesTogetherCount = p.Value,
                Percentage = report.TotalTransactions > 0
                    ? Math.Round((decimal)p.Value / report.TotalTransactions * 100, 2) : 0
            })
            .ToList();

        return report;
    }

    public async Task<NewVsReturningReport> GenerateNewVsReturningReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var report = new NewVsReturningReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get loyalty transactions for the period
        var transactions = await context.Set<Core.Entities.LoyaltyTransaction>()
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .ToListAsync(cancellationToken);

        // Get first transaction date for each member
        var memberFirstDates = await context.Set<Core.Entities.LoyaltyTransaction>()
            .GroupBy(t => t.LoyaltyMemberId)
            .Select(g => new { MemberId = g.Key, FirstDate = g.Min(t => t.TransactionDate) })
            .ToDictionaryAsync(x => x.MemberId, x => x.FirstDate, cancellationToken);

        var newTransactions = transactions.Where(t =>
            memberFirstDates.TryGetValue(t.LoyaltyMemberId, out var firstDate) &&
            firstDate >= startDate).ToList();

        var returningTransactions = transactions.Except(newTransactions).ToList();

        report.NewCustomerCount = newTransactions.Select(t => t.LoyaltyMemberId).Distinct().Count();
        report.NewCustomerRevenue = newTransactions.Sum(t => t.MonetaryValue);
        report.NewCustomerAverageOrder = newTransactions.Any() ? newTransactions.Average(t => t.MonetaryValue) : 0;

        report.ReturningCustomerCount = returningTransactions.Select(t => t.LoyaltyMemberId).Distinct().Count();
        report.ReturningCustomerRevenue = returningTransactions.Sum(t => t.MonetaryValue);
        report.ReturningCustomerAverageOrder = returningTransactions.Any() ? returningTransactions.Average(t => t.MonetaryValue) : 0;

        return report;
    }

    public async Task<List<ChurnPrediction>> PredictChurnAsync(
        int? storeId = null,
        decimal minRiskScore = 0.5m,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today,
            StoreId = storeId
        };

        var rfmScores = await CalculateRFMScoresAsync(parameters, cancellationToken);

        var predictions = rfmScores
            .Where(c => c.ChurnRiskScore >= minRiskScore * 100)
            .Select(c => new ChurnPrediction
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                ChurnProbability = c.ChurnRiskScore / 100,
                RiskLevel = c.ChurnRiskLevel,
                HistoricalValue = c.TotalSpent,
                PotentialLostValue = c.PredictedLifetimeValue,
                DaysSinceLastPurchase = c.DaysSinceLastPurchase,
                RiskFactors = GetRiskFactors(c),
                RecommendedAction = c.RecommendedAction,
                SuggestedOfferValue = c.AverageOrderValue * 0.2m
            })
            .OrderByDescending(p => p.ChurnProbability * p.PotentialLostValue)
            .ToList();

        return limit.HasValue ? predictions.Take(limit.Value).ToList() : predictions;
    }

    public async Task<List<CustomerRFMScore>> GetAtRiskCustomersAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default)
    {
        var parameters = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today,
            StoreId = storeId
        };

        var rfmScores = await CalculateRFMScoresAsync(parameters, cancellationToken);

        return rfmScores
            .Where(c => c.Segment == RFMSegment.AtRisk ||
                       c.Segment == RFMSegment.CantLoseThem ||
                       c.ChurnRiskLevel == ChurnRiskLevel.High ||
                       c.ChurnRiskLevel == ChurnRiskLevel.Critical)
            .OrderByDescending(c => c.TotalSpent)
            .Take(limit ?? 50)
            .ToList();
    }

    public async Task<List<CustomerRFMScore>> GetHighValueCustomersAsync(
        int? storeId = null,
        int? limit = 50,
        CancellationToken cancellationToken = default)
    {
        var parameters = new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today,
            StoreId = storeId
        };

        var rfmScores = await CalculateRFMScoresAsync(parameters, cancellationToken);

        return rfmScores
            .Where(c => c.Segment == RFMSegment.Champion || c.Segment == RFMSegment.Loyal)
            .OrderByDescending(c => c.PredictedLifetimeValue)
            .Take(limit ?? 50)
            .ToList();
    }

    public async Task<List<RFMSegmentSummary>> GetSegmentSummariesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var report = await GenerateCustomerAnalyticsReportAsync(new RFMAnalysisParameters
        {
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today,
            StoreId = storeId
        }, cancellationToken);

        return report.SegmentSummaries;
    }

    public async Task<CustomerRetentionMetrics> GetRetentionMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var metrics = new CustomerRetentionMetrics
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var periodLength = endDate - startDate;
        var previousStart = startDate - periodLength;
        var previousEnd = startDate.AddDays(-1);

        var currentMembers = await context.Set<Core.Entities.LoyaltyTransaction>()
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .Select(t => t.LoyaltyMemberId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var previousMembers = await context.Set<Core.Entities.LoyaltyTransaction>()
            .Where(t => t.TransactionDate >= previousStart && t.TransactionDate <= previousEnd)
            .Select(t => t.LoyaltyMemberId)
            .Distinct()
            .ToListAsync(cancellationToken);

        metrics.StartingCustomers = previousMembers.Count;
        metrics.EndingCustomers = currentMembers.Count;

        var retainedMembers = currentMembers.Intersect(previousMembers).ToList();
        var newMembers = currentMembers.Except(previousMembers).ToList();
        var lostMembers = previousMembers.Except(currentMembers).ToList();

        metrics.NewCustomers = newMembers.Count;
        metrics.LostCustomers = lostMembers.Count;

        return metrics;
    }

    private static RFMSegment ClassifyCustomer(int r, int f, int m)
    {
        return (r, f, m) switch
        {
            (>= 4, >= 4, >= 4) => RFMSegment.Champion,
            (>= 3, >= 3, >= 3) when r + f + m >= 12 => RFMSegment.Loyal,
            (>= 3, >= 2, >= 2) when r >= 4 => RFMSegment.PotentialLoyalist,
            (>= 4, 1, _) => RFMSegment.NewCustomer,
            (>= 3, 1, _) => RFMSegment.Promising,
            (3, >= 3, >= 3) => RFMSegment.NeedsAttention,
            (2, >= 2, >= 2) => RFMSegment.AboutToSleep,
            (1, >= 4, >= 4) => RFMSegment.CantLoseThem,
            (<= 2, >= 3, >= 3) => RFMSegment.AtRisk,
            (<= 2, <= 2, _) => RFMSegment.Hibernating,
            _ when r == 1 => RFMSegment.Lost,
            _ => RFMSegment.Hibernating
        };
    }

    private static decimal CalculateChurnRisk(int r, int f, int m, int daysSinceLastPurchase)
    {
        var baseRisk = (5 - r) * 15 + (5 - f) * 10 + (5 - m) * 5;
        var timeRisk = daysSinceLastPurchase switch
        {
            > 180 => 30,
            > 90 => 20,
            > 60 => 10,
            > 30 => 5,
            _ => 0
        };
        return Math.Min(100, baseRisk + timeRisk);
    }

    private static string GetRecommendedAction(RFMSegment segment, decimal churnRisk) => segment switch
    {
        RFMSegment.Champion => "Reward with exclusive offers",
        RFMSegment.Loyal => "Upsell higher value products",
        RFMSegment.PotentialLoyalist => "Offer membership benefits",
        RFMSegment.NewCustomer => "Welcome series with incentive",
        RFMSegment.Promising => "Create brand awareness",
        RFMSegment.NeedsAttention => "Limited-time offers",
        RFMSegment.AboutToSleep => "Win back with personalized recommendations",
        RFMSegment.AtRisk => "Targeted win-back campaigns",
        RFMSegment.CantLoseThem => "Personal outreach with special offers",
        RFMSegment.Hibernating => "Reactivation with significant discount",
        RFMSegment.Lost => "Survey and win-back email",
        _ => "Monitor and engage"
    };

    private static string GetSegmentStrategy(RFMSegment segment) => segment switch
    {
        RFMSegment.Champion => "Exclusive rewards, early access",
        RFMSegment.Loyal => "Loyalty program upgrades",
        RFMSegment.PotentialLoyalist => "Membership enrollment",
        RFMSegment.NewCustomer => "Welcome series, onboarding",
        RFMSegment.Promising => "Value demonstration",
        RFMSegment.NeedsAttention => "Time-limited promotions",
        RFMSegment.AboutToSleep => "Reactivation offers",
        RFMSegment.AtRisk => "Win-back campaigns",
        RFMSegment.CantLoseThem => "VIP treatment restoration",
        RFMSegment.Hibernating => "Deep discounts",
        RFMSegment.Lost => "Exit surveys",
        _ => "Standard engagement"
    };

    private static List<string> GetRiskFactors(CustomerRFMScore customer)
    {
        var factors = new List<string>();
        if (customer.DaysSinceLastPurchase > 90) factors.Add($"No purchase in {customer.DaysSinceLastPurchase} days");
        if (customer.RecencyScore <= 2) factors.Add("Low recency score");
        if (customer.FrequencyScore <= 2) factors.Add("Low purchase frequency");
        return factors;
    }

    private static List<decimal> CalculatePercentiles(List<decimal> values)
    {
        if (!values.Any()) return new List<decimal> { 0, 0, 0, 0 };
        return new List<decimal>
        {
            values[(int)(values.Count * 0.2)],
            values[(int)(values.Count * 0.4)],
            values[(int)(values.Count * 0.6)],
            values[(int)(values.Count * 0.8)]
        };
    }

    private static int GetPercentileScore(decimal value, List<decimal> percentiles)
    {
        if (value >= percentiles[3]) return 5;
        if (value >= percentiles[2]) return 4;
        if (value >= percentiles[1]) return 3;
        if (value >= percentiles[0]) return 2;
        return 1;
    }

    private static CLVBracket CreateCLVBracket(string name, decimal min, decimal max, List<decimal> allValues)
    {
        var bracketValues = allValues.Where(v => v >= min && v < max).ToList();
        var totalValue = allValues.Sum();
        return new CLVBracket
        {
            BracketName = name,
            MinValue = min,
            MaxValue = max == decimal.MaxValue ? allValues.Max() : max,
            CustomerCount = bracketValues.Count,
            TotalValue = bracketValues.Sum(),
            PercentageOfCustomers = allValues.Count > 0
                ? Math.Round((decimal)bracketValues.Count / allValues.Count * 100, 2) : 0,
            PercentageOfValue = totalValue > 0
                ? Math.Round(bracketValues.Sum() / totalValue * 100, 2) : 0
        };
    }

    private static List<CustomerAnalyticsRecommendation> GenerateRecommendations(CustomerAnalyticsReport report)
    {
        var recommendations = new List<CustomerAnalyticsRecommendation>();

        if (report.ChurnAnalysis.CriticalRiskCount > 0)
        {
            recommendations.Add(new CustomerAnalyticsRecommendation
            {
                Title = "Critical Churn Risk Alert",
                Description = $"{report.ChurnAnalysis.CriticalRiskCount} high-value customers at critical risk",
                TargetSegment = RFMSegment.CantLoseThem,
                TargetCustomerCount = report.ChurnAnalysis.CriticalRiskCount,
                SuggestedAction = "Immediate personal outreach",
                Priority = 1
            });
        }

        var newCustomers = report.SegmentSummaries.FirstOrDefault(s => s.Segment == RFMSegment.NewCustomer);
        if (newCustomers != null && newCustomers.CustomerCount > 10)
        {
            recommendations.Add(new CustomerAnalyticsRecommendation
            {
                Title = "New Customer Conversion",
                Description = $"{newCustomers.CustomerCount} new customers to convert",
                TargetSegment = RFMSegment.NewCustomer,
                TargetCustomerCount = newCustomers.CustomerCount,
                SuggestedAction = "Launch welcome series",
                Priority = 2
            });
        }

        return recommendations;
    }
}
