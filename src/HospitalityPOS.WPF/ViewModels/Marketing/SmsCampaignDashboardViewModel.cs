using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Marketing;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels.Marketing;

/// <summary>
/// ViewModel for the SMS Campaign Dashboard.
/// Displays campaign statistics, recent campaigns, and quick actions.
/// </summary>
public partial class SmsCampaignDashboardViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    // Campaign Statistics
    [ObservableProperty]
    private int _totalCampaigns;

    [ObservableProperty]
    private int _activeCampaigns;

    [ObservableProperty]
    private int _scheduledCampaigns;

    [ObservableProperty]
    private int _completedCampaigns;

    [ObservableProperty]
    private int _draftCampaigns;

    [ObservableProperty]
    private int _totalMessagesSent;

    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private decimal _overallDeliveryRate;

    // Recent Campaigns
    public ObservableCollection<SmsCampaign> RecentCampaigns { get; } = new();

    // Templates Count
    [ObservableProperty]
    private int _totalTemplates;

    // Segments Count
    [ObservableProperty]
    private int _totalSegments;

    #endregion

    public SmsCampaignDashboardViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;
    }

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDashboardAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading dashboard...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null)
            {
                _logger.Warning("ISmsMarketingService is not registered");
                ErrorMessage = "SMS Marketing service is not available.";
                return;
            }

            // Load campaigns
            var allCampaigns = await marketingService.GetCampaignsAsync();
            TotalCampaigns = allCampaigns.Count;
            ActiveCampaigns = allCampaigns.Count(c => c.Status == CampaignStatus.Sending);
            ScheduledCampaigns = allCampaigns.Count(c => c.Status == CampaignStatus.Scheduled);
            CompletedCampaigns = allCampaigns.Count(c => c.Status == CampaignStatus.Completed);
            DraftCampaigns = allCampaigns.Count(c => c.Status == CampaignStatus.Draft);

            // Calculate totals
            TotalMessagesSent = allCampaigns.Sum(c => c.SentCount);
            TotalCost = allCampaigns.Sum(c => c.TotalCost);

            var totalDelivered = allCampaigns.Sum(c => c.DeliveredCount);
            OverallDeliveryRate = TotalMessagesSent > 0 ? (totalDelivered * 100m / TotalMessagesSent) : 0;

            // Load recent campaigns (last 10)
            RecentCampaigns.Clear();
            foreach (var campaign in allCampaigns.OrderByDescending(c => c.CreatedAt).Take(10))
            {
                RecentCampaigns.Add(campaign);
            }

            // Load template count
            var templates = await marketingService.GetTemplatesAsync();
            TotalTemplates = templates.Count;

            // Load segment count
            var segments = await marketingService.GetSegmentsAsync();
            TotalSegments = segments.Count;

            _logger.Information("SMS Campaign Dashboard loaded: {Campaigns} campaigns, {Templates} templates, {Segments} segments",
                TotalCampaigns, TotalTemplates, TotalSegments);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load SMS campaign dashboard");
            ErrorMessage = "Failed to load dashboard data.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void NavigateToTemplates()
    {
        _navigationService.NavigateTo<SmsTemplateListViewModel>();
    }

    [RelayCommand]
    private void NavigateToSegments()
    {
        _navigationService.NavigateTo<CustomerSegmentListViewModel>();
    }

    [RelayCommand]
    private async Task CreateCampaignAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null)
            {
                await _dialogService.ShowErrorAsync("Error", "SMS Marketing service is not available.");
                return;
            }

            // Check if templates exist
            var templates = await marketingService.GetTemplatesAsync();
            if (templates.Count == 0)
            {
                var createTemplate = await _dialogService.ShowConfirmAsync(
                    "No Templates",
                    "You need at least one SMS template before creating a campaign. Would you like to create a template now?");

                if (createTemplate)
                {
                    NavigateToTemplates();
                }
                return;
            }

            // Check if segments exist
            var segments = await marketingService.GetSegmentsAsync();
            if (segments.Count == 0)
            {
                var createSegment = await _dialogService.ShowConfirmAsync(
                    "No Customer Segments",
                    "You need at least one customer segment before creating a campaign. Would you like to create a segment now?");

                if (createSegment)
                {
                    NavigateToSegments();
                }
                return;
            }

            // Get campaign name from user
            var campaignName = await _dialogService.ShowInputAsync(
                "New Campaign",
                "Enter a name for the campaign:",
                $"Campaign {DateTime.Now:yyyy-MM-dd}");

            if (string.IsNullOrWhiteSpace(campaignName))
            {
                return; // User cancelled
            }

            // Create a draft campaign with the first template and segment
            var request = new SmsCampaignRequest
            {
                Name = campaignName,
                TemplateId = templates.First().Id,
                SegmentId = segments.First().Id,
                Status = CampaignStatus.Draft
            };

            var result = await marketingService.CreateCampaignAsync(request);

            if (result.Success)
            {
                await _dialogService.ShowInfoAsync(
                    "Campaign Created",
                    $"Draft campaign '{campaignName}' has been created.\n\n" +
                    $"Template: {templates.First().Name}\n" +
                    $"Segment: {segments.First().Name}\n\n" +
                    "You can now view and configure the campaign from the list.");

                await LoadDashboardAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create campaign");
            await _dialogService.ShowErrorAsync("Error", "Failed to create campaign. Please try again.");
        }
    }

    [RelayCommand]
    private async Task ViewCampaignAsync(SmsCampaign campaign)
    {
        if (campaign == null) return;

        using var scope = _scopeFactory.CreateScope();
        var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

        if (marketingService == null) return;

        var report = await marketingService.GenerateCampaignReportAsync(campaign.Id);

        var message = $"Campaign: {campaign.Name}\n" +
                      $"Status: {campaign.Status}\n" +
                      $"Recipients: {report.TotalRecipients}\n" +
                      $"Sent: {report.SentCount}\n" +
                      $"Delivered: {report.DeliveredCount}\n" +
                      $"Failed: {report.FailedCount}\n" +
                      $"Delivery Rate: {report.DeliveryRate:N1}%\n" +
                      $"Total Cost: KES {report.TotalCost:N2}";

        await _dialogService.ShowInfoAsync("Campaign Details", message);
    }

    [RelayCommand]
    private async Task PauseCampaignAsync(SmsCampaign campaign)
    {
        if (campaign == null || campaign.Status != CampaignStatus.Sending) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Pause Campaign",
            $"Are you sure you want to pause '{campaign.Name}'?");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var result = await marketingService.PauseCampaignAsync(campaign.Id);
            if (result.Success)
            {
                await _dialogService.ShowInfoAsync("Success", "Campaign paused successfully.");
                await LoadDashboardAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to pause campaign {CampaignId}", campaign.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to pause campaign.");
        }
    }

    [RelayCommand]
    private async Task ResumeCampaignAsync(SmsCampaign campaign)
    {
        if (campaign == null || campaign.Status != CampaignStatus.Paused) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Resume Campaign",
            $"Are you sure you want to resume '{campaign.Name}'?");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var result = await marketingService.ResumeCampaignAsync(campaign.Id);
            if (result.Success)
            {
                await _dialogService.ShowInfoAsync("Success", "Campaign resumed successfully.");
                await LoadDashboardAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to resume campaign {CampaignId}", campaign.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to resume campaign.");
        }
    }

    [RelayCommand]
    private async Task CancelCampaignAsync(SmsCampaign campaign)
    {
        if (campaign == null) return;
        if (campaign.Status == CampaignStatus.Completed || campaign.Status == CampaignStatus.Cancelled) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Cancel Campaign",
            $"Are you sure you want to cancel '{campaign.Name}'? This cannot be undone.");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var result = await marketingService.CancelCampaignAsync(campaign.Id);
            if (result.Success)
            {
                await _dialogService.ShowInfoAsync("Success", "Campaign cancelled.");
                await LoadDashboardAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to cancel campaign {CampaignId}", campaign.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to cancel campaign.");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Helpers

    public static string GetStatusColor(CampaignStatus status)
    {
        return status switch
        {
            CampaignStatus.Draft => "#6B7280",      // Gray
            CampaignStatus.Scheduled => "#3B82F6", // Blue
            CampaignStatus.Sending => "#F59E0B",   // Yellow/Orange
            CampaignStatus.Paused => "#F97316",    // Orange
            CampaignStatus.Completed => "#10B981", // Green
            CampaignStatus.Cancelled => "#EF4444", // Red
            _ => "#6B7280"
        };
    }

    public static string GetStatusIcon(CampaignStatus status)
    {
        return status switch
        {
            CampaignStatus.Draft => "\uE70F",      // Edit
            CampaignStatus.Scheduled => "\uE787", // Clock
            CampaignStatus.Sending => "\uE724",   // Send
            CampaignStatus.Paused => "\uE769",    // Pause
            CampaignStatus.Completed => "\uE73E", // Checkmark
            CampaignStatus.Cancelled => "\uE711", // Cancel
            _ => "\uE946"
        };
    }

    #endregion
}
