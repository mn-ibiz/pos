using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.BackgroundJobs;
using HospitalityPOS.Infrastructure.Data;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the TerminalHealthMonitoringJob class.
/// Tests cover health status determination, summary generation, and terminal health queries.
/// </summary>
public class TerminalHealthMonitoringJobTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger<TerminalHealthMonitoringJob>> _loggerMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly TerminalHealthMonitoringJob _healthJob;
    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public TerminalHealthMonitoringJobTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger<TerminalHealthMonitoringJob>>();

        // Setup service provider
        var services = new ServiceCollection();
        services.AddScoped(_ => _context);
        _serviceProvider = services.BuildServiceProvider();

        var healthOptions = Options.Create(new TerminalHealthMonitoringOptions
        {
            IntervalSeconds = 30,
            HeartbeatTimeoutSeconds = 60,
            LogStatusChanges = true
        });

        _healthJob = new TerminalHealthMonitoringJob(_serviceProvider, _loggerMock.Object, healthOptions);

        // Seed test store
        SeedTestStore();
    }

    private void SeedTestStore()
    {
        var store = new Store
        {
            Id = TestStoreId,
            Name = "Test Store",
            Code = "TS001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };
        _context.Stores.Add(store);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _healthJob.Dispose();
        _serviceProvider.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<Terminal> CreateTestTerminalAsync(
        string code = "REG-001",
        string name = "Register 1",
        TerminalType terminalType = TerminalType.Register,
        bool isActive = true,
        DateTime? lastHeartbeat = null,
        string? machineIdentifier = null,
        string? ipAddress = null)
    {
        var terminal = new Terminal
        {
            StoreId = TestStoreId,
            Code = code,
            Name = name,
            TerminalType = terminalType,
            BusinessMode = BusinessMode.Supermarket,
            MachineIdentifier = machineIdentifier ?? string.Empty,
            IsActive = isActive,
            LastHeartbeat = lastHeartbeat,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Terminals.Add(terminal);
        await _context.SaveChangesAsync();

        return terminal;
    }

    #endregion

    #region GetAllTerminalHealthAsync Tests

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldReturnAllTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5));
        await CreateTestTerminalAsync(code: "REG-003", isActive: false);

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldShowOnlineStatus_WhenRecentHeartbeat()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow.AddSeconds(-30));

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsOnline.Should().BeTrue();
        result[0].Status.Should().Be(TerminalStatus.Online);
    }

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldShowOfflineStatus_WhenExpiredHeartbeat()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow.AddSeconds(-90));

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsOnline.Should().BeFalse();
        result[0].Status.Should().Be(TerminalStatus.Offline);
    }

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldShowUnknownStatus_WhenNoHeartbeat()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: null);

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(TerminalStatus.Unknown);
    }

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldCalculateSecondsSinceHeartbeat()
    {
        // Arrange
        var heartbeatTime = DateTime.UtcNow.AddSeconds(-45);
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: heartbeatTime);

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result[0].SecondsSinceLastHeartbeat.Should().NotBeNull();
        result[0].SecondsSinceLastHeartbeat.Should().BeInRange(44, 46);
    }

    [Fact]
    public async Task GetAllTerminalHealthAsync_ShouldIncludeWarnings_WhenNoMachineIdentifier()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", machineIdentifier: null, lastHeartbeat: DateTime.UtcNow);

        // Act
        var result = await _healthJob.GetAllTerminalHealthAsync(TestStoreId);

        // Assert
        result[0].Warnings.Should().Contain(w => w.Contains("machine identifier"));
        result[0].HasWarnings.Should().BeTrue();
    }

    #endregion

    #region GetTerminalHealthAsync Tests

    [Fact]
    public async Task GetTerminalHealthAsync_ShouldReturnHealth_WhenExists()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow);

        // Act
        var result = await _healthJob.GetTerminalHealthAsync(terminal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TerminalId.Should().Be(terminal.Id);
        result.Code.Should().Be("REG-001");
    }

    [Fact]
    public async Task GetTerminalHealthAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _healthJob.GetTerminalHealthAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetOfflineTerminalsAsync Tests

    [Fact]
    public async Task GetOfflineTerminalsAsync_ShouldReturnOnlyOfflineActiveTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true); // Online
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true); // Offline
        await CreateTestTerminalAsync(code: "REG-003", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: false); // Inactive

        // Act
        var result = await _healthJob.GetOfflineTerminalsAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("REG-002");
    }

    [Fact]
    public async Task GetOfflineTerminalsAsync_ShouldReturnEmpty_WhenAllOnline()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddSeconds(-30), isActive: true);

        // Act
        var result = await _healthJob.GetOfflineTerminalsAsync(TestStoreId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetTerminalsWithWarningsAsync Tests

    [Fact]
    public async Task GetTerminalsWithWarningsAsync_ShouldReturnTerminalsWithWarnings()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", machineIdentifier: "AA:BB:CC", lastHeartbeat: DateTime.UtcNow);
        await CreateTestTerminalAsync(code: "REG-002", machineIdentifier: null, lastHeartbeat: DateTime.UtcNow);
        await CreateTestTerminalAsync(code: "REG-003", machineIdentifier: "", lastHeartbeat: DateTime.UtcNow);

        // Act
        var result = await _healthJob.GetTerminalsWithWarningsAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(2); // REG-002 and REG-003 have no machine identifier
    }

    #endregion

    #region GetStoreHealthSummaryAsync Tests

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-003", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);
        await CreateTestTerminalAsync(code: "REG-004", isActive: false);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.TotalTerminals.Should().Be(4);
        result.OnlineTerminals.Should().Be(2);
        result.OfflineTerminals.Should().Be(1);
        result.InactiveTerminals.Should().Be(1);
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldCalculateHealthPercentage()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.HealthPercentage.Should().Be(50); // 1 online out of 2 active = 50%
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldReturn100Percent_WhenNoActiveTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", isActive: false);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.HealthPercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldSetOverallStatus_Healthy()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow, isActive: true);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.OverallStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldSetOverallStatus_Good()
    {
        // Arrange - 4 online, 1 offline = 80%
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-003", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-004", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-005", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.OverallStatus.Should().Be("Good");
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldSetOverallStatus_Degraded()
    {
        // Arrange - 1 online, 1 offline = 50%
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.OverallStatus.Should().Be("Degraded");
    }

    [Fact]
    public async Task GetStoreHealthSummaryAsync_ShouldSetOverallStatus_Critical()
    {
        // Arrange - 1 online, 3 offline = 25%
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);
        await CreateTestTerminalAsync(code: "REG-003", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);
        await CreateTestTerminalAsync(code: "REG-004", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5), isActive: true);

        // Act
        var result = await _healthJob.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        result.OverallStatus.Should().Be("Critical");
    }

    #endregion

    #region RunHealthCheckNowAsync Tests

    [Fact]
    public async Task RunHealthCheckNowAsync_ShouldReturnCheckedCount()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow, isActive: true);

        // Act
        var result = await _healthJob.RunHealthCheckNowAsync(TestStoreId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task RunHealthCheckNowAsync_ShouldOnlyCheckActiveTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow, isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", isActive: false);

        // Act
        var result = await _healthJob.RunHealthCheckNowAsync(TestStoreId);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task RunHealthCheckNowAsync_ShouldUpdateLastCheckTime()
    {
        // Arrange
        var beforeCheck = _healthJob.GetLastCheckTime();
        await CreateTestTerminalAsync(code: "REG-001", isActive: true);

        // Act
        await _healthJob.RunHealthCheckNowAsync(TestStoreId);

        // Assert
        var afterCheck = _healthJob.GetLastCheckTime();
        afterCheck.Should().NotBeNull();
        afterCheck.Should().BeAfter(beforeCheck ?? DateTime.MinValue);
    }

    #endregion

    #region IsRunning Tests

    [Fact]
    public void IsRunning_ShouldReturnFalse_Initially()
    {
        // Assert
        _healthJob.IsRunning.Should().BeFalse();
    }

    #endregion

    #region GetLastCheckTime Tests

    [Fact]
    public void GetLastCheckTime_ShouldReturnNull_Initially()
    {
        // Assert
        _healthJob.GetLastCheckTime().Should().BeNull();
    }

    #endregion
}
