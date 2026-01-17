using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class EmployeeEditorViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private int? _employeeId;

    [ObservableProperty]
    private string _title = "New Employee";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    // Personal Information
    [ObservableProperty]
    private string _employeeNumber = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string? _nationalId;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private DateTime? _dateOfBirth;

    // Employment Information
    [ObservableProperty]
    private DateTime _hireDate = DateTime.Today;

    [ObservableProperty]
    private string? _department;

    [ObservableProperty]
    private string? _position;

    [ObservableProperty]
    private EmploymentType _employmentType = EmploymentType.FullTime;

    [ObservableProperty]
    private PayFrequency _payFrequency = PayFrequency.Monthly;

    // Salary & Banking
    [ObservableProperty]
    private decimal _basicSalary;

    [ObservableProperty]
    private string? _bankName;

    [ObservableProperty]
    private string? _bankAccountNumber;

    // Statutory IDs
    [ObservableProperty]
    private string? _taxId;

    [ObservableProperty]
    private string? _nssfNumber;

    [ObservableProperty]
    private string? _nhifNumber;

    // Link to User
    [ObservableProperty]
    private int? _userId;

    [ObservableProperty]
    private ObservableCollection<User> _users = [];

    [ObservableProperty]
    private User? _selectedUser;

    // Salary Components
    [ObservableProperty]
    private ObservableCollection<SalaryComponent> _availableComponents = [];

    [ObservableProperty]
    private ObservableCollection<EmployeeSalaryComponent> _employeeSalaryComponents = [];

    public IEnumerable<EmploymentType> EmploymentTypes => Enum.GetValues<EmploymentType>();
    public IEnumerable<PayFrequency> PayFrequencies => Enum.GetValues<PayFrequency>();

    public EmployeeEditorViewModel(
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync(int? employeeId = null)
    {
        _employeeId = employeeId;
        IsEditing = employeeId.HasValue;
        Title = IsEditing ? "Edit Employee" : "New Employee";

        await LoadUsersAsync();
        await LoadSalaryComponentsAsync();

        if (IsEditing)
        {
            await LoadEmployeeAsync();
        }
        else
        {
            await GenerateEmployeeNumberAsync();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var users = await userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(users);
        }
        catch
        {
            // Users list is optional
        }
    }

    private async Task LoadSalaryComponentsAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var components = await employeeService.GetAllSalaryComponentsAsync();
            AvailableComponents = new ObservableCollection<SalaryComponent>(components.Where(c => !c.IsStatutory));
        }
        catch
        {
            // Components list is optional
        }
    }

    private async Task GenerateEmployeeNumberAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            EmployeeNumber = await employeeService.GenerateEmployeeNumberAsync();
        }
        catch
        {
            EmployeeNumber = $"EMP{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    private async Task LoadEmployeeAsync()
    {
        if (!_employeeId.HasValue) return;

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var employee = await employeeService.GetEmployeeByIdAsync(_employeeId.Value);
            if (employee == null)
            {
                await _dialogService.ShowErrorAsync("Employee not found.");
                _navigationService.GoBack();
                return;
            }

            // Personal Info
            EmployeeNumber = employee.EmployeeNumber;
            FirstName = employee.FirstName;
            LastName = employee.LastName;
            NationalId = employee.NationalId;
            Phone = employee.Phone;
            Email = employee.Email;
            Address = employee.Address;
            DateOfBirth = employee.DateOfBirth;

            // Employment Info
            HireDate = employee.HireDate;
            Department = employee.Department;
            Position = employee.Position;
            EmploymentType = employee.EmploymentType;
            PayFrequency = employee.PayFrequency;

            // Salary & Banking
            BasicSalary = employee.BasicSalary;
            BankName = employee.BankName;
            BankAccountNumber = employee.BankAccountNumber;

            // Statutory
            TaxId = employee.TaxId;
            NssfNumber = employee.NssfNumber;
            NhifNumber = employee.NhifNumber;

            // User link
            UserId = employee.UserId;
            SelectedUser = Users.FirstOrDefault(u => u.Id == employee.UserId);

            // Salary components
            var employeeComponents = await employeeService.GetEmployeeSalaryComponentsAsync(_employeeId.Value);
            EmployeeSalaryComponents = new ObservableCollection<EmployeeSalaryComponent>(employeeComponents);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            await _dialogService.ShowErrorAsync("First name and last name are required.");
            return;
        }

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            Employee employee;

            if (IsEditing && _employeeId.HasValue)
            {
                employee = await employeeService.GetEmployeeByIdAsync(_employeeId.Value)
                    ?? throw new InvalidOperationException("Employee not found.");
            }
            else
            {
                employee = new Employee();
            }

            // Update properties
            employee.EmployeeNumber = EmployeeNumber;
            employee.FirstName = FirstName;
            employee.LastName = LastName;
            employee.NationalId = NationalId;
            employee.Phone = Phone;
            employee.Email = Email;
            employee.Address = Address;
            employee.DateOfBirth = DateOfBirth;
            employee.HireDate = HireDate;
            employee.Department = Department;
            employee.Position = Position;
            employee.EmploymentType = EmploymentType;
            employee.PayFrequency = PayFrequency;
            employee.BasicSalary = BasicSalary;
            employee.BankName = BankName;
            employee.BankAccountNumber = BankAccountNumber;
            employee.TaxId = TaxId;
            employee.NssfNumber = NssfNumber;
            employee.NhifNumber = NhifNumber;
            employee.UserId = SelectedUser?.Id;

            if (IsEditing)
            {
                await employeeService.UpdateEmployeeAsync(employee);
            }
            else
            {
                await employeeService.CreateEmployeeAsync(employee);
            }

            await _dialogService.ShowSuccessAsync("Employee saved successfully.");
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error saving employee: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task AddSalaryComponentAsync()
    {
        if (!_employeeId.HasValue)
        {
            await _dialogService.ShowErrorAsync("Please save the employee first before adding salary components.");
            return;
        }

        // Show dialog to select component and amount
        // For simplicity, we'll add a placeholder implementation
        await _dialogService.ShowInfoAsync("Salary component editor coming soon.");
    }
}
