namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO representing a store in the multi-store system.
/// </summary>
public class StoreDto
{
    public int Id { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public bool IsHeadquarters { get; set; }
    public bool ReceivesCentralUpdates { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new store.
/// </summary>
public class CreateStoreDto
{
    public string StoreCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public bool IsHeadquarters { get; set; }
    public bool ReceivesCentralUpdates { get; set; } = true;
    public string? ManagerName { get; set; }
}

/// <summary>
/// DTO for a store product override.
/// </summary>
public class StoreProductOverrideDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CentralPrice { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal EffectivePrice => OverridePrice ?? CentralPrice;
    public decimal? OverrideCost { get; set; }
    public bool IsAvailable { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? LastSyncTime { get; set; }
}

/// <summary>
/// DTO for creating/updating a store product override.
/// </summary>
public class CreateStoreProductOverrideDto
{
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal? OverrideCost { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal? OverrideMinStock { get; set; }
    public decimal? OverrideMaxStock { get; set; }
    public decimal? OverrideTaxRate { get; set; }
    public string? OverrideReason { get; set; }
}

/// <summary>
/// DTO for pushing products to stores.
/// </summary>
public class PushProductsDto
{
    public List<int> ProductIds { get; set; } = new();
    public List<int> StoreIds { get; set; } = new();
    public bool OverwriteExistingOverrides { get; set; }
}

/// <summary>
/// Result of a product push operation.
/// </summary>
public class ProductPushResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProductsPushed { get; set; }
    public int StoresUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime PushTime { get; set; }

    public static ProductPushResult Success(int productCount, int storeCount) => new()
    {
        IsSuccess = true,
        ProductsPushed = productCount,
        StoresUpdated = storeCount,
        PushTime = DateTime.UtcNow
    };

    public static ProductPushResult Failure(string errorMessage, List<string>? errors = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        Errors = errors ?? new(),
        PushTime = DateTime.UtcNow
    };
}

/// <summary>
/// DTO representing effective product pricing for a store.
/// </summary>
public class StoreProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal CentralPrice { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal EffectivePrice => OverridePrice ?? CentralPrice;
    public decimal? CentralCost { get; set; }
    public decimal? OverrideCost { get; set; }
    public decimal? EffectiveCost => OverrideCost ?? CentralCost;
    public bool IsAvailable { get; set; }
    public bool HasOverride { get; set; }
    public bool IsCentralProduct { get; set; }
    public DateTime? LastSyncTime { get; set; }
}

/// <summary>
/// Sync status for a store in multi-store context.
/// </summary>
public class MultiStoreStoreSyncStatusDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public DateTime? LastSyncTime { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsWithOverrides { get; set; }
    public int PendingSyncProducts { get; set; }
    public bool IsOnline { get; set; }
    public string SyncStatus => PendingSyncProducts > 0 ? "Pending" : "Synced";
}

// ================== Pricing Zone DTOs ==================

/// <summary>
/// DTO representing a pricing zone.
/// </summary>
public class PricingZoneDto
{
    public int Id { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = "KES";
    public decimal? DefaultTaxRate { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public int StoreCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating/updating a pricing zone.
/// </summary>
public class CreatePricingZoneDto
{
    public string ZoneCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = "KES";
    public decimal? DefaultTaxRate { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public List<int>? StoreIds { get; set; }
}

/// <summary>
/// DTO representing a zone price for a product.
/// </summary>
public class ZonePriceDto
{
    public int Id { get; set; }
    public int PricingZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CentralPrice { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? MinimumPrice { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }
    public bool IsCurrentlyEffective { get; set; }
    public decimal PriceDifference => Price - CentralPrice;
    public decimal PriceDifferencePercent => CentralPrice != 0 ? Math.Round((Price - CentralPrice) / CentralPrice * 100, 2) : 0;
}

/// <summary>
/// DTO for creating/updating a zone price.
/// </summary>
public class CreateZonePriceDto
{
    public int PricingZoneId { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? MinimumPrice { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for bulk zone price updates.
/// </summary>
public class BulkZonePriceDto
{
    public int PricingZoneId { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public decimal? PriceAdjustmentPercent { get; set; }
    public decimal? PriceAdjustmentAmount { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}

/// <summary>
/// DTO representing a scheduled price change.
/// </summary>
public class ScheduledPriceChangeDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? PricingZoneId { get; set; }
    public string? ZoneName { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal? NewCostPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public decimal PriceChange => NewPrice - OldPrice;
    public decimal PriceChangePercent => OldPrice != 0 ? Math.Round((NewPrice - OldPrice) / OldPrice * 100, 2) : 0;
    public string Scope => StoreId.HasValue ? "Store" : (PricingZoneId.HasValue ? "Zone" : "Global");
}

/// <summary>
/// DTO for creating a scheduled price change.
/// </summary>
public class CreateScheduledPriceChangeDto
{
    public int ProductId { get; set; }
    public int? PricingZoneId { get; set; }
    public int? StoreId { get; set; }
    public decimal NewPrice { get; set; }
    public decimal? NewCostPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for applying scheduled price changes.
/// </summary>
public class PriceChangeApplicationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalChanges { get; set; }
    public int AppliedChanges { get; set; }
    public int FailedChanges { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; }

    public static PriceChangeApplicationResult Success(int total, int applied) => new()
    {
        IsSuccess = true,
        TotalChanges = total,
        AppliedChanges = applied,
        ProcessedAt = DateTime.UtcNow
    };

    public static PriceChangeApplicationResult Failure(string errorMessage, List<string>? errors = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        Errors = errors ?? new(),
        ProcessedAt = DateTime.UtcNow
    };
}

/// <summary>
/// DTO for product pricing summary across zones.
/// </summary>
public class ProductPricingSummaryDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CentralPrice { get; set; }
    public decimal? CentralCost { get; set; }
    public List<ZonePriceDto> ZonePrices { get; set; } = new();
    public List<ScheduledPriceChangeDto> ScheduledChanges { get; set; } = new();
    public decimal MinPrice => ZonePrices.Any() ? Math.Min(CentralPrice, ZonePrices.Min(z => z.Price)) : CentralPrice;
    public decimal MaxPrice => ZonePrices.Any() ? Math.Max(CentralPrice, ZonePrices.Max(z => z.Price)) : CentralPrice;
    public int ZoneCount => ZonePrices.Count;
    public int PendingChanges => ScheduledChanges.Count(c => c.Status == "Scheduled");
}
