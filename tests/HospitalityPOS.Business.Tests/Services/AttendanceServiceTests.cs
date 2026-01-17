// tests/HospitalityPOS.Business.Tests/Services/AttendanceServiceTests.cs
// Unit tests for AttendanceService (Enhanced)
// Story 45-1: Time and Attendance

using FluentAssertions;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Infrastructure.Data;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class AttendanceServiceTests
{
    private readonly AttendanceService _service;

    public AttendanceServiceTests()
    {
        // Create in-memory database context
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new POSDbContext(options);
        _service = new AttendanceService(context);
    }

    #region Clock In Tests

    [Fact]
    public async Task ClockInWithPinAsync_ValidPin_ReturnsSuccess()
    {
        // Arrange
        var request = new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "1234"
        };

        // Act
        var result = await _service.ClockInWithPinAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record.Should().NotBeNull();
        result.Record!.ClockInTime.Should().NotBeNull();
        result.Message.Should().Contain("Welcome");
    }

    [Fact]
    public async Task ClockInWithPinAsync_InvalidPin_ReturnsFailed()
    {
        // Arrange
        var request = new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "wrong"
        };

        // Act
        var result = await _service.ClockInWithPinAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PIN");
    }

    [Fact]
    public async Task ClockInWithPinAsync_AlreadyClockedIn_ReturnsFailed()
    {
        // Arrange
        var request = new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "1234"
        };

        // First clock in
        await _service.ClockInWithPinAsync(request);

        // Act - try to clock in again
        var result = await _service.ClockInWithPinAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ALREADY_CLOCKED_IN");
    }

    [Fact]
    public async Task ClockInWithPinAsync_LateArrival_MarksAsLate()
    {
        // Arrange
        var settings = await _service.GetSettingsAsync();
        var lateTime = DateTime.Today.Add(settings.StandardStartTime.ToTimeSpan()).AddMinutes(settings.GracePeriodMinutes + 30);

        var request = new ClockInRequest
        {
            EmployeeId = 3,
            Pin = "9012",
            ClockInTime = lateTime
        };

        // Act
        var result = await _service.ClockInWithPinAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(AttendanceStatus.Late);
    }

    [Fact]
    public async Task ClockInWithPinAsync_OnTime_MarksAsPresent()
    {
        // Arrange
        var settings = await _service.GetSettingsAsync();
        var onTimeTime = DateTime.Today.Add(settings.StandardStartTime.ToTimeSpan()).AddMinutes(-5);

        var request = new ClockInRequest
        {
            EmployeeId = 4,
            Pin = "3456",
            ClockInTime = onTimeTime
        };

        // Act
        var result = await _service.ClockInWithPinAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.Status.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public async Task ClockInWithPinAsync_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.EmployeeClockIn += (sender, args) => { eventRaised = true; };

        var request = new ClockInRequest
        {
            EmployeeId = 5,
            Pin = "7890"
        };

        // Act
        await _service.ClockInWithPinAsync(request);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Clock Out Tests

    [Fact]
    public async Task ClockOutWithPinAsync_AfterClockIn_ReturnsSuccess()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        var request = new ClockOutRequest
        {
            EmployeeId = 1,
            Pin = "1234"
        };

        // Act
        var result = await _service.ClockOutWithPinAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record.Should().NotBeNull();
        result.Record!.ClockOutTime.Should().NotBeNull();
        result.HoursWorkedToday.Should().NotBeNull();
    }

    [Fact]
    public async Task ClockOutWithPinAsync_WithoutClockIn_ReturnsFailed()
    {
        // Arrange
        var request = new ClockOutRequest
        {
            EmployeeId = 99, // Never clocked in
            Pin = "1234"
        };

        // Act
        var result = await _service.ClockOutWithPinAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_CLOCKED_IN");
    }

    [Fact]
    public async Task ClockOutWithPinAsync_InvalidPin_ReturnsFailed()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        var request = new ClockOutRequest
        {
            EmployeeId = 1,
            Pin = "wrong"
        };

        // Act
        var result = await _service.ClockOutWithPinAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PIN");
    }

    [Fact]
    public async Task ClockOutWithPinAsync_CalculatesWorkedMinutes()
    {
        // Arrange
        var clockInTime = DateTime.Now.AddHours(-8);
        await _service.ClockInWithPinAsync(new ClockInRequest
        {
            EmployeeId = 2,
            Pin = "5678",
            ClockInTime = clockInTime
        });

        // Act
        var result = await _service.ClockOutWithPinAsync(new ClockOutRequest
        {
            EmployeeId = 2,
            Pin = "5678"
        });

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.TotalWorkedMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ClockOutWithPinAsync_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.EmployeeClockOut += (sender, args) => { eventRaised = true; };

        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 3, Pin = "9012" });

        // Act
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 3, Pin = "9012" });

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Break Tests

    [Fact]
    public async Task StartBreakWithPinAsync_AfterClockIn_ReturnsSuccess()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var result = await _service.StartBreakWithPinAsync(1, "1234");

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.BreakStartTime.Should().NotBeNull();
    }

    [Fact]
    public async Task StartBreakWithPinAsync_WithoutClockIn_ReturnsFailed()
    {
        // Act
        var result = await _service.StartBreakWithPinAsync(99, "1234");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_CLOCKED_IN");
    }

    [Fact]
    public async Task StartBreakWithPinAsync_BreakAlreadyStarted_ReturnsFailed()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.StartBreakWithPinAsync(1, "1234");

        // Act
        var result = await _service.StartBreakWithPinAsync(1, "1234");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("BREAK_ALREADY_STARTED");
    }

    [Fact]
    public async Task EndBreakWithPinAsync_AfterBreakStart_ReturnsSuccess()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.StartBreakWithPinAsync(1, "1234");

        // Act
        var result = await _service.EndBreakWithPinAsync(1, "1234");

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.BreakEndTime.Should().NotBeNull();
        result.Record.TotalBreakMinutes.Should().NotBeNull();
    }

    [Fact]
    public async Task EndBreakWithPinAsync_WithoutBreakStart_ReturnsFailed()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var result = await _service.EndBreakWithPinAsync(1, "1234");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_BREAK_STARTED");
    }

    [Fact]
    public async Task BreakRaisesEvents()
    {
        // Arrange
        var breakStartRaised = false;
        var breakEndRaised = false;
        _service.EmployeeBreakStart += (sender, args) => { breakStartRaised = true; };
        _service.EmployeeBreakEnd += (sender, args) => { breakEndRaised = true; };

        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 4, Pin = "3456" });

        // Act
        await _service.StartBreakWithPinAsync(4, "3456");
        await _service.EndBreakWithPinAsync(4, "3456");

        // Assert
        breakStartRaised.Should().BeTrue();
        breakEndRaised.Should().BeTrue();
    }

    #endregion

    #region Status Tests

    [Fact]
    public async Task GetCurrentStatusAsync_NotClockedIn_ReturnsNotClockedIn()
    {
        // Act
        var status = await _service.GetCurrentStatusAsync(99);

        // Assert
        status.Should().Be(ClockStatus.NotClockedIn);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_AfterClockIn_ReturnsClockedIn()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var status = await _service.GetCurrentStatusAsync(1);

        // Assert
        status.Should().Be(ClockStatus.ClockedIn);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_OnBreak_ReturnsOnBreak()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.StartBreakWithPinAsync(1, "1234");

        // Act
        var status = await _service.GetCurrentStatusAsync(1);

        // Assert
        status.Should().Be(ClockStatus.OnBreak);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_AfterClockOut_ReturnsClockedOut()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var status = await _service.GetCurrentStatusAsync(1);

        // Assert
        status.Should().Be(ClockStatus.ClockedOut);
    }

    #endregion

    #region Manager Operations Tests

    [Fact]
    public async Task EditRecordAsync_ValidManagerPin_ReturnsSuccess()
    {
        // Arrange
        var clockInResult = await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        var recordId = clockInResult.Record!.Id;

        var request = new AttendanceEditRequest
        {
            AttendanceRecordId = recordId,
            ManagerUserId = 100,
            ManagerPin = "0000",
            NewClockInTime = DateTime.Now.AddHours(-9),
            Reason = "Employee forgot to clock in on time"
        };

        // Act
        var result = await _service.EditRecordAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task EditRecordAsync_InvalidManagerPin_ReturnsFailed()
    {
        // Arrange
        var clockInResult = await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        var request = new AttendanceEditRequest
        {
            AttendanceRecordId = clockInResult.Record!.Id,
            ManagerUserId = 100,
            ManagerPin = "wrong",
            NewClockInTime = DateTime.Now.AddHours(-9),
            Reason = "Test"
        };

        // Act
        var result = await _service.EditRecordAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_MANAGER_PIN");
    }

    [Fact]
    public async Task EditRecordAsync_NoReason_ReturnsFailed()
    {
        // Arrange
        var clockInResult = await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        var request = new AttendanceEditRequest
        {
            AttendanceRecordId = clockInResult.Record!.Id,
            ManagerUserId = 100,
            ManagerPin = "0000",
            NewClockInTime = DateTime.Now.AddHours(-9),
            Reason = ""
        };

        // Act
        var result = await _service.EditRecordAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("REASON_REQUIRED");
    }

    [Fact]
    public async Task AddMissedPunchAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new MissedPunchRequest
        {
            EmployeeId = 5,
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
            PunchType = AttendanceEventType.ClockIn,
            PunchTime = DateTime.Now.AddDays(-1).Date.AddHours(9),
            ManagerUserId = 100,
            ManagerPin = "0000",
            Reason = "Employee forgot to clock in"
        };

        // Act
        var result = await _service.AddMissedPunchAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Record!.ClockInTime.Should().NotBeNull();
        result.Record.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAbsentAsync_CreatesAbsenceRecord()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

        // Act
        var record = await _service.MarkAbsentAsync(5, date, "Sick leave", 100);

        // Assert
        record.Should().NotBeNull();
        record.Status.Should().Be(AttendanceStatus.Absent);
        record.Notes.Should().Be("Sick leave");
    }

    [Fact]
    public async Task GetEditHistoryAsync_WithEdits_ReturnsHistory()
    {
        // Arrange
        var clockInResult = await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        var recordId = clockInResult.Record!.Id;

        await _service.EditRecordAsync(new AttendanceEditRequest
        {
            AttendanceRecordId = recordId,
            ManagerUserId = 100,
            ManagerPin = "0000",
            NewClockInTime = DateTime.Now.AddHours(-9),
            Reason = "Edit 1"
        });

        // Act
        var history = await _service.GetEditHistoryAsync(recordId);

        // Assert
        history.Should().HaveCount(1);
        history[0].Reason.Should().Be("Edit 1");
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetEmployeesOnShiftAsync_WithClockedInEmployees_ReturnsList()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 2, Pin = "5678" });

        // Act
        var onShift = await _service.GetEmployeesOnShiftAsync();

        // Assert
        onShift.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetTodaySummaryAsync_ReturnsValidSummary()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var summary = await _service.GetTodaySummaryAsync();

        // Assert
        summary.Should().NotBeNull();
        summary.Date.Should().Be(DateOnly.FromDateTime(DateTime.Now));
        summary.EmployeesOnShift.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLateArrivalsAsync_ReturnsLateEmployees()
    {
        // Arrange
        var settings = await _service.GetSettingsAsync();
        var lateTime = DateTime.Today.Add(settings.StandardStartTime.ToTimeSpan()).AddMinutes(settings.GracePeriodMinutes + 30);

        await _service.ClockInWithPinAsync(new ClockInRequest
        {
            EmployeeId = 3,
            Pin = "9012",
            ClockInTime = lateTime
        });

        // Act
        var lateArrivals = await _service.GetLateArrivalsAsync(DateOnly.FromDateTime(DateTime.Now));

        // Assert
        lateArrivals.Should().Contain(r => r.EmployeeId == 3);
    }

    #endregion

    #region Reporting Tests

    [Fact]
    public async Task GenerateReportAsync_ReturnsValidReport()
    {
        // Arrange - sample data should have records from yesterday
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var report = await _service.GenerateReportAsync(startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.StartDate.Should().Be(startDate);
        report.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task GenerateEmployeeReportAsync_ReturnsDetailedReport()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 1, Pin = "1234" });

        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var report = await _service.GenerateEmployeeReportAsync(1, startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.EmployeeId.Should().Be(1);
        report.DailyEntries.Should().NotBeEmpty();
        report.Summary.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateSummaryAsync_CalculatesCorrectTotals()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "1234",
            ClockInTime = DateTime.Now.AddHours(-8)
        });
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 1, Pin = "1234" });

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var summary = await _service.CalculateSummaryAsync(1, startDate, endDate);

        // Assert
        summary.Should().NotBeNull();
        summary.TotalHours.Should().BeGreaterThan(0);
    }

    #endregion

    #region Payroll Integration Tests

    [Fact]
    public async Task ExportForPayrollAsync_ReturnsValidExport()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "1234",
            ClockInTime = DateTime.Now.AddHours(-8)
        });
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 1, Pin = "1234" });

        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var export = await _service.ExportForPayrollAsync(startDate, endDate);

        // Assert
        export.Should().NotBeNull();
        export.PeriodStart.Should().Be(startDate);
        export.PeriodEnd.Should().Be(endDate);
        export.Employees.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateHoursAsync_ReturnsCorrectHours()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest
        {
            EmployeeId = 1,
            Pin = "1234",
            ClockInTime = DateTime.Now.AddHours(-10) // 10 hours ago, should have overtime
        });
        await _service.ClockOutWithPinAsync(new ClockOutRequest { EmployeeId = 1, Pin = "1234" });

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var (regularHours, overtimeHours) = await _service.CalculateHoursAsync(1, startDate, endDate);

        // Assert
        regularHours.Should().BeGreaterThan(0);
        (regularHours + overtimeHours).Should().BeGreaterThan(0);
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.GracePeriodMinutes.Should().BeGreaterThan(0);
        settings.OvertimeThresholdHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesSettings()
    {
        // Arrange
        var newSettings = new AttendanceSettings
        {
            GracePeriodMinutes = 30,
            OvertimeThresholdHours = 9,
            StandardStartTime = new TimeOnly(8, 0),
            StandardEndTime = new TimeOnly(18, 0)
        };

        // Act
        var result = await _service.UpdateSettingsAsync(newSettings);

        // Assert
        result.GracePeriodMinutes.Should().Be(30);
        result.OvertimeThresholdHours.Should().Be(9);

        // Verify persistence
        var retrieved = await _service.GetSettingsAsync();
        retrieved.GracePeriodMinutes.Should().Be(30);
    }

    #endregion

    #region Record Retrieval Tests

    [Fact]
    public async Task GetTodayRecordAsync_WithRecord_ReturnsRecord()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });

        // Act
        var record = await _service.GetTodayRecordAsync(1);

        // Assert
        record.Should().NotBeNull();
        record!.EmployeeId.Should().Be(1);
    }

    [Fact]
    public async Task GetTodayRecordAsync_NoRecord_ReturnsNull()
    {
        // Act
        var record = await _service.GetTodayRecordAsync(99);

        // Assert
        record.Should().BeNull();
    }

    [Fact]
    public async Task GetRecordsAsync_ReturnsRecordsInDateRange()
    {
        // Arrange - sample data has records from yesterday
        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var records = await _service.GetRecordsAsync(1, startDate, endDate);

        // Assert
        records.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecordsForDateAsync_ReturnsAllRecordsForDate()
    {
        // Arrange
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 1, Pin = "1234" });
        await _service.ClockInWithPinAsync(new ClockInRequest { EmployeeId = 2, Pin = "5678" });

        // Act
        var records = await _service.GetRecordsForDateAsync(DateOnly.FromDateTime(DateTime.Now));

        // Assert
        records.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion

    #region AttendanceRecord Model Tests

    [Fact]
    public void AttendanceRecord_CurrentStatus_Derived()
    {
        // Arrange
        var notClockedIn = new AttendanceRecord();
        var clockedIn = new AttendanceRecord { ClockInTime = DateTime.Now };
        var onBreak = new AttendanceRecord { ClockInTime = DateTime.Now, BreakStartTime = DateTime.Now };
        var clockedOut = new AttendanceRecord { ClockInTime = DateTime.Now, ClockOutTime = DateTime.Now };

        // Assert
        notClockedIn.CurrentStatus.Should().Be(ClockStatus.NotClockedIn);
        clockedIn.CurrentStatus.Should().Be(ClockStatus.ClockedIn);
        onBreak.CurrentStatus.Should().Be(ClockStatus.OnBreak);
        clockedOut.CurrentStatus.Should().Be(ClockStatus.ClockedOut);
    }

    [Fact]
    public void AttendanceRecord_MinutesLate_Calculated()
    {
        // Arrange
        var record = new AttendanceRecord
        {
            ClockInTime = DateTime.Today.AddHours(9).AddMinutes(30),
            ScheduledStartTime = new TimeOnly(9, 0)
        };

        // Act
        var minutesLate = record.MinutesLate;

        // Assert
        minutesLate.Should().Be(30);
    }

    [Fact]
    public void AttendanceRecord_WorkedHoursDisplay_Formatted()
    {
        // Arrange
        var record = new AttendanceRecord { TotalWorkedMinutes = 500 }; // 8h 20m

        // Act
        var display = record.WorkedHoursDisplay;

        // Assert
        display.Should().Be("8h 20m");
    }

    #endregion

    #region ClockResult Tests

    [Fact]
    public void ClockResult_Succeeded_CreatesSuccessResult()
    {
        // Arrange
        var record = new AttendanceRecord { Id = 1 };

        // Act
        var result = ClockResult.Succeeded(record, "Welcome!", "9:00 AM");

        // Assert
        result.Success.Should().BeTrue();
        result.Record.Should().Be(record);
        result.Message.Should().Be("Welcome!");
    }

    [Fact]
    public void ClockResult_Failed_CreatesFailedResult()
    {
        // Act
        var result = ClockResult.Failed("Invalid PIN", "INVALID_PIN");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid PIN");
        result.ErrorCode.Should().Be("INVALID_PIN");
    }

    #endregion

    #region AttendanceSummary Tests

    [Fact]
    public void AttendanceSummary_Status_Calculated()
    {
        // Arrange
        var goodStanding = new HospitalityPOS.Core.Models.HR.AttendanceSummary { DaysLate = 0, DaysAbsent = 0 };
        var minorIssues = new HospitalityPOS.Core.Models.HR.AttendanceSummary { DaysLate = 2, DaysAbsent = 1 };
        var needsAttention = new HospitalityPOS.Core.Models.HR.AttendanceSummary { DaysLate = 5, DaysAbsent = 3 };

        // Assert
        goodStanding.Status.Should().Be("Good Standing");
        minorIssues.Status.Should().Be("Minor Issues");
        needsAttention.Status.Should().Be("Needs Attention");
    }

    [Fact]
    public void AttendanceSummary_AverageHoursPerDay_Calculated()
    {
        // Arrange
        var summary = new HospitalityPOS.Core.Models.HR.AttendanceSummary
        {
            TotalHours = 40,
            DaysPresent = 5
        };

        // Act
        var average = summary.AverageHoursPerDay;

        // Assert
        average.Should().Be(8);
    }

    #endregion
}
