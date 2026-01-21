using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.WPF.ViewModels;

public partial class MenuEngineeringViewModel : ObservableObject
{
    private readonly IMenuEngineeringService _menuEngineeringService;
    private readonly ILogger<MenuEngineeringViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Select date range and analyze menu";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private MenuEngineeringReport? _report;

    public MenuEngineeringViewModel(
        IMenuEngineeringService menuEngineeringService,
        ILogger<MenuEngineeringViewModel> logger)
    {
        _menuEngineeringService = menuEngineeringService;
        _logger = logger;

        _ = GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Analyzing menu engineering data...";

            var parameters = new MenuEngineeringParameters
            {
                StartDate = StartDate,
                EndDate = EndDate,
                IncludeRecommendations = true,
                IncludePreviousPeriod = true,
                PopularityThresholdMethod = ThresholdMethod.Average,
                ProfitabilityThresholdMethod = ThresholdMethod.WeightedAverage
            };

            Report = await _menuEngineeringService.GenerateMenuEngineeringReportAsync(parameters);

            StatusMessage = $"Analysis complete for {StartDate:MMM dd} - {EndDate:MMM dd, yyyy}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating menu engineering report");
            StatusMessage = "Error analyzing menu. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
