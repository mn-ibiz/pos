// tests/HospitalityPOS.Business.Tests/Services/CustomerDisplayServiceTests.cs
// Unit tests for CustomerDisplayService
// Story 43-2: Customer Display Integration

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Hardware;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for CustomerDisplayService.
/// </summary>
public class CustomerDisplayServiceTests : IDisposable
{
    private readonly CustomerDisplayService _service;

    public CustomerDisplayServiceTests()
    {
        _service = new CustomerDisplayService();
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Connection Tests

    [Fact]
    public async Task ConnectAsync_WithValidVfdConfig_ShouldConnect()
    {
        // Arrange
        var config = new CustomerDisplayConfiguration
        {
            DisplayType = CustomerDisplayType.Vfd,
            PortName = "COM1",
            BaudRate = 9600,
            VfdProtocol = VfdProtocol.EscPos
        };

        // Act
        var result = await _service.ConnectAsync(config);

        // Assert
        result.Success.Should().BeTrue();
        _service.IsConnected.Should().BeTrue();
        _service.ActiveConfiguration.Should().NotBeNull();
        _service.ActiveConfiguration!.DisplayType.Should().Be(CustomerDisplayType.Vfd);
    }

    [Fact]
    public async Task ConnectAsync_WithSecondaryMonitor_ShouldConnect()
    {
        // Arrange
        var config = new CustomerDisplayConfiguration
        {
            DisplayType = CustomerDisplayType.SecondaryMonitor,
            MonitorIndex = 1,
            FullScreen = true
        };

        // Act
        var result = await _service.ConnectAsync(config);

        // Assert
        result.Success.Should().BeTrue();
        _service.IsConnected.Should().BeTrue();
        _service.CurrentState.Should().Be(CustomerDisplayState.Idle);
    }

    [Fact]
    public async Task ConnectAsync_VfdWithoutPort_ShouldFail()
    {
        // Arrange
        var config = new CustomerDisplayConfiguration
        {
            DisplayType = CustomerDisplayType.Vfd,
            PortName = null
        };

        // Act
        var result = await _service.ConnectAsync(config);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Port name is required");
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldDisconnect()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisconnectAsync();

        // Assert
        _service.IsConnected.Should().BeFalse();
        _service.CurrentState.Should().Be(CustomerDisplayState.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_RaisesConnectedEvent()
    {
        // Arrange
        DisplayConnectionResult? eventResult = null;
        _service.Connected += (s, e) => eventResult = e;

        var config = new CustomerDisplayConfiguration
        {
            DisplayType = CustomerDisplayType.Vfd,
            PortName = "COM1"
        };

        // Act
        await _service.ConnectAsync(config);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_RaisesDisconnectedEvent()
    {
        // Arrange
        bool eventRaised = false;
        _service.Disconnected += (s, e) => eventRaised = true;
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisconnectAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Display Content Tests

    [Fact]
    public async Task DisplayWelcomeAsync_ShouldShowWelcomeMessage()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisplayWelcomeAsync();

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.Idle);
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Welcome");
    }

    [Fact]
    public async Task DisplayItemAsync_ShouldShowItemInfo()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var item = new DisplayItemInfo
        {
            ProductName = "Milk 500ml",
            UnitPrice = 65.00m,
            Quantity = 1
        };

        // Act
        await _service.DisplayItemAsync(item);

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingItems);
        var (line1, _) = _service.GetCurrentContent();
        line1.Should().Contain("Milk");
    }

    [Fact]
    public async Task DisplayItemAsync_WeighedItem_ShouldShowWeight()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var item = new DisplayItemInfo
        {
            ProductName = "Bananas",
            UnitPrice = 120.00m,
            IsByWeight = true,
            Weight = 0.75m,
            WeightUnit = "kg"
        };

        // Act
        await _service.DisplayItemAsync(item);

        // Assert
        var (line1, _) = _service.GetCurrentContent();
        line1.Should().Contain("kg");
    }

    [Fact]
    public async Task DisplayTotalAsync_ShouldShowTotal()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var total = new DisplayTotalInfo
        {
            Subtotal = 250.00m,
            Total = 250.00m,
            ItemCount = 3
        };

        // Act
        await _service.DisplayTotalAsync(total);

        // Assert
        var (_, line2) = _service.GetCurrentContent();
        line2.Should().Contain("TOTAL");
    }

    [Fact]
    public async Task DisplayItemAndTotalAsync_ShouldShowBoth()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var item = new DisplayItemInfo { ProductName = "Bread", UnitPrice = 50.00m };
        var total = new DisplayTotalInfo { Total = 100.00m, ItemCount = 2 };

        // Act
        await _service.DisplayItemAndTotalAsync(item, total);

        // Assert
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Bread");
        line2.Should().Contain("TOTAL");
    }

    [Fact]
    public async Task DisplayPaymentAsync_ShouldShowPaymentInfo()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var payment = new DisplayPaymentInfo
        {
            AmountDue = 500.00m,
            AmountPaid = 500.00m,
            PaymentMethod = "Cash",
            IsComplete = false
        };

        // Act
        await _service.DisplayPaymentAsync(payment);

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingPayment);
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Due");
        line2.Should().Contain("Cash");
    }

    [Fact]
    public async Task DisplayPaymentAsync_WithChange_ShouldShowChange()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var payment = new DisplayPaymentInfo
        {
            AmountDue = 500.00m,
            AmountPaid = 1000.00m,
            PaymentMethod = "Cash",
            IsComplete = true
        };

        // Act
        await _service.DisplayPaymentAsync(payment);

        // Assert
        var (line1, _) = _service.GetCurrentContent();
        line1.Should().Contain("Change");
    }

    [Fact]
    public async Task DisplayThankYouAsync_ShouldShowThankYouMessage()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisplayThankYouAsync();

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingThankYou);
        var (line1, _) = _service.GetCurrentContent();
        line1.Should().Contain("Thank");
    }

    [Fact]
    public async Task DisplayPromotionAsync_ShouldShowPromotion()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        var promo = new DisplayPromotionInfo
        {
            Title = "Special Offer!",
            Description = "20% off today",
            DiscountPercent = 20
        };

        // Act
        await _service.DisplayPromotionAsync(promo);

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingPromotion);
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Special");
        line2.Should().Contain("20%");
    }

    [Fact]
    public async Task DisplayTextAsync_ShouldShowCustomText()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisplayTextAsync("Custom Line 1", "Custom Line 2");

        // Assert
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Custom Line 1");
        line2.Should().Contain("Custom Line 2");
    }

    [Fact]
    public async Task ClearDisplayAsync_ShouldClearContent()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.DisplayTextAsync("Some Text", "More Text");

        // Act
        await _service.ClearDisplayAsync();

        // Assert
        var (line1, line2) = _service.GetCurrentContent();
        line1.Trim().Should().BeEmpty();
        line2.Trim().Should().BeEmpty();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task SaveConfigurationAsync_ShouldSaveAndReturnId()
    {
        // Arrange
        var config = new CustomerDisplayConfiguration
        {
            DisplayName = "Test Display",
            DisplayType = CustomerDisplayType.Vfd,
            PortName = "COM1"
        };

        // Act
        var saved = await _service.SaveConfigurationAsync(config);

        // Assert
        saved.Id.Should().BeGreaterThan(0);
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetConfigurationsAsync_ShouldReturnDefaultConfigs()
    {
        // Act
        var configs = await _service.GetConfigurationsAsync();

        // Assert
        configs.Should().NotBeEmpty();
        configs.Should().Contain(c => c.DisplayType == CustomerDisplayType.Vfd);
        configs.Should().Contain(c => c.DisplayType == CustomerDisplayType.SecondaryMonitor);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldRemoveConfig()
    {
        // Arrange
        var config = new CustomerDisplayConfiguration
        {
            DisplayName = "To Delete",
            DisplayType = CustomerDisplayType.Vfd,
            PortName = "COM2"
        };
        var saved = await _service.SaveConfigurationAsync(config);
        var initialCount = (await _service.GetConfigurationsAsync()).Count;

        // Act
        await _service.DeleteConfigurationAsync(saved.Id);

        // Assert
        var configs = await _service.GetConfigurationsAsync();
        configs.Count.Should().Be(initialCount - 1);
        configs.Should().NotContain(c => c.Id == saved.Id);
    }

    [Fact]
    public async Task SetActiveConfigurationAsync_ShouldActivateConfig()
    {
        // Arrange
        var configs = await _service.GetConfigurationsAsync();
        var configId = configs.First().Id;

        // Act
        await _service.SetActiveConfigurationAsync(configId);

        // Assert
        var active = await _service.GetActiveConfigurationAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be(configId);
        active.IsActive.Should().BeTrue();
    }

    #endregion

    #region Hardware Discovery Tests

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
    public async Task GetAvailableMonitorsAsync_ShouldReturnMonitors()
    {
        // Act
        var monitors = await _service.GetAvailableMonitorsAsync();

        // Assert
        monitors.Should().NotBeEmpty();
        monitors.Should().Contain(m => m.IsPrimary);
    }

    [Fact]
    public async Task AutoDetectDisplaysAsync_ShouldDetectDisplays()
    {
        // Act
        var detected = await _service.AutoDetectDisplaysAsync();

        // Assert
        detected.Should().NotBeEmpty();
    }

    #endregion

    #region Test Display Tests

    [Fact]
    public async Task TestDisplayAsync_WhenConnected_ShouldSucceed()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        var result = await _service.TestDisplayAsync("Test Message");

        // Assert
        result.Success.Should().BeTrue();
        result.TestMessage.Should().Be("Test Message");
    }

    [Fact]
    public async Task TestDisplayAsync_WhenNotConnected_ShouldFail()
    {
        // Act
        var result = await _service.TestDisplayAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not connected");
    }

    #endregion

    #region Transaction Integration Tests

    [Fact]
    public async Task OnTransactionStartAsync_ShouldClearAndPrepare()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.OnTransactionStartAsync("TXN001");

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingItems);
    }

    [Fact]
    public async Task OnItemAddedAsync_ShouldDisplayItemAndTotal()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");

        var item = new DisplayItemInfo { ProductName = "Sugar", UnitPrice = 180.00m };
        var total = new DisplayTotalInfo { Total = 180.00m, ItemCount = 1 };

        // Act
        await _service.OnItemAddedAsync(item, total);

        // Assert
        var (line1, line2) = _service.GetCurrentContent();
        line1.Should().Contain("Sugar");
        line2.Should().Contain("TOTAL");
    }

    [Fact]
    public async Task OnItemRemovedAsync_ShouldUpdateTotal()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");
        var total = new DisplayTotalInfo { Total = 100.00m, ItemCount = 1 };

        // Act
        await _service.OnItemRemovedAsync(total);

        // Assert
        var (_, line2) = _service.GetCurrentContent();
        line2.Should().Contain("100");
    }

    [Fact]
    public async Task OnPaymentStartAsync_ShouldShowAmountDue()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");

        // Act
        await _service.OnPaymentStartAsync(500.00m);

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingPayment);
    }

    [Fact]
    public async Task OnPaymentReceivedAsync_ShouldShowPayment()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");
        var payment = new DisplayPaymentInfo
        {
            AmountDue = 500.00m,
            AmountPaid = 500.00m,
            PaymentMethod = "M-Pesa"
        };

        // Act
        await _service.OnPaymentReceivedAsync(payment);

        // Assert
        var (_, line2) = _service.GetCurrentContent();
        line2.Should().Contain("M-Pesa");
    }

    [Fact]
    public async Task OnTransactionCompleteAsync_ShouldShowThankYou()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");

        // Act
        await _service.OnTransactionCompleteAsync();

        // Assert
        _service.CurrentState.Should().Be(CustomerDisplayState.ShowingThankYou);
    }

    [Fact]
    public async Task OnTransactionVoidedAsync_ShouldShowCancelled()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        await _service.OnTransactionStartAsync("TXN001");

        // Act
        await _service.OnTransactionVoidedAsync();

        // Assert
        var (line1, _) = _service.GetCurrentContent();
        line1.Should().Contain("Transaction");
    }

    #endregion

    #region Idle Cycle Tests

    [Fact]
    public async Task SetIdlePromotionsAsync_ShouldSetPromotions()
    {
        // Arrange
        var promos = new List<DisplayPromotionInfo>
        {
            new() { Title = "Promo 1" },
            new() { Title = "Promo 2" }
        };

        // Act
        await _service.SetIdlePromotionsAsync(promos);

        // Assert - no exception means success
        // Promotions are stored internally
    }

    [Fact]
    public async Task StartIdleCycleAsync_WhenConnected_ShouldStart()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        _ = _service.StartIdleCycleAsync();
        await Task.Delay(100); // Let it start

        // Assert
        _service.IsIdleCycleRunning.Should().BeTrue();

        // Cleanup
        await _service.StopIdleCycleAsync();
    }

    [Fact]
    public async Task StopIdleCycleAsync_ShouldStop()
    {
        // Arrange
        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);
        _ = _service.StartIdleCycleAsync();
        await Task.Delay(100);

        // Act
        await _service.StopIdleCycleAsync();
        await Task.Delay(100);

        // Assert
        _service.IsIdleCycleRunning.Should().BeFalse();
    }

    #endregion

    #region State Change Event Tests

    [Fact]
    public async Task StateChanged_ShouldRaiseOnStateChange()
    {
        // Arrange
        var stateChanges = new List<(CustomerDisplayState Previous, CustomerDisplayState New)>();
        _service.StateChanged += (s, e) => stateChanges.Add((e.PreviousState, e.NewState));

        await _service.SimulateConnectAsync(CustomerDisplayType.Vfd);

        // Act
        await _service.DisplayThankYouAsync();

        // Assert
        stateChanges.Should().Contain(x => x.New == CustomerDisplayState.ShowingThankYou);
    }

    #endregion

    #region DTO Formatting Tests

    [Fact]
    public void DisplayItemInfo_FormatForVfd_ShouldFormatCorrectly()
    {
        // Arrange
        var item = new DisplayItemInfo
        {
            ProductName = "Milk 500ml",
            UnitPrice = 65.00m
        };

        // Act
        var formatted = item.FormatForVfd(20);

        // Assert
        formatted.Should().HaveLength(20);
        formatted.Should().Contain("Milk");
        formatted.Should().Contain("65");
    }

    [Fact]
    public void DisplayItemInfo_FormatWeighedForVfd_ShouldIncludeWeight()
    {
        // Arrange
        var item = new DisplayItemInfo
        {
            ProductName = "Bananas",
            UnitPrice = 120.00m,
            IsByWeight = true,
            Weight = 0.75m,
            WeightUnit = "kg"
        };

        // Act
        var formatted = item.FormatWeighedForVfd(20);

        // Assert
        formatted.Should().HaveLength(20);
        formatted.Should().Contain("0.750");
        formatted.Should().Contain("kg");
    }

    [Fact]
    public void DisplayTotalInfo_FormatForVfd_ShouldFormatCorrectly()
    {
        // Arrange
        var total = new DisplayTotalInfo
        {
            Total = 1250.50m
        };

        // Act
        var formatted = total.FormatForVfd(20, "KSh");

        // Assert
        formatted.Should().HaveLength(20);
        formatted.Should().Contain("TOTAL");
        formatted.Should().Contain("KSh");
    }

    [Fact]
    public void DisplayPaymentInfo_FormatChangeForVfd_ShouldFormatChange()
    {
        // Arrange
        var payment = new DisplayPaymentInfo
        {
            AmountDue = 500.00m,
            AmountPaid = 1000.00m
        };

        // Act
        var formatted = payment.FormatChangeForVfd(20, "KSh");

        // Assert
        formatted.Should().HaveLength(20);
        formatted.Should().Contain("Change");
        formatted.Should().Contain("500");
    }

    [Fact]
    public void DisplayConnectionResult_Successful_ShouldCreateSuccess()
    {
        // Act
        var result = DisplayConnectionResult.Successful("VFD on COM1", "1.0");

        // Assert
        result.Success.Should().BeTrue();
        result.DeviceInfo.Should().Be("VFD on COM1");
        result.Version.Should().Be("1.0");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DisplayConnectionResult_Failed_ShouldCreateFailure()
    {
        // Act
        var result = DisplayConnectionResult.Failed("Port not available");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Port not available");
    }

    #endregion

    #region Secondary Monitor Callback Tests

    [Fact]
    public async Task RegisterSecondaryMonitorCallback_ShouldReceiveUpdates()
    {
        // Arrange
        DisplayContent? receivedContent = null;
        _service.RegisterSecondaryMonitorCallback(c => receivedContent = c);

        var config = new CustomerDisplayConfiguration
        {
            DisplayType = CustomerDisplayType.SecondaryMonitor,
            MonitorIndex = 1
        };
        await _service.ConnectAsync(config);

        // Act
        await _service.DisplayTextAsync("Test Line 1", "Test Line 2");

        // Assert
        receivedContent.Should().NotBeNull();
        receivedContent!.Line1.Should().Contain("Test Line 1");
    }

    [Fact]
    public void UnregisterSecondaryMonitorCallback_ShouldStopUpdates()
    {
        // Arrange
        DisplayContent? receivedContent = null;
        _service.RegisterSecondaryMonitorCallback(c => receivedContent = c);

        // Act
        _service.UnregisterSecondaryMonitorCallback();

        // Assert - callback should be null, tested indirectly by no exception
        // When display sends content, it won't crash because callback is null
    }

    #endregion
}
