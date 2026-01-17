using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Navigation service implementation for view navigation.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<object> _navigationHistory = new();
    private object? _currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving ViewModels.</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            _currentView = value;
        }
    }

    /// <inheritdoc />
    public bool CanGoBack => _navigationHistory.Count > 0;

    /// <inheritdoc />
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <inheritdoc />
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        NavigateTo<TViewModel>(null!);
    }

    /// <inheritdoc />
    public void NavigateTo<TViewModel>(object parameter) where TViewModel : class
    {
        // Push current view to history if it exists
        if (_currentView is not null)
        {
            _navigationHistory.Push(_currentView);
        }

        // Resolve the new ViewModel
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        // If ViewModel supports parameters, pass them
        if (viewModel is INavigationAware navigationAware && parameter is not null)
        {
            navigationAware.OnNavigatedTo(parameter);
        }

        CurrentView = viewModel;

        // Raise the Navigated event
        Navigated?.Invoke(this, new NavigationEventArgs(viewModel, parameter));
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (!CanGoBack) return;

        // Notify current view that we're leaving
        if (_currentView is INavigationAware currentNavigationAware)
        {
            currentNavigationAware.OnNavigatedFrom();
        }

        // Pop the previous view
        var previousView = _navigationHistory.Pop();

        // Notify the view we're returning to
        if (previousView is INavigationAware previousNavigationAware)
        {
            previousNavigationAware.OnNavigatedTo(null);
        }

        CurrentView = previousView;

        // Raise the Navigated event
        Navigated?.Invoke(this, new NavigationEventArgs(previousView));
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _navigationHistory.Clear();
    }
}

/// <summary>
/// Interface for ViewModels that need to receive navigation notifications.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when the ViewModel is navigated to.
    /// </summary>
    /// <param name="parameter">The navigation parameter, if any.</param>
    void OnNavigatedTo(object? parameter);

    /// <summary>
    /// Called when the ViewModel is navigated away from.
    /// </summary>
    void OnNavigatedFrom();
}
