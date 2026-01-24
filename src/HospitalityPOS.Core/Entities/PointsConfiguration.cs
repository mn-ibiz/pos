namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Configuration for the loyalty points system.
/// </summary>
public class PointsConfiguration : BaseEntity
{
    /// <summary>
    /// Gets or sets the configuration name (e.g., "Default", "VIP", etc.).
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the amount of spending in KSh required to earn 1 point.
    /// Default: KSh 100 = 1 point.
    /// </summary>
    public decimal EarningRate { get; set; } = 100m;

    /// <summary>
    /// Gets or sets the value in KSh of each point when redeemed.
    /// Default: 1 point = KSh 1.
    /// </summary>
    public decimal RedemptionValue { get; set; } = 1m;

    /// <summary>
    /// Gets or sets the minimum points required for redemption.
    /// </summary>
    public int MinimumRedemptionPoints { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum points that can be redeemed in a single transaction (0 = unlimited).
    /// </summary>
    public int MaximumRedemptionPoints { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum percentage of transaction that can be paid with points (0-100).
    /// </summary>
    public int MaxRedemptionPercentage { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether points can be earned on discounted items.
    /// </summary>
    public bool EarnOnDiscountedItems { get; set; } = true;

    /// <summary>
    /// Gets or sets whether points can be earned on tax portion.
    /// </summary>
    public bool EarnOnTax { get; set; } = false;

    /// <summary>
    /// Gets or sets the points expiry period in days (0 = never expire).
    /// </summary>
    public int PointsExpiryDays { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this is the default configuration.
    /// </summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets the description of this configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the welcome bonus points awarded to new members on enrollment (0 = disabled).
    /// </summary>
    public int WelcomeBonusPoints { get; set; } = 0;

    /// <summary>
    /// Gets or sets the custom welcome bonus message for SMS.
    /// </summary>
    public string? WelcomeBonusMessage { get; set; }
}
