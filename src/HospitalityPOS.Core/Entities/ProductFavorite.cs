namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a user's favorite/pinned product for quick access at POS.
/// </summary>
public class ProductFavorite : BaseEntity
{
    /// <summary>
    /// Gets or sets the user ID who owns this favorite.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the favorited product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting favorites.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Navigation property to the product.
    /// </summary>
    public virtual Product? Product { get; set; }
}
