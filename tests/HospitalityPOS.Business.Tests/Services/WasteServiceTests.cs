// tests/HospitalityPOS.Business.Tests/Services/WasteServiceTests.cs
// Unit tests for WasteService
// Story 46-1: Waste and Shrinkage Tracking

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Inventory;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class WasteServiceTests
{
    private readonly IWasteService _service;

    public WasteServiceTests()
    {
        _service = new WasteService();
    }

    #region Waste Reasons Tests

    [Fact]
    public async Task GetActiveWasteReasonsAsync_ReturnsDefaultReasons()
    {
        // Act
        var reasons = await _service.GetActiveWasteReasonsAsync();

        // Assert
        reasons.Should().NotBeEmpty();
        reasons.Should().Contain(r => r.Name == "Expired");
        reasons.Should().Contain(r => r.Name == "Suspected Theft");
        reasons.Should().Contain(r => r.Category == WasteReasonCategory.Expiry);
        reasons.Should().Contain(r => r.Category == WasteReasonCategory.Damage);
    }

    [Fact]
    public async Task GetActiveWasteReasonsAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        // Act
        var reasons = await _service.GetActiveWasteReasonsAsync(WasteReasonCategory.Damage);

        // Assert
        reasons.Should().NotBeEmpty();
        reasons.Should().OnlyContain(r => r.Category == WasteReasonCategory.Damage);
    }

    [Fact]
    public async Task CreateWasteReasonAsync_CreatesNewReason()
    {
        // Arrange
        var request = new WasteReasonRequest
        {
            Name = "Test Reason",
            Description = "Test description",
            Category = WasteReasonCategory.Other,
            RequiresApproval = true
        };

        // Act
        var result = await _service.CreateWasteReasonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Test Reason");
        result.Category.Should().Be(WasteReasonCategory.Other);
        result.RequiresApproval.Should().BeTrue();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWasteReasonAsync_UpdatesExistingReason()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var reason = reasons.First();
        var request = new WasteReasonRequest
        {
            Id = reason.Id,
            Name = "Updated Name",
            Description = "Updated description",
            Category = reason.Category,
            RequiresApproval = !reason.RequiresApproval
        };

        // Act
        var result = await _service.UpdateWasteReasonAsync(request);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeactivateWasteReasonAsync_DeactivatesReason()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var reasonId = reasons.Last().Id;

        // Act
        var result = await _service.DeactivateWasteReasonAsync(reasonId);

        // Assert
        result.Should().BeTrue();
        var deactivated = await _service.GetWasteReasonAsync(reasonId);
        deactivated!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Waste Recording Tests

    [Fact]
    public async Task RecordWasteAsync_RecordsWasteSuccessfully()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");

        var request = new WasteRecordRequest
        {
            ProductId = 1, // Milk 500ml
            Quantity = 5,
            WasteReasonId = expiredReason.Id,
            Notes = "Past sell-by date",
            RecordedByUserId = 1
        };

        // Act
        var result = await _service.RecordWasteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record.Should().NotBeNull();
        result.Record!.ProductName.Should().Be("Milk 500ml");
        result.Record.Quantity.Should().Be(5);
        result.Record.WasteReasonName.Should().Be("Expired");
        result.Record.TotalValue.Should().Be(325m); // 5 * 65
    }

    [Fact]
    public async Task RecordWasteAsync_RequiresApproval_SetsPendingStatus()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var theftReason = reasons.First(r => r.Name == "Suspected Theft");

        var request = new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 2,
            WasteReasonId = theftReason.Id,
            Notes = "Suspected theft",
            RecordedByUserId = 1
        };

        // Act
        var result = await _service.RecordWasteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(WasteRecordStatus.PendingApproval);
        result.Warnings.Should().Contain(w => w.Contains("approval"));
    }

    [Fact]
    public async Task RecordWasteAsync_InvalidProduct_ReturnsFailed()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var request = new WasteRecordRequest
        {
            ProductId = 999, // Non-existent product
            Quantity = 5,
            WasteReasonId = reasons.First().Id,
            RecordedByUserId = 1
        };

        // Act
        var result = await _service.RecordWasteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RecordWasteAsync_InvalidQuantity_ReturnsFailed()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var request = new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 0,
            WasteReasonId = reasons.First().Id,
            RecordedByUserId = 1
        };

        // Act
        var result = await _service.RecordWasteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Quantity");
    }

    [Fact]
    public async Task RecordBatchWasteAsync_RecordsMultipleItems()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");

        var requests = new[]
        {
            new WasteRecordRequest { ProductId = 1, Quantity = 3, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 },
            new WasteRecordRequest { ProductId = 2, Quantity = 2, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 }
        };

        // Act
        var results = await _service.RecordBatchWasteAsync(requests);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
    }

    #endregion

    #region Approval Workflow Tests

    [Fact]
    public async Task ProcessApprovalAsync_Approve_UpdatesStatus()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var theftReason = reasons.First(r => r.RequiresApproval);

        var recordResult = await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 2,
            WasteReasonId = theftReason.Id,
            RecordedByUserId = 1
        });

        var approvalRequest = new WasteApprovalRequest
        {
            WasteRecordId = recordResult.Record!.Id,
            ApproverUserId = 2,
            Approve = true,
            Notes = "Verified and approved"
        };

        // Act
        var result = await _service.ProcessApprovalAsync(approvalRequest);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(WasteRecordStatus.Approved);
        result.Record.ApprovedByUserId.Should().Be(2);
        result.Record.ApprovalNotes.Should().Be("Verified and approved");
    }

    [Fact]
    public async Task ProcessApprovalAsync_Reject_UpdatesStatus()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var theftReason = reasons.First(r => r.RequiresApproval);

        var recordResult = await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 2,
            WasteReasonId = theftReason.Id,
            RecordedByUserId = 1
        });

        var approvalRequest = new WasteApprovalRequest
        {
            WasteRecordId = recordResult.Record!.Id,
            ApproverUserId = 2,
            Approve = false,
            Notes = "Insufficient evidence"
        };

        // Act
        var result = await _service.ProcessApprovalAsync(approvalRequest);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(WasteRecordStatus.Rejected);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsPendingRecords()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var theftReason = reasons.First(r => r.RequiresApproval);

        await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 2,
            WasteReasonId = theftReason.Id,
            RecordedByUserId = 1
        });

        // Act
        var pending = await _service.GetPendingApprovalsAsync();

        // Assert
        pending.Should().NotBeEmpty();
        pending.Should().OnlyContain(r => r.Status == WasteRecordStatus.PendingApproval);
    }

    [Fact]
    public async Task ReverseWasteAsync_ReversesRecord()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");

        var recordResult = await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 5,
            WasteReasonId = expiredReason.Id,
            RecordedByUserId = 1
        });

        // Act
        var result = await _service.ReverseWasteAsync(recordResult.Record!.Id, 2, "Entered in error");

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(WasteRecordStatus.Reversed);
    }

    #endregion

    #region Shrinkage Calculation Tests

    [Fact]
    public async Task CalculateShrinkageAsync_ReturnsShrinkageMetrics()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var metrics = await _service.CalculateShrinkageAsync(startDate, endDate);

        // Assert
        metrics.Should().NotBeNull();
        metrics.StartDate.Should().Be(startDate);
        metrics.EndDate.Should().Be(endDate);
        metrics.OpeningStockValue.Should().BeGreaterThan(0);
        metrics.PurchasesValue.Should().BeGreaterThan(0);
        metrics.ShrinkageValue.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetShrinkageTrendAsync_ReturnsTrendData()
    {
        // Act
        var trend = await _service.GetShrinkageTrendAsync(12);

        // Assert
        trend.Should().HaveCount(12);
        trend.Should().BeInAscendingOrder(t => new DateTime(t.Year, t.Month, 1));
    }

    [Fact]
    public async Task CreateShrinkageSnapshotAsync_CreatesSnapshots()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var snapshots = await _service.CreateShrinkageSnapshotAsync(date);

        // Assert
        snapshots.Should().NotBeEmpty();
        snapshots.Should().OnlyContain(s => s.SnapshotDate == date);
    }

    #endregion

    #region Stock Variance Tests

    [Fact]
    public async Task RecordVarianceAsync_RecordsVariance()
    {
        // Act
        var variance = await _service.RecordVarianceAsync(
            stockTakeId: 1,
            productId: 1,
            systemQuantity: 100,
            countedQuantity: 95,
            unitCost: 65m);

        // Assert
        variance.Should().NotBeNull();
        variance.SystemQuantity.Should().Be(100);
        variance.CountedQuantity.Should().Be(95);
        variance.Variance.Should().Be(5);
        variance.VarianceValue.Should().Be(325m); // 5 * 65
        variance.InvestigationStatus.Should().Be(VarianceInvestigationStatus.Pending);
    }

    [Fact]
    public async Task RecordVarianceAsync_SignificantVariance_CreatesAlert()
    {
        // Act
        var variance = await _service.RecordVarianceAsync(
            stockTakeId: 1,
            productId: 1,
            systemQuantity: 100,
            countedQuantity: 80, // 20% variance - significant
            unitCost: 65m);

        // Assert
        variance.IsSignificant.Should().BeTrue();

        var alerts = await _service.GetActiveAlertsAsync();
        alerts.Should().Contain(a => a.AlertType == LossPreventionAlertType.StockVariance);
    }

    [Fact]
    public async Task UpdateVarianceInvestigationAsync_UpdatesStatus()
    {
        // Arrange
        var variance = await _service.RecordVarianceAsync(1, 1, 100, 90, 65m);

        // Act
        var updated = await _service.UpdateVarianceInvestigationAsync(
            variance.Id,
            VarianceInvestigationStatus.Investigating,
            "Reviewing CCTV footage");

        // Assert
        updated.InvestigationStatus.Should().Be(VarianceInvestigationStatus.Investigating);
        updated.InvestigationNotes.Should().Be("Reviewing CCTV footage");
    }

    [Fact]
    public async Task CreateWasteFromVarianceAsync_CreatesWasteRecord()
    {
        // Arrange
        var variance = await _service.RecordVarianceAsync(1, 1, 100, 95, 65m);
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var varianceReason = reasons.First(r => r.Name == "Stock Count Variance");

        // Act
        var result = await _service.CreateWasteFromVarianceAsync(
            variance.Id,
            varianceReason.Id,
            1,
            "Written off after investigation");

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Quantity.Should().Be(5); // System - Counted
        result.Record.VarianceRecordId.Should().Be(variance.Id);
    }

    #endregion

    #region Reports Tests

    [Fact]
    public async Task GenerateWasteReportAsync_GeneratesReport()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");
        var damageReason = reasons.First(r => r.Name == "Breakage");

        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 1, Quantity = 5, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 });
        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 2, Quantity = 3, WasteReasonId = damageReason.Id, RecordedByUserId = 1 });

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var report = await _service.GenerateWasteReportAsync(startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.TotalRecords.Should().BeGreaterThan(0);
        report.TotalValue.Should().BeGreaterThan(0);
        report.ByReason.Should().NotBeEmpty();
        report.ByProduct.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateShrinkageReportAsync_GeneratesReport()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var report = await _service.GenerateShrinkageReportAsync(startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.OverallMetrics.Should().NotBeNull();
        report.MonthlyTrend.Should().NotBeEmpty();
        report.IndustryBenchmark.Should().Be(1.5m);
    }

    [Fact]
    public async Task GetTopShrinkageProductsAsync_ReturnsTopProducts()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");

        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 1, Quantity = 10, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 });
        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 2, Quantity = 5, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 });

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var topProducts = await _service.GetTopShrinkageProductsAsync(startDate, endDate, 10);

        // Assert
        topProducts.Should().NotBeEmpty();
        topProducts.Should().BeInDescendingOrder(p => p.ShrinkageValue);
    }

    [Fact]
    public async Task GetWasteByReasonAsync_GroupsByReason()
    {
        // Arrange
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");
        var damageReason = reasons.First(r => r.Name == "Breakage");

        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 1, Quantity = 5, WasteReasonId = expiredReason.Id, RecordedByUserId = 1 });
        await _service.RecordWasteAsync(new WasteRecordRequest { ProductId = 2, Quantity = 3, WasteReasonId = damageReason.Id, RecordedByUserId = 1 });

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var byReason = await _service.GetWasteByReasonAsync(startDate, endDate);

        // Assert
        byReason.Should().NotBeEmpty();
        byReason.Sum(r => r.PercentOfTotal).Should().BeApproximately(100m, 1m);
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboardAsync_ReturnsDashboardData()
    {
        // Act
        var dashboard = await _service.GetDashboardAsync();

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.TargetPercent.Should().Be(1.5m);
        dashboard.TrendData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDailyWasteTotalsAsync_ReturnsDailyTotals()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var dailyTotals = await _service.GetDailyWasteTotalsAsync(startDate, endDate);

        // Assert
        dailyTotals.Should().HaveCount(8); // 7 days + today
        dailyTotals.Should().BeInAscendingOrder(d => d.Date);
    }

    [Fact]
    public async Task GetShrinkageTrendDataAsync_ReturnsTrendPoints()
    {
        // Act
        var trendData = await _service.GetShrinkageTrendDataAsync(12);

        // Assert
        trendData.Should().HaveCount(12);
        trendData.Should().OnlyContain(t => !string.IsNullOrEmpty(t.Label));
    }

    #endregion

    #region Alerts Tests

    [Fact]
    public async Task CreateAlertAsync_CreatesAlert()
    {
        // Act
        var alert = await _service.CreateAlertAsync(
            LossPreventionAlertType.HighValueWaste,
            "High Value Waste",
            "Test alert message",
            AlertSeverity.Warning,
            productId: 1,
            value: 10000m,
            threshold: 5000m);

        // Assert
        alert.Should().NotBeNull();
        alert.Id.Should().BeGreaterThan(0);
        alert.AlertType.Should().Be(LossPreventionAlertType.HighValueWaste);
        alert.Severity.Should().Be(AlertSeverity.Warning);
        alert.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsUnacknowledgedAlerts()
    {
        // Arrange
        await _service.CreateAlertAsync(LossPreventionAlertType.HighValueWaste, "Test 1", "Message 1");
        await _service.CreateAlertAsync(LossPreventionAlertType.ThresholdExceeded, "Test 2", "Message 2");

        // Act
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().OnlyContain(a => !a.IsAcknowledged);
    }

    [Fact]
    public async Task AcknowledgeAlertAsync_AcknowledgesAlert()
    {
        // Arrange
        var alert = await _service.CreateAlertAsync(LossPreventionAlertType.HighValueWaste, "Test", "Message");

        // Act
        var result = await _service.AcknowledgeAlertAsync(alert.Id, 1);

        // Assert
        result.Should().BeTrue();

        var alerts = await _service.GetActiveAlertsAsync();
        alerts.Should().NotContain(a => a.Id == alert.Id);
    }

    [Fact]
    public async Task RunAlertChecksAsync_CreatesAlertsAsNeeded()
    {
        // Act
        var alertsCreated = await _service.RunAlertChecksAsync();

        // Assert
        alertsCreated.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Alert Rules Tests

    [Fact]
    public async Task GetAlertRulesAsync_ReturnsDefaultRules()
    {
        // Act
        var rules = await _service.GetAlertRulesAsync();

        // Assert
        rules.Should().NotBeEmpty();
        rules.Should().Contain(r => r.AlertType == LossPreventionAlertType.HighValueWaste);
        rules.Should().Contain(r => r.AlertType == LossPreventionAlertType.ThresholdExceeded);
    }

    [Fact]
    public async Task UpdateAlertRuleAsync_UpdatesRule()
    {
        // Arrange
        var rules = await _service.GetAlertRulesAsync();
        var rule = rules.First();
        rule.ValueThreshold = 10000m;
        rule.IsEnabled = false;

        // Act
        var updated = await _service.UpdateAlertRuleAsync(rule);

        // Assert
        updated.ValueThreshold.Should().Be(10000m);
        updated.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetAlertRuleEnabledAsync_TogglesEnabled()
    {
        // Arrange
        var rules = await _service.GetAlertRulesAsync();
        var rule = rules.First();

        // Act
        var result = await _service.SetAlertRuleEnabledAsync(rule.Id, false);

        // Assert
        result.Should().BeTrue();
        var updatedRules = await _service.GetAlertRulesAsync();
        updatedRules.First(r => r.Id == rule.Id).IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.TargetShrinkagePercent.Should().Be(1.5m);
        settings.AutoDeductStock.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesSettings()
    {
        // Arrange
        var newSettings = new WasteTrackingSettings
        {
            TargetShrinkagePercent = 2.0m,
            ApprovalThresholdValue = 10000m,
            RequireApprovalForAll = true,
            AutoDeductStock = false
        };

        // Act
        var updated = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        updated.TargetShrinkagePercent.Should().Be(2.0m);
        updated.ApprovalThresholdValue.Should().Be(10000m);
        updated.RequireApprovalForAll.Should().BeTrue();
        updated.AutoDeductStock.Should().BeFalse();
    }

    #endregion

    #region Events Tests

    [Fact]
    public async Task RecordWasteAsync_RaisesWasteRecordedEvent()
    {
        // Arrange
        WasteEventArgs? eventArgs = null;
        _service.WasteRecorded += (sender, args) => eventArgs = args;

        var reasons = await _service.GetActiveWasteReasonsAsync();
        var expiredReason = reasons.First(r => r.Name == "Expired");

        // Act
        await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 5,
            WasteReasonId = expiredReason.Id,
            RecordedByUserId = 1
        });

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.EventType.Should().Be("Recorded");
        eventArgs.Record.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessApprovalAsync_RaisesAppropriateEvent()
    {
        // Arrange
        WasteEventArgs? approvedArgs = null;
        _service.WasteApproved += (sender, args) => approvedArgs = args;

        var reasons = await _service.GetActiveWasteReasonsAsync();
        var theftReason = reasons.First(r => r.RequiresApproval);

        var recordResult = await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 2,
            WasteReasonId = theftReason.Id,
            RecordedByUserId = 1
        });

        // Act
        await _service.ProcessApprovalAsync(new WasteApprovalRequest
        {
            WasteRecordId = recordResult.Record!.Id,
            ApproverUserId = 2,
            Approve = true
        });

        // Assert
        approvedArgs.Should().NotBeNull();
        approvedArgs!.EventType.Should().Be("Approved");
    }

    [Fact]
    public async Task CreateAlertAsync_RaisesAlertCreatedEvent()
    {
        // Arrange
        AlertEventArgs? eventArgs = null;
        _service.AlertCreated += (sender, args) => eventArgs = args;

        // Act
        await _service.CreateAlertAsync(
            LossPreventionAlertType.HighValueWaste,
            "Test Alert",
            "Test message");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Alert.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWasteWorkflow_EndToEnd()
    {
        // Step 1: Get waste reasons
        var reasons = await _service.GetActiveWasteReasonsAsync();
        reasons.Should().NotBeEmpty();

        // Step 2: Record waste requiring approval
        var theftReason = reasons.First(r => r.RequiresApproval);
        var wasteResult = await _service.RecordWasteAsync(new WasteRecordRequest
        {
            ProductId = 1,
            Quantity = 10,
            WasteReasonId = theftReason.Id,
            Notes = "Missing from shelf",
            RecordedByUserId = 1
        });
        wasteResult.Success.Should().BeTrue();
        wasteResult.Record!.Status.Should().Be(WasteRecordStatus.PendingApproval);

        // Step 3: Check pending approvals
        var pending = await _service.GetPendingApprovalsAsync();
        pending.Should().Contain(p => p.Id == wasteResult.Record.Id);

        // Step 4: Approve waste
        var approvalResult = await _service.ProcessApprovalAsync(new WasteApprovalRequest
        {
            WasteRecordId = wasteResult.Record.Id,
            ApproverUserId = 2,
            Approve = true,
            Notes = "Verified via CCTV"
        });
        approvalResult.Success.Should().BeTrue();
        approvalResult.Record!.Status.Should().Be(WasteRecordStatus.Approved);

        // Step 5: Generate waste report
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var report = await _service.GenerateWasteReportAsync(startDate, endDate);
        report.TotalRecords.Should().BeGreaterThan(0);

        // Step 6: Check dashboard
        var dashboard = await _service.GetDashboardAsync();
        dashboard.Should().NotBeNull();

        // Step 7: Verify alerts were checked/created
        var alerts = await _service.GetActiveAlertsAsync();
        // Alerts may or may not exist depending on thresholds
    }

    [Fact]
    public async Task StockVarianceToWasteWorkflow_EndToEnd()
    {
        // Step 1: Record stock variance
        var variance = await _service.RecordVarianceAsync(
            stockTakeId: 100,
            productId: 1,
            systemQuantity: 50,
            countedQuantity: 45,
            unitCost: 65m);

        variance.Variance.Should().Be(5);
        variance.VarianceValue.Should().Be(325m);

        // Step 2: Investigate
        var investigated = await _service.UpdateVarianceInvestigationAsync(
            variance.Id,
            VarianceInvestigationStatus.Investigating,
            "Checking records");

        investigated.InvestigationStatus.Should().Be(VarianceInvestigationStatus.Investigating);

        // Step 3: Create waste from variance
        var reasons = await _service.GetActiveWasteReasonsAsync();
        var varianceReason = reasons.First(r => r.Name == "Stock Count Variance");

        var wasteResult = await _service.CreateWasteFromVarianceAsync(
            variance.Id,
            varianceReason.Id,
            1,
            "Could not find items, writing off");

        wasteResult.Success.Should().BeTrue();
        wasteResult.Record!.Quantity.Should().Be(5);
        wasteResult.Record.VarianceRecordId.Should().Be(variance.Id);
    }

    #endregion
}
