// src/HospitalityPOS.Core/Models/HR/CommissionDtos.cs
// DTOs for sales commission calculation and tracking
// Story 45-3: Commission Calculation

namespace HospitalityPOS.Core.Models.HR;

#region Enums

/// <summary>
/// Type of commission rule.
/// </summary>
public enum CommissionRuleType
{
    /// <summary>Commission based on employee role.</summary>
    Role,
    /// <summary>Commission based on product category.</summary>
    Category,
    /// <summary>Commission based on specific product.</summary>
    Product,
    /// <summary>Commission based on employee individually.</summary>
    Employee,
    /// <summary>Global default commission rate.</summary>
    Global
}

/// <summary>
/// Type of commission transaction.
/// </summary>
public enum CommissionTransactionType
{
    /// <summary>Commission earned from a sale.</summary>
    Earned,
    /// <summary>Commission reversed due to return/void.</summary>
    Reversed,
    /// <summary>Manual commission adjustment.</summary>
    Adjustment,
    /// <summary>Commission paid out.</summary>
    Paid
}

/// <summary>
/// Commission calculation method.
/// </summary>
public enum CommissionCalculationMethod
{
    /// <summary>Percentage of sale amount.</summary>
    Percentage,
    /// <summary>Fixed amount per sale.</summary>
    FixedAmount,
    /// <summary>Fixed amount per unit sold.</summary>
    PerUnit
}

/// <summary>
/// Status of commission payout.
/// </summary>
public enum CommissionPayoutStatus
{
    /// <summary>Commission pending payout.</summary>
    Pending,
    /// <summary>Commission approved for payout.</summary>
    Approved,
    /// <summary>Commission paid.</summary>
    Paid,
    /// <summary>Commission on hold.</summary>
    OnHold
}

#endregion

#region Settings

/// <summary>
/// Commission system settings.
/// </summary>
public class CommissionSettings
{
    /// <summary>Default commission percentage.</summary>
    public decimal DefaultCommissionPercent { get; set; } = 0m;

    /// <summary>Enable tiered commission.</summary>
    public bool EnableTieredCommission { get; set; } = true;

    /// <summary>Enable split commissions for shared sales.</summary>
    public bool EnableSplitCommissions { get; set; } = true;

    /// <summary>Minimum sale amount for commission eligibility.</summary>
    public decimal MinimumSaleAmount { get; set; } = 0m;

    /// <summary>Calculate commission before or after discounts.</summary>
    public bool CalculateOnGrossAmount { get; set; } = false;

    /// <summary>Include tax in commission calculation.</summary>
    public bool IncludeTaxInCalculation { get; set; } = false;

    /// <summary>Commission payout frequency (Monthly, BiWeekly, Weekly).</summary>
    public string PayoutFrequency { get; set; } = "Monthly";

    /// <summary>Day of month/week for payout processing.</summary>
    public int PayoutDay { get; set; } = 15;

    /// <summary>Require manager approval for commission payouts.</summary>
    public bool RequireApprovalForPayout { get; set; } = true;

    /// <summary>Days after sale before commission is confirmed.</summary>
    public int ConfirmationPeriodDays { get; set; } = 14;

    /// <summary>Track commission by product, category, or total.</summary>
    public string TrackingLevel { get; set; } = "Product";
}

#endregion

#region Commission Rules

/// <summary>
/// Commission rule definition.
/// </summary>
public class CommissionRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CommissionRuleType RuleType { get; set; }
    public CommissionCalculationMethod CalculationMethod { get; set; } = CommissionCalculationMethod.Percentage;

    // Target (one of these based on RuleType)
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }

    // Commission rates
    public decimal CommissionRate { get; set; }
    public decimal? FixedAmount { get; set; }

    // Tiered commission
    public decimal? TierThreshold { get; set; }
    public decimal? TierCommissionRate { get; set; }
    public List<CommissionTier> Tiers { get; set; } = new();

    // Constraints
    public decimal MinimumSaleAmount { get; set; } = 0m;
    public decimal? MaximumCommission { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher = checked first

    // Validity
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Commission tier for tiered rates.
/// </summary>
public class CommissionTier
{
    public decimal ThresholdAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request to create or update a commission rule.
/// </summary>
public class CommissionRuleRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CommissionRuleType RuleType { get; set; }
    public CommissionCalculationMethod CalculationMethod { get; set; } = CommissionCalculationMethod.Percentage;
    public int? TargetId { get; set; } // RoleId, CategoryId, ProductId, or EmployeeId
    public decimal CommissionRate { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal MinimumSaleAmount { get; set; }
    public decimal? MaximumCommission { get; set; }
    public List<CommissionTier>? Tiers { get; set; }
    public int Priority { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

#endregion

#region Commission Transactions

/// <summary>
/// Commission transaction record.
/// </summary>
public class CommissionTransaction
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public CommissionTransactionType TransactionType { get; set; } = CommissionTransactionType.Earned;
    public string? Notes { get; set; }
    public int? RelatedTransactionId { get; set; } // For reversals
    public DateTime CreatedAt { get; set; }

    // Attribution
    public decimal SplitPercentage { get; set; } = 100m;
    public int? OriginalEmployeeId { get; set; } // If split or reassigned

    // Rule reference
    public int? CommissionRuleId { get; set; }
    public string? RuleDescription { get; set; }
}

/// <summary>
/// Commission calculation result.
/// </summary>
public class CommissionCalculationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal TotalCommission { get; set; }
    public List<CommissionLineItem> LineItems { get; set; } = new();
    public CommissionRule? AppliedRule { get; set; }
    public bool TierBonusApplied { get; set; }
    public decimal TierBonusAmount { get; set; }

    public static CommissionCalculationResult Calculated(decimal total, List<CommissionLineItem> items)
        => new() { Success = true, TotalCommission = total, LineItems = items };

    public static CommissionCalculationResult Failed(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Commission line item breakdown.
/// </summary>
public class CommissionLineItem
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public int? RuleId { get; set; }
    public string? RuleDescription { get; set; }
}

#endregion

#region Sales Attribution

/// <summary>
/// Sales attribution for commission purposes.
/// </summary>
public class SalesAttribution
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public List<EmployeeAttribution> Employees { get; set; } = new();
    public bool IsSplit { get; set; }
    public int? OverriddenByUserId { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? OverriddenAt { get; set; }
}

/// <summary>
/// Employee's share of a sale.
/// </summary>
public class EmployeeAttribution
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal SplitPercentage { get; set; } = 100m;
    public bool IsPrimary { get; set; } = true;
}

/// <summary>
/// Request to attribute or reassign a sale.
/// </summary>
public class AttributionRequest
{
    public int ReceiptId { get; set; }
    public List<EmployeeAttribution> Attributions { get; set; } = new();
    public int? OverriddenByUserId { get; set; }
    public string? Reason { get; set; }
}

#endregion

#region Reports

/// <summary>
/// Employee commission summary.
/// </summary>
public class EmployeeCommissionSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Department { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    // Sales metrics
    public int TotalSales { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal AverageSaleAmount => TotalSales > 0 ? TotalSalesAmount / TotalSales : 0m;

    // Commission metrics
    public decimal TotalCommissionEarned { get; set; }
    public decimal TotalCommissionReversed { get; set; }
    public decimal NetCommission => TotalCommissionEarned - TotalCommissionReversed;
    public decimal AverageCommissionRate => TotalSalesAmount > 0
        ? (TotalCommissionEarned / TotalSalesAmount) * 100
        : 0m;

    // Tier information
    public bool TierBonusEarned { get; set; }
    public decimal TierBonusAmount { get; set; }

    // Breakdown
    public List<CommissionByCategory> ByCategory { get; set; } = new();
    public List<CommissionTransaction> Transactions { get; set; } = new();
}

/// <summary>
/// Commission breakdown by category.
/// </summary>
public class CommissionByCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public int SalesCount { get; set; }
}

/// <summary>
/// Commission report for multiple employees.
/// </summary>
public class CommissionReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<EmployeeCommissionSummary> Employees { get; set; } = new();
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalCommission { get; set; }
    public int TotalTransactions { get; set; }

    // Top performers
    public EmployeeCommissionSummary? TopEarner { get; set; }
    public EmployeeCommissionSummary? TopSeller { get; set; }

    // By category summary
    public List<CommissionByCategory> ByCategorySummary { get; set; } = new();
}

/// <summary>
/// Commission payout record.
/// </summary>
public class CommissionPayout
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal Adjustments { get; set; }
    public decimal NetPayout { get; set; }
    public CommissionPayoutStatus Status { get; set; } = CommissionPayoutStatus.Pending;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to process commission payout.
/// </summary>
public class PayoutRequest
{
    public int EmployeeId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? Adjustments { get; set; }
    public string? AdjustmentReason { get; set; }
}

/// <summary>
/// Result of payout operation.
/// </summary>
public class PayoutResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CommissionPayout? Payout { get; set; }

    public static PayoutResult Succeeded(CommissionPayout payout, string message = "Payout created")
        => new() { Success = true, Message = message, Payout = payout };

    public static PayoutResult Failed(string message)
        => new() { Success = false, Message = message };
}

#endregion

#region Payroll Integration

/// <summary>
/// Commission data for payroll export.
/// </summary>
public class CommissionPayrollExport
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public List<EmployeeCommissionPayroll> Employees { get; set; } = new();
    public decimal TotalCommission { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Employee commission for payroll.
/// </summary>
public class EmployeeCommissionPayroll
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal Adjustments { get; set; }
    public decimal NetCommission { get; set; }
    public int TransactionCount { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Events

/// <summary>
/// Event args for commission events.
/// </summary>
public class CommissionEventArgs : EventArgs
{
    public CommissionTransaction Transaction { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public CommissionEventArgs(CommissionTransaction transaction, string eventType)
    {
        Transaction = transaction;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for payout events.
/// </summary>
public class PayoutEventArgs : EventArgs
{
    public CommissionPayout Payout { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public PayoutEventArgs(CommissionPayout payout, string eventType)
    {
        Payout = payout;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
