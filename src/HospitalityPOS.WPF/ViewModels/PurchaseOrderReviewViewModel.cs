using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the manager PO review dashboard.
/// </summary>
public partial class PurchaseOrderReviewViewModel : ViewModelBase, INavigationAware
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IInventoryAnalyticsService _analyticsService;
    private readonly IPurchaseOrderConsolidationService _consolidationService;
    private readonly INotificationService _notificationService;
    private readonly ISupplierService _supplierService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<ReorderSuggestion> _pendingSuggestions = [];

    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _pendingPurchaseOrders = [];

    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _recentPurchaseOrders = [];

    [ObservableProperty]
    private ObservableCollection<SupplierSuggestionGroup> _supplierGroups = [];

    [ObservableProperty]
    private ReorderSuggestion? _selectedSuggestion;

    [ObservableProperty]
    private PurchaseOrder? _selectedPurchaseOrder;

    [ObservableProperty]
    private bool _isLoading;

    // Dashboard statistics
    [ObservableProperty]
    private int _pendingSuggestionCount;

    [ObservableProperty]
    private int _pendingPOCount;

    [ObservableProperty]
    private int _approvedTodayCount;

    [ObservableProperty]
    private decimal _totalPendingValue;

    [ObservableProperty]
    private decimal _totalApprovedTodayValue;

    [ObservableProperty]
    private int _overdueCount;

    // Tab selection
    [ObservableProperty]
    private int _selectedTabIndex;

    public PurchaseOrderReviewViewModel(
        IPurchaseOrderService purchaseOrderService,
        IInventoryAnalyticsService analyticsService,
        IPurchaseOrderConsolidationService consolidationService,
        INotificationService notificationService,
        ISupplierService supplierService,
        IDialogService dialogService,
        ISessionService sessionService,
        INavigationService navigationService,
        ILogger logger) : base(logger)
    {
        _purchaseOrderService = purchaseOrderService ?? throw new ArgumentNullException(nameof(purchaseOrderService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _consolidationService = consolidationService ?? throw new ArgumentNullException(nameof(consolidationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Purchase Order Review Dashboard";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Loads all dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                var storeId = _sessionService.CurrentStoreId ?? 1;

                // Load pending suggestions
                var suggestions = await _analyticsService.GetReorderSuggestionsAsync(storeId, "Pending").ConfigureAwait(true);
                PendingSuggestions = new ObservableCollection<ReorderSuggestion>(suggestions);
                PendingSuggestionCount = PendingSuggestions.Count;

                // Load pending POs (draft status)
                var allPOs = await _purchaseOrderService.GetAllPurchaseOrdersAsync(true).ConfigureAwait(true);
                var draftPOs = allPOs.Where(po => po.Status == PurchaseOrderStatus.Draft && !po.IsDeleted).ToList();
                PendingPurchaseOrders = new ObservableCollection<PurchaseOrder>(draftPOs);
                PendingPOCount = draftPOs.Count;
                TotalPendingValue = draftPOs.Sum(po => po.TotalAmount);

                // Load recent POs (sent in last 7 days)
                var recentPOs = allPOs
                    .Where(po => po.Status != PurchaseOrderStatus.Draft &&
                                 po.UpdatedAt >= DateTime.UtcNow.AddDays(-7) &&
                                 !po.IsDeleted)
                    .OrderByDescending(po => po.UpdatedAt)
                    .Take(20)
                    .ToList();
                RecentPurchaseOrders = new ObservableCollection<PurchaseOrder>(recentPOs);

                // Calculate approved today
                var today = DateTime.Today;
                var approvedToday = allPOs.Where(po =>
                    po.Status == PurchaseOrderStatus.Sent &&
                    po.UpdatedAt?.Date == today).ToList();
                ApprovedTodayCount = approvedToday.Count;
                TotalApprovedTodayValue = approvedToday.Sum(po => po.TotalAmount);

                // Calculate overdue
                OverdueCount = allPOs.Count(po =>
                    po.ExpectedDate.HasValue &&
                    po.ExpectedDate.Value.Date < DateTime.Today &&
                    po.Status != PurchaseOrderStatus.Complete &&
                    po.Status != PurchaseOrderStatus.Cancelled &&
                    !po.IsDeleted);

                // Load supplier groups
                var groups = await _consolidationService.GroupSuggestionsBySupplierAsync(storeId).ConfigureAwait(true);
                SupplierGroups = new ObservableCollection<SupplierSuggestionGroup>(groups);
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading dashboard...").ConfigureAwait(true);
    }

    /// <summary>
    /// Approves a single suggestion.
    /// </summary>
    [RelayCommand]
    private async Task ApproveSuggestionAsync(ReorderSuggestion? suggestion)
    {
        suggestion ??= SelectedSuggestion;
        if (suggestion == null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            suggestion.Status = "Approved";
            suggestion.ApprovedByUserId = _sessionService.CurrentUserId;
            suggestion.ApprovedAt = DateTime.UtcNow;
            await _analyticsService.UpdateReorderSuggestionAsync(suggestion).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Approved",
                $"Suggestion for {suggestion.Product?.Name} has been approved.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Approving suggestion...").ConfigureAwait(true);
    }

    /// <summary>
    /// Rejects a single suggestion.
    /// </summary>
    [RelayCommand]
    private async Task RejectSuggestionAsync(ReorderSuggestion? suggestion)
    {
        suggestion ??= SelectedSuggestion;
        if (suggestion == null)
        {
            return;
        }

        var reason = await _dialogService.ShowInputAsync("Reject Suggestion", "Enter rejection reason:").ConfigureAwait(true);
        if (reason == null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            suggestion.Status = "Rejected";
            suggestion.Notes = reason;
            suggestion.ApprovedByUserId = _sessionService.CurrentUserId;
            suggestion.ApprovedAt = DateTime.UtcNow;
            await _analyticsService.UpdateReorderSuggestionAsync(suggestion).ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Rejecting suggestion...").ConfigureAwait(true);
    }

    /// <summary>
    /// Approves all pending suggestions.
    /// </summary>
    [RelayCommand]
    private async Task ApproveAllSuggestionsAsync()
    {
        if (!PendingSuggestions.Any())
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Approve All",
            $"Approve all {PendingSuggestions.Count} pending suggestions?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var userId = _sessionService.CurrentUserId;
            var now = DateTime.UtcNow;

            foreach (var suggestion in PendingSuggestions)
            {
                suggestion.Status = "Approved";
                suggestion.ApprovedByUserId = userId;
                suggestion.ApprovedAt = now;
                await _analyticsService.UpdateReorderSuggestionAsync(suggestion).ConfigureAwait(true);
            }

            await _dialogService.ShowMessageAsync("Success",
                $"Approved {PendingSuggestions.Count} suggestions.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Approving all suggestions...").ConfigureAwait(true);
    }

    /// <summary>
    /// Sends a draft PO to supplier.
    /// </summary>
    [RelayCommand]
    private async Task SendPurchaseOrderAsync(PurchaseOrder? po)
    {
        po ??= SelectedPurchaseOrder;
        if (po == null || po.Status != PurchaseOrderStatus.Draft)
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Send to Supplier",
            $"Send PO {po.PONumber} to {po.Supplier?.Name}?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _purchaseOrderService.SendToSupplierAsync(po.Id).ConfigureAwait(true);

            await _notificationService.NotifyPOGeneratedAsync(
                po.Id,
                po.PONumber,
                po.TotalAmount).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success",
                $"PO {po.PONumber} sent to supplier.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Sending to supplier...").ConfigureAwait(true);
    }

    /// <summary>
    /// Archives a purchase order.
    /// </summary>
    [RelayCommand]
    private async Task ArchivePurchaseOrderAsync(PurchaseOrder? po)
    {
        po ??= SelectedPurchaseOrder;
        if (po == null)
        {
            return;
        }

        var reason = await _dialogService.ShowInputAsync("Archive PO", "Enter archive reason (optional):").ConfigureAwait(true);
        if (reason == null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            po.Status = PurchaseOrderStatus.Archived;
            po.ArchivedAt = DateTime.UtcNow;
            po.ArchivedByUserId = _sessionService.CurrentUserId;
            po.ArchiveReason = string.IsNullOrEmpty(reason) ? null : reason;

            await _purchaseOrderService.UpdatePurchaseOrderAsync(po).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success",
                $"PO {po.PONumber} has been archived.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Archiving PO...").ConfigureAwait(true);
    }

    /// <summary>
    /// Deletes a purchase order.
    /// </summary>
    [RelayCommand]
    private async Task DeletePurchaseOrderAsync(PurchaseOrder? po)
    {
        po ??= SelectedPurchaseOrder;
        if (po == null)
        {
            return;
        }

        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Cancelled)
        {
            await _dialogService.ShowErrorAsync("Cannot Delete",
                "Only draft or cancelled POs can be deleted.").ConfigureAwait(true);
            return;
        }

        var reason = await _dialogService.ShowInputAsync("Delete PO",
            $"Enter deletion reason for PO {po.PONumber}:").ConfigureAwait(true);

        if (string.IsNullOrEmpty(reason))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            po.IsDeleted = true;
            po.DeletedAt = DateTime.UtcNow;
            po.DeletedByUserId = _sessionService.CurrentUserId;
            po.DeletionReason = reason;

            await _purchaseOrderService.UpdatePurchaseOrderAsync(po).ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success",
                $"PO {po.PONumber} has been deleted.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Deleting PO...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates consolidated POs from approved suggestions.
    /// </summary>
    [RelayCommand]
    private async Task CreateConsolidatedPOsAsync()
    {
        var approvedCount = PendingSuggestions.Count(s => s.Status == "Approved");
        if (approvedCount == 0)
        {
            await _dialogService.ShowErrorAsync("No Approved Suggestions",
                "Please approve some suggestions first before creating POs.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Create Consolidated POs",
            $"Create purchase orders from {approvedCount} approved suggestions?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var storeId = _sessionService.CurrentStoreId ?? 1;
            var result = await _consolidationService.CreateConsolidatedPurchaseOrdersAsync(
                storeId,
                sendImmediately: false).ConfigureAwait(true);

            if (result.Success)
            {
                var message = $"Created {result.PurchaseOrdersCreated} purchase orders " +
                              $"worth {result.TotalOrderValue:C}.";

                if (result.Warnings.Any())
                {
                    message += $"\n\nWarnings:\n{string.Join("\n", result.Warnings)}";
                }

                await _dialogService.ShowMessageAsync("POs Created", message).ConfigureAwait(true);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.ErrorMessage ?? "Failed to create POs.").ConfigureAwait(true);
            }

            await LoadDataAsync().ConfigureAwait(true);
        }, "Creating purchase orders...").ConfigureAwait(true);
    }

    /// <summary>
    /// Sends all pending POs.
    /// </summary>
    [RelayCommand]
    private async Task SendAllPendingPOsAsync()
    {
        if (!PendingPurchaseOrders.Any())
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Send All POs",
            $"Send all {PendingPurchaseOrders.Count} pending POs to suppliers?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var sent = 0;
            foreach (var po in PendingPurchaseOrders)
            {
                await _purchaseOrderService.SendToSupplierAsync(po.Id).ConfigureAwait(true);
                sent++;
            }

            await _dialogService.ShowMessageAsync("Success",
                $"Sent {sent} purchase orders to suppliers.").ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);
        }, "Sending all POs...").ConfigureAwait(true);
    }

    /// <summary>
    /// Views PO details.
    /// </summary>
    [RelayCommand]
    private void ViewPurchaseOrder(PurchaseOrder? po)
    {
        po ??= SelectedPurchaseOrder;
        if (po != null)
        {
            _navigationService.NavigateTo<PurchaseOrdersViewModel>(po.Id);
        }
    }

    /// <summary>
    /// Navigates back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Gets the priority color.
    /// </summary>
    public static string GetPriorityColor(string? priority)
    {
        return priority switch
        {
            "Critical" => "#F44336",
            "High" => "#FF9800",
            "Medium" => "#2196F3",
            "Low" => "#4CAF50",
            _ => "#9E9E9E"
        };
    }
}
