using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Configuration DTOs

/// <summary>
/// Labor configuration DTO.
/// </summary>
public class LaborConfigurationDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public decimal TargetLaborPercent { get; set; }
    public decimal TargetSPLH { get; set; }
    public int MinStaffPerShift { get; set; }
    public int MaxStaffPerShift { get; set; }
    public int OvertimeThresholdHours { get; set; }
    public decimal OvertimeMultiplier { get; set; }
    public int MinShiftHours { get; set; }
    public int MaxShiftHours { get; set; }
    public int MinHoursBetweenShifts { get; set; }
    public bool EnableForecasting { get; set; }
    public int ForecastHistoryDays { get; set; }
    public int ForecastAheadWeeks { get; set; }
    public List<LaborRoleConfigurationDto> Roles { get; set; } = new();
}

/// <summary>
/// Role configuration DTO.
/// </summary>
public class LaborRoleConfigurationDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int MinStaff { get; set; }
    public int MaxStaff { get; set; }
    public decimal TransactionsPerHour { get; set; }
    public bool IsRequiredRole { get; set; }
    public int DisplayOrder { get; set; }
}

#endregion

#region Forecast DTOs

/// <summary>
/// Hourly labor forecast DTO.
/// </summary>
public class HourlyLaborForecastDto
{
    public int Id { get; set; }
    public DateTime Hour { get; set; }
    public decimal ForecastedSales { get; set; }
    public int ForecastedTransactions { get; set; }
    public int ForecastedCovers { get; set; }
    public decimal SalesPerLaborHour { get; set; }
    public int RecommendedTotalStaff { get; set; }
    public Dictionary<string, int> StaffByRole { get; set; } = new();
    public decimal ConfidenceLevel { get; set; }
    public List<string> Factors { get; set; } = new();
    public decimal LaborCostEstimate { get; set; }
}

/// <summary>
/// Daily labor forecast DTO.
/// </summary>
public class DailyLaborForecastDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int StoreId { get; set; }
    public decimal TotalForecastedSales { get; set; }
    public decimal TotalLaborHoursNeeded { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal ForecastedLaborPercent { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public ForecastStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<string> SpecialFactors { get; set; } = new();
    public List<HourlyLaborForecastDto> HourlyForecasts { get; set; } = new();
    public List<ShiftRecommendationDto> RecommendedShifts { get; set; } = new();
}

/// <summary>
/// Weekly labor forecast DTO.
/// </summary>
public class WeeklyLaborForecastDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int StoreId { get; set; }
    public decimal TotalForecastedSales { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal AverageLaborPercent { get; set; }
    public decimal AverageConfidence { get; set; }
    public List<DailyLaborForecastDto> DailyForecasts { get; set; } = new();
    public WeeklySummaryByRole RoleSummary { get; set; } = new();
}

/// <summary>
/// Weekly summary by role.
/// </summary>
public class WeeklySummaryByRole
{
    public Dictionary<string, decimal> HoursByRole { get; set; } = new();
    public Dictionary<string, decimal> CostByRole { get; set; } = new();
}

/// <summary>
/// Shift recommendation DTO.
/// </summary>
public class ShiftRecommendationDto
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int HeadCount { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal HoursPerShift => (decimal)(EndTime.ToTimeSpan() - StartTime.ToTimeSpan()).TotalHours;
}

#endregion

#region Schedule DTOs

/// <summary>
/// Schedule recommendation for a specific day.
/// </summary>
public class ScheduleRecommendationDto
{
    public DateTime Date { get; set; }
    public int StoreId { get; set; }
    public List<ShiftRecommendationDto> Shifts { get; set; } = new();
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal EstimatedLaborPercent { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Weekly schedule recommendation.
/// </summary>
public class WeeklyScheduleRecommendationDto
{
    public DateTime WeekStart { get; set; }
    public int StoreId { get; set; }
    public List<ScheduleRecommendationDto> DailySchedules { get; set; } = new();
    public decimal TotalWeeklyHours { get; set; }
    public decimal TotalWeeklyCost { get; set; }
    public decimal AverageDailyStaff { get; set; }
    public OvertimeRiskAssessmentDto OvertimeRisk { get; set; } = new();
}

#endregion

#region Optimization DTOs

/// <summary>
/// Result of schedule optimization.
/// </summary>
public class ScheduleOptimizationResultDto
{
    public bool HasIssues { get; set; }
    public List<StaffingIssueDto> Issues { get; set; } = new();
    public List<OptimizationSuggestionDto> Suggestions { get; set; } = new();
    public decimal CurrentLaborCost { get; set; }
    public decimal OptimizedLaborCost { get; set; }
    public decimal PotentialSavings { get; set; }
    public decimal SavingsPercent { get; set; }
    public int IssuesFound { get; set; }
    public int SuggestionsGenerated { get; set; }
}

/// <summary>
/// Staffing issue DTO.
/// </summary>
public class StaffingIssueDto
{
    public int Id { get; set; }
    public DateTime Hour { get; set; }
    public StaffingIssueType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int CurrentStaff { get; set; }
    public int RecommendedStaff { get; set; }
    public int Variance { get; set; }
    public decimal ImpactEstimate { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
}

/// <summary>
/// Optimization suggestion DTO.
/// </summary>
public class OptimizationSuggestionDto
{
    public int Id { get; set; }
    public OptimizationSuggestionType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? Role { get; set; }
    public string? CurrentValue { get; set; }
    public string? SuggestedValue { get; set; }
    public decimal EstimatedSavings { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
}

/// <summary>
/// Staffing gap analysis.
/// </summary>
public class StaffingGapDto
{
    public DateTime Date { get; set; }
    public int StoreId { get; set; }
    public int TotalGapHours { get; set; }
    public int UnderstaffedHours { get; set; }
    public int OverstaffedHours { get; set; }
    public decimal EstimatedCostImpact { get; set; }
    public List<HourlyGapDto> HourlyGaps { get; set; } = new();
    public List<StaffingIssueDto> Issues { get; set; } = new();
}

/// <summary>
/// Gap analysis for a specific hour.
/// </summary>
public class HourlyGapDto
{
    public DateTime Hour { get; set; }
    public int ScheduledStaff { get; set; }
    public int RecommendedStaff { get; set; }
    public int Gap { get; set; }
    public string Status { get; set; } = string.Empty; // Understaffed, Overstaffed, Optimal
    public Dictionary<string, int> GapByRole { get; set; } = new();
}

#endregion

#region Analytics DTOs

/// <summary>
/// Labor efficiency report.
/// </summary>
public class LaborEfficiencyReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int StoreId { get; set; }

    // Sales Metrics
    public decimal TotalSales { get; set; }
    public decimal AverageDailySales { get; set; }
    public decimal SalesForecastAccuracy { get; set; }

    // Labor Metrics
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal ActualLaborPercent { get; set; }
    public decimal TargetLaborPercent { get; set; }
    public decimal LaborPercentVariance { get; set; }

    // SPLH Metrics
    public decimal ActualSPLH { get; set; }
    public decimal TargetSPLH { get; set; }
    public decimal SPLHVariance { get; set; }

    // Efficiency Metrics
    public int TotalUnderstaffedHours { get; set; }
    public int TotalOverstaffedHours { get; set; }
    public decimal ScheduleEfficiency { get; set; }
    public decimal ForecastAccuracy { get; set; }

    // Overtime
    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalOvertimeCost { get; set; }
    public decimal OvertimePercent { get; set; }

    // Daily breakdown
    public List<DailyEfficiencyMetricsDto> DailyMetrics { get; set; } = new();

    // By role breakdown
    public List<RoleEfficiencyDto> ByRole { get; set; } = new();
}

/// <summary>
/// Daily efficiency metrics.
/// </summary>
public class DailyEfficiencyMetricsDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal LaborPercent { get; set; }
    public decimal SPLH { get; set; }
    public int UnderstaffedHours { get; set; }
    public int OverstaffedHours { get; set; }
}

/// <summary>
/// Efficiency by role.
/// </summary>
public class RoleEfficiencyDto
{
    public string RoleName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageHourlyRate { get; set; }
    public decimal PercentOfTotalLabor { get; set; }
}

/// <summary>
/// Overtime risk assessment.
/// </summary>
public class OvertimeRiskAssessmentDto
{
    public DateTime WeekStart { get; set; }
    public int StoreId { get; set; }
    public int EmployeesAtRisk { get; set; }
    public decimal ProjectedOvertimeHours { get; set; }
    public decimal ProjectedOvertimeCost { get; set; }
    public List<EmployeeOvertimeRiskDto> AtRiskEmployees { get; set; } = new();
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Individual employee overtime risk.
/// </summary>
public class EmployeeOvertimeRiskDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal ScheduledHours { get; set; }
    public decimal OvertimeThreshold { get; set; }
    public decimal ProjectedOvertimeHours { get; set; }
    public decimal ProjectedOvertimeCost { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// Labor cost forecast.
/// </summary>
public class LaborCostForecastDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int StoreId { get; set; }
    public decimal ForecastedSales { get; set; }
    public decimal ForecastedLaborCost { get; set; }
    public decimal ForecastedLaborPercent { get; set; }
    public decimal ForecastedRegularCost { get; set; }
    public decimal ForecastedOvertimeCost { get; set; }
    public List<DailyCostForecastDto> DailyForecasts { get; set; } = new();
    public Dictionary<string, decimal> CostByRole { get; set; } = new();
}

/// <summary>
/// Daily cost forecast.
/// </summary>
public class DailyCostForecastDto
{
    public DateTime Date { get; set; }
    public decimal ForecastedSales { get; set; }
    public decimal ForecastedLaborCost { get; set; }
    public decimal ForecastedLaborPercent { get; set; }
}

#endregion

#region Request DTOs

/// <summary>
/// Request to generate a forecast.
/// </summary>
public class GenerateForecastRequest
{
    public DateTime Date { get; set; }
    public int StoreId { get; set; }
    public bool IncludeShiftRecommendations { get; set; } = true;
    public List<int>? AvailableEmployeeIds { get; set; }
}

/// <summary>
/// Request to optimize a schedule.
/// </summary>
public class OptimizeScheduleRequest
{
    public DateTime Date { get; set; }
    public int StoreId { get; set; }
    public List<ShiftDto> CurrentShifts { get; set; } = new();
}

/// <summary>
/// Shift DTO for optimization.
/// </summary>
public class ShiftDto
{
    public int? ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal HourlyRate { get; set; }
}

#endregion

#region Job Result DTOs

/// <summary>
/// Result of the labor forecasting job.
/// </summary>
public class LaborForecastingJobResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int StoresProcessed { get; set; }
    public int ForecastsGenerated { get; set; }
    public int IssuesIdentified { get; set; }
    public List<string> Errors { get; set; } = new();
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;
}

#endregion
