using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for displaying a loyalty member's transaction history.
/// </summary>
public partial class MemberTransactionHistoryViewModel : ViewModelBase, INavigationAware
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the current loyalty member.
    /// </summary>
    [ObservableProperty]
    private LoyaltyMemberDto? _member;

    /// <summary>
    /// Gets or sets the collection of transactions.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LoyaltyTransactionDto> _transactions = new();

    /// <summary>
    /// Gets or sets the filter by transaction type (null = all types).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterTypeDisplay))]
    private LoyaltyTransactionType? _filterType;

    /// <summary>
    /// Gets the filter type display name.
    /// </summary>
    public string FilterTypeDisplay => FilterType?.ToString() ?? "All Types";

    /// <summary>
    /// Gets or sets the filter start date.
    /// </summary>
    [ObservableProperty]
    private DateTime? _filterStartDate;

    /// <summary>
    /// Gets or sets the filter end date.
    /// </summary>
    [ObservableProperty]
    private DateTime? _filterEndDate;

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    private int _currentPage = 1;

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    private int _totalPages;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [ObservableProperty]
    private int _pageSize = 20;

    /// <summary>
    /// Gets or sets the total transaction count.
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets the page display string.
    /// </summary>
    public string PageDisplay => TotalPages > 0 ? $"Page {CurrentPage} of {TotalPages}" : "No results";

    /// <summary>
    /// Gets the available transaction type filter options.
    /// </summary>
    public List<LoyaltyTransactionType?> TransactionTypeOptions { get; } = new()
    {
        null, // All types
        LoyaltyTransactionType.Earned,
        LoyaltyTransactionType.Redeemed,
        LoyaltyTransactionType.Adjustment,
        LoyaltyTransactionType.Expired
    };

    /// <summary>
    /// Gets the date range preset options.
    /// </summary>
    public List<DateRangePreset> DateRangePresets { get; } = new()
    {
        new DateRangePreset("Last 7 Days", DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1)),
        new DateRangePreset("Last 30 Days", DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1)),
        new DateRangePreset("Last 90 Days", DateTime.Today.AddDays(-90), DateTime.Today.AddDays(1)),
        new DateRangePreset("This Year", new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today.AddDays(1)),
        new DateRangePreset("All Time", null, null)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberTransactionHistoryViewModel"/> class.
    /// </summary>
    public MemberTransactionHistoryViewModel(
        ILoyaltyService loyaltyService,
        INavigationService navigationService,
        ILogger logger)
        : base(logger)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        Title = "Transaction History";

        // Default to last 30 days
        FilterStartDate = DateTime.Today.AddDays(-30);
        FilterEndDate = DateTime.Today.AddDays(1);
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is int memberId)
        {
            await LoadMemberAsync(memberId);
        }
        else if (parameter is LoyaltyMemberDto member)
        {
            Member = member;
            await LoadTransactionsAsync();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Loads member information.
    /// </summary>
    private async Task LoadMemberAsync(int memberId)
    {
        await ExecuteAsync(async () =>
        {
            Member = await _loyaltyService.GetByIdAsync(memberId);
            if (Member != null)
            {
                Title = $"Transaction History - {Member.Name}";
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Member not found";
            }
        }, "Loading member...");
    }

    /// <summary>
    /// Loads the transactions with current filter settings.
    /// </summary>
    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        if (Member == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _loyaltyService.GetPagedTransactionHistoryAsync(
                Member.Id,
                FilterType,
                FilterStartDate,
                FilterEndDate,
                CurrentPage,
                PageSize);

            Transactions.Clear();
            foreach (var transaction in result.Transactions)
            {
                Transactions.Add(transaction);
            }

            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
            CurrentPage = result.CurrentPage;

            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(HasPreviousPage));
        }, "Loading transactions...");
    }

    /// <summary>
    /// Applies the filter and reloads transactions from page 1.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        CurrentPage = 1;
        await LoadTransactionsAsync();
    }

    /// <summary>
    /// Clears all filters and reloads.
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FilterType = null;
        FilterStartDate = null;
        FilterEndDate = null;
        CurrentPage = 1;
        await LoadTransactionsAsync();
    }

    /// <summary>
    /// Sets the date range filter from a preset.
    /// </summary>
    [RelayCommand]
    private async Task ApplyDateRangePresetAsync(DateRangePreset preset)
    {
        FilterStartDate = preset.StartDate;
        FilterEndDate = preset.EndDate;
        CurrentPage = 1;
        await LoadTransactionsAsync();
    }

    /// <summary>
    /// Goes to the next page.
    /// </summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadTransactionsAsync();
        }
    }

    /// <summary>
    /// Goes to the previous page.
    /// </summary>
    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadTransactionsAsync();
        }
    }

    /// <summary>
    /// Goes to a specific page.
    /// </summary>
    [RelayCommand]
    private async Task GoToPageAsync(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadTransactionsAsync();
        }
    }

    /// <summary>
    /// Exports transactions to CSV.
    /// </summary>
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        if (Member == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"TransactionHistory_{Member.MembershipNumber}_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            await ExecuteAsync(async () =>
            {
                // Load all transactions (not just current page)
                var allTransactions = await _loyaltyService.GetTransactionHistoryAsync(
                    Member.Id,
                    FilterStartDate,
                    FilterEndDate,
                    10000);

                var sb = new StringBuilder();
                sb.AppendLine("Date,Time,Type,Points,Value (KES),Balance,Reference,Description");

                foreach (var t in allTransactions)
                {
                    var points = t.IsCredit ? $"+{t.Points:N0}" : t.Points.ToString("N0");
                    sb.AppendLine($"{t.TransactionDate:yyyy-MM-dd},{t.TransactionDate:HH:mm},{t.TransactionTypeName},{points},{t.MonetaryValue:N2},{t.BalanceAfter:N0},{t.ReferenceNumber ?? "-"},{EscapeCsvField(t.Description)}");
                }

                await File.WriteAllTextAsync(dialog.FileName, sb.ToString());
                _logger.Information("Exported transaction history to {FileName}", dialog.FileName);

                await DialogService.ShowMessageAsync("Export Complete", $"Transaction history exported to:\n{dialog.FileName}");
            }, "Exporting...");
        }
    }

    /// <summary>
    /// Navigates to view a receipt.
    /// </summary>
    [RelayCommand]
    private void ViewReceipt(LoyaltyTransactionDto? transaction)
    {
        if (transaction?.ReceiptId != null)
        {
            _navigationService.NavigateTo("ReceiptDetailView", transaction.ReceiptId);
        }
    }

    /// <summary>
    /// Navigates back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "-";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}

/// <summary>
/// Represents a date range preset for quick filtering.
/// </summary>
public class DateRangePreset
{
    public string Name { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    public DateRangePreset(string name, DateTime? startDate, DateTime? endDate)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
    }

    public override string ToString() => Name;
}
