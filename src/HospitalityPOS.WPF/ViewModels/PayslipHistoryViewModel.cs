using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class PayslipHistoryViewModel : ObservableObject, INavigationAware
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
    private int _payslipCount;

    [ObservableProperty]
    private decimal _totalGrossEarnings;

    [ObservableProperty]
    private decimal _totalDeductions;

    [ObservableProperty]
    private decimal _totalNetPay;

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

    #region INavigationAware

    public void OnNavigatedTo(object? parameter)
    {
        int? employeeId = parameter switch
        {
            int id => id,
            Employee emp => emp.Id,
            _ => null
        };

        if (employeeId.HasValue)
        {
            _ = InitializeAsync(employeeId.Value);
        }
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

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
            Payslips = new ObservableCollection<Payslip>(payslips.OrderByDescending(p => p.PayrollPeriod?.PayDate));

            // Calculate YTD totals
            PayslipCount = Payslips.Count;
            TotalGrossEarnings = payslips.Sum(p => p.GrossEarnings);
            TotalDeductions = payslips.Sum(p => p.TotalDeductions);
            TotalNetPay = payslips.Sum(p => p.NetPay);
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
            var employeeName = Employee?.FullName ?? "Employee";
            var periodName = SelectedPayslip.PayrollPeriod?.PeriodName ?? "Payslip";
            var filename = $"Payslip_{Employee?.EmployeeNumber}_{periodName.Replace(" ", "_")}.pdf";

            await _dialogService.ShowHtmlPreviewAsync($"Payslip - {employeeName}", periodName, html, filename);
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
    private async Task ExportAllPayslipsAsync()
    {
        if (!Payslips.Any())
        {
            await _dialogService.ShowInfoAsync("No Payslips", "There are no payslips to export.");
            return;
        }

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            var exportData = Payslips.Select(p => new
            {
                Period = p.PayrollPeriod?.PeriodName ?? "Unknown",
                PayDate = p.PayrollPeriod?.PayDate.ToString("d") ?? "",
                BasicSalary = p.BasicSalary,
                Allowances = p.Allowances,
                Overtime = p.OvertimePay,
                GrossEarnings = p.GrossEarnings,
                NHIF = p.NhifDeduction,
                NSSF = p.NssfDeduction,
                PAYE = p.PayeDeduction,
                OtherDeductions = p.OtherDeductions,
                TotalDeductions = p.TotalDeductions,
                NetPay = p.NetPay,
                Status = p.IsPaid ? "Paid" : "Pending"
            }).ToList();

            var employeeName = Employee?.FullName?.Replace(" ", "_") ?? "Employee";
            var filename = $"PayslipHistory_{employeeName}_{FilterYear}";

            await exportService.ExportToExcelAsync(exportData, filename, "Payslip History");
            await _dialogService.ShowSuccessAsync("Export Complete", $"Payslip history exported to {filename}.xlsx");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error exporting payslips: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
