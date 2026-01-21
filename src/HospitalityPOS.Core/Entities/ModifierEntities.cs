namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of modifier group selection.
/// </summary>
public enum ModifierSelectionType
{
    /// <summary>Single selection (radio buttons).</summary>
    Single = 1,
    /// <summary>Multiple selection (checkboxes).</summary>
    Multiple = 2,
    /// <summary>Quantity-based selection (e.g., extra cheese x2).</summary>
    Quantity = 3
}

/// <summary>
/// Represents a group of modifiers (e.g., "Size", "Toppings", "Sides").
/// Modifiers are used in restaurant mode to customize menu items.
/// </summary>
public class ModifierGroup : BaseEntity
{
    /// <summary>
    /// Name of the modifier group (e.g., "Size", "Toppings", "Cooking Level").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of this modifier group.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of selection allowed.
    /// </summary>
    public ModifierSelectionType SelectionType { get; set; } = ModifierSelectionType.Multiple;

    /// <summary>
    /// Whether this modifier group is required (must select at least MinSelections).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Minimum number of selections required.
    /// </summary>
    public int MinSelections { get; set; }

    /// <summary>
    /// Maximum number of selections allowed (0 = unlimited).
    /// </summary>
    public int MaxSelections { get; set; }

    /// <summary>
    /// Whether free selections are allowed before charging.
    /// </summary>
    public int FreeSelections { get; set; }

    /// <summary>
    /// Display order for UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Color code for visual representation.
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon path for visual representation.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Whether this modifier group should be printed on KOT.
    /// </summary>
    public bool PrintOnKOT { get; set; } = true;

    /// <summary>
    /// Whether this modifier group should be shown on receipts.
    /// </summary>
    public bool ShowOnReceipt { get; set; } = true;

    /// <summary>
    /// Kitchen station for this modifier group.
    /// </summary>
    public string? KitchenStation { get; set; }

    // Navigation properties
    /// <summary>
    /// Items in this modifier group.
    /// </summary>
    public virtual ICollection<ModifierItem> Items { get; set; } = new List<ModifierItem>();

    /// <summary>
    /// Products that use this modifier group.
    /// </summary>
    public virtual ICollection<ProductModifierGroup> ProductModifierGroups { get; set; } = new List<ProductModifierGroup>();

    /// <summary>
    /// Categories that have this modifier group as default.
    /// </summary>
    public virtual ICollection<CategoryModifierGroup> CategoryModifierGroups { get; set; } = new List<CategoryModifierGroup>();
}

/// <summary>
/// Represents a specific modifier item within a group (e.g., "Extra Cheese" in "Toppings").
/// </summary>
public class ModifierItem : BaseEntity
{
    /// <summary>
    /// The parent modifier group.
    /// </summary>
    public int ModifierGroupId { get; set; }

    /// <summary>
    /// Name of the modifier item (e.g., "Extra Cheese", "Well Done").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Short code for POS display (e.g., "XCH" for "Extra Cheese").
    /// </summary>
    public string? ShortCode { get; set; }

    /// <summary>
    /// Description of this modifier.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Price of this modifier (0 = free).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Cost price for this modifier (for margin calculation).
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Whether this is the default selection.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Maximum quantity allowed for this modifier.
    /// </summary>
    public int MaxQuantity { get; set; } = 10;

    /// <summary>
    /// Color code for visual representation.
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Image path for visual representation.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Display order within the group.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this modifier is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Text to print on KOT (if different from name).
    /// </summary>
    public string? KOTText { get; set; }

    /// <summary>
    /// Tax rate for this modifier.
    /// </summary>
    public decimal TaxRate { get; set; } = 16.00m;

    /// <summary>
    /// Inventory item to deduct when this modifier is used.
    /// </summary>
    public int? InventoryProductId { get; set; }

    /// <summary>
    /// Quantity to deduct from inventory.
    /// </summary>
    public decimal InventoryDeductQuantity { get; set; }

    /// <summary>
    /// Calories for nutritional info.
    /// </summary>
    public int? Calories { get; set; }

    /// <summary>
    /// Allergen information.
    /// </summary>
    public string? Allergens { get; set; }

    // Navigation properties
    /// <summary>
    /// The parent modifier group.
    /// </summary>
    public virtual ModifierGroup? ModifierGroup { get; set; }

    /// <summary>
    /// The inventory product for deduction.
    /// </summary>
    public virtual Product? InventoryProduct { get; set; }

    /// <summary>
    /// Nested modifier groups (e.g., "Cheese Type" under "Extra Cheese").
    /// </summary>
    public virtual ICollection<ModifierItemNestedGroup> NestedGroups { get; set; } = new List<ModifierItemNestedGroup>();
}

/// <summary>
/// Links a modifier item to a nested modifier group.
/// </summary>
public class ModifierItemNestedGroup : BaseEntity
{
    /// <summary>
    /// The parent modifier item.
    /// </summary>
    public int ModifierItemId { get; set; }

    /// <summary>
    /// The nested modifier group.
    /// </summary>
    public int NestedModifierGroupId { get; set; }

    /// <summary>
    /// Whether selection from nested group is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    /// <summary>
    /// The parent modifier item.
    /// </summary>
    public virtual ModifierItem? ModifierItem { get; set; }

    /// <summary>
    /// The nested modifier group.
    /// </summary>
    public virtual ModifierGroup? NestedModifierGroup { get; set; }
}

/// <summary>
/// Links a product to a modifier group.
/// </summary>
public class ProductModifierGroup : BaseEntity
{
    /// <summary>
    /// The product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The modifier group.
    /// </summary>
    public int ModifierGroupId { get; set; }

    /// <summary>
    /// Whether this modifier group is required for this product.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Minimum selections required (overrides group default).
    /// </summary>
    public int? MinSelections { get; set; }

    /// <summary>
    /// Maximum selections allowed (overrides group default).
    /// </summary>
    public int? MaxSelections { get; set; }

    /// <summary>
    /// Free selections before charging (overrides group default).
    /// </summary>
    public int? FreeSelections { get; set; }

    /// <summary>
    /// Display order for this modifier group on this product.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    /// <summary>
    /// The product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// The modifier group.
    /// </summary>
    public virtual ModifierGroup? ModifierGroup { get; set; }
}

/// <summary>
/// Links a category to a modifier group (default modifiers for category).
/// </summary>
public class CategoryModifierGroup : BaseEntity
{
    /// <summary>
    /// The category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// The modifier group.
    /// </summary>
    public int ModifierGroupId { get; set; }

    /// <summary>
    /// Whether this modifier group is required for products in this category.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether products inherit this modifier group by default.
    /// </summary>
    public bool InheritToProducts { get; set; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    /// <summary>
    /// The category.
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// The modifier group.
    /// </summary>
    public virtual ModifierGroup? ModifierGroup { get; set; }
}

/// <summary>
/// Represents a modifier selection on an order item.
/// </summary>
public class OrderItemModifier : BaseEntity
{
    /// <summary>
    /// The order item.
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// The modifier item selected.
    /// </summary>
    public int ModifierItemId { get; set; }

    /// <summary>
    /// Quantity of this modifier.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price at time of order.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price (Quantity * UnitPrice).
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Notes for this modifier.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this was printed to kitchen.
    /// </summary>
    public bool PrintedToKitchen { get; set; }

    // Navigation properties
    /// <summary>
    /// The order item.
    /// </summary>
    public virtual OrderItem? OrderItem { get; set; }

    /// <summary>
    /// The modifier item.
    /// </summary>
    public virtual ModifierItem? ModifierItem { get; set; }
}

/// <summary>
/// Preset modifier combinations for quick selection.
/// </summary>
public class ModifierPreset : BaseEntity
{
    /// <summary>
    /// Name of the preset (e.g., "The Works", "Plain").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Product this preset applies to (null for global).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Category this preset applies to (null for global).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Color code for visual representation.
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    /// <summary>
    /// The product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// The category.
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Modifier items in this preset.
    /// </summary>
    public virtual ICollection<ModifierPresetItem> PresetItems { get; set; } = new List<ModifierPresetItem>();
}

/// <summary>
/// A modifier item in a preset.
/// </summary>
public class ModifierPresetItem : BaseEntity
{
    /// <summary>
    /// The preset.
    /// </summary>
    public int ModifierPresetId { get; set; }

    /// <summary>
    /// The modifier item.
    /// </summary>
    public int ModifierItemId { get; set; }

    /// <summary>
    /// Quantity of this modifier.
    /// </summary>
    public int Quantity { get; set; } = 1;

    // Navigation properties
    /// <summary>
    /// The preset.
    /// </summary>
    public virtual ModifierPreset? ModifierPreset { get; set; }

    /// <summary>
    /// The modifier item.
    /// </summary>
    public virtual ModifierItem? ModifierItem { get; set; }
}
