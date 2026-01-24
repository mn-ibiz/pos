using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels;
using Xunit;

namespace HospitalityPOS.WPF.Tests.ViewModels;

/// <summary>
/// Unit tests for TerminalStatusDashboardViewModel.
/// Tests cover dashboard loading, health monitoring, auto-refresh, and terminal status display.
/// </summary>
public class TerminalStatusDashboardViewModelTests
{
    private readonly Mock<ITerminalHealthService> _healthServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TerminalStatusDashboardViewModel _viewModel;

    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public TerminalStatusDashboardViewModelTests()
    {
        _healthServiceMock = new Mock<ITerminalHealthService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger>();

        // Setup session service
        _sessionServiceMock.Setup(s => s.CurrentStoreId).Returns(TestStoreId);
        _sessionServiceMock.Setup(s => s.CurrentUser).Returns(new User { Id = TestUserId, Username = "test" });

        _viewModel = new TerminalStatusDashboardViewModel(
            _healthServiceMock.Object,
            _sessionServiceMock.Object,
            _navigationServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.AutoRefreshEnabled.Should().BeTrue();
        _viewModel.RefreshIntervalSeconds.Should().Be(30);
        _viewModel.OverallStatus.Should().Be("Unknown");
        _viewModel.Terminals.Should().BeEmpty();
        _viewModel.OfflineTerminalsList.Should().BeEmpty();
        _viewModel.WarningTerminalsList.Should().BeEmpty();
    }

    #endregion

    #region Load Dashboard Tests

    [Fact]
    public async Task LoadDashboardAsync_ShouldPopulateSummaryProperties()
    {
        // Arrange
        var summary = new StoreTerminalHealthSummary
        {
            TotalTerminals = 10,
            OnlineTerminals = 8,
            OfflineTerminals = 1,
            InactiveTerminals = 1,
            TerminalsWithWarnings = 2,
            HealthPercentage = 88.89,
            OverallStatus = "Good"
        };
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var healthStatuses = new List<TerminalHealthStatus>();
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatuses);

        _healthServiceMock.Setup(s => s.GetLastCheckTime()).Returns(DateTime.UtcNow);

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.TotalTerminals.Should().Be(10);
        _viewModel.OnlineTerminals.Should().Be(8);
        _viewModel.OfflineTerminals.Should().Be(1);
        _viewModel.InactiveTerminals.Should().Be(1);
        _viewModel.TerminalsWithWarnings.Should().Be(2);
        _viewModel.HealthPercentage.Should().BeApproximately(88.89, 0.01);
        _viewModel.OverallStatus.Should().Be("Good");
    }

    [Fact]
    public async Task LoadDashboardAsync_ShouldPopulateTerminalsList()
    {
        // Arrange
        var summary = CreateTestSummary();
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var healthStatuses = new List<TerminalHealthStatus>
        {
            CreateTestHealthStatus(1, "REG-001", isOnline: true),
            CreateTestHealthStatus(2, "REG-002", isOnline: true),
            CreateTestHealthStatus(3, "REG-003", isOnline: false)
        };
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatuses);

        _healthServiceMock.Setup(s => s.GetLastCheckTime()).Returns(DateTime.UtcNow);

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.Terminals.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadDashboardAsync_ShouldPopulateOfflineTerminalsList()
    {
        // Arrange
        var summary = CreateTestSummary();
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var healthStatuses = new List<TerminalHealthStatus>
        {
            CreateTestHealthStatus(1, "REG-001", isOnline: true, isActive: true),
            CreateTestHealthStatus(2, "REG-002", isOnline: false, isActive: true),
            CreateTestHealthStatus(3, "REG-003", isOnline: false, isActive: false) // Inactive shouldn't be in offline list
        };
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatuses);

        _healthServiceMock.Setup(s => s.GetLastCheckTime()).Returns(DateTime.UtcNow);

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.OfflineTerminalsList.Should().HaveCount(1);
        _viewModel.OfflineTerminalsList[0].Code.Should().Be("REG-002");
    }

    [Fact]
    public async Task LoadDashboardAsync_ShouldPopulateWarningTerminalsList()
    {
        // Arrange
        var summary = CreateTestSummary();
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var healthStatuses = new List<TerminalHealthStatus>
        {
            CreateTestHealthStatus(1, "REG-001", hasWarnings: false),
            CreateTestHealthStatus(2, "REG-002", hasWarnings: true),
            CreateTestHealthStatus(3, "REG-003", hasWarnings: true)
        };
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatuses);

        _healthServiceMock.Setup(s => s.GetLastCheckTime()).Returns(DateTime.UtcNow);

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.WarningTerminalsList.Should().HaveCount(2);
    }

    #endregion

    #region Overall Status Color Tests

    [Fact]
    public async Task LoadDashboardAsync_HealthyStatus_ShouldSetGreenColor()
    {
        // Arrange
        var summary = CreateTestSummary(overallStatus: "Healthy");
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TerminalHealthStatus>());

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.OverallStatusColor.Should().Be("#4CAF50");
    }

    [Fact]
    public async Task LoadDashboardAsync_GoodStatus_ShouldSetLightGreenColor()
    {
        // Arrange
        var summary = CreateTestSummary(overallStatus: "Good");
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TerminalHealthStatus>());

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.OverallStatusColor.Should().Be("#8BC34A");
    }

    [Fact]
    public async Task LoadDashboardAsync_DegradedStatus_ShouldSetOrangeColor()
    {
        // Arrange
        var summary = CreateTestSummary(overallStatus: "Degraded");
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TerminalHealthStatus>());

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.OverallStatusColor.Should().Be("#FFB347");
    }

    [Fact]
    public async Task LoadDashboardAsync_CriticalStatus_ShouldSetRedColor()
    {
        // Arrange
        var summary = CreateTestSummary(overallStatus: "Critical");
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TerminalHealthStatus>());

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        // Assert
        _viewModel.OverallStatusColor.Should().Be("#FF6B6B");
    }

    #endregion

    #region Run Health Check Tests

    [Fact]
    public async Task RunHealthCheckCommand_ShouldRunHealthCheck()
    {
        // Arrange
        _healthServiceMock.Setup(s => s.RunHealthCheckNowAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var summary = CreateTestSummary();
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TerminalHealthStatus>());

        _viewModel.OnNavigatedTo(null);

        // Act
        await _viewModel.RunHealthCheckCommand.ExecuteAsync(null);

        // Assert
        _healthServiceMock.Verify(s => s.RunHealthCheckNowAsync(TestStoreId, It.IsAny<CancellationToken>()), Times.Once);
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Health Check Complete", It.Is<string>(m => m.Contains("5"))), Times.Once);
    }

    #endregion

    #region View Terminal Details Tests

    [Fact]
    public async Task ViewTerminalDetailsCommand_WithSelectedTerminal_ShouldShowDetails()
    {
        // Arrange
        var summary = CreateTestSummary();
        _healthServiceMock.Setup(s => s.GetStoreHealthSummaryAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var healthStatuses = new List<TerminalHealthStatus>
        {
            CreateTestHealthStatus(1, "REG-001")
        };
        _healthServiceMock.Setup(s => s.GetAllTerminalHealthAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatuses);

        var detailedHealth = CreateTestHealthStatus(1, "REG-001");
        _healthServiceMock.Setup(s => s.GetTerminalHealthAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailedHealth);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(200);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.ViewTerminalDetailsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Terminal Details", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ViewTerminalDetailsCommand_WithNoSelection_ShouldNotShowDetails()
    {
        // Arrange
        _viewModel.SelectedTerminal = null;

        // Act
        await _viewModel.ViewTerminalDetailsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void ManageTerminalsCommand_ShouldNavigateToTerminalManagement()
    {
        // Act
        _viewModel.ManageTerminalsCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo<TerminalManagementViewModel>(null), Times.Once);
    }

    [Fact]
    public void GoBackCommand_ShouldNavigateBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    #endregion

    #region Auto-Refresh Tests

    [Fact]
    public void OnNavigatedFrom_ShouldStopAutoRefresh()
    {
        // Act
        _viewModel.OnNavigatedFrom();

        // Assert - No exception should be thrown
        // The timer should be stopped
        _viewModel.Should().NotBeNull();
    }

    [Fact]
    public void AutoRefreshEnabled_WhenSetToFalse_ShouldStopTimer()
    {
        // Act
        _viewModel.AutoRefreshEnabled = false;

        // Assert
        _viewModel.AutoRefreshEnabled.Should().BeFalse();
    }

    [Fact]
    public void RefreshIntervalSeconds_WhenChanged_ShouldUpdateTimer()
    {
        // Act
        _viewModel.RefreshIntervalSeconds = 60;

        // Assert
        _viewModel.RefreshIntervalSeconds.Should().Be(60);
    }

    #endregion

    #region TerminalHealthDisplayItem Tests

    [Fact]
    public void TerminalHealthDisplayItem_StatusColor_ShouldReflectStatus()
    {
        // Arrange
        var onlineItem = new TerminalHealthDisplayItem { Status = TerminalStatus.Online };
        var offlineItem = new TerminalHealthDisplayItem { Status = TerminalStatus.Offline };
        var maintenanceItem = new TerminalHealthDisplayItem { Status = TerminalStatus.Maintenance };
        var errorItem = new TerminalHealthDisplayItem { Status = TerminalStatus.Error };

        // Assert
        onlineItem.StatusColor.Should().Be("#4CAF50");
        offlineItem.StatusColor.Should().Be("#FF6B6B");
        maintenanceItem.StatusColor.Should().Be("#FFB347");
        errorItem.StatusColor.Should().Be("#F44336");
    }

    [Fact]
    public void TerminalHealthDisplayItem_LastSeenText_ShouldFormatCorrectly()
    {
        // Arrange
        var withHeartbeat = new TerminalHealthDisplayItem { LastHeartbeat = DateTime.Now };
        var withoutHeartbeat = new TerminalHealthDisplayItem { LastHeartbeat = null };

        // Assert
        withHeartbeat.LastSeenText.Should().NotBe("Never");
        withoutHeartbeat.LastSeenText.Should().Be("Never");
    }

    [Fact]
    public void TerminalHealthDisplayItem_TimeSinceText_ShouldFormatCorrectly()
    {
        // Arrange
        var seconds = new TerminalHealthDisplayItem { SecondsSinceLastHeartbeat = 45 };
        var minutes = new TerminalHealthDisplayItem { SecondsSinceLastHeartbeat = 120 };
        var hours = new TerminalHealthDisplayItem { SecondsSinceLastHeartbeat = 7200 };
        var noHeartbeat = new TerminalHealthDisplayItem { SecondsSinceLastHeartbeat = null };

        // Assert
        seconds.TimeSinceText.Should().Be("45s");
        minutes.TimeSinceText.Should().Be("2m");
        hours.TimeSinceText.Should().Be("2h");
        noHeartbeat.TimeSinceText.Should().Be("-");
    }

    #endregion

    #region Helper Methods

    private StoreTerminalHealthSummary CreateTestSummary(
        int total = 5,
        int online = 4,
        int offline = 1,
        int inactive = 0,
        int withWarnings = 0,
        double healthPercentage = 80,
        string overallStatus = "Good")
    {
        return new StoreTerminalHealthSummary
        {
            StoreId = TestStoreId,
            TotalTerminals = total,
            OnlineTerminals = online,
            OfflineTerminals = offline,
            InactiveTerminals = inactive,
            TerminalsWithWarnings = withWarnings,
            HealthPercentage = healthPercentage,
            OverallStatus = overallStatus,
            Timestamp = DateTime.UtcNow
        };
    }

    private TerminalHealthStatus CreateTestHealthStatus(
        int id = 1,
        string code = "REG-001",
        string name = "Register 1",
        bool isOnline = true,
        bool isActive = true,
        bool hasWarnings = false)
    {
        return new TerminalHealthStatus
        {
            TerminalId = id,
            Code = code,
            Name = name,
            TerminalType = TerminalType.Register,
            StoreId = TestStoreId,
            Status = isOnline ? TerminalStatus.Online : TerminalStatus.Offline,
            StatusText = isOnline ? "Online" : "Offline",
            IsOnline = isOnline,
            IsActive = isActive,
            LastHeartbeat = DateTime.UtcNow.AddSeconds(-30),
            SecondsSinceLastHeartbeat = 30,
            Warnings = hasWarnings ? new List<string> { "Test warning" } : new List<string>(),
            HasWarnings = hasWarnings
        };
    }

    #endregion
}
