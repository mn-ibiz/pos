namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a suspended transaction.
/// </summary>
public enum SuspendedTransactionStatus
{
    /// <summary>Transaction is parked and active.</summary>
    Parked = 1,
    /// <summary>Transaction has been recalled and completed.</summary>
    Recalled = 2,
    /// <summary>Transaction was voided/cancelled.</summary>
    Voided = 3,
    /// <summary>Transaction expired without being recalled.</summary>
    Expired = 4
}

/// <summary>
/// Suspended (parked) transaction for recall later.
/// </summary>
public class SuspendedTransaction : BaseEntity
{
    /// <summary>
    /// Store where transaction was parked.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Terminal/register ID.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// User who parked the transaction.
    /// </summary>
    public int ParkedByUserId { get; set; }

    /// <summary>
    /// Unique reference number for recall.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer name/identifier for the parked transaction.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Table number (for restaurant).
    /// </summary>
    public string? TableNumber { get; set; }

    /// <summary>
    /// Order type (Dine-In, Takeaway, Delivery).
    /// </summary>
    public string? OrderType { get; set; }

    /// <summary>
    /// Notes about the parked transaction.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Subtotal before tax.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of items in the transaction.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Status of the suspended transaction.
    /// </summary>
    public SuspendedTransactionStatus Status { get; set; } = SuspendedTransactionStatus.Parked;

    /// <summary>
    /// When the transaction was parked.
    /// </summary>
    public DateTime ParkedAt { get; set; }

    /// <summary>
    /// When the transaction was recalled.
    /// </summary>
    public DateTime? RecalledAt { get; set; }

    /// <summary>
    /// User who recalled the transaction.
    /// </summary>
    public int? RecalledByUserId { get; set; }

    /// <summary>
    /// Receipt ID if transaction was completed.
    /// </summary>
    public int? CompletedReceiptId { get; set; }

    /// <summary>
    /// Expiration time for automatic cleanup.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this suspended transaction is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Applied loyalty member ID.
    /// </summary>
    public int? LoyaltyMemberId { get; set; }

    /// <summary>
    /// Applied promotion IDs (JSON array).
    /// </summary>
    public string? AppliedPromotionIds { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual User ParkedByUser { get; set; } = null!;
    public virtual User? RecalledByUser { get; set; }
    public virtual Receipt? CompletedReceipt { get; set; }
    public virtual LoyaltyMember? LoyaltyMember { get; set; }
    public virtual ICollection<SuspendedTransactionItem> Items { get; set; } = new List<SuspendedTransactionItem>();
}

/// <summary>
/// Item in a suspended transaction.
/// </summary>
public class SuspendedTransactionItem : BaseEntity
{
    /// <summary>
    /// Reference to suspended transaction.
    /// </summary>
    public int SuspendedTransactionId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product name (snapshot).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product code (snapshot).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Line total.
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Item notes/modifications.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Modifiers JSON.
    /// </summary>
    public string? ModifiersJson { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual SuspendedTransaction SuspendedTransaction { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Customer-facing display configuration.
/// </summary>
public class CustomerDisplayConfig : BaseEntity
{
    /// <summary>
    /// Store this configuration applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Terminal/register ID.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// Configuration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display type (Secondary Monitor, Tablet, Web).
    /// </summary>
    public string DisplayType { get; set; } = "SecondaryMonitor";

    /// <summary>
    /// Whether display is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Screen resolution width.
    /// </summary>
    public int ScreenWidth { get; set; } = 1920;

    /// <summary>
    /// Screen resolution height.
    /// </summary>
    public int ScreenHeight { get; set; } = 1080;

    /// <summary>
    /// Background color (hex).
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Primary text color (hex).
    /// </summary>
    public string PrimaryTextColor { get; set; } = "#000000";

    /// <summary>
    /// Accent color (hex).
    /// </summary>
    public string AccentColor { get; set; } = "#007BFF";

    /// <summary>
    /// Font family.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Header font size.
    /// </summary>
    public int HeaderFontSize { get; set; } = 36;

    /// <summary>
    /// Item font size.
    /// </summary>
    public int ItemFontSize { get; set; } = 24;

    /// <summary>
    /// Total font size.
    /// </summary>
    public int TotalFontSize { get; set; } = 48;

    /// <summary>
    /// Whether to show item images.
    /// </summary>
    public bool ShowItemImages { get; set; } = true;

    /// <summary>
    /// Whether to show promotional messages.
    /// </summary>
    public bool ShowPromotionalMessages { get; set; } = true;

    /// <summary>
    /// Promotional messages rotation interval (seconds).
    /// </summary>
    public int PromotionalRotationSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to show store logo.
    /// </summary>
    public bool ShowStoreLogo { get; set; } = true;

    /// <summary>
    /// Store logo URL.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Welcome message.
    /// </summary>
    public string? WelcomeMessage { get; set; }

    /// <summary>
    /// Thank you message.
    /// </summary>
    public string? ThankYouMessage { get; set; }

    /// <summary>
    /// Idle screen type (Logo, Slideshow, Promotions).
    /// </summary>
    public string IdleScreenType { get; set; } = "Logo";

    /// <summary>
    /// Idle timeout seconds before showing idle screen.
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to show currency symbol.
    /// </summary>
    public bool ShowCurrencySymbol { get; set; } = true;

    /// <summary>
    /// Currency symbol.
    /// </summary>
    public string CurrencySymbol { get; set; } = "KES";

    /// <summary>
    /// Layout template name.
    /// </summary>
    public string LayoutTemplate { get; set; } = "Standard";

    /// <summary>
    /// Whether this configuration is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<CustomerDisplayMessage> Messages { get; set; } = new List<CustomerDisplayMessage>();
}

/// <summary>
/// Promotional message for customer display.
/// </summary>
public class CustomerDisplayMessage : BaseEntity
{
    /// <summary>
    /// Reference to display configuration.
    /// </summary>
    public int DisplayConfigId { get; set; }

    /// <summary>
    /// Message title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether message is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Start date for display.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for display.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this message is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual CustomerDisplayConfig DisplayConfig { get; set; } = null!;
}

/// <summary>
/// Split payment method type.
/// </summary>
public enum SplitPaymentMethodType
{
    /// <summary>Split by equal amounts.</summary>
    EqualSplit = 1,
    /// <summary>Split by custom amounts.</summary>
    CustomAmount = 2,
    /// <summary>Split by items.</summary>
    ByItem = 3,
    /// <summary>Split by percentage.</summary>
    ByPercentage = 4
}

/// <summary>
/// Split payment configuration.
/// </summary>
public class SplitPaymentConfig : BaseEntity
{
    /// <summary>
    /// Store this configuration applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Maximum number of split ways allowed.
    /// </summary>
    public int MaxSplitWays { get; set; } = 10;

    /// <summary>
    /// Minimum amount per split.
    /// </summary>
    public decimal MinSplitAmount { get; set; } = 1;

    /// <summary>
    /// Whether to allow mixing payment methods.
    /// </summary>
    public bool AllowMixedPaymentMethods { get; set; } = true;

    /// <summary>
    /// Whether to allow by-item split.
    /// </summary>
    public bool AllowItemSplit { get; set; } = true;

    /// <summary>
    /// Whether to allow equal split.
    /// </summary>
    public bool AllowEqualSplit { get; set; } = true;

    /// <summary>
    /// Whether to allow custom amount split.
    /// </summary>
    public bool AllowCustomAmountSplit { get; set; } = true;

    /// <summary>
    /// Whether to allow percentage split.
    /// </summary>
    public bool AllowPercentageSplit { get; set; } = true;

    /// <summary>
    /// Default split method.
    /// </summary>
    public SplitPaymentMethodType DefaultSplitMethod { get; set; } = SplitPaymentMethodType.EqualSplit;

    /// <summary>
    /// Whether to require all parties present.
    /// </summary>
    public bool RequireAllPartiesPresent { get; set; }

    /// <summary>
    /// Whether to print separate receipts.
    /// </summary>
    public bool PrintSeparateReceipts { get; set; } = true;

    /// <summary>
    /// Whether this configuration is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}

/// <summary>
/// Split payment session for a transaction.
/// </summary>
public class SplitPaymentSession : BaseEntity
{
    /// <summary>
    /// Original receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Split method used.
    /// </summary>
    public SplitPaymentMethodType SplitMethod { get; set; }

    /// <summary>
    /// Number of split ways.
    /// </summary>
    public int NumberOfSplits { get; set; }

    /// <summary>
    /// Total transaction amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid so far.
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Remaining amount.
    /// </summary>
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    /// <summary>
    /// Whether split is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// User who initiated the split.
    /// </summary>
    public int InitiatedByUserId { get; set; }

    /// <summary>
    /// Session start time.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Session completion time.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether this session is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual User InitiatedByUser { get; set; } = null!;
    public virtual ICollection<SplitPaymentPart> Parts { get; set; } = new List<SplitPaymentPart>();
}

/// <summary>
/// Individual payment part in a split payment.
/// </summary>
public class SplitPaymentPart : BaseEntity
{
    /// <summary>
    /// Reference to split session.
    /// </summary>
    public int SplitSessionId { get; set; }

    /// <summary>
    /// Part number (1, 2, 3, etc.).
    /// </summary>
    public int PartNumber { get; set; }

    /// <summary>
    /// Payer name/identifier.
    /// </summary>
    public string? PayerName { get; set; }

    /// <summary>
    /// Amount for this part.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this part is paid.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payment reference number.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// When payment was made.
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Items included in this part (JSON array of item IDs for by-item split).
    /// </summary>
    public string? IncludedItemIds { get; set; }

    /// <summary>
    /// Percentage of total (for percentage split).
    /// </summary>
    public decimal? Percentage { get; set; }

    // Navigation properties
    public virtual SplitPaymentSession SplitSession { get; set; } = null!;
}

/// <summary>
/// Quick amount button configuration.
/// </summary>
public class QuickAmountButton : BaseEntity
{
    /// <summary>
    /// Store this button applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Button label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Amount value.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Button type (Fixed, RoundUp, Custom).
    /// </summary>
    public string ButtonType { get; set; } = "Fixed";

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether button is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Button color (hex).
    /// </summary>
    public string? ButtonColor { get; set; }

    /// <summary>
    /// Text color (hex).
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Keyboard shortcut (e.g., F1, F2).
    /// </summary>
    public string? KeyboardShortcut { get; set; }

    /// <summary>
    /// Payment method this applies to (null for all).
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Whether this button is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}

/// <summary>
/// Quick amount button set for organized grouping.
/// </summary>
public class QuickAmountButtonSet : BaseEntity
{
    /// <summary>
    /// Store this set applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Set name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is the active set.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Payment method this set applies to.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Button IDs in this set (JSON array).
    /// </summary>
    public string? ButtonIds { get; set; }

    /// <summary>
    /// Whether this button set is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}
