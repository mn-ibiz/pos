using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for financial reports (Trial Balance, Income Statement, Balance Sheet).
/// </summary>
public partial class FinancialReportsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private int _selectedReportIndex;

    [ObservableProperty]
    private DateTime _asOfDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _startDate = new(DateTime.Today.Year, 1, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private bool _isLoading;

    // Trial Balance
    [ObservableProperty]
    private TrialBalance? _trialBalance;

    [ObservableProperty]
    private ObservableCollection<TrialBalanceLine> _trialBalanceLines = [];

    // Income Statement
    [ObservableProperty]
    private IncomeStatement? _incomeStatement;

    [ObservableProperty]
    private ObservableCollection<IncomeStatementDisplayLine> _incomeStatementLines = [];

    // Balance Sheet
    [ObservableProperty]
    private BalanceSheet? _balanceSheet;

    [ObservableProperty]
    private ObservableCollection<BalanceSheetDisplayLine> _balanceSheetLines = [];

    // Account Ledger
    [ObservableProperty]
    private ObservableCollection<ChartOfAccount> _accounts = [];

    [ObservableProperty]
    private ChartOfAccount? _selectedAccount;

    [ObservableProperty]
    private AccountLedger? _accountLedger;

    [ObservableProperty]
    private ObservableCollection<LedgerLine> _ledgerLines = [];

    [ObservableProperty]
    private string _reportHtml = string.Empty;

    public FinancialReportsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        IExportService exportService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _exportService = exportService;
    }

    public async Task InitializeAsync()
    {
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

        var accounts = await accountingService.GetAllAccountsAsync();
        Accounts = new ObservableCollection<ChartOfAccount>(accounts.Where(a => a.IsActive).OrderBy(a => a.AccountCode));
    }

    [RelayCommand]
    private async Task GenerateTrialBalanceAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            TrialBalance = await accountingService.GetTrialBalanceAsync(AsOfDate);
            TrialBalanceLines = new ObservableCollection<TrialBalanceLine>(TrialBalance.Lines);

            // Generate HTML for export
            ReportHtml = await accountingService.GenerateTrialBalanceHtmlAsync(AsOfDate);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to generate trial balance: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateIncomeStatementAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            IncomeStatement = await accountingService.GetIncomeStatementAsync(StartDate, EndDate);

            // Flatten for display
            var lines = new List<IncomeStatementDisplayLine>();

            lines.Add(new IncomeStatementDisplayLine { DisplayText = "REVENUE", IsHeader = true });
            foreach (var section in IncomeStatement.RevenueSections)
            {
                lines.Add(new IncomeStatementDisplayLine { DisplayText = section.SectionName, IsSection = true });
                foreach (var line in section.Lines)
                {
                    lines.Add(new IncomeStatementDisplayLine
                    {
                        DisplayText = $"  {line.AccountCode} - {line.AccountName}",
                        Amount = line.Amount
                    });
                }
                lines.Add(new IncomeStatementDisplayLine
                {
                    DisplayText = $"Total {section.SectionName}",
                    Amount = section.Total,
                    IsSubtotal = true
                });
            }
            lines.Add(new IncomeStatementDisplayLine
            {
                DisplayText = "TOTAL REVENUE",
                Amount = IncomeStatement.TotalRevenue,
                IsTotal = true
            });

            lines.Add(new IncomeStatementDisplayLine { DisplayText = "" }); // Spacer

            lines.Add(new IncomeStatementDisplayLine { DisplayText = "EXPENSES", IsHeader = true });
            foreach (var section in IncomeStatement.ExpenseSections)
            {
                lines.Add(new IncomeStatementDisplayLine { DisplayText = section.SectionName, IsSection = true });
                foreach (var line in section.Lines)
                {
                    lines.Add(new IncomeStatementDisplayLine
                    {
                        DisplayText = $"  {line.AccountCode} - {line.AccountName}",
                        Amount = line.Amount
                    });
                }
                lines.Add(new IncomeStatementDisplayLine
                {
                    DisplayText = $"Total {section.SectionName}",
                    Amount = section.Total,
                    IsSubtotal = true
                });
            }
            lines.Add(new IncomeStatementDisplayLine
            {
                DisplayText = "TOTAL EXPENSES",
                Amount = IncomeStatement.TotalExpenses,
                IsTotal = true
            });

            lines.Add(new IncomeStatementDisplayLine { DisplayText = "" }); // Spacer
            lines.Add(new IncomeStatementDisplayLine
            {
                DisplayText = "NET INCOME",
                Amount = IncomeStatement.NetIncome,
                IsTotal = true,
                IsHighlight = IncomeStatement.NetIncome >= 0
            });

            IncomeStatementLines = new ObservableCollection<IncomeStatementDisplayLine>(lines);

            // Generate HTML for export
            ReportHtml = await accountingService.GenerateIncomeStatementHtmlAsync(StartDate, EndDate);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to generate income statement: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateBalanceSheetAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            BalanceSheet = await accountingService.GetBalanceSheetAsync(AsOfDate);

            // Flatten for display
            var lines = new List<BalanceSheetDisplayLine>();

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "ASSETS", IsHeader = true });
            foreach (var section in BalanceSheet.AssetSections)
            {
                lines.Add(new BalanceSheetDisplayLine { DisplayText = section.SectionName, IsSection = true });
                foreach (var line in section.Lines)
                {
                    lines.Add(new BalanceSheetDisplayLine
                    {
                        DisplayText = $"  {line.AccountCode} - {line.AccountName}",
                        Amount = line.Amount
                    });
                }
                lines.Add(new BalanceSheetDisplayLine
                {
                    DisplayText = $"Total {section.SectionName}",
                    Amount = section.Total,
                    IsSubtotal = true
                });
            }
            lines.Add(new BalanceSheetDisplayLine
            {
                DisplayText = "TOTAL ASSETS",
                Amount = BalanceSheet.TotalAssets,
                IsTotal = true
            });

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "" }); // Spacer

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "LIABILITIES", IsHeader = true });
            foreach (var section in BalanceSheet.LiabilitySections)
            {
                lines.Add(new BalanceSheetDisplayLine { DisplayText = section.SectionName, IsSection = true });
                foreach (var line in section.Lines)
                {
                    lines.Add(new BalanceSheetDisplayLine
                    {
                        DisplayText = $"  {line.AccountCode} - {line.AccountName}",
                        Amount = line.Amount
                    });
                }
                lines.Add(new BalanceSheetDisplayLine
                {
                    DisplayText = $"Total {section.SectionName}",
                    Amount = section.Total,
                    IsSubtotal = true
                });
            }
            lines.Add(new BalanceSheetDisplayLine
            {
                DisplayText = "TOTAL LIABILITIES",
                Amount = BalanceSheet.TotalLiabilities,
                IsTotal = true
            });

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "" }); // Spacer

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "EQUITY", IsHeader = true });
            foreach (var section in BalanceSheet.EquitySections)
            {
                lines.Add(new BalanceSheetDisplayLine { DisplayText = section.SectionName, IsSection = true });
                foreach (var line in section.Lines)
                {
                    lines.Add(new BalanceSheetDisplayLine
                    {
                        DisplayText = $"  {line.AccountCode} - {line.AccountName}",
                        Amount = line.Amount
                    });
                }
                lines.Add(new BalanceSheetDisplayLine
                {
                    DisplayText = $"Total {section.SectionName}",
                    Amount = section.Total,
                    IsSubtotal = true
                });
            }
            if (BalanceSheet.RetainedEarnings != 0)
            {
                lines.Add(new BalanceSheetDisplayLine
                {
                    DisplayText = "  Retained Earnings",
                    Amount = BalanceSheet.RetainedEarnings
                });
            }
            lines.Add(new BalanceSheetDisplayLine
            {
                DisplayText = "TOTAL EQUITY",
                Amount = BalanceSheet.TotalEquity,
                IsTotal = true
            });

            lines.Add(new BalanceSheetDisplayLine { DisplayText = "" }); // Spacer
            lines.Add(new BalanceSheetDisplayLine
            {
                DisplayText = "TOTAL LIABILITIES & EQUITY",
                Amount = BalanceSheet.TotalLiabilities + BalanceSheet.TotalEquity,
                IsTotal = true,
                IsHighlight = BalanceSheet.IsBalanced
            });

            BalanceSheetLines = new ObservableCollection<BalanceSheetDisplayLine>(lines);

            // Generate HTML for export
            ReportHtml = await accountingService.GenerateBalanceSheetHtmlAsync(AsOfDate);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to generate balance sheet: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateAccountLedgerAsync()
    {
        if (SelectedAccount == null)
        {
            await _dialogService.ShowErrorAsync("Error", "Please select an account.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();

            AccountLedger = await accountingService.GetAccountLedgerAsync(
                SelectedAccount.Id, StartDate, EndDate);
            LedgerLines = new ObservableCollection<LedgerLine>(AccountLedger.Lines);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to generate account ledger: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToHtmlAsync()
    {
        if (string.IsNullOrEmpty(ReportHtml))
        {
            await _dialogService.ShowErrorAsync("Error", "Please generate a report first.");
            return;
        }

        try
        {
            var reportName = SelectedReportIndex switch
            {
                0 => "TrialBalance",
                1 => "IncomeStatement",
                2 => "BalanceSheet",
                _ => "Report"
            };

            var fileName = $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var reportsPath = Path.Combine(documentsPath, "POS Reports");
            Directory.CreateDirectory(reportsPath);

            var filePath = Path.Combine(reportsPath, fileName);
            await File.WriteAllTextAsync(filePath, ReportHtml);

            // Open in browser
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to export report: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (string.IsNullOrEmpty(ReportHtml))
        {
            await _dialogService.ShowErrorAsync("Error", "Please generate a report first.");
            return;
        }

        // Export to HTML first, then user can print from browser
        await ExportToHtmlAsync();
    }
}

/// <summary>
/// Display line for income statement.
/// </summary>
public class IncomeStatementDisplayLine
{
    public string DisplayText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsHeader { get; set; }
    public bool IsSection { get; set; }
    public bool IsSubtotal { get; set; }
    public bool IsTotal { get; set; }
    public bool IsHighlight { get; set; }
}

/// <summary>
/// Display line for balance sheet.
/// </summary>
public class BalanceSheetDisplayLine
{
    public string DisplayText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsHeader { get; set; }
    public bool IsSection { get; set; }
    public bool IsSubtotal { get; set; }
    public bool IsTotal { get; set; }
    public bool IsHighlight { get; set; }
}
