using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for managing modifier groups in the admin interface.
/// </summary>
public partial class ModifierGroupsViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    [ObservableProperty]
    private ObservableCollection<ModifierGroup> _modifierGroups = new();

    [ObservableProperty]
    private ModifierGroup? _selectedGroup;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    // Statistics
    [ObservableProperty]
    private int _totalGroups;

    [ObservableProperty]
    private int _activeGroups;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _productsUsingModifiers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifierGroupsViewModel"/> class.
    /// </summary>
    public ModifierGroupsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISessionService sessionService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _sessionService = sessionService;
        _logger = logger;
    }

    private int CurrentUserId => _sessionService.CurrentUser?.Id ?? 1;

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    /// <summary>
    /// Loads all modifier groups and statistics.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

            // Load all modifier groups with their items
            var groups = await modifierService.GetAllModifierGroupsAsync();

            ModifierGroups = new ObservableCollection<ModifierGroup>(groups);

            // Load statistics
            var stats = await modifierService.GetModifierStatisticsAsync();
            TotalGroups = stats.TotalGroups;
            ActiveGroups = stats.ActiveGroups;
            TotalItems = stats.TotalItems;
            ProductsUsingModifiers = stats.ProductsWithModifiers;

            _logger.Information("Loaded {Count} modifier groups", ModifierGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load modifier groups");
            ErrorMessage = "Failed to load modifier groups. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Filters groups based on search text.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        FilterGroups();
    }

    private async void FilterGroups()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

            IEnumerable<ModifierGroup> groups;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                groups = await modifierService.GetAllModifierGroupsAsync();
            }
            else
            {
                groups = await modifierService.SearchModifierGroupsAsync(SearchText);
            }

            ModifierGroups = new ObservableCollection<ModifierGroup>(groups);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to filter modifier groups");
        }
    }

    /// <summary>
    /// Creates a new modifier group.
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        try
        {
            var result = await _dialogService.ShowModifierGroupEditorDialogAsync(null);

            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                var dto = new ModifierGroupDto
                {
                    Name = result.Name,
                    Description = result.Description,
                    SelectionType = result.SelectionType,
                    MinSelections = result.MinSelections,
                    MaxSelections = result.MaxSelections,
                    IsRequired = result.IsRequired,
                    DisplayOrder = result.DisplayOrder,
                    Items = result.Items.Select(i => new ModifierItemDto
                    {
                        Name = i.Name,
                        DisplayName = i.DisplayName,
                        ShortCode = i.ShortCode,
                        Price = i.Price,
                        CostPrice = i.CostPrice,
                        TaxRate = i.TaxRate,
                        MaxQuantity = i.MaxQuantity,
                        KOTText = i.KOTText,
                        Allergens = i.Allergens,
                        IsDefault = i.IsDefault,
                        IsAvailable = i.IsAvailable
                    }).ToList()
                };

                await modifierService.CreateModifierGroupAsync(dto, CurrentUserId);

                _logger.Information("Created modifier group: {Name}", result.Name);
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create modifier group");
            ErrorMessage = "Failed to create modifier group. Please try again.";
        }
    }

    /// <summary>
    /// Edits the selected modifier group.
    /// </summary>
    [RelayCommand]
    private async Task EditGroupAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            var result = await _dialogService.ShowModifierGroupEditorDialogAsync(SelectedGroup);

            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                var dto = new ModifierGroupDto
                {
                    Name = result.Name,
                    Description = result.Description,
                    SelectionType = result.SelectionType,
                    MinSelections = result.MinSelections,
                    MaxSelections = result.MaxSelections,
                    IsRequired = result.IsRequired,
                    DisplayOrder = result.DisplayOrder,
                    Items = result.Items.Select(i => new ModifierItemDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        DisplayName = i.DisplayName,
                        ShortCode = i.ShortCode,
                        Price = i.Price,
                        CostPrice = i.CostPrice,
                        TaxRate = i.TaxRate,
                        MaxQuantity = i.MaxQuantity,
                        KOTText = i.KOTText,
                        Allergens = i.Allergens,
                        IsDefault = i.IsDefault,
                        IsAvailable = i.IsAvailable
                    }).ToList()
                };

                await modifierService.UpdateModifierGroupAsync(SelectedGroup.Id, dto, CurrentUserId);

                _logger.Information("Updated modifier group: {Name}", result.Name);
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update modifier group");
            ErrorMessage = "Failed to update modifier group. Please try again.";
        }
    }

    /// <summary>
    /// Deletes the selected modifier group.
    /// </summary>
    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Modifier Group",
                $"Are you sure you want to delete '{SelectedGroup.Name}'? This will also delete all items in this group.");

            if (confirmed)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                await modifierService.DeleteModifierGroupAsync(SelectedGroup.Id, CurrentUserId);

                _logger.Information("Deleted modifier group: {Name}", SelectedGroup.Name);
                SelectedGroup = null;
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete modifier group");
            ErrorMessage = "Failed to delete modifier group. Please try again.";
        }
    }

    /// <summary>
    /// Adds a new item to the selected modifier group.
    /// </summary>
    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            var result = await _dialogService.ShowModifierItemEditorDialogAsync(null, SelectedGroup);

            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                var dto = new ModifierItemDto
                {
                    Name = result.Name,
                    DisplayName = result.DisplayName,
                    ShortCode = result.ShortCode,
                    Price = result.Price,
                    CostPrice = result.CostPrice,
                    TaxRate = result.TaxRate,
                    MaxQuantity = result.MaxQuantity,
                    KOTText = result.KOTText,
                    Allergens = result.Allergens,
                    IsDefault = result.IsDefault,
                    IsAvailable = result.IsAvailable
                };

                await modifierService.AddModifierItemAsync(SelectedGroup.Id, dto, CurrentUserId);

                _logger.Information("Added modifier item '{ItemName}' to group '{GroupName}'",
                    result.Name, SelectedGroup.Name);

                var groupId = SelectedGroup.Id;
                await LoadDataAsync();

                // Re-select the group to show updated items
                SelectedGroup = ModifierGroups.FirstOrDefault(g => g.Id == groupId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add modifier item");
            ErrorMessage = "Failed to add modifier item. Please try again.";
        }
    }

    /// <summary>
    /// Edits a modifier item.
    /// </summary>
    [RelayCommand]
    private async Task EditItemAsync(ModifierItem? item)
    {
        if (item == null || SelectedGroup == null) return;

        try
        {
            var result = await _dialogService.ShowModifierItemEditorDialogAsync(item, SelectedGroup);

            if (result != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                var dto = new ModifierItemDto
                {
                    Name = result.Name,
                    DisplayName = result.DisplayName,
                    ShortCode = result.ShortCode,
                    Price = result.Price,
                    CostPrice = result.CostPrice,
                    TaxRate = result.TaxRate,
                    MaxQuantity = result.MaxQuantity,
                    KOTText = result.KOTText,
                    Allergens = result.Allergens,
                    IsDefault = result.IsDefault,
                    IsAvailable = result.IsAvailable
                };

                await modifierService.UpdateModifierItemAsync(item.Id, dto, CurrentUserId);

                _logger.Information("Updated modifier item: {Name}", result.Name);

                var groupId = SelectedGroup.Id;
                await LoadDataAsync();

                // Re-select the group to show updated items
                SelectedGroup = ModifierGroups.FirstOrDefault(g => g.Id == groupId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update modifier item");
            ErrorMessage = "Failed to update modifier item. Please try again.";
        }
    }

    /// <summary>
    /// Deletes a modifier item.
    /// </summary>
    [RelayCommand]
    private async Task DeleteItemAsync(ModifierItem? item)
    {
        if (item == null || SelectedGroup == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Modifier Item",
                $"Are you sure you want to delete '{item.Name}'?");

            if (confirmed)
            {
                using var scope = _scopeFactory.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                await modifierService.DeleteModifierItemAsync(item.Id, CurrentUserId);

                _logger.Information("Deleted modifier item: {Name}", item.Name);

                var groupId = SelectedGroup.Id;
                await LoadDataAsync();

                // Re-select the group to show updated items
                SelectedGroup = ModifierGroups.FirstOrDefault(g => g.Id == groupId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete modifier item");
            ErrorMessage = "Failed to delete modifier item. Please try again.";
        }
    }

    /// <summary>
    /// Toggles the active status of the selected group.
    /// </summary>
    [RelayCommand]
    private async Task ToggleGroupActiveAsync()
    {
        if (SelectedGroup == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            // Toggle the active status using the dedicated method
            var newActiveStatus = !SelectedGroup.IsActive;
            var success = await modifierService.SetModifierGroupActiveAsync(
                SelectedGroup.Id,
                newActiveStatus,
                sessionService.CurrentUserId);

            if (success)
            {
                _logger.Information("Modifier group '{Name}' active status set to {IsActive}",
                    SelectedGroup.Name, newActiveStatus);
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = "Failed to update modifier group. Group not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle modifier group active status");
            ErrorMessage = "Failed to update modifier group. Please try again.";
        }
    }

    /// <summary>
    /// Toggles the availability of a modifier item.
    /// </summary>
    [RelayCommand]
    private async Task ToggleItemAvailabilityAsync(ModifierItem? item)
    {
        if (item == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

            await modifierService.SetModifierItemAvailabilityAsync(item.Id, !item.IsAvailable, CurrentUserId);

            _logger.Information("Toggled modifier item availability: {Name} = {IsAvailable}",
                item.Name, !item.IsAvailable);

            // Update locally
            item.IsAvailable = !item.IsAvailable;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle modifier item availability");
            ErrorMessage = "Failed to update modifier item. Please try again.";
        }
    }

    /// <summary>
    /// Moves a modifier item up in the display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveItemUpAsync(ModifierItem? item)
    {
        if (item == null || SelectedGroup == null) return;

        try
        {
            var items = SelectedGroup.Items.OrderBy(i => i.DisplayOrder).ToList();
            var index = items.FindIndex(i => i.Id == item.Id);

            if (index <= 0) return; // Already at top

            // Swap with previous item
            items.RemoveAt(index);
            items.Insert(index - 1, item);

            // Get new order as list of IDs
            var newOrder = items.Select(i => i.Id).ToList();

            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

            await modifierService.ReorderModifierItemsAsync(SelectedGroup.Id, newOrder, CurrentUserId);

            _logger.Information("Moved modifier item '{ItemName}' up in group '{GroupName}'",
                item.Name, SelectedGroup.Name);

            // Reload to show updated order
            var groupId = SelectedGroup.Id;
            await LoadDataAsync();
            SelectedGroup = ModifierGroups.FirstOrDefault(g => g.Id == groupId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to reorder modifier item");
            ErrorMessage = "Failed to reorder item. Please try again.";
        }
    }

    /// <summary>
    /// Moves a modifier item down in the display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveItemDownAsync(ModifierItem? item)
    {
        if (item == null || SelectedGroup == null) return;

        try
        {
            var items = SelectedGroup.Items.OrderBy(i => i.DisplayOrder).ToList();
            var index = items.FindIndex(i => i.Id == item.Id);

            if (index >= items.Count - 1) return; // Already at bottom

            // Swap with next item
            items.RemoveAt(index);
            items.Insert(index + 1, item);

            // Get new order as list of IDs
            var newOrder = items.Select(i => i.Id).ToList();

            using var scope = _scopeFactory.CreateScope();
            var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

            await modifierService.ReorderModifierItemsAsync(SelectedGroup.Id, newOrder, CurrentUserId);

            _logger.Information("Moved modifier item '{ItemName}' down in group '{GroupName}'",
                item.Name, SelectedGroup.Name);

            // Reload to show updated order
            var groupId = SelectedGroup.Id;
            await LoadDataAsync();
            SelectedGroup = ModifierGroups.FirstOrDefault(g => g.Id == groupId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to reorder modifier item");
            ErrorMessage = "Failed to reorder item. Please try again.";
        }
    }
}
