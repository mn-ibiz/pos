namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Configuration settings for automatic purchase order generation.
/// </summary>
public class PurchaseOrderSettings : BaseEntity
{
    /// <summary>
    /// Store this configuration applies to (null for global settings).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether automatic purchase order generation is enabled.
    /// </summary>
    public bool AutoGeneratePurchaseOrders { get; set; }

    /// <summary>
    /// Whether to automatically send POs to suppliers (vs draft mode).
    /// </summary>
    public bool AutoSendPurchaseOrders { get; set; }

    /// <summary>
    /// Maximum PO value that can be auto-approved without manager review.
    /// </summary>
    public decimal AutoApprovalThreshold { get; set; } = 50000;

    /// <summary>
    /// Whether manager approval is required for all auto-generated POs.
    /// </summary>
    public bool RequireManagerApproval { get; set; } = true;

    /// <summary>
    /// Interval in minutes for stock check monitoring.
    /// </summary>
    public int StockCheckIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to send notifications on low stock.
    /// </summary>
    public bool NotifyOnLowStock { get; set; } = true;

    /// <summary>
    /// Whether to send notifications when PO is generated.
    /// </summary>
    public bool NotifyOnPOGenerated { get; set; } = true;

    /// <summary>
    /// Whether to send notifications when PO is sent to supplier.
    /// </summary>
    public bool NotifyOnPOSent { get; set; } = true;

    /// <summary>
    /// Days threshold for low stock warning.
    /// </summary>
    public int LowStockThresholdDays { get; set; } = 7;

    /// <summary>
    /// Default lead time in days for suppliers without specific lead times.
    /// </summary>
    public int DefaultLeadTimeDays { get; set; } = 7;

    /// <summary>
    /// Whether to consolidate POs by supplier.
    /// </summary>
    public bool ConsolidatePOsBySupplier { get; set; } = true;

    /// <summary>
    /// Minimum PO amount (below this, PO is not created).
    /// </summary>
    public decimal MinimumPOAmount { get; set; }

    /// <summary>
    /// Maximum items per single PO (for splitting large orders).
    /// </summary>
    public int MaxItemsPerPO { get; set; } = 50;

    /// <summary>
    /// Whether to send daily pending PO digest emails.
    /// </summary>
    public bool SendDailyPendingPODigest { get; set; }

    /// <summary>
    /// Time to send daily digest (e.g., "08:00").
    /// </summary>
    public string? DailyDigestTime { get; set; } = "08:00";

    /// <summary>
    /// Whether to send weekly summary emails.
    /// </summary>
    public bool SendWeeklySummary { get; set; }

    /// <summary>
    /// Day of week for weekly summary (0 = Sunday).
    /// </summary>
    public int WeeklySummaryDay { get; set; } = 1; // Monday

    /// <summary>
    /// Email addresses for digest recipients (comma-separated).
    /// </summary>
    public string? DigestRecipientEmails { get; set; }

    /// <summary>
    /// Start hour for business hours (stock monitoring only runs during these hours).
    /// </summary>
    public int BusinessHoursStart { get; set; } = 6;

    /// <summary>
    /// End hour for business hours.
    /// </summary>
    public int BusinessHoursEnd { get; set; } = 22;

    /// <summary>
    /// Whether to run stock monitoring on weekends.
    /// </summary>
    public bool RunOnWeekends { get; set; } = true;

    /// <summary>
    /// Last time stock monitoring was run.
    /// </summary>
    public DateTime? LastStockCheckTime { get; set; }

    /// <summary>
    /// User who last updated these settings.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Whether this configuration is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}
