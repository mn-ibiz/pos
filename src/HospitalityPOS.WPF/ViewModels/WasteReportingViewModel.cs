using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Waste Reporting - handles waste incident logging, analysis, trends, and cost impact.
/// </summary>
public partial class WasteReportingViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<WasteIncident> _wasteIncidents = new();

    [ObservableProperty]
    private ObservableCollection<WasteCategory> _wasteCategories = new();

    [ObservableProperty]
    private ObservableCollection<WasteByReason> _wasteByReason = new();

    [ObservableProperty]
    private ObservableCollection<WasteByProduct> _wasteByProduct = new();

    [ObservableProperty]
    private ObservableCollection<WasteTrend> _wasteTrends = new();

    [ObservableProperty]
    private WasteIncident? _selectedIncident;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [ObservableProperty]
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    // Summary stats
    [ObservableProperty]
    private decimal _totalWasteCost;

    [ObservableProperty]
    private decimal _wastePercentage;

    [ObservableProperty]
    private int _incidentCount;

    [ObservableProperty]
    private string _topWasteReason = string.Empty;

    [ObservableProperty]
    private string _topWasteProduct = string.Empty;

    // Incident Editor
    [ObservableProperty]
    private bool _isIncidentEditorOpen;

    [ObservableProperty]
    private WasteIncidentRequest _editingIncident = new();

    [ObservableProperty]
    private bool _isNewIncident;

    #endregion

    public List<string> WasteReasons { get; } = new()
    {
        "Expired", "Spoilage", "Preparation Error", "Customer Return",
        "Spillage", "Equipment Failure", "Over Production", "Quality Issue", "Other"
    };

    public WasteReportingViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Waste Reporting";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;

            using var scope = _scopeFactory.CreateScope();
            var wasteService = scope.ServiceProvider.GetService<IWasteReportService>();

            if (wasteService is null)
            {
                ErrorMessage = "Waste Report service not available";
                return;
            }

            // Load waste incidents
            var incidents = await wasteService.GetIncidentsAsync(StartDate, EndDate);
            WasteIncidents = new ObservableCollection<WasteIncident>(incidents);
            IncidentCount = incidents.Count;
            TotalWasteCost = incidents.Sum(i => i.CostImpact);

            // Load waste by reason
            var byReason = await wasteService.GetWasteByReasonAsync(StartDate, EndDate);
            WasteByReason = new ObservableCollection<WasteByReason>(byReason);
            if (byReason.Count > 0)
            {
                TopWasteReason = byReason.OrderByDescending(r => r.TotalCost).First().Reason;
            }

            // Load waste by product
            var byProduct = await wasteService.GetWasteByProductAsync(StartDate, EndDate, 10);
            WasteByProduct = new ObservableCollection<WasteByProduct>(byProduct);
            if (byProduct.Count > 0)
            {
                TopWasteProduct = byProduct.First().ProductName;
            }

            // Load waste trends
            var trends = await wasteService.GetWasteTrendsAsync(StartDate, EndDate);
            WasteTrends = new ObservableCollection<WasteTrend>(trends);

            // Calculate waste percentage
            var summary = await wasteService.GetSummaryAsync(StartDate, EndDate);
            WastePercentage = summary.WastePercentage;

            // Load categories
            var categories = await wasteService.GetCategoriesAsync();
            WasteCategories = new ObservableCollection<WasteCategory>(categories);

            IsLoading = false;
        }, "Loading waste data...");
    }

    [RelayCommand]
    private void LogIncident()
    {
        EditingIncident = new WasteIncidentRequest
        {
            IncidentDate = DateOnly.FromDateTime(DateTime.Today),
            IncidentTime = TimeOnly.FromDateTime(DateTime.Now)
        };
        IsNewIncident = true;
        IsIncidentEditorOpen = true;
    }

    [RelayCommand]
    private void EditIncident(WasteIncident? incident)
    {
        if (incident is null) return;

        EditingIncident = new WasteIncidentRequest
        {
            Id = incident.Id,
            ProductId = incident.ProductId,
            Quantity = incident.Quantity,
            Unit = incident.Unit,
            Reason = incident.Reason,
            Description = incident.Description,
            IncidentDate = incident.IncidentDate,
            IncidentTime = incident.IncidentTime,
            WasteCategoryId = incident.WasteCategoryId
        };
        IsNewIncident = false;
        IsIncidentEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveIncidentAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var wasteService = scope.ServiceProvider.GetService<IWasteReportService>();

            if (wasteService is null)
            {
                ErrorMessage = "Waste Report service not available";
                return;
            }

            EditingIncident.RecordedByUserId = SessionService.CurrentUserId;

            if (IsNewIncident)
            {
                await wasteService.LogIncidentAsync(EditingIncident);
            }
            else
            {
                await wasteService.UpdateIncidentAsync(EditingIncident);
            }

            IsIncidentEditorOpen = false;
            await LoadDataAsync();
        }, "Saving incident...");
    }

    [RelayCommand]
    private void CancelEditIncident()
    {
        IsIncidentEditorOpen = false;
    }

    [RelayCommand]
    private async Task DeleteIncidentAsync(WasteIncident? incident)
    {
        if (incident is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Delete Incident",
            $"Delete waste incident for {incident.ProductName}?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var wasteService = scope.ServiceProvider.GetService<IWasteReportService>();

            if (wasteService is null)
            {
                ErrorMessage = "Waste Report service not available";
                return;
            }

            await wasteService.DeleteIncidentAsync(incident.Id);
            await LoadDataAsync();
        }, "Deleting incident...");
    }

    [RelayCommand]
    private async Task ViewAnalysisAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var wasteService = scope.ServiceProvider.GetService<IWasteReportService>();

            if (wasteService is null)
            {
                ErrorMessage = "Waste Report service not available";
                return;
            }

            var analysis = await wasteService.GetAnalysisAsync(StartDate, EndDate);
            await DialogService.ShowMessageAsync(
                "Waste Analysis",
                $"Total Waste Cost: KSh {analysis.TotalCost:N0}\n" +
                $"Waste %: {analysis.WastePercentage:N2}%\n" +
                $"Main Issue: {analysis.MainIssue}\n" +
                $"Recommendation: {analysis.Recommendation}");
        }, "Generating analysis...");
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Waste report export functionality will be available soon.");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnStartDateChanged(DateOnly value) => _ = LoadDataAsync();
    partial void OnEndDateChanged(DateOnly value) => _ = LoadDataAsync();
}

// DTOs
public class WasteIncident
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostImpact { get; set; }
    public DateOnly IncidentDate { get; set; }
    public TimeOnly IncidentTime { get; set; }
    public int? WasteCategoryId { get; set; }
    public string? WasteCategoryName { get; set; }
    public int RecordedByUserId { get; set; }
    public string? RecordedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WasteIncidentRequest
{
    public int? Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "pcs";
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly IncidentDate { get; set; }
    public TimeOnly IncidentTime { get; set; }
    public int? WasteCategoryId { get; set; }
    public int RecordedByUserId { get; set; }
}

public class WasteCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WasteByReason
{
    public string Reason { get; set; } = string.Empty;
    public int IncidentCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal Percentage { get; set; }
}

public class WasteByProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int IncidentCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

public class WasteTrend
{
    public DateOnly Date { get; set; }
    public decimal WasteCost { get; set; }
    public decimal WastePercentage { get; set; }
    public int IncidentCount { get; set; }
}

public class WasteSummary
{
    public decimal TotalCost { get; set; }
    public decimal WastePercentage { get; set; }
    public int TotalIncidents { get; set; }
    public decimal AverageDailyCost { get; set; }
}

public class WasteAnalysis
{
    public decimal TotalCost { get; set; }
    public decimal WastePercentage { get; set; }
    public string MainIssue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> TopAffectedProducts { get; set; } = new();
    public List<string> TopReasons { get; set; } = new();
}
