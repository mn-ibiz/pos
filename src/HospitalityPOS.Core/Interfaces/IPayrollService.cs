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
    Task<EnhancedStatutoryDeductions> CalculateEnhancedStatutoryDeductionsAsync(decimal grossPay, int employeeId, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePAYEAsync(decimal taxableIncome, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePAYEWithReliefsAsync(decimal taxableIncome, int employeeId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateNHIFAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateSHIFAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateNSSFAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateHousingLevyAsync(decimal grossPay, CancellationToken cancellationToken = default);
    Task<decimal> CalculateHELBDeductionAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateTotalTaxReliefsAsync(int employeeId, decimal grossPay, CancellationToken cancellationToken = default);

    // Tax relief management
    Task<EmployeeTaxRelief> AddTaxReliefAsync(EmployeeTaxRelief relief, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeTaxRelief>> GetEmployeeTaxReliefsAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeTaxRelief> UpdateTaxReliefAsync(EmployeeTaxRelief relief, CancellationToken cancellationToken = default);
    Task DeleteTaxReliefAsync(int reliefId, CancellationToken cancellationToken = default);

    // HELB deduction management
    Task<HelbDeduction> AddHelbDeductionAsync(HelbDeduction deduction, CancellationToken cancellationToken = default);
    Task<HelbDeduction?> GetActiveHelbDeductionAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<HelbDeduction> UpdateHelbDeductionAsync(HelbDeduction deduction, CancellationToken cancellationToken = default);
    Task<HelbDeduction> RecordHelbPaymentAsync(int employeeId, decimal amount, int payslipId, CancellationToken cancellationToken = default);

    // Loan deductions for payroll
    Task<decimal> CalculateLoanDeductionsAsync(int employeeId, CancellationToken cancellationToken = default);
    Task ProcessLoanRepaymentsAsync(int payslipId, CancellationToken cancellationToken = default);

    // P9 Form generation
    Task<P9Record> GenerateP9FormAsync(int employeeId, int taxYear, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<P9Record>> GenerateAllP9FormsAsync(int taxYear, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<P9Record?> GetP9RecordAsync(int employeeId, int taxYear, CancellationToken cancellationToken = default);
    Task<string> GenerateP9HtmlAsync(int employeeId, int taxYear, CancellationToken cancellationToken = default);

    // Statutory returns
    Task<StatutoryReturn> GeneratePAYEReturnAsync(int periodId, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<StatutoryReturn> GenerateNSSFReturnAsync(int periodId, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<StatutoryReturn> GenerateSHIFReturnAsync(int periodId, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<StatutoryReturn> GenerateHousingLevyReturnAsync(int periodId, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<StatutoryReturn> GenerateHELBReturnAsync(int periodId, int generatedByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatutoryReturn>> GetStatutoryReturnsAsync(int? periodId = null, StatutoryReturnType? returnType = null, CancellationToken cancellationToken = default);
    Task<StatutoryReturn> MarkReturnAsSubmittedAsync(int returnId, string submissionReference, CancellationToken cancellationToken = default);

    // Reports
    Task<PayrollSummary> GetPayrollSummaryAsync(int periodId, CancellationToken cancellationToken = default);
    Task<EnhancedPayrollSummary> GetEnhancedPayrollSummaryAsync(int periodId, CancellationToken cancellationToken = default);
    Task<string> GeneratePayslipHtmlAsync(int payslipId, CancellationToken cancellationToken = default);
    Task<string> GenerateEnhancedPayslipHtmlAsync(int payslipId, CancellationToken cancellationToken = default);
    Task<string> GeneratePayrollReportHtmlAsync(int periodId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kenya statutory deductions (basic).
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
/// Enhanced Kenya statutory deductions with SHIF, HELB, loans, and tax reliefs.
/// </summary>
public class EnhancedStatutoryDeductions
{
    public decimal GrossPay { get; set; }
    public decimal TaxableIncome { get; set; }

    // Tax reliefs (reduce tax payable)
    public decimal PersonalRelief { get; set; }
    public decimal InsuranceRelief { get; set; }
    public decimal MortgageRelief { get; set; }
    public decimal HousingLevyRelief { get; set; }
    public decimal DisabilityExemption { get; set; }
    public decimal PensionRelief { get; set; }
    public decimal TotalTaxReliefs => PersonalRelief + InsuranceRelief + MortgageRelief +
        HousingLevyRelief + DisabilityExemption + PensionRelief;

    // Statutory deductions
    public decimal PAYE { get; set; }
    public decimal PAYEBeforeReliefs { get; set; }
    public decimal NSSF { get; set; }
    public decimal SHIF { get; set; }           // Social Health Insurance Fund (replaced NHIF)
    public decimal NHIF { get; set; }           // Legacy NHIF (for comparison/transition)
    public decimal HousingLevy { get; set; }

    // Other deductions
    public decimal HELBDeduction { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal SalaryAdvanceDeductions { get; set; }

    public decimal TotalStatutoryDeductions => PAYE + NSSF + SHIF + HousingLevy;
    public decimal TotalOtherDeductions => HELBDeduction + LoanDeductions + SalaryAdvanceDeductions;
    public decimal TotalDeductions => TotalStatutoryDeductions + TotalOtherDeductions;
    public decimal NetPay => GrossPay - TotalDeductions;
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

/// <summary>
/// Enhanced payroll summary with SHIF, HELB, loans, and tax reliefs.
/// </summary>
public class EnhancedPayrollSummary : PayrollSummary
{
    // SHIF (replaces NHIF)
    public decimal TotalSHIF { get; set; }

    // HELB deductions
    public decimal TotalHELBDeductions { get; set; }
    public int HELBDeductionCount { get; set; }

    // Loan deductions
    public decimal TotalLoanDeductions { get; set; }
    public decimal TotalSalaryAdvanceDeductions { get; set; }
    public int LoansDeductedCount { get; set; }

    // Tax reliefs applied
    public decimal TotalPersonalRelief { get; set; }
    public decimal TotalInsuranceRelief { get; set; }
    public decimal TotalMortgageRelief { get; set; }
    public decimal TotalHousingLevyRelief { get; set; }
    public decimal TotalOtherReliefs { get; set; }
    public decimal TotalTaxReliefs => TotalPersonalRelief + TotalInsuranceRelief +
        TotalMortgageRelief + TotalHousingLevyRelief + TotalOtherReliefs;

    // Statutory return status
    public bool PAYEReturnGenerated { get; set; }
    public bool NSSFReturnGenerated { get; set; }
    public bool SHIFReturnGenerated { get; set; }
    public bool HousingLevyReturnGenerated { get; set; }
    public bool HELBReturnGenerated { get; set; }

    // Dates
    public DateTime? PayrollProcessedAt { get; set; }
    public DateTime? PayrollApprovedAt { get; set; }
}
