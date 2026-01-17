// tests/HospitalityPOS.Business.Tests/Services/LeaveServiceTests.cs
// Unit tests for LeaveService
// Story 45-4: Leave Management

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class LeaveServiceTests
{
    private readonly ILeaveService _service;

    public LeaveServiceTests()
    {
        _service = new LeaveService();
    }

    #region Leave Types Tests

    [Fact]
    public async Task GetActiveLeaveTypesAsync_ShouldReturnDefaultTypes()
    {
        // Act
        var types = await _service.GetActiveLeaveTypesAsync();

        // Assert
        types.Should().NotBeEmpty();
        types.Should().Contain(t => t.Name == "Annual Leave");
        types.Should().Contain(t => t.Name == "Sick Leave");
        types.Should().Contain(t => t.Name == "Maternity Leave");
    }

    [Fact]
    public async Task CreateLeaveTypeAsync_ShouldCreateType()
    {
        // Arrange
        var request = new LeaveTypeRequest
        {
            Name = "Study Leave",
            Description = "Leave for educational purposes",
            DefaultDaysPerYear = 5,
            IsPaid = true
        };

        // Act
        var result = await _service.CreateLeaveTypeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Study Leave");
        result.DefaultDaysPerYear.Should().Be(5);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLeaveTypeAsync_ShouldUpdateType()
    {
        // Arrange
        var created = await _service.CreateLeaveTypeAsync(new LeaveTypeRequest
        {
            Name = "Original Name",
            DefaultDaysPerYear = 10
        });

        // Act
        var updated = await _service.UpdateLeaveTypeAsync(new LeaveTypeRequest
        {
            Id = created.Id,
            Name = "Updated Name",
            DefaultDaysPerYear = 15
        });

        // Assert
        updated.Name.Should().Be("Updated Name");
        updated.DefaultDaysPerYear.Should().Be(15);
    }

    [Fact]
    public async Task DeactivateLeaveTypeAsync_ShouldDeactivate()
    {
        // Arrange
        var created = await _service.CreateLeaveTypeAsync(new LeaveTypeRequest
        {
            Name = "Temp Leave",
            DefaultDaysPerYear = 5
        });

        // Act
        var result = await _service.DeactivateLeaveTypeAsync(created.Id);
        var types = await _service.GetActiveLeaveTypesAsync();

        // Assert
        result.Should().BeTrue();
        types.Should().NotContain(t => t.Id == created.Id);
    }

    #endregion

    #region Leave Request Tests

    [Fact]
    public async Task SubmitRequestAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var submission = new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1, // Annual Leave
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(18)),
            Reason = "Vacation"
        };

        // Act
        var result = await _service.SubmitRequestAsync(submission);

        // Assert
        result.Success.Should().BeTrue();
        result.Request.Should().NotBeNull();
        result.Request!.Status.Should().Be(LeaveRequestStatus.Pending);
        result.Request.DaysRequested.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitRequestAsync_WithInvalidDates_ShouldFail()
    {
        // Arrange
        var submission = new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)) // End before start
        };

        // Act
        var result = await _service.SubmitRequestAsync(submission);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("End date");
    }

    [Fact]
    public async Task ProcessApprovalAsync_Approve_ShouldUpdateStatus()
    {
        // Arrange
        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(32))
        });

        // Act
        var approvalResult = await _service.ProcessApprovalAsync(new LeaveApprovalRequest
        {
            RequestId = submitResult.Request!.Id,
            ReviewerUserId = 100,
            Approve = true,
            Notes = "Approved"
        });

        // Assert
        approvalResult.Success.Should().BeTrue();
        approvalResult.Request!.Status.Should().Be(LeaveRequestStatus.Approved);
        approvalResult.Request.ReviewedByUserId.Should().Be(100);
    }

    [Fact]
    public async Task ProcessApprovalAsync_Reject_ShouldUpdateStatus()
    {
        // Arrange
        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(40)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(42))
        });

        // Act
        var approvalResult = await _service.ProcessApprovalAsync(new LeaveApprovalRequest
        {
            RequestId = submitResult.Request!.Id,
            ReviewerUserId = 100,
            Approve = false,
            Notes = "Insufficient coverage"
        });

        // Assert
        approvalResult.Success.Should().BeTrue();
        approvalResult.Request!.Status.Should().Be(LeaveRequestStatus.Rejected);
    }

    [Fact]
    public async Task CancelRequestAsync_PendingRequest_ShouldCancel()
    {
        // Arrange
        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(50)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(52))
        });

        // Act
        var cancelResult = await _service.CancelRequestAsync(
            submitResult.Request!.Id, 1, "Plans changed");

        // Assert
        cancelResult.Success.Should().BeTrue();
        cancelResult.Request!.Status.Should().Be(LeaveRequestStatus.Cancelled);
    }

    [Fact]
    public async Task CancelRequestAsync_OtherEmployeeRequest_ShouldFail()
    {
        // Arrange
        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(62))
        });

        // Act
        var cancelResult = await _service.CancelRequestAsync(
            submitResult.Request!.Id, 2, "Cancel"); // Different employee

        // Assert
        cancelResult.Success.Should().BeFalse();
        cancelResult.Message.Should().Contain("your own");
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ShouldReturnPendingOnly()
    {
        // Arrange
        await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 2,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(70)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(72))
        });

        // Act
        var pending = await _service.GetPendingRequestsAsync();

        // Assert
        pending.Should().NotBeEmpty();
        pending.Should().OnlyContain(r => r.Status == LeaveRequestStatus.Pending);
    }

    [Fact]
    public async Task GetEmployeeRequestsAsync_ShouldReturnEmployeeRequests()
    {
        // Arrange
        await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 3,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(80)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(82))
        });

        // Act
        var requests = await _service.GetEmployeeRequestsAsync(3);

        // Assert
        requests.Should().NotBeEmpty();
        requests.Should().OnlyContain(r => r.EmployeeId == 3);
    }

    #endregion

    #region Leave Balance Tests

    [Fact]
    public async Task GetEmployeeBalanceAsync_ShouldReturnBalance()
    {
        // Act
        var balance = await _service.GetEmployeeBalanceAsync(1, DateTime.Today.Year);

        // Assert
        balance.Should().NotBeNull();
        balance.EmployeeId.Should().Be(1);
        balance.Allocations.Should().NotBeEmpty();
        balance.TotalAllocated.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetEmployeeAllocationsAsync_ShouldReturnAllocations()
    {
        // Act
        var allocations = await _service.GetEmployeeAllocationsAsync(1, DateTime.Today.Year);

        // Assert
        allocations.Should().NotBeEmpty();
        allocations.Should().Contain(a => a.LeaveTypeName == "Annual Leave");
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_WithBalance_ShouldReturnTrue()
    {
        // Act
        var hasBalance = await _service.HasSufficientBalanceAsync(1, 1, 5);

        // Assert
        hasBalance.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_ExceedsBalance_ShouldReturnFalse()
    {
        // Act
        var hasBalance = await _service.HasSufficientBalanceAsync(1, 1, 100);

        // Assert
        hasBalance.Should().BeFalse();
    }

    [Fact]
    public async Task AdjustBalanceAsync_ShouldAdjustAllocation()
    {
        // Arrange
        var balanceBefore = await _service.GetEmployeeBalanceAsync(1, DateTime.Today.Year);
        var annualBefore = balanceBefore.Allocations.First(a => a.LeaveTypeName == "Annual Leave");

        // Act
        var adjusted = await _service.AdjustBalanceAsync(1, 1, DateTime.Today.Year, 5, "Bonus days", 100);

        // Assert
        adjusted.AllocatedDays.Should().Be(annualBefore.AllocatedDays + 5);
    }

    [Fact]
    public async Task InitializeYearAllocationsAsync_ShouldCreateAllocations()
    {
        // Arrange
        var futureYear = DateTime.Today.Year + 5;

        // Act
        var count = await _service.InitializeYearAllocationsAsync(futureYear, new[] { 1 });
        var allocations = await _service.GetEmployeeAllocationsAsync(1, futureYear);

        // Assert
        count.Should().BeGreaterThan(0);
        allocations.Should().NotBeEmpty();
    }

    #endregion

    #region Calendar Tests

    [Fact]
    public async Task GetCalendarViewAsync_ShouldReturnCalendar()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var endDate = startDate.AddDays(30);

        // Act
        var calendar = await _service.GetCalendarViewAsync(startDate, endDate);

        // Assert
        calendar.Should().NotBeNull();
        calendar.StartDate.Should().Be(startDate);
        calendar.EndDate.Should().Be(endDate);
        calendar.DailyCoverage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckCoverageAsync_ShouldReturnCoverageInfo()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var endDate = startDate.AddDays(7);

        // Act
        var coverage = await _service.CheckCoverageAsync(startDate, endDate);

        // Assert
        coverage.Should().NotBeEmpty();
        coverage.Should().AllSatisfy(c =>
        {
            c.TotalEmployees.Should().BeGreaterThan(0);
            c.EmployeesAvailable.Should().BeLessOrEqualTo(c.TotalEmployees);
        });
    }

    [Fact]
    public async Task CheckCoverageConflictsAsync_ShouldReturnWarnings()
    {
        // Act
        var warnings = await _service.CheckCoverageConflictsAsync(
            1,
            DateOnly.FromDateTime(DateTime.Today.AddDays(100)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(105)));

        // Assert
        warnings.Should().NotBeNull();
        // May or may not have warnings depending on existing approved leave
    }

    #endregion

    #region Reports Tests

    [Fact]
    public async Task GenerateBalanceReportAsync_ShouldReturnReport()
    {
        // Act
        var report = await _service.GenerateBalanceReportAsync(DateTime.Today.Year);

        // Assert
        report.Should().NotBeNull();
        report.Year.Should().Be(DateTime.Today.Year);
        report.Balances.Should().NotBeEmpty();
        report.ByLeaveType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEmployeeHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 4,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(90)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(92))
        });

        // Act
        var history = await _service.GetEmployeeHistoryAsync(
            4,
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(365)));

        // Assert
        history.Should().NotBeNull();
        history.EmployeeId.Should().Be(4);
        history.Requests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateUtilizationReportAsync_ShouldReturnReport()
    {
        // Act
        var report = await _service.GenerateUtilizationReportAsync(DateTime.Today.Year);

        // Assert
        report.Should().NotBeNull();
        report.Year.Should().Be(DateTime.Today.Year);
        report.MonthlyBreakdown.Should().HaveCount(12);
    }

    #endregion

    #region Utilities Tests

    [Fact]
    public async Task CalculateWorkingDaysAsync_ShouldExcludeWeekends()
    {
        // Arrange - A week (7 calendar days)
        var monday = DateOnly.FromDateTime(DateTime.Today);
        while (monday.DayOfWeek != DayOfWeek.Monday)
            monday = monday.AddDays(1);
        var sunday = monday.AddDays(6);

        // Act
        var days = await _service.CalculateWorkingDaysAsync(monday, sunday, excludeWeekends: true, excludeHolidays: false);

        // Assert
        days.Should().Be(5); // Mon-Fri
    }

    [Fact]
    public async Task GetPublicHolidaysAsync_ShouldReturnHolidays()
    {
        // Act
        var holidays = await _service.GetPublicHolidaysAsync(2026);

        // Assert
        holidays.Should().NotBeEmpty();
        holidays.Should().Contain(new DateOnly(2026, 12, 25)); // Christmas
    }

    [Fact]
    public async Task AddPublicHolidayAsync_ShouldAddHoliday()
    {
        // Arrange
        var newHoliday = new DateOnly(2027, 1, 1);

        // Act
        var result = await _service.AddPublicHolidayAsync(newHoliday, "New Year 2027");
        var holidays = await _service.GetPublicHolidaysAsync(2027);

        // Assert
        result.Should().BeTrue();
        holidays.Should().Contain(newHoliday);
    }

    [Fact]
    public async Task AddPublicHolidayAsync_DuplicateDate_ShouldReturnFalse()
    {
        // Arrange
        var existingHoliday = new DateOnly(2026, 12, 25);

        // Act
        var result = await _service.AddPublicHolidayAsync(existingHoliday, "Duplicate");

        // Assert
        result.Should().BeFalse();
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
        var newSettings = new LeaveSettings
        {
            RequireManagerApproval = false,
            AllowBackdatedRequests = true,
            ExcludeWeekends = true
        };

        // Act
        var updated = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        updated.RequireManagerApproval.Should().BeFalse();
        updated.AllowBackdatedRequests.Should().BeTrue();
    }

    #endregion

    #region Model Tests

    [Fact]
    public void LeaveAllocation_CalculatedProperties_ShouldWork()
    {
        var allocation = new LeaveAllocation
        {
            AllocatedDays = 21,
            CarriedOverDays = 5,
            UsedDays = 10,
            PendingDays = 3
        };

        allocation.TotalAvailable.Should().Be(26);
        allocation.RemainingDays.Should().Be(16);
        allocation.AvailableForRequest.Should().Be(13);
        allocation.UtilizationPercent.Should().BeApproximately(38.46m, 0.1m);
    }

    [Fact]
    public void LeaveResult_Factories_ShouldWork()
    {
        // Success
        var request = new LeaveRequest { Id = 1 };
        var success = LeaveResult.Succeeded(request, "Created");
        success.Success.Should().BeTrue();
        success.Request.Should().Be(request);

        // Failed
        var failure = LeaveResult.Failed("Error");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Error");
    }

    [Fact]
    public void DayCoverage_CalculatedProperties_ShouldWork()
    {
        var coverage = new DayCoverage
        {
            TotalEmployees = 10,
            EmployeesOnLeave = 3
        };

        coverage.EmployeesAvailable.Should().Be(7);
    }

    [Fact]
    public void LeaveBalanceReport_CalculatedProperties_ShouldWork()
    {
        var report = new LeaveBalanceReport
        {
            TotalAllocated = 100,
            TotalUsed = 40
        };

        report.OverallUtilization.Should().Be(40);
    }

    [Fact]
    public void LeaveTypeSummary_CalculatedProperties_ShouldWork()
    {
        var summary = new LeaveTypeSummary
        {
            TotalAllocated = 100,
            TotalUsed = 25,
            TotalRemaining = 75
        };

        summary.UtilizationPercent.Should().Be(25);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task SubmitRequestAsync_ShouldRaiseEvent()
    {
        // Arrange
        LeaveRequest? submittedRequest = null;
        ((LeaveService)_service).RequestSubmitted += (sender, args) =>
        {
            submittedRequest = args.Request;
        };

        // Act
        await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(110)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(112))
        });

        // Assert
        submittedRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessApprovalAsync_Approve_ShouldRaiseEvent()
    {
        // Arrange
        LeaveRequest? approvedRequest = null;
        ((LeaveService)_service).RequestApproved += (sender, args) =>
        {
            approvedRequest = args.Request;
        };

        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(120)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(122))
        });

        // Act
        await _service.ProcessApprovalAsync(new LeaveApprovalRequest
        {
            RequestId = submitResult.Request!.Id,
            ReviewerUserId = 100,
            Approve = true
        });

        // Assert
        approvedRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelRequestAsync_ShouldRaiseEvent()
    {
        // Arrange
        LeaveRequest? cancelledRequest = null;
        ((LeaveService)_service).RequestCancelled += (sender, args) =>
        {
            cancelledRequest = args.Request;
        };

        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(130)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(132))
        });

        // Act
        await _service.CancelRequestAsync(submitResult.Request!.Id, 5);

        // Assert
        cancelledRequest.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullLeaveWorkflow_ShouldWork()
    {
        // 1. Check initial balance
        var initialBalance = await _service.GetEmployeeBalanceAsync(1, DateTime.Today.Year);
        var annualBefore = initialBalance.Allocations.First(a => a.LeaveTypeName == "Annual Leave");
        var remainingBefore = annualBefore.RemainingDays;

        // 2. Submit request
        var submitResult = await _service.SubmitRequestAsync(new LeaveRequestSubmission
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(200)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(204)),
            Reason = "Vacation"
        });
        submitResult.Success.Should().BeTrue();

        // 3. Check pending days increased
        var afterSubmit = await _service.GetEmployeeBalanceAsync(1, DateTime.Today.Year);
        var annualAfterSubmit = afterSubmit.Allocations.First(a => a.LeaveTypeName == "Annual Leave");
        annualAfterSubmit.PendingDays.Should().BeGreaterThan(0);

        // 4. Approve request
        var approvalResult = await _service.ProcessApprovalAsync(new LeaveApprovalRequest
        {
            RequestId = submitResult.Request!.Id,
            ReviewerUserId = 100,
            Approve = true
        });
        approvalResult.Success.Should().BeTrue();

        // 5. Check balance decreased
        var afterApproval = await _service.GetEmployeeBalanceAsync(1, DateTime.Today.Year);
        var annualAfterApproval = afterApproval.Allocations.First(a => a.LeaveTypeName == "Annual Leave");
        annualAfterApproval.UsedDays.Should().BeGreaterThan(annualBefore.UsedDays);
        annualAfterApproval.RemainingDays.Should().BeLessThan(remainingBefore);

        // 6. Check calendar shows approved leave
        var calendar = await _service.GetCalendarViewAsync(
            DateOnly.FromDateTime(DateTime.Today.AddDays(200)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(210)));
        calendar.Entries.Should().Contain(e => e.RequestId == submitResult.Request.Id);
    }

    #endregion
}
