using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing UI shell layout based on business mode.
/// </summary>
public interface IUiShellService
{
    /// <summary>
    /// Gets the current business mode.
    /// </summary>
    BusinessMode CurrentMode { get; }

    /// <summary>
    /// Gets the current layout mode (used in Hybrid mode).
    /// </summary>
    PosLayoutMode CurrentLayout { get; }

    /// <summary>
    /// Gets whether the current mode supports layout switching.
    /// </summary>
    bool CanSwitchLayout { get; }

    /// <summary>
    /// Gets whether barcode auto-focus should be enabled.
    /// </summary>
    bool ShouldAutoFocusBarcode { get; }

    /// <summary>
    /// Gets whether table management should be shown.
    /// </summary>
    bool ShouldShowTableManagement { get; }

    /// <summary>
    /// Switches to a different layout (only in Hybrid mode).
    /// </summary>
    /// <param name="layout">The layout to switch to.</param>
    void SwitchLayout(PosLayoutMode layout);

    /// <summary>
    /// Toggles between Restaurant and Supermarket layouts.
    /// </summary>
    void ToggleLayout();

    /// <summary>
    /// Refreshes the layout based on current configuration.
    /// </summary>
    Task RefreshAsync();

    /// <summary>
    /// Event raised when the layout changes.
    /// </summary>
    event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
}

/// <summary>
/// POS screen layout modes.
/// </summary>
public enum PosLayoutMode
{
    /// <summary>
    /// Restaurant layout with 3-panel view (ticket, categories, products).
    /// </summary>
    Restaurant,

    /// <summary>
    /// Supermarket layout with barcode focus and item list.
    /// </summary>
    Supermarket
}

/// <summary>
/// Event arguments for layout changes.
/// </summary>
public class LayoutChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the previous layout.
    /// </summary>
    public PosLayoutMode PreviousLayout { get; set; }

    /// <summary>
    /// Gets or sets the new layout.
    /// </summary>
    public PosLayoutMode NewLayout { get; set; }

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode Mode { get; set; }
}
