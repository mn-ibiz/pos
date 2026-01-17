using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels;
using Xunit;

namespace HospitalityPOS.WPF.Tests.ViewModels;

/// <summary>
/// Unit tests for the SalesReportsViewModel class.
/// </summary>
public class SalesReportsViewModelTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IReportPrintService> _reportPrintServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<ILogger> _loggerMock;

    public SalesReportsViewModelTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _reportPrintServiceMock = new Mock<IReportPrintService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _loggerMock = new Mock<ILogger>();
    }

    private SalesReportsViewModel CreateViewModel()
    {
        return new SalesReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _navigationServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Title.Should().Be("Sales Reports");
        viewModel.ReportTypes.Should().HaveCount(6);
        viewModel.SelectedReportType.Should().NotBeNull();
        viewModel.FromDate.Should().Be(DateTime.Today);
        viewModel.ToDate.Should().Be(DateTime.Today);
        viewModel.HasReport.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullReportService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SalesReportsViewModel(
            null!,
            _reportPrintServiceMock.Object,
            _navigationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportService");
    }

    [Fact]
    public void Constructor_WithNullReportPrintService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SalesReportsViewModel(
            _reportServiceMock.Object,
            null!,
            _navigationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportPrintService");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SalesReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationService");
    }

    #endregion

    #region ReportTypes Tests

    [Fact]
    public void ReportTypes_ShouldContainAllSalesReportTypes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.DailySummary);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.ByProduct);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.ByCategory);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.ByCashier);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.ByPaymentMethod);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == SalesReportType.HourlySales);
    }

    #endregion

    #region Date Range Commands Tests

    [Fact]
    public void SetTodayCommand_ShouldSetBothDatesToToday()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.FromDate = DateTime.Today.AddDays(-5);
        viewModel.ToDate = DateTime.Today.AddDays(-5);

        // Act
        viewModel.SetTodayCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(DateTime.Today);
        viewModel.ToDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public void SetYesterdayCommand_ShouldSetBothDatesToYesterday()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetYesterdayCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(DateTime.Today.AddDays(-1));
        viewModel.ToDate.Should().Be(DateTime.Today.AddDays(-1));
    }

    [Fact]
    public void SetThisWeekCommand_ShouldSetDateRangeToThisWeek()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var expectedStart = today.AddDays(-dayOfWeek);

        // Act
        viewModel.SetThisWeekCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(expectedStart);
        viewModel.ToDate.Should().Be(today);
    }

    [Fact]
    public void SetThisMonthCommand_ShouldSetDateRangeToThisMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var today = DateTime.Today;
        var expectedStart = new DateTime(today.Year, today.Month, 1);

        // Act
        viewModel.SetThisMonthCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(expectedStart);
        viewModel.ToDate.Should().Be(today);
    }

    #endregion

    #region GenerateReportCommand Tests

    [Fact]
    public async Task GenerateReportCommand_ShouldGenerateReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var expectedResult = new SalesReportResult
        {
            Summary = new DailySalesSummary
            {
                TransactionCount = 10,
                TotalRevenue = 5000m
            }
        };

        _reportServiceMock
            .Setup(s => s.GenerateSalesReportAsync(
                It.IsAny<SalesReportType>(),
                It.IsAny<SalesReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.CurrentReport.Should().NotBeNull();
        viewModel.DailySummary.Should().NotBeNull();
        viewModel.DailySummary!.TransactionCount.Should().Be(10);
    }

    [Fact]
    public async Task GenerateReportCommand_WithInvalidDateRange_ShouldShowError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.FromDate = DateTime.Today;
        viewModel.ToDate = DateTime.Today.AddDays(-1); // Invalid: ToDate before FromDate

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        viewModel.HasReport.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateReportCommand_ByProduct_ShouldShowProductSalesSection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == SalesReportType.ByProduct);

        _reportServiceMock
            .Setup(s => s.GenerateSalesReportAsync(
                SalesReportType.ByProduct,
                It.IsAny<SalesReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesReportResult
            {
                Summary = new DailySalesSummary(),
                ProductSales = [new ProductSalesReport { ProductName = "Test Product" }]
            });

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.ShowProductSales.Should().BeTrue();
        viewModel.ShowCategorySales.Should().BeFalse();
        viewModel.ShowCashierSales.Should().BeFalse();
    }

    #endregion

    #region SelectedReportType Changed Tests

    [Fact]
    public void SelectedReportType_Changed_ShouldClearCurrentReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.HasReport = true;
        viewModel.CurrentReport = new SalesReportResult();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.Last();

        // Assert
        viewModel.HasReport.Should().BeFalse();
        viewModel.CurrentReport.Should().BeNull();
    }

    #endregion

    #region GoBackCommand Tests

    [Fact]
    public void GoBackCommand_ShouldNavigateBack()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.GoBackCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(s => s.GoBack(), Times.Once);
    }

    #endregion

    #region PrintReportCommand Tests

    [Fact]
    public async Task PrintReportCommand_WithNoReport_ShouldShowError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.CurrentReport = null;

        // Act
        await viewModel.PrintReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        viewModel.ErrorMessage.Should().Contain("No report to print");
        _reportPrintServiceMock.Verify(s => s.PrintSalesReport(It.IsAny<SalesReportResult>()), Times.Never);
    }

    [Fact]
    public async Task PrintReportCommand_WithReport_ShouldCallPrintService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var report = new SalesReportResult
        {
            Summary = new DailySalesSummary { TransactionCount = 5, TotalRevenue = 1000m },
            Parameters = new SalesReportParameters
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            }
        };
        viewModel.CurrentReport = report;
        viewModel.HasReport = true;

        _reportPrintServiceMock
            .Setup(s => s.PrintSalesReport(It.IsAny<SalesReportResult>()))
            .Returns(true);

        // Act
        await viewModel.PrintReportCommand.ExecuteAsync(null);

        // Assert
        _reportPrintServiceMock.Verify(s => s.PrintSalesReport(report), Times.Once);
    }

    [Fact]
    public async Task PrintReportCommand_WhenPrintCancelled_ShouldNotShowError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.CurrentReport = new SalesReportResult
        {
            Summary = new DailySalesSummary(),
            Parameters = new SalesReportParameters
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            }
        };
        viewModel.HasReport = true;

        _reportPrintServiceMock
            .Setup(s => s.PrintSalesReport(It.IsAny<SalesReportResult>()))
            .Returns(false); // User cancelled

        // Act
        await viewModel.PrintReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.ErrorMessage.Should().BeNullOrEmpty();
    }

    #endregion
}
