using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the payment methods management screen.
/// </summary>
public partial class PaymentMethodsViewModel : ViewModelBase, INavigationAware
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of payment methods.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _paymentMethods = [];

    /// <summary>
    /// Gets or sets the selected payment method.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditMethodCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteMethodCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private PaymentMethod? _selectedMethod;

    /// <summary>
    /// Gets the total count of payment methods.
    /// </summary>
    [ObservableProperty]
    private int _totalMethodCount;

    /// <summary>
    /// Gets the count of active payment methods.
    /// </summary>
    [ObservableProperty]
    private int _activeMethodCount;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentMethodsViewModel"/> class.
    /// </summary>
    public PaymentMethodsViewModel(
        ILogger logger,
        IPaymentMethodService paymentMethodService,
        INavigationService navigationService,
        IDialogService dialogService)
        : base(logger)
    {
        _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Title = "Payment Methods";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadPaymentMethodsAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Clean up if needed
    }

    #region Commands

    /// <summary>
    /// Loads all payment methods.
    /// </summary>
    [RelayCommand]
    private async Task LoadPaymentMethodsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var methods = await _paymentMethodService.GetAllAsync();
            PaymentMethods = new ObservableCollection<PaymentMethod>(methods);

            TotalMethodCount = methods.Count;
            ActiveMethodCount = methods.Count(m => m.IsActive);

            _logger.Debug("Loaded {MethodCount} payment methods ({ActiveCount} active)",
                TotalMethodCount, ActiveMethodCount);
        }, "Loading payment methods...").ConfigureAwait(true);
    }

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    [RelayCommand]
    private async Task CreateMethodAsync()
    {
        if (!RequirePermission(PermissionNames.Settings.Modify, "create payment methods"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to create payment methods.");
            return;
        }

        var result = await _dialogService.ShowPaymentMethodEditorDialogAsync(null);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new PaymentMethodDto
                {
                    Name = result.Name,
                    Code = result.Code,
                    Type = result.Type,
                    Description = result.Description,
                    IsActive = result.IsActive,
                    RequiresReference = result.RequiresReference,
                    ReferenceLabel = result.ReferenceLabel,
                    ReferenceMinLength = result.ReferenceMinLength,
                    ReferenceMaxLength = result.ReferenceMaxLength,
                    SupportsChange = result.SupportsChange,
                    OpensDrawer = result.OpensDrawer,
                    DisplayOrder = result.DisplayOrder,
                    BackgroundColor = result.BackgroundColor
                };

                await _paymentMethodService.CreateAsync(dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Payment method '{result.Name}' has been created.");
                await LoadPaymentMethodsAsync();
            }, "Creating payment method...").ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Edits the selected payment method.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditMethod))]
    private async Task EditMethodAsync()
    {
        if (SelectedMethod is null) return;

        if (!RequirePermission(PermissionNames.Settings.Modify, "edit payment methods"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to edit payment methods.");
            return;
        }

        var result = await _dialogService.ShowPaymentMethodEditorDialogAsync(SelectedMethod);
        if (result is not null)
        {
            await ExecuteAsync(async () =>
            {
                var dto = new PaymentMethodDto
                {
                    Name = result.Name,
                    Code = result.Code,
                    Type = result.Type,
                    Description = result.Description,
                    IsActive = result.IsActive,
                    RequiresReference = result.RequiresReference,
                    ReferenceLabel = result.ReferenceLabel,
                    ReferenceMinLength = result.ReferenceMinLength,
                    ReferenceMaxLength = result.ReferenceMaxLength,
                    SupportsChange = result.SupportsChange,
                    OpensDrawer = result.OpensDrawer,
                    DisplayOrder = result.DisplayOrder,
                    BackgroundColor = result.BackgroundColor
                };

                await _paymentMethodService.UpdateAsync(SelectedMethod.Id, dto, SessionService.CurrentUserId);
                await _dialogService.ShowMessageAsync("Success", $"Payment method '{result.Name}' has been updated.");
                await LoadPaymentMethodsAsync();
            }, "Updating payment method...").ConfigureAwait(true);
        }
    }

    private bool CanEditMethod() => SelectedMethod is not null;

    /// <summary>
    /// Deletes the selected payment method.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteMethod))]
    private async Task DeleteMethodAsync()
    {
        if (SelectedMethod is null) return;

        if (!RequirePermission(PermissionNames.Settings.Modify, "delete payment methods"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to delete payment methods.");
            return;
        }

        // Check if method has payments
        if (await _paymentMethodService.HasPaymentsAsync(SelectedMethod.Id))
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Delete",
                "This payment method has been used for payments. Please deactivate it instead.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Payment Method",
            $"Are you sure you want to delete '{SelectedMethod.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            var deleted = await _paymentMethodService.DeleteAsync(SelectedMethod.Id, SessionService.CurrentUserId);
            if (deleted)
            {
                await _dialogService.ShowMessageAsync(
                    "Deleted",
                    $"Payment method '{SelectedMethod.Name}' has been deleted.");
                await LoadPaymentMethodsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    "Delete Failed",
                    "Failed to delete the payment method. Please try again.");
            }
        }, "Deleting payment method...").ConfigureAwait(true);
    }

    private bool CanDeleteMethod() => SelectedMethod is not null;

    /// <summary>
    /// Toggles the active status of the selected payment method.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleActive))]
    private async Task ToggleActiveAsync()
    {
        if (SelectedMethod is null) return;

        if (!RequirePermission(PermissionNames.Settings.Modify, "change payment method status"))
        {
            await _dialogService.ShowErrorAsync("Permission Denied", ErrorMessage ?? "You don't have permission to change payment method status.");
            return;
        }

        var newStatus = !SelectedMethod.IsActive;
        var action = newStatus ? "activate" : "deactivate";

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"{(newStatus ? "Activate" : "Deactivate")} Payment Method",
            $"Are you sure you want to {action} '{SelectedMethod.Name}'?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _paymentMethodService.ToggleActiveAsync(SelectedMethod.Id, SessionService.CurrentUserId);
            await _dialogService.ShowMessageAsync(
                "Success",
                $"Payment method '{SelectedMethod.Name}' has been {(newStatus ? "activated" : "deactivated")}.");
            await LoadPaymentMethodsAsync();
        }, $"{(newStatus ? "Activating" : "Deactivating")} payment method...").ConfigureAwait(true);
    }

    private bool CanToggleActive() => SelectedMethod is not null;

    /// <summary>
    /// Moves the selected payment method up in display order.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private async Task MoveUpAsync()
    {
        if (SelectedMethod is null) return;

        await ExecuteAsync(async () =>
        {
            var ordered = PaymentMethods.OrderBy(m => m.DisplayOrder).ToList();
            var currentIndex = ordered.FindIndex(m => m.Id == SelectedMethod.Id);

            if (currentIndex > 0)
            {
                var orderings = new List<PaymentMethodOrderDto>
                {
                    new() { PaymentMethodId = SelectedMethod.Id, DisplayOrder = ordered[currentIndex - 1].DisplayOrder },
                    new() { PaymentMethodId = ordered[currentIndex - 1].Id, DisplayOrder = SelectedMethod.DisplayOrder }
                };

                await _paymentMethodService.ReorderAsync(orderings, SessionService.CurrentUserId);
                await LoadPaymentMethodsAsync();
            }
        }, "Reordering...").ConfigureAwait(true);
    }

    private bool CanMoveUp()
    {
        if (SelectedMethod is null) return false;
        var ordered = PaymentMethods.OrderBy(m => m.DisplayOrder).ToList();
        return ordered.FindIndex(m => m.Id == SelectedMethod.Id) > 0;
    }

    /// <summary>
    /// Moves the selected payment method down in display order.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private async Task MoveDownAsync()
    {
        if (SelectedMethod is null) return;

        await ExecuteAsync(async () =>
        {
            var ordered = PaymentMethods.OrderBy(m => m.DisplayOrder).ToList();
            var currentIndex = ordered.FindIndex(m => m.Id == SelectedMethod.Id);

            if (currentIndex < ordered.Count - 1)
            {
                var orderings = new List<PaymentMethodOrderDto>
                {
                    new() { PaymentMethodId = SelectedMethod.Id, DisplayOrder = ordered[currentIndex + 1].DisplayOrder },
                    new() { PaymentMethodId = ordered[currentIndex + 1].Id, DisplayOrder = SelectedMethod.DisplayOrder }
                };

                await _paymentMethodService.ReorderAsync(orderings, SessionService.CurrentUserId);
                await LoadPaymentMethodsAsync();
            }
        }, "Reordering...").ConfigureAwait(true);
    }

    private bool CanMoveDown()
    {
        if (SelectedMethod is null) return false;
        var ordered = PaymentMethods.OrderBy(m => m.DisplayOrder).ToList();
        var currentIndex = ordered.FindIndex(m => m.Id == SelectedMethod.Id);
        return currentIndex < ordered.Count - 1;
    }

    /// <summary>
    /// Goes back to the previous screen.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}
