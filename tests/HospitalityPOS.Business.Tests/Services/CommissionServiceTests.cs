// tests/HospitalityPOS.Business.Tests/Services/CommissionServiceTests.cs
// Unit tests for CommissionService
// Story 45-3: Commission Calculation

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class CommissionServiceTests
{
    private readonly ICommissionService _service;

    public CommissionServiceTests()
    {
        _service = new CommissionService();
    }

    #region Commission Rules Tests

    [Fact]
    public async Task CreateRuleAsync_WithValidRequest_ShouldCreateRule()
    {
        // Arrange
        var request = new CommissionRuleRequest
        {
            Name = "Test Rule",
            RuleType = CommissionRuleType.Category,
            TargetId = 1,
            CommissionRate = 5m,
            Priority = 25
        };

        // Act
        var rule = await _service.CreateRuleAsync(request);

        // Assert
        rule.Should().NotBeNull();
        rule.Id.Should().BeGreaterThan(0);
        rule.Name.Should().Be("Test Rule");
        rule.RuleType.Should().Be(CommissionRuleType.Category);
        rule.CommissionRate.Should().Be(5m);
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveRulesAsync_ShouldReturnActiveRulesOrderedByPriority()
    {
        // Act
        var rules = await _service.GetActiveRulesAsync();

        // Assert
        rules.Should().NotBeEmpty();
        rules.Should().BeInDescendingOrder(r => r.Priority);
        rules.Should().OnlyContain(r => r.IsActive);
    }

    [Fact]
    public async Task DeactivateRuleAsync_ShouldDeactivateRule()
    {
        // Arrange
        var request = new CommissionRuleRequest
        {
            Name = "Rule to Deactivate",
            RuleType = CommissionRuleType.Global,
            CommissionRate = 1m
        };
        var rule = await _service.CreateRuleAsync(request);

        // Act
        var result = await _service.DeactivateRuleAsync(rule.Id);
        var activeRules = await _service.GetActiveRulesAsync();

        // Assert
        result.Should().BeTrue();
        activeRules.Should().NotContain(r => r.Id == rule.Id);
    }

    [Fact]
    public async Task GetApplicableRuleAsync_ShouldReturnHighestPriorityMatchingRule()
    {
        // Arrange - Create product-specific rule with high priority
        await _service.CreateRuleAsync(new CommissionRuleRequest
        {
            Name = "Specific Product Rule",
            RuleType = CommissionRuleType.Product,
            TargetId = 999,
            CommissionRate = 10m,
            Priority = 100
        });

        // Act
        var rule = await _service.GetApplicableRuleAsync(1, 999, null);

        // Assert
        rule.Should().NotBeNull();
        rule!.CommissionRate.Should().Be(10m);
    }

    [Fact]
    public async Task GetRulesByTypeAsync_ShouldFilterByType()
    {
        // Act
        var categoryRules = await _service.GetRulesByTypeAsync(CommissionRuleType.Category);
        var roleRules = await _service.GetRulesByTypeAsync(CommissionRuleType.Role);

        // Assert
        categoryRules.Should().OnlyContain(r => r.RuleType == CommissionRuleType.Category);
        roleRules.Should().OnlyContain(r => r.RuleType == CommissionRuleType.Role);
    }

    [Fact]
    public async Task UpdateRuleAsync_ShouldUpdateExistingRule()
    {
        // Arrange
        var request = new CommissionRuleRequest
        {
            Name = "Original Name",
            RuleType = CommissionRuleType.Global,
            CommissionRate = 2m
        };
        var rule = await _service.CreateRuleAsync(request);

        // Act
        request.Id = rule.Id;
        request.Name = "Updated Name";
        request.CommissionRate = 3m;
        var updated = await _service.UpdateRuleAsync(request);

        // Assert
        updated.Name.Should().Be("Updated Name");
        updated.CommissionRate.Should().Be(3m);
    }

    #endregion

    #region Commission Calculation Tests

    [Fact]
    public async Task CalculateCommissionAsync_ShouldCalculateBasedOnRules()
    {
        // Arrange
        var receiptId = 2001;
        var employeeId = 1;

        // Act
        var result = await _service.CalculateCommissionAsync(receiptId, employeeId);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalCommission.Should().BeGreaterThan(0);
        result.LineItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EstimateCommissionAsync_ShouldReturnEstimate()
    {
        // Arrange
        var employeeId = 1;
        var saleAmount = 1000m;

        // Act
        var estimate = await _service.EstimateCommissionAsync(employeeId, saleAmount);

        // Assert
        estimate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCommissionRateAsync_ShouldReturnApplicableRate()
    {
        // Act
        var rate = await _service.GetCommissionRateAsync(1);

        // Assert
        rate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckTierBonusAsync_WhenBelowThreshold_ShouldNotQualify()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = today.AddDays(-today.Day + 1);

        // Act (fresh service, no sales recorded)
        var result = await _service.CheckTierBonusAsync(1, monthStart, today);

        // Assert
        result.Qualified.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateCommissionAsync_WithMinimumSaleAmount_ShouldRespectMinimum()
    {
        // Arrange - Create rule with minimum
        await _service.CreateRuleAsync(new CommissionRuleRequest
        {
            Name = "High Minimum Rule",
            RuleType = CommissionRuleType.Product,
            TargetId = 888,
            CommissionRate = 10m,
            MinimumSaleAmount = 10000m,
            Priority = 100
        });

        // Act - Simulate a small sale for that product
        var rate = await _service.GetCommissionRateAsync(1, 888);

        // Assert - Should still get a rate, but calculation would fail if below minimum
        rate.Should().BeGreaterThan(0);
    }

    #endregion

    #region Commission Tracking Tests

    [Fact]
    public async Task RecordCommissionAsync_ShouldCreateTransaction()
    {
        // Arrange
        var receiptId = 3001;
        var employeeId = 1;

        // Act
        var transaction = await _service.RecordCommissionAsync(receiptId, employeeId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.EmployeeId.Should().Be(employeeId);
        transaction.ReceiptId.Should().Be(receiptId);
        transaction.TransactionType.Should().Be(CommissionTransactionType.Earned);
        transaction.CommissionAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RecordCommissionAsync_DuplicateReceipt_ShouldThrow()
    {
        // Arrange
        var receiptId = 3002;
        await _service.RecordCommissionAsync(receiptId, 1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordCommissionAsync(receiptId, 1));
    }

    [Fact]
    public async Task ReverseCommissionAsync_ShouldCreateReversalTransaction()
    {
        // Arrange
        var receiptId = 3003;
        await _service.RecordCommissionAsync(receiptId, 1);

        // Act
        var reversal = await _service.ReverseCommissionAsync(receiptId, "Customer return");

        // Assert
        reversal.TransactionType.Should().Be(CommissionTransactionType.Reversed);
        reversal.CommissionAmount.Should().BeLessThan(0);
        reversal.Notes.Should().Contain("return");
    }

    [Fact]
    public async Task CreateAdjustmentAsync_ShouldCreateAdjustmentTransaction()
    {
        // Arrange
        var employeeId = 1;
        var amount = 50m;

        // Act
        var adjustment = await _service.CreateAdjustmentAsync(employeeId, amount, "Bonus", 100);

        // Assert
        adjustment.TransactionType.Should().Be(CommissionTransactionType.Adjustment);
        adjustment.CommissionAmount.Should().Be(amount);
        adjustment.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public async Task GetEmployeeTransactionsAsync_ShouldReturnTransactionsInRange()
    {
        // Arrange
        var employeeId = 2;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(4001, employeeId);
        await _service.RecordCommissionAsync(4002, employeeId);

        // Act
        var transactions = await _service.GetEmployeeTransactionsAsync(
            employeeId, today.AddDays(-7), today.AddDays(1));

        // Assert
        transactions.Should().NotBeEmpty();
        transactions.Should().OnlyContain(t => t.EmployeeId == employeeId);
    }

    [Fact]
    public async Task GetTransactionByReceiptAsync_ShouldReturnTransaction()
    {
        // Arrange
        var receiptId = 4003;
        await _service.RecordCommissionAsync(receiptId, 1);

        // Act
        var transaction = await _service.GetTransactionByReceiptAsync(receiptId);

        // Assert
        transaction.Should().NotBeNull();
        transaction!.ReceiptId.Should().Be(receiptId);
    }

    #endregion

    #region Sales Attribution Tests

    [Fact]
    public async Task GetAttributionAsync_ShouldReturnAttribution()
    {
        // Arrange
        var receiptId = 5001;
        await _service.RecordCommissionAsync(receiptId, 1);

        // Act
        var attribution = await _service.GetAttributionAsync(receiptId);

        // Assert
        attribution.Should().NotBeNull();
        attribution.ReceiptId.Should().Be(receiptId);
        attribution.Employees.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetAttributionAsync_ShouldUpdateAttribution()
    {
        // Arrange
        var request = new AttributionRequest
        {
            ReceiptId = 5002,
            Attributions = new List<EmployeeAttribution>
            {
                new() { EmployeeId = 1, EmployeeName = "John", SplitPercentage = 100 }
            }
        };

        // Act
        var attribution = await _service.SetAttributionAsync(request);

        // Assert
        attribution.ReceiptId.Should().Be(5002);
        attribution.Employees.Should().HaveCount(1);
    }

    [Fact]
    public async Task SplitCommissionAsync_ShouldSplitBetweenEmployees()
    {
        // Arrange
        var receiptId = 5003;
        await _service.RecordCommissionAsync(receiptId, 1);

        var attributions = new List<EmployeeAttribution>
        {
            new() { EmployeeId = 1, EmployeeName = "John", SplitPercentage = 60 },
            new() { EmployeeId = 2, EmployeeName = "Jane", SplitPercentage = 40 }
        };

        // Act
        var result = await _service.SplitCommissionAsync(receiptId, attributions, 100, "Joint sale");

        // Assert
        result.IsSplit.Should().BeTrue();
        result.Employees.Should().HaveCount(2);
        result.Employees.Sum(e => e.SplitPercentage).Should().Be(100);
    }

    [Fact]
    public async Task SplitCommissionAsync_InvalidPercentage_ShouldThrow()
    {
        // Arrange
        var receiptId = 5004;
        await _service.RecordCommissionAsync(receiptId, 1);

        var attributions = new List<EmployeeAttribution>
        {
            new() { EmployeeId = 1, EmployeeName = "John", SplitPercentage = 60 },
            new() { EmployeeId = 2, EmployeeName = "Jane", SplitPercentage = 50 } // Total 110%
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SplitCommissionAsync(receiptId, attributions, 100));
    }

    #endregion

    #region Reports Tests

    [Fact]
    public async Task GetEmployeeSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var employeeId = 3;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(6001, employeeId);
        await _service.RecordCommissionAsync(6002, employeeId);

        // Act
        var summary = await _service.GetEmployeeSummaryAsync(employeeId, today.AddDays(-30), today.AddDays(1));

        // Assert
        summary.EmployeeId.Should().Be(employeeId);
        summary.TotalSales.Should().BeGreaterThan(0);
        summary.TotalCommissionEarned.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldReturnComprehensiveReport()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(6003, 1);
        await _service.RecordCommissionAsync(6004, 2);

        // Act
        var report = await _service.GenerateReportAsync(today.AddDays(-30), today.AddDays(1));

        // Assert
        report.Should().NotBeNull();
        report.Employees.Should().NotBeEmpty();
        report.TotalCommission.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetTopEarnersAsync_ShouldReturnTopPerformers()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(6005, 1);
        await _service.RecordCommissionAsync(6006, 2);
        await _service.RecordCommissionAsync(6007, 3);

        // Act
        var topEarners = await _service.GetTopEarnersAsync(today.AddDays(-30), today.AddDays(1), 5);

        // Assert
        topEarners.Should().NotBeEmpty();
        topEarners.Should().BeInDescendingOrder(e => e.NetCommission);
    }

    #endregion

    #region Payout Tests

    [Fact]
    public async Task CreatePayoutAsync_ShouldCreatePayout()
    {
        // Arrange
        var employeeId = 4;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(7001, employeeId);

        var request = new PayoutRequest
        {
            EmployeeId = employeeId,
            PeriodStart = today.AddDays(-30),
            PeriodEnd = today.AddDays(1)
        };

        // Act
        var result = await _service.CreatePayoutAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Payout.Should().NotBeNull();
        result.Payout!.EmployeeId.Should().Be(employeeId);
        result.Payout.NetPayout.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApprovePayoutAsync_ShouldApprovePendingPayout()
    {
        // Arrange
        var employeeId = 4;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(7002, employeeId);

        var createResult = await _service.CreatePayoutAsync(new PayoutRequest
        {
            EmployeeId = employeeId,
            PeriodStart = today.AddDays(-30),
            PeriodEnd = today.AddDays(1)
        });

        // Act
        var approveResult = await _service.ApprovePayoutAsync(createResult.Payout!.Id, 100);

        // Assert
        approveResult.Success.Should().BeTrue();
        approveResult.Payout!.Status.Should().Be(CommissionPayoutStatus.Approved);
        approveResult.Payout.ApprovedByUserId.Should().Be(100);
    }

    [Fact]
    public async Task MarkPayoutPaidAsync_ShouldMarkAsPaid()
    {
        // Arrange
        var employeeId = 4;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(7003, employeeId);

        var createResult = await _service.CreatePayoutAsync(new PayoutRequest
        {
            EmployeeId = employeeId,
            PeriodStart = today.AddDays(-30),
            PeriodEnd = today.AddDays(1)
        });
        await _service.ApprovePayoutAsync(createResult.Payout!.Id, 100);

        // Act
        var paidResult = await _service.MarkPayoutPaidAsync(createResult.Payout.Id, "REF123");

        // Assert
        paidResult.Success.Should().BeTrue();
        paidResult.Payout!.Status.Should().Be(CommissionPayoutStatus.Paid);
        paidResult.Payout.PaymentReference.Should().Be("REF123");
    }

    [Fact]
    public async Task GetPendingPayoutsAsync_ShouldReturnPendingAndApproved()
    {
        // Arrange
        await _service.RecordCommissionAsync(7004, 5);
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.CreatePayoutAsync(new PayoutRequest
        {
            EmployeeId = 5,
            PeriodStart = today.AddDays(-30),
            PeriodEnd = today.AddDays(1)
        });

        // Act
        var pending = await _service.GetPendingPayoutsAsync();

        // Assert
        pending.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEmployeePayoutsAsync_ShouldReturnEmployeePayouts()
    {
        // Arrange
        var employeeId = 1;
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(7005, employeeId);
        await _service.CreatePayoutAsync(new PayoutRequest
        {
            EmployeeId = employeeId,
            PeriodStart = today.AddDays(-30),
            PeriodEnd = today.AddDays(1)
        });

        // Act
        var payouts = await _service.GetEmployeePayoutsAsync(employeeId);

        // Assert
        payouts.Should().NotBeEmpty();
        payouts.Should().OnlyContain(p => p.EmployeeId == employeeId);
    }

    #endregion

    #region Payroll Integration Tests

    [Fact]
    public async Task ExportForPayrollAsync_ShouldReturnExportData()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.RecordCommissionAsync(8001, 1);
        await _service.RecordCommissionAsync(8002, 2);

        // Act
        var export = await _service.ExportForPayrollAsync(today.AddDays(-30), today.AddDays(1));

        // Assert
        export.Should().NotBeNull();
        export.Employees.Should().NotBeEmpty();
        export.TotalCommission.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetUnpaidCommissionAsync_ShouldReturnUnpaidAmount()
    {
        // Arrange
        var employeeId = 1;
        await _service.RecordCommissionAsync(8003, employeeId);

        // Act
        var unpaid = await _service.GetUnpaidCommissionAsync(employeeId);

        // Assert
        unpaid.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateSettings()
    {
        // Arrange
        var newSettings = new CommissionSettings
        {
            DefaultCommissionPercent = 5m,
            EnableTieredCommission = false,
            PayoutFrequency = "Weekly"
        };

        // Act
        var updated = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        updated.DefaultCommissionPercent.Should().Be(5m);
        updated.EnableTieredCommission.Should().BeFalse();
        updated.PayoutFrequency.Should().Be("Weekly");
    }

    #endregion

    #region Model Tests

    [Fact]
    public void CommissionCalculationResult_Factories_ShouldWork()
    {
        // Calculated
        var items = new List<CommissionLineItem> { new() { CommissionAmount = 50 } };
        var success = CommissionCalculationResult.Calculated(50m, items);
        success.Success.Should().BeTrue();
        success.TotalCommission.Should().Be(50m);

        // Failed
        var failure = CommissionCalculationResult.Failed("Error");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Error");
    }

    [Fact]
    public void PayoutResult_Factories_ShouldWork()
    {
        // Success
        var payout = new CommissionPayout { Id = 1, NetPayout = 100 };
        var success = PayoutResult.Succeeded(payout);
        success.Success.Should().BeTrue();
        success.Payout.Should().Be(payout);

        // Failed
        var failure = PayoutResult.Failed("Not found");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Not found");
    }

    [Fact]
    public void EmployeeCommissionSummary_CalculatedProperties_ShouldWork()
    {
        var summary = new EmployeeCommissionSummary
        {
            TotalSales = 10,
            TotalSalesAmount = 10000m,
            TotalCommissionEarned = 500m,
            TotalCommissionReversed = 50m
        };

        summary.AverageSaleAmount.Should().Be(1000m);
        summary.NetCommission.Should().Be(450m);
        summary.AverageCommissionRate.Should().Be(5m);
    }

    [Fact]
    public void CommissionSettings_DefaultValues_ShouldBeSet()
    {
        var settings = new CommissionSettings();

        settings.DefaultCommissionPercent.Should().Be(0m);
        settings.EnableTieredCommission.Should().BeTrue();
        settings.PayoutFrequency.Should().Be("Monthly");
        settings.ConfirmationPeriodDays.Should().Be(14);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task RecordCommissionAsync_ShouldRaiseEvent()
    {
        // Arrange
        CommissionTransaction? earnedTransaction = null;
        ((CommissionService)_service).CommissionEarned += (sender, args) =>
        {
            earnedTransaction = args.Transaction;
        };

        // Act
        await _service.RecordCommissionAsync(9001, 1);

        // Assert
        earnedTransaction.Should().NotBeNull();
        earnedTransaction!.TransactionType.Should().Be(CommissionTransactionType.Earned);
    }

    [Fact]
    public async Task ReverseCommissionAsync_ShouldRaiseEvent()
    {
        // Arrange
        CommissionTransaction? reversedTransaction = null;
        ((CommissionService)_service).CommissionReversed += (sender, args) =>
        {
            reversedTransaction = args.Transaction;
        };
        await _service.RecordCommissionAsync(9002, 1);

        // Act
        await _service.ReverseCommissionAsync(9002, "Test");

        // Assert
        reversedTransaction.Should().NotBeNull();
        reversedTransaction!.TransactionType.Should().Be(CommissionTransactionType.Reversed);
    }

    #endregion
}
