namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Parameters for generating audit trail reports with pagination support.
/// </summary>
public class AuditReportParameters
{
    private int _pageNumber = 1;
    private int _pageSize = 50;

    /// <summary>
    /// Gets or sets the start date for the report.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the report.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the user ID to filter by (optional).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the action type to filter by (optional).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the entity type to filter by (optional).
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records to return.
    /// Deprecated: Use PageNumber and PageSize for pagination instead.
    /// </summary>
    [Obsolete("Use PageNumber and PageSize for proper pagination")]
    public int MaxRecords { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the page number (1-based). Default is 1.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the page size. Default is 50, max is 500.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 50 : (value > 500 ? 500 : value);
    }

    /// <summary>
    /// Gets the number of records to skip for pagination.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of records to take for pagination.
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Gets or sets whether to use pagination (true) or legacy MaxRecords (false).
    /// Default is true for new code.
    /// </summary>
    public bool UsePagination { get; set; } = true;
}

/// <summary>
/// Result of a user activity report.
/// </summary>
public class UserActivityReportResult
{
    /// <summary>
    /// Gets or sets the total action count.
    /// </summary>
    public int TotalActions { get; set; }

    /// <summary>
    /// Gets or sets the login count.
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// Gets or sets the logout count.
    /// </summary>
    public int LogoutCount { get; set; }

    /// <summary>
    /// Gets or sets the failed login count.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the activity items.
    /// </summary>
    public List<UserActivityItem> Items { get; set; } = [];
}

/// <summary>
/// Single user activity item.
/// </summary>
public class UserActivityItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action display name.
    /// </summary>
    public string ActionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Result of a transaction log report.
/// </summary>
public class TransactionLogReportResult
{
    /// <summary>
    /// Gets or sets the total transaction count.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Gets or sets the order count.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the settlement count.
    /// </summary>
    public int SettlementCount { get; set; }

    /// <summary>
    /// Gets or sets the total sales value.
    /// </summary>
    public decimal TotalSalesValue { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the transaction items.
    /// </summary>
    public List<TransactionLogItem> Items { get; set; } = [];
}

/// <summary>
/// Single transaction log item.
/// </summary>
public class TransactionLogItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action display name.
    /// </summary>
    public string ActionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the old values (JSON).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the parsed amount from values.
    /// </summary>
    public decimal? Amount { get; set; }
}

/// <summary>
/// Result of a void/refund log report.
/// </summary>
public class VoidRefundLogReportResult
{
    /// <summary>
    /// Gets or sets the total void count.
    /// </summary>
    public int TotalVoids { get; set; }

    /// <summary>
    /// Gets or sets the total void value.
    /// </summary>
    public decimal TotalVoidValue { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the void log items.
    /// </summary>
    public List<VoidRefundLogItem> Items { get; set; } = [];
}

/// <summary>
/// Single void/refund log item.
/// </summary>
public class VoidRefundLogItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user name who initiated.
    /// </summary>
    public string RequestedByUser { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authorizing user name.
    /// </summary>
    public string? AuthorizedByUser { get; set; }

    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Gets or sets the void reason.
    /// </summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Gets or sets the voided amount.
    /// </summary>
    public decimal VoidedAmount { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON).
    /// </summary>
    public string? NewValues { get; set; }
}

/// <summary>
/// Result of a price change log report.
/// </summary>
public class PriceChangeLogReportResult
{
    /// <summary>
    /// Gets or sets the total price change count.
    /// </summary>
    public int TotalPriceChanges { get; set; }

    /// <summary>
    /// Gets or sets the products affected count.
    /// </summary>
    public int ProductsAffected { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the price change log items.
    /// </summary>
    public List<PriceChangeLogItem> Items { get; set; } = [];
}

/// <summary>
/// Single price change log item.
/// </summary>
public class PriceChangeLogItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old price.
    /// </summary>
    public decimal? OldPrice { get; set; }

    /// <summary>
    /// Gets or sets the new price.
    /// </summary>
    public decimal? NewPrice { get; set; }

    /// <summary>
    /// Gets or sets the price difference.
    /// </summary>
    public decimal PriceDifference { get; set; }

    /// <summary>
    /// Gets or sets the change percentage.
    /// </summary>
    public decimal ChangePercentage { get; set; }

    /// <summary>
    /// Gets or sets the old values (JSON).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON).
    /// </summary>
    public string? NewValues { get; set; }
}

/// <summary>
/// Result of a permission override log report.
/// </summary>
public class PermissionOverrideLogReportResult
{
    /// <summary>
    /// Gets or sets the total override count.
    /// </summary>
    public int TotalOverrides { get; set; }

    /// <summary>
    /// Gets or sets the override by type breakdown.
    /// </summary>
    public Dictionary<string, int> OverridesByType { get; set; } = [];

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the permission override log items.
    /// </summary>
    public List<PermissionOverrideLogItem> Items { get; set; } = [];
}

/// <summary>
/// Single permission override log item.
/// </summary>
public class PermissionOverrideLogItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user who requested the action.
    /// </summary>
    public string RequestedByUser { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who authorized the override.
    /// </summary>
    public string AuthorizedByUser { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission that was overridden.
    /// </summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission display name.
    /// </summary>
    public string PermissionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action that was authorized.
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Gets or sets the entity reference.
    /// </summary>
    public string? EntityReference { get; set; }

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON).
    /// </summary>
    public string? NewValues { get; set; }
}

/// <summary>
/// Combined audit trail report result with pagination support.
/// </summary>
public class AuditTrailReportResult
{
    /// <summary>
    /// Gets or sets the total action count (total matching records across all pages).
    /// </summary>
    public int TotalActions { get; set; }

    /// <summary>
    /// Gets or sets the unique users count.
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets the audit log items for the current page.
    /// </summary>
    public List<AuditTrailItem> Items { get; set; } = [];

    // Pagination properties

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalActions / (double)PageSize) : 0;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Single audit trail item.
/// </summary>
public class AuditTrailItem
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public long AuditLogId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action display name.
    /// </summary>
    public string ActionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the old values (JSON).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string? MachineName { get; set; }
}

/// <summary>
/// Audit report types.
/// </summary>
public enum AuditReportType
{
    /// <summary>
    /// All activity log.
    /// </summary>
    AllActivity,

    /// <summary>
    /// User activity log.
    /// </summary>
    UserActivity,

    /// <summary>
    /// Transaction log.
    /// </summary>
    TransactionLog,

    /// <summary>
    /// Void/refund log.
    /// </summary>
    VoidRefundLog,

    /// <summary>
    /// Price change log.
    /// </summary>
    PriceChangeLog,

    /// <summary>
    /// Permission override log.
    /// </summary>
    PermissionOverrideLog
}
