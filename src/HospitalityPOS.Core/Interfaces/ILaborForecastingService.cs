using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for labor forecasting and schedule optimization.
/// </summary>
public interface ILaborForecastingService
{
    #region Configuration

    /// <summary>
    /// Gets labor configuration for a store.
    /// </summary>
    Task<LaborConfigurationDto> GetConfigurationAsync(int storeId);

    /// <summary>
    /// Updates labor configuration.
    /// </summary>
    Task<LaborConfigurationDto> UpdateConfigurationAsync(LaborConfigurationDto config);

    /// <summary>
    /// Gets role configurations for a store.
    /// </summary>
    Task<List<LaborRoleConfigurationDto>> GetRoleConfigurationsAsync(int storeId);

    /// <summary>
    /// Updates a role configuration.
    /// </summary>
    Task<LaborRoleConfigurationDto> UpdateRoleConfigurationAsync(int storeId, LaborRoleConfigurationDto role);

    /// <summary>
    /// Checks if forecasting is enabled.
    /// </summary>
    Task<bool> IsForecastingEnabledAsync(int storeId);

    #endregion

    #region Forecasting

    /// <summary>
    /// Forecasts hourly labor needs for a specific date.
    /// </summary>
    Task<List<HourlyLaborForecastDto>> ForecastLaborNeedsAsync(DateTime date, int storeId);

    /// <summary>
    /// Forecasts labor needs for a full week.
    /// </summary>
    Task<WeeklyLaborForecastDto> ForecastWeekAsync(DateTime weekStart, int storeId);

    /// <summary>
    /// Forecasts labor needs for a specific day with full details.
    /// </summary>
    Task<DailyLaborForecastDto> ForecastDayAsync(DateTime date, int storeId);

    /// <summary>
    /// Gets an existing forecast by ID.
    /// </summary>
    Task<DailyLaborForecastDto?> GetForecastAsync(int forecastId);

    /// <summary>
    /// Gets forecasts for a date range.
    /// </summary>
    Task<List<DailyLaborForecastDto>> GetForecastsAsync(DateTime from, DateTime to, int storeId);

    /// <summary>
    /// Activates a draft forecast.
    /// </summary>
    Task<DailyLaborForecastDto> ActivateForecastAsync(int forecastId, int userId);

    /// <summary>
    /// Regenerates a forecast with updated data.
    /// </summary>
    Task<DailyLaborForecastDto> RegenerateForecastAsync(int forecastId);

    #endregion

    #region Schedule Recommendations

    /// <summary>
    /// Generates schedule recommendation for a specific date.
    /// </summary>
    Task<ScheduleRecommendationDto> GenerateScheduleRecommendationAsync(DateTime date, int storeId, List<int>? availableEmployeeIds = null);

    /// <summary>
    /// Generates weekly schedule recommendations.
    /// </summary>
    Task<WeeklyScheduleRecommendationDto> GenerateWeeklyScheduleAsync(DateTime weekStart, int storeId);

    /// <summary>
    /// Gets shift recommendations for a forecast.
    /// </summary>
    Task<List<ShiftRecommendationDto>> GetShiftRecommendationsAsync(int dailyForecastId);

    #endregion

    #region Optimization

    /// <summary>
    /// Optimizes an existing schedule against the forecast.
    /// </summary>
    Task<ScheduleOptimizationResultDto> OptimizeScheduleAsync(List<ShiftDto> currentSchedule, DateTime date, int storeId);

    /// <summary>
    /// Identifies staffing gaps for a specific date.
    /// </summary>
    Task<StaffingGapDto> IdentifyStaffingGapsAsync(DateTime date, int storeId, List<ShiftDto> currentSchedule);

    /// <summary>
    /// Gets staffing issues for a date range.
    /// </summary>
    Task<List<StaffingIssueDto>> GetStaffingIssuesAsync(DateTime from, DateTime to, int storeId);

    /// <summary>
    /// Gets optimization suggestions.
    /// </summary>
    Task<List<OptimizationSuggestionDto>> GetOptimizationSuggestionsAsync(DateTime date, int storeId);

    /// <summary>
    /// Applies an optimization suggestion.
    /// </summary>
    Task ApplyOptimizationSuggestionAsync(int suggestionId);

    /// <summary>
    /// Marks a staffing issue as resolved.
    /// </summary>
    Task ResolveStaffingIssueAsync(int issueId);

    #endregion

    #region Analysis

    /// <summary>
    /// Analyzes labor efficiency for a date range.
    /// </summary>
    Task<LaborEfficiencyReportDto> AnalyzeLaborEfficiencyAsync(DateTime from, DateTime to, int storeId);

    /// <summary>
    /// Assesses overtime risk for a week.
    /// </summary>
    Task<OvertimeRiskAssessmentDto> AssessOvertimeRiskAsync(DateTime weekStart, int storeId);

    /// <summary>
    /// Forecasts labor costs for a date range.
    /// </summary>
    Task<LaborCostForecastDto> ForecastLaborCostAsync(DateTime from, DateTime to, int storeId);

    /// <summary>
    /// Gets SPLH trends over time.
    /// </summary>
    Task<List<DailyEfficiencyMetricsDto>> GetSPLHTrendsAsync(DateTime from, DateTime to, int storeId);

    /// <summary>
    /// Compares forecast accuracy over time.
    /// </summary>
    Task<decimal> CalculateForecastAccuracyAsync(DateTime from, DateTime to, int storeId);

    #endregion

    #region Metrics

    /// <summary>
    /// Updates efficiency metrics for a day (called after day ends).
    /// </summary>
    Task UpdateDailyMetricsAsync(DateTime date, int storeId);

    /// <summary>
    /// Gets daily efficiency metrics.
    /// </summary>
    Task<LaborEfficiencyMetrics?> GetDailyMetricsAsync(DateTime date, int storeId);

    #endregion
}

/// <summary>
/// Interface for the labor forecasting background job.
/// </summary>
public interface ILaborForecastingJob
{
    /// <summary>
    /// Gets the last run result.
    /// </summary>
    LaborForecastingJobResult? LastResult { get; }

    /// <summary>
    /// Triggers forecast generation for upcoming days.
    /// </summary>
    Task TriggerForecastGenerationAsync();
}
