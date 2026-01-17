using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO representing a central promotion.
/// </summary>
public class CentralPromotionDto
{
    public int Id { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType Type { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? OfferPrice { get; set; }
    public decimal? MinimumPurchase { get; set; }
    public int MinQuantity { get; set; }
    public int? MaxQuantityPerTransaction { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PromotionStatus Status { get; set; }
    public bool RequiresCouponCode { get; set; }
    public string? CouponCode { get; set; }
    public int Priority { get; set; }
    public bool IsCentrallyManaged { get; set; }
    public int ProductCount { get; set; }
    public int CategoryCount { get; set; }
    public int DeploymentCount { get; set; }
    public int TotalRedemptions { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public bool IsCurrentlyActive { get; set; }
}

/// <summary>
/// DTO for creating/updating a central promotion.
/// </summary>
public class CreatePromotionDto
{
    public string PromotionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalNotes { get; set; }
    public PromotionType Type { get; set; } = PromotionType.PercentageDiscount;
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? OfferPrice { get; set; }
    public decimal? MinimumPurchase { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int? MaxQuantityPerTransaction { get; set; }
    public int? MaxTotalRedemptions { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ValidDaysOfWeek { get; set; }
    public TimeSpan? ValidFromTime { get; set; }
    public TimeSpan? ValidToTime { get; set; }
    public bool RequiresCouponCode { get; set; }
    public string? CouponCode { get; set; }
    public bool IsCombinableWithOtherPromotions { get; set; } = true;
    public int Priority { get; set; } = 100;
    public List<int>? ProductIds { get; set; }
    public List<int>? CategoryIds { get; set; }
}

/// <summary>
/// DTO for deployment request.
/// </summary>
public class DeployPromotionDto
{
    public int PromotionId { get; set; }
    public DeploymentScope Scope { get; set; }
    public List<int>? ZoneIds { get; set; }
    public List<int>? StoreIds { get; set; }
    public bool OverwriteExisting { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO representing a promotion deployment.
/// </summary>
public class PromotionDeploymentDto
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public DeploymentScope Scope { get; set; }
    public string ScopeName => Scope.ToString();
    public DateTime DeployedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DeploymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int StoresDeployedCount { get; set; }
    public int StoresFailedCount { get; set; }
    public int TotalStores { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }
    public string? DeployedByUserName { get; set; }
    public List<DeploymentStoreDto> StoreDetails { get; set; } = new();
}

/// <summary>
/// DTO for deployment store status.
/// </summary>
public class DeploymentStoreDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? SyncedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// DTO representing a promotion redemption.
/// </summary>
public class PromotionRedemptionDto
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal DiscountGiven { get; set; }
    public decimal FinalAmount { get; set; }
    public int QuantityApplied { get; set; }
    public DateTime RedeemedAt { get; set; }
    public string? CouponCodeUsed { get; set; }
    public string? CustomerName { get; set; }
    public bool IsVoided { get; set; }
}

/// <summary>
/// DTO for recording a promotion redemption.
/// </summary>
public class RecordRedemptionDto
{
    public int PromotionId { get; set; }
    public int StoreId { get; set; }
    public int ReceiptId { get; set; }
    public int? ReceiptItemId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountGiven { get; set; }
    public int QuantityApplied { get; set; } = 1;
    public string? CouponCodeUsed { get; set; }
    public int? LoyaltyMemberId { get; set; }
}

/// <summary>
/// DTO for redemption summary by store.
/// </summary>
public class StoreRedemptionSummaryDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public int RedemptionCount { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal TotalOriginalAmount { get; set; }
    public decimal TotalFinalAmount { get; set; }
    public int TotalQuantity { get; set; }
    public DateTime? LastRedemption { get; set; }
}

/// <summary>
/// DTO for promotion dashboard summary.
/// </summary>
public class PromotionDashboardDto
{
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public PromotionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalStoresDeployed { get; set; }
    public int TotalRedemptions { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal AverageDiscountPerTransaction { get; set; }
    public List<StoreRedemptionSummaryDto> StoreBreakdown { get; set; } = new();
}

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeploymentId { get; set; }
    public int StoresDeployed { get; set; }
    public int StoresFailed { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime DeployedAt { get; set; }

    public static DeploymentResult Success(int deploymentId, int storesDeployed) => new()
    {
        IsSuccess = true,
        DeploymentId = deploymentId,
        StoresDeployed = storesDeployed,
        DeployedAt = DateTime.UtcNow
    };

    public static DeploymentResult Failure(string errorMessage, List<string>? errors = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        Errors = errors ?? new(),
        DeployedAt = DateTime.UtcNow
    };
}

/// <summary>
/// DTO for querying promotions with filters.
/// </summary>
public class PromotionQueryDto
{
    public PromotionStatus? Status { get; set; }
    public PromotionType? Type { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public int? StoreId { get; set; }
    public int? ZoneId { get; set; }
}

/// <summary>
/// DTO for active promotion at a store.
/// </summary>
public class StoreActivePromotionDto
{
    public int PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType Type { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? OfferPrice { get; set; }
    public int MinQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool RequiresCouponCode { get; set; }
    public List<int> ApplicableProductIds { get; set; } = new();
    public List<int> ApplicableCategoryIds { get; set; } = new();
}
