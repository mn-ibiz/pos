using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing UI shell layout based on business mode.
/// </summary>
public class UiShellService : IUiShellService
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly ILogger<UiShellService> _logger;
    private PosLayoutMode _currentLayout = PosLayoutMode.Restaurant;
    private BusinessMode _currentMode = BusinessMode.Restaurant;

    /// <inheritdoc />
    public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

    /// <inheritdoc />
    public BusinessMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public PosLayoutMode CurrentLayout => _currentLayout;

    /// <inheritdoc />
    public bool CanSwitchLayout => _currentMode == BusinessMode.Hybrid;

    /// <inheritdoc />
    public bool ShouldAutoFocusBarcode
    {
        get
        {
            var config = _configurationService.CachedConfiguration;
            if (config == null) return false;

            // Auto-focus if in Supermarket layout or if feature is explicitly enabled
            return _currentLayout == PosLayoutMode.Supermarket ||
                   (config.EnableBarcodeAutoFocus && _currentMode != BusinessMode.Restaurant);
        }
    }

    /// <inheritdoc />
    public bool ShouldShowTableManagement
    {
        get
        {
            var config = _configurationService.CachedConfiguration;
            if (config == null) return false;

            // Show table management in Restaurant layout if feature is enabled
            return config.EnableTableManagement &&
                   (_currentLayout == PosLayoutMode.Restaurant || _currentMode == BusinessMode.Restaurant);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UiShellService"/> class.
    /// </summary>
    public UiShellService(
        ISystemConfigurationService configurationService,
        ILogger<UiShellService> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize from current configuration
        _ = RefreshAsync();
    }

    /// <inheritdoc />
    public void SwitchLayout(PosLayoutMode layout)
    {
        if (!CanSwitchLayout && layout != GetDefaultLayout())
        {
            _logger.LogWarning("Cannot switch layout in {Mode} mode", _currentMode);
            return;
        }

        if (_currentLayout == layout)
        {
            return;
        }

        var previousLayout = _currentLayout;
        _currentLayout = layout;

        _logger.LogInformation("Layout switched from {PreviousLayout} to {NewLayout}", previousLayout, layout);

        OnLayoutChanged(new LayoutChangedEventArgs
        {
            PreviousLayout = previousLayout,
            NewLayout = layout,
            Mode = _currentMode
        });
    }

    /// <inheritdoc />
    public void ToggleLayout()
    {
        if (!CanSwitchLayout)
        {
            return;
        }

        var newLayout = _currentLayout == PosLayoutMode.Restaurant
            ? PosLayoutMode.Supermarket
            : PosLayoutMode.Restaurant;

        SwitchLayout(newLayout);
    }

    /// <inheritdoc />
    public async Task RefreshAsync()
    {
        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            if (config != null)
            {
                _currentMode = config.Mode;
                _currentLayout = GetDefaultLayout();

                _logger.LogDebug("UI Shell refreshed. Mode: {Mode}, Layout: {Layout}", _currentMode, _currentLayout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing UI shell configuration");
        }
    }

    private PosLayoutMode GetDefaultLayout()
    {
        return _currentMode switch
        {
            BusinessMode.Restaurant => PosLayoutMode.Restaurant,
            BusinessMode.Supermarket => PosLayoutMode.Supermarket,
            BusinessMode.Hybrid => PosLayoutMode.Restaurant, // Default to restaurant in hybrid
            _ => PosLayoutMode.Restaurant
        };
    }

    /// <summary>
    /// Raises the LayoutChanged event.
    /// </summary>
    protected virtual void OnLayoutChanged(LayoutChangedEventArgs e)
    {
        LayoutChanged?.Invoke(this, e);
    }
}
