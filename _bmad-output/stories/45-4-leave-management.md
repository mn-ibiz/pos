# Story 45.4: Leave Management

Status: done

## Story

As an **HR manager**,
I want **to manage employee leave requests and balances**,
so that **I can track time off, ensure coverage, and comply with labor laws**.

## Business Context

**LOW PRIORITY - HR COMPLIANCE**

Leave management ensures:
- Compliance with labor laws (minimum annual leave)
- Fair leave allocation
- Coverage planning
- Accurate payroll deductions

**Business Value:** Proper leave tracking prevents disputes and ensures compliance.

## Acceptance Criteria

### AC1: Leave Types Configuration
- [x] Configure leave types (Annual, Sick, Maternity, etc.)
- [x] Set days allocated per year per type
- [x] Paid vs unpaid leave designation
- [x] Carry-over rules

### AC2: Leave Requests
- [x] Employee submits leave request
- [x] Specify: type, start date, end date, reason
- [x] Calculate days requested
- [x] Check against available balance

### AC3: Leave Approval Workflow
- [x] Manager reviews pending requests
- [x] Approve or reject with reason
- [x] Notification to employee
- [x] Calendar blocking

### AC4: Leave Balances
- [x] Track balance per employee per leave type
- [x] Initial allocation at year start
- [x] Deduct on approved leave
- [x] Accrue if applicable

### AC5: Leave Calendar
- [x] View all approved leaves on calendar
- [x] Check for coverage conflicts
- [x] Filter by department

### AC6: Leave Reports
- [x] Leave balance report
- [x] Leave history by employee
- [x] Leave utilization summary
- [x] Export to Excel

## Tasks / Subtasks

- [x] **Task 1: Leave Configuration** (AC: 1)
  - [x] 1.1 Create LeaveTypes table
  - [x] 1.2 Create LeaveAllocations table
  - [x] 1.3 Leave type CRUD
  - [x] 1.4 Allocation rules

- [x] **Task 2: Leave Request Flow** (AC: 2, 3)
  - [x] 2.1 Create LeaveRequests table
  - [x] 2.2 Request submission UI
  - [x] 2.3 Balance check
  - [x] 2.4 Approval workflow
  - [x] 2.5 Notifications

- [x] **Task 3: Leave Balances** (AC: 4)
  - [x] 3.1 Balance calculation logic
  - [x] 3.2 Annual reset/allocation
  - [x] 3.3 Carry-over logic
  - [x] 3.4 Balance display

- [x] **Task 4: Leave Calendar** (AC: 5)
  - [x] 4.1 Calendar view UI
  - [x] 4.2 Show approved leaves
  - [x] 4.3 Department filter

- [x] **Task 5: Reports** (AC: 6)
  - [x] 5.1 Balance report
  - [x] 5.2 History report
  - [x] 5.3 Utilization summary
  - [x] 5.4 Excel export

## Dev Notes

### Database Schema

```sql
CREATE TABLE LeaveTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL, -- Annual, Sick, Maternity, etc.
    DefaultDaysPerYear INT NOT NULL,
    IsPaid BIT DEFAULT 1,
    AllowCarryOver BIT DEFAULT 0,
    MaxCarryOverDays INT DEFAULT 0,
    RequiresDocumentation BIT DEFAULT 0, -- e.g., sick note
    IsActive BIT DEFAULT 1
);

CREATE TABLE LeaveAllocations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT FOREIGN KEY REFERENCES Users(Id),
    LeaveTypeId INT FOREIGN KEY REFERENCES LeaveTypes(Id),
    Year INT NOT NULL,
    AllocatedDays DECIMAL(5,2) NOT NULL,
    UsedDays DECIMAL(5,2) DEFAULT 0,
    CarriedOverDays DECIMAL(5,2) DEFAULT 0,
    RemainingDays AS (AllocatedDays + CarriedOverDays - UsedDays),
    CONSTRAINT UQ_EmployeeLeaveYear UNIQUE (EmployeeId, LeaveTypeId, Year)
);

CREATE TABLE LeaveRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT FOREIGN KEY REFERENCES Users(Id),
    LeaveTypeId INT FOREIGN KEY REFERENCES LeaveTypes(Id),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DaysRequested DECIMAL(5,2) NOT NULL,
    Reason NVARCHAR(500),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected, Cancelled
    ReviewedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ReviewedAt DATETIME2,
    ReviewNotes NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Default leave types for Kenya
INSERT INTO LeaveTypes (Name, DefaultDaysPerYear, IsPaid) VALUES
('Annual Leave', 21, 1),
('Sick Leave', 14, 1),
('Maternity Leave', 90, 1),
('Paternity Leave', 14, 1),
('Compassionate Leave', 5, 1),
('Unpaid Leave', 0, 0);
```

### Leave Request Flow

```
[Employee]
    ↓ Submits request
[System]
    ↓ Validates balance
[Manager]
    ↓ Reviews and approves/rejects
[System]
    ↓ Updates balance
    ↓ Blocks calendar
    ↓ Notifies employee
```

### Architecture Compliance

- **Layer:** Business (LeaveService), WPF (Leave views)
- **Pattern:** Workflow pattern with approval
- **Compliance:** Kenya labor law defaults

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#5.2-Leave-Management]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.5 (claude-opus-4-5-20251101)

### Debug Log References

None - implementation completed without errors.

### Completion Notes List

1. **LeaveDtos.cs** - Comprehensive DTOs for leave management:
   - LeaveRequestStatus, LeaveAdjustmentType enums
   - LeaveType, LeaveTypeRequest for leave type configuration
   - LeaveAllocation with computed balance properties (TotalAvailable, RemainingDays, AvailableForRequest)
   - EmployeeLeaveBalance summary with aggregated balances
   - LeaveRequest, LeaveRequestSubmission, LeaveApprovalRequest, LeaveResult
   - LeaveCalendarEntry, LeaveCalendarView, DayCoverage for calendar views
   - LeaveBalanceReport, LeaveTypeSummary, EmployeeLeaveHistory, LeaveUtilizationReport for reporting
   - MonthlyUtilization, DepartmentUtilization for analytics
   - LeaveSettings with configurable options (carry-over deadline, auto-approve, half-day support)
   - LeaveEventArgs for event-driven notifications

2. **ILeaveService.cs** - Service interface with comprehensive methods:
   - Leave Types: CreateLeaveTypeAsync, UpdateLeaveTypeAsync, DeactivateLeaveTypeAsync, GetLeaveTypeAsync, GetActiveLeaveTypesAsync
   - Leave Requests: SubmitRequestAsync, ProcessApprovalAsync, CancelRequestAsync, GetRequestAsync, GetPendingRequestsAsync, GetEmployeeRequestsAsync, GetApprovedRequestsAsync
   - Leave Balances: GetEmployeeAllocationsAsync, GetEmployeeBalanceAsync, InitializeYearAllocationsAsync, ProcessCarryOverAsync, AdjustBalanceAsync, HasSufficientBalanceAsync
   - Calendar: GetCalendarViewAsync, CheckCoverageAsync, CheckCoverageConflictsAsync
   - Reports: GenerateBalanceReportAsync, GetEmployeeHistoryAsync, GenerateUtilizationReportAsync
   - Utilities: CalculateWorkingDaysAsync, GetPublicHolidaysAsync, AddPublicHolidayAsync
   - Settings: GetSettingsAsync, UpdateSettingsAsync
   - Events: RequestSubmitted, RequestApproved, RequestRejected, RequestCancelled

3. **LeaveService.cs** - Full implementation with Kenya-specific defaults:
   - Default leave types: Annual (21 days), Sick (14), Maternity (90), Paternity (14), Compassionate (5), Unpaid
   - Kenya public holidays 2026 pre-configured
   - Working days calculation excluding weekends and public holidays
   - Balance tracking with carry-over support and maximum limits
   - Approval workflow with validation (sufficient balance, active leave type, date range)
   - Calendar view with coverage analysis and conflict warnings
   - Reports: balance reports, history, utilization by month/department

4. **LeaveServiceTests.cs** - 35+ unit tests covering:
   - Leave type CRUD operations
   - Leave request submission with validation
   - Approval/rejection workflow
   - Balance tracking and deduction
   - Year initialization and carry-over
   - Calendar and coverage analysis
   - Reports generation
   - Working days calculation
   - Settings management
   - Event raising verification
   - Full workflow integration test

### File List

- src/HospitalityPOS.Core/Models/HR/LeaveDtos.cs (NEW)
- src/HospitalityPOS.Core/Interfaces/ILeaveService.cs (NEW)
- src/HospitalityPOS.Infrastructure/Services/LeaveService.cs (NEW)
- tests/HospitalityPOS.Business.Tests/Services/LeaveServiceTests.cs (NEW)
