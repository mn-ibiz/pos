using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Offer Performance Report.
/// </summary>
public partial class OfferReportViewModel : ViewModelBase, INavigationAware
{
    private readonly IOfferService _offerService;
    private readonly IOrderService _orderService;
    private readonly IDialogService _dialogService;
    private readonly IReportPrintService _reportPrintService;

    public OfferReportViewModel(
        IOfferService offerService,
        IOrderService orderService,
        IDialogService dialogService,
        IReportPrintService reportPrintService,
        ILogger logger) : base(logger)
    {
        _offerService = offerService;
        _orderService = orderService;
        _dialogService = dialogService;
        _reportPrintService = reportPrintService;

        // Default date range - last 30 days
        StartDate = DateTime.Today.AddDays(-30);
        EndDate = DateTime.Today;
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets the report start date.
    /// </summary>
    [ObservableProperty]
    private DateTime _startDate;

    /// <summary>
    /// Gets or sets the report end date.
    /// </summary>
    [ObservableProperty]
    private DateTime _endDate;

    /// <summary>
    /// Gets or sets the selected status filter.
    /// </summary>
    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    /// <summary>
    /// Gets the available status filters.
    /// </summary>
    public ObservableCollection<string> StatusFilters { get; } = new()
    {
        "All",
        "Active",
        "Expired",
        "Upcoming"
    };

    /// <summary>
    /// Gets or sets the offer performance data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<OfferPerformanceItem> _offerPerformance = [];

    /// <summary>
    /// Gets or sets the total redemptions.
    /// </summary>
    [ObservableProperty]
    private int _totalRedemptions;

    /// <summary>
    /// Gets or sets the total revenue from offers.
    /// </summary>
    [ObservableProperty]
    private decimal _totalRevenue;

    /// <summary>
    /// Gets or sets the total discount given.
    /// </summary>
    [ObservableProperty]
    private decimal _totalDiscountGiven;

    /// <summary>
    /// Gets or sets whether the report is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    #endregion

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = GenerateReportAsync();
    }

    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    #endregion

    #region Commands

    /// <summary>
    /// Generates the offer performance report.
    /// </summary>
    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;
            try
            {
                var performanceData = await _offerService.GetOfferPerformanceAsync(
                    StartDate,
                    EndDate.AddDays(1).AddSeconds(-1)); // Include full end date

                // Apply status filter
                var filteredData = SelectedStatusFilter switch
                {
                    "Active" => performanceData.Where(p => p.Status == "Active"),
                    "Expired" => performanceData.Where(p => p.Status == "Expired"),
                    "Upcoming" => performanceData.Where(p => p.Status == "Upcoming"),
                    _ => performanceData
                };

                var items = filteredData.Select(p => new OfferPerformanceItem
                {
                    OfferId = p.OfferId,
                    OfferName = p.OfferName,
                    ProductName = p.ProductName,
                    OriginalPrice = p.OriginalPrice,
                    OfferPrice = p.OfferPrice,
                    RedemptionCount = p.RedemptionCount,
                    TotalRevenue = p.TotalRevenue,
                    TotalDiscountGiven = p.TotalDiscountGiven,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                }).ToList();

                OfferPerformance = new ObservableCollection<OfferPerformanceItem>(items);

                // Calculate totals
                TotalRedemptions = items.Sum(i => i.RedemptionCount);
                TotalRevenue = items.Sum(i => i.TotalRevenue);
                TotalDiscountGiven = items.Sum(i => i.TotalDiscountGiven);

                _logger.Information("Generated offer report with {Count} offers, {Redemptions} redemptions",
                    items.Count, TotalRedemptions);
            }
            finally
            {
                IsLoading = false;
            }
        }, "Generating report...").ConfigureAwait(true);
    }

    /// <summary>
    /// Exports the report to PDF.
    /// </summary>
    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        if (!OfferPerformance.Any())
        {
            await _dialogService.ShowWarningAsync("No Data", "Please generate the report first.");
            return;
        }

        try
        {
            var reportContent = GenerateReportHtml();
            await _reportPrintService.ExportToPdfAsync(reportContent, "OfferPerformanceReport");
            await _dialogService.ShowMessageAsync("Export Complete", "Report exported to PDF successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export offer report to PDF");
            await _dialogService.ShowErrorAsync("Export Failed", $"Failed to export report: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports the report to Excel.
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (!OfferPerformance.Any())
        {
            await _dialogService.ShowWarningAsync("No Data", "Please generate the report first.");
            return;
        }

        try
        {
            await _reportPrintService.ExportToCsvAsync(GenerateReportCsv(), "OfferPerformanceReport");
            await _dialogService.ShowMessageAsync("Export Complete", "Report exported to CSV successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export offer report to Excel");
            await _dialogService.ShowErrorAsync("Export Failed", $"Failed to export report: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints the report.
    /// </summary>
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (!OfferPerformance.Any())
        {
            await _dialogService.ShowWarningAsync("No Data", "Please generate the report first.");
            return;
        }

        try
        {
            var reportContent = GenerateReportHtml();
            await _reportPrintService.PrintReportAsync(reportContent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to print offer report");
            await _dialogService.ShowErrorAsync("Print Failed", $"Failed to print report: {ex.Message}");
        }
    }

    #endregion

    #region Report Generation

    private string GenerateReportHtml()
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("h1 { color: #1e1e2e; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #4f46e5; color: white; }");
        html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        html.AppendLine(".summary { margin-top: 20px; font-size: 16px; }");
        html.AppendLine(".active { color: #22c55e; } .expired { color: #ef4444; } .upcoming { color: #f59e0b; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine($"<h1>Offer Performance Report</h1>");
        html.AppendLine($"<p>Period: {StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}</p>");
        html.AppendLine($"<p>Filter: {SelectedStatusFilter}</p>");

        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Offer Name</th><th>Product</th><th>Original</th><th>Offer</th>");
        html.AppendLine("<th>Redemptions</th><th>Revenue</th><th>Discount Given</th><th>Status</th></tr>");

        foreach (var item in OfferPerformance)
        {
            var statusClass = item.Status.ToLower();
            html.AppendLine($"<tr>");
            html.AppendLine($"<td>{item.OfferName}</td>");
            html.AppendLine($"<td>{item.ProductName}</td>");
            html.AppendLine($"<td>KSh {item.OriginalPrice:N2}</td>");
            html.AppendLine($"<td>KSh {item.OfferPrice:N2}</td>");
            html.AppendLine($"<td>{item.RedemptionCount}</td>");
            html.AppendLine($"<td>KSh {item.TotalRevenue:N2}</td>");
            html.AppendLine($"<td>KSh {item.TotalDiscountGiven:N2}</td>");
            html.AppendLine($"<td class=\"{statusClass}\">{item.Status}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");

        html.AppendLine("<div class=\"summary\">");
        html.AppendLine($"<p><strong>Total Redemptions:</strong> {TotalRedemptions}</p>");
        html.AppendLine($"<p><strong>Total Revenue:</strong> KSh {TotalRevenue:N2}</p>");
        html.AppendLine($"<p><strong>Total Discount Given:</strong> KSh {TotalDiscountGiven:N2}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private string GenerateReportCsv()
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Offer Name,Product,Original Price,Offer Price,Redemptions,Revenue,Discount Given,Status,Start Date,End Date");

        foreach (var item in OfferPerformance)
        {
            csv.AppendLine($"\"{item.OfferName}\",\"{item.ProductName}\",{item.OriginalPrice},{item.OfferPrice},{item.RedemptionCount},{item.TotalRevenue},{item.TotalDiscountGiven},\"{item.Status}\",{item.StartDate:yyyy-MM-dd},{item.EndDate:yyyy-MM-dd}");
        }

        csv.AppendLine();
        csv.AppendLine($"TOTALS,,,{TotalRedemptions},{TotalRevenue},{TotalDiscountGiven},,");

        return csv.ToString();
    }

    #endregion
}

/// <summary>
/// Represents an offer performance item for the report.
/// </summary>
public class OfferPerformanceItem
{
    public int OfferId { get; set; }
    public string OfferName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal OriginalPrice { get; set; }
    public decimal OfferPrice { get; set; }
    public int RedemptionCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets the discount percentage.
    /// </summary>
    public decimal DiscountPercent => OriginalPrice > 0
        ? Math.Round((1 - (OfferPrice / OriginalPrice)) * 100, 1)
        : 0;

    /// <summary>
    /// Gets the average savings per redemption.
    /// </summary>
    public decimal AvgSavingsPerRedemption => RedemptionCount > 0
        ? TotalDiscountGiven / RedemptionCount
        : 0;
}
