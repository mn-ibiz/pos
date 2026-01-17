using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of bank reconciliation service.
/// </summary>
public class BankReconciliationService : IBankReconciliationService
{
    private readonly POSDbContext _context;
    private readonly ILogger<BankReconciliationService> _logger;

    public BankReconciliationService(POSDbContext context, ILogger<BankReconciliationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Bank Account Management

    public async Task<BankAccount> CreateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        account.CurrentBalance = account.OpeningBalance;
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return account;
    }

    public async Task<BankAccount?> GetBankAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Include(a => a.ChartOfAccount)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<BankAccount>> GetAllBankAccountsAsync(BankAccountStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BankAccounts.Where(a => a.IsActive);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query
            .OrderBy(a => a.BankName)
            .ThenBy(a => a.AccountNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        _context.BankAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return account;
    }

    public async Task CloseBankAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {accountId} not found.");

        account.Status = BankAccountStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LinkMpesaAccountAsync(int accountId, string mpesaShortCode, CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(accountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {accountId} not found.");

        account.MpesaShortCode = mpesaShortCode;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Transaction Import

    public async Task<BankStatementImport> ImportFromCsvAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        CsvImportOptions options,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(bankAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        var batchId = $"CSV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var transactions = new List<BankTransaction>();
        var importResult = new BankStatementImport
        {
            BankAccountId = bankAccountId,
            BatchId = batchId,
            FileName = fileName,
            FileFormat = "CSV",
            ImportedByUserId = userId,
            IsActive = true
        };

        try
        {
            using var reader = new StreamReader(fileStream);
            var lineNumber = 0;
            decimal? firstBalance = null;
            decimal? lastBalance = null;
            DateTime? minDate = null;
            DateTime? maxDate = null;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                lineNumber++;

                if (options.HasHeaderRow && lineNumber == 1)
                    continue;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = ParseCsvLine(line, options.Delimiter);

                try
                {
                    var transaction = ParseCsvTransaction(columns, options, bankAccountId, batchId, fileName);

                    // Check for duplicates
                    var isDuplicate = await _context.BankTransactions
                        .AnyAsync(t => t.BankAccountId == bankAccountId &&
                                      t.BankReference == transaction.BankReference &&
                                      t.TransactionDate == transaction.TransactionDate &&
                                      t.Amount == transaction.Amount,
                                 cancellationToken).ConfigureAwait(false);

                    if (isDuplicate)
                    {
                        importResult.SkippedCount++;
                        continue;
                    }

                    transactions.Add(transaction);

                    if (transaction.DepositAmount.HasValue)
                        importResult.TotalDeposits += transaction.DepositAmount.Value;
                    if (transaction.WithdrawalAmount.HasValue)
                        importResult.TotalWithdrawals += transaction.WithdrawalAmount.Value;

                    if (!minDate.HasValue || transaction.TransactionDate < minDate)
                        minDate = transaction.TransactionDate;
                    if (!maxDate.HasValue || transaction.TransactionDate > maxDate)
                        maxDate = transaction.TransactionDate;

                    if (transaction.RunningBalance.HasValue)
                    {
                        firstBalance ??= transaction.RunningBalance.Value;
                        lastBalance = transaction.RunningBalance.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse CSV line {LineNumber} during import", lineNumber);
                    importResult.FailedCount++;
                }
            }

            importResult.StatementStartDate = minDate ?? DateTime.UtcNow;
            importResult.StatementEndDate = maxDate ?? DateTime.UtcNow;
            importResult.OpeningBalance = firstBalance ?? 0;
            importResult.ClosingBalance = lastBalance ?? 0;
            importResult.TotalTransactions = transactions.Count + importResult.SkippedCount + importResult.FailedCount;
            importResult.ImportedCount = transactions.Count;
            importResult.Status = "Completed";

            _context.BankStatementImports.Add(importResult);
            _context.BankTransactions.AddRange(transactions);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import bank statement from file {FileName}", fileName);
            importResult.Status = "Failed";
            importResult.ErrorMessage = ex.Message;
            _context.BankStatementImports.Add(importResult);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return importResult;
    }

    public async Task<BankStatementImport> ImportFromExcelAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        ExcelImportOptions options,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Excel import would require a library like EPPlus or ClosedXML
        // For now, return a placeholder implementation
        var batchId = $"XLS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var importResult = new BankStatementImport
        {
            BankAccountId = bankAccountId,
            BatchId = batchId,
            FileName = fileName,
            FileFormat = "Excel",
            ImportedByUserId = userId,
            Status = "NotImplemented",
            ErrorMessage = "Excel import requires additional library integration",
            IsActive = true
        };

        _context.BankStatementImports.Add(importResult);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return importResult;
    }

    public async Task<BankStatementImport> ImportFromOfxAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // OFX import would require parsing the OFX/QFX format
        var batchId = $"OFX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var importResult = new BankStatementImport
        {
            BankAccountId = bankAccountId,
            BatchId = batchId,
            FileName = fileName,
            FileFormat = "OFX",
            ImportedByUserId = userId,
            Status = "NotImplemented",
            ErrorMessage = "OFX import requires additional parser integration",
            IsActive = true
        };

        _context.BankStatementImports.Add(importResult);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return importResult;
    }

    public async Task<IEnumerable<BankStatementImport>> GetImportHistoryAsync(
        int bankAccountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BankStatementImports
            .Where(i => i.BankAccountId == bankAccountId && i.IsActive);

        if (startDate.HasValue)
            query = query.Where(i => i.ImportedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.ImportedAt <= endDate.Value);

        return await query
            .OrderByDescending(i => i.ImportedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<BankTransaction>> GetBankTransactionsAsync(
        int bankAccountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        ReconciliationMatchStatus? matchStatus = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BankTransactions
            .Where(t => t.BankAccountId == bankAccountId && t.IsActive);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        if (matchStatus.HasValue)
            query = query.Where(t => t.MatchStatus == matchStatus.Value);

        return await query
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BankTransaction> AddBankTransactionAsync(BankTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(transaction.BankReference))
        {
            transaction.BankReference = $"MAN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        }

        _context.BankTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return transaction;
    }

    public async Task DeleteBankTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank transaction {transactionId} not found.");

        if (transaction.MatchStatus is ReconciliationMatchStatus.AutoMatched or ReconciliationMatchStatus.ManuallyMatched)
        {
            throw new InvalidOperationException("Cannot delete a matched transaction.");
        }

        transaction.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Reconciliation Session

    public async Task<ReconciliationSession> StartReconciliationSessionAsync(
        int bankAccountId,
        DateTime periodStartDate,
        DateTime periodEndDate,
        decimal statementBalance,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(bankAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        // Check for existing active session
        var existingSession = await GetActiveReconciliationSessionAsync(bankAccountId, cancellationToken).ConfigureAwait(false);
        if (existingSession != null)
        {
            throw new InvalidOperationException("An active reconciliation session already exists for this account.");
        }

        var sessionNumber = $"REC-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var session = new ReconciliationSession
        {
            BankAccountId = bankAccountId,
            SessionNumber = sessionNumber,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            StatementBalance = statementBalance,
            BookBalance = account.CurrentBalance,
            Difference = statementBalance - account.CurrentBalance,
            Status = ReconciliationSessionStatus.InProgress,
            StartedByUserId = userId,
            IsActive = true
        };

        _context.ReconciliationSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }

    public async Task<ReconciliationSession?> GetReconciliationSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ReconciliationSessions
            .Include(s => s.BankAccount)
            .Include(s => s.Matches)
            .Include(s => s.Discrepancies)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ReconciliationSession>> GetReconciliationSessionsAsync(
        int bankAccountId,
        ReconciliationSessionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReconciliationSessions
            .Where(s => s.BankAccountId == bankAccountId && s.IsActive);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ReconciliationSession?> GetActiveReconciliationSessionAsync(int bankAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.BankAccountId == bankAccountId &&
                                      s.Status == ReconciliationSessionStatus.InProgress &&
                                      s.IsActive,
                                 cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CompleteReconciliationSessionAsync(int sessionId, int userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        // Update session balances first
        await UpdateSessionBalancesAsync(sessionId, cancellationToken).ConfigureAwait(false);

        session.Status = ReconciliationSessionStatus.Completed;
        session.CompletedByUserId = userId;
        session.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(notes))
            session.Notes = notes;

        // Update bank account last reconciliation
        var account = session.BankAccount;
        account.LastReconciliationDate = session.PeriodEndDate;
        account.LastReconciledBalance = session.StatementBalance;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RejectReconciliationSessionAsync(int sessionId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        session.Status = ReconciliationSessionStatus.Rejected;
        session.CompletedByUserId = userId;
        session.CompletedAt = DateTime.UtcNow;
        session.Notes = $"Rejected: {reason}";

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateSessionBalancesAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        var matches = await GetMatchesAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var discrepancies = await GetDiscrepanciesAsync(sessionId, null, cancellationToken).ConfigureAwait(false);

        session.MatchedCount = matches.Count();
        session.DiscrepancyCount = discrepancies.Count();

        // Count unmatched
        var unmatchedBank = await _context.BankTransactions
            .CountAsync(t => t.BankAccountId == session.BankAccountId &&
                            t.TransactionDate >= session.PeriodStartDate &&
                            t.TransactionDate <= session.PeriodEndDate &&
                            t.MatchStatus == ReconciliationMatchStatus.Unmatched &&
                            t.IsActive,
                        cancellationToken).ConfigureAwait(false);

        session.UnmatchedCount = unmatchedBank;
        session.Difference = session.StatementBalance - session.BookBalance;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Matching

    public async Task<AutoMatchResult> RunAutoMatchAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        var result = new AutoMatchResult { SessionId = sessionId };

        // Get unmatched bank transactions
        var bankTransactions = await _context.BankTransactions
            .Where(t => t.BankAccountId == session.BankAccountId &&
                       t.TransactionDate >= session.PeriodStartDate &&
                       t.TransactionDate <= session.PeriodEndDate &&
                       t.MatchStatus == ReconciliationMatchStatus.Unmatched &&
                       t.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        result.TotalBankTransactions = bankTransactions.Count;

        // Get POS payments for the period
        var payments = await _context.Payments
            .Where(p => p.PaymentDate >= session.PeriodStartDate &&
                       p.PaymentDate <= session.PeriodEndDate &&
                       p.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        result.TotalPOSTransactions = payments.Count;

        // Get matching rules
        var rules = await GetMatchingRulesAsync(session.BankAccountId, cancellationToken).ConfigureAwait(false);
        var activeRules = rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority).ToList();

        foreach (var bankTxn in bankTransactions)
        {
            var bestMatch = await FindBestMatchAsync(bankTxn, payments, activeRules, cancellationToken).ConfigureAwait(false);

            if (bestMatch != null && bestMatch.ConfidenceScore >= (activeRules.FirstOrDefault()?.MinimumConfidence ?? 80))
            {
                var match = new ReconciliationMatch
                {
                    ReconciliationSessionId = sessionId,
                    BankTransactionId = bankTxn.Id,
                    PaymentId = bestMatch.PaymentId,
                    ReceiptId = bestMatch.ReceiptId,
                    BankAmount = bankTxn.Amount,
                    POSAmount = bestMatch.POSAmount,
                    AmountDifference = bankTxn.Amount - bestMatch.POSAmount,
                    MatchType = ReconciliationMatchStatus.AutoMatched,
                    MatchConfidence = bestMatch.ConfidenceScore,
                    MatchingRule = bestMatch.MatchingRule,
                    IsActive = true
                };

                _context.ReconciliationMatches.Add(match);
                bankTxn.MatchStatus = ReconciliationMatchStatus.AutoMatched;
                bankTxn.MatchedPaymentId = bestMatch.PaymentId;
                bankTxn.ReconciliationSessionId = sessionId;

                result.Matches.Add(match);
                result.AutoMatchedCount++;
                result.TotalMatchedAmount += Math.Abs(bankTxn.Amount);

                // Remove matched payment from available pool
                var matchedPayment = payments.FirstOrDefault(p => p.Id == bestMatch.PaymentId);
                if (matchedPayment != null)
                    payments.Remove(matchedPayment);
            }
            else
            {
                result.UnmatchedBankCount++;
                result.TotalUnmatchedBankAmount += Math.Abs(bankTxn.Amount);
            }
        }

        result.UnmatchedPOSCount = payments.Count;
        result.TotalUnmatchedPOSAmount = payments.Sum(p => p.Amount);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSessionBalancesAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task<IEnumerable<MatchSuggestion>> GetMatchSuggestionsAsync(int bankTransactionId, CancellationToken cancellationToken = default)
    {
        var bankTxn = await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.Id == bankTransactionId && t.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank transaction {bankTransactionId} not found.");

        var suggestions = new List<MatchSuggestion>();

        // Get payments within date range
        var dateRange = 7;
        var minDate = bankTxn.TransactionDate.AddDays(-dateRange);
        var maxDate = bankTxn.TransactionDate.AddDays(dateRange);

        var payments = await _context.Payments
            .Where(p => p.PaymentDate >= minDate &&
                       p.PaymentDate <= maxDate &&
                       p.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var payment in payments)
        {
            var confidence = CalculateMatchConfidence(bankTxn, payment);
            if (confidence >= 30) // Show suggestions with at least 30% confidence
            {
                suggestions.Add(new MatchSuggestion
                {
                    BankTransactionId = bankTransactionId,
                    PaymentId = payment.Id,
                    ReceiptId = payment.ReceiptId,
                    Reference = payment.ReferenceNumber,
                    TransactionDate = payment.PaymentDate,
                    BankAmount = bankTxn.Amount,
                    POSAmount = payment.Amount,
                    AmountDifference = bankTxn.Amount - payment.Amount,
                    ConfidenceScore = confidence,
                    MatchingRule = "Manual Suggestion",
                    Reason = GetMatchReason(bankTxn, payment, confidence)
                });
            }
        }

        return suggestions.OrderByDescending(s => s.ConfidenceScore).Take(10);
    }

    public async Task<ReconciliationMatch> CreateManualMatchAsync(
        int sessionId,
        int bankTransactionId,
        int? paymentId,
        int? receiptId,
        int userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        var bankTxn = await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.Id == bankTransactionId && t.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank transaction {bankTransactionId} not found.");

        decimal posAmount = 0;
        if (paymentId.HasValue)
        {
            var payment = await _context.Payments.FindAsync(new object[] { paymentId.Value }, cancellationToken).ConfigureAwait(false);
            posAmount = payment?.Amount ?? 0;
        }

        var match = new ReconciliationMatch
        {
            ReconciliationSessionId = sessionId,
            BankTransactionId = bankTransactionId,
            PaymentId = paymentId,
            ReceiptId = receiptId,
            BankAmount = bankTxn.Amount,
            POSAmount = posAmount,
            AmountDifference = bankTxn.Amount - posAmount,
            MatchType = ReconciliationMatchStatus.ManuallyMatched,
            MatchedByUserId = userId,
            Notes = notes,
            IsActive = true
        };

        _context.ReconciliationMatches.Add(match);
        bankTxn.MatchStatus = ReconciliationMatchStatus.ManuallyMatched;
        bankTxn.MatchedPaymentId = paymentId;
        bankTxn.ReconciliationSessionId = sessionId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSessionBalancesAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return match;
    }

    public async Task UnmatchTransactionAsync(int matchId, int userId, CancellationToken cancellationToken = default)
    {
        var match = await _context.ReconciliationMatches
            .Include(m => m.BankTransaction)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Match {matchId} not found.");

        match.IsActive = false;
        match.BankTransaction.MatchStatus = ReconciliationMatchStatus.Unmatched;
        match.BankTransaction.MatchedPaymentId = null;
        match.BankTransaction.ReconciliationSessionId = null;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSessionBalancesAsync(match.ReconciliationSessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ReconciliationMatch>> GetMatchesAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ReconciliationMatches
            .Include(m => m.BankTransaction)
            .Include(m => m.Payment)
            .Where(m => m.ReconciliationSessionId == sessionId && m.IsActive)
            .OrderBy(m => m.BankTransaction.TransactionDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ExcludeTransactionAsync(int bankTransactionId, string reason, int userId, CancellationToken cancellationToken = default)
    {
        var bankTxn = await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.Id == bankTransactionId && t.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank transaction {bankTransactionId} not found.");

        bankTxn.MatchStatus = ReconciliationMatchStatus.Excluded;
        bankTxn.Notes = $"Excluded: {reason}";

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Discrepancy Handling

    public async Task<ReconciliationDiscrepancy> CreateDiscrepancyAsync(
        int sessionId,
        DiscrepancyType type,
        int? bankTransactionId,
        int? paymentId,
        decimal differenceAmount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var discrepancyNumber = $"DSC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var discrepancy = new ReconciliationDiscrepancy
        {
            ReconciliationSessionId = sessionId,
            DiscrepancyNumber = discrepancyNumber,
            DiscrepancyType = type,
            BankTransactionId = bankTransactionId,
            PaymentId = paymentId,
            DifferenceAmount = differenceAmount,
            Description = description,
            ResolutionStatus = DiscrepancyResolutionStatus.Open,
            IsActive = true
        };

        if (bankTransactionId.HasValue)
        {
            var bankTxn = await _context.BankTransactions.FindAsync(new object[] { bankTransactionId.Value }, cancellationToken).ConfigureAwait(false);
            discrepancy.BankAmount = bankTxn?.Amount;
        }

        if (paymentId.HasValue)
        {
            var payment = await _context.Payments.FindAsync(new object[] { paymentId.Value }, cancellationToken).ConfigureAwait(false);
            discrepancy.POSAmount = payment?.Amount;
        }

        _context.ReconciliationDiscrepancies.Add(discrepancy);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSessionBalancesAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return discrepancy;
    }

    public async Task<IEnumerable<ReconciliationDiscrepancy>> GetDiscrepanciesAsync(
        int sessionId,
        DiscrepancyResolutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReconciliationDiscrepancies
            .Include(d => d.BankTransaction)
            .Include(d => d.Payment)
            .Where(d => d.ReconciliationSessionId == sessionId && d.IsActive);

        if (status.HasValue)
            query = query.Where(d => d.ResolutionStatus == status.Value);

        return await query
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ResolveDiscrepancyAsync(
        int discrepancyId,
        string resolutionAction,
        int userId,
        int? adjustmentJournalEntryId = null,
        CancellationToken cancellationToken = default)
    {
        var discrepancy = await _context.ReconciliationDiscrepancies
            .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Discrepancy {discrepancyId} not found.");

        discrepancy.ResolutionStatus = DiscrepancyResolutionStatus.Resolved;
        discrepancy.ResolutionAction = resolutionAction;
        discrepancy.ResolvedByUserId = userId;
        discrepancy.ResolvedAt = DateTime.UtcNow;
        discrepancy.AdjustmentJournalEntryId = adjustmentJournalEntryId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteOffDiscrepancyAsync(
        int discrepancyId,
        int userId,
        int journalEntryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var discrepancy = await _context.ReconciliationDiscrepancies
            .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Discrepancy {discrepancyId} not found.");

        discrepancy.ResolutionStatus = DiscrepancyResolutionStatus.WrittenOff;
        discrepancy.ResolutionAction = $"Written off: {reason}";
        discrepancy.ResolvedByUserId = userId;
        discrepancy.ResolvedAt = DateTime.UtcNow;
        discrepancy.AdjustmentJournalEntryId = journalEntryId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task EscalateDiscrepancyAsync(int discrepancyId, string reason, int userId, CancellationToken cancellationToken = default)
    {
        var discrepancy = await _context.ReconciliationDiscrepancies
            .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Discrepancy {discrepancyId} not found.");

        discrepancy.ResolutionStatus = DiscrepancyResolutionStatus.Escalated;
        discrepancy.Notes = $"Escalated by user {userId}: {reason}";

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Matching Rules

    public async Task<IEnumerable<ReconciliationMatchingRule>> GetMatchingRulesAsync(int? bankAccountId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReconciliationMatchingRules.Where(r => r.IsActive);

        if (bankAccountId.HasValue)
            query = query.Where(r => r.BankAccountId == null || r.BankAccountId == bankAccountId);

        return await query
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ReconciliationMatchingRule> CreateMatchingRuleAsync(ReconciliationMatchingRule rule, CancellationToken cancellationToken = default)
    {
        _context.ReconciliationMatchingRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rule;
    }

    public async Task<ReconciliationMatchingRule> UpdateMatchingRuleAsync(ReconciliationMatchingRule rule, CancellationToken cancellationToken = default)
    {
        _context.ReconciliationMatchingRules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rule;
    }

    public async Task DeleteMatchingRuleAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.ReconciliationMatchingRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (rule != null)
        {
            rule.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Reports

    public async Task<ReconciliationSummaryReport> GetReconciliationSummaryAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetReconciliationSessionAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        var bankTransactions = await GetBankTransactionsAsync(
            session.BankAccountId,
            session.PeriodStartDate,
            session.PeriodEndDate,
            null,
            cancellationToken).ConfigureAwait(false);

        var report = new ReconciliationSummaryReport
        {
            SessionId = sessionId,
            SessionNumber = session.SessionNumber,
            PeriodStartDate = session.PeriodStartDate,
            PeriodEndDate = session.PeriodEndDate,
            StatementClosingBalance = session.StatementBalance,
            BookClosingBalance = session.BookBalance,
            TotalBankTransactions = bankTransactions.Count(),
            TotalBankDeposits = bankTransactions.Where(t => t.DepositAmount > 0).Sum(t => t.DepositAmount ?? 0),
            TotalBankWithdrawals = bankTransactions.Where(t => t.WithdrawalAmount > 0).Sum(t => t.WithdrawalAmount ?? 0),
            MatchedCount = session.MatchedCount,
            UnmatchedBankCount = session.UnmatchedCount,
            DiscrepancyCount = session.DiscrepancyCount,
            ReconciledDifference = session.Difference,
            IsBalanced = Math.Abs(session.Difference) < 0.01m
        };

        return report;
    }

    public async Task<OutstandingItemsReport> GetOutstandingItemsReportAsync(int bankAccountId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(bankAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        var unmatchedBank = await _context.BankTransactions
            .Where(t => t.BankAccountId == bankAccountId &&
                       t.TransactionDate <= asOfDate &&
                       t.MatchStatus == ReconciliationMatchStatus.Unmatched &&
                       t.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var report = new OutstandingItemsReport
        {
            BankAccountId = bankAccountId,
            AsOfDate = asOfDate
        };

        foreach (var txn in unmatchedBank)
        {
            var item = new OutstandingItem
            {
                BankTransactionId = txn.Id,
                Reference = txn.BankReference,
                TransactionDate = txn.TransactionDate,
                Amount = txn.Amount,
                Description = txn.Description,
                Source = "Bank",
                AgeDays = (asOfDate - txn.TransactionDate).Days
            };

            if (txn.DepositAmount > 0)
            {
                report.DepositsInTransit.Add(item);
                report.TotalOutstandingDeposits += txn.DepositAmount ?? 0;
            }
            else if (txn.WithdrawalAmount > 0)
            {
                if (!string.IsNullOrEmpty(txn.ChequeNumber))
                    report.UnpresentedCheques.Add(item);
                else
                    report.UnmatchedBankItems.Add(item);
                report.TotalOutstandingWithdrawals += txn.WithdrawalAmount ?? 0;
            }
        }

        report.NetOutstanding = report.TotalOutstandingDeposits - report.TotalOutstandingWithdrawals;

        return report;
    }

    public async Task<BalanceComparisonReport> GetBalanceComparisonAsync(int bankAccountId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var account = await GetBankAccountByIdAsync(bankAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        var outstandingReport = await GetOutstandingItemsReportAsync(bankAccountId, asOfDate, cancellationToken).ConfigureAwait(false);

        var report = new BalanceComparisonReport
        {
            BankAccountId = bankAccountId,
            AccountName = account.AccountName,
            AsOfDate = asOfDate,
            BankStatementBalance = account.LastReconciledBalance ?? 0,
            BookBalance = account.CurrentBalance,
            AddDepositsInTransit = outstandingReport.TotalOutstandingDeposits,
            LessUnpresentedCheques = outstandingReport.TotalOutstandingWithdrawals,
            LastReconciliationDate = account.LastReconciliationDate
        };

        report.Difference = report.BankStatementBalance - report.BookBalance;
        report.AdjustedBankBalance = report.BankStatementBalance + report.AddDepositsInTransit - report.LessUnpresentedCheques;
        report.AdjustedBookBalance = report.BookBalance;
        report.IsReconciled = Math.Abs(report.AdjustedBankBalance - report.AdjustedBookBalance) < 0.01m;

        return report;
    }

    #endregion

    #region Private Methods

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == delimiter && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += ch;
            }
        }
        result.Add(current.Trim());

        return result.ToArray();
    }

    private static BankTransaction ParseCsvTransaction(string[] columns, CsvImportOptions options, int bankAccountId, string batchId, string fileName)
    {
        var transaction = new BankTransaction
        {
            BankAccountId = bankAccountId,
            ImportBatchId = batchId,
            SourceFileName = fileName,
            IsActive = true
        };

        // Parse date
        if (DateTime.TryParseExact(columns[options.DateColumnIndex], options.DateFormat,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
        {
            transaction.TransactionDate = date;
        }

        // Parse description
        transaction.Description = columns[options.DescriptionColumnIndex];
        transaction.BankReference = ExtractReference(transaction.Description) ?? $"REF-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Parse amounts
        if (options.AmountColumnIndex.HasValue)
        {
            if (decimal.TryParse(columns[options.AmountColumnIndex.Value].Replace(",", ""), out var amount))
            {
                if (options.NegativeForWithdrawals)
                {
                    if (amount >= 0)
                        transaction.DepositAmount = amount;
                    else
                        transaction.WithdrawalAmount = Math.Abs(amount);
                }
                else
                {
                    transaction.DepositAmount = amount;
                }
            }
        }
        else
        {
            if (options.DepositColumnIndex.HasValue &&
                decimal.TryParse(columns[options.DepositColumnIndex.Value].Replace(",", ""), out var deposit) &&
                deposit > 0)
            {
                transaction.DepositAmount = deposit;
            }

            if (options.WithdrawalColumnIndex.HasValue &&
                decimal.TryParse(columns[options.WithdrawalColumnIndex.Value].Replace(",", ""), out var withdrawal) &&
                withdrawal > 0)
            {
                transaction.WithdrawalAmount = withdrawal;
            }
        }

        // Parse balance
        if (options.BalanceColumnIndex.HasValue &&
            decimal.TryParse(columns[options.BalanceColumnIndex.Value].Replace(",", ""), out var balance))
        {
            transaction.RunningBalance = balance;
        }

        // Parse reference
        if (options.ReferenceColumnIndex.HasValue && !string.IsNullOrWhiteSpace(columns[options.ReferenceColumnIndex.Value]))
        {
            transaction.BankReference = columns[options.ReferenceColumnIndex.Value];
        }

        // Try to extract M-Pesa code
        transaction.MpesaCode = ExtractMpesaCode(transaction.Description);
        if (!string.IsNullOrEmpty(transaction.MpesaCode))
        {
            transaction.TransactionType = BankTransactionType.MpesaTransaction;
        }
        else if (transaction.DepositAmount > 0)
        {
            transaction.TransactionType = BankTransactionType.Deposit;
        }
        else
        {
            transaction.TransactionType = BankTransactionType.Withdrawal;
        }

        return transaction;
    }

    private static string? ExtractReference(string description)
    {
        // Try to extract reference number patterns from description
        var patterns = new[]
        {
            @"REF[:\s]*([A-Z0-9]+)",
            @"TRN[:\s]*([A-Z0-9]+)",
            @"CHQ[:\s]*(\d+)",
            @"[A-Z]{3}[A-Z0-9]{7,10}"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(description, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
        }

        return null;
    }

    private static string? ExtractMpesaCode(string description)
    {
        // M-Pesa codes are typically 10 alphanumeric characters starting with a letter
        var match = Regex.Match(description, @"\b([A-Z]{1,3}[A-Z0-9]{7,10})\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpper() : null;
    }

    private async Task<MatchSuggestion?> FindBestMatchAsync(
        BankTransaction bankTxn,
        List<Payment> payments,
        List<ReconciliationMatchingRule> rules,
        CancellationToken cancellationToken)
    {
        MatchSuggestion? bestMatch = null;
        var highestConfidence = 0;

        foreach (var payment in payments)
        {
            var confidence = CalculateMatchConfidence(bankTxn, payment);

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
                bestMatch = new MatchSuggestion
                {
                    BankTransactionId = bankTxn.Id,
                    PaymentId = payment.Id,
                    ReceiptId = payment.ReceiptId,
                    Reference = payment.ReferenceNumber,
                    TransactionDate = payment.PaymentDate,
                    BankAmount = bankTxn.Amount,
                    POSAmount = payment.Amount,
                    AmountDifference = bankTxn.Amount - payment.Amount,
                    ConfidenceScore = confidence,
                    MatchingRule = "AutoMatch"
                };
            }
        }

        return bestMatch;
    }

    private static int CalculateMatchConfidence(BankTransaction bankTxn, Payment payment)
    {
        var confidence = 0;

        // Amount match (40 points)
        var amountDiff = Math.Abs(Math.Abs(bankTxn.Amount) - payment.Amount);
        if (amountDiff == 0)
            confidence += 40;
        else if (amountDiff <= 1)
            confidence += 35;
        else if (amountDiff <= 10)
            confidence += 20;

        // Date match (30 points)
        var daysDiff = Math.Abs((bankTxn.TransactionDate.Date - payment.PaymentDate.Date).Days);
        if (daysDiff == 0)
            confidence += 30;
        else if (daysDiff <= 1)
            confidence += 25;
        else if (daysDiff <= 3)
            confidence += 15;
        else if (daysDiff <= 7)
            confidence += 5;

        // Reference match (30 points)
        if (!string.IsNullOrEmpty(bankTxn.BankReference) && !string.IsNullOrEmpty(payment.ReferenceNumber))
        {
            if (bankTxn.BankReference.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                confidence += 30;
            else if (bankTxn.BankReference.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                     payment.ReferenceNumber.Contains(bankTxn.BankReference, StringComparison.OrdinalIgnoreCase))
                confidence += 20;
        }

        // M-Pesa code match (bonus)
        if (!string.IsNullOrEmpty(bankTxn.MpesaCode) && !string.IsNullOrEmpty(payment.ReferenceNumber))
        {
            if (bankTxn.MpesaCode.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                confidence += 20;
        }

        return Math.Min(confidence, 100);
    }

    private static string GetMatchReason(BankTransaction bankTxn, Payment payment, int confidence)
    {
        var reasons = new List<string>();

        if (Math.Abs(bankTxn.Amount) == payment.Amount)
            reasons.Add("Exact amount match");
        else if (Math.Abs(Math.Abs(bankTxn.Amount) - payment.Amount) <= 10)
            reasons.Add("Close amount match");

        if (bankTxn.TransactionDate.Date == payment.PaymentDate.Date)
            reasons.Add("Same date");
        else if (Math.Abs((bankTxn.TransactionDate.Date - payment.PaymentDate.Date).Days) <= 3)
            reasons.Add("Close date");

        if (!string.IsNullOrEmpty(bankTxn.BankReference) && !string.IsNullOrEmpty(payment.ReferenceNumber) &&
            bankTxn.BankReference.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
            reasons.Add("Reference match");

        return string.Join("; ", reasons);
    }

    #endregion
}
