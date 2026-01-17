using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for M-Pesa dashboard.
/// </summary>
public partial class MpesaDashboardViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    // Dashboard Stats
    [ObservableProperty]
    private bool _isConfigured;

    [ObservableProperty]
    private bool _isTestMode;

    [ObservableProperty]
    private string? _shortCode;

    [ObservableProperty]
    private DateTime? _lastSuccessfulTransaction;

    [ObservableProperty]
    private int _todayTransactions;

    [ObservableProperty]
    private decimal _todayAmount;

    [ObservableProperty]
    private int _todayPending;

    [ObservableProperty]
    private int _todayFailed;

    [ObservableProperty]
    private int _monthTransactions;

    [ObservableProperty]
    private decimal _monthAmount;

    [ObservableProperty]
    private int _unverifiedManualEntries;

    // STK Push
    [ObservableProperty]
    private string _stkPhoneNumber = string.Empty;

    [ObservableProperty]
    private decimal _stkAmount;

    [ObservableProperty]
    private string _stkAccountReference = string.Empty;

    [ObservableProperty]
    private string _stkDescription = string.Empty;

    [ObservableProperty]
    private string? _stkResultMessage;

    [ObservableProperty]
    private bool _isStkProcessing;

    // Manual Entry
    [ObservableProperty]
    private string _manualReceiptNumber = string.Empty;

    [ObservableProperty]
    private decimal _manualAmount;

    [ObservableProperty]
    private string _manualPhoneNumber = string.Empty;

    [ObservableProperty]
    private DateTime _manualTransactionDate = DateTime.Now;

    [ObservableProperty]
    private string? _manualNotes;

    [ObservableProperty]
    private string? _manualResultMessage;

    // Pending Requests
    [ObservableProperty]
    private ObservableCollection<MpesaStkPushRequest> _pendingRequests = [];

    [ObservableProperty]
    private MpesaStkPushRequest? _selectedPendingRequest;

    // Recent Transactions
    [ObservableProperty]
    private ObservableCollection<MpesaTransaction> _recentTransactions = [];

    [ObservableProperty]
    private MpesaTransaction? _selectedTransaction;

    // Unverified Transactions
    [ObservableProperty]
    private ObservableCollection<MpesaTransaction> _unverifiedTransactions = [];

    [ObservableProperty]
    private MpesaTransaction? _selectedUnverifiedTransaction;

    // Chart Data
    [ObservableProperty]
    private ObservableCollection<MpesaHourlyStats> _hourlyStats = [];

    public MpesaDashboardViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var dashboard = await mpesaService.GetDashboardDataAsync();

            IsConfigured = dashboard.IsConfigured;
            IsTestMode = dashboard.IsTestMode;
            ShortCode = dashboard.ShortCode;
            LastSuccessfulTransaction = dashboard.LastSuccessfulTransaction;
            TodayTransactions = dashboard.TodayTransactions;
            TodayAmount = dashboard.TodayAmount;
            TodayPending = dashboard.TodayPending;
            TodayFailed = dashboard.TodayFailed;
            MonthTransactions = dashboard.MonthTransactions;
            MonthAmount = dashboard.MonthAmount;
            UnverifiedManualEntries = dashboard.UnverifiedManualEntries;

            HourlyStats = new ObservableCollection<MpesaHourlyStats>(dashboard.TodayHourlyStats);

            // Load pending requests
            var pendingReqs = await mpesaService.GetPendingStkRequestsAsync();
            PendingRequests = new ObservableCollection<MpesaStkPushRequest>(pendingReqs);

            // Load recent transactions (last 7 days)
            var endDate = DateTime.Today.AddDays(1);
            var startDate = DateTime.Today.AddDays(-7);
            var transactions = await mpesaService.GetTransactionsByDateRangeAsync(startDate, endDate);
            RecentTransactions = new ObservableCollection<MpesaTransaction>(transactions.Take(50));

            // Load unverified transactions
            var unverified = await mpesaService.GetUnverifiedTransactionsAsync();
            UnverifiedTransactions = new ObservableCollection<MpesaTransaction>(unverified);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InitiateStkPushAsync()
    {
        if (string.IsNullOrWhiteSpace(StkPhoneNumber) || StkAmount <= 0)
        {
            StkResultMessage = "Please enter a valid phone number and amount.";
            return;
        }

        IsStkProcessing = true;
        StkResultMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            // Validate phone number
            var isValid = await mpesaService.ValidatePhoneNumberAsync(StkPhoneNumber);
            if (!isValid)
            {
                StkResultMessage = "Invalid phone number format. Use 07XXXXXXXX or 254XXXXXXXXX.";
                IsStkProcessing = false;
                return;
            }

            var result = await mpesaService.InitiateStkPushAsync(
                StkPhoneNumber,
                StkAmount,
                string.IsNullOrWhiteSpace(StkAccountReference) ? "Payment" : StkAccountReference,
                string.IsNullOrWhiteSpace(StkDescription) ? "Payment" : StkDescription);

            if (result.Success)
            {
                StkResultMessage = $"STK Push initiated! {result.CustomerMessage}";

                // Clear form
                StkPhoneNumber = string.Empty;
                StkAmount = 0;
                StkAccountReference = string.Empty;
                StkDescription = string.Empty;

                // Refresh pending requests
                await LoadDashboardAsync();
            }
            else
            {
                StkResultMessage = $"Failed: {result.ErrorMessage ?? result.ResponseDescription}";
            }
        }
        catch (Exception ex)
        {
            StkResultMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsStkProcessing = false;
        }
    }

    [RelayCommand]
    private async Task RecordManualTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualReceiptNumber) || ManualAmount <= 0)
        {
            ManualResultMessage = "Please enter receipt number and amount.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            // Check for duplicate
            var existing = await mpesaService.GetTransactionByReceiptNumberAsync(ManualReceiptNumber);
            if (existing != null)
            {
                ManualResultMessage = "Transaction with this receipt number already exists.";
                return;
            }

            var transaction = await mpesaService.RecordManualTransactionAsync(
                ManualReceiptNumber,
                ManualAmount,
                ManualPhoneNumber,
                ManualTransactionDate,
                ManualNotes,
                1); // TODO: Get actual user ID

            ManualResultMessage = "Manual transaction recorded successfully!";

            // Clear form
            ManualReceiptNumber = string.Empty;
            ManualAmount = 0;
            ManualPhoneNumber = string.Empty;
            ManualTransactionDate = DateTime.Now;
            ManualNotes = null;

            // Refresh dashboard
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            ManualResultMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task QueryTransactionStatusAsync()
    {
        if (SelectedPendingRequest == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var result = await mpesaService.QueryTransactionStatusAsync(SelectedPendingRequest.CheckoutRequestId);

            if (result.Success)
            {
                MessageBox.Show(
                    $"Status: {result.Status}\nReceipt: {result.MpesaReceiptNumber ?? "N/A"}\n{result.ResultDescription}",
                    "Transaction Status",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Query failed: {result.ErrorMessage}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task QueryAllPendingAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            await mpesaService.QueryPendingTransactionsAsync();
            await LoadDashboardAsync();

            MessageBox.Show("All pending transactions queried.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task VerifyTransactionAsync()
    {
        if (SelectedUnverifiedTransaction == null) return;

        var result = MessageBox.Show(
            $"Verify transaction {SelectedUnverifiedTransaction.MpesaReceiptNumber} for KES {SelectedUnverifiedTransaction.Amount:N2}?",
            "Confirm Verification",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var success = await mpesaService.VerifyTransactionAsync(
                SelectedUnverifiedTransaction.Id,
                1); // TODO: Get actual user ID

            if (success)
            {
                MessageBox.Show("Transaction verified successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDashboardAsync();
            }
            else
            {
                MessageBox.Show("Failed to verify transaction.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void FormatPhoneNumber()
    {
        if (string.IsNullOrWhiteSpace(StkPhoneNumber)) return;

        using var scope = _serviceProvider.CreateScope();
        var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();
        StkPhoneNumber = mpesaService.FormatPhoneNumber(StkPhoneNumber);
    }

    [RelayCommand]
    private async Task ExportTransactionsToExcelAsync()
    {
        if (RecentTransactions.Count == 0)
        {
            MessageBox.Show("No transactions to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            var data = RecentTransactions.Select(t => new
            {
                t.MpesaReceiptNumber,
                Date = t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                t.PhoneNumber,
                Amount = $"KES {t.Amount:N2}",
                Status = t.Status.ToString(),
                Type = t.TransactionType.ToString(),
                Source = t.Source.ToString(),
                IsVerified = t.IsVerified ? "Yes" : "No",
                VerifiedBy = t.VerifiedByUserId?.ToString() ?? "",
                Notes = t.Notes ?? ""
            });

            var result = await exportService.ExportToExcelAsync(
                data,
                $"MpesaTransactions_{DateTime.Now:yyyyMMdd}",
                "M-Pesa Transactions");

            if (result)
            {
                MessageBox.Show("Transactions exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportDailyReportToExcelAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            // Get today's transactions
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(1);
            var transactions = await mpesaService.GetTransactionsByDateRangeAsync(startDate, endDate);

            if (!transactions.Any())
            {
                MessageBox.Show("No transactions found for today.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var data = transactions.Select(t => new
            {
                t.MpesaReceiptNumber,
                Time = t.TransactionDate.ToString("HH:mm:ss"),
                t.PhoneNumber,
                Amount = $"KES {t.Amount:N2}",
                Status = t.Status.ToString(),
                Type = t.TransactionType.ToString(),
                Source = t.Source.ToString(),
                IsVerified = t.IsVerified ? "Yes" : "No"
            });

            var result = await exportService.ExportToExcelAsync(
                data,
                $"MpesaDailyReport_{DateTime.Today:yyyyMMdd}",
                "Daily Report");

            if (result)
            {
                MessageBox.Show("Daily report exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportMonthlyReportToExcelAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            // Get this month's transactions
            var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = startDate.AddMonths(1);
            var transactions = await mpesaService.GetTransactionsByDateRangeAsync(startDate, endDate);

            if (!transactions.Any())
            {
                MessageBox.Show("No transactions found for this month.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var data = transactions.Select(t => new
            {
                t.MpesaReceiptNumber,
                Date = t.TransactionDate.ToString("yyyy-MM-dd"),
                Time = t.TransactionDate.ToString("HH:mm:ss"),
                t.PhoneNumber,
                Amount = $"KES {t.Amount:N2}",
                Status = t.Status.ToString(),
                Type = t.TransactionType.ToString(),
                Source = t.Source.ToString(),
                IsVerified = t.IsVerified ? "Yes" : "No"
            });

            var result = await exportService.ExportToExcelAsync(
                data,
                $"MpesaMonthlyReport_{startDate:yyyyMM}",
                "Monthly Report");

            if (result)
            {
                MessageBox.Show("Monthly report exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
