using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Analytics;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.WPF.ViewModels;

public partial class CustomerAnalyticsViewModel : ObservableObject
{
    private readonly ICustomerAnalyticsService _customerAnalyticsService;
    private readonly ILogger<CustomerAnalyticsViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Select date range to analyze customers";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-6);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private CustomerAnalyticsReport? _report;

    public CustomerAnalyticsViewModel(
        ICustomerAnalyticsService customerAnalyticsService,
        ILogger<CustomerAnalyticsViewModel> logger)
    {
        _customerAnalyticsService = customerAnalyticsService;
        _logger = logger;

        _ = AnalyzeAsync();
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Analyzing customer data...";

            var parameters = new RFMAnalysisParameters
            {
                StartDate = StartDate,
                EndDate = EndDate,
                IncludeChurnPrediction = true,
                IncludeCLV = true,
                IncludeRecommendations = true,
                TopAtRiskCount = 20,
                TopHighValueCount = 20
            };

            Report = await _customerAnalyticsService.GenerateCustomerAnalyticsReportAsync(parameters);

            StatusMessage = $"Analysis complete for {StartDate:MMM dd} - {EndDate:MMM dd, yyyy}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing customers");
            StatusMessage = "Error analyzing customers. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
