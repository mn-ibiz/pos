using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Base class for all domain entities providing common properties.
/// </summary>
public abstract class BaseEntity : IEntity, ISoftDeletable, IAuditable
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public int? CreatedByUserId { get; set; }

    /// <inheritdoc />
    public int? UpdatedByUserId { get; set; }
}
