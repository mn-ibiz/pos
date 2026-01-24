using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Type of staffing issue identified.
/// </summary>
public enum StaffingIssueType
{
    /// <summary>Not enough staff for predicted demand.</summary>
    Understaffed = 1,

    /// <summary>More staff than needed.</summary>
    Overstaffed = 2,

    /// <summary>Wrong roles scheduled for needs.</summary>
    SkillGap = 3,

    /// <summary>Employee approaching overtime threshold.</summary>
    OvertimeRisk = 4,

    /// <summary>No coverage for required break.</summary>
    NoBreakCoverage = 5,

    /// <summary>Required role not scheduled (manager, closer).</summary>
    KeyRoleMissing = 6
}

/// <summary>
/// Status of a labor forecast.
/// </summary>
public enum ForecastStatus
{
    Draft = 1,
    Active = 2,
    Superseded = 3,
    Archived = 4
}

/// <summary>
/// Type of optimization suggestion.
/// </summary>
public enum OptimizationSuggestionType
{
    AddStaff = 1,
    RemoveStaff = 2,
    ExtendShift = 3,
    ShortenShift = 4,
    ChangeStartTime = 5,
    ChangeRole = 6,
    SplitShift = 7,
    MergeShifts = 8
}

#endregion

#region Configuration

/// <summary>
/// Store-level labor configuration.
/// </summary>
public class LaborConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Target labor cost as percentage of sales.
    /// </summary>
    public decimal TargetLaborPercent { get; set; } = 25m;

    /// <summary>
    /// Target Sales Per Labor Hour.
    /// </summary>
    public decimal TargetSPLH { get; set; } = 50m;

    /// <summary>
    /// Minimum staff required per shift.
    /// </summary>
    public int MinStaffPerShift { get; set; } = 2;

    /// <summary>
    /// Maximum staff allowed per shift.
    /// </summary>
    public int MaxStaffPerShift { get; set; } = 20;

    /// <summary>
    /// Weekly hours threshold for overtime.
    /// </summary>
    public int OvertimeThresholdHours { get; set; } = 40;

    /// <summary>
    /// Overtime pay multiplier.
    /// </summary>
    public decimal OvertimeMultiplier { get; set; } = 1.5m;

    /// <summary>
    /// Minimum shift length in hours.
    /// </summary>
    public int MinShiftHours { get; set; } = 4;

    /// <summary>
    /// Maximum shift length in hours.
    /// </summary>
    public int MaxShiftHours { get; set; } = 10;

    /// <summary>
    /// Hours between shifts for same employee.
    /// </summary>
    public int MinHoursBetweenShifts { get; set; } = 8;

    /// <summary>
    /// Whether forecasting is enabled.
    /// </summary>
    public bool EnableForecasting { get; set; } = true;

    /// <summary>
    /// Days of historical data to use for forecasting.
    /// </summary>
    public int ForecastHistoryDays { get; set; } = 90;

    /// <summary>
    /// Number of weeks to forecast ahead.
    /// </summary>
    public int ForecastAheadWeeks { get; set; } = 2;

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Hourly rate configuration by role.
/// </summary>
public class LaborRoleConfiguration : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Role name (e.g., "Cashier", "Server", "Manager").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Hourly pay rate for this role.
    /// </summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Minimum staff required for this role.
    /// </summary>
    public int MinStaff { get; set; } = 1;

    /// <summary>
    /// Maximum staff allowed for this role.
    /// </summary>
    public int MaxStaff { get; set; } = 10;

    /// <summary>
    /// Transactions per staff hour capacity.
    /// </summary>
    public decimal TransactionsPerHour { get; set; } = 20;

    /// <summary>
    /// Whether this role is required for every shift.
    /// </summary>
    public bool IsRequiredRole { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion

#region Forecasts

/// <summary>
/// Labor forecast for a specific day.
/// </summary>
public class DailyLaborForecast : BaseEntity
{
    /// <summary>
    /// Date of forecast.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Total forecasted sales for the day.
    /// </summary>
    public decimal TotalForecastedSales { get; set; }

    /// <summary>
    /// Total labor hours needed.
    /// </summary>
    public decimal TotalLaborHoursNeeded { get; set; }

    /// <summary>
    /// Estimated total labor cost.
    /// </summary>
    public decimal TotalLaborCost { get; set; }

    /// <summary>
    /// Forecasted labor as percentage of sales.
    /// </summary>
    public decimal ForecastedLaborPercent { get; set; }

    /// <summary>
    /// Overall forecast confidence (0-1).
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Forecast status.
    /// </summary>
    public ForecastStatus Status { get; set; } = ForecastStatus.Draft;

    /// <summary>
    /// When forecast was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// User who generated/approved forecast.
    /// </summary>
    public int? GeneratedByUserId { get; set; }

    /// <summary>
    /// Special factors affecting this day (JSON).
    /// </summary>
    [MaxLength(1000)]
    public string? SpecialFactors { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? GeneratedByUser { get; set; }
    public virtual ICollection<HourlyLaborForecast> HourlyForecasts { get; set; } = new List<HourlyLaborForecast>();
    public virtual ICollection<ShiftRecommendation> ShiftRecommendations { get; set; } = new List<ShiftRecommendation>();
}

/// <summary>
/// Hourly labor forecast within a day.
/// </summary>
public class HourlyLaborForecast : BaseEntity
{
    /// <summary>
    /// Parent daily forecast ID.
    /// </summary>
    public int DailyForecastId { get; set; }

    /// <summary>
    /// Hour of day (0-23).
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Full datetime for this hour.
    /// </summary>
    public DateTime HourDateTime { get; set; }

    /// <summary>
    /// Forecasted sales for this hour.
    /// </summary>
    public decimal ForecastedSales { get; set; }

    /// <summary>
    /// Forecasted number of transactions.
    /// </summary>
    public int ForecastedTransactions { get; set; }

    /// <summary>
    /// Forecasted covers (for restaurants).
    /// </summary>
    public int ForecastedCovers { get; set; }

    /// <summary>
    /// Target SPLH for this hour.
    /// </summary>
    public decimal TargetSPLH { get; set; }

    /// <summary>
    /// Total staff recommended.
    /// </summary>
    public int RecommendedTotalStaff { get; set; }

    /// <summary>
    /// Estimated labor cost for this hour.
    /// </summary>
    public decimal LaborCostEstimate { get; set; }

    /// <summary>
    /// Confidence level for this hour's forecast.
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Factors affecting this hour (JSON).
    /// </summary>
    [MaxLength(500)]
    public string? Factors { get; set; }

    // Navigation properties
    public virtual DailyLaborForecast? DailyForecast { get; set; }
    public virtual ICollection<HourlyRoleForecast> RoleForecasts { get; set; } = new List<HourlyRoleForecast>();
}

/// <summary>
/// Staff recommendation by role for a specific hour.
/// </summary>
public class HourlyRoleForecast : BaseEntity
{
    /// <summary>
    /// Parent hourly forecast ID.
    /// </summary>
    public int HourlyForecastId { get; set; }

    /// <summary>
    /// Role name.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Recommended staff count for this role.
    /// </summary>
    public int RecommendedStaff { get; set; }

    /// <summary>
    /// Estimated labor cost for this role this hour.
    /// </summary>
    public decimal LaborCostEstimate { get; set; }

    // Navigation properties
    public virtual HourlyLaborForecast? HourlyForecast { get; set; }
}

/// <summary>
/// Shift recommendation within a daily forecast.
/// </summary>
public class ShiftRecommendation : BaseEntity
{
    /// <summary>
    /// Parent daily forecast ID.
    /// </summary>
    public int DailyForecastId { get; set; }

    /// <summary>
    /// Role for this shift.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Shift start time.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Shift end time.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Number of staff needed for this shift.
    /// </summary>
    public int HeadCount { get; set; }

    /// <summary>
    /// Estimated total cost for this shift.
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Reason for this shift recommendation.
    /// </summary>
    [MaxLength(200)]
    public string? Reason { get; set; }

    /// <summary>
    /// Priority ranking.
    /// </summary>
    public int Priority { get; set; }

    // Navigation properties
    public virtual DailyLaborForecast? DailyForecast { get; set; }
}

#endregion

#region Analysis

/// <summary>
/// Identified staffing issue.
/// </summary>
public class StaffingIssue : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Date and hour of the issue.
    /// </summary>
    public DateTime IssueDateTime { get; set; }

    /// <summary>
    /// Type of staffing issue.
    /// </summary>
    public StaffingIssueType IssueType { get; set; }

    /// <summary>
    /// Role affected.
    /// </summary>
    [MaxLength(50)]
    public string? RoleName { get; set; }

    /// <summary>
    /// Current/scheduled staff count.
    /// </summary>
    public int CurrentStaff { get; set; }

    /// <summary>
    /// Recommended staff count.
    /// </summary>
    public int RecommendedStaff { get; set; }

    /// <summary>
    /// Variance (positive = overstaffed, negative = understaffed).
    /// </summary>
    public int Variance { get; set; }

    /// <summary>
    /// Estimated cost impact of this issue.
    /// </summary>
    public decimal ImpactEstimate { get; set; }

    /// <summary>
    /// Recommended action.
    /// </summary>
    [MaxLength(500)]
    public string? Recommendation { get; set; }

    /// <summary>
    /// Whether issue has been resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// When issue was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Schedule optimization suggestion.
/// </summary>
public class OptimizationSuggestion : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Date of the schedule being optimized.
    /// </summary>
    public DateTime ScheduleDate { get; set; }

    /// <summary>
    /// Type of suggestion.
    /// </summary>
    public OptimizationSuggestionType SuggestionType { get; set; }

    /// <summary>
    /// Employee ID if applicable.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Role affected.
    /// </summary>
    [MaxLength(50)]
    public string? RoleName { get; set; }

    /// <summary>
    /// Current value (time, hours, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? CurrentValue { get; set; }

    /// <summary>
    /// Suggested value.
    /// </summary>
    [MaxLength(100)]
    public string? SuggestedValue { get; set; }

    /// <summary>
    /// Estimated savings from this change.
    /// </summary>
    public decimal EstimatedSavings { get; set; }

    /// <summary>
    /// Explanation of the suggestion.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether suggestion was applied.
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// When suggestion was applied.
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual Employee? Employee { get; set; }
}

#endregion

#region Metrics

/// <summary>
/// Daily labor efficiency metrics.
/// </summary>
public class LaborEfficiencyMetrics : BaseEntity
{
    /// <summary>
    /// Date of metrics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Forecasted sales.
    /// </summary>
    public decimal ForecastedSales { get; set; }

    /// <summary>
    /// Actual sales.
    /// </summary>
    public decimal ActualSales { get; set; }

    /// <summary>
    /// Sales forecast accuracy (0-1).
    /// </summary>
    public decimal SalesForecastAccuracy { get; set; }

    /// <summary>
    /// Forecasted labor hours.
    /// </summary>
    public decimal ForecastedLaborHours { get; set; }

    /// <summary>
    /// Actual labor hours.
    /// </summary>
    public decimal ActualLaborHours { get; set; }

    /// <summary>
    /// Forecasted labor cost.
    /// </summary>
    public decimal ForecastedLaborCost { get; set; }

    /// <summary>
    /// Actual labor cost.
    /// </summary>
    public decimal ActualLaborCost { get; set; }

    /// <summary>
    /// Actual SPLH (Sales Per Labor Hour).
    /// </summary>
    public decimal ActualSPLH { get; set; }

    /// <summary>
    /// Target SPLH.
    /// </summary>
    public decimal TargetSPLH { get; set; }

    /// <summary>
    /// Actual labor as percentage of sales.
    /// </summary>
    public decimal ActualLaborPercent { get; set; }

    /// <summary>
    /// Target labor percent.
    /// </summary>
    public decimal TargetLaborPercent { get; set; }

    /// <summary>
    /// Number of understaffed hours.
    /// </summary>
    public int UnderstaffedHours { get; set; }

    /// <summary>
    /// Number of overstaffed hours.
    /// </summary>
    public int OverstaffedHours { get; set; }

    /// <summary>
    /// Total overtime hours.
    /// </summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>
    /// Total overtime cost.
    /// </summary>
    public decimal OvertimeCost { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

#endregion
