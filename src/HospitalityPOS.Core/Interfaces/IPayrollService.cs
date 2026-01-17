using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for payroll processing operations.
/// </summary>
public interface IPayrollService
{
    // Payroll period management
    Task<PayrollPeriod> CreatePayrollPeriodAsync(string periodName, DateTime startDate, DateTime endDate, DateTime payDate, CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollPeriod>> GetAllPayrollPeriodsAsync(int? year = null, CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetCurrentPayrollPeriodAsync(CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetLatestPayrollPeriodAsync(CancellationToken cancellationToken = default);

    // Payroll processing
    Task<PayrollPeriod> ProcessPayrollAsync(int periodId, int processedByUserId, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> ApprovePayrollAsync(int periodId, int approvedByUserId, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> MarkAsPaidAsync(int periodId, CancellationToken cancellationToken = default);

    // Payslip management
    Task<Payslip> GeneratePayslipAsync(int periodId, int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payslip>> GenerateAllPayslipsAsync(int periodId, CancellationToken cancellationToken = default);
    Task<Payslip?> GetPayslipByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payslip>> GetPayslipsForPeriodAsync(int periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payslip>> GetEmployeePayslipsAsync(int employeeId, int? year = null, CancellationToken cancellationToken = default);
    Task<Payslip> RecalculatePayslipAsync(int payslipId, CancellationToken cancellationToken = default);

    // Payslip payment
    Task<Payslip> MarkPayslipAsPaidAsync(int payslipId, string paymentMethod, string? paymentReference = null, CancellationToken cancellationToken = default);
    Task<int> MarkAllPayslipsAsPaidAsync(int periodId, string paymentMethod, CancellationToken cancellationToken = default);

    // Kenya statutory calculations
    Task<StatutoryDeductions> CalculateStatutoryDeductionsAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePAYEAsync(decimal taxableIncome, CancellationToken cancellationToken = default);
    Task<decimal> CalculateNHIFAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateNSSFAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateHousingLevyAsync(decimal grossPay, CancellationToken cancellationToken = default);

    // Reports
    Task<PayrollSummary> GetPayrollSummaryAsync(int periodId, CancellationToken cancellationToken = default);
    Task<string> GeneratePayslipHtmlAsync(int payslipId, CancellationToken cancellationToken = default);
    Task<string> GeneratePayrollReportHtmlAsync(int periodId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kenya statutory deductions.
/// </summary>
public class StatutoryDeductions
{
    public decimal PAYE { get; set; }
    public decimal NHIF { get; set; }
    public decimal NSSF { get; set; }
    public decimal HousingLevy { get; set; }
    public decimal TotalDeductions => PAYE + NHIF + NSSF + HousingLevy;
}

/// <summary>
/// Payroll summary for a period.
/// </summary>
public class PayrollSummary
{
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalPAYE { get; set; }
    public decimal TotalNHIF { get; set; }
    public decimal TotalNSSF { get; set; }
    public decimal TotalHousingLevy { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
}
