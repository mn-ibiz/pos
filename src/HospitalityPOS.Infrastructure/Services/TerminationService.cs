// src/HospitalityPOS.Infrastructure/Services/TerminationService.cs
// Implementation of employee termination and final settlement service.
// Compliant with Kenya Employment Act 2007.

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing employee terminations and final settlements.
/// </summary>
public class TerminationService : ITerminationService
{
    private readonly POSDbContext _context;
    private readonly ILoanService? _loanService;
    private readonly IDisciplinaryDeductionService? _deductionService;

    public TerminationService(
        POSDbContext context,
        ILoanService? loanService = null,
        IDisciplinaryDeductionService? deductionService = null)
    {
        _context = context;
        _loanService = loanService;
        _deductionService = deductionService;
    }

    #region Termination CRUD

    public async Task<EmployeeTermination> InitiateTerminationAsync(TerminationRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { request.EmployeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found");

        // Check for existing active termination
        var existingTermination = await _context.EmployeeTerminations
            .FirstOrDefaultAsync(t => t.EmployeeId == request.EmployeeId && t.Status != TerminationStatus.Cancelled, cancellationToken);

        if (existingTermination != null)
        {
            throw new InvalidOperationException("Employee already has an active termination process");
        }

        // Calculate final settlement
        var calculation = await CalculateFinalSettlementAsync(request.EmployeeId, request.TerminationType, request.EffectiveDate, cancellationToken);
        var serviceDuration = await CalculateServiceDurationAsync(request.EmployeeId, request.EffectiveDate, cancellationToken);

        var termination = new EmployeeTermination
        {
            EmployeeId = request.EmployeeId,
            ReferenceNumber = await GenerateReferenceNumberAsync(cancellationToken),
            TerminationType = request.TerminationType,
            Status = TerminationStatus.Initiated,
            NoticeDate = request.NoticeDate,
            EffectiveDate = request.EffectiveDate,
            LastWorkingDay = request.LastWorkingDay,
            NoticePeriodServed = request.NoticePeriodServed,
            Reason = request.Reason,
            DetailedNotes = request.DetailedNotes,
            YearsOfService = serviceDuration.Years,
            MonthsOfService = serviceDuration.Months,

            // Earnings
            DaysWorkedInFinalMonth = CalculateDaysWorked(request.LastWorkingDay),
            ProRataBasicSalary = calculation.ProRataSalary,
            AccruedLeaveDays = calculation.AccruedLeaveDays,
            LeavePayment = calculation.LeaveEncashment,
            NoticePay = calculation.NoticePay,
            SeverancePay = calculation.SeverancePay,
            OtherEarnings = calculation.OtherEarnings,
            TotalEarnings = calculation.TotalEarnings,

            // Deductions
            OutstandingLoans = calculation.OutstandingLoans,
            OutstandingAdvances = calculation.OutstandingAdvances,
            PendingDeductions = calculation.PendingDeductions,
            TaxOnTermination = calculation.TaxPayable,
            OtherDeductions = calculation.OtherDeductions,
            TotalDeductions = calculation.TotalDeductions,

            // Net Settlement
            NetFinalSettlement = calculation.NetSettlement,

            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeTerminations.Add(termination);
        await _context.SaveChangesAsync(cancellationToken);

        return termination;
    }

    public async Task<EmployeeTermination?> GetByIdAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == terminationId, cancellationToken);
    }

    public async Task<EmployeeTermination?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.ReferenceNumber == referenceNumber, cancellationToken);
    }

    public async Task<EmployeeTermination?> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Status != TerminationStatus.Cancelled, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeTermination>> GetTerminationsAsync(TerminationFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeTerminations
            .Include(t => t.Employee)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.TerminationType.HasValue)
                query = query.Where(t => t.TerminationType == filter.TerminationType.Value);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.EffectiveDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(t => t.EffectiveDate <= filter.ToDate.Value);
        }

        return await query.OrderByDescending(t => t.EffectiveDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeTermination>> GetPendingTerminationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .Where(t => t.Status == TerminationStatus.Initiated || t.Status == TerminationStatus.AwaitingClearance)
            .OrderBy(t => t.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Calculations

    public async Task<TerminationCalculation> CalculateFinalSettlementAsync(int employeeId, TerminationType type, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var serviceDuration = await CalculateServiceDurationAsync(employeeId, effectiveDate, cancellationToken);

        // Calculate each component
        var proRataSalary = await CalculateProRataSalaryAsync(employeeId, effectiveDate, cancellationToken);
        var leaveEncashment = await CalculateLeaveEncashmentAsync(employeeId, effectiveDate, cancellationToken);
        var noticePay = await CalculateNoticePayAsync(employeeId, false, cancellationToken); // Assume notice not served for calculation
        var severancePay = await CalculateSeverancePayAsync(employeeId, effectiveDate, cancellationToken);

        // Only redundancy qualifies for severance
        if (type != TerminationType.Redundancy)
        {
            severancePay = 0;
        }

        var otherEarnings = 0m;
        var totalEarnings = proRataSalary + leaveEncashment + noticePay + severancePay + otherEarnings;

        // Deductions
        var outstandingLoans = _loanService != null
            ? await _loanService.GetOutstandingBalanceAsync(employeeId, cancellationToken)
            : await GetOutstandingLoansAsync(employeeId, cancellationToken);

        var outstandingAdvances = 0m; // Advances are included in loans

        var pendingDeductions = _deductionService != null
            ? await _deductionService.GetPendingDeductionsAsync(employeeId, cancellationToken)
            : await GetPendingDeductionsAsync(employeeId, cancellationToken);

        var taxPayable = CalculateTerminationTax(totalEarnings, serviceDuration.Years);
        var otherDeductions = 0m;
        var totalDeductions = outstandingLoans + outstandingAdvances + pendingDeductions + taxPayable + otherDeductions;

        var netSettlement = totalEarnings - totalDeductions;

        // Get accrued leave days
        var accruedLeaveDays = await GetAccruedLeaveDaysAsync(employeeId, effectiveDate.Year, cancellationToken);

        return new TerminationCalculation(
            EmployeeId: employeeId,
            EmployeeName: $"{employee.FirstName} {employee.LastName}",
            TerminationDate: effectiveDate,
            TerminationType: type,
            YearsOfService: serviceDuration.Years,
            MonthsOfService: serviceDuration.Months,
            ProRataSalary: proRataSalary,
            LeaveEncashment: leaveEncashment,
            AccruedLeaveDays: (int)accruedLeaveDays,
            NoticePay: noticePay,
            SeverancePay: severancePay,
            OtherEarnings: otherEarnings,
            TotalEarnings: totalEarnings,
            OutstandingLoans: outstandingLoans,
            OutstandingAdvances: outstandingAdvances,
            PendingDeductions: pendingDeductions,
            TaxPayable: taxPayable,
            OtherDeductions: otherDeductions,
            TotalDeductions: totalDeductions,
            NetSettlement: netSettlement,
            EarningsBreakdown: BuildEarningsBreakdown(proRataSalary, leaveEncashment, noticePay, severancePay),
            DeductionsBreakdown: BuildDeductionsBreakdown(outstandingLoans, pendingDeductions, taxPayable)
        );
    }

    public async Task<decimal> CalculateSeverancePayAsync(int employeeId, DateOnly terminationDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var serviceDuration = await CalculateServiceDurationAsync(employeeId, terminationDate, cancellationToken);

        // Kenya Employment Act: 15 days' basic wage per completed year of service
        var dailyWage = employee.BasicSalary / 22m;
        return dailyWage * 15 * serviceDuration.Years;
    }

    public async Task<decimal> CalculateLeaveEncashmentAsync(int employeeId, DateOnly terminationDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var accruedDays = await GetAccruedLeaveDaysAsync(employeeId, terminationDate.Year, cancellationToken);
        var dailyWage = employee.BasicSalary / 22m;

        return dailyWage * accruedDays;
    }

    public async Task<decimal> CalculateNoticePayAsync(int employeeId, bool noticePeriodServed, CancellationToken cancellationToken = default)
    {
        if (noticePeriodServed) return 0;

        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var noticeDays = await GetNoticePeriodDaysAsync(employeeId, cancellationToken);
        var dailyWage = employee.BasicSalary / 22m;

        return dailyWage * noticeDays;
    }

    public async Task<decimal> CalculateProRataSalaryAsync(int employeeId, DateOnly lastWorkingDay, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var daysWorked = CalculateDaysWorked(lastWorkingDay);
        var totalDaysInMonth = DateTime.DaysInMonth(lastWorkingDay.Year, lastWorkingDay.Month);
        var dailyRate = employee.BasicSalary / totalDaysInMonth;

        return dailyRate * daysWorked;
    }

    public Task<int> GetNoticePeriodDaysAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        // Kenya Employment Act notice periods:
        // - Daily/hourly: End of day
        // - Weekly/fortnightly: End of week/fortnight
        // - Monthly: 28 days or 1 month
        // For simplicity, default to 28 days for monthly employees
        return Task.FromResult(28);
    }

    public async Task<ServiceDuration> CalculateServiceDurationAsync(int employeeId, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        if (!employee.DateOfJoining.HasValue)
        {
            return new ServiceDuration(0, 0, 0);
        }

        var joinDate = DateOnly.FromDateTime(employee.DateOfJoining.Value);
        var totalMonths = ((asOfDate.Year - joinDate.Year) * 12) + (asOfDate.Month - joinDate.Month);

        if (asOfDate.Day < joinDate.Day)
        {
            totalMonths--;
        }

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        return new ServiceDuration(years, months, totalMonths);
    }

    #endregion

    #region Workflow

    public async Task<TerminationResult> ApproveTerminationAsync(int terminationId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        if (termination.Status != TerminationStatus.Initiated)
            return new TerminationResult(false, $"Termination is not in initiated status (status: {termination.Status})");

        termination.Status = TerminationStatus.AwaitingClearance;
        termination.ApprovedByUserId = approverUserId;
        termination.ApprovedAt = DateTime.UtcNow;
        termination.ApprovalNotes = notes;
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Termination approved, pending clearance", termination);
    }

    public async Task<TerminationResult> RecalculateSettlementAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        var calculation = await CalculateFinalSettlementAsync(
            termination.EmployeeId,
            termination.TerminationType,
            termination.EffectiveDate,
            cancellationToken);

        // Update termination with new values
        termination.ProRataBasicSalary = calculation.ProRataSalary;
        termination.LeavePayment = calculation.LeaveEncashment;
        termination.NoticePay = calculation.NoticePay;
        termination.SeverancePay = calculation.SeverancePay;
        termination.TotalEarnings = calculation.TotalEarnings;
        termination.OutstandingLoans = calculation.OutstandingLoans;
        termination.PendingDeductions = calculation.PendingDeductions;
        termination.TaxOnTermination = calculation.TaxPayable;
        termination.TotalDeductions = calculation.TotalDeductions;
        termination.NetFinalSettlement = calculation.NetSettlement;
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Settlement recalculated", termination);
    }

    public async Task<TerminationResult> ProcessFinalPaymentAsync(int terminationId, string paymentMethod, string? paymentReference = null, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        if (termination.Status != TerminationStatus.AwaitingPayment)
            return new TerminationResult(false, "Clearance must be completed before payment");

        termination.Status = TerminationStatus.Completed;
        termination.PaymentDate = DateOnly.FromDateTime(DateTime.Today);
        termination.PaymentMethod = paymentMethod;
        termination.PaymentReference = paymentReference;
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Final settlement payment processed", termination);
    }

    public async Task<TerminationResult> CompleteTerminationAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == terminationId, cancellationToken);

        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        if (termination.Status != TerminationStatus.Completed)
            return new TerminationResult(false, "Settlement must be paid before completion");

        termination.Status = TerminationStatus.Completed;
        termination.UpdatedAt = DateTime.UtcNow;

        // Update employee status
        termination.Employee.EmploymentStatus = "Terminated";
        termination.Employee.TerminationDate = termination.EffectiveDate.ToDateTime(TimeOnly.MinValue);

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Termination completed", termination);
    }

    public async Task<TerminationResult> CancelTerminationAsync(int terminationId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        if (termination.Status == TerminationStatus.Completed)
            return new TerminationResult(false, "Cannot cancel a completed termination");

        termination.Status = TerminationStatus.Cancelled;
        termination.DetailedNotes = $"{termination.DetailedNotes}\n\nCancelled: {reason}";
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Termination cancelled", termination);
    }

    #endregion

    #region Clearance

    public async Task<TerminationResult> CompleteClearanceAsync(int terminationId, ClearanceType clearanceType, string? notes = null, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        var today = DateOnly.FromDateTime(DateTime.Today);

        switch (clearanceType)
        {
            case ClearanceType.IT:
                termination.ITClearance = true;
                termination.ITClearanceDate = today;
                termination.ITClearanceNotes = notes;
                break;
            case ClearanceType.Finance:
                termination.FinanceClearance = true;
                termination.FinanceClearanceDate = today;
                termination.FinanceClearanceNotes = notes;
                break;
            case ClearanceType.HR:
                termination.HRClearance = true;
                termination.HRClearanceDate = today;
                termination.HRClearanceNotes = notes;
                break;
            case ClearanceType.Operations:
                termination.OperationsClearance = true;
                termination.OperationsClearanceDate = today;
                termination.OperationsClearanceNotes = notes;
                break;
        }

        // Check if all clearances complete
        if (termination.ITClearance && termination.FinanceClearance && termination.HRClearance && termination.OperationsClearance)
        {
            termination.Status = TerminationStatus.AwaitingPayment;
        }

        termination.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new TerminationResult(true, $"{clearanceType} clearance completed", termination);
    }

    public async Task<ClearanceStatus> GetClearanceStatusAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Termination {terminationId} not found");

        return new ClearanceStatus(
            ITCleared: termination.ITClearance,
            ITClearanceDate: termination.ITClearanceDate,
            ITNotes: termination.ITClearanceNotes,
            FinanceCleared: termination.FinanceClearance,
            FinanceClearanceDate: termination.FinanceClearanceDate,
            FinanceNotes: termination.FinanceClearanceNotes,
            HRCleared: termination.HRClearance,
            HRClearanceDate: termination.HRClearanceDate,
            HRNotes: termination.HRClearanceNotes,
            OperationsCleared: termination.OperationsClearance,
            OperationsClearanceDate: termination.OperationsClearanceDate,
            OperationsNotes: termination.OperationsClearanceNotes,
            AllCleared: termination.ITClearance && termination.FinanceClearance && termination.HRClearance && termination.OperationsClearance
        );
    }

    #endregion

    #region Documents

    public async Task<byte[]> GenerateCertificateOfServiceAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == terminationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Termination {terminationId} not found");

        var sb = new StringBuilder();
        sb.AppendLine("CERTIFICATE OF SERVICE");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"This is to certify that {termination.Employee.FirstName} {termination.Employee.LastName}");
        sb.AppendLine($"ID/Passport No: {termination.Employee.NationalId}");
        sb.AppendLine();
        sb.AppendLine($"Was employed by this organization from {termination.Employee.DateOfJoining:dd/MM/yyyy}");
        sb.AppendLine($"to {termination.EffectiveDate:dd/MM/yyyy} ({termination.YearsOfService} years, {termination.MonthsOfService} months).");
        sb.AppendLine();
        sb.AppendLine($"Position: {termination.Employee.Position}");
        sb.AppendLine($"Department: {termination.Employee.Department}");
        sb.AppendLine();
        sb.AppendLine($"Reason for Leaving: {termination.TerminationType}");
        sb.AppendLine();
        sb.AppendLine("This certificate is issued upon request in accordance with");
        sb.AppendLine("Section 51 of the Kenya Employment Act 2007.");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Today:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("_________________________");
        sb.AppendLine("Authorized Signature");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GenerateFinalSettlementStatementAsync(int terminationId, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == terminationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Termination {terminationId} not found");

        var sb = new StringBuilder();
        sb.AppendLine("FINAL SETTLEMENT STATEMENT");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"Employee: {termination.Employee.FirstName} {termination.Employee.LastName}");
        sb.AppendLine($"Employee No: {termination.Employee.EmployeeNumber}");
        sb.AppendLine($"Termination Date: {termination.EffectiveDate:dd/MM/yyyy}");
        sb.AppendLine($"Reference: {termination.ReferenceNumber}");
        sb.AppendLine();
        sb.AppendLine("EARNINGS:");
        sb.AppendLine($"  Pro-rata Salary:       {termination.ProRataBasicSalary,15:N2}");
        sb.AppendLine($"  Leave Encashment:      {termination.LeavePayment,15:N2}");
        sb.AppendLine($"  Notice Pay:            {termination.NoticePay,15:N2}");
        sb.AppendLine($"  Severance Pay:         {termination.SeverancePay,15:N2}");
        sb.AppendLine($"  Other Earnings:        {termination.OtherEarnings,15:N2}");
        sb.AppendLine($"  ----------------------------------------");
        sb.AppendLine($"  Total Earnings:        {termination.TotalEarnings,15:N2}");
        sb.AppendLine();
        sb.AppendLine("DEDUCTIONS:");
        sb.AppendLine($"  Outstanding Loans:     {termination.OutstandingLoans,15:N2}");
        sb.AppendLine($"  Outstanding Advances:  {termination.OutstandingAdvances,15:N2}");
        sb.AppendLine($"  Pending Deductions:    {termination.PendingDeductions,15:N2}");
        sb.AppendLine($"  Tax on Termination:    {termination.TaxOnTermination,15:N2}");
        sb.AppendLine($"  Other Deductions:      {termination.OtherDeductions,15:N2}");
        sb.AppendLine($"  ----------------------------------------");
        sb.AppendLine($"  Total Deductions:      {termination.TotalDeductions,15:N2}");
        sb.AppendLine();
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine($"  NET SETTLEMENT:        {termination.NetFinalSettlement,15:N2}");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Today:dd/MM/yyyy}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<TerminationResult> MarkCertificateIssuedAsync(int terminationId, string? documentPath = null, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        termination.CertificateIssued = true;
        termination.CertificateIssuedDate = DateOnly.FromDateTime(DateTime.Today);
        termination.CertificateDocumentPath = documentPath;
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Certificate of service marked as issued", termination);
    }

    #endregion

    #region Exit Interview

    public async Task<TerminationResult> RecordExitInterviewAsync(int terminationId, string notes, CancellationToken cancellationToken = default)
    {
        var termination = await _context.EmployeeTerminations.FindAsync(new object[] { terminationId }, cancellationToken);
        if (termination == null)
            return new TerminationResult(false, "Termination not found");

        termination.ExitInterviewDate = DateOnly.FromDateTime(DateTime.Today);
        termination.ExitInterviewNotes = notes;
        termination.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new TerminationResult(true, "Exit interview recorded", termination);
    }

    #endregion

    #region Reports

    public async Task<TerminationSummaryReport> GenerateSummaryReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var terminations = await _context.EmployeeTerminations
            .Include(t => t.Employee)
            .Where(t => t.EffectiveDate >= startDate && t.EffectiveDate <= endDate && t.Status == TerminationStatus.Completed)
            .ToListAsync(cancellationToken);

        var byType = terminations
            .GroupBy(t => t.TerminationType)
            .Select(g => new TerminationByTypeBreakdown(
                TerminationType: g.Key,
                Count: g.Count(),
                TotalSettlement: g.Sum(t => t.NetFinalSettlement)
            ))
            .ToList();

        var byDepartment = terminations
            .GroupBy(t => t.Employee.Department ?? "Unknown")
            .Select(g => new TerminationByDepartmentBreakdown(
                Department: g.Key,
                Count: g.Count(),
                TotalSettlement: g.Sum(t => t.NetFinalSettlement)
            ))
            .ToList();

        return new TerminationSummaryReport(
            StartDate: startDate,
            EndDate: endDate,
            TotalTerminations: terminations.Count,
            TotalSettlementPaid: terminations.Sum(t => t.NetFinalSettlement),
            ByType: byType,
            ByDepartment: byDepartment,
            AverageServiceYears: terminations.Any() ? (decimal)terminations.Average(t => t.YearsOfService) : 0m,
            AverageSettlement: terminations.Any() ? terminations.Average(t => t.NetFinalSettlement) : 0m
        );
    }

    public async Task<TurnoverStatistics> GetTurnoverStatisticsAsync(int year, CancellationToken cancellationToken = default)
    {
        var terminations = await _context.EmployeeTerminations
            .Where(t => t.EffectiveDate.Year == year && t.Status == TerminationStatus.Completed)
            .ToListAsync(cancellationToken);

        var employees = await _context.Employees.CountAsync(cancellationToken);
        var newHires = await _context.Employees
            .CountAsync(e => e.DateOfJoining.HasValue && e.DateOfJoining.Value.Year == year, cancellationToken);

        var voluntaryTerminations = terminations.Count(t =>
            t.TerminationType == TerminationType.Resignation ||
            t.TerminationType == TerminationType.Retirement);

        var involuntaryTerminations = terminations.Count - voluntaryTerminations;

        var monthlyBreakdown = Enumerable.Range(1, 12)
            .Select(month => new MonthlyTurnover(
                Month: month,
                MonthName: new DateTime(year, month, 1).ToString("MMMM"),
                Terminations: terminations.Count(t => t.EffectiveDate.Month == month),
                NewHires: 0, // Would need hire date tracking
                Headcount: employees
            ))
            .ToList();

        return new TurnoverStatistics(
            Year: year,
            TotalTerminations: terminations.Count,
            TotalNewHires: newHires,
            TurnoverRate: employees > 0 ? (decimal)terminations.Count / employees * 100 : 0,
            VoluntaryTurnoverRate: employees > 0 ? (decimal)voluntaryTerminations / employees * 100 : 0,
            InvoluntaryTurnoverRate: employees > 0 ? (decimal)involuntaryTerminations / employees * 100 : 0,
            AverageHeadcount: employees,
            MonthlyBreakdown: monthlyBreakdown
        );
    }

    #endregion

    #region Utility

    public async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.Today.Year.ToString()[2..];
        var count = await _context.EmployeeTerminations
            .CountAsync(t => t.ReferenceNumber.StartsWith($"TRM-{year}"), cancellationToken);

        return $"TRM-{year}-{(count + 1):D5}";
    }

    public async Task<TerminationBlockers> CheckTerminationBlockersAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();

        var outstandingLoans = await GetOutstandingLoansAsync(employeeId, cancellationToken);
        var hasActiveLoans = outstandingLoans > 0;

        var pendingDeductions = await GetPendingDeductionsAsync(employeeId, cancellationToken);
        var hasPendingDeductions = pendingDeductions > 0;

        // Check pending leave requests
        var pendingLeaveRequests = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employeeId && l.Status == LeaveRequestStatus.Pending, cancellationToken);

        if (hasActiveLoans)
            blockers.Add($"Outstanding loan balance: {outstandingLoans:N2}");

        if (hasPendingDeductions)
            blockers.Add($"Pending deductions: {pendingDeductions:N2}");

        if (pendingLeaveRequests > 0)
            blockers.Add($"Pending leave requests: {pendingLeaveRequests}");

        return new TerminationBlockers(
            HasBlockers: blockers.Any(),
            HasActiveLoans: hasActiveLoans,
            OutstandingLoanAmount: outstandingLoans,
            HasPendingDeductions: hasPendingDeductions,
            PendingDeductionAmount: pendingDeductions,
            HasPendingLeaveRequests: pendingLeaveRequests > 0,
            PendingLeaveRequestCount: pendingLeaveRequests,
            BlockerMessages: blockers
        );
    }

    #endregion

    #region Private Helpers

    private static decimal CalculateDaysWorked(DateOnly lastWorkingDay)
    {
        return lastWorkingDay.Day;
    }

    private async Task<decimal> GetOutstandingLoansAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _context.EmployeeLoans
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .SumAsync(l => l.TotalAmountDue - l.AmountPaid, cancellationToken);
    }

    private async Task<decimal> GetPendingDeductionsAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _context.DisciplinaryDeductions
            .Where(d => d.EmployeeId == employeeId && d.Status == DisciplinaryDeductionStatus.Approved && !d.DeductedInPayslipId.HasValue)
            .SumAsync(d => d.Amount, cancellationToken);
    }

    private async Task<decimal> GetAccruedLeaveDaysAsync(int employeeId, int year, CancellationToken cancellationToken)
    {
        var allocation = await _context.LeaveAllocations
            .Where(a => a.EmployeeId == employeeId && a.Year == year && a.LeaveType.Name.Contains("Annual"))
            .FirstOrDefaultAsync(cancellationToken);

        if (allocation == null) return 0;

        return allocation.AllocatedDays + allocation.CarriedOverDays - allocation.UsedDays;
    }

    private static decimal CalculateTerminationTax(decimal totalEarnings, int yearsOfService)
    {
        // Simplified tax calculation - Kenya has specific tax relief for termination benefits
        // Based on years of service, there are tax exemptions
        // For now, use a simplified 10% rate on earnings above exemption
        var exemptionPerYear = 60000m; // KES 60,000 per year of service exempted
        var totalExemption = exemptionPerYear * yearsOfService;
        var taxableAmount = Math.Max(0, totalEarnings - totalExemption);

        return taxableAmount * 0.1m; // 10% withholding
    }

    private static string BuildEarningsBreakdown(decimal proRata, decimal leave, decimal notice, decimal severance)
    {
        return $"Pro-rata: {proRata:N2}, Leave: {leave:N2}, Notice: {notice:N2}, Severance: {severance:N2}";
    }

    private static string BuildDeductionsBreakdown(decimal loans, decimal deductions, decimal tax)
    {
        return $"Loans: {loans:N2}, Deductions: {deductions:N2}, Tax: {tax:N2}";
    }

    #endregion
}
