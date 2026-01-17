# Story 1.5: MVVM Base Infrastructure

Status: done

## Story

As a developer,
I want the MVVM infrastructure set up for the WPF application,
So that views and business logic are properly separated.

## Acceptance Criteria

1. **Given** the WPF project exists
   **When** MVVM infrastructure is implemented
   **Then** BaseViewModel should implement INotifyPropertyChanged

2. **Given** BaseViewModel exists
   **When** commands are needed
   **Then** RelayCommand/AsyncRelayCommand should be available for command binding

3. **Given** views exist
   **When** navigation is needed
   **Then** NavigationService should allow view switching

4. **Given** dialogs are needed
   **When** modal interactions occur
   **Then** DialogService should provide modal dialog support

5. **Given** services are needed in ViewModels
   **When** the application starts
   **Then** dependency injection container should be configured

## Tasks / Subtasks

- [x] Task 1: Create BaseViewModel (AC: #1)
  - [x] Create BaseViewModel class inheriting from ObservableObject (ViewModelBase already existed)
  - [x] Add IsBusy property for loading states
  - [x] Add Title property
  - [x] Add error handling support (ExecuteAsync methods with try/catch)

- [x] Task 2: Create Command Infrastructure (AC: #2)
  - [x] Verify CommunityToolkit.Mvvm is installed (already in project)
  - [x] Create example using RelayCommand attribute (ClearError command in ViewModelBase)
  - [x] Create example using AsyncRelayCommand (ShowAboutAsync, ExitApplicationAsync in MainViewModel)
  - [x] Document command patterns in Dev Notes

- [x] Task 3: Create NavigationService (AC: #3)
  - [x] Create INavigationService interface
  - [x] Create NavigationService implementation
  - [x] Implement NavigateTo<TViewModel> method
  - [x] Implement GoBack method
  - [x] Create MainWindow with ContentControl for view hosting

- [x] Task 4: Create DialogService (AC: #4)
  - [x] Create IDialogService interface
  - [x] Implement ShowMessageAsync method
  - [x] Implement ShowConfirmationAsync method
  - [x] Implement ShowErrorAsync method
  - [x] Implement ShowInputAsync method (with InputDialog)

- [x] Task 5: Configure Dependency Injection (AC: #5)
  - [x] Configure IServiceCollection in App.xaml.cs
  - [x] DbContext already registered via AddInfrastructureServices
  - [x] All repositories already registered via AddInfrastructureServices
  - [x] NavigationService and DialogService registered
  - [x] MainViewModel registered
  - [x] Create ViewModelLocator for XAML binding

## Dev Notes

### BaseViewModel with CommunityToolkit.Mvvm

```csharp
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    protected void SetBusy(bool busy, string? message = null)
    {
        IsBusy = busy;
        ErrorMessage = message;
    }
}
```

### C# 14 Field-Backed Properties Alternative

```csharp
// C# 14 introduces the 'field' keyword for simpler property implementations
// This is an alternative to [ObservableProperty] for cases requiring custom logic
public partial class ProductViewModel : ObservableObject
{
    // C# 14 field-backed property with validation
    public string Name
    {
        get => field ?? string.Empty;
        set => SetProperty(ref field, value);
    }

    // C# 14 field-backed property with business rules
    public decimal Price
    {
        get => field;
        set
        {
            if (value < 0)
                throw new ArgumentException("Price cannot be negative");
            SetProperty(ref field, value);
        }
    }

    // C# 14 field-backed property with lazy initialization
    public string DisplayName
    {
        get => field ??= $"{Code} - {Name}";
    }
}
```

### C# 14 Extension Members for Validation

```csharp
// Extension members provide cleaner validation patterns
public extension class ValidationExtensions for string
{
    public bool IsValidMpesaCode => Length == 10 && All(char.IsLetterOrDigit);
    public bool IsValidPhone => Length >= 10 && All(c => char.IsDigit(c) || c == '+');
}

// Usage in ViewModels
if (mpesaCode.IsValidMpesaCode)
{
    await ProcessMpesaPaymentAsync(mpesaCode);
}
```

### ViewModel Example with Commands

```csharp
public partial class LoginViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(IUserService userService, INavigationService navigationService)
    {
        _userService = userService;
        _navigationService = navigationService;
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;
            var user = await _userService.AuthenticateAsync(Username, Password);
            if (user != null)
            {
                _navigationService.NavigateTo<POSViewModel>();
            }
            else
            {
                ErrorMessage = "Invalid username or password";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### NavigationService Interface

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    void NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel;
    void GoBack();
    bool CanGoBack { get; }
}
```

### DialogService Interface

```csharp
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string message);
    Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "");
}
```

### App.xaml.cs DI Configuration

```csharp
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // DbContext
        services.AddDbContext<HospitalityDbContext>();

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        // ... other repositories

        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISalesService, SalesService>();
        // ... other services

        // Navigation and Dialog
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<POSViewModel>();
        services.AddTransient<MainViewModel>();
        // ... other ViewModels

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

### MainWindow.xaml Structure

```xml
<Window x:Class="HospitalityPOS.WPF.MainWindow">
    <Grid>
        <ContentControl Content="{Binding CurrentView}" />
    </Grid>
</Window>
```

### ViewModelLocator (Optional)

```csharp
public class ViewModelLocator
{
    public LoginViewModel Login => App.ServiceProvider.GetRequiredService<LoginViewModel>();
    public POSViewModel POS => App.ServiceProvider.GetRequiredService<POSViewModel>();
    public MainViewModel Main => App.ServiceProvider.GetRequiredService<MainViewModel>();
}
```

### References
- [Source: _bmad-output/architecture.md#2-System-Architecture]
- [Source: _bmad-output/project-context.md#Critical-Patterns]
- [CommunityToolkit.Mvvm Documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- ViewModelBase already existed from Story 1-1 with IsBusy, BusyMessage, ErrorMessage, ExecuteAsync
- Added Title property to ViewModelBase
- Created INavigationService with NavigateTo, GoBack, CanGoBack, and Navigated event
- Created NavigationService with navigation history stack
- Created INavigationAware interface for ViewModels receiving navigation callbacks
- Created IDialogService with message, confirmation, error, warning, and input dialogs
- Created DialogService using WPF MessageBox and custom InputDialog
- Created InputDialog with support for text and password input
- Created MainViewModel as shell ViewModel managing navigation and status bar
- Created ViewModelLocator for XAML-based ViewModel resolution
- Updated MainWindow.xaml with ContentControl for view hosting and data binding to MainViewModel
- Updated App.xaml.cs to register NavigationService, DialogService, and MainViewModel

### File List
**New Files:**
- src/HospitalityPOS.WPF/Services/INavigationService.cs
- src/HospitalityPOS.WPF/Services/NavigationService.cs
- src/HospitalityPOS.WPF/Services/IDialogService.cs
- src/HospitalityPOS.WPF/Services/DialogService.cs
- src/HospitalityPOS.WPF/Views/Dialogs/InputDialog.xaml
- src/HospitalityPOS.WPF/Views/Dialogs/InputDialog.xaml.cs
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs
- src/HospitalityPOS.WPF/ViewModels/ViewModelLocator.cs

**Modified Files:**
- src/HospitalityPOS.WPF/ViewModels/ViewModelBase.cs (added Title property)
- src/HospitalityPOS.WPF/Views/MainWindow.xaml (added ViewModelLocator, ContentControl binding)
- src/HospitalityPOS.WPF/App.xaml.cs (added service and ViewModel registrations)

## Senior Developer Review

### Review Date
2025-12-30

### Review Result
**APPROVED WITH FIXES** - 6 issues identified, all addressed

### Issues Found

| # | Severity | File | Issue | Resolution |
|---|----------|------|-------|------------|
| 1 | HIGH | MainViewModel.cs | MainViewModel creates DispatcherTimer and subscribes to NavigationService.Navigated event but doesn't implement IDisposable - causes resource leaks | FIXED: Added IDisposable implementation with proper cleanup of timer and event unsubscription |
| 2 | MEDIUM | DialogService.cs | Missing null validation on string parameters - methods could throw NullReferenceException | FIXED: Added ArgumentException.ThrowIfNullOrWhiteSpace() to all public methods |
| 3 | MEDIUM | DialogService.cs | ShowPinEntryAsync accepts maxLength but doesn't validate it's positive | FIXED: Added ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1) |
| 4 | MEDIUM | NavigationService.cs | CurrentView property doesn't raise PropertyChanged when set - MainViewModel.CurrentView won't update via binding | ACCEPTABLE: NavigationService uses Navigated event for notification; MainViewModel subscribes to this event to update its own CurrentView property which does notify. Design is intentional - navigation state change is communicated via dedicated event rather than INotifyPropertyChanged. |
| 5 | LOW | NavigationService.cs | INavigationAware interface defined in NavigationService.cs file | ACCEPTABLE: Small supporting interface closely related to the service - keeping in same file is acceptable |
| 6 | LOW | INavigationService.cs | NavigationEventArgs class defined in interface file | ACCEPTABLE: Small EventArgs class closely related to the interface - keeping in same file is acceptable |

### Code Quality Assessment
- **Architecture**: Clean MVVM separation with proper abstraction boundaries
- **Patterns**: Repository pattern, Service Locator (ViewModelLocator), Observer (events)
- **Testability**: All services are interface-based, easily mockable
- **Resource Management**: Proper IDisposable implementation on MainViewModel

## Change Log
- 2025-12-30: Implementation completed - Full MVVM infrastructure with navigation and dialog services
- 2025-12-30: Code review completed - Fixed HIGH and MEDIUM issues, documented ACCEPTABLE items
