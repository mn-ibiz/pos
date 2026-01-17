namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents the type of pricing for an offer.
/// </summary>
public enum OfferPricingType
{
    /// <summary>Fixed price offer.</summary>
    FixedPrice = 1,
    /// <summary>Percentage discount offer.</summary>
    PercentageDiscount = 2
}

/// <summary>
/// Represents the status of a promotional offer.
/// </summary>
public enum OfferStatus
{
    /// <summary>Offer is upcoming (start date in future).</summary>
    Upcoming = 1,
    /// <summary>Offer is currently active.</summary>
    Active = 2,
    /// <summary>Offer has expired (end date has passed).</summary>
    Expired = 3,
    /// <summary>Offer is manually deactivated.</summary>
    Inactive = 4
}

/// <summary>
/// Represents a promotional offer for a product (Supermarket feature).
/// </summary>
public class ProductOffer : BaseEntity
{
    public int ProductId { get; set; }
    public string OfferName { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Type of pricing: Fixed price or percentage discount.
    /// </summary>
    public OfferPricingType PricingType { get; set; } = OfferPricingType.FixedPrice;

    /// <summary>
    /// The offer price (used when PricingType is FixedPrice).
    /// </summary>
    public decimal OfferPrice { get; set; }

    /// <summary>
    /// The discount percentage (used when PricingType is PercentageDiscount).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Start date of the offer.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the offer.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Minimum quantity required to qualify for the offer.
    /// </summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity that can receive the offer price (null = unlimited).
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// ID of the user who created the offer.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets whether the offer is currently active based on dates and status.
    /// </summary>
    public bool IsCurrentlyActive => IsActive
        && DateTime.Now >= StartDate
        && DateTime.Now <= EndDate;

    /// <summary>
    /// Gets the current status of the offer.
    /// </summary>
    public OfferStatus Status
    {
        get
        {
            if (!IsActive) return OfferStatus.Inactive;
            var now = DateTime.Now;
            if (now < StartDate) return OfferStatus.Upcoming;
            if (now > EndDate) return OfferStatus.Expired;
            return OfferStatus.Active;
        }
    }

    /// <summary>
    /// Calculates the effective price for a given original price.
    /// </summary>
    public decimal CalculateOfferPrice(decimal originalPrice)
    {
        if (PricingType == OfferPricingType.PercentageDiscount && DiscountPercent.HasValue)
        {
            return originalPrice * (1 - DiscountPercent.Value / 100);
        }
        return OfferPrice;
    }

    /// <summary>
    /// Calculates the savings amount for a given original price.
    /// </summary>
    public decimal CalculateSavings(decimal originalPrice)
    {
        return originalPrice - CalculateOfferPrice(originalPrice);
    }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
}
