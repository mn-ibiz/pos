namespace HospitalityPOS.Core.DTOs;

#region Enums

/// <summary>
/// Status of a KDS station for DTOs.
/// </summary>
public enum KdsStationStatusDto
{
    Offline = 1,
    Online = 2,
    Paused = 3
}

/// <summary>
/// Type of KDS station for DTOs.
/// </summary>
public enum KdsStationTypeDto
{
    PrepStation = 1,
    Expo = 2,
    Bar = 3,
    Dessert = 4
}

/// <summary>
/// Status of a KDS order for DTOs.
/// </summary>
public enum KdsOrderStatusDto
{
    New = 1,
    InProgress = 2,
    Ready = 3,
    Served = 4,
    Recalled = 5,
    Voided = 6
}

/// <summary>
/// Status of an individual KDS order item for DTOs.
/// </summary>
public enum KdsItemStatusDto
{
    Pending = 1,
    Preparing = 2,
    Done = 3,
    Voided = 4
}

/// <summary>
/// Priority level for orders in DTOs.
/// </summary>
public enum OrderPriorityDto
{
    Normal = 0,
    Rush = 1,
    VIP = 2
}

/// <summary>
/// Timer color status for DTOs.
/// </summary>
public enum TimerColorDto
{
    Green = 1,
    Yellow = 2,
    Red = 3
}

/// <summary>
/// Priority level for all-call messages in DTOs.
/// </summary>
public enum AllCallPriorityDto
{
    Normal = 1,
    Urgent = 2
}

#endregion

#region Station DTOs

/// <summary>
/// Full KDS station details.
/// </summary>
public class KdsStationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public KdsStationTypeDto StationType { get; set; }
    public KdsStationStatusDto Status { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsExpo { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? Description { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public KdsDisplaySettingsDto? DisplaySettings { get; set; }
    public List<KdsStationCategoryDto> Categories { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Create KDS station request.
/// </summary>
public class CreateKdsStationDto
{
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public KdsStationTypeDto StationType { get; set; } = KdsStationTypeDto.PrepStation;
    public int DisplayOrder { get; set; }
    public bool IsExpo { get; set; }
    public int StoreId { get; set; }
    public string? Description { get; set; }
    public CreateKdsDisplaySettingsDto? DisplaySettings { get; set; }
    public List<int>? CategoryIds { get; set; }
}

/// <summary>
/// Update KDS station request.
/// </summary>
public class UpdateKdsStationDto
{
    public string? Name { get; set; }
    public string? DeviceIdentifier { get; set; }
    public KdsStationTypeDto? StationType { get; set; }
    public KdsStationStatusDto? Status { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsExpo { get; set; }
    public string? Description { get; set; }
    public UpdateKdsDisplaySettingsDto? DisplaySettings { get; set; }
}

/// <summary>
/// KDS station list item.
/// </summary>
public class KdsStationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public KdsStationTypeDto StationType { get; set; }
    public KdsStationStatusDto Status { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsExpo { get; set; }
    public int CategoryCount { get; set; }
    public int ActiveOrderCount { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// KDS station category mapping.
/// </summary>
public class KdsStationCategoryDto
{
    public int Id { get; set; }
    public int StationId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Assign category to station request.
/// </summary>
public class AssignCategoryDto
{
    public int StationId { get; set; }
    public int CategoryId { get; set; }
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Display settings for a KDS station.
/// </summary>
public class KdsDisplaySettingsDto
{
    public int Id { get; set; }
    public int ColumnsCount { get; set; }
    public int FontSize { get; set; }
    public int WarningThresholdMinutes { get; set; }
    public int AlertThresholdMinutes { get; set; }
    public int GreenThresholdMinutes { get; set; }
    public bool ShowModifiers { get; set; }
    public bool ShowSpecialInstructions { get; set; }
    public bool AudioAlerts { get; set; }
    public bool FlashWhenOverdue { get; set; }
    public int FlashIntervalSeconds { get; set; }
    public int AudioRepeatIntervalSeconds { get; set; }
    public int RecallWindowMinutes { get; set; }
    public string? ThemeName { get; set; }
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// Create display settings request.
/// </summary>
public class CreateKdsDisplaySettingsDto
{
    public int ColumnsCount { get; set; } = 4;
    public int FontSize { get; set; } = 16;
    public int WarningThresholdMinutes { get; set; } = 10;
    public int AlertThresholdMinutes { get; set; } = 15;
    public int GreenThresholdMinutes { get; set; } = 5;
    public bool ShowModifiers { get; set; } = true;
    public bool ShowSpecialInstructions { get; set; } = true;
    public bool AudioAlerts { get; set; } = true;
    public bool FlashWhenOverdue { get; set; } = true;
    public int FlashIntervalSeconds { get; set; } = 2;
    public int AudioRepeatIntervalSeconds { get; set; } = 30;
    public int RecallWindowMinutes { get; set; } = 10;
    public string? ThemeName { get; set; }
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// Update display settings request.
/// </summary>
public class UpdateKdsDisplaySettingsDto
{
    public int? ColumnsCount { get; set; }
    public int? FontSize { get; set; }
    public int? WarningThresholdMinutes { get; set; }
    public int? AlertThresholdMinutes { get; set; }
    public int? GreenThresholdMinutes { get; set; }
    public bool? ShowModifiers { get; set; }
    public bool? ShowSpecialInstructions { get; set; }
    public bool? AudioAlerts { get; set; }
    public bool? FlashWhenOverdue { get; set; }
    public int? FlashIntervalSeconds { get; set; }
    public int? AudioRepeatIntervalSeconds { get; set; }
    public int? RecallWindowMinutes { get; set; }
    public string? ThemeName { get; set; }
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// Query parameters for KDS stations.
/// </summary>
public class KdsStationQueryDto
{
    public int? StoreId { get; set; }
    public KdsStationTypeDto? StationType { get; set; }
    public KdsStationStatusDto? Status { get; set; }
    public bool? IsExpo { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

#endregion

#region Order DTOs

/// <summary>
/// Full KDS order details.
/// </summary>
public class KdsOrderDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string? CustomerName { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public KdsOrderStatusDto Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public OrderPriorityDto Priority { get; set; }
    public bool IsPriority { get; set; }
    public string? Notes { get; set; }
    public TimeSpan ElapsedTime => DateTime.UtcNow - ReceivedAt;
    public List<KdsOrderItemDto> Items { get; set; } = new();
    public KdsTimerStatusDto? TimerStatus { get; set; }
}

/// <summary>
/// KDS order item details.
/// </summary>
public class KdsOrderItemDto
{
    public int Id { get; set; }
    public int KdsOrderId { get; set; }
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<string> Modifiers { get; set; } = new();
    public string? SpecialInstructions { get; set; }
    public int StationId { get; set; }
    public string? StationName { get; set; }
    public KdsItemStatusDto Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SequenceNumber { get; set; }
    public int? CourseNumber { get; set; }
}

/// <summary>
/// KDS order list item for display.
/// </summary>
public class KdsOrderListDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public KdsOrderStatusDto Status { get; set; }
    public OrderPriorityDto Priority { get; set; }
    public bool IsPriority { get; set; }
    public int ItemCount { get; set; }
    public int CompletedItemCount { get; set; }
    public TimeSpan ElapsedTime => DateTime.UtcNow - ReceivedAt;
    public TimerColorDto TimerColor { get; set; }
    public bool IsOverdue { get; set; }
    public bool ShouldFlash { get; set; }
}

/// <summary>
/// Query parameters for KDS orders.
/// </summary>
public class KdsOrderQueryDto
{
    public int? StationId { get; set; }
    public int? StoreId { get; set; }
    public KdsOrderStatusDto? Status { get; set; }
    public List<KdsOrderStatusDto>? Statuses { get; set; }
    public OrderPriorityDto? Priority { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IncludeItems { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Route order to KDS request.
/// </summary>
public class RouteOrderToKdsDto
{
    public int OrderId { get; set; }
    public int StoreId { get; set; }
    public OrderPriorityDto? Priority { get; set; }
}

/// <summary>
/// Result of routing an order to KDS.
/// </summary>
public class RouteOrderResultDto
{
    public bool Success { get; set; }
    public int? KdsOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public List<KdsOrderItemRoutingDto> ItemRoutings { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Item routing result.
/// </summary>
public class KdsOrderItemRoutingDto
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
}

#endregion

#region Status Management DTOs

/// <summary>
/// Start preparation request.
/// </summary>
public class StartPreparationDto
{
    public int KdsOrderId { get; set; }
    public int StationId { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Mark item done request.
/// </summary>
public class MarkItemDoneDto
{
    public int KdsOrderItemId { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Bump order request.
/// </summary>
public class BumpOrderDto
{
    public int KdsOrderId { get; set; }
    public int StationId { get; set; }
    public int? UserId { get; set; }
    public bool PlayAudio { get; set; } = true;
}

/// <summary>
/// Recall order request.
/// </summary>
public class RecallOrderDto
{
    public int KdsOrderId { get; set; }
    public int StationId { get; set; }
    public int? UserId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Result of bumping an order.
/// </summary>
public class BumpOrderResultDto
{
    public bool Success { get; set; }
    public int KdsOrderId { get; set; }
    public KdsOrderStatusDto NewStatus { get; set; }
    public bool AllItemsDone { get; set; }
    public bool OrderComplete { get; set; }
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Result of recalling an order.
/// </summary>
public class RecallOrderResultDto
{
    public bool Success { get; set; }
    public int KdsOrderId { get; set; }
    public KdsOrderStatusDto NewStatus { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Order status log entry.
/// </summary>
public class KdsOrderStatusLogDto
{
    public int Id { get; set; }
    public int KdsOrderId { get; set; }
    public int? StationId { get; set; }
    public string? StationName { get; set; }
    public KdsOrderStatusDto PreviousStatus { get; set; }
    public KdsOrderStatusDto NewStatus { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Timer DTOs

/// <summary>
/// Timer status for an order.
/// </summary>
public class KdsTimerStatusDto
{
    public TimeSpan Elapsed { get; set; }
    public TimerColorDto Color { get; set; }
    public bool IsOverdue { get; set; }
    public bool ShouldFlash { get; set; }
    public bool ShouldPlayAudio { get; set; }
    public string DisplayTime { get; set; } = string.Empty;
}

/// <summary>
/// Timer configuration DTO.
/// </summary>
public class KdsTimerConfigDto
{
    public int Id { get; set; }
    public int GreenThresholdMinutes { get; set; }
    public int YellowThresholdMinutes { get; set; }
    public int RedThresholdMinutes { get; set; }
    public bool FlashWhenOverdue { get; set; }
    public int FlashIntervalSeconds { get; set; }
    public bool AudioAlertOnOverdue { get; set; }
    public int AudioRepeatIntervalSeconds { get; set; }
    public int StoreId { get; set; }
}

/// <summary>
/// Update timer configuration request.
/// </summary>
public class UpdateKdsTimerConfigDto
{
    public int? GreenThresholdMinutes { get; set; }
    public int? YellowThresholdMinutes { get; set; }
    public int? RedThresholdMinutes { get; set; }
    public bool? FlashWhenOverdue { get; set; }
    public int? FlashIntervalSeconds { get; set; }
    public bool? AudioAlertOnOverdue { get; set; }
    public int? AudioRepeatIntervalSeconds { get; set; }
}

/// <summary>
/// Mark order as rush/priority request.
/// </summary>
public class MarkRushOrderDto
{
    public int KdsOrderId { get; set; }
    public OrderPriorityDto Priority { get; set; } = OrderPriorityDto.Rush;
    public int? UserId { get; set; }
}

/// <summary>
/// Overdue orders summary.
/// </summary>
public class OverdueOrdersSummaryDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalOverdue { get; set; }
    public int YellowCount { get; set; }
    public int RedCount { get; set; }
    public TimeSpan OldestOrderAge { get; set; }
    public List<KdsOrderListDto> OverdueOrders { get; set; } = new();
}

#endregion

#region Expo DTOs

/// <summary>
/// Expo station order view showing all stations.
/// </summary>
public class ExpoOrderViewDto
{
    public int KdsOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public int GuestCount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public TimeSpan ElapsedTime => DateTime.UtcNow - ReceivedAt;
    public bool IsComplete { get; set; }
    public KdsOrderStatusDto Status { get; set; }
    public OrderPriorityDto Priority { get; set; }
    public TimerColorDto TimerColor { get; set; }
    public List<ExpoStationStatusDto> StationStatuses { get; set; } = new();
}

/// <summary>
/// Station status within an expo order view.
/// </summary>
public class ExpoStationStatusDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public KdsItemStatusDto Status { get; set; }
    public bool IsComplete => CompletedItems == TotalItems;
    public decimal PercentComplete => TotalItems > 0 ? (decimal)CompletedItems / TotalItems * 100 : 0;
}

/// <summary>
/// Expo display view model.
/// </summary>
public class ExpoDisplayDto
{
    public List<ExpoOrderViewDto> PendingOrders { get; set; } = new();
    public List<ExpoOrderViewDto> ReadyOrders { get; set; } = new();
    public List<AllCallMessageDto> ActiveMessages { get; set; } = new();
    public Dictionary<int, string> StationNames { get; set; } = new();
    public DateTime LastRefreshTime { get; set; }
}

/// <summary>
/// Mark order as served request.
/// </summary>
public class MarkOrderServedDto
{
    public int KdsOrderId { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Expo summary statistics.
/// </summary>
public class ExpoSummaryDto
{
    public int TotalActiveOrders { get; set; }
    public int TotalPendingOrders { get; set; }
    public int TotalInProgressOrders { get; set; }
    public int TotalReadyOrders { get; set; }
    public int TotalOverdueOrders { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan AveragePrepTime { get; set; }
    public Dictionary<int, int> OrdersByStation { get; set; } = new();
}

#endregion

#region All-Call DTOs

/// <summary>
/// All-call message DTO.
/// </summary>
public class AllCallMessageDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SentByUserId { get; set; }
    public string? SentByUserName { get; set; }
    public DateTime SentAt { get; set; }
    public AllCallPriorityDto Priority { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public int StoreId { get; set; }
    public List<int> TargetStationIds { get; set; } = new();
}

/// <summary>
/// Send all-call message request.
/// </summary>
public class SendAllCallDto
{
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int StoreId { get; set; }
    public List<int>? StationIds { get; set; }
    public AllCallPriorityDto Priority { get; set; } = AllCallPriorityDto.Normal;
    public int? ExpirationMinutes { get; set; }
}

/// <summary>
/// Dismiss all-call message request.
/// </summary>
public class DismissAllCallDto
{
    public int MessageId { get; set; }
    public int StationId { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// All-call message dismissal DTO.
/// </summary>
public class AllCallDismissalDto
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int StationId { get; set; }
    public string? StationName { get; set; }
    public DateTime DismissedAt { get; set; }
    public int? DismissedByUserId { get; set; }
    public string? DismissedByUserName { get; set; }
}

#endregion

#region Display View Models

/// <summary>
/// KDS order view model for display.
/// </summary>
public class KdsOrderViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public List<KdsOrderItemViewModel> Items { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }
    public TimerColorDto TimerColor { get; set; }
    public bool IsPriority { get; set; }
    public OrderPriorityDto Priority { get; set; }
    public bool IsFlashing { get; set; }
    public KdsOrderStatusDto Status { get; set; }
    public string DisplayTime => ElapsedTime.TotalMinutes < 60
        ? $"{(int)ElapsedTime.TotalMinutes}:{ElapsedTime.Seconds:D2}"
        : $"{(int)ElapsedTime.TotalHours}:{ElapsedTime.Minutes:D2}:{ElapsedTime.Seconds:D2}";
}

/// <summary>
/// KDS order item view model for display.
/// </summary>
public class KdsOrderItemViewModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<string> Modifiers { get; set; } = new();
    public string? SpecialInstructions { get; set; }
    public KdsItemStatusDto Status { get; set; }
    public int? CourseNumber { get; set; }
}

/// <summary>
/// KDS station display state.
/// </summary>
public class KdsStationDisplayDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public KdsStationStatusDto Status { get; set; }
    public KdsDisplaySettingsDto Settings { get; set; } = new();
    public List<KdsOrderViewModel> Orders { get; set; } = new();
    public List<AllCallMessageDto> ActiveMessages { get; set; } = new();
    public DateTime LastRefreshTime { get; set; }
}

#endregion

#region Statistics DTOs

/// <summary>
/// KDS station statistics.
/// </summary>
public class KdsStationStatsDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalOrdersToday { get; set; }
    public int CompletedOrdersToday { get; set; }
    public TimeSpan AveragePrepTime { get; set; }
    public TimeSpan FastestPrepTime { get; set; }
    public TimeSpan SlowestPrepTime { get; set; }
    public int CurrentActiveOrders { get; set; }
    public int CurrentOverdueOrders { get; set; }
}

/// <summary>
/// KDS performance summary.
/// </summary>
public class KdsPerformanceSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int OverdueOrders { get; set; }
    public decimal OnTimePercentage { get; set; }
    public TimeSpan AveragePrepTime { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public List<KdsStationStatsDto> StationStats { get; set; } = new();
}

#endregion
