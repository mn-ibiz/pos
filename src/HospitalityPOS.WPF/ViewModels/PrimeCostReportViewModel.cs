using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.WPF.ViewModels;

public partial class PrimeCostReportViewModel : ObservableObject
{
    private readonly IPrimeCostReportService _reportService;
    private readonly ILogger<PrimeCostReportViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Select date range and generate report";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private PrimeCostReport? _report;

    [ObservableProperty]
    private Brush _primeCostColor = Brushes.White;

    [ObservableProperty]
    private Brush _statusBackground = new SolidColorBrush(Color.FromRgb(31, 31, 35));

    [ObservableProperty]
    private Brush _trendColor = Brushes.White;

    public PrimeCostReportViewModel(
        IPrimeCostReportService reportService,
        ILogger<PrimeCostReportViewModel> logger)
    {
        _reportService = reportService;
        _logger = logger;

        _ = GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Generating prime cost report...";

            var parameters = new PrimeCostReportParameters
            {
                StartDate = StartDate,
                EndDate = EndDate,
                IncludePreviousPeriod = true,
                IncludeBreakdown = true
            };

            Report = await _reportService.GeneratePrimeCostReportAsync(parameters);

            UpdateColors();
            StatusMessage = $"Report generated for {StartDate:MMM dd} - {EndDate:MMM dd, yyyy}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prime cost report");
            StatusMessage = "Error generating report. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateColors()
    {
        if (Report == null) return;

        // Prime Cost Color based on status
        PrimeCostColor = Report.Status switch
        {
            PrimeCostStatus.Excellent => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green
            PrimeCostStatus.Good => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue
            PrimeCostStatus.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Yellow
            PrimeCostStatus.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
            _ => Brushes.White
        };

        // Status Background
        StatusBackground = Report.Status switch
        {
            PrimeCostStatus.Excellent => new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)),
            PrimeCostStatus.Good => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
            PrimeCostStatus.Warning => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
            PrimeCostStatus.Critical => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
            _ => new SolidColorBrush(Color.FromRgb(31, 31, 35))
        };

        // Trend Color
        if (Report.TrendChange.HasValue)
        {
            TrendColor = Report.TrendChange.Value switch
            {
                < 0 => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green (improvement)
                > 0 => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red (worse)
                _ => Brushes.White
            };
        }
    }
}
