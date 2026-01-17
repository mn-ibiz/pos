// src/HospitalityPOS.WPF/ViewModels/CustomerDisplayViewModel.cs
// ViewModel for Customer Display Window (Secondary Monitor)
// Story 43-2: Customer Display Integration

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Hardware;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the customer-facing display window.
/// Handles display content and animations for secondary monitor display.
/// </summary>
public partial class CustomerDisplayViewModel : ObservableObject
{
    private readonly ICustomerDisplayService _displayService;

    #region Observable Properties

    /// <summary>Current display state.</summary>
    [ObservableProperty]
    private CustomerDisplayState _displayState = CustomerDisplayState.Disconnected;

    /// <summary>Welcome message for idle screen.</summary>
    [ObservableProperty]
    private string _welcomeMessage = "Welcome!";

    /// <summary>Secondary welcome message.</summary>
    [ObservableProperty]
    private string _welcomeMessageLine2 = "We appreciate your business";

    /// <summary>Thank you message.</summary>
    [ObservableProperty]
    private string _thankYouMessage = "Thank You!";

    /// <summary>Secondary thank you message.</summary>
    [ObservableProperty]
    private string _thankYouMessageLine2 = "Please come again";

    /// <summary>Last item name added.</summary>
    [ObservableProperty]
    private string _lastItemName = string.Empty;

    /// <summary>Last item price.</summary>
    [ObservableProperty]
    private decimal _lastItemPrice;

    /// <summary>Last item quantity.</summary>
    [ObservableProperty]
    private int _lastItemQuantity = 1;

    /// <summary>Formatted last item price.</summary>
    [ObservableProperty]
    private string _formattedLastItemPrice = string.Empty;

    /// <summary>Whether last item was weighed.</summary>
    [ObservableProperty]
    private bool _isWeighedItem;

    /// <summary>Weight display for weighed items.</summary>
    [ObservableProperty]
    private string _weightDisplay = string.Empty;

    /// <summary>Number of items in order.</summary>
    [ObservableProperty]
    private int _itemCount;

    /// <summary>Order subtotal.</summary>
    [ObservableProperty]
    private decimal _subtotal;

    /// <summary>Order tax amount.</summary>
    [ObservableProperty]
    private decimal _taxAmount;

    /// <summary>Order discount amount.</summary>
    [ObservableProperty]
    private decimal _discountAmount;

    /// <summary>Order total.</summary>
    [ObservableProperty]
    private decimal _total;

    /// <summary>Formatted total.</summary>
    [ObservableProperty]
    private string _formattedTotal = "KSh 0.00";

    /// <summary>Amount due for payment.</summary>
    [ObservableProperty]
    private decimal _amountDue;

    /// <summary>Amount paid.</summary>
    [ObservableProperty]
    private decimal _amountPaid;

    /// <summary>Change to return.</summary>
    [ObservableProperty]
    private decimal _change;

    /// <summary>Payment method name.</summary>
    [ObservableProperty]
    private string _paymentMethod = string.Empty;

    /// <summary>Formatted amount due.</summary>
    [ObservableProperty]
    private string _formattedAmountDue = string.Empty;

    /// <summary>Formatted amount paid.</summary>
    [ObservableProperty]
    private string _formattedAmountPaid = string.Empty;

    /// <summary>Formatted change.</summary>
    [ObservableProperty]
    private string _formattedChange = string.Empty;

    /// <summary>Current promotion title.</summary>
    [ObservableProperty]
    private string _promotionTitle = string.Empty;

    /// <summary>Current promotion description.</summary>
    [ObservableProperty]
    private string _promotionDescription = string.Empty;

    /// <summary>Promotion discount percentage.</summary>
    [ObservableProperty]
    private decimal? _promotionDiscount;

    /// <summary>Whether to show the logo.</summary>
    [ObservableProperty]
    private bool _showLogo = true;

    /// <summary>Logo path.</summary>
    [ObservableProperty]
    private string? _logoPath;

    /// <summary>Background color (hex).</summary>
    [ObservableProperty]
    private string _backgroundColor = "#1a1a2e";

    /// <summary>Primary text color (hex).</summary>
    [ObservableProperty]
    private string _primaryTextColor = "#FFFFFF";

    /// <summary>Accent color (hex).</summary>
    [ObservableProperty]
    private string _accentColor = "#22C55E";

    /// <summary>Currency symbol.</summary>
    [ObservableProperty]
    private string _currencySymbol = "KSh";

    /// <summary>Whether showing item added animation.</summary>
    [ObservableProperty]
    private bool _isShowingItemAnimation;

    /// <summary>Status message for errors.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Whether there's an error.</summary>
    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether to show the idle/welcome screen.
    /// </summary>
    public bool ShowWelcome => DisplayState == CustomerDisplayState.Idle ||
                               DisplayState == CustomerDisplayState.Connected;

    /// <summary>
    /// Whether to show item/order content.
    /// </summary>
    public bool ShowItems => DisplayState == CustomerDisplayState.ShowingItems;

    /// <summary>
    /// Whether to show payment content.
    /// </summary>
    public bool ShowPayment => DisplayState == CustomerDisplayState.ShowingPayment;

    /// <summary>
    /// Whether to show thank you screen.
    /// </summary>
    public bool ShowThankYou => DisplayState == CustomerDisplayState.ShowingThankYou;

    /// <summary>
    /// Whether to show promotion.
    /// </summary>
    public bool ShowPromotion => DisplayState == CustomerDisplayState.ShowingPromotion;

    /// <summary>
    /// Whether there's any discount.
    /// </summary>
    public bool HasDiscount => DiscountAmount > 0;

    /// <summary>
    /// Whether there's any tax.
    /// </summary>
    public bool HasTax => TaxAmount > 0;

    /// <summary>
    /// Whether to show change amount.
    /// </summary>
    public bool ShowChange => Change > 0;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the CustomerDisplayViewModel.
    /// </summary>
    /// <param name="displayService">Customer display service.</param>
    public CustomerDisplayViewModel(ICustomerDisplayService displayService)
    {
        _displayService = displayService;

        // Subscribe to service events
        _displayService.StateChanged += OnStateChanged;
        _displayService.Error += OnError;

        // Register callback for display updates
        if (_displayService is Infrastructure.Services.CustomerDisplayService service)
        {
            service.RegisterSecondaryMonitorCallback(OnDisplayContentUpdated);
        }
    }

    #endregion

    #region Event Handlers

    private void OnStateChanged(object? sender, DisplayStateChangedEventArgs e)
    {
        DisplayState = e.NewState;
        OnPropertyChanged(nameof(ShowWelcome));
        OnPropertyChanged(nameof(ShowItems));
        OnPropertyChanged(nameof(ShowPayment));
        OnPropertyChanged(nameof(ShowThankYou));
        OnPropertyChanged(nameof(ShowPromotion));
    }

    private void OnError(object? sender, DisplayErrorEventArgs e)
    {
        HasError = true;
        StatusMessage = e.ErrorMessage;
    }

    private void OnDisplayContentUpdated(DisplayContent content)
    {
        DisplayState = content.ContentType;

        // Update based on content type
        switch (content.ContentType)
        {
            case CustomerDisplayState.Idle:
                // Welcome screen content is already set from configuration
                break;

            case CustomerDisplayState.ShowingItems:
                UpdateItemDisplay(content);
                break;

            case CustomerDisplayState.ShowingPayment:
                UpdatePaymentDisplay(content);
                break;

            case CustomerDisplayState.ShowingThankYou:
                // Thank you content is already set from configuration
                break;

            case CustomerDisplayState.ShowingPromotion:
                UpdatePromotionDisplay(content);
                break;
        }

        // Update computed properties
        OnPropertyChanged(nameof(ShowWelcome));
        OnPropertyChanged(nameof(ShowItems));
        OnPropertyChanged(nameof(ShowPayment));
        OnPropertyChanged(nameof(ShowThankYou));
        OnPropertyChanged(nameof(ShowPromotion));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(HasTax));
        OnPropertyChanged(nameof(ShowChange));
    }

    #endregion

    #region Update Methods

    private void UpdateItemDisplay(DisplayContent content)
    {
        if (content.ItemInfo != null)
        {
            var item = content.ItemInfo;
            LastItemName = item.ProductName;
            LastItemPrice = item.LineTotal;
            LastItemQuantity = item.Quantity;
            FormattedLastItemPrice = $"{CurrencySymbol} {item.LineTotal:N2}";
            IsWeighedItem = item.IsByWeight;

            if (item.IsByWeight && item.Weight.HasValue)
            {
                WeightDisplay = $"{item.Weight:N3} {item.WeightUnit} @ {CurrencySymbol} {item.UnitPrice:N2}/{item.WeightUnit}";
            }
            else
            {
                WeightDisplay = string.Empty;
            }

            // Trigger item animation
            TriggerItemAnimation();
        }

        if (content.TotalInfo != null)
        {
            var total = content.TotalInfo;
            ItemCount = total.ItemCount;
            Subtotal = total.Subtotal;
            TaxAmount = total.TaxAmount;
            DiscountAmount = total.DiscountAmount;
            Total = total.Total;
            FormattedTotal = $"{CurrencySymbol} {total.Total:N2}";
        }
    }

    private void UpdatePaymentDisplay(DisplayContent content)
    {
        if (content.PaymentInfo != null)
        {
            var payment = content.PaymentInfo;
            AmountDue = payment.AmountDue;
            AmountPaid = payment.AmountPaid;
            Change = payment.Change;
            PaymentMethod = payment.PaymentMethod;

            FormattedAmountDue = $"{CurrencySymbol} {payment.AmountDue:N2}";
            FormattedAmountPaid = $"{CurrencySymbol} {payment.AmountPaid:N2}";
            FormattedChange = payment.Change > 0 ? $"{CurrencySymbol} {payment.Change:N2}" : string.Empty;
        }
    }

    private void UpdatePromotionDisplay(DisplayContent content)
    {
        if (content.PromotionInfo != null)
        {
            var promo = content.PromotionInfo;
            PromotionTitle = promo.Title;
            PromotionDescription = promo.Description;
            PromotionDiscount = promo.DiscountPercent;
        }
    }

    private async void TriggerItemAnimation()
    {
        IsShowingItemAnimation = true;
        await Task.Delay(500); // Animation duration
        IsShowingItemAnimation = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the display with configuration settings.
    /// </summary>
    /// <param name="configuration">Display configuration.</param>
    public void Initialize(CustomerDisplayConfiguration configuration)
    {
        WelcomeMessage = configuration.WelcomeMessage;
        WelcomeMessageLine2 = configuration.WelcomeMessageLine2;
        ThankYouMessage = configuration.ThankYouMessage;
        ThankYouMessageLine2 = configuration.ThankYouMessageLine2;
        ShowLogo = configuration.ShowLogo;
        LogoPath = configuration.LogoPath;
        BackgroundColor = configuration.BackgroundColor;
        PrimaryTextColor = configuration.PrimaryTextColor;
        AccentColor = configuration.AccentColor;
        CurrencySymbol = configuration.CurrencySymbol;
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Cleanup()
    {
        _displayService.StateChanged -= OnStateChanged;
        _displayService.Error -= OnError;

        if (_displayService is Infrastructure.Services.CustomerDisplayService service)
        {
            service.UnregisterSecondaryMonitorCallback();
        }
    }

    #endregion
}

/// <summary>
/// ViewModel for display configuration settings.
/// </summary>
public partial class DisplaySettingsViewModel : ObservableObject
{
    private readonly ICustomerDisplayService _displayService;

    #region Observable Properties

    /// <summary>Available configurations.</summary>
    [ObservableProperty]
    private List<CustomerDisplayConfiguration> _configurations = new();

    /// <summary>Selected configuration.</summary>
    [ObservableProperty]
    private CustomerDisplayConfiguration? _selectedConfiguration;

    /// <summary>Available serial ports.</summary>
    [ObservableProperty]
    private List<DisplayPortInfo> _availablePorts = new();

    /// <summary>Available monitors.</summary>
    [ObservableProperty]
    private List<MonitorInfo> _availableMonitors = new();

    /// <summary>Selected display type.</summary>
    [ObservableProperty]
    private CustomerDisplayType _selectedDisplayType = CustomerDisplayType.Vfd;

    /// <summary>Selected port name.</summary>
    [ObservableProperty]
    private string _selectedPort = "COM1";

    /// <summary>Selected monitor index.</summary>
    [ObservableProperty]
    private int _selectedMonitorIndex = 1;

    /// <summary>Baud rate.</summary>
    [ObservableProperty]
    private int _baudRate = 9600;

    /// <summary>Selected VFD protocol.</summary>
    [ObservableProperty]
    private VfdProtocol _selectedProtocol = VfdProtocol.EscPos;

    /// <summary>Welcome message.</summary>
    [ObservableProperty]
    private string _welcomeMessage = "Welcome!";

    /// <summary>Thank you message.</summary>
    [ObservableProperty]
    private string _thankYouMessage = "Thank You!";

    /// <summary>Whether display is connected.</summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>Connection status text.</summary>
    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    /// <summary>Test result message.</summary>
    [ObservableProperty]
    private string _testResult = string.Empty;

    /// <summary>Whether testing is in progress.</summary>
    [ObservableProperty]
    private bool _isTesting;

    /// <summary>Error message if any.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Whether there's an error.</summary>
    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of DisplaySettingsViewModel.
    /// </summary>
    /// <param name="displayService">Customer display service.</param>
    public DisplaySettingsViewModel(ICustomerDisplayService displayService)
    {
        _displayService = displayService;

        // Subscribe to events
        _displayService.Connected += (s, e) =>
        {
            IsConnected = e.Success;
            ConnectionStatus = e.Success ? "Connected" : $"Failed: {e.ErrorMessage}";
        };

        _displayService.Disconnected += (s, e) =>
        {
            IsConnected = false;
            ConnectionStatus = "Disconnected";
        };
    }

    #endregion

    #region Commands

    /// <summary>
    /// Loads available hardware and configurations.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            Configurations = (await _displayService.GetConfigurationsAsync()).ToList();
            AvailablePorts = (await _displayService.GetAvailablePortsAsync()).ToList();
            AvailableMonitors = (await _displayService.GetAvailableMonitorsAsync()).ToList();

            IsConnected = _displayService.IsConnected;
            ConnectionStatus = IsConnected ? "Connected" : "Disconnected";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Auto-detects displays.
    /// </summary>
    [RelayCommand]
    private async Task AutoDetectAsync()
    {
        try
        {
            var detected = await _displayService.AutoDetectDisplaysAsync();
            if (detected.Any())
            {
                TestResult = $"Detected {detected.Count} display(s)";
                await LoadAsync();
            }
            else
            {
                TestResult = "No displays detected";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Connects to the selected display.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            var config = BuildConfiguration();
            var result = await _displayService.ConnectAsync(config);

            if (result.Success)
            {
                IsConnected = true;
                ConnectionStatus = $"Connected: {result.DeviceInfo}";
                HasError = false;
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Connection failed";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Disconnects from the display.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _displayService.DisconnectAsync();
        IsConnected = false;
        ConnectionStatus = "Disconnected";
    }

    /// <summary>
    /// Tests the display connection.
    /// </summary>
    [RelayCommand]
    private async Task TestDisplayAsync()
    {
        IsTesting = true;
        TestResult = "Testing...";

        try
        {
            var result = await _displayService.TestDisplayAsync("Test Message OK!");

            if (result.Success)
            {
                TestResult = "Test successful!";
            }
            else
            {
                TestResult = $"Test failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            TestResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// Saves the current configuration.
    /// </summary>
    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            var config = BuildConfiguration();
            await _displayService.SaveConfigurationAsync(config);
            await LoadAsync();
            TestResult = "Configuration saved";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Deletes the selected configuration.
    /// </summary>
    [RelayCommand]
    private async Task DeleteConfigurationAsync()
    {
        if (SelectedConfiguration == null) return;

        try
        {
            await _displayService.DeleteConfigurationAsync(SelectedConfiguration.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    #endregion

    #region Private Methods

    private CustomerDisplayConfiguration BuildConfiguration()
    {
        return new CustomerDisplayConfiguration
        {
            Id = SelectedConfiguration?.Id ?? 0,
            DisplayType = SelectedDisplayType,
            PortName = SelectedPort,
            BaudRate = BaudRate,
            VfdProtocol = SelectedProtocol,
            MonitorIndex = SelectedMonitorIndex,
            WelcomeMessage = WelcomeMessage,
            ThankYouMessage = ThankYouMessage,
            IsActive = true
        };
    }

    #endregion
}
