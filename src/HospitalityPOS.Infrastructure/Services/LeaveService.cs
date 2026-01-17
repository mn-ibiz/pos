// src/HospitalityPOS.Infrastructure/Services/LeaveService.cs
// Implementation of employee leave management service
// Story 45-4: Leave Management

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for employee leave management.
/// </summary>
public class LeaveService : ILeaveService
{
    #region Private Fields

    private readonly Dictionary<int, LeaveType> _leaveTypes = new();
    private readonly Dictionary<int, LeaveAllocation> _allocations = new();
    private readonly Dictionary<int, LeaveRequest> _requests = new();
    private readonly Dictionary<int, LeaveBalanceAdjustment> _adjustments = new();
    private readonly Dictionary<int, string> _employees = new();
    private readonly Dictionary<int, string> _departments = new();
    private readonly List<(DateOnly Date, string Name)> _publicHolidays = new();

    private LeaveSettings _settings = new();
    private int _nextLeaveTypeId = 1;
    private int _nextAllocationId = 1;
    private int _nextRequestId = 1;
    private int _nextAdjustmentId = 1;

    #endregion

    #region Events

    public event EventHandler<LeaveEventArgs>? RequestSubmitted;
    public event EventHandler<LeaveEventArgs>? RequestApproved;
    public event EventHandler<LeaveEventArgs>? RequestRejected;
    public event EventHandler<LeaveEventArgs>? RequestCancelled;

    #endregion

    #region Constructor

    public LeaveService()
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

        // Sample departments
        _departments[1] = "Sales";
        _departments[2] = "Kitchen";
        _departments[3] = "Management";

        // Kenya default leave types
        CreateDefaultLeaveTypes();

        // Kenya public holidays 2026
        CreatePublicHolidays();

        // Initialize allocations for current year
        var currentYear = DateTime.Today.Year;
        foreach (var empId in _employees.Keys)
        {
            foreach (var leaveType in _leaveTypes.Values)
            {
                CreateAllocation(empId, leaveType.Id, currentYear, leaveType.DefaultDaysPerYear);
            }
        }
    }

    private void CreateDefaultLeaveTypes()
    {
        // Kenya Employment Act 2007 defaults
        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Annual Leave",
            Description = "Paid annual vacation leave",
            DefaultDaysPerYear = 21,
            IsPaid = true,
            AllowCarryOver = true,
            MaxCarryOverDays = 10,
            MinimumNoticeDays = 7,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Sick Leave",
            Description = "Paid sick leave with documentation",
            DefaultDaysPerYear = 14,
            IsPaid = true,
            RequiresDocumentation = true,
            MaxConsecutiveDays = 14,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Maternity Leave",
            Description = "Paid maternity leave",
            DefaultDaysPerYear = 90,
            IsPaid = true,
            RequiresDocumentation = true,
            MinimumNoticeDays = 30,
            MinServiceMonthsRequired = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Paternity Leave",
            Description = "Paid paternity leave",
            DefaultDaysPerYear = 14,
            IsPaid = true,
            RequiresDocumentation = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Compassionate Leave",
            Description = "Leave for family emergencies",
            DefaultDaysPerYear = 5,
            IsPaid = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[_nextLeaveTypeId] = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = "Unpaid Leave",
            Description = "Leave without pay",
            DefaultDaysPerYear = 0,
            IsPaid = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void CreatePublicHolidays()
    {
        // Kenya public holidays 2026
        _publicHolidays.Add((new DateOnly(2026, 1, 1), "New Year's Day"));
        _publicHolidays.Add((new DateOnly(2026, 4, 10), "Good Friday"));
        _publicHolidays.Add((new DateOnly(2026, 4, 13), "Easter Monday"));
        _publicHolidays.Add((new DateOnly(2026, 5, 1), "Labour Day"));
        _publicHolidays.Add((new DateOnly(2026, 6, 1), "Madaraka Day"));
        _publicHolidays.Add((new DateOnly(2026, 10, 10), "Huduma Day"));
        _publicHolidays.Add((new DateOnly(2026, 10, 20), "Mashujaa Day"));
        _publicHolidays.Add((new DateOnly(2026, 12, 12), "Jamhuri Day"));
        _publicHolidays.Add((new DateOnly(2026, 12, 25), "Christmas Day"));
        _publicHolidays.Add((new DateOnly(2026, 12, 26), "Boxing Day"));
    }

    private LeaveAllocation CreateAllocation(int employeeId, int leaveTypeId, int year, decimal days)
    {
        var allocation = new LeaveAllocation
        {
            Id = _nextAllocationId++,
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            LeaveTypeId = leaveTypeId,
            LeaveTypeName = _leaveTypes.GetValueOrDefault(leaveTypeId)?.Name ?? "Unknown",
            Year = year,
            AllocatedDays = days,
            UsedDays = 0,
            CarriedOverDays = 0,
            PendingDays = 0,
            LastUpdated = DateTime.UtcNow
        };
        _allocations[allocation.Id] = allocation;
        return allocation;
    }

    #endregion

    #region Leave Types

    public Task<LeaveType> CreateLeaveTypeAsync(LeaveTypeRequest request)
    {
        var leaveType = new LeaveType
        {
            Id = _nextLeaveTypeId++,
            Name = request.Name,
            Description = request.Description,
            DefaultDaysPerYear = request.DefaultDaysPerYear,
            IsPaid = request.IsPaid,
            AllowCarryOver = request.AllowCarryOver,
            MaxCarryOverDays = request.MaxCarryOverDays,
            RequiresDocumentation = request.RequiresDocumentation,
            MinimumNoticeDays = request.MinimumNoticeDays,
            MaxConsecutiveDays = request.MaxConsecutiveDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _leaveTypes[leaveType.Id] = leaveType;
        return Task.FromResult(leaveType);
    }

    public Task<LeaveType> UpdateLeaveTypeAsync(LeaveTypeRequest request)
    {
        if (!request.Id.HasValue || !_leaveTypes.ContainsKey(request.Id.Value))
        {
            throw new KeyNotFoundException($"Leave type {request.Id} not found");
        }

        var leaveType = _leaveTypes[request.Id.Value];
        leaveType.Name = request.Name;
        leaveType.Description = request.Description;
        leaveType.DefaultDaysPerYear = request.DefaultDaysPerYear;
        leaveType.IsPaid = request.IsPaid;
        leaveType.AllowCarryOver = request.AllowCarryOver;
        leaveType.MaxCarryOverDays = request.MaxCarryOverDays;
        leaveType.RequiresDocumentation = request.RequiresDocumentation;
        leaveType.MinimumNoticeDays = request.MinimumNoticeDays;
        leaveType.MaxConsecutiveDays = request.MaxConsecutiveDays;

        return Task.FromResult(leaveType);
    }

    public Task<bool> DeactivateLeaveTypeAsync(int leaveTypeId)
    {
        if (_leaveTypes.TryGetValue(leaveTypeId, out var leaveType))
        {
            leaveType.IsActive = false;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<LeaveType?> GetLeaveTypeAsync(int leaveTypeId)
    {
        _leaveTypes.TryGetValue(leaveTypeId, out var leaveType);
        return Task.FromResult(leaveType);
    }

    public Task<IReadOnlyList<LeaveType>> GetActiveLeaveTypesAsync()
    {
        var types = _leaveTypes.Values.Where(t => t.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<LeaveType>>(types);
    }

    #endregion

    #region Leave Requests

    public async Task<LeaveResult> SubmitRequestAsync(LeaveRequestSubmission submission)
    {
        // Validate leave type
        if (!_leaveTypes.TryGetValue(submission.LeaveTypeId, out var leaveType))
        {
            return LeaveResult.Failed("Invalid leave type");
        }

        // Validate dates
        if (submission.EndDate < submission.StartDate)
        {
            return LeaveResult.Failed("End date cannot be before start date");
        }

        // Calculate days
        var days = await CalculateWorkingDaysAsync(submission.StartDate, submission.EndDate);
        if (submission.IsHalfDayStart) days -= 0.5m;
        if (submission.IsHalfDayEnd) days -= 0.5m;

        if (days <= 0)
        {
            return LeaveResult.Failed("No working days in selected range");
        }

        // Check minimum notice
        if (leaveType.MinimumNoticeDays.HasValue)
        {
            var daysUntilStart = (submission.StartDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
            if (daysUntilStart < leaveType.MinimumNoticeDays.Value)
            {
                return LeaveResult.Failed(
                    $"Minimum {leaveType.MinimumNoticeDays.Value} days notice required for {leaveType.Name}");
            }
        }

        // Check max consecutive days
        if (leaveType.MaxConsecutiveDays.HasValue && days > leaveType.MaxConsecutiveDays.Value)
        {
            return LeaveResult.Failed(
                $"Maximum {leaveType.MaxConsecutiveDays.Value} consecutive days allowed for {leaveType.Name}");
        }

        // Check balance
        var hasBalance = await HasSufficientBalanceAsync(submission.EmployeeId, submission.LeaveTypeId, days);
        if (!hasBalance && leaveType.DefaultDaysPerYear > 0)
        {
            return LeaveResult.Failed("Insufficient leave balance");
        }

        var request = new LeaveRequest
        {
            Id = _nextRequestId++,
            EmployeeId = submission.EmployeeId,
            EmployeeName = _employees.GetValueOrDefault(submission.EmployeeId, $"Employee {submission.EmployeeId}"),
            LeaveTypeId = submission.LeaveTypeId,
            LeaveTypeName = leaveType.Name,
            StartDate = submission.StartDate,
            EndDate = submission.EndDate,
            DaysRequested = days,
            Reason = submission.Reason,
            Status = LeaveRequestStatus.Pending,
            DocumentationRequired = leaveType.RequiresDocumentation,
            IsHalfDayStart = submission.IsHalfDayStart,
            IsHalfDayEnd = submission.IsHalfDayEnd,
            CreatedAt = DateTime.UtcNow
        };

        _requests[request.Id] = request;

        // Update pending days in allocation
        UpdatePendingDays(submission.EmployeeId, submission.LeaveTypeId, days);

        // Check coverage conflicts (warnings only)
        var warnings = await CheckCoverageConflictsAsync(submission.EmployeeId, submission.StartDate, submission.EndDate);
        var result = LeaveResult.Succeeded(request);
        result.Warnings = warnings.ToList();

        RequestSubmitted?.Invoke(this, new LeaveEventArgs(request, "Submitted"));
        return result;
    }

    public async Task<LeaveResult> ProcessApprovalAsync(LeaveApprovalRequest approvalRequest)
    {
        if (!_requests.TryGetValue(approvalRequest.RequestId, out var request))
        {
            return LeaveResult.Failed("Request not found");
        }

        if (request.Status != LeaveRequestStatus.Pending)
        {
            return LeaveResult.Failed($"Request is not pending (status: {request.Status})");
        }

        request.ReviewedByUserId = approvalRequest.ReviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = approvalRequest.Notes;

        if (approvalRequest.Approve)
        {
            request.Status = LeaveRequestStatus.Approved;

            // Update used days in allocation
            var allocation = await GetOrCreateAllocation(request.EmployeeId, request.LeaveTypeId, DateTime.Today.Year);
            allocation.UsedDays += request.DaysRequested;
            allocation.PendingDays -= request.DaysRequested;
            allocation.LastUpdated = DateTime.UtcNow;

            RequestApproved?.Invoke(this, new LeaveEventArgs(request, "Approved"));
            return LeaveResult.Succeeded(request, "Leave request approved");
        }
        else
        {
            request.Status = LeaveRequestStatus.Rejected;

            // Restore pending days
            UpdatePendingDays(request.EmployeeId, request.LeaveTypeId, -request.DaysRequested);

            RequestRejected?.Invoke(this, new LeaveEventArgs(request, "Rejected"));
            return LeaveResult.Succeeded(request, "Leave request rejected");
        }
    }

    public Task<LeaveResult> CancelRequestAsync(int requestId, int employeeId, string? reason = null)
    {
        if (!_requests.TryGetValue(requestId, out var request))
        {
            return Task.FromResult(LeaveResult.Failed("Request not found"));
        }

        if (request.EmployeeId != employeeId)
        {
            return Task.FromResult(LeaveResult.Failed("You can only cancel your own requests"));
        }

        if (request.Status != LeaveRequestStatus.Pending && request.Status != LeaveRequestStatus.Approved)
        {
            return Task.FromResult(LeaveResult.Failed($"Cannot cancel request with status {request.Status}"));
        }

        // If approved, restore the balance
        if (request.Status == LeaveRequestStatus.Approved)
        {
            var allocation = GetAllocation(request.EmployeeId, request.LeaveTypeId, DateTime.Today.Year);
            if (allocation != null)
            {
                allocation.UsedDays -= request.DaysRequested;
                allocation.LastUpdated = DateTime.UtcNow;
            }
        }
        else
        {
            // Restore pending days
            UpdatePendingDays(request.EmployeeId, request.LeaveTypeId, -request.DaysRequested);
        }

        request.Status = LeaveRequestStatus.Cancelled;
        request.ReviewNotes = reason;

        RequestCancelled?.Invoke(this, new LeaveEventArgs(request, "Cancelled"));
        return Task.FromResult(LeaveResult.Succeeded(request, "Leave request cancelled"));
    }

    public Task<LeaveRequest?> GetRequestAsync(int requestId)
    {
        _requests.TryGetValue(requestId, out var request);
        return Task.FromResult(request);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsAsync(int? managerId = null)
    {
        var pending = _requests.Values
            .Where(r => r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<LeaveRequest>>(pending);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetEmployeeRequestsAsync(int employeeId, int? year = null)
    {
        var requests = _requests.Values
            .Where(r => r.EmployeeId == employeeId &&
                       (!year.HasValue || r.StartDate.Year == year.Value))
            .OrderByDescending(r => r.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<LeaveRequest>>(requests);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetApprovedRequestsAsync(
        DateOnly startDate, DateOnly endDate, int? departmentId = null)
    {
        var requests = _requests.Values
            .Where(r => r.Status == LeaveRequestStatus.Approved &&
                       r.StartDate <= endDate && r.EndDate >= startDate)
            .OrderBy(r => r.StartDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<LeaveRequest>>(requests);
    }

    #endregion

    #region Leave Balances

    public Task<IReadOnlyList<LeaveAllocation>> GetEmployeeAllocationsAsync(int employeeId, int year)
    {
        var allocations = _allocations.Values
            .Where(a => a.EmployeeId == employeeId && a.Year == year)
            .ToList();
        return Task.FromResult<IReadOnlyList<LeaveAllocation>>(allocations);
    }

    public async Task<EmployeeLeaveBalance> GetEmployeeBalanceAsync(int employeeId, int year)
    {
        var allocations = await GetEmployeeAllocationsAsync(employeeId, year);

        return new EmployeeLeaveBalance
        {
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            Year = year,
            Allocations = allocations.ToList()
        };
    }

    public Task<int> InitializeYearAllocationsAsync(int year, IEnumerable<int>? employeeIds = null)
    {
        var targetEmployees = employeeIds?.ToList() ?? _employees.Keys.ToList();
        var count = 0;

        foreach (var empId in targetEmployees)
        {
            foreach (var leaveType in _leaveTypes.Values.Where(t => t.IsActive))
            {
                var existing = _allocations.Values.FirstOrDefault(a =>
                    a.EmployeeId == empId && a.LeaveTypeId == leaveType.Id && a.Year == year);

                if (existing == null)
                {
                    CreateAllocation(empId, leaveType.Id, year, leaveType.DefaultDaysPerYear);
                    count++;
                }
            }
        }

        return Task.FromResult(count);
    }

    public Task<int> ProcessCarryOverAsync(int fromYear, int toYear)
    {
        var count = 0;

        foreach (var allocation in _allocations.Values.Where(a => a.Year == fromYear))
        {
            var leaveType = _leaveTypes.GetValueOrDefault(allocation.LeaveTypeId);
            if (leaveType == null || !leaveType.AllowCarryOver)
                continue;

            var remainingDays = allocation.RemainingDays;
            if (remainingDays <= 0)
                continue;

            var carryOver = Math.Min(remainingDays, leaveType.MaxCarryOverDays);

            // Find or create next year allocation
            var nextAllocation = _allocations.Values.FirstOrDefault(a =>
                a.EmployeeId == allocation.EmployeeId &&
                a.LeaveTypeId == allocation.LeaveTypeId &&
                a.Year == toYear);

            if (nextAllocation == null)
            {
                nextAllocation = CreateAllocation(
                    allocation.EmployeeId, allocation.LeaveTypeId, toYear, leaveType.DefaultDaysPerYear);
            }

            nextAllocation.CarriedOverDays = carryOver;
            nextAllocation.LastUpdated = DateTime.UtcNow;
            count++;
        }

        return Task.FromResult(count);
    }

    public async Task<LeaveAllocation> AdjustBalanceAsync(
        int employeeId, int leaveTypeId, int year, decimal days, string reason, int adjustedByUserId)
    {
        var allocation = await GetOrCreateAllocation(employeeId, leaveTypeId, year);

        allocation.AllocatedDays += days;
        allocation.LastUpdated = DateTime.UtcNow;

        // Record adjustment
        var adjustment = new LeaveBalanceAdjustment
        {
            Id = _nextAdjustmentId++,
            AllocationId = allocation.Id,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            AdjustmentType = LeaveAdjustmentType.Adjustment,
            Days = days,
            Reason = reason,
            AdjustedByUserId = adjustedByUserId,
            CreatedAt = DateTime.UtcNow
        };
        _adjustments[adjustment.Id] = adjustment;

        return allocation;
    }

    public async Task<bool> HasSufficientBalanceAsync(int employeeId, int leaveTypeId, decimal days)
    {
        var allocation = await GetOrCreateAllocation(employeeId, leaveTypeId, DateTime.Today.Year);
        return allocation.AvailableForRequest >= days;
    }

    private void UpdatePendingDays(int employeeId, int leaveTypeId, decimal days)
    {
        var allocation = _allocations.Values.FirstOrDefault(a =>
            a.EmployeeId == employeeId &&
            a.LeaveTypeId == leaveTypeId &&
            a.Year == DateTime.Today.Year);

        if (allocation != null)
        {
            allocation.PendingDays += days;
            if (allocation.PendingDays < 0) allocation.PendingDays = 0;
        }
    }

    private LeaveAllocation? GetAllocation(int employeeId, int leaveTypeId, int year)
    {
        return _allocations.Values.FirstOrDefault(a =>
            a.EmployeeId == employeeId &&
            a.LeaveTypeId == leaveTypeId &&
            a.Year == year);
    }

    private async Task<LeaveAllocation> GetOrCreateAllocation(int employeeId, int leaveTypeId, int year)
    {
        var allocation = GetAllocation(employeeId, leaveTypeId, year);
        if (allocation == null)
        {
            var leaveType = await GetLeaveTypeAsync(leaveTypeId);
            allocation = CreateAllocation(employeeId, leaveTypeId, year, leaveType?.DefaultDaysPerYear ?? 0);
        }
        return allocation;
    }

    #endregion

    #region Calendar

    public async Task<LeaveCalendarView> GetCalendarViewAsync(DateOnly startDate, DateOnly endDate, int? departmentId = null)
    {
        var approved = await GetApprovedRequestsAsync(startDate, endDate, departmentId);
        var coverage = await CheckCoverageAsync(startDate, endDate, departmentId);

        var entries = approved.Select(r => new LeaveCalendarEntry
        {
            RequestId = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.EmployeeName,
            Department = r.Department,
            LeaveTypeName = r.LeaveTypeName,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            Days = r.DaysRequested,
            Status = r.Status,
            Color = GetLeaveTypeColor(r.LeaveTypeName)
        }).ToList();

        return new LeaveCalendarView
        {
            StartDate = startDate,
            EndDate = endDate,
            Entries = entries,
            DailyCoverage = coverage.ToList(),
            DepartmentId = departmentId
        };
    }

    public async Task<IReadOnlyList<DayCoverage>> CheckCoverageAsync(
        DateOnly startDate, DateOnly endDate, int? departmentId = null)
    {
        var coverage = new List<DayCoverage>();
        var approved = await GetApprovedRequestsAsync(startDate, endDate, departmentId);
        var totalEmployees = _employees.Count;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var onLeave = approved
                .Where(r => r.StartDate <= date && r.EndDate >= date)
                .ToList();

            coverage.Add(new DayCoverage
            {
                Date = date,
                TotalEmployees = totalEmployees,
                EmployeesOnLeave = onLeave.Count,
                EmployeesOnLeaveNames = onLeave.Select(r => r.EmployeeName).ToList(),
                HasCoverageIssue = onLeave.Count > totalEmployees * 0.5 // >50% on leave
            });
        }

        return coverage;
    }

    public async Task<IReadOnlyList<string>> CheckCoverageConflictsAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var warnings = new List<string>();
        var approved = await GetApprovedRequestsAsync(startDate, endDate);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var onLeave = approved.Count(r => r.StartDate <= date && r.EndDate >= date);
            var percentage = (decimal)onLeave / _employees.Count * 100;

            if (percentage >= 30)
            {
                warnings.Add($"On {date:MMM d}, {percentage:F0}% of staff will be on leave");
            }
        }

        return warnings;
    }

    private static string GetLeaveTypeColor(string leaveTypeName)
    {
        return leaveTypeName switch
        {
            "Annual Leave" => "#4CAF50",
            "Sick Leave" => "#F44336",
            "Maternity Leave" => "#E91E63",
            "Paternity Leave" => "#9C27B0",
            "Compassionate Leave" => "#FF9800",
            _ => "#2196F3"
        };
    }

    #endregion

    #region Reports

    public async Task<LeaveBalanceReport> GenerateBalanceReportAsync(int year, int? departmentId = null)
    {
        var balances = new List<EmployeeLeaveBalance>();

        foreach (var empId in _employees.Keys)
        {
            var balance = await GetEmployeeBalanceAsync(empId, year);
            if (balance.Allocations.Any())
            {
                balances.Add(balance);
            }
        }

        var byType = _leaveTypes.Values
            .Where(t => t.IsActive)
            .Select(t => new LeaveTypeSummary
            {
                LeaveTypeId = t.Id,
                LeaveTypeName = t.Name,
                TotalAllocated = balances.Sum(b => b.Allocations
                    .Where(a => a.LeaveTypeId == t.Id)
                    .Sum(a => a.TotalAvailable)),
                TotalUsed = balances.Sum(b => b.Allocations
                    .Where(a => a.LeaveTypeId == t.Id)
                    .Sum(a => a.UsedDays)),
                TotalRemaining = balances.Sum(b => b.Allocations
                    .Where(a => a.LeaveTypeId == t.Id)
                    .Sum(a => a.RemainingDays)),
                EmployeesWithBalance = balances.Count(b => b.Allocations
                    .Any(a => a.LeaveTypeId == t.Id && a.RemainingDays > 0))
            })
            .ToList();

        return new LeaveBalanceReport
        {
            Year = year,
            GeneratedDate = DateOnly.FromDateTime(DateTime.Today),
            Balances = balances,
            ByLeaveType = byType,
            TotalEmployees = balances.Count,
            TotalAllocated = balances.Sum(b => b.TotalAllocated),
            TotalUsed = balances.Sum(b => b.TotalUsed)
        };
    }

    public async Task<EmployeeLeaveHistory> GetEmployeeHistoryAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var requests = await GetEmployeeRequestsAsync(employeeId);
        var filteredRequests = requests
            .Where(r => r.StartDate >= startDate && r.EndDate <= endDate)
            .ToList();

        var adjustments = _adjustments.Values
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        var daysByType = filteredRequests
            .Where(r => r.Status == LeaveRequestStatus.Approved)
            .GroupBy(r => r.LeaveTypeName)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DaysRequested));

        return new EmployeeLeaveHistory
        {
            EmployeeId = employeeId,
            EmployeeName = _employees.GetValueOrDefault(employeeId, $"Employee {employeeId}"),
            StartDate = startDate,
            EndDate = endDate,
            Requests = filteredRequests,
            Adjustments = adjustments,
            DaysByType = daysByType,
            TotalDaysTaken = daysByType.Values.Sum()
        };
    }

    public async Task<LeaveUtilizationReport> GenerateUtilizationReportAsync(int year, int? departmentId = null)
    {
        var requests = _requests.Values.Where(r => r.StartDate.Year == year).ToList();

        var monthlyBreakdown = Enumerable.Range(1, 12)
            .Select(month => new MonthlyUtilization
            {
                Month = month,
                MonthName = new DateTime(year, month, 1).ToString("MMMM"),
                TotalDaysTaken = requests
                    .Where(r => r.StartDate.Month == month && r.Status == LeaveRequestStatus.Approved)
                    .Sum(r => r.DaysRequested),
                RequestCount = requests
                    .Count(r => r.StartDate.Month == month && r.Status == LeaveRequestStatus.Approved),
                EmployeesOnLeave = requests
                    .Where(r => r.StartDate.Month == month && r.Status == LeaveRequestStatus.Approved)
                    .Select(r => r.EmployeeId)
                    .Distinct()
                    .Count()
            })
            .ToList();

        return new LeaveUtilizationReport
        {
            Year = year,
            StartDate = new DateOnly(year, 1, 1),
            EndDate = new DateOnly(year, 12, 31),
            MonthlyBreakdown = monthlyBreakdown,
            TotalRequestsApproved = requests.Count(r => r.Status == LeaveRequestStatus.Approved),
            TotalRequestsRejected = requests.Count(r => r.Status == LeaveRequestStatus.Rejected),
            TotalRequestsPending = requests.Count(r => r.Status == LeaveRequestStatus.Pending)
        };
    }

    #endregion

    #region Utilities

    public Task<decimal> CalculateWorkingDaysAsync(DateOnly startDate, DateOnly endDate, bool excludeWeekends = true, bool excludeHolidays = true)
    {
        decimal days = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (excludeWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                continue;

            if (excludeHolidays && _publicHolidays.Any(h => h.Date == date))
                continue;

            days++;
        }

        return Task.FromResult(days);
    }

    public Task<IReadOnlyList<DateOnly>> GetPublicHolidaysAsync(int year)
    {
        var holidays = _publicHolidays
            .Where(h => h.Date.Year == year)
            .Select(h => h.Date)
            .ToList();
        return Task.FromResult<IReadOnlyList<DateOnly>>(holidays);
    }

    public Task<bool> AddPublicHolidayAsync(DateOnly date, string name)
    {
        if (_publicHolidays.Any(h => h.Date == date))
            return Task.FromResult(false);

        _publicHolidays.Add((date, name));
        return Task.FromResult(true);
    }

    #endregion

    #region Settings

    public Task<LeaveSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    public Task<LeaveSettings> UpdateSettingsAsync(LeaveSettings settings)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    #endregion
}
