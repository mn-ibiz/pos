using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing product variants and barcodes.
/// </summary>
public class ProductVariantService : IProductVariantService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    public ProductVariantService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Variant Options

    public async Task<IReadOnlyList<VariantOption>> GetAllVariantOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VariantOptions
            .AsNoTracking()
            .Include(vo => vo.Values.OrderBy(v => v.DisplayOrder))
            .OrderBy(vo => vo.DisplayOrder)
            .ThenBy(vo => vo.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VariantOption>> GetGlobalVariantOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VariantOptions
            .AsNoTracking()
            .Where(vo => vo.IsGlobal && vo.IsActive)
            .Include(vo => vo.Values.Where(v => v.IsActive).OrderBy(v => v.DisplayOrder))
            .OrderBy(vo => vo.DisplayOrder)
            .ThenBy(vo => vo.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<VariantOption?> GetVariantOptionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.VariantOptions
            .AsNoTracking()
            .Include(vo => vo.Values.OrderBy(v => v.DisplayOrder))
            .FirstOrDefaultAsync(vo => vo.Id == id, cancellationToken);
    }

    public async Task<VariantOption> CreateVariantOptionAsync(VariantOptionDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var option = new VariantOption
        {
            Name = dto.Name.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            OptionType = dto.OptionType,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsGlobal = dto.IsGlobal,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        // Add values
        var order = 0;
        foreach (var valueDto in dto.Values)
        {
            option.Values.Add(new VariantOptionValue
            {
                Value = valueDto.Value.Trim(),
                DisplayName = valueDto.DisplayName?.Trim(),
                ColorCode = valueDto.ColorCode,
                ImagePath = valueDto.ImagePath,
                PriceAdjustment = valueDto.PriceAdjustment,
                IsPriceAdjustmentPercent = valueDto.IsPriceAdjustmentPercent,
                DisplayOrder = valueDto.DisplayOrder > 0 ? valueDto.DisplayOrder : order++,
                SkuSuffix = valueDto.SkuSuffix,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            });
        }

        await _context.VariantOptions.AddAsync(option, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Variant option '{OptionName}' created with {ValueCount} values by user {UserId}",
            option.Name, option.Values.Count, createdByUserId);

        return option;
    }

    public async Task<VariantOption> UpdateVariantOptionAsync(int id, VariantOptionDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var option = await _context.VariantOptions
            .Include(vo => vo.Values)
            .FirstOrDefaultAsync(vo => vo.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Variant option with ID {id} not found.");

        option.Name = dto.Name.Trim();
        option.DisplayName = dto.DisplayName?.Trim();
        option.OptionType = dto.OptionType;
        option.Description = dto.Description;
        option.DisplayOrder = dto.DisplayOrder;
        option.IsGlobal = dto.IsGlobal;
        option.UpdatedAt = DateTime.UtcNow;
        option.UpdatedByUserId = modifiedByUserId;

        // Update existing values and add new ones
        var existingValueIds = option.Values.Select(v => v.Id).ToHashSet();
        var incomingValueIds = dto.Values.Where(v => v.Id > 0).Select(v => v.Id).ToHashSet();

        // Remove values not in incoming
        var valuesToRemove = option.Values.Where(v => !incomingValueIds.Contains(v.Id)).ToList();
        foreach (var value in valuesToRemove)
        {
            _context.VariantOptionValues.Remove(value);
        }

        // Update or add values
        foreach (var valueDto in dto.Values)
        {
            if (valueDto.Id > 0 && existingValueIds.Contains(valueDto.Id))
            {
                var existing = option.Values.First(v => v.Id == valueDto.Id);
                existing.Value = valueDto.Value.Trim();
                existing.DisplayName = valueDto.DisplayName?.Trim();
                existing.ColorCode = valueDto.ColorCode;
                existing.ImagePath = valueDto.ImagePath;
                existing.PriceAdjustment = valueDto.PriceAdjustment;
                existing.IsPriceAdjustmentPercent = valueDto.IsPriceAdjustmentPercent;
                existing.DisplayOrder = valueDto.DisplayOrder;
                existing.SkuSuffix = valueDto.SkuSuffix;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = modifiedByUserId;
            }
            else
            {
                option.Values.Add(new VariantOptionValue
                {
                    Value = valueDto.Value.Trim(),
                    DisplayName = valueDto.DisplayName?.Trim(),
                    ColorCode = valueDto.ColorCode,
                    ImagePath = valueDto.ImagePath,
                    PriceAdjustment = valueDto.PriceAdjustment,
                    IsPriceAdjustmentPercent = valueDto.IsPriceAdjustmentPercent,
                    DisplayOrder = valueDto.DisplayOrder,
                    SkuSuffix = valueDto.SkuSuffix,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = modifiedByUserId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Variant option '{OptionName}' (ID: {OptionId}) updated by user {UserId}",
            option.Name, id, modifiedByUserId);

        return option;
    }

    public async Task<bool> DeleteVariantOptionAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var option = await _context.VariantOptions
            .Include(vo => vo.ProductVariantOptions)
            .FirstOrDefaultAsync(vo => vo.Id == id, cancellationToken);

        if (option is null) return false;

        if (option.ProductVariantOptions.Any())
        {
            throw new InvalidOperationException("Cannot delete variant option that is linked to products.");
        }

        _context.VariantOptions.Remove(option);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Variant option '{OptionName}' (ID: {OptionId}) deleted by user {UserId}",
            option.Name, id, deletedByUserId);

        return true;
    }

    public async Task<VariantOptionValue> AddVariantOptionValueAsync(int optionId, VariantOptionValueDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var option = await _context.VariantOptions.FindAsync(new object[] { optionId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variant option with ID {optionId} not found.");

        var value = new VariantOptionValue
        {
            VariantOptionId = optionId,
            Value = dto.Value.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            ColorCode = dto.ColorCode,
            ImagePath = dto.ImagePath,
            PriceAdjustment = dto.PriceAdjustment,
            IsPriceAdjustmentPercent = dto.IsPriceAdjustmentPercent,
            DisplayOrder = dto.DisplayOrder,
            SkuSuffix = dto.SkuSuffix,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.VariantOptionValues.AddAsync(value, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return value;
    }

    public async Task<VariantOptionValue> UpdateVariantOptionValueAsync(int valueId, VariantOptionValueDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var value = await _context.VariantOptionValues.FindAsync(new object[] { valueId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variant option value with ID {valueId} not found.");

        value.Value = dto.Value.Trim();
        value.DisplayName = dto.DisplayName?.Trim();
        value.ColorCode = dto.ColorCode;
        value.ImagePath = dto.ImagePath;
        value.PriceAdjustment = dto.PriceAdjustment;
        value.IsPriceAdjustmentPercent = dto.IsPriceAdjustmentPercent;
        value.DisplayOrder = dto.DisplayOrder;
        value.SkuSuffix = dto.SkuSuffix;
        value.UpdatedAt = DateTime.UtcNow;
        value.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return value;
    }

    public async Task<bool> DeleteVariantOptionValueAsync(int valueId, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var value = await _context.VariantOptionValues
            .Include(v => v.ProductVariantValues)
            .FirstOrDefaultAsync(v => v.Id == valueId, cancellationToken);

        if (value is null) return false;

        if (value.ProductVariantValues.Any())
        {
            throw new InvalidOperationException("Cannot delete variant value that is used by product variants.");
        }

        _context.VariantOptionValues.Remove(value);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Product Variants

    public async Task<IReadOnlyList<ProductVariant>> GetProductVariantsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Where(pv => pv.ProductId == productId)
            .Include(pv => pv.VariantValues)
                .ThenInclude(vv => vv.VariantOptionValue)
                    .ThenInclude(vov => vov!.VariantOption)
            .OrderBy(pv => pv.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetAvailableProductVariantsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Where(pv => pv.ProductId == productId && pv.IsAvailable && pv.IsActive)
            .Include(pv => pv.VariantValues)
                .ThenInclude(vv => vv.VariantOptionValue)
            .OrderBy(pv => pv.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Include(pv => pv.Product)
            .Include(pv => pv.VariantValues)
                .ThenInclude(vv => vv.VariantOptionValue)
                    .ThenInclude(vov => vov!.VariantOption)
            .FirstOrDefaultAsync(pv => pv.Id == variantId, cancellationToken);
    }

    public async Task<ProductVariant?> GetVariantBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Include(pv => pv.Product)
            .FirstOrDefaultAsync(pv => pv.SKU == sku, cancellationToken);
    }

    public async Task<ProductVariant?> GetVariantByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // First check direct barcode
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(pv => pv.Product)
            .FirstOrDefaultAsync(pv => pv.Barcode == barcode, cancellationToken);

        if (variant != null) return variant;

        // Check in ProductBarcodes table
        var productBarcode = await _context.ProductBarcodes
            .AsNoTracking()
            .Include(pb => pb.ProductVariant)
                .ThenInclude(pv => pv!.Product)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode && pb.ProductVariantId != null, cancellationToken);

        return productBarcode?.ProductVariant;
    }

    public async Task<ProductVariant> CreateVariantAsync(int productId, ProductVariantDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        if (!await IsSkuUniqueAsync(dto.SKU, null, cancellationToken))
        {
            throw new InvalidOperationException($"SKU '{dto.SKU}' is already in use.");
        }

        if (!string.IsNullOrEmpty(dto.Barcode) && !await IsBarcodeUniqueAsync(dto.Barcode, null, cancellationToken))
        {
            throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already in use.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            SKU = dto.SKU.Trim(),
            Barcode = dto.Barcode?.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            SellingPrice = dto.SellingPrice,
            CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity,
            LowStockThreshold = dto.LowStockThreshold,
            ReorderLevel = dto.ReorderLevel,
            Weight = dto.Weight,
            WeightUnit = dto.WeightUnit,
            Dimensions = dto.Dimensions,
            ImagePath = dto.ImagePath,
            IsAvailable = dto.IsAvailable,
            TrackInventory = dto.TrackInventory,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        // Add variant values
        foreach (var valueId in dto.VariantOptionValueIds)
        {
            variant.VariantValues.Add(new ProductVariantValue
            {
                VariantOptionValueId = valueId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            });
        }

        await _context.ProductVariants.AddAsync(variant, cancellationToken);

        // Update product to indicate it has variants
        product.HasVariants = true;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Product variant '{SKU}' created for product {ProductId} by user {UserId}",
            variant.SKU, productId, createdByUserId);

        return variant;
    }

    public async Task<ProductVariant> UpdateVariantAsync(int variantId, ProductVariantDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants
            .Include(pv => pv.VariantValues)
            .FirstOrDefaultAsync(pv => pv.Id == variantId, cancellationToken)
            ?? throw new InvalidOperationException($"Variant with ID {variantId} not found.");

        if (!await IsSkuUniqueAsync(dto.SKU, variantId, cancellationToken))
        {
            throw new InvalidOperationException($"SKU '{dto.SKU}' is already in use.");
        }

        variant.SKU = dto.SKU.Trim();
        variant.Barcode = dto.Barcode?.Trim();
        variant.DisplayName = dto.DisplayName?.Trim();
        variant.SellingPrice = dto.SellingPrice;
        variant.CostPrice = dto.CostPrice;
        variant.StockQuantity = dto.StockQuantity;
        variant.LowStockThreshold = dto.LowStockThreshold;
        variant.ReorderLevel = dto.ReorderLevel;
        variant.Weight = dto.Weight;
        variant.WeightUnit = dto.WeightUnit;
        variant.Dimensions = dto.Dimensions;
        variant.ImagePath = dto.ImagePath;
        variant.IsAvailable = dto.IsAvailable;
        variant.TrackInventory = dto.TrackInventory;
        variant.UpdatedAt = DateTime.UtcNow;
        variant.UpdatedByUserId = modifiedByUserId;

        // Update variant values
        _context.ProductVariantValues.RemoveRange(variant.VariantValues);
        foreach (var valueId in dto.VariantOptionValueIds)
        {
            variant.VariantValues.Add(new ProductVariantValue
            {
                ProductVariantId = variantId,
                VariantOptionValueId = valueId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = modifiedByUserId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return variant;
    }

    public async Task<bool> DeleteVariantAsync(int variantId, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants
            .Include(pv => pv.OrderItems)
            .FirstOrDefaultAsync(pv => pv.Id == variantId, cancellationToken);

        if (variant is null) return false;

        if (variant.OrderItems.Any())
        {
            // Soft delete - just mark as inactive
            variant.IsActive = false;
            variant.IsAvailable = false;
            variant.UpdatedAt = DateTime.UtcNow;
            variant.UpdatedByUserId = deletedByUserId;
        }
        else
        {
            _context.ProductVariants.Remove(variant);
        }

        // Check if product still has variants
        var remainingVariants = await _context.ProductVariants
            .CountAsync(pv => pv.ProductId == variant.ProductId && pv.Id != variantId && pv.IsActive, cancellationToken);

        if (remainingVariants == 0)
        {
            var product = await _context.Products.FindAsync(new object[] { variant.ProductId }, cancellationToken);
            if (product != null)
            {
                product.HasVariants = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Product variant {VariantId} deleted by user {UserId}", variantId, deletedByUserId);

        return true;
    }

    public async Task<ProductVariant> SetVariantAvailabilityAsync(int variantId, bool isAvailable, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants.FindAsync(new object[] { variantId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variant with ID {variantId} not found.");

        variant.IsAvailable = isAvailable;
        variant.UpdatedAt = DateTime.UtcNow;
        variant.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return variant;
    }

    public async Task<ProductVariant> UpdateVariantStockAsync(int variantId, int quantity, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants.FindAsync(new object[] { variantId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variant with ID {variantId} not found.");

        variant.StockQuantity = quantity;
        variant.UpdatedAt = DateTime.UtcNow;
        variant.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return variant;
    }

    public async Task<ProductVariant> AdjustVariantStockAsync(int variantId, int delta, string reason, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants.FindAsync(new object[] { variantId }, cancellationToken)
            ?? throw new InvalidOperationException($"Variant with ID {variantId} not found.");

        var oldStock = variant.StockQuantity;
        variant.StockQuantity += delta;
        variant.UpdatedAt = DateTime.UtcNow;
        variant.UpdatedByUserId = modifiedByUserId;

        // Create audit log for stock adjustment
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "VariantStockAdjusted",
            EntityType = nameof(ProductVariant),
            EntityId = variantId,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = oldStock }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = variant.StockQuantity, Delta = delta, Reason = reason }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Variant {VariantId} stock adjusted by {Delta} (was {OldStock}, now {NewStock}). Reason: {Reason}",
            variantId, delta, oldStock, variant.StockQuantity, reason);

        return variant;
    }

    #endregion

    #region Product Variant Configuration

    public async Task<IReadOnlyList<ProductVariantOption>> GetProductVariantOptionsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariantOptions
            .AsNoTracking()
            .Where(pvo => pvo.ProductId == productId)
            .Include(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo!.Values.OrderBy(v => v.DisplayOrder))
            .OrderBy(pvo => pvo.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task LinkVariantOptionsToProductAsync(int productId, List<ProductVariantOptionDto> options, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        // Remove existing links
        var existingLinks = await _context.ProductVariantOptions
            .Where(pvo => pvo.ProductId == productId)
            .ToListAsync(cancellationToken);
        _context.ProductVariantOptions.RemoveRange(existingLinks);

        // Add new links
        foreach (var dto in options)
        {
            await _context.ProductVariantOptions.AddAsync(new ProductVariantOption
            {
                ProductId = productId,
                VariantOptionId = dto.VariantOptionId,
                IsRequired = dto.IsRequired,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = modifiedByUserId
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Linked {Count} variant options to product {ProductId}", options.Count, productId);
    }

    public async Task RemoveVariantOptionFromProductAsync(int productId, int variantOptionId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var link = await _context.ProductVariantOptions
            .FirstOrDefaultAsync(pvo => pvo.ProductId == productId && pvo.VariantOptionId == variantOptionId, cancellationToken);

        if (link != null)
        {
            _context.ProductVariantOptions.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<VariantGenerationResult> GenerateVariantsAsync(VariantGenerationDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var result = new VariantGenerationResult();

        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {dto.ProductId} not found.");

        // Get all option values for selected options
        var optionValuesByOption = new List<List<VariantOptionValue>>();
        foreach (var optionDto in dto.VariantOptions)
        {
            var values = await _context.VariantOptionValues
                .Where(v => optionDto.SelectedValueIds.Contains(v.Id))
                .ToListAsync(cancellationToken);
            optionValuesByOption.Add(values);
        }

        // Generate all combinations
        var combinations = GenerateCombinations(optionValuesByOption);

        // Get existing variants to avoid duplicates
        var existingSkus = await _context.ProductVariants
            .Where(pv => pv.ProductId == dto.ProductId)
            .Select(pv => pv.SKU)
            .ToHashSetAsync(cancellationToken);

        var variantIndex = existingSkus.Count + 1;

        foreach (var combination in combinations)
        {
            var skuSuffix = string.Join("-", combination.Select(v => v.SkuSuffix ?? v.Value));
            var sku = $"{dto.SkuPrefix}-{skuSuffix}".ToUpperInvariant().Replace(" ", "-");

            if (existingSkus.Contains(sku))
            {
                result.SkippedDuplicates++;
                continue;
            }

            // Calculate price adjustments
            var priceAdjustment = 0m;
            foreach (var value in combination)
            {
                if (value.IsPriceAdjustmentPercent && dto.DefaultPrice.HasValue)
                {
                    priceAdjustment += dto.DefaultPrice.Value * (value.PriceAdjustment / 100);
                }
                else
                {
                    priceAdjustment += value.PriceAdjustment;
                }
            }

            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                SKU = sku,
                DisplayName = $"{product.Name} - {string.Join(", ", combination.Select(v => v.DisplayName ?? v.Value))}",
                SellingPrice = dto.DefaultPrice.HasValue ? dto.DefaultPrice.Value + priceAdjustment : null,
                TrackInventory = dto.TrackInventory,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            foreach (var value in combination)
            {
                variant.VariantValues.Add(new ProductVariantValue
                {
                    VariantOptionValueId = value.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = createdByUserId
                });
            }

            if (dto.GenerateBarcodes)
            {
                variant.Barcode = await GenerateBarcodeAsync("EAN13", cancellationToken);
            }

            await _context.ProductVariants.AddAsync(variant, cancellationToken);
            result.GeneratedVariants.Add(variant);
            existingSkus.Add(sku);
            variantIndex++;
        }

        if (result.GeneratedVariants.Count > 0)
        {
            product.HasVariants = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        result.TotalGenerated = result.GeneratedVariants.Count;

        _logger.Information("Generated {Count} variants for product {ProductId}. Skipped {Skipped} duplicates.",
            result.TotalGenerated, dto.ProductId, result.SkippedDuplicates);

        return result;
    }

    public async Task<List<List<VariantOptionValue>>> GetVariantMatrixAsync(int productId, CancellationToken cancellationToken = default)
    {
        var productOptions = await _context.ProductVariantOptions
            .Where(pvo => pvo.ProductId == productId)
            .Include(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo!.Values.Where(v => v.IsActive).OrderBy(v => v.DisplayOrder))
            .OrderBy(pvo => pvo.DisplayOrder)
            .ToListAsync(cancellationToken);

        var valuesByOption = productOptions
            .Select(pvo => pvo.VariantOption!.Values.ToList())
            .ToList();

        return GenerateCombinations(valuesByOption);
    }

    private static List<List<VariantOptionValue>> GenerateCombinations(List<List<VariantOptionValue>> valuesByOption)
    {
        var result = new List<List<VariantOptionValue>>();

        if (valuesByOption.Count == 0)
            return result;

        GenerateCombinationsRecursive(valuesByOption, 0, new List<VariantOptionValue>(), result);

        return result;
    }

    private static void GenerateCombinationsRecursive(
        List<List<VariantOptionValue>> valuesByOption,
        int optionIndex,
        List<VariantOptionValue> current,
        List<List<VariantOptionValue>> result)
    {
        if (optionIndex == valuesByOption.Count)
        {
            result.Add(new List<VariantOptionValue>(current));
            return;
        }

        foreach (var value in valuesByOption[optionIndex])
        {
            current.Add(value);
            GenerateCombinationsRecursive(valuesByOption, optionIndex + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    #endregion

    #region Product Barcodes

    public async Task<IReadOnlyList<ProductBarcode>> GetProductBarcodesAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductBarcodes
            .AsNoTracking()
            .Where(pb => pb.ProductId == productId)
            .Include(pb => pb.ProductVariant)
            .OrderByDescending(pb => pb.IsPrimary)
            .ThenBy(pb => pb.Barcode)
            .ToListAsync(cancellationToken);
    }

    public async Task<(Product? Product, ProductVariant? Variant)> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // Check ProductBarcodes table first
        var productBarcode = await _context.ProductBarcodes
            .AsNoTracking()
            .Include(pb => pb.Product)
            .Include(pb => pb.ProductVariant)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode, cancellationToken);

        if (productBarcode != null)
        {
            return (productBarcode.Product, productBarcode.ProductVariant);
        }

        // Check variant barcodes
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(pv => pv.Product)
            .FirstOrDefaultAsync(pv => pv.Barcode == barcode, cancellationToken);

        if (variant != null)
        {
            return (variant.Product, variant);
        }

        // Check product barcodes
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);

        return (product, null);
    }

    public async Task<ProductBarcode> AddBarcodeAsync(int productId, ProductBarcodeDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsBarcodeUniqueAsync(dto.Barcode, null, cancellationToken))
        {
            throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already in use.");
        }

        // If this is the primary barcode, unset any existing primary
        if (dto.IsPrimary)
        {
            var existingPrimary = await _context.ProductBarcodes
                .Where(pb => pb.ProductId == productId && pb.ProductVariantId == dto.ProductVariantId && pb.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingPrimary)
            {
                existing.IsPrimary = false;
            }
        }

        var barcode = new ProductBarcode
        {
            ProductId = productId,
            ProductVariantId = dto.ProductVariantId,
            Barcode = dto.Barcode.Trim(),
            BarcodeType = dto.BarcodeType,
            IsPrimary = dto.IsPrimary,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.ProductBarcodes.AddAsync(barcode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return barcode;
    }

    public async Task<ProductBarcode> UpdateBarcodeAsync(int barcodeId, ProductBarcodeDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var barcode = await _context.ProductBarcodes.FindAsync(new object[] { barcodeId }, cancellationToken)
            ?? throw new InvalidOperationException($"Barcode with ID {barcodeId} not found.");

        if (barcode.Barcode != dto.Barcode && !await IsBarcodeUniqueAsync(dto.Barcode, barcodeId, cancellationToken))
        {
            throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already in use.");
        }

        barcode.Barcode = dto.Barcode.Trim();
        barcode.BarcodeType = dto.BarcodeType;
        barcode.Description = dto.Description;
        barcode.UpdatedAt = DateTime.UtcNow;
        barcode.UpdatedByUserId = modifiedByUserId;

        if (dto.IsPrimary && !barcode.IsPrimary)
        {
            await SetPrimaryBarcodeAsync(barcodeId, modifiedByUserId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return barcode;
    }

    public async Task<bool> DeleteBarcodeAsync(int barcodeId, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var barcode = await _context.ProductBarcodes.FindAsync(new object[] { barcodeId }, cancellationToken);

        if (barcode is null) return false;

        _context.ProductBarcodes.Remove(barcode);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task SetPrimaryBarcodeAsync(int barcodeId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var barcode = await _context.ProductBarcodes.FindAsync(new object[] { barcodeId }, cancellationToken)
            ?? throw new InvalidOperationException($"Barcode with ID {barcodeId} not found.");

        // Unset existing primary for this product/variant
        var existingPrimary = await _context.ProductBarcodes
            .Where(pb => pb.ProductId == barcode.ProductId &&
                         pb.ProductVariantId == barcode.ProductVariantId &&
                         pb.IsPrimary &&
                         pb.Id != barcodeId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingPrimary)
        {
            existing.IsPrimary = false;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = modifiedByUserId;
        }

        barcode.IsPrimary = true;
        barcode.UpdatedAt = DateTime.UtcNow;
        barcode.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public bool ValidateBarcodeFormat(string barcode, string barcodeType)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;

        return barcodeType.ToUpperInvariant() switch
        {
            "EAN13" => barcode.Length == 13 && barcode.All(char.IsDigit) && ValidateEan13Checksum(barcode),
            "EAN8" => barcode.Length == 8 && barcode.All(char.IsDigit),
            "UPC" => barcode.Length == 12 && barcode.All(char.IsDigit),
            "CODE128" => barcode.Length >= 1 && barcode.Length <= 80,
            "CODE39" => barcode.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%".Contains(c)),
            "QR" => barcode.Length >= 1 && barcode.Length <= 4296,
            _ => true // Unknown types pass validation
        };
    }

    private static bool ValidateEan13Checksum(string barcode)
    {
        if (barcode.Length != 13) return false;

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = barcode[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return (barcode[12] - '0') == checkDigit;
    }

    public async Task<string> GenerateBarcodeAsync(string barcodeType = "EAN13", CancellationToken cancellationToken = default)
    {
        var random = new Random();
        string barcode;

        do
        {
            if (barcodeType == "EAN13")
            {
                // Generate 12 random digits
                var digits = new char[12];
                for (var i = 0; i < 12; i++)
                {
                    digits[i] = (char)('0' + random.Next(10));
                }

                // Calculate check digit
                var sum = 0;
                for (var i = 0; i < 12; i++)
                {
                    var digit = digits[i] - '0';
                    sum += i % 2 == 0 ? digit : digit * 3;
                }
                var checkDigit = (10 - (sum % 10)) % 10;

                barcode = new string(digits) + checkDigit;
            }
            else
            {
                // Generic random barcode
                barcode = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            }
        }
        while (!await IsBarcodeUniqueAsync(barcode, null, cancellationToken));

        return barcode;
    }

    #endregion

    #region Validation

    public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeVariantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductVariants.Where(pv => pv.SKU == sku);

        if (excludeVariantId.HasValue)
        {
            query = query.Where(pv => pv.Id != excludeVariantId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeBarcodeId = null, CancellationToken cancellationToken = default)
    {
        // Check ProductBarcodes
        var barcodeQuery = _context.ProductBarcodes.Where(pb => pb.Barcode == barcode);
        if (excludeBarcodeId.HasValue)
        {
            barcodeQuery = barcodeQuery.Where(pb => pb.Id != excludeBarcodeId.Value);
        }
        if (await barcodeQuery.AnyAsync(cancellationToken))
            return false;

        // Check ProductVariants
        if (await _context.ProductVariants.AnyAsync(pv => pv.Barcode == barcode, cancellationToken))
            return false;

        // Check Products
        if (await _context.Products.AnyAsync(p => p.Barcode == barcode, cancellationToken))
            return false;

        return true;
    }

    public async Task<bool> ValidateVariantOptionsAsync(int productId, List<int> optionValueIds, CancellationToken cancellationToken = default)
    {
        var productOptions = await _context.ProductVariantOptions
            .Where(pvo => pvo.ProductId == productId)
            .Include(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo!.Values)
            .ToListAsync(cancellationToken);

        // Check each required option has a value selected
        foreach (var option in productOptions.Where(po => po.IsRequired))
        {
            var validValueIds = option.VariantOption!.Values.Select(v => v.Id).ToHashSet();
            if (!optionValueIds.Any(id => validValueIds.Contains(id)))
            {
                return false;
            }
        }

        // Check all selected values belong to valid options
        var allValidValueIds = productOptions
            .SelectMany(po => po.VariantOption!.Values.Select(v => v.Id))
            .ToHashSet();

        return optionValueIds.All(id => allValidValueIds.Contains(id));
    }

    #endregion

    #region Statistics

    public async Task<VariantStatistics> GetVariantStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return new VariantStatistics
        {
            TotalVariantOptions = await _context.VariantOptions.CountAsync(cancellationToken),
            ActiveVariantOptions = await _context.VariantOptions.CountAsync(vo => vo.IsActive, cancellationToken),
            TotalVariantValues = await _context.VariantOptionValues.CountAsync(cancellationToken),
            ProductsWithVariants = await _context.Products.CountAsync(p => p.HasVariants, cancellationToken),
            TotalProductVariants = await _context.ProductVariants.CountAsync(cancellationToken)
        };
    }

    public async Task<int> GetVariantOptionUsageCountAsync(int variantOptionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariantOptions
            .CountAsync(pvo => pvo.VariantOptionId == variantOptionId, cancellationToken);
    }

    #endregion
}
