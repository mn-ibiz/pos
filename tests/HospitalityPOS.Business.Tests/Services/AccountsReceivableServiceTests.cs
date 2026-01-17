using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for AccountsReceivableService.
/// </summary>
public class AccountsReceivableServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly AccountsReceivableService _service;

    public AccountsReceivableServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        var logger = NullLogger<AccountsReceivableService>.Instance;
        _service = new AccountsReceivableService(_context, logger);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Account Management Tests

    [Fact]
    public async Task CreateAccountAsync_ShouldCreateAccountWithGeneratedNumber()
    {
        // Arrange
        var account = new CustomerCreditAccount
        {
            ContactName = "John Doe",
            BusinessName = "ABC Company",
            CreditLimit = 50000m,
            PaymentTermsDays = 30,
            Email = "john@abc.com",
            Phone = "+254722123456"
        };

        // Act
        var result = await _service.CreateAccountAsync(account);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.AccountNumber.Should().StartWith("AR-");
        result.Status.Should().Be(CreditAccountStatus.Active);
        result.CurrentBalance.Should().Be(0m);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        var result = await _service.GetAccountByIdAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.AccountNumber.Should().Be(account.AccountNumber);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetAccountByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountByNumberAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        var result = await _service.GetAccountByNumberAsync(account.AccountNumber);

        // Assert
        result.Should().NotBeNull();
        result!.AccountNumber.Should().Be(account.AccountNumber);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        await CreateTestAccount("Account 1");
        await CreateTestAccount("Account 2");
        await CreateTestAccount("Account 3");

        // Act
        var result = await _service.GetAllAccountsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ShouldFilterByStatus()
    {
        // Arrange
        var account1 = await CreateTestAccount("Active Account");
        var account2 = await CreateTestAccount("Suspended Account");
        account2.Status = CreditAccountStatus.Suspended;
        await _context.SaveChangesAsync();

        // Act
        var activeAccounts = await _service.GetAllAccountsAsync(CreditAccountStatus.Active);
        var suspendedAccounts = await _service.GetAllAccountsAsync(CreditAccountStatus.Suspended);

        // Assert
        activeAccounts.Should().HaveCount(1);
        suspendedAccounts.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateCreditLimitAsync_ShouldUpdateLimit()
    {
        // Arrange
        var account = await CreateTestAccount();
        var newLimit = 100000m;

        // Act
        await _service.UpdateCreditLimitAsync(account.Id, newLimit, 1);

        // Assert
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CreditLimit.Should().Be(newLimit);
    }

    [Fact]
    public async Task SuspendAccountAsync_ShouldSetStatusToSuspended()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        await _service.SuspendAccountAsync(account.Id, "Non-payment", 1);

        // Assert
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.Status.Should().Be(CreditAccountStatus.Suspended);
    }

    [Fact]
    public async Task ReactivateAccountAsync_ShouldSetStatusToActive()
    {
        // Arrange
        var account = await CreateTestAccount();
        await _service.SuspendAccountAsync(account.Id, "Non-payment", 1);

        // Act
        await _service.ReactivateAccountAsync(account.Id, 1);

        // Assert
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.Status.Should().Be(CreditAccountStatus.Active);
    }

    #endregion

    #region Credit Purchase Check Tests

    [Fact]
    public async Task CanMakeCreditPurchaseAsync_ShouldAllowPurchase_WhenWithinLimit()
    {
        // Arrange
        var account = await CreateTestAccount(creditLimit: 50000m);

        // Act
        var result = await _service.CanMakeCreditPurchaseAsync(account.Id, 10000m);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.AvailableCredit.Should().Be(50000m);
        result.RequestedAmount.Should().Be(10000m);
    }

    [Fact]
    public async Task CanMakeCreditPurchaseAsync_ShouldDenyPurchase_WhenExceedsLimit()
    {
        // Arrange
        var account = await CreateTestAccount(creditLimit: 50000m);
        account.CurrentBalance = 45000m;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanMakeCreditPurchaseAsync(account.Id, 10000m);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Contain("credit limit");
        result.AvailableCredit.Should().Be(5000m);
    }

    [Fact]
    public async Task CanMakeCreditPurchaseAsync_ShouldDenyPurchase_WhenAccountSuspended()
    {
        // Arrange
        var account = await CreateTestAccount();
        await _service.SuspendAccountAsync(account.Id, "Test", 1);

        // Act
        var result = await _service.CanMakeCreditPurchaseAsync(account.Id, 1000m);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Contain("not active");
    }

    [Fact]
    public async Task CanMakeCreditPurchaseAsync_ShouldReturnNotAllowed_WhenAccountNotFound()
    {
        // Act
        var result = await _service.CanMakeCreditPurchaseAsync(999, 1000m);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Contain("not found");
    }

    #endregion

    #region Credit Transaction Tests

    [Fact]
    public async Task RecordCreditSaleAsync_ShouldCreateTransaction()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();

        // Act
        var transaction = await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        // Assert
        transaction.Should().NotBeNull();
        transaction.TransactionType.Should().Be(CreditTransactionType.Sale);
        transaction.Amount.Should().Be(5000m);
        transaction.ReceiptId.Should().Be(receipt.Id);
        transaction.DueDate.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordCreditSaleAsync_ShouldUpdateAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();

        // Act
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        // Assert
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CurrentBalance.Should().Be(5000m);
        updated.LastTransactionDate.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordCreditNoteAsync_ShouldDecreaseBalance()
    {
        // Arrange
        var account = await CreateTestAccount();
        account.CurrentBalance = 10000m;
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _service.RecordCreditNoteAsync(account.Id, 2000m, "Return", 1);

        // Assert
        transaction.TransactionType.Should().Be(CreditTransactionType.CreditNote);
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CurrentBalance.Should().Be(8000m);
    }

    [Fact]
    public async Task RecordAdjustmentAsync_PositiveAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var account = await CreateTestAccount();
        account.CurrentBalance = 5000m;
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _service.RecordAdjustmentAsync(account.Id, 1000m, "Late fee", 1);

        // Assert
        transaction.TransactionType.Should().Be(CreditTransactionType.DebitAdjustment);
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CurrentBalance.Should().Be(6000m);
    }

    [Fact]
    public async Task RecordAdjustmentAsync_NegativeAmount_ShouldDecreaseBalance()
    {
        // Arrange
        var account = await CreateTestAccount();
        account.CurrentBalance = 5000m;
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _service.RecordAdjustmentAsync(account.Id, -1000m, "Goodwill", 1);

        // Assert
        transaction.TransactionType.Should().Be(CreditTransactionType.CreditAdjustment);
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CurrentBalance.Should().Be(4000m);
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldReturnTransactionsForAccount()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);
        await _service.RecordCreditNoteAsync(account.Id, 1000m, "Return", 1);

        // Act
        var transactions = await _service.GetTransactionsAsync(account.Id);

        // Assert
        transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOutstandingTransactionsAsync_ShouldReturnUnpaidSales()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt1 = await CreateTestReceipt();
        var receipt2 = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt1.Id, 5000m, 1);
        await _service.RecordCreditSaleAsync(account.Id, receipt2.Id, 3000m, 1);

        // Act
        var outstanding = await _service.GetOutstandingTransactionsAsync(account.Id);

        // Assert
        outstanding.Should().HaveCount(2);
        outstanding.Sum(t => t.RemainingBalance).Should().Be(8000m);
    }

    #endregion

    #region Payment Tests

    [Fact]
    public async Task RecordPaymentAsync_ShouldCreatePayment()
    {
        // Arrange
        var account = await CreateTestAccount();
        var paymentMethod = await CreateTestPaymentMethod();
        var payment = new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 5000m,
            PaymentMethodId = paymentMethod.Id,
            ExternalReference = "CHQ-001"
        };

        // Act
        var result = await _service.RecordPaymentAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.PaymentNumber.Should().StartWith("PMT-");
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldUpdateAccountBalance()
    {
        // Arrange
        var account = await CreateTestAccount();
        account.CurrentBalance = 10000m;
        await _context.SaveChangesAsync();

        var paymentMethod = await CreateTestPaymentMethod();
        var payment = new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 5000m,
            PaymentMethodId = paymentMethod.Id
        };

        // Act
        await _service.RecordPaymentAsync(payment);

        // Assert
        var updated = await _service.GetAccountByIdAsync(account.Id);
        updated!.CurrentBalance.Should().Be(5000m);
        updated.LastPaymentDate.Should().NotBeNull();
    }

    [Fact]
    public async Task AllocatePaymentAsync_ShouldAllocateToTransactions()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        var sale = await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        var paymentMethod = await CreateTestPaymentMethod();
        var payment = new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 3000m,
            PaymentMethodId = paymentMethod.Id
        };
        var createdPayment = await _service.RecordPaymentAsync(payment);

        var allocations = new List<(int, decimal)> { (sale.Id, 3000m) };

        // Act
        await _service.AllocatePaymentAsync(createdPayment.Id, allocations);

        // Assert
        var updatedSale = await _context.CreditTransactions.FindAsync(sale.Id);
        updatedSale!.AmountPaid.Should().Be(3000m);
        updatedSale.RemainingBalance.Should().Be(2000m);

        var updatedPayment = await _context.CustomerPayments.FindAsync(createdPayment.Id);
        updatedPayment!.AllocatedAmount.Should().Be(3000m);
    }

    [Fact]
    public async Task AutoAllocatePaymentAsync_ShouldAllocateFIFO()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt1 = await CreateTestReceipt();
        var receipt2 = await CreateTestReceipt();

        // Create two sales - oldest first
        var sale1 = await _service.RecordCreditSaleAsync(account.Id, receipt1.Id, 3000m, 1);
        sale1.TransactionDate = DateTime.UtcNow.AddDays(-10);
        await _context.SaveChangesAsync();

        var sale2 = await _service.RecordCreditSaleAsync(account.Id, receipt2.Id, 5000m, 1);

        var paymentMethod = await CreateTestPaymentMethod();
        var payment = new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 4000m,
            PaymentMethodId = paymentMethod.Id
        };
        var createdPayment = await _service.RecordPaymentAsync(payment);

        // Act
        var allocations = await _service.AutoAllocatePaymentAsync(createdPayment.Id);

        // Assert
        allocations.Should().HaveCount(2);

        var updatedSale1 = await _context.CreditTransactions.FindAsync(sale1.Id);
        updatedSale1!.AmountPaid.Should().Be(3000m);
        updatedSale1.IsFullyPaid.Should().BeTrue();

        var updatedSale2 = await _context.CreditTransactions.FindAsync(sale2.Id);
        updatedSale2!.AmountPaid.Should().Be(1000m);
    }

    [Fact]
    public async Task GetPaymentsAsync_ShouldReturnPaymentsForAccount()
    {
        // Arrange
        var account = await CreateTestAccount();
        var paymentMethod = await CreateTestPaymentMethod();

        await _service.RecordPaymentAsync(new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 3000m,
            PaymentMethodId = paymentMethod.Id
        });
        await _service.RecordPaymentAsync(new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 2000m,
            PaymentMethodId = paymentMethod.Id
        });

        // Act
        var payments = await _service.GetPaymentsAsync(account.Id);

        // Assert
        payments.Should().HaveCount(2);
        payments.Sum(p => p.Amount).Should().Be(5000m);
    }

    #endregion

    #region Aging Report Tests

    [Fact]
    public async Task GetAgingReportAsync_ShouldReturnAgingEntries()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        // Act
        var report = await _service.GetAgingReportAsync(DateTime.UtcNow);

        // Assert
        report.Should().HaveCount(1);
        var entry = report.First();
        entry.CreditAccountId.Should().Be(account.Id);
        entry.TotalBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task GetAgingSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var account1 = await CreateTestAccount("Account 1");
        var account2 = await CreateTestAccount("Account 2");
        var receipt1 = await CreateTestReceipt();
        var receipt2 = await CreateTestReceipt();

        await _service.RecordCreditSaleAsync(account1.Id, receipt1.Id, 5000m, 1);
        await _service.RecordCreditSaleAsync(account2.Id, receipt2.Id, 3000m, 1);

        // Act
        var summary = await _service.GetAgingSummaryAsync(DateTime.UtcNow);

        // Assert
        summary.TotalAccounts.Should().Be(2);
        summary.TotalOutstanding.Should().Be(8000m);
    }

    [Fact]
    public async Task GetAccountAgingDetailAsync_ShouldReturnDetail()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt1 = await CreateTestReceipt();
        var receipt2 = await CreateTestReceipt();

        await _service.RecordCreditSaleAsync(account.Id, receipt1.Id, 3000m, 1);
        await _service.RecordCreditSaleAsync(account.Id, receipt2.Id, 2000m, 1);

        // Act
        var detail = await _service.GetAccountAgingDetailAsync(account.Id, DateTime.UtcNow);

        // Assert
        detail.AccountId.Should().Be(account.Id);
        detail.TotalBalance.Should().Be(5000m);
        detail.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverdueAccountsAsync_ShouldReturnOverdueAccounts()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        var sale = await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        // Set the sale as overdue
        sale.DueDate = DateTime.UtcNow.AddDays(-35);
        await _context.SaveChangesAsync();

        // Update account status
        account.Status = CreditAccountStatus.Overdue;
        await _context.SaveChangesAsync();

        // Act
        var overdueAccounts = await _service.GetOverdueAccountsAsync(30);

        // Assert
        overdueAccounts.Should().HaveCount(1);
    }

    #endregion

    #region Statement Tests

    [Fact]
    public async Task GenerateStatementAsync_ShouldCreateStatement()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var statement = await _service.GenerateStatementAsync(account.Id, startDate, endDate);

        // Assert
        statement.Should().NotBeNull();
        statement.StatementNumber.Should().StartWith("STM-");
        statement.TotalCharges.Should().Be(5000m);
        statement.ClosingBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task GenerateStatementAsync_ShouldIncludePayments()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        var paymentMethod = await CreateTestPaymentMethod();
        await _service.RecordPaymentAsync(new CustomerPayment
        {
            CreditAccountId = account.Id,
            Amount = 2000m,
            PaymentMethodId = paymentMethod.Id
        });

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var statement = await _service.GenerateStatementAsync(account.Id, startDate, endDate);

        // Assert
        statement.TotalCharges.Should().Be(5000m);
        statement.TotalPayments.Should().Be(2000m);
        statement.ClosingBalance.Should().Be(3000m);
    }

    [Fact]
    public async Task GenerateAllStatementsAsync_ShouldCreateStatementsForActiveAccounts()
    {
        // Arrange
        var account1 = await CreateTestAccount("Account 1");
        var account2 = await CreateTestAccount("Account 2");
        var receipt1 = await CreateTestReceipt();
        var receipt2 = await CreateTestReceipt();

        await _service.RecordCreditSaleAsync(account1.Id, receipt1.Id, 5000m, 1);
        await _service.RecordCreditSaleAsync(account2.Id, receipt2.Id, 3000m, 1);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var statements = await _service.GenerateAllStatementsAsync(startDate, endDate);

        // Assert
        statements.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStatementsAsync_ShouldReturnStatementsForAccount()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);

        await _service.GenerateStatementAsync(account.Id, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-31));
        await _service.GenerateStatementAsync(account.Id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Act
        var statements = await _service.GetStatementsAsync(account.Id);

        // Assert
        statements.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkStatementSentAsync_ShouldUpdateStatement()
    {
        // Arrange
        var account = await CreateTestAccount();
        var receipt = await CreateTestReceipt();
        await _service.RecordCreditSaleAsync(account.Id, receipt.Id, 5000m, 1);
        var statement = await _service.GenerateStatementAsync(account.Id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Act
        await _service.MarkStatementSentAsync(statement.Id, "Email");

        // Assert
        var updated = await _context.CustomerStatements.FindAsync(statement.Id);
        updated!.SentAt.Should().NotBeNull();
        updated.SentVia.Should().Be("Email");
    }

    #endregion

    #region Helper Methods

    private async Task<CustomerCreditAccount> CreateTestAccount(string name = "Test Customer", decimal creditLimit = 50000m)
    {
        var account = new CustomerCreditAccount
        {
            AccountNumber = $"AR-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ContactName = name,
            BusinessName = name + " Ltd",
            CreditLimit = creditLimit,
            PaymentTermsDays = 30,
            Status = CreditAccountStatus.Active,
            IsActive = true
        };

        _context.CustomerCreditAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task<Receipt> CreateTestReceipt()
    {
        var receipt = new Receipt
        {
            ReceiptNumber = $"RCP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            SubTotal = 1000m,
            TaxAmount = 160m,
            TotalAmount = 1160m,
            ReceiptDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();
        return receipt;
    }

    private async Task<PaymentMethod> CreateTestPaymentMethod()
    {
        var paymentMethod = new PaymentMethod
        {
            Name = "Cheque",
            Code = "CHQ",
            IsActive = true
        };

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();
        return paymentMethod;
    }

    #endregion
}
