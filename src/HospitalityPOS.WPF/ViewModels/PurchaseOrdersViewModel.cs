using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the purchase orders management view.
/// </summary>
public partial class PurchaseOrdersViewModel : ViewModelBase, INavigationAware
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISupplierService _supplierService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly IEmailService _emailService;
    private readonly ISystemConfigurationService _configurationService;
    private readonly IInventoryAnalyticsService _inventoryAnalyticsService;

    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _purchaseOrders = [];

    [ObservableProperty]
    private PurchaseOrder? _selectedPurchaseOrder;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private PurchaseOrderStatus? _selectedStatusFilter;

    [ObservableProperty]
    private int? _filterSupplierId;

    [ObservableProperty]
    private Supplier? _filterSupplier;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = [];

    [ObservableProperty]
    private int _draftCount;

    [ObservableProperty]
    private int _sentCount;

    [ObservableProperty]
    private int _partiallyReceivedCount;

    [ObservableProperty]
    private int _overdueCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReorderSuggestions))]
    private ObservableCollection<ReorderSuggestion> _reorderSuggestions = [];

    [ObservableProperty]
    private bool _isSuggestionsPanelExpanded = true;

    /// <summary>
    /// Gets whether there are any reorder suggestions.
    /// </summary>
    public bool HasReorderSuggestions => ReorderSuggestions.Count > 0;

    private IReadOnlyList<PurchaseOrder> _allPurchaseOrders = [];

    /// <summary>
    /// Gets the available status options for filtering.
    /// </summary>
    public static IReadOnlyList<PurchaseOrderStatus?> StatusOptions { get; } =
    [
        null, // All
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.Complete,
        PurchaseOrderStatus.Cancelled
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrdersViewModel"/> class.
    /// </summary>
    public PurchaseOrdersViewModel(
        IPurchaseOrderService purchaseOrderService,
        ISupplierService supplierService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService,
        IEmailService emailService,
        ISystemConfigurationService configurationService,
        IInventoryAnalyticsService inventoryAnalyticsService,
        ILogger logger) : base(logger)
    {
        _purchaseOrderService = purchaseOrderService ?? throw new ArgumentNullException(nameof(purchaseOrderService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _inventoryAnalyticsService = inventoryAnalyticsService ?? throw new ArgumentNullException(nameof(inventoryAnalyticsService));

        Title = "Purchase Orders";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // Check if navigating with a supplier filter
        if (parameter is int supplierId)
        {
            FilterSupplierId = supplierId;
        }

        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatusFilterChanged(PurchaseOrderStatus? value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Loads all data including purchase orders and suppliers.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                // Load suppliers for filter dropdown
                var suppliers = await _supplierService.GetAllSuppliersAsync().ConfigureAwait(true);
                Suppliers = new ObservableCollection<Supplier>(suppliers);

                // Set filter supplier if navigated with ID
                if (FilterSupplierId.HasValue)
                {
                    FilterSupplier = suppliers.FirstOrDefault(s => s.Id == FilterSupplierId.Value);
                }

                // Load purchase orders
                if (FilterSupplierId.HasValue)
                {
                    _allPurchaseOrders = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(FilterSupplierId.Value, true).ConfigureAwait(true);
                }
                else
                {
                    _allPurchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync(true).ConfigureAwait(true);
                }

                // Load status counts
                DraftCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Draft).ConfigureAwait(true);
                SentCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Sent).ConfigureAwait(true);
                PartiallyReceivedCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.PartiallyReceived).ConfigureAwait(true);

                // Calculate overdue count (POs with expected date in the past and not complete/cancelled)
                OverdueCount = _allPurchaseOrders.Count(po =>
                    po.ExpectedDate.HasValue &&
                    po.ExpectedDate.Value.Date < DateTime.Today &&
                    po.Status != PurchaseOrderStatus.Complete &&
                    po.Status != PurchaseOrderStatus.Cancelled);

                // Load reorder suggestions
                await LoadReorderSuggestionsAsync().ConfigureAwait(true);

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }, "Loading purchase orders...").ConfigureAwait(true);
    }

    private void ApplyFilter()
    {
        var filtered = _allPurchaseOrders.AsEnumerable();

        // Apply status filter
        if (SelectedStatusFilter.HasValue)
        {
            filtered = filtered.Where(po => po.Status == SelectedStatusFilter.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(po =>
                po.PONumber.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                po.Supplier.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                po.Supplier.Code.Contains(searchLower, StringComparison.OrdinalIgnoreCase));
        }

        PurchaseOrders = new ObservableCollection<PurchaseOrder>(filtered);
    }

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    [RelayCommand]
    private async Task CreatePurchaseOrderAsync()
    {
        // Show supplier selection dialog or navigate to PO editor
        if (Suppliers.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "No suppliers available. Please create a supplier first.").ConfigureAwait(true);
            return;
        }

        // For now, navigate to a simplified creation dialog
        // In a full implementation, you'd have a PurchaseOrderEditorDialog
        var supplierCode = await _dialogService.ShowInputAsync("New Purchase Order", "Enter supplier code:").ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            return;
        }

        var supplier = await _supplierService.GetSupplierByCodeAsync(supplierCode.ToUpperInvariant()).ConfigureAwait(true);

        if (supplier is null)
        {
            await _dialogService.ShowErrorAsync("Error", $"Supplier with code '{supplierCode}' not found.").ConfigureAwait(true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var purchaseOrder = new PurchaseOrder
            {
                SupplierId = supplier.Id,
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Draft
            };

            var currentUserId = _sessionService.CurrentUserId;

            await _purchaseOrderService.CreatePurchaseOrderAsync(purchaseOrder, currentUserId).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {purchaseOrder.PONumber} created for {supplier.Name}.").ConfigureAwait(true);
        }, "Creating purchase order...").ConfigureAwait(true);
    }

    /// <summary>
    /// Views/edits the selected purchase order.
    /// </summary>
    [RelayCommand]
    private async Task ViewPurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        // In a full implementation, navigate to PO detail view
        var details = $"PO Number: {SelectedPurchaseOrder.PONumber}\n" +
                      $"Supplier: {SelectedPurchaseOrder.Supplier.Name}\n" +
                      $"Order Date: {SelectedPurchaseOrder.OrderDate:yyyy-MM-dd}\n" +
                      $"Status: {SelectedPurchaseOrder.Status}\n" +
                      $"Items: {SelectedPurchaseOrder.PurchaseOrderItems.Count}\n" +
                      $"SubTotal: KSh {SelectedPurchaseOrder.SubTotal:N2}\n" +
                      $"Tax: KSh {SelectedPurchaseOrder.TaxAmount:N2}\n" +
                      $"Total: KSh {SelectedPurchaseOrder.TotalAmount:N2}";

        await _dialogService.ShowMessageAsync("Purchase Order Details", details).ConfigureAwait(true);
    }

    /// <summary>
    /// Sends the selected purchase order to the supplier.
    /// </summary>
    [RelayCommand]
    private async Task SendToSupplierAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        if (SelectedPurchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            await _dialogService.ShowErrorAsync("Error", "Only draft purchase orders can be sent to suppliers.").ConfigureAwait(true);
            return;
        }

        if (SelectedPurchaseOrder.PurchaseOrderItems.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "Cannot send a purchase order with no items.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Send to Supplier",
            $"Are you sure you want to send PO {SelectedPurchaseOrder.PONumber} to {SelectedPurchaseOrder.Supplier.Name}?\n\nThis action cannot be undone.").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _purchaseOrderService.SendToSupplierAsync(SelectedPurchaseOrder.Id).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {SelectedPurchaseOrder.PONumber} has been sent to the supplier.").ConfigureAwait(true);
        }, "Sending to supplier...").ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels the selected purchase order.
    /// </summary>
    [RelayCommand]
    private async Task CancelPurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        if (SelectedPurchaseOrder.Status == PurchaseOrderStatus.Complete)
        {
            await _dialogService.ShowErrorAsync("Error", "Cannot cancel a completed purchase order.").ConfigureAwait(true);
            return;
        }

        if (SelectedPurchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            await _dialogService.ShowErrorAsync("Error", "Purchase order is already cancelled.").ConfigureAwait(true);
            return;
        }

        var reason = await _dialogService.ShowInputAsync(
            "Cancel Purchase Order",
            $"Enter cancellation reason for PO {SelectedPurchaseOrder.PONumber}:").ConfigureAwait(true);

        if (reason is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _purchaseOrderService.CancelPurchaseOrderAsync(SelectedPurchaseOrder.Id, reason).ConfigureAwait(true);
            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success", $"Purchase order {SelectedPurchaseOrder.PONumber} has been cancelled.").ConfigureAwait(true);
        }, "Cancelling purchase order...").ConfigureAwait(true);
    }

    /// <summary>
    /// Duplicates the selected purchase order.
    /// </summary>
    [RelayCommand]
    private async Task DuplicatePurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Duplicate Purchase Order",
            $"Create a new draft PO with the same items as {SelectedPurchaseOrder.PONumber}?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var currentUserId = _sessionService.CurrentUserId;
            var newPO = await _purchaseOrderService.DuplicatePurchaseOrderAsync(SelectedPurchaseOrder.Id, currentUserId).ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success",
                $"Created duplicate purchase order {newPO.PONumber} from {SelectedPurchaseOrder.PONumber}.").ConfigureAwait(true);
        }, "Duplicating purchase order...").ConfigureAwait(true);
    }

    /// <summary>
    /// Emails the purchase order to the supplier.
    /// </summary>
    [RelayCommand]
    private async Task EmailPurchaseOrderAsync()
    {
        if (SelectedPurchaseOrder is null)
        {
            return;
        }

        // Check if supplier has an email
        if (string.IsNullOrEmpty(SelectedPurchaseOrder.Supplier?.Email))
        {
            await _dialogService.ShowErrorAsync("No Email",
                $"Supplier {SelectedPurchaseOrder.Supplier?.Name} does not have an email address configured.").ConfigureAwait(true);
            return;
        }

        // Check if email is configured
        var isConfigured = await _emailService.IsConfiguredAsync().ConfigureAwait(true);
        if (!isConfigured)
        {
            await _dialogService.ShowErrorAsync("Email Not Configured",
                "Email service is not configured. Please configure SMTP settings first.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Email Purchase Order",
            $"Send PO {SelectedPurchaseOrder.PONumber} to {SelectedPurchaseOrder.Supplier.Email}?").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Get business info for the email header
            var config = await _configurationService.GetConfigurationAsync().ConfigureAwait(true);
            if (config is null)
            {
                await _dialogService.ShowErrorAsync("Configuration Error",
                    "System configuration not found. Please configure business settings first.").ConfigureAwait(true);
                return;
            }

            // Generate email content
            var htmlContent = GeneratePurchaseOrderEmailHtml(SelectedPurchaseOrder, config);
            var plainTextContent = GeneratePurchaseOrderEmailPlainText(SelectedPurchaseOrder, config);

            var message = new EmailMessageDto
            {
                ToAddresses = [SelectedPurchaseOrder.Supplier.Email],
                Subject = $"Purchase Order {SelectedPurchaseOrder.PONumber} from {config.BusinessName}",
                HtmlBody = htmlContent,
                PlainTextBody = plainTextContent,
                ReportType = EmailReportType.Custom
            };

            var result = await _emailService.SendEmailAsync(message).ConfigureAwait(true);

            if (result.Success)
            {
                await _dialogService.ShowMessageAsync("Success",
                    $"Purchase order emailed to {SelectedPurchaseOrder.Supplier.Email}.").ConfigureAwait(true);

                // Update PO notes to record the email was sent
                SelectedPurchaseOrder.Notes = string.IsNullOrEmpty(SelectedPurchaseOrder.Notes)
                    ? $"Emailed to supplier on {DateTime.Now:yyyy-MM-dd HH:mm}"
                    : $"{SelectedPurchaseOrder.Notes}\nEmailed to supplier on {DateTime.Now:yyyy-MM-dd HH:mm}";

                await _purchaseOrderService.UpdatePurchaseOrderAsync(SelectedPurchaseOrder).ConfigureAwait(true);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Email Failed",
                    $"Failed to send email: {result.ErrorMessage}").ConfigureAwait(true);
            }
        }, "Sending email...").ConfigureAwait(true);
    }

    private static string GeneratePurchaseOrderEmailHtml(PurchaseOrder po, SystemConfiguration config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
        sb.AppendLine(".header { background-color: #2196F3; color: white; padding: 20px; text-align: center; }");
        sb.AppendLine(".content { padding: 20px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
        sb.AppendLine("th { background-color: #f5f5f5; }");
        sb.AppendLine(".total { font-weight: bold; font-size: 1.1em; }");
        sb.AppendLine(".footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 0.9em; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>Purchase Order</h1>");
        sb.AppendLine($"<h2>{po.PONumber}</h2>");
        sb.AppendLine("</div>");

        // Content
        sb.AppendLine("<div class='content'>");

        // Business info
        sb.AppendLine($"<p><strong>From:</strong> {config.BusinessName}</p>");
        if (!string.IsNullOrEmpty(config.BusinessAddress))
            sb.AppendLine($"<p>{config.BusinessAddress}</p>");
        if (!string.IsNullOrEmpty(config.BusinessPhone))
            sb.AppendLine($"<p>Phone: {config.BusinessPhone}</p>");
        if (!string.IsNullOrEmpty(config.BusinessEmail))
            sb.AppendLine($"<p>Email: {config.BusinessEmail}</p>");

        sb.AppendLine("<hr/>");

        // PO Details
        sb.AppendLine($"<p><strong>Order Date:</strong> {po.OrderDate:yyyy-MM-dd}</p>");
        if (po.ExpectedDate.HasValue)
            sb.AppendLine($"<p><strong>Expected Delivery:</strong> {po.ExpectedDate.Value:yyyy-MM-dd}</p>");
        sb.AppendLine($"<p><strong>Status:</strong> {po.Status}</p>");

        // Items table
        sb.AppendLine("<h3>Order Items</h3>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>#</th><th>Product</th><th>Quantity</th><th>Unit Cost</th><th>Total</th></tr>");

        var itemNum = 1;
        foreach (var item in po.PurchaseOrderItems)
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{itemNum}</td>");
            sb.AppendLine($"<td>{item.Product?.Name ?? "Unknown"}</td>");
            sb.AppendLine($"<td>{item.OrderedQuantity}</td>");
            sb.AppendLine($"<td>{config.CurrencySymbol} {item.UnitCost:N2}</td>");
            sb.AppendLine($"<td>{config.CurrencySymbol} {item.TotalCost:N2}</td>");
            sb.AppendLine("</tr>");
            itemNum++;
        }

        sb.AppendLine("</table>");

        // Totals
        sb.AppendLine("<table style='width: 300px; margin-left: auto;'>");
        sb.AppendLine($"<tr><td>Subtotal:</td><td style='text-align: right;'>{config.CurrencySymbol} {po.SubTotal:N2}</td></tr>");
        sb.AppendLine($"<tr><td>Tax ({config.DefaultTaxRate}%):</td><td style='text-align: right;'>{config.CurrencySymbol} {po.TaxAmount:N2}</td></tr>");
        sb.AppendLine($"<tr class='total'><td>Total:</td><td style='text-align: right;'>{config.CurrencySymbol} {po.TotalAmount:N2}</td></tr>");
        sb.AppendLine("</table>");

        // Notes
        if (!string.IsNullOrEmpty(po.Notes))
        {
            sb.AppendLine($"<h3>Notes</h3>");
            sb.AppendLine($"<p>{po.Notes}</p>");
        }

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>This purchase order was generated by {config.BusinessName}.</p>");
        sb.AppendLine($"<p>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string GeneratePurchaseOrderEmailPlainText(PurchaseOrder po, SystemConfiguration config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("PURCHASE ORDER");
        sb.AppendLine($"PO Number: {po.PONumber}");
        sb.AppendLine(new string('-', 50));

        sb.AppendLine($"\nFrom: {config.BusinessName}");
        if (!string.IsNullOrEmpty(config.BusinessAddress))
            sb.AppendLine(config.BusinessAddress);
        if (!string.IsNullOrEmpty(config.BusinessPhone))
            sb.AppendLine($"Phone: {config.BusinessPhone}");
        if (!string.IsNullOrEmpty(config.BusinessEmail))
            sb.AppendLine($"Email: {config.BusinessEmail}");

        sb.AppendLine(new string('-', 50));

        sb.AppendLine($"\nOrder Date: {po.OrderDate:yyyy-MM-dd}");
        if (po.ExpectedDate.HasValue)
            sb.AppendLine($"Expected Delivery: {po.ExpectedDate.Value:yyyy-MM-dd}");
        sb.AppendLine($"Status: {po.Status}");

        sb.AppendLine("\nORDER ITEMS:");
        sb.AppendLine(new string('-', 50));

        var itemNum = 1;
        foreach (var item in po.PurchaseOrderItems)
        {
            sb.AppendLine($"{itemNum}. {item.Product?.Name ?? "Unknown"}");
            sb.AppendLine($"   Qty: {item.OrderedQuantity} @ {config.CurrencySymbol} {item.UnitCost:N2} = {config.CurrencySymbol} {item.TotalCost:N2}");
            itemNum++;
        }

        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"Subtotal: {config.CurrencySymbol} {po.SubTotal:N2}");
        sb.AppendLine($"Tax ({config.DefaultTaxRate}%): {config.CurrencySymbol} {po.TaxAmount:N2}");
        sb.AppendLine($"TOTAL: {config.CurrencySymbol} {po.TotalAmount:N2}");

        if (!string.IsNullOrEmpty(po.Notes))
        {
            sb.AppendLine($"\nNotes:\n{po.Notes}");
        }

        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");

        return sb.ToString();
    }

    /// <summary>
    /// Clears the supplier filter.
    /// </summary>
    [RelayCommand]
    private async Task ClearSupplierFilterAsync()
    {
        FilterSupplierId = null;
        FilterSupplier = null;
        await LoadDataAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Gets the status display color for a purchase order status.
    /// </summary>
    public static string GetStatusColor(PurchaseOrderStatus status)
    {
        return status switch
        {
            PurchaseOrderStatus.Draft => "#6E6E8E",
            PurchaseOrderStatus.Sent => "#2196F3",
            PurchaseOrderStatus.PartiallyReceived => "#FF9800",
            PurchaseOrderStatus.Complete => "#4CAF50",
            PurchaseOrderStatus.Cancelled => "#F44336",
            _ => "#6E6E8E"
        };
    }

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public static string GetStatusDisplay(PurchaseOrderStatus? status)
    {
        if (!status.HasValue)
        {
            return "All Statuses";
        }

        return status.Value switch
        {
            PurchaseOrderStatus.Draft => "Draft",
            PurchaseOrderStatus.Sent => "Sent",
            PurchaseOrderStatus.PartiallyReceived => "Partially Received",
            PurchaseOrderStatus.Complete => "Complete",
            PurchaseOrderStatus.Cancelled => "Cancelled",
            _ => status.Value.ToString()
        };
    }

    #region Reorder Suggestions

    /// <summary>
    /// Loads reorder suggestions from analytics service.
    /// </summary>
    private async Task LoadReorderSuggestionsAsync()
    {
        try
        {
            var storeId = _sessionService.CurrentStoreId;
            var suggestions = await _inventoryAnalyticsService.GetPendingReorderSuggestionsAsync(storeId).ConfigureAwait(true);
            ReorderSuggestions = new ObservableCollection<ReorderSuggestion>(suggestions);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load reorder suggestions");
            ReorderSuggestions = [];
        }
    }

    /// <summary>
    /// Toggles the suggestions panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSuggestionsPanel()
    {
        IsSuggestionsPanelExpanded = !IsSuggestionsPanelExpanded;
    }

    /// <summary>
    /// Creates a purchase order from selected suggestions.
    /// </summary>
    [RelayCommand]
    private async Task CreatePoFromSuggestionsAsync()
    {
        var pendingSuggestions = ReorderSuggestions.Where(s => s.Status == "Pending").ToList();
        if (pendingSuggestions.Count == 0)
        {
            await _dialogService.ShowMessageAsync("No Suggestions", "There are no pending suggestions to convert to purchase orders.").ConfigureAwait(true);
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Create Purchase Orders",
            $"Create purchase orders from {pendingSuggestions.Count} reorder suggestion(s)?\n\nSuggestions will be grouped by supplier.").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var storeId = _sessionService.CurrentStoreId;
            var suggestionIds = pendingSuggestions.Select(s => s.Id).ToList();
            var result = await _inventoryAnalyticsService.ConvertSuggestionsToPurchaseOrdersAsync(storeId, suggestionIds).ConfigureAwait(true);

            await LoadDataAsync().ConfigureAwait(true);

            await _dialogService.ShowMessageAsync("Success",
                $"Created {result.PurchaseOrdersCreated} purchase order(s) from {result.SuggestionsProcessed} suggestion(s).").ConfigureAwait(true);
        }, "Creating purchase orders from suggestions...").ConfigureAwait(true);
    }

    /// <summary>
    /// Dismisses a single reorder suggestion.
    /// </summary>
    [RelayCommand]
    private async Task DismissSuggestionAsync(ReorderSuggestion suggestion)
    {
        if (suggestion is null)
        {
            return;
        }

        var reason = await _dialogService.ShowInputAsync(
            "Dismiss Suggestion",
            $"Enter reason for dismissing the reorder suggestion for {suggestion.Product?.Name}:").ConfigureAwait(true);

        if (reason is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _inventoryAnalyticsService.RejectReorderSuggestionAsync(suggestion.Id, reason).ConfigureAwait(true);
            await LoadReorderSuggestionsAsync().ConfigureAwait(true);
        }, "Dismissing suggestion...").ConfigureAwait(true);
    }

    /// <summary>
    /// Refreshes reorder suggestions by regenerating them.
    /// </summary>
    [RelayCommand]
    private async Task RefreshSuggestionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var storeId = _sessionService.CurrentStoreId;
            // Generate suggestions from explicit reorder rules
            await _inventoryAnalyticsService.GenerateReorderSuggestionsAsync(storeId).ConfigureAwait(true);
            // Also generate suggestions from low stock products without rules
            await _inventoryAnalyticsService.GenerateLowStockSuggestionsAsync(storeId).ConfigureAwait(true);
            await LoadReorderSuggestionsAsync().ConfigureAwait(true);
        }, "Refreshing reorder suggestions...").ConfigureAwait(true);
    }

    #endregion
}
