namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service interface for displaying dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an informational message dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>True if the user clicked Yes, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    Task ShowErrorAsync(string message);

    /// <summary>
    /// Shows an error dialog with a title.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message to display.</param>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an input dialog to collect text from the user.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="defaultValue">The default value in the input field.</param>
    /// <returns>The entered text, or null if cancelled.</returns>
    Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "");

    /// <summary>
    /// Shows a PIN entry dialog for numeric input.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="maxLength">Maximum number of digits.</param>
    /// <returns>The entered PIN, or null if cancelled.</returns>
    Task<string?> ShowPinEntryAsync(string title, string prompt, int maxLength = 4);

    /// <summary>
    /// Shows a warning dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The warning message to display.</param>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with custom button text.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="confirmText">Text for the confirm button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <returns>True if confirmed, false if cancelled.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText);

    /// <summary>
    /// Shows an authorization override dialog for entering a manager PIN.
    /// </summary>
    /// <param name="actionDescription">Description of the action requiring authorization.</param>
    /// <param name="permissionRequired">The permission name that is required.</param>
    /// <returns>The entered PIN if authorized, null if cancelled.</returns>
    Task<string?> ShowAuthorizationOverrideAsync(string actionDescription, string permissionRequired);

    /// <summary>
    /// Shows the Open Work Period dialog for entering an opening float.
    /// </summary>
    /// <param name="previousClosingBalance">Optional previous period's closing balance.</param>
    /// <returns>The opening float amount if confirmed, null if cancelled.</returns>
    Task<decimal?> ShowOpenWorkPeriodDialogAsync(decimal? previousClosingBalance = null);

    /// <summary>
    /// Shows the X-Report dialog.
    /// </summary>
    /// <param name="report">The X-Report to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowXReportDialogAsync(HospitalityPOS.Core.Models.Reports.XReport report);

    /// <summary>
    /// Shows the X-Report dialog for the new XReportData DTO.
    /// </summary>
    /// <param name="report">The X-Report data to display.</param>
    /// <param name="autoPrint">If true, automatically prints the report when shown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowXReportDialogAsync(HospitalityPOS.Core.DTOs.XReportData report, bool autoPrint = false);

    /// <summary>
    /// Shows the Combined X-Report dialog with data from all terminals.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowCombinedXReportDialogAsync();

    /// <summary>
    /// Shows the Combined Z-Report preview dialog with data from all terminals.
    /// </summary>
    /// <param name="workPeriodId">Optional work period ID. If not specified, uses current work period.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowCombinedZReportDialogAsync(int? workPeriodId = null);

    /// <summary>
    /// Shows the Z-Report dialog.
    /// </summary>
    /// <param name="report">The Z-Report to display.</param>
    /// <param name="autoPrint">If true, automatically prints the report when shown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowZReportDialogAsync(HospitalityPOS.Core.Models.Reports.ZReport report, bool autoPrint = false);

    /// <summary>
    /// Shows the Close Work Period dialog.
    /// </summary>
    /// <param name="expectedCash">The expected cash amount.</param>
    /// <param name="openingFloat">The opening float amount.</param>
    /// <param name="cashSales">The total cash sales.</param>
    /// <param name="cashPayouts">The total cash payouts.</param>
    /// <param name="unsettledReceipts">Optional list of unsettled receipts.</param>
    /// <returns>A tuple of (closing cash, notes) if confirmed, null if cancelled.</returns>
    Task<(decimal ClosingCash, string? Notes)?> ShowCloseWorkPeriodDialogAsync(
        decimal expectedCash,
        decimal openingFloat,
        decimal cashSales,
        decimal cashPayouts,
        IReadOnlyList<HospitalityPOS.Core.Entities.Receipt>? unsettledReceipts = null);

    /// <summary>
    /// Shows the Category Editor dialog for creating or editing a category.
    /// </summary>
    /// <param name="existingCategory">The category to edit, or null for creating a new category.</param>
    /// <param name="defaultParentId">Optional default parent category ID (used when creating subcategory).</param>
    /// <returns>The category editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.CategoryEditorResult?> ShowCategoryEditorDialogAsync(
        HospitalityPOS.Core.Entities.Category? existingCategory,
        int? defaultParentId = null);

    /// <summary>
    /// Shows the Product Editor dialog for creating or editing a product.
    /// </summary>
    /// <param name="existingProduct">The product to edit, or null for creating a new product.</param>
    /// <param name="defaultCategoryId">Optional default category ID (used when creating in specific category).</param>
    /// <returns>The product editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.ProductEditorResult?> ShowProductEditorDialogAsync(
        HospitalityPOS.Core.Entities.Product? existingProduct,
        int? defaultCategoryId = null);

    /// <summary>
    /// Shows an ownership override dialog for accessing another user's receipt.
    /// </summary>
    /// <param name="ownerName">Name of the receipt owner.</param>
    /// <param name="actionDescription">Description of the action being requested.</param>
    /// <returns>A tuple of (PIN, Reason) if authorized, null if cancelled.</returns>
    Task<(string Pin, string Reason)?> ShowOwnershipOverrideDialogAsync(string ownerName, string actionDescription);

    /// <summary>
    /// Shows the split bill dialog.
    /// </summary>
    /// <param name="receipt">The receipt to split.</param>
    /// <returns>The split bill result if confirmed, null if cancelled.</returns>
    Task<SplitBillDialogResult?> ShowSplitBillDialogAsync(HospitalityPOS.Core.Entities.Receipt receipt);

    /// <summary>
    /// Shows the merge bill dialog.
    /// </summary>
    /// <param name="receipts">The available receipts to merge.</param>
    /// <returns>The list of selected receipt IDs if confirmed, null if cancelled.</returns>
    Task<List<int>?> ShowMergeBillDialogAsync(IEnumerable<HospitalityPOS.Core.Entities.Receipt> receipts);

    /// <summary>
    /// Shows the void receipt dialog.
    /// </summary>
    /// <param name="receipt">The receipt to void.</param>
    /// <param name="voidReasons">Available void reasons.</param>
    /// <returns>The void dialog result if confirmed, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.VoidReceiptDialogResult?> ShowVoidReceiptDialogAsync(
        HospitalityPOS.Core.Entities.Receipt receipt,
        IReadOnlyList<HospitalityPOS.Core.Entities.VoidReason> voidReasons);

    /// <summary>
    /// Shows the Payment Method Editor dialog for creating or editing a payment method.
    /// </summary>
    /// <param name="existingMethod">The payment method to edit, or null for creating a new one.</param>
    /// <returns>The payment method editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.PaymentMethodEditorResult?> ShowPaymentMethodEditorDialogAsync(
        HospitalityPOS.Core.Entities.PaymentMethod? existingMethod);

    /// <summary>
    /// Shows the Supplier Editor dialog for creating or editing a supplier.
    /// </summary>
    /// <param name="existingSupplier">The supplier to edit, or null for creating a new supplier.</param>
    /// <returns>The supplier editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.SupplierEditorResult?> ShowSupplierEditorDialogAsync(
        HospitalityPOS.Core.Entities.Supplier? existingSupplier);

    /// <summary>
    /// Shows an info dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The info message to display.</param>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows an info dialog with default title.
    /// </summary>
    /// <param name="message">The info message to display.</param>
    Task ShowInfoAsync(string message);

    /// <summary>
    /// Shows a success dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The success message to display.</param>
    Task ShowSuccessAsync(string title, string message);

    /// <summary>
    /// Shows a success dialog with default title.
    /// </summary>
    /// <param name="message">The success message to display.</param>
    Task ShowSuccessAsync(string message);

    /// <summary>
    /// Shows an action sheet with multiple options.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <param name="destructiveText">Optional text for a destructive action.</param>
    /// <param name="options">Array of option strings.</param>
    /// <returns>The selected option, or null if cancelled.</returns>
    Task<string?> ShowActionSheetAsync(string title, string? message, string cancelText, string? destructiveText, params string[] options);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options (alias for ShowConfirmationAsync).
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>True if the user clicked Yes, false otherwise.</returns>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// Shows the Offer Editor dialog for creating or editing a product offer.
    /// </summary>
    /// <param name="existingOffer">The offer to edit, or null for creating a new offer.</param>
    /// <returns>The offer if saved, null if cancelled.</returns>
    Task<HospitalityPOS.Core.Entities.ProductOffer?> ShowOfferEditorDialogAsync(
        HospitalityPOS.Core.Entities.ProductOffer? existingOffer);

    /// <summary>
    /// Shows a date picker dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="defaultDate">The default date.</param>
    /// <returns>The selected date if confirmed, null if cancelled.</returns>
    Task<DateTime?> ShowDatePickerDialogAsync(string title, string prompt, DateTime defaultDate);

    /// <summary>
    /// Shows the Supplier Payment dialog for recording a payment to a supplier.
    /// </summary>
    /// <param name="supplier">The supplier to make payment to.</param>
    /// <param name="invoice">Optional specific invoice to pay.</param>
    /// <returns>The payment if confirmed, null if cancelled.</returns>
    Task<HospitalityPOS.Core.Entities.SupplierPayment?> ShowSupplierPaymentDialogAsync(
        HospitalityPOS.Core.Entities.Supplier supplier,
        HospitalityPOS.Core.Entities.SupplierInvoice? invoice = null);

    /// <summary>
    /// Shows the Supplier Invoice Editor dialog for creating or editing an invoice.
    /// </summary>
    /// <param name="existingInvoice">The invoice to edit, or null for creating a new invoice.</param>
    /// <param name="supplier">The supplier for the invoice.</param>
    /// <returns>The invoice if saved, null if cancelled.</returns>
    Task<HospitalityPOS.Core.Entities.SupplierInvoice?> ShowSupplierInvoiceEditorDialogAsync(
        HospitalityPOS.Core.Entities.SupplierInvoice? existingInvoice,
        HospitalityPOS.Core.Entities.Supplier supplier);

    /// <summary>
    /// Shows the Supplier Statement dialog displaying transaction history and balances.
    /// </summary>
    /// <param name="supplier">The supplier to view statement for.</param>
    Task ShowSupplierStatementDialogAsync(HospitalityPOS.Core.Entities.Supplier supplier);

    /// <summary>
    /// Shows an HTML preview dialog for viewing HTML content.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="subtitle">Optional subtitle.</param>
    /// <param name="htmlContent">HTML content to display.</param>
    /// <param name="exportFilename">Optional filename for PDF export.</param>
    Task ShowHtmlPreviewAsync(string title, string? subtitle, string htmlContent, string? exportFilename = null);

    /// <summary>
    /// Shows the Variant Option Editor dialog for creating or editing a variant option.
    /// </summary>
    /// <param name="existingOption">The variant option to edit, or null for creating a new one.</param>
    /// <returns>The variant option editor result if saved, null if cancelled.</returns>
    Task<VariantOptionEditorResult?> ShowVariantOptionEditorDialogAsync(
        HospitalityPOS.Core.Entities.VariantOption? existingOption);

    /// <summary>
    /// Shows the Variant Value Editor dialog for adding or editing a variant option value.
    /// </summary>
    /// <param name="existingValue">The variant option value to edit, or null for creating a new one.</param>
    /// <param name="parentOption">The parent variant option.</param>
    /// <returns>The variant value editor result if saved, null if cancelled.</returns>
    Task<VariantOptionValueEditorResult?> ShowVariantValueEditorDialogAsync(
        HospitalityPOS.Core.Entities.VariantOptionValue? existingValue,
        HospitalityPOS.Core.Entities.VariantOption parentOption);

    /// <summary>
    /// Shows the Modifier Group Editor dialog for creating or editing a modifier group.
    /// </summary>
    /// <param name="existingGroup">The modifier group to edit, or null for creating a new one.</param>
    /// <returns>The modifier group editor result if saved, null if cancelled.</returns>
    Task<ModifierGroupEditorResult?> ShowModifierGroupEditorDialogAsync(
        HospitalityPOS.Core.Entities.ModifierGroup? existingGroup);

    /// <summary>
    /// Shows the Modifier Item Editor dialog for adding or editing a modifier item.
    /// </summary>
    /// <param name="existingItem">The modifier item to edit, or null for creating a new one.</param>
    /// <param name="parentGroup">The parent modifier group.</param>
    /// <returns>The modifier item editor result if saved, null if cancelled.</returns>
    Task<ModifierItemEditorResult?> ShowModifierItemEditorDialogAsync(
        HospitalityPOS.Core.Entities.ModifierItem? existingItem,
        HospitalityPOS.Core.Entities.ModifierGroup parentGroup);

    /// <summary>
    /// Shows the Variant Selection dialog for POS.
    /// </summary>
    /// <param name="product">The product to select variants for.</param>
    /// <param name="availableOptions">The available variant options.</param>
    /// <param name="productVariants">The product's variant combinations.</param>
    /// <returns>The variant selection result if confirmed, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.VariantSelectionResult?> ShowVariantSelectionDialogAsync(
        HospitalityPOS.Core.Entities.Product product,
        IReadOnlyList<HospitalityPOS.Core.Entities.VariantOption> availableOptions,
        IReadOnlyList<HospitalityPOS.Core.Entities.ProductVariant> productVariants);

    /// <summary>
    /// Shows the Modifier Selection dialog for POS.
    /// </summary>
    /// <param name="product">The product to select modifiers for.</param>
    /// <param name="modifierGroups">The available modifier groups.</param>
    /// <returns>The modifier selection result if confirmed, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.ModifierSelectionResult?> ShowModifierSelectionDialogAsync(
        HospitalityPOS.Core.Entities.Product product,
        IReadOnlyList<HospitalityPOS.Core.Entities.ModifierGroup> modifierGroups);

    /// <summary>
    /// Shows the Expense Editor dialog for creating or editing an expense.
    /// </summary>
    /// <param name="existingExpense">The expense to edit, or null for creating a new expense.</param>
    /// <returns>The expense editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.ExpenseEditorResult?> ShowExpenseEditorDialogAsync(
        HospitalityPOS.Core.Entities.Expense? existingExpense);

    /// <summary>
    /// Shows the Expense Category Editor dialog for creating or editing an expense category.
    /// </summary>
    /// <param name="existingCategory">The category to edit, or null for creating a new category.</param>
    /// <returns>The expense category editor result if saved, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.ExpenseCategoryEditorResult?> ShowExpenseCategoryEditorDialogAsync(
        HospitalityPOS.Core.Entities.ExpenseCategory? existingCategory);

    /// <summary>
    /// Shows the Manual Attendance Entry dialog.
    /// </summary>
    /// <param name="employee">The employee for the manual entry.</param>
    /// <returns>The manual attendance entry result if submitted, null if cancelled.</returns>
    Task<HospitalityPOS.WPF.Views.Dialogs.ManualAttendanceEntryResult?> ShowManualAttendanceEntryDialogAsync(
        HospitalityPOS.Core.Entities.Employee? employee);

    /// <summary>
    /// Shows the Recurring Expense Editor dialog for creating or editing a recurring expense.
    /// </summary>
    /// <param name="existingExpense">The recurring expense to edit, or null for creating a new one.</param>
    /// <returns>The recurring expense if saved, null if cancelled.</returns>
    Task<HospitalityPOS.Core.Entities.RecurringExpense?> ShowRecurringExpenseEditorDialogAsync(
        HospitalityPOS.Core.Entities.RecurringExpense? existingExpense);

    /// <summary>
    /// Shows the Expense Budget Editor dialog for creating or editing an expense budget.
    /// </summary>
    /// <param name="existingBudget">The budget to edit, or null for creating a new one.</param>
    /// <returns>The expense budget if saved, null if cancelled.</returns>
    Task<HospitalityPOS.Core.Entities.ExpenseBudget?> ShowExpenseBudgetEditorDialogAsync(
        HospitalityPOS.Core.Entities.ExpenseBudget? existingBudget);
}

/// <summary>
/// Result from the split bill dialog.
/// </summary>
public class SplitBillDialogResult
{
    /// <summary>
    /// Gets or sets whether this is an equal split.
    /// </summary>
    public bool IsEqualSplit { get; set; }

    /// <summary>
    /// Gets or sets the number of ways to split (for equal split).
    /// </summary>
    public int NumberOfWays { get; set; }

    /// <summary>
    /// Gets or sets the split requests (for item-based split).
    /// </summary>
    public List<HospitalityPOS.Core.Models.SplitItemRequest> SplitRequests { get; set; } = new();
}

/// <summary>
/// Result from the variant option editor dialog.
/// </summary>
public class VariantOptionEditorResult
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public HospitalityPOS.Core.Entities.VariantOptionType OptionType { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsGlobal { get; set; } = true;
    public List<VariantOptionValueEditorResult> Values { get; set; } = new();
}

/// <summary>
/// Result from the variant option value editor dialog.
/// </summary>
public class VariantOptionValueEditorResult
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ColorCode { get; set; }
    public decimal PriceAdjustment { get; set; }
    public bool IsPriceAdjustmentPercent { get; set; }
    public int DisplayOrder { get; set; }
    public string? SkuSuffix { get; set; }
}

/// <summary>
/// Result from the modifier group editor dialog.
/// </summary>
public class ModifierGroupEditorResult
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public HospitalityPOS.Core.Entities.ModifierSelectionType SelectionType { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorCode { get; set; }
    public string? KitchenStation { get; set; }
    public bool PrintOnKOT { get; set; } = true;
    public bool ShowOnReceipt { get; set; } = true;
    public List<ModifierItemEditorResult> Items { get; set; } = new();
}

/// <summary>
/// Result from the modifier item editor dialog.
/// </summary>
public class ModifierItemEditorResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ShortCode { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public int MaxQuantity { get; set; } = 10;
    public int DisplayOrder { get; set; }
    public string? ColorCode { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? KOTText { get; set; }
    public decimal TaxRate { get; set; } = 16.00m;
    public int? InventoryProductId { get; set; }
    public decimal InventoryDeductQuantity { get; set; }
    public string? Allergens { get; set; }
}
