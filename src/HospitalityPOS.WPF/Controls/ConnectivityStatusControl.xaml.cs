using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Controls;

/// <summary>
/// DTO for service status display in tooltip.
/// </summary>
public class ServiceStatusItem
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
}

/// <summary>
/// User control that displays the current network connectivity status.
/// Shows a colored indicator (green/yellow/red) with an optional text label.
/// </summary>
public partial class ConnectivityStatusControl : UserControl, INotifyPropertyChanged
{
    private IConnectivityService? _connectivityService;
    private ConnectivityStatus _status = ConnectivityStatus.Offline;
    private DateTime? _lastSyncTime;
    private bool _showText = true;
    private readonly ObservableCollection<ServiceStatusItem> _serviceStatuses = new();

    #region Dependency Properties

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(ConnectivityStatus), typeof(ConnectivityStatusControl),
            new PropertyMetadata(ConnectivityStatus.Offline, OnStatusChanged));

    public static readonly DependencyProperty ShowTextProperty =
        DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(ConnectivityStatusControl),
            new PropertyMetadata(true));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the current connectivity status.
    /// </summary>
    public ConnectivityStatus Status
    {
        get => (ConnectivityStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the status text label.
    /// </summary>
    public bool ShowText
    {
        get => (bool)GetValue(ShowTextProperty);
        set => SetValue(ShowTextProperty, value);
    }

    /// <summary>
    /// Gets the formatted last sync time text.
    /// </summary>
    public string LastSyncText
    {
        get
        {
            if (_lastSyncTime == null)
                return "Never synced";

            var elapsed = DateTime.UtcNow - _lastSyncTime.Value;

            if (elapsed.TotalMinutes < 1)
                return "Last sync: Just now";
            if (elapsed.TotalMinutes < 60)
                return $"Last sync: {(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24)
                return $"Last sync: {(int)elapsed.TotalHours}h ago";

            return $"Last sync: {_lastSyncTime.Value:g}";
        }
    }

    /// <summary>
    /// Gets whether there's a last sync time to display.
    /// </summary>
    public bool HasLastSync => _lastSyncTime != null;

    /// <summary>
    /// Gets the collection of service statuses for the tooltip.
    /// </summary>
    public ObservableCollection<ServiceStatusItem> ServiceStatuses => _serviceStatuses;

    /// <summary>
    /// Gets whether there are service details to display.
    /// </summary>
    public bool HasServiceDetails => _serviceStatuses.Count > 0;

    #endregion

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    public ConnectivityStatusControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Try to get connectivity service from DI
        try
        {
            _connectivityService = App.Services.GetService<IConnectivityService>();

            if (_connectivityService != null)
            {
                _connectivityService.StatusChanged += OnConnectivityStatusChanged;
                Status = _connectivityService.CurrentStatus;
                _lastSyncTime = _connectivityService.LastOnlineTime;

                // Start monitoring if not already
                if (!_connectivityService.IsMonitoring)
                {
                    _connectivityService.StartMonitoring();
                }

                // Initial status check
                _ = RefreshStatusAsync();
            }
        }
        catch
        {
            // Service not available - show offline
            Status = ConnectivityStatus.Offline;
        }

        UpdateAnimation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_connectivityService != null)
        {
            _connectivityService.StatusChanged -= OnConnectivityStatusChanged;
        }
    }

    private void OnConnectivityStatusChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            Status = e.NewStatus;
            if (e.NewStatus == ConnectivityStatus.Online)
            {
                _lastSyncTime = e.Timestamp;
                OnPropertyChanged(nameof(LastSyncText));
                OnPropertyChanged(nameof(HasLastSync));
            }

            UpdateAnimation();
        });
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConnectivityStatusControl control)
        {
            control.UpdateAnimation();
        }
    }

    private void UpdateAnimation()
    {
        var storyboard = (Storyboard)Resources["PulseAnimation"];

        if (Status == ConnectivityStatus.Offline)
        {
            storyboard.Begin(this, true);
        }
        else
        {
            storyboard.Stop(this);
            StatusIndicator.Opacity = 1;
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_connectivityService == null)
            return;

        try
        {
            var statuses = await _connectivityService.GetServiceStatusesAsync();

            await Dispatcher.InvokeAsync(() =>
            {
                _serviceStatuses.Clear();
                foreach (var (service, isReachable) in statuses)
                {
                    _serviceStatuses.Add(new ServiceStatusItem
                    {
                        ServiceName = service,
                        IsReachable = isReachable
                    });
                }
                OnPropertyChanged(nameof(HasServiceDetails));
            });
        }
        catch
        {
            // Ignore errors during refresh
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
