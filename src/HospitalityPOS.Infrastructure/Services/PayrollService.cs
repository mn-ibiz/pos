using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for payroll processing with Kenya statutory calculations.
/// Includes SHIF (replacing NHIF), HELB, tax reliefs, P9 forms, and statutory returns.
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly POSDbContext _context;

    // Kenya 2024/2025 Tax Bands (Monthly)
    private static readonly (decimal UpperLimit, decimal Rate)[] TaxBands =
    [
        (24_000m, 0.10m),      // First 24,000 at 10%
        (8_333m, 0.25m),       // Next 8,333 at 25%
        (467_667m, 0.30m),     // Next 467,667 at 30%
        (300_000m, 0.325m),    // Next 300,000 at 32.5%
        (decimal.MaxValue, 0.35m)  // Above 800,000 at 35%
    ];

    private const decimal PersonalRelief = 2_400m; // Monthly personal relief

    // NSSF Tier I and II rates (2024)
    private const decimal NssfTier1Limit = 7_000m;
    private const decimal NssfTier2Limit = 36_000m;
    private const decimal NssfRate = 0.06m; // 6%

    // Housing Levy rate
    private const decimal HousingLevyRate = 0.015m; // 1.5%
    private const decimal HousingLevyReliefRate = 0.15m; // 15% relief on Housing Levy

    // SHIF (Social Health Insurance Fund) - replaced NHIF in 2024
    private const decimal ShifRate = 0.0275m; // 2.75% of gross pay
    private const decimal ShifMinimum = 300m; // Minimum KES 300/month

    // Insurance relief caps
    private const decimal InsuranceReliefRate = 0.15m; // 15% of premiums
    private const decimal InsuranceReliefMaxMonthly = 5_000m; // Max KES 5,000/month

    // Mortgage interest relief caps
    private const decimal MortgageReliefRate = 0.15m; // 15% of interest
    private const decimal MortgageReliefMaxMonthly = 25_000m; // Max KES 25,000/month

    // Disability exemption
    private const decimal DisabilityExemptionMax = 150_000m; // Max exemption

    // Pension contribution relief
    private const decimal PensionReliefMaxMonthly = 20_000m; // Max KES 20,000/month
    private const decimal PensionReliefMaxPercent = 0.30m; // Or 30% of gross

    // NHIF bands (legacy - for comparison)
    private static readonly (decimal LowerLimit, decimal UpperLimit, decimal Contribution)[] NhifBands =
    [
        (0m, 5_999m, 150m),
        (6_000m, 7_999m, 300m),
        (8_000m, 11_999m, 400m),
        (12_000m, 14_999m, 500m),
        (15_000m, 19_999m, 600m),
        (20_000m, 24_999m, 750m),
        (25_000m, 29_999m, 850m),
        (30_000m, 34_999m, 900m),
        (35_000m, 39_999m, 950m),
        (40_000m, 44_999m, 1_000m),
        (45_000m, 49_999m, 1_100m),
        (50_000m, 59_999m, 1_200m),
        (60_000m, 69_999m, 1_300m),
        (70_000m, 79_999m, 1_400m),
        (80_000m, 89_999m, 1_500m),
        (90_000m, 99_999m, 1_600m),
        (100_000m, decimal.MaxValue, 1_700m)
    ];

    public PayrollService(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollPeriod> CreatePayrollPeriodAsync(string periodName, DateTime startDate, DateTime endDate, DateTime payDate, CancellationToken cancellationToken = default)
    {
        var period = new PayrollPeriod
        {
            PeriodName = periodName,
            StartDate = startDate,
            EndDate = endDate,
            PayDate = payDate,
            Status = PayrollStatus.Draft
        };

        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.ProcessedByUser)
            .Include(p => p.ApprovedByUser)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollPeriod>> GetAllPayrollPeriodsAsync(int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollPeriods.AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.StartDate.Year == year.Value);
        }

        return await query
            .Include(p => p.Payslips)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PayrollPeriod?> GetCurrentPayrollPeriodAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= today && p.EndDate >= today, cancellationToken);
    }

    public async Task<PayrollPeriod?> GetLatestPayrollPeriodAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PayrollPeriods
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PayrollPeriod> ProcessPayrollAsync(int periodId, int processedByUserId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period with ID {periodId} not found.");

        if (period.Status != PayrollStatus.Draft)
        {
            throw new InvalidOperationException("Only draft payroll periods can be processed.");
        }

        // Generate payslips for all active employees
        await GenerateAllPayslipsAsync(periodId, cancellationToken);

        period.Status = PayrollStatus.Processing;
        period.ProcessedByUserId = processedByUserId;
        period.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PayrollPeriod> ApprovePayrollAsync(int periodId, int approvedByUserId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period with ID {periodId} not found.");

        if (period.Status != PayrollStatus.Processing)
        {
            throw new InvalidOperationException("Only processing payroll periods can be approved.");
        }

        period.Status = PayrollStatus.Approved;
        period.ApprovedByUserId = approvedByUserId;
        period.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PayrollPeriod> MarkAsPaidAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period with ID {periodId} not found.");

        if (period.Status != PayrollStatus.Approved)
        {
            throw new InvalidOperationException("Only approved payroll periods can be marked as paid.");
        }

        period.Status = PayrollStatus.Paid;

        // Mark all payslips as paid
        foreach (var payslip in period.Payslips)
        {
            payslip.PaymentStatus = PayslipPaymentStatus.Paid;
            payslip.PaidAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<Payslip> GeneratePayslipAsync(int periodId, int employeeId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period with ID {periodId} not found.");

        var employee = await _context.Employees
            .Include(e => e.EmployeeSalaryComponents)
                .ThenInclude(esc => esc.SalaryComponent)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        // Check if payslip already exists
        var existingPayslip = await _context.Payslips
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == periodId && p.EmployeeId == employeeId, cancellationToken);

        if (existingPayslip != null)
        {
            return await RecalculatePayslipAsync(existingPayslip.Id, cancellationToken);
        }

        var payslip = new Payslip
        {
            PayrollPeriodId = periodId,
            EmployeeId = employeeId,
            BasicSalary = employee.BasicSalary,
            PaymentStatus = PayslipPaymentStatus.Pending
        };

        _context.Payslips.Add(payslip);
        await _context.SaveChangesAsync(cancellationToken);

        // Calculate earnings and deductions
        await CalculatePayslipDetailsAsync(payslip, employee, cancellationToken);

        return payslip;
    }

    public async Task<IReadOnlyList<Payslip>> GenerateAllPayslipsAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null)
            .ToListAsync(cancellationToken);

        var payslips = new List<Payslip>();
        foreach (var employee in employees)
        {
            var payslip = await GeneratePayslipAsync(periodId, employee.Id, cancellationToken);
            payslips.Add(payslip);
        }

        return payslips;
    }

    public async Task<Payslip?> GetPayslipByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.PayrollPeriod)
            .Include(p => p.PayslipDetails)
                .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payslip>> GetPayslipsForPeriodAsync(int periodId, CancellationToken cancellationToken = default)
    {
        return await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.PayslipDetails)
                .ThenInclude(pd => pd.SalaryComponent)
            .Where(p => p.PayrollPeriodId == periodId)
            .OrderBy(p => p.Employee.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payslip>> GetEmployeePayslipsAsync(int employeeId, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Payslips
            .Include(p => p.PayrollPeriod)
            .Include(p => p.PayslipDetails)
                .ThenInclude(pd => pd.SalaryComponent)
            .Where(p => p.EmployeeId == employeeId);

        if (year.HasValue)
        {
            query = query.Where(p => p.PayrollPeriod.StartDate.Year == year.Value);
        }

        return await query
            .OrderByDescending(p => p.PayrollPeriod.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payslip> RecalculatePayslipAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _context.Payslips
            .Include(p => p.PayslipDetails)
            .FirstOrDefaultAsync(p => p.Id == payslipId, cancellationToken)
            ?? throw new InvalidOperationException($"Payslip with ID {payslipId} not found.");

        var employee = await _context.Employees
            .Include(e => e.EmployeeSalaryComponents)
                .ThenInclude(esc => esc.SalaryComponent)
            .FirstOrDefaultAsync(e => e.Id == payslip.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        // Remove existing details
        _context.PayslipDetails.RemoveRange(payslip.PayslipDetails);

        // Recalculate
        payslip.BasicSalary = employee.BasicSalary;
        await CalculatePayslipDetailsAsync(payslip, employee, cancellationToken);

        return payslip;
    }

    public async Task<Payslip> MarkPayslipAsPaidAsync(int payslipId, string paymentMethod, string? paymentReference = null, CancellationToken cancellationToken = default)
    {
        var payslip = await _context.Payslips.FindAsync([payslipId], cancellationToken)
            ?? throw new InvalidOperationException($"Payslip with ID {payslipId} not found.");

        payslip.PaymentStatus = PayslipPaymentStatus.Paid;
        payslip.PaymentMethod = paymentMethod;
        payslip.PaymentReference = paymentReference;
        payslip.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return payslip;
    }

    public async Task<int> MarkAllPayslipsAsPaidAsync(int periodId, string paymentMethod, CancellationToken cancellationToken = default)
    {
        var payslips = await _context.Payslips
            .Where(p => p.PayrollPeriodId == periodId && p.PaymentStatus == PayslipPaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var payslip in payslips)
        {
            payslip.PaymentStatus = PayslipPaymentStatus.Paid;
            payslip.PaymentMethod = paymentMethod;
            payslip.PaidAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return payslips.Count;
    }

    public Task<StatutoryDeductions> CalculateStatutoryDeductionsAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        var paye = CalculatePAYE(grossPay);
        var nhif = CalculateNHIF(grossPay);
        var nssf = CalculateNSSF(grossPay);
        var housingLevy = CalculateHousingLevy(grossPay);

        return Task.FromResult(new StatutoryDeductions
        {
            PAYE = paye,
            NHIF = nhif,
            NSSF = nssf,
            HousingLevy = housingLevy
        });
    }

    public Task<decimal> CalculatePAYEAsync(decimal taxableIncome, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculatePAYE(taxableIncome));
    }

    public Task<decimal> CalculateNHIFAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateNHIF(grossPay));
    }

    public Task<decimal> CalculateNSSFAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateNSSF(grossPay));
    }

    public Task<decimal> CalculateHousingLevyAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateHousingLevy(grossPay));
    }

    public async Task<PayrollSummary> GetPayrollSummaryAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period with ID {periodId} not found.");

        var payslips = period.Payslips;

        return new PayrollSummary
        {
            PeriodId = periodId,
            PeriodName = period.PeriodName,
            EmployeeCount = payslips.Count,
            TotalBasicSalary = payslips.Sum(p => p.BasicSalary),
            TotalEarnings = payslips.Sum(p => p.TotalEarnings),
            TotalDeductions = payslips.Sum(p => p.TotalDeductions),
            TotalNetPay = payslips.Sum(p => p.NetPay),
            TotalPAYE = payslips.SelectMany(p => p.PayslipDetails)
                .Where(pd => pd.SalaryComponent.Name == "PAYE")
                .Sum(pd => pd.Amount),
            TotalNHIF = payslips.SelectMany(p => p.PayslipDetails)
                .Where(pd => pd.SalaryComponent.Name == "NHIF")
                .Sum(pd => pd.Amount),
            TotalNSSF = payslips.SelectMany(p => p.PayslipDetails)
                .Where(pd => pd.SalaryComponent.Name == "NSSF")
                .Sum(pd => pd.Amount),
            TotalHousingLevy = payslips.SelectMany(p => p.PayslipDetails)
                .Where(pd => pd.SalaryComponent.Name == "Housing Levy")
                .Sum(pd => pd.Amount),
            PaidCount = payslips.Count(p => p.PaymentStatus == PayslipPaymentStatus.Paid),
            PendingCount = payslips.Count(p => p.PaymentStatus == PayslipPaymentStatus.Pending)
        };
    }

    public async Task<string> GeneratePayslipHtmlAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await GetPayslipByIdAsync(payslipId, cancellationToken)
            ?? throw new InvalidOperationException($"Payslip with ID {payslipId} not found.");

        var earnings = payslip.PayslipDetails.Where(d => d.ComponentType == ComponentType.Earning).ToList();
        var deductions = payslip.PayslipDetails.Where(d => d.ComponentType == ComponentType.Deduction).ToList();

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px; }");
        html.AppendLine(".section { margin-bottom: 20px; }");
        html.AppendLine(".section-title { font-weight: bold; border-bottom: 2px solid #333; padding-bottom: 5px; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        html.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        html.AppendLine("th { background-color: #f5f5f5; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine(".total-row { font-weight: bold; background-color: #f0f0f0; }");
        html.AppendLine(".net-pay { font-size: 1.2em; font-weight: bold; color: #2e7d32; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>PAYSLIP</h1>");
        html.AppendLine($"<p>Period: {payslip.PayrollPeriod.PeriodName}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='info-grid'>");
        html.AppendLine("<div>");
        html.AppendLine($"<p><strong>Employee:</strong> {payslip.Employee.FullName}</p>");
        html.AppendLine($"<p><strong>Employee No:</strong> {payslip.Employee.EmployeeNumber}</p>");
        html.AppendLine($"<p><strong>Department:</strong> {payslip.Employee.Department ?? "N/A"}</p>");
        html.AppendLine("</div>");
        html.AppendLine("<div>");
        html.AppendLine($"<p><strong>Position:</strong> {payslip.Employee.Position ?? "N/A"}</p>");
        html.AppendLine($"<p><strong>Pay Date:</strong> {payslip.PayrollPeriod.PayDate:dd/MM/yyyy}</p>");
        html.AppendLine($"<p><strong>Bank:</strong> {payslip.Employee.BankName ?? "N/A"}</p>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Earnings
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>EARNINGS</div>");
        html.AppendLine("<table><tr><th>Description</th><th class='amount'>Amount (KSh)</th></tr>");
        html.AppendLine($"<tr><td>Basic Salary</td><td class='amount'>{payslip.BasicSalary:N2}</td></tr>");
        foreach (var earning in earnings)
        {
            html.AppendLine($"<tr><td>{earning.SalaryComponent.Name}</td><td class='amount'>{earning.Amount:N2}</td></tr>");
        }
        html.AppendLine($"<tr class='total-row'><td>Total Earnings</td><td class='amount'>{payslip.TotalEarnings:N2}</td></tr>");
        html.AppendLine("</table></div>");

        // Deductions
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>DEDUCTIONS</div>");
        html.AppendLine("<table><tr><th>Description</th><th class='amount'>Amount (KSh)</th></tr>");
        foreach (var deduction in deductions)
        {
            html.AppendLine($"<tr><td>{deduction.SalaryComponent.Name}</td><td class='amount'>{deduction.Amount:N2}</td></tr>");
        }
        html.AppendLine($"<tr class='total-row'><td>Total Deductions</td><td class='amount'>{payslip.TotalDeductions:N2}</td></tr>");
        html.AppendLine("</table></div>");

        // Net Pay
        html.AppendLine("<div class='section'>");
        html.AppendLine($"<p class='net-pay'>NET PAY: KSh {payslip.NetPay:N2}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    public async Task<string> GeneratePayrollReportHtmlAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var summary = await GetPayrollSummaryAsync(periodId, cancellationToken);
        var payslips = await GetPayslipsForPeriodAsync(periodId, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 30px; }");
        html.AppendLine(".summary-card { background: #f5f5f5; padding: 15px; border-radius: 8px; text-align: center; }");
        html.AppendLine(".summary-value { font-size: 1.5em; font-weight: bold; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; }");
        html.AppendLine("th, td { padding: 10px; text-align: left; border: 1px solid #ddd; }");
        html.AppendLine("th { background-color: #2d2d44; color: white; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>PAYROLL REPORT</h1>");
        html.AppendLine($"<p>Period: {summary.PeriodName}</p>");
        html.AppendLine($"<p>Generated: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='summary'>");
        html.AppendLine($"<div class='summary-card'><div>Employees</div><div class='summary-value'>{summary.EmployeeCount}</div></div>");
        html.AppendLine($"<div class='summary-card'><div>Total Earnings</div><div class='summary-value'>KSh {summary.TotalEarnings:N0}</div></div>");
        html.AppendLine($"<div class='summary-card'><div>Total Deductions</div><div class='summary-value'>KSh {summary.TotalDeductions:N0}</div></div>");
        html.AppendLine($"<div class='summary-card'><div>Total Net Pay</div><div class='summary-value'>KSh {summary.TotalNetPay:N0}</div></div>");
        html.AppendLine("</div>");

        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Employee</th><th>Basic Salary</th><th>Earnings</th><th>Deductions</th><th>Net Pay</th><th>Status</th></tr>");
        foreach (var payslip in payslips)
        {
            var status = payslip.PaymentStatus == PayslipPaymentStatus.Paid ? "Paid" : "Pending";
            html.AppendLine($"<tr>");
            html.AppendLine($"<td>{payslip.Employee.FullName}</td>");
            html.AppendLine($"<td class='amount'>{payslip.BasicSalary:N2}</td>");
            html.AppendLine($"<td class='amount'>{payslip.TotalEarnings:N2}</td>");
            html.AppendLine($"<td class='amount'>{payslip.TotalDeductions:N2}</td>");
            html.AppendLine($"<td class='amount'>{payslip.NetPay:N2}</td>");
            html.AppendLine($"<td>{status}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private async Task CalculatePayslipDetailsAsync(Payslip payslip, Employee employee, CancellationToken cancellationToken)
    {
        var grossPay = employee.BasicSalary;
        var totalEarnings = grossPay;
        var totalDeductions = 0m;

        // Get or create standard salary components
        var payeComponent = await GetOrCreateStatutoryComponentAsync("PAYE", ComponentType.Deduction, true, cancellationToken);
        var nhifComponent = await GetOrCreateStatutoryComponentAsync("NHIF", ComponentType.Deduction, true, cancellationToken);
        var nssfComponent = await GetOrCreateStatutoryComponentAsync("NSSF", ComponentType.Deduction, true, cancellationToken);
        var housingLevyComponent = await GetOrCreateStatutoryComponentAsync("Housing Levy", ComponentType.Deduction, true, cancellationToken);

        // Process employee-specific components
        var employeeComponents = employee.EmployeeSalaryComponents
            .Where(esc => esc.EffectiveTo == null || esc.EffectiveTo >= DateTime.Today)
            .ToList();

        foreach (var component in employeeComponents)
        {
            decimal amount;
            if (component.Amount.HasValue)
            {
                amount = component.Amount.Value;
            }
            else if (component.Percent.HasValue)
            {
                amount = grossPay * (component.Percent.Value / 100);
            }
            else if (component.SalaryComponent.DefaultAmount.HasValue)
            {
                amount = component.SalaryComponent.DefaultAmount.Value;
            }
            else if (component.SalaryComponent.DefaultPercent.HasValue)
            {
                amount = grossPay * (component.SalaryComponent.DefaultPercent.Value / 100);
            }
            else
            {
                continue;
            }

            var detail = new PayslipDetail
            {
                PayslipId = payslip.Id,
                SalaryComponentId = component.SalaryComponentId,
                ComponentType = component.SalaryComponent.ComponentType,
                Amount = amount
            };

            _context.PayslipDetails.Add(detail);

            if (component.SalaryComponent.ComponentType == ComponentType.Earning)
            {
                totalEarnings += amount;
                if (component.SalaryComponent.IsTaxable)
                {
                    grossPay += amount;
                }
            }
            else
            {
                totalDeductions += amount;
            }
        }

        // Calculate statutory deductions
        var statutory = await CalculateStatutoryDeductionsAsync(grossPay, cancellationToken);

        // Add PAYE
        _context.PayslipDetails.Add(new PayslipDetail
        {
            PayslipId = payslip.Id,
            SalaryComponentId = payeComponent.Id,
            ComponentType = ComponentType.Deduction,
            Amount = statutory.PAYE
        });
        totalDeductions += statutory.PAYE;

        // Add NHIF
        _context.PayslipDetails.Add(new PayslipDetail
        {
            PayslipId = payslip.Id,
            SalaryComponentId = nhifComponent.Id,
            ComponentType = ComponentType.Deduction,
            Amount = statutory.NHIF
        });
        totalDeductions += statutory.NHIF;

        // Add NSSF
        _context.PayslipDetails.Add(new PayslipDetail
        {
            PayslipId = payslip.Id,
            SalaryComponentId = nssfComponent.Id,
            ComponentType = ComponentType.Deduction,
            Amount = statutory.NSSF
        });
        totalDeductions += statutory.NSSF;

        // Add Housing Levy
        _context.PayslipDetails.Add(new PayslipDetail
        {
            PayslipId = payslip.Id,
            SalaryComponentId = housingLevyComponent.Id,
            ComponentType = ComponentType.Deduction,
            Amount = statutory.HousingLevy
        });
        totalDeductions += statutory.HousingLevy;

        // Update payslip totals
        payslip.TotalEarnings = totalEarnings;
        payslip.TotalDeductions = totalDeductions;
        payslip.NetPay = totalEarnings - totalDeductions;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<SalaryComponent> GetOrCreateStatutoryComponentAsync(string name, ComponentType type, bool isStatutory, CancellationToken cancellationToken)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Name == name && sc.IsStatutory, cancellationToken);

        if (component == null)
        {
            component = new SalaryComponent
            {
                Name = name,
                ComponentType = type,
                IsStatutory = isStatutory,
                IsTaxable = false,
                IsFixed = false,
                DisplayOrder = type == ComponentType.Deduction ? 100 : 0
            };
            _context.SalaryComponents.Add(component);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return component;
    }

    private static decimal CalculatePAYE(decimal grossPay)
    {
        // Subtract NSSF (tax-deductible up to Tier II limit)
        var nssfDeduction = CalculateNSSF(grossPay);
        var taxableIncome = grossPay - nssfDeduction;

        if (taxableIncome <= 0) return 0;

        decimal tax = 0;
        decimal remaining = taxableIncome;
        decimal cumulativeLimit = 0;

        foreach (var (limit, rate) in TaxBands)
        {
            if (remaining <= 0) break;

            var taxableAtThisRate = Math.Min(remaining, limit);
            tax += taxableAtThisRate * rate;
            remaining -= taxableAtThisRate;
            cumulativeLimit += limit;
        }

        // Apply personal relief
        tax = Math.Max(0, tax - PersonalRelief);

        return Math.Round(tax, 2);
    }

    private static decimal CalculateNHIF(decimal grossPay)
    {
        foreach (var (lower, upper, contribution) in NhifBands)
        {
            if (grossPay >= lower && grossPay <= upper)
            {
                return contribution;
            }
        }
        return NhifBands[^1].Contribution;
    }

    private static decimal CalculateNSSF(decimal grossPay)
    {
        // Tier I: 6% of first 7,000
        var tier1Contribution = Math.Min(grossPay, NssfTier1Limit) * NssfRate;

        // Tier II: 6% of next 29,000 (7,001 to 36,000)
        var tier2Amount = Math.Max(0, Math.Min(grossPay, NssfTier2Limit) - NssfTier1Limit);
        var tier2Contribution = tier2Amount * NssfRate;

        return Math.Round(tier1Contribution + tier2Contribution, 2);
    }

    private static decimal CalculateHousingLevy(decimal grossPay)
    {
        return Math.Round(grossPay * HousingLevyRate, 2);
    }

    private static decimal CalculateSHIF(decimal grossPay)
    {
        // SHIF is 2.75% of gross pay with minimum of KES 300
        var shif = grossPay * ShifRate;
        return Math.Round(Math.Max(shif, ShifMinimum), 2);
    }

    #region SHIF and Enhanced Statutory Calculations

    public Task<decimal> CalculateSHIFAsync(decimal grossPay, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateSHIF(grossPay));
    }

    public async Task<EnhancedStatutoryDeductions> CalculateEnhancedStatutoryDeductionsAsync(
        decimal grossPay, int employeeId, CancellationToken cancellationToken = default)
    {
        var result = new EnhancedStatutoryDeductions
        {
            GrossPay = grossPay
        };

        // Calculate NSSF (tax-deductible)
        result.NSSF = CalculateNSSF(grossPay);

        // Calculate SHIF (replaces NHIF)
        result.SHIF = CalculateSHIF(grossPay);
        result.NHIF = CalculateNHIF(grossPay); // For reference/transition

        // Calculate Housing Levy
        result.HousingLevy = CalculateHousingLevy(grossPay);

        // Get employee tax reliefs
        var reliefs = await GetEmployeeTaxReliefsAsync(employeeId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var relief in reliefs.Where(r => r.IsActive &&
            r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today)))
        {
            switch (relief.ReliefType)
            {
                case TaxReliefType.PersonalRelief:
                    result.PersonalRelief = PersonalRelief;
                    break;
                case TaxReliefType.InsuranceRelief:
                    var insuranceRelief = relief.MonthlyAmount * InsuranceReliefRate;
                    result.InsuranceRelief = Math.Min(insuranceRelief, InsuranceReliefMaxMonthly);
                    break;
                case TaxReliefType.MortgageInterestRelief:
                    var mortgageRelief = relief.MonthlyAmount * MortgageReliefRate;
                    result.MortgageRelief = Math.Min(mortgageRelief, MortgageReliefMaxMonthly);
                    break;
                case TaxReliefType.HousingLevyRelief:
                    result.HousingLevyRelief = result.HousingLevy * HousingLevyReliefRate;
                    break;
                case TaxReliefType.DisabilityExemption:
                    result.DisabilityExemption = Math.Min(relief.MonthlyAmount, DisabilityExemptionMax);
                    break;
                case TaxReliefType.PensionContributionRelief:
                    var pensionMax = Math.Min(grossPay * PensionReliefMaxPercent, PensionReliefMaxMonthly);
                    result.PensionRelief = Math.Min(relief.MonthlyAmount, pensionMax);
                    break;
            }
        }

        // Default personal relief if not explicitly added
        if (result.PersonalRelief == 0)
        {
            result.PersonalRelief = PersonalRelief;
        }

        // Taxable income = Gross - NSSF (pension contribution is deductible)
        result.TaxableIncome = grossPay - result.NSSF;

        // Calculate PAYE before reliefs
        result.PAYEBeforeReliefs = CalculatePAYEWithoutRelief(result.TaxableIncome);

        // Apply all reliefs to PAYE
        result.PAYE = Math.Max(0, result.PAYEBeforeReliefs - result.TotalTaxReliefs);

        // Get HELB deduction
        result.HELBDeduction = await CalculateHELBDeductionAsync(employeeId, cancellationToken);

        // Get loan deductions
        var loanDeductions = await CalculateLoanDeductionsDetailedAsync(employeeId, cancellationToken);
        result.LoanDeductions = loanDeductions.EmployeeLoanDeductions;
        result.SalaryAdvanceDeductions = loanDeductions.SalaryAdvanceDeductions;

        return result;
    }

    private static decimal CalculatePAYEWithoutRelief(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0;

        decimal tax = 0;
        decimal remaining = taxableIncome;

        foreach (var (limit, rate) in TaxBands)
        {
            if (remaining <= 0) break;

            var taxableAtThisRate = Math.Min(remaining, limit);
            tax += taxableAtThisRate * rate;
            remaining -= taxableAtThisRate;
        }

        return Math.Round(tax, 2);
    }

    public async Task<decimal> CalculatePAYEWithReliefsAsync(decimal taxableIncome, int employeeId,
        CancellationToken cancellationToken = default)
    {
        var taxBeforeReliefs = CalculatePAYEWithoutRelief(taxableIncome);
        var totalReliefs = await CalculateTotalTaxReliefsAsync(employeeId, taxableIncome, cancellationToken);
        return Math.Max(0, taxBeforeReliefs - totalReliefs);
    }

    public async Task<decimal> CalculateTotalTaxReliefsAsync(int employeeId, decimal grossPay,
        CancellationToken cancellationToken = default)
    {
        var reliefs = await GetEmployeeTaxReliefsAsync(employeeId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);

        decimal totalRelief = PersonalRelief; // Always include personal relief

        foreach (var relief in reliefs.Where(r => r.IsActive &&
            r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today)))
        {
            switch (relief.ReliefType)
            {
                case TaxReliefType.InsuranceRelief:
                    totalRelief += Math.Min(relief.MonthlyAmount * InsuranceReliefRate, InsuranceReliefMaxMonthly);
                    break;
                case TaxReliefType.MortgageInterestRelief:
                    totalRelief += Math.Min(relief.MonthlyAmount * MortgageReliefRate, MortgageReliefMaxMonthly);
                    break;
                case TaxReliefType.HousingLevyRelief:
                    totalRelief += CalculateHousingLevy(grossPay) * HousingLevyReliefRate;
                    break;
                case TaxReliefType.DisabilityExemption:
                    totalRelief += Math.Min(relief.MonthlyAmount, DisabilityExemptionMax);
                    break;
                case TaxReliefType.PensionContributionRelief:
                    var pensionMax = Math.Min(grossPay * PensionReliefMaxPercent, PensionReliefMaxMonthly);
                    totalRelief += Math.Min(relief.MonthlyAmount, pensionMax);
                    break;
            }
        }

        return totalRelief;
    }

    #endregion

    #region Tax Relief Management

    public async Task<EmployeeTaxRelief> AddTaxReliefAsync(EmployeeTaxRelief relief, CancellationToken cancellationToken = default)
    {
        // Calculate the relief amount based on type
        relief.CalculatedRelief = CalculateTaxReliefAmount(relief);

        _context.Set<EmployeeTaxRelief>().Add(relief);
        await _context.SaveChangesAsync(cancellationToken);
        return relief;
    }

    public async Task<IReadOnlyList<EmployeeTaxRelief>> GetEmployeeTaxReliefsAsync(int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<EmployeeTaxRelief>()
            .Where(r => r.EmployeeId == employeeId)
            .OrderBy(r => r.ReliefType)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeTaxRelief> UpdateTaxReliefAsync(EmployeeTaxRelief relief,
        CancellationToken cancellationToken = default)
    {
        relief.CalculatedRelief = CalculateTaxReliefAmount(relief);
        _context.Set<EmployeeTaxRelief>().Update(relief);
        await _context.SaveChangesAsync(cancellationToken);
        return relief;
    }

    public async Task DeleteTaxReliefAsync(int reliefId, CancellationToken cancellationToken = default)
    {
        var relief = await _context.Set<EmployeeTaxRelief>().FindAsync([reliefId], cancellationToken);
        if (relief != null)
        {
            _context.Set<EmployeeTaxRelief>().Remove(relief);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private decimal CalculateTaxReliefAmount(EmployeeTaxRelief relief)
    {
        return relief.ReliefType switch
        {
            TaxReliefType.PersonalRelief => PersonalRelief,
            TaxReliefType.InsuranceRelief => Math.Min(relief.MonthlyAmount * InsuranceReliefRate, InsuranceReliefMaxMonthly),
            TaxReliefType.MortgageInterestRelief => Math.Min(relief.MonthlyAmount * MortgageReliefRate, MortgageReliefMaxMonthly),
            TaxReliefType.HousingLevyRelief => relief.MonthlyAmount * HousingLevyReliefRate,
            TaxReliefType.DisabilityExemption => Math.Min(relief.MonthlyAmount, DisabilityExemptionMax),
            TaxReliefType.PensionContributionRelief => Math.Min(relief.MonthlyAmount, PensionReliefMaxMonthly),
            _ => 0m
        };
    }

    #endregion

    #region HELB Deduction Management

    public async Task<HelbDeduction> AddHelbDeductionAsync(HelbDeduction deduction,
        CancellationToken cancellationToken = default)
    {
        // Deactivate any existing active HELB deductions for this employee
        var existingActive = await _context.Set<HelbDeduction>()
            .Where(h => h.EmployeeId == deduction.EmployeeId && h.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingActive)
        {
            existing.IsActive = false;
            existing.EffectiveTo = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        }

        _context.Set<HelbDeduction>().Add(deduction);
        await _context.SaveChangesAsync(cancellationToken);

        // Update employee's HELB flag
        var employee = await _context.Employees.FindAsync([deduction.EmployeeId], cancellationToken);
        if (employee != null)
        {
            employee.HasHelbDeduction = true;
            employee.HelbNumber = deduction.HelbAccountNumber;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return deduction;
    }

    public async Task<HelbDeduction?> GetActiveHelbDeductionAsync(int employeeId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Set<HelbDeduction>()
            .FirstOrDefaultAsync(h => h.EmployeeId == employeeId && h.IsActive &&
                h.EffectiveFrom <= today && (h.EffectiveTo == null || h.EffectiveTo >= today),
                cancellationToken);
    }

    public async Task<HelbDeduction> UpdateHelbDeductionAsync(HelbDeduction deduction,
        CancellationToken cancellationToken = default)
    {
        _context.Set<HelbDeduction>().Update(deduction);
        await _context.SaveChangesAsync(cancellationToken);
        return deduction;
    }

    public async Task<HelbDeduction> RecordHelbPaymentAsync(int employeeId, decimal amount, int payslipId,
        CancellationToken cancellationToken = default)
    {
        var deduction = await GetActiveHelbDeductionAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException($"No active HELB deduction found for employee {employeeId}");

        deduction.TotalRepaid += amount;
        deduction.LastDeductionMonth = DateOnly.FromDateTime(DateTime.Today);

        // Check if fully repaid
        if (deduction.OutstandingBalance <= 0)
        {
            deduction.IsActive = false;
            deduction.EffectiveTo = DateOnly.FromDateTime(DateTime.Today);

            var employee = await _context.Employees.FindAsync([employeeId], cancellationToken);
            if (employee != null)
            {
                employee.HasHelbDeduction = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return deduction;
    }

    public async Task<decimal> CalculateHELBDeductionAsync(int employeeId,
        CancellationToken cancellationToken = default)
    {
        var deduction = await GetActiveHelbDeductionAsync(employeeId, cancellationToken);
        if (deduction == null) return 0;

        // Don't deduct more than outstanding balance
        return Math.Min(deduction.MonthlyDeduction, deduction.OutstandingBalance);
    }

    #endregion

    #region Loan Deductions

    public async Task<decimal> CalculateLoanDeductionsAsync(int employeeId,
        CancellationToken cancellationToken = default)
    {
        var loans = await _context.Set<EmployeeLoan>()
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        return loans.Sum(l => Math.Min(l.MonthlyInstallment, l.OutstandingBalance));
    }

    private async Task<(decimal EmployeeLoanDeductions, decimal SalaryAdvanceDeductions)>
        CalculateLoanDeductionsDetailedAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var loans = await _context.Set<EmployeeLoan>()
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        decimal employeeLoans = 0;
        decimal salaryAdvances = 0;

        foreach (var loan in loans)
        {
            var deduction = Math.Min(loan.MonthlyInstallment, loan.OutstandingBalance);
            if (loan.LoanType == LoanType.SalaryAdvance)
                salaryAdvances += deduction;
            else
                employeeLoans += deduction;
        }

        return (employeeLoans, salaryAdvances);
    }

    public async Task ProcessLoanRepaymentsAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _context.Payslips
            .Include(p => p.PayslipDetails)
            .FirstOrDefaultAsync(p => p.Id == payslipId, cancellationToken)
            ?? throw new InvalidOperationException($"Payslip {payslipId} not found");

        var loans = await _context.Set<EmployeeLoan>()
            .Where(l => l.EmployeeId == payslip.EmployeeId && l.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var loan in loans)
        {
            var deduction = Math.Min(loan.MonthlyInstallment, loan.OutstandingBalance);
            if (deduction <= 0) continue;

            loan.AmountPaid += deduction;
            loan.InstallmentsPaid++;
            loan.LastPaymentDate = DateOnly.FromDateTime(DateTime.Today);

            // Create repayment record
            var repayment = new LoanRepayment
            {
                EmployeeLoanId = loan.Id,
                InstallmentNumber = loan.InstallmentsPaid,
                DueDate = DateOnly.FromDateTime(DateTime.Today),
                AmountDue = loan.MonthlyInstallment,
                AmountPaid = deduction,
                PaidDate = DateOnly.FromDateTime(DateTime.Today),
                IsPaid = true,
                IsFromPayroll = true,
                BalanceAfterPayment = loan.OutstandingBalance
            };

            _context.Set<LoanRepayment>().Add(repayment);

            // Check if loan is fully repaid
            if (loan.OutstandingBalance <= 0)
            {
                loan.Status = LoanStatus.Completed;
                loan.ActualCompletionDate = DateOnly.FromDateTime(DateTime.Today);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region P9 Form Generation

    public async Task<P9Record> GenerateP9FormAsync(int employeeId, int taxYear, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found");

        // Get all payslips for the tax year
        var payslips = await _context.Payslips
            .Include(p => p.PayrollPeriod)
            .Include(p => p.PayslipDetails)
                .ThenInclude(pd => pd.SalaryComponent)
            .Where(p => p.EmployeeId == employeeId &&
                p.PayrollPeriod.StartDate.Year == taxYear)
            .OrderBy(p => p.PayrollPeriod.StartDate)
            .ToListAsync(cancellationToken);

        var monthlyData = new List<P9MonthlyData>();
        decimal totalGross = 0, totalNssf = 0, totalPaye = 0;
        decimal totalPersonalRelief = 0, totalInsuranceRelief = 0;

        foreach (var payslip in payslips)
        {
            var month = payslip.PayrollPeriod.StartDate.Month;
            var paye = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "PAYE")?.Amount ?? 0;
            var nssf = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "NSSF")?.Amount ?? 0;

            var monthData = new P9MonthlyData
            {
                Month = month,
                GrossPay = payslip.TotalEarnings,
                DefinedContribution = nssf,
                ChargeablePay = payslip.TotalEarnings - nssf,
                PersonalRelief = PersonalRelief,
                PAYE = paye
            };

            monthlyData.Add(monthData);
            totalGross += payslip.TotalEarnings;
            totalNssf += nssf;
            totalPaye += paye;
            totalPersonalRelief += PersonalRelief;
        }

        var p9Record = new P9Record
        {
            EmployeeId = employeeId,
            TaxYear = taxYear,
            EmployeePIN = employee.TaxId ?? "",
            EmployerPIN = "P051234567X", // TODO: Get from settings
            TotalGrossPay = totalGross,
            TotalDefinedContribution = totalNssf,
            TotalChargeablePay = totalGross - totalNssf,
            TotalTaxCharged = totalPaye + totalPersonalRelief, // Before reliefs
            TotalPersonalRelief = totalPersonalRelief,
            TotalInsuranceRelief = totalInsuranceRelief,
            TotalPAYE = totalPaye,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = generatedByUserId,
            MonthlyDataJson = JsonSerializer.Serialize(monthlyData)
        };

        // Check if P9 already exists for this employee/year
        var existing = await _context.Set<P9Record>()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.TaxYear == taxYear, cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.TotalGrossPay = p9Record.TotalGrossPay;
            existing.TotalDefinedContribution = p9Record.TotalDefinedContribution;
            existing.TotalChargeablePay = p9Record.TotalChargeablePay;
            existing.TotalTaxCharged = p9Record.TotalTaxCharged;
            existing.TotalPersonalRelief = p9Record.TotalPersonalRelief;
            existing.TotalInsuranceRelief = p9Record.TotalInsuranceRelief;
            existing.TotalPAYE = p9Record.TotalPAYE;
            existing.GeneratedAt = DateTime.UtcNow;
            existing.GeneratedByUserId = generatedByUserId;
            existing.MonthlyDataJson = p9Record.MonthlyDataJson;
            p9Record = existing;
        }
        else
        {
            _context.Set<P9Record>().Add(p9Record);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return p9Record;
    }

    public async Task<IReadOnlyList<P9Record>> GenerateAllP9FormsAsync(int taxYear, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive || e.TerminationDate?.Year == taxYear)
            .ToListAsync(cancellationToken);

        var p9Records = new List<P9Record>();
        foreach (var employee in employees)
        {
            var record = await GenerateP9FormAsync(employee.Id, taxYear, generatedByUserId, cancellationToken);
            p9Records.Add(record);
        }

        return p9Records;
    }

    public async Task<P9Record?> GetP9RecordAsync(int employeeId, int taxYear,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<P9Record>()
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.TaxYear == taxYear, cancellationToken);
    }

    public async Task<string> GenerateP9HtmlAsync(int employeeId, int taxYear,
        CancellationToken cancellationToken = default)
    {
        var p9 = await GetP9RecordAsync(employeeId, taxYear, cancellationToken)
            ?? throw new InvalidOperationException($"P9 record not found for employee {employeeId}, year {taxYear}");

        var employee = p9.Employee!;
        var monthlyData = JsonSerializer.Deserialize<List<P9MonthlyData>>(p9.MonthlyDataJson) ?? [];

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; font-size: 10pt; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 20px; }");
        html.AppendLine(".header h1 { margin: 0; font-size: 14pt; }");
        html.AppendLine(".header h2 { margin: 5px 0; font-size: 12pt; font-weight: normal; }");
        html.AppendLine(".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 15px; }");
        html.AppendLine(".info-box { border: 1px solid #000; padding: 8px; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        html.AppendLine("th, td { border: 1px solid #000; padding: 4px; text-align: right; font-size: 9pt; }");
        html.AppendLine("th { background-color: #f0f0f0; }");
        html.AppendLine(".month-col { text-align: left; }");
        html.AppendLine(".total-row { font-weight: bold; background-color: #e0e0e0; }");
        html.AppendLine(".signature-section { margin-top: 30px; display: grid; grid-template-columns: 1fr 1fr; gap: 50px; }");
        html.AppendLine(".signature-line { border-top: 1px solid #000; margin-top: 40px; padding-top: 5px; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>REPUBLIC OF KENYA</h1>");
        html.AppendLine("<h2>TAX DEDUCTION CARD (P9A)</h2>");
        html.AppendLine($"<p>Year: {taxYear}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='info-grid'>");
        html.AppendLine("<div class='info-box'>");
        html.AppendLine($"<p><strong>Employee Name:</strong> {employee.FullName}</p>");
        html.AppendLine($"<p><strong>Employee PIN:</strong> {p9.EmployeePIN}</p>");
        html.AppendLine($"<p><strong>ID/Passport No:</strong> {employee.NationalId ?? "N/A"}</p>");
        html.AppendLine("</div>");
        html.AppendLine("<div class='info-box'>");
        html.AppendLine($"<p><strong>Employer:</strong> Company Name</p>");
        html.AppendLine($"<p><strong>Employer PIN:</strong> {p9.EmployerPIN}</p>");
        html.AppendLine($"<p><strong>Employee No:</strong> {employee.EmployeeNumber}</p>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        html.AppendLine("<table>");
        html.AppendLine("<tr>");
        html.AppendLine("<th class='month-col'>Month</th>");
        html.AppendLine("<th>Gross Pay (A)</th>");
        html.AppendLine("<th>Benefits (B)</th>");
        html.AppendLine("<th>Total (C=A+B)</th>");
        html.AppendLine("<th>Defined Cont. (D)</th>");
        html.AppendLine("<th>Chargeable (E=C-D)</th>");
        html.AppendLine("<th>Tax Charged (F)</th>");
        html.AppendLine("<th>Personal Relief (G)</th>");
        html.AppendLine("<th>Insurance Relief (H)</th>");
        html.AppendLine("<th>PAYE (I=F-G-H)</th>");
        html.AppendLine("</tr>");

        var monthNames = new[] { "", "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December" };

        foreach (var month in monthlyData)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td class='month-col'>{monthNames[month.Month]}</td>");
            html.AppendLine($"<td>{month.GrossPay:N2}</td>");
            html.AppendLine("<td>0.00</td>"); // Benefits
            html.AppendLine($"<td>{month.GrossPay:N2}</td>");
            html.AppendLine($"<td>{month.DefinedContribution:N2}</td>");
            html.AppendLine($"<td>{month.ChargeablePay:N2}</td>");
            html.AppendLine($"<td>{(month.PAYE + month.PersonalRelief):N2}</td>");
            html.AppendLine($"<td>{month.PersonalRelief:N2}</td>");
            html.AppendLine($"<td>{month.InsuranceRelief:N2}</td>");
            html.AppendLine($"<td>{month.PAYE:N2}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("<tr class='total-row'>");
        html.AppendLine("<td class='month-col'>TOTAL</td>");
        html.AppendLine($"<td>{p9.TotalGrossPay:N2}</td>");
        html.AppendLine("<td>0.00</td>");
        html.AppendLine($"<td>{p9.TotalGrossPay:N2}</td>");
        html.AppendLine($"<td>{p9.TotalDefinedContribution:N2}</td>");
        html.AppendLine($"<td>{p9.TotalChargeablePay:N2}</td>");
        html.AppendLine($"<td>{p9.TotalTaxCharged:N2}</td>");
        html.AppendLine($"<td>{p9.TotalPersonalRelief:N2}</td>");
        html.AppendLine($"<td>{p9.TotalInsuranceRelief:N2}</td>");
        html.AppendLine($"<td>{p9.TotalPAYE:N2}</td>");
        html.AppendLine("</tr>");
        html.AppendLine("</table>");

        html.AppendLine("<div class='signature-section'>");
        html.AppendLine("<div>");
        html.AppendLine("<div class='signature-line'>Employer's Signature</div>");
        html.AppendLine("<p>Date: _______________</p>");
        html.AppendLine("</div>");
        html.AppendLine("<div>");
        html.AppendLine("<div class='signature-line'>Official Stamp</div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        html.AppendLine($"<p style='margin-top: 20px; font-size: 8pt;'>Generated: {p9.GeneratedAt:dd/MM/yyyy HH:mm}</p>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    #endregion

    #region Statutory Returns

    public async Task<StatutoryReturn> GeneratePAYEReturnAsync(int periodId, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period {periodId} not found");

        var details = new List<object>();
        decimal totalPaye = 0;

        foreach (var payslip in period.Payslips)
        {
            var paye = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "PAYE")?.Amount ?? 0;

            details.Add(new
            {
                EmployeeName = payslip.Employee.FullName,
                EmployeePin = payslip.Employee.TaxId,
                GrossPay = payslip.TotalEarnings,
                PAYE = paye
            });

            totalPaye += paye;
        }

        var statutoryReturn = new StatutoryReturn
        {
            ReturnType = StatutoryReturnType.PAYE,
            PayrollPeriodId = periodId,
            Month = period.StartDate.Month,
            Year = period.StartDate.Year,
            TotalAmount = totalPaye,
            EmployeeCount = period.Payslips.Count,
            DueDate = new DateOnly(period.EndDate.Year, period.EndDate.Month, 9).AddMonths(1),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(details)
        };

        _context.Set<StatutoryReturn>().Add(statutoryReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return statutoryReturn;
    }

    public async Task<StatutoryReturn> GenerateNSSFReturnAsync(int periodId, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period {periodId} not found");

        var details = new List<object>();
        decimal totalNssf = 0;

        foreach (var payslip in period.Payslips)
        {
            var nssf = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "NSSF")?.Amount ?? 0;

            details.Add(new
            {
                EmployeeName = payslip.Employee.FullName,
                NssfNumber = payslip.Employee.NssfNumber,
                GrossPay = payslip.TotalEarnings,
                EmployeeContribution = nssf,
                EmployerContribution = nssf, // Matched contribution
                TotalContribution = nssf * 2
            });

            totalNssf += nssf;
        }

        var statutoryReturn = new StatutoryReturn
        {
            ReturnType = StatutoryReturnType.NSSF,
            PayrollPeriodId = periodId,
            Month = period.StartDate.Month,
            Year = period.StartDate.Year,
            TotalAmount = totalNssf * 2, // Employee + Employer
            EmployeeCount = period.Payslips.Count,
            DueDate = new DateOnly(period.EndDate.Year, period.EndDate.Month, 15).AddMonths(1),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(details)
        };

        _context.Set<StatutoryReturn>().Add(statutoryReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return statutoryReturn;
    }

    public async Task<StatutoryReturn> GenerateSHIFReturnAsync(int periodId, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period {periodId} not found");

        var details = new List<object>();
        decimal totalShif = 0;

        foreach (var payslip in period.Payslips)
        {
            // Try SHIF first, fall back to NHIF
            var shif = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "SHIF")?.Amount ??
                payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "NHIF")?.Amount ?? 0;

            details.Add(new
            {
                EmployeeName = payslip.Employee.FullName,
                NhifNumber = payslip.Employee.NhifNumber,
                GrossPay = payslip.TotalEarnings,
                Contribution = shif
            });

            totalShif += shif;
        }

        var statutoryReturn = new StatutoryReturn
        {
            ReturnType = StatutoryReturnType.SHIF,
            PayrollPeriodId = periodId,
            Month = period.StartDate.Month,
            Year = period.StartDate.Year,
            TotalAmount = totalShif,
            EmployeeCount = period.Payslips.Count,
            DueDate = new DateOnly(period.EndDate.Year, period.EndDate.Month, 9).AddMonths(1),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(details)
        };

        _context.Set<StatutoryReturn>().Add(statutoryReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return statutoryReturn;
    }

    public async Task<StatutoryReturn> GenerateHousingLevyReturnAsync(int periodId, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period {periodId} not found");

        var details = new List<object>();
        decimal totalLevy = 0;

        foreach (var payslip in period.Payslips)
        {
            var levy = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "Housing Levy")?.Amount ?? 0;

            details.Add(new
            {
                EmployeeName = payslip.Employee.FullName,
                EmployeePin = payslip.Employee.TaxId,
                GrossPay = payslip.TotalEarnings,
                EmployeeContribution = levy,
                EmployerContribution = levy, // Matched
                TotalContribution = levy * 2
            });

            totalLevy += levy;
        }

        var statutoryReturn = new StatutoryReturn
        {
            ReturnType = StatutoryReturnType.HousingLevy,
            PayrollPeriodId = periodId,
            Month = period.StartDate.Month,
            Year = period.StartDate.Year,
            TotalAmount = totalLevy * 2, // Employee + Employer
            EmployeeCount = period.Payslips.Count,
            DueDate = new DateOnly(period.EndDate.Year, period.EndDate.Month, 9).AddMonths(1),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(details)
        };

        _context.Set<StatutoryReturn>().Add(statutoryReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return statutoryReturn;
    }

    public async Task<StatutoryReturn> GenerateHELBReturnAsync(int periodId, int generatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.Employee)
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payroll period {periodId} not found");

        var details = new List<object>();
        decimal totalHelb = 0;
        int helbCount = 0;

        foreach (var payslip in period.Payslips)
        {
            var helb = payslip.PayslipDetails
                .FirstOrDefault(d => d.SalaryComponent.Name == "HELB")?.Amount ?? 0;

            if (helb > 0)
            {
                details.Add(new
                {
                    EmployeeName = payslip.Employee.FullName,
                    HelbNumber = payslip.Employee.HelbNumber,
                    GrossPay = payslip.TotalEarnings,
                    Deduction = helb
                });

                totalHelb += helb;
                helbCount++;
            }
        }

        var statutoryReturn = new StatutoryReturn
        {
            ReturnType = StatutoryReturnType.HELB,
            PayrollPeriodId = periodId,
            Month = period.StartDate.Month,
            Year = period.StartDate.Year,
            TotalAmount = totalHelb,
            EmployeeCount = helbCount,
            DueDate = new DateOnly(period.EndDate.Year, period.EndDate.Month, 15).AddMonths(1),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(details)
        };

        _context.Set<StatutoryReturn>().Add(statutoryReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return statutoryReturn;
    }

    public async Task<IReadOnlyList<StatutoryReturn>> GetStatutoryReturnsAsync(int? periodId = null,
        StatutoryReturnType? returnType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<StatutoryReturn>().AsQueryable();

        if (periodId.HasValue)
            query = query.Where(r => r.PayrollPeriodId == periodId.Value);

        if (returnType.HasValue)
            query = query.Where(r => r.ReturnType == returnType.Value);

        return await query
            .Include(r => r.PayrollPeriod)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ThenBy(r => r.ReturnType)
            .ToListAsync(cancellationToken);
    }

    public async Task<StatutoryReturn> MarkReturnAsSubmittedAsync(int returnId, string submissionReference,
        CancellationToken cancellationToken = default)
    {
        var statutoryReturn = await _context.Set<StatutoryReturn>().FindAsync([returnId], cancellationToken)
            ?? throw new InvalidOperationException($"Statutory return {returnId} not found");

        statutoryReturn.IsSubmitted = true;
        statutoryReturn.SubmittedAt = DateTime.UtcNow;
        statutoryReturn.SubmissionReference = submissionReference;

        await _context.SaveChangesAsync(cancellationToken);
        return statutoryReturn;
    }

    #endregion

    #region Enhanced Reports

    public async Task<EnhancedPayrollSummary> GetEnhancedPayrollSummaryAsync(int periodId,
        CancellationToken cancellationToken = default)
    {
        var baseSummary = await GetPayrollSummaryAsync(periodId, cancellationToken);
        var period = await _context.PayrollPeriods
            .Include(p => p.Payslips)
                .ThenInclude(ps => ps.PayslipDetails)
                    .ThenInclude(pd => pd.SalaryComponent)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new InvalidOperationException($"Period {periodId} not found");

        var enhanced = new EnhancedPayrollSummary
        {
            PeriodId = baseSummary.PeriodId,
            PeriodName = baseSummary.PeriodName,
            EmployeeCount = baseSummary.EmployeeCount,
            TotalBasicSalary = baseSummary.TotalBasicSalary,
            TotalEarnings = baseSummary.TotalEarnings,
            TotalDeductions = baseSummary.TotalDeductions,
            TotalNetPay = baseSummary.TotalNetPay,
            TotalPAYE = baseSummary.TotalPAYE,
            TotalNHIF = baseSummary.TotalNHIF,
            TotalNSSF = baseSummary.TotalNSSF,
            TotalHousingLevy = baseSummary.TotalHousingLevy,
            PaidCount = baseSummary.PaidCount,
            PendingCount = baseSummary.PendingCount,
            PayrollProcessedAt = period.ProcessedAt,
            PayrollApprovedAt = period.ApprovedAt
        };

        // Calculate SHIF
        enhanced.TotalSHIF = period.Payslips.SelectMany(p => p.PayslipDetails)
            .Where(pd => pd.SalaryComponent.Name == "SHIF")
            .Sum(pd => pd.Amount);

        // Calculate HELB
        enhanced.TotalHELBDeductions = period.Payslips.SelectMany(p => p.PayslipDetails)
            .Where(pd => pd.SalaryComponent.Name == "HELB")
            .Sum(pd => pd.Amount);
        enhanced.HELBDeductionCount = period.Payslips
            .Count(p => p.PayslipDetails.Any(pd => pd.SalaryComponent.Name == "HELB" && pd.Amount > 0));

        // Calculate loan deductions
        enhanced.TotalLoanDeductions = period.Payslips.SelectMany(p => p.PayslipDetails)
            .Where(pd => pd.SalaryComponent.Name.Contains("Loan"))
            .Sum(pd => pd.Amount);

        // Check statutory return status
        var returns = await GetStatutoryReturnsAsync(periodId, cancellationToken: cancellationToken);
        enhanced.PAYEReturnGenerated = returns.Any(r => r.ReturnType == StatutoryReturnType.PAYE);
        enhanced.NSSFReturnGenerated = returns.Any(r => r.ReturnType == StatutoryReturnType.NSSF);
        enhanced.SHIFReturnGenerated = returns.Any(r => r.ReturnType == StatutoryReturnType.SHIF);
        enhanced.HousingLevyReturnGenerated = returns.Any(r => r.ReturnType == StatutoryReturnType.HousingLevy);
        enhanced.HELBReturnGenerated = returns.Any(r => r.ReturnType == StatutoryReturnType.HELB);

        return enhanced;
    }

    public async Task<string> GenerateEnhancedPayslipHtmlAsync(int payslipId,
        CancellationToken cancellationToken = default)
    {
        var payslip = await GetPayslipByIdAsync(payslipId, cancellationToken)
            ?? throw new InvalidOperationException($"Payslip {payslipId} not found");

        var earnings = payslip.PayslipDetails.Where(d => d.ComponentType == ComponentType.Earning).ToList();
        var statutory = payslip.PayslipDetails.Where(d =>
            d.SalaryComponent.IsStatutory && d.ComponentType == ComponentType.Deduction).ToList();
        var otherDeductions = payslip.PayslipDetails.Where(d =>
            !d.SalaryComponent.IsStatutory && d.ComponentType == ComponentType.Deduction).ToList();

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }");
        html.AppendLine(".payslip { max-width: 800px; margin: 0 auto; background: white; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
        html.AppendLine(".header { text-align: center; border-bottom: 3px solid #2d2d44; padding-bottom: 20px; margin-bottom: 20px; }");
        html.AppendLine(".header h1 { margin: 0; color: #2d2d44; }");
        html.AppendLine(".header h2 { margin: 5px 0; color: #666; font-weight: normal; }");
        html.AppendLine(".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 25px; }");
        html.AppendLine(".info-box { background: #f8f9fa; padding: 15px; border-radius: 8px; }");
        html.AppendLine(".info-box h3 { margin: 0 0 10px 0; color: #2d2d44; font-size: 0.9em; text-transform: uppercase; }");
        html.AppendLine(".info-box p { margin: 5px 0; }");
        html.AppendLine(".section { margin-bottom: 25px; }");
        html.AppendLine(".section-title { font-weight: bold; color: #2d2d44; border-bottom: 2px solid #e0e0e0; padding-bottom: 8px; margin-bottom: 12px; }");
        html.AppendLine(".columns { display: grid; grid-template-columns: 1fr 1fr; gap: 30px; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; }");
        html.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #eee; }");
        html.AppendLine("th { background-color: #f8f9fa; color: #2d2d44; font-size: 0.85em; text-transform: uppercase; }");
        html.AppendLine(".amount { text-align: right; font-family: 'Consolas', monospace; }");
        html.AppendLine(".total-row { font-weight: bold; background-color: #f0f0f0; }");
        html.AppendLine(".statutory { background-color: #fff3cd; }");
        html.AppendLine(".summary-box { background: linear-gradient(135deg, #2d2d44, #4a4a6a); color: white; padding: 20px; border-radius: 8px; text-align: center; margin-top: 20px; }");
        html.AppendLine(".summary-box .net-pay { font-size: 2em; font-weight: bold; }");
        html.AppendLine(".summary-box .label { font-size: 0.9em; opacity: 0.8; }");
        html.AppendLine(".footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 0.85em; color: #666; text-align: center; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='payslip'>");

        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>PAYSLIP</h1>");
        html.AppendLine($"<h2>{payslip.PayrollPeriod.PeriodName}</h2>");
        html.AppendLine("</div>");

        // Employee & Period Info
        html.AppendLine("<div class='info-grid'>");
        html.AppendLine("<div class='info-box'>");
        html.AppendLine("<h3>Employee Details</h3>");
        html.AppendLine($"<p><strong>Name:</strong> {payslip.Employee.FullName}</p>");
        html.AppendLine($"<p><strong>Employee No:</strong> {payslip.Employee.EmployeeNumber}</p>");
        html.AppendLine($"<p><strong>Department:</strong> {payslip.Employee.Department ?? "N/A"}</p>");
        html.AppendLine($"<p><strong>Position:</strong> {payslip.Employee.Position ?? "N/A"}</p>");
        html.AppendLine($"<p><strong>KRA PIN:</strong> {payslip.Employee.TaxId ?? "N/A"}</p>");
        html.AppendLine("</div>");
        html.AppendLine("<div class='info-box'>");
        html.AppendLine("<h3>Payment Details</h3>");
        html.AppendLine($"<p><strong>Pay Date:</strong> {payslip.PayrollPeriod.PayDate:dd MMMM yyyy}</p>");
        html.AppendLine($"<p><strong>Bank:</strong> {payslip.Employee.BankName ?? "N/A"}</p>");
        html.AppendLine($"<p><strong>Account:</strong> {MaskAccountNumber(payslip.Employee.BankAccountNumber)}</p>");
        html.AppendLine($"<p><strong>NSSF No:</strong> {payslip.Employee.NssfNumber ?? "N/A"}</p>");
        html.AppendLine($"<p><strong>NHIF/SHIF No:</strong> {payslip.Employee.NhifNumber ?? "N/A"}</p>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Earnings and Deductions in columns
        html.AppendLine("<div class='columns'>");

        // Earnings
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>EARNINGS</div>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Description</th><th class='amount'>Amount (KSh)</th></tr>");
        html.AppendLine($"<tr><td>Basic Salary</td><td class='amount'>{payslip.BasicSalary:N2}</td></tr>");
        foreach (var earning in earnings)
        {
            html.AppendLine($"<tr><td>{earning.SalaryComponent.Name}</td><td class='amount'>{earning.Amount:N2}</td></tr>");
        }
        html.AppendLine($"<tr class='total-row'><td>Total Earnings</td><td class='amount'>{payslip.TotalEarnings:N2}</td></tr>");
        html.AppendLine("</table></div>");

        // Deductions
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>DEDUCTIONS</div>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Description</th><th class='amount'>Amount (KSh)</th></tr>");

        // Statutory deductions
        foreach (var ded in statutory)
        {
            html.AppendLine($"<tr class='statutory'><td>{ded.SalaryComponent.Name}</td><td class='amount'>{ded.Amount:N2}</td></tr>");
        }

        // Other deductions
        foreach (var ded in otherDeductions)
        {
            html.AppendLine($"<tr><td>{ded.SalaryComponent.Name}</td><td class='amount'>{ded.Amount:N2}</td></tr>");
        }

        html.AppendLine($"<tr class='total-row'><td>Total Deductions</td><td class='amount'>{payslip.TotalDeductions:N2}</td></tr>");
        html.AppendLine("</table></div>");

        html.AppendLine("</div>"); // End columns

        // Net Pay Summary
        html.AppendLine("<div class='summary-box'>");
        html.AppendLine("<div class='label'>NET PAY</div>");
        html.AppendLine($"<div class='net-pay'>KSh {payslip.NetPay:N2}</div>");
        html.AppendLine("</div>");

        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>This is a computer-generated payslip. No signature required.</p>");
        html.AppendLine($"<p>Generated on {DateTime.Now:dd MMMM yyyy} at {DateTime.Now:HH:mm}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div>"); // End payslip
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return "N/A";

        return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
    }

    #endregion
}
