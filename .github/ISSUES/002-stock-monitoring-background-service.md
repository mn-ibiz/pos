# feat: Create Stock Monitoring Background Service

**Labels:** `enhancement` `backend` `inventory` `automation` `priority-high`

## Overview

Create a background service that periodically monitors inventory levels and triggers reorder suggestion generation. This service runs as a hosted service within the WPF application and provides the automation backbone for the auto-PO generation feature.

## Background

The codebase already has background jobs in `QuickTechPOS/BackgroundJobs/`:
- `ExpireBatchesJob.cs`
- `ExpirePointsJob.cs`
- `ExpireReservationsJob.cs`

A similar pattern should be used for stock monitoring, but with configurable intervals and smart execution logic.

## Requirements

### Background Service Implementation

Create `QuickTechPOS/BackgroundJobs/StockMonitoringJob.cs`:

```csharp
public class StockMonitoringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockMonitoringJob> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _timer;

    public StockMonitoringJob(
        IServiceScopeFactory scopeFactory,
        ILogger<StockMonitoringJob> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>("StockMonitoring:IntervalMinutes", 15);

        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.FromMinutes(1), // Initial delay
            TimeSpan.FromMinutes(intervalMinutes));

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = _scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IInventoryAnalyticsService>();
        var configService = scope.ServiceProvider.GetRequiredService<ISystemConfigurationService>();

        try
        {
            // Check if auto-generation is enabled
            var config = await configService.GetConfigurationAsync();
            if (!config.AutoGeneratePurchaseOrders)
            {
                _logger.LogDebug("Auto-generate POs is disabled, skipping stock check");
                return;
            }

            _logger.LogInformation("Starting stock level check...");

            // Generate reorder suggestions
            var suggestions = await analyticsService.GenerateReorderSuggestionsAsync(storeId: null);

            if (suggestions.Any())
            {
                _logger.LogInformation("Generated {Count} reorder suggestions", suggestions.Count);

                // Trigger notification
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.NotifyReorderSuggestionsGeneratedAsync(suggestions);

                // If auto-send is enabled, create and send POs
                if (config.AutoSendPurchaseOrders)
                {
                    await CreateAndSendPurchaseOrdersAsync(scope.ServiceProvider, suggestions);
                }
            }

            _logger.LogInformation("Stock level check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stock monitoring");
        }
    }
}
```

### Configuration Settings

Add to `appsettings.json`:
```json
{
  "StockMonitoring": {
    "IntervalMinutes": 15,
    "EnabledHoursStart": 6,
    "EnabledHoursEnd": 22,
    "RunOnWeekends": false,
    "MaxSuggestionsPerRun": 100
  }
}
```

### Smart Scheduling Features

1. **Business Hours Only** - Only run during configured hours
2. **Weekend Handling** - Optionally skip weekends
3. **Throttling** - Maximum suggestions per run to prevent overwhelming
4. **Last Run Tracking** - Store last successful run time
5. **Backoff on Errors** - Exponential backoff if errors occur

```csharp
private bool ShouldRunNow()
{
    var now = DateTime.Now;
    var config = _configuration.GetSection("StockMonitoring");

    // Check business hours
    var startHour = config.GetValue<int>("EnabledHoursStart", 6);
    var endHour = config.GetValue<int>("EnabledHoursEnd", 22);
    if (now.Hour < startHour || now.Hour >= endHour)
        return false;

    // Check weekends
    var runOnWeekends = config.GetValue<bool>("RunOnWeekends", false);
    if (!runOnWeekends && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday))
        return false;

    return true;
}
```

### Manual Trigger Support

Add method to allow manual triggering from UI:
```csharp
public interface IStockMonitoringService
{
    Task<List<ReorderSuggestion>> RunStockCheckNowAsync();
    DateTime? GetLastRunTime();
    bool IsRunning { get; }
}
```

## Acceptance Criteria

### Core Functionality
- [ ] Background service starts with application
- [ ] Service runs at configurable intervals (default: 15 minutes)
- [ ] Service calls `IInventoryAnalyticsService.GenerateReorderSuggestionsAsync()`
- [ ] Service respects `AutoGeneratePurchaseOrders` system setting (skip if disabled)
- [ ] Service triggers notification when suggestions are generated
- [ ] Service can optionally auto-create POs based on `AutoSendPurchaseOrders` setting

### Scheduling
- [ ] Service only runs during configured business hours
- [ ] Service optionally skips weekends
- [ ] Service tracks last run time
- [ ] Service uses exponential backoff on repeated errors

### Manual Control
- [ ] Manual trigger available via `IStockMonitoringService`
- [ ] Manual trigger can be called from UI (button in PurchaseOrders view)
- [ ] Running status is exposed for UI feedback

### Error Handling
- [ ] All exceptions are caught and logged
- [ ] Errors don't crash the application
- [ ] After 3 consecutive errors, service waits longer before retry
- [ ] Error count resets on successful run

### Logging
- [ ] Log when check starts
- [ ] Log number of suggestions generated
- [ ] Log when check completes
- [ ] Log errors with full exception details
- [ ] Log when skipping due to disabled setting
- [ ] Log when skipping due to business hours

### Registration
- [ ] Service registered as `AddHostedService<StockMonitoringJob>()`
- [ ] `IStockMonitoringService` registered for manual trigger access

## Technical Notes

### Service Registration in App.xaml.cs or Startup

```csharp
services.AddHostedService<StockMonitoringJob>();
services.AddSingleton<IStockMonitoringService>(sp =>
    sp.GetServices<IHostedService>()
      .OfType<StockMonitoringJob>()
      .First());
```

### Thread Safety Considerations
- Use `SemaphoreSlim` to prevent concurrent runs
- Use `Interlocked` for status flags
- Scoped services created fresh for each run

### WPF Application Hosting
Since this is a WPF app (not ASP.NET Core), ensure the generic host is set up:

```csharp
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // ... other services
                services.AddHostedService<StockMonitoringJob>();
            })
            .Build();

        await _host.StartAsync();
        base.OnStartup(e);
    }
}
```

## Test Cases

1. **Normal execution** - Service runs, generates suggestions, logs success
2. **Auto-generate disabled** - Service skips and logs "disabled" message
3. **No low stock products** - Service runs, generates 0 suggestions, logs completion
4. **Outside business hours** - Service skips execution
5. **Weekend with RunOnWeekends=false** - Service skips execution
6. **Error during execution** - Error logged, service continues on next interval
7. **Manual trigger** - Runs immediately, returns suggestions
8. **Concurrent manual + scheduled** - Only one runs at a time

## Dependencies
- Issue #001: IInventoryAnalyticsService (must be implemented first)
- Issue #003: System Configuration Settings (for reading config)
- Issue #005: Notification Service (for sending alerts)

## Blocked By
- Issue #001: Implement IInventoryAnalyticsService

## Blocks
- None (other features can work with manual trigger while this provides automation)

## Estimated Complexity
**Medium** - Standard background service pattern with scheduling logic
