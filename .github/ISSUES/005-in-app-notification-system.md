# feat: Create In-App Notification System

**Labels:** `enhancement` `frontend` `backend` `notifications` `priority-high`

## Overview

Implement a comprehensive in-app notification system for the WPF application that alerts users to important events like low stock, PO generation, pending approvals, and overdue POs. The system should support both toast notifications and a notification center.

## Background

Research indicates several WPF notification libraries are available:
- **Notifications.Wpf.Core** - Modern, .NET 6+ compatible
- **ToastNotifications** - Feature-rich with customization
- **HandyControl** - Part of larger UI library

This feature requires:
1. Backend notification service to create/store notifications
2. Toast UI component for real-time alerts
3. Notification center for viewing history

## Requirements

### Notification Entity

```csharp
public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
    public int? UserId { get; set; } // null = all users
    public string? ActionUrl { get; set; } // e.g., "purchaseorder:123"
    public string? ActionLabel { get; set; } // e.g., "View PO"
    public string? RelatedEntityType { get; set; } // e.g., "PurchaseOrder"
    public int? RelatedEntityId { get; set; }
}

public enum NotificationType
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Success = 4,
    LowStock = 10,
    POGenerated = 11,
    POPending = 12,
    POSent = 13,
    POOverdue = 14,
    POApproved = 15,
    POReceived = 16
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}
```

### Notification Service Interface

```csharp
public interface INotificationService
{
    // Create notifications
    Task<Notification> CreateNotificationAsync(Notification notification);
    Task NotifyLowStockAsync(List<Product> products);
    Task NotifyPOGeneratedAsync(PurchaseOrder po);
    Task NotifyPOsPendingApprovalAsync(int count);
    Task NotifyPOSentAsync(PurchaseOrder po);
    Task NotifyPOOverdueAsync(PurchaseOrder po);
    Task NotifyReorderSuggestionsGeneratedAsync(List<ReorderSuggestion> suggestions);

    // Read notifications
    Task<List<Notification>> GetUnreadNotificationsAsync(int? userId = null);
    Task<List<Notification>> GetAllNotificationsAsync(int? userId = null, int limit = 50);
    Task<int> GetUnreadCountAsync(int? userId = null);

    // Update notifications
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int? userId = null);
    Task DeleteNotificationAsync(int notificationId);
    Task DeleteOldNotificationsAsync(int daysOld = 30);

    // Events for real-time UI updates
    event EventHandler<NotificationEventArgs>? NotificationReceived;
}

public class NotificationEventArgs : EventArgs
{
    public Notification Notification { get; set; } = null!;
}
```

### Toast Notification UI

Use **Notifications.Wpf.Core** or similar library:

```xml
<!-- In MainWindow.xaml -->
<Window ...>
    <Grid>
        <!-- Main content -->
        <ContentControl Content="{Binding CurrentView}" />

        <!-- Notification area -->
        <notifications:NotificationArea
            x:Name="NotificationArea"
            Position="TopRight"
            MaxItems="5" />
    </Grid>
</Window>
```

```csharp
// NotificationDisplayService.cs
public class NotificationDisplayService : INotificationDisplayService
{
    private readonly INotificationManager _notificationManager;

    public void ShowToast(Notification notification)
    {
        var content = new NotificationContent
        {
            Title = notification.Title,
            Message = notification.Message,
            Type = MapToNotificationType(notification.Type)
        };

        _notificationManager.Show(content, onClick: () =>
        {
            if (!string.IsNullOrEmpty(notification.ActionUrl))
            {
                NavigateToAction(notification.ActionUrl);
            }
        });
    }
}
```

### Notification Center UI

Create a flyout/sidebar for viewing all notifications:

```
+------------------------------------------+
| NOTIFICATIONS                      [Mark All Read] |
+------------------------------------------+
| [!] 3 POs pending approval          2m ago |
|     Click to review                        |
+------------------------------------------+
| [i] PO-20250123-001 created        15m ago |
|     10 items for ABC Supplier              |
|     [View PO]                              |
+------------------------------------------+
| [!] Low stock alert                  1h ago |
|     5 products below reorder point         |
|     [View Products]                        |
+------------------------------------------+
| [✓] PO-20250122-015 received     Yesterday |
|     All items received                     |
+------------------------------------------+
```

### Notification Badge

Add a badge to the main navigation showing unread count:

```xml
<Button Command="{Binding OpenNotificationsCommand}">
    <Grid>
        <materialDesign:PackIcon Kind="Bell" />
        <Border Visibility="{Binding HasUnreadNotifications}"
                Background="Red" CornerRadius="8"
                Padding="4,2" HorizontalAlignment="Right"
                VerticalAlignment="Top" Margin="0,-5,-5,0">
            <TextBlock Text="{Binding UnreadCount}"
                       Foreground="White" FontSize="10" />
        </Border>
    </Grid>
</Button>
```

## Acceptance Criteria

### Backend Service
- [ ] `Notification` entity created with proper properties
- [ ] `INotificationService` implemented with all methods
- [ ] Notifications stored in database
- [ ] Old notifications auto-deleted (configurable days)
- [ ] Service registered in DI container

### Toast Notifications
- [ ] Toast appears for new notifications when app is open
- [ ] Toast shows title, message, and icon based on type
- [ ] Toast auto-dismisses after configurable time (default 5 seconds)
- [ ] Clicking toast navigates to related item (if ActionUrl set)
- [ ] Toast can be manually dismissed
- [ ] Maximum 5 toasts visible at once

### Notification Center
- [ ] Opens from bell icon in navigation
- [ ] Shows all notifications, newest first
- [ ] Unread notifications visually distinguished
- [ ] Click notification to navigate and mark as read
- [ ] "Mark All Read" button works
- [ ] Infinite scroll or pagination for many notifications
- [ ] Empty state when no notifications

### Badge
- [ ] Badge shows unread count
- [ ] Badge updates in real-time when new notification arrives
- [ ] Badge hidden when count is 0
- [ ] Badge shows "99+" for counts over 99

### PO-Specific Notifications
- [ ] "Low Stock Alert" when products below reorder point
- [ ] "PO Generated" when auto-PO created
- [ ] "POs Pending Approval" summary notification
- [ ] "PO Sent" when PO emailed to supplier
- [ ] "PO Overdue" when expected date passed

### Settings Integration
- [ ] Respects `NotifyOnLowStock` setting
- [ ] Respects `NotifyOnPOGenerated` setting
- [ ] Respects `NotifyOnPOSent` setting
- [ ] Toast can be disabled in settings (still shows in center)

## Technical Notes

### Real-Time Updates

Use `INotificationService.NotificationReceived` event:

```csharp
public class MainViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;

    public MainViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.NotificationReceived += OnNotificationReceived;
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UnreadCount++;
            _displayService.ShowToast(e.Notification);
        });
    }
}
```

### NuGet Package

Add to project:
```xml
<PackageReference Include="Notifications.Wpf.Core" Version="1.3.2" />
```

### Database Migration

```csharp
public partial class AddNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(maxLength: 200, nullable: false),
                Message = table.Column<string>(maxLength: 1000, nullable: false),
                Type = table.Column<int>(nullable: false),
                Priority = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                ReadAt = table.Column<DateTime>(nullable: true),
                UserId = table.Column<int>(nullable: true),
                ActionUrl = table.Column<string>(maxLength: 500, nullable: true),
                ActionLabel = table.Column<string>(maxLength: 100, nullable: true),
                RelatedEntityType = table.Column<string>(maxLength: 50, nullable: true),
                RelatedEntityId = table.Column<int>(nullable: true)
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_ReadAt",
            table: "Notifications",
            columns: new[] { "UserId", "ReadAt" });
    }
}
```

## Test Cases

1. **Create notification** - Saved to database, event fired
2. **Toast display** - Toast shown with correct content
3. **Toast click** - Navigates to ActionUrl, marks as read
4. **Notification center** - Shows all notifications correctly
5. **Mark as read** - Updates ReadAt, decrements badge
6. **Mark all read** - All notifications marked, badge cleared
7. **Badge count** - Updates in real-time
8. **Setting disabled** - No toast shown, still saved to database
9. **Old cleanup** - Notifications older than threshold deleted

## Dependencies
- Issue #003: System Configuration (notification settings)

## Blocked By
- None (can be developed in parallel)

## Blocks
- Issue #002: Stock Monitoring Background Service (sends notifications)

## Estimated Complexity
**Medium-High** - UI components plus backend service with real-time updates
