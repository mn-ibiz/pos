namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Status of a KDS station.
/// </summary>
public enum KdsStationStatus
{
    /// <summary>
    /// Station is offline and not receiving orders.
    /// </summary>
    Offline = 1,

    /// <summary>
    /// Station is online and receiving orders.
    /// </summary>
    Online = 2,

    /// <summary>
    /// Station is paused temporarily.
    /// </summary>
    Paused = 3
}

/// <summary>
/// Type of KDS station.
/// </summary>
public enum KdsStationType
{
    /// <summary>
    /// Standard prep station (Hot Line, Cold Station, etc.).
    /// </summary>
    PrepStation = 1,

    /// <summary>
    /// Expo station that sees all orders.
    /// </summary>
    Expo = 2,

    /// <summary>
    /// Bar station for beverages.
    /// </summary>
    Bar = 3,

    /// <summary>
    /// Dessert station.
    /// </summary>
    Dessert = 4
}

/// <summary>
/// Status of a KDS order.
/// </summary>
public enum KdsOrderStatus
{
    /// <summary>
    /// Order just received and not started.
    /// </summary>
    New = 1,

    /// <summary>
    /// Order preparation is in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Order is ready for pickup/serving.
    /// </summary>
    Ready = 3,

    /// <summary>
    /// Order has been served to the customer.
    /// </summary>
    Served = 4,

    /// <summary>
    /// Order was recalled after being marked ready.
    /// </summary>
    Recalled = 5,

    /// <summary>
    /// Order was voided/cancelled.
    /// </summary>
    Voided = 6
}

/// <summary>
/// Status of an individual KDS order item.
/// </summary>
public enum KdsItemStatus
{
    /// <summary>
    /// Item is pending preparation.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Item is being prepared.
    /// </summary>
    Preparing = 2,

    /// <summary>
    /// Item preparation is complete.
    /// </summary>
    Done = 3,

    /// <summary>
    /// Item was voided/cancelled.
    /// </summary>
    Voided = 4
}

/// <summary>
/// Priority level for orders.
/// </summary>
public enum OrderPriority
{
    /// <summary>
    /// Normal priority order.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Rush order requiring faster preparation.
    /// </summary>
    Rush = 1,

    /// <summary>
    /// VIP order with highest priority.
    /// </summary>
    VIP = 2
}

/// <summary>
/// Timer color status for order age.
/// </summary>
public enum TimerColor
{
    /// <summary>
    /// Order is on time (green).
    /// </summary>
    Green = 1,

    /// <summary>
    /// Order is approaching threshold (yellow).
    /// </summary>
    Yellow = 2,

    /// <summary>
    /// Order is overdue (red).
    /// </summary>
    Red = 3
}

/// <summary>
/// Priority level for all-call messages.
/// </summary>
public enum AllCallPriority
{
    /// <summary>
    /// Normal priority message.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Urgent priority message.
    /// </summary>
    Urgent = 2
}

#endregion

#region Station Entities

/// <summary>
/// Represents a Kitchen Display System station.
/// </summary>
public class KdsStation : BaseEntity
{
    /// <summary>
    /// Name of the station (e.g., "Hot Line", "Cold Station", "Bar").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique device identifier (IP address or hostname).
    /// </summary>
    public string DeviceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Type of KDS station.
    /// </summary>
    public KdsStationType StationType { get; set; } = KdsStationType.PrepStation;

    /// <summary>
    /// Current status of the station.
    /// </summary>
    public KdsStationStatus Status { get; set; } = KdsStationStatus.Offline;

    /// <summary>
    /// Display order for sorting stations.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is an expo station (sees all orders).
    /// </summary>
    public bool IsExpo { get; set; }

    /// <summary>
    /// Store this station belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Description or notes about the station.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Last time the station connected.
    /// </summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// Display settings for the station.
    /// </summary>
    public int? DisplaySettingsId { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual KdsDisplaySettings? DisplaySettings { get; set; }
    public virtual ICollection<KdsStationCategory> Categories { get; set; } = new List<KdsStationCategory>();
    public virtual ICollection<KdsOrderItem> OrderItems { get; set; } = new List<KdsOrderItem>();
    public virtual ICollection<KdsOrderStatusLog> StatusLogs { get; set; } = new List<KdsOrderStatusLog>();
}

/// <summary>
/// Junction table linking KDS stations to product categories.
/// </summary>
public class KdsStationCategory : BaseEntity
{
    /// <summary>
    /// The KDS station ID.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The product category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Display order for sorting categories on this station.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual KdsStation Station { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
}

/// <summary>
/// Display settings for a KDS station.
/// </summary>
public class KdsDisplaySettings : BaseEntity
{
    /// <summary>
    /// Number of columns in the display grid.
    /// </summary>
    public int ColumnsCount { get; set; } = 4;

    /// <summary>
    /// Font size for order display.
    /// </summary>
    public int FontSize { get; set; } = 16;

    /// <summary>
    /// Minutes before order shows warning color (yellow).
    /// </summary>
    public int WarningThresholdMinutes { get; set; } = 10;

    /// <summary>
    /// Minutes before order shows alert color (red).
    /// </summary>
    public int AlertThresholdMinutes { get; set; } = 15;

    /// <summary>
    /// Minutes in green zone.
    /// </summary>
    public int GreenThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to show modifiers on items.
    /// </summary>
    public bool ShowModifiers { get; set; } = true;

    /// <summary>
    /// Whether to show special instructions.
    /// </summary>
    public bool ShowSpecialInstructions { get; set; } = true;

    /// <summary>
    /// Whether to play audio alerts.
    /// </summary>
    public bool AudioAlerts { get; set; } = true;

    /// <summary>
    /// Whether to flash overdue orders.
    /// </summary>
    public bool FlashWhenOverdue { get; set; } = true;

    /// <summary>
    /// Flash interval in seconds.
    /// </summary>
    public int FlashIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Audio repeat interval for overdue orders in seconds.
    /// </summary>
    public int AudioRepeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of minutes a bumped order can be recalled.
    /// </summary>
    public int RecallWindowMinutes { get; set; } = 10;

    /// <summary>
    /// Theme/color scheme name.
    /// </summary>
    public string? ThemeName { get; set; }

    /// <summary>
    /// Background color for the display.
    /// </summary>
    public string? BackgroundColor { get; set; }

    // Navigation property
    public virtual ICollection<KdsStation> Stations { get; set; } = new List<KdsStation>();
}

#endregion

#region Order Entities

/// <summary>
/// Represents an order on the KDS system.
/// </summary>
public class KdsOrder : BaseEntity
{
    /// <summary>
    /// Reference to the original order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Order number for display.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Table number or identifier.
    /// </summary>
    public string? TableNumber { get; set; }

    /// <summary>
    /// Customer name if available.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Number of guests at the table.
    /// </summary>
    public int GuestCount { get; set; } = 1;

    /// <summary>
    /// When the order was received on KDS.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public KdsOrderStatus Status { get; set; } = KdsOrderStatus.New;

    /// <summary>
    /// When preparation started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When preparation was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the order was served.
    /// </summary>
    public DateTime? ServedAt { get; set; }

    /// <summary>
    /// Priority level of the order.
    /// </summary>
    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    /// <summary>
    /// Whether this is a priority order (rush/VIP).
    /// </summary>
    public bool IsPriority { get; set; }

    /// <summary>
    /// General notes or instructions.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Store this order belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// User who marked the order as served.
    /// </summary>
    public int? ServedByUserId { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual User? ServedByUser { get; set; }
    public virtual ICollection<KdsOrderItem> Items { get; set; } = new List<KdsOrderItem>();
    public virtual ICollection<KdsOrderStatusLog> StatusLogs { get; set; } = new List<KdsOrderStatusLog>();
}

/// <summary>
/// Represents an item in a KDS order.
/// </summary>
public class KdsOrderItem : BaseEntity
{
    /// <summary>
    /// The parent KDS order.
    /// </summary>
    public int KdsOrderId { get; set; }

    /// <summary>
    /// Reference to the original order item.
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// Product name for display.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Modifiers as comma-separated string.
    /// </summary>
    public string? Modifiers { get; set; }

    /// <summary>
    /// Special instructions for the item.
    /// </summary>
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Station this item should be prepared at.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// Current status of the item.
    /// </summary>
    public KdsItemStatus Status { get; set; } = KdsItemStatus.Pending;

    /// <summary>
    /// When the item started being prepared.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the item was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who completed the item.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Sequence number for ordering items.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Course number (1=appetizers, 2=mains, 3=desserts).
    /// </summary>
    public int? CourseNumber { get; set; }

    // Navigation properties
    public virtual KdsOrder KdsOrder { get; set; } = null!;
    public virtual OrderItem OrderItem { get; set; } = null!;
    public virtual KdsStation Station { get; set; } = null!;
    public virtual User? CompletedByUser { get; set; }
}

/// <summary>
/// Log entry for KDS order status changes.
/// </summary>
public class KdsOrderStatusLog : BaseEntity
{
    /// <summary>
    /// The KDS order this log belongs to.
    /// </summary>
    public int KdsOrderId { get; set; }

    /// <summary>
    /// The station where the action occurred.
    /// </summary>
    public int? StationId { get; set; }

    /// <summary>
    /// Previous status before the change.
    /// </summary>
    public KdsOrderStatus PreviousStatus { get; set; }

    /// <summary>
    /// New status after the change.
    /// </summary>
    public KdsOrderStatus NewStatus { get; set; }

    /// <summary>
    /// User who made the change.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Timestamp of the status change.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the change.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual KdsOrder KdsOrder { get; set; } = null!;
    public virtual KdsStation? Station { get; set; }
    public virtual User? User { get; set; }
}

#endregion

#region All-Call Entities

/// <summary>
/// Represents an all-call message sent to KDS stations.
/// </summary>
public class AllCallMessage : BaseEntity
{
    /// <summary>
    /// The message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// User who sent the message.
    /// </summary>
    public int SentByUserId { get; set; }

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Priority level of the message.
    /// </summary>
    public AllCallPriority Priority { get; set; } = AllCallPriority.Normal;

    /// <summary>
    /// When the message expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the message is still active.
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Store this message was sent in.
    /// </summary>
    public int StoreId { get; set; }

    // Navigation properties
    public virtual User SentByUser { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<AllCallMessageTarget> Targets { get; set; } = new List<AllCallMessageTarget>();
    public virtual ICollection<AllCallMessageDismissal> Dismissals { get; set; } = new List<AllCallMessageDismissal>();
}

/// <summary>
/// Target station for an all-call message.
/// </summary>
public class AllCallMessageTarget : BaseEntity
{
    /// <summary>
    /// The message this target belongs to.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// The target station (null means all stations).
    /// </summary>
    public int? StationId { get; set; }

    // Navigation properties
    public virtual AllCallMessage Message { get; set; } = null!;
    public virtual KdsStation? Station { get; set; }
}

/// <summary>
/// Record of a station dismissing an all-call message.
/// </summary>
public class AllCallMessageDismissal : BaseEntity
{
    /// <summary>
    /// The message that was dismissed.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// The station that dismissed the message.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// When the message was dismissed.
    /// </summary>
    public DateTime DismissedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who dismissed the message.
    /// </summary>
    public int? DismissedByUserId { get; set; }

    // Navigation properties
    public virtual AllCallMessage Message { get; set; } = null!;
    public virtual KdsStation Station { get; set; } = null!;
    public virtual User? DismissedByUser { get; set; }
}

#endregion

#region Timer Configuration

/// <summary>
/// Timer configuration for KDS order timing.
/// </summary>
public class KdsTimerConfig : BaseEntity
{
    /// <summary>
    /// Minutes for green threshold (on time).
    /// </summary>
    public int GreenThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Minutes for yellow threshold (warning).
    /// </summary>
    public int YellowThresholdMinutes { get; set; } = 10;

    /// <summary>
    /// Minutes for red threshold (overdue).
    /// </summary>
    public int RedThresholdMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to flash when overdue.
    /// </summary>
    public bool FlashWhenOverdue { get; set; } = true;

    /// <summary>
    /// Flash interval in seconds.
    /// </summary>
    public int FlashIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Whether to play audio alert on overdue.
    /// </summary>
    public bool AudioAlertOnOverdue { get; set; } = true;

    /// <summary>
    /// Audio repeat interval in seconds.
    /// </summary>
    public int AudioRepeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Store this configuration belongs to.
    /// </summary>
    public int StoreId { get; set; }

    // Navigation property
    public virtual Store Store { get; set; } = null!;
}

#endregion
