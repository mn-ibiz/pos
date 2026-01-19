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
/// Unit tests for the AuditReportsViewModel class.
/// </summary>
public class AuditReportsViewModelTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IReportPrintService> _reportPrintServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<ILogger> _loggerMock;

    public AuditReportsViewModelTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _reportPrintServiceMock = new Mock<IReportPrintService>();
        _userServiceMock = new Mock<IUserService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _exportServiceMock = new Mock<IExportService>();
        _loggerMock = new Mock<ILogger>();

        // Setup default returns
        _userServiceMock
            .Setup(s => s.GetAllUsersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _reportServiceMock
            .Setup(s => s.GetDistinctAuditActionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
    }

    private AuditReportsViewModel CreateViewModel()
    {
        return new AuditReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
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
        viewModel.Title.Should().Be("Audit Trail Reports");
        viewModel.ReportTypes.Should().HaveCount(6);
        viewModel.SelectedReportType.Should().NotBeNull();
        viewModel.HasReport.Should().BeFalse();
        viewModel.FromDate.Should().Be(DateTime.Today);
        viewModel.ToDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public void Constructor_WithNullReportService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new AuditReportsViewModel(
            null!,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
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
        var action = () => new AuditReportsViewModel(
            _reportServiceMock.Object,
            null!,
            _userServiceMock.Object,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportPrintService");
    }

    [Fact]
    public void Constructor_WithNullUserService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new AuditReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            null!,
            _navigationServiceMock.Object,
            _exportServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("userService");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new AuditReportsViewModel(
            _reportServiceMock.Object,
            _reportPrintServiceMock.Object,
            _userServiceMock.Object,
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
    public void ReportTypes_ShouldContainAllSixReportTypes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.AllActivity);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.UserActivity);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.TransactionLog);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.VoidRefundLog);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.PriceChangeLog);
        viewModel.ReportTypes.Should().Contain(r => r.ReportType == AuditReportType.PermissionOverrideLog);
    }

    #endregion

    #region GenerateReportCommand Tests

    [Fact]
    public async Task GenerateReportCommand_AllActivity_ShouldGenerateAuditTrailReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == AuditReportType.AllActivity);

        var expectedResult = new AuditTrailReportResult
        {
            TotalActions = 100,
            UniqueUsers = 5,
            Items =
            [
                new AuditTrailItem
                {
                    AuditLogId = 1,
                    Timestamp = DateTime.UtcNow,
                    UserName = "John Smith",
                    Action = "Login",
                    ActionDisplayName = "User Login"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateAuditTrailReportAsync(
                It.IsAny<AuditReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowAllActivityReport.Should().BeTrue();
        viewModel.AuditTrailReport.Should().NotBeNull();
        viewModel.AuditTrailReport!.TotalActions.Should().Be(100);
        viewModel.AuditTrailItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateReportCommand_UserActivity_ShouldGenerateUserActivityReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == AuditReportType.UserActivity);

        var expectedResult = new UserActivityReportResult
        {
            TotalActions = 50,
            LoginCount = 20,
            LogoutCount = 15,
            FailedLoginCount = 5,
            Items =
            [
                new UserActivityItem
                {
                    AuditLogId = 1,
                    Timestamp = DateTime.UtcNow,
                    UserName = "John Smith",
                    Action = "Login"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateUserActivityReportAsync(
                It.IsAny<AuditReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowUserActivityReport.Should().BeTrue();
        viewModel.UserActivityReport.Should().NotBeNull();
        viewModel.UserActivityReport!.LoginCount.Should().Be(20);
    }

    [Fact]
    public async Task GenerateReportCommand_VoidRefundLog_ShouldGenerateVoidRefundLogReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == AuditReportType.VoidRefundLog);

        var expectedResult = new VoidRefundLogReportResult
        {
            TotalVoids = 10,
            TotalVoidValue = 50000m,
            Items =
            [
                new VoidRefundLogItem
                {
                    AuditLogId = 1,
                    Timestamp = DateTime.UtcNow,
                    RequestedByUser = "John Smith",
                    AuthorizedByUser = "Mary Manager",
                    ReceiptNumber = "R-0001",
                    VoidedAmount = 5000m
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GenerateVoidRefundLogReportAsync(
                It.IsAny<AuditReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowVoidRefundLogReport.Should().BeTrue();
        viewModel.VoidRefundLogReport.Should().NotBeNull();
        viewModel.VoidRefundLogReport!.TotalVoids.Should().Be(10);
        viewModel.VoidRefundLogReport.TotalVoidValue.Should().Be(50000m);
    }

    [Fact]
    public async Task GenerateReportCommand_PriceChangeLog_ShouldGeneratePriceChangeLogReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == AuditReportType.PriceChangeLog);

        var expectedResult = new PriceChangeLogReportResult
        {
            TotalPriceChanges = 5,
            ProductsAffected = 3,
            Items =
            [
                new PriceChangeLogItem
                {
                    AuditLogId = 1,
                    Timestamp = DateTime.UtcNow,
                    UserName = "Admin",
                    ProductName = "Coca Cola",
                    OldPrice = 100m,
                    NewPrice = 120m,
                    ChangePercentage = 20m
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GeneratePriceChangeLogReportAsync(
                It.IsAny<AuditReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowPriceChangeLogReport.Should().BeTrue();
        viewModel.PriceChangeLogReport.Should().NotBeNull();
        viewModel.PriceChangeLogReport!.TotalPriceChanges.Should().Be(5);
        viewModel.PriceChangeLogItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateReportCommand_PermissionOverrideLog_ShouldGeneratePermissionOverrideLogReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedReportType = viewModel.ReportTypes.First(r => r.ReportType == AuditReportType.PermissionOverrideLog);

        var expectedResult = new PermissionOverrideLogReportResult
        {
            TotalOverrides = 8,
            OverridesByType = new Dictionary<string, int> { { "VoidReceipt", 5 }, { "ApplyDiscount", 3 } },
            Items =
            [
                new PermissionOverrideLogItem
                {
                    AuditLogId = 1,
                    Timestamp = DateTime.UtcNow,
                    RequestedByUser = "John",
                    AuthorizedByUser = "Mary",
                    Permission = "VoidReceipt",
                    PermissionDisplayName = "Void Receipt"
                }
            ]
        };

        _reportServiceMock
            .Setup(s => s.GeneratePermissionOverrideLogReportAsync(
                It.IsAny<AuditReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasReport.Should().BeTrue();
        viewModel.ShowPermissionOverrideLogReport.Should().BeTrue();
        viewModel.PermissionOverrideLogReport.Should().NotBeNull();
        viewModel.PermissionOverrideLogReport!.TotalOverrides.Should().Be(8);
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

    #region Filter Commands Tests

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

    [Fact]
    public void ClearActionFilterCommand_ShouldClearSelectedAction()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedAction = "Login";

        // Act
        viewModel.ClearActionFilterCommand.Execute(null);

        // Assert
        viewModel.SelectedAction.Should().BeNull();
    }

    #endregion

    #region SelectedReportType Changed Tests

    [Fact]
    public void SelectedReportType_Changed_ShouldClearCurrentReport()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.HasReport = true;
        viewModel.ShowAllActivityReport = true;
        viewModel.AuditTrailReport = new AuditTrailReportResult();

        // Act
        viewModel.SelectedReportType = viewModel.ReportTypes.Last();

        // Assert
        viewModel.HasReport.Should().BeFalse();
        viewModel.ShowAllActivityReport.Should().BeFalse();
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

    #region OnNavigatedToAsync Tests

    [Fact]
    public async Task OnNavigatedTo_ShouldLoadUsersAndActions()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var users = new List<User>
        {
            new() { Id = 1, FullName = "User 1" },
            new() { Id = 2, FullName = "User 2" }
        };
        var actions = new List<string> { "Login", "Logout", "OrderCreated" };

        _userServiceMock
            .Setup(s => s.GetAllUsersAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _reportServiceMock
            .Setup(s => s.GetDistinctAuditActionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(actions);

        // Act - OnNavigatedTo triggers async loading
        viewModel.OnNavigatedTo(null);

        // Allow time for async loading to complete
        await Task.Delay(100);

        // Assert
        viewModel.Users.Should().HaveCount(2);
        viewModel.AvailableActions.Should().HaveCount(3);
    }

    #endregion
}
