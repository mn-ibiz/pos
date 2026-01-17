using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for handling sync conflict detection and resolution.
/// </summary>
public interface IConflictResolutionService
{
    #region Conflict Detection

    /// <summary>
    /// Detects a conflict between local and remote data.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="localData">JSON serialized local data.</param>
    /// <param name="remoteData">JSON serialized remote data.</param>
    /// <param name="localTimestamp">Local modification timestamp.</param>
    /// <param name="remoteTimestamp">Remote modification timestamp.</param>
    /// <param name="syncBatchId">Optional sync batch ID.</param>
    /// <returns>The detected conflict, or null if no conflict.</returns>
    Task<ConflictDetailDto?> DetectConflictAsync(
        string entityType,
        int entityId,
        string localData,
        string remoteData,
        DateTime localTimestamp,
        DateTime remoteTimestamp,
        int? syncBatchId = null);

    /// <summary>
    /// Compares local and remote data to identify conflicting fields.
    /// </summary>
    /// <param name="localData">JSON serialized local data.</param>
    /// <param name="remoteData">JSON serialized remote data.</param>
    /// <returns>List of property names that conflict.</returns>
    List<string> GetConflictingFields(string localData, string remoteData);

    /// <summary>
    /// Checks if data has actually changed between versions.
    /// </summary>
    /// <param name="localData">JSON serialized local data.</param>
    /// <param name="remoteData">JSON serialized remote data.</param>
    /// <returns>True if there are meaningful differences.</returns>
    bool HasMeaningfulDifference(string localData, string remoteData);

    #endregion

    #region Conflict Resolution

    /// <summary>
    /// Resolves a conflict using configured rules.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <returns>The resolution result.</returns>
    Task<ConflictResolutionResultDto> ResolveAsync(SyncConflict conflict);

    /// <summary>
    /// Resolves a conflict by ID using configured rules.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <returns>The resolution result.</returns>
    Task<ConflictResolutionResultDto> ResolveByIdAsync(int conflictId);

    /// <summary>
    /// Manually resolves a conflict.
    /// </summary>
    /// <param name="request">The manual resolution request.</param>
    /// <returns>The resolution result.</returns>
    Task<ConflictResolutionResultDto> ManualResolveAsync(ManualResolveConflictDto request);

    /// <summary>
    /// Ignores a conflict (marks it as ignored without applying changes).
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <param name="userId">The user ignoring the conflict.</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>True if successfully ignored.</returns>
    Task<bool> IgnoreConflictAsync(int conflictId, int userId, string? notes = null);

    /// <summary>
    /// Applies the resolved data to the database entity.
    /// </summary>
    /// <param name="conflict">The resolved conflict.</param>
    /// <param name="resolvedData">The data to apply.</param>
    /// <returns>True if successfully applied.</returns>
    Task<bool> ApplyResolutionAsync(SyncConflict conflict, string resolvedData);

    #endregion

    #region Conflict Queries

    /// <summary>
    /// Gets all pending conflicts requiring manual resolution.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>List of pending conflicts.</returns>
    Task<List<ConflictDetailDto>> GetPendingConflictsAsync(int? storeId = null);

    /// <summary>
    /// Gets a conflict by ID.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <returns>The conflict details.</returns>
    Task<ConflictDetailDto?> GetConflictByIdAsync(int conflictId);

    /// <summary>
    /// Queries conflicts with filters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>List of matching conflicts.</returns>
    Task<List<ConflictDetailDto>> QueryConflictsAsync(ConflictQueryDto query);

    /// <summary>
    /// Gets the count of conflicts by status.
    /// </summary>
    /// <param name="status">The status to count.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>The count.</returns>
    Task<int> GetConflictCountByStatusAsync(ConflictStatus status, int? storeId = null);

    /// <summary>
    /// Gets a summary of all conflicts.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Conflict summary.</returns>
    Task<ConflictSummaryDto> GetConflictSummaryAsync(int? storeId = null);

    #endregion

    #region Resolution Rules

    /// <summary>
    /// Gets the applicable resolution rule for an entity/property.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="propertyName">Optional property name for property-specific rules.</param>
    /// <returns>The applicable rule, or default rule if none specific.</returns>
    ConflictResolutionRuleDto GetApplicableRule(string entityType, string? propertyName = null);

    /// <summary>
    /// Gets all configured resolution rules.
    /// </summary>
    /// <returns>List of all rules.</returns>
    List<ConflictResolutionRuleDto> GetAllRules();

    /// <summary>
    /// Adds or updates a resolution rule.
    /// </summary>
    /// <param name="rule">The rule to add/update.</param>
    void AddOrUpdateRule(ConflictResolutionRuleDto rule);

    /// <summary>
    /// Removes a resolution rule.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="propertyName">Optional property name.</param>
    /// <returns>True if removed.</returns>
    bool RemoveRule(string entityType, string? propertyName = null);

    /// <summary>
    /// Resets rules to defaults.
    /// </summary>
    void ResetToDefaultRules();

    #endregion

    #region Audit Trail

    /// <summary>
    /// Gets the audit trail for a conflict.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <returns>List of audit entries.</returns>
    Task<List<ConflictAuditDto>> GetConflictAuditTrailAsync(int conflictId);

    /// <summary>
    /// Logs an audit entry for a conflict action.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <param name="action">The action taken.</param>
    /// <param name="oldStatus">Previous status.</param>
    /// <param name="newStatus">New status.</param>
    /// <param name="userId">User who performed the action.</param>
    /// <param name="details">Additional details.</param>
    Task LogAuditAsync(int conflictId, string action, ConflictStatus? oldStatus, ConflictStatus newStatus, int? userId = null, string? details = null);

    #endregion

    #region Batch Operations

    /// <summary>
    /// Auto-resolves all conflicts that have applicable auto-resolution rules.
    /// </summary>
    /// <returns>Number of conflicts resolved.</returns>
    Task<int> AutoResolveAllAsync();

    /// <summary>
    /// Resolves multiple conflicts with the same resolution.
    /// </summary>
    /// <param name="conflictIds">The conflict IDs.</param>
    /// <param name="resolution">The resolution to apply.</param>
    /// <param name="userId">The user resolving.</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>Number of conflicts resolved.</returns>
    Task<int> BulkResolveAsync(IEnumerable<int> conflictIds, ConflictResolutionType resolution, int userId, string? notes = null);

    /// <summary>
    /// Purges resolved conflicts older than the specified date.
    /// </summary>
    /// <param name="olderThan">Date threshold.</param>
    /// <returns>Number of conflicts purged.</returns>
    Task<int> PurgeResolvedConflictsAsync(DateTime olderThan);

    #endregion
}
