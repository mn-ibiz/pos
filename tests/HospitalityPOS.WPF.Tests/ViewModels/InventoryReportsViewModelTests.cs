using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels;
using Xunit;

namespace HospitalityPOS.WPF.Tests.ViewModels;

/// <summary>
/// Unit tests for the InventoryReportsViewModel class.
/// </summary>
public class InventoryReportsViewModelTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IReportPrintService> _reportPrintServiceMock;
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<ILogger> _loggerMock;

    public InventoryReportsViewModelTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _reportPrintServiceMock = new Mock<IReportPrintService>();
        _categoryServiceMock = new Mock<ICategoryService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _exportServiceMock = new Mock<IExportService>();
        _loggerMock = new Mock<ILogger>();
    }

    private InventoryReportsViewModel CreateViewModel()
    {
        return new InventoryReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _categoryServiceMock.Object,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Title.Should().Be("Inventory Reports");
        viewModel.ReportTypes.Should().HaveCount(5);
        viewModel.SelectedReportType.Should().NotBeNull();
        viewModel.HasReport.Should().BeFalse();
        viewModel.IncludeOutOfStock.Should().BeTrue();
        viewModel.DeadStockDays.Should().Be(30);
    }

    [Fact]
    public void Constructor_WithNullReportService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryReportsViewModel(
            null!,
            _reportPrintServiceMock.Object,
            _categoryServiceMock.Object,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportService");
    }

    [Fact]
    public void Constructor_WithNullReportPrintService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryReportsViewModel(
            _reportServiceMock.Object,
            null!,
            _categoryServiceMock.Object,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportPrintService");
    }

    [Fact]
    public void Constructor_WithNullCategoryService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            null!,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("categoryService");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _categoryServiceMock.Object,
            null!,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationService");
    }

    #endregion

    #region ReportTypes Tests

    [Fact]
    public void ReportTypes_ShouldContainAllFiveReportTypes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "CurrentStock");
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "LowStock");
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "StockMovement");
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "StockValuation");
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "DeadStock");
    }

    [Fact]
    public void ReportTypes_StockMovement_ShouldRequireDateRange()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var stockMovementReport = viewModel.ReportTypes.First(r => r.ReportType == "StockMovement");

        // Assert
        stockMovementReport.RequiresDateRange.Should().BeTrue();
    }

    #endregion

    #region GenerateReportCommand Tests

    [Fact]
    public async Task GenerateReportCommand_CurrentStock_ShouldGenerateCurrentStockReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "CurrentStock");

        var expectedResult = new CurrentStockReportResult
        {
            TotalSkuCount = 50,
            ItemsInStock = 45,
            OutOfStockCount = 3,
            LowStockCount = 2,
            TotalStockValue = 250000m,
            TotalRetailValue = 350000m,
            Items =
            [
                new CurrentStockItem
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "Test Product",
                    CurrentStock = 100,
                    Status = "OK"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateCurrentStockReportAsync(
                It.IsAny<InventoryReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowCurrentStockReport.Should().BeTrue();
        viewModel.CurrentStockReport.Should().NotBeNull();
        viewModel.CurrentStockReport!.TotalSkuCount.Should().Be(50);
        viewModel.CurrentStockItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateReportCommand_LowStock_ShouldGenerateLowStockReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "LowStock");

        var expectedResult = new LowStockReportResult
        {
            CriticalCount = 5,
            LowStockCount = 10,
            TotalReorderValue = 50000m,
            Items =
            [
                new LowStockItem
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "Low Stock Item",
                    CurrentStock = 2,
                    MinStock = 10,
                    Status = "LOW"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateLowStockReportAsync(
                It.IsAny<InventoryReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowLowStockReport.Should().BeTrue();
        viewModel.LowStockReport.Should().NotBeNull();
        viewModel.LowStockReport!.CriticalCount.Should().Be(5);
    }

    [Fact]
    public async Task GenerateReportCommand_StockMovement_ShouldGenerateStockMovementReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "StockMovement");

        var expectedResult = new StockMovementReportResult
        {
            TotalReceived = 500,
            TotalSold = 300,
            TotalAdjusted = 10,
            NetMovement = 210,
            Items =
            [
                new StockMovementItem
                {
                    MovementId = 1,
                    ProductName = "Test Product",
                    MovementType = "Sale",
                    Quantity = -5
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateStockMovementReportAsync(
                It.IsAny<InventoryReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowStockMovementReport.Should().BeTrue();
        viewModel.StockMovementReport.Should().NotBeNull();
        viewModel.StockMovementReport!.NetMovement.Should().Be(210);
    }

    [Fact]
    public async Task GenerateReportCommand_StockValuation_ShouldGenerateStockValuationReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "StockValuation");

        var expectedResult = new StockValuationReportResult
        {
            TotalCostValue = 500000m,
            TotalRetailValue = 750000m,
            PotentialProfit = 250000m,
            Categories =
            [
                new CategoryValuation
                {
                    CategoryId = 1,
                    CategoryName = "Beverages",
                    ItemCount = 20,
                    CostValue = 100000m,
                    RetailValue = 150000m
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateStockValuationReportAsync(
                It.IsAny<InventoryReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowStockValuationReport.Should().BeTrue();
        viewModel.StockValuationReport.Should().NotBeNull();
        viewModel.StockValuationReport!.TotalCostValue.Should().Be(500000m);
        viewModel.CategoryValuations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateReportCommand_DeadStock_ShouldGenerateDeadStockReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "DeadStock");
        viewModel.DeadStockDays = 60;

        var expectedResult = new DeadStockReportResult
        {
            TotalCount = 15,
            TotalValue = 75000m,
            DaysThreshold = 60,
            Items =
            [
                new HospitalityPOS.Core.Models.Reports.DeadStockItem
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "Dead Stock Item",
                    CurrentStock = 50,
                    StockValue = 5000m,
                    DaysSinceMovement = 90
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateDeadStockReportAsync(
                It.IsAny<InventoryReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowDeadStockReport.Should().BeTrue();
        viewModel.DeadStockReport.Should().NotBeNull();
        viewModel.DeadStockReport!.DaysThreshold.Should().Be(60);
        viewModel.DeadStockItems.Should().HaveCount(1);
    }

    #endregion

    #region Date Range Commands Tests

    [Fact]
    public void SetTodayCommand_ShouldSetBothDatesToToday()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.FromDate = DateTime.Today.AddDays(-30);
        viewModel.ToDate = DateTime.Today.AddDays(-30);

        // Act
        viewModel.SetTodayCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(DateTime.Today);
        viewModel.ToDate.Should().Be(DateTime.Today);
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

    [Fact]
    public void SetLast30DaysCommand_ShouldSetDateRangeToLast30Days()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetLast30DaysCommand.Execute(null);

        // Assert
        viewModel.FromDate.Should().Be(DateTime.Today.AddDays(-30));
        viewModel.ToDate.Should().Be(DateTime.Today);
    }

    #endregion

    #region SelectedReportType Changed Tests

    [Fact]
    public void SelectedReportType_Changed_ShouldClearCurrentReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.HasReport = true;
        viewModel.ShowCurrentStockReport = true;
        viewModel.CurrentStockReport = new CurrentStockReportResult();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.Last();

        // Assert
        viewModel.HasReport.Should().BeFalse();
        viewModel.ShowCurrentStockReport.Should().BeFalse();
        viewModel.ShowLowStockReport.Should().BeFalse();
    }

    [Fact]
    public void SelectedReportType_StockMovement_ShouldShowDateRange()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "StockMovement");

        // Assert
        viewModel.ShowDateRange.Should().BeTrue();
    }

    [Fact]
    public void SelectedReportType_DeadStock_ShouldShowDeadStockFilter()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "DeadStock");

        // Assert
        viewModel.ShowDeadStockFilter.Should().BeTrue();
    }

    #endregion

    #region ClearCategoryFilterCommand Tests

    [Fact]
    public void ClearCategoryFilterCommand_ShouldClearSelectedCategory()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedCategory = new Category { Id = 1, Name = "Test Category" };

        // Act
        viewModel.ClearCategoryFilterCommand.Execute(null);

        // Assert
        viewModel.SelectedCategory.Should().BeNull();
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
        viewModel.HasReport = false;

        // Act
        await viewModel.PrintReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        viewModel.ErrorMessage.Should().Contain("No report to print");
    }

    #endregion

    #region ExportCsvCommand Tests

    [Fact]
    public async Task ExportCsvCommand_WithNoReport_ShouldShowError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.HasReport = false;

        // Act
        await viewModel.ExportCsvCommand.ExecuteAsync(null);

        // Assert
        viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        viewModel.ErrorMessage.Should().Contain("No report to export");
    }

    #endregion
}
