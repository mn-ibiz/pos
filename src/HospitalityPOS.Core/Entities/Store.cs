namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a store/branch in a multi-store chain.
/// </summary>
public class Store : BaseEntity
{
    /// <summary>
    /// Unique store code for identification (e.g., "STR001").
    /// </summary>
    public string StoreCode { get; set; } = string.Empty;
    public string Code { get => StoreCode; set => StoreCode = value; }

    /// <summary>
    /// Store name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the store.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City where the store is located.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Region/County for the store.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Store phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
    public string? Phone { get => PhoneNumber; set => PhoneNumber = value; }

    /// <summary>
    /// Store email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Tax registration number for the store (KRA PIN).
    /// </summary>
    public string? TaxRegistrationNumber { get; set; }

    /// <summary>
    /// eTIMS Device Serial Number for this store.
    /// </summary>
    public string? EtimsDeviceSerial { get; set; }

    /// <summary>
    /// Whether this is the headquarters/main store.
    /// </summary>
    public bool IsHeadquarters { get; set; }

    /// <summary>
    /// Whether the store can receive central product updates.
    /// </summary>
    public bool ReceivesCentralUpdates { get; set; } = true;

    /// <summary>
    /// Last time the store synchronized with HQ.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Store timezone (e.g., "Africa/Nairobi").
    /// </summary>
    public string TimeZone { get; set; } = "Africa/Nairobi";

    /// <summary>
    /// Opening time for the store.
    /// </summary>
    public TimeSpan? OpeningTime { get; set; }

    /// <summary>
    /// Closing time for the store.
    /// </summary>
    public TimeSpan? ClosingTime { get; set; }

    /// <summary>
    /// Manager name for the store.
    /// </summary>
    public string? ManagerName { get; set; }

    /// <summary>
    /// Notes about the store.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Pricing zone for this store (for regional pricing).
    /// </summary>
    public int? PricingZoneId { get; set; }

    // Navigation properties
    public virtual PricingZone? PricingZone { get; set; }
    public virtual ICollection<StoreProductOverride> ProductOverrides { get; set; } = new List<StoreProductOverride>();
    public virtual ICollection<ScheduledPriceChange> ScheduledPriceChanges { get; set; } = new List<ScheduledPriceChange>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
