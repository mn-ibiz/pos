// src/HospitalityPOS.Infrastructure/Services/AttendanceService.cs
// Service implementation for employee attendance tracking
// Story 45-1: Time and Attendance (Enhanced)

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;
using HospitalityPOS.Infrastructure.Data;
using AttendanceSummary = HospitalityPOS.Core.Models.HR.AttendanceSummary;
using AttendanceSummaryLegacy = HospitalityPOS.Core.Interfaces.AttendanceSummary;
using AttendanceStatusEntity = HospitalityPOS.Core.Entities.AttendanceStatus;
using AttendanceStatusModel = HospitalityPOS.Core.Models.HR.AttendanceStatus;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for employee attendance tracking.
/// Handles clock in/out with PIN, break tracking, reporting, and payroll integration.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly POSDbContext _context;
    private AttendanceSettings _settings = new();
    private readonly Dictionary<int, AttendanceRecord> _records = new();
    private readonly Dictionary<int, List<AttendanceEdit>> _editHistory = new();
    private readonly Dictionary<int, string> _employeePins = new();
    private readonly Dictionary<int, string> _managerPins = new();
    private int _nextRecordId = 1;

    private const decimal StandardHoursPerDay = 8m;

    #region Events

    public event EventHandler<AttendanceEventArgs>? EmployeeClockIn;
    public event EventHandler<AttendanceEventArgs>? EmployeeClockOut;
    public event EventHandler<AttendanceEventArgs>? EmployeeBreakStart;
    public event EventHandler<AttendanceEventArgs>? EmployeeBreakEnd;

    #endregion

    public AttendanceService(POSDbContext context)
    {
        _context = context;
        InitializeSampleData();
    }

    #region Clock Operations (Enhanced - PIN based)

    public async Task<ClockResult> ClockInWithPinAsync(ClockInRequest request)
    {
        // Validate PIN
        if (!ValidateEmployeePin(request.EmployeeId, request.Pin))
        {
            return ClockResult.Failed("Invalid PIN. Please try again.", "INVALID_PIN");
        }

        // Check if already clocked in
        var today = DateOnly.FromDateTime(DateTime.Now);
        var existingRecord = await GetRecordAsync(request.EmployeeId, today);

        if (existingRecord != null && existingRecord.ClockInTime.HasValue)
        {
            return ClockResult.Failed("You have already clocked in today.", "ALREADY_CLOCKED_IN");
        }

        var now = request.ClockInTime ?? DateTime.Now;
        var employeeName = await GetEmployeeNameAsync(request.EmployeeId);

        AttendanceRecord record;
        if (existingRecord != null)
        {
            existingRecord.ClockInTime = now;
            existingRecord.UpdatedAt = DateTime.UtcNow;
            record = existingRecord;
        }
        else
        {
            record = new AttendanceRecord
            {
                Id = _nextRecordId++,
                EmployeeId = request.EmployeeId,
                EmployeeName = employeeName,
                AttendanceDate = today,
                ClockInTime = now,
                Notes = request.Notes,
                ScheduledStartTime = _settings.StandardStartTime,
                ScheduledEndTime = _settings.StandardEndTime,
                CreatedAt = DateTime.UtcNow
            };
            _records[record.Id] = record;
        }

        // Determine if late
        var clockInTimeOnly = TimeOnly.FromDateTime(now);
        var gracePeriod = TimeSpan.FromMinutes(_settings.GracePeriodMinutes);
        var lateThreshold = _settings.StandardStartTime.Add(gracePeriod);

        if (clockInTimeOnly > lateThreshold)
        {
            record.Status = AttendanceStatusModel.Late;
        }
        else
        {
            record.Status = AttendanceStatusModel.Present;
        }

        // Raise event
        EmployeeClockIn?.Invoke(this, new AttendanceEventArgs
        {
            EmployeeId = request.EmployeeId,
            EmployeeName = employeeName,
            EventType = AttendanceEventType.ClockIn,
            Timestamp = now,
            Record = record
        });

        return ClockResult.Succeeded(record, $"Welcome {employeeName}! Clocked in at {now:h:mm tt}", now.ToString("h:mm tt"));
    }

    public async Task<ClockResult> ClockOutWithPinAsync(ClockOutRequest request)
    {
        // Validate PIN
        if (!ValidateEmployeePin(request.EmployeeId, request.Pin))
        {
            return ClockResult.Failed("Invalid PIN. Please try again.", "INVALID_PIN");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = await GetRecordAsync(request.EmployeeId, today);

        if (record == null || !record.ClockInTime.HasValue)
        {
            return ClockResult.Failed("You must clock in before clocking out.", "NOT_CLOCKED_IN");
        }

        if (record.ClockOutTime.HasValue)
        {
            return ClockResult.Failed("You have already clocked out today.", "ALREADY_CLOCKED_OUT");
        }

        var now = request.ClockOutTime ?? DateTime.Now;
        record.ClockOutTime = now;
        record.UpdatedAt = DateTime.UtcNow;

        // Calculate worked minutes
        var totalMinutes = (int)(now - record.ClockInTime!.Value).TotalMinutes;
        var breakMinutes = record.TotalBreakMinutes ?? 0;
        record.TotalWorkedMinutes = totalMinutes - breakMinutes;

        // Add notes if provided
        if (!string.IsNullOrEmpty(request.Notes))
        {
            record.Notes = string.IsNullOrEmpty(record.Notes)
                ? request.Notes
                : $"{record.Notes}; {request.Notes}";
        }

        var employeeName = await GetEmployeeNameAsync(request.EmployeeId);

        // Raise event
        EmployeeClockOut?.Invoke(this, new AttendanceEventArgs
        {
            EmployeeId = request.EmployeeId,
            EmployeeName = employeeName,
            EventType = AttendanceEventType.ClockOut,
            Timestamp = now,
            Record = record
        });

        var hoursWorked = record.WorkedHoursDisplay;
        var result = ClockResult.Succeeded(record, $"Goodbye {employeeName}! Worked {hoursWorked} today.", now.ToString("h:mm tt"));
        result.HoursWorkedToday = hoursWorked;
        return result;
    }

    public async Task<ClockResult> StartBreakWithPinAsync(int employeeId, string pin)
    {
        if (!ValidateEmployeePin(employeeId, pin))
        {
            return ClockResult.Failed("Invalid PIN.", "INVALID_PIN");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = await GetRecordAsync(employeeId, today);

        if (record == null || !record.ClockInTime.HasValue)
        {
            return ClockResult.Failed("You must clock in first.", "NOT_CLOCKED_IN");
        }

        if (record.BreakStartTime.HasValue && !record.BreakEndTime.HasValue)
        {
            return ClockResult.Failed("Break already started.", "BREAK_ALREADY_STARTED");
        }

        var now = DateTime.Now;
        record.BreakStartTime = now;
        record.BreakEndTime = null;
        record.UpdatedAt = DateTime.UtcNow;

        var employeeName = await GetEmployeeNameAsync(employeeId);

        EmployeeBreakStart?.Invoke(this, new AttendanceEventArgs
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EventType = AttendanceEventType.BreakStart,
            Timestamp = now,
            Record = record
        });

        return ClockResult.Succeeded(record, $"Break started at {now:h:mm tt}", now.ToString("h:mm tt"));
    }

    public async Task<ClockResult> EndBreakWithPinAsync(int employeeId, string pin)
    {
        if (!ValidateEmployeePin(employeeId, pin))
        {
            return ClockResult.Failed("Invalid PIN.", "INVALID_PIN");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = await GetRecordAsync(employeeId, today);

        if (record == null || !record.BreakStartTime.HasValue)
        {
            return ClockResult.Failed("No break to end.", "NO_BREAK_STARTED");
        }

        if (record.BreakEndTime.HasValue)
        {
            return ClockResult.Failed("Break already ended.", "BREAK_ALREADY_ENDED");
        }

        var now = DateTime.Now;
        record.BreakEndTime = now;

        // Calculate break minutes
        var breakMinutes = (int)(now - record.BreakStartTime!.Value).TotalMinutes;
        record.TotalBreakMinutes = (record.TotalBreakMinutes ?? 0) + breakMinutes;
        record.UpdatedAt = DateTime.UtcNow;

        var employeeName = await GetEmployeeNameAsync(employeeId);

        EmployeeBreakEnd?.Invoke(this, new AttendanceEventArgs
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EventType = AttendanceEventType.BreakEnd,
            Timestamp = now,
            Record = record
        });

        return ClockResult.Succeeded(record, $"Break ended at {now:h:mm tt}. Break duration: {breakMinutes} minutes.", now.ToString("h:mm tt"));
    }

    public Task<ClockStatus> GetCurrentStatusAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = _records.Values.FirstOrDefault(r => r.EmployeeId == employeeId && r.AttendanceDate == today);

        if (record == null) return Task.FromResult(ClockStatus.NotClockedIn);

        return Task.FromResult(record.CurrentStatus);
    }

    #endregion

    #region Legacy Clock Operations (backward compatibility)

    public async Task<Attendance> ClockInAsync(int employeeId, TimeSpan? clockInTime = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var existing = await GetTodayAttendanceAsync(employeeId, cancellationToken);

        if (existing != null)
        {
            if (existing.ClockIn.HasValue)
            {
                throw new InvalidOperationException("Employee has already clocked in today.");
            }
            existing.ClockIn = clockInTime ?? DateTime.Now.TimeOfDay;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var attendance = new Attendance
        {
            EmployeeId = employeeId,
            AttendanceDate = today,
            ClockIn = clockInTime ?? DateTime.Now.TimeOfDay,
            Status = AttendanceStatusEntity.Present
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public async Task<Attendance> ClockOutAsync(int employeeId, TimeSpan? clockOutTime = null, CancellationToken cancellationToken = default)
    {
        var attendance = await GetTodayAttendanceAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("No attendance record found for today. Please clock in first.");

        if (!attendance.ClockIn.HasValue)
        {
            throw new InvalidOperationException("Cannot clock out without clocking in first.");
        }

        attendance.ClockOut = clockOutTime ?? DateTime.Now.TimeOfDay;
        var hoursWorked = CalculateHoursWorked(attendance);
        attendance.HoursWorked = hoursWorked;

        if (hoursWorked > StandardHoursPerDay)
        {
            attendance.OvertimeHours = hoursWorked - StandardHoursPerDay;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public async Task<Attendance> StartBreakAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var attendance = await GetTodayAttendanceAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("No attendance record found for today.");

        if (attendance.BreakStart.HasValue)
        {
            throw new InvalidOperationException("Break already started.");
        }

        attendance.BreakStart = DateTime.Now.TimeOfDay;
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public async Task<Attendance> EndBreakAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var attendance = await GetTodayAttendanceAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("No attendance record found for today.");

        if (!attendance.BreakStart.HasValue)
        {
            throw new InvalidOperationException("Break not started.");
        }

        attendance.BreakEnd = DateTime.Now.TimeOfDay;
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    #endregion

    #region Attendance Records (Enhanced)

    public Task<AttendanceRecord?> GetTodayRecordAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return GetRecordAsync(employeeId, today);
    }

    public Task<AttendanceRecord?> GetRecordAsync(int employeeId, DateOnly date)
    {
        var record = _records.Values.FirstOrDefault(r => r.EmployeeId == employeeId && r.AttendanceDate == date);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<AttendanceRecord>> GetRecordsAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var records = _records.Values
            .Where(r => r.EmployeeId == employeeId && r.AttendanceDate >= startDate && r.AttendanceDate <= endDate)
            .OrderByDescending(r => r.AttendanceDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(records);
    }

    public Task<IReadOnlyList<AttendanceRecord>> GetRecordsForDateAsync(DateOnly date)
    {
        var records = _records.Values
            .Where(r => r.AttendanceDate == date)
            .OrderBy(r => r.EmployeeName)
            .ToList();

        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(records);
    }

    #endregion

    #region Legacy Attendance Records (backward compatibility)

    public async Task<Attendance?> GetTodayAttendanceAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today, cancellationToken);
    }

    public async Task<Attendance?> GetAttendanceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendanceByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.AttendanceDate == date.Date)
            .OrderBy(a => a.Employee.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetEmployeeAttendanceAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId &&
                       a.AttendanceDate >= startDate.Date &&
                       a.AttendanceDate <= endDate.Date)
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Attendance> CreateManualAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        attendance.IsManualEntry = true;

        if (attendance.ClockIn.HasValue && attendance.ClockOut.HasValue)
        {
            attendance.HoursWorked = CalculateHoursWorked(attendance);
            if (attendance.HoursWorked > StandardHoursPerDay)
            {
                attendance.OvertimeHours = attendance.HoursWorked - StandardHoursPerDay;
            }
        }

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public async Task<Attendance> UpdateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        if (attendance.ClockIn.HasValue && attendance.ClockOut.HasValue)
        {
            attendance.HoursWorked = CalculateHoursWorked(attendance);
            if (attendance.HoursWorked > StandardHoursPerDay)
            {
                attendance.OvertimeHours = attendance.HoursWorked - StandardHoursPerDay;
            }
            else
            {
                attendance.OvertimeHours = 0;
            }
        }

        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public async Task<bool> DeleteAttendanceAsync(int id, CancellationToken cancellationToken = default)
    {
        var attendance = await _context.Attendances.FindAsync([id], cancellationToken);
        if (attendance == null) return false;

        _context.Attendances.Remove(attendance);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Manager Operations

    public async Task<ClockResult> EditRecordAsync(AttendanceEditRequest request)
    {
        // Validate manager
        if (!await ValidateManagerPinAsync(request.ManagerUserId, request.ManagerPin))
        {
            return ClockResult.Failed("Invalid manager PIN.", "INVALID_MANAGER_PIN");
        }

        if (!_records.TryGetValue(request.AttendanceRecordId, out var record))
        {
            return ClockResult.Failed("Record not found.", "RECORD_NOT_FOUND");
        }

        if (string.IsNullOrEmpty(request.Reason))
        {
            return ClockResult.Failed("Reason is required for edits.", "REASON_REQUIRED");
        }

        var edits = new List<AttendanceEdit>();

        // Edit clock in time
        if (request.NewClockInTime.HasValue && request.NewClockInTime != record.ClockInTime)
        {
            edits.Add(CreateEdit(record.Id, request.ManagerUserId, "ClockInTime",
                record.ClockInTime?.ToString("g"), request.NewClockInTime.Value.ToString("g"), request.Reason));
            record.ClockInTime = request.NewClockInTime;
        }

        // Edit clock out time
        if (request.NewClockOutTime.HasValue && request.NewClockOutTime != record.ClockOutTime)
        {
            edits.Add(CreateEdit(record.Id, request.ManagerUserId, "ClockOutTime",
                record.ClockOutTime?.ToString("g"), request.NewClockOutTime.Value.ToString("g"), request.Reason));
            record.ClockOutTime = request.NewClockOutTime;
        }

        // Edit break times
        if (request.NewBreakStartTime.HasValue)
        {
            edits.Add(CreateEdit(record.Id, request.ManagerUserId, "BreakStartTime",
                record.BreakStartTime?.ToString("g"), request.NewBreakStartTime.Value.ToString("g"), request.Reason));
            record.BreakStartTime = request.NewBreakStartTime;
        }

        if (request.NewBreakEndTime.HasValue)
        {
            edits.Add(CreateEdit(record.Id, request.ManagerUserId, "BreakEndTime",
                record.BreakEndTime?.ToString("g"), request.NewBreakEndTime.Value.ToString("g"), request.Reason));
            record.BreakEndTime = request.NewBreakEndTime;
        }

        // Recalculate totals
        RecalculateTotals(record);
        record.IsEdited = true;
        record.UpdatedAt = DateTime.UtcNow;

        // Store edit history
        if (!_editHistory.ContainsKey(record.Id))
        {
            _editHistory[record.Id] = new List<AttendanceEdit>();
        }
        _editHistory[record.Id].AddRange(edits);

        return ClockResult.Succeeded(record, $"Record updated successfully. {edits.Count} field(s) changed.", DateTime.Now.ToString("g"));
    }

    public async Task<ClockResult> AddMissedPunchAsync(MissedPunchRequest request)
    {
        if (!await ValidateManagerPinAsync(request.ManagerUserId, request.ManagerPin))
        {
            return ClockResult.Failed("Invalid manager PIN.", "INVALID_MANAGER_PIN");
        }

        var record = await GetRecordAsync(request.EmployeeId, request.Date);
        var employeeName = await GetEmployeeNameAsync(request.EmployeeId);

        if (record == null)
        {
            record = new AttendanceRecord
            {
                Id = _nextRecordId++,
                EmployeeId = request.EmployeeId,
                EmployeeName = employeeName,
                AttendanceDate = request.Date,
                IsEdited = true,
                CreatedAt = DateTime.UtcNow
            };
            _records[record.Id] = record;
        }

        switch (request.PunchType)
        {
            case AttendanceEventType.ClockIn:
                record.ClockInTime = request.PunchTime;
                break;
            case AttendanceEventType.ClockOut:
                record.ClockOutTime = request.PunchTime;
                break;
            case AttendanceEventType.BreakStart:
                record.BreakStartTime = request.PunchTime;
                break;
            case AttendanceEventType.BreakEnd:
                record.BreakEndTime = request.PunchTime;
                break;
        }

        RecalculateTotals(record);
        record.UpdatedAt = DateTime.UtcNow;

        // Log the edit
        var edit = CreateEdit(record.Id, request.ManagerUserId, request.PunchType.ToString(),
            null, request.PunchTime.ToString("g"), request.Reason);

        if (!_editHistory.ContainsKey(record.Id))
        {
            _editHistory[record.Id] = new List<AttendanceEdit>();
        }
        _editHistory[record.Id].Add(edit);

        return ClockResult.Succeeded(record, $"Missed {request.PunchType} added successfully.", request.PunchTime.ToString("h:mm tt"));
    }

    public async Task<AttendanceRecord> MarkAbsentAsync(int employeeId, DateOnly date, string reason, int managerUserId)
    {
        var employeeName = await GetEmployeeNameAsync(employeeId);
        var record = new AttendanceRecord
        {
            Id = _nextRecordId++,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            AttendanceDate = date,
            Status = AttendanceStatusModel.Absent,
            Notes = reason,
            CreatedAt = DateTime.UtcNow
        };

        _records[record.Id] = record;
        return record;
    }

    public Task<IReadOnlyList<AttendanceEdit>> GetEditHistoryAsync(int recordId)
    {
        if (_editHistory.TryGetValue(recordId, out var edits))
        {
            return Task.FromResult<IReadOnlyList<AttendanceEdit>>(edits);
        }
        return Task.FromResult<IReadOnlyList<AttendanceEdit>>(new List<AttendanceEdit>());
    }

    public Task<bool> ValidateManagerPinAsync(int userId, string pin)
    {
        // In production, validate against database with role check
        return Task.FromResult(_managerPins.TryGetValue(userId, out var storedPin) && storedPin == pin);
    }

    #endregion

    #region Dashboard & Status

    public Task<IReadOnlyList<EmployeeOnShift>> GetEmployeesOnShiftAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = DateTime.Now;

        var onShift = _records.Values
            .Where(r => r.AttendanceDate == today && r.ClockInTime.HasValue && !r.ClockOutTime.HasValue)
            .Select(r => new EmployeeOnShift
            {
                EmployeeId = r.EmployeeId,
                EmployeeName = r.EmployeeName,
                ClockInTime = r.ClockInTime!.Value,
                Status = r.CurrentStatus,
                IsLate = r.Status == AttendanceStatusModel.Late,
                MinutesLate = r.MinutesLate,
                HoursWorkedSoFar = (decimal)(now - r.ClockInTime!.Value).TotalHours
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<EmployeeOnShift>>(onShift);
    }

    public async Task<TodayAttendanceSummary> GetTodaySummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var onShift = await GetEmployeesOnShiftAsync();

        return new TodayAttendanceSummary
        {
            Date = today,
            EmployeesOnShift = onShift.ToList(),
            ExpectedEmployees = 10 // Would come from schedule in production
        };
    }

    public Task<IReadOnlyList<AttendanceRecord>> GetLateArrivalsAsync(DateOnly date)
    {
        var records = _records.Values
            .Where(r => r.AttendanceDate == date && r.Status == AttendanceStatusModel.Late)
            .ToList();

        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(records);
    }

    public Task<IReadOnlyList<AttendanceRecord>> GetEarlyDeparturesAsync(DateOnly date)
    {
        var records = _records.Values
            .Where(r => r.AttendanceDate == date &&
                        r.ClockOutTime.HasValue &&
                        r.ScheduledEndTime.HasValue &&
                        TimeOnly.FromDateTime(r.ClockOutTime.Value) < r.ScheduledEndTime.Value)
            .ToList();

        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(records);
    }

    #endregion

    #region Reporting (Enhanced)

    public async Task<AttendanceReport> GenerateReportAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null)
    {
        var filteredRecords = _records.Values
            .Where(r => r.AttendanceDate >= startDate && r.AttendanceDate <= endDate);

        if (employeeIds != null)
        {
            var idSet = employeeIds.ToHashSet();
            filteredRecords = filteredRecords.Where(r => idSet.Contains(r.EmployeeId));
        }

        var groupedByEmployee = filteredRecords.GroupBy(r => r.EmployeeId);

        var summaries = new List<AttendanceSummary>();
        foreach (var group in groupedByEmployee)
        {
            var summary = await CalculateSummaryAsync(group.Key, startDate, endDate);
            summaries.Add(summary);
        }

        return new AttendanceReport
        {
            StartDate = startDate,
            EndDate = endDate,
            EmployeeSummaries = summaries,
            GeneratedAt = DateTime.Now
        };
    }

    public async Task<EmployeeAttendanceReport> GenerateEmployeeReportAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var records = await GetRecordsAsync(employeeId, startDate, endDate);
        var summary = await CalculateSummaryAsync(employeeId, startDate, endDate);
        var employeeName = await GetEmployeeNameAsync(employeeId);

        var dailyEntries = records.Select(r => new DailyAttendanceEntry
        {
            Date = r.AttendanceDate,
            ClockIn = r.ClockInTime,
            ClockOut = r.ClockOutTime,
            BreakMinutes = r.TotalBreakMinutes ?? 0,
            HoursWorked = (r.TotalWorkedMinutes ?? 0) / 60m,
            RegularHours = Math.Min((r.TotalWorkedMinutes ?? 0) / 60m, _settings.OvertimeThresholdHours),
            OvertimeHours = Math.Max(0, (r.TotalWorkedMinutes ?? 0) / 60m - _settings.OvertimeThresholdHours),
            Status = r.Status,
            Notes = r.Notes
        }).ToList();

        // Get all edit history for this employee's records
        var editHistory = new List<AttendanceEdit>();
        foreach (var record in records)
        {
            if (_editHistory.TryGetValue(record.Id, out var edits))
            {
                editHistory.AddRange(edits);
            }
        }

        return new EmployeeAttendanceReport
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            StartDate = startDate,
            EndDate = endDate,
            DailyEntries = dailyEntries,
            Summary = summary,
            EditHistory = editHistory
        };
    }

    public async Task<AttendanceSummary> CalculateSummaryAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var records = await GetRecordsAsync(employeeId, startDate, endDate);
        var employeeName = await GetEmployeeNameAsync(employeeId);

        var totalMinutes = records.Sum(r => r.TotalWorkedMinutes ?? 0);
        var breakMinutes = records.Sum(r => r.TotalBreakMinutes ?? 0);

        var regularMinutes = Math.Min(totalMinutes, (int)(_settings.OvertimeThresholdHours * 60 * records.Count));
        var overtimeMinutes = Math.Max(0, totalMinutes - regularMinutes);

        return new AttendanceSummary
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            StartDate = startDate,
            EndDate = endDate,
            TotalHours = totalMinutes / 60m,
            RegularHours = regularMinutes / 60m,
            OvertimeHours = overtimeMinutes / 60m,
            BreakHours = breakMinutes / 60m,
            DaysPresent = records.Count(r => r.Status == AttendanceStatusModel.Present),
            DaysLate = records.Count(r => r.Status == AttendanceStatusModel.Late),
            DaysAbsent = records.Count(r => r.Status == AttendanceStatusModel.Absent),
            TotalLateMinutes = records.Where(r => r.MinutesLate.HasValue).Sum(r => r.MinutesLate!.Value)
        };
    }

    #endregion

    #region Legacy Reporting (backward compatibility)

    public async Task<AttendanceSummaryLegacy> GetEmployeeAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken);
        var attendances = await GetEmployeeAttendanceAsync(employeeId, startDate, endDate, cancellationToken);

        var workDays = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workDays++;
            }
        }

        return new AttendanceSummaryLegacy
        {
            EmployeeId = employeeId,
            EmployeeName = employee?.FullName ?? "Unknown",
            TotalWorkDays = workDays,
            PresentDays = attendances.Count(a => a.Status == AttendanceStatusEntity.Present),
            AbsentDays = attendances.Count(a => a.Status == AttendanceStatusEntity.Absent),
            LateDays = attendances.Count(a => a.Status == AttendanceStatusEntity.Late),
            HalfDays = attendances.Count(a => a.Status == AttendanceStatusEntity.HalfDay),
            LeaveDays = attendances.Count(a => a.Status == AttendanceStatusEntity.OnLeave),
            TotalHoursWorked = attendances.Sum(a => a.HoursWorked),
            TotalOvertimeHours = attendances.Sum(a => a.OvertimeHours)
        };
    }

    public async Task<IReadOnlyList<AttendanceSummaryLegacy>> GetAttendanceSummaryByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null)
            .ToListAsync(cancellationToken);

        var summaries = new List<AttendanceSummaryLegacy>();
        foreach (var employee in employees)
        {
            var summary = await GetEmployeeAttendanceSummaryAsync(employee.Id, startDate, endDate, cancellationToken);
            summaries.Add(summary);
        }

        return summaries;
    }

    public async Task<decimal> CalculateOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Attendances
            .Where(a => a.EmployeeId == employeeId &&
                       a.AttendanceDate >= startDate.Date &&
                       a.AttendanceDate <= endDate.Date)
            .SumAsync(a => a.OvertimeHours, cancellationToken);
    }

    #endregion

    #region Payroll Integration

    public async Task<PayrollExportData> ExportForPayrollAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null)
    {
        var report = await GenerateReportAsync(startDate, endDate, employeeIds);

        var entries = report.EmployeeSummaries.Select(s => new EmployeePayrollEntry
        {
            EmployeeId = s.EmployeeId,
            EmployeeName = s.EmployeeName,
            RegularHours = s.RegularHours,
            OvertimeHours = s.OvertimeHours,
            DaysWorked = s.DaysPresent + s.DaysLate,
            DaysAbsent = s.DaysAbsent,
            DaysLate = s.DaysLate,
            LateDeductionMinutes = s.TotalLateMinutes
        }).ToList();

        return new PayrollExportData
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Employees = entries,
            GeneratedAt = DateTime.Now
        };
    }

    public async Task<(decimal RegularHours, decimal OvertimeHours)> CalculateHoursAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var summary = await CalculateSummaryAsync(employeeId, startDate, endDate);
        return (summary.RegularHours, summary.OvertimeHours);
    }

    #endregion

    #region Settings

    public Task<AttendanceSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<AttendanceSettings> UpdateSettingsAsync(AttendanceSettings settings)
    {
        settings.Id = _settings.Id;
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion

    #region Helper Methods

    private static decimal CalculateHoursWorked(Attendance attendance)
    {
        if (!attendance.ClockIn.HasValue || !attendance.ClockOut.HasValue)
            return 0;

        var totalMinutes = (attendance.ClockOut.Value - attendance.ClockIn.Value).TotalMinutes;

        if (attendance.BreakStart.HasValue && attendance.BreakEnd.HasValue)
        {
            var breakMinutes = (attendance.BreakEnd.Value - attendance.BreakStart.Value).TotalMinutes;
            totalMinutes -= breakMinutes;
        }

        return Math.Max(0, (decimal)(totalMinutes / 60));
    }

    private void RecalculateTotals(AttendanceRecord record)
    {
        if (record.ClockInTime.HasValue && record.ClockOutTime.HasValue)
        {
            var totalMinutes = (int)(record.ClockOutTime.Value - record.ClockInTime.Value).TotalMinutes;

            if (record.BreakStartTime.HasValue && record.BreakEndTime.HasValue)
            {
                var breakMinutes = (int)(record.BreakEndTime.Value - record.BreakStartTime.Value).TotalMinutes;
                record.TotalBreakMinutes = breakMinutes;
                totalMinutes -= breakMinutes;
            }

            record.TotalWorkedMinutes = totalMinutes;
        }
    }

    private bool ValidateEmployeePin(int employeeId, string pin)
    {
        // In production, validate against database
        return _employeePins.TryGetValue(employeeId, out var storedPin) && storedPin == pin;
    }

    private Task<string> GetEmployeeNameAsync(int employeeId)
    {
        // In production, fetch from database
        var names = new Dictionary<int, string>
        {
            { 1, "John Kamau" },
            { 2, "Jane Wanjiku" },
            { 3, "Peter Ochieng" },
            { 4, "Mary Akinyi" },
            { 5, "David Mwangi" }
        };

        return Task.FromResult(names.GetValueOrDefault(employeeId, $"Employee {employeeId}"));
    }

    private AttendanceEdit CreateEdit(int recordId, int userId, string field, string? oldValue, string newValue, string reason)
    {
        return new AttendanceEdit
        {
            Id = _editHistory.Values.Sum(l => l.Count) + 1,
            AttendanceRecordId = recordId,
            EditedByUserId = userId,
            EditedByUserName = $"Manager {userId}",
            FieldEdited = field,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            EditedAt = DateTime.UtcNow
        };
    }

    private void InitializeSampleData()
    {
        // Sample employee PINs
        _employeePins[1] = "1234";
        _employeePins[2] = "5678";
        _employeePins[3] = "9012";
        _employeePins[4] = "3456";
        _employeePins[5] = "7890";

        // Sample manager PINs
        _managerPins[100] = "0000";
        _managerPins[101] = "1111";

        // Default settings
        _settings = new AttendanceSettings
        {
            Id = 1,
            GracePeriodMinutes = 15,
            OvertimeThresholdHours = 8m,
            StandardStartTime = new TimeOnly(9, 0),
            StandardEndTime = new TimeOnly(17, 0),
            RequirePinForClockIn = true
        };

        // Sample attendance records (yesterday and today)
        var yesterday = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Yesterday's completed records
        _records[_nextRecordId++] = new AttendanceRecord
        {
            Id = _nextRecordId - 1,
            EmployeeId = 1,
            EmployeeName = "John Kamau",
            AttendanceDate = yesterday,
            ClockInTime = yesterday.ToDateTime(new TimeOnly(8, 55)),
            ClockOutTime = yesterday.ToDateTime(new TimeOnly(17, 15)),
            TotalWorkedMinutes = 500,
            Status = AttendanceStatusModel.Present
        };

        _records[_nextRecordId++] = new AttendanceRecord
        {
            Id = _nextRecordId - 1,
            EmployeeId = 2,
            EmployeeName = "Jane Wanjiku",
            AttendanceDate = yesterday,
            ClockInTime = yesterday.ToDateTime(new TimeOnly(9, 30)),
            ClockOutTime = yesterday.ToDateTime(new TimeOnly(17, 0)),
            TotalWorkedMinutes = 450,
            Status = AttendanceStatusModel.Late
        };
    }

    #endregion
}
