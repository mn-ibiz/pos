namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service interface for navigation between views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current ViewModel being displayed.
    /// </summary>
    object? CurrentView { get; }

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates to the specified ViewModel type.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type to navigate to.</typeparam>
    void NavigateTo<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Navigates to the specified ViewModel type with a parameter.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type to navigate to.</typeparam>
    /// <param name="parameter">The navigation parameter.</param>
    void NavigateTo<TViewModel>(object parameter) where TViewModel : class;

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates back to the previous view (alias for GoBack).
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// Clears the navigation history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Occurs when the current view changes.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the ViewModel that was navigated to.
    /// </summary>
    public object ViewModel { get; }

    /// <summary>
    /// Gets the optional navigation parameter.
    /// </summary>
    public object? Parameter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationEventArgs"/> class.
    /// </summary>
    /// <param name="viewModel">The ViewModel navigated to.</param>
    /// <param name="parameter">The navigation parameter.</param>
    public NavigationEventArgs(object viewModel, object? parameter = null)
    {
        ViewModel = viewModel;
        Parameter = parameter;
    }
}
