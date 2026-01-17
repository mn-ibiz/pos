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
/// Unit tests for the ExceptionReportsViewModel class.
/// </summary>
public class ExceptionReportsViewModelTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IReportPrintService> _reportPrintServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<ILogger> _loggerMock;

    public ExceptionReportsViewModelTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _reportPrintServiceMock = new Mock<IReportPrintService>();
        _userServiceMock = new Mock<IUserService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _loggerMock = new Mock<ILogger>();
    }

    private ExceptionReportsViewModel CreateViewModel()
    {
        return new ExceptionReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
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
        viewModel.Title.Should().Be("Exception Reports");
        viewModel.ReportTypes.Should().HaveCount(2);
        viewModel.SelectedReportType.Should().NotBeNull();
        viewModel.FromDate.Should().Be(DateTime.Today);
        viewModel.ToDate.Should().Be(DateTime.Today);
        viewModel.HasReport.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullReportService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExceptionReportsViewModel(
            null!,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
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
        var action = () => new ExceptionReportsViewModel(
            _reportServiceMock.Object,
            null!,
            _userServiceMock.Object,
            _navigationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportPrintService");
    }

    [Fact]
    public void Constructor_WithNullUserService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExceptionReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            null!,
            _navigationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("userService");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExceptionReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationService");
    }

    #endregion

    #region ReportTypes Tests

    [Fact]
    public void ReportTypes_ShouldContainVoidAndDiscountTypes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "Void");
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == "Discount");
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
    public async Task GenerateReportCommand_VoidReport_ShouldGenerateVoidReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "Void");

        var expectedResult = new VoidReportResult
        {
            TotalCount = 5,
            TotalAmount = 2500m,
            Items =
            [
                new VoidReportItem
                {
                    ReceiptNumber = "R-001",
                    VoidedAmount = 500m,
                    VoidedBy = "John",
                    Reason = "Test"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateVoidReportAsync(
                It.IsAny<ExceptionReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowVoidReport.Should().BeTrue();
        viewModel.ShowDiscountReport.Should().BeFalse();
        viewModel.VoidReport.Should().NotBeNull();
        viewModel.VoidReport!.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GenerateReportCommand_DiscountReport_ShouldGenerateDiscountReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == "Discount");

        var expectedResult = new DiscountReportResult
        {
            TotalDiscounts = 1500m,
            DiscountTransactionCount = 10,
            DiscountRate = 5.5m,
            Items =
            [
                new DiscountReportItem
                {
                    ReceiptNumber = "R-001",
                    DiscountAmount = 150m,
                    AppliedBy = "John"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateDiscountReportAsync(
                It.IsAny<ExceptionReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowVoidReport.Should().BeFalse();
        viewModel.ShowDiscountReport.Should().BeTrue();
        viewModel.DiscountReport.Should().NotBeNull();
        viewModel.DiscountReport!.TotalDiscounts.Should().Be(1500m);
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

    #endregion

    #region SelectedReportType Changed Tests

    [Fact]
    public void SelectedReportType_Changed_ShouldClearCurrentReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.HasReport = true;
        viewModel.ShowVoidReport = true;
        viewModel.VoidReport = new VoidReportResult();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.Last();

        // Assert
        viewModel.HasReport.Should().BeFalse();
        viewModel.ShowVoidReport.Should().BeFalse();
        viewModel.ShowDiscountReport.Should().BeFalse();
    }

    #endregion

    #region ClearUserFilterCommand Tests

    [Fact]
    public void ClearUserFilterCommand_ShouldClearSelectedUser()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedUser = new User { Id = 1, FullName = "Test User" };

        // Act
        viewModel.ClearUserFilterCommand.Execute(null);

        // Assert
        viewModel.SelectedUser.Should().BeNull();
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
}
