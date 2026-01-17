namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Scope of promotion deployment.
/// </summary>
public enum DeploymentScope
{
    /// <summary>Deploy to all stores.</summary>
    AllStores = 1,
    /// <summary>Deploy to stores in specific zones.</summary>
    ByZone = 2,
    /// <summary>Deploy to specific individual stores.</summary>
    IndividualStores = 3
}

/// <summary>
/// Status of a deployment.
/// </summary>
public enum DeploymentStatus
{
    /// <summary>Deployment is pending.</summary>
    Pending = 0,
    /// <summary>Deployment is in progress.</summary>
    InProgress = 1,
    /// <summary>Deployment completed successfully.</summary>
    Completed = 2,
    /// <summary>Deployment partially completed.</summary>
    PartiallyCompleted = 3,
    /// <summary>Deployment failed.</summary>
    Failed = 4,
    /// <summary>Deployment was cancelled.</summary>
    Cancelled = 5,
    /// <summary>Deployment was rolled back.</summary>
    RolledBack = 6
}

/// <summary>
/// Represents a deployment of a promotion to stores.
/// </summary>
public class PromotionDeployment : BaseEntity
{
    public int PromotionId { get; set; }

    /// <summary>
    /// Scope of the deployment.
    /// </summary>
    public DeploymentScope Scope { get; set; }

    /// <summary>
    /// When the deployment was initiated.
    /// </summary>
    public DateTime DeployedAt { get; set; }

    /// <summary>
    /// When the deployment was completed (or failed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current status of the deployment.
    /// </summary>
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;

    /// <summary>
    /// Number of stores successfully deployed to.
    /// </summary>
    public int StoresDeployedCount { get; set; }

    /// <summary>
    /// Number of stores that failed deployment.
    /// </summary>
    public int StoresFailedCount { get; set; }

    /// <summary>
    /// Error message if deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Notes about the deployment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID who initiated the deployment.
    /// </summary>
    public int? DeployedByUserId { get; set; }

    /// <summary>
    /// Whether to overwrite existing store promotions.
    /// </summary>
    public bool OverwriteExisting { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual User? DeployedByUser { get; set; }
    public virtual ICollection<DeploymentZone> Zones { get; set; } = new List<DeploymentZone>();
    public virtual ICollection<DeploymentStore> Stores { get; set; } = new List<DeploymentStore>();
}

/// <summary>
/// Links a deployment to specific zones.
/// </summary>
public class DeploymentZone : BaseEntity
{
    public int DeploymentId { get; set; }
    public int PricingZoneId { get; set; }

    // Navigation properties
    public virtual PromotionDeployment Deployment { get; set; } = null!;
    public virtual PricingZone PricingZone { get; set; } = null!;
}

/// <summary>
/// Tracks deployment status for individual stores.
/// </summary>
public class DeploymentStore : BaseEntity
{
    public int DeploymentId { get; set; }
    public int StoreId { get; set; }

    /// <summary>
    /// Status of deployment to this specific store.
    /// </summary>
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;

    /// <summary>
    /// When the store was synced.
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    /// <summary>
    /// Error message if deployment to this store failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    // Navigation properties
    public virtual PromotionDeployment Deployment { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
