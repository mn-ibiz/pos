using FluentAssertions;
using HospitalityPOS.Core.Models.Hardware;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Serilog;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for ScaleService.
/// </summary>
public class ScaleServiceTests : IDisposable
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly ScaleService _service;

    public ScaleServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(_loggerMock.Object);
        _service = new ScaleService(_loggerMock.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Connection Tests

    [Fact]
    public async Task ConnectAsync_WithConfig_ShouldConnectSuccessfully()
    {
        // Arrange
        var config = new ScaleConfiguration
        {
            Name = "Test Scale",
            ConnectionType = ScaleConnectionType.Serial,
            Protocol = ScaleProtocol.Cas,
            PortName = "COM1"
        };

        // Act
        var result = await _service.ConnectAsync(config);

        // Assert
        result.Success.Should().BeTrue();
        _service.IsConnected.Should().BeTrue();
        _service.Status.Should().Be(ScaleStatus.Ready);
        result.ScaleModel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConnectAsync_WithActiveConfig_ShouldConnectSuccessfully()
    {
        // Act
        var result = await _service.ConnectAsync();

        // Assert
        result.Success.Should().BeTrue();
        _service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldDisconnect()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        await _service.DisconnectAsync();

        // Assert
        _service.IsConnected.Should().BeFalse();
        _service.Status.Should().Be(ScaleStatus.Disconnected);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnTestResult()
    {
        // Arrange
        var config = new ScaleConfiguration
        {
            Name = "Test Scale",
            ConnectionType = ScaleConnectionType.Serial,
            Protocol = ScaleProtocol.Cas
        };

        // Act
        var result = await _service.TestConnectionAsync(config);

        // Assert
        result.Success.Should().BeTrue();
        result.Reading.Should().NotBeNull();
        result.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Weight Reading Tests

    [Fact]
    public async Task ReadWeightAsync_WhenConnected_ShouldReturnReading()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(1.5m);

        // Act
        var reading = await _service.ReadWeightAsync();

        // Assert
        reading.Should().NotBeNull();
        reading.Weight.Should().Be(1.5m);
        reading.Unit.Should().Be(WeightUnit.Kilogram);
        reading.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ReadWeightAsync_WhenNotConnected_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReadWeightAsync());
    }

    [Fact]
    public async Task ReadWeightAsync_WithTare_ShouldSubtractTare()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(2.0m);
        await _service.SetTareWeightAsync(0.5m);

        // Act
        var reading = await _service.ReadWeightAsync();

        // Assert
        reading.Weight.Should().Be(1.5m);
        reading.TareWeight.Should().Be(0.5m);
        reading.GrossWeight.Should().Be(2.0m);
    }

    [Fact]
    public async Task WaitForStableWeightAsync_ShouldReturnWhenStable()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(1.25m, stable: true);

        // Act
        var reading = await _service.WaitForStableWeightAsync(timeoutMs: 3000);

        // Assert
        reading.Should().NotBeNull();
        reading!.IsStable.Should().BeTrue();
        reading.Weight.Should().Be(1.25m);
    }

    [Fact]
    public async Task StartContinuousReadingAsync_ShouldStartReading()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        await _service.StartContinuousReadingAsync(500);

        // Assert
        _service.IsContinuousReadingActive.Should().BeTrue();

        // Cleanup
        await _service.StopContinuousReadingAsync();
    }

    [Fact]
    public async Task StopContinuousReadingAsync_ShouldStopReading()
    {
        // Arrange
        await _service.ConnectAsync();
        await _service.StartContinuousReadingAsync(500);

        // Act
        await _service.StopContinuousReadingAsync();

        // Assert
        _service.IsContinuousReadingActive.Should().BeFalse();
    }

    #endregion

    #region Tare and Zero Tests

    [Fact]
    public async Task TareAsync_ShouldSetTareWeight()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(0.3m);

        // Act
        var result = await _service.TareAsync();

        // Assert
        result.Should().BeTrue();
        _service.CurrentTareWeight.Should().Be(0.3m);
    }

    [Fact]
    public async Task ZeroAsync_ShouldClearWeightAndTare()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(1.0m);
        await _service.SetTareWeightAsync(0.2m);

        // Act
        var result = await _service.ZeroAsync();

        // Assert
        result.Should().BeTrue();
        _service.CurrentTareWeight.Should().Be(0m);
    }

    [Fact]
    public async Task SetTareWeightAsync_ShouldSetManualTare()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        await _service.SetTareWeightAsync(0.15m);

        // Assert
        _service.CurrentTareWeight.Should().Be(0.15m);
    }

    [Fact]
    public async Task ClearTareAsync_ShouldClearTare()
    {
        // Arrange
        await _service.ConnectAsync();
        await _service.SetTareWeightAsync(0.5m);

        // Act
        await _service.ClearTareAsync();

        // Assert
        _service.CurrentTareWeight.Should().Be(0m);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task GetConfigurationsAsync_ShouldReturnConfigs()
    {
        // Act
        var configs = await _service.GetConfigurationsAsync();

        // Assert
        configs.Should().NotBeEmpty();
        configs.Should().Contain(c => c.Protocol == ScaleProtocol.Cas);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_ShouldReturnActiveConfig()
    {
        // Act
        var config = await _service.GetActiveConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SaveConfigurationAsync_NewConfig_ShouldSave()
    {
        // Arrange
        var config = new ScaleConfiguration
        {
            Name = "New Test Scale",
            ConnectionType = ScaleConnectionType.UsbHid,
            Protocol = ScaleProtocol.GenericUsbHid
        };

        // Act
        var saved = await _service.SaveConfigurationAsync(config);

        // Assert
        saved.Id.Should().BeGreaterThan(0);
        saved.Name.Should().Be("New Test Scale");

        // Verify it can be retrieved
        var configs = await _service.GetConfigurationsAsync();
        configs.Should().Contain(c => c.Name == "New Test Scale");
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldDelete()
    {
        // Arrange
        var config = new ScaleConfiguration { Name = "To Delete" };
        var saved = await _service.SaveConfigurationAsync(config);

        // Act
        var result = await _service.DeleteConfigurationAsync(saved.Id);

        // Assert
        result.Should().BeTrue();
        var configs = await _service.GetConfigurationsAsync();
        configs.Should().NotContain(c => c.Id == saved.Id);
    }

    [Fact]
    public async Task SetActiveConfigurationAsync_ShouldSetActive()
    {
        // Arrange
        var configs = await _service.GetConfigurationsAsync();
        var targetConfig = configs.First(c => !c.IsActive);

        // Act
        var result = await _service.SetActiveConfigurationAsync(targetConfig.Id);

        // Assert
        result.Should().BeTrue();
        var active = await _service.GetActiveConfigurationAsync();
        active!.Id.Should().Be(targetConfig.Id);
    }

    [Fact]
    public async Task GetAvailablePortsAsync_ShouldReturnPorts()
    {
        // Act
        var ports = await _service.GetAvailablePortsAsync();

        // Assert
        ports.Should().NotBeEmpty();
        ports.Should().Contain(p => p.PortName.StartsWith("COM"));
    }

    [Fact]
    public async Task AutoDetectScalesAsync_ShouldReturnDetected()
    {
        // Act
        var detected = await _service.AutoDetectScalesAsync();

        // Assert
        detected.Should().NotBeEmpty();
    }

    #endregion

    #region Product Configuration Tests

    [Fact]
    public async Task GetWeighedProductConfigAsync_ExistingProduct_ShouldReturnConfig()
    {
        // Act
        var config = await _service.GetWeighedProductConfigAsync(1001);

        // Assert
        config.Should().NotBeNull();
        config!.ProductId.Should().Be(1001);
        config.IsWeighed.Should().BeTrue();
        config.PricePerUnit.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetWeighedProductConfigAsync_NonExistentProduct_ShouldReturnNull()
    {
        // Act
        var config = await _service.GetWeighedProductConfigAsync(99999);

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public async Task SetWeighedProductConfigAsync_ShouldSaveConfig()
    {
        // Arrange
        var config = new WeighedProductConfig
        {
            ProductId = 2001,
            IsWeighed = true,
            PricePerUnit = 250m,
            WeightUnit = WeightUnit.Kilogram,
            DefaultTareWeight = 0.05m
        };

        // Act
        var saved = await _service.SetWeighedProductConfigAsync(config);

        // Assert
        saved.ProductId.Should().Be(2001);

        // Verify retrieval
        var retrieved = await _service.GetWeighedProductConfigAsync(2001);
        retrieved.Should().NotBeNull();
        retrieved!.PricePerUnit.Should().Be(250m);
    }

    [Fact]
    public async Task GetAllWeighedProductsAsync_ShouldReturnAll()
    {
        // Act
        var products = await _service.GetAllWeighedProductsAsync();

        // Assert
        products.Should().NotBeEmpty();
        products.Should().OnlyContain(p => p.IsWeighed);
    }

    [Fact]
    public async Task IsProductWeighedAsync_WeighedProduct_ShouldReturnTrue()
    {
        // Act
        var isWeighed = await _service.IsProductWeighedAsync(1001);

        // Assert
        isWeighed.Should().BeTrue();
    }

    [Fact]
    public async Task IsProductWeighedAsync_NonWeighedProduct_ShouldReturnFalse()
    {
        // Act
        var isWeighed = await _service.IsProductWeighedAsync(99999);

        // Assert
        isWeighed.Should().BeFalse();
    }

    #endregion

    #region Order Integration Tests

    [Fact]
    public async Task CreateWeighedOrderItemAsync_FromScale_ShouldCreateItem()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(0.75m);

        // Act
        var item = await _service.CreateWeighedOrderItemAsync(1001);

        // Assert
        item.Should().NotBeNull();
        item.ProductId.Should().Be(1001);
        item.Weight.Should().Be(0.75m);
        item.WeightUnit.Should().Be(WeightUnit.Kilogram);
        item.TotalPrice.Should().Be(0.75m * 120m); // Bananas @ 120/kg
    }

    [Fact]
    public async Task CreateWeighedOrderItemAsync_WithSpecifiedWeight_ShouldCreateItem()
    {
        // Act
        var item = await _service.CreateWeighedOrderItemAsync(1002, 1.25m, WeightUnit.Kilogram);

        // Assert
        item.Should().NotBeNull();
        item.ProductId.Should().Be(1002);
        item.Weight.Should().Be(1.25m);
        item.TotalPrice.Should().Be(1.25m * 80m); // Tomatoes @ 80/kg
    }

    [Fact]
    public async Task CreateWeighedOrderItemAsync_InvalidProduct_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateWeighedOrderItemAsync(99999, 1m, WeightUnit.Kilogram));
    }

    [Fact]
    public async Task CalculatePriceAsync_ShouldCalculateCorrectly()
    {
        // Act
        var price = await _service.CalculatePriceAsync(1001, 2.5m, WeightUnit.Kilogram);

        // Assert
        price.Should().Be(300m); // 2.5kg * 120 KSh/kg = 300
    }

    #endregion

    #region Utility Tests

    [Fact]
    public void ConvertWeight_KgToGram_ShouldConvertCorrectly()
    {
        // Act
        var result = _service.ConvertWeight(1.5m, WeightUnit.Kilogram, WeightUnit.Gram);

        // Assert
        result.Should().Be(1500m);
    }

    [Fact]
    public void ConvertWeight_GramToKg_ShouldConvertCorrectly()
    {
        // Act
        var result = _service.ConvertWeight(2500m, WeightUnit.Gram, WeightUnit.Kilogram);

        // Assert
        result.Should().Be(2.5m);
    }

    [Fact]
    public void ConvertWeight_SameUnit_ShouldReturnSameValue()
    {
        // Act
        var result = _service.ConvertWeight(1.5m, WeightUnit.Kilogram, WeightUnit.Kilogram);

        // Assert
        result.Should().Be(1.5m);
    }

    [Fact]
    public void FormatWeight_ShouldFormatCorrectly()
    {
        // Act
        var formatted = _service.FormatWeight(1.234m, WeightUnit.Kilogram, 3);

        // Assert
        formatted.Should().Be("1.234 kg");
    }

    [Fact]
    public async Task FormatReceiptLine_ShouldFormatCorrectly()
    {
        // Arrange
        var item = await _service.CreateWeighedOrderItemAsync(1001, 0.75m, WeightUnit.Kilogram);

        // Act
        var line = _service.FormatReceiptLine(item);

        // Assert
        line.Should().Contain("0.750 kg");
        line.Should().Contain("120.00/kg");
        line.Should().Contain("90.00");
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task WeightChanged_ShouldRaiseWhenWeightRead()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(1.0m);

        WeightChangedEventArgs? receivedArgs = null;
        _service.WeightChanged += (s, e) => receivedArgs = e;

        // Act
        await _service.ReadWeightAsync();

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Reading.Weight.Should().Be(1.0m);
    }

    [Fact]
    public async Task StatusChanged_ShouldRaiseOnConnect()
    {
        // Arrange
        var statusChanges = new List<ScaleStatusChangedEventArgs>();
        _service.StatusChanged += (s, e) => statusChanges.Add(e);

        // Act
        await _service.ConnectAsync();

        // Assert
        statusChanges.Should().Contain(e => e.NewStatus == ScaleStatus.Connecting);
        statusChanges.Should().Contain(e => e.NewStatus == ScaleStatus.Ready);
    }

    [Fact]
    public async Task Disconnected_ShouldRaiseOnDisconnect()
    {
        // Arrange
        await _service.ConnectAsync();

        var disconnectedRaised = false;
        _service.Disconnected += (s, e) => disconnectedRaised = true;

        // Act
        await _service.DisconnectAsync();

        // Assert
        disconnectedRaised.Should().BeTrue();
    }

    #endregion

    #region Simulation Tests

    [Fact]
    public async Task SimulatePlaceItem_ShouldUpdateWeight()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        _service.SimulatePlaceItem(2.5m);

        // Assert
        var reading = await _service.ReadWeightAsync();
        reading.Weight.Should().Be(2.5m);
    }

    [Fact]
    public async Task SimulateRemoveItem_ShouldClearWeight()
    {
        // Arrange
        await _service.ConnectAsync();
        _service.SetSimulatedWeight(1.5m);

        // Act
        _service.SimulateRemoveItem();
        var reading = await _service.ReadWeightAsync();

        // Assert
        reading.Weight.Should().Be(0m);
    }

    #endregion

    #region Weight Conversion Static Tests

    [Theory]
    [InlineData(1.0, WeightUnit.Kilogram, WeightUnit.Gram, 1000)]
    [InlineData(500, WeightUnit.Gram, WeightUnit.Kilogram, 0.5)]
    [InlineData(1.0, WeightUnit.Pound, WeightUnit.Kilogram, 0.453592)]
    public void WeightConversion_Convert_ShouldConvertCorrectly(
        decimal input, WeightUnit from, WeightUnit to, decimal expected)
    {
        // Act
        var result = WeightConversion.Convert(input, from, to);

        // Assert
        result.Should().BeApproximately(expected, 0.001m);
    }

    [Theory]
    [InlineData("kg", WeightUnit.Kilogram)]
    [InlineData("g", WeightUnit.Gram)]
    [InlineData("lb", WeightUnit.Pound)]
    [InlineData("oz", WeightUnit.Ounce)]
    [InlineData("KILOGRAM", WeightUnit.Kilogram)]
    [InlineData("", WeightUnit.Kilogram)]
    [InlineData(null, WeightUnit.Kilogram)]
    public void WeightConversion_ParseUnit_ShouldParseCorrectly(string? input, WeightUnit expected)
    {
        // Act
        var result = WeightConversion.ParseUnit(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(WeightUnit.Kilogram, "kg")]
    [InlineData(WeightUnit.Gram, "g")]
    [InlineData(WeightUnit.Pound, "lb")]
    [InlineData(WeightUnit.Ounce, "oz")]
    public void WeightConversion_GetSymbol_ShouldReturnCorrectSymbol(WeightUnit unit, string expected)
    {
        // Act
        var result = WeightConversion.GetSymbol(unit);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
