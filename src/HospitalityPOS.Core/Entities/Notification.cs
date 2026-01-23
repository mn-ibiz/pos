namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    /// <summary>General information.</summary>
    Info = 1,
    /// <summary>Warning message.</summary>
    Warning = 2,
    /// <summary>Error message.</summary>
    Error = 3,
    /// <summary>Success message.</summary>
    Success = 4,
    /// <summary>Low stock alert.</summary>
    LowStock = 5,
    /// <summary>Purchase order generated.</summary>
    POGenerated = 6,
    /// <summary>Purchase order pending approval.</summary>
    POPendingApproval = 7,
    /// <summary>Purchase order approved.</summary>
    POApproved = 8,
    /// <summary>Purchase order sent to supplier.</summary>
    POSent = 9,
    /// <summary>Purchase order overdue.</summary>
    POOverdue = 10,
    /// <summary>Goods received.</summary>
    GoodsReceived = 11,
    /// <summary>Stock take reminder.</summary>
    StockTakeReminder = 12,
    /// <summary>Product expiry warning.</summary>
    ExpiryWarning = 13,
    /// <summary>System alert.</summary>
    SystemAlert = 14
}

/// <summary>
/// Notification priority levels.
/// </summary>
public enum NotificationPriority
{
    /// <summary>Low priority.</summary>
    Low = 1,
    /// <summary>Normal priority.</summary>
    Normal = 2,
    /// <summary>High priority.</summary>
    High = 3,
    /// <summary>Urgent priority.</summary>
    Urgent = 4
}

/// <summary>
/// In-app notification entity.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// Store this notification applies to (null for all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// User this notification is for (null for all users with appropriate role).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Role this notification is for (if UserId is null).
    /// </summary>
    public int? RoleId { get; set; }

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification.
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>
    /// Priority level.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Navigation action (e.g., "PurchaseOrders", "Inventory").
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Navigation parameter (e.g., PO ID).
    /// </summary>
    public string? ActionParameter { get; set; }

    /// <summary>
    /// Related entity type (e.g., "PurchaseOrder", "Product").
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related entity ID.
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Expiration date for the notification.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this notification has been dismissed.
    /// </summary>
    public bool IsDismissed { get; set; }

    /// <summary>
    /// Whether this notification is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}
