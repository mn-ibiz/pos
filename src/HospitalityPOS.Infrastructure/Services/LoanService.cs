// src/HospitalityPOS.Infrastructure/Services/LoanService.cs
// Implementation of employee loan and salary advance service.
// Compliant with Kenya Employment Act 2007 Section 17 & 19.

using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing employee loans and salary advances.
/// </summary>
public class LoanService : ILoanService
{
    private readonly POSDbContext _context;

    public LoanService(POSDbContext context)
    {
        _context = context;
    }

    #region Loan/Advance CRUD

    public async Task<EmployeeLoan> CreateLoanApplicationAsync(LoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { request.EmployeeId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found");

        // Check eligibility
        var eligibility = await CheckEligibilityAsync(request.EmployeeId, request.Amount, request.LoanType, cancellationToken);
        if (!eligibility.IsEligible)
        {
            throw new InvalidOperationException($"Employee is not eligible for this loan: {string.Join("; ", eligibility.Errors)}");
        }

        // Calculate interest and total amount
        var interestRate = GetInterestRate(request.LoanType);
        var totalInterest = CalculateSimpleInterest(request.Amount, interestRate, request.NumberOfInstallments);
        var totalAmountDue = request.Amount + totalInterest;
        var monthlyInstallment = totalAmountDue / request.NumberOfInstallments;

        var loan = new EmployeeLoan
        {
            EmployeeId = request.EmployeeId,
            LoanNumber = await GenerateLoanNumberAsync(request.LoanType, cancellationToken),
            LoanType = request.LoanType,
            Status = LoanStatus.Pending,
            PrincipalAmount = request.Amount,
            InterestRate = interestRate,
            TotalInterest = totalInterest,
            TotalAmountDue = totalAmountDue,
            AmountPaid = 0,
            NumberOfInstallments = request.NumberOfInstallments,
            MonthlyInstallment = monthlyInstallment,
            InstallmentsPaid = 0,
            ApplicationDate = DateOnly.FromDateTime(DateTime.Today),
            RequestedDisbursementDate = request.RequestedDisbursementDate,
            ExpectedCompletionDate = request.RequestedDisbursementDate.AddMonths(request.NumberOfInstallments),
            EmployeeBasicSalaryAtApplication = employee.BasicSalary,
            GuarantorEmployeeId = request.GuarantorEmployeeId,
            RequiresGuarantor = request.Amount > (employee.BasicSalary * 2),
            Purpose = request.Purpose,
            ExceedsTwoMonthsSalary = request.Amount > (employee.BasicSalary * 2) && request.LoanType == LoanType.SalaryAdvance,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeLoans.Add(loan);
        await _context.SaveChangesAsync(cancellationToken);

        return loan;
    }

    public async Task<EmployeeLoan?> GetLoanByIdAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeLoans
            .Include(l => l.Employee)
            .Include(l => l.Guarantor)
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);
    }

    public async Task<EmployeeLoan?> GetLoanByNumberAsync(string loanNumber, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeLoans
            .Include(l => l.Employee)
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLoan>> GetEmployeeLoansAsync(int employeeId, bool includeCompleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeLoans
            .Include(l => l.Repayments)
            .Where(l => l.EmployeeId == employeeId);

        if (!includeCompleted)
        {
            query = query.Where(l => l.Status != LoanStatus.Completed && l.Status != LoanStatus.Rejected && l.Status != LoanStatus.Cancelled);
        }

        return await query.OrderByDescending(l => l.ApplicationDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLoan>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeLoans
            .Include(l => l.Employee)
            .Where(l => l.Status == LoanStatus.Pending)
            .OrderBy(l => l.ApplicationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLoan>> GetActiveLoansAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeLoans
            .Include(l => l.Employee)
            .Include(l => l.Repayments)
            .Where(l => l.Status == LoanStatus.Active)
            .OrderBy(l => l.ExpectedCompletionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLoan>> GetLoansAsync(LoanFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeLoans
            .Include(l => l.Employee)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.EmployeeId.HasValue)
                query = query.Where(l => l.EmployeeId == filter.EmployeeId.Value);

            if (filter.LoanType.HasValue)
                query = query.Where(l => l.LoanType == filter.LoanType.Value);

            if (filter.Status.HasValue)
                query = query.Where(l => l.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.ApplicationDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.ApplicationDate <= filter.ToDate.Value);
        }

        return await query.OrderByDescending(l => l.ApplicationDate).ToListAsync(cancellationToken);
    }

    #endregion

    #region Approval Workflow

    public async Task<LoanResult> ApproveLoanAsync(int loanId, int approverUserId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans.FindAsync(new object[] { loanId }, cancellationToken);
        if (loan == null)
            return new LoanResult(false, "Loan not found");

        if (loan.Status != LoanStatus.Pending)
            return new LoanResult(false, $"Loan is not pending (status: {loan.Status})");

        loan.Status = LoanStatus.Approved;
        loan.ApprovedByUserId = approverUserId;
        loan.ApprovalDate = DateOnly.FromDateTime(DateTime.Today);
        loan.ApprovalNotes = notes;
        loan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new LoanResult(true, "Loan approved successfully", loan);
    }

    public async Task<LoanResult> RejectLoanAsync(int loanId, int approverUserId, string reason, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans.FindAsync(new object[] { loanId }, cancellationToken);
        if (loan == null)
            return new LoanResult(false, "Loan not found");

        if (loan.Status != LoanStatus.Pending)
            return new LoanResult(false, $"Loan is not pending (status: {loan.Status})");

        loan.Status = LoanStatus.Rejected;
        loan.RejectedByUserId = approverUserId;
        loan.RejectionReason = reason;
        loan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new LoanResult(true, "Loan rejected", loan);
    }

    public async Task<LoanResult> CancelLoanAsync(int loanId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans.FindAsync(new object[] { loanId }, cancellationToken);
        if (loan == null)
            return new LoanResult(false, "Loan not found");

        if (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Completed)
            return new LoanResult(false, "Cannot cancel a loan that has been disbursed");

        loan.Status = LoanStatus.Cancelled;
        loan.Notes = $"Cancelled by user {userId}: {reason}";
        loan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return new LoanResult(true, "Loan cancelled", loan);
    }

    #endregion

    #region Disbursement

    public async Task<LoanResult> MarkAsDisbursedAsync(int loanId, DateOnly disbursementDate, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
            return new LoanResult(false, "Loan not found");

        if (loan.Status != LoanStatus.Approved)
            return new LoanResult(false, $"Loan must be approved before disbursement (status: {loan.Status})");

        loan.Status = LoanStatus.Active;
        loan.DisbursementDate = disbursementDate;
        loan.FirstInstallmentDate = disbursementDate.AddMonths(1);
        loan.ExpectedCompletionDate = disbursementDate.AddMonths(loan.NumberOfInstallments);
        loan.UpdatedAt = DateTime.UtcNow;

        // Generate repayment schedule
        await GenerateRepaymentScheduleInternalAsync(loan, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return new LoanResult(true, "Loan marked as disbursed", loan);
    }

    #endregion

    #region Repayment

    public async Task<LoanResult> RecordRepaymentAsync(int loanId, decimal amount, DateOnly paymentDate, string? notes = null, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
            return new LoanResult(false, "Loan not found");

        if (loan.Status != LoanStatus.Active)
            return new LoanResult(false, $"Loan is not active (status: {loan.Status})");

        // Find the next pending repayment or update an existing one
        var pendingRepayment = loan.Repayments
            .Where(r => !r.IsPaid)
            .OrderBy(r => r.InstallmentNumber)
            .FirstOrDefault();

        if (pendingRepayment != null)
        {
            pendingRepayment.AmountPaid = amount;
            pendingRepayment.PaidDate = paymentDate;
            pendingRepayment.IsPaid = true;
            pendingRepayment.Notes = notes;
        }
        else
        {
            // Create a manual repayment
            var repayment = new LoanRepayment
            {
                EmployeeLoanId = loanId,
                InstallmentNumber = loan.InstallmentsPaid + 1,
                DueDate = paymentDate,
                AmountDue = amount,
                AmountPaid = amount,
                PaidDate = paymentDate,
                IsPaid = true,
                Notes = notes
            };
            _context.LoanRepayments.Add(repayment);
        }

        // Update loan totals
        loan.AmountPaid += amount;
        loan.InstallmentsPaid++;
        loan.LastPaymentDate = paymentDate;
        loan.UpdatedAt = DateTime.UtcNow;

        // Update balance after payment for all subsequent repayments
        var remainingBalance = loan.OutstandingBalance;
        foreach (var repayment in loan.Repayments.Where(r => r.IsPaid).OrderBy(r => r.InstallmentNumber))
        {
            repayment.BalanceAfterPayment = remainingBalance;
        }

        // Check if loan is fully paid
        if (loan.OutstandingBalance <= 0)
        {
            loan.Status = LoanStatus.Completed;
            loan.ActualCompletionDate = paymentDate;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new LoanResult(true, "Repayment recorded successfully", loan);
    }

    public async Task<IReadOnlyList<LoanRepayment>> GetRepaymentScheduleAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _context.LoanRepayments
            .Where(r => r.EmployeeLoanId == loanId)
            .OrderBy(r => r.InstallmentNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetPendingDeductionAsync(int employeeId, DateOnly payrollDate, CancellationToken cancellationToken = default)
    {
        var activeLoans = await _context.EmployeeLoans
            .Include(l => l.Repayments)
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        decimal totalDeduction = 0;
        foreach (var loan in activeLoans)
        {
            var nextRepayment = loan.Repayments
                .Where(r => !r.IsPaid && r.DueDate <= payrollDate)
                .OrderBy(r => r.DueDate)
                .FirstOrDefault();

            if (nextRepayment != null)
            {
                totalDeduction += nextRepayment.AmountDue;
            }
        }

        return totalDeduction;
    }

    public async Task RecordPayrollDeductionAsync(int loanId, int payslipDetailId, decimal amount, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
            throw new KeyNotFoundException($"Loan {loanId} not found");

        // Find the next pending repayment
        var repayment = loan.Repayments
            .Where(r => !r.IsPaid)
            .OrderBy(r => r.InstallmentNumber)
            .FirstOrDefault();

        if (repayment != null)
        {
            repayment.AmountPaid = amount;
            repayment.PaidDate = DateOnly.FromDateTime(DateTime.Today);
            repayment.IsPaid = true;
            repayment.PayslipDetailId = payslipDetailId;
            repayment.BalanceAfterPayment = loan.OutstandingBalance - amount;
        }

        loan.AmountPaid += amount;
        loan.InstallmentsPaid++;
        loan.LastPaymentDate = DateOnly.FromDateTime(DateTime.Today);
        loan.UpdatedAt = DateTime.UtcNow;

        if (loan.OutstandingBalance <= 0)
        {
            loan.Status = LoanStatus.Completed;
            loan.ActualCompletionDate = DateOnly.FromDateTime(DateTime.Today);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoanRepayment>> GenerateRepaymentScheduleAsync(int loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        if (loan == null)
            throw new KeyNotFoundException($"Loan {loanId} not found");

        return await GenerateRepaymentScheduleInternalAsync(loan, cancellationToken);
    }

    private async Task<IReadOnlyList<LoanRepayment>> GenerateRepaymentScheduleInternalAsync(EmployeeLoan loan, CancellationToken cancellationToken)
    {
        // Clear existing unpaid schedule
        var unpaidRepayments = loan.Repayments.Where(r => !r.IsPaid).ToList();
        _context.LoanRepayments.RemoveRange(unpaidRepayments);

        var startDate = loan.FirstInstallmentDate ?? loan.DisbursementDate?.AddMonths(1) ?? DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
        var remainingAmount = loan.OutstandingBalance;
        var monthlyInstallment = loan.MonthlyInstallment;
        var startInstallment = loan.InstallmentsPaid + 1;
        var remainingInstallments = loan.NumberOfInstallments - loan.InstallmentsPaid;

        for (int i = 0; i < remainingInstallments; i++)
        {
            var installmentAmount = i == remainingInstallments - 1
                ? remainingAmount // Last installment takes remaining balance
                : Math.Min(monthlyInstallment, remainingAmount);

            // Calculate principal and interest portions (simple interest)
            var interestPortion = (loan.TotalInterest / loan.NumberOfInstallments);
            var principalPortion = installmentAmount - interestPortion;

            remainingAmount -= principalPortion;

            var repayment = new LoanRepayment
            {
                EmployeeLoanId = loan.Id,
                InstallmentNumber = startInstallment + i,
                DueDate = startDate.AddMonths(i),
                AmountDue = installmentAmount,
                PrincipalPortion = principalPortion,
                InterestPortion = interestPortion,
                BalanceAfterPayment = remainingAmount,
                IsPaid = false
            };

            _context.LoanRepayments.Add(repayment);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetRepaymentScheduleAsync(loan.Id, cancellationToken);
    }

    #endregion

    #region Validation (Kenya Compliance)

    public async Task<LoanEligibilityResult> CheckEligibilityAsync(int employeeId, decimal requestedAmount, LoanType loanType, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
        if (employee == null)
        {
            return new LoanEligibilityResult(false, 0, 0, 0, 0,
                new List<string>(),
                new List<string> { "Employee not found" });
        }

        var warnings = new List<string>();
        var errors = new List<string>();

        // Get current outstanding loans
        var outstandingLoans = await _context.EmployeeLoans
            .Where(l => l.EmployeeId == employeeId && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Approved))
            .SumAsync(l => l.OutstandingBalance, cancellationToken);

        // Get current monthly deductions from active loans
        var currentMonthlyDeductions = await _context.EmployeeLoans
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .SumAsync(l => l.MonthlyInstallment, cancellationToken);

        // Calculate max eligible amount
        var maxAmount = await CalculateMaxLoanAmountAsync(employeeId, loanType, cancellationToken);

        // Calculate available deduction capacity
        var grossSalary = employee.BasicSalary + employee.Allowances;
        var maxDeduction = await CalculateMaxDeductionAsync(employeeId, grossSalary, currentMonthlyDeductions, cancellationToken);
        var availableCapacity = maxDeduction - currentMonthlyDeductions;

        // Validation checks
        if (requestedAmount > maxAmount)
        {
            errors.Add($"Requested amount exceeds maximum eligible amount ({maxAmount:N0})");
        }

        // Kenya Employment Act 2007 Section 17: Salary advances > 2 months not recoverable in court
        if (loanType == LoanType.SalaryAdvance && requestedAmount > employee.BasicSalary * 2)
        {
            warnings.Add("Salary advance exceeds 2 months' wages. Per Kenya Employment Act Section 17, amounts above this may not be recoverable through legal means.");
        }

        // Check 2/3 take-home rule
        var estimatedMonthlyDeduction = requestedAmount / 12; // Assume 12-month repayment for check
        var wouldViolate = await WouldExceedTwoThirdsRuleAsync(employeeId, estimatedMonthlyDeduction, grossSalary, currentMonthlyDeductions, cancellationToken);
        if (wouldViolate)
        {
            errors.Add("This loan would cause total deductions to exceed 2/3 of salary (Kenya law requires 1/3 take-home minimum)");
        }

        var isEligible = !errors.Any();

        return new LoanEligibilityResult(
            IsEligible: isEligible,
            MaxEligibleAmount: maxAmount,
            CurrentOutstandingLoans: outstandingLoans,
            CurrentMonthlyDeductions: currentMonthlyDeductions,
            AvailableDeductionCapacity: availableCapacity,
            Warnings: warnings,
            Errors: errors
        );
    }

    public async Task<decimal> CalculateMaxLoanAmountAsync(int employeeId, LoanType loanType, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
        if (employee == null) return 0;

        var basicSalary = employee.BasicSalary;

        return loanType switch
        {
            LoanType.SalaryAdvance => basicSalary * 2, // Max 2 months (Kenya law recoverable limit)
            LoanType.EmergencyLoan => basicSalary * 3, // Emergency: up to 3 months
            LoanType.EmployeeLoan => basicSalary * 12, // Standard loan: up to 12 months based on repayment capacity
            _ => basicSalary * 2
        };
    }

    public Task<decimal> CalculateMaxDeductionAsync(int employeeId, decimal grossSalary, decimal otherDeductions, CancellationToken cancellationToken = default)
    {
        // Kenya law: Loan deductions max 50% of wages after other statutory deductions
        // But total deductions cannot exceed 2/3 of salary (1/3 must be take-home)
        var maxTotalDeduction = grossSalary * (2m / 3m); // Max 2/3 can be deducted
        var maxLoanDeduction = (grossSalary - otherDeductions) * 0.5m; // 50% rule for loans

        return Task.FromResult(Math.Min(maxTotalDeduction - otherDeductions, maxLoanDeduction));
    }

    public Task<bool> WouldExceedTwoThirdsRuleAsync(int employeeId, decimal newDeduction, decimal grossSalary, decimal existingDeductions, CancellationToken cancellationToken = default)
    {
        // Kenya Employment Act: Employee must take home at least 1/3 of salary
        var totalDeductions = existingDeductions + newDeduction;
        var maxAllowedDeduction = grossSalary * (2m / 3m);

        return Task.FromResult(totalDeductions > maxAllowedDeduction);
    }

    #endregion

    #region Reports

    public async Task<LoanSummaryReport> GenerateSummaryReportAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        var activeLoans = await _context.EmployeeLoans
            .Where(l => l.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        var pendingLoans = await _context.EmployeeLoans
            .Where(l => l.Status == LoanStatus.Pending)
            .ToListAsync(cancellationToken);

        var overdueRepayments = await _context.LoanRepayments
            .Include(r => r.EmployeeLoan)
            .Where(r => !r.IsPaid && r.DueDate < date && r.EmployeeLoan.Status == LoanStatus.Active)
            .ToListAsync(cancellationToken);

        var byType = activeLoans
            .GroupBy(l => l.LoanType)
            .Select(g => new LoanByTypeBreakdown(
                LoanType: g.Key,
                Count: g.Count(),
                TotalAmount: g.Sum(l => l.PrincipalAmount),
                OutstandingBalance: g.Sum(l => l.OutstandingBalance)
            ))
            .ToList();

        return new LoanSummaryReport(
            AsOfDate: date,
            TotalActiveLoans: activeLoans.Count,
            TotalOutstandingPrincipal: activeLoans.Sum(l => l.PrincipalAmount - l.AmountPaid + l.TotalInterest),
            TotalOutstandingInterest: activeLoans.Sum(l => l.TotalInterest * (1 - (l.AmountPaid / l.TotalAmountDue))),
            TotalOutstandingBalance: activeLoans.Sum(l => l.OutstandingBalance),
            TotalPendingApplications: pendingLoans.Count,
            TotalPendingAmount: pendingLoans.Sum(l => l.PrincipalAmount),
            OverdueInstallmentsCount: overdueRepayments.Count,
            OverdueAmount: overdueRepayments.Sum(r => r.AmountDue - r.AmountPaid),
            ByType: byType
        );
    }

    public async Task<EmployeeLoanStatement> GenerateEmployeeStatementAsync(int employeeId, int loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _context.EmployeeLoans
            .Include(l => l.Employee)
            .Include(l => l.Repayments)
            .FirstOrDefaultAsync(l => l.Id == loanId && l.EmployeeId == employeeId, cancellationToken);

        if (loan == null)
            throw new KeyNotFoundException($"Loan {loanId} not found for employee {employeeId}");

        return new EmployeeLoanStatement(
            LoanId: loan.Id,
            LoanNumber: loan.LoanNumber,
            EmployeeId: loan.EmployeeId,
            EmployeeName: $"{loan.Employee.FirstName} {loan.Employee.LastName}",
            LoanType: loan.LoanType,
            PrincipalAmount: loan.PrincipalAmount,
            TotalInterest: loan.TotalInterest,
            TotalAmountDue: loan.TotalAmountDue,
            AmountPaid: loan.AmountPaid,
            OutstandingBalance: loan.OutstandingBalance,
            DisbursementDate: loan.DisbursementDate,
            ExpectedCompletionDate: loan.ExpectedCompletionDate,
            Repayments: loan.Repayments.OrderBy(r => r.InstallmentNumber).ToList()
        );
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeLoans
            .Where(l => l.EmployeeId == employeeId && l.Status == LoanStatus.Active)
            .SumAsync(l => l.OutstandingBalance, cancellationToken);
    }

    #endregion

    #region Utility

    public async Task<string> GenerateLoanNumberAsync(LoanType loanType, CancellationToken cancellationToken = default)
    {
        var prefix = loanType switch
        {
            LoanType.SalaryAdvance => "ADV",
            LoanType.EmergencyLoan => "EMG",
            _ => "LN"
        };

        var year = DateTime.Today.Year.ToString()[2..];
        var count = await _context.EmployeeLoans
            .CountAsync(l => l.LoanNumber.StartsWith($"{prefix}-{year}"), cancellationToken);

        return $"{prefix}-{year}-{(count + 1):D5}";
    }

    private static decimal GetInterestRate(LoanType loanType)
    {
        return loanType switch
        {
            LoanType.SalaryAdvance => 0, // No interest on advances
            LoanType.EmergencyLoan => 0, // No interest on emergency
            LoanType.EmployeeLoan => 5, // 5% annual for staff loans
            _ => 0
        };
    }

    private static decimal CalculateSimpleInterest(decimal principal, decimal annualRate, int months)
    {
        if (annualRate <= 0) return 0;
        return principal * (annualRate / 100) * (months / 12m);
    }

    #endregion
}
