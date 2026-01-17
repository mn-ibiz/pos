namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Base interface for all domain entities with integer primary key.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    int Id { get; set; }
}

/// <summary>
/// Interface for entities that support soft delete.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is active (not deleted).
    /// </summary>
    bool IsActive { get; set; }
}

/// <summary>
/// Interface for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the entity.
    /// </summary>
    int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last modified the entity.
    /// </summary>
    int? UpdatedByUserId { get; set; }
}
