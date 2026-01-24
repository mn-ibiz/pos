using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job for generating labor forecasts and updating metrics.
/// Runs daily to generate forecasts for upcoming days.
/// </summary>
public class LaborForecastingJob : BackgroundService, ILaborForecastingJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LaborForecastingJob> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(6); // Run every 6 hours
    private readonly TimeSpan _metricsInterval = TimeSpan.FromMinutes(30); // Update metrics every 30 minutes

    public LaborForecastingJobResult? LastResult { get; private set; }

    public LaborForecastingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<LaborForecastingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Labor Forecasting Job started");

        // Wait for application startup
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        var lastForecastRun = DateTime.MinValue;
        var lastMetricsRun = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Run forecast generation
                if (now - lastForecastRun >= _runInterval)
                {
                    await TriggerForecastGenerationAsync();
                    lastForecastRun = now;
                }

                // Update metrics more frequently
                if (now - lastMetricsRun >= _metricsInterval)
                {
                    await UpdateMetricsAsync();
                    lastMetricsRun = now;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in Labor Forecasting Job");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Labor Forecasting Job stopped");
    }

    public async Task TriggerForecastGenerationAsync()
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<string>();
        int storesProcessed = 0;
        int forecastsGenerated = 0;
        int issuesIdentified = 0;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();
            var forecastingService = scope.ServiceProvider.GetRequiredService<ILaborForecastingService>();

            await using var context = await contextFactory.CreateDbContextAsync();

            // Get all stores with forecasting enabled
            var configs = await context.LaborConfigurations
                .AsNoTracking()
                .Where(c => c.IsActive && c.EnableForecasting)
                .ToListAsync();

            foreach (var config in configs)
            {
                try
                {
                    storesProcessed++;
                    var fullConfig = await forecastingService.GetConfigurationAsync(config.StoreId);

                    // Generate forecasts for the next N weeks
                    var forecastDays = fullConfig.ForecastAheadWeeks * 7;
                    var startDate = DateTime.Today.AddDays(1);
                    var endDate = startDate.AddDays(forecastDays);

                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        try
                        {
                            // Check if forecast already exists
                            var existingForecast = await context.DailyLaborForecasts
                                .AsNoTracking()
                                .AnyAsync(f => f.Date.Date == date.Date &&
                                         f.StoreId == config.StoreId &&
                                         f.Status == ForecastStatus.Active);

                            if (!existingForecast)
                            {
                                var forecast = await forecastingService.ForecastDayAsync(date, config.StoreId);

                                // Auto-activate forecasts for dates more than 3 days out
                                if (date >= DateTime.Today.AddDays(3))
                                {
                                    await forecastingService.ActivateForecastAsync(forecast.Id, 0);
                                }

                                forecastsGenerated++;
                                _logger.LogDebug(
                                    "Generated forecast for store {StoreId} on {Date}",
                                    config.StoreId, date.ToShortDateString());
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Store {config.StoreId}, Date {date:d}: {ex.Message}");
                            _logger.LogWarning(ex,
                                "Failed to generate forecast for store {StoreId} on {Date}",
                                config.StoreId, date);
                        }
                    }

                    // Identify staffing issues for upcoming days
                    var issues = await IdentifyUpcomingIssuesAsync(context, forecastingService, config.StoreId);
                    issuesIdentified += issues;
                }
                catch (Exception ex)
                {
                    errors.Add($"Store {config.StoreId}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to process store {StoreId}", config.StoreId);
                }
            }

            _logger.LogInformation(
                "Labor Forecasting Job completed: {Stores} stores, {Forecasts} forecasts, {Issues} issues",
                storesProcessed, forecastsGenerated, issuesIdentified);
        }
        catch (Exception ex)
        {
            errors.Add($"General error: {ex.Message}");
            _logger.LogError(ex, "Labor Forecasting Job failed");
        }

        LastResult = new LaborForecastingJobResult
        {
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            StoresProcessed = storesProcessed,
            ForecastsGenerated = forecastsGenerated,
            IssuesIdentified = issuesIdentified,
            Errors = errors
        };
    }

    private async Task<int> IdentifyUpcomingIssuesAsync(
        POSDbContext context,
        ILaborForecastingService forecastingService,
        int storeId)
    {
        int issuesFound = 0;

        try
        {
            // Get forecasts for the next 7 days
            var forecasts = await forecastingService.GetForecastsAsync(
                DateTime.Today,
                DateTime.Today.AddDays(7),
                storeId);

            foreach (var forecast in forecasts.Where(f => f.Status == ForecastStatus.Active))
            {
                // Check for understaffing risks
                foreach (var hourly in forecast.HourlyForecasts)
                {
                    // Check if any hour has very low staff for high sales
                    if (hourly.ForecastedSales > 500 && hourly.RecommendedTotalStaff < 3)
                    {
                        var existingIssue = await context.StaffingIssues
                            .AnyAsync(i => i.StoreId == storeId &&
                                     i.IssueDateTime == hourly.Hour &&
                                     !i.IsResolved);

                        if (!existingIssue)
                        {
                            context.StaffingIssues.Add(new StaffingIssue
                            {
                                StoreId = storeId,
                                IssueDateTime = hourly.Hour,
                                IssueType = StaffingIssueType.Understaffed,
                                RecommendedStaff = hourly.RecommendedTotalStaff + 2,
                                CurrentStaff = hourly.RecommendedTotalStaff,
                                Variance = -2,
                                ImpactEstimate = hourly.ForecastedSales * 0.05m,
                                Recommendation = "Consider adding extra staff during peak hour",
                                IsResolved = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            issuesFound++;
                        }
                    }
                }

                // Check for key role coverage
                foreach (var shift in forecast.RecommendedShifts)
                {
                    if (shift.HeadCount == 0)
                    {
                        var existingIssue = await context.StaffingIssues
                            .AnyAsync(i => i.StoreId == storeId &&
                                     i.IssueDateTime.Date == forecast.Date.Date &&
                                     i.IssueType == StaffingIssueType.KeyRoleMissing &&
                                     i.RoleName == shift.Role &&
                                     !i.IsResolved);

                        if (!existingIssue)
                        {
                            context.StaffingIssues.Add(new StaffingIssue
                            {
                                StoreId = storeId,
                                IssueDateTime = forecast.Date.Date.Add(shift.StartTime.ToTimeSpan()),
                                IssueType = StaffingIssueType.KeyRoleMissing,
                                RoleName = shift.Role,
                                RecommendedStaff = 1,
                                CurrentStaff = 0,
                                Variance = -1,
                                Recommendation = $"Ensure {shift.Role} coverage for {shift.StartTime:t}-{shift.EndTime:t}",
                                IsResolved = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            issuesFound++;
                        }
                    }
                }
            }

            if (issuesFound > 0)
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to identify issues for store {StoreId}", storeId);
        }

        return issuesFound;
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();
            var forecastingService = scope.ServiceProvider.GetRequiredService<ILaborForecastingService>();

            await using var context = await contextFactory.CreateDbContextAsync();

            // Get all stores with forecasting enabled
            var storeIds = await context.LaborConfigurations
                .AsNoTracking()
                .Where(c => c.IsActive && c.EnableForecasting)
                .Select(c => c.StoreId)
                .ToListAsync();

            // Update metrics for today
            foreach (var storeId in storeIds)
            {
                try
                {
                    await forecastingService.UpdateDailyMetricsAsync(DateTime.Today, storeId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update metrics for store {StoreId}", storeId);
                }
            }

            _logger.LogDebug("Updated labor metrics for {Count} stores", storeIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update labor metrics");
        }
    }
}

/// <summary>
/// Background job for archiving old forecasts and cleaning up data.
/// Runs weekly to maintain data hygiene.
/// </summary>
public class LaborForecastCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LaborForecastCleanupJob> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromDays(1); // Run daily
    private readonly int _archiveAfterDays = 30;
    private readonly int _deleteAfterDays = 365;

    public LaborForecastCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<LaborForecastCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Labor Forecast Cleanup Job started");

        // Wait for application startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldDataAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in Labor Forecast Cleanup Job");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Labor Forecast Cleanup Job stopped");
    }

    private async Task CleanupOldDataAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();

        await using var context = await contextFactory.CreateDbContextAsync();

        var archiveDate = DateTime.Today.AddDays(-_archiveAfterDays);
        var deleteDate = DateTime.Today.AddDays(-_deleteAfterDays);

        // Archive old active forecasts
        var forecastsToArchive = await context.DailyLaborForecasts
            .Where(f => f.Date < archiveDate && f.Status == ForecastStatus.Active)
            .ToListAsync();

        foreach (var forecast in forecastsToArchive)
        {
            forecast.Status = ForecastStatus.Archived;
            forecast.UpdatedAt = DateTime.UtcNow;
        }

        // Resolve old staffing issues
        var issuesToResolve = await context.StaffingIssues
            .Where(i => i.IssueDateTime < archiveDate && !i.IsResolved)
            .ToListAsync();

        foreach (var issue in issuesToResolve)
        {
            issue.IsResolved = true;
            issue.ResolvedAt = DateTime.UtcNow;
            issue.UpdatedAt = DateTime.UtcNow;
        }

        // Delete very old data
        var forecastsToDelete = await context.DailyLaborForecasts
            .Where(f => f.Date < deleteDate)
            .ToListAsync();

        context.DailyLaborForecasts.RemoveRange(forecastsToDelete);

        var suggestionsToDelete = await context.OptimizationSuggestions
            .Where(s => s.ScheduleDate < deleteDate)
            .ToListAsync();

        context.OptimizationSuggestions.RemoveRange(suggestionsToDelete);

        var issuesToDelete = await context.StaffingIssues
            .Where(i => i.IssueDateTime < deleteDate)
            .ToListAsync();

        context.StaffingIssues.RemoveRange(issuesToDelete);

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleanup completed: {Archived} archived, {Resolved} resolved, {Deleted} deleted",
            forecastsToArchive.Count, issuesToResolve.Count,
            forecastsToDelete.Count + suggestionsToDelete.Count + issuesToDelete.Count);
    }
}
