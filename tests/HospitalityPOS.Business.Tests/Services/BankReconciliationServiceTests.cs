using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for BankReconciliationService.
/// </summary>
public class BankReconciliationServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger<BankReconciliationService>> _loggerMock;
    private readonly BankReconciliationService _service;

    public BankReconciliationServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger<BankReconciliationService>>();
        _service = new BankReconciliationService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Bank Account Tests

    [Fact]
    public async Task CreateBankAccountAsync_ShouldCreateAccount()
    {
        // Arrange
        var account = new BankAccount
        {
            BankName = "Kenya Commercial Bank",
            AccountNumber = "1234567890",
            AccountName = "Test Company Ltd",
            AccountType = "Current",
            CurrencyCode = "KES",
            OpeningBalance = 100000m,
            IsActive = true
        };

        // Act
        var result = await _service.CreateBankAccountAsync(account);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CurrentBalance.Should().Be(100000m);
        result.Status.Should().Be(BankAccountStatus.Active);
    }

    [Fact]
    public async Task GetBankAccountByIdAsync_ShouldReturnAccount()
    {
        // Arrange
        var account = await CreateTestBankAccount();

        // Act
        var result = await _service.GetBankAccountByIdAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetAllBankAccountsAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        await CreateTestBankAccount("Account 1");
        await CreateTestBankAccount("Account 2");
        await CreateTestBankAccount("Account 3");

        // Act
        var result = await _service.GetAllBankAccountsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task CloseBankAccountAsync_ShouldSetStatusToClosed()
    {
        // Arrange
        var account = await CreateTestBankAccount();

        // Act
        await _service.CloseBankAccountAsync(account.Id, 1);

        // Assert
        var updated = await _service.GetBankAccountByIdAsync(account.Id);
        updated!.Status.Should().Be(BankAccountStatus.Closed);
    }

    [Fact]
    public async Task LinkMpesaAccountAsync_ShouldUpdateShortCode()
    {
        // Arrange
        var account = await CreateTestBankAccount();

        // Act
        await _service.LinkMpesaAccountAsync(account.Id, "123456");

        // Assert
        var updated = await _service.GetBankAccountByIdAsync(account.Id);
        updated!.MpesaShortCode.Should().Be("123456");
    }

    #endregion

    #region Bank Transaction Tests

    [Fact]
    public async Task AddBankTransactionAsync_ShouldCreateTransaction()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var transaction = new BankTransaction
        {
            BankAccountId = account.Id,
            TransactionType = BankTransactionType.Deposit,
            TransactionDate = DateTime.UtcNow,
            Description = "Customer payment",
            DepositAmount = 5000m,
            IsActive = true
        };

        // Act
        var result = await _service.AddBankTransactionAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.BankReference.Should().StartWith("MAN-");
    }

    [Fact]
    public async Task GetBankTransactionsAsync_ShouldReturnTransactions()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        await CreateTestBankTransaction(account.Id, 1000m);
        await CreateTestBankTransaction(account.Id, 2000m);

        // Act
        var result = await _service.GetBankTransactionsAsync(account.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBankTransactionsAsync_ShouldFilterByMatchStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var txn1 = await CreateTestBankTransaction(account.Id, 1000m);
        var txn2 = await CreateTestBankTransaction(account.Id, 2000m);
        txn2.MatchStatus = ReconciliationMatchStatus.AutoMatched;
        await _context.SaveChangesAsync();

        // Act
        var unmatched = await _service.GetBankTransactionsAsync(account.Id, matchStatus: ReconciliationMatchStatus.Unmatched);
        var matched = await _service.GetBankTransactionsAsync(account.Id, matchStatus: ReconciliationMatchStatus.AutoMatched);

        // Assert
        unmatched.Should().HaveCount(1);
        matched.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteBankTransactionAsync_ShouldSoftDelete()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var transaction = await CreateTestBankTransaction(account.Id, 1000m);

        // Act
        await _service.DeleteBankTransactionAsync(transaction.Id);

        // Assert
        var result = await _context.BankTransactions.FirstOrDefaultAsync(t => t.Id == transaction.Id);
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBankTransactionAsync_ShouldThrow_WhenMatched()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var transaction = await CreateTestBankTransaction(account.Id, 1000m);
        transaction.MatchStatus = ReconciliationMatchStatus.AutoMatched;
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteBankTransactionAsync(transaction.Id));
    }

    #endregion

    #region Reconciliation Session Tests

    [Fact]
    public async Task StartReconciliationSessionAsync_ShouldCreateSession()
    {
        // Arrange
        var account = await CreateTestBankAccount();

        // Act
        var result = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Assert
        result.Should().NotBeNull();
        result.SessionNumber.Should().StartWith("REC-");
        result.Status.Should().Be(ReconciliationSessionStatus.InProgress);
    }

    [Fact]
    public async Task StartReconciliationSessionAsync_ShouldThrow_WhenActiveSessionExists()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartReconciliationSessionAsync(
                account.Id,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-31),
                90000m,
                1));
    }

    [Fact]
    public async Task GetActiveReconciliationSessionAsync_ShouldReturnActiveSession()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Act
        var result = await _service.GetActiveReconciliationSessionAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task CompleteReconciliationSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Act
        await _service.CompleteReconciliationSessionAsync(session.Id, 1, "Completed successfully");

        // Assert
        var updated = await _service.GetReconciliationSessionAsync(session.Id);
        updated!.Status.Should().Be(ReconciliationSessionStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RejectReconciliationSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Act
        await _service.RejectReconciliationSessionAsync(session.Id, 1, "Discrepancies found");

        // Assert
        var updated = await _service.GetReconciliationSessionAsync(session.Id);
        updated!.Status.Should().Be(ReconciliationSessionStatus.Rejected);
    }

    #endregion

    #region Matching Tests

    [Fact]
    public async Task CreateManualMatchAsync_ShouldCreateMatch()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);
        var bankTxn = await CreateTestBankTransaction(account.Id, 5000m);
        var payment = await CreateTestPayment(5000m);

        // Act
        var match = await _service.CreateManualMatchAsync(session.Id, bankTxn.Id, payment.Id, null, 1, "Manual match");

        // Assert
        match.Should().NotBeNull();
        match.MatchType.Should().Be(ReconciliationMatchStatus.ManuallyMatched);
        match.BankAmount.Should().Be(5000m);

        var updatedTxn = await _context.BankTransactions.FindAsync(bankTxn.Id);
        updatedTxn!.MatchStatus.Should().Be(ReconciliationMatchStatus.ManuallyMatched);
    }

    [Fact]
    public async Task UnmatchTransactionAsync_ShouldResetMatchStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);
        var bankTxn = await CreateTestBankTransaction(account.Id, 5000m);
        var payment = await CreateTestPayment(5000m);
        var match = await _service.CreateManualMatchAsync(session.Id, bankTxn.Id, payment.Id, null, 1);

        // Act
        await _service.UnmatchTransactionAsync(match.Id, 1);

        // Assert
        var updatedTxn = await _context.BankTransactions.FindAsync(bankTxn.Id);
        updatedTxn!.MatchStatus.Should().Be(ReconciliationMatchStatus.Unmatched);
    }

    [Fact]
    public async Task GetMatchSuggestionsAsync_ShouldReturnSuggestions()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var bankTxn = await CreateTestBankTransaction(account.Id, 5000m);
        var payment = await CreateTestPayment(5000m);

        // Act
        var suggestions = await _service.GetMatchSuggestionsAsync(bankTxn.Id);

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.First().ConfidenceScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExcludeTransactionAsync_ShouldSetExcludedStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var bankTxn = await CreateTestBankTransaction(account.Id, 5000m);

        // Act
        await _service.ExcludeTransactionAsync(bankTxn.Id, "Not relevant", 1);

        // Assert
        var updated = await _context.BankTransactions.FindAsync(bankTxn.Id);
        updated!.MatchStatus.Should().Be(ReconciliationMatchStatus.Excluded);
    }

    #endregion

    #region Discrepancy Tests

    [Fact]
    public async Task CreateDiscrepancyAsync_ShouldCreateDiscrepancy()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);
        var bankTxn = await CreateTestBankTransaction(account.Id, 5000m);

        // Act
        var discrepancy = await _service.CreateDiscrepancyAsync(
            session.Id,
            DiscrepancyType.MissingFromPOS,
            bankTxn.Id,
            null,
            5000m,
            "Transaction not found in POS");

        // Assert
        discrepancy.Should().NotBeNull();
        discrepancy.DiscrepancyNumber.Should().StartWith("DSC-");
        discrepancy.ResolutionStatus.Should().Be(DiscrepancyResolutionStatus.Open);
    }

    [Fact]
    public async Task ResolveDiscrepancyAsync_ShouldUpdateStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);
        var discrepancy = await _service.CreateDiscrepancyAsync(
            session.Id,
            DiscrepancyType.AmountMismatch,
            null,
            null,
            100m,
            "Amount difference");

        // Act
        await _service.ResolveDiscrepancyAsync(discrepancy.Id, "Adjusted in books", 1);

        // Assert
        var updated = await _context.ReconciliationDiscrepancies.FindAsync(discrepancy.Id);
        updated!.ResolutionStatus.Should().Be(DiscrepancyResolutionStatus.Resolved);
    }

    [Fact]
    public async Task EscalateDiscrepancyAsync_ShouldUpdateStatus()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);
        var discrepancy = await _service.CreateDiscrepancyAsync(
            session.Id,
            DiscrepancyType.Other,
            null,
            null,
            10000m,
            "Large unexplained difference");

        // Act
        await _service.EscalateDiscrepancyAsync(discrepancy.Id, "Requires management review", 1);

        // Assert
        var updated = await _context.ReconciliationDiscrepancies.FindAsync(discrepancy.Id);
        updated!.ResolutionStatus.Should().Be(DiscrepancyResolutionStatus.Escalated);
    }

    #endregion

    #region Matching Rules Tests

    [Fact]
    public async Task CreateMatchingRuleAsync_ShouldCreateRule()
    {
        // Arrange
        var rule = new ReconciliationMatchingRule
        {
            Name = "Test Rule",
            Priority = 1,
            MatchOnAmount = true,
            MatchOnDate = true,
            DateToleranceDays = 3,
            MinimumConfidence = 80,
            IsActive = true
        };

        // Act
        var result = await _service.CreateMatchingRuleAsync(rule);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMatchingRulesAsync_ShouldReturnRules()
    {
        // Arrange
        await _service.CreateMatchingRuleAsync(new ReconciliationMatchingRule
        {
            Name = "Rule 1",
            Priority = 1,
            IsActive = true
        });
        await _service.CreateMatchingRuleAsync(new ReconciliationMatchingRule
        {
            Name = "Rule 2",
            Priority = 2,
            IsActive = true
        });

        // Act
        var result = await _service.GetMatchingRulesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Rule 1");
    }

    [Fact]
    public async Task DeleteMatchingRuleAsync_ShouldSoftDelete()
    {
        // Arrange
        var rule = await _service.CreateMatchingRuleAsync(new ReconciliationMatchingRule
        {
            Name = "To Delete",
            IsActive = true
        });

        // Act
        await _service.DeleteMatchingRuleAsync(rule.Id);

        // Assert
        var result = await _context.ReconciliationMatchingRules.FirstOrDefaultAsync(r => r.Id == rule.Id);
        result!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Report Tests

    [Fact]
    public async Task GetReconciliationSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        var session = await _service.StartReconciliationSessionAsync(
            account.Id,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            100000m,
            1);

        // Act
        var report = await _service.GetReconciliationSummaryAsync(session.Id);

        // Assert
        report.Should().NotBeNull();
        report.SessionId.Should().Be(session.Id);
        report.StatementClosingBalance.Should().Be(100000m);
    }

    [Fact]
    public async Task GetOutstandingItemsReportAsync_ShouldReturnReport()
    {
        // Arrange
        var account = await CreateTestBankAccount();
        await CreateTestBankTransaction(account.Id, 5000m);

        // Act
        var report = await _service.GetOutstandingItemsReportAsync(account.Id, DateTime.UtcNow);

        // Assert
        report.Should().NotBeNull();
        report.BankAccountId.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetBalanceComparisonAsync_ShouldReturnReport()
    {
        // Arrange
        var account = await CreateTestBankAccount();

        // Act
        var report = await _service.GetBalanceComparisonAsync(account.Id, DateTime.UtcNow);

        // Assert
        report.Should().NotBeNull();
        report.AccountName.Should().Be(account.AccountName);
        report.BookBalance.Should().Be(account.CurrentBalance);
    }

    #endregion

    #region Helper Methods

    private async Task<BankAccount> CreateTestBankAccount(string name = "Test Account")
    {
        var account = new BankAccount
        {
            BankName = "Kenya Commercial Bank",
            AccountNumber = $"ACC-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            AccountName = name,
            AccountType = "Current",
            CurrencyCode = "KES",
            OpeningBalance = 100000m,
            CurrentBalance = 100000m,
            Status = BankAccountStatus.Active,
            IsActive = true
        };

        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task<BankTransaction> CreateTestBankTransaction(int bankAccountId, decimal amount)
    {
        var transaction = new BankTransaction
        {
            BankAccountId = bankAccountId,
            TransactionType = BankTransactionType.Deposit,
            TransactionDate = DateTime.UtcNow,
            BankReference = $"REF-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Description = "Test transaction",
            DepositAmount = amount,
            MatchStatus = ReconciliationMatchStatus.Unmatched,
            IsActive = true
        };

        _context.BankTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    private async Task<Payment> CreateTestPayment(decimal amount)
    {
        var paymentMethod = await GetOrCreatePaymentMethod();

        var payment = new Payment
        {
            PaymentMethodId = paymentMethod.Id,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            ReferenceNumber = $"PMT-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            IsActive = true
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    private async Task<PaymentMethod> GetOrCreatePaymentMethod()
    {
        var existing = await _context.PaymentMethods.FirstOrDefaultAsync();
        if (existing != null) return existing;

        var paymentMethod = new PaymentMethod
        {
            Name = "M-Pesa",
            Code = "MPESA",
            IsActive = true
        };

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();
        return paymentMethod;
    }

    #endregion
}
