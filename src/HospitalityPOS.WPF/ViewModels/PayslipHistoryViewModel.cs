using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class PayslipHistoryViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private int _employeeId;

    [ObservableProperty]
    private string _title = "Payslip History";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Employee? _employee;

    [ObservableProperty]
    private ObservableCollection<Payslip> _payslips = [];

    [ObservableProperty]
    private Payslip? _selectedPayslip;

    [ObservableProperty]
    private int? _filterYear;

    [ObservableProperty]
    private ObservableCollection<int> _years = [];

    [ObservableProperty]
    private decimal _totalEarningsYTD;

    [ObservableProperty]
    private decimal _totalDeductionsYTD;

    [ObservableProperty]
    private decimal _totalNetPayYTD;

    public PayslipHistoryViewModel(
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync(int employeeId)
    {
        _employeeId = employeeId;

        // Populate years for filter
        var currentYear = DateTime.Today.Year;
        Years = new ObservableCollection<int>(Enumerable.Range(currentYear - 5, 6).Reverse());
        FilterYear = currentYear;

        await LoadEmployeeAsync();
        await LoadPayslipsAsync();
    }

    private async Task LoadEmployeeAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();

            Employee = await employeeService.GetEmployeeByIdAsync(_employeeId);
            Title = $"Payslip History - {Employee?.FullName}";
        }
        catch
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task LoadPayslipsAsync()
    {
        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var payslips = await payrollService.GetEmployeePayslipsAsync(_employeeId, FilterYear);
            Payslips = new ObservableCollection<Payslip>(payslips);

            // Calculate YTD totals
            TotalEarningsYTD = payslips.Sum(p => p.TotalEarnings);
            TotalDeductionsYTD = payslips.Sum(p => p.TotalDeductions);
            TotalNetPayYTD = payslips.Sum(p => p.NetPay);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewPayslipAsync()
    {
        if (SelectedPayslip == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var html = await payrollService.GeneratePayslipHtmlAsync(SelectedPayslip.Id);
            await _dialogService.ShowInfoAsync("Payslip preview coming soon.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error viewing payslip: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintPayslipAsync()
    {
        if (SelectedPayslip == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            var html = await payrollService.GeneratePayslipHtmlAsync(SelectedPayslip.Id);
            var filename = $"Payslip_{Employee?.EmployeeNumber}_{SelectedPayslip.PayrollPeriod.PeriodName.Replace(" ", "_")}.pdf";

            await exportService.ExportToPdfAsync(html, filename);
            await _dialogService.ShowSuccessAsync($"Payslip exported to {filename}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error printing payslip: {ex.Message}");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnFilterYearChanged(int? value)
    {
        _ = LoadPayslipsAsync();
    }
}
