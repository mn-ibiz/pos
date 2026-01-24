using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

#region DTOs

/// <summary>
/// DTO for creating or updating a modifier group.
/// </summary>
public class ModifierGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public ModifierSelectionType SelectionType { get; set; } = ModifierSelectionType.Multiple;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int FreeSelections { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorCode { get; set; }
    public string? IconPath { get; set; }
    public bool PrintOnKOT { get; set; } = true;
    public bool ShowOnReceipt { get; set; } = true;
    public string? KitchenStation { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ModifierItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for creating or updating a modifier item.
/// </summary>
public class ModifierItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ShortCode { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public bool IsDefault { get; set; }
    public int MaxQuantity { get; set; } = 10;
    public string? ColorCode { get; set; }
    public string? ImagePath { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? KOTText { get; set; }
    public decimal TaxRate { get; set; } = 16.00m;
    public int? InventoryProductId { get; set; }
    public decimal InventoryDeductQuantity { get; set; }
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
    public List<int> NestedGroupIds { get; set; } = new();
}

/// <summary>
/// DTO for linking a modifier group to a product.
/// </summary>
public class ProductModifierGroupDto
{
    public int ModifierGroupId { get; set; }
    public bool IsRequired { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public int? FreeSelections { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for linking a modifier group to a category.
/// </summary>
public class CategoryModifierGroupDto
{
    public int ModifierGroupId { get; set; }
    public bool IsRequired { get; set; }
    public bool InheritToProducts { get; set; } = true;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for modifier selection on an order item.
/// </summary>
public class OrderItemModifierDto
{
    public int ModifierItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a modifier preset.
/// </summary>
public class ModifierPresetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string? ColorCode { get; set; }
    public int DisplayOrder { get; set; }
    public List<ModifierPresetItemDto> PresetItems { get; set; } = new();
}

/// <summary>
/// DTO for preset item.
/// </summary>
public class ModifierPresetItemDto
{
    public int ModifierItemId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Modifier statistics.
/// </summary>
public class ModifierStatistics
{
    public int TotalGroups { get; set; }
    public int ActiveGroups { get; set; }
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int ProductsWithModifiers { get; set; }
}

/// <summary>
/// Result of modifier validation.
/// </summary>
public class ModifierValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ModifierGroup> MissingRequiredGroups { get; set; } = new();
    public decimal TotalModifierPrice { get; set; }
}

/// <summary>
/// Calculated modifier pricing.
/// </summary>
public class ModifierPricing
{
    public decimal TotalPrice { get; set; }
    public decimal TotalTax { get; set; }
    public int FreeItemsApplied { get; set; }
    public List<ModifierPricingLine> Lines { get; set; } = new();
}

/// <summary>
/// Single modifier pricing line.
/// </summary>
public class ModifierPricingLine
{
    public int ModifierItemId { get; set; }
    public string ModifierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsFree { get; set; }
}

#endregion

/// <summary>
/// Service interface for managing product modifiers.
/// </summary>
public interface IModifierService
{
    #region Modifier Groups

    /// <summary>
    /// Gets all modifier groups.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetAllModifierGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches modifier groups by name.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> SearchModifierGroupsAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active modifier groups.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetActiveModifierGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a modifier group by ID with its items.
    /// </summary>
    Task<ModifierGroup?> GetModifierGroupByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new modifier group with items.
    /// </summary>
    Task<ModifierGroup> CreateModifierGroupAsync(ModifierGroupDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a modifier group and its items.
    /// </summary>
    Task<ModifierGroup> UpdateModifierGroupAsync(int id, ModifierGroupDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a modifier group.
    /// </summary>
    Task<bool> DeleteModifierGroupAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active status of a modifier group.
    /// </summary>
    /// <param name="groupId">The modifier group ID.</param>
    /// <param name="isActive">The active status to set.</param>
    /// <param name="modifiedByUserId">The user ID making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful; false if group not found.</returns>
    Task<bool> SetModifierGroupActiveAsync(int groupId, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a modifier group.
    /// </summary>
    Task<ModifierGroup> DuplicateModifierGroupAsync(int id, string newName, int createdByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Modifier Items

    /// <summary>
    /// Gets all items in a modifier group.
    /// </summary>
    Task<IReadOnlyList<ModifierItem>> GetModifierItemsAsync(int groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available items in a modifier group.
    /// </summary>
    Task<IReadOnlyList<ModifierItem>> GetAvailableModifierItemsAsync(int groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a modifier item by ID.
    /// </summary>
    Task<ModifierItem?> GetModifierItemByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to a modifier group.
    /// </summary>
    Task<ModifierItem> AddModifierItemAsync(int groupId, ModifierItemDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a modifier item.
    /// </summary>
    Task<ModifierItem> UpdateModifierItemAsync(int id, ModifierItemDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a modifier item.
    /// </summary>
    Task<bool> DeleteModifierItemAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets modifier item availability.
    /// </summary>
    Task<ModifierItem> SetModifierItemAvailabilityAsync(int id, bool isAvailable, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders items within a modifier group.
    /// </summary>
    Task ReorderModifierItemsAsync(int groupId, List<int> itemIds, int modifiedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Modifiers

    /// <summary>
    /// Gets modifier groups for a product.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetProductModifierGroupsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets modifier groups for a product including category defaults.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetEffectiveProductModifierGroupsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links modifier groups to a product.
    /// </summary>
    Task LinkModifierGroupsToProductAsync(int productId, List<ProductModifierGroupDto> groups, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a modifier group from a product.
    /// </summary>
    Task RemoveModifierGroupFromProductAsync(int productId, int modifierGroupId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates product modifier group settings.
    /// </summary>
    Task UpdateProductModifierGroupAsync(int productId, int modifierGroupId, ProductModifierGroupDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Category Modifiers

    /// <summary>
    /// Gets default modifier groups for a category.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetCategoryModifierGroupsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links default modifier groups to a category.
    /// </summary>
    Task LinkModifierGroupsToCategoryAsync(int categoryId, List<CategoryModifierGroupDto> groups, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a modifier group from a category.
    /// </summary>
    Task RemoveModifierGroupFromCategoryAsync(int categoryId, int modifierGroupId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies category modifiers to all products in the category.
    /// </summary>
    Task ApplyCategoryModifiersToProductsAsync(int categoryId, int modifiedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Modifier Presets

    /// <summary>
    /// Gets presets for a product.
    /// </summary>
    Task<IReadOnlyList<ModifierPreset>> GetProductPresetsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets presets for a category.
    /// </summary>
    Task<IReadOnlyList<ModifierPreset>> GetCategoryPresetsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global presets.
    /// </summary>
    Task<IReadOnlyList<ModifierPreset>> GetGlobalPresetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a modifier preset.
    /// </summary>
    Task<ModifierPreset> CreatePresetAsync(ModifierPresetDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a modifier preset.
    /// </summary>
    Task<ModifierPreset> UpdatePresetAsync(int id, ModifierPresetDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a modifier preset.
    /// </summary>
    Task<bool> DeletePresetAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a preset to get modifier selections.
    /// </summary>
    Task<List<OrderItemModifierDto>> ApplyPresetAsync(int presetId, CancellationToken cancellationToken = default);

    #endregion

    #region Order Item Modifiers

    /// <summary>
    /// Adds modifiers to an order item.
    /// </summary>
    Task<IReadOnlyList<OrderItemModifier>> AddModifiersToOrderItemAsync(int orderItemId, List<OrderItemModifierDto> modifiers, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets modifiers for an order item.
    /// </summary>
    Task<IReadOnlyList<OrderItemModifier>> GetOrderItemModifiersAsync(int orderItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates modifiers on an order item.
    /// </summary>
    Task UpdateOrderItemModifiersAsync(int orderItemId, List<OrderItemModifierDto> modifiers, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all modifiers from an order item.
    /// </summary>
    Task ClearOrderItemModifiersAsync(int orderItemId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks order item modifiers as printed to kitchen.
    /// </summary>
    Task MarkModifiersPrintedAsync(int orderItemId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation & Pricing

    /// <summary>
    /// Validates modifier selections for a product.
    /// </summary>
    Task<ModifierValidationResult> ValidateModifierSelectionsAsync(int productId, List<OrderItemModifierDto> selections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates pricing for modifier selections.
    /// </summary>
    Task<ModifierPricing> CalculateModifierPricingAsync(int productId, List<OrderItemModifierDto> selections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total price for modifiers on an order item.
    /// </summary>
    Task<decimal> GetOrderItemModifierTotalAsync(int orderItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deducts inventory for modifiers.
    /// </summary>
    Task DeductModifierInventoryAsync(int orderItemId, CancellationToken cancellationToken = default);

    #endregion

    #region Nested Modifiers

    /// <summary>
    /// Links a nested modifier group to a modifier item.
    /// </summary>
    Task LinkNestedGroupAsync(int modifierItemId, int nestedGroupId, bool isRequired, int displayOrder, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a nested modifier group from a modifier item.
    /// </summary>
    Task RemoveNestedGroupAsync(int modifierItemId, int nestedGroupId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nested modifier groups for a modifier item.
    /// </summary>
    Task<IReadOnlyList<ModifierGroup>> GetNestedGroupsAsync(int modifierItemId, CancellationToken cancellationToken = default);

    #endregion

    #region Reporting

    /// <summary>
    /// Gets modifier usage statistics.
    /// </summary>
    Task<Dictionary<int, int>> GetModifierUsageStatsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revenue by modifier.
    /// </summary>
    Task<Dictionary<int, decimal>> GetModifierRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets modifier statistics.
    /// </summary>
    Task<ModifierStatistics> GetModifierStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion
}
