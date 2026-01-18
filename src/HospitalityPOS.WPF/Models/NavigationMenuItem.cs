using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HospitalityPOS.WPF.Models;

/// <summary>
/// Represents a navigation menu item in the sidebar.
/// </summary>
public partial class NavigationMenuItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the display title of the menu item.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets the icon glyph (Unicode character) for the menu item.
    /// </summary>
    [ObservableProperty]
    private string _icon = string.Empty;

    /// <summary>
    /// Gets or sets the navigation target ViewModel type name.
    /// </summary>
    [ObservableProperty]
    private string? _targetViewModel;

    /// <summary>
    /// Gets or sets the command to execute when the menu item is clicked.
    /// </summary>
    [ObservableProperty]
    private ICommand? _command;

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    [ObservableProperty]
    private object? _commandParameter;

    /// <summary>
    /// Gets or sets whether this menu item is currently selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this menu item is expanded (for parent items).
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets the child menu items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationMenuItem>? _children;

    /// <summary>
    /// Gets or sets the required permission to view this menu item.
    /// </summary>
    [ObservableProperty]
    private string? _requiredPermission;

    /// <summary>
    /// Gets or sets whether this menu item is visible based on permissions.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Gets or sets whether this is a separator item.
    /// </summary>
    [ObservableProperty]
    private bool _isSeparator;

    /// <summary>
    /// Gets or sets the badge text (e.g., notification count).
    /// </summary>
    [ObservableProperty]
    private string? _badge;

    /// <summary>
    /// Gets or sets the badge color.
    /// </summary>
    [ObservableProperty]
    private string? _badgeColor;

    /// <summary>
    /// Gets whether this menu item has children.
    /// </summary>
    public bool HasChildren => Children is not null && Children.Count > 0;

    /// <summary>
    /// Gets whether this menu item has a badge.
    /// </summary>
    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}

/// <summary>
/// Represents a group of navigation menu items.
/// </summary>
public partial class NavigationMenuGroup : ObservableObject
{
    /// <summary>
    /// Gets or sets the group header text.
    /// </summary>
    [ObservableProperty]
    private string _header = string.Empty;

    /// <summary>
    /// Gets or sets whether to show the group header.
    /// </summary>
    [ObservableProperty]
    private bool _showHeader = true;

    /// <summary>
    /// Gets or sets the menu items in this group.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationMenuItem> _items = new();
}
