// src/HospitalityPOS.Infrastructure/Services/DisciplinaryDeductionService.cs
// Implementation of disciplinary deduction (fines/penalties) service.
// Compliant with Kenya Employment Act 2007 Section 19.

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing disciplinary deductions.
/// </summary>
public class DisciplinaryDeductionService : IDisciplinaryDeductionService
{
    private readonly POSDbContext _context;

    public DisciplinaryDeductionService(POSDbContext context)
    {
        _context = context;
    }

    #region CRUD Operations

    public async Task<DisciplinaryDeduction> CreateDeductionAsync(DisciplinaryDeductionRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { request.EmployeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found");

        var dailyWage = await CalculateDailyWageAsync(request.EmployeeId, cancellationToken);

        var deduction = new DisciplinaryDeduction
        {
            EmployeeId = request.EmployeeId,
            ReferenceNumber = await GenerateReferenceNumberAsync(cancellationToken),
            ReasonType = request.ReasonType,
            Status = DisciplinaryDeductionStatus.Pending,
            IncidentDate = request.IncidentDate,
            Description = request.Description,
            Amount = request.Amount,
            DaysAbsent = request.DaysAbsent,
            DailyWageRate = dailyWage,
            ActualLossAmount = request.ActualLossAmount,
            EvidenceDocumentPath = request.EvidenceDocumentPath,
            WitnessEmployeeId = request.WitnessEmployeeId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.DisciplinaryDeductions.Add(deduction);
        await _context.SaveChangesAsync(cancellationToken);

        return deduction;
    }

    public async Task<DisciplinaryDeduction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryDeductions
            .Include(d => d.Employee)
            .Include(d => d.WitnessEmployee)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DisciplinaryDeduction?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryDeductions
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.ReferenceNumber == referenceNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<DisciplinaryDeduction>> GetEmployeeDeductionsAsync(int employeeId, bool includeApplied = false, CancellationToken cancellationToken = default)
    {
        var query = _context.DisciplinaryDeductions
            .Where(d => d.EmployeeId == employeeId);

        if (!includeApplied)
        {
            query = query.Where(d => d.Status != DisciplinaryDeductionStatus.Deducted && d.Status != DisciplinaryDeductionStatus.Rejected);
        }

        return await query.OrderByDescending(d => d.IncidentDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DisciplinaryDeduction>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryDeductions
            .Include(d => d.Employee)
            .Where(d => d.Status == DisciplinaryDeductionStatus.Pending)
            .OrderBy(d => d.IncidentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DisciplinaryDeduction>> GetDeductionsAsync(DeductionFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DisciplinaryDeductions
            .Include(d => d.Employee)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.EmployeeId.HasValue)
                query = query.Where(d => d.EmployeeId == filter.EmployeeId.Value);

            if (filter.ReasonType.HasValue)
                query = query.Where(d => d.ReasonType == filter.ReasonType.Value);

            if (filter.Status.HasValue)
                query = query.Where(d => d.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(d => d.IncidentDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(d => d.IncidentDate <= filter.ToDate.Value);
        }

        return await query.OrderByDescending(d => d.IncidentDate).ToListAsync(cancellationToken);
    }

    #endregion

    #region Workflow Operations

    public async Task<DeductionResult> ApproveDeductionAsync(int deductionId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        if (deduction.Status != DisciplinaryDeductionStatus.Pending)
            return new DeductionResult(false, $"Deduction is not pending (status: {deduction.Status})");

        deduction.Status = DisciplinaryDeductionStatus.Approved;
        deduction.ApprovedByUserId = approverUserId;
        deduction.ApprovedAt = DateTime.UtcNow;
        deduction.ApprovalNotes = notes;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, "Deduction approved", deduction);
    }

    public async Task<DeductionResult> RejectDeductionAsync(int deductionId, int reviewerUserId, string reason, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        if (deduction.Status != DisciplinaryDeductionStatus.Pending)
            return new DeductionResult(false, $"Deduction is not pending (status: {deduction.Status})");

        deduction.Status = DisciplinaryDeductionStatus.Rejected;
        deduction.ApprovedByUserId = reviewerUserId;
        deduction.ApprovalNotes = reason;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, "Deduction rejected", deduction);
    }

    public async Task<DeductionResult> RecordEmployeeAcknowledgmentAsync(int deductionId, string? response = null, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        deduction.EmployeeAcknowledged = true;
        deduction.AcknowledgedAt = DateTime.UtcNow;
        deduction.EmployeeResponse = response;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, "Employee acknowledgment recorded", deduction);
    }

    public async Task<DeductionResult> CancelDeductionAsync(int deductionId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        if (deduction.Status == DisciplinaryDeductionStatus.Deducted)
            return new DeductionResult(false, "Cannot cancel a deduction that has been applied to payroll");

        deduction.Status = DisciplinaryDeductionStatus.Rejected;
        deduction.ApprovalNotes = $"Cancelled: {reason}";
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, "Deduction cancelled", deduction);
    }

    #endregion

    #region Appeal Operations

    public async Task<DeductionResult> SubmitAppealAsync(int deductionId, string reason, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        if (deduction.Status != DisciplinaryDeductionStatus.Approved)
            return new DeductionResult(false, "Can only appeal approved deductions");

        deduction.Status = DisciplinaryDeductionStatus.Appealed;
        deduction.AppealedAt = DateTime.UtcNow;
        deduction.AppealReason = reason;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, "Appeal submitted", deduction);
    }

    public async Task<DeductionResult> ProcessAppealAsync(int deductionId, int reviewerUserId, bool upheld, string decision, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            return new DeductionResult(false, "Deduction not found");

        if (deduction.Status != DisciplinaryDeductionStatus.Appealed)
            return new DeductionResult(false, "Deduction is not under appeal");

        deduction.AppealReviewedByUserId = reviewerUserId;
        deduction.AppealDecidedAt = DateTime.UtcNow;
        deduction.AppealDecision = decision;
        deduction.UpdatedAt = DateTime.UtcNow;

        if (upheld)
        {
            deduction.Status = DisciplinaryDeductionStatus.Approved; // Return to approved for application
        }
        else
        {
            deduction.Status = DisciplinaryDeductionStatus.Rejected; // Appeal successful, deduction cancelled
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new DeductionResult(true, upheld ? "Appeal rejected, deduction upheld" : "Appeal successful, deduction cancelled", deduction);
    }

    #endregion

    #region Payroll Integration

    public async Task<decimal> GetPendingDeductionsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryDeductions
            .Where(d => d.EmployeeId == employeeId && d.Status == DisciplinaryDeductionStatus.Approved && !d.DeductedInPayslipId.HasValue)
            .SumAsync(d => d.Amount, cancellationToken);
    }

    public async Task RecordPayrollDeductionAsync(int deductionId, int payslipId, CancellationToken cancellationToken = default)
    {
        var deduction = await _context.DisciplinaryDeductions.FindAsync(new object[] { deductionId }, cancellationToken);
        if (deduction == null)
            throw new KeyNotFoundException($"Deduction {deductionId} not found");

        deduction.Status = DisciplinaryDeductionStatus.Deducted;
        deduction.DeductedInPayslipId = payslipId;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DisciplinaryDeduction>> GetDeductionsForPayrollAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.DisciplinaryDeductions
            .Where(d => d.EmployeeId == employeeId && d.Status == DisciplinaryDeductionStatus.Approved && !d.DeductedInPayslipId.HasValue)
            .OrderBy(d => d.IncidentDate)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Validation

    public async Task<bool> WouldViolateTakeHomeRuleAsync(int employeeId, decimal deductionAmount, decimal grossSalary, decimal otherDeductions, CancellationToken cancellationToken = default)
    {
        // Kenya Employment Act: Employee must take home at least 1/3 of salary
        var totalDeductions = otherDeductions + deductionAmount;
        var maxAllowedDeduction = grossSalary * (2m / 3m);

        return totalDeductions > maxAllowedDeduction;
    }

    #endregion

    #region Helper Methods for Specific Deduction Types

    public async Task<DisciplinaryDeduction> CreateAbsenceDeductionAsync(int employeeId, DateOnly[] absenceDates, string description, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var dailyWage = await CalculateDailyWageAsync(employeeId, cancellationToken);
        var daysAbsent = absenceDates.Length;
        var amount = dailyWage * daysAbsent;

        var request = new DisciplinaryDeductionRequest(
            EmployeeId: employeeId,
            ReasonType: DeductionReasonType.AbsenceWithoutLeave,
            IncidentDate: absenceDates.Min(),
            Description: description,
            Amount: amount,
            DaysAbsent: daysAbsent
        );

        return await CreateDeductionAsync(request, cancellationToken);
    }

    public async Task<DisciplinaryDeduction> CreateDamageDeductionAsync(int employeeId, decimal damageAmount, string description, string? evidencePath, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var request = new DisciplinaryDeductionRequest(
            EmployeeId: employeeId,
            ReasonType: DeductionReasonType.DamageToProperty,
            IncidentDate: DateOnly.FromDateTime(DateTime.Today),
            Description: description,
            Amount: damageAmount,
            ActualLossAmount: damageAmount,
            EvidenceDocumentPath: evidencePath
        );

        return await CreateDeductionAsync(request, cancellationToken);
    }

    public async Task<DisciplinaryDeduction> CreateCashShortageDeductionAsync(int employeeId, decimal shortageAmount, DateOnly incidentDate, string description, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var request = new DisciplinaryDeductionRequest(
            EmployeeId: employeeId,
            ReasonType: DeductionReasonType.CashShortage,
            IncidentDate: incidentDate,
            Description: description,
            Amount: shortageAmount,
            ActualLossAmount: shortageAmount
        );

        return await CreateDeductionAsync(request, cancellationToken);
    }

    public async Task<DisciplinaryDeduction> CreateOverpaymentDeductionAsync(int employeeId, decimal overpaymentAmount, string description, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var request = new DisciplinaryDeductionRequest(
            EmployeeId: employeeId,
            ReasonType: DeductionReasonType.Overpayment,
            IncidentDate: DateOnly.FromDateTime(DateTime.Today),
            Description: description,
            Amount: overpaymentAmount
        );

        return await CreateDeductionAsync(request, cancellationToken);
    }

    #endregion

    #region Reports

    public async Task<DeductionSummaryReport> GenerateSummaryReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var deductions = await _context.DisciplinaryDeductions
            .Where(d => d.IncidentDate >= startDate && d.IncidentDate <= endDate)
            .ToListAsync(cancellationToken);

        var byType = deductions
            .GroupBy(d => d.ReasonType)
            .Select(g => new DeductionByTypeBreakdown(
                ReasonType: g.Key,
                Count: g.Count(),
                TotalAmount: g.Sum(d => d.Amount)
            ))
            .ToList();

        return new DeductionSummaryReport(
            StartDate: startDate,
            EndDate: endDate,
            TotalDeductions: deductions.Count,
            TotalAmount: deductions.Sum(d => d.Amount),
            ApprovedCount: deductions.Count(d => d.Status == DisciplinaryDeductionStatus.Approved || d.Status == DisciplinaryDeductionStatus.Deducted),
            ApprovedAmount: deductions.Where(d => d.Status == DisciplinaryDeductionStatus.Approved || d.Status == DisciplinaryDeductionStatus.Deducted).Sum(d => d.Amount),
            RejectedCount: deductions.Count(d => d.Status == DisciplinaryDeductionStatus.Rejected),
            PendingCount: deductions.Count(d => d.Status == DisciplinaryDeductionStatus.Pending),
            AppealedCount: deductions.Count(d => d.Status == DisciplinaryDeductionStatus.Appealed),
            ByType: byType
        );
    }

    public async Task<EmployeeDeductionHistory> GetEmployeeHistoryAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var deductions = await _context.DisciplinaryDeductions
            .Where(d => d.EmployeeId == employeeId)
            .OrderByDescending(d => d.IncidentDate)
            .ToListAsync(cancellationToken);

        return new EmployeeDeductionHistory(
            EmployeeId: employeeId,
            EmployeeName: $"{employee.FirstName} {employee.LastName}",
            TotalDeductions: deductions.Count,
            TotalAmount: deductions.Sum(d => d.Amount),
            TotalDeducted: deductions.Where(d => d.Status == DisciplinaryDeductionStatus.Deducted).Sum(d => d.Amount),
            TotalPending: deductions.Where(d => d.Status == DisciplinaryDeductionStatus.Approved && !d.DeductedInPayslipId.HasValue).Sum(d => d.Amount),
            Deductions: deductions
        );
    }

    #endregion

    #region Utility

    public async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.Today.Year.ToString()[2..];
        var count = await _context.DisciplinaryDeductions
            .CountAsync(d => d.ReferenceNumber.StartsWith($"DD-{year}"), cancellationToken);

        return $"DD-{year}-{(count + 1):D5}";
    }

    public async Task<decimal> CalculateDailyWageAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        // Assume 22 working days per month for daily wage calculation
        return employee.BasicSalary / 22m;
    }

    #endregion
}
