// src/HospitalityPOS.Infrastructure/Services/CommissionService.cs
// Implementation of sales commission calculation and tracking service
// Story 45-3: Commission Calculation

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for sales commission calculation and tracking.
/// </summary>
public class CommissionService : ICommissionService
{
    #region Private Fields

    private readonly Dictionary<int, CommissionRule> _rules = new();
    private readonly Dictionary<int, CommissionTransaction> _transactions = new();
    private readonly Dictionary<int, SalesAttribution> _attributions = new();
    private readonly Dictionary<int, CommissionPayout> _payouts = new();
    private readonly Dictionary<int, string> _employees = new();
    private readonly Dictionary<int, string> _roles = new();
    private readonly Dictionary<int, (string Name, int CategoryId)> _products = new();
    private readonly Dictionary<int, string> _categories = new();
    private readonly Dictionary<int, decimal> _receiptTotals = new(); // Simulated receipts

    private CommissionSettings _settings = new();
    private int _nextRuleId = 1;
    private int _nextTransactionId = 1;
    private int _nextPayoutId = 1;
    private int _nextAttributionId = 1;

    #endregion

    #region Events

    public event EventHandler<CommissionEventArgs>? CommissionEarned;
    public event EventHandler<CommissionEventArgs>? CommissionReversed;
    public event EventHandler<PayoutEventArgs>? PayoutProcessed;

    #endregion

    #region Constructor

    public CommissionService()
    {
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Sample employees
        _employees[1] = "John Doe";
        _employees[2] = "Jane Smith";
        _employees[3] = "Bob Wilson";
        _employees[4] = "Alice Brown";
        _employees[5] = "Charlie Davis";

        // Sample roles
        _roles[1] = "Sales Associate";
        _roles[2] = "Senior Sales";
        _roles[3] = "Sales Manager";

        // Sample categories
        _categories[1] = "Electronics";
        _categories[2] = "Furniture";
        _categories[3] = "Appliances";
        _categories[4] = "Accessories";

        // Sample products
        _products[101] = ("Laptop", 1);
        _products[102] = ("Smartphone", 1);
        _products[103] = ("TV", 1);
        _products[201] = ("Sofa", 2);
        _products[202] = ("Dining Table", 2);
        _products[301] = ("Refrigerator", 3);
        _products[302] = ("Washing Machine", 3);
        _products[401] = ("Phone Case", 4);
        _products[402] = ("Cables", 4);

        // Create default rules
        CreateDefaultRules();

        // Sample receipts
        _receiptTotals[1001] = 1500m;
        _receiptTotals[1002] = 2500m;
        _receiptTotals[1003] = 800m;
    }

    private void CreateDefaultRules()
    {
        // Global default rate
        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Default Commission",
            RuleType = CommissionRuleType.Global,
            CommissionRate = 2m,
            Priority = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Role-based rules
        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Sales Associate Base",
            RuleType = CommissionRuleType.Role,
            RoleId = 1,
            RoleName = "Sales Associate",
            CommissionRate = 3m,
            Priority = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Senior Sales Base",
            RuleType = CommissionRuleType.Role,
            RoleId = 2,
            RoleName = "Senior Sales",
            CommissionRate = 4m,
            TierThreshold = 50000m,
            TierCommissionRate = 5m,
            Priority = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Category-based rules
        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Electronics Commission",
            RuleType = CommissionRuleType.Category,
            CategoryId = 1,
            CategoryName = "Electronics",
            CommissionRate = 5m,
            Priority = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Furniture Commission",
            RuleType = CommissionRuleType.Category,
            CategoryId = 2,
            CategoryName = "Furniture",
            CommissionRate = 6m,
            Priority = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Accessories Low Commission",
            RuleType = CommissionRuleType.Category,
            CategoryId = 4,
            CategoryName = "Accessories",
            CommissionRate = 1m,
            Priority = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Product-specific rule
        _rules[_nextRuleId] = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = "Laptop High Commission",
            RuleType = CommissionRuleType.Product,
            ProductId = 101,
            ProductName = "Laptop",
            CommissionRate = 7m,
            MinimumSaleAmount = 1000m,
            Priority = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Commission Rules

    public Task<CommissionRule> CreateRuleAsync(CommissionRuleRequest request)
    {
        var rule = new CommissionRule
        {
            Id = _nextRuleId++,
            Name = request.Name,
            RuleType = request.RuleType,
            CalculationMethod = request.CalculationMethod,
            CommissionRate = request.CommissionRate,
            FixedAmount = request.FixedAmount,
            MinimumSaleAmount = request.MinimumSaleAmount,
            MaximumCommission = request.MaximumCommission,
            Priority = request.Priority,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Set target based on rule type
        if (request.TargetId.HasValue)
        {
            switch (request.RuleType)
            {
                case CommissionRuleType.Role:
                    rule.RoleId = request.TargetId;
                    rule.RoleName = _roles.GetValueOrDefault(request.TargetId.Value, "Unknown");
                    break;
                case CommissionRuleType.Category:
                    rule.CategoryId = request.TargetId;
                    rule.CategoryName = _categories.GetValueOrDefault(request.TargetId.Value, "Unknown");
                    break;
                case CommissionRuleType.Product:
                    rule.ProductId = request.TargetId;
                    rule.ProductName = _products.GetValueOrDefault(request.TargetId.Value, ("Unknown", 0)).Name;
                    break;
                case CommissionRuleType.Employee:
                    rule.EmployeeId = request.TargetId;
                    rule.EmployeeName = _employees.GetValueOrDefault(request.TargetId.Value, "Unknown");
                    break;
            }
        }

        if (request.Tiers != null)
        {
            rule.Tiers = request.Tiers;
        }

        _rules[rule.Id] = rule;
        return Task.FromResult(rule);
    }

    public Task<CommissionRule> UpdateRuleAsync(CommissionRuleRequest request)
    {
        if (!request.Id.HasValue || !_rules.ContainsKey(request.Id.Value))
        {
            throw new KeyNotFoundException($"Rule {request.Id} not found");
        }

        var rule = _rules[request.Id.Value];
        rule.Name = request.Name;
        rule.CommissionRate = request.CommissionRate;
        rule.FixedAmount = request.FixedAmount;
        rule.MinimumSaleAmount = request.MinimumSaleAmount;
        rule.MaximumCommission = request.MaximumCommission;
        rule.Priority = request.Priority;
        rule.ValidFrom = request.ValidFrom;
        rule.ValidTo = request.ValidTo;

        if (request.Tiers != null)
        {
            rule.Tiers = request.Tiers;
        }

        return Task.FromResult(rule);
    }

    public Task<bool> DeactivateRuleAsync(int ruleId)
    {
        if (_rules.TryGetValue(ruleId, out var rule))
        {
            rule.IsActive = false;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<CommissionRule?> GetRuleAsync(int ruleId)
    {
        _rules.TryGetValue(ruleId, out var rule);
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyList<CommissionRule>> GetActiveRulesAsync()
    {
        var rules = _rules.Values
            .Where(r => r.IsActive && IsRuleValid(r))
            .OrderByDescending(r => r.Priority)
            .ToList();
        return Task.FromResult<IReadOnlyList<CommissionRule>>(rules);
    }

    public Task<IReadOnlyList<CommissionRule>> GetRulesByTypeAsync(CommissionRuleType ruleType)
    {
        var rules = _rules.Values
            .Where(r => r.RuleType == ruleType && r.IsActive && IsRuleValid(r))
            .OrderByDescending(r => r.Priority)
            .ToList();
        return Task.FromResult<IReadOnlyList<CommissionRule>>(rules);
    }

    public Task<CommissionRule?> GetApplicableRuleAsync(int employeeId, int? productId, int? categoryId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeRules = _rules.Values
            .Where(r => r.IsActive && IsRuleValid(r))
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Check product-specific rule first
        if (productId.HasValue)
        {
            var productRule = activeRules.FirstOrDefault(r =>
                r.RuleType == CommissionRuleType.Product && r.ProductId == productId);
            if (productRule != null)
                return Task.FromResult<CommissionRule?>(productRule);
        }

        // Check category rule
        if (categoryId.HasValue)
        {
            var categoryRule = activeRules.FirstOrDefault(r =>
                r.RuleType == CommissionRuleType.Category && r.CategoryId == categoryId);
            if (categoryRule != null)
                return Task.FromResult<CommissionRule?>(categoryRule);
        }

        // Check employee-specific rule
        var employeeRule = activeRules.FirstOrDefault(r =>
            r.RuleType == CommissionRuleType.Employee && r.EmployeeId == employeeId);
        if (employeeRule != null)
            return Task.FromResult<CommissionRule?>(employeeRule);

        // Check role rule (simulated - assume employee 1-2 are role 1, 3-4 are role 2)
        var roleId = employeeId <= 2 ? 1 : 2;
        var roleRule = activeRules.FirstOrDefault(r =>
            r.RuleType == CommissionRuleType.Role && r.RoleId == roleId);
        if (roleRule != null)
            return Task.FromResult<CommissionRule?>(roleRule);

        // Fall back to global rule
        var globalRule = activeRules.FirstOrDefault(r => r.RuleType == CommissionRuleType.Global);
        return Task.FromResult(globalRule);
    }

    private bool IsRuleValid(CommissionRule rule)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (rule.ValidFrom.HasValue && today < rule.ValidFrom.Value)
            return false;
        if (rule.ValidTo.HasValue && today > rule.ValidTo.Value)
            return false;
        return true;
    }

    #endregion

    #region Commission Calculation

    public async Task<CommissionCalculationResult> CalculateCommissionAsync(int receiptId, int employeeId)
    {
        if (!_receiptTotals.TryGetValue(receiptId, out var saleAmount))
        {
            // Create simulated receipt if not exists
            saleAmount = 1000m;
            _receiptTotals[receiptId] = saleAmount;
        }

        var lineItems = new List<CommissionLineItem>();
        decimal totalCommission = 0m;
        CommissionRule? appliedRule = null;

        // Simplified: calculate on total sale
        var rule = await GetApplicableRuleAsync(employeeId, null, null);
        if (rule != null)
        {
            appliedRule = rule;
            var rate = rule.CommissionRate;

            if (saleAmount < rule.MinimumSaleAmount)
            {
                return CommissionCalculationResult.Failed(
                    $"Sale amount ${saleAmount:F2} is below minimum ${rule.MinimumSaleAmount:F2} for commission");
            }

            decimal commission;
            if (rule.CalculationMethod == CommissionCalculationMethod.FixedAmount && rule.FixedAmount.HasValue)
            {
                commission = rule.FixedAmount.Value;
            }
            else
            {
                commission = saleAmount * (rate / 100m);
            }

            // Apply maximum if set
            if (rule.MaximumCommission.HasValue && commission > rule.MaximumCommission.Value)
            {
                commission = rule.MaximumCommission.Value;
            }

            totalCommission = commission;

            lineItems.Add(new CommissionLineItem
            {
                ProductName = "Sale Total",
                SaleAmount = saleAmount,
                CommissionRate = rate,
                CommissionAmount = commission,
                RuleId = rule.Id,
                RuleDescription = rule.Name
            });
        }

        // Check for tier bonus
        var tierResult = await CheckTierBonusAsync(employeeId,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-DateTime.Today.Day + 1)),
            DateOnly.FromDateTime(DateTime.Today));

        var result = CommissionCalculationResult.Calculated(totalCommission, lineItems);
        result.AppliedRule = appliedRule;

        if (tierResult.Qualified)
        {
            var tierBonus = saleAmount * ((tierResult.BonusRate - (appliedRule?.CommissionRate ?? 0)) / 100m);
            result.TierBonusApplied = true;
            result.TierBonusAmount = tierBonus;
            result.TotalCommission += tierBonus;
        }

        return result;
    }

    public async Task<decimal> EstimateCommissionAsync(int employeeId, decimal saleAmount, int? productId = null, int? categoryId = null)
    {
        var rule = await GetApplicableRuleAsync(employeeId, productId, categoryId);
        if (rule == null)
            return 0m;

        if (saleAmount < rule.MinimumSaleAmount)
            return 0m;

        decimal commission;
        if (rule.CalculationMethod == CommissionCalculationMethod.FixedAmount && rule.FixedAmount.HasValue)
        {
            commission = rule.FixedAmount.Value;
        }
        else
        {
            commission = saleAmount * (rule.CommissionRate / 100m);
        }

        if (rule.MaximumCommission.HasValue && commission > rule.MaximumCommission.Value)
        {
            commission = rule.MaximumCommission.Value;
        }

        return commission;
    }

    public async Task<decimal> GetCommissionRateAsync(int employeeId, int? productId = null, int? categoryId = null)
    {
        var rule = await GetApplicableRuleAsync(employeeId, productId, categoryId);
        return rule?.CommissionRate ?? _settings.DefaultCommissionPercent;
    }

    public Task<(bool Qualified, decimal BonusRate, decimal CurrentTotal, decimal Threshold)> CheckTierBonusAsync(
        int employeeId, DateOnly periodStart, DateOnly periodEnd)
    {
        // Get employee's sales total for period
        var periodSales = _transactions.Values
            .Where(t => t.EmployeeId == employeeId &&
                        t.TransactionDate >= periodStart &&
                        t.TransactionDate <= periodEnd &&
                        t.TransactionType == CommissionTransactionType.Earned)
            .Sum(t => t.SaleAmount);

        // Check if any rule has tier bonus
        var tierRule = _rules.Values
            .Where(r => r.IsActive && r.TierThreshold.HasValue && r.TierCommissionRate.HasValue)
            .FirstOrDefault();

        if (tierRule != null && periodSales >= tierRule.TierThreshold)
        {
            return Task.FromResult((true, tierRule.TierCommissionRate!.Value, periodSales, tierRule.TierThreshold.Value));
        }

        return Task.FromResult((false, 0m, periodSales, tierRule?.TierThreshold ?? 50000m));
    }

    #endregion

    #region Commission Tracking

    public async Task<CommissionTransaction> RecordCommissionAsync(int receiptId, int employeeId)
    {
        // Check if already recorded
        var existing = _transactions.Values.FirstOrDefault(t =>
            t.ReceiptId == receiptId && t.TransactionType == CommissionTransactionType.Earned);
        if (existing != null)
        {
            throw new InvalidOperationException($"Commission already recorded for receipt {receiptId}");
        }

        var calcResult = await CalculateCommissionAsync(receiptId, employeeId);
        if (!calcResult.Success)
        {
            throw new InvalidOperationException(calcResult.Message);
        }

        var saleAmount = _receiptTotals.GetValueOrDefault(receiptId, 0m);

        var transaction = new CommissionTransaction
        {
            Id = _nextTransactionId++,
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            ReceiptId = receiptId,
            ReceiptNumber = $"R{receiptId:D6}",
            TransactionDate = DateOnly.FromDateTime(DateTime.Today),
            SaleAmount = saleAmount,
            CommissionRate = calcResult.AppliedRule?.CommissionRate ?? 0,
            CommissionAmount = calcResult.TotalCommission,
            TransactionType = CommissionTransactionType.Earned,
            CommissionRuleId = calcResult.AppliedRule?.Id,
            RuleDescription = calcResult.AppliedRule?.Name,
            CreatedAt = DateTime.UtcNow
        };

        _transactions[transaction.Id] = transaction;

        // Create attribution
        _attributions[receiptId] = new SalesAttribution
        {
            Id = _nextAttributionId++,
            ReceiptId = receiptId,
            Employees = new List<EmployeeAttribution>
            {
                new()
                {
                    EmployeeId = employeeId,
                    EmployeeName = transaction.EmployeeName,
                    SplitPercentage = 100m,
                    IsPrimary = true
                }
            }
        };

        CommissionEarned?.Invoke(this, new CommissionEventArgs(transaction, "Earned"));
        return transaction;
    }

    public Task<CommissionTransaction> ReverseCommissionAsync(int receiptId, string reason)
    {
        var original = _transactions.Values.FirstOrDefault(t =>
            t.ReceiptId == receiptId && t.TransactionType == CommissionTransactionType.Earned);

        if (original == null)
        {
            throw new KeyNotFoundException($"No commission found for receipt {receiptId}");
        }

        var reversal = new CommissionTransaction
        {
            Id = _nextTransactionId++,
            EmployeeId = original.EmployeeId,
            EmployeeName = original.EmployeeName,
            ReceiptId = receiptId,
            ReceiptNumber = original.ReceiptNumber,
            TransactionDate = DateOnly.FromDateTime(DateTime.Today),
            SaleAmount = original.SaleAmount,
            CommissionRate = original.CommissionRate,
            CommissionAmount = -original.CommissionAmount,
            TransactionType = CommissionTransactionType.Reversed,
            Notes = reason,
            RelatedTransactionId = original.Id,
            CreatedAt = DateTime.UtcNow
        };

        _transactions[reversal.Id] = reversal;
        CommissionReversed?.Invoke(this, new CommissionEventArgs(reversal, "Reversed"));
        return Task.FromResult(reversal);
    }

    public Task<CommissionTransaction> CreateAdjustmentAsync(int employeeId, decimal amount, string reason, int adjustedByUserId)
    {
        var adjustment = new CommissionTransaction
        {
            Id = _nextTransactionId++,
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            ReceiptId = 0,
            ReceiptNumber = "ADJUSTMENT",
            TransactionDate = DateOnly.FromDateTime(DateTime.Today),
            SaleAmount = 0,
            CommissionRate = 0,
            CommissionAmount = amount,
            TransactionType = CommissionTransactionType.Adjustment,
            Notes = $"Adjustment by user {adjustedByUserId}: {reason}",
            CreatedAt = DateTime.UtcNow
        };

        _transactions[adjustment.Id] = adjustment;
        return Task.FromResult(adjustment);
    }

    public Task<IReadOnlyList<CommissionTransaction>> GetEmployeeTransactionsAsync(
        int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var transactions = _transactions.Values
            .Where(t => t.EmployeeId == employeeId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<CommissionTransaction>>(transactions);
    }

    public Task<CommissionTransaction?> GetTransactionByReceiptAsync(int receiptId)
    {
        var transaction = _transactions.Values
            .FirstOrDefault(t => t.ReceiptId == receiptId && t.TransactionType == CommissionTransactionType.Earned);
        return Task.FromResult(transaction);
    }

    #endregion

    #region Sales Attribution

    public Task<SalesAttribution> GetAttributionAsync(int receiptId)
    {
        if (!_attributions.TryGetValue(receiptId, out var attribution))
        {
            attribution = new SalesAttribution
            {
                Id = _nextAttributionId++,
                ReceiptId = receiptId,
                Employees = new List<EmployeeAttribution>()
            };
        }
        return Task.FromResult(attribution);
    }

    public Task<SalesAttribution> SetAttributionAsync(AttributionRequest request)
    {
        var attribution = new SalesAttribution
        {
            Id = _attributions.ContainsKey(request.ReceiptId)
                ? _attributions[request.ReceiptId].Id
                : _nextAttributionId++,
            ReceiptId = request.ReceiptId,
            Employees = request.Attributions,
            IsSplit = request.Attributions.Count > 1,
            OverriddenByUserId = request.OverriddenByUserId,
            OverrideReason = request.Reason,
            OverriddenAt = request.OverriddenByUserId.HasValue ? DateTime.UtcNow : null
        };

        _attributions[request.ReceiptId] = attribution;
        return Task.FromResult(attribution);
    }

    public Task<SalesAttribution> SplitCommissionAsync(
        int receiptId, List<EmployeeAttribution> attributions, int overriddenByUserId, string? reason = null)
    {
        // Validate split percentages sum to 100
        var totalPercent = attributions.Sum(a => a.SplitPercentage);
        if (Math.Abs(totalPercent - 100m) > 0.01m)
        {
            throw new ArgumentException($"Split percentages must sum to 100% (got {totalPercent}%)");
        }

        // Update existing transaction if exists
        var existingTransaction = _transactions.Values
            .FirstOrDefault(t => t.ReceiptId == receiptId && t.TransactionType == CommissionTransactionType.Earned);

        if (existingTransaction != null)
        {
            // Create split transactions
            var originalAmount = existingTransaction.CommissionAmount;
            existingTransaction.SplitPercentage = attributions.First(a => a.EmployeeId == existingTransaction.EmployeeId)?.SplitPercentage ?? 0;
            existingTransaction.CommissionAmount = originalAmount * (existingTransaction.SplitPercentage / 100m);

            // Create transactions for other employees
            foreach (var attr in attributions.Where(a => a.EmployeeId != existingTransaction.EmployeeId))
            {
                var splitTransaction = new CommissionTransaction
                {
                    Id = _nextTransactionId++,
                    EmployeeId = attr.EmployeeId,
                    EmployeeName = attr.EmployeeName,
                    ReceiptId = receiptId,
                    ReceiptNumber = existingTransaction.ReceiptNumber,
                    TransactionDate = existingTransaction.TransactionDate,
                    SaleAmount = existingTransaction.SaleAmount * (attr.SplitPercentage / 100m),
                    CommissionRate = existingTransaction.CommissionRate,
                    CommissionAmount = originalAmount * (attr.SplitPercentage / 100m),
                    TransactionType = CommissionTransactionType.Earned,
                    SplitPercentage = attr.SplitPercentage,
                    OriginalEmployeeId = existingTransaction.EmployeeId,
                    Notes = $"Split from original sale - {attr.SplitPercentage}%",
                    CreatedAt = DateTime.UtcNow
                };
                _transactions[splitTransaction.Id] = splitTransaction;
            }
        }

        var attribution = new SalesAttribution
        {
            Id = _attributions.ContainsKey(receiptId) ? _attributions[receiptId].Id : _nextAttributionId++,
            ReceiptId = receiptId,
            Employees = attributions,
            IsSplit = true,
            OverriddenByUserId = overriddenByUserId,
            OverrideReason = reason,
            OverriddenAt = DateTime.UtcNow
        };

        _attributions[receiptId] = attribution;
        return Task.FromResult(attribution);
    }

    #endregion

    #region Reports

    public async Task<EmployeeCommissionSummary> GetEmployeeSummaryAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var transactions = await GetEmployeeTransactionsAsync(employeeId, startDate, endDate);

        var earned = transactions.Where(t => t.TransactionType == CommissionTransactionType.Earned).ToList();
        var reversed = transactions.Where(t => t.TransactionType == CommissionTransactionType.Reversed).ToList();

        var tierInfo = await CheckTierBonusAsync(employeeId, startDate, endDate);

        return new EmployeeCommissionSummary
        {
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalSales = earned.Count,
            TotalSalesAmount = earned.Sum(t => t.SaleAmount),
            TotalCommissionEarned = earned.Sum(t => t.CommissionAmount),
            TotalCommissionReversed = Math.Abs(reversed.Sum(t => t.CommissionAmount)),
            TierBonusEarned = tierInfo.Qualified,
            TierBonusAmount = tierInfo.Qualified ? tierInfo.BonusRate : 0,
            Transactions = transactions.ToList()
        };
    }

    public async Task<CommissionReport> GenerateReportAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null)
    {
        var targetEmployees = employeeIds?.ToList() ?? _employees.Keys.ToList();
        var summaries = new List<EmployeeCommissionSummary>();

        foreach (var empId in targetEmployees)
        {
            var summary = await GetEmployeeSummaryAsync(empId, startDate, endDate);
            if (summary.TotalSales > 0 || summary.NetCommission != 0)
            {
                summaries.Add(summary);
            }
        }

        var report = new CommissionReport
        {
            StartDate = startDate,
            EndDate = endDate,
            Employees = summaries,
            TotalSalesAmount = summaries.Sum(s => s.TotalSalesAmount),
            TotalCommission = summaries.Sum(s => s.NetCommission),
            TotalTransactions = summaries.Sum(s => s.TotalSales)
        };

        if (summaries.Any())
        {
            report.TopEarner = summaries.OrderByDescending(s => s.NetCommission).First();
            report.TopSeller = summaries.OrderByDescending(s => s.TotalSalesAmount).First();
        }

        return report;
    }

    public async Task<IReadOnlyList<EmployeeCommissionSummary>> GetTopEarnersAsync(DateOnly startDate, DateOnly endDate, int top = 10)
    {
        var report = await GenerateReportAsync(startDate, endDate);
        return report.Employees
            .OrderByDescending(e => e.NetCommission)
            .Take(top)
            .ToList();
    }

    #endregion

    #region Payouts

    public async Task<PayoutResult> CreatePayoutAsync(PayoutRequest request)
    {
        var summary = await GetEmployeeSummaryAsync(request.EmployeeId, request.PeriodStart, request.PeriodEnd);

        if (summary.NetCommission <= 0)
        {
            return PayoutResult.Failed("No commission to pay out");
        }

        var payout = new CommissionPayout
        {
            Id = _nextPayoutId++,
            EmployeeId = request.EmployeeId,
            EmployeeName = summary.EmployeeName,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            GrossCommission = summary.TotalCommissionEarned,
            Adjustments = request.Adjustments ?? 0,
            NetPayout = summary.NetCommission + (request.Adjustments ?? 0),
            Status = _settings.RequireApprovalForPayout
                ? CommissionPayoutStatus.Pending
                : CommissionPayoutStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        _payouts[payout.Id] = payout;
        return PayoutResult.Succeeded(payout);
    }

    public Task<PayoutResult> ApprovePayoutAsync(int payoutId, int approvedByUserId)
    {
        if (!_payouts.TryGetValue(payoutId, out var payout))
        {
            return Task.FromResult(PayoutResult.Failed("Payout not found"));
        }

        if (payout.Status != CommissionPayoutStatus.Pending)
        {
            return Task.FromResult(PayoutResult.Failed($"Payout is not pending (status: {payout.Status})"));
        }

        payout.Status = CommissionPayoutStatus.Approved;
        payout.ApprovedByUserId = approvedByUserId;
        payout.ApprovedAt = DateTime.UtcNow;

        return Task.FromResult(PayoutResult.Succeeded(payout, "Payout approved"));
    }

    public Task<PayoutResult> MarkPayoutPaidAsync(int payoutId, string? paymentReference = null)
    {
        if (!_payouts.TryGetValue(payoutId, out var payout))
        {
            return Task.FromResult(PayoutResult.Failed("Payout not found"));
        }

        if (payout.Status != CommissionPayoutStatus.Approved)
        {
            return Task.FromResult(PayoutResult.Failed($"Payout must be approved first (status: {payout.Status})"));
        }

        payout.Status = CommissionPayoutStatus.Paid;
        payout.PaidAt = DateTime.UtcNow;
        payout.PaymentReference = paymentReference;

        // Mark related transactions as paid
        var periodTransactions = _transactions.Values
            .Where(t => t.EmployeeId == payout.EmployeeId &&
                        t.TransactionDate >= payout.PeriodStart &&
                        t.TransactionDate <= payout.PeriodEnd)
            .ToList();

        foreach (var t in periodTransactions)
        {
            // In real implementation, would mark as paid
        }

        PayoutProcessed?.Invoke(this, new PayoutEventArgs(payout, "Paid"));
        return Task.FromResult(PayoutResult.Succeeded(payout, "Payout marked as paid"));
    }

    public Task<IReadOnlyList<CommissionPayout>> GetPendingPayoutsAsync()
    {
        var pending = _payouts.Values
            .Where(p => p.Status == CommissionPayoutStatus.Pending || p.Status == CommissionPayoutStatus.Approved)
            .OrderBy(p => p.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<CommissionPayout>>(pending);
    }

    public Task<IReadOnlyList<CommissionPayout>> GetEmployeePayoutsAsync(int employeeId)
    {
        var payouts = _payouts.Values
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<CommissionPayout>>(payouts);
    }

    public Task<CommissionPayout?> GetPayoutAsync(int payoutId)
    {
        _payouts.TryGetValue(payoutId, out var payout);
        return Task.FromResult(payout);
    }

    #endregion

    #region Payroll Integration

    public async Task<CommissionPayrollExport> ExportForPayrollAsync(
        DateOnly periodStart, DateOnly periodEnd, IEnumerable<int>? employeeIds = null)
    {
        var report = await GenerateReportAsync(periodStart, periodEnd, employeeIds);

        return new CommissionPayrollExport
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Employees = report.Employees.Select(e => new EmployeeCommissionPayroll
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.EmployeeName,
                GrossCommission = e.TotalCommissionEarned,
                Adjustments = 0, // Would include adjustments
                NetCommission = e.NetCommission,
                TransactionCount = e.TotalSales
            }).ToList(),
            TotalCommission = report.TotalCommission,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public Task<decimal> GetUnpaidCommissionAsync(int employeeId)
    {
        // Calculate total earned minus total paid
        var totalEarned = _transactions.Values
            .Where(t => t.EmployeeId == employeeId &&
                        (t.TransactionType == CommissionTransactionType.Earned ||
                         t.TransactionType == CommissionTransactionType.Adjustment))
            .Sum(t => t.CommissionAmount);

        var totalReversed = _transactions.Values
            .Where(t => t.EmployeeId == employeeId && t.TransactionType == CommissionTransactionType.Reversed)
            .Sum(t => Math.Abs(t.CommissionAmount));

        var totalPaid = _payouts.Values
            .Where(p => p.EmployeeId == employeeId && p.Status == CommissionPayoutStatus.Paid)
            .Sum(p => p.NetPayout);

        return Task.FromResult(totalEarned - totalReversed - totalPaid);
    }

    #endregion

    #region Settings

    public Task<CommissionSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<CommissionSettings> UpdateSettingsAsync(CommissionSettings settings)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion
}
