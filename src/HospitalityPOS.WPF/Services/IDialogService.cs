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
