using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class EmployeesViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<Employee> _employees = [];

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private bool _showInactive;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private decimal _totalMonthlyPayroll;

    public EmployeesViewModel(
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadEmployeesAsync();
    }

    [RelayCommand]
    private async Task LoadEmployeesAsync()
    {
        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var employees = await employeeService.GetAllEmployeesAsync(ShowInactive);
            Employees = new ObservableCollection<Employee>(employees);

            ActiveCount = await employeeService.GetActiveEmployeeCountAsync();
            TotalCount = employees.Count;
            TotalMonthlyPayroll = await employeeService.GetTotalMonthlyPayrollAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            await LoadEmployeesAsync();
            return;
        }

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            var employees = await employeeService.SearchEmployeesAsync(SearchTerm);
            Employees = new ObservableCollection<Employee>(employees);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CreateEmployee()
    {
        _navigationService.NavigateTo<EmployeeEditorViewModel>();
    }

    [RelayCommand]
    private void EditEmployee()
    {
        if (SelectedEmployee == null) return;
        _navigationService.NavigateTo<EmployeeEditorViewModel>(SelectedEmployee.Id);
    }

    [RelayCommand]
    private async Task TerminateEmployeeAsync()
    {
        if (SelectedEmployee == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Terminate Employee",
            $"Are you sure you want to terminate {SelectedEmployee.FullName}?");

        if (!confirm) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            await employeeService.TerminateEmployeeAsync(SelectedEmployee.Id, DateTime.Today);
            await _dialogService.ShowSuccessAsync("Employee terminated successfully.");
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error terminating employee: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReactivateEmployeeAsync()
    {
        if (SelectedEmployee == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            await employeeService.ReactivateEmployeeAsync(SelectedEmployee.Id);
            await _dialogService.ShowSuccessAsync("Employee reactivated successfully.");
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error reactivating employee: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ViewAttendance()
    {
        if (SelectedEmployee == null) return;
        _navigationService.NavigateTo<AttendanceViewModel>(SelectedEmployee.Id);
    }

    [RelayCommand]
    private void ViewPayslips()
    {
        if (SelectedEmployee == null) return;
        _navigationService.NavigateTo<PayslipHistoryViewModel>(SelectedEmployee.Id);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnShowInactiveChanged(bool value)
    {
        _ = LoadEmployeesAsync();
    }

    partial void OnSearchTermChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadEmployeesAsync();
        }
    }
}
