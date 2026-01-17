// src/HospitalityPOS.Infrastructure/Services/CustomerDisplayService.cs
// Implementation of customer display service for VFD and secondary monitors
// Story 43-2: Customer Display Integration

using System.Text;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Hardware;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing customer-facing displays including VFD pole displays
/// and secondary monitor displays.
/// </summary>
public class CustomerDisplayService : ICustomerDisplayService, IDisposable
{
    #region Fields

    private readonly Dictionary<int, CustomerDisplayConfiguration> _configurations = new();
    private readonly List<DisplayPromotionInfo> _idlePromotions = new();
    private CustomerDisplayConfiguration? _activeConfiguration;
    private CustomerDisplayState _currentState = CustomerDisplayState.Disconnected;
    private bool _isConnected;
    private bool _isIdleCycleRunning;
    private CancellationTokenSource? _idleCycleCts;
    private int _nextConfigId = 1;
    private string? _currentTransactionId;

    // Simulated display content for testing
    private string _displayLine1 = string.Empty;
    private string _displayLine2 = string.Empty;

    // For secondary monitor integration (set by WPF layer)
    private Action<DisplayContent>? _secondaryMonitorCallback;

    #endregion

    #region Properties

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public CustomerDisplayState CurrentState => _currentState;

    /// <inheritdoc />
    public CustomerDisplayConfiguration? ActiveConfiguration => _activeConfiguration;

    /// <inheritdoc />
    public bool IsIdleCycleRunning => _isIdleCycleRunning;

    /// <summary>
    /// Gets the current display line 1 content (for testing).
    /// </summary>
    public string DisplayLine1 => _displayLine1;

    /// <summary>
    /// Gets the current display line 2 content (for testing).
    /// </summary>
    public string DisplayLine2 => _displayLine2;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the CustomerDisplayService.
    /// </summary>
    public CustomerDisplayService()
    {
        // Add default configurations
        AddDefaultConfigurations();
    }

    private void AddDefaultConfigurations()
    {
        // Default VFD configuration (ESC/POS)
        var vfdConfig = new CustomerDisplayConfiguration
        {
            Id = _nextConfigId++,
            DisplayType = CustomerDisplayType.Vfd,
            DisplayName = "VFD Pole Display",
            PortName = "COM1",
            BaudRate = 9600,
            VfdProtocol = VfdProtocol.EscPos,
            CharactersPerLine = 20,
            NumberOfLines = 2,
            WelcomeMessage = "Welcome!",
            WelcomeMessageLine2 = "We appreciate your business",
            ThankYouMessage = "Thank You!",
            ThankYouMessageLine2 = "Please come again",
            IsActive = false
        };
        _configurations[vfdConfig.Id] = vfdConfig;

        // Default secondary monitor configuration
        var monitorConfig = new CustomerDisplayConfiguration
        {
            Id = _nextConfigId++,
            DisplayType = CustomerDisplayType.SecondaryMonitor,
            DisplayName = "Secondary Monitor Display",
            MonitorIndex = 1,
            FullScreen = true,
            ShowLogo = true,
            ShowPromotions = true,
            ItemDisplaySeconds = 3,
            IdleTimeSeconds = 10,
            IsActive = false
        };
        _configurations[monitorConfig.Id] = monitorConfig;

        // Add sample promotions
        _idlePromotions.AddRange(new[]
        {
            new DisplayPromotionInfo
            {
                Title = "Special Offer!",
                Description = "10% off on fresh produce",
                DiscountPercent = 10,
                DisplayDurationSeconds = 5
            },
            new DisplayPromotionInfo
            {
                Title = "Loyalty Points",
                Description = "Earn 2x points today!",
                DisplayDurationSeconds = 5
            },
            new DisplayPromotionInfo
            {
                Title = "New Arrivals",
                Description = "Check our new products",
                DisplayDurationSeconds = 5
            }
        });
    }

    #endregion

    #region Connection Management

    /// <inheritdoc />
    public async Task<DisplayConnectionResult> ConnectAsync(CustomerDisplayConfiguration configuration)
    {
        try
        {
            if (_isConnected)
            {
                await DisconnectAsync();
            }

            // Validate configuration
            if (configuration.DisplayType == CustomerDisplayType.Vfd &&
                string.IsNullOrEmpty(configuration.PortName))
            {
                return DisplayConnectionResult.Failed("Port name is required for VFD display");
            }

            // Simulate connection based on display type
            switch (configuration.DisplayType)
            {
                case CustomerDisplayType.Vfd:
                    // In production: Open serial port
                    // await OpenSerialPortAsync(configuration);
                    break;

                case CustomerDisplayType.SecondaryMonitor:
                    // In production: Open full-screen window on secondary monitor
                    // Will be handled by WPF layer
                    break;

                case CustomerDisplayType.Tablet:
                case CustomerDisplayType.NetworkDisplay:
                    // Network-based connection
                    if (string.IsNullOrEmpty(configuration.IpAddress))
                    {
                        return DisplayConnectionResult.Failed("IP address is required for network display");
                    }
                    break;
            }

            _activeConfiguration = configuration;
            _isConnected = true;
            SetState(CustomerDisplayState.Connected);

            var result = DisplayConnectionResult.Successful(
                GetDeviceInfo(configuration),
                "1.0.0"
            );

            Connected?.Invoke(this, result);

            // Show welcome screen
            await DisplayWelcomeAsync();

            return result;
        }
        catch (Exception ex)
        {
            var result = DisplayConnectionResult.Failed(ex.Message);
            Error?.Invoke(this, new DisplayErrorEventArgs
            {
                ErrorMessage = ex.Message,
                Exception = ex,
                IsFatal = true
            });
            return result;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        if (!_isConnected) return Task.CompletedTask;

        StopIdleCycleAsync();

        // In production: Close serial port or secondary window
        _isConnected = false;
        _activeConfiguration = null;
        SetState(CustomerDisplayState.Disconnected);

        Disconnected?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<DisplayTestResult> TestDisplayAsync(string? testMessage = null)
    {
        if (!_isConnected)
        {
            return new DisplayTestResult
            {
                Success = false,
                ErrorMessage = "Display not connected"
            };
        }

        try
        {
            var message = testMessage ?? "Display Test OK!";
            await DisplayTextAsync(message, DateTime.Now.ToString("HH:mm:ss"));

            return new DisplayTestResult
            {
                Success = true,
                TestMessage = message
            };
        }
        catch (Exception ex)
        {
            return new DisplayTestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CustomerDisplayConfiguration>> AutoDetectDisplaysAsync()
    {
        var detected = new List<CustomerDisplayConfiguration>();

        // Auto-detect VFD on serial ports
        var ports = GetSerialPorts();
        foreach (var port in ports)
        {
            detected.Add(new CustomerDisplayConfiguration
            {
                Id = _nextConfigId++,
                DisplayType = CustomerDisplayType.Vfd,
                DisplayName = $"VFD on {port}",
                PortName = port,
                VfdProtocol = VfdProtocol.EscPos,
                IsActive = false
            });
        }

        // Auto-detect secondary monitors
        var monitors = GetMonitors();
        foreach (var monitor in monitors.Where(m => !m.IsPrimary))
        {
            detected.Add(new CustomerDisplayConfiguration
            {
                Id = _nextConfigId++,
                DisplayType = CustomerDisplayType.SecondaryMonitor,
                DisplayName = $"Monitor: {monitor.Name}",
                MonitorIndex = monitor.Index,
                FullScreen = true,
                IsActive = false
            });
        }

        return Task.FromResult<IReadOnlyList<CustomerDisplayConfiguration>>(detected);
    }

    #endregion

    #region Display Content

    /// <inheritdoc />
    public Task ClearDisplayAsync()
    {
        if (!_isConnected) return Task.CompletedTask;

        _displayLine1 = string.Empty.PadRight(_activeConfiguration?.CharactersPerLine ?? 20);
        _displayLine2 = string.Empty.PadRight(_activeConfiguration?.CharactersPerLine ?? 20);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.Connected
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayWelcomeAsync()
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        _displayLine1 = CenterText(config.WelcomeMessage, lineWidth);
        _displayLine2 = CenterText(config.WelcomeMessageLine2, lineWidth);

        SetState(CustomerDisplayState.Idle);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.Idle
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayItemAsync(DisplayItemInfo item)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        if (item.IsByWeight && item.Weight.HasValue)
        {
            _displayLine1 = item.FormatWeighedForVfd(lineWidth);
        }
        else
        {
            _displayLine1 = item.FormatForVfd(lineWidth);
        }

        SetState(CustomerDisplayState.ShowingItems);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.ShowingItems,
            ItemInfo = item
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayTotalAsync(DisplayTotalInfo total)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        _displayLine2 = total.FormatForVfd(config.CharactersPerLine, config.CurrencySymbol);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = _currentState,
            TotalInfo = total
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayItemAndTotalAsync(DisplayItemInfo item, DisplayTotalInfo total)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        // Line 1: Item
        if (item.IsByWeight && item.Weight.HasValue)
        {
            _displayLine1 = item.FormatWeighedForVfd(lineWidth);
        }
        else
        {
            _displayLine1 = item.FormatForVfd(lineWidth);
        }

        // Line 2: Total
        _displayLine2 = total.FormatForVfd(lineWidth, config.CurrencySymbol);

        SetState(CustomerDisplayState.ShowingItems);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.ShowingItems,
            ItemInfo = item,
            TotalInfo = total
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayPaymentAsync(DisplayPaymentInfo payment)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        if (payment.IsComplete && payment.Change > 0)
        {
            // Show change
            _displayLine1 = payment.FormatChangeForVfd(lineWidth, config.CurrencySymbol);
            _displayLine2 = CenterText(config.ThankYouMessage, lineWidth);
        }
        else
        {
            // Show amount due and payment
            _displayLine1 = payment.FormatAmountDueForVfd(lineWidth, config.CurrencySymbol);
            _displayLine2 = payment.FormatPaymentForVfd(lineWidth, config.CurrencySymbol);
        }

        SetState(CustomerDisplayState.ShowingPayment);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.ShowingPayment,
            PaymentInfo = payment
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayThankYouAsync()
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        _displayLine1 = CenterText(config.ThankYouMessage, lineWidth);
        _displayLine2 = CenterText(config.ThankYouMessageLine2, lineWidth);

        SetState(CustomerDisplayState.ShowingThankYou);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.ShowingThankYou
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayPromotionAsync(DisplayPromotionInfo promotion)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        _displayLine1 = promotion.FormatLine1ForVfd(lineWidth);
        _displayLine2 = promotion.FormatLine2ForVfd(lineWidth);

        SetState(CustomerDisplayState.ShowingPromotion);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = CustomerDisplayState.ShowingPromotion,
            PromotionInfo = promotion
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisplayTextAsync(string line1, string? line2 = null)
    {
        if (!_isConnected) return Task.CompletedTask;

        var config = _activeConfiguration!;
        var lineWidth = config.CharactersPerLine;

        _displayLine1 = TruncateOrPad(line1, lineWidth);
        _displayLine2 = TruncateOrPad(line2 ?? string.Empty, lineWidth);

        SendToDisplay(new DisplayContent
        {
            Line1 = _displayLine1,
            Line2 = _displayLine2,
            ContentType = _currentState
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetBrightnessAsync(int level)
    {
        if (!_isConnected || _activeConfiguration?.DisplayType != CustomerDisplayType.Vfd)
        {
            return Task.CompletedTask;
        }

        // In production: Send brightness command to VFD
        // byte[] command = level switch
        // {
        //     >= 4 => VfdCommands.BrightnessHigh,
        //     >= 2 => VfdCommands.BrightnessMedium,
        //     _ => VfdCommands.BrightnessLow
        // };
        // await WriteToPortAsync(command);

        return Task.CompletedTask;
    }

    #endregion

    #region Configuration Management

    /// <inheritdoc />
    public Task<CustomerDisplayConfiguration> SaveConfigurationAsync(CustomerDisplayConfiguration configuration)
    {
        if (configuration.Id == 0)
        {
            configuration.Id = _nextConfigId++;
            configuration.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            configuration.UpdatedAt = DateTime.UtcNow;
        }

        _configurations[configuration.Id] = configuration;

        return Task.FromResult(configuration);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CustomerDisplayConfiguration>> GetConfigurationsAsync()
    {
        return Task.FromResult<IReadOnlyList<CustomerDisplayConfiguration>>(_configurations.Values.ToList());
    }

    /// <inheritdoc />
    public Task<CustomerDisplayConfiguration?> GetActiveConfigurationAsync()
    {
        var active = _configurations.Values.FirstOrDefault(c => c.IsActive);
        return Task.FromResult(active);
    }

    /// <inheritdoc />
    public Task DeleteConfigurationAsync(int configurationId)
    {
        if (_configurations.TryGetValue(configurationId, out var config))
        {
            if (_activeConfiguration?.Id == configurationId)
            {
                DisconnectAsync();
            }
            _configurations.Remove(configurationId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetActiveConfigurationAsync(int configurationId)
    {
        // Deactivate all others
        foreach (var config in _configurations.Values)
        {
            config.IsActive = false;
        }

        // Activate selected
        if (_configurations.TryGetValue(configurationId, out var selected))
        {
            selected.IsActive = true;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Hardware Discovery

    /// <inheritdoc />
    public Task<IReadOnlyList<DisplayPortInfo>> GetAvailablePortsAsync()
    {
        var ports = GetSerialPorts().Select(p => new DisplayPortInfo
        {
            PortName = p,
            Description = $"Serial Port {p}",
            InUse = _activeConfiguration?.PortName == p && _isConnected
        }).ToList();

        return Task.FromResult<IReadOnlyList<DisplayPortInfo>>(ports);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MonitorInfo>> GetAvailableMonitorsAsync()
    {
        return Task.FromResult<IReadOnlyList<MonitorInfo>>(GetMonitors());
    }

    #endregion

    #region Idle/Promotion Management

    /// <inheritdoc />
    public Task SetIdlePromotionsAsync(IEnumerable<DisplayPromotionInfo> promotions)
    {
        _idlePromotions.Clear();
        _idlePromotions.AddRange(promotions);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartIdleCycleAsync()
    {
        if (_isIdleCycleRunning || !_isConnected || _idlePromotions.Count == 0)
        {
            return;
        }

        _isIdleCycleRunning = true;
        _idleCycleCts = new CancellationTokenSource();

        try
        {
            var index = 0;
            while (!_idleCycleCts.Token.IsCancellationRequested)
            {
                // Only show promotions when idle (not during transaction)
                if (_currentState == CustomerDisplayState.Idle ||
                    _currentState == CustomerDisplayState.ShowingPromotion)
                {
                    var promo = _idlePromotions[index];
                    await DisplayPromotionAsync(promo);

                    await Task.Delay(
                        TimeSpan.FromSeconds(promo.DisplayDurationSeconds),
                        _idleCycleCts.Token
                    );

                    index = (index + 1) % _idlePromotions.Count;

                    // Show welcome between promotions
                    await DisplayWelcomeAsync();
                    await Task.Delay(
                        TimeSpan.FromSeconds(3),
                        _idleCycleCts.Token
                    );
                }
                else
                {
                    // Wait before checking again
                    await Task.Delay(1000, _idleCycleCts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        finally
        {
            _isIdleCycleRunning = false;
        }
    }

    /// <inheritdoc />
    public Task StopIdleCycleAsync()
    {
        _idleCycleCts?.Cancel();
        _isIdleCycleRunning = false;
        return Task.CompletedTask;
    }

    #endregion

    #region Transaction Integration

    /// <inheritdoc />
    public async Task OnTransactionStartAsync(string transactionId)
    {
        _currentTransactionId = transactionId;

        // Stop idle cycle during transaction
        await StopIdleCycleAsync();

        // Clear display for new transaction
        await ClearDisplayAsync();

        SetState(CustomerDisplayState.ShowingItems);
    }

    /// <inheritdoc />
    public async Task OnItemAddedAsync(DisplayItemInfo item, DisplayTotalInfo runningTotal)
    {
        await DisplayItemAndTotalAsync(item, runningTotal);
    }

    /// <inheritdoc />
    public async Task OnItemRemovedAsync(DisplayTotalInfo runningTotal)
    {
        await DisplayTotalAsync(runningTotal);
    }

    /// <inheritdoc />
    public async Task OnPaymentStartAsync(decimal amountDue)
    {
        var payment = new DisplayPaymentInfo
        {
            AmountDue = amountDue,
            AmountPaid = 0,
            IsComplete = false
        };
        await DisplayPaymentAsync(payment);
    }

    /// <inheritdoc />
    public async Task OnPaymentReceivedAsync(DisplayPaymentInfo payment)
    {
        await DisplayPaymentAsync(payment);
    }

    /// <inheritdoc />
    public async Task OnTransactionCompleteAsync()
    {
        _currentTransactionId = null;

        await DisplayThankYouAsync();

        // Wait then return to idle (fire-and-forget with proper exception handling)
        _ = ReturnToIdleAfterDelayAsync(_activeConfiguration?.IdleTimeSeconds ?? 10, showPromotions: _activeConfiguration?.ShowPromotions == true);
    }

    /// <summary>
    /// Returns display to idle state after a delay with proper exception handling.
    /// </summary>
    private async Task ReturnToIdleAfterDelayAsync(int delaySeconds, bool showPromotions)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (_isConnected && _currentTransactionId == null)
            {
                await DisplayWelcomeAsync();
                if (showPromotions)
                {
                    await StartIdleCycleAsync();
                }
            }
        }
        catch (Exception)
        {
            // Silently ignore errors during idle transition - non-critical
        }
    }

    /// <inheritdoc />
    public async Task OnTransactionVoidedAsync()
    {
        _currentTransactionId = null;

        await DisplayTextAsync("Transaction", "Cancelled");

        // Wait then return to idle (fire-and-forget with proper exception handling)
        _ = ReturnToWelcomeAfterDelayAsync(3);
    }

    /// <summary>
    /// Returns display to welcome state after a delay with proper exception handling.
    /// </summary>
    private async Task ReturnToWelcomeAfterDelayAsync(int delaySeconds)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (_isConnected && _currentTransactionId == null)
            {
                await DisplayWelcomeAsync();
            }
        }
        catch (Exception)
        {
            // Silently ignore errors during welcome transition - non-critical
        }
    }

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<DisplayStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<DisplayConnectionResult>? Connected;

    /// <inheritdoc />
    public event EventHandler? Disconnected;

    /// <inheritdoc />
    public event EventHandler<DisplayErrorEventArgs>? Error;

    #endregion

    #region Secondary Monitor Integration

    /// <summary>
    /// Registers a callback for secondary monitor display updates.
    /// Used by WPF layer to receive display content.
    /// </summary>
    /// <param name="callback">Callback to invoke with display content.</param>
    public void RegisterSecondaryMonitorCallback(Action<DisplayContent> callback)
    {
        _secondaryMonitorCallback = callback;
    }

    /// <summary>
    /// Unregisters the secondary monitor callback.
    /// </summary>
    public void UnregisterSecondaryMonitorCallback()
    {
        _secondaryMonitorCallback = null;
    }

    #endregion

    #region Private Methods

    private void SetState(CustomerDisplayState newState)
    {
        if (_currentState == newState) return;

        var previousState = _currentState;
        _currentState = newState;

        StateChanged?.Invoke(this, new DisplayStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState
        });
    }

    private void SendToDisplay(DisplayContent content)
    {
        if (!_isConnected || _activeConfiguration == null) return;

        switch (_activeConfiguration.DisplayType)
        {
            case CustomerDisplayType.Vfd:
                SendToVfd(content);
                break;

            case CustomerDisplayType.SecondaryMonitor:
            case CustomerDisplayType.Tablet:
                _secondaryMonitorCallback?.Invoke(content);
                break;

            case CustomerDisplayType.NetworkDisplay:
                // In production: Send via network
                break;
        }
    }

    private void SendToVfd(DisplayContent content)
    {
        // In production: Write to serial port
        // var commands = new List<byte>();
        // commands.AddRange(VfdCommands.ClearScreen);
        // commands.AddRange(VfdCommands.MoveLine1);
        // commands.AddRange(Encoding.ASCII.GetBytes(content.Line1));
        // commands.AddRange(VfdCommands.MoveLine2);
        // commands.AddRange(Encoding.ASCII.GetBytes(content.Line2));
        // _serialPort.Write(commands.ToArray(), 0, commands.Count);
    }

    private string GetDeviceInfo(CustomerDisplayConfiguration config)
    {
        return config.DisplayType switch
        {
            CustomerDisplayType.Vfd => $"VFD {config.CharactersPerLine}x{config.NumberOfLines} on {config.PortName}",
            CustomerDisplayType.SecondaryMonitor => $"Secondary Monitor #{config.MonitorIndex}",
            CustomerDisplayType.Tablet => $"Tablet Display at {config.IpAddress}:{config.Port}",
            CustomerDisplayType.NetworkDisplay => $"Network Display at {config.IpAddress}:{config.Port}",
            _ => "Unknown Display"
        };
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text.Substring(0, width);
        var padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }

    private static string TruncateOrPad(string text, int width)
    {
        if (text.Length > width) return text.Substring(0, width);
        return text.PadRight(width);
    }

    private static List<string> GetSerialPorts()
    {
        // In production: return System.IO.Ports.SerialPort.GetPortNames().ToList();
        // For testing, return simulated ports
        return new List<string> { "COM1", "COM2", "COM3", "COM4" };
    }

    private static List<MonitorInfo> GetMonitors()
    {
        // In production: Use System.Windows.Forms.Screen or WPF equivalent
        // For testing, return simulated monitors
        return new List<MonitorInfo>
        {
            new MonitorInfo
            {
                Index = 0,
                Name = "Primary Display",
                IsPrimary = true,
                Width = 1920,
                Height = 1080,
                Bounds = (0, 0, 1920, 1080)
            },
            new MonitorInfo
            {
                Index = 1,
                Name = "Customer Display",
                IsPrimary = false,
                Width = 1024,
                Height = 768,
                Bounds = (1920, 0, 1024, 768)
            }
        };
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _idleCycleCts?.Cancel();
        _idleCycleCts?.Dispose();

        // In production: Close serial port
        _isConnected = false;
        _disposed = true;
    }

    #endregion

    #region Test/Simulation Methods

    /// <summary>
    /// Simulates connecting a display for testing.
    /// </summary>
    public async Task<DisplayConnectionResult> SimulateConnectAsync(CustomerDisplayType displayType)
    {
        var config = new CustomerDisplayConfiguration
        {
            Id = _nextConfigId++,
            DisplayType = displayType,
            DisplayName = $"Simulated {displayType}",
            PortName = displayType == CustomerDisplayType.Vfd ? "COM1" : null,
            MonitorIndex = displayType == CustomerDisplayType.SecondaryMonitor ? 1 : 0,
            IsActive = true
        };

        return await ConnectAsync(config);
    }

    /// <summary>
    /// Gets the current display content for testing.
    /// </summary>
    public (string Line1, string Line2) GetCurrentContent()
    {
        return (_displayLine1, _displayLine2);
    }

    #endregion
}
