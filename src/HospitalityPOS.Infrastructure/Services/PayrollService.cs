using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for payroll processing with Kenya statutory calculations.
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly POSDbContext _context;

    // Kenya 2024 Tax Bands (Monthly)
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

    // NHIF bands
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
}
