using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for customer loyalty program enrollment.
/// Supports quick enrollment with phone number as the primary identifier.
/// </summary>
public partial class CustomerEnrollmentViewModel : ViewModelBase, INavigationAware
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the phone number (required, Kenya format).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnrollCommand))]
    [NotifyPropertyChangedFor(nameof(FormattedPhoneNumber))]
    private string _phoneNumber = string.Empty;

    /// <summary>
    /// Gets or sets the customer name (optional).
    /// </summary>
    [ObservableProperty]
    private string? _name;

    /// <summary>
    /// Gets or sets the customer email (optional).
    /// </summary>
    [ObservableProperty]
    private string? _email;

    /// <summary>
    /// Gets the formatted phone number for display.
    /// </summary>
    public string FormattedPhoneNumber
    {
        get
        {
            var normalized = _loyaltyService.NormalizePhoneNumber(PhoneNumber);
            if (string.IsNullOrEmpty(normalized)) return string.Empty;
            return $"+{normalized[..3]} {normalized[3..6]} {normalized[6..]}";
        }
    }

    /// <summary>
    /// Gets or sets the phone number validation error.
    /// </summary>
    [ObservableProperty]
    private string? _phoneError;

    /// <summary>
    /// Gets or sets the email validation error.
    /// </summary>
    [ObservableProperty]
    private string? _emailError;

    /// <summary>
    /// Gets or sets whether the enrollment is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnrollCommand))]
    private bool _isEnrolling;

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    [ObservableProperty]
    private string? _successMessage;

    /// <summary>
    /// Gets or sets the enrolled member details.
    /// </summary>
    [ObservableProperty]
    private LoyaltyMemberDto? _enrolledMember;

    /// <summary>
    /// Gets a value indicating whether enrollment was successful.
    /// </summary>
    public bool IsEnrollmentComplete => EnrolledMember != null;

    /// <summary>
    /// Gets a value indicating whether phone number is valid.
    /// </summary>
    public bool IsPhoneValid => !string.IsNullOrEmpty(PhoneNumber) &&
                                _loyaltyService.ValidatePhoneNumber(PhoneNumber);

    /// <summary>
    /// Gets or sets whether a duplicate member was found.
    /// </summary>
    [ObservableProperty]
    private bool _isDuplicateFound;

    /// <summary>
    /// Gets or sets the existing member details when duplicate is found.
    /// </summary>
    [ObservableProperty]
    private LoyaltyMemberDto? _existingMember;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerEnrollmentViewModel"/> class.
    /// </summary>
    public CustomerEnrollmentViewModel(
        ILoyaltyService loyaltyService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService,
        ILogger logger) : base(logger)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        Title = "Enroll Customer";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        ClearForm();

        // If a phone number was passed, pre-fill it
        if (parameter is string phoneNumber)
        {
            PhoneNumber = phoneNumber;
            _ = CheckDuplicateAsync();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    #region Validation

    partial void OnPhoneNumberChanged(string value)
    {
        ValidatePhone();
        IsDuplicateFound = false;
        ExistingMember = null;
    }

    partial void OnEmailChanged(string? value)
    {
        ValidateEmail();
    }

    private bool ValidatePhone()
    {
        PhoneError = null;

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneError = "Phone number is required.";
            return false;
        }

        if (!_loyaltyService.ValidatePhoneNumber(PhoneNumber))
        {
            PhoneError = "Enter a valid Kenya phone number (e.g., 0712345678).";
            return false;
        }

        return true;
    }

    private bool ValidateEmail()
    {
        EmailError = null;

        if (!string.IsNullOrWhiteSpace(Email))
        {
            // Simple email validation
            if (!Email.Contains('@') || !Email.Contains('.'))
            {
                EmailError = "Enter a valid email address.";
                return false;
            }
        }

        return true;
    }

    private bool ValidateAll()
    {
        var phoneValid = ValidatePhone();
        var emailValid = ValidateEmail();

        return phoneValid && emailValid;
    }

    #endregion

    #region Commands

    private bool CanEnroll() => !IsEnrolling && !string.IsNullOrWhiteSpace(PhoneNumber);

    /// <summary>
    /// Checks for duplicate before enrollment.
    /// </summary>
    [RelayCommand]
    private async Task CheckDuplicateAsync()
    {
        if (!ValidatePhone()) return;

        await ExecuteAsync(async () =>
        {
            var existingMember = await _loyaltyService.GetByPhoneAsync(PhoneNumber)
                .ConfigureAwait(true);

            if (existingMember != null)
            {
                IsDuplicateFound = true;
                ExistingMember = existingMember;
            }
            else
            {
                IsDuplicateFound = false;
                ExistingMember = null;
            }
        }, "Checking...").ConfigureAwait(true);
    }

    /// <summary>
    /// Enrolls the customer in the loyalty program.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEnroll))]
    private async Task EnrollAsync()
    {
        if (!ValidateAll()) return;

        // Get current user ID
        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "You must be logged in to enroll customers.");
            return;
        }

        try
        {
            IsEnrolling = true;
            ErrorMessage = null;
            SuccessMessage = null;
            EnrolledMember = null;

            var dto = new EnrollCustomerDto
            {
                PhoneNumber = PhoneNumber,
                Name = Name?.Trim(),
                Email = Email?.Trim()
            };

            var result = await _loyaltyService.EnrollCustomerAsync(dto, currentUserId)
                .ConfigureAwait(true);

            if (result.IsSuccess)
            {
                EnrolledMember = result.Member;
                SuccessMessage = $"Customer enrolled successfully!\nMembership #: {result.Member?.MembershipNumber}";
                OnPropertyChanged(nameof(IsEnrollmentComplete));

                _logger.Information(
                    "Customer enrolled: {MembershipNumber}, Phone: {Phone}",
                    result.Member?.MembershipNumber,
                    PhoneNumber);

                await _dialogService.ShowMessageAsync(
                    "Enrollment Successful",
                    $"Customer has been enrolled in the loyalty program.\n\n" +
                    $"Membership Number: {result.Member?.MembershipNumber}\n" +
                    $"A welcome SMS has been sent to {PhoneNumber}.");
            }
            else if (result.IsDuplicate)
            {
                IsDuplicateFound = true;
                ExistingMember = result.Member;
                ErrorMessage = "This phone number is already registered.";

                await _dialogService.ShowWarningAsync(
                    "Already Enrolled",
                    $"This customer is already a loyalty member.\n\n" +
                    $"Membership Number: {result.Member?.MembershipNumber}\n" +
                    $"Points Balance: {result.Member?.PointsBalance:N0}");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Enrollment failed. Please try again.";
                _logger.Warning("Enrollment failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error enrolling customer");
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsEnrolling = false;
        }
    }

    /// <summary>
    /// Clears the form and starts a new enrollment.
    /// </summary>
    [RelayCommand]
    private void ClearForm()
    {
        PhoneNumber = string.Empty;
        Name = null;
        Email = null;
        PhoneError = null;
        EmailError = null;
        ErrorMessage = null;
        SuccessMessage = null;
        EnrolledMember = null;
        IsDuplicateFound = false;
        ExistingMember = null;
        OnPropertyChanged(nameof(IsEnrollmentComplete));
    }

    /// <summary>
    /// Cancels and returns to the previous view.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Shows the existing member details.
    /// </summary>
    [RelayCommand]
    private async Task ViewExistingMemberAsync()
    {
        if (ExistingMember == null) return;

        await _dialogService.ShowMessageAsync(
            "Existing Member",
            $"Name: {ExistingMember.Name ?? "(Not provided)"}\n" +
            $"Phone: {ExistingMember.PhoneNumber}\n" +
            $"Membership #: {ExistingMember.MembershipNumber}\n" +
            $"Tier: {ExistingMember.Tier}\n" +
            $"Points Balance: {ExistingMember.PointsBalance:N0}\n" +
            $"Enrolled: {ExistingMember.EnrolledAt:d}\n" +
            $"Visit Count: {ExistingMember.VisitCount}");
    }

    /// <summary>
    /// Enrolls another customer after successful enrollment.
    /// </summary>
    [RelayCommand]
    private void EnrollAnother()
    {
        ClearForm();
    }

    #endregion
}
