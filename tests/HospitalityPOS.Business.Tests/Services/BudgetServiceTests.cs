using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the BudgetService class.
/// </summary>
public class BudgetServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly IBudgetService _service;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _service = new BudgetService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Store
        _context.Stores.Add(new Store { Id = 1, Name = "Test Store", Code = "TST001" });

        // Users
        _context.Users.AddRange(
            new User { Id = 1, Username = "admin", PasswordHash = "hash", FullName = "Admin User" },
            new User { Id = 2, Username = "approver", PasswordHash = "hash", FullName = "Approver User" }
        );

        // Chart of Accounts
        _context.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = 1, AccountCode = "4000", Name = "Sales Revenue", AccountType = "Revenue" },
            new ChartOfAccount { Id = 2, AccountCode = "5000", Name = "Cost of Goods Sold", AccountType = "Expense" },
            new ChartOfAccount { Id = 3, AccountCode = "6000", Name = "Operating Expenses", AccountType = "Expense" },
            new ChartOfAccount { Id = 4, AccountCode = "6100", Name = "Rent Expense", AccountType = "Expense" }
        );

        // Departments
        _context.Departments.Add(new Department
        {
            Id = 1,
            StoreId = 1,
            Code = "SALES",
            Name = "Sales Department",
            IsProfitCenter = true,
            IsEnabled = true
        });

        // Expense Categories
        _context.ExpenseCategories.Add(new ExpenseCategory
        {
            Id = 1,
            Name = "Utilities",
            Description = "Utility expenses"
        });

        // Journal entries for actuals testing
        var journalEntry = new JournalEntry
        {
            Id = 1,
            EntryNumber = "JE001",
            ReferenceNumber = "REF001",
            EntryDate = DateTime.UtcNow.AddDays(-10),
            Description = "Monthly expenses",
            IsPosted = true
        };
        _context.JournalEntries.Add(journalEntry);

        _context.JournalEntryLines.AddRange(
            new JournalEntryLine { Id = 1, JournalEntryId = 1, AccountId = 3, DebitAmount = 1000, CreditAmount = 0, Description = "Operating expenses" },
            new JournalEntryLine { Id = 2, JournalEntryId = 1, AccountId = 1, DebitAmount = 0, CreditAmount = 5000, Description = "Sales revenue" }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Budget Management Tests

    [Fact]
    public async Task CreateBudgetAsync_ShouldCreateBudget()
    {
        // Arrange
        var request = new CreateBudgetRequest
        {
            StoreId = 1,
            Name = "2024 Annual Budget",
            Description = "Annual budget for 2024",
            FiscalYear = 2024,
            PeriodType = BudgetPeriodType.Monthly,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            CreatedByUserId = 1
        };

        // Act
        var result = await _service.CreateBudgetAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("2024 Annual Budget");
        result.Status.Should().Be(BudgetStatus.Draft);
        result.FiscalYear.Should().Be(2024);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_ShouldReturnBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Budget");
    }

    [Fact]
    public async Task GetBudgetsAsync_ShouldFilterByStoreId()
    {
        // Arrange
        await CreateTestBudgetAsync();

        // Act
        var result = await _service.GetBudgetsAsync(storeId: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBudgetsAsync_ShouldFilterByFiscalYear()
    {
        // Arrange
        await CreateTestBudgetAsync();

        // Act
        var result = await _service.GetBudgetsAsync(fiscalYear: 2024);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBudgetsAsync_ShouldFilterByStatus()
    {
        // Arrange
        await CreateTestBudgetAsync();

        // Act
        var result = await _service.GetBudgetsAsync(status: BudgetStatus.Draft);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateBudgetAsync_ShouldUpdateDraftBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        budget.Name = "Updated Budget Name";
        budget.Description = "Updated description";

        // Act
        var result = await _service.UpdateBudgetAsync(budget);

        // Assert
        result.Name.Should().Be("Updated Budget Name");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateBudgetAsync_ShouldThrowForApprovedBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);
        await _service.SubmitBudgetForApprovalAsync(budget.Id);
        await _service.ApproveBudgetAsync(budget.Id, 2);

        var updatedBudget = await _service.GetBudgetByIdAsync(budget.Id);
        updatedBudget!.Name = "Try to update";

        // Act & Assert
        var act = async () => await _service.UpdateBudgetAsync(updatedBudget);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft status*");
    }

    [Fact]
    public async Task SubmitBudgetForApprovalAsync_ShouldChangeStatus()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);

        // Act
        await _service.SubmitBudgetForApprovalAsync(budget.Id);

        // Assert
        var result = await _service.GetBudgetByIdAsync(budget.Id);
        result!.Status.Should().Be(BudgetStatus.PendingApproval);
    }

    [Fact]
    public async Task SubmitBudgetForApprovalAsync_ShouldThrowWithoutLines()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();

        // Act & Assert
        var act = async () => await _service.SubmitBudgetForApprovalAsync(budget.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without any budget lines*");
    }

    [Fact]
    public async Task ApproveBudgetAsync_ShouldApproveBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);
        await _service.SubmitBudgetForApprovalAsync(budget.Id);

        // Act
        await _service.ApproveBudgetAsync(budget.Id, 2);

        // Assert
        var result = await _service.GetBudgetByIdAsync(budget.Id);
        result!.Status.Should().Be(BudgetStatus.Approved);
        result.ApprovedByUserId.Should().Be(2);
        result.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RejectBudgetAsync_ShouldRejectBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);
        await _service.SubmitBudgetForApprovalAsync(budget.Id);

        // Act
        await _service.RejectBudgetAsync(budget.Id, "Needs revision");

        // Assert
        var result = await _service.GetBudgetByIdAsync(budget.Id);
        result!.Status.Should().Be(BudgetStatus.Draft);
        result.Notes.Should().Contain("Rejected: Needs revision");
    }

    [Fact]
    public async Task CloseBudgetAsync_ShouldCloseBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);
        await _service.SubmitBudgetForApprovalAsync(budget.Id);
        await _service.ApproveBudgetAsync(budget.Id, 2);

        // Act
        await _service.CloseBudgetAsync(budget.Id);

        // Assert
        var result = await _service.GetBudgetByIdAsync(budget.Id);
        result!.Status.Should().Be(BudgetStatus.Closed);
    }

    [Fact]
    public async Task CopyBudgetAsync_ShouldCopyBudget()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);

        // Act
        var result = await _service.CopyBudgetAsync(budget.Id, "2025 Budget", 2025);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("2025 Budget");
        result.FiscalYear.Should().Be(2025);
        result.Status.Should().Be(BudgetStatus.Draft);

        var lines = await _service.GetBudgetLinesAsync(result.Id);
        lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateBudgetFromPriorYearAsync_ShouldCreateWithAdjustment()
    {
        // Arrange
        var priorBudget = await CreateTestBudgetAsync(2023);
        await AddBudgetLineAsync(priorBudget.Id, amount: 10000);

        // Act
        var result = await _service.CreateBudgetFromPriorYearAsync(1, 2024, 10); // 10% increase

        // Assert
        result.Should().NotBeNull();
        result.FiscalYear.Should().Be(2024);
        result.IsBasedOnPriorYear.Should().BeTrue();
        result.PriorYearAdjustmentPercent.Should().Be(10);

        var lines = await _service.GetBudgetLinesAsync(result.Id);
        lines.Should().HaveCount(1);
        lines.First().Amount.Should().Be(11000); // 10000 + 10%
    }

    #endregion

    #region Budget Lines Tests

    [Fact]
    public async Task AddBudgetLineAsync_ShouldAddLine()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        var line = new BudgetLine
        {
            BudgetId = budget.Id,
            AccountId = 3,
            PeriodNumber = 1,
            Amount = 5000
        };

        // Act
        var result = await _service.AddBudgetLineAsync(line);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Amount.Should().Be(5000);
    }

    [Fact]
    public async Task GetBudgetLinesAsync_ShouldReturnLines()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id);

        // Act
        var result = await _service.GetBudgetLinesAsync(budget.Id);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateBudgetLineAsync_ShouldUpdateLine()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        var line = await AddBudgetLineAsync(budget.Id);
        line.Amount = 7500;

        // Act
        var result = await _service.UpdateBudgetLineAsync(line);

        // Assert
        result.Amount.Should().Be(7500);
    }

    [Fact]
    public async Task DeleteBudgetLineAsync_ShouldDeleteLine()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        var line = await AddBudgetLineAsync(budget.Id);

        // Act
        await _service.DeleteBudgetLineAsync(line.Id);

        // Assert
        var lines = await _service.GetBudgetLinesAsync(budget.Id);
        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task BulkUpdateBudgetLinesAsync_ShouldUpdateMultipleLines()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        var line1 = await AddBudgetLineAsync(budget.Id, 1, 1000);
        var line2 = await AddBudgetLineAsync(budget.Id, 2, 2000);

        line1.Amount = 1500;
        line2.Amount = 2500;

        // Act
        await _service.BulkUpdateBudgetLinesAsync(new[] { line1, line2 });

        // Assert
        var lines = await _service.GetBudgetLinesAsync(budget.Id);
        lines.Should().Contain(l => l.Amount == 1500);
        lines.Should().Contain(l => l.Amount == 2500);
    }

    #endregion

    #region Budget vs Actual Tests

    [Fact]
    public async Task GetBudgetVsActualSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id, accountId: 3, amount: 2000); // Operating Expenses budgeted at 2000

        // Act
        var result = await _service.GetBudgetVsActualSummaryAsync(budget.Id, DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.BudgetName.Should().Be("Test Budget");
        result.TotalBudgeted.Should().Be(2000);
        result.TotalActual.Should().Be(1000); // From seeded journal entry
        result.AccountSummaries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBudgetVarianceAlertsAsync_ShouldReturnAlerts()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id, accountId: 3, amount: 500); // Budget 500, Actual 1000 = 100% over

        // Act
        var result = await _service.GetBudgetVarianceAlertsAsync(budget.Id, varianceThresholdPercent: 10);

        // Assert
        result.Should().NotBeEmpty();
        var alert = result.First();
        alert.AlertLevel.Should().Be("Critical"); // 100% variance is critical
    }

    [Fact]
    public async Task GetBudgetUtilizationAsync_ShouldReturnUtilization()
    {
        // Arrange
        var budget = await CreateTestBudgetAsync();
        await AddBudgetLineAsync(budget.Id, accountId: 3, amount: 12000); // Annual budget

        // Act
        var result = await _service.GetBudgetUtilizationAsync(budget.Id);

        // Assert
        result.Should().NotBeEmpty();
        var utilization = result.First();
        utilization.AnnualBudget.Should().Be(12000);
        utilization.YTDActual.Should().Be(1000);
    }

    #endregion

    #region Recurring Expense Tests

    [Fact]
    public async Task CreateRecurringExpenseTemplateAsync_ShouldCreateTemplate()
    {
        // Arrange
        var template = new RecurringExpenseTemplate
        {
            StoreId = 1,
            Name = "Monthly Rent",
            ExpenseCategoryId = 1,
            AccountId = 4,
            Amount = 5000,
            Frequency = "Monthly",
            DayOfMonth = 1,
            AutoPost = true,
            IsEnabled = true,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.CreateRecurringExpenseTemplateAsync(template);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Monthly Rent");
        result.NextScheduledDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecurringExpenseTemplatesAsync_ShouldReturnTemplates()
    {
        // Arrange
        await CreateTestRecurringTemplateAsync();

        // Act
        var result = await _service.GetRecurringExpenseTemplatesAsync(storeId: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateRecurringExpenseTemplateAsync_ShouldUpdateTemplate()
    {
        // Arrange
        var template = await CreateTestRecurringTemplateAsync();
        template.Amount = 6000;
        template.Name = "Updated Rent";

        // Act
        var result = await _service.UpdateRecurringExpenseTemplateAsync(template);

        // Assert
        result.Amount.Should().Be(6000);
        result.Name.Should().Be("Updated Rent");
    }

    [Fact]
    public async Task DeleteRecurringExpenseTemplateAsync_ShouldSoftDelete()
    {
        // Arrange
        var template = await CreateTestRecurringTemplateAsync();

        // Act
        await _service.DeleteRecurringExpenseTemplateAsync(template.Id);

        // Assert
        var templates = await _service.GetRecurringExpenseTemplatesAsync(storeId: 1);
        templates.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRecurringExpensesAsync_ShouldProcessTemplates()
    {
        // Arrange
        var template = new RecurringExpenseTemplate
        {
            StoreId = 1,
            Name = "Due Expense",
            ExpenseCategoryId = 1,
            AccountId = 4,
            Amount = 1000,
            Frequency = "Monthly",
            DayOfMonth = 1,
            AutoPost = true,
            IsEnabled = true,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            NextScheduledDate = DateTime.UtcNow.AddDays(-1) // Due yesterday
        };

        _context.RecurringExpenseTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessDueRecurringExpensesAsync(DateTime.UtcNow);

        // Assert
        result.TotalProcessed.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.Details.Should().HaveCount(1);
        result.Details.First().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDueRecurringExpensesAsync_ShouldHandleVariableAmounts()
    {
        // Arrange
        var template = new RecurringExpenseTemplate
        {
            StoreId = 1,
            Name = "Variable Expense",
            ExpenseCategoryId = 1,
            AccountId = 4,
            IsVariableAmount = true,
            Frequency = "Monthly",
            DayOfMonth = 1,
            AutoPost = false,
            IsEnabled = true,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            NextScheduledDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.RecurringExpenseTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessDueRecurringExpensesAsync(DateTime.UtcNow);

        // Assert
        result.PendingConfirmationCount.Should().Be(1);
        result.Details.First().RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingRecurringExpenseEntriesAsync_ShouldReturnPendingEntries()
    {
        // Arrange
        var template = await CreateTestRecurringTemplateAsync();

        var entry = new RecurringExpenseEntry
        {
            TemplateId = template.Id,
            ScheduledDate = DateTime.UtcNow,
            Status = "Pending",
            Amount = 5000
        };
        _context.RecurringExpenseEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingRecurringExpenseEntriesAsync(storeId: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ConfirmRecurringExpenseAmountAsync_ShouldConfirmEntry()
    {
        // Arrange
        var template = await CreateTestRecurringTemplateAsync();

        var entry = new RecurringExpenseEntry
        {
            TemplateId = template.Id,
            ScheduledDate = DateTime.UtcNow,
            Status = "Pending"
        };
        _context.RecurringExpenseEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConfirmRecurringExpenseAmountAsync(entry.Id, 4500, userId: 1);

        // Assert
        result.Status.Should().Be("Generated");
        result.Amount.Should().Be(4500);
        result.ConfirmedByUserId.Should().Be(1);
    }

    [Fact]
    public async Task SkipRecurringExpenseEntryAsync_ShouldSkipEntry()
    {
        // Arrange
        var template = await CreateTestRecurringTemplateAsync();

        var entry = new RecurringExpenseEntry
        {
            TemplateId = template.Id,
            ScheduledDate = DateTime.UtcNow,
            Status = "Pending"
        };
        _context.RecurringExpenseEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        await _service.SkipRecurringExpenseEntryAsync(entry.Id, "Holiday period");

        // Assert
        var updated = await _context.RecurringExpenseEntries.FindAsync(entry.Id);
        updated!.Status.Should().Be("Skipped");
        updated.Notes.Should().Be("Holiday period");
    }

    #endregion

    #region Cost Center Tests

    [Fact]
    public async Task GetCostCenterExpenseSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var expense = new Expense
        {
            StoreId = 1,
            CategoryId = 1,
            AccountId = 3,
            DepartmentId = 1,
            ExpenseDate = DateTime.UtcNow,
            Amount = 500,
            Description = "Test expense",
            Status = "Approved"
        };
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCostCenterExpenseSummaryAsync(
            1,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        // Assert
        result.Should().NotBeEmpty();
        var summary = result.First();
        summary.DepartmentName.Should().Be("Sales Department");
        summary.TotalExpenses.Should().Be(500);
    }

    [Fact]
    public async Task AllocateExpenseToCostCentersAsync_ShouldAllocateExpense()
    {
        // Arrange
        var expense = new Expense
        {
            StoreId = 1,
            CategoryId = 1,
            AccountId = 3,
            ExpenseDate = DateTime.UtcNow,
            Amount = 1000,
            Description = "Shared expense",
            Status = "Approved"
        };
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        var allocations = new List<ExpenseCostCenterAllocation>
        {
            new() { DepartmentId = 1, Amount = 600, Percentage = 60 },
            new() { DepartmentId = 1, Amount = 400, Percentage = 40 } // Same dept for test simplicity
        };

        // Act
        await _service.AllocateExpenseToCostCentersAsync(expense.Id, allocations);

        // Assert
        var updated = await _context.Expenses.FindAsync(expense.Id);
        updated!.DepartmentId.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private async Task<Budget> CreateTestBudgetAsync(int fiscalYear = 2024)
    {
        var request = new CreateBudgetRequest
        {
            StoreId = 1,
            Name = "Test Budget",
            FiscalYear = fiscalYear,
            PeriodType = BudgetPeriodType.Monthly,
            StartDate = new DateTime(fiscalYear, 1, 1),
            EndDate = new DateTime(fiscalYear, 12, 31),
            CreatedByUserId = 1
        };

        return await _service.CreateBudgetAsync(request);
    }

    private async Task<BudgetLine> AddBudgetLineAsync(int budgetId, int periodNumber = 1, decimal amount = 5000, int accountId = 3)
    {
        var line = new BudgetLine
        {
            BudgetId = budgetId,
            AccountId = accountId,
            PeriodNumber = periodNumber,
            Amount = amount
        };

        return await _service.AddBudgetLineAsync(line);
    }

    private async Task<RecurringExpenseTemplate> CreateTestRecurringTemplateAsync()
    {
        var template = new RecurringExpenseTemplate
        {
            StoreId = 1,
            Name = "Monthly Rent",
            ExpenseCategoryId = 1,
            AccountId = 4,
            Amount = 5000,
            Frequency = "Monthly",
            DayOfMonth = 1,
            AutoPost = true,
            IsEnabled = true,
            StartDate = DateTime.UtcNow
        };

        return await _service.CreateRecurringExpenseTemplateAsync(template);
    }

    #endregion
}
