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
/// ViewModel for managing SMS templates.
/// </summary>
public partial class SmsTemplateListViewModel : ObservableObject, INavigationAware
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
    private SmsTemplateCategory? _selectedCategory;

    [ObservableProperty]
    private SmsTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _previewMessage = string.Empty;

    public ObservableCollection<SmsTemplate> Templates { get; } = new();
    public ObservableCollection<CategoryFilterOption> Categories { get; } = new();

    #endregion

    public SmsTemplateListViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;

        // Initialize categories
        Categories.Add(new CategoryFilterOption(null, "All Categories"));
        Categories.Add(new CategoryFilterOption(SmsTemplateCategory.Promotion, "Promotion"));
        Categories.Add(new CategoryFilterOption(SmsTemplateCategory.Loyalty, "Loyalty"));
        Categories.Add(new CategoryFilterOption(SmsTemplateCategory.Transactional, "Transactional"));
        Categories.Add(new CategoryFilterOption(SmsTemplateCategory.Special, "Special"));
    }

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadTemplatesAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading templates...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null)
            {
                ErrorMessage = "SMS Marketing service is not available.";
                return;
            }

            var templates = await marketingService.GetTemplatesAsync(SelectedCategory);

            Templates.Clear();
            foreach (var template in templates.OrderBy(t => t.Category).ThenBy(t => t.Name))
            {
                Templates.Add(template);
            }

            _logger.Information("Loaded {Count} SMS templates", Templates.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load SMS templates");
            ErrorMessage = "Failed to load templates.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    partial void OnSelectedCategoryChanged(SmsTemplateCategory? value)
    {
        _ = LoadTemplatesAsync();
    }

    partial void OnSelectedTemplateChanged(SmsTemplate? value)
    {
        if (value != null)
        {
            _ = PreviewTemplateAsync(value);
        }
        else
        {
            PreviewMessage = string.Empty;
        }
    }

    private async Task PreviewTemplateAsync(SmsTemplate template)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetService<ISmsMarketingService>();

            if (marketingService == null) return;

            var sampleData = new Dictionary<string, string>
            {
                { "CustomerName", "John Doe" },
                { "FirstName", "John" },
                { "Points", "500" },
                { "TierName", "Gold" },
                { "StoreName", "HospitalityPOS" },
                { "ExpiryDate", DateTime.Now.AddDays(30).ToString("MMM dd, yyyy") },
                { "DiscountPercent", "15" }
            };

            var preview = await marketingService.PreviewTemplateAsync(template.Id, sampleData);
            PreviewMessage = preview.RenderedMessage;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to preview template {TemplateId}", template.Id);
            PreviewMessage = template.MessageText;
        }
    }

    [RelayCommand]
    private async Task CreateTemplateAsync()
    {
        var result = await ShowTemplateEditorAsync(null);
        if (result != null)
        {
            await LoadTemplatesAsync();
        }
    }

    [RelayCommand]
    private async Task EditTemplateAsync(SmsTemplate? template)
    {
        if (template == null) return;

        var result = await ShowTemplateEditorAsync(template);
        if (result != null)
        {
            await LoadTemplatesAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(SmsTemplate? template)
    {
        if (template == null) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Delete Template",
            $"Are you sure you want to delete '{template.Name}'? This cannot be undone.");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var success = await marketingService.DeleteTemplateAsync(template.Id);
            if (success)
            {
                Templates.Remove(template);
                _logger.Information("Deleted template {TemplateId}", template.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to delete template.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete template {TemplateId}", template.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to delete template.");
        }
    }

    [RelayCommand]
    private async Task DuplicateTemplateAsync(SmsTemplate? template)
    {
        if (template == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var request = new SmsTemplateRequest
            {
                Name = $"{template.Name} (Copy)",
                Category = template.Category,
                MessageText = template.MessageText
            };

            var newTemplate = await marketingService.CreateTemplateAsync(request);
            Templates.Add(newTemplate);
            SelectedTemplate = newTemplate;

            _logger.Information("Duplicated template {TemplateId} as {NewTemplateId}", template.Id, newTemplate.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to duplicate template {TemplateId}", template.Id);
            await _dialogService.ShowErrorAsync("Error", "Failed to duplicate template.");
        }
    }

    private async Task<SmsTemplate?> ShowTemplateEditorAsync(SmsTemplate? existingTemplate)
    {
        // Simple input dialog for now - can be replaced with full editor dialog
        var name = existingTemplate?.Name ?? string.Empty;
        var message = existingTemplate?.MessageText ?? string.Empty;
        var category = existingTemplate?.Category ?? SmsTemplateCategory.Promotion;

        // Show simple dialog for template editing
        var input = await _dialogService.ShowInputAsync(
            existingTemplate == null ? "Create Template" : "Edit Template",
            "Enter template name:",
            existingTemplate?.Name ?? "");

        if (string.IsNullOrWhiteSpace(input)) return null;

        var messageInput = await _dialogService.ShowInputAsync(
            "Template Message",
            "Enter message text (use placeholders like {CustomerName}, {Points}):",
            existingTemplate?.MessageText ?? "");

        if (string.IsNullOrWhiteSpace(messageInput)) return null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var marketingService = scope.ServiceProvider.GetRequiredService<ISmsMarketingService>();

            var request = new SmsTemplateRequest
            {
                Id = existingTemplate?.Id,
                Name = input.Trim(),
                Category = category,
                MessageText = messageInput.Trim()
            };

            SmsTemplate result;
            if (existingTemplate == null)
            {
                result = await marketingService.CreateTemplateAsync(request);
            }
            else
            {
                result = await marketingService.UpdateTemplateAsync(request);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save template");
            await _dialogService.ShowErrorAsync("Error", "Failed to save template.");
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

    public static string GetCategoryColor(SmsTemplateCategory category)
    {
        return category switch
        {
            SmsTemplateCategory.Promotion => "#F59E0B",    // Amber
            SmsTemplateCategory.Loyalty => "#10B981",     // Green
            SmsTemplateCategory.Transactional => "#3B82F6", // Blue
            SmsTemplateCategory.Special => "#EC4899",     // Pink
            _ => "#6B7280"
        };
    }

    #endregion
}

/// <summary>
/// Category filter option for dropdown.
/// </summary>
public class CategoryFilterOption
{
    public SmsTemplateCategory? Value { get; }
    public string DisplayName { get; }

    public CategoryFilterOption(SmsTemplateCategory? value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
