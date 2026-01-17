// src/HospitalityPOS.Infrastructure/Services/WasteService.cs
// Implementation of waste and shrinkage tracking service
// Story 46-1: Waste and Shrinkage Tracking

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Inventory;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for waste and shrinkage tracking.
/// Handles waste recording, shrinkage calculation, variance analysis, and loss prevention alerts.
/// </summary>
public class WasteService : IWasteService
{
    // In-memory storage for demo
    private readonly List<WasteReason> _wasteReasons = new();
    private readonly List<WasteRecord> _wasteRecords = new();
    private readonly List<ShrinkageSnapshot> _shrinkageSnapshots = new();
    private readonly List<StockVarianceRecord> _stockVariances = new();
    private readonly List<LossPreventionAlert> _alerts = new();
    private readonly List<AlertRuleConfig> _alertRules = new();
    private WasteTrackingSettings _settings = new();
    private int _nextReasonId = 1;
    private int _nextRecordId = 1;
    private int _nextSnapshotId = 1;
    private int _nextVarianceId = 1;
    private int _nextAlertId = 1;
    private int _nextRuleId = 1;

    // Simulated product/inventory data
    private readonly Dictionary<int, (string Name, string? Sku, int CategoryId, string CategoryName, decimal UnitCost, decimal Stock)> _products = new();
    private readonly Dictionary<int, decimal> _stockHistory = new(); // Product ID -> stock value

    public WasteService()
    {
        InitializeDefaultWasteReasons();
        InitializeDefaultAlertRules();
        InitializeSampleProducts();
    }

    private void InitializeDefaultWasteReasons()
    {
        var defaults = new[]
        {
            new WasteReasonRequest { Name = "Expired", Description = "Product past expiry date", Category = WasteReasonCategory.Expiry, RequiresApproval = false },
            new WasteReasonRequest { Name = "Spoiled/Rotten", Description = "Product spoiled before expiry", Category = WasteReasonCategory.Expiry, RequiresApproval = false },
            new WasteReasonRequest { Name = "Damaged - Handling", Description = "Damaged during handling/storage", Category = WasteReasonCategory.Damage, RequiresApproval = false },
            new WasteReasonRequest { Name = "Damaged - Customer", Description = "Damaged by customer", Category = WasteReasonCategory.Damage, RequiresApproval = false },
            new WasteReasonRequest { Name = "Breakage", Description = "Broken/shattered product", Category = WasteReasonCategory.Damage, RequiresApproval = false },
            new WasteReasonRequest { Name = "Suspected Theft", Description = "Shrinkage suspected due to theft", Category = WasteReasonCategory.Theft, RequiresApproval = true },
            new WasteReasonRequest { Name = "Stock Count Variance", Description = "Variance from stock count", Category = WasteReasonCategory.Administrative, RequiresApproval = true },
            new WasteReasonRequest { Name = "Recalled Product", Description = "Product recalled by manufacturer", Category = WasteReasonCategory.Administrative, RequiresApproval = false },
            new WasteReasonRequest { Name = "Other", Description = "Other waste reason", Category = WasteReasonCategory.Other, RequiresApproval = true }
        };

        foreach (var request in defaults)
        {
            CreateWasteReasonAsync(request).GetAwaiter().GetResult();
        }
    }

    private void InitializeDefaultAlertRules()
    {
        _alertRules.AddRange(new[]
        {
            new AlertRuleConfig
            {
                Id = _nextRuleId++,
                AlertType = LossPreventionAlertType.HighValueWaste,
                Name = "High Value Waste Alert",
                Description = "Alert when waste exceeds value threshold",
                ValueThreshold = 5000m,
                DefaultSeverity = AlertSeverity.Warning,
                IsEnabled = true
            },
            new AlertRuleConfig
            {
                Id = _nextRuleId++,
                AlertType = LossPreventionAlertType.ThresholdExceeded,
                Name = "Shrinkage Threshold Alert",
                Description = "Alert when shrinkage exceeds target percentage",
                PercentThreshold = 1.5m,
                DefaultSeverity = AlertSeverity.Critical,
                IsEnabled = true
            },
            new AlertRuleConfig
            {
                Id = _nextRuleId++,
                AlertType = LossPreventionAlertType.UnusualVoidPattern,
                Name = "Unusual Void Pattern Alert",
                Description = "Alert on unusual void patterns by user",
                CountThreshold = 10,
                TimeWindowHours = 24,
                DefaultSeverity = AlertSeverity.Warning,
                IsEnabled = true
            },
            new AlertRuleConfig
            {
                Id = _nextRuleId++,
                AlertType = LossPreventionAlertType.RepeatedShrinkage,
                Name = "Repeated Shrinkage Alert",
                Description = "Alert on products with repeated shrinkage",
                CountThreshold = 3,
                TimeWindowHours = 168, // 1 week
                DefaultSeverity = AlertSeverity.Warning,
                IsEnabled = true
            },
            new AlertRuleConfig
            {
                Id = _nextRuleId++,
                AlertType = LossPreventionAlertType.StockVariance,
                Name = "Stock Variance Alert",
                Description = "Alert on significant stock variances",
                PercentThreshold = 5m,
                DefaultSeverity = AlertSeverity.Warning,
                IsEnabled = true
            }
        });
    }

    private void InitializeSampleProducts()
    {
        _products[1] = ("Milk 500ml", "MILK-500", 1, "Dairy", 65m, 100);
        _products[2] = ("Fresh Bread", "BRD-001", 2, "Bakery", 50m, 50);
        _products[3] = ("Bananas 1kg", "BAN-1KG", 3, "Fruits", 80m, 30);
        _products[4] = ("Eggs (Tray of 30)", "EGG-30", 1, "Dairy", 450m, 20);
        _products[5] = ("Chicken Wings 1kg", "CHK-WNG", 4, "Meat", 650m, 15);
    }

    #region Waste Reasons

    public Task<WasteReason> CreateWasteReasonAsync(WasteReasonRequest request)
    {
        var reason = new WasteReason
        {
            Id = _nextReasonId++,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            RequiresApproval = request.RequiresApproval,
            ApprovalThresholdValue = request.ApprovalThresholdValue,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SortOrder = _wasteReasons.Count + 1
        };

        _wasteReasons.Add(reason);
        return Task.FromResult(reason);
    }

    public Task<WasteReason> UpdateWasteReasonAsync(WasteReasonRequest request)
    {
        var reason = _wasteReasons.FirstOrDefault(r => r.Id == request.Id)
            ?? throw new InvalidOperationException($"Waste reason {request.Id} not found");

        reason.Name = request.Name;
        reason.Description = request.Description;
        reason.Category = request.Category;
        reason.RequiresApproval = request.RequiresApproval;
        reason.ApprovalThresholdValue = request.ApprovalThresholdValue;

        return Task.FromResult(reason);
    }

    public Task<bool> DeactivateWasteReasonAsync(int reasonId)
    {
        var reason = _wasteReasons.FirstOrDefault(r => r.Id == reasonId);
        if (reason == null) return Task.FromResult(false);

        reason.IsActive = false;
        return Task.FromResult(true);
    }

    public Task<WasteReason?> GetWasteReasonAsync(int reasonId)
    {
        var reason = _wasteReasons.FirstOrDefault(r => r.Id == reasonId);
        return Task.FromResult(reason);
    }

    public Task<IReadOnlyList<WasteReason>> GetActiveWasteReasonsAsync(WasteReasonCategory? category = null)
    {
        var query = _wasteReasons.Where(r => r.IsActive);
        if (category.HasValue)
            query = query.Where(r => r.Category == category.Value);

        return Task.FromResult<IReadOnlyList<WasteReason>>(query.OrderBy(r => r.SortOrder).ToList());
    }

    public Task<IReadOnlyList<WasteReason>> GetWasteReasonsWithStatsAsync(DateOnly startDate, DateOnly endDate)
    {
        var reasons = _wasteReasons.Select(r =>
        {
            var records = _wasteRecords.Where(w =>
                w.WasteReasonId == r.Id &&
                w.WasteDate >= startDate &&
                w.WasteDate <= endDate);

            r.RecordCount = records.Count();
            r.TotalValue = records.Sum(w => w.TotalValue);
            return r;
        }).ToList();

        return Task.FromResult<IReadOnlyList<WasteReason>>(reasons);
    }

    #endregion

    #region Waste Recording

    public Task<WasteResult> RecordWasteAsync(WasteRecordRequest request)
    {
        // Validate product exists
        if (!_products.TryGetValue(request.ProductId, out var product))
            return Task.FromResult(WasteResult.Failed($"Product {request.ProductId} not found"));

        // Validate waste reason
        var reason = _wasteReasons.FirstOrDefault(r => r.Id == request.WasteReasonId);
        if (reason == null)
            return Task.FromResult(WasteResult.Failed($"Waste reason {request.WasteReasonId} not found"));

        if (!reason.IsActive)
            return Task.FromResult(WasteResult.Failed("Waste reason is not active"));

        // Validate quantity
        if (request.Quantity <= 0)
            return Task.FromResult(WasteResult.Failed("Quantity must be greater than zero"));

        // Determine if approval is needed
        var totalValue = request.Quantity * product.UnitCost;
        var needsApproval = reason.RequiresApproval ||
            (_settings.RequireApprovalForAll) ||
            (totalValue >= _settings.ApprovalThresholdValue);

        var record = new WasteRecord
        {
            Id = _nextRecordId++,
            ProductId = request.ProductId,
            ProductName = product.Name,
            ProductSku = product.Sku,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            ProductBatchId = request.ProductBatchId,
            Quantity = request.Quantity,
            UnitCost = product.UnitCost,
            WasteReasonId = request.WasteReasonId,
            WasteReasonName = reason.Name,
            ReasonCategory = reason.Category,
            Notes = request.Notes,
            ImagePaths = request.ImagePaths ?? new List<string>(),
            RecordedByUserId = request.RecordedByUserId,
            RecordedByName = $"User {request.RecordedByUserId}",
            RecordedAt = DateTime.UtcNow,
            Status = needsApproval ? WasteRecordStatus.PendingApproval : WasteRecordStatus.Recorded,
            WasteDate = request.WasteDate ?? DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.UtcNow
        };

        _wasteRecords.Add(record);

        // Deduct stock if not pending approval and auto-deduct enabled
        if (!needsApproval && _settings.AutoDeductStock)
        {
            // In real implementation, would call inventory service
            _products[request.ProductId] = (product.Name, product.Sku, product.CategoryId,
                product.CategoryName, product.UnitCost, product.Stock - request.Quantity);
        }

        // Raise event
        OnWasteRecorded(new WasteEventArgs(record, "Recorded"));

        // Check for high-value waste alert
        if (totalValue >= 5000m)
        {
            CheckAndCreateHighValueAlert(record);
        }

        // Check for repeated shrinkage
        CheckAndCreateRepeatedShrinkageAlert(request.ProductId);

        var warnings = new List<string>();
        if (needsApproval)
            warnings.Add("This waste record requires manager approval");

        return Task.FromResult(new WasteResult
        {
            Success = true,
            Message = needsApproval ? "Waste recorded, pending approval" : "Waste recorded successfully",
            Record = record,
            Warnings = warnings
        });
    }

    public async Task<IReadOnlyList<WasteResult>> RecordBatchWasteAsync(IEnumerable<WasteRecordRequest> requests)
    {
        var results = new List<WasteResult>();
        foreach (var request in requests)
        {
            results.Add(await RecordWasteAsync(request));
        }
        return results;
    }

    public Task<WasteResult> ProcessApprovalAsync(WasteApprovalRequest request)
    {
        var record = _wasteRecords.FirstOrDefault(r => r.Id == request.WasteRecordId);
        if (record == null)
            return Task.FromResult(WasteResult.Failed($"Waste record {request.WasteRecordId} not found"));

        if (record.Status != WasteRecordStatus.PendingApproval)
            return Task.FromResult(WasteResult.Failed("Record is not pending approval"));

        record.ApprovedByUserId = request.ApproverUserId;
        record.ApprovedByName = $"User {request.ApproverUserId}";
        record.ApprovedAt = DateTime.UtcNow;
        record.ApprovalNotes = request.Notes;

        if (request.Approve)
        {
            record.Status = WasteRecordStatus.Approved;

            // Deduct stock
            if (_settings.AutoDeductStock && _products.TryGetValue(record.ProductId, out var product))
            {
                _products[record.ProductId] = (product.Name, product.Sku, product.CategoryId,
                    product.CategoryName, product.UnitCost, product.Stock - record.Quantity);
            }

            OnWasteApproved(new WasteEventArgs(record, "Approved"));
            return Task.FromResult(WasteResult.Succeeded(record, "Waste approved"));
        }
        else
        {
            record.Status = WasteRecordStatus.Rejected;
            OnWasteRejected(new WasteEventArgs(record, "Rejected"));
            return Task.FromResult(WasteResult.Succeeded(record, "Waste rejected"));
        }
    }

    public Task<WasteResult> ReverseWasteAsync(int recordId, int userId, string reason)
    {
        var record = _wasteRecords.FirstOrDefault(r => r.Id == recordId);
        if (record == null)
            return Task.FromResult(WasteResult.Failed($"Waste record {recordId} not found"));

        if (record.Status == WasteRecordStatus.Reversed)
            return Task.FromResult(WasteResult.Failed("Record is already reversed"));

        // Restore stock
        if (_products.TryGetValue(record.ProductId, out var product))
        {
            _products[record.ProductId] = (product.Name, product.Sku, product.CategoryId,
                product.CategoryName, product.UnitCost, product.Stock + record.Quantity);
        }

        record.Status = WasteRecordStatus.Reversed;
        record.ApprovalNotes = $"Reversed by User {userId}: {reason}";

        OnWasteReversed(new WasteEventArgs(record, "Reversed"));
        return Task.FromResult(WasteResult.Succeeded(record, "Waste record reversed"));
    }

    public Task<WasteRecord?> GetWasteRecordAsync(int recordId)
    {
        var record = _wasteRecords.FirstOrDefault(r => r.Id == recordId);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<WasteRecord>> GetPendingApprovalsAsync()
    {
        var pending = _wasteRecords
            .Where(r => r.Status == WasteRecordStatus.PendingApproval)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<WasteRecord>>(pending);
    }

    public Task<IReadOnlyList<WasteRecord>> GetWasteRecordsAsync(
        DateOnly startDate, DateOnly endDate, int? categoryId = null, WasteReasonCategory? reasonCategory = null)
    {
        var query = _wasteRecords.Where(r => r.WasteDate >= startDate && r.WasteDate <= endDate);

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        if (reasonCategory.HasValue)
            query = query.Where(r => r.ReasonCategory == reasonCategory.Value);

        return Task.FromResult<IReadOnlyList<WasteRecord>>(query.OrderByDescending(r => r.WasteDate).ToList());
    }

    public Task<IReadOnlyList<WasteRecord>> GetProductWasteRecordsAsync(
        int productId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _wasteRecords.Where(r => r.ProductId == productId);

        if (startDate.HasValue)
            query = query.Where(r => r.WasteDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.WasteDate <= endDate.Value);

        return Task.FromResult<IReadOnlyList<WasteRecord>>(query.OrderByDescending(r => r.WasteDate).ToList());
    }

    #endregion

    #region Shrinkage Calculation

    public Task<ShrinkageMetrics> CalculateShrinkageAsync(DateOnly startDate, DateOnly endDate, int? categoryId = null)
    {
        // Simulated calculation
        var wasteRecords = _wasteRecords.Where(r => r.WasteDate >= startDate && r.WasteDate <= endDate);
        if (categoryId.HasValue)
            wasteRecords = wasteRecords.Where(r => r.CategoryId == categoryId.Value);

        var wasteValue = wasteRecords.Sum(r => r.TotalValue);

        // Simulated financial values
        var openingStock = 500000m;
        var purchases = 200000m;
        var sales = 180000m; // Cost of goods sold
        var expectedClosing = openingStock + purchases - sales;
        var actualClosing = expectedClosing - wasteValue - 5000m; // 5000 unexplained

        var metrics = new ShrinkageMetrics
        {
            StartDate = startDate,
            EndDate = endDate,
            CategoryId = categoryId,
            CategoryName = categoryId.HasValue ? _products.Values.FirstOrDefault(p => p.CategoryId == categoryId)?.CategoryName : null,
            OpeningStockValue = openingStock,
            PurchasesValue = purchases,
            SalesValue = sales,
            ExpectedClosingValue = expectedClosing,
            ActualClosingValue = actualClosing,
            WasteValue = wasteValue,
            TargetPercent = _settings.TargetShrinkagePercent
        };

        return Task.FromResult(metrics);
    }

    public Task<IReadOnlyList<ShrinkageSnapshot>> CreateShrinkageSnapshotAsync(
        DateOnly date, int? productId = null, int? categoryId = null)
    {
        var snapshots = new List<ShrinkageSnapshot>();
        var products = _products.AsEnumerable();

        if (productId.HasValue)
            products = products.Where(p => p.Key == productId.Value);
        if (categoryId.HasValue)
            products = products.Where(p => p.Value.CategoryId == categoryId.Value);

        foreach (var (id, product) in products)
        {
            // Simulated expected vs actual
            var expectedStock = product.Stock + 10; // Simulated expected
            var snapshot = new ShrinkageSnapshot
            {
                Id = _nextSnapshotId++,
                SnapshotDate = date,
                ProductId = id,
                ProductName = product.Name,
                CategoryId = product.CategoryId,
                CategoryName = product.CategoryName,
                ExpectedStock = expectedStock,
                ActualStock = product.Stock,
                VarianceValue = (expectedStock - product.Stock) * product.UnitCost,
                CreatedAt = DateTime.UtcNow
            };
            snapshots.Add(snapshot);
            _shrinkageSnapshots.Add(snapshot);
        }

        return Task.FromResult<IReadOnlyList<ShrinkageSnapshot>>(snapshots);
    }

    public Task<IReadOnlyList<ShrinkageSnapshot>> GetShrinkageSnapshotsAsync(DateOnly startDate, DateOnly endDate)
    {
        var snapshots = _shrinkageSnapshots
            .Where(s => s.SnapshotDate >= startDate && s.SnapshotDate <= endDate)
            .OrderByDescending(s => s.SnapshotDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<ShrinkageSnapshot>>(snapshots);
    }

    public Task<IReadOnlyList<MonthlyShrinkage>> GetShrinkageTrendAsync(int months = 12)
    {
        var trend = new List<MonthlyShrinkage>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = today.AddMonths(-i);
            monthStart = new DateOnly(monthStart.Year, monthStart.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var wasteRecords = _wasteRecords.Where(r => r.WasteDate >= monthStart && r.WasteDate <= monthEnd);
            var wasteValue = wasteRecords.Sum(r => r.TotalValue);

            // Simulated metrics
            trend.Add(new MonthlyShrinkage
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                MonthName = monthStart.ToString("MMMM yyyy"),
                ShrinkageValue = wasteValue + 5000m, // Include unexplained
                ShrinkagePercent = 1.2m + (i * 0.1m), // Simulated trend
                WasteValue = wasteValue,
                WasteRecordCount = wasteRecords.Count()
            });
        }

        return Task.FromResult<IReadOnlyList<MonthlyShrinkage>>(trend);
    }

    #endregion

    #region Stock Variance

    public Task<StockVarianceRecord> RecordVarianceAsync(
        int stockTakeId, int productId, decimal systemQuantity, decimal countedQuantity, decimal unitCost)
    {
        var product = _products.GetValueOrDefault(productId);

        var variance = new StockVarianceRecord
        {
            Id = _nextVarianceId++,
            StockTakeId = stockTakeId,
            CountDate = DateOnly.FromDateTime(DateTime.Today),
            ProductId = productId,
            ProductName = product.Name ?? $"Product {productId}",
            ProductSku = product.Sku,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            SystemQuantity = systemQuantity,
            CountedQuantity = countedQuantity,
            UnitCost = unitCost,
            IsSignificant = Math.Abs((systemQuantity - countedQuantity) / systemQuantity * 100) >= _settings.SignificantVarianceThreshold,
            InvestigationStatus = VarianceInvestigationStatus.Pending
        };

        _stockVariances.Add(variance);

        // Check for significant variance alert
        if (variance.IsSignificant)
        {
            CreateAlertAsync(
                LossPreventionAlertType.StockVariance,
                "Significant Stock Variance Detected",
                $"Product '{variance.ProductName}' has {variance.VariancePercent:F1}% variance (System: {systemQuantity}, Counted: {countedQuantity})",
                AlertSeverity.Warning,
                productId: productId,
                value: Math.Abs(variance.VarianceValue),
                threshold: _settings.SignificantVarianceThreshold
            ).GetAwaiter().GetResult();
        }

        return Task.FromResult(variance);
    }

    public Task<IReadOnlyList<StockVarianceRecord>> GetStockTakeVariancesAsync(int stockTakeId)
    {
        var variances = _stockVariances.Where(v => v.StockTakeId == stockTakeId).ToList();
        return Task.FromResult<IReadOnlyList<StockVarianceRecord>>(variances);
    }

    public Task<IReadOnlyList<StockVarianceRecord>> GetSignificantVariancesAsync(
        DateOnly startDate, DateOnly endDate, decimal minVariancePercent = 5m)
    {
        var variances = _stockVariances
            .Where(v => v.CountDate >= startDate && v.CountDate <= endDate)
            .Where(v => Math.Abs(v.VariancePercent) >= minVariancePercent)
            .OrderByDescending(v => Math.Abs(v.VarianceValue))
            .ToList();
        return Task.FromResult<IReadOnlyList<StockVarianceRecord>>(variances);
    }

    public Task<StockVarianceRecord> UpdateVarianceInvestigationAsync(
        int varianceId, VarianceInvestigationStatus status, string? notes)
    {
        var variance = _stockVariances.FirstOrDefault(v => v.Id == varianceId)
            ?? throw new InvalidOperationException($"Variance {varianceId} not found");

        variance.InvestigationStatus = status;
        variance.InvestigationNotes = notes;

        return Task.FromResult(variance);
    }

    public async Task<WasteResult> CreateWasteFromVarianceAsync(int varianceId, int wasteReasonId, int userId, string? notes)
    {
        var variance = _stockVariances.FirstOrDefault(v => v.Id == varianceId)
            ?? throw new InvalidOperationException($"Variance {varianceId} not found");

        if (variance.WasteRecordId.HasValue)
            return WasteResult.Failed("Waste record already created for this variance");

        var quantity = variance.SystemQuantity - variance.CountedQuantity;
        if (quantity <= 0)
            return WasteResult.Failed("No positive variance to record as waste");

        var result = await RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = variance.ProductId,
            Quantity = quantity,
            WasteReasonId = wasteReasonId,
            Notes = notes ?? $"Created from stock variance {varianceId}: {variance.InvestigationNotes}",
            RecordedByUserId = userId
        });

        if (result.Success && result.Record != null)
        {
            variance.WasteRecordId = result.Record.Id;
            result.Record.VarianceRecordId = varianceId;
            variance.InvestigationStatus = VarianceInvestigationStatus.Resolved;
        }

        return result;
    }

    #endregion

    #region Reports

    public async Task<WasteReport> GenerateWasteReportAsync(DateOnly startDate, DateOnly endDate, int? categoryId = null)
    {
        var records = await GetWasteRecordsAsync(startDate, endDate, categoryId);
        var effectiveRecords = records.Where(r => r.Status == WasteRecordStatus.Recorded || r.Status == WasteRecordStatus.Approved).ToList();

        var report = new WasteReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedDate = DateOnly.FromDateTime(DateTime.Today),
            CategoryFilter = categoryId,
            Records = effectiveRecords,
            TotalRecords = effectiveRecords.Count,
            TotalQuantity = effectiveRecords.Sum(r => r.Quantity),
            TotalValue = effectiveRecords.Sum(r => r.TotalValue)
        };

        // By reason
        report.ByReason = effectiveRecords
            .GroupBy(r => new { r.WasteReasonId, r.WasteReasonName, r.ReasonCategory })
            .Select(g => new WasteByReason
            {
                WasteReasonId = g.Key.WasteReasonId,
                WasteReasonName = g.Key.WasteReasonName,
                Category = g.Key.ReasonCategory,
                RecordCount = g.Count(),
                TotalQuantity = g.Sum(r => r.Quantity),
                TotalValue = g.Sum(r => r.TotalValue),
                PercentOfTotal = report.TotalValue > 0 ? g.Sum(r => r.TotalValue) / report.TotalValue * 100 : 0
            })
            .OrderByDescending(r => r.TotalValue)
            .ToList();

        // By category
        report.ByCategory = effectiveRecords
            .Where(r => r.CategoryId.HasValue)
            .GroupBy(r => new { r.CategoryId, r.CategoryName })
            .Select(g => new WasteByCategory
            {
                CategoryId = g.Key.CategoryId!.Value,
                CategoryName = g.Key.CategoryName ?? "Unknown",
                RecordCount = g.Count(),
                TotalQuantity = g.Sum(r => r.Quantity),
                TotalValue = g.Sum(r => r.TotalValue),
                PercentOfTotal = report.TotalValue > 0 ? g.Sum(r => r.TotalValue) / report.TotalValue * 100 : 0
            })
            .OrderByDescending(c => c.TotalValue)
            .ToList();

        // By product
        report.ByProduct = effectiveRecords
            .GroupBy(r => new { r.ProductId, r.ProductName, r.ProductSku, r.CategoryName, r.UnitName })
            .Select(g => new WasteByProduct
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductSku = g.Key.ProductSku,
                CategoryName = g.Key.CategoryName,
                RecordCount = g.Count(),
                TotalQuantity = g.Sum(r => r.Quantity),
                UnitName = g.Key.UnitName,
                TotalValue = g.Sum(r => r.TotalValue),
                PercentOfTotal = report.TotalValue > 0 ? g.Sum(r => r.TotalValue) / report.TotalValue * 100 : 0,
                PrimaryReason = g.GroupBy(r => r.WasteReasonName).OrderByDescending(x => x.Count()).First().Key
            })
            .OrderByDescending(p => p.TotalValue)
            .Take(20)
            .ToList();

        // By day
        report.ByDay = effectiveRecords
            .GroupBy(r => r.WasteDate)
            .Select(g => new DailyWaste
            {
                Date = g.Key,
                RecordCount = g.Count(),
                TotalValue = g.Sum(r => r.TotalValue)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return report;
    }

    public async Task<ShrinkageReport> GenerateShrinkageReportAsync(DateOnly startDate, DateOnly endDate)
    {
        var metrics = await CalculateShrinkageAsync(startDate, endDate);
        var trend = await GetShrinkageTrendAsync(12);
        var topProducts = await GetTopShrinkageProductsAsync(startDate, endDate, 10);
        var variances = await GetSignificantVariancesAsync(startDate, endDate, _settings.SignificantVarianceThreshold);

        // By category
        var byCategory = new List<ShrinkageByCategory>();
        foreach (var catGroup in _products.GroupBy(p => p.Value.CategoryId))
        {
            var catMetrics = await CalculateShrinkageAsync(startDate, endDate, catGroup.Key);
            byCategory.Add(new ShrinkageByCategory
            {
                CategoryId = catGroup.Key,
                CategoryName = catGroup.First().Value.CategoryName,
                ShrinkageValue = catMetrics.ShrinkageValue,
                ShrinkagePercent = catMetrics.ShrinkagePercent,
                WasteValue = catMetrics.WasteValue,
                UnexplainedVariance = catMetrics.UnexplainedVariance,
                ProductCount = catGroup.Count()
            });
        }

        return new ShrinkageReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedDate = DateOnly.FromDateTime(DateTime.Today),
            OverallMetrics = metrics,
            ByCategory = byCategory,
            TopShrinkageProducts = topProducts.ToList(),
            MonthlyTrend = trend.ToList(),
            SignificantVariances = variances.ToList(),
            IndustryBenchmark = 1.5m
        };
    }

    public Task<IReadOnlyList<ShrinkageByProduct>> GetTopShrinkageProductsAsync(
        DateOnly startDate, DateOnly endDate, int limit = 10)
    {
        var products = _wasteRecords
            .Where(r => r.WasteDate >= startDate && r.WasteDate <= endDate)
            .Where(r => r.Status == WasteRecordStatus.Recorded || r.Status == WasteRecordStatus.Approved)
            .GroupBy(r => new { r.ProductId, r.ProductName, r.ProductSku, r.CategoryName })
            .Select(g => new ShrinkageByProduct
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductSku = g.Key.ProductSku,
                CategoryName = g.Key.CategoryName,
                ShrinkageValue = g.Sum(r => r.TotalValue),
                ShrinkagePercent = 0, // Would need more data to calculate properly
                WasteCount = g.Count(),
                MostCommonReason = g.GroupBy(r => r.WasteReasonName).OrderByDescending(x => x.Count()).First().Key
            })
            .OrderByDescending(p => p.ShrinkageValue)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShrinkageByProduct>>(products);
    }

    public Task<IReadOnlyList<WasteByReason>> GetWasteByReasonAsync(DateOnly startDate, DateOnly endDate)
    {
        var records = _wasteRecords
            .Where(r => r.WasteDate >= startDate && r.WasteDate <= endDate)
            .Where(r => r.Status == WasteRecordStatus.Recorded || r.Status == WasteRecordStatus.Approved);

        var totalValue = records.Sum(r => r.TotalValue);

        var byReason = records
            .GroupBy(r => new { r.WasteReasonId, r.WasteReasonName, r.ReasonCategory })
            .Select(g => new WasteByReason
            {
                WasteReasonId = g.Key.WasteReasonId,
                WasteReasonName = g.Key.WasteReasonName,
                Category = g.Key.ReasonCategory,
                RecordCount = g.Count(),
                TotalQuantity = g.Sum(r => r.Quantity),
                TotalValue = g.Sum(r => r.TotalValue),
                PercentOfTotal = totalValue > 0 ? g.Sum(r => r.TotalValue) / totalValue * 100 : 0
            })
            .OrderByDescending(r => r.TotalValue)
            .ToList();

        return Task.FromResult<IReadOnlyList<WasteByReason>>(byReason);
    }

    public Task<IReadOnlyList<WasteByCategory>> GetWasteByCategoryAsync(DateOnly startDate, DateOnly endDate)
    {
        var records = _wasteRecords
            .Where(r => r.WasteDate >= startDate && r.WasteDate <= endDate)
            .Where(r => r.Status == WasteRecordStatus.Recorded || r.Status == WasteRecordStatus.Approved)
            .Where(r => r.CategoryId.HasValue);

        var totalValue = records.Sum(r => r.TotalValue);

        var byCategory = records
            .GroupBy(r => new { r.CategoryId, r.CategoryName })
            .Select(g => new WasteByCategory
            {
                CategoryId = g.Key.CategoryId!.Value,
                CategoryName = g.Key.CategoryName ?? "Unknown",
                RecordCount = g.Count(),
                TotalQuantity = g.Sum(r => r.Quantity),
                TotalValue = g.Sum(r => r.TotalValue),
                PercentOfTotal = totalValue > 0 ? g.Sum(r => r.TotalValue) / totalValue * 100 : 0
            })
            .OrderByDescending(c => c.TotalValue)
            .ToList();

        return Task.FromResult<IReadOnlyList<WasteByCategory>>(byCategory);
    }

    #endregion

    #region Dashboard

    public async Task<ShrinkageDashboard> GetDashboardAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var prevMonthStart = monthStart.AddMonths(-1);
        var prevMonthEnd = monthStart.AddDays(-1);

        var currentMetrics = await CalculateShrinkageAsync(monthStart, monthEnd);
        var prevMetrics = await CalculateShrinkageAsync(prevMonthStart, prevMonthEnd);

        var topProducts = await GetTopShrinkageProductsAsync(monthStart, monthEnd, 5);
        var wasteByReason = await GetWasteByReasonAsync(monthStart, monthEnd);

        // Trend data
        var trend = new List<ShrinkageTrendPoint>();
        for (int i = 11; i >= 0; i--)
        {
            var start = today.AddDays(-i * 7);
            var end = start.AddDays(6);
            var weekRecords = _wasteRecords.Where(r => r.WasteDate >= start && r.WasteDate <= end);
            trend.Add(new ShrinkageTrendPoint
            {
                Date = start,
                Label = $"Week {12 - i}",
                ShrinkagePercent = 1.2m + (i * 0.05m), // Simulated
                ShrinkageValue = weekRecords.Sum(r => r.TotalValue)
            });
        }

        return new ShrinkageDashboard
        {
            AsOfDate = today,
            CurrentMonthShrinkagePercent = currentMetrics.ShrinkagePercent,
            CurrentMonthShrinkageValue = currentMetrics.ShrinkageValue,
            TargetPercent = _settings.TargetShrinkagePercent,
            PreviousMonthShrinkagePercent = prevMetrics.ShrinkagePercent,
            TopLossItems = topProducts.Select(p => new TopShrinkageItem
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                TotalLossValue = p.ShrinkageValue,
                IncidentCount = (int)p.WasteCount
            }).ToList(),
            WasteByReasonSummary = wasteByReason.ToList(),
            TrendData = trend,
            ActiveAlerts = _alerts.Count(a => !a.IsAcknowledged)
        };
    }

    public Task<IReadOnlyList<DailyWaste>> GetDailyWasteTotalsAsync(DateOnly startDate, DateOnly endDate)
    {
        var dailyTotals = new List<DailyWaste>();
        var current = startDate;
        while (current <= endDate)
        {
            var dayRecords = _wasteRecords.Where(r => r.WasteDate == current);
            dailyTotals.Add(new DailyWaste
            {
                Date = current,
                RecordCount = dayRecords.Count(),
                TotalValue = dayRecords.Sum(r => r.TotalValue)
            });
            current = current.AddDays(1);
        }
        return Task.FromResult<IReadOnlyList<DailyWaste>>(dailyTotals);
    }

    public Task<IReadOnlyList<ShrinkageTrendPoint>> GetShrinkageTrendDataAsync(int weeks = 12)
    {
        var trend = new List<ShrinkageTrendPoint>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int i = weeks - 1; i >= 0; i--)
        {
            var start = today.AddDays(-i * 7);
            var end = start.AddDays(6);
            var weekRecords = _wasteRecords.Where(r => r.WasteDate >= start && r.WasteDate <= end);
            trend.Add(new ShrinkageTrendPoint
            {
                Date = start,
                Label = start.ToString("MMM dd"),
                ShrinkagePercent = 1.2m + (i * 0.05m), // Simulated
                ShrinkageValue = weekRecords.Sum(r => r.TotalValue) + 5000m // Include unexplained
            });
        }

        return Task.FromResult<IReadOnlyList<ShrinkageTrendPoint>>(trend);
    }

    #endregion

    #region Alerts

    public Task<LossPreventionAlert> CreateAlertAsync(
        LossPreventionAlertType alertType, string title, string message, AlertSeverity severity = AlertSeverity.Warning,
        int? productId = null, int? wasteRecordId = null, int? userId = null, decimal? value = null, decimal? threshold = null)
    {
        var alert = new LossPreventionAlert
        {
            Id = _nextAlertId++,
            AlertType = alertType,
            Severity = severity,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            ProductId = productId,
            ProductName = productId.HasValue ? _products.GetValueOrDefault(productId.Value).Name : null,
            WasteRecordId = wasteRecordId,
            UserId = userId,
            UserName = userId.HasValue ? $"User {userId}" : null,
            Value = value,
            Threshold = threshold
        };

        _alerts.Add(alert);
        OnAlertCreated(new AlertEventArgs(alert));

        return Task.FromResult(alert);
    }

    public Task<IReadOnlyList<LossPreventionAlert>> GetActiveAlertsAsync(bool includeAcknowledged = false)
    {
        var alerts = _alerts.Where(a => includeAcknowledged || !a.IsAcknowledged)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<LossPreventionAlert>>(alerts);
    }

    public Task<IReadOnlyList<LossPreventionAlert>> GetAlertsAsync(
        DateOnly startDate, DateOnly endDate, LossPreventionAlertType? alertType = null)
    {
        var query = _alerts.Where(a =>
            DateOnly.FromDateTime(a.CreatedAt) >= startDate &&
            DateOnly.FromDateTime(a.CreatedAt) <= endDate);

        if (alertType.HasValue)
            query = query.Where(a => a.AlertType == alertType.Value);

        return Task.FromResult<IReadOnlyList<LossPreventionAlert>>(query.OrderByDescending(a => a.CreatedAt).ToList());
    }

    public Task<bool> AcknowledgeAlertAsync(int alertId, int userId)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null) return Task.FromResult(false);

        alert.IsAcknowledged = true;
        alert.AcknowledgedByUserId = userId;
        alert.AcknowledgedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<(int UserId, string UserName, int VoidCount)>> CheckUnusualVoidPatternsAsync(DateOnly date)
    {
        // Simulated void check - in real implementation would check void records
        var results = new List<(int, string, int)>
        {
            (1, "User 1", 3),
            (2, "User 2", 8),
            (3, "User 3", 12) // Exceeds threshold
        };

        return Task.FromResult<IReadOnlyList<(int, string, int)>>(
            results.Where(r => r.Item3 >= _settings.VoidCountThreshold).ToList());
    }

    public async Task<int> RunAlertChecksAsync()
    {
        var alertsCreated = 0;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Check for unusual void patterns
        var voidPatterns = await CheckUnusualVoidPatternsAsync(today);
        foreach (var (userId, userName, voidCount) in voidPatterns)
        {
            await CreateAlertAsync(
                LossPreventionAlertType.UnusualVoidPattern,
                "Unusual Void Pattern",
                $"User '{userName}' has {voidCount} voids today, exceeding threshold of {_settings.VoidCountThreshold}",
                AlertSeverity.Warning,
                userId: userId,
                value: voidCount,
                threshold: _settings.VoidCountThreshold);
            alertsCreated++;
        }

        // Check shrinkage threshold
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var metrics = await CalculateShrinkageAsync(monthStart, today);
        if (metrics.ExceedsTarget)
        {
            await CreateAlertAsync(
                LossPreventionAlertType.ThresholdExceeded,
                "Shrinkage Target Exceeded",
                $"Current shrinkage of {metrics.ShrinkagePercent:F1}% exceeds target of {metrics.TargetPercent:F1}%",
                AlertSeverity.Critical,
                value: metrics.ShrinkagePercent,
                threshold: metrics.TargetPercent);
            alertsCreated++;
        }

        return alertsCreated;
    }

    private void CheckAndCreateHighValueAlert(WasteRecord record)
    {
        var rule = _alertRules.FirstOrDefault(r =>
            r.AlertType == LossPreventionAlertType.HighValueWaste && r.IsEnabled);

        if (rule != null && record.TotalValue >= (rule.ValueThreshold ?? 5000m))
        {
            CreateAlertAsync(
                LossPreventionAlertType.HighValueWaste,
                "High Value Waste Recorded",
                $"Waste of KSh {record.TotalValue:N0} recorded for '{record.ProductName}'",
                rule.DefaultSeverity,
                productId: record.ProductId,
                wasteRecordId: record.Id,
                value: record.TotalValue,
                threshold: rule.ValueThreshold
            ).GetAwaiter().GetResult();
        }
    }

    private void CheckAndCreateRepeatedShrinkageAlert(int productId)
    {
        var rule = _alertRules.FirstOrDefault(r =>
            r.AlertType == LossPreventionAlertType.RepeatedShrinkage && r.IsEnabled);

        if (rule == null) return;

        var cutoffTime = DateTime.UtcNow.AddHours(-(rule.TimeWindowHours ?? 168));
        var recentCount = _wasteRecords.Count(r =>
            r.ProductId == productId &&
            r.CreatedAt >= cutoffTime);

        if (recentCount >= (rule.CountThreshold ?? 3))
        {
            var product = _products.GetValueOrDefault(productId);
            CreateAlertAsync(
                LossPreventionAlertType.RepeatedShrinkage,
                "Repeated Shrinkage on Product",
                $"Product '{product.Name}' has {recentCount} waste records in the past week",
                rule.DefaultSeverity,
                productId: productId,
                value: recentCount,
                threshold: rule.CountThreshold
            ).GetAwaiter().GetResult();
        }
    }

    #endregion

    #region Alert Rules

    public Task<IReadOnlyList<AlertRuleConfig>> GetAlertRulesAsync()
    {
        return Task.FromResult<IReadOnlyList<AlertRuleConfig>>(_alertRules.ToList());
    }

    public Task<AlertRuleConfig> UpdateAlertRuleAsync(AlertRuleConfig rule)
    {
        var existing = _alertRules.FirstOrDefault(r => r.Id == rule.Id)
            ?? throw new InvalidOperationException($"Alert rule {rule.Id} not found");

        existing.Name = rule.Name;
        existing.Description = rule.Description;
        existing.IsEnabled = rule.IsEnabled;
        existing.ValueThreshold = rule.ValueThreshold;
        existing.PercentThreshold = rule.PercentThreshold;
        existing.CountThreshold = rule.CountThreshold;
        existing.TimeWindowHours = rule.TimeWindowHours;
        existing.DefaultSeverity = rule.DefaultSeverity;
        existing.NotifyManager = rule.NotifyManager;
        existing.NotifyOwner = rule.NotifyOwner;

        return Task.FromResult(existing);
    }

    public Task<bool> SetAlertRuleEnabledAsync(int ruleId, bool enabled)
    {
        var rule = _alertRules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null) return Task.FromResult(false);

        rule.IsEnabled = enabled;
        return Task.FromResult(true);
    }

    #endregion

    #region Settings

    public Task<WasteTrackingSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<WasteTrackingSettings> UpdateSettingsAsync(WasteTrackingSettings settings)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion

    #region Events

    public event EventHandler<WasteEventArgs>? WasteRecorded;
    public event EventHandler<WasteEventArgs>? WasteApproved;
    public event EventHandler<WasteEventArgs>? WasteRejected;
    public event EventHandler<WasteEventArgs>? WasteReversed;
    public event EventHandler<AlertEventArgs>? AlertCreated;

    protected virtual void OnWasteRecorded(WasteEventArgs e) => WasteRecorded?.Invoke(this, e);
    protected virtual void OnWasteApproved(WasteEventArgs e) => WasteApproved?.Invoke(this, e);
    protected virtual void OnWasteRejected(WasteEventArgs e) => WasteRejected?.Invoke(this, e);
    protected virtual void OnWasteReversed(WasteEventArgs e) => WasteReversed?.Invoke(this, e);
    protected virtual void OnAlertCreated(AlertEventArgs e) => AlertCreated?.Invoke(this, e);

    #endregion
}
