using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public class ProductService : IProductService
{
    private readonly POSDbContext _context;
    private readonly IImageService _imageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="imageService">The image service.</param>
    /// <param name="logger">The logger.</param>
    public ProductService(POSDbContext context, IImageService imageService, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .OrderBy(p => p.Category!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .OrderBy(p => p.Category!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Code == code.Trim(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Barcode == barcode.Trim(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Product> CreateProductAsync(CreateProductDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        // Validate code uniqueness
        if (!await IsCodeUniqueAsync(dto.Code, null, cancellationToken))
        {
            throw new InvalidOperationException($"A product with code '{dto.Code}' already exists.");
        }

        // Validate barcode uniqueness if provided
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && !await IsBarcodeUniqueAsync(dto.Barcode, null, cancellationToken))
        {
            throw new InvalidOperationException($"A product with barcode '{dto.Barcode}' already exists.");
        }

        // Validate category exists
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        if (!categoryExists)
        {
            throw new InvalidOperationException($"Category with ID {dto.CategoryId} not found.");
        }

        // Validate price
        if (dto.SellingPrice <= 0)
        {
            throw new InvalidOperationException("Selling price must be greater than zero.");
        }

        // Validate stock levels
        if (dto.MinStockLevel.HasValue && dto.MaxStockLevel.HasValue && dto.MinStockLevel >= dto.MaxStockLevel)
        {
            throw new InvalidOperationException("Minimum stock level must be less than maximum stock level.");
        }

        // Validate tax rate bounds
        if (dto.TaxRate < 0 || dto.TaxRate > 100)
        {
            throw new InvalidOperationException("Tax rate must be between 0 and 100.");
        }

        // Save image if provided
        string? imagePath = null;
        if (!string.IsNullOrWhiteSpace(dto.ImagePath))
        {
            imagePath = await _imageService.SaveProductImageAsync(dto.ImagePath, dto.Code.Trim().ToUpperInvariant());
        }

        var product = new Product
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CategoryId = dto.CategoryId,
            SellingPrice = dto.SellingPrice,
            CostPrice = dto.CostPrice,
            TaxRate = dto.TaxRate,
            UnitOfMeasure = dto.UnitOfMeasure.Trim(),
            ImagePath = imagePath,
            Barcode = dto.Barcode?.Trim(),
            MinStockLevel = dto.MinStockLevel,
            MaxStockLevel = dto.MaxStockLevel,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create inventory record if initial stock is provided
        var inventory = new Inventory
        {
            ProductId = product.Id,
            CurrentStock = dto.InitialStock,
            ReservedStock = 0,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Inventories.AddAsync(inventory, cancellationToken).ConfigureAwait(false);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "ProductCreated",
            EntityType = nameof(Product),
            EntityId = product.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                product.Id,
                product.Code,
                product.Name,
                product.CategoryId,
                product.SellingPrice,
                product.CostPrice,
                product.TaxRate,
                product.IsActive,
                InitialStock = dto.InitialStock
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Product '{ProductName}' (Code: {ProductCode}) created by user {UserId}",
            product.Name, product.Code, createdByUserId);

        return product;
    }

    /// <inheritdoc />
    public async Task<Product> UpdateProductAsync(int id, UpdateProductDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {id} not found.");
        }

        // Validate code uniqueness
        if (!await IsCodeUniqueAsync(dto.Code, id, cancellationToken))
        {
            throw new InvalidOperationException($"A product with code '{dto.Code}' already exists.");
        }

        // Validate barcode uniqueness if provided
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && !await IsBarcodeUniqueAsync(dto.Barcode, id, cancellationToken))
        {
            throw new InvalidOperationException($"A product with barcode '{dto.Barcode}' already exists.");
        }

        // Validate category exists
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        if (!categoryExists)
        {
            throw new InvalidOperationException($"Category with ID {dto.CategoryId} not found.");
        }

        // Validate price
        if (dto.SellingPrice <= 0)
        {
            throw new InvalidOperationException("Selling price must be greater than zero.");
        }

        // Validate stock levels
        if (dto.MinStockLevel.HasValue && dto.MaxStockLevel.HasValue && dto.MinStockLevel >= dto.MaxStockLevel)
        {
            throw new InvalidOperationException("Minimum stock level must be less than maximum stock level.");
        }

        // Validate tax rate bounds
        if (dto.TaxRate < 0 || dto.TaxRate > 100)
        {
            throw new InvalidOperationException("Tax rate must be between 0 and 100.");
        }

        var oldValues = new
        {
            product.Code,
            product.Name,
            product.CategoryId,
            product.SellingPrice,
            product.CostPrice,
            product.TaxRate,
            product.IsActive
        };

        // Handle image update
        string? newImagePath = product.ImagePath; // Keep existing by default
        var oldProductCode = product.Code; // Store old code for image cleanup
        var newProductCode = dto.Code.Trim().ToUpperInvariant();
        var codeChanged = !string.Equals(oldProductCode, newProductCode, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(dto.ImagePath))
        {
            // Check if this is a new image file (not already in our images folder)
            var imagesBasePath = _imageService.ImagesBasePath;
            var isNewImage = !dto.ImagePath.StartsWith(imagesBasePath, StringComparison.OrdinalIgnoreCase);

            if (isNewImage)
            {
                // Delete old image with old code first (handles code change scenario)
                if (!string.IsNullOrWhiteSpace(oldProductCode))
                {
                    await _imageService.DeleteProductImageAsync(oldProductCode);
                }

                // Save the new image with new code
                newImagePath = await _imageService.SaveProductImageAsync(dto.ImagePath, newProductCode);
            }
            else if (codeChanged)
            {
                // Code changed but keeping same image - need to rename/move the file
                // For simplicity, delete old and copy to new location
                if (!string.IsNullOrWhiteSpace(oldProductCode))
                {
                    await _imageService.DeleteProductImageAsync(oldProductCode);
                }
                // Re-save with new code (copies from current location)
                newImagePath = await _imageService.SaveProductImageAsync(dto.ImagePath, newProductCode);
            }
            else
            {
                // Keep existing image path
                newImagePath = dto.ImagePath;
            }
        }
        else if (string.IsNullOrEmpty(dto.ImagePath) && !string.IsNullOrWhiteSpace(product.ImagePath))
        {
            // Image was cleared (null or empty string) - delete the old image
            await _imageService.DeleteProductImageAsync(oldProductCode);
            newImagePath = null;
        }

        product.Code = dto.Code.Trim().ToUpperInvariant();
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.CategoryId = dto.CategoryId;
        product.SellingPrice = dto.SellingPrice;
        product.CostPrice = dto.CostPrice;
        product.TaxRate = dto.TaxRate;
        product.UnitOfMeasure = dto.UnitOfMeasure.Trim();
        product.ImagePath = newImagePath;
        product.Barcode = dto.Barcode?.Trim();
        product.MinStockLevel = dto.MinStockLevel;
        product.MaxStockLevel = dto.MaxStockLevel;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedByUserId = modifiedByUserId;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "ProductUpdated",
            EntityType = nameof(Product),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                product.Code,
                product.Name,
                product.CategoryId,
                product.SellingPrice,
                product.CostPrice,
                product.TaxRate,
                product.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Product '{ProductName}' (ID: {ProductId}) updated by user {UserId}",
            product.Name, id, modifiedByUserId);

        return product;
    }

    /// <inheritdoc />
    public async Task<Product> SetProductActiveAsync(int id, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {id} not found.");
        }

        if (product.IsActive == isActive)
        {
            return product;
        }

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedByUserId = modifiedByUserId;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = isActive ? "ProductActivated" : "ProductDeactivated",
            EntityType = nameof(Product),
            EntityId = id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { product.Name, product.Code, IsActive = isActive }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Product '{ProductName}' (ID: {ProductId}) {Action} by user {UserId}",
            product.Name, id, isActive ? "activated" : "deactivated", modifiedByUserId);

        return product;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return false;
        }

        // Check if product has any order items
        var hasOrderItems = await _context.OrderItems
            .AnyAsync(oi => oi.ProductId == id, cancellationToken)
            .ConfigureAwait(false);

        if (hasOrderItems)
        {
            throw new InvalidOperationException("Cannot delete a product that has been used in orders. Consider deactivating it instead.");
        }

        // Create audit log before deletion
        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "ProductDeleted",
            EntityType = nameof(Product),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                product.Code,
                product.Name,
                product.CategoryId,
                product.SellingPrice
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        // Delete inventory record first
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == id, cancellationToken)
            .ConfigureAwait(false);

        if (inventory is not null)
        {
            _context.Inventories.Remove(inventory);
        }

        // Delete product image if exists
        if (!string.IsNullOrWhiteSpace(product.Code))
        {
            await _imageService.DeleteProductImageAsync(product.Code);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Product '{ProductName}' (ID: {ProductId}) deleted by user {UserId}",
            product.Name, id, deletedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var trimmedCode = code.Trim().ToUpperInvariant();

        var query = _context.Products
            .Where(p => EF.Functions.Collate(p.Code, "Latin1_General_CI_AS") == EF.Functions.Collate(trimmedCode, "Latin1_General_CI_AS"));

        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return true; // Empty barcode is always "unique"

        var trimmedBarcode = barcode.Trim();

        var query = _context.Products
            .Where(p => p.Barcode != null && EF.Functions.Collate(p.Barcode, "Latin1_General_CI_AS") == EF.Functions.Collate(trimmedBarcode, "Latin1_General_CI_AS"));

        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, int? categoryId = null, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return activeOnly
                ? await GetActiveProductsAsync(cancellationToken)
                : await GetAllProductsAsync(cancellationToken);
        }

        // Escape special LIKE characters and prepare search term
        var escapedTerm = searchTerm.Trim()
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");

        var query = _context.Products.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Search in name, code, or barcode using case-insensitive collation
        query = query.Where(p =>
            EF.Functions.Like(EF.Functions.Collate(p.Name, "Latin1_General_CI_AS"), $"%{escapedTerm}%") ||
            EF.Functions.Like(EF.Functions.Collate(p.Code, "Latin1_General_CI_AS"), $"%{escapedTerm}%") ||
            (p.Barcode != null && EF.Functions.Like(EF.Functions.Collate(p.Barcode, "Latin1_General_CI_AS"), $"%{escapedTerm}%")));

        return await query
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .OrderBy(p => p.Name)
            .Take(100) // Limit results for performance
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Where(p => p.MinStockLevel.HasValue)
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.Inventory != null && p.Inventory.CurrentStock < p.MinStockLevel)
            .OrderBy(p => p.Inventory!.CurrentStock)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetProductCountByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .CountAsync(p => p.CategoryId == categoryId, cancellationToken)
            .ConfigureAwait(false);
    }
}
