# Story 3.1: Open Work Period

Status: done

## Story

As a manager,
I want to open a work period with an opening cash float,
So that the business day can begin and transactions can be processed.

## Acceptance Criteria

1. **Given** no work period is currently open
   **When** a manager opens a new work period
   **Then** system should prompt for opening cash float amount

2. **Given** opening float is entered
   **When** work period is created
   **Then** work period should be created with status "Open"

3. **Given** work period is opened
   **When** opening details are recorded
   **Then** opening timestamp and user should be recorded

4. **Given** work period is active
   **When** viewing the dashboard
   **Then** visual indicator should show work period is active on dashboard

5. **Given** previous day's closing exists
   **When** opening new work period
   **Then** optional: previous day's closing balance can be carried forward

## Tasks / Subtasks

- [x] Task 1: Create Work Period Service (AC: #1, #2, #3)
  - [x] Create IWorkPeriodService interface
  - [x] Implement GetCurrentWorkPeriodAsync
  - [x] Implement IsWorkPeriodOpenAsync
  - [x] Implement OpenWorkPeriodAsync
  - [x] Record opening user and timestamp

- [x] Task 2: Create Open Work Period Dialog (AC: #1, #5)
  - [x] Create OpenWorkPeriodDialog.xaml
  - [x] Add opening float amount input
  - [x] Add numeric keypad for touch entry
  - [x] Show previous closing balance (if exists)
  - [x] Add "Carry Forward" option

- [x] Task 3: Create Work Period Status Display (AC: #4)
  - [x] Add work period indicator to main layout status bar
  - [x] Show status: "OPEN" with green indicator
  - [x] Show duration since opening (updates every second)
  - [x] Color-coded indicator (green=open, gray=closed)

- [x] Task 4: Implement Permission Check
  - [x] Require WorkPeriod.Open permission
  - [x] Integrated with permission override system
  - [x] Allow permission override if needed

- [ ] Task 5: Prevent Sales Without Work Period
  - [ ] Check work period status before any sale (deferred to Epic 5)
  - [ ] Display message if no active work period (deferred to Epic 5)
  - [ ] Redirect to open work period if Manager (deferred to Epic 5)

## Dev Notes

### IWorkPeriodService Interface

```csharp
public interface IWorkPeriodService
{
    Task<WorkPeriod?> GetCurrentWorkPeriodAsync();
    Task<bool> IsWorkPeriodOpenAsync();
    Task<WorkPeriod> OpenWorkPeriodAsync(decimal openingFloat, int userId);
    Task<WorkPeriod> CloseWorkPeriodAsync(decimal closingCash, int userId);
    Task<WorkPeriod?> GetLastClosedWorkPeriodAsync();
    Task<XReport> GenerateXReportAsync(int workPeriodId);
    Task<ZReport> GenerateZReportAsync(int workPeriodId);
}
```

### Open Work Period Dialog Layout

```
+------------------------------------------+
|  Open Work Period                         |
+------------------------------------------+
|                                           |
|  Previous Period Closing: KSh 15,250.00   |
|                                           |
|  Opening Cash Float:                      |
|  +-----------------------------------+    |
|  |  KSh 10,000.00                    |    |
|  +-----------------------------------+    |
|                                           |
|  [ ] Carry forward previous balance      |
|                                           |
|    +---+  +---+  +---+                    |
|    | 1 |  | 2 |  | 3 |                    |
|    +---+  +---+  +---+                    |
|    +---+  +---+  +---+                    |
|    | 4 |  | 5 |  | 6 |                    |
|    +---+  +---+  +---+                    |
|    +---+  +---+  +---+                    |
|    | 7 |  | 8 |  | 9 |                    |
|    +---+  +---+  +---+                    |
|    +---+  +---+  +---+                    |
|    | . |  | 0 |  |CLR|                    |
|    +---+  +---+  +---+                    |
|                                           |
|  [Open Period]  [Cancel]                  |
+------------------------------------------+
```

### Work Period Entity

```csharp
public class WorkPeriod
{
    public int Id { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int OpenedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Variance { get; set; }
    public int? ZReportNumber { get; set; }
    public string Status { get; set; } = "Open"; // Open, Closed
    public string? Notes { get; set; }

    // Navigation
    public User OpenedByUser { get; set; } = null!;
    public User? ClosedByUser { get; set; }
}
```

### Work Period Status Indicator (Header)

```xml
<!-- In MainWindow or Header -->
<Border Background="{Binding WorkPeriodStatusColor}" CornerRadius="4" Padding="8,4">
    <StackPanel Orientation="Horizontal">
        <Ellipse Width="10" Height="10" Fill="LimeGreen" Margin="0,0,8,0"
                 Visibility="{Binding IsWorkPeriodOpen, Converter={StaticResource BoolToVisibility}}"/>
        <TextBlock Text="{Binding WorkPeriodStatusText}" FontWeight="Bold"/>
        <TextBlock Text="{Binding WorkPeriodDuration}" Margin="16,0,0,0"/>
    </StackPanel>
</Border>
```

### Status Display Examples
- "OPEN - 3h 45m" (green indicator)
- "CLOSED" (red indicator)
- "No Active Period" (gray indicator)

### Validation Rules
- Opening float must be >= 0
- Opening float must be a valid decimal
- Only one work period can be open at a time
- User must have WorkPeriod_Open permission

### Audit Logging
Log work period opening with:
- Timestamp
- User who opened
- Opening float amount
- Previous period reference (if carrying forward)

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.1-Work-Period-Management]
- [Source: docs/PRD_Hospitality_POS_System.md#5.1.1-Opening-Work-Period]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created IWorkPeriodService interface with comprehensive work period management methods
- Implemented WorkPeriodService with full CRUD operations and audit logging
- Created OpenWorkPeriodDialog.xaml with touch-optimized 4-column numeric keypad
- Added previous closing balance display with "Use This" carry-forward button
- Updated IDialogService and DialogService with ShowOpenWorkPeriodDialogAsync
- Updated MainViewModel with IServiceScopeFactory for scoped service access
- Added IsWorkPeriodOpen property and real-time duration display (updates every second)
- Integrated with permission override system (WorkPeriod.Open permission)
- Enhanced MainWindow status bar with color-coded work period indicator
- Registered IWorkPeriodService as Scoped in DI container
- Task 5 (sales prevention) deferred to Epic 5 (Sales & Order Management) as it requires sales functionality

### File List
- src/HospitalityPOS.Core/Interfaces/IWorkPeriodService.cs (new)
- src/HospitalityPOS.Core/Models/OpenWorkPeriodResult.cs (new)
- src/HospitalityPOS.Infrastructure/Services/WorkPeriodService.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/OpenWorkPeriodDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/OpenWorkPeriodDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs (modified)
- src/HospitalityPOS.WPF/Views/MainWindow.xaml (modified)
- src/HospitalityPOS.WPF/App.xaml.cs (modified)

### Change Log
- 2025-12-30: Story implemented - Tasks 1-4 completed, Task 5 deferred
