# Story 27.4: Order Timer and Priorities

## Story
**As a** kitchen manager,
**I want to** see order age timers and priority indicators,
**So that** no orders are forgotten or delayed.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 27: Kitchen Display System (KDS)**

## Acceptance Criteria

### AC1: Timer Display
**Given** order is displayed
**When** time elapses
**Then** timer shows age, color changes (green → yellow → red)

### AC2: Rush Order Priority
**Given** rush order is submitted
**When** displaying
**Then** shows priority flag and appears at top of queue

### AC3: Overdue Alerts
**Given** order exceeds time threshold
**When** alerting
**Then** visual/audio alert for overdue orders

## Technical Notes
```csharp
public class KdsTimerConfig
{
    public int GreenThresholdMinutes { get; set; } = 5;   // 0-5 min = green
    public int YellowThresholdMinutes { get; set; } = 10; // 5-10 min = yellow
    public int RedThresholdMinutes { get; set; } = 15;    // >10 min = red
    public bool FlashWhenOverdue { get; set; } = true;
    public int FlashIntervalSeconds { get; set; } = 2;
    public bool AudioAlertOnOverdue { get; set; } = true;
    public int AudioRepeatIntervalSeconds { get; set; } = 30;
}

public enum OrderPriority
{
    Normal = 0,
    Rush = 1,
    VIP = 2
}

public enum TimerColor
{
    Green,   // On time
    Yellow,  // Warning
    Red      // Overdue
}
```

## Definition of Done
- [x] Real-time timer display (updates every second)
- [x] Configurable color thresholds
- [x] Rush/VIP priority flagging
- [x] Priority orders sorted to top
- [x] Flashing animation for overdue orders
- [x] Audio alerts with configurable intervals
- [x] Priority indicator icons
- [x] Timer configuration UI
- [x] Unit tests passing

## Implementation Summary

### Entities Created (KdsEntities.cs)
- **KdsTimerConfig**: Store-level timer configuration with GreenThresholdMinutes, YellowThresholdMinutes, RedThresholdMinutes, FlashWhenOverdue, FlashIntervalSeconds, EnableAudioAlerts, AudioAlertIntervalSeconds
- **OrderPriority enum**: Normal, Rush, VIP
- **TimerColor enum**: Green, Yellow, Red

### DTOs Created (KdsDtos.cs)
- KdsTimerStatusDto with ElapsedSeconds, CurrentColor, IsOverdue, ShouldFlash, ShouldPlayAudio, TimeUntilNextThreshold
- KdsTimerConfigDto, UpdateTimerConfigDto for configuration
- OrderPriorityDto enum matching entity
- KdsOrderPriorityChangeEventArgs for events
- AudioAlertEventArgs for audio triggers
- OrderTimerStatisticsDto for analytics

### Service Implementation (KdsTimerService.cs ~450 lines)
- **GetOrderTimerStatusAsync**: Calculates elapsed time, determines color based on thresholds, sets flash/audio flags
- **SetOrderPriorityAsync**: Updates order priority (Normal/Rush/VIP), sets IsPriority flag
- **GetOverdueOrdersAsync**: Returns orders exceeding red threshold
- **GetTimerConfigurationAsync**: Retrieves store timer config
- **UpdateTimerConfigurationAsync**: Creates/updates timer thresholds
- **CheckAndTriggerAlertsAsync**: Periodic check for overdue orders, triggers AudioAlertNeeded event
- **GetAverageCompletionTimeAsync**: Statistics on order completion times
- **GetOrdersApproachingThresholdAsync**: Early warning for orders about to go yellow/red
- Event-driven notifications (OrderOverdue, PriorityChanged, AudioAlertNeeded)

### Unit Tests (KdsTimerServiceTests.cs ~20 tests)
- Constructor null argument validation
- GetOrderTimerStatusAsync tests (green/yellow/red status, invalid order)
- SetOrderPriorityAsync tests (Normal→Rush, VIP priority)
- GetOverdueOrdersAsync tests
- GetTimerConfigurationAsync tests (existing/no config)
- UpdateTimerConfigurationAsync tests (update existing, create new)
- CheckAndTriggerAlertsAsync tests with event verification
- GetAverageCompletionTimeAsync tests
