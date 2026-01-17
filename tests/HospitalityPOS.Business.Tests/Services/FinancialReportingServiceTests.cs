using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the FinancialReportingService class.
/// </summary>
public class FinancialReportingServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly IFinancialReportingService _service;

    public FinancialReportingServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _service = new FinancialReportingService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Store
        _context.Stores.Add(new Store { Id = 1, Name = "Test Store", Code = "TST001" });

        // Chart of Accounts
        _context.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = 1, AccountCode = "1000", Name = "Cash", AccountType = "Asset" },
            new ChartOfAccount { Id = 2, AccountCode = "4000", Name = "Sales Revenue", AccountType = "Revenue" },
            new ChartOfAccount { Id = 3, AccountCode = "5000", Name = "Cost of Goods Sold", AccountType = "Expense" },
            new ChartOfAccount { Id = 4, AccountCode = "6000", Name = "Operating Expenses", AccountType = "Expense" },
            new ChartOfAccount { Id = 5, AccountCode = "6100", Name = "Rent Expense", AccountType = "Expense" }
        );

        // Categories and Products
        _context.Categories.Add(new Category { Id = 1, Name = "Electronics", Code = "ELEC" });
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Widget A", Code = "WA001", CategoryId = 1, Price = 100, Cost = 60 },
            new Product { Id = 2, Name = "Widget B", Code = "WB001", CategoryId = 1, Price = 150, Cost = 100 }
        );

        // Receipts with items
        var receipt = new Receipt
        {
            Id = 1,
            ReceiptNumber = "R001",
            StoreId = 1,
            ReceiptDate = DateTime.UtcNow.AddDays(-5),
            TotalAmount = 250,
            IsPaid = true
        };
        _context.Receipts.Add(receipt);

        _context.ReceiptItems.AddRange(
            new ReceiptItem { Id = 1, ReceiptId = 1, ProductId = 1, Quantity = 1, UnitPrice = 100 },
            new ReceiptItem { Id = 2, ReceiptId = 1, ProductId = 2, Quantity = 1, UnitPrice = 150 }
        );

        // Journal Entries
        var journalEntry = new JournalEntry
        {
            Id = 1,
            EntryNumber = "JE001",
            ReferenceNumber = "REF001",
            EntryDate = DateTime.UtcNow.AddDays(-3),
            Description = "Sales transaction",
            IsPosted = true
        };
        _context.JournalEntries.Add(journalEntry);

        _context.JournalEntryLines.AddRange(
            new JournalEntryLine { Id = 1, JournalEntryId = 1, AccountId = 1, DebitAmount = 250, CreditAmount = 0, Description = "Cash received" },
            new JournalEntryLine { Id = 2, JournalEntryId = 1, AccountId = 2, DebitAmount = 0, CreditAmount = 250, Description = "Sales" }
        );

        // Department
        _context.Departments.Add(new Department
        {
            Id = 1,
            StoreId = 1,
            Code = "SALES",
            Name = "Sales Department",
            IsProfitCenter = true,
            IsEnabled = true,
            AllocatedCategoryIds = "1"
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Cash Flow Statement Tests

    [Fact]
    public async Task GenerateCashFlowStatementAsync_ShouldGenerateStatement()
    {
        // Arrange
        var request = new CashFlowStatementRequest
        {
            StoreId = 1,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateCashFlowStatementAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().Be(request.StartDate);
        result.EndDate.Should().Be(request.EndDate);
    }

    [Fact]
    public async Task SaveCashFlowMappingAsync_ShouldSaveMapping()
    {
        // Arrange
        var mapping = new CashFlowMapping
        {
            AccountId = 1,
            ActivityType = CashFlowActivityType.Operating,
            LineItem = "Cash from sales",
            IsInflow = true
        };

        // Act
        var result = await _service.SaveCashFlowMappingAsync(mapping);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.LineItem.Should().Be("Cash from sales");
    }

    [Fact]
    public async Task GetCashFlowMappingsAsync_ShouldReturnMappings()
    {
        // Arrange
        await _service.SaveCashFlowMappingAsync(new CashFlowMapping
        {
            AccountId = 1,
            ActivityType = CashFlowActivityType.Operating,
            LineItem = "Cash from sales",
            IsInflow = true
        });

        // Act
        var result = await _service.GetCashFlowMappingsAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region General Ledger Tests

    [Fact]
    public async Task GenerateGeneralLedgerReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var request = new GeneralLedgerReportRequest
        {
            AccountId = 1,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateGeneralLedgerReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(1);
        result.AccountCode.Should().Be("1000");
        result.AccountName.Should().Be("Cash");
    }

    [Fact]
    public async Task GetGLAccountActivityAsync_ShouldReturnActivity()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetGLAccountActivityAsync(1, startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Gross Margin Analysis Tests

    [Fact]
    public async Task GenerateGrossMarginReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var request = new GrossMarginReportRequest
        {
            StoreId = 1,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            GroupBy = "Category"
        };

        // Act
        var result = await _service.GenerateGrossMarginReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(250);
        result.TotalCOGS.Should().Be(160); // 60 + 100
        result.TotalGrossMargin.Should().Be(90);
    }

    [Fact]
    public async Task GetProductMarginsAsync_ShouldReturnProductMargins()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetProductMarginsAsync(1, null, startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        var widgetA = result.First(p => p.ProductId == 1);
        widgetA.Revenue.Should().Be(100);
        widgetA.COGS.Should().Be(60);
        widgetA.MarginPercent.Should().Be(40); // (100-60)/100 * 100
    }

    [Fact]
    public async Task GetCategoryMarginsAsync_ShouldReturnCategoryMargins()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetCategoryMarginsAsync(1, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        var electronics = result.First();
        electronics.CategoryName.Should().Be("Electronics");
        electronics.Revenue.Should().Be(250);
    }

    [Fact]
    public async Task SaveMarginThresholdAsync_ShouldSaveThreshold()
    {
        // Arrange
        var threshold = new MarginThreshold
        {
            StoreId = 1,
            CategoryId = 1,
            MinMarginPercent = 20,
            TargetMarginPercent = 30
        };

        // Act
        var result = await _service.SaveMarginThresholdAsync(threshold);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.MinMarginPercent.Should().Be(20);
    }

    #endregion

    #region Comparative Reports Tests

    [Fact]
    public async Task GenerateComparativePLReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var request = new ComparativePLRequest
        {
            StoreId = 1,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-30),
            CurrentPeriodEnd = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateComparativePLReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPeriod.Should().NotBeNull();
        result.CurrentPeriod.Revenue.Should().Be(250);
    }

    [Fact]
    public async Task GenerateYearOverYearReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var currentStart = DateTime.UtcNow.AddDays(-30);
        var currentEnd = DateTime.UtcNow;

        // Act
        var result = await _service.GenerateYearOverYearReportAsync(1, currentStart, currentEnd);

        // Assert
        result.Should().NotBeNull();
        result.CurrentYear.Should().NotBeNull();
        result.PriorYear.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateBudgetVsActualReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var budget = new Budget
        {
            Id = 1,
            StoreId = 1,
            Name = "2024 Budget",
            FiscalYear = 2024,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Status = BudgetStatus.Approved
        };
        _context.Budgets.Add(budget);

        _context.BudgetLines.Add(new BudgetLine
        {
            BudgetId = 1,
            AccountId = 2,
            PeriodNumber = 1,
            Amount = 5000
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateBudgetVsActualReportAsync(1, 1, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.BudgetId.Should().Be(1);
        result.BudgetName.Should().Be("2024 Budget");
    }

    #endregion

    #region Departmental P&L Tests

    [Fact]
    public async Task GenerateDepartmentalPLReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var request = new DepartmentalPLRequest
        {
            StoreId = 1,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            IncludeOverheadAllocation = false
        };

        // Act
        var result = await _service.GenerateDepartmentalPLReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.DepartmentResults.Should().HaveCount(1);
        var salesDept = result.DepartmentResults.First();
        salesDept.DepartmentName.Should().Be("Sales Department");
        salesDept.Revenue.Should().Be(250);
    }

    [Fact]
    public async Task GetDepartmentsAsync_ShouldReturnDepartments()
    {
        // Act
        var result = await _service.GetDepartmentsAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Sales Department");
    }

    [Fact]
    public async Task CreateDepartmentAsync_ShouldCreateDepartment()
    {
        // Arrange
        var department = new Department
        {
            StoreId = 1,
            Code = "MKTG",
            Name = "Marketing Department",
            IsProfitCenter = false,
            IsEnabled = true
        };

        // Act
        var result = await _service.CreateDepartmentAsync(department);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Code.Should().Be("MKTG");
    }

    [Fact]
    public async Task SaveOverheadAllocationRuleAsync_ShouldSaveRule()
    {
        // Arrange
        var rule = new OverheadAllocationRule
        {
            StoreId = 1,
            Name = "Rent Allocation",
            SourceAccountId = 5,
            AllocationBasis = "Revenue",
            IsEnabled = true
        };

        // Act
        var result = await _service.SaveOverheadAllocationRuleAsync(rule);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Rent Allocation");
    }

    #endregion

    #region Report Management Tests

    [Fact]
    public async Task SaveReportConfigurationAsync_ShouldSaveReport()
    {
        // Arrange
        var report = new SavedReport
        {
            StoreId = 1,
            Name = "Monthly P&L",
            ReportType = "ComparativePL",
            ParametersJson = "{\"period\":\"monthly\"}"
        };

        // Act
        var result = await _service.SaveReportConfigurationAsync(report);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Monthly P&L");
    }

    [Fact]
    public async Task GetSavedReportsAsync_ShouldReturnReports()
    {
        // Arrange
        await _service.SaveReportConfigurationAsync(new SavedReport
        {
            StoreId = 1,
            Name = "Monthly P&L",
            ReportType = "ComparativePL"
        });

        // Act
        var result = await _service.GetSavedReportsAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteSavedReportAsync_ShouldDeleteReport()
    {
        // Arrange
        var report = await _service.SaveReportConfigurationAsync(new SavedReport
        {
            StoreId = 1,
            Name = "Test Report",
            ReportType = "GrossMargin"
        });

        // Act
        await _service.DeleteSavedReportAsync(report.Id);

        // Assert
        var result = await _service.GetSavedReportsAsync(1);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LogReportExecutionAsync_ShouldLogExecution()
    {
        // Arrange
        var log = new ReportExecutionLog
        {
            ReportType = "GrossMargin",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(5),
            DurationMs = 5000,
            IsSuccess = true
        };

        // Act
        await _service.LogReportExecutionAsync(log);

        // Assert
        var history = await _service.GetReportExecutionHistoryAsync();
        history.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExportReportAsync_ShouldReturnExportResult()
    {
        // Act
        var result = await _service.ExportReportAsync("GrossMargin", new { }, "pdf");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ContentType.Should().Be("application/pdf");
    }

    #endregion

    #region Margin Trends Tests

    [Fact]
    public async Task GetMarginTrendsAsync_ShouldReturnTrends()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetMarginTrendsAsync(1, null, startDate, endDate, "day");

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion
}
