namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents the current inventory level for a product.
/// </summary>
public class Inventory : BaseEntity
{
    public int ProductId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReservedStock { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}
