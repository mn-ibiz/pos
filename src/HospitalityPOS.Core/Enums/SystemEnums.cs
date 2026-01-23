namespace HospitalityPOS.Core.Enums;

/// <summary>
/// System operation modes for the POS system.
/// </summary>
public enum SystemMode
{
    /// <summary>
    /// Hospitality mode for hotels, bars, and restaurants.
    /// </summary>
    Hospitality = 1,

    /// <summary>
    /// Retail mode for supermarkets and shops.
    /// </summary>
    Retail = 2,

    /// <summary>
    /// Quick service mode for fast food establishments.
    /// </summary>
    QuickService = 3
}

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is open and items can be added.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Order has been sent to kitchen.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Order is being prepared.
    /// </summary>
    Preparing = 3,

    /// <summary>
    /// Order is ready for service.
    /// </summary>
    Ready = 4,

    /// <summary>
    /// Order has been served.
    /// </summary>
    Served = 5,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Order is on hold.
    /// </summary>
    OnHold = 7
}

/// <summary>
/// Receipt status enumeration.
/// </summary>
public enum ReceiptStatus
{
    /// <summary>
    /// Receipt is open.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Receipt has been created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Receipt is pending payment.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Receipt is fully settled.
    /// </summary>
    Settled = 3,

    /// <summary>
    /// Receipt has been voided.
    /// </summary>
    Voided = 4,

    /// <summary>
    /// Receipt has been split into child receipts.
    /// </summary>
    Split = 5,

    /// <summary>
    /// Receipt has been merged into another receipt.
    /// </summary>
    Merged = 6
}

/// <summary>
/// Bill split type enumeration.
/// </summary>
public enum SplitType
{
    /// <summary>
    /// Split equally among customers.
    /// </summary>
    Equal = 1,

    /// <summary>
    /// Split by selecting specific items.
    /// </summary>
    ByItem = 2
}

/// <summary>
/// Payment method types.
/// </summary>
public enum PaymentMethodType
{
    /// <summary>
    /// Cash payment.
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Credit/Debit card payment.
    /// </summary>
    Card = 2,

    /// <summary>
    /// M-Pesa mobile payment.
    /// </summary>
    MPesa = 3,

    /// <summary>
    /// Bank transfer.
    /// </summary>
    BankTransfer = 4,

    /// <summary>
    /// Credit account payment.
    /// </summary>
    Credit = 5,

    /// <summary>
    /// Loyalty points redemption.
    /// </summary>
    LoyaltyPoints = 6
}

/// <summary>
/// Work period status.
/// </summary>
public enum WorkPeriodStatus
{
    /// <summary>
    /// Work period is currently open.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Work period has been closed.
    /// </summary>
    Closed = 2
}

/// <summary>
/// Stock movement types.
/// </summary>
public enum MovementType
{
    Sale = 1,
    Purchase = 2,
    Adjustment = 3,
    Void = 4,
    StockTake = 5,
    Transfer = 6,
    Return = 7,
    Waste = 8,
    PurchaseReceive = 9
}

/// <summary>
/// Purchase order status.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Complete = 4,
    Cancelled = 5,
    Archived = 6
}

/// <summary>
/// Payment status for purchase orders.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3
}

/// <summary>
/// Supplier invoice status.
/// </summary>
public enum InvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4
}

/// <summary>
/// Employment type for employees.
/// </summary>
public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2,
    Contract = 3
}

/// <summary>
/// Pay frequency for salary.
/// </summary>
public enum PayFrequency
{
    Weekly = 1,
    BiWeekly = 2,
    Monthly = 3
}

/// <summary>
/// Salary component type.
/// </summary>
public enum ComponentType
{
    Earning = 1,
    Deduction = 2
}

/// <summary>
/// Payroll period status.
/// </summary>
public enum PayrollStatus
{
    Draft = 1,
    Processing = 2,
    Approved = 3,
    Paid = 4
}

/// <summary>
/// Payslip payment status.
/// </summary>
public enum PayslipPaymentStatus
{
    Pending = 1,
    Paid = 2
}

/// <summary>
/// Expense status.
/// </summary>
public enum ExpenseStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4
}

/// <summary>
/// Account type for chart of accounts.
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

/// <summary>
/// Accounting period status.
/// </summary>
public enum AccountingPeriodStatus
{
    Open = 1,
    Closed = 2
}

/// <summary>
/// Journal entry status.
/// </summary>
public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}

/// <summary>
/// Business mode for the POS system.
/// </summary>
public enum BusinessMode
{
    Restaurant = 1,
    Supermarket = 2,
    Hybrid = 3
}

/// <summary>
/// eTIMS submission status.
/// </summary>
public enum EtimsStatus
{
    /// <summary>
    /// Pending submission to KRA.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Successfully submitted to KRA.
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Submission failed, retry required.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Queued for offline submission.
    /// </summary>
    Queued = 4
}

/// <summary>
/// Product stock status for display purposes.
/// </summary>
public enum StockStatus
{
    /// <summary>
    /// Product has adequate stock.
    /// </summary>
    InStock = 1,

    /// <summary>
    /// Product stock is below minimum level.
    /// </summary>
    LowStock = 2,

    /// <summary>
    /// Product is out of stock.
    /// </summary>
    OutOfStock = 3
}

/// <summary>
/// Stock take status.
/// </summary>
public enum StockTakeStatus
{
    /// <summary>
    /// Stock take is in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Stock take is pending approval.
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Stock take has been approved and adjustments applied.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Stock take has been cancelled.
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Sales report type enumeration.
/// </summary>
public enum SalesReportType
{
    /// <summary>
    /// Daily sales summary report.
    /// </summary>
    DailySummary = 1,

    /// <summary>
    /// Sales breakdown by product.
    /// </summary>
    ByProduct = 2,

    /// <summary>
    /// Sales breakdown by category.
    /// </summary>
    ByCategory = 3,

    /// <summary>
    /// Sales breakdown by cashier/user.
    /// </summary>
    ByCashier = 4,

    /// <summary>
    /// Sales breakdown by payment method.
    /// </summary>
    ByPaymentMethod = 5,

    /// <summary>
    /// Hourly sales analysis.
    /// </summary>
    HourlySales = 6
}

/// <summary>
/// Audit action types for logging.
/// </summary>
public enum AuditActionType
{
    /// <summary>
    /// User login action.
    /// </summary>
    Login = 1,

    /// <summary>
    /// User logout action.
    /// </summary>
    Logout = 2,

    /// <summary>
    /// Order created.
    /// </summary>
    OrderCreated = 3,

    /// <summary>
    /// Order modified.
    /// </summary>
    OrderModified = 4,

    /// <summary>
    /// Order voided.
    /// </summary>
    OrderVoided = 5,

    /// <summary>
    /// Payment received.
    /// </summary>
    PaymentReceived = 6,

    /// <summary>
    /// Receipt voided.
    /// </summary>
    ReceiptVoided = 7,

    /// <summary>
    /// Work period opened.
    /// </summary>
    WorkPeriodOpened = 8,

    /// <summary>
    /// Work period closed.
    /// </summary>
    WorkPeriodClosed = 9,

    /// <summary>
    /// Price changed.
    /// </summary>
    PriceChanged = 10,

    /// <summary>
    /// Discount applied.
    /// </summary>
    DiscountApplied = 11,

    /// <summary>
    /// Permission override used.
    /// </summary>
    PermissionOverride = 12,

    /// <summary>
    /// Stock adjusted.
    /// </summary>
    StockAdjusted = 13,

    /// <summary>
    /// User created.
    /// </summary>
    UserCreated = 14,

    /// <summary>
    /// User modified.
    /// </summary>
    UserModified = 15,

    /// <summary>
    /// Configuration changed.
    /// </summary>
    ConfigurationChanged = 16,

    /// <summary>
    /// Customer enrolled in loyalty program.
    /// </summary>
    LoyaltyEnrollment = 17,

    /// <summary>
    /// Loyalty points earned.
    /// </summary>
    LoyaltyPointsEarned = 18,

    /// <summary>
    /// Loyalty points redeemed.
    /// </summary>
    LoyaltyPointsRedeemed = 19
}

/// <summary>
/// Membership tier levels for the loyalty program.
/// </summary>
public enum MembershipTier
{
    /// <summary>
    /// Entry level membership tier.
    /// </summary>
    Bronze = 1,

    /// <summary>
    /// Mid-level membership tier with enhanced benefits.
    /// </summary>
    Silver = 2,

    /// <summary>
    /// Premium membership tier with significant benefits.
    /// </summary>
    Gold = 3,

    /// <summary>
    /// Top-tier membership with maximum benefits.
    /// </summary>
    Platinum = 4
}

/// <summary>
/// Types of loyalty point transactions.
/// </summary>
public enum LoyaltyTransactionType
{
    /// <summary>
    /// Points earned from a purchase.
    /// </summary>
    Earned = 1,

    /// <summary>
    /// Points redeemed for a discount.
    /// </summary>
    Redeemed = 2,

    /// <summary>
    /// Points adjusted manually (positive or negative).
    /// </summary>
    Adjustment = 3,

    /// <summary>
    /// Points expired due to inactivity.
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Bonus points awarded (promotion, signup, etc.).
    /// </summary>
    Bonus = 5,

    /// <summary>
    /// Points refunded due to void/refund.
    /// </summary>
    Refund = 6,

    /// <summary>
    /// Points transferred from another member.
    /// </summary>
    TransferIn = 7,

    /// <summary>
    /// Points transferred to another member.
    /// </summary>
    TransferOut = 8
}
