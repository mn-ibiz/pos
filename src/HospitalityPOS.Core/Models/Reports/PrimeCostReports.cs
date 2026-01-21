namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Prime Cost Report - COGS + Labor as percentage of revenue.
/// Industry target: 60% or less.
/// </summary>
public class PrimeCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Revenue
    public decimal GrossRevenue { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetRevenue => GrossRevenue - Discounts;

    // Cost of Goods Sold
    public decimal FoodCost { get; set; }
    public decimal BeverageCost { get; set; }
    public decimal OtherCOGS { get; set; }
    public decimal TotalCOGS => FoodCost + BeverageCost + OtherCOGS;
    public decimal COGSPercentage => NetRevenue > 0 ? Math.Round(TotalCOGS / NetRevenue * 100, 2) : 0;

    // Labor Costs
    public decimal WagesAndSalaries { get; set; }
    public decimal PayrollTaxes { get; set; }
    public decimal EmployeeBenefits { get; set; }
    public decimal TotalLaborCost => WagesAndSalaries + PayrollTaxes + EmployeeBenefits;
    public decimal LaborCostPercentage => NetRevenue > 0 ? Math.Round(TotalLaborCost / NetRevenue * 100, 2) : 0;

    // Prime Cost
    public decimal PrimeCost => TotalCOGS + TotalLaborCost;
    public decimal PrimeCostPercentage => NetRevenue > 0 ? Math.Round(PrimeCost / NetRevenue * 100, 2) : 0;

    // Gross Profit
    public decimal GrossProfit => NetRevenue - TotalCOGS;
    public decimal GrossProfitPercentage => NetRevenue > 0 ? Math.Round(GrossProfit / NetRevenue * 100, 2) : 0;

    // Status
    public PrimeCostStatus Status => PrimeCostPercentage switch
    {
        <= 55 => PrimeCostStatus.Excellent,
        <= 60 => PrimeCostStatus.Good,
        <= 65 => PrimeCostStatus.Warning,
        _ => PrimeCostStatus.Critical
    };

    public string StatusMessage => Status switch
    {
        PrimeCostStatus.Excellent => "Excellent cost control",
        PrimeCostStatus.Good => "Within target range",
        PrimeCostStatus.Warning => "Above target - review costs",
        PrimeCostStatus.Critical => "Critical - immediate action required",
        _ => "Unknown"
    };

    // Breakdown details
    public List<CostBreakdownItem> COGSBreakdown { get; set; } = [];
    public List<CostBreakdownItem> LaborBreakdown { get; set; } = [];

    // Trend comparison
    public decimal? PreviousPeriodPrimeCostPercentage { get; set; }
    public decimal? TrendChange => PreviousPeriodPrimeCostPercentage.HasValue
        ? PrimeCostPercentage - PreviousPeriodPrimeCostPercentage.Value
        : null;
}

public enum PrimeCostStatus
{
    Excellent,
    Good,
    Warning,
    Critical
}

public class CostBreakdownItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public decimal? PreviousAmount { get; set; }
    public decimal? Variance => PreviousAmount.HasValue ? Amount - PreviousAmount.Value : null;
}

/// <summary>
/// Food Cost Report - Detailed food cost analysis.
/// Industry target: 28-35% of food sales.
/// </summary>
public class FoodCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Inventory-based calculation
    public decimal BeginningInventory { get; set; }
    public decimal Purchases { get; set; }
    public decimal EndingInventory { get; set; }
    public decimal ActualFoodCost => BeginningInventory + Purchases - EndingInventory;

    // Sales
    public decimal FoodSales { get; set; }

    // Food Cost Percentage
    public decimal FoodCostPercentage => FoodSales > 0
        ? Math.Round(ActualFoodCost / FoodSales * 100, 2) : 0;

    // Theoretical vs Actual
    public decimal TheoreticalFoodCost { get; set; }
    public decimal TheoreticalFoodCostPercentage => FoodSales > 0
        ? Math.Round(TheoreticalFoodCost / FoodSales * 100, 2) : 0;
    public decimal Variance => ActualFoodCost - TheoreticalFoodCost;
    public decimal VariancePercentage => TheoreticalFoodCost > 0
        ? Math.Round(Variance / TheoreticalFoodCost * 100, 2) : 0;

    // Status
    public FoodCostStatus Status => FoodCostPercentage switch
    {
        <= 28 => FoodCostStatus.Excellent,
        <= 32 => FoodCostStatus.Good,
        <= 35 => FoodCostStatus.Acceptable,
        _ => FoodCostStatus.High
    };

    // Category breakdown
    public List<FoodCostCategoryItem> CategoryBreakdown { get; set; } = [];

    // Top variance items
    public List<FoodCostVarianceItem> TopVarianceItems { get; set; } = [];
}

public enum FoodCostStatus
{
    Excellent,
    Good,
    Acceptable,
    High
}

public class FoodCostCategoryItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal CostPercentage => Sales > 0 ? Math.Round(Cost / Sales * 100, 2) : 0;
    public decimal TheoreticalCost { get; set; }
    public decimal Variance => Cost - TheoreticalCost;
}

public class FoodCostVarianceItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TheoreticalUsage { get; set; }
    public decimal ActualUsage { get; set; }
    public decimal Variance => ActualUsage - TheoreticalUsage;
    public decimal VarianceValue { get; set; }
    public string PossibleCause { get; set; } = string.Empty;
}

/// <summary>
/// Labor Cost Report - Detailed labor cost analysis.
/// Industry target: 25-35% of revenue.
/// </summary>
public class LaborCostReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Revenue
    public decimal GrossRevenue { get; set; }

    // Labor Costs
    public decimal HourlyWages { get; set; }
    public decimal SalariedWages { get; set; }
    public decimal Overtime { get; set; }
    public decimal PayrollTaxes { get; set; }
    public decimal Benefits { get; set; }
    public decimal TotalLaborCost => HourlyWages + SalariedWages + Overtime + PayrollTaxes + Benefits;

    // Labor Percentage
    public decimal LaborCostPercentage => GrossRevenue > 0
        ? Math.Round(TotalLaborCost / GrossRevenue * 100, 2) : 0;

    // Labor Hours
    public decimal TotalHoursWorked { get; set; }
    public decimal SalesPerLaborHour => TotalHoursWorked > 0
        ? Math.Round(GrossRevenue / TotalHoursWorked, 2) : 0;
    public decimal CostPerLaborHour => TotalHoursWorked > 0
        ? Math.Round(TotalLaborCost / TotalHoursWorked, 2) : 0;

    // Status
    public LaborCostStatus Status => LaborCostPercentage switch
    {
        <= 25 => LaborCostStatus.Excellent,
        <= 30 => LaborCostStatus.Good,
        <= 35 => LaborCostStatus.Acceptable,
        _ => LaborCostStatus.High
    };

    // Department breakdown
    public List<LaborCostDepartmentItem> DepartmentBreakdown { get; set; } = [];

    // Employee productivity
    public List<EmployeeProductivityItem> TopPerformers { get; set; } = [];

    // Overtime analysis
    public decimal OvertimePercentage => TotalLaborCost > 0
        ? Math.Round(Overtime / TotalLaborCost * 100, 2) : 0;
    public bool HasExcessiveOvertime => OvertimePercentage > 5;
}

public enum LaborCostStatus
{
    Excellent,
    Good,
    Acceptable,
    High
}

public class LaborCostDepartmentItem
{
    public string Department { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Cost { get; set; }
    public decimal CostPercentage { get; set; }
    public decimal SalesPerHour { get; set; }
    public int EmployeeCount { get; set; }
}

public class EmployeeProductivityItem
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal HoursWorked { get; set; }
    public decimal TotalSales { get; set; }
    public decimal SalesPerHour => HoursWorked > 0 ? Math.Round(TotalSales / HoursWorked, 2) : 0;
    public int TransactionCount { get; set; }
    public decimal AverageTicket => TransactionCount > 0 ? Math.Round(TotalSales / TransactionCount, 2) : 0;
}

/// <summary>
/// Profit & Loss Statement.
/// </summary>
public class ProfitLossStatement
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Revenue Section
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal Returns { get; set; }
    public decimal NetSales => GrossSales - Discounts - Returns;
    public decimal OtherRevenue { get; set; }
    public decimal TotalRevenue => NetSales + OtherRevenue;

    // Cost of Goods Sold
    public decimal FoodCost { get; set; }
    public decimal BeverageCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal TotalCOGS => FoodCost + BeverageCost + PackagingCost;

    // Gross Profit
    public decimal GrossProfit => TotalRevenue - TotalCOGS;
    public decimal GrossProfitMargin => TotalRevenue > 0
        ? Math.Round(GrossProfit / TotalRevenue * 100, 2) : 0;

    // Operating Expenses
    public decimal LaborCost { get; set; }
    public decimal Rent { get; set; }
    public decimal Utilities { get; set; }
    public decimal Marketing { get; set; }
    public decimal Insurance { get; set; }
    public decimal Repairs { get; set; }
    public decimal Depreciation { get; set; }
    public decimal OtherOperatingExpenses { get; set; }
    public decimal TotalOperatingExpenses => LaborCost + Rent + Utilities + Marketing +
        Insurance + Repairs + Depreciation + OtherOperatingExpenses;

    // Operating Income
    public decimal OperatingIncome => GrossProfit - TotalOperatingExpenses;
    public decimal OperatingMargin => TotalRevenue > 0
        ? Math.Round(OperatingIncome / TotalRevenue * 100, 2) : 0;

    // Other Income/Expenses
    public decimal InterestExpense { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal OtherExpenses { get; set; }

    // Net Income
    public decimal NetIncome => OperatingIncome - InterestExpense + OtherIncome - OtherExpenses;
    public decimal NetProfitMargin => TotalRevenue > 0
        ? Math.Round(NetIncome / TotalRevenue * 100, 2) : 0;

    // Line items for detailed view
    public List<PLLineItem> LineItems { get; set; } = [];

    // Comparison
    public ProfitLossStatement? PriorPeriod { get; set; }
    public ProfitLossStatement? Budget { get; set; }
}

public class PLLineItem
{
    public string Section { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal? PriorPeriodAmount { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal PercentageOfRevenue { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsTotal { get; set; }
    public bool IsSubtotal { get; set; }
    public int IndentLevel { get; set; }
}

/// <summary>
/// Day-Part Analysis Report.
/// </summary>
public class DayPartAnalysisReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public List<DayPartSummary> DayParts { get; set; } = [];
    public decimal TotalSales => DayParts.Sum(d => d.Sales);
    public int TotalTransactions => DayParts.Sum(d => d.TransactionCount);

    // Best performing day part
    public DayPartSummary? BestPerformer => DayParts.OrderByDescending(d => d.Sales).FirstOrDefault();
    public DayPartSummary? MostEfficient => DayParts.OrderByDescending(d => d.SalesPerLaborHour).FirstOrDefault();
}

public class DayPartSummary
{
    public string DayPartName { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, Late Night
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Sales { get; set; }
    public decimal SalesPercentage { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTicket => TransactionCount > 0 ? Math.Round(Sales / TransactionCount, 2) : 0;
    public int GuestCount { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal SalesPerLaborHour => LaborHours > 0 ? Math.Round(Sales / LaborHours, 2) : 0;
    public decimal LaborCostPercentage => Sales > 0 ? Math.Round(LaborCost / Sales * 100, 2) : 0;

    // Comparison to previous period
    public decimal? PreviousSales { get; set; }
    public decimal? SalesGrowth => PreviousSales.HasValue && PreviousSales > 0
        ? Math.Round((Sales - PreviousSales.Value) / PreviousSales.Value * 100, 2) : null;

    // Top items for this day part
    public List<DayPartTopItem> TopItems { get; set; } = [];
}

public class DayPartTopItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Sales Per Labor Hour (SPLH) Report.
/// </summary>
public class SalesPerLaborHourReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public decimal TotalSales { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal OverallSPLH => TotalLaborHours > 0 ? Math.Round(TotalSales / TotalLaborHours, 2) : 0;

    // Target comparison
    public decimal TargetSPLH { get; set; }
    public decimal SPLHVariance => OverallSPLH - TargetSPLH;
    public bool MeetingTarget => OverallSPLH >= TargetSPLH;

    // By day of week
    public List<SPLHDailySummary> DailyBreakdown { get; set; } = [];

    // By hour
    public List<SPLHHourlySummary> HourlyBreakdown { get; set; } = [];

    // By employee
    public List<SPLHEmployeeSummary> EmployeeBreakdown { get; set; } = [];

    // Recommendations
    public List<SPLHRecommendation> Recommendations { get; set; } = [];
}

public class SPLHDailySummary
{
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public decimal Sales { get; set; }
    public decimal LaborHours { get; set; }
    public decimal SPLH => LaborHours > 0 ? Math.Round(Sales / LaborHours, 2) : 0;
    public decimal LaborCost { get; set; }
    public decimal LaborCostPercentage => Sales > 0 ? Math.Round(LaborCost / Sales * 100, 2) : 0;
}

public class SPLHHourlySummary
{
    public int Hour { get; set; }
    public string HourDisplay => $"{Hour:D2}:00 - {(Hour + 1) % 24:D2}:00";
    public decimal Sales { get; set; }
    public decimal LaborHours { get; set; }
    public decimal SPLH => LaborHours > 0 ? Math.Round(Sales / LaborHours, 2) : 0;
    public bool IsUnderstaffed { get; set; }
    public bool IsOverstaffed { get; set; }
}

public class SPLHEmployeeSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal SPLH => HoursWorked > 0 ? Math.Round(TotalSales / HoursWorked, 2) : 0;
    public int Rank { get; set; }
    public decimal PerformanceIndex { get; set; } // Compared to average
}

public class SPLHRecommendation
{
    public string Category { get; set; } = string.Empty; // Scheduling, Training, Staffing
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public int Priority { get; set; }
}

/// <summary>
/// Parameters for prime cost report generation.
/// </summary>
public class PrimeCostReportParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public bool IncludePreviousPeriod { get; set; } = true;
    public bool IncludeBreakdown { get; set; } = true;
}

/// <summary>
/// Parameters for food cost report generation.
/// </summary>
public class FoodCostReportParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeTheoreticalAnalysis { get; set; } = true;
    public bool IncludeVarianceItems { get; set; } = true;
    public int TopVarianceCount { get; set; } = 10;
}

/// <summary>
/// Parameters for labor cost report generation.
/// </summary>
public class LaborCostReportParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? DepartmentId { get; set; }
    public bool IncludeEmployeeBreakdown { get; set; } = true;
    public int TopPerformersCount { get; set; } = 10;
}

/// <summary>
/// Parameters for P&L statement generation.
/// </summary>
public class ProfitLossParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public bool IncludePriorPeriod { get; set; } = true;
    public bool IncludeBudget { get; set; }
    public int? BudgetId { get; set; }
}

/// <summary>
/// Parameters for day-part analysis report.
/// </summary>
public class DayPartAnalysisParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public bool IncludePreviousPeriod { get; set; } = true;
    public int TopItemsPerDayPart { get; set; } = 5;

    // Day part definitions (defaults)
    public List<DayPartDefinition> DayParts { get; set; } = new()
    {
        new() { Name = "Breakfast", StartHour = 6, EndHour = 11 },
        new() { Name = "Lunch", StartHour = 11, EndHour = 15 },
        new() { Name = "Dinner", StartHour = 15, EndHour = 21 },
        new() { Name = "Late Night", StartHour = 21, EndHour = 6 }
    };
}

public class DayPartDefinition
{
    public string Name { get; set; } = string.Empty;
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}

/// <summary>
/// Parameters for SPLH report generation.
/// </summary>
public class SPLHReportParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public decimal TargetSPLH { get; set; } = 50m; // Default target
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludeEmployeeBreakdown { get; set; } = true;
}
