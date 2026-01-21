namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Expense category type for Prime Cost calculations and reporting.
/// </summary>
public enum ExpenseCategoryType
{
    /// <summary>
    /// Cost of Goods Sold - Food, Beverage, Packaging.
    /// </summary>
    COGS = 1,

    /// <summary>
    /// Labor costs - Wages, Benefits, Payroll taxes.
    /// </summary>
    Labor = 2,

    /// <summary>
    /// Occupancy costs - Rent, Utilities, Property insurance.
    /// </summary>
    Occupancy = 3,

    /// <summary>
    /// General operating expenses.
    /// </summary>
    Operating = 4,

    /// <summary>
    /// Marketing and advertising expenses.
    /// </summary>
    Marketing = 5,

    /// <summary>
    /// Administrative expenses - Office, Professional services.
    /// </summary>
    Administrative = 6,

    /// <summary>
    /// Other/Miscellaneous expenses.
    /// </summary>
    Other = 7
}

/// <summary>
/// Recurrence frequency for recurring expenses.
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// Expense occurs daily.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Expense occurs weekly.
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Expense occurs every two weeks.
    /// </summary>
    BiWeekly = 3,

    /// <summary>
    /// Expense occurs monthly.
    /// </summary>
    Monthly = 4,

    /// <summary>
    /// Expense occurs quarterly.
    /// </summary>
    Quarterly = 5,

    /// <summary>
    /// Expense occurs annually.
    /// </summary>
    Annually = 6
}

/// <summary>
/// Budget period type.
/// </summary>
public enum BudgetPeriod
{
    /// <summary>
    /// Weekly budget period.
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// Monthly budget period.
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// Quarterly budget period.
    /// </summary>
    Quarterly = 3,

    /// <summary>
    /// Annual budget period.
    /// </summary>
    Annually = 4
}
