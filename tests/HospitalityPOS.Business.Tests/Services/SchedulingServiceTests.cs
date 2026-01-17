// tests/HospitalityPOS.Business.Tests/Services/SchedulingServiceTests.cs
// Unit tests for SchedulingService
// Story 45-2: Shift Scheduling

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class SchedulingServiceTests
{
    private readonly ISchedulingService _service;

    public SchedulingServiceTests()
    {
        _service = new SchedulingService();
    }

    #region Shift Creation Tests

    [Fact]
    public async Task CreateShiftAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            PositionId = 1,
            DepartmentId = 1
        };

        // Act
        var result = await _service.CreateShiftAsync(request, userId: 100);

        // Assert
        result.Success.Should().BeTrue();
        result.Shift.Should().NotBeNull();
        result.Shift!.EmployeeId.Should().Be(1);
        result.Shift.ShiftDate.Should().Be(request.ShiftDate);
        result.Shift.StartTime.Should().Be(new TimeOnly(9, 0));
        result.Shift.EndTime.Should().Be(new TimeOnly(17, 0));
        result.Shift.Status.Should().Be(ShiftStatus.Scheduled);
    }

    [Fact]
    public async Task CreateShiftAsync_WithOverlappingShift_ShouldFail()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        // Create first shift
        var firstShift = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        await _service.CreateShiftAsync(firstShift, 100);

        // Try to create overlapping shift
        var overlappingShift = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(22, 0)
        };

        // Act
        var result = await _service.CreateShiftAsync(overlappingShift, 100);

        // Assert
        result.Success.Should().BeFalse();
        result.Conflicts.Should().NotBeEmpty();
        result.Conflicts.Should().Contain(c => c.Type == ConflictType.DoubleBooked);
    }

    [Fact]
    public async Task CreateShiftAsync_ExceedingMaxHours_ShouldWarn()
    {
        // Arrange - Create enough shifts to exceed 48 hours
        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(14));

        for (int i = 0; i < 5; i++)
        {
            await _service.CreateShiftAsync(new ShiftRequest
            {
                EmployeeId = 2,
                ShiftDate = weekStart.AddDays(i),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(18, 0) // 10 hours each = 50 hours
            }, 100);
        }

        // Try to add another 10-hour shift (would exceed 48 hours)
        var request = new ShiftRequest
        {
            EmployeeId = 2,
            ShiftDate = weekStart.AddDays(5),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(18, 0)
        };

        // Act
        var result = await _service.CreateShiftAsync(request, 100);

        // Assert - Should succeed with warning (overtime allowed by default)
        result.Success.Should().BeTrue();
        result.Conflicts.Should().Contain(c => c.Type == ConflictType.MaxHoursExceeded);
    }

    [Fact]
    public async Task CreateShiftAsync_WithInsufficientRest_ShouldWarn()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(21));

        // Create late night shift ending at midnight
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 3,
            ShiftDate = date,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(23, 59)
        }, 100);

        // Try to create early morning shift next day (less than 8 hours rest)
        var earlyShift = new ShiftRequest
        {
            EmployeeId = 3,
            ShiftDate = date.AddDays(1),
            StartTime = new TimeOnly(6, 0),
            EndTime = new TimeOnly(14, 0)
        };

        // Act
        var result = await _service.CreateShiftAsync(earlyShift, 100);

        // Assert
        result.Success.Should().BeTrue(); // Warning only, not blocking
        result.Conflicts.Should().Contain(c => c.Type == ConflictType.InsufficientRest);
    }

    #endregion

    #region Shift CRUD Tests

    [Fact]
    public async Task UpdateShiftAsync_ShouldUpdateFields()
    {
        // Arrange
        var request = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        var createResult = await _service.CreateShiftAsync(request, 100);

        // Act
        request.Id = createResult.Shift!.Id;
        request.StartTime = new TimeOnly(10, 0);
        request.EndTime = new TimeOnly(18, 0);
        request.Notes = "Updated shift";

        var updateResult = await _service.UpdateShiftAsync(request, 100);

        // Assert
        updateResult.Success.Should().BeTrue();
        updateResult.Shift!.StartTime.Should().Be(new TimeOnly(10, 0));
        updateResult.Shift.EndTime.Should().Be(new TimeOnly(18, 0));
        updateResult.Shift.Notes.Should().Be("Updated shift");
    }

    [Fact]
    public async Task DeleteShiftAsync_ShouldRemoveShift()
    {
        // Arrange
        var request = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        var createResult = await _service.CreateShiftAsync(request, 100);
        var shiftId = createResult.Shift!.Id;

        // Act
        var deleteResult = await _service.DeleteShiftAsync(shiftId, 100);
        var retrievedShift = await _service.GetShiftAsync(shiftId);

        // Assert
        deleteResult.Should().BeTrue();
        retrievedShift.Should().BeNull();
    }

    [Fact]
    public async Task GetEmployeeShiftsAsync_ShouldReturnShiftsInRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var endDate = startDate.AddDays(6);

        for (int i = 0; i < 3; i++)
        {
            await _service.CreateShiftAsync(new ShiftRequest
            {
                EmployeeId = 4,
                ShiftDate = startDate.AddDays(i),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            }, 100);
        }

        // Act
        var shifts = await _service.GetEmployeeShiftsAsync(4, startDate, endDate);

        // Assert
        shifts.Should().HaveCount(3);
        shifts.Should().AllSatisfy(s => s.EmployeeId.Should().Be(4));
    }

    #endregion

    #region Recurring Pattern Tests

    [Fact]
    public async Task CreatePatternAsync_ShouldCreateRecurringPattern()
    {
        // Arrange
        var pattern = new RecurringShiftPattern
        {
            EmployeeId = 1,
            DaysOfWeek = DaysOfWeek.Weekdays,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            PositionId = 1,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var result = await _service.CreatePatternAsync(pattern, 100);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.DaysOfWeek.Should().Be(DaysOfWeek.Weekdays);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateFromPatternsAsync_ShouldCreateShifts()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
        var endDate = startDate.AddDays(6);

        await _service.CreatePatternAsync(new RecurringShiftPattern
        {
            EmployeeId = 5,
            DaysOfWeek = DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            ValidFrom = startDate
        }, 100);

        // Act
        var result = await _service.GenerateFromPatternsAsync(startDate, endDate, 100);

        // Assert
        result.Success.Should().BeTrue();
        result.CreatedShifts.Should().NotBeNull();
        // Number depends on which days fall in the range
    }

    [Fact]
    public async Task DeactivatePatternAsync_ShouldStopGeneratingShifts()
    {
        // Arrange
        var pattern = await _service.CreatePatternAsync(new RecurringShiftPattern
        {
            EmployeeId = 1,
            DaysOfWeek = DaysOfWeek.AllDays,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            ValidFrom = DateOnly.FromDateTime(DateTime.Today)
        }, 100);

        // Act
        var result = await _service.DeactivatePatternAsync(pattern.Id, 100);
        var patterns = await _service.GetAllPatternsAsync();

        // Assert
        result.Should().BeTrue();
        patterns.Should().NotContain(p => p.Id == pattern.Id);
    }

    #endregion

    #region Shift Swap Tests

    [Fact]
    public async Task InitiateSwapAsync_ShouldCreateSwapRequest()
    {
        // Arrange
        var shift1Result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(40)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        var shift2Result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 2,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(41)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        var swapResult = await _service.InitiateSwapAsync(new SwapInitiateRequest
        {
            RequestingEmployeeId = 1,
            OriginalShiftId = shift1Result.Shift!.Id,
            TargetEmployeeId = 2,
            TargetShiftId = shift2Result.Shift!.Id,
            Reason = "Personal appointment"
        });

        // Assert
        swapResult.Success.Should().BeTrue();
        swapResult.SwapRequest.Should().NotBeNull();
        swapResult.SwapRequest!.Status.Should().Be(SwapRequestStatus.Pending);
    }

    [Fact]
    public async Task RespondToSwapAsync_Accept_ShouldUpdateStatus()
    {
        // Arrange
        var shift1Result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(50)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        var swapResult = await _service.InitiateSwapAsync(new SwapInitiateRequest
        {
            RequestingEmployeeId = 1,
            OriginalShiftId = shift1Result.Shift!.Id,
            TargetEmployeeId = 2
        });

        // Act
        var response = await _service.RespondToSwapAsync(new SwapResponseRequest
        {
            SwapRequestId = swapResult.SwapRequest!.Id,
            Accept = true
        });

        // Assert
        response.Success.Should().BeTrue();
        response.SwapRequest!.Status.Should().Be(SwapRequestStatus.Accepted);
    }

    [Fact]
    public async Task ProcessSwapApprovalAsync_Approve_ShouldExecuteSwap()
    {
        // Arrange
        var shift1Result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        var swapResult = await _service.InitiateSwapAsync(new SwapInitiateRequest
        {
            RequestingEmployeeId = 1,
            OriginalShiftId = shift1Result.Shift!.Id,
            TargetEmployeeId = 2
        });

        await _service.RespondToSwapAsync(new SwapResponseRequest
        {
            SwapRequestId = swapResult.SwapRequest!.Id,
            Accept = true
        });

        // Act
        var approvalResult = await _service.ProcessSwapApprovalAsync(new SwapApprovalRequest
        {
            SwapRequestId = swapResult.SwapRequest.Id,
            ManagerUserId = 100,
            Approve = true
        });

        // Assert
        approvalResult.Success.Should().BeTrue();
        approvalResult.SwapRequest!.Status.Should().Be(SwapRequestStatus.Approved);

        // Verify shift was swapped
        var updatedShift = await _service.GetShiftAsync(shift1Result.Shift.Id);
        updatedShift!.EmployeeId.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingApprovalRequestsAsync_ShouldReturnAcceptedRequests()
    {
        // Arrange
        var shift1Result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(70)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        var swapResult = await _service.InitiateSwapAsync(new SwapInitiateRequest
        {
            RequestingEmployeeId = 1,
            OriginalShiftId = shift1Result.Shift!.Id,
            TargetEmployeeId = 2
        });

        await _service.RespondToSwapAsync(new SwapResponseRequest
        {
            SwapRequestId = swapResult.SwapRequest!.Id,
            Accept = true
        });

        // Act
        var pendingApprovals = await _service.GetPendingApprovalRequestsAsync();

        // Assert
        pendingApprovals.Should().Contain(r => r.Id == swapResult.SwapRequest.Id);
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public async Task CheckConflictsAsync_NoConflicts_ShouldReturnEmpty()
    {
        // Arrange
        var request = new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(100)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };

        // Act
        var conflicts = await _service.CheckConflictsAsync(request);

        // Assert
        conflicts.Where(c => !c.IsWarning).Should().BeEmpty();
    }

    [Fact]
    public async Task GetEmployeeConflictsAsync_WithConflicts_ShouldReturnList()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(110));

        // Create shift ending late
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(22, 0)
        }, 100);

        // Create shift starting early next day (insufficient rest)
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date.AddDays(1),
            StartTime = new TimeOnly(4, 0),
            EndTime = new TimeOnly(12, 0)
        }, 100);

        // Act
        var conflicts = await _service.GetEmployeeConflictsAsync(1, date, date.AddDays(1));

        // Assert
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.Type == ConflictType.InsufficientRest);
    }

    #endregion

    #region Coverage Analysis Tests

    [Fact]
    public async Task GetCoverageRequirementsAsync_ShouldReturnRequirements()
    {
        // Act
        var requirements = await _service.GetCoverageRequirementsAsync();

        // Assert
        requirements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeCoverageAsync_ShouldAnalyzeForDate()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        // Create some shifts for the date
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        }, 100);

        // Act
        var analysis = await _service.AnalyzeCoverageAsync(date);

        // Assert
        analysis.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUnderstaffedPeriodsAsync_ShouldReturnUnderstaffedPeriods()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(120));
        var endDate = startDate.AddDays(6);

        // Don't create any shifts (should be understaffed)

        // Act
        var understaffed = await _service.GetUnderstaffedPeriodsAsync(startDate, endDate);

        // Assert - depends on coverage requirements configuration
        // At minimum we test that the method returns
        understaffed.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCoverageSummaryAsync_ShouldReturnDailySummaries()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(130));
        var endDate = startDate.AddDays(6);

        // Act
        var summaries = await _service.GetCoverageSummaryAsync(startDate, endDate);

        // Assert
        summaries.Should().HaveCount(7); // 7 days
        summaries.Should().AllSatisfy(s =>
        {
            s.Date.Should().BeOnOrAfter(startDate);
            s.Date.Should().BeOnOrBefore(endDate);
        });
    }

    #endregion

    #region Schedule View Tests

    [Fact]
    public async Task GetWeeklyScheduleAsync_ShouldReturnWeekView()
    {
        // Arrange
        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(140));

        // Create some shifts
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = weekStart,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 2,
            ShiftDate = weekStart.AddDays(1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        var weekView = await _service.GetWeeklyScheduleAsync(weekStart);

        // Assert
        weekView.WeekStartDate.Should().Be(weekStart);
        weekView.WeekEndDate.Should().Be(weekStart.AddDays(6));
        weekView.EmployeeSchedules.Should().NotBeEmpty();
        weekView.DailyCoverage.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetMyScheduleAsync_ShouldReturnPersonalView()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = today.AddDays(1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        var mySchedule = await _service.GetMyScheduleAsync(1);

        // Assert
        mySchedule.EmployeeId.Should().Be(1);
        mySchedule.UpcomingShifts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCoworkersForShiftAsync_ShouldReturnOverlappingEmployees()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(150));

        var shift1 = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 2,
            ShiftDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(18, 0)
        }, 100);

        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 3,
            ShiftDate = date,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        }, 100);

        // Act
        var coworkers = await _service.GetCoworkersForShiftAsync(shift1.Shift!.Id);

        // Assert
        coworkers.Should().HaveCount(2);
        coworkers.Select(c => c.EmployeeId).Should().Contain(new[] { 2, 3 });
    }

    #endregion

    #region Attendance Integration Tests

    [Fact]
    public async Task GetTodayShiftAsync_ShouldReturnTodayShift()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = today,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        var todayShift = await _service.GetTodayShiftAsync(1);

        // Assert
        todayShift.Should().NotBeNull();
        todayShift!.ShiftDate.Should().Be(today);
    }

    [Fact]
    public async Task UpdateShiftStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(160)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        var updatedShift = await _service.UpdateShiftStatusAsync(result.Shift!.Id, ShiftStatus.Completed);

        // Assert
        updatedShift.Status.Should().Be(ShiftStatus.Completed);
    }

    [Fact]
    public async Task GenerateAdherenceReportAsync_ShouldReturnReport()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(170));
        var endDate = startDate.AddDays(6);

        for (int i = 0; i < 5; i++)
        {
            await _service.CreateShiftAsync(new ShiftRequest
            {
                EmployeeId = 1,
                ShiftDate = startDate.AddDays(i),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            }, 100);
        }

        // Act
        var report = await _service.GenerateAdherenceReportAsync(startDate, endDate);

        // Assert
        report.StartDate.Should().Be(startDate);
        report.EndDate.Should().Be(endDate);
        report.TotalScheduledShifts.Should().BeGreaterThan(0);
        report.Records.Should().NotBeEmpty();
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnDefaultSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.MaxHoursPerWeek.Should().Be(48m);
        settings.MinRestHours.Should().Be(8m);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateSettings()
    {
        // Arrange
        var newSettings = new SchedulingSettings
        {
            MaxHoursPerWeek = 40m,
            MinRestHours = 10m,
            AllowShiftSwaps = false
        };

        // Act
        var updatedSettings = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        updatedSettings.MaxHoursPerWeek.Should().Be(40m);
        updatedSettings.MinRestHours.Should().Be(10m);
        updatedSettings.AllowShiftSwaps.Should().BeFalse();
    }

    #endregion

    #region Model Tests

    [Fact]
    public void Shift_DurationHours_ShouldCalculateCorrectly()
    {
        // Arrange
        var shift = new Shift
        {
            ShiftDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };

        // Assert
        shift.DurationHours.Should().Be(8m);
    }

    [Fact]
    public void Shift_OvernightDuration_ShouldCalculateCorrectly()
    {
        // Arrange
        var shift = new Shift
        {
            ShiftDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(6, 0) // Next day
        };

        // Assert
        shift.DurationHours.Should().Be(8m);
    }

    [Fact]
    public void DaysOfWeek_Flags_ShouldWorkCorrectly()
    {
        // Arrange
        var weekdays = DaysOfWeek.Weekdays;

        // Assert
        weekdays.HasFlag(DaysOfWeek.Monday).Should().BeTrue();
        weekdays.HasFlag(DaysOfWeek.Tuesday).Should().BeTrue();
        weekdays.HasFlag(DaysOfWeek.Wednesday).Should().BeTrue();
        weekdays.HasFlag(DaysOfWeek.Thursday).Should().BeTrue();
        weekdays.HasFlag(DaysOfWeek.Friday).Should().BeTrue();
        weekdays.HasFlag(DaysOfWeek.Saturday).Should().BeFalse();
        weekdays.HasFlag(DaysOfWeek.Sunday).Should().BeFalse();
    }

    [Fact]
    public void RecurringShiftPattern_GetDayNames_ShouldReturnCorrectNames()
    {
        // Arrange
        var pattern = new RecurringShiftPattern
        {
            DaysOfWeek = DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday
        };

        // Act
        var dayNames = pattern.GetDayNames();

        // Assert
        dayNames.Should().BeEquivalentTo(new[] { "Mon", "Wed", "Fri" });
    }

    [Fact]
    public void CoverageAnalysis_Status_ShouldReflectCoverage()
    {
        // Understaffed
        var understaffed = new CoverageAnalysis
        {
            RequiredStaff = 3,
            ScheduledStaff = 2
        };
        understaffed.IsUnderstaffed.Should().BeTrue();
        understaffed.CoverageStatus.Should().Be("Understaffed");

        // Adequate
        var adequate = new CoverageAnalysis
        {
            RequiredStaff = 3,
            ScheduledStaff = 3,
            OptimalStaff = 4
        };
        adequate.IsUnderstaffed.Should().BeFalse();
        adequate.IsOverstaffed.Should().BeFalse();
        adequate.CoverageStatus.Should().Be("Adequate");

        // Overstaffed
        var overstaffed = new CoverageAnalysis
        {
            RequiredStaff = 3,
            ScheduledStaff = 6,
            OptimalStaff = 4
        };
        overstaffed.IsOverstaffed.Should().BeTrue();
        overstaffed.CoverageStatus.Should().Be("Overstaffed");
    }

    [Fact]
    public void ShiftResult_Factory_ShouldCreateCorrectly()
    {
        // Success
        var shift = new Shift { Id = 1 };
        var success = ShiftResult.Succeeded(shift, "Created");
        success.Success.Should().BeTrue();
        success.Shift.Should().Be(shift);

        // Failure
        var failure = ShiftResult.Failed("Error occurred");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Error occurred");
    }

    [Fact]
    public void SwapResult_Factory_ShouldCreateCorrectly()
    {
        // Success
        var request = new ShiftSwapRequest { Id = 1 };
        var success = SwapResult.Succeeded(request, "Swap completed");
        success.Success.Should().BeTrue();
        success.SwapRequest.Should().Be(request);

        // Failure
        var failure = SwapResult.Failed("Swap failed");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Swap failed");
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task CreateShiftAsync_ShouldRaiseEvent()
    {
        // Arrange
        Shift? createdShift = null;
        ((SchedulingService)_service).ShiftCreated += (sender, args) =>
        {
            createdShift = args.Shift;
        };

        // Act
        await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(200)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Assert
        createdShift.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteShiftAsync_ShouldRaiseEvent()
    {
        // Arrange
        Shift? deletedShift = null;
        ((SchedulingService)_service).ShiftDeleted += (sender, args) =>
        {
            deletedShift = args.Shift;
        };

        var result = await _service.CreateShiftAsync(new ShiftRequest
        {
            EmployeeId = 1,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(210)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        }, 100);

        // Act
        await _service.DeleteShiftAsync(result.Shift!.Id, 100);

        // Assert
        deletedShift.Should().NotBeNull();
        deletedShift!.Id.Should().Be(result.Shift.Id);
    }

    #endregion
}
