using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing customer credit accounts and accounts receivable.
/// </summary>
public class AccountsReceivableService : IAccountsReceivableService
{
    private readonly POSDbContext _context;
    private readonly ILogger<AccountsReceivableService> _logger;

    // Pagination defaults to prevent unbounded result sets
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 1000;

    public AccountsReceivableService(POSDbContext context, ILogger<AccountsReceivableService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Credit Account Management

    /// <inheritdoc />
    public async Task<CustomerCreditAccount> CreateAccountAsync(CustomerCreditAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (string.IsNullOrEmpty(account.AccountNumber))
        {
            account.AccountNumber = await GenerateAccountNumberAsync(cancellationToken).ConfigureAwait(false);
        }

        account.AccountOpenedDate = DateTime.UtcNow;
        await _context.Set<CustomerCreditAccount>().AddAsync(account, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created credit account {AccountNumber} for {CustomerName}",
            account.AccountNumber, account.ContactName);

        return account;
    }

    private async Task<string> GenerateAccountNumberAsync(CancellationToken cancellationToken)
    {
        var lastAccount = await _context.Set<CustomerCreditAccount>()
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var nextNumber = (lastAccount?.Id ?? 0) + 1;
        return $"CRD{nextNumber:D6}";
    }

    /// <inheritdoc />
    public async Task<CustomerCreditAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerCreditAccount>()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CustomerCreditAccount?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerCreditAccount>()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerCreditAccount>> GetAllAccountsAsync(CreditAccountStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CustomerCreditAccount>()
            .Include(a => a.Customer)
            .Where(a => a.IsActive);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.OrderBy(a => a.ContactName).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CustomerCreditAccount> UpdateAccountAsync(CustomerCreditAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated credit account {AccountNumber}", account.AccountNumber);
        return account;
    }

    /// <inheritdoc />
    public async Task UpdateCreditLimitAsync(int accountId, decimal newLimit, int userId, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var oldLimit = account.CreditLimit;
        account.CreditLimit = newLimit;

        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated credit limit for {AccountNumber}: {OldLimit} -> {NewLimit} by user {UserId}",
            account.AccountNumber, oldLimit, newLimit, userId);
    }

    /// <inheritdoc />
    public async Task SuspendAccountAsync(int accountId, string reason, int userId, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        account.Status = CreditAccountStatus.Suspended;
        account.Notes = $"[{DateTime.UtcNow:yyyy-MM-dd}] Suspended: {reason}\n{account.Notes}";

        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Suspended credit account {AccountNumber}: {Reason}", account.AccountNumber, reason);
    }

    /// <inheritdoc />
    public async Task ReactivateAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        account.Status = CreditAccountStatus.Active;
        account.Notes = $"[{DateTime.UtcNow:yyyy-MM-dd}] Reactivated\n{account.Notes}";

        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Reactivated credit account {AccountNumber}", account.AccountNumber);
    }

    /// <inheritdoc />
    public async Task<CreditPurchaseCheckResult> CanMakeCreditPurchaseAsync(int accountId, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);

        if (account == null)
        {
            return new CreditPurchaseCheckResult
            {
                IsAllowed = false,
                DenialReason = "Account not found"
            };
        }

        var result = new CreditPurchaseCheckResult
        {
            CurrentBalance = account.CurrentBalance,
            CreditLimit = account.CreditLimit,
            AvailableCredit = account.AvailableCredit,
            RequestedAmount = amount,
            AccountStatus = account.Status
        };

        if (account.Status != CreditAccountStatus.Active)
        {
            result.IsAllowed = false;
            result.DenialReason = $"Account is {account.Status}";
            return result;
        }

        if (amount > account.AvailableCredit)
        {
            result.IsAllowed = false;
            result.DenialReason = $"Insufficient credit. Available: {account.AvailableCredit:C}, Requested: {amount:C}";
            return result;
        }

        result.IsAllowed = true;
        return result;
    }

    #endregion

    #region Credit Transactions

    /// <inheritdoc />
    public async Task<CreditTransaction> RecordCreditSaleAsync(
        int accountId,
        int receiptId,
        decimal amount,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var checkResult = await CanMakeCreditPurchaseAsync(accountId, amount, cancellationToken).ConfigureAwait(false);
        if (!checkResult.IsAllowed)
            throw new InvalidOperationException($"Credit purchase denied: {checkResult.DenialReason}");

        var transaction = new CreditTransaction
        {
            CreditAccountId = accountId,
            TransactionType = CreditTransactionType.Sale,
            ReferenceNumber = $"INV{receiptId:D8}",
            TransactionDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(account.PaymentTermsDays),
            Amount = amount,
            ReceiptId = receiptId,
            ProcessedByUserId = userId
        };

        account.CurrentBalance += amount;
        account.LastTransactionDate = DateTime.UtcNow;
        transaction.RunningBalance = account.CurrentBalance;

        await _context.Set<CreditTransaction>().AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Recorded credit sale {ReferenceNumber} for {Amount} on account {AccountNumber}",
            transaction.ReferenceNumber, amount, account.AccountNumber);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<CreditTransaction> RecordCreditNoteAsync(
        int accountId,
        decimal amount,
        string reason,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var transaction = new CreditTransaction
        {
            CreditAccountId = accountId,
            TransactionType = CreditTransactionType.CreditNote,
            ReferenceNumber = $"CN{DateTime.UtcNow:yyyyMMddHHmmss}",
            TransactionDate = DateTime.UtcNow,
            Amount = amount,
            Description = reason,
            ProcessedByUserId = userId
        };

        account.CurrentBalance -= amount;
        account.LastTransactionDate = DateTime.UtcNow;
        transaction.RunningBalance = account.CurrentBalance;

        await _context.Set<CreditTransaction>().AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Recorded credit note {ReferenceNumber} for {Amount} on account {AccountNumber}",
            transaction.ReferenceNumber, amount, account.AccountNumber);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<CreditTransaction> RecordAdjustmentAsync(
        int accountId,
        decimal amount,
        string reason,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var transactionType = amount >= 0 ? CreditTransactionType.DebitAdjustment : CreditTransactionType.CreditAdjustment;
        var absAmount = Math.Abs(amount);

        var transaction = new CreditTransaction
        {
            CreditAccountId = accountId,
            TransactionType = transactionType,
            ReferenceNumber = $"ADJ{DateTime.UtcNow:yyyyMMddHHmmss}",
            TransactionDate = DateTime.UtcNow,
            Amount = absAmount,
            Description = reason,
            ProcessedByUserId = userId
        };

        account.CurrentBalance += amount;
        account.LastTransactionDate = DateTime.UtcNow;
        transaction.RunningBalance = account.CurrentBalance;

        await _context.Set<CreditTransaction>().AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Recorded adjustment {ReferenceNumber} for {Amount} on account {AccountNumber}",
            transaction.ReferenceNumber, amount, account.AccountNumber);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CreditTransaction>> GetTransactionsAsync(
        int accountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Use default pagination to prevent memory issues with large transaction histories
        return await GetTransactionsAsync(accountId, startDate, endDate, skip: 0, take: DefaultPageSize, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets transactions for an account with pagination support.
    /// </summary>
    public async Task<IEnumerable<CreditTransaction>> GetTransactionsAsync(
        int accountId,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Enforce maximum page size
        take = Math.Min(take, MaxPageSize);

        var query = _context.Set<CreditTransaction>()
            .Include(t => t.Receipt)
            .Include(t => t.ProcessedByUser)
            .Where(t => t.CreditAccountId == accountId && t.IsActive);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CreditTransaction>> GetOutstandingTransactionsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CreditTransaction>()
            .Where(t => t.CreditAccountId == accountId
                && t.IsActive
                && t.TransactionType == CreditTransactionType.Sale
                && t.Amount > t.AmountPaid)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Payment Processing

    /// <inheritdoc />
    public async Task<CustomerPayment> RecordPaymentAsync(CustomerPayment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var account = await GetAccountByIdAsync(payment.CreditAccountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {payment.CreditAccountId} not found");

        if (string.IsNullOrEmpty(payment.PaymentNumber))
        {
            payment.PaymentNumber = $"PMT{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        await _context.Set<CustomerPayment>().AddAsync(payment, cancellationToken).ConfigureAwait(false);

        // Record as transaction
        var transaction = new CreditTransaction
        {
            CreditAccountId = payment.CreditAccountId,
            TransactionType = CreditTransactionType.Payment,
            ReferenceNumber = payment.PaymentNumber,
            TransactionDate = payment.PaymentDate,
            Amount = payment.Amount,
            Description = $"Payment via {payment.ExternalReference ?? "Cash"}",
            ProcessedByUserId = payment.ReceivedByUserId
        };

        account.CurrentBalance -= payment.Amount;
        account.LastPaymentDate = payment.PaymentDate;
        account.LastTransactionDate = payment.PaymentDate;
        transaction.RunningBalance = account.CurrentBalance;

        // Update status based on balance
        if (account.CurrentBalance <= 0)
        {
            account.Status = CreditAccountStatus.Active;
        }

        await _context.Set<CreditTransaction>().AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        _context.Set<CustomerCreditAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Recorded payment {PaymentNumber} for {Amount} on account {AccountNumber}",
            payment.PaymentNumber, payment.Amount, account.AccountNumber);

        return payment;
    }

    /// <inheritdoc />
    public async Task AllocatePaymentAsync(
        int paymentId,
        IEnumerable<(int transactionId, decimal amount)> allocations,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Set<CustomerPayment>()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            .ConfigureAwait(false);

        if (payment == null)
            throw new InvalidOperationException($"Payment {paymentId} not found");

        foreach (var (transactionId, amount) in allocations)
        {
            var transaction = await _context.Set<CreditTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken)
                .ConfigureAwait(false);

            if (transaction == null) continue;

            var allocation = new PaymentAllocation
            {
                PaymentId = paymentId,
                TransactionId = transactionId,
                Amount = amount,
                AllocationDate = DateTime.UtcNow
            };

            transaction.AmountPaid += amount;
            payment.AllocatedAmount += amount;

            await _context.Set<PaymentAllocation>().AddAsync(allocation, cancellationToken).ConfigureAwait(false);
            _context.Set<CreditTransaction>().Update(transaction);
        }

        _context.Set<CustomerPayment>().Update(payment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentAllocation>> AutoAllocatePaymentAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Set<CustomerPayment>()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            .ConfigureAwait(false);

        if (payment == null)
            throw new InvalidOperationException($"Payment {paymentId} not found");

        var outstanding = await GetOutstandingTransactionsAsync(payment.CreditAccountId, cancellationToken).ConfigureAwait(false);
        var allocations = new List<PaymentAllocation>();
        var remainingAmount = payment.Amount - payment.AllocatedAmount;

        foreach (var transaction in outstanding)
        {
            if (remainingAmount <= 0) break;

            var amountToAllocate = Math.Min(remainingAmount, transaction.RemainingBalance);

            var allocation = new PaymentAllocation
            {
                PaymentId = paymentId,
                TransactionId = transaction.Id,
                Amount = amountToAllocate,
                AllocationDate = DateTime.UtcNow
            };

            transaction.AmountPaid += amountToAllocate;
            payment.AllocatedAmount += amountToAllocate;
            remainingAmount -= amountToAllocate;

            allocations.Add(allocation);
            await _context.Set<PaymentAllocation>().AddAsync(allocation, cancellationToken).ConfigureAwait(false);
            _context.Set<CreditTransaction>().Update(transaction);
        }

        _context.Set<CustomerPayment>().Update(payment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return allocations;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerPayment>> GetPaymentsAsync(
        int accountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CustomerPayment>()
            .Include(p => p.PaymentMethod)
            .Include(p => p.ReceivedByUser)
            .Where(p => p.CreditAccountId == accountId && p.IsActive);

        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Aging Reports

    /// <inheritdoc />
    public async Task<IEnumerable<AgingEntry>> GetAgingReportAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Set<CustomerCreditAccount>()
            .Where(a => a.IsActive && a.CurrentBalance > 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<AgingEntry>();

        foreach (var account in accounts)
        {
            var detail = await GetAccountAgingDetailAsync(account.Id, asOfDate, cancellationToken).ConfigureAwait(false);

            entries.Add(new AgingEntry
            {
                CreditAccountId = account.Id,
                AccountNumber = account.AccountNumber,
                CustomerName = account.BusinessName ?? account.ContactName,
                CurrentAmount = detail.CurrentAmount,
                Days1To30 = detail.Days1To30,
                Days31To60 = detail.Days31To60,
                Days61To90 = detail.Days61To90,
                Over90Days = detail.Over90Days,
                TotalBalance = detail.TotalBalance
            });
        }

        return entries.OrderByDescending(e => e.TotalBalance);
    }

    /// <inheritdoc />
    public async Task<AgingSummary> GetAgingSummaryAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var entries = await GetAgingReportAsync(asOfDate, cancellationToken).ConfigureAwait(false);
        var entryList = entries.ToList();

        return new AgingSummary
        {
            AsOfDate = asOfDate,
            TotalAccounts = entryList.Count,
            TotalOutstanding = entryList.Sum(e => e.TotalBalance),
            CurrentAmount = entryList.Sum(e => e.CurrentAmount),
            Days1To30 = entryList.Sum(e => e.Days1To30),
            Days31To60 = entryList.Sum(e => e.Days31To60),
            Days61To90 = entryList.Sum(e => e.Days61To90),
            Over90Days = entryList.Sum(e => e.Over90Days)
        };
    }

    /// <inheritdoc />
    public async Task<AccountAgingDetail> GetAccountAgingDetailAsync(int accountId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var outstanding = await GetOutstandingTransactionsAsync(accountId, cancellationToken).ConfigureAwait(false);

        var detail = new AccountAgingDetail
        {
            AccountId = accountId,
            AccountNumber = account.AccountNumber,
            CustomerName = account.BusinessName ?? account.ContactName,
            CreditLimit = account.CreditLimit
        };

        foreach (var transaction in outstanding)
        {
            var dueDate = transaction.DueDate ?? transaction.TransactionDate;
            var daysOverdue = (asOfDate - dueDate).Days;
            var bucket = GetAgingBucket(daysOverdue);
            var amount = transaction.RemainingBalance;

            var transactionDetail = new TransactionAgingDetail
            {
                TransactionId = transaction.Id,
                ReferenceNumber = transaction.ReferenceNumber,
                TransactionDate = transaction.TransactionDate,
                DueDate = transaction.DueDate,
                OriginalAmount = transaction.Amount,
                OutstandingAmount = amount,
                DaysOverdue = Math.Max(0, daysOverdue),
                Bucket = bucket
            };

            detail.Transactions.Add(transactionDetail);

            switch (bucket)
            {
                case AgingBucket.Current:
                    detail.CurrentAmount += amount;
                    break;
                case AgingBucket.Days1To30:
                    detail.Days1To30 += amount;
                    break;
                case AgingBucket.Days31To60:
                    detail.Days31To60 += amount;
                    break;
                case AgingBucket.Days61To90:
                    detail.Days61To90 += amount;
                    break;
                case AgingBucket.Over90Days:
                    detail.Over90Days += amount;
                    break;
            }

            detail.TotalBalance += amount;
        }

        return detail;
    }

    private static AgingBucket GetAgingBucket(int daysOverdue)
    {
        return daysOverdue switch
        {
            <= 0 => AgingBucket.Current,
            <= 30 => AgingBucket.Days1To30,
            <= 60 => AgingBucket.Days31To60,
            <= 90 => AgingBucket.Days61To90,
            _ => AgingBucket.Over90Days
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerCreditAccount>> GetOverdueAccountsAsync(int daysOverdue = 1, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOverdue);

        var overdueAccountIds = await _context.Set<CreditTransaction>()
            .Where(t => t.IsActive
                && t.TransactionType == CreditTransactionType.Sale
                && t.Amount > t.AmountPaid
                && t.DueDate < cutoffDate)
            .Select(t => t.CreditAccountId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _context.Set<CustomerCreditAccount>()
            .Where(a => overdueAccountIds.Contains(a.Id) && a.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Statement Generation

    /// <inheritdoc />
    public async Task<CustomerStatement> GenerateStatementAsync(
        int accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");

        var transactions = await GetTransactionsAsync(accountId, startDate, endDate, cancellationToken).ConfigureAwait(false);
        var transactionList = transactions.ToList();

        // Calculate opening balance (balance before period)
        var priorTransactions = await _context.Set<CreditTransaction>()
            .Where(t => t.CreditAccountId == accountId && t.TransactionDate < startDate && t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal openingBalance = 0;
        foreach (var t in priorTransactions)
        {
            if (t.TransactionType == CreditTransactionType.Sale || t.TransactionType == CreditTransactionType.DebitAdjustment)
                openingBalance += t.Amount;
            else
                openingBalance -= t.Amount;
        }

        var totalCharges = transactionList
            .Where(t => t.TransactionType == CreditTransactionType.Sale || t.TransactionType == CreditTransactionType.DebitAdjustment)
            .Sum(t => t.Amount);

        var totalPayments = transactionList
            .Where(t => t.TransactionType == CreditTransactionType.Payment)
            .Sum(t => t.Amount);

        var totalCredits = transactionList
            .Where(t => t.TransactionType == CreditTransactionType.CreditNote || t.TransactionType == CreditTransactionType.CreditAdjustment)
            .Sum(t => t.Amount);

        var statement = new CustomerStatement
        {
            CreditAccountId = accountId,
            StatementNumber = $"STMT{DateTime.UtcNow:yyyyMMdd}{accountId:D4}",
            PeriodStartDate = startDate,
            PeriodEndDate = endDate,
            OpeningBalance = openingBalance,
            TotalCharges = totalCharges,
            TotalPayments = totalPayments,
            TotalCredits = totalCredits,
            ClosingBalance = openingBalance + totalCharges - totalPayments - totalCredits,
            GeneratedAt = DateTime.UtcNow
        };

        await _context.Set<CustomerStatement>().AddAsync(statement, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Generated statement {StatementNumber} for account {AccountNumber}",
            statement.StatementNumber, account.AccountNumber);

        return statement;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerStatement>> GenerateAllStatementsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Set<CustomerCreditAccount>()
            .Where(a => a.IsActive && a.Status != CreditAccountStatus.Closed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var statements = new List<CustomerStatement>();

        foreach (var account in accounts)
        {
            var statement = await GenerateStatementAsync(account.Id, startDate, endDate, cancellationToken).ConfigureAwait(false);
            statements.Add(statement);
        }

        return statements;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerStatement>> GetStatementsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerStatement>()
            .Where(s => s.CreditAccountId == accountId && s.IsActive)
            .OrderByDescending(s => s.PeriodEndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkStatementSentAsync(int statementId, string sentVia, CancellationToken cancellationToken = default)
    {
        var statement = await _context.Set<CustomerStatement>()
            .FirstOrDefaultAsync(s => s.Id == statementId, cancellationToken)
            .ConfigureAwait(false);

        if (statement == null)
            throw new InvalidOperationException($"Statement {statementId} not found");

        statement.SentAt = DateTime.UtcNow;
        statement.SentVia = sentVia;

        _context.Set<CustomerStatement>().Update(statement);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
