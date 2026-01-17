using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing product categories.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public CategoryService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null);

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories.OrderBy(sc => sc.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetCategoryTreeAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        // Get all categories at once for efficiency
        var query = _context.Categories.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var allCategories = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Build tree in memory
        var categoryDict = allCategories.ToDictionary(c => c.Id);
        var rootCategories = new List<Category>();

        foreach (var category in allCategories)
        {
            if (category.ParentCategoryId.HasValue && categoryDict.TryGetValue(category.ParentCategoryId.Value, out var parent))
            {
                parent.SubCategories.Add(category);
                category.ParentCategory = parent;
            }
            else if (!category.ParentCategoryId.HasValue)
            {
                rootCategories.Add(category);
            }
        }

        return rootCategories;
    }

    /// <inheritdoc />
    public async Task<Category> CreateCategoryAsync(CategoryDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        // Validate name uniqueness
        if (!await IsNameUniqueAsync(dto.Name, dto.ParentCategoryId, null, cancellationToken))
        {
            throw new InvalidOperationException($"A category named '{dto.Name}' already exists in this location.");
        }

        // Validate parent exists if specified
        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.ParentCategoryId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new InvalidOperationException($"Parent category with ID {dto.ParentCategoryId} not found.");
            }
        }

        var category = new Category
        {
            Name = dto.Name.Trim(),
            ParentCategoryId = dto.ParentCategoryId,
            ImagePath = dto.ImagePath,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create audit log after save so we have the EntityId
        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "CategoryCreated",
            EntityType = nameof(Category),
            EntityId = category.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                category.Id,
                category.Name,
                category.ParentCategoryId,
                category.DisplayOrder,
                category.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Category '{CategoryName}' created by user {UserId}", category.Name, createdByUserId);

        return category;
    }

    /// <inheritdoc />
    public async Task<Category> UpdateCategoryAsync(int id, CategoryDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            throw new InvalidOperationException($"Category with ID {id} not found.");
        }

        // Validate name uniqueness
        if (!await IsNameUniqueAsync(dto.Name, dto.ParentCategoryId, id, cancellationToken))
        {
            throw new InvalidOperationException($"A category named '{dto.Name}' already exists in this location.");
        }

        // Validate parent (cannot be self or descendant)
        if (dto.ParentCategoryId.HasValue)
        {
            if (dto.ParentCategoryId == id)
            {
                throw new InvalidOperationException("A category cannot be its own parent.");
            }

            if (await IsDescendantOfAsync(dto.ParentCategoryId.Value, id, cancellationToken))
            {
                throw new InvalidOperationException("Cannot set parent to a descendant category.");
            }
        }

        var oldValues = new
        {
            category.Name,
            category.ParentCategoryId,
            category.DisplayOrder,
            category.IsActive
        };

        category.Name = dto.Name.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;
        category.ImagePath = dto.ImagePath;
        category.DisplayOrder = dto.DisplayOrder;
        category.IsActive = dto.IsActive;
        category.ModifiedAt = DateTime.UtcNow;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "CategoryUpdated",
            EntityType = nameof(Category),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                category.Name,
                category.ParentCategoryId,
                category.DisplayOrder,
                category.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Category '{CategoryName}' (ID: {CategoryId}) updated by user {UserId}", category.Name, id, modifiedByUserId);

        return category;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            return false;
        }

        // Check for products
        if (await HasProductsAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a category that has products. Remove products first or move them to another category.");
        }

        // Check for subcategories
        if (await HasSubcategoriesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a category that has subcategories. Remove subcategories first.");
        }

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "CategoryDeleted",
            EntityType = nameof(Category),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                category.Name,
                category.ParentCategoryId,
                category.DisplayOrder
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Category '{CategoryName}' (ID: {CategoryId}) deleted by user {UserId}", category.Name, id, deletedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<Category> SetCategoryActiveAsync(int id, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            throw new InvalidOperationException($"Category with ID {id} not found.");
        }

        if (category.IsActive == isActive)
        {
            return category;
        }

        category.IsActive = isActive;
        category.ModifiedAt = DateTime.UtcNow;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = isActive ? "CategoryActivated" : "CategoryDeactivated",
            EntityType = nameof(Category),
            EntityId = id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { category.Name, IsActive = isActive }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Category '{CategoryName}' (ID: {CategoryId}) {Action} by user {UserId}",
            category.Name, id, isActive ? "activated" : "deactivated", modifiedByUserId);

        return category;
    }

    /// <inheritdoc />
    public async Task ReorderCategoriesAsync(IEnumerable<CategoryOrderDto> orderings, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderings);

        var orderingsList = orderings.ToList();
        if (orderingsList.Count == 0)
        {
            return;
        }

        var categoryIds = orderingsList.Select(o => o.CategoryId).ToList();
        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changes = new List<object>();

        foreach (var ordering in orderingsList)
        {
            var category = categories.FirstOrDefault(c => c.Id == ordering.CategoryId);
            if (category is null)
            {
                continue;
            }

            if (category.DisplayOrder != ordering.DisplayOrder || category.ParentCategoryId != ordering.ParentCategoryId)
            {
                changes.Add(new
                {
                    ordering.CategoryId,
                    OldOrder = category.DisplayOrder,
                    NewOrder = ordering.DisplayOrder,
                    OldParent = category.ParentCategoryId,
                    NewParent = ordering.ParentCategoryId
                });

                category.DisplayOrder = ordering.DisplayOrder;
                category.ParentCategoryId = ordering.ParentCategoryId;
                category.ModifiedAt = DateTime.UtcNow;
            }
        }

        if (changes.Count > 0)
        {
            var auditLog = new AuditLog
            {
                UserId = modifiedByUserId,
                Action = "CategoriesReordered",
                EntityType = nameof(Category),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Changes = changes }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.Information("Categories reordered by user {UserId}. Changes: {ChangeCount}", modifiedByUserId, changes.Count);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.CategoryId == categoryId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasSubcategoriesAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsNameUniqueAsync(string name, int? parentCategoryId, int? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();

        // Use EF.Functions.Collate for case-insensitive comparison on SQL Server
        // This is more reliable than ToLower() which may have collation issues
        var query = _context.Categories
            .Where(c => EF.Functions.Collate(c.Name, "Latin1_General_CI_AS") == EF.Functions.Collate(trimmedName, "Latin1_General_CI_AS"))
            .Where(c => c.ParentCategoryId == parentCategoryId);

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the target category is a descendant of the source category.
    /// Optimized to fetch all parent relationships in a single query.
    /// </summary>
    private async Task<bool> IsDescendantOfAsync(int targetId, int sourceId, CancellationToken cancellationToken)
    {
        // Fetch all categories with their parent relationships in a single query
        var categoryParents = await _context.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentCategoryId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentCategoryId, cancellationToken)
            .ConfigureAwait(false);

        var visited = new HashSet<int>();
        var current = targetId;

        while (current != 0)
        {
            if (visited.Contains(current))
            {
                // Circular reference - treat as invalid
                return true;
            }

            visited.Add(current);

            if (!categoryParents.TryGetValue(current, out var parentId))
            {
                break;
            }

            if (parentId == sourceId)
            {
                return true;
            }

            current = parentId ?? 0;
        }

        return false;
    }
}
