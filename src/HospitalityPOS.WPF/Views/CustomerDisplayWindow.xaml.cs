using System.Windows;
using System.Windows.Forms;
using HospitalityPOS.Core.Models.Hardware;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Views;

/// <summary>
/// Full-screen customer display window for secondary monitor.
/// Shows item prices, running totals, and promotional content.
/// </summary>
public partial class CustomerDisplayWindow : Window
{
    private readonly CustomerDisplayViewModel _viewModel;
    private readonly int _targetMonitorIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerDisplayWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this window.</param>
    /// <param name="targetMonitorIndex">The index of the monitor to display on (0-based).</param>
    public CustomerDisplayWindow(CustomerDisplayViewModel viewModel, int targetMonitorIndex = 1)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _targetMonitorIndex = targetMonitorIndex;
        DataContext = viewModel;

        // Position window on target monitor
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnMonitor(_targetMonitorIndex);
    }

    /// <summary>
    /// Positions the window on the specified monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based).</param>
    public void PositionOnMonitor(int monitorIndex)
    {
        var screens = Screen.AllScreens;

        if (monitorIndex >= 0 && monitorIndex < screens.Length)
        {
            var targetScreen = screens[monitorIndex];
            var workingArea = targetScreen.WorkingArea;

            // Position window on target screen
            Left = workingArea.Left;
            Top = workingArea.Top;
            Width = workingArea.Width;
            Height = workingArea.Height;

            // Ensure full screen
            WindowState = WindowState.Maximized;
        }
        else if (screens.Length > 1)
        {
            // Default to secondary monitor if available
            var secondaryScreen = screens.FirstOrDefault(s => !s.Primary) ?? screens[0];
            var workingArea = secondaryScreen.WorkingArea;

            Left = workingArea.Left;
            Top = workingArea.Top;
            Width = workingArea.Width;
            Height = workingArea.Height;

            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Initializes the display with configuration settings.
    /// </summary>
    /// <param name="configuration">Display configuration.</param>
    public void Initialize(CustomerDisplayConfiguration configuration)
    {
        _viewModel.Initialize(configuration);

        // Position on configured monitor
        PositionOnMonitor(configuration.MonitorIndex);
    }

    /// <summary>
    /// Gets available monitors.
    /// </summary>
    /// <returns>List of available monitors.</returns>
    public static IReadOnlyList<MonitorInfo> GetAvailableMonitors()
    {
        var screens = Screen.AllScreens;
        var monitors = new List<MonitorInfo>();

        for (int i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            monitors.Add(new MonitorInfo
            {
                Index = i,
                Name = screen.DeviceName,
                IsPrimary = screen.Primary,
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                Bounds = (screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height)
            });
        }

        return monitors;
    }

    /// <summary>
    /// Clean up when window is closing.
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Cleanup();
        base.OnClosing(e);
    }

    /// <summary>
    /// Handles keyboard shortcuts (ESC to close for testing).
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Allow ESC to close for testing/development
        // In production, this should be controlled by the main application
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            // Only close if in debug mode or explicitly allowed
#if DEBUG
            Close();
#endif
        }
    }
}
