using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for labor forecasting and schedule optimization.
/// </summary>
public class LaborForecastingService : ILaborForecastingService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<LaborForecastingService> _logger;

    public LaborForecastingService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<LaborForecastingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Configuration

    public async Task<LaborConfigurationDto> GetConfigurationAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.LaborConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.IsActive);

        if (config == null)
        {
            // Return default configuration
            return new LaborConfigurationDto
            {
                StoreId = storeId,
                TargetLaborPercent = 25m,
                TargetSPLH = 50m,
                MinStaffPerShift = 2,
                MaxStaffPerShift = 20,
                OvertimeThresholdHours = 40,
                OvertimeMultiplier = 1.5m,
                MinShiftHours = 4,
                MaxShiftHours = 10,
                MinHoursBetweenShifts = 8,
                EnableForecasting = true,
                ForecastHistoryDays = 90,
                ForecastAheadWeeks = 2
            };
        }

        var roles = await context.LaborRoleConfigurations
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        return new LaborConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            TargetLaborPercent = config.TargetLaborPercent,
            TargetSPLH = config.TargetSPLH,
            MinStaffPerShift = config.MinStaffPerShift,
            MaxStaffPerShift = config.MaxStaffPerShift,
            OvertimeThresholdHours = config.OvertimeThresholdHours,
            OvertimeMultiplier = config.OvertimeMultiplier,
            MinShiftHours = config.MinShiftHours,
            MaxShiftHours = config.MaxShiftHours,
            MinHoursBetweenShifts = config.MinHoursBetweenShifts,
            EnableForecasting = config.EnableForecasting,
            ForecastHistoryDays = config.ForecastHistoryDays,
            ForecastAheadWeeks = config.ForecastAheadWeeks,
            Roles = roles.Select(r => new LaborRoleConfigurationDto
            {
                Id = r.Id,
                RoleName = r.RoleName,
                HourlyRate = r.HourlyRate,
                MinStaff = r.MinStaff,
                MaxStaff = r.MaxStaff,
                TransactionsPerHour = r.TransactionsPerHour,
                IsRequiredRole = r.IsRequiredRole,
                DisplayOrder = r.DisplayOrder
            }).ToList()
        };
    }

    public async Task<LaborConfigurationDto> UpdateConfigurationAsync(LaborConfigurationDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.LaborConfigurations
            .FirstOrDefaultAsync(c => c.StoreId == dto.StoreId && c.IsActive);

        if (config == null)
        {
            config = new LaborConfiguration
            {
                StoreId = dto.StoreId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.LaborConfigurations.Add(config);
        }

        config.TargetLaborPercent = dto.TargetLaborPercent;
        config.TargetSPLH = dto.TargetSPLH;
        config.MinStaffPerShift = dto.MinStaffPerShift;
        config.MaxStaffPerShift = dto.MaxStaffPerShift;
        config.OvertimeThresholdHours = dto.OvertimeThresholdHours;
        config.OvertimeMultiplier = dto.OvertimeMultiplier;
        config.MinShiftHours = dto.MinShiftHours;
        config.MaxShiftHours = dto.MaxShiftHours;
        config.MinHoursBetweenShifts = dto.MinHoursBetweenShifts;
        config.EnableForecasting = dto.EnableForecasting;
        config.ForecastHistoryDays = dto.ForecastHistoryDays;
        config.ForecastAheadWeeks = dto.ForecastAheadWeeks;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        dto.Id = config.Id;

        _logger.LogInformation("Updated labor configuration for store {StoreId}", dto.StoreId);
        return dto;
    }

    public async Task<List<LaborRoleConfigurationDto>> GetRoleConfigurationsAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var roles = await context.LaborRoleConfigurations
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        return roles.Select(r => new LaborRoleConfigurationDto
        {
            Id = r.Id,
            RoleName = r.RoleName,
            HourlyRate = r.HourlyRate,
            MinStaff = r.MinStaff,
            MaxStaff = r.MaxStaff,
            TransactionsPerHour = r.TransactionsPerHour,
            IsRequiredRole = r.IsRequiredRole,
            DisplayOrder = r.DisplayOrder
        }).ToList();
    }

    public async Task<LaborRoleConfigurationDto> UpdateRoleConfigurationAsync(int storeId, LaborRoleConfigurationDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var role = dto.Id > 0
            ? await context.LaborRoleConfigurations.FindAsync(dto.Id)
            : null;

        if (role == null)
        {
            role = new LaborRoleConfiguration
            {
                StoreId = storeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.LaborRoleConfigurations.Add(role);
        }

        role.RoleName = dto.RoleName;
        role.HourlyRate = dto.HourlyRate;
        role.MinStaff = dto.MinStaff;
        role.MaxStaff = dto.MaxStaff;
        role.TransactionsPerHour = dto.TransactionsPerHour;
        role.IsRequiredRole = dto.IsRequiredRole;
        role.DisplayOrder = dto.DisplayOrder;
        role.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        dto.Id = role.Id;

        return dto;
    }

    public async Task<bool> IsForecastingEnabledAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.LaborConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.IsActive);

        return config?.EnableForecasting ?? false;
    }

    #endregion

    #region Forecasting

    public async Task<List<HourlyLaborForecastDto>> ForecastLaborNeedsAsync(DateTime date, int storeId)
    {
        var dailyForecast = await ForecastDayAsync(date, storeId);
        return dailyForecast.HourlyForecasts;
    }

    public async Task<WeeklyLaborForecastDto> ForecastWeekAsync(DateTime weekStart, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        var weekEnd = weekStart.AddDays(6);

        var dailyForecasts = new List<DailyLaborForecastDto>();
        for (var day = weekStart; day <= weekEnd; day = day.AddDays(1))
        {
            var forecast = await ForecastDayAsync(day, storeId);
            dailyForecasts.Add(forecast);
        }

        var roleSummary = new WeeklySummaryByRole();
        foreach (var daily in dailyForecasts)
        {
            foreach (var hourly in daily.HourlyForecasts)
            {
                foreach (var (role, count) in hourly.StaffByRole)
                {
                    if (!roleSummary.HoursByRole.ContainsKey(role))
                        roleSummary.HoursByRole[role] = 0;
                    roleSummary.HoursByRole[role] += count;

                    var roleConfig = config.Roles.FirstOrDefault(r => r.RoleName == role);
                    if (roleConfig != null)
                    {
                        if (!roleSummary.CostByRole.ContainsKey(role))
                            roleSummary.CostByRole[role] = 0;
                        roleSummary.CostByRole[role] += count * roleConfig.HourlyRate;
                    }
                }
            }
        }

        return new WeeklyLaborForecastDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            StoreId = storeId,
            TotalForecastedSales = dailyForecasts.Sum(d => d.TotalForecastedSales),
            TotalLaborHours = dailyForecasts.Sum(d => d.TotalLaborHoursNeeded),
            TotalLaborCost = dailyForecasts.Sum(d => d.TotalLaborCost),
            AverageLaborPercent = dailyForecasts.Average(d => d.ForecastedLaborPercent),
            AverageConfidence = dailyForecasts.Average(d => d.ConfidenceLevel),
            DailyForecasts = dailyForecasts,
            RoleSummary = roleSummary
        };
    }

    public async Task<DailyLaborForecastDto> ForecastDayAsync(DateTime date, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check for existing forecast
        var existing = await context.DailyLaborForecasts
            .AsNoTracking()
            .Include(f => f.HourlyForecasts)
                .ThenInclude(h => h.RoleForecasts)
            .Include(f => f.ShiftRecommendations)
            .FirstOrDefaultAsync(f => f.Date.Date == date.Date && f.StoreId == storeId && f.Status == ForecastStatus.Active);

        if (existing != null)
        {
            return MapToDto(existing);
        }

        // Generate new forecast
        var config = await GetConfigurationAsync(storeId);
        var historicalData = await GetHistoricalSalesDataAsync(context, date, storeId, config.ForecastHistoryDays);

        var forecast = new DailyLaborForecast
        {
            Date = date.Date,
            StoreId = storeId,
            Status = ForecastStatus.Draft,
            GeneratedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var hourlyForecasts = new List<HourlyLaborForecast>();
        decimal totalSales = 0;
        decimal totalHours = 0;
        decimal totalCost = 0;

        // Generate forecasts for each operating hour (6 AM to 11 PM typical)
        for (int hour = 6; hour <= 22; hour++)
        {
            var hourlyForecast = await GenerateHourlyForecastAsync(
                context, date, hour, storeId, config, historicalData);
            hourlyForecasts.Add(hourlyForecast);

            totalSales += hourlyForecast.ForecastedSales;
            totalHours += hourlyForecast.RecommendedTotalStaff; // Staff hours
            totalCost += hourlyForecast.LaborCostEstimate;
        }

        forecast.TotalForecastedSales = totalSales;
        forecast.TotalLaborHoursNeeded = totalHours;
        forecast.TotalLaborCost = totalCost;
        forecast.ForecastedLaborPercent = totalSales > 0 ? (totalCost / totalSales) * 100 : 0;
        forecast.ConfidenceLevel = CalculateOverallConfidence(hourlyForecasts);
        forecast.SpecialFactors = JsonSerializer.Serialize(GetSpecialFactors(date));
        forecast.HourlyForecasts = hourlyForecasts;

        // Generate shift recommendations
        var shiftRecommendations = GenerateShiftRecommendations(hourlyForecasts, config);
        forecast.ShiftRecommendations = shiftRecommendations;

        context.DailyLaborForecasts.Add(forecast);
        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Generated labor forecast for store {StoreId} on {Date}: ${Sales:N0} sales, {Hours:N1} labor hours",
            storeId, date.ToShortDateString(), totalSales, totalHours);

        return MapToDto(forecast);
    }

    public async Task<DailyLaborForecastDto?> GetForecastAsync(int forecastId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var forecast = await context.DailyLaborForecasts
            .AsNoTracking()
            .Include(f => f.HourlyForecasts)
                .ThenInclude(h => h.RoleForecasts)
            .Include(f => f.ShiftRecommendations)
            .FirstOrDefaultAsync(f => f.Id == forecastId);

        return forecast == null ? null : MapToDto(forecast);
    }

    public async Task<List<DailyLaborForecastDto>> GetForecastsAsync(DateTime from, DateTime to, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var forecasts = await context.DailyLaborForecasts
            .AsNoTracking()
            .Include(f => f.HourlyForecasts)
                .ThenInclude(h => h.RoleForecasts)
            .Include(f => f.ShiftRecommendations)
            .Where(f => f.StoreId == storeId && f.Date >= from && f.Date <= to)
            .OrderBy(f => f.Date)
            .ToListAsync();

        return forecasts.Select(MapToDto).ToList();
    }

    public async Task<DailyLaborForecastDto> ActivateForecastAsync(int forecastId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var forecast = await context.DailyLaborForecasts
            .Include(f => f.HourlyForecasts)
            .Include(f => f.ShiftRecommendations)
            .FirstOrDefaultAsync(f => f.Id == forecastId);

        if (forecast == null)
            throw new InvalidOperationException($"Forecast {forecastId} not found");

        // Supersede existing active forecast for same date
        var existing = await context.DailyLaborForecasts
            .Where(f => f.Date == forecast.Date && f.StoreId == forecast.StoreId &&
                   f.Status == ForecastStatus.Active && f.Id != forecastId)
            .ToListAsync();

        foreach (var old in existing)
        {
            old.Status = ForecastStatus.Superseded;
            old.UpdatedAt = DateTime.UtcNow;
        }

        forecast.Status = ForecastStatus.Active;
        forecast.GeneratedByUserId = userId;
        forecast.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Activated forecast {ForecastId} for store {StoreId} on {Date}",
            forecastId, forecast.StoreId, forecast.Date.ToShortDateString());

        return MapToDto(forecast);
    }

    public async Task<DailyLaborForecastDto> RegenerateForecastAsync(int forecastId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var oldForecast = await context.DailyLaborForecasts
            .FirstOrDefaultAsync(f => f.Id == forecastId);

        if (oldForecast == null)
            throw new InvalidOperationException($"Forecast {forecastId} not found");

        oldForecast.Status = ForecastStatus.Superseded;
        oldForecast.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return await ForecastDayAsync(oldForecast.Date, oldForecast.StoreId);
    }

    #endregion

    #region Schedule Recommendations

    public async Task<ScheduleRecommendationDto> GenerateScheduleRecommendationAsync(
        DateTime date, int storeId, List<int>? availableEmployeeIds = null)
    {
        var forecast = await ForecastDayAsync(date, storeId);
        var config = await GetConfigurationAsync(storeId);

        var warnings = new List<string>();
        var notes = new List<string>();

        // Check for special factors
        if (forecast.SpecialFactors.Any())
        {
            notes.Add($"Special factors: {string.Join(", ", forecast.SpecialFactors)}");
        }

        // Validate against available employees if provided
        if (availableEmployeeIds != null)
        {
            var totalNeeded = forecast.RecommendedShifts.Sum(s => s.HeadCount);
            if (availableEmployeeIds.Count < totalNeeded)
            {
                warnings.Add($"Only {availableEmployeeIds.Count} employees available, but {totalNeeded} staff needed");
            }
        }

        // Check labor percentage
        if (forecast.ForecastedLaborPercent > config.TargetLaborPercent * 1.1m)
        {
            warnings.Add($"Forecasted labor % ({forecast.ForecastedLaborPercent:F1}%) exceeds target ({config.TargetLaborPercent:F1}%)");
        }

        return new ScheduleRecommendationDto
        {
            Date = date,
            StoreId = storeId,
            Shifts = forecast.RecommendedShifts,
            TotalLaborHours = forecast.TotalLaborHoursNeeded,
            TotalLaborCost = forecast.TotalLaborCost,
            EstimatedLaborPercent = forecast.ForecastedLaborPercent,
            Warnings = warnings,
            Notes = notes
        };
    }

    public async Task<WeeklyScheduleRecommendationDto> GenerateWeeklyScheduleAsync(DateTime weekStart, int storeId)
    {
        var dailySchedules = new List<ScheduleRecommendationDto>();

        for (var day = weekStart; day < weekStart.AddDays(7); day = day.AddDays(1))
        {
            var schedule = await GenerateScheduleRecommendationAsync(day, storeId);
            dailySchedules.Add(schedule);
        }

        var overtimeRisk = await AssessOvertimeRiskAsync(weekStart, storeId);

        return new WeeklyScheduleRecommendationDto
        {
            WeekStart = weekStart,
            StoreId = storeId,
            DailySchedules = dailySchedules,
            TotalWeeklyHours = dailySchedules.Sum(d => d.TotalLaborHours),
            TotalWeeklyCost = dailySchedules.Sum(d => d.TotalLaborCost),
            AverageDailyStaff = dailySchedules.Average(d => d.Shifts.Sum(s => s.HeadCount)),
            OvertimeRisk = overtimeRisk
        };
    }

    public async Task<List<ShiftRecommendationDto>> GetShiftRecommendationsAsync(int dailyForecastId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var shifts = await context.ShiftRecommendations
            .AsNoTracking()
            .Where(s => s.DailyForecastId == dailyForecastId)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(s => new ShiftRecommendationDto
        {
            Id = s.Id,
            Role = s.RoleName,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            HeadCount = s.HeadCount,
            EstimatedCost = s.EstimatedCost,
            Reason = s.Reason ?? string.Empty
        }).ToList();
    }

    #endregion

    #region Optimization

    public async Task<ScheduleOptimizationResultDto> OptimizeScheduleAsync(
        List<ShiftDto> currentSchedule, DateTime date, int storeId)
    {
        var forecast = await ForecastDayAsync(date, storeId);
        var config = await GetConfigurationAsync(storeId);
        var gaps = await IdentifyStaffingGapsAsync(date, storeId, currentSchedule);

        var issues = new List<StaffingIssueDto>();
        var suggestions = new List<OptimizationSuggestionDto>();
        decimal currentCost = 0;
        decimal optimizedCost = 0;

        // Calculate current labor cost
        foreach (var shift in currentSchedule)
        {
            var hours = (shift.EndTime.ToTimeSpan() - shift.StartTime.ToTimeSpan()).TotalHours;
            currentCost += shift.HourlyRate * (decimal)hours;
        }

        // Analyze each hour for issues
        foreach (var hourlyGap in gaps.HourlyGaps)
        {
            if (hourlyGap.Gap < 0) // Understaffed
            {
                issues.Add(new StaffingIssueDto
                {
                    Hour = hourlyGap.Hour,
                    Type = StaffingIssueType.Understaffed,
                    TypeDescription = "Understaffed",
                    CurrentStaff = hourlyGap.ScheduledStaff,
                    RecommendedStaff = hourlyGap.RecommendedStaff,
                    Variance = hourlyGap.Gap,
                    Severity = Math.Abs(hourlyGap.Gap) >= 3 ? "High" : Math.Abs(hourlyGap.Gap) >= 2 ? "Medium" : "Low",
                    Recommendation = $"Add {Math.Abs(hourlyGap.Gap)} staff at {hourlyGap.Hour:t}"
                });

                suggestions.Add(new OptimizationSuggestionDto
                {
                    Type = OptimizationSuggestionType.AddStaff,
                    TypeDescription = "Add Staff",
                    SuggestedValue = $"Add {Math.Abs(hourlyGap.Gap)} staff at {hourlyGap.Hour:t}",
                    EstimatedSavings = 0, // Adding staff costs money, no savings
                    Description = $"Increase staffing by {Math.Abs(hourlyGap.Gap)} to meet demand",
                    Priority = Math.Abs(hourlyGap.Gap) >= 3 ? 1 : 2
                });
            }
            else if (hourlyGap.Gap > 0) // Overstaffed
            {
                var avgRate = config.Roles.Any() ? config.Roles.Average(r => r.HourlyRate) : 15m;
                var savingsPerHour = hourlyGap.Gap * avgRate;

                issues.Add(new StaffingIssueDto
                {
                    Hour = hourlyGap.Hour,
                    Type = StaffingIssueType.Overstaffed,
                    TypeDescription = "Overstaffed",
                    CurrentStaff = hourlyGap.ScheduledStaff,
                    RecommendedStaff = hourlyGap.RecommendedStaff,
                    Variance = hourlyGap.Gap,
                    ImpactEstimate = savingsPerHour,
                    Severity = hourlyGap.Gap >= 3 ? "High" : hourlyGap.Gap >= 2 ? "Medium" : "Low",
                    Recommendation = $"Reduce {hourlyGap.Gap} staff at {hourlyGap.Hour:t}"
                });

                suggestions.Add(new OptimizationSuggestionDto
                {
                    Type = OptimizationSuggestionType.RemoveStaff,
                    TypeDescription = "Remove Staff",
                    SuggestedValue = $"Remove {hourlyGap.Gap} staff at {hourlyGap.Hour:t}",
                    EstimatedSavings = savingsPerHour,
                    Description = $"Reduce excess staffing by {hourlyGap.Gap}",
                    Priority = hourlyGap.Gap >= 3 ? 1 : 2
                });
            }
        }

        // Calculate potential optimized cost
        optimizedCost = forecast.TotalLaborCost;
        var potentialSavings = currentCost - optimizedCost;

        return new ScheduleOptimizationResultDto
        {
            HasIssues = issues.Any(),
            Issues = issues.OrderBy(i => i.Hour).ToList(),
            Suggestions = suggestions.OrderBy(s => s.Priority).ToList(),
            CurrentLaborCost = currentCost,
            OptimizedLaborCost = optimizedCost > 0 ? optimizedCost : currentCost,
            PotentialSavings = potentialSavings > 0 ? potentialSavings : 0,
            SavingsPercent = currentCost > 0 ? (potentialSavings / currentCost) * 100 : 0,
            IssuesFound = issues.Count,
            SuggestionsGenerated = suggestions.Count
        };
    }

    public async Task<StaffingGapDto> IdentifyStaffingGapsAsync(
        DateTime date, int storeId, List<ShiftDto> currentSchedule)
    {
        var forecast = await ForecastDayAsync(date, storeId);
        var hourlyGaps = new List<HourlyGapDto>();
        int totalUnderstaffed = 0;
        int totalOverstaffed = 0;
        decimal totalImpact = 0;

        foreach (var hourlyForecast in forecast.HourlyForecasts)
        {
            // Count scheduled staff for this hour
            int scheduledStaff = currentSchedule.Count(s =>
                s.StartTime <= TimeOnly.FromDateTime(hourlyForecast.Hour) &&
                s.EndTime > TimeOnly.FromDateTime(hourlyForecast.Hour));

            int gap = scheduledStaff - hourlyForecast.RecommendedTotalStaff;

            var gapByRole = new Dictionary<string, int>();
            foreach (var (role, needed) in hourlyForecast.StaffByRole)
            {
                int roleScheduled = currentSchedule.Count(s =>
                    s.Role == role &&
                    s.StartTime <= TimeOnly.FromDateTime(hourlyForecast.Hour) &&
                    s.EndTime > TimeOnly.FromDateTime(hourlyForecast.Hour));
                gapByRole[role] = roleScheduled - needed;
            }

            string status = gap == 0 ? "Optimal" : gap > 0 ? "Overstaffed" : "Understaffed";

            hourlyGaps.Add(new HourlyGapDto
            {
                Hour = hourlyForecast.Hour,
                ScheduledStaff = scheduledStaff,
                RecommendedStaff = hourlyForecast.RecommendedTotalStaff,
                Gap = gap,
                Status = status,
                GapByRole = gapByRole
            });

            if (gap < 0) totalUnderstaffed++;
            else if (gap > 0) totalOverstaffed++;

            totalImpact += Math.Abs(gap) * hourlyForecast.LaborCostEstimate / Math.Max(1, hourlyForecast.RecommendedTotalStaff);
        }

        return new StaffingGapDto
        {
            Date = date,
            StoreId = storeId,
            TotalGapHours = totalUnderstaffed + totalOverstaffed,
            UnderstaffedHours = totalUnderstaffed,
            OverstaffedHours = totalOverstaffed,
            EstimatedCostImpact = totalImpact,
            HourlyGaps = hourlyGaps
        };
    }

    public async Task<List<StaffingIssueDto>> GetStaffingIssuesAsync(DateTime from, DateTime to, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var issues = await context.StaffingIssues
            .AsNoTracking()
            .Where(i => i.StoreId == storeId &&
                   i.IssueDateTime >= from &&
                   i.IssueDateTime <= to &&
                   !i.IsResolved)
            .OrderBy(i => i.IssueDateTime)
            .ToListAsync();

        return issues.Select(i => new StaffingIssueDto
        {
            Id = i.Id,
            Hour = i.IssueDateTime,
            Type = i.IssueType,
            TypeDescription = i.IssueType.ToString(),
            Role = i.RoleName,
            CurrentStaff = i.CurrentStaff,
            RecommendedStaff = i.RecommendedStaff,
            Variance = i.Variance,
            ImpactEstimate = i.ImpactEstimate,
            Recommendation = i.Recommendation ?? string.Empty,
            Severity = Math.Abs(i.Variance) >= 3 ? "High" : Math.Abs(i.Variance) >= 2 ? "Medium" : "Low"
        }).ToList();
    }

    public async Task<List<OptimizationSuggestionDto>> GetOptimizationSuggestionsAsync(DateTime date, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var suggestions = await context.OptimizationSuggestions
            .AsNoTracking()
            .Where(s => s.StoreId == storeId &&
                   s.ScheduleDate.Date == date.Date &&
                   !s.IsApplied)
            .OrderBy(s => s.Id)
            .ToListAsync();

        return suggestions.Select(s => new OptimizationSuggestionDto
        {
            Id = s.Id,
            Type = s.SuggestionType,
            TypeDescription = s.SuggestionType.ToString(),
            EmployeeId = s.EmployeeId,
            Role = s.RoleName,
            CurrentValue = s.CurrentValue,
            SuggestedValue = s.SuggestedValue,
            EstimatedSavings = s.EstimatedSavings,
            Description = s.Description ?? string.Empty,
            Priority = 1
        }).ToList();
    }

    public async Task ApplyOptimizationSuggestionAsync(int suggestionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var suggestion = await context.OptimizationSuggestions.FindAsync(suggestionId);
        if (suggestion != null)
        {
            suggestion.IsApplied = true;
            suggestion.AppliedAt = DateTime.UtcNow;
            suggestion.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Applied optimization suggestion {SuggestionId}", suggestionId);
        }
    }

    public async Task ResolveStaffingIssueAsync(int issueId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var issue = await context.StaffingIssues.FindAsync(issueId);
        if (issue != null)
        {
            issue.IsResolved = true;
            issue.ResolvedAt = DateTime.UtcNow;
            issue.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Resolved staffing issue {IssueId}", issueId);
        }
    }

    #endregion

    #region Analysis

    public async Task<LaborEfficiencyReportDto> AnalyzeLaborEfficiencyAsync(DateTime from, DateTime to, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        var metrics = await context.LaborEfficiencyMetrics
            .AsNoTracking()
            .Where(m => m.StoreId == storeId && m.Date >= from && m.Date <= to)
            .OrderBy(m => m.Date)
            .ToListAsync();

        if (!metrics.Any())
        {
            return new LaborEfficiencyReportDto
            {
                FromDate = from,
                ToDate = to,
                StoreId = storeId,
                TargetLaborPercent = config.TargetLaborPercent,
                TargetSPLH = config.TargetSPLH
            };
        }

        var totalSales = metrics.Sum(m => m.ActualSales);
        var totalHours = metrics.Sum(m => m.ActualLaborHours);
        var totalCost = metrics.Sum(m => m.ActualLaborCost);
        var totalOvertimeHours = metrics.Sum(m => m.OvertimeHours);
        var totalOvertimeCost = metrics.Sum(m => m.OvertimeCost);

        return new LaborEfficiencyReportDto
        {
            FromDate = from,
            ToDate = to,
            StoreId = storeId,
            TotalSales = totalSales,
            AverageDailySales = totalSales / metrics.Count,
            SalesForecastAccuracy = metrics.Average(m => m.SalesForecastAccuracy),
            TotalLaborHours = totalHours,
            TotalLaborCost = totalCost,
            ActualLaborPercent = totalSales > 0 ? (totalCost / totalSales) * 100 : 0,
            TargetLaborPercent = config.TargetLaborPercent,
            LaborPercentVariance = totalSales > 0 ? ((totalCost / totalSales) * 100) - config.TargetLaborPercent : 0,
            ActualSPLH = totalHours > 0 ? totalSales / totalHours : 0,
            TargetSPLH = config.TargetSPLH,
            SPLHVariance = totalHours > 0 ? (totalSales / totalHours) - config.TargetSPLH : 0,
            TotalUnderstaffedHours = metrics.Sum(m => m.UnderstaffedHours),
            TotalOverstaffedHours = metrics.Sum(m => m.OverstaffedHours),
            ScheduleEfficiency = CalculateScheduleEfficiency(metrics),
            ForecastAccuracy = metrics.Average(m => m.SalesForecastAccuracy),
            TotalOvertimeHours = totalOvertimeHours,
            TotalOvertimeCost = totalOvertimeCost,
            OvertimePercent = totalHours > 0 ? (totalOvertimeHours / totalHours) * 100 : 0,
            DailyMetrics = metrics.Select(m => new DailyEfficiencyMetricsDto
            {
                Date = m.Date,
                Sales = m.ActualSales,
                LaborHours = m.ActualLaborHours,
                LaborCost = m.ActualLaborCost,
                LaborPercent = m.ActualLaborPercent,
                SPLH = m.ActualSPLH,
                UnderstaffedHours = m.UnderstaffedHours,
                OverstaffedHours = m.OverstaffedHours
            }).ToList()
        };
    }

    public async Task<OvertimeRiskAssessmentDto> AssessOvertimeRiskAsync(DateTime weekStart, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        var weekEnd = weekStart.AddDays(6);

        // Get employee scheduled hours for the week (simplified - would need actual schedule data)
        var atRiskEmployees = new List<EmployeeOvertimeRiskDto>();
        decimal projectedOvertimeHours = 0;
        decimal projectedOvertimeCost = 0;

        // This would normally query actual schedule data
        // For now, return a placeholder assessment
        var riskLevel = projectedOvertimeHours > 20 ? "High" :
                        projectedOvertimeHours > 10 ? "Medium" : "Low";

        var recommendations = new List<string>();
        if (projectedOvertimeHours > 0)
        {
            recommendations.Add("Review shift distribution to balance hours");
            recommendations.Add("Consider bringing in additional part-time staff");
        }

        return new OvertimeRiskAssessmentDto
        {
            WeekStart = weekStart,
            StoreId = storeId,
            EmployeesAtRisk = atRiskEmployees.Count,
            ProjectedOvertimeHours = projectedOvertimeHours,
            ProjectedOvertimeCost = projectedOvertimeCost,
            AtRiskEmployees = atRiskEmployees,
            RiskLevel = riskLevel,
            Recommendations = recommendations
        };
    }

    public async Task<LaborCostForecastDto> ForecastLaborCostAsync(DateTime from, DateTime to, int storeId)
    {
        var dailyForecasts = new List<DailyCostForecastDto>();
        decimal totalSales = 0;
        decimal totalCost = 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var forecast = await ForecastDayAsync(date, storeId);

            dailyForecasts.Add(new DailyCostForecastDto
            {
                Date = date,
                ForecastedSales = forecast.TotalForecastedSales,
                ForecastedLaborCost = forecast.TotalLaborCost,
                ForecastedLaborPercent = forecast.ForecastedLaborPercent
            });

            totalSales += forecast.TotalForecastedSales;
            totalCost += forecast.TotalLaborCost;
        }

        var config = await GetConfigurationAsync(storeId);

        return new LaborCostForecastDto
        {
            FromDate = from,
            ToDate = to,
            StoreId = storeId,
            ForecastedSales = totalSales,
            ForecastedLaborCost = totalCost,
            ForecastedLaborPercent = totalSales > 0 ? (totalCost / totalSales) * 100 : 0,
            ForecastedRegularCost = totalCost * 0.95m, // Estimate 95% regular
            ForecastedOvertimeCost = totalCost * 0.05m, // Estimate 5% overtime
            DailyForecasts = dailyForecasts
        };
    }

    public async Task<List<DailyEfficiencyMetricsDto>> GetSPLHTrendsAsync(DateTime from, DateTime to, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var metrics = await context.LaborEfficiencyMetrics
            .AsNoTracking()
            .Where(m => m.StoreId == storeId && m.Date >= from && m.Date <= to)
            .OrderBy(m => m.Date)
            .ToListAsync();

        return metrics.Select(m => new DailyEfficiencyMetricsDto
        {
            Date = m.Date,
            Sales = m.ActualSales,
            LaborHours = m.ActualLaborHours,
            LaborCost = m.ActualLaborCost,
            LaborPercent = m.ActualLaborPercent,
            SPLH = m.ActualSPLH,
            UnderstaffedHours = m.UnderstaffedHours,
            OverstaffedHours = m.OverstaffedHours
        }).ToList();
    }

    public async Task<decimal> CalculateForecastAccuracyAsync(DateTime from, DateTime to, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var metrics = await context.LaborEfficiencyMetrics
            .AsNoTracking()
            .Where(m => m.StoreId == storeId && m.Date >= from && m.Date <= to)
            .ToListAsync();

        if (!metrics.Any()) return 0;

        return metrics.Average(m => m.SalesForecastAccuracy);
    }

    #endregion

    #region Metrics

    public async Task UpdateDailyMetricsAsync(DateTime date, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);

        // Get forecast for the day
        var forecast = await context.DailyLaborForecasts
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Date.Date == date.Date && f.StoreId == storeId);

        // Get actual sales from orders
        var actualSales = await context.Orders
            .Where(o => o.StoreId == storeId &&
                   o.CreatedAt.Date == date.Date &&
                   o.Status == OrderStatus.Completed)
            .SumAsync(o => o.Total);

        // Get actual labor data (would need timesheet integration)
        // For now, estimate based on scheduled shifts
        decimal actualHours = forecast?.TotalLaborHoursNeeded ?? 0;
        decimal actualCost = forecast?.TotalLaborCost ?? 0;

        var metrics = await context.LaborEfficiencyMetrics
            .FirstOrDefaultAsync(m => m.Date.Date == date.Date && m.StoreId == storeId);

        if (metrics == null)
        {
            metrics = new LaborEfficiencyMetrics
            {
                Date = date.Date,
                StoreId = storeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.LaborEfficiencyMetrics.Add(metrics);
        }

        metrics.ForecastedSales = forecast?.TotalForecastedSales ?? 0;
        metrics.ActualSales = actualSales;
        metrics.SalesForecastAccuracy = forecast?.TotalForecastedSales > 0
            ? Math.Min(1, actualSales / forecast.TotalForecastedSales)
            : 0;
        metrics.ForecastedLaborHours = forecast?.TotalLaborHoursNeeded ?? 0;
        metrics.ActualLaborHours = actualHours;
        metrics.ForecastedLaborCost = forecast?.TotalLaborCost ?? 0;
        metrics.ActualLaborCost = actualCost;
        metrics.ActualSPLH = actualHours > 0 ? actualSales / actualHours : 0;
        metrics.TargetSPLH = config.TargetSPLH;
        metrics.ActualLaborPercent = actualSales > 0 ? (actualCost / actualSales) * 100 : 0;
        metrics.TargetLaborPercent = config.TargetLaborPercent;
        metrics.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated daily metrics for store {StoreId} on {Date}: ${Sales:N0} sales, SPLH={SPLH:N2}",
            storeId, date.ToShortDateString(), actualSales, metrics.ActualSPLH);
    }

    public async Task<LaborEfficiencyMetrics?> GetDailyMetricsAsync(DateTime date, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.LaborEfficiencyMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Date.Date == date.Date && m.StoreId == storeId);
    }

    #endregion

    #region Private Methods

    private async Task<List<(DateTime Date, decimal Sales, int Transactions)>> GetHistoricalSalesDataAsync(
        POSDbContext context, DateTime date, int storeId, int historyDays)
    {
        var historicalStart = date.AddDays(-historyDays);

        var data = await context.Orders
            .Where(o => o.StoreId == storeId &&
                   o.CreatedAt >= historicalStart &&
                   o.CreatedAt < date &&
                   o.Status == OrderStatus.Completed)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Sales = g.Sum(o => o.Total),
                Transactions = g.Count()
            })
            .ToListAsync();

        return data.Select(d => (d.Date, d.Sales, d.Transactions)).ToList();
    }

    private async Task<HourlyLaborForecast> GenerateHourlyForecastAsync(
        POSDbContext context,
        DateTime date,
        int hour,
        int storeId,
        LaborConfigurationDto config,
        List<(DateTime Date, decimal Sales, int Transactions)> historicalData)
    {
        var hourDateTime = date.Date.AddHours(hour);

        // Get historical data for same day of week and hour
        var dayOfWeek = date.DayOfWeek;
        var sameHourData = await context.Orders
            .Where(o => o.StoreId == storeId &&
                   o.CreatedAt.Hour == hour &&
                   o.CreatedAt.DayOfWeek == dayOfWeek &&
                   o.Status == OrderStatus.Completed)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Sales = g.Sum(o => o.Total),
                Transactions = g.Count()
            })
            .ToListAsync();

        // Calculate averages with trend adjustment
        decimal avgSales = sameHourData.Any() ? sameHourData.Average(d => d.Sales) : 0;
        int avgTransactions = sameHourData.Any() ? (int)sameHourData.Average(d => d.Transactions) : 0;

        // Apply seasonal/special event factors
        var factors = GetHourlyFactors(date, hour);
        decimal adjustmentFactor = 1.0m + factors.Sum(f => GetFactorWeight(f));

        decimal forecastedSales = avgSales * adjustmentFactor;
        int forecastedTransactions = (int)(avgTransactions * adjustmentFactor);

        // Calculate required staff based on SPLH target
        decimal laborHoursNeeded = config.TargetSPLH > 0 ? forecastedSales / config.TargetSPLH : 0;
        int recommendedStaff = Math.Max(config.MinStaffPerShift,
            Math.Min(config.MaxStaffPerShift, (int)Math.Ceiling(laborHoursNeeded)));

        // Distribute staff across roles
        var roleForecasts = new List<HourlyRoleForecast>();
        decimal totalLaborCost = 0;

        if (config.Roles.Any())
        {
            int remainingStaff = recommendedStaff;

            // First, assign minimum required roles
            foreach (var role in config.Roles.Where(r => r.IsRequiredRole).OrderBy(r => r.DisplayOrder))
            {
                int staffForRole = Math.Min(role.MinStaff, remainingStaff);
                remainingStaff -= staffForRole;

                roleForecasts.Add(new HourlyRoleForecast
                {
                    RoleName = role.RoleName,
                    RecommendedStaff = staffForRole,
                    LaborCostEstimate = staffForRole * role.HourlyRate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                totalLaborCost += staffForRole * role.HourlyRate;
            }

            // Distribute remaining staff based on transaction volume
            foreach (var role in config.Roles.Where(r => !r.IsRequiredRole).OrderBy(r => r.DisplayOrder))
            {
                if (remainingStaff <= 0) break;

                int transactionsNeeded = forecastedTransactions;
                int staffForRole = Math.Min(remainingStaff,
                    Math.Max(role.MinStaff, (int)Math.Ceiling(transactionsNeeded / role.TransactionsPerHour)));
                remainingStaff -= staffForRole;

                roleForecasts.Add(new HourlyRoleForecast
                {
                    RoleName = role.RoleName,
                    RecommendedStaff = staffForRole,
                    LaborCostEstimate = staffForRole * role.HourlyRate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                totalLaborCost += staffForRole * role.HourlyRate;
            }
        }
        else
        {
            // No roles configured, use average rate
            totalLaborCost = recommendedStaff * 15m; // Default $15/hour
        }

        // Calculate confidence based on historical data availability
        decimal confidence = CalculateHourlyConfidence(sameHourData.Count, factors);

        return new HourlyLaborForecast
        {
            Hour = hour,
            HourDateTime = hourDateTime,
            ForecastedSales = forecastedSales,
            ForecastedTransactions = forecastedTransactions,
            ForecastedCovers = (int)(forecastedTransactions * 1.5m), // Assume 1.5 covers per transaction
            TargetSPLH = config.TargetSPLH,
            RecommendedTotalStaff = recommendedStaff,
            LaborCostEstimate = totalLaborCost,
            ConfidenceLevel = confidence,
            Factors = JsonSerializer.Serialize(factors),
            RoleForecasts = roleForecasts,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private List<string> GetHourlyFactors(DateTime date, int hour)
    {
        var factors = new List<string>();

        // Day of week factors
        if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
            factors.Add("Weekend");

        // Time of day factors
        if (hour >= 11 && hour <= 13) factors.Add("Lunch Rush");
        if (hour >= 17 && hour <= 20) factors.Add("Dinner Rush");
        if (hour >= 21) factors.Add("Late Night");

        // Holiday checks (simplified)
        if (date.Month == 12 && date.Day >= 20 && date.Day <= 31)
            factors.Add("Holiday Season");
        if (date.Month == 2 && date.Day == 14)
            factors.Add("Valentine's Day");
        if (date.Month == 5 && date.DayOfWeek == DayOfWeek.Sunday && date.Day >= 8 && date.Day <= 14)
            factors.Add("Mother's Day");

        return factors;
    }

    private decimal GetFactorWeight(string factor)
    {
        return factor switch
        {
            "Weekend" => 0.15m,
            "Lunch Rush" => 0.10m,
            "Dinner Rush" => 0.20m,
            "Late Night" => -0.10m,
            "Holiday Season" => 0.25m,
            "Valentine's Day" => 0.40m,
            "Mother's Day" => 0.50m,
            _ => 0m
        };
    }

    private List<string> GetSpecialFactors(DateTime date)
    {
        var factors = new List<string>();

        if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
            factors.Add("Weekend");

        if (date.Month == 12 && date.Day >= 20)
            factors.Add("Holiday Season");

        return factors;
    }

    private decimal CalculateHourlyConfidence(int dataPoints, List<string> factors)
    {
        // Base confidence on data availability
        decimal baseConfidence = dataPoints switch
        {
            >= 12 => 0.90m,
            >= 8 => 0.80m,
            >= 4 => 0.70m,
            >= 2 => 0.60m,
            _ => 0.50m
        };

        // Reduce confidence for unusual factors
        if (factors.Any(f => f.Contains("Holiday") || f.Contains("Valentine") || f.Contains("Mother")))
            baseConfidence *= 0.85m;

        return Math.Round(baseConfidence, 2);
    }

    private decimal CalculateOverallConfidence(List<HourlyLaborForecast> hourlyForecasts)
    {
        if (!hourlyForecasts.Any()) return 0;
        return Math.Round(hourlyForecasts.Average(h => h.ConfidenceLevel), 2);
    }

    private List<ShiftRecommendation> GenerateShiftRecommendations(
        List<HourlyLaborForecast> hourlyForecasts,
        LaborConfigurationDto config)
    {
        var recommendations = new List<ShiftRecommendation>();

        if (!hourlyForecasts.Any()) return recommendations;

        var roles = config.Roles.Any()
            ? config.Roles.Select(r => r.RoleName).ToList()
            : new List<string> { "General" };

        foreach (var role in roles)
        {
            var roleConfig = config.Roles.FirstOrDefault(r => r.RoleName == role);
            decimal hourlyRate = roleConfig?.HourlyRate ?? 15m;

            // Find peak hours for this role
            var roleHours = hourlyForecasts
                .Where(h => h.RoleForecasts.Any(r => r.RoleName == role && r.RecommendedStaff > 0))
                .OrderBy(h => h.Hour)
                .ToList();

            if (!roleHours.Any())
            {
                // Use total staff recommendations
                roleHours = hourlyForecasts.Where(h => h.RecommendedTotalStaff > 0).OrderBy(h => h.Hour).ToList();
            }

            if (!roleHours.Any()) continue;

            // Generate morning shift
            var morningHours = roleHours.Where(h => h.Hour >= 6 && h.Hour < 14).ToList();
            if (morningHours.Any())
            {
                var avgStaff = role == "General"
                    ? (int)Math.Ceiling(morningHours.Average(h => h.RecommendedTotalStaff) / Math.Max(1, roles.Count))
                    : (int)Math.Ceiling(morningHours.Average(h =>
                        h.RoleForecasts.FirstOrDefault(r => r.RoleName == role)?.RecommendedStaff ?? 1));

                recommendations.Add(new ShiftRecommendation
                {
                    RoleName = role,
                    StartTime = new TimeOnly(6, 0),
                    EndTime = new TimeOnly(14, 0),
                    HeadCount = Math.Max(1, avgStaff),
                    EstimatedCost = avgStaff * hourlyRate * 8,
                    Reason = "Morning coverage",
                    Priority = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Generate afternoon/evening shift
            var eveningHours = roleHours.Where(h => h.Hour >= 14 && h.Hour <= 22).ToList();
            if (eveningHours.Any())
            {
                var avgStaff = role == "General"
                    ? (int)Math.Ceiling(eveningHours.Average(h => h.RecommendedTotalStaff) / Math.Max(1, roles.Count))
                    : (int)Math.Ceiling(eveningHours.Average(h =>
                        h.RoleForecasts.FirstOrDefault(r => r.RoleName == role)?.RecommendedStaff ?? 1));

                recommendations.Add(new ShiftRecommendation
                {
                    RoleName = role,
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(22, 0),
                    HeadCount = Math.Max(1, avgStaff),
                    EstimatedCost = avgStaff * hourlyRate * 8,
                    Reason = "Afternoon/Evening coverage",
                    Priority = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Generate peak coverage shift if needed
            var peakHours = roleHours.Where(h => h.Hour >= 11 && h.Hour <= 13 || h.Hour >= 17 && h.Hour <= 20).ToList();
            if (peakHours.Any())
            {
                var peakStaff = role == "General"
                    ? (int)Math.Ceiling(peakHours.Max(h => h.RecommendedTotalStaff) / Math.Max(1, roles.Count))
                    : (int)Math.Ceiling(peakHours.Max(h =>
                        h.RoleForecasts.FirstOrDefault(r => r.RoleName == role)?.RecommendedStaff ?? 1));

                var baselineStaff = recommendations.Where(r => r.RoleName == role).Sum(r => r.HeadCount) /
                    Math.Max(1, recommendations.Count(r => r.RoleName == role));

                if (peakStaff > baselineStaff + 1)
                {
                    recommendations.Add(new ShiftRecommendation
                    {
                        RoleName = role,
                        StartTime = new TimeOnly(11, 0),
                        EndTime = new TimeOnly(14, 0),
                        HeadCount = peakStaff - (int)baselineStaff,
                        EstimatedCost = (peakStaff - (int)baselineStaff) * hourlyRate * 3,
                        Reason = "Lunch rush support",
                        Priority = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    recommendations.Add(new ShiftRecommendation
                    {
                        RoleName = role,
                        StartTime = new TimeOnly(17, 0),
                        EndTime = new TimeOnly(21, 0),
                        HeadCount = peakStaff - (int)baselineStaff,
                        EstimatedCost = (peakStaff - (int)baselineStaff) * hourlyRate * 4,
                        Reason = "Dinner rush support",
                        Priority = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        return recommendations.OrderBy(r => r.StartTime).ThenBy(r => r.Priority).ToList();
    }

    private decimal CalculateScheduleEfficiency(List<LaborEfficiencyMetrics> metrics)
    {
        if (!metrics.Any()) return 0;

        int totalHours = metrics.Sum(m => m.UnderstaffedHours + m.OverstaffedHours);
        int operatingHours = metrics.Count * 16; // Assume 16 operating hours per day

        if (operatingHours == 0) return 100;

        return Math.Round(100 - ((decimal)totalHours / operatingHours * 100), 1);
    }

    private DailyLaborForecastDto MapToDto(DailyLaborForecast forecast)
    {
        return new DailyLaborForecastDto
        {
            Id = forecast.Id,
            Date = forecast.Date,
            StoreId = forecast.StoreId,
            TotalForecastedSales = forecast.TotalForecastedSales,
            TotalLaborHoursNeeded = forecast.TotalLaborHoursNeeded,
            TotalLaborCost = forecast.TotalLaborCost,
            ForecastedLaborPercent = forecast.ForecastedLaborPercent,
            ConfidenceLevel = forecast.ConfidenceLevel,
            Status = forecast.Status,
            GeneratedAt = forecast.GeneratedAt,
            SpecialFactors = string.IsNullOrEmpty(forecast.SpecialFactors)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(forecast.SpecialFactors) ?? new List<string>(),
            HourlyForecasts = forecast.HourlyForecasts.Select(h => new HourlyLaborForecastDto
            {
                Id = h.Id,
                Hour = h.HourDateTime,
                ForecastedSales = h.ForecastedSales,
                ForecastedTransactions = h.ForecastedTransactions,
                ForecastedCovers = h.ForecastedCovers,
                SalesPerLaborHour = h.TargetSPLH,
                RecommendedTotalStaff = h.RecommendedTotalStaff,
                StaffByRole = h.RoleForecasts.ToDictionary(r => r.RoleName, r => r.RecommendedStaff),
                ConfidenceLevel = h.ConfidenceLevel,
                Factors = string.IsNullOrEmpty(h.Factors)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(h.Factors) ?? new List<string>(),
                LaborCostEstimate = h.LaborCostEstimate
            }).OrderBy(h => h.Hour).ToList(),
            RecommendedShifts = forecast.ShiftRecommendations.Select(s => new ShiftRecommendationDto
            {
                Id = s.Id,
                Role = s.RoleName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                HeadCount = s.HeadCount,
                EstimatedCost = s.EstimatedCost,
                Reason = s.Reason ?? string.Empty
            }).OrderBy(s => s.StartTime).ToList()
        };
    }

    #endregion
}
