// src/HospitalityPOS.Infrastructure/Services/SchedulingService.cs
// Implementation of shift scheduling and workforce management service
// Story 45-2: Shift Scheduling

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for employee shift scheduling and workforce management.
/// </summary>
public class SchedulingService : ISchedulingService
{
    #region Private Fields

    private readonly Dictionary<int, Shift> _shifts = new();
    private readonly Dictionary<int, RecurringShiftPattern> _patterns = new();
    private readonly Dictionary<int, ShiftSwapRequest> _swapRequests = new();
    private readonly List<CoverageRequirement> _coverageRequirements = new();
    private readonly Dictionary<int, string> _employees = new();
    private readonly Dictionary<int, string> _positions = new();
    private readonly Dictionary<int, string> _departments = new();

    private SchedulingSettings _settings = new();
    private int _nextShiftId = 1;
    private int _nextPatternId = 1;
    private int _nextSwapRequestId = 1;
    private int _nextRequirementId = 1;

    #endregion

    #region Events

    public event EventHandler<ScheduleEventArgs>? ShiftCreated;
    public event EventHandler<ScheduleEventArgs>? ShiftUpdated;
    public event EventHandler<ScheduleEventArgs>? ShiftDeleted;
    public event EventHandler<SwapRequestEventArgs>? SwapRequested;
    public event EventHandler<SwapRequestEventArgs>? SwapCompleted;

    #endregion

    #region Constructor

    public SchedulingService()
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

        // Sample positions
        _positions[1] = "Cashier";
        _positions[2] = "Manager";
        _positions[3] = "Cook";
        _positions[4] = "Server";

        // Sample departments
        _departments[1] = "Front of House";
        _departments[2] = "Kitchen";
        _departments[3] = "Management";

        // Default coverage requirements (sample)
        AddDefaultCoverageRequirements();
    }

    private void AddDefaultCoverageRequirements()
    {
        // Weekdays 8am-4pm (morning shift)
        foreach (DayOfWeek day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                           DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            _coverageRequirements.Add(new CoverageRequirement
            {
                Id = _nextRequirementId++,
                DayOfWeek = day,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                MinimumStaff = 2,
                OptimalStaff = 3
            });

            // Evening shift
            _coverageRequirements.Add(new CoverageRequirement
            {
                Id = _nextRequirementId++,
                DayOfWeek = day,
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(23, 0),
                MinimumStaff = 3,
                OptimalStaff = 4
            });
        }

        // Weekend coverage (higher)
        foreach (DayOfWeek day in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            _coverageRequirements.Add(new CoverageRequirement
            {
                Id = _nextRequirementId++,
                DayOfWeek = day,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(22, 0),
                MinimumStaff = 4,
                OptimalStaff = 5
            });
        }
    }

    #endregion

    #region Shift Management

    public Task<ShiftResult> CreateShiftAsync(ShiftRequest request, int userId)
    {
        // Check for conflicts
        var conflicts = CheckConflictsInternal(request);
        var blockingConflicts = conflicts.Where(c => !c.IsWarning).ToList();

        if (blockingConflicts.Any())
        {
            return Task.FromResult(ShiftResult.Failed(
                $"Cannot create shift: {blockingConflicts.First().Message}",
                conflicts.ToList()));
        }

        var shift = new Shift
        {
            Id = _nextShiftId++,
            EmployeeId = request.EmployeeId,
            EmployeeName = _employees.GetValueOrDefault(request.EmployeeId, $"Employee {request.EmployeeId}"),
            ShiftDate = request.ShiftDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            PositionId = request.PositionId,
            PositionName = request.PositionId.HasValue
                ? _positions.GetValueOrDefault(request.PositionId.Value, "Unknown")
                : null,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentId.HasValue
                ? _departments.GetValueOrDefault(request.DepartmentId.Value, "Unknown")
                : null,
            Notes = request.Notes,
            Status = ShiftStatus.Scheduled,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _shifts[shift.Id] = shift;

        ShiftCreated?.Invoke(this, new ScheduleEventArgs(shift, "Created", userId));

        var result = ShiftResult.Succeeded(shift);
        if (conflicts.Any())
        {
            result.Conflicts = conflicts.ToList();
            result.Message = "Shift created with warnings";
        }

        return Task.FromResult(result);
    }

    public Task<ShiftResult> UpdateShiftAsync(ShiftRequest request, int userId)
    {
        if (!request.Id.HasValue || !_shifts.ContainsKey(request.Id.Value))
        {
            return Task.FromResult(ShiftResult.Failed("Shift not found"));
        }

        // Check for conflicts (excluding this shift)
        var conflicts = CheckConflictsInternal(request, request.Id);
        var blockingConflicts = conflicts.Where(c => !c.IsWarning).ToList();

        if (blockingConflicts.Any())
        {
            return Task.FromResult(ShiftResult.Failed(
                $"Cannot update shift: {blockingConflicts.First().Message}",
                conflicts.ToList()));
        }

        var shift = _shifts[request.Id.Value];
        shift.EmployeeId = request.EmployeeId;
        shift.EmployeeName = _employees.GetValueOrDefault(request.EmployeeId, $"Employee {request.EmployeeId}");
        shift.ShiftDate = request.ShiftDate;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.PositionId = request.PositionId;
        shift.PositionName = request.PositionId.HasValue
            ? _positions.GetValueOrDefault(request.PositionId.Value, "Unknown")
            : null;
        shift.DepartmentId = request.DepartmentId;
        shift.DepartmentName = request.DepartmentId.HasValue
            ? _departments.GetValueOrDefault(request.DepartmentId.Value, "Unknown")
            : null;
        shift.Notes = request.Notes;
        shift.ModifiedAt = DateTime.UtcNow;

        ShiftUpdated?.Invoke(this, new ScheduleEventArgs(shift, "Updated", userId));

        var result = ShiftResult.Succeeded(shift, "Shift updated successfully");
        if (conflicts.Any())
        {
            result.Conflicts = conflicts.ToList();
            result.Message = "Shift updated with warnings";
        }

        return Task.FromResult(result);
    }

    public Task<bool> DeleteShiftAsync(int shiftId, int userId)
    {
        if (!_shifts.TryGetValue(shiftId, out var shift))
        {
            return Task.FromResult(false);
        }

        _shifts.Remove(shiftId);
        ShiftDeleted?.Invoke(this, new ScheduleEventArgs(shift, "Deleted", userId));

        return Task.FromResult(true);
    }

    public Task<Shift?> GetShiftAsync(int shiftId)
    {
        _shifts.TryGetValue(shiftId, out var shift);
        return Task.FromResult(shift);
    }

    public Task<IReadOnlyList<Shift>> GetEmployeeShiftsAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var shifts = _shifts.Values
            .Where(s => s.EmployeeId == employeeId &&
                        s.ShiftDate >= startDate &&
                        s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToList();

        return Task.FromResult<IReadOnlyList<Shift>>(shifts);
    }

    public Task<IReadOnlyList<Shift>> GetShiftsByDateAsync(DateOnly date)
    {
        var shifts = _shifts.Values
            .Where(s => s.ShiftDate == date)
            .OrderBy(s => s.StartTime)
            .ToList();

        return Task.FromResult<IReadOnlyList<Shift>>(shifts);
    }

    public Task<IReadOnlyList<Shift>> GetShiftsAsync(DateOnly startDate, DateOnly endDate)
    {
        var shifts = _shifts.Values
            .Where(s => s.ShiftDate >= startDate && s.ShiftDate <= endDate)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToList();

        return Task.FromResult<IReadOnlyList<Shift>>(shifts);
    }

    #endregion

    #region Recurring Patterns

    public Task<RecurringShiftPattern> CreatePatternAsync(RecurringShiftPattern pattern, int userId)
    {
        pattern.Id = _nextPatternId++;
        pattern.EmployeeName = _employees.GetValueOrDefault(pattern.EmployeeId, $"Employee {pattern.EmployeeId}");
        pattern.PositionName = pattern.PositionId.HasValue
            ? _positions.GetValueOrDefault(pattern.PositionId.Value, "Unknown")
            : null;
        pattern.CreatedAt = DateTime.UtcNow;
        pattern.IsActive = true;

        _patterns[pattern.Id] = pattern;
        return Task.FromResult(pattern);
    }

    public Task<RecurringShiftPattern> UpdatePatternAsync(RecurringShiftPattern pattern, int userId)
    {
        if (_patterns.ContainsKey(pattern.Id))
        {
            pattern.EmployeeName = _employees.GetValueOrDefault(pattern.EmployeeId, $"Employee {pattern.EmployeeId}");
            pattern.PositionName = pattern.PositionId.HasValue
                ? _positions.GetValueOrDefault(pattern.PositionId.Value, "Unknown")
                : null;
            _patterns[pattern.Id] = pattern;
        }
        return Task.FromResult(pattern);
    }

    public Task<bool> DeactivatePatternAsync(int patternId, int userId)
    {
        if (_patterns.TryGetValue(patternId, out var pattern))
        {
            pattern.IsActive = false;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<RecurringShiftPattern>> GetEmployeePatternsAsync(int employeeId)
    {
        var patterns = _patterns.Values
            .Where(p => p.EmployeeId == employeeId && p.IsActive)
            .ToList();
        return Task.FromResult<IReadOnlyList<RecurringShiftPattern>>(patterns);
    }

    public Task<IReadOnlyList<RecurringShiftPattern>> GetAllPatternsAsync()
    {
        var patterns = _patterns.Values.Where(p => p.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<RecurringShiftPattern>>(patterns);
    }

    public async Task<ShiftResult> GenerateFromPatternsAsync(DateOnly startDate, DateOnly endDate, int userId)
    {
        var createdShifts = new List<Shift>();
        var activePatterns = _patterns.Values.Where(p => p.IsActive).ToList();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = GetDaysOfWeekFlag(date.DayOfWeek);

            foreach (var pattern in activePatterns)
            {
                // Check if pattern applies to this day
                if (!pattern.DaysOfWeek.HasFlag(dayOfWeek))
                    continue;

                // Check if date is within pattern's validity period
                if (date < pattern.ValidFrom || (pattern.ValidTo.HasValue && date > pattern.ValidTo.Value))
                    continue;

                // Check if shift already exists for this date/employee
                var existingShift = _shifts.Values.FirstOrDefault(s =>
                    s.EmployeeId == pattern.EmployeeId &&
                    s.ShiftDate == date &&
                    s.RecurringPatternId == pattern.Id);

                if (existingShift != null)
                    continue;

                var request = new ShiftRequest
                {
                    EmployeeId = pattern.EmployeeId,
                    ShiftDate = date,
                    StartTime = pattern.StartTime,
                    EndTime = pattern.EndTime,
                    PositionId = pattern.PositionId,
                    DepartmentId = pattern.DepartmentId
                };

                var result = await CreateShiftAsync(request, userId);
                if (result.Success && result.Shift != null)
                {
                    result.Shift.RecurringPatternId = pattern.Id;
                    createdShifts.Add(result.Shift);
                }
            }
        }

        return ShiftResult.SucceededMultiple(createdShifts,
            $"Generated {createdShifts.Count} shifts from recurring patterns");
    }

    private static DaysOfWeek GetDaysOfWeekFlag(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => DaysOfWeek.Sunday,
            DayOfWeek.Monday => DaysOfWeek.Monday,
            DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
            DayOfWeek.Thursday => DaysOfWeek.Thursday,
            DayOfWeek.Friday => DaysOfWeek.Friday,
            DayOfWeek.Saturday => DaysOfWeek.Saturday,
            _ => DaysOfWeek.None
        };
    }

    #endregion

    #region Shift Swap

    public Task<SwapResult> InitiateSwapAsync(SwapInitiateRequest request)
    {
        // Validate original shift exists and belongs to requester
        if (!_shifts.TryGetValue(request.OriginalShiftId, out var originalShift))
        {
            return Task.FromResult(SwapResult.Failed("Original shift not found"));
        }

        if (originalShift.EmployeeId != request.RequestingEmployeeId)
        {
            return Task.FromResult(SwapResult.Failed("You can only swap your own shifts"));
        }

        // Validate target shift if specified
        Shift? targetShift = null;
        if (request.TargetShiftId.HasValue)
        {
            if (!_shifts.TryGetValue(request.TargetShiftId.Value, out targetShift))
            {
                return Task.FromResult(SwapResult.Failed("Target shift not found"));
            }

            if (targetShift.EmployeeId != request.TargetEmployeeId)
            {
                return Task.FromResult(SwapResult.Failed("Target shift does not belong to the specified employee"));
            }
        }

        var swapRequest = new ShiftSwapRequest
        {
            Id = _nextSwapRequestId++,
            RequestingEmployeeId = request.RequestingEmployeeId,
            RequestingEmployeeName = _employees.GetValueOrDefault(request.RequestingEmployeeId,
                $"Employee {request.RequestingEmployeeId}"),
            OriginalShiftId = request.OriginalShiftId,
            OriginalShift = originalShift,
            TargetEmployeeId = request.TargetEmployeeId,
            TargetEmployeeName = _employees.GetValueOrDefault(request.TargetEmployeeId,
                $"Employee {request.TargetEmployeeId}"),
            TargetShiftId = request.TargetShiftId,
            TargetShift = targetShift,
            Reason = request.Reason,
            Status = SwapRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _swapRequests[swapRequest.Id] = swapRequest;
        SwapRequested?.Invoke(this, new SwapRequestEventArgs(swapRequest, "Created"));

        return Task.FromResult(SwapResult.Succeeded(swapRequest, "Swap request created"));
    }

    public Task<SwapResult> RespondToSwapAsync(SwapResponseRequest request)
    {
        if (!_swapRequests.TryGetValue(request.SwapRequestId, out var swapRequest))
        {
            return Task.FromResult(SwapResult.Failed("Swap request not found"));
        }

        if (swapRequest.Status != SwapRequestStatus.Pending)
        {
            return Task.FromResult(SwapResult.Failed($"Swap request is already {swapRequest.Status}"));
        }

        swapRequest.RespondedAt = DateTime.UtcNow;

        if (request.Accept)
        {
            swapRequest.Status = _settings.RequireSwapApproval
                ? SwapRequestStatus.Accepted
                : SwapRequestStatus.Approved;

            if (!_settings.RequireSwapApproval)
            {
                // Auto-approve and execute swap
                ExecuteSwap(swapRequest);
            }

            return Task.FromResult(SwapResult.Succeeded(swapRequest,
                _settings.RequireSwapApproval
                    ? "Swap accepted, pending manager approval"
                    : "Swap completed"));
        }
        else
        {
            swapRequest.Status = SwapRequestStatus.Rejected;
            swapRequest.RejectionReason = request.ResponseReason;
            return Task.FromResult(SwapResult.Succeeded(swapRequest, "Swap request rejected"));
        }
    }

    public Task<SwapResult> ProcessSwapApprovalAsync(SwapApprovalRequest request)
    {
        if (!_swapRequests.TryGetValue(request.SwapRequestId, out var swapRequest))
        {
            return Task.FromResult(SwapResult.Failed("Swap request not found"));
        }

        if (swapRequest.Status != SwapRequestStatus.Accepted)
        {
            return Task.FromResult(SwapResult.Failed(
                $"Swap request must be accepted before approval. Current status: {swapRequest.Status}"));
        }

        swapRequest.ApprovedByUserId = request.ManagerUserId;
        swapRequest.ApprovedAt = DateTime.UtcNow;

        if (request.Approve)
        {
            swapRequest.Status = SwapRequestStatus.Approved;
            ExecuteSwap(swapRequest);
            SwapCompleted?.Invoke(this, new SwapRequestEventArgs(swapRequest, "Completed"));
            return Task.FromResult(SwapResult.Succeeded(swapRequest, "Swap approved and completed"));
        }
        else
        {
            swapRequest.Status = SwapRequestStatus.Rejected;
            swapRequest.RejectionReason = request.Reason;
            return Task.FromResult(SwapResult.Succeeded(swapRequest, "Swap request rejected by manager"));
        }
    }

    private void ExecuteSwap(ShiftSwapRequest swapRequest)
    {
        // Swap the shifts
        if (_shifts.TryGetValue(swapRequest.OriginalShiftId, out var originalShift))
        {
            originalShift.EmployeeId = swapRequest.TargetEmployeeId;
            originalShift.EmployeeName = swapRequest.TargetEmployeeName;
            originalShift.Status = ShiftStatus.Swapped;
            originalShift.ModifiedAt = DateTime.UtcNow;
        }

        if (swapRequest.TargetShiftId.HasValue &&
            _shifts.TryGetValue(swapRequest.TargetShiftId.Value, out var targetShift))
        {
            targetShift.EmployeeId = swapRequest.RequestingEmployeeId;
            targetShift.EmployeeName = swapRequest.RequestingEmployeeName;
            targetShift.Status = ShiftStatus.Swapped;
            targetShift.ModifiedAt = DateTime.UtcNow;
        }
    }

    public Task<IReadOnlyList<ShiftSwapRequest>> GetEmployeeSwapRequestsAsync(int employeeId, bool includeHistory = false)
    {
        var requests = _swapRequests.Values
            .Where(r => (r.RequestingEmployeeId == employeeId || r.TargetEmployeeId == employeeId) &&
                        (includeHistory || r.Status == SwapRequestStatus.Pending || r.Status == SwapRequestStatus.Accepted))
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShiftSwapRequest>>(requests);
    }

    public Task<IReadOnlyList<ShiftSwapRequest>> GetPendingApprovalRequestsAsync()
    {
        var requests = _swapRequests.Values
            .Where(r => r.Status == SwapRequestStatus.Accepted)
            .OrderBy(r => r.RequestedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShiftSwapRequest>>(requests);
    }

    public Task<ShiftSwapRequest?> GetSwapRequestAsync(int requestId)
    {
        _swapRequests.TryGetValue(requestId, out var request);
        return Task.FromResult(request);
    }

    #endregion

    #region Conflict Detection

    public Task<IReadOnlyList<ShiftConflict>> CheckConflictsAsync(ShiftRequest request)
    {
        var conflicts = CheckConflictsInternal(request);
        return Task.FromResult<IReadOnlyList<ShiftConflict>>(conflicts.ToList());
    }

    private IEnumerable<ShiftConflict> CheckConflictsInternal(ShiftRequest request, int? excludeShiftId = null)
    {
        var conflicts = new List<ShiftConflict>();

        // Get employee's existing shifts for the week
        var weekStart = request.ShiftDate.AddDays(-(int)request.ShiftDate.DayOfWeek);
        var weekEnd = weekStart.AddDays(6);

        var existingShifts = _shifts.Values
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd &&
                        (!excludeShiftId.HasValue || s.Id != excludeShiftId.Value))
            .ToList();

        // Check for double booking (same day overlap)
        var sameDayShifts = existingShifts.Where(s => s.ShiftDate == request.ShiftDate);
        foreach (var existing in sameDayShifts)
        {
            if (ShiftsOverlap(request.StartTime, request.EndTime, existing.StartTime, existing.EndTime))
            {
                conflicts.Add(ShiftConflict.DoubleBooked(existing.Id,
                    $"Overlaps with existing shift {existing.StartTime:HH:mm}-{existing.EndTime:HH:mm}"));
            }
        }

        // Calculate proposed shift duration
        var newShiftDuration = CalculateDuration(request.ShiftDate, request.StartTime, request.EndTime);

        // Check max hours per week
        var totalWeekHours = existingShifts.Sum(s => s.DurationHours) + newShiftDuration;
        if (totalWeekHours > _settings.MaxHoursPerWeek)
        {
            conflicts.Add(ShiftConflict.MaxHours(
                $"Would exceed {_settings.MaxHoursPerWeek}h/week limit ({totalWeekHours:F1}h total)"));
        }

        // Check minimum rest between shifts
        var previousShift = existingShifts
            .Where(s => s.ShiftDate < request.ShiftDate ||
                       (s.ShiftDate == request.ShiftDate && s.EndTime <= request.StartTime))
            .OrderByDescending(s => s.EndDateTime)
            .FirstOrDefault();

        if (previousShift != null)
        {
            var newShiftStart = request.ShiftDate.ToDateTime(request.StartTime);
            var restHours = (decimal)(newShiftStart - previousShift.EndDateTime).TotalHours;
            if (restHours < _settings.MinRestHours)
            {
                conflicts.Add(ShiftConflict.InsufficientRest(
                    $"Only {restHours:F1}h rest since last shift (min {_settings.MinRestHours}h)"));
            }
        }

        // Check next shift for rest period
        var nextShift = existingShifts
            .Where(s => s.ShiftDate > request.ShiftDate ||
                       (s.ShiftDate == request.ShiftDate && s.StartTime >= request.EndTime))
            .OrderBy(s => s.StartDateTime)
            .FirstOrDefault();

        if (nextShift != null)
        {
            var newShiftEnd = request.EndTime < request.StartTime
                ? request.ShiftDate.AddDays(1).ToDateTime(request.EndTime)
                : request.ShiftDate.ToDateTime(request.EndTime);
            var restHours = (decimal)(nextShift.StartDateTime - newShiftEnd).TotalHours;
            if (restHours < _settings.MinRestHours)
            {
                conflicts.Add(ShiftConflict.InsufficientRest(
                    $"Only {restHours:F1}h rest before next shift (min {_settings.MinRestHours}h)"));
            }
        }

        return conflicts;
    }

    private static bool ShiftsOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
    {
        // Handle overnight shifts
        var overnight1 = end1 < start1;
        var overnight2 = end2 < start2;

        if (!overnight1 && !overnight2)
        {
            // Both are same-day shifts
            return start1 < end2 && start2 < end1;
        }

        // At least one overnight shift - convert to comparable ranges
        return true; // Simplified - consider any overnight overlap as conflict
    }

    private static decimal CalculateDuration(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var start = date.ToDateTime(startTime);
        var end = endTime < startTime
            ? date.AddDays(1).ToDateTime(endTime)
            : date.ToDateTime(endTime);
        return (decimal)(end - start).TotalHours;
    }

    public Task<IReadOnlyList<ShiftConflict>> ValidateSwapAsync(SwapInitiateRequest request)
    {
        var conflicts = new List<ShiftConflict>();

        // Get the shifts involved
        if (!_shifts.TryGetValue(request.OriginalShiftId, out var originalShift))
        {
            return Task.FromResult<IReadOnlyList<ShiftConflict>>(conflicts);
        }

        // Check if target employee can work the original shift
        var targetRequest = new ShiftRequest
        {
            EmployeeId = request.TargetEmployeeId,
            ShiftDate = originalShift.ShiftDate,
            StartTime = originalShift.StartTime,
            EndTime = originalShift.EndTime
        };
        conflicts.AddRange(CheckConflictsInternal(targetRequest));

        // If there's a target shift, check if requester can work it
        if (request.TargetShiftId.HasValue && _shifts.TryGetValue(request.TargetShiftId.Value, out var targetShift))
        {
            var requesterRequest = new ShiftRequest
            {
                EmployeeId = request.RequestingEmployeeId,
                ShiftDate = targetShift.ShiftDate,
                StartTime = targetShift.StartTime,
                EndTime = targetShift.EndTime
            };
            conflicts.AddRange(CheckConflictsInternal(requesterRequest, request.OriginalShiftId));
        }

        return Task.FromResult<IReadOnlyList<ShiftConflict>>(conflicts);
    }

    public Task<IReadOnlyList<ShiftConflict>> GetEmployeeConflictsAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var conflicts = new List<ShiftConflict>();
        var shifts = _shifts.Values
            .Where(s => s.EmployeeId == employeeId && s.ShiftDate >= startDate && s.ShiftDate <= endDate)
            .OrderBy(s => s.StartDateTime)
            .ToList();

        for (int i = 0; i < shifts.Count - 1; i++)
        {
            var current = shifts[i];
            var next = shifts[i + 1];

            // Check for overlapping shifts
            if (current.ShiftDate == next.ShiftDate)
            {
                if (ShiftsOverlap(current.StartTime, current.EndTime, next.StartTime, next.EndTime))
                {
                    conflicts.Add(ShiftConflict.DoubleBooked(next.Id,
                        $"Shift {next.Id} overlaps with shift {current.Id}"));
                }
            }

            // Check rest period
            var restHours = (decimal)(next.StartDateTime - current.EndDateTime).TotalHours;
            if (restHours >= 0 && restHours < _settings.MinRestHours)
            {
                conflicts.Add(ShiftConflict.InsufficientRest(
                    $"Only {restHours:F1}h rest between shifts {current.Id} and {next.Id}"));
            }
        }

        return Task.FromResult<IReadOnlyList<ShiftConflict>>(conflicts);
    }

    #endregion

    #region Coverage Analysis

    public Task<IReadOnlyList<CoverageRequirement>> GetCoverageRequirementsAsync()
    {
        return Task.FromResult<IReadOnlyList<CoverageRequirement>>(_coverageRequirements);
    }

    public Task<IReadOnlyList<CoverageRequirement>> UpdateCoverageRequirementsAsync(IEnumerable<CoverageRequirement> requirements)
    {
        _coverageRequirements.Clear();
        foreach (var req in requirements)
        {
            if (req.Id == 0)
                req.Id = _nextRequirementId++;
            _coverageRequirements.Add(req);
        }
        return Task.FromResult<IReadOnlyList<CoverageRequirement>>(_coverageRequirements);
    }

    public Task<IReadOnlyList<CoverageAnalysis>> AnalyzeCoverageAsync(DateOnly date)
    {
        var analyses = new List<CoverageAnalysis>();
        var dayOfWeek = date.DayOfWeek;

        var dayRequirements = _coverageRequirements.Where(r => r.DayOfWeek == dayOfWeek);
        var dayShifts = _shifts.Values.Where(s => s.ShiftDate == date).ToList();

        foreach (var requirement in dayRequirements)
        {
            var scheduledEmployees = dayShifts
                .Where(s => ShiftCoversRequirement(s, requirement))
                .Select(s => s.EmployeeName)
                .ToList();

            analyses.Add(new CoverageAnalysis
            {
                Date = date,
                StartTime = requirement.StartTime,
                EndTime = requirement.EndTime,
                RequiredStaff = requirement.MinimumStaff,
                OptimalStaff = requirement.OptimalStaff,
                ScheduledStaff = scheduledEmployees.Count,
                ScheduledEmployees = scheduledEmployees,
                DepartmentId = requirement.DepartmentId,
                DepartmentName = requirement.DepartmentName
            });
        }

        return Task.FromResult<IReadOnlyList<CoverageAnalysis>>(analyses);
    }

    private static bool ShiftCoversRequirement(Shift shift, CoverageRequirement requirement)
    {
        // Check if shift covers at least part of the requirement period
        return shift.StartTime <= requirement.EndTime && shift.EndTime >= requirement.StartTime;
    }

    public async Task<IReadOnlyList<DailyCoverageSummary>> GetCoverageSummaryAsync(DateOnly startDate, DateOnly endDate)
    {
        var summaries = new List<DailyCoverageSummary>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var periods = await AnalyzeCoverageAsync(date);
            var dayShifts = _shifts.Values.Where(s => s.ShiftDate == date).ToList();

            summaries.Add(new DailyCoverageSummary
            {
                Date = date,
                DayName = date.DayOfWeek.ToString(),
                TotalScheduledHours = (int)dayShifts.Sum(s => s.DurationHours),
                TotalRequiredHours = periods.Sum(p =>
                    (int)((p.EndTime.ToTimeSpan() - p.StartTime.ToTimeSpan()).TotalHours * p.RequiredStaff)),
                UnderstaffedPeriods = periods.Count(p => p.IsUnderstaffed),
                OverstaffedPeriods = periods.Count(p => p.IsOverstaffed),
                TotalEmployeesScheduled = dayShifts.Select(s => s.EmployeeId).Distinct().Count(),
                Periods = periods.ToList()
            });
        }

        return summaries;
    }

    public async Task<IReadOnlyList<CoverageAnalysis>> GetUnderstaffedPeriodsAsync(DateOnly startDate, DateOnly endDate)
    {
        var understaffed = new List<CoverageAnalysis>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var periods = await AnalyzeCoverageAsync(date);
            understaffed.AddRange(periods.Where(p => p.IsUnderstaffed));
        }

        return understaffed;
    }

    #endregion

    #region Schedule Views

    public async Task<WeeklyScheduleView> GetWeeklyScheduleAsync(DateOnly weekStartDate)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        var shifts = await GetShiftsAsync(weekStartDate, weekEndDate);
        var coverage = await GetCoverageSummaryAsync(weekStartDate, weekEndDate);

        var employeeSchedules = shifts
            .GroupBy(s => s.EmployeeId)
            .Select(g => new EmployeeWeekSchedule
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().EmployeeName,
                Department = g.First().DepartmentName,
                Position = g.First().PositionName,
                ShiftsByDay = g.GroupBy(s => s.ShiftDate.DayOfWeek)
                    .ToDictionary(d => d.Key, d => d.ToList()),
                TotalHours = g.Sum(s => s.DurationHours),
                Conflicts = new List<ShiftConflict>()
            })
            .ToList();

        // Check for conflicts
        foreach (var schedule in employeeSchedules)
        {
            var conflicts = await GetEmployeeConflictsAsync(schedule.EmployeeId, weekStartDate, weekEndDate);
            schedule.Conflicts = conflicts.ToList();
            schedule.HasConflicts = conflicts.Any();
        }

        return new WeeklyScheduleView
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            EmployeeSchedules = employeeSchedules,
            DailyCoverage = coverage.ToList(),
            TotalScheduledHours = (int)shifts.Sum(s => s.DurationHours),
            TotalEmployees = employeeSchedules.Count
        };
    }

    public async Task<MyScheduleView> GetMyScheduleAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekEnd = today.AddDays(14); // Show 2 weeks ahead

        var upcomingShifts = await GetEmployeeShiftsAsync(employeeId, today, weekEnd);
        var swapRequests = await GetEmployeeSwapRequestsAsync(employeeId, false);

        // Get today's coworkers
        var todayShifts = await GetShiftsByDateAsync(today);
        var myTodayShift = todayShifts.FirstOrDefault(s => s.EmployeeId == employeeId);
        var coworkers = new List<CoworkerOnShift>();

        if (myTodayShift != null)
        {
            coworkers = todayShifts
                .Where(s => s.EmployeeId != employeeId)
                .Select(s => new CoworkerOnShift
                {
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.EmployeeName,
                    Position = s.PositionName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList();
        }

        // Calculate hours
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisWeekEnd = weekStart.AddDays(6);
        var nextWeekStart = thisWeekEnd.AddDays(1);
        var nextWeekEnd = nextWeekStart.AddDays(6);

        var thisWeekShifts = await GetEmployeeShiftsAsync(employeeId, weekStart, thisWeekEnd);
        var nextWeekShifts = await GetEmployeeShiftsAsync(employeeId, nextWeekStart, nextWeekEnd);

        return new MyScheduleView
        {
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            UpcomingShifts = upcomingShifts.ToList(),
            PendingSwapRequests = swapRequests.Where(r =>
                r.Status == SwapRequestStatus.Pending || r.Status == SwapRequestStatus.Accepted).ToList(),
            HoursThisWeek = thisWeekShifts.Sum(s => s.DurationHours),
            HoursNextWeek = nextWeekShifts.Sum(s => s.DurationHours),
            TodaysCoworkers = coworkers
        };
    }

    public async Task<IReadOnlyList<CoworkerOnShift>> GetCoworkersForShiftAsync(int shiftId)
    {
        if (!_shifts.TryGetValue(shiftId, out var shift))
        {
            return new List<CoworkerOnShift>();
        }

        var dayShifts = await GetShiftsByDateAsync(shift.ShiftDate);

        return dayShifts
            .Where(s => s.Id != shiftId && ShiftsOverlap(shift.StartTime, shift.EndTime, s.StartTime, s.EndTime))
            .Select(s => new CoworkerOnShift
            {
                EmployeeId = s.EmployeeId,
                EmployeeName = s.EmployeeName,
                Position = s.PositionName,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToList();
    }

    #endregion

    #region Attendance Integration

    public Task<IReadOnlyList<ScheduleAttendanceComparison>> CompareScheduleToAttendanceAsync(
        DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null)
    {
        // This would integrate with AttendanceService in real implementation
        // For now, return sample comparison data
        var comparisons = new List<ScheduleAttendanceComparison>();

        var shifts = _shifts.Values
            .Where(s => s.ShiftDate >= startDate && s.ShiftDate <= endDate &&
                       (employeeIds == null || employeeIds.Contains(s.EmployeeId)))
            .ToList();

        foreach (var shift in shifts)
        {
            // Simulate attendance data
            var wasLate = new Random().Next(10) < 2; // 20% late
            var lateMinutes = wasLate ? new Random().Next(5, 30) : 0;

            comparisons.Add(new ScheduleAttendanceComparison
            {
                EmployeeId = shift.EmployeeId,
                EmployeeName = shift.EmployeeName,
                Date = shift.ShiftDate,
                ScheduledStart = shift.StartTime,
                ScheduledEnd = shift.EndTime,
                ActualStart = shift.StartTime.AddMinutes(lateMinutes),
                ActualEnd = shift.EndTime,
                ScheduledHours = shift.DurationHours,
                ActualHours = shift.DurationHours - (lateMinutes / 60m),
                WasLate = wasLate,
                LateMinutes = lateMinutes,
                WasEarlyDeparture = false,
                WasNoShow = shift.Status == ShiftStatus.NoShow
            });
        }

        return Task.FromResult<IReadOnlyList<ScheduleAttendanceComparison>>(comparisons);
    }

    public async Task<ScheduleAdherenceReport> GenerateAdherenceReportAsync(
        DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null)
    {
        var comparisons = await CompareScheduleToAttendanceAsync(startDate, endDate, employeeIds);

        return new ScheduleAdherenceReport
        {
            StartDate = startDate,
            EndDate = endDate,
            Records = comparisons.ToList(),
            TotalScheduledShifts = comparisons.Count,
            TotalCompletedShifts = comparisons.Count(c => !c.WasNoShow),
            TotalLateArrivals = comparisons.Count(c => c.WasLate),
            TotalEarlyDepartures = comparisons.Count(c => c.WasEarlyDeparture),
            TotalNoShows = comparisons.Count(c => c.WasNoShow),
            TotalScheduledHours = comparisons.Sum(c => c.ScheduledHours),
            TotalActualHours = comparisons.Sum(c => c.ActualHours)
        };
    }

    public Task<Shift?> GetTodayShiftAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var shift = _shifts.Values.FirstOrDefault(s =>
            s.EmployeeId == employeeId && s.ShiftDate == today);
        return Task.FromResult(shift);
    }

    public Task<Shift> UpdateShiftStatusAsync(int shiftId, ShiftStatus status)
    {
        if (!_shifts.TryGetValue(shiftId, out var shift))
        {
            throw new KeyNotFoundException($"Shift {shiftId} not found");
        }

        shift.Status = status;
        shift.ModifiedAt = DateTime.UtcNow;
        return Task.FromResult(shift);
    }

    #endregion

    #region Settings

    public Task<SchedulingSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<SchedulingSettings> UpdateSettingsAsync(SchedulingSettings settings)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion
}
