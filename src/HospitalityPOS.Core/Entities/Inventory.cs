using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents the current inventory level for a product.
/// </summary>
public class Inventory : BaseEntity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public decimal CurrentStock { get; set; }

    [NotMapped]
    public decimal QuantityOnHand { get => CurrentStock; set => CurrentStock = value; }

    [NotMapped]
    public decimal Quantity { get => CurrentStock; set => CurrentStock = value; }
    public decimal ReservedStock { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
