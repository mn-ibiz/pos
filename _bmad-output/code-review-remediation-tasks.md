# Code Review Remediation Tasks

**Generated:** 2026-01-16
**Review Scope:** Epics 23-28, 39-40 (36 stories)
**Total Issues Found:** 119 (28 Critical, 32 High, 41 Medium, 18 Low)

---

## Priority Legend

- **P0 - CRITICAL**: Blocking issues, app crashes, legal/compliance risk
- **P1 - HIGH**: Major functionality gaps, security issues
- **P2 - MEDIUM**: Performance, incomplete features
- **P3 - LOW**: Code quality, minor improvements

---

## Phase 1: Critical Infrastructure Fixes (P0)

### 1.1 Dependency Injection Registration

**File:** `src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Task 1.1.1**: Register Stock Transfer services
  ```csharp
  services.AddScoped<IStockTransferService, StockTransferService>();
  services.AddScoped<IStockReservationService, StockReservationService>();
  ```

- [ ] **Task 1.1.2**: Register Epic 25 Sync services
  ```csharp
  services.AddScoped<ILocalDatabaseService, LocalDatabaseService>();
  services.AddScoped<ISyncQueueService, SyncQueueService>();
  services.AddScoped<ISyncHubService, SyncHubService>();
  services.AddScoped<ISyncHubServiceFactory, SyncHubServiceFactory>();
  services.AddScoped<IConflictResolutionService, ConflictResolutionService>();
  services.AddScoped<ISyncStatusService, SyncStatusService>();
  ```

- [ ] **Task 1.1.3**: Register Connectivity service (Epic 39)
  ```csharp
  services.AddScoped<IConnectivityService, ConnectivityService>();
  ```

- [ ] **Task 1.1.4**: Verify all registered services resolve correctly with integration test

---

### 1.2 Missing WPF Value Converters (Epic 40 - App Will Crash)

**Location:** `src/HospitalityPOS.WPF/Converters/`

- [ ] **Task 1.2.1**: Create `BoolToColorConverter.cs`
  - Convert bool to Brush (true = Green, false = Red)
  - Register in `App.xaml` ResourceDictionary

- [ ] **Task 1.2.2**: Create `GreaterThanZeroToVisibilityConverter.cs`
  - Convert int/decimal > 0 to Visible, else Collapsed

- [ ] **Task 1.2.3**: Create `CurrentHourBrushConverter.cs`
  - Highlight current hour in hourly sales chart

- [ ] **Task 1.2.4**: Create `SalesToHeightConverter.cs`
  - Convert sales value to bar height for chart

- [ ] **Task 1.2.5**: Register all converters in `App.xaml`
  ```xml
  <converters:BoolToColorConverter x:Key="BoolToColorConverter"/>
  <converters:GreaterThanZeroToVisibilityConverter x:Key="GreaterThanZeroToVisibility"/>
  <converters:CurrentHourBrushConverter x:Key="CurrentHourBrushConverter"/>
  <converters:SalesToHeightConverter x:Key="SalesToHeightConverter"/>
  ```

---

### 1.3 Missing Background Jobs (Epic 39 - Compliance Risk)

**Location:** `src/HospitalityPOS.Infrastructure/BackgroundJobs/`

- [ ] **Task 1.3.1**: Create `ExpireBatchesJob.cs`
  - Implement as `BackgroundService` or `IHostedService`
  - Run daily at midnight
  - Query batches where `ExpiryDate < DateTime.Today && Status == "Active"`
  - Update status to "Expired"
  - Log all expired batches
  - Send notification to managers

- [ ] **Task 1.3.2**: Create `ExpirePointsJob.cs`
  - Implement as `BackgroundService`
  - Run monthly
  - Query loyalty transactions where `ExpiresAt < DateTime.UtcNow`
  - Create "Expire" transaction to deduct points
  - Update member balance
  - Optionally send SMS notification before expiry

- [ ] **Task 1.3.3**: Create `ExpireReservationsJob.cs`
  - Run every hour
  - Query reservations where `ExpiresAt < DateTime.UtcNow && Status == "Active"`
  - Release reserved stock back to available
  - Log expired reservations

- [ ] **Task 1.3.4**: Register all jobs in `Program.cs`
  ```csharp
  services.AddHostedService<ExpireBatchesJob>();
  services.AddHostedService<ExpirePointsJob>();
  services.AddHostedService<ExpireReservationsJob>();
  ```

---

### 1.4 Connectivity Service (Epic 39 - Offline Detection)

**Location:** `src/HospitalityPOS.Infrastructure/Services/`

- [ ] **Task 1.4.1**: Create `IConnectivityService.cs` interface
  ```csharp
  public interface IConnectivityService
  {
      ConnectivityStatus CurrentStatus { get; }
      DateTime? LastOnlineTime { get; }
      event EventHandler<ConnectivityChangedEventArgs> StatusChanged;
      Task<ConnectivityStatus> CheckConnectivityAsync();
      void StartMonitoring(TimeSpan interval);
      void StopMonitoring();
  }
  ```

- [ ] **Task 1.4.2**: Create `ConnectivityService.cs` implementation
  - Use Timer for periodic checks (default 30 seconds)
  - Ping eTIMS endpoint for connectivity check
  - Fallback to general internet check
  - Raise `StatusChanged` event on state transitions
  - States: Online, Degraded, Offline

- [ ] **Task 1.4.3**: Create `ConnectivityStatusControl.xaml` UserControl
  - Green circle = Online
  - Yellow circle = Degraded
  - Red circle = Offline
  - Tooltip with last sync time

- [ ] **Task 1.4.4**: Add status control to `MainWindow.xaml` status bar

---

## Phase 2: Epic 25 - Sync Functionality (Currently Non-Functional)

### 2.1 Fix SyncQueueService ProcessItemAsync

**File:** `src/HospitalityPOS.Infrastructure/Services/SyncQueueService.cs`

- [ ] **Task 2.1.1**: Implement actual sync logic in `ProcessItemAsync`
  - Remove TODO stub code
  - Add HTTP client for cloud API calls
  - Handle different queue types (eTIMS, M-Pesa, CloudBackup)
  - Implement proper error handling and retry logic

- [ ] **Task 2.1.2**: Add cloud API endpoint configuration
  - Add `CloudApiSettings` to appsettings.json
  - Create `ICloudApiService` for actual data transmission

- [ ] **Task 2.1.3**: Integrate with `IConnectivityService`
  - Only process queue when online
  - Subscribe to connectivity status changes
  - Auto-trigger sync on reconnection

---

### 2.2 SignalR Hub Server Implementation

**Location:** `src/HospitalityPOS.Infrastructure/Hubs/`

- [ ] **Task 2.2.1**: Create `SyncHub.cs` SignalR hub
  ```csharp
  public class SyncHub : Hub<ISyncHubClient>
  {
      Task ReceiveTransaction(TransactionSyncDto dto);
      Task ReceiveProductUpdate(ProductSyncDto dto);
      Task ReceiveInventoryUpdate(InventorySyncDto dto);
      Task SyncCompleted(SyncResultDto result);
  }
  ```

- [ ] **Task 2.2.2**: Configure SignalR in `Program.cs`
  ```csharp
  builder.Services.AddSignalR();
  app.MapHub<SyncHub>("/hubs/sync");
  ```

- [ ] **Task 2.2.3**: Add authentication to SignalR hub
  - JWT token validation
  - Store/tenant isolation

---

### 2.3 Fix ConflictResolutionService

**File:** `src/HospitalityPOS.Infrastructure/Services/ConflictResolutionService.cs`

- [ ] **Task 2.3.1**: Implement `ApplyResolutionAsync` to actually update entities
  - Currently only logs and updates notes
  - Must apply resolved data to actual database entity
  - Use reflection or typed handlers per entity type

- [ ] **Task 2.3.2**: Persist audit trail to database
  - Currently in-memory `List<ConflictAuditDto>`
  - Create `SyncConflictAudit` entity
  - Save all resolution actions

- [ ] **Task 2.3.3**: Persist resolution rules to database
  - Currently in-memory `List<ConflictResolutionRuleDto>`
  - Create `ConflictResolutionRule` entity
  - Allow admin configuration

---

### 2.4 Sync Status Dashboard UI

**Location:** `src/HospitalityPOS.WPF/`

- [ ] **Task 2.4.1**: Create `SyncStatusDashboardView.xaml`
  - Pending items by type (eTIMS, M-Pesa, etc.)
  - Sync history with timestamps
  - Failed items with error details
  - Manual "Sync Now" button

- [ ] **Task 2.4.2**: Create `SyncStatusDashboardViewModel.cs`
  - Inject `ISyncStatusService`
  - ObservableCollection for pending items
  - RelayCommand for manual sync

- [ ] **Task 2.4.3**: Add navigation to sync dashboard from main menu

---

## Phase 3: Epic 23 - Stock Transfer UI

### 3.1 Stock Transfer ViewModels

**Location:** `src/HospitalityPOS.WPF/ViewModels/`

- [ ] **Task 3.1.1**: Create `StockTransferListViewModel.cs`
  - List all transfer requests with filters
  - Navigation to create/view/approve

- [ ] **Task 3.1.2**: Create `CreateTransferRequestViewModel.cs`
  - Source location selection
  - Product selection with available stock display
  - Quantity entry with validation
  - Submit request command

- [ ] **Task 3.1.3**: Create `TransferApprovalViewModel.cs`
  - Incoming request queue
  - Approve/Reject/Modify workflow
  - Stock reservation on approval

- [ ] **Task 3.1.4**: Create `TransferShipmentViewModel.cs`
  - Pick list display
  - Barcode scanning for pick confirmation
  - Dispatch confirmation

- [ ] **Task 3.1.5**: Create `TransferReceivingViewModel.cs`
  - Pending shipments queue
  - Receive quantities entry
  - Variance calculation and display

---

### 3.2 Stock Transfer Views

**Location:** `src/HospitalityPOS.WPF/Views/StockTransfer/`

- [ ] **Task 3.2.1**: Create `StockTransferListView.xaml`
- [ ] **Task 3.2.2**: Create `CreateTransferRequestView.xaml`
- [ ] **Task 3.2.3**: Create `TransferApprovalView.xaml`
- [ ] **Task 3.2.4**: Create `TransferShipmentView.xaml`
- [ ] **Task 3.2.5**: Create `TransferReceivingView.xaml`
- [ ] **Task 3.2.6**: Create `PickListPrintView.xaml` (print preview)

---

### 3.3 Print Services for Stock Transfer

**Location:** `src/HospitalityPOS.Infrastructure/Services/`

- [ ] **Task 3.3.1**: Implement pick list printing
  - Generate PDF from `PickListDto`
  - ESC/POS commands for thermal printers
  - Integrate with existing `IPrintService`

- [ ] **Task 3.3.2**: Implement transfer document printing
  - Generate PDF from `TransferDocumentDto`
  - Include all shipment details
  - Signature line for receiver

---

### 3.4 Missing Entity

**File:** `src/HospitalityPOS.Core/Entities/StockTransferEntities.cs`

- [ ] **Task 3.4.1**: Add `TransferVarianceInvestigation` entity
  ```csharp
  public class TransferVarianceInvestigation : BaseEntity
  {
      public int TransferReceiptId { get; set; }
      public int ProductId { get; set; }
      public decimal VarianceQuantity { get; set; }
      public string Status { get; set; } // Open, InProgress, Resolved
      public string? Resolution { get; set; }
      public int? ResolvedByUserId { get; set; }
      public DateTime? ResolvedAt { get; set; }
  }
  ```

- [ ] **Task 3.4.2**: Add DbSet and EF configuration
- [ ] **Task 3.4.3**: Create migration

---

## Phase 4: Epic 24 & 39 - Batch/Expiry UI

### 4.1 Missing ViewModels

**Location:** `src/HospitalityPOS.WPF/ViewModels/`

- [ ] **Task 4.1.1**: Create `ExpiryDashboardViewModel.cs`
- [ ] **Task 4.1.2**: Create `BatchTraceabilityViewModel.cs`
- [ ] **Task 4.1.3**: Create `WasteReportViewModel.cs`

### 4.2 Missing Views

**Location:** `src/HospitalityPOS.WPF/Views/`

- [ ] **Task 4.2.1**: Create `ExpiryDashboardView.xaml`
  - Color-coded expiry groups (30/14/7 days)
  - Click to view affected products

- [ ] **Task 4.2.2**: Create `ExpiryAlertWidget.xaml` (reusable)
- [ ] **Task 4.2.3**: Create `BatchTraceabilityView.xaml`
- [ ] **Task 4.2.4**: Create `WasteReportView.xaml`

### 4.3 Expiry Settings UI

- [ ] **Task 4.3.1**: Create `ExpirySettingsView.xaml`
  - System-wide thresholds
  - Category overrides
  - Blocking/warning configuration

- [ ] **Task 4.3.2**: Create `ExpirySettingsViewModel.cs`

### 4.4 Manager Override Audit

- [ ] **Task 4.4.1**: Create `ExpiredItemOverride` entity
  ```csharp
  public class ExpiredItemOverride : BaseEntity
  {
      public int ProductBatchId { get; set; }
      public int OrderItemId { get; set; }
      public int OverrideByUserId { get; set; }
      public string Reason { get; set; }
      public DateTime CreatedAt { get; set; }
  }
  ```

- [ ] **Task 4.4.2**: Add DbSet and migration
- [ ] **Task 4.4.3**: Implement override logging in `ExpiryValidationService`

---

## Phase 5: Epic 26-28 - KDS & Labels

### 5.1 SignalR Hub for KDS

**Location:** `src/HospitalityPOS.Infrastructure/Hubs/`

- [ ] **Task 5.1.1**: Create `KdsHub.cs`
  ```csharp
  public class KdsHub : Hub<IKdsHubClient>
  {
      Task ReceiveOrder(KdsOrderDto order);
      Task OrderStatusChanged(int orderId, KdsOrderStatus status);
      Task AllCallMessage(AllCallMessageDto message);
      Task StationStatusChanged(int stationId, bool isOnline);
  }
  ```

- [ ] **Task 5.1.2**: Configure hub in `Program.cs`
- [ ] **Task 5.1.3**: Integrate with `KdsOrderService` to broadcast on order creation

### 5.2 Fix Label Printer Tests

**File:** `src/HospitalityPOS.Infrastructure/Services/LabelPrinterService.cs`

- [ ] **Task 5.2.1**: Implement actual serial port test (lines 482-518)
- [ ] **Task 5.2.2**: Implement actual USB printer test
- [ ] **Task 5.2.3**: Fix network printer test with proper error handling
- [ ] **Task 5.2.4**: Add TSPL language support to `GenerateTestLabel`

### 5.3 Recipe Cost Unit Conversion

**File:** `src/HospitalityPOS.Infrastructure/Services/RecipeService.cs`

- [ ] **Task 5.3.1**: Fix `CalculateIngredientCost` (line 734)
  - Add unit conversion before cost calculation
  - Use `UnitConversion` entity for conversion factors
  - Handle different units (g/kg, ml/L, etc.)

---

## Phase 6: Epic 39 - Remaining Gaps

### 6.1 eTIMS UI

- [x] **Task 6.1.1**: Create `EtimsSettingsView.xaml` (Already existed in codebase)
- [x] **Task 6.1.2**: Integrate eTIMS auto-submit with `ReceiptService.SettleReceiptAsync` (Verified already integrated)

### 6.2 M-Pesa Enhancements

- [x] **Task 6.2.1**: Add Excel export for M-Pesa reports
- [x] **Task 6.2.2**: Fix phone validation to include Telkom `254[1]` prefix
- [x] **Task 6.2.3**: Add M-Pesa offline queue integration

### 6.3 Loyalty UI

- [x] **Task 6.3.1**: Create `CustomerListView.xaml`
- [x] **Task 6.3.2**: Create `LoyaltySettingsView.xaml`
- [x] **Task 6.3.3**: Integrate loyalty with POSViewModel

---

## Phase 7: Epic 40 - Dashboard Fixes

### 7.1 Multi-Branch Support

**File:** `src/HospitalityPOS.Infrastructure/Services/DashboardService.cs`

- [x] **Task 7.1.1**: Add `StoreId` to `Receipt` entity
- [x] **Task 7.1.2**: Implement actual store filtering in all dashboard methods
- [x] **Task 7.1.3**: Fix `GetBranchSummariesAsync` to return real data

### 7.2 Dashboard UI Fixes

- [x] **Task 7.2.1**: Add branch filter dropdown to `DashboardView.xaml`
- [x] **Task 7.2.2**: Add sync status widget to dashboard
- [x] **Task 7.2.3**: Implement click-to-view-product-details navigation
- [x] **Task 7.2.4**: Add expiry alert click navigation

---

## Phase 8: Documentation & Status Sync

### 8.1 Sprint Status Corrections

**File:** `_bmad-output/sprint-status.yaml`

- [x] **Task 8.1.1**: Update story 23-2 status to `in-progress`
- [x] **Task 8.1.2**: Update story 23-3 status to `in-progress`
- [x] **Task 8.1.3**: Update story 23-4 status to `in-progress`
- [x] **Task 8.1.4**: Update story 40-1 status to `in-progress` (Already done per dashboard implementation)

### 8.2 Story File Checkbox Updates

- [x] **Task 8.2.1**: Update all Epic 39 story files to check completed task boxes (sprint-status.yaml updated)
- [x] **Task 8.2.2**: Uncheck incomplete tasks in stories marked "Done" (Not applicable - no false Done stories)

---

## Verification Checklist

After completing all phases:

- [ ] All services resolve from DI container without exceptions
- [ ] DashboardView loads without XamlParseException
- [ ] Background jobs run on schedule
- [ ] Connectivity status updates correctly when network changes
- [ ] Sync queue actually syncs data (not just marks complete)
- [ ] KDS receives real-time order updates via SignalR
- [ ] Expired batches are auto-blocked at POS
- [ ] Loyalty points expire according to configuration
- [ ] Stock transfer workflow completes end-to-end with UI
- [ ] All unit tests pass
- [ ] Integration tests added for critical paths

---

## Estimated Effort

| Phase | Tasks | Estimated Hours |
|-------|-------|-----------------|
| Phase 1 | 17 | 16-24 |
| Phase 2 | 12 | 24-32 |
| Phase 3 | 15 | 32-40 |
| Phase 4 | 10 | 16-24 |
| Phase 5 | 7 | 12-16 |
| Phase 6 | 7 | 12-16 |
| Phase 7 | 5 | 8-12 |
| Phase 8 | 4 | 2-4 |
| **Total** | **77** | **122-168** |

---

## Notes for Dev Agent

1. **Start with Phase 1** - these are blocking issues that prevent the app from running
2. **Phase 2 (Epic 25)** can be deprioritized if offline sync is not immediately needed
3. **Phase 3 (Stock Transfer UI)** is a large effort - consider breaking into smaller PRs
4. **Always run existing tests** after changes to ensure no regressions
5. **Create integration tests** for new background jobs
6. **Document any architecture decisions** made during implementation
