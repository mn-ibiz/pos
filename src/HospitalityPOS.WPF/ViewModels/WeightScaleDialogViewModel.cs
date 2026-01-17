using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Hardware;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the weight scale dialog used during POS checkout.
/// Displays weight from scale and allows adding weighed items to order.
/// </summary>
public partial class WeightScaleDialogViewModel : ViewModelBase
{
    private readonly IScaleService _scaleService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the product ID being weighed.
    /// </summary>
    [ObservableProperty]
    private int _productId;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    [ObservableProperty]
    private string _productName = string.Empty;

    /// <summary>
    /// Gets or sets the price per unit.
    /// </summary>
    [ObservableProperty]
    private decimal _pricePerUnit;

    /// <summary>
    /// Gets or sets the weight unit.
    /// </summary>
    [ObservableProperty]
    private WeightUnit _weightUnit = WeightUnit.Kilogram;

    /// <summary>
    /// Gets or sets the current weight reading.
    /// </summary>
    [ObservableProperty]
    private decimal _currentWeight;

    /// <summary>
    /// Gets or sets the tare weight.
    /// </summary>
    [ObservableProperty]
    private decimal _tareWeight;

    /// <summary>
    /// Gets or sets whether the weight is stable.
    /// </summary>
    [ObservableProperty]
    private bool _isStable;

    /// <summary>
    /// Gets or sets whether reading is in motion.
    /// </summary>
    [ObservableProperty]
    private bool _isInMotion;

    /// <summary>
    /// Gets or sets whether the scale is connected.
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Gets or sets the scale status.
    /// </summary>
    [ObservableProperty]
    private ScaleStatus _scaleStatus = ScaleStatus.Disconnected;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Connecting to scale...";

    /// <summary>
    /// Gets or sets whether manual weight entry is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isManualEntryEnabled;

    /// <summary>
    /// Gets or sets the manual weight entry value.
    /// </summary>
    [ObservableProperty]
    private decimal _manualWeight;

    /// <summary>
    /// Gets or sets whether loading is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets whether there's an error.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets the calculated total price.
    /// </summary>
    public decimal TotalPrice => CurrentWeight * PricePerUnit;

    /// <summary>
    /// Gets the formatted current weight.
    /// </summary>
    public string FormattedWeight => $"{CurrentWeight:N3} {GetUnitSymbol()}";

    /// <summary>
    /// Gets the formatted price per unit.
    /// </summary>
    public string FormattedPricePerUnit => $"KSh {PricePerUnit:N2}/{GetUnitSymbol()}";

    /// <summary>
    /// Gets the formatted total price.
    /// </summary>
    public string FormattedTotalPrice => $"KSh {TotalPrice:N2}";

    /// <summary>
    /// Gets the scale status display text.
    /// </summary>
    public string ScaleStatusText => ScaleStatus switch
    {
        ScaleStatus.Disconnected => "Disconnected",
        ScaleStatus.Connecting => "Connecting...",
        ScaleStatus.Ready => IsStable ? "Ready - Stable" : "Reading...",
        ScaleStatus.Reading => "Reading...",
        ScaleStatus.Error => "Error",
        ScaleStatus.Overload => "OVERLOAD",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the scale status color.
    /// </summary>
    public string ScaleStatusColor => ScaleStatus switch
    {
        ScaleStatus.Ready when IsStable => "#22C55E", // Green
        ScaleStatus.Ready => "#F59E0B", // Yellow
        ScaleStatus.Reading => "#3B82F6", // Blue
        ScaleStatus.Error or ScaleStatus.Overload => "#EF4444", // Red
        _ => "#A0A0B0" // Gray
    };

    #endregion

    /// <summary>
    /// Event raised when the dialog should close with result.
    /// </summary>
    public event EventHandler<WeighedOrderItem?>? DialogClosed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeightScaleDialogViewModel"/> class.
    /// </summary>
    public WeightScaleDialogViewModel(IScaleService scaleService, ILogger logger) : base(logger)
    {
        _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));

        // Subscribe to scale events
        _scaleService.WeightChanged += OnWeightChanged;
        _scaleService.StableWeightDetected += OnStableWeightDetected;
        _scaleService.StatusChanged += OnScaleStatusChanged;
        _scaleService.Disconnected += OnScaleDisconnected;
    }

    #region Public Methods

    /// <summary>
    /// Initializes the dialog for a specific product.
    /// </summary>
    public async Task InitializeAsync(int productId, string productName, decimal pricePerUnit, WeightUnit unit)
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            ProductId = productId;
            ProductName = productName;
            PricePerUnit = pricePerUnit;
            WeightUnit = unit;
            CurrentWeight = 0;
            TareWeight = 0;

            // Get product config for default tare
            var config = await _scaleService.GetWeighedProductConfigAsync(productId);
            if (config != null)
            {
                TareWeight = config.DefaultTareWeight;
            }

            // Connect to scale if not connected
            if (!_scaleService.IsConnected)
            {
                StatusMessage = "Connecting to scale...";
                var result = await _scaleService.ConnectAsync();

                if (!result.Success)
                {
                    HasError = true;
                    ErrorMessage = result.ErrorMessage ?? "Failed to connect to scale";
                    StatusMessage = "Scale not connected - use manual entry";
                    IsManualEntryEnabled = true;
                    IsLoading = false;
                    return;
                }
            }

            IsConnected = _scaleService.IsConnected;
            ScaleStatus = _scaleService.Status;
            StatusMessage = "Place item on scale";

            // Start continuous reading
            await _scaleService.StartContinuousReadingAsync(200);

            Logger.Information("Weight dialog initialized for product {Name} @ KSh {Price}/{Unit}",
                productName, pricePerUnit, GetUnitSymbol());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize weight dialog");
            HasError = true;
            ErrorMessage = ex.Message;
            IsManualEntryEnabled = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Cleans up when dialog is closing.
    /// </summary>
    public async Task CleanupAsync()
    {
        // Unsubscribe from events
        _scaleService.WeightChanged -= OnWeightChanged;
        _scaleService.StableWeightDetected -= OnStableWeightDetected;
        _scaleService.StatusChanged -= OnScaleStatusChanged;
        _scaleService.Disconnected -= OnScaleDisconnected;

        // Stop continuous reading
        await _scaleService.StopContinuousReadingAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to read weight from scale.
    /// </summary>
    [RelayCommand]
    private async Task ReadWeightAsync()
    {
        if (!_scaleService.IsConnected)
        {
            HasError = true;
            ErrorMessage = "Scale not connected";
            return;
        }

        try
        {
            StatusMessage = "Reading weight...";
            var reading = await _scaleService.ReadWeightAsync();
            UpdateWeightFromReading(reading);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to read weight");
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Command to tare the scale.
    /// </summary>
    [RelayCommand]
    private async Task TareAsync()
    {
        if (!_scaleService.IsConnected)
        {
            return;
        }

        try
        {
            await _scaleService.TareAsync();
            TareWeight = _scaleService.CurrentTareWeight;
            StatusMessage = $"Tared at {TareWeight:N3} {GetUnitSymbol()}";
            Logger.Information("Scale tared at {Weight}", TareWeight);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to tare");
            HasError = true;
            ErrorMessage = "Failed to tare scale";
        }
    }

    /// <summary>
    /// Command to zero the scale.
    /// </summary>
    [RelayCommand]
    private async Task ZeroAsync()
    {
        if (!_scaleService.IsConnected)
        {
            return;
        }

        try
        {
            await _scaleService.ZeroAsync();
            TareWeight = 0;
            CurrentWeight = 0;
            StatusMessage = "Scale zeroed";
            UpdatePriceDisplay();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to zero");
            HasError = true;
            ErrorMessage = "Failed to zero scale";
        }
    }

    /// <summary>
    /// Command to toggle manual entry mode.
    /// </summary>
    [RelayCommand]
    private void ToggleManualEntry()
    {
        IsManualEntryEnabled = !IsManualEntryEnabled;

        if (IsManualEntryEnabled)
        {
            ManualWeight = CurrentWeight;
            StatusMessage = "Enter weight manually";
        }
        else
        {
            StatusMessage = "Place item on scale";
        }
    }

    /// <summary>
    /// Command to apply manual weight.
    /// </summary>
    [RelayCommand]
    private void ApplyManualWeight()
    {
        if (ManualWeight > 0)
        {
            CurrentWeight = ManualWeight;
            IsStable = true;
            UpdatePriceDisplay();
            StatusMessage = "Manual weight applied";
            Logger.Information("Manual weight applied: {Weight}", ManualWeight);
        }
    }

    /// <summary>
    /// Command to add item to order.
    /// </summary>
    [RelayCommand]
    private async Task AddToOrderAsync()
    {
        if (CurrentWeight <= 0)
        {
            HasError = true;
            ErrorMessage = "Weight must be greater than zero";
            return;
        }

        try
        {
            var item = await _scaleService.CreateWeighedOrderItemAsync(
                ProductId,
                CurrentWeight,
                WeightUnit);

            item.ProductName = ProductName;

            Logger.Information("Added weighed item to order: {Product} {Weight}{Unit} = KSh {Total}",
                ProductName, CurrentWeight, GetUnitSymbol(), TotalPrice);

            await CleanupAsync();
            DialogClosed?.Invoke(this, item);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to add item to order");
            HasError = true;
            ErrorMessage = "Failed to add item";
        }
    }

    /// <summary>
    /// Command to cancel and close dialog.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await CleanupAsync();
        DialogClosed?.Invoke(this, null);
    }

    /// <summary>
    /// Command to retry connecting to scale.
    /// </summary>
    [RelayCommand]
    private async Task RetryConnectionAsync()
    {
        HasError = false;
        ErrorMessage = null;
        await InitializeAsync(ProductId, ProductName, PricePerUnit, WeightUnit);
    }

    #endregion

    #region Event Handlers

    private void OnWeightChanged(object? sender, WeightChangedEventArgs e)
    {
        UpdateWeightFromReading(e.Reading);
    }

    private void OnStableWeightDetected(object? sender, WeightChangedEventArgs e)
    {
        UpdateWeightFromReading(e.Reading);

        if (e.Reading.Weight > 0)
        {
            StatusMessage = "Weight stable - Ready to add";
        }
    }

    private void OnScaleStatusChanged(object? sender, ScaleStatusChangedEventArgs e)
    {
        ScaleStatus = e.NewStatus;
        OnPropertyChanged(nameof(ScaleStatusText));
        OnPropertyChanged(nameof(ScaleStatusColor));

        if (e.NewStatus == ScaleStatus.Error)
        {
            StatusMessage = e.Message ?? "Scale error";
            IsManualEntryEnabled = true;
        }
    }

    private void OnScaleDisconnected(object? sender, EventArgs e)
    {
        IsConnected = false;
        ScaleStatus = ScaleStatus.Disconnected;
        StatusMessage = "Scale disconnected - use manual entry";
        IsManualEntryEnabled = true;
        OnPropertyChanged(nameof(ScaleStatusText));
        OnPropertyChanged(nameof(ScaleStatusColor));
    }

    #endregion

    #region Private Methods

    private void UpdateWeightFromReading(WeightReading reading)
    {
        CurrentWeight = reading.Weight;
        IsStable = reading.IsStable;
        IsInMotion = reading.IsInMotion;

        if (reading.IsOverload)
        {
            StatusMessage = "OVERLOAD - Remove weight!";
            HasError = true;
            ErrorMessage = "Scale capacity exceeded";
        }
        else if (reading.IsInMotion)
        {
            StatusMessage = "Stabilizing...";
        }
        else if (reading.Weight > 0)
        {
            StatusMessage = "Weight stable - Ready to add";
        }
        else
        {
            StatusMessage = "Place item on scale";
        }

        UpdatePriceDisplay();
    }

    private void UpdatePriceDisplay()
    {
        OnPropertyChanged(nameof(FormattedWeight));
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(FormattedTotalPrice));
    }

    private string GetUnitSymbol() => WeightUnit switch
    {
        WeightUnit.Kilogram => "kg",
        WeightUnit.Gram => "g",
        WeightUnit.Pound => "lb",
        WeightUnit.Ounce => "oz",
        _ => "kg"
    };

    #endregion
}

/// <summary>
/// ViewModel for scale settings/configuration.
/// </summary>
public partial class ScaleSettingsViewModel : ViewModelBase
{
    private readonly IScaleService _scaleService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<ScaleConfiguration> _configurations = new();

    [ObservableProperty]
    private ScaleConfiguration? _selectedConfiguration;

    [ObservableProperty]
    private ObservableCollection<AvailablePort> _availablePorts = new();

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private ScaleTestResult? _lastTestResult;

    // New configuration fields
    [ObservableProperty]
    private string _newConfigName = "New Scale";

    [ObservableProperty]
    private ScaleConnectionType _newConfigConnectionType = ScaleConnectionType.Serial;

    [ObservableProperty]
    private ScaleProtocol _newConfigProtocol = ScaleProtocol.Cas;

    [ObservableProperty]
    private string _newConfigPort = "COM1";

    [ObservableProperty]
    private int _newConfigBaudRate = 9600;

    #endregion

    public ScaleSettingsViewModel(IScaleService scaleService, ILogger logger) : base(logger)
    {
        _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            // Load configurations
            var configs = await _scaleService.GetConfigurationsAsync();
            Configurations.Clear();
            foreach (var config in configs)
            {
                Configurations.Add(config);
            }

            // Load available ports
            var ports = await _scaleService.GetAvailablePortsAsync();
            AvailablePorts.Clear();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }

            IsConnected = _scaleService.IsConnected;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load scale settings");
            StatusMessage = "Failed to load settings";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (SelectedConfiguration == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Testing connection...";

            LastTestResult = await _scaleService.TestConnectionAsync(SelectedConfiguration);

            StatusMessage = LastTestResult.Success
                ? $"Connection successful! Read: {LastTestResult.Reading?.FormattedWeight}"
                : $"Connection failed: {LastTestResult.ErrorMessage}";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Scale test failed");
            StatusMessage = $"Test failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedConfiguration == null) return;

        try
        {
            IsLoading = true;
            var result = await _scaleService.ConnectAsync(SelectedConfiguration);
            IsConnected = result.Success;
            StatusMessage = result.Success ? "Connected" : $"Failed: {result.ErrorMessage}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _scaleService.DisconnectAsync();
        IsConnected = false;
        StatusMessage = "Disconnected";
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        var config = new ScaleConfiguration
        {
            Name = NewConfigName,
            ConnectionType = NewConfigConnectionType,
            Protocol = NewConfigProtocol,
            PortName = NewConfigPort,
            BaudRate = NewConfigBaudRate
        };

        await _scaleService.SaveConfigurationAsync(config);
        await LoadAsync();
        StatusMessage = "Configuration saved";
    }

    [RelayCommand]
    private async Task DeleteConfigurationAsync()
    {
        if (SelectedConfiguration == null) return;

        await _scaleService.DeleteConfigurationAsync(SelectedConfiguration.Id);
        await LoadAsync();
        StatusMessage = "Configuration deleted";
    }

    [RelayCommand]
    private async Task SetActiveAsync()
    {
        if (SelectedConfiguration == null) return;

        await _scaleService.SetActiveConfigurationAsync(SelectedConfiguration.Id);
        await LoadAsync();
        StatusMessage = $"'{SelectedConfiguration.Name}' set as active";
    }

    [RelayCommand]
    private async Task AutoDetectAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning for scales...";

            var detected = await _scaleService.AutoDetectScalesAsync();

            if (detected.Count > 0)
            {
                StatusMessage = $"Found {detected.Count} scale(s)";
                foreach (var scale in detected)
                {
                    await _scaleService.SaveConfigurationAsync(scale);
                }
                await LoadAsync();
            }
            else
            {
                StatusMessage = "No scales detected";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
