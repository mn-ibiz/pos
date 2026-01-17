using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

public partial class PayrollViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<PayrollPeriod> _payrollPeriods = [];

    [ObservableProperty]
    private PayrollPeriod? _selectedPeriod;

    [ObservableProperty]
    private ObservableCollection<Payslip> _payslips = [];

    [ObservableProperty]
    private Payslip? _selectedPayslip;

    [ObservableProperty]
    private PayrollSummary? _summary;

    [ObservableProperty]
    private int? _filterYear;

    [ObservableProperty]
    private ObservableCollection<int> _years = [];

    public PayrollViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _sessionService = sessionService;
    }

    public async Task InitializeAsync()
    {
        // Populate years for filter
        var currentYear = DateTime.Today.Year;
        Years = new ObservableCollection<int>(Enumerable.Range(currentYear - 5, 6).Reverse());
        FilterYear = currentYear;

        await LoadPayrollPeriodsAsync();
    }

    [RelayCommand]
    private async Task LoadPayrollPeriodsAsync()
    {
        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var periods = await payrollService.GetAllPayrollPeriodsAsync(FilterYear);
            PayrollPeriods = new ObservableCollection<PayrollPeriod>(periods);

            if (PayrollPeriods.Any())
            {
                SelectedPeriod = PayrollPeriods.First();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreatePeriodAsync()
    {
        // Get the next month period
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, today.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var payDate = endDate.AddDays(5);
        var periodName = $"{startDate:MMMM yyyy}";

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            await payrollService.CreatePayrollPeriodAsync(periodName, startDate, endDate, payDate);
            await _dialogService.ShowSuccessAsync($"Payroll period '{periodName}' created.");
            await LoadPayrollPeriodsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error creating period: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ProcessPayrollAsync()
    {
        if (SelectedPeriod == null) return;

        if (SelectedPeriod.Status != PayrollStatus.Draft)
        {
            await _dialogService.ShowErrorAsync("Only draft payroll periods can be processed.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Process Payroll",
            $"Process payroll for {SelectedPeriod.PeriodName}? This will generate payslips for all active employees.");

        if (!confirm) return;

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var currentUser = _sessionService.CurrentUser;
            await payrollService.ProcessPayrollAsync(SelectedPeriod.Id, currentUser?.Id ?? 0);

            await _dialogService.ShowSuccessAsync("Payroll processed successfully.");
            await LoadPayrollPeriodsAsync();
            await LoadPayslipsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error processing payroll: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApprovePayrollAsync()
    {
        if (SelectedPeriod == null) return;

        if (SelectedPeriod.Status != PayrollStatus.Processing)
        {
            await _dialogService.ShowErrorAsync("Only processing payroll periods can be approved.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Approve Payroll",
            $"Approve payroll for {SelectedPeriod.PeriodName}?");

        if (!confirm) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var currentUser = _sessionService.CurrentUser;
            await payrollService.ApprovePayrollAsync(SelectedPeriod.Id, currentUser?.Id ?? 0);

            await _dialogService.ShowSuccessAsync("Payroll approved.");
            await LoadPayrollPeriodsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error approving payroll: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task MarkAsPaidAsync()
    {
        if (SelectedPeriod == null) return;

        if (SelectedPeriod.Status != PayrollStatus.Approved)
        {
            await _dialogService.ShowErrorAsync("Only approved payroll periods can be marked as paid.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "Mark as Paid",
            $"Mark all payslips for {SelectedPeriod.PeriodName} as paid?");

        if (!confirm) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            await payrollService.MarkAsPaidAsync(SelectedPeriod.Id);

            await _dialogService.ShowSuccessAsync("Payroll marked as paid.");
            await LoadPayrollPeriodsAsync();
            await LoadPayslipsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadPayslipsAsync()
    {
        if (SelectedPeriod == null) return;

        try
        {
            IsLoading = true;

            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var payslips = await payrollService.GetPayslipsForPeriodAsync(SelectedPeriod.Id);
            Payslips = new ObservableCollection<Payslip>(payslips);

            Summary = await payrollService.GetPayrollSummaryAsync(SelectedPeriod.Id);
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

            // Show in dialog or save as HTML file
            await _dialogService.ShowInfoAsync("Payslip preview coming soon. The payslip has been generated.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error generating payslip: {ex.Message}");
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
            var filename = $"Payslip_{SelectedPayslip.Employee.EmployeeNumber}_{SelectedPeriod?.PeriodName.Replace(" ", "_")}.pdf";

            await exportService.ExportToPdfAsync(html, filename);
            await _dialogService.ShowSuccessAsync($"Payslip exported to {filename}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error printing payslip: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportPayrollReportAsync()
    {
        if (SelectedPeriod == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            var html = await payrollService.GeneratePayrollReportHtmlAsync(SelectedPeriod.Id);
            var filename = $"PayrollReport_{SelectedPeriod.PeriodName.Replace(" ", "_")}.pdf";

            await exportService.ExportToPdfAsync(html, filename);
            await _dialogService.ShowSuccessAsync($"Report exported to {filename}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Error exporting report: {ex.Message}");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedPeriodChanged(PayrollPeriod? value)
    {
        if (value != null)
        {
            _ = LoadPayslipsAsync();
        }
    }

    partial void OnFilterYearChanged(int? value)
    {
        _ = LoadPayrollPeriodsAsync();
    }
}
