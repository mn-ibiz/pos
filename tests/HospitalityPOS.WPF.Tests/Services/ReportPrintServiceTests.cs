using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using Xunit;

namespace HospitalityPOS.WPF.Tests.Services;

/// <summary>
/// Unit tests for the ReportPrintService class.
/// </summary>
public class ReportPrintServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly ReportPrintService _service;

    public ReportPrintServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _service = new ReportPrintService(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReportPrintService(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GeneratePrintContent Tests

    [Fact]
    public void GeneratePrintContent_WithNullReport_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => _service.GeneratePrintContent(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GeneratePrintContent_ShouldIncludeHeader()
    {
        // Arrange
        var report = CreateBasicReport();

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES REPORT");
    }

    [Fact]
    public void GeneratePrintContent_ShouldIncludeDateRange()
    {
        // Arrange
        var report = CreateBasicReport();
        report.Parameters.StartDate = new DateTime(2024, 1, 1);
        report.Parameters.EndDate = new DateTime(2024, 1, 31);

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("From: 2024-01-01");
        result.Should().Contain("To:   2024-01-31");
    }

    [Fact]
    public void GeneratePrintContent_ShouldIncludeSummary()
    {
        // Arrange
        var report = CreateBasicReport();
        report.Summary = new DailySalesSummary
        {
            GrossSales = 10000m,
            Discounts = 500m,
            NetSales = 9500m,
            TaxCollected = 1520m,
            TotalRevenue = 11020m,
            TransactionCount = 50,
            AverageTransaction = 200m
        };

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES SUMMARY");
        result.Should().Contain("KSh 10,000.00"); // Gross Sales
        result.Should().Contain("Transactions: 50");
    }

    [Fact]
    public void GeneratePrintContent_WithProductSales_ShouldIncludeProductSection()
    {
        // Arrange
        var report = CreateBasicReport();
        report.ProductSales =
        [
            new ProductSalesReport
            {
                ProductId = 1,
                ProductName = "Test Product",
                QuantitySold = 100,
                GrossSales = 5000m,
                NetSales = 4750m,
                Percentage = 50m
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES BY PRODUCT");
        result.Should().Contain("Test Product");
        result.Should().Contain("Qty: 100");
    }

    [Fact]
    public void GeneratePrintContent_WithCategorySales_ShouldIncludeCategorySection()
    {
        // Arrange
        var report = CreateBasicReport();
        report.CategorySales =
        [
            new CategorySalesReport
            {
                CategoryId = 1,
                CategoryName = "Food",
                ItemCount = 25,
                QuantitySold = 150,
                NetSales = 7500m,
                Percentage = 75m
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES BY CATEGORY");
        result.Should().Contain("Food");
    }

    [Fact]
    public void GeneratePrintContent_WithCashierSales_ShouldIncludeCashierSection()
    {
        // Arrange
        var report = CreateBasicReport();
        report.CashierSales =
        [
            new CashierSalesReport
            {
                UserId = 1,
                CashierName = "John Doe",
                TransactionCount = 30,
                TotalSales = 6000m,
                AverageTransaction = 200m,
                VoidCount = 2
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES BY CASHIER");
        result.Should().Contain("John Doe");
        result.Should().Contain("Trans: 30");
    }

    [Fact]
    public void GeneratePrintContent_WithPaymentMethodSales_ShouldIncludePaymentSection()
    {
        // Arrange
        var report = CreateBasicReport();
        report.PaymentMethodSales =
        [
            new PaymentMethodSalesReport
            {
                PaymentMethodId = 1,
                PaymentMethodName = "Cash",
                TransactionCount = 40,
                TotalAmount = 8000m,
                Percentage = 80m
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("SALES BY PAYMENT METHOD");
        result.Should().Contain("Cash");
    }

    [Fact]
    public void GeneratePrintContent_WithHourlySales_ShouldIncludeHourlySection()
    {
        // Arrange
        var report = CreateBasicReport();
        report.HourlySales =
        [
            new HourlySalesReport
            {
                Hour = 12,
                HourDisplay = "12:00 - 13:00",
                TransactionCount = 15,
                TotalSales = 3000m
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("HOURLY SALES BREAKDOWN");
        result.Should().Contain("12:00 - 13:00");
    }

    [Fact]
    public void GeneratePrintContent_ShouldIncludeFooter()
    {
        // Arrange
        var report = CreateBasicReport();

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("** END OF REPORT **");
    }

    [Fact]
    public void GeneratePrintContent_WithLongProductName_ShouldTruncate()
    {
        // Arrange
        var report = CreateBasicReport();
        report.ProductSales =
        [
            new ProductSalesReport
            {
                ProductId = 1,
                ProductName = "This is a very long product name that should be truncated",
                QuantitySold = 10,
                NetSales = 1000m,
                Percentage = 10m
            }
        ];

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("...");
    }

    [Fact]
    public void GeneratePrintContent_WithManyProducts_ShouldLimitTo20()
    {
        // Arrange
        var report = CreateBasicReport();
        report.ProductSales = Enumerable.Range(1, 25)
            .Select(i => new ProductSalesReport
            {
                ProductId = i,
                ProductName = $"Product {i}",
                QuantitySold = i * 10,
                NetSales = i * 100m,
                Percentage = 4m
            }).ToList();

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("... and 5 more products");
    }

    [Fact]
    public void GeneratePrintContent_WithVoidedTransactions_ShouldIncludeVoidInfo()
    {
        // Arrange
        var report = CreateBasicReport();
        report.Summary = new DailySalesSummary
        {
            GrossSales = 10000m,
            NetSales = 10000m,
            TransactionCount = 50,
            VoidedCount = 3,
            VoidedAmount = 450m
        };

        // Act
        var result = _service.GeneratePrintContent(report);

        // Assert
        result.Should().Contain("Voided Transactions: 3");
        result.Should().Contain("Voided Amount: KSh 450.00");
    }

    #endregion

    #region PrintSalesReport Tests

    [Fact]
    public void PrintSalesReport_WithNullReport_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => _service.PrintSalesReport(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("report");
    }

    // Note: PrintSalesReport with actual printing cannot be easily unit tested
    // as it requires WPF UI components (PrintDialog). Integration tests would be needed.

    #endregion

    private static SalesReportResult CreateBasicReport()
    {
        return new SalesReportResult
        {
            Parameters = new SalesReportParameters
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                GeneratedByUserId = 1
            },
            Summary = new DailySalesSummary
            {
                TransactionCount = 0,
                TotalRevenue = 0m
            },
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "Test User"
        };
    }
}
