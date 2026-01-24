using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.BackgroundJobs;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;
using ILogger = Serilog.ILogger;

namespace HospitalityPOS.Business.Tests.Integration;

/// <summary>
/// Integration tests for multi-terminal features.
/// Tests verify correct interaction between TerminalService and TerminalHealthMonitoringJob.
/// </summary>
public class TerminalIntegrationTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILogger<TerminalHealthMonitoringJob>> _healthLoggerMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly ITerminalService _terminalService;
    private readonly ITerminalHealthService _healthService;
    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public TerminalIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();
        _healthLoggerMock = new Mock<ILogger<TerminalHealthMonitoringJob>>();

        // Setup service provider for health job
        var services = new ServiceCollection();
        services.AddScoped(_ => _context);
        _serviceProvider = services.BuildServiceProvider();

        var healthOptions = Options.Create(new TerminalHealthMonitoringOptions
        {
            IntervalSeconds = 30,
            HeartbeatTimeoutSeconds = 60,
            LogStatusChanges = true
        });

        _terminalService = new TerminalService(_context, _loggerMock.Object);
        _healthService = new TerminalHealthMonitoringJob(_serviceProvider, _healthLoggerMock.Object, healthOptions);

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
        (_healthService as IDisposable)?.Dispose();
        _serviceProvider.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Terminal Registration to Health Monitoring Integration

    [Fact]
    public async Task RegisterTerminal_ThenCheckHealth_ShouldShowOnline()
    {
        // Arrange - Register a terminal
        var request = new TerminalRegistrationRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            MachineIdentifier = "AA:BB:CC:DD:EE:FF",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket,
            IpAddress = "192.168.1.100"
        };

        var terminal = await _terminalService.RegisterTerminalAsync(request, TestUserId);

        // Act - Check health
        var health = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Assert - Terminal should be online (just registered = recent heartbeat)
        health.Should().NotBeNull();
        health!.IsOnline.Should().BeTrue();
        health.Status.Should().Be(TerminalStatus.Online);
        health.Code.Should().Be("REG-001");
    }

    [Fact]
    public async Task CreateTerminal_UpdateHeartbeat_ThenCheckHealth_ShouldShowOnline()
    {
        // Arrange - Create terminal without heartbeat
        var createRequest = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        var terminal = await _terminalService.CreateTerminalAsync(createRequest, TestUserId);

        // Initial health check - should be offline/unknown
        var initialHealth = await _healthService.GetTerminalHealthAsync(terminal.Id);
        initialHealth!.IsOnline.Should().BeFalse();

        // Act - Update heartbeat
        var heartbeat = new TerminalHeartbeat
        {
            IpAddress = "192.168.1.100",
            CurrentUserId = TestUserId
        };
        await _terminalService.UpdateHeartbeatAsync(terminal.Id, heartbeat);

        // Check health again
        var finalHealth = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Assert - Terminal should now be online
        finalHealth!.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateTerminal_ThenCheckHealth_ShouldShowInactive()
    {
        // Arrange - Create and register terminal
        var createRequest = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        var terminal = await _terminalService.CreateTerminalAsync(createRequest, TestUserId);
        await _terminalService.UpdateHeartbeatAsync(terminal.Id, new TerminalHeartbeat { IpAddress = "192.168.1.100" });

        // Act - Deactivate terminal
        await _terminalService.DeactivateTerminalAsync(terminal.Id, TestUserId);

        // Check health
        var health = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Assert - Terminal should show inactive
        health!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Store Health Summary Integration

    [Fact]
    public async Task CreateMultipleTerminals_GetStoreSummary_ShouldReflectAccurately()
    {
        // Arrange - Create multiple terminals with various states
        // Online terminal
        var onlineTerminal = await CreateTerminalWithHeartbeat("REG-001", DateTime.UtcNow);

        // Another online terminal
        await CreateTerminalWithHeartbeat("REG-002", DateTime.UtcNow.AddSeconds(-30));

        // Offline terminal (old heartbeat)
        await CreateTerminalWithHeartbeat("REG-003", DateTime.UtcNow.AddMinutes(-5));

        // Inactive terminal
        var inactiveRequest = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-004",
            Name = "Register 4",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        var inactiveTerminal = await _terminalService.CreateTerminalAsync(inactiveRequest, TestUserId);
        await _terminalService.DeactivateTerminalAsync(inactiveTerminal.Id, TestUserId);

        // Act
        var summary = await _healthService.GetStoreHealthSummaryAsync(TestStoreId);

        // Assert
        summary.TotalTerminals.Should().Be(4);
        summary.OnlineTerminals.Should().Be(2);
        summary.OfflineTerminals.Should().Be(1);
        summary.InactiveTerminals.Should().Be(1);
        summary.HealthPercentage.Should().BeApproximately(66.67, 1); // 2/3 active = 66.67%
    }

    [Fact]
    public async Task BindAndUnbindMachine_CheckHealthWarnings_ShouldUpdateAccordingly()
    {
        // Arrange - Create terminal without machine identifier
        var createRequest = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        var terminal = await _terminalService.CreateTerminalAsync(createRequest, TestUserId);

        // Check initial warnings
        var initialHealth = await _healthService.GetTerminalHealthAsync(terminal.Id);
        initialHealth!.Warnings.Should().Contain(w => w.Contains("machine identifier"));

        // Act - Bind machine
        await _terminalService.BindMachineAsync(terminal.Id, "AA:BB:CC:DD:EE:FF", TestUserId);

        // Check warnings after binding
        var boundHealth = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Assert - No machine identifier warning after binding
        boundHealth!.Warnings.Should().NotContain(w => w.Contains("machine identifier"));

        // Act - Unbind machine
        await _terminalService.UnbindMachineAsync(terminal.Id, TestUserId);

        // Check warnings after unbinding
        var unboundHealth = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Assert - Warning should return
        unboundHealth!.Warnings.Should().Contain(w => w.Contains("machine identifier"));
    }

    #endregion

    #region Code Generation Integration

    [Fact]
    public async Task CreateMultipleTerminals_CodeGenerationShouldIncrement()
    {
        // Arrange & Act - Create terminals using generated codes
        var code1 = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);
        var request1 = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = code1,
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        await _terminalService.CreateTerminalAsync(request1, TestUserId);

        var code2 = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);
        var request2 = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = code2,
            Name = "Register 2",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        await _terminalService.CreateTerminalAsync(request2, TestUserId);

        var code3 = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);

        // Assert - Codes should be sequential
        code1.Should().Be("REG-001");
        code2.Should().Be("REG-002");
        code3.Should().Be("REG-003");
    }

    [Fact]
    public async Task CreateDifferentTerminalTypes_CodeGenerationShouldUseDifferentPrefixes()
    {
        // Act
        var regCode = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);
        var tillCode = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Till);
        var kdsCode = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.KitchenDisplay);
        var mobCode = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.MobileTerminal);
        var scoCode = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.SelfCheckout);

        // Assert
        regCode.Should().StartWith("REG-");
        tillCode.Should().StartWith("TILL-");
        kdsCode.Should().StartWith("KDS-");
        mobCode.Should().StartWith("MOB-");
        scoCode.Should().StartWith("SCO-");
    }

    #endregion

    #region Configuration Update Integration

    [Fact]
    public async Task UpdateConfiguration_TerminalServiceAndHealthService_ShouldBeConsistent()
    {
        // Arrange - Create terminal
        var createRequest = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };
        var terminal = await _terminalService.CreateTerminalAsync(createRequest, TestUserId);

        var printerConfig = """{"EnableReceiptPrinting":true,"ReceiptPrinterId":1}""";
        var hardwareConfig = """{"EnableCashDrawer":true,"CashDrawerPort":"COM1"}""";

        // Act - Update configuration via service
        await _terminalService.UpdateTerminalConfigurationAsync(
            terminal.Id, printerConfig, hardwareConfig, TestUserId);

        // Verify via health service (which reads from same context)
        var health = await _healthService.GetTerminalHealthAsync(terminal.Id);

        // Get terminal directly to verify config
        var updatedTerminal = await _terminalService.GetTerminalByIdAsync(terminal.Id);

        // Assert
        health.Should().NotBeNull();
        updatedTerminal!.PrinterConfiguration.Should().Be(printerConfig);
        updatedTerminal.HardwareConfiguration.Should().Be(hardwareConfig);
    }

    #endregion

    #region Run Health Check Integration

    [Fact]
    public async Task RunHealthCheck_ShouldDetectStatusChanges()
    {
        // Arrange - Create multiple terminals
        await CreateTerminalWithHeartbeat("REG-001", DateTime.UtcNow);
        await CreateTerminalWithHeartbeat("REG-002", DateTime.UtcNow.AddMinutes(-5));

        // Act - Run health check
        var checkedCount = await _healthService.RunHealthCheckNowAsync(TestStoreId);

        // Assert
        checkedCount.Should().Be(2);
        _healthService.GetLastCheckTime().Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<Terminal> CreateTerminalWithHeartbeat(string code, DateTime heartbeatTime)
    {
        var terminal = new Terminal
        {
            StoreId = TestStoreId,
            Code = code,
            Name = $"Terminal {code}",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket,
            MachineIdentifier = $"MAC-{code}",
            IsActive = true,
            LastHeartbeat = heartbeatTime,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Terminals.Add(terminal);
        await _context.SaveChangesAsync();

        return terminal;
    }

    #endregion
}
