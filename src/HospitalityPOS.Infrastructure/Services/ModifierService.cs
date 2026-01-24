using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing product modifiers.
/// </summary>
public class ModifierService : IModifierService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    public ModifierService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Modifier Groups

    public async Task<IReadOnlyList<ModifierGroup>> GetAllModifierGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ModifierGroups
            .AsNoTracking()
            .Include(mg => mg.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(mg => mg.DisplayOrder)
            .ThenBy(mg => mg.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModifierGroup>> SearchModifierGroupsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchText.Trim().ToLower();

        return await _context.ModifierGroups
            .AsNoTracking()
            .Where(mg => mg.Name.ToLower().Contains(normalizedSearch) ||
                        (mg.DisplayName != null && mg.DisplayName.ToLower().Contains(normalizedSearch)) ||
                        (mg.Description != null && mg.Description.ToLower().Contains(normalizedSearch)))
            .Include(mg => mg.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(mg => mg.DisplayOrder)
            .ThenBy(mg => mg.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModifierGroup>> GetActiveModifierGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ModifierGroups
            .AsNoTracking()
            .Where(mg => mg.IsActive)
            .Include(mg => mg.Items.Where(i => i.IsActive && i.IsAvailable).OrderBy(i => i.DisplayOrder))
            .OrderBy(mg => mg.DisplayOrder)
            .ThenBy(mg => mg.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ModifierGroup?> GetModifierGroupByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierGroups
            .AsNoTracking()
            .Include(mg => mg.Items.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(mg => mg.Id == id, cancellationToken);
    }

    public async Task<ModifierGroup> CreateModifierGroupAsync(ModifierGroupDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var group = new ModifierGroup
        {
            Name = dto.Name.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            Description = dto.Description,
            SelectionType = dto.SelectionType,
            IsRequired = dto.IsRequired,
            MinSelections = dto.MinSelections,
            MaxSelections = dto.MaxSelections,
            FreeSelections = dto.FreeSelections,
            DisplayOrder = dto.DisplayOrder,
            ColorCode = dto.ColorCode,
            IconPath = dto.IconPath,
            PrintOnKOT = dto.PrintOnKOT,
            ShowOnReceipt = dto.ShowOnReceipt,
            KitchenStation = dto.KitchenStation,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        // Add items
        var order = 0;
        foreach (var itemDto in dto.Items)
        {
            group.Items.Add(new ModifierItem
            {
                Name = itemDto.Name.Trim(),
                DisplayName = itemDto.DisplayName?.Trim(),
                ShortCode = itemDto.ShortCode,
                Description = itemDto.Description,
                Price = itemDto.Price,
                CostPrice = itemDto.CostPrice,
                IsDefault = itemDto.IsDefault,
                MaxQuantity = itemDto.MaxQuantity,
                ColorCode = itemDto.ColorCode,
                ImagePath = itemDto.ImagePath,
                DisplayOrder = itemDto.DisplayOrder > 0 ? itemDto.DisplayOrder : order++,
                IsAvailable = itemDto.IsAvailable,
                KOTText = itemDto.KOTText,
                TaxRate = itemDto.TaxRate,
                InventoryProductId = itemDto.InventoryProductId,
                InventoryDeductQuantity = itemDto.InventoryDeductQuantity,
                Calories = itemDto.Calories,
                Allergens = itemDto.Allergens,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            });
        }

        await _context.ModifierGroups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Modifier group '{GroupName}' created with {ItemCount} items by user {UserId}",
            group.Name, group.Items.Count, createdByUserId);

        return group;
    }

    public async Task<ModifierGroup> UpdateModifierGroupAsync(int id, ModifierGroupDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ModifierGroups
            .Include(mg => mg.Items)
            .FirstOrDefaultAsync(mg => mg.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier group with ID {id} not found.");

        group.Name = dto.Name.Trim();
        group.DisplayName = dto.DisplayName?.Trim();
        group.Description = dto.Description;
        group.SelectionType = dto.SelectionType;
        group.IsRequired = dto.IsRequired;
        group.MinSelections = dto.MinSelections;
        group.MaxSelections = dto.MaxSelections;
        group.FreeSelections = dto.FreeSelections;
        group.DisplayOrder = dto.DisplayOrder;
        group.ColorCode = dto.ColorCode;
        group.IconPath = dto.IconPath;
        group.PrintOnKOT = dto.PrintOnKOT;
        group.ShowOnReceipt = dto.ShowOnReceipt;
        group.KitchenStation = dto.KitchenStation;
        group.UpdatedAt = DateTime.UtcNow;
        group.UpdatedByUserId = modifiedByUserId;

        // Update items
        var existingItemIds = group.Items.Select(i => i.Id).ToHashSet();
        var incomingItemIds = dto.Items.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();

        // Remove items not in incoming
        var itemsToRemove = group.Items.Where(i => !incomingItemIds.Contains(i.Id)).ToList();
        foreach (var item in itemsToRemove)
        {
            _context.ModifierItems.Remove(item);
        }

        // Update or add items
        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Id > 0 && existingItemIds.Contains(itemDto.Id))
            {
                var existing = group.Items.First(i => i.Id == itemDto.Id);
                existing.Name = itemDto.Name.Trim();
                existing.DisplayName = itemDto.DisplayName?.Trim();
                existing.ShortCode = itemDto.ShortCode;
                existing.Description = itemDto.Description;
                existing.Price = itemDto.Price;
                existing.CostPrice = itemDto.CostPrice;
                existing.IsDefault = itemDto.IsDefault;
                existing.MaxQuantity = itemDto.MaxQuantity;
                existing.ColorCode = itemDto.ColorCode;
                existing.ImagePath = itemDto.ImagePath;
                existing.DisplayOrder = itemDto.DisplayOrder;
                existing.IsAvailable = itemDto.IsAvailable;
                existing.KOTText = itemDto.KOTText;
                existing.TaxRate = itemDto.TaxRate;
                existing.InventoryProductId = itemDto.InventoryProductId;
                existing.InventoryDeductQuantity = itemDto.InventoryDeductQuantity;
                existing.Calories = itemDto.Calories;
                existing.Allergens = itemDto.Allergens;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = modifiedByUserId;
            }
            else
            {
                group.Items.Add(new ModifierItem
                {
                    Name = itemDto.Name.Trim(),
                    DisplayName = itemDto.DisplayName?.Trim(),
                    ShortCode = itemDto.ShortCode,
                    Description = itemDto.Description,
                    Price = itemDto.Price,
                    CostPrice = itemDto.CostPrice,
                    IsDefault = itemDto.IsDefault,
                    MaxQuantity = itemDto.MaxQuantity,
                    ColorCode = itemDto.ColorCode,
                    ImagePath = itemDto.ImagePath,
                    DisplayOrder = itemDto.DisplayOrder,
                    IsAvailable = itemDto.IsAvailable,
                    KOTText = itemDto.KOTText,
                    TaxRate = itemDto.TaxRate,
                    InventoryProductId = itemDto.InventoryProductId,
                    InventoryDeductQuantity = itemDto.InventoryDeductQuantity,
                    Calories = itemDto.Calories,
                    Allergens = itemDto.Allergens,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = modifiedByUserId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Modifier group '{GroupName}' (ID: {GroupId}) updated by user {UserId}",
            group.Name, id, modifiedByUserId);

        return group;
    }

    public async Task<bool> DeleteModifierGroupAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ModifierGroups
            .Include(mg => mg.ProductModifierGroups)
            .Include(mg => mg.CategoryModifierGroups)
            .FirstOrDefaultAsync(mg => mg.Id == id, cancellationToken);

        if (group is null) return false;

        if (group.ProductModifierGroups.Any() || group.CategoryModifierGroups.Any())
        {
            // Soft delete
            group.IsActive = false;
            group.UpdatedAt = DateTime.UtcNow;
            group.UpdatedByUserId = deletedByUserId;
        }
        else
        {
            _context.ModifierGroups.Remove(group);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Modifier group '{GroupName}' (ID: {GroupId}) deleted by user {UserId}",
            group.Name, id, deletedByUserId);

        return true;
    }

    public async Task<bool> SetModifierGroupActiveAsync(int groupId, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ModifierGroups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group == null) return false;

        group.IsActive = isActive;
        group.UpdatedAt = DateTime.UtcNow;
        group.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Modifier group '{GroupName}' (ID: {GroupId}) active status set to {IsActive} by user {UserId}",
            group.Name, groupId, isActive, modifiedByUserId);

        return true;
    }

    public async Task<ModifierGroup> DuplicateModifierGroupAsync(int id, string newName, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var source = await _context.ModifierGroups
            .AsNoTracking()
            .Include(mg => mg.Items)
            .FirstOrDefaultAsync(mg => mg.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier group with ID {id} not found.");

        var dto = new ModifierGroupDto
        {
            Id = source.Id,
            Name = newName.Trim(),
            DisplayName = source.DisplayName,
            Description = source.Description,
            SelectionType = source.SelectionType,
            IsRequired = source.IsRequired,
            MinSelections = source.MinSelections,
            MaxSelections = source.MaxSelections,
            FreeSelections = source.FreeSelections,
            DisplayOrder = source.DisplayOrder + 1,
            ColorCode = source.ColorCode,
            IconPath = source.IconPath,
            PrintOnKOT = source.PrintOnKOT,
            ShowOnReceipt = source.ShowOnReceipt,
            KitchenStation = source.KitchenStation,
            IsActive = source.IsActive,
            Items = source.Items.Select(i => new ModifierItemDto
            {
                Name = i.Name,
                DisplayName = i.DisplayName,
                ShortCode = i.ShortCode,
                Description = i.Description,
                Price = i.Price,
                CostPrice = i.CostPrice,
                IsDefault = i.IsDefault,
                MaxQuantity = i.MaxQuantity,
                ColorCode = i.ColorCode,
                ImagePath = i.ImagePath,
                DisplayOrder = i.DisplayOrder,
                IsAvailable = i.IsAvailable,
                KOTText = i.KOTText,
                TaxRate = i.TaxRate,
                InventoryProductId = i.InventoryProductId,
                InventoryDeductQuantity = i.InventoryDeductQuantity,
                Calories = i.Calories,
                Allergens = i.Allergens
            }).ToList()
        };

        return await CreateModifierGroupAsync(dto, createdByUserId, cancellationToken);
    }

    #endregion

    #region Modifier Items

    public async Task<IReadOnlyList<ModifierItem>> GetModifierItemsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierItems
            .AsNoTracking()
            .Where(mi => mi.ModifierGroupId == groupId)
            .Include(mi => mi.NestedGroups)
                .ThenInclude(ng => ng.NestedModifierGroup)
            .OrderBy(mi => mi.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModifierItem>> GetAvailableModifierItemsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierItems
            .AsNoTracking()
            .Where(mi => mi.ModifierGroupId == groupId && mi.IsActive && mi.IsAvailable)
            .OrderBy(mi => mi.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ModifierItem?> GetModifierItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierItems
            .AsNoTracking()
            .Include(mi => mi.ModifierGroup)
            .Include(mi => mi.NestedGroups)
                .ThenInclude(ng => ng.NestedModifierGroup)
            .FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);
    }

    public async Task<ModifierItem> AddModifierItemAsync(int groupId, ModifierItemDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ModifierGroups.FindAsync(new object[] { groupId }, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier group with ID {groupId} not found.");

        var item = new ModifierItem
        {
            ModifierGroupId = groupId,
            Name = dto.Name.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            ShortCode = dto.ShortCode,
            Description = dto.Description,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            IsDefault = dto.IsDefault,
            MaxQuantity = dto.MaxQuantity,
            ColorCode = dto.ColorCode,
            ImagePath = dto.ImagePath,
            DisplayOrder = dto.DisplayOrder,
            IsAvailable = dto.IsAvailable,
            KOTText = dto.KOTText,
            TaxRate = dto.TaxRate,
            InventoryProductId = dto.InventoryProductId,
            InventoryDeductQuantity = dto.InventoryDeductQuantity,
            Calories = dto.Calories,
            Allergens = dto.Allergens,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.ModifierItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Add nested groups
        foreach (var nestedGroupId in dto.NestedGroupIds)
        {
            await LinkNestedGroupAsync(item.Id, nestedGroupId, false, 0, createdByUserId, cancellationToken);
        }

        return item;
    }

    public async Task<ModifierItem> UpdateModifierItemAsync(int id, ModifierItemDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var item = await _context.ModifierItems.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier item with ID {id} not found.");

        item.Name = dto.Name.Trim();
        item.DisplayName = dto.DisplayName?.Trim();
        item.ShortCode = dto.ShortCode;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.CostPrice = dto.CostPrice;
        item.IsDefault = dto.IsDefault;
        item.MaxQuantity = dto.MaxQuantity;
        item.ColorCode = dto.ColorCode;
        item.ImagePath = dto.ImagePath;
        item.DisplayOrder = dto.DisplayOrder;
        item.IsAvailable = dto.IsAvailable;
        item.KOTText = dto.KOTText;
        item.TaxRate = dto.TaxRate;
        item.InventoryProductId = dto.InventoryProductId;
        item.InventoryDeductQuantity = dto.InventoryDeductQuantity;
        item.Calories = dto.Calories;
        item.Allergens = dto.Allergens;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<bool> DeleteModifierItemAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var item = await _context.ModifierItems
            .Include(mi => mi.NestedGroups)
            .FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);

        if (item is null) return false;

        // Check if used in any orders
        var usedInOrders = await _context.OrderItemModifiers
            .AnyAsync(oim => oim.ModifierItemId == id, cancellationToken);

        if (usedInOrders)
        {
            item.IsActive = false;
            item.IsAvailable = false;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedByUserId = deletedByUserId;
        }
        else
        {
            _context.ModifierItems.Remove(item);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ModifierItem> SetModifierItemAvailabilityAsync(int id, bool isAvailable, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var item = await _context.ModifierItems.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier item with ID {id} not found.");

        item.IsAvailable = isAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task ReorderModifierItemsAsync(int groupId, List<int> itemIds, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ModifierItems
            .Where(mi => mi.ModifierGroupId == groupId && itemIds.Contains(mi.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < itemIds.Count; i++)
        {
            var item = items.FirstOrDefault(it => it.Id == itemIds[i]);
            if (item != null)
            {
                item.DisplayOrder = i;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedByUserId = modifiedByUserId;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Product Modifiers

    public async Task<IReadOnlyList<ModifierGroup>> GetProductModifierGroupsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var links = await _context.ProductModifierGroups
            .AsNoTracking()
            .Where(pmg => pmg.ProductId == productId)
            .Include(pmg => pmg.ModifierGroup)
                .ThenInclude(mg => mg!.Items.Where(i => i.IsActive && i.IsAvailable).OrderBy(i => i.DisplayOrder))
            .OrderBy(pmg => pmg.DisplayOrder)
            .ToListAsync(cancellationToken);

        return links.Select(l => l.ModifierGroup!).ToList();
    }

    public async Task<IReadOnlyList<ModifierGroup>> GetEffectiveProductModifierGroupsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
            return new List<ModifierGroup>();

        // Get product-specific modifiers
        var productModifiers = await _context.ProductModifierGroups
            .AsNoTracking()
            .Where(pmg => pmg.ProductId == productId)
            .Include(pmg => pmg.ModifierGroup)
                .ThenInclude(mg => mg!.Items.Where(i => i.IsActive && i.IsAvailable).OrderBy(i => i.DisplayOrder))
            .OrderBy(pmg => pmg.DisplayOrder)
            .ToListAsync(cancellationToken);

        var result = productModifiers.Select(pm => pm.ModifierGroup!).ToList();
        var usedGroupIds = result.Select(g => g.Id).ToHashSet();

        // Get category default modifiers if product has a category
        if (product.CategoryId.HasValue)
        {
            var categoryModifiers = await _context.CategoryModifierGroups
                .AsNoTracking()
                .Where(cmg => cmg.CategoryId == product.CategoryId.Value && cmg.InheritToProducts)
                .Include(cmg => cmg.ModifierGroup)
                    .ThenInclude(mg => mg!.Items.Where(i => i.IsActive && i.IsAvailable).OrderBy(i => i.DisplayOrder))
                .OrderBy(cmg => cmg.DisplayOrder)
                .ToListAsync(cancellationToken);

            // Add category modifiers that aren't already in product modifiers
            foreach (var cm in categoryModifiers)
            {
                if (!usedGroupIds.Contains(cm.ModifierGroupId))
                {
                    result.Add(cm.ModifierGroup!);
                    usedGroupIds.Add(cm.ModifierGroupId);
                }
            }
        }

        return result;
    }

    public async Task LinkModifierGroupsToProductAsync(int productId, List<ProductModifierGroupDto> groups, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        // Remove existing links
        var existingLinks = await _context.ProductModifierGroups
            .Where(pmg => pmg.ProductId == productId)
            .ToListAsync(cancellationToken);
        _context.ProductModifierGroups.RemoveRange(existingLinks);

        // Add new links
        foreach (var dto in groups)
        {
            await _context.ProductModifierGroups.AddAsync(new ProductModifierGroup
            {
                ProductId = productId,
                ModifierGroupId = dto.ModifierGroupId,
                IsRequired = dto.IsRequired,
                MinSelections = dto.MinSelections,
                MaxSelections = dto.MaxSelections,
                FreeSelections = dto.FreeSelections,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = modifiedByUserId
            }, cancellationToken);
        }

        product.HasModifiers = groups.Count > 0;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Linked {Count} modifier groups to product {ProductId}", groups.Count, productId);
    }

    public async Task RemoveModifierGroupFromProductAsync(int productId, int modifierGroupId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = await _context.ProductModifierGroups
            .FirstOrDefaultAsync(pmg => pmg.ProductId == productId && pmg.ModifierGroupId == modifierGroupId, cancellationToken);

        if (link != null)
        {
            _context.ProductModifierGroups.Remove(link);

            // Check if product still has modifiers
            var remainingCount = await _context.ProductModifierGroups
                .CountAsync(pmg => pmg.ProductId == productId && pmg.ModifierGroupId != modifierGroupId, cancellationToken);

            if (remainingCount == 0)
            {
                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product != null)
                {
                    product.HasModifiers = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateProductModifierGroupAsync(int productId, int modifierGroupId, ProductModifierGroupDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = await _context.ProductModifierGroups
            .FirstOrDefaultAsync(pmg => pmg.ProductId == productId && pmg.ModifierGroupId == modifierGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"Product-modifier group link not found.");

        link.IsRequired = dto.IsRequired;
        link.MinSelections = dto.MinSelections;
        link.MaxSelections = dto.MaxSelections;
        link.FreeSelections = dto.FreeSelections;
        link.DisplayOrder = dto.DisplayOrder;
        link.UpdatedAt = DateTime.UtcNow;
        link.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Category Modifiers

    public async Task<IReadOnlyList<ModifierGroup>> GetCategoryModifierGroupsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var links = await _context.CategoryModifierGroups
            .AsNoTracking()
            .Where(cmg => cmg.CategoryId == categoryId)
            .Include(cmg => cmg.ModifierGroup)
                .ThenInclude(mg => mg!.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(cmg => cmg.DisplayOrder)
            .ToListAsync(cancellationToken);

        return links.Select(l => l.ModifierGroup!).ToList();
    }

    public async Task LinkModifierGroupsToCategoryAsync(int categoryId, List<CategoryModifierGroupDto> groups, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FindAsync(new object[] { categoryId }, cancellationToken)
            ?? throw new InvalidOperationException($"Category with ID {categoryId} not found.");

        // Remove existing links
        var existingLinks = await _context.CategoryModifierGroups
            .Where(cmg => cmg.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        _context.CategoryModifierGroups.RemoveRange(existingLinks);

        // Add new links
        foreach (var dto in groups)
        {
            await _context.CategoryModifierGroups.AddAsync(new CategoryModifierGroup
            {
                CategoryId = categoryId,
                ModifierGroupId = dto.ModifierGroupId,
                IsRequired = dto.IsRequired,
                InheritToProducts = dto.InheritToProducts,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = modifiedByUserId
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Linked {Count} modifier groups to category {CategoryId}", groups.Count, categoryId);
    }

    public async Task RemoveModifierGroupFromCategoryAsync(int categoryId, int modifierGroupId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = await _context.CategoryModifierGroups
            .FirstOrDefaultAsync(cmg => cmg.CategoryId == categoryId && cmg.ModifierGroupId == modifierGroupId, cancellationToken);

        if (link != null)
        {
            _context.CategoryModifierGroups.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ApplyCategoryModifiersToProductsAsync(int categoryId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var categoryModifiers = await _context.CategoryModifierGroups
            .Where(cmg => cmg.CategoryId == categoryId && cmg.InheritToProducts)
            .ToListAsync(cancellationToken);

        var products = await _context.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            var existingProductModifiers = await _context.ProductModifierGroups
                .Where(pmg => pmg.ProductId == product.Id)
                .Select(pmg => pmg.ModifierGroupId)
                .ToHashSetAsync(cancellationToken);

            foreach (var cm in categoryModifiers)
            {
                if (!existingProductModifiers.Contains(cm.ModifierGroupId))
                {
                    await _context.ProductModifierGroups.AddAsync(new ProductModifierGroup
                    {
                        ProductId = product.Id,
                        ModifierGroupId = cm.ModifierGroupId,
                        IsRequired = cm.IsRequired,
                        DisplayOrder = cm.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = modifiedByUserId
                    }, cancellationToken);

                    product.HasModifiers = true;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Applied category {CategoryId} modifiers to {ProductCount} products", categoryId, products.Count);
    }

    #endregion

    #region Modifier Presets

    public async Task<IReadOnlyList<ModifierPreset>> GetProductPresetsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierPresets
            .AsNoTracking()
            .Where(mp => mp.ProductId == productId && mp.IsActive)
            .Include(mp => mp.PresetItems)
                .ThenInclude(pi => pi.ModifierItem)
            .OrderBy(mp => mp.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModifierPreset>> GetCategoryPresetsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.ModifierPresets
            .AsNoTracking()
            .Where(mp => mp.CategoryId == categoryId && mp.IsActive)
            .Include(mp => mp.PresetItems)
                .ThenInclude(pi => pi.ModifierItem)
            .OrderBy(mp => mp.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModifierPreset>> GetGlobalPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ModifierPresets
            .AsNoTracking()
            .Where(mp => mp.ProductId == null && mp.CategoryId == null && mp.IsActive)
            .Include(mp => mp.PresetItems)
                .ThenInclude(pi => pi.ModifierItem)
            .OrderBy(mp => mp.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ModifierPreset> CreatePresetAsync(ModifierPresetDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var preset = new ModifierPreset
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            ProductId = dto.ProductId,
            CategoryId = dto.CategoryId,
            ColorCode = dto.ColorCode,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        foreach (var item in dto.PresetItems)
        {
            preset.PresetItems.Add(new ModifierPresetItem
            {
                ModifierItemId = item.ModifierItemId,
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            });
        }

        await _context.ModifierPresets.AddAsync(preset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return preset;
    }

    public async Task<ModifierPreset> UpdatePresetAsync(int id, ModifierPresetDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var preset = await _context.ModifierPresets
            .Include(mp => mp.PresetItems)
            .FirstOrDefaultAsync(mp => mp.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier preset with ID {id} not found.");

        preset.Name = dto.Name.Trim();
        preset.Description = dto.Description;
        preset.ProductId = dto.ProductId;
        preset.CategoryId = dto.CategoryId;
        preset.ColorCode = dto.ColorCode;
        preset.DisplayOrder = dto.DisplayOrder;
        preset.UpdatedAt = DateTime.UtcNow;
        preset.UpdatedByUserId = modifiedByUserId;

        _context.ModifierPresetItems.RemoveRange(preset.PresetItems);

        foreach (var item in dto.PresetItems)
        {
            preset.PresetItems.Add(new ModifierPresetItem
            {
                ModifierPresetId = id,
                ModifierItemId = item.ModifierItemId,
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = modifiedByUserId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return preset;
    }

    public async Task<bool> DeletePresetAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var preset = await _context.ModifierPresets.FindAsync(new object[] { id }, cancellationToken);

        if (preset is null) return false;

        _context.ModifierPresets.Remove(preset);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<OrderItemModifierDto>> ApplyPresetAsync(int presetId, CancellationToken cancellationToken = default)
    {
        var preset = await _context.ModifierPresets
            .AsNoTracking()
            .Include(mp => mp.PresetItems)
            .FirstOrDefaultAsync(mp => mp.Id == presetId, cancellationToken)
            ?? throw new InvalidOperationException($"Modifier preset with ID {presetId} not found.");

        return preset.PresetItems.Select(pi => new OrderItemModifierDto
        {
            ModifierItemId = pi.ModifierItemId,
            Quantity = pi.Quantity
        }).ToList();
    }

    #endregion

    #region Order Item Modifiers

    public async Task<IReadOnlyList<OrderItemModifier>> AddModifiersToOrderItemAsync(int orderItemId, List<OrderItemModifierDto> modifiers, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var orderItem = await _context.OrderItems.FindAsync(new object[] { orderItemId }, cancellationToken)
            ?? throw new InvalidOperationException($"Order item with ID {orderItemId} not found.");

        var result = new List<OrderItemModifier>();

        foreach (var dto in modifiers)
        {
            var modifierItem = await _context.ModifierItems.FindAsync(new object[] { dto.ModifierItemId }, cancellationToken)
                ?? throw new InvalidOperationException($"Modifier item with ID {dto.ModifierItemId} not found.");

            var totalPrice = modifierItem.Price * dto.Quantity;
            var taxAmount = totalPrice * (modifierItem.TaxRate / 100);

            var orderItemModifier = new OrderItemModifier
            {
                OrderItemId = orderItemId,
                ModifierItemId = dto.ModifierItemId,
                Quantity = dto.Quantity,
                UnitPrice = modifierItem.Price,
                TotalPrice = totalPrice,
                TaxAmount = taxAmount,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _context.OrderItemModifiers.AddAsync(orderItemModifier, cancellationToken);
            result.Add(orderItemModifier);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<OrderItemModifier>> GetOrderItemModifiersAsync(int orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItemModifiers
            .AsNoTracking()
            .Where(oim => oim.OrderItemId == orderItemId)
            .Include(oim => oim.ModifierItem)
                .ThenInclude(mi => mi!.ModifierGroup)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateOrderItemModifiersAsync(int orderItemId, List<OrderItemModifierDto> modifiers, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        // Remove existing modifiers
        var existing = await _context.OrderItemModifiers
            .Where(oim => oim.OrderItemId == orderItemId)
            .ToListAsync(cancellationToken);
        _context.OrderItemModifiers.RemoveRange(existing);

        // Add new modifiers
        await AddModifiersToOrderItemAsync(orderItemId, modifiers, modifiedByUserId, cancellationToken);
    }

    public async Task ClearOrderItemModifiersAsync(int orderItemId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.OrderItemModifiers
            .Where(oim => oim.OrderItemId == orderItemId)
            .ToListAsync(cancellationToken);
        _context.OrderItemModifiers.RemoveRange(existing);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkModifiersPrintedAsync(int orderItemId, CancellationToken cancellationToken = default)
    {
        var modifiers = await _context.OrderItemModifiers
            .Where(oim => oim.OrderItemId == orderItemId)
            .ToListAsync(cancellationToken);

        foreach (var modifier in modifiers)
        {
            modifier.PrintedToKitchen = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Validation & Pricing

    public async Task<ModifierValidationResult> ValidateModifierSelectionsAsync(int productId, List<OrderItemModifierDto> selections, CancellationToken cancellationToken = default)
    {
        var result = new ModifierValidationResult { IsValid = true };

        var effectiveGroups = await GetEffectiveProductModifierGroupsAsync(productId, cancellationToken);
        var productModifierLinks = await _context.ProductModifierGroups
            .Where(pmg => pmg.ProductId == productId)
            .ToDictionaryAsync(pmg => pmg.ModifierGroupId, cancellationToken);

        // Group selections by modifier group
        var selectionsByGroup = new Dictionary<int, List<OrderItemModifierDto>>();
        foreach (var selection in selections)
        {
            var item = await _context.ModifierItems
                .AsNoTracking()
                .FirstOrDefaultAsync(mi => mi.Id == selection.ModifierItemId, cancellationToken);

            if (item != null)
            {
                if (!selectionsByGroup.ContainsKey(item.ModifierGroupId))
                {
                    selectionsByGroup[item.ModifierGroupId] = new List<OrderItemModifierDto>();
                }
                selectionsByGroup[item.ModifierGroupId].Add(selection);
            }
        }

        foreach (var group in effectiveGroups)
        {
            var isRequired = group.IsRequired;
            var minSelections = group.MinSelections;
            var maxSelections = group.MaxSelections;

            // Check for product-specific overrides
            if (productModifierLinks.TryGetValue(group.Id, out var link))
            {
                isRequired = link.IsRequired;
                if (link.MinSelections.HasValue) minSelections = link.MinSelections.Value;
                if (link.MaxSelections.HasValue) maxSelections = link.MaxSelections.Value;
            }

            var groupSelections = selectionsByGroup.GetValueOrDefault(group.Id, new List<OrderItemModifierDto>());
            var totalQuantity = groupSelections.Sum(s => s.Quantity);

            if (isRequired && totalQuantity < minSelections)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' requires at least {minSelections} selection(s).");
                result.MissingRequiredGroups.Add(group);
            }

            if (maxSelections > 0 && totalQuantity > maxSelections)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' allows maximum {maxSelections} selection(s).");
            }
        }

        if (result.IsValid)
        {
            var pricing = await CalculateModifierPricingAsync(productId, selections, cancellationToken);
            result.TotalModifierPrice = pricing.TotalPrice;
        }

        return result;
    }

    public async Task<ModifierPricing> CalculateModifierPricingAsync(int productId, List<OrderItemModifierDto> selections, CancellationToken cancellationToken = default)
    {
        var result = new ModifierPricing();

        // Get free selections per group from product or group defaults
        var productModifierLinks = await _context.ProductModifierGroups
            .Where(pmg => pmg.ProductId == productId)
            .ToDictionaryAsync(pmg => pmg.ModifierGroupId, cancellationToken);

        var freeSelectionsByGroup = new Dictionary<int, int>();

        foreach (var selection in selections)
        {
            var item = await _context.ModifierItems
                .Include(mi => mi.ModifierGroup)
                .FirstOrDefaultAsync(mi => mi.Id == selection.ModifierItemId, cancellationToken);

            if (item == null) continue;

            var groupId = item.ModifierGroupId;

            // Initialize free selections for this group
            if (!freeSelectionsByGroup.ContainsKey(groupId))
            {
                var freeCount = item.ModifierGroup!.FreeSelections;
                if (productModifierLinks.TryGetValue(groupId, out var link) && link.FreeSelections.HasValue)
                {
                    freeCount = link.FreeSelections.Value;
                }
                freeSelectionsByGroup[groupId] = freeCount;
            }

            var isFree = false;
            var quantity = selection.Quantity;
            var freeQuantity = 0;

            // Use free selections first
            if (freeSelectionsByGroup[groupId] > 0)
            {
                freeQuantity = Math.Min(quantity, freeSelectionsByGroup[groupId]);
                freeSelectionsByGroup[groupId] -= freeQuantity;
                if (freeQuantity == quantity)
                {
                    isFree = true;
                }
            }

            var chargeableQuantity = quantity - freeQuantity;
            var lineTotal = item.Price * chargeableQuantity;
            var taxAmount = lineTotal * (item.TaxRate / 100);

            var line = new ModifierPricingLine
            {
                ModifierItemId = item.Id,
                ModifierName = item.DisplayName ?? item.Name,
                Quantity = quantity,
                UnitPrice = item.Price,
                LineTotal = lineTotal,
                TaxAmount = taxAmount,
                IsFree = isFree
            };

            result.Lines.Add(line);
            result.TotalPrice += lineTotal;
            result.TotalTax += taxAmount;
            result.FreeItemsApplied += freeQuantity;
        }

        return result;
    }

    public async Task<decimal> GetOrderItemModifierTotalAsync(int orderItemId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItemModifiers
            .Where(oim => oim.OrderItemId == orderItemId)
            .SumAsync(oim => oim.TotalPrice, cancellationToken);
    }

    public async Task DeductModifierInventoryAsync(int orderItemId, CancellationToken cancellationToken = default)
    {
        var modifiers = await _context.OrderItemModifiers
            .Include(oim => oim.ModifierItem)
            .Where(oim => oim.OrderItemId == orderItemId)
            .ToListAsync(cancellationToken);

        foreach (var modifier in modifiers)
        {
            if (modifier.ModifierItem?.InventoryProductId.HasValue == true &&
                modifier.ModifierItem.InventoryDeductQuantity > 0)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == modifier.ModifierItem.InventoryProductId.Value, cancellationToken);

                if (inventory != null)
                {
                    var deductQuantity = modifier.ModifierItem.InventoryDeductQuantity * modifier.Quantity;
                    inventory.CurrentStock -= deductQuantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Nested Modifiers

    public async Task LinkNestedGroupAsync(int modifierItemId, int nestedGroupId, bool isRequired, int displayOrder, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = new ModifierItemNestedGroup
        {
            ModifierItemId = modifierItemId,
            NestedModifierGroupId = nestedGroupId,
            IsRequired = isRequired,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = modifiedByUserId
        };

        await _context.ModifierItemNestedGroups.AddAsync(link, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveNestedGroupAsync(int modifierItemId, int nestedGroupId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = await _context.ModifierItemNestedGroups
            .FirstOrDefaultAsync(ng => ng.ModifierItemId == modifierItemId && ng.NestedModifierGroupId == nestedGroupId, cancellationToken);

        if (link != null)
        {
            _context.ModifierItemNestedGroups.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ModifierGroup>> GetNestedGroupsAsync(int modifierItemId, CancellationToken cancellationToken = default)
    {
        var links = await _context.ModifierItemNestedGroups
            .AsNoTracking()
            .Where(ng => ng.ModifierItemId == modifierItemId)
            .Include(ng => ng.NestedModifierGroup)
                .ThenInclude(mg => mg!.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(ng => ng.DisplayOrder)
            .ToListAsync(cancellationToken);

        return links.Select(l => l.NestedModifierGroup!).ToList();
    }

    #endregion

    #region Reporting

    public async Task<Dictionary<int, int>> GetModifierUsageStatsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItemModifiers
            .Where(oim => oim.CreatedAt >= startDate && oim.CreatedAt <= endDate)
            .GroupBy(oim => oim.ModifierItemId)
            .Select(g => new { ModifierItemId = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ModifierItemId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<int, decimal>> GetModifierRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItemModifiers
            .Where(oim => oim.CreatedAt >= startDate && oim.CreatedAt <= endDate)
            .GroupBy(oim => oim.ModifierItemId)
            .Select(g => new { ModifierItemId = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .ToDictionaryAsync(x => x.ModifierItemId, x => x.Revenue, cancellationToken);
    }

    public async Task<ModifierStatistics> GetModifierStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return new ModifierStatistics
        {
            TotalGroups = await _context.ModifierGroups.CountAsync(cancellationToken),
            ActiveGroups = await _context.ModifierGroups.CountAsync(mg => mg.IsActive, cancellationToken),
            TotalItems = await _context.ModifierItems.CountAsync(cancellationToken),
            AvailableItems = await _context.ModifierItems.CountAsync(mi => mi.IsActive && mi.IsAvailable, cancellationToken),
            ProductsWithModifiers = await _context.Products.CountAsync(p => p.HasModifiers, cancellationToken)
        };
    }

    #endregion
}
