using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing product offers and promotions.
/// </summary>
public class OfferService : IOfferService
{
    private readonly POSDbContext _context;
    private readonly ILogger<OfferService> _logger;

    public OfferService(POSDbContext context, ILogger<OfferService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetAllOffersAsync(OfferStatus? status = null)
    {
        var query = _context.ProductOffers
            .Include(o => o.Product)
            .Include(o => o.CreatedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            var now = DateTime.Now;
            query = status.Value switch
            {
                OfferStatus.Active => query.Where(o => o.IsActive && o.StartDate <= now && o.EndDate >= now),
                OfferStatus.Upcoming => query.Where(o => o.IsActive && o.StartDate > now),
                OfferStatus.Expired => query.Where(o => o.IsActive && o.EndDate < now),
                OfferStatus.Inactive => query.Where(o => !o.IsActive),
                _ => query
            };
        }

        return await query
            .OrderByDescending(o => o.StartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductOffer?> GetOfferByIdAsync(int id)
    {
        return await _context.ProductOffers
            .Include(o => o.Product)
            .Include(o => o.CreatedByUser)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetActiveOffersForProductAsync(int productId)
    {
        var now = DateTime.Now;
        return await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.ProductId == productId
                && o.IsActive
                && o.StartDate <= now
                && o.EndDate >= now)
            .OrderBy(o => o.MinQuantity)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductOffer?> GetBestOfferForProductAsync(int productId, int quantity = 1)
    {
        var now = DateTime.Now;
        var activeOffers = await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.ProductId == productId
                && o.IsActive
                && o.StartDate <= now
                && o.EndDate >= now
                && o.MinQuantity <= quantity
                && (!o.MaxQuantity.HasValue || o.MaxQuantity.Value >= quantity))
            .ToListAsync();

        if (!activeOffers.Any())
            return null;

        // Find the offer that gives the best price
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return null;

        ProductOffer? bestOffer = null;
        decimal bestPrice = product.SellingPrice;

        foreach (var offer in activeOffers)
        {
            var offerPrice = offer.CalculateOfferPrice(product.SellingPrice);
            if (offerPrice < bestPrice)
            {
                bestPrice = offerPrice;
                bestOffer = offer;
            }
        }

        return bestOffer;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetOffersForProductAsync(int productId)
    {
        return await _context.ProductOffers
            .Include(o => o.Product)
            .Include(o => o.CreatedByUser)
            .Where(o => o.ProductId == productId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductOffer> CreateOfferAsync(ProductOffer offer)
    {
        var validation = await ValidateOfferAsync(offer);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        }

        offer.CreatedAt = DateTime.UtcNow;
        _context.ProductOffers.Add(offer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created offer {OfferName} for product {ProductId}", offer.OfferName, offer.ProductId);

        return offer;
    }

    /// <inheritdoc />
    public async Task<ProductOffer> UpdateOfferAsync(ProductOffer offer)
    {
        var existing = await _context.ProductOffers.FindAsync(offer.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Offer with ID {offer.Id} not found.");
        }

        var validation = await ValidateOfferAsync(offer);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        }

        existing.OfferName = offer.OfferName;
        existing.Description = offer.Description;
        existing.PricingType = offer.PricingType;
        existing.OfferPrice = offer.OfferPrice;
        existing.DiscountPercent = offer.DiscountPercent;
        existing.StartDate = offer.StartDate;
        existing.EndDate = offer.EndDate;
        existing.MinQuantity = offer.MinQuantity;
        existing.MaxQuantity = offer.MaxQuantity;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated offer {OfferId}: {OfferName}", offer.Id, offer.OfferName);

        return existing;
    }

    /// <inheritdoc />
    public async Task DeactivateOfferAsync(int id)
    {
        var offer = await _context.ProductOffers.FindAsync(id);
        if (offer == null)
        {
            throw new InvalidOperationException($"Offer with ID {id} not found.");
        }

        offer.IsActive = false;
        offer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated offer {OfferId}", id);
    }

    /// <inheritdoc />
    public async Task DeleteOfferAsync(int id)
    {
        var offer = await _context.ProductOffers.FindAsync(id);
        if (offer == null)
        {
            throw new InvalidOperationException($"Offer with ID {id} not found.");
        }

        _context.ProductOffers.Remove(offer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted offer {OfferId}", id);
    }

    /// <inheritdoc />
    public async Task<OfferValidationResult> ValidateOfferAsync(ProductOffer offer)
    {
        var result = new OfferValidationResult { IsValid = true };

        // Check dates
        if (offer.EndDate <= offer.StartDate)
        {
            result.IsValid = false;
            result.Errors.Add("End date must be after start date.");
        }

        // Check product exists
        var product = await _context.Products.FindAsync(offer.ProductId);
        if (product == null)
        {
            result.IsValid = false;
            result.Errors.Add("Product not found.");
            return result;
        }

        // Check offer price
        if (offer.PricingType == OfferPricingType.FixedPrice)
        {
            if (offer.OfferPrice <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Offer price must be greater than zero.");
            }
            else if (offer.OfferPrice >= product.SellingPrice)
            {
                result.Warnings.Add("Offer price is not less than the selling price.");
            }
        }
        else if (offer.PricingType == OfferPricingType.PercentageDiscount)
        {
            if (!offer.DiscountPercent.HasValue || offer.DiscountPercent.Value <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Discount percentage must be greater than zero.");
            }
            else if (offer.DiscountPercent.Value >= 100)
            {
                result.IsValid = false;
                result.Errors.Add("Discount percentage cannot be 100% or more.");
            }
        }

        // Check for overlapping offers
        var overlapping = await _context.ProductOffers
            .Where(o => o.ProductId == offer.ProductId
                && o.Id != offer.Id
                && o.IsActive
                && o.StartDate <= offer.EndDate
                && o.EndDate >= offer.StartDate)
            .AnyAsync();

        if (overlapping)
        {
            result.Warnings.Add("There is an overlapping offer for this product. The best offer will be applied automatically.");
        }

        // Check min/max quantity
        if (offer.MaxQuantity.HasValue && offer.MaxQuantity.Value < offer.MinQuantity)
        {
            result.IsValid = false;
            result.Errors.Add("Maximum quantity cannot be less than minimum quantity.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetUpcomingOffersAsync(int days = 7)
    {
        var now = DateTime.Now;
        var futureDate = now.AddDays(days);

        return await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.IsActive
                && o.StartDate > now
                && o.StartDate <= futureDate)
            .OrderBy(o => o.StartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetExpiredOffersAsync(int days = 30)
    {
        var now = DateTime.Now;
        var pastDate = now.AddDays(-days);

        return await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.IsActive
                && o.EndDate < now
                && o.EndDate >= pastDate)
            .OrderByDescending(o => o.EndDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ExtendOfferAsync(int id, DateTime newEndDate)
    {
        var offer = await _context.ProductOffers.FindAsync(id);
        if (offer == null)
        {
            throw new InvalidOperationException($"Offer with ID {id} not found.");
        }

        if (newEndDate <= offer.EndDate)
        {
            throw new InvalidOperationException("New end date must be after current end date.");
        }

        offer.EndDate = newEndDate;
        offer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Extended offer {OfferId} to {NewEndDate}", id, newEndDate);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductOffer>> GetActiveOffersAsync()
    {
        var now = DateTime.Now;
        return await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.IsActive
                && o.StartDate <= now
                && o.EndDate >= now)
            .OrderBy(o => o.Product.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OfferPerformanceData>> GetOfferPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        // Get all offers that were active during the period
        var offers = await _context.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.IsActive &&
                ((o.StartDate <= endDate && o.EndDate >= startDate) ||
                 (o.StartDate >= startDate && o.StartDate <= endDate)))
            .ToListAsync();

        var performanceData = new List<OfferPerformanceData>();

        foreach (var offer in offers)
        {
            // Query order items that used this offer during the period
            var orderItems = await _context.Set<OrderItem>()
                .Include(oi => oi.Order)
                .Where(oi => oi.AppliedOfferId == offer.Id
                    && oi.Order.CreatedAt >= startDate
                    && oi.Order.CreatedAt <= endDate)
                .ToListAsync();

            var redemptionCount = orderItems.Count;
            var totalRevenue = orderItems.Sum(oi => oi.TotalAmount);
            var totalDiscountGiven = orderItems.Sum(oi => oi.SavingsAmount);

            var now = DateTime.Now;
            var status = offer.EndDate < now ? "Expired"
                : offer.StartDate > now ? "Upcoming"
                : "Active";

            performanceData.Add(new OfferPerformanceData
            {
                OfferId = offer.Id,
                OfferName = offer.OfferName,
                ProductName = offer.Product?.Name ?? "Unknown",
                OriginalPrice = offer.Product?.SellingPrice ?? 0,
                OfferPrice = offer.OfferPrice,
                RedemptionCount = redemptionCount,
                TotalRevenue = totalRevenue,
                TotalDiscountGiven = totalDiscountGiven,
                Status = status,
                StartDate = offer.StartDate,
                EndDate = offer.EndDate
            });
        }

        return performanceData.OrderByDescending(p => p.RedemptionCount);
    }
}
