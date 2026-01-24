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
/// ViewModel for managing customer segments for SMS targeting.
/// </summary>
public partial class CustomerSegmentListViewModel : ObservableObject, INavigationAware
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

    [ObservableProperty]
    private CustomerSegment? _selectedSegment;

    [ObservableProperty]
    private int _previewCount;

    [ObservableProperty]
    private bool _isPreviewLoading;

    public ObservableCollection<CustomerSegment> Segments { get; } = new();
    public ObservableCollection<CustomerSmsInfo> PreviewCustomers { get; } = new();

    #endregion

    public CustomerSegmentListViewModel(
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
        _ = LoadSegmentsAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadSegmentsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading segments...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null)
            {
                ErrorMessage = "SMS Marketing service is not available.";
                return;
            }

            var segments = await marketingService.GetSegmentsAsync();

            Segments.Clear();
            foreach (var segment in segments.OrderBy(s => s.Name))
            {
                Segments.Add(segment);
            }

            _logger.Information("Loaded {Count} customer segments", Segments.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load customer segments");
            ErrorMessage = "Failed to load segments.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    partial void OnSelectedSegmentChanged(CustomerSegment? value)
    {
        if (value != null)
        {
            _ = LoadSegmentPreviewAsync(value);
        }
        else
        {
            PreviewCount = 0;
            PreviewCustomers.Clear();
        }
    }

    private async Task LoadSegmentPreviewAsync(CustomerSegment segment)
    {
        try
        {
            IsPreviewLoading = true;
            PreviewCustomers.Clear();

            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null) return;

            var result = await marketingService.EvaluateSegmentAsync(segment.FilterCriteria);
            PreviewCount = result.MatchingCount;

            // Show first 10 customers as preview
            foreach (var customer in result.Customers.Take(10))
            {
                PreviewCustomers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load segment preview for {SegmentId}", segment.Id);
            PreviewCount = segment.CachedCount;
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateSegmentAsync()
    {
        var result = await ShowSegmentBuilderAsync(null);
        if (result != null)
        {
            Segments.Add(result);
            SelectedSegment = result;
        }
    }

    [RelayCommand]
    private async Task EditSegmentAsync(CustomerSegment? segment)
    {
        if (segment == null) return;

        var result = await ShowSegmentBuilderAsync(segment);
        if (result != null)
        {
            await LoadSegmentsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSegmentAsync(CustomerSegment? segment)
    {
        if (segment == null) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Delete Segment",
            $"Are you sure you want to delete '{segment.Name}'? This cannot be undone.");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var success = await marketingService.DeleteSegmentAsync(segment.Id);
            if (success)
            {
                Segments.Remove(segment);
                if (SelectedSegment == segment)
                {
                    SelectedSegment = null;
                }
                _logger.Information("Deleted segment {SegmentId}", segment.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to delete segment.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete segment {SegmentId}", segment.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to delete segment.");
        }
    }

    [RelayCommand]
    private async Task RefreshCountAsync(CustomerSegment? segment)
    {
        if (segment == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var count = await marketingService.GetSegmentCountAsync(segment.FilterCriteria);
            segment.CachedCount = count;
            segment.LastCalculatedAt = DateTime.Now;

            // Update in list
            var index = Segments.IndexOf(segment);
            if (index >= 0)
            {
                Segments[index] = segment;
            }

            if (SelectedSegment?.Id == segment.Id)
            {
                PreviewCount = count;
            }

            _logger.Information("Refreshed segment {SegmentId} count: {Count}", segment.Id, count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to refresh segment count {SegmentId}", segment.Id);
        }
    }

    [RelayCommand]
    private async Task CreateCampaignFromSegmentAsync(CustomerSegment? segment)
    {
        if (segment == null) return;

        // Navigate to campaign creation with segment pre-selected
        await _dialogService.ShowInfoAsync("Create Campaign",
            $"Creating campaign for segment '{segment.Name}' with {segment.CachedCount} customers.\n\n" +
            "Full campaign wizard will be available in the next update.");
    }

    private async Task<CustomerSegment?> ShowSegmentBuilderAsync(CustomerSegment? existingSegment)
    {
        // Simple segment creation for now - can be replaced with full builder dialog
        var name = await _dialogService.ShowInputAsync(
            existingSegment == null ? "Create Segment" : "Edit Segment",
            "Enter segment name:",
            existingSegment?.Name ?? "");

        if (string.IsNullOrWhiteSpace(name)) return null;

        var description = await _dialogService.ShowInputAsync(
            "Segment Description",
            "Enter description (optional):",
            existingSegment?.Description ?? "");

        // For now, create a simple segment with all opted-in customers
        // Full segment builder with criteria will be added later
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            var userId = sessionService.CurrentUser?.Id ?? 0;

            var request = new CustomerSegmentRequest
            {
                Id = existingSegment?.Id,
                Name = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                FilterCriteria = existingSegment?.FilterCriteria ?? new SegmentFilter
                {
                    Logic = SegmentLogic.And,
                    IncludeOnlyOptedIn = true,
                    IncludeOnlyActive = true
                },
                CreatedByUserId = userId
            };

            CustomerSegment result;
            if (existingSegment == null)
            {
                result = await marketingService.CreateSegmentAsync(request);
            }
            else
            {
                result = await marketingService.UpdateSegmentAsync(request);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save segment");
            await _dialogService.ShowErrorAsync("Error", "Failed to save segment.");
            return null;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion

    #region Helpers

    public static string GetSegmentTypeDisplay(SegmentType type)
    {
        return type switch
        {
            SegmentType.Static => "Static List",
            SegmentType.Dynamic => "Dynamic Query",
            _ => type.ToString()
        };
    }

    #endregion
}
