using System.Timers;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Hardware;
using Serilog;
using Timer = System.Timers.Timer;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for weight scale integration.
/// Supports USB HID and RS-232 serial scales for supermarket weighed products.
/// </summary>
public class ScaleService : IScaleService
{
    private readonly ILogger _logger;
    private readonly Dictionary<int, ScaleConfiguration> _configurations = new();
    private readonly Dictionary<int, WeighedProductConfig> _weighedProducts = new();
    private readonly Dictionary<int, (string Name, decimal Price)> _productInfo = new();

    private Timer? _continuousReadTimer;
    private ScaleConfiguration? _currentConfig;
    private WeightReading? _lastReading;
    private decimal _currentTareWeight;
    private bool _isConnected;
    private bool _isContinuousReadingActive;
    private int _nextConfigId = 1;
    private ScaleStatus _status = ScaleStatus.Disconnected;
    private readonly object _lock = new();

    // Simulation state
    private decimal _simulatedWeight;
    private bool _simulatedStable = true;

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public ScaleStatus Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                var oldStatus = _status;
                _status = value;
                StatusChanged?.Invoke(this, new ScaleStatusChangedEventArgs
                {
                    PreviousStatus = oldStatus,
                    NewStatus = value
                });
            }
        }
    }

    /// <inheritdoc />
    public ScaleConfiguration? CurrentConfiguration => _currentConfig;

    /// <inheritdoc />
    public WeightReading? LastReading => _lastReading;

    /// <inheritdoc />
    public bool IsContinuousReadingActive => _isContinuousReadingActive;

    /// <inheritdoc />
    public decimal CurrentTareWeight => _currentTareWeight;

    #region Events

    /// <inheritdoc />
    public event EventHandler<WeightChangedEventArgs>? WeightChanged;

    /// <inheritdoc />
    public event EventHandler<WeightChangedEventArgs>? StableWeightDetected;

    /// <inheritdoc />
    public event EventHandler<ScaleStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler? Disconnected;

    /// <inheritdoc />
    public event EventHandler? Overload;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ScaleService"/> class.
    /// </summary>
    public ScaleService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDefaultConfigurations();
        InitializeSampleProducts();
    }

    private void InitializeDefaultConfigurations()
    {
        // Default CAS serial scale configuration
        var casConfig = new ScaleConfiguration
        {
            Id = _nextConfigId++,
            Name = "CAS Scale (Serial)",
            ConnectionType = ScaleConnectionType.Serial,
            Protocol = ScaleProtocol.Cas,
            PortName = "COM1",
            BaudRate = 9600,
            DataBits = 8,
            Parity = "None",
            StopBits = 1,
            DefaultUnit = WeightUnit.Kilogram,
            AutoReadOnStable = true,
            IsActive = true,
            WeightCommand = "W",
            TareCommand = "T",
            ZeroCommand = "Z"
        };
        _configurations[casConfig.Id] = casConfig;

        // USB HID scale configuration
        var usbConfig = new ScaleConfiguration
        {
            Id = _nextConfigId++,
            Name = "USB HID Scale",
            ConnectionType = ScaleConnectionType.UsbHid,
            Protocol = ScaleProtocol.GenericUsbHid,
            UsbVendorId = 0x0922, // Common USB scale vendor
            UsbProductId = 0x8003,
            DefaultUnit = WeightUnit.Kilogram,
            AutoReadOnStable = true,
            IsActive = false
        };
        _configurations[usbConfig.Id] = usbConfig;

        _logger.Information("Scale service initialized with {Count} configurations", _configurations.Count);
    }

    private void InitializeSampleProducts()
    {
        // Sample weighed products
        var products = new[]
        {
            (Id: 1001, Name: "Bananas", Price: 120m),
            (Id: 1002, Name: "Tomatoes", Price: 80m),
            (Id: 1003, Name: "Onions", Price: 60m),
            (Id: 1004, Name: "Potatoes", Price: 50m),
            (Id: 1005, Name: "Beef", Price: 800m),
            (Id: 1006, Name: "Chicken", Price: 500m),
            (Id: 1007, Name: "Rice (Loose)", Price: 180m),
            (Id: 1008, Name: "Sugar (Loose)", Price: 150m)
        };

        foreach (var (id, name, price) in products)
        {
            _productInfo[id] = (name, price);
            _weighedProducts[id] = new WeighedProductConfig
            {
                ProductId = id,
                IsWeighed = true,
                PricePerUnit = price,
                WeightUnit = WeightUnit.Kilogram,
                DefaultTareWeight = 0m
            };
        }
    }

    #region Connection

    /// <inheritdoc />
    public Task<ScaleConnectionResult> ConnectAsync(ScaleConfiguration config)
    {
        lock (_lock)
        {
            _logger.Information("Connecting to scale: {Name}, Type: {Type}, Protocol: {Protocol}",
                config.Name, config.ConnectionType, config.Protocol);

            Status = ScaleStatus.Connecting;

            try
            {
                // In production, this would initialize actual hardware connection
                // For now, we simulate a successful connection
                _currentConfig = config;
                _isConnected = true;
                Status = ScaleStatus.Ready;

                _logger.Information("Scale connected successfully: {Name}", config.Name);

                return Task.FromResult(new ScaleConnectionResult
                {
                    Success = true,
                    ScaleModel = GetScaleModelDescription(config),
                    MaxCapacity = 30m, // 30 kg typical
                    Resolution = 0.001m // 1 gram
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect to scale: {Name}", config.Name);
                Status = ScaleStatus.Error;

                return Task.FromResult(new ScaleConnectionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }

    /// <inheritdoc />
    public async Task<ScaleConnectionResult> ConnectAsync()
    {
        var activeConfig = await GetActiveConfigurationAsync();
        if (activeConfig == null)
        {
            return new ScaleConnectionResult
            {
                Success = false,
                ErrorMessage = "No active scale configuration found"
            };
        }

        return await ConnectAsync(activeConfig);
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            if (_isConnected)
            {
                _ = StopContinuousReadingAsync();

                _isConnected = false;
                _currentConfig = null;
                _lastReading = null;
                Status = ScaleStatus.Disconnected;

                _logger.Information("Scale disconnected");
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ScaleTestResult> TestConnectionAsync(ScaleConfiguration config)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var recommendations = new List<string>();

        try
        {
            // Attempt connection
            var connectResult = await ConnectAsync(config);
            if (!connectResult.Success)
            {
                return new ScaleTestResult
                {
                    Success = false,
                    ErrorMessage = connectResult.ErrorMessage,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Recommendations = new List<string>
                    {
                        "Check that the scale is powered on",
                        "Verify the cable connection",
                        $"Ensure the correct port ({config.PortName}) is selected"
                    }
                };
            }

            // Try to read weight
            var reading = await ReadWeightAsync();

            // Disconnect after test
            await DisconnectAsync();

            stopwatch.Stop();

            if (reading.Weight == 0 && !reading.IsStable)
            {
                recommendations.Add("Place a known weight on the scale to verify accuracy");
            }

            if (reading.IsInMotion)
            {
                recommendations.Add("Wait for the scale to stabilize before taking readings");
            }

            return new ScaleTestResult
            {
                Success = true,
                Reading = reading,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ScaleTestResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Recommendations = new List<string>
                {
                    "Check that no other application is using the scale",
                    "Try restarting the scale",
                    "Verify the protocol settings match your scale model"
                }
            };
        }
    }

    private static string GetScaleModelDescription(ScaleConfiguration config)
    {
        return config.Protocol switch
        {
            ScaleProtocol.Cas => "CAS Compatible Scale",
            ScaleProtocol.Toledo => "Toledo/Mettler-Toledo Scale",
            ScaleProtocol.GenericUsbHid => "Generic USB HID Scale",
            ScaleProtocol.Jadever => "Jadever Scale",
            ScaleProtocol.Ohaus => "Ohaus Scale",
            _ => "Digital Scale"
        };
    }

    #endregion

    #region Weight Reading

    /// <inheritdoc />
    public Task<WeightReading> ReadWeightAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Scale is not connected");
        }

        Status = ScaleStatus.Reading;

        try
        {
            // In production, this would read from actual hardware
            // For simulation, we generate a reading based on _simulatedWeight
            var reading = new WeightReading
            {
                Weight = Math.Max(0, _simulatedWeight - _currentTareWeight),
                GrossWeight = _simulatedWeight,
                TareWeight = _currentTareWeight,
                Unit = _currentConfig?.DefaultUnit ?? WeightUnit.Kilogram,
                IsStable = _simulatedStable,
                IsInMotion = !_simulatedStable,
                IsZeroed = _simulatedWeight == 0,
                IsOverload = _simulatedWeight > 30m, // 30kg max
                Timestamp = DateTime.UtcNow
            };

            _lastReading = reading;
            Status = ScaleStatus.Ready;

            WeightChanged?.Invoke(this, new WeightChangedEventArgs { Reading = reading });

            if (reading.IsStable && reading.Weight > 0)
            {
                StableWeightDetected?.Invoke(this, new WeightChangedEventArgs { Reading = reading });
            }

            if (reading.IsOverload)
            {
                Overload?.Invoke(this, EventArgs.Empty);
            }

            return Task.FromResult(reading);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to read weight");
            Status = ScaleStatus.Error;
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WeightReading?> WaitForStableWeightAsync(int timeoutMs = 5000)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Scale is not connected");
        }

        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var reading = await ReadWeightAsync();
            if (reading.IsStable && reading.Weight > 0)
            {
                return reading;
            }

            await Task.Delay(100);
        }

        _logger.Warning("Timeout waiting for stable weight");
        return null;
    }

    /// <inheritdoc />
    public Task StartContinuousReadingAsync(int intervalMs = 200)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Scale is not connected");
        }

        lock (_lock)
        {
            if (_isContinuousReadingActive)
            {
                return Task.CompletedTask;
            }

            _continuousReadTimer = new Timer(intervalMs);
            _continuousReadTimer.Elapsed += OnContinuousReadTimerElapsed;
            _continuousReadTimer.Start();
            _isContinuousReadingActive = true;

            _logger.Information("Started continuous weight reading at {Interval}ms interval", intervalMs);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopContinuousReadingAsync()
    {
        lock (_lock)
        {
            if (_continuousReadTimer != null)
            {
                _continuousReadTimer.Stop();
                _continuousReadTimer.Elapsed -= OnContinuousReadTimerElapsed;
                _continuousReadTimer.Dispose();
                _continuousReadTimer = null;
            }

            _isContinuousReadingActive = false;
            _logger.Information("Stopped continuous weight reading");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Timer callback for continuous weight reading.
    /// Uses fire-and-forget pattern with proper exception handling.
    /// </summary>
    private void OnContinuousReadTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Fire-and-forget with proper exception handling
        _ = SafeReadWeightAsync();
    }

    /// <summary>
    /// Safely reads weight with exception handling for timer callback.
    /// </summary>
    private async Task SafeReadWeightAsync()
    {
        try
        {
            await ReadWeightAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during continuous weight reading");
        }
    }

    #endregion

    #region Tare and Zero

    /// <inheritdoc />
    public Task<bool> TareAsync()
    {
        if (!_isConnected)
        {
            return Task.FromResult(false);
        }

        lock (_lock)
        {
            _currentTareWeight = _simulatedWeight;
            _logger.Information("Scale tared at {Weight}kg", _currentTareWeight);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ZeroAsync()
    {
        if (!_isConnected)
        {
            return Task.FromResult(false);
        }

        lock (_lock)
        {
            _currentTareWeight = 0;
            _simulatedWeight = 0;
            _logger.Information("Scale zeroed");
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SetTareWeightAsync(decimal tareWeight)
    {
        if (!_isConnected)
        {
            return Task.FromResult(false);
        }

        lock (_lock)
        {
            _currentTareWeight = tareWeight;
            _logger.Information("Manual tare weight set: {Weight}kg", tareWeight);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ClearTareAsync()
    {
        lock (_lock)
        {
            _currentTareWeight = 0;
            _logger.Information("Tare cleared");
        }

        return Task.FromResult(true);
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public Task<List<ScaleConfiguration>> GetConfigurationsAsync()
    {
        return Task.FromResult(_configurations.Values.ToList());
    }

    /// <inheritdoc />
    public Task<ScaleConfiguration?> GetActiveConfigurationAsync()
    {
        var active = _configurations.Values.FirstOrDefault(c => c.IsActive);
        return Task.FromResult(active);
    }

    /// <inheritdoc />
    public Task<ScaleConfiguration> SaveConfigurationAsync(ScaleConfiguration config)
    {
        if (config.Id == 0)
        {
            config.Id = _nextConfigId++;
        }

        _configurations[config.Id] = config;
        _logger.Information("Saved scale configuration: {Name}", config.Name);

        return Task.FromResult(config);
    }

    /// <inheritdoc />
    public Task<bool> DeleteConfigurationAsync(int configId)
    {
        var removed = _configurations.Remove(configId);
        if (removed)
        {
            _logger.Information("Deleted scale configuration: {Id}", configId);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<bool> SetActiveConfigurationAsync(int configId)
    {
        if (!_configurations.ContainsKey(configId))
        {
            return Task.FromResult(false);
        }

        foreach (var config in _configurations.Values)
        {
            config.IsActive = config.Id == configId;
        }

        _logger.Information("Set active configuration: {Id}", configId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<List<AvailablePort>> GetAvailablePortsAsync()
    {
        // In production, this would enumerate actual serial ports
        var ports = new List<AvailablePort>
        {
            new() { PortName = "COM1", Description = "Communications Port (COM1)" },
            new() { PortName = "COM2", Description = "Communications Port (COM2)" },
            new() { PortName = "COM3", Description = "USB Serial Port (COM3)" },
            new() { PortName = "COM4", Description = "USB Serial Port (COM4)" }
        };

        return Task.FromResult(ports);
    }

    /// <inheritdoc />
    public Task<List<ScaleConfiguration>> AutoDetectScalesAsync()
    {
        // In production, this would scan for connected scales
        var detected = new List<ScaleConfiguration>();

        // Simulate detecting a USB scale
        detected.Add(new ScaleConfiguration
        {
            Name = "Detected USB Scale",
            ConnectionType = ScaleConnectionType.UsbHid,
            Protocol = ScaleProtocol.GenericUsbHid,
            UsbVendorId = 0x0922,
            UsbProductId = 0x8003,
            DefaultUnit = WeightUnit.Kilogram
        });

        _logger.Information("Auto-detected {Count} scales", detected.Count);
        return Task.FromResult(detected);
    }

    #endregion

    #region Product Configuration

    /// <inheritdoc />
    public Task<WeighedProductConfig?> GetWeighedProductConfigAsync(int productId)
    {
        _weighedProducts.TryGetValue(productId, out var config);
        return Task.FromResult(config);
    }

    /// <inheritdoc />
    public Task<WeighedProductConfig> SetWeighedProductConfigAsync(WeighedProductConfig config)
    {
        _weighedProducts[config.ProductId] = config;
        _logger.Information("Set weighed product config for product {Id}", config.ProductId);
        return Task.FromResult(config);
    }

    /// <inheritdoc />
    public Task<List<WeighedProductConfig>> GetAllWeighedProductsAsync()
    {
        return Task.FromResult(_weighedProducts.Values.ToList());
    }

    /// <inheritdoc />
    public Task<bool> IsProductWeighedAsync(int productId)
    {
        return Task.FromResult(_weighedProducts.ContainsKey(productId) && _weighedProducts[productId].IsWeighed);
    }

    #endregion

    #region Order Integration

    /// <inheritdoc />
    public async Task<WeighedOrderItem> CreateWeighedOrderItemAsync(int productId)
    {
        var reading = await ReadWeightAsync();
        return await CreateWeighedOrderItemAsync(productId, reading.Weight, reading.Unit);
    }

    /// <inheritdoc />
    public Task<WeighedOrderItem> CreateWeighedOrderItemAsync(int productId, decimal weight, WeightUnit unit)
    {
        if (!_weighedProducts.TryGetValue(productId, out var config))
        {
            throw new ArgumentException($"Product {productId} is not configured as a weighed product");
        }

        var productName = _productInfo.TryGetValue(productId, out var info) ? info.Name : $"Product {productId}";

        // Convert weight if units differ
        var convertedWeight = unit == config.WeightUnit
            ? weight
            : WeightConversion.Convert(weight, unit, config.WeightUnit);

        var item = new WeighedOrderItem
        {
            ProductId = productId,
            ProductName = productName,
            Weight = convertedWeight,
            WeightUnit = config.WeightUnit,
            PricePerUnit = config.PricePerUnit,
            TareWeight = config.DefaultTareWeight,
            GrossWeight = convertedWeight + config.DefaultTareWeight
        };

        _logger.Information("Created weighed order item: {Product}, {Weight}kg @ KSh {Price}/kg = KSh {Total}",
            productName, item.Weight, item.PricePerUnit, item.TotalPrice);

        return Task.FromResult(item);
    }

    /// <inheritdoc />
    public Task<decimal> CalculatePriceAsync(int productId, decimal weight, WeightUnit unit)
    {
        if (!_weighedProducts.TryGetValue(productId, out var config))
        {
            throw new ArgumentException($"Product {productId} is not configured as a weighed product");
        }

        // Convert weight if units differ
        var convertedWeight = unit == config.WeightUnit
            ? weight
            : WeightConversion.Convert(weight, unit, config.WeightUnit);

        var price = convertedWeight * config.PricePerUnit;
        return Task.FromResult(Math.Round(price, 2));
    }

    #endregion

    #region Utility

    /// <inheritdoc />
    public decimal ConvertWeight(decimal weight, WeightUnit fromUnit, WeightUnit toUnit)
    {
        return WeightConversion.Convert(weight, fromUnit, toUnit);
    }

    /// <inheritdoc />
    public string FormatWeight(decimal weight, WeightUnit unit, int decimalPlaces = 3)
    {
        var format = $"N{decimalPlaces}";
        return $"{weight.ToString(format)} {WeightConversion.GetSymbol(unit)}";
    }

    /// <inheritdoc />
    public string FormatReceiptLine(WeighedOrderItem item)
    {
        var unit = WeightConversion.GetSymbol(item.WeightUnit);
        return $"  {item.Weight:N3} {unit} @ KSh {item.PricePerUnit:N2}/{unit}      KSh {item.TotalPrice:N2}";
    }

    #endregion

    #region Simulation Methods (for testing)

    /// <summary>
    /// Sets a simulated weight for testing purposes.
    /// </summary>
    public void SetSimulatedWeight(decimal weight, bool stable = true)
    {
        _simulatedWeight = weight;
        _simulatedStable = stable;
    }

    /// <summary>
    /// Simulates placing an item on the scale.
    /// </summary>
    public void SimulatePlaceItem(decimal weight)
    {
        _simulatedStable = false;
        _simulatedWeight = weight;

        // Simulate weight stabilizing after a moment
        Task.Delay(500).ContinueWith(_ =>
        {
            _simulatedStable = true;
            if (_isConnected)
            {
                _ = ReadWeightAsync();
            }
        });
    }

    /// <summary>
    /// Simulates removing an item from the scale.
    /// </summary>
    public void SimulateRemoveItem()
    {
        _simulatedWeight = 0;
        _simulatedStable = true;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                if (_continuousReadTimer != null)
                {
                    _continuousReadTimer.Stop();
                    _continuousReadTimer.Elapsed -= OnContinuousReadTimerElapsed;
                    _continuousReadTimer.Dispose();
                    _continuousReadTimer = null;
                }
                _isContinuousReadingActive = false;
            }

            _isConnected = false;
            _currentConfig = null;
            _lastReading = null;
            _status = ScaleStatus.Disconnected;
        }
    }

    /// <summary>
    /// Finalizer as safety net.
    /// </summary>
    ~ScaleService()
    {
        Dispose(false);
    }
}
