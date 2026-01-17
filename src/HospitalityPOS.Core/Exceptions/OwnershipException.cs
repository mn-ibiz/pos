namespace HospitalityPOS.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to modify an entity they don't own.
/// </summary>
public class OwnershipException : Exception
{
    /// <summary>
    /// Gets the ID of the entity owner.
    /// </summary>
    public int OwnerId { get; }

    /// <summary>
    /// Gets the ID of the user attempting the modification.
    /// </summary>
    public int AttemptingUserId { get; }

    /// <summary>
    /// Gets the type of entity being accessed.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the ID of the entity being accessed.
    /// </summary>
    public int EntityId { get; }

    /// <summary>
    /// Gets the name of the owner for display purposes.
    /// </summary>
    public string? OwnerName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnershipException"/> class.
    /// </summary>
    /// <param name="ownerId">The ID of the entity owner.</param>
    /// <param name="attemptingUserId">The ID of the user attempting the modification.</param>
    /// <param name="entityType">The type of entity being accessed.</param>
    /// <param name="entityId">The ID of the entity being accessed.</param>
    /// <param name="ownerName">Optional name of the owner for display.</param>
    public OwnershipException(
        int ownerId,
        int attemptingUserId,
        string entityType,
        int entityId,
        string? ownerName = null)
        : base(GenerateMessage(ownerId, attemptingUserId, entityType, entityId, ownerName))
    {
        OwnerId = ownerId;
        AttemptingUserId = attemptingUserId;
        EntityType = entityType;
        EntityId = entityId;
        OwnerName = ownerName;
    }

    private static string GenerateMessage(int ownerId, int attemptingUserId, string entityType, int entityId, string? ownerName)
    {
        return string.IsNullOrEmpty(ownerName)
            ? $"User {attemptingUserId} is not authorized to modify {entityType} {entityId}. Owner is user {ownerId}."
            : $"User {attemptingUserId} is not authorized to modify {entityType} {entityId}. Owner is {ownerName} (ID: {ownerId}).";
    }
}
