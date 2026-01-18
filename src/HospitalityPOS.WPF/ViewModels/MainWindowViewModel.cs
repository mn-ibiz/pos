using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using WorkPeriodStatusEnum = HospitalityPOS.Core.Enums.WorkPeriodStatus;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Minimal MainWindow ViewModel that serves as the shell for the application.
/// Manages navigation and displays the current view.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _clockTimer;

    private WorkPeriod? _currentWorkPeriod;

    /// <summary>
    /// Gets the current view (ViewModel) to display.
    /// </summary>
    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Gets the current time for status bar display.
    /// </summary>
    [ObservableProperty]
    private DateTime _currentTime;

    /// <summary>
    /// Gets the current user's display name.
    /// </summary>
    [ObservableProperty]
    private string _currentUserName = "Not Logged In";

    /// <summary>
    /// Gets the current work period status.
    /// </summary>
    [ObservableProperty]
    private string _workPeriodStatus = "Not Started";

    /// <summary>
    /// Gets a value indicating whether a work period is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isWorkPeriodOpen;

    /// <summary>
    /// Gets the application title.
    /// </summary>
    [ObservableProperty]
    private string _applicationTitle = "Hospitality POS System";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel(
        ILogger logger,
        INavigationService navigationService,
        ISessionService sessionService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        // Subscribe to navigation events
        _navigationService.Navigated += OnNavigated;

        // Subscribe to session events
        _sessionService.UserLoggedIn += OnUserLoggedIn;
        _sessionService.UserLoggedOut += OnUserLoggedOut;

        // Initialize the clock
        CurrentTime = DateTime.Now;
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();

        _logger.Information("MainWindowViewModel initialized");
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
        UpdateWorkPeriodDuration();
    }

    private async void OnUserLoggedIn(object? sender, SessionEventArgs e)
    {
        CurrentUserName = e.User?.FullName ?? "Unknown User";
        _logger.Information("User logged in: {UserName}", CurrentUserName);

        // Refresh work period status when user logs in
        await RefreshWorkPeriodStatusAsync();
    }

    private void OnUserLoggedOut(object? sender, SessionEventArgs e)
    {
        CurrentUserName = "Not Logged In";
        _logger.Information("User logged out. Reason: {Reason}", e.Reason);
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentView = e.ViewModel;
    }

    /// <summary>
    /// Refreshes the work period status from the database.
    /// </summary>
    public async Task RefreshWorkPeriodStatusAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
            _currentWorkPeriod = await workPeriodService.GetCurrentWorkPeriodAsync();

            IsWorkPeriodOpen = _currentWorkPeriod is not null;
            UpdateWorkPeriodDuration();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to refresh work period status");
            WorkPeriodStatus = "Error";
            IsWorkPeriodOpen = false;
        }
    }

    private void UpdateWorkPeriodDuration()
    {
        if (_currentWorkPeriod is null || _currentWorkPeriod.Status != WorkPeriodStatusEnum.Open)
        {
            WorkPeriodStatus = "Not Started";
            IsWorkPeriodOpen = false;
            return;
        }

        var duration = DateTime.UtcNow - _currentWorkPeriod.OpenedAt;
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        WorkPeriodStatus = $"OPEN - {hours}h {minutes:D2}m";
        IsWorkPeriodOpen = true;
    }

    /// <summary>
    /// Releases resources used by the MainWindowViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _clockTimer.Stop();

                _navigationService.Navigated -= OnNavigated;
                _sessionService.UserLoggedIn -= OnUserLoggedIn;
                _sessionService.UserLoggedOut -= OnUserLoggedOut;
            }

            _disposed = true;
        }
    }
}
