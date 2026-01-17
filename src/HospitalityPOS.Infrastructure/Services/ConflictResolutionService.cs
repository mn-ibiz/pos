using System.Text.Json;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for handling sync conflict detection and resolution.
/// </summary>
public class ConflictResolutionService : IConflictResolutionService
{
    private readonly IRepository<SyncConflict> _conflictRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConflictResolutionService> _logger;
    private readonly List<ConflictResolutionRuleDto> _rules;
    private readonly List<ConflictAuditDto> _auditLog;  // In-memory for now, can be persisted

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ConflictResolutionService(
        IRepository<SyncConflict> conflictRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConflictResolutionService> logger)
    {
        _conflictRepository = conflictRepository ?? throw new ArgumentNullException(nameof(conflictRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rules = new List<ConflictResolutionRuleDto>(DefaultConflictRules.Rules);
        _auditLog = new List<ConflictAuditDto>();
    }

    #region Conflict Detection

    public async Task<ConflictDetailDto?> DetectConflictAsync(
        string entityType,
        int entityId,
        string localData,
        string remoteData,
        DateTime localTimestamp,
        DateTime remoteTimestamp,
        int? syncBatchId = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentNullException(nameof(entityType));

        // Check if there's actually a difference
        if (!HasMeaningfulDifference(localData, remoteData))
        {
            _logger.LogDebug("No meaningful difference detected for {EntityType}:{EntityId}", entityType, entityId);
            return null;
        }

        var conflictingFields = GetConflictingFields(localData, remoteData);

        // Create the conflict entity
        var conflict = new SyncConflict
        {
            SyncBatchId = syncBatchId ?? 0,
            EntityType = Enum.TryParse<SyncEntityType>(entityType, out var type) ? type : SyncEntityType.Product,
            EntityId = entityId,
            LocalData = localData,
            RemoteData = remoteData,
            LocalTimestamp = localTimestamp,
            RemoteTimestamp = remoteTimestamp,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _conflictRepository.AddAsync(conflict);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Conflict detected for {EntityType}:{EntityId}, ConflictId: {ConflictId}",
            entityType, entityId, conflict.Id);

        // Log the detection
        await LogAuditAsync(conflict.Id, "Detected", null, ConflictStatus.Detected, null,
            $"Conflicting fields: {string.Join(", ", conflictingFields)}");

        var rule = GetApplicableRule(entityType, null);

        return new ConflictDetailDto
        {
            Id = conflict.Id,
            SyncBatchId = conflict.SyncBatchId,
            EntityType = entityType,
            EntityId = entityId,
            LocalData = localData,
            RemoteData = remoteData,
            LocalTimestamp = localTimestamp,
            RemoteTimestamp = remoteTimestamp,
            Status = ConflictStatus.Detected,
            DetectedAt = conflict.CreatedAt,
            ConflictingFields = JsonSerializer.Serialize(conflictingFields),
            ApplicableRule = rule
        };
    }

    public List<string> GetConflictingFields(string localData, string remoteData)
    {
        var conflictingFields = new List<string>();

        try
        {
            using var localDoc = JsonDocument.Parse(localData);
            using var remoteDoc = JsonDocument.Parse(remoteData);

            var localProps = localDoc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());
            var remoteProps = remoteDoc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());

            // Find properties that differ
            foreach (var prop in localProps)
            {
                if (remoteProps.TryGetValue(prop.Key, out var remoteValue))
                {
                    if (prop.Value != remoteValue)
                    {
                        conflictingFields.Add(prop.Key);
                    }
                }
                else
                {
                    conflictingFields.Add(prop.Key);  // Property only in local
                }
            }

            // Find properties only in remote
            foreach (var prop in remoteProps.Keys)
            {
                if (!localProps.ContainsKey(prop))
                {
                    conflictingFields.Add(prop);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON for conflict field comparison");
        }

        return conflictingFields;
    }

    public bool HasMeaningfulDifference(string localData, string remoteData)
    {
        if (string.IsNullOrWhiteSpace(localData) && string.IsNullOrWhiteSpace(remoteData))
            return false;

        if (string.IsNullOrWhiteSpace(localData) || string.IsNullOrWhiteSpace(remoteData))
            return true;

        try
        {
            // Normalize JSON for comparison (ignore whitespace/formatting)
            using var localDoc = JsonDocument.Parse(localData);
            using var remoteDoc = JsonDocument.Parse(remoteData);

            var localNormalized = JsonSerializer.Serialize(localDoc.RootElement);
            var remoteNormalized = JsonSerializer.Serialize(remoteDoc.RootElement);

            return localNormalized != remoteNormalized;
        }
        catch (JsonException)
        {
            // If JSON parsing fails, do string comparison
            return localData.Trim() != remoteData.Trim();
        }
    }

    #endregion

    #region Conflict Resolution

    public async Task<ConflictResolutionResultDto> ResolveAsync(SyncConflict conflict)
    {
        if (conflict == null)
            throw new ArgumentNullException(nameof(conflict));

        var entityType = conflict.EntityType.ToString();
        var rule = GetApplicableRule(entityType, null);

        // If rule requires manual review, don't auto-resolve
        if (rule.RequireManualReview)
        {
            _logger.LogInformation("Conflict {ConflictId} requires manual review per rule", conflict.Id);

            await LogAuditAsync(conflict.Id, "PendingManual", ConflictStatus.Detected, ConflictStatus.PendingManual,
                null, "Rule requires manual review");

            return new ConflictResolutionResultDto
            {
                Success = false,
                ConflictId = conflict.Id,
                NewStatus = ConflictStatus.PendingManual,
                ErrorMessage = "Conflict requires manual resolution"
            };
        }

        string? resolvedData;
        var resolution = rule.DefaultResolution;

        switch (resolution)
        {
            case ConflictResolutionType.LocalWins:
                resolvedData = conflict.LocalData;
                break;

            case ConflictResolutionType.RemoteWins:
                resolvedData = conflict.RemoteData;
                break;

            case ConflictResolutionType.LastWriteWins:
                resolvedData = conflict.LocalTimestamp > conflict.RemoteTimestamp
                    ? conflict.LocalData
                    : conflict.RemoteData;
                break;

            case ConflictResolutionType.Manual:
                return ConflictResolutionResultDto.Failed(conflict.Id, "Conflict requires manual resolution");

            case ConflictResolutionType.Merged:
                resolvedData = MergeData(conflict.LocalData, conflict.RemoteData);
                break;

            default:
                resolvedData = conflict.RemoteData;
                break;
        }

        // Update conflict record
        conflict.IsResolved = true;
        conflict.Resolution = MapResolutionToWinner(resolution, conflict.LocalTimestamp, conflict.RemoteTimestamp);
        conflict.ResolvedAt = DateTime.UtcNow;
        conflict.ResolutionNotes = $"Auto-resolved using rule: {rule.Description ?? resolution.ToString()}";

        await _conflictRepository.UpdateAsync(conflict);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(conflict.Id, "AutoResolved", ConflictStatus.Detected, ConflictStatus.AutoResolved,
            null, $"Applied resolution: {resolution}");

        _logger.LogInformation("Conflict {ConflictId} auto-resolved with {Resolution}", conflict.Id, resolution);

        return ConflictResolutionResultDto.Succeeded(conflict.Id, resolution, resolvedData, autoResolved: true);
    }

    public async Task<ConflictResolutionResultDto> ResolveByIdAsync(int conflictId)
    {
        var conflict = await _conflictRepository.GetByIdAsync(conflictId);
        if (conflict == null)
        {
            return ConflictResolutionResultDto.Failed(conflictId, "Conflict not found");
        }

        return await ResolveAsync(conflict);
    }

    public async Task<ConflictResolutionResultDto> ManualResolveAsync(ManualResolveConflictDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var conflict = await _conflictRepository.GetByIdAsync(request.ConflictId);
        if (conflict == null)
        {
            return ConflictResolutionResultDto.Failed(request.ConflictId, "Conflict not found");
        }

        if (conflict.IsResolved)
        {
            return ConflictResolutionResultDto.Failed(request.ConflictId, "Conflict already resolved");
        }

        var oldStatus = conflict.IsResolved ? ConflictStatus.Resolved : ConflictStatus.PendingManual;
        string? resolvedData;

        switch (request.Resolution)
        {
            case ConflictResolutionType.LocalWins:
                resolvedData = conflict.LocalData;
                break;

            case ConflictResolutionType.RemoteWins:
                resolvedData = conflict.RemoteData;
                break;

            case ConflictResolutionType.LastWriteWins:
                resolvedData = conflict.LocalTimestamp > conflict.RemoteTimestamp
                    ? conflict.LocalData
                    : conflict.RemoteData;
                break;

            case ConflictResolutionType.Merged:
                resolvedData = request.MergedData ?? MergeData(conflict.LocalData, conflict.RemoteData);
                break;

            default:
                resolvedData = conflict.RemoteData;
                break;
        }

        conflict.IsResolved = true;
        conflict.Resolution = MapResolutionToWinner(request.Resolution, conflict.LocalTimestamp, conflict.RemoteTimestamp);
        conflict.ResolvedByUserId = request.ResolvedByUserId;
        conflict.ResolvedAt = DateTime.UtcNow;
        conflict.ResolutionNotes = request.Notes;

        await _conflictRepository.UpdateAsync(conflict);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(conflict.Id, "ManualResolved", oldStatus, ConflictStatus.Resolved,
            request.ResolvedByUserId, $"Resolution: {request.Resolution}, Notes: {request.Notes}");

        _logger.LogInformation("Conflict {ConflictId} manually resolved by user {UserId}",
            conflict.Id, request.ResolvedByUserId);

        return ConflictResolutionResultDto.Succeeded(conflict.Id, request.Resolution, resolvedData, autoResolved: false);
    }

    public async Task<bool> IgnoreConflictAsync(int conflictId, int userId, string? notes = null)
    {
        var conflict = await _conflictRepository.GetByIdAsync(conflictId);
        if (conflict == null)
        {
            _logger.LogWarning("Attempted to ignore non-existent conflict {ConflictId}", conflictId);
            return false;
        }

        var oldStatus = conflict.IsResolved ? ConflictStatus.Resolved : ConflictStatus.Detected;

        conflict.IsResolved = true;
        conflict.ResolvedByUserId = userId;
        conflict.ResolvedAt = DateTime.UtcNow;
        conflict.ResolutionNotes = $"[IGNORED] {notes ?? "No reason provided"}";

        await _conflictRepository.UpdateAsync(conflict);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(conflictId, "Ignored", oldStatus, ConflictStatus.Ignored, userId, notes);

        _logger.LogInformation("Conflict {ConflictId} ignored by user {UserId}", conflictId, userId);

        return true;
    }

    public async Task<bool> ApplyResolutionAsync(SyncConflict conflict, string resolvedData)
    {
        // This would apply the resolved data to the actual entity
        // Implementation depends on how entities are stored and updated
        // For now, log the action - actual implementation would update the entity

        _logger.LogInformation("Applying resolution for {EntityType}:{EntityId} with data: {Data}",
            conflict.EntityType, conflict.EntityId, resolvedData);

        // Mark the resolution as applied
        if (!string.IsNullOrEmpty(conflict.ResolutionNotes))
        {
            conflict.ResolutionNotes += " [APPLIED]";
        }
        else
        {
            conflict.ResolutionNotes = "[APPLIED]";
        }

        await _conflictRepository.UpdateAsync(conflict);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Conflict Queries

    public async Task<List<ConflictDetailDto>> GetPendingConflictsAsync(int? storeId = null)
    {
        var conflicts = await _conflictRepository.FindAsync(c => !c.IsResolved && c.IsActive);

        return conflicts.Select(MapToDetailDto).ToList();
    }

    public async Task<ConflictDetailDto?> GetConflictByIdAsync(int conflictId)
    {
        var conflict = await _conflictRepository.GetByIdAsync(conflictId);
        return conflict != null ? MapToDetailDto(conflict) : null;
    }

    public async Task<List<ConflictDetailDto>> QueryConflictsAsync(ConflictQueryDto query)
    {
        var conflicts = await _conflictRepository.FindAsync(c => c.IsActive);

        var filtered = conflicts.AsEnumerable();

        if (query.EntityType != null)
        {
            if (Enum.TryParse<SyncEntityType>(query.EntityType, out var entityType))
            {
                filtered = filtered.Where(c => c.EntityType == entityType);
            }
        }

        if (query.EntityId.HasValue)
        {
            filtered = filtered.Where(c => c.EntityId == query.EntityId.Value);
        }

        if (query.FromDate.HasValue)
        {
            filtered = filtered.Where(c => c.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            filtered = filtered.Where(c => c.CreatedAt <= query.ToDate.Value);
        }

        if (!query.IncludeResolved)
        {
            filtered = filtered.Where(c => !c.IsResolved);
        }

        if (query.Status.HasValue)
        {
            filtered = query.Status.Value switch
            {
                ConflictStatus.Detected => filtered.Where(c => !c.IsResolved && c.Resolution == null),
                ConflictStatus.PendingManual => filtered.Where(c => !c.IsResolved),
                ConflictStatus.Resolved => filtered.Where(c => c.IsResolved && c.ResolvedByUserId != null),
                ConflictStatus.AutoResolved => filtered.Where(c => c.IsResolved && c.ResolvedByUserId == null),
                ConflictStatus.Ignored => filtered.Where(c => c.IsResolved && c.ResolutionNotes != null && c.ResolutionNotes.Contains("[IGNORED]")),
                _ => filtered
            };
        }

        return filtered
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(MapToDetailDto)
            .ToList();
    }

    public async Task<int> GetConflictCountByStatusAsync(ConflictStatus status, int? storeId = null)
    {
        var conflicts = await _conflictRepository.FindAsync(c => c.IsActive);

        return status switch
        {
            ConflictStatus.Detected => conflicts.Count(c => !c.IsResolved && c.Resolution == null),
            ConflictStatus.PendingManual => conflicts.Count(c => !c.IsResolved),
            ConflictStatus.Resolved => conflicts.Count(c => c.IsResolved && c.ResolvedByUserId != null),
            ConflictStatus.AutoResolved => conflicts.Count(c => c.IsResolved && c.ResolvedByUserId == null),
            ConflictStatus.Ignored => conflicts.Count(c => c.IsResolved && c.ResolutionNotes != null && c.ResolutionNotes.Contains("[IGNORED]")),
            _ => 0
        };
    }

    public async Task<ConflictSummaryDto> GetConflictSummaryAsync(int? storeId = null)
    {
        var conflicts = await _conflictRepository.FindAsync(c => c.IsActive);
        var conflictList = conflicts.ToList();

        var summary = new ConflictSummaryDto
        {
            TotalConflicts = conflictList.Count,
            PendingManual = conflictList.Count(c => !c.IsResolved),
            AutoResolved = conflictList.Count(c => c.IsResolved && c.ResolvedByUserId == null),
            ManuallyResolved = conflictList.Count(c => c.IsResolved && c.ResolvedByUserId != null),
            Ignored = conflictList.Count(c => c.IsResolved && c.ResolutionNotes != null && c.ResolutionNotes.Contains("[IGNORED]")),
            ByEntityType = conflictList
                .GroupBy(c => c.EntityType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            RecentConflicts = conflictList
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .Select(MapToDetailDto)
                .ToList()
        };

        return summary;
    }

    #endregion

    #region Resolution Rules

    public ConflictResolutionRuleDto GetApplicableRule(string entityType, string? propertyName = null)
    {
        // First try to find a property-specific rule
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var propertyRule = _rules
                .Where(r => r.IsActive && r.EntityType == entityType && r.PropertyName == propertyName)
                .OrderBy(r => r.Priority)
                .FirstOrDefault();

            if (propertyRule != null)
                return propertyRule;
        }

        // Then try entity-level rule
        var entityRule = _rules
            .Where(r => r.IsActive && r.EntityType == entityType && string.IsNullOrEmpty(r.PropertyName))
            .OrderBy(r => r.Priority)
            .FirstOrDefault();

        if (entityRule != null)
            return entityRule;

        // Return default rule
        return new ConflictResolutionRuleDto
        {
            EntityType = entityType,
            DefaultResolution = ConflictResolutionType.RemoteWins,
            Description = "Default: Remote (HQ) wins"
        };
    }

    public List<ConflictResolutionRuleDto> GetAllRules() => _rules.ToList();

    public void AddOrUpdateRule(ConflictResolutionRuleDto rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        var existing = _rules.FirstOrDefault(r =>
            r.EntityType == rule.EntityType && r.PropertyName == rule.PropertyName);

        if (existing != null)
        {
            _rules.Remove(existing);
        }

        _rules.Add(rule);
        _logger.LogInformation("Rule added/updated for {EntityType}.{Property}",
            rule.EntityType, rule.PropertyName ?? "*");
    }

    public bool RemoveRule(string entityType, string? propertyName = null)
    {
        var rule = _rules.FirstOrDefault(r =>
            r.EntityType == entityType && r.PropertyName == propertyName);

        if (rule != null)
        {
            _rules.Remove(rule);
            _logger.LogInformation("Rule removed for {EntityType}.{Property}",
                entityType, propertyName ?? "*");
            return true;
        }

        return false;
    }

    public void ResetToDefaultRules()
    {
        _rules.Clear();
        _rules.AddRange(DefaultConflictRules.Rules);
        _logger.LogInformation("Conflict resolution rules reset to defaults");
    }

    #endregion

    #region Audit Trail

    public Task<List<ConflictAuditDto>> GetConflictAuditTrailAsync(int conflictId)
    {
        var audits = _auditLog
            .Where(a => a.ConflictId == conflictId)
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        return Task.FromResult(audits);
    }

    public Task LogAuditAsync(int conflictId, string action, ConflictStatus? oldStatus, ConflictStatus newStatus, int? userId = null, string? details = null)
    {
        var audit = new ConflictAuditDto
        {
            Id = _auditLog.Count + 1,
            ConflictId = conflictId,
            Action = action,
            OldStatus = oldStatus?.ToString(),
            NewStatus = newStatus.ToString(),
            UserId = userId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _auditLog.Add(audit);

        _logger.LogDebug("Audit: Conflict {ConflictId} - {Action} ({OldStatus} -> {NewStatus})",
            conflictId, action, oldStatus, newStatus);

        return Task.CompletedTask;
    }

    #endregion

    #region Batch Operations

    public async Task<int> AutoResolveAllAsync()
    {
        var pendingConflicts = await _conflictRepository.FindAsync(c => !c.IsResolved && c.IsActive);
        var resolved = 0;

        foreach (var conflict in pendingConflicts)
        {
            var rule = GetApplicableRule(conflict.EntityType.ToString(), null);

            if (!rule.RequireManualReview && rule.DefaultResolution != ConflictResolutionType.Manual)
            {
                var result = await ResolveAsync(conflict);
                if (result.Success)
                {
                    resolved++;
                }
            }
        }

        _logger.LogInformation("Auto-resolved {Count} conflicts", resolved);
        return resolved;
    }

    public async Task<int> BulkResolveAsync(IEnumerable<int> conflictIds, ConflictResolutionType resolution, int userId, string? notes = null)
    {
        var resolved = 0;

        foreach (var conflictId in conflictIds)
        {
            var request = new ManualResolveConflictDto
            {
                ConflictId = conflictId,
                Resolution = resolution,
                ResolvedByUserId = userId,
                Notes = notes ?? "Bulk resolution"
            };

            var result = await ManualResolveAsync(request);
            if (result.Success)
            {
                resolved++;
            }
        }

        _logger.LogInformation("Bulk resolved {Count} conflicts with {Resolution}", resolved, resolution);
        return resolved;
    }

    public async Task<int> PurgeResolvedConflictsAsync(DateTime olderThan)
    {
        var toDelete = await _conflictRepository.FindAsync(c =>
            c.IsResolved && c.ResolvedAt.HasValue && c.ResolvedAt.Value < olderThan);

        var count = toDelete.Count();

        foreach (var conflict in toDelete)
        {
            conflict.IsActive = false;  // Soft delete
            await _conflictRepository.UpdateAsync(conflict);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Purged {Count} resolved conflicts older than {Date}", count, olderThan);
        return count;
    }

    #endregion

    #region Private Helpers

    private ConflictDetailDto MapToDetailDto(SyncConflict conflict)
    {
        var conflictingFields = GetConflictingFields(conflict.LocalData, conflict.RemoteData);
        var rule = GetApplicableRule(conflict.EntityType.ToString(), null);

        ConflictStatus status;
        if (!conflict.IsResolved)
        {
            status = rule.RequireManualReview ? ConflictStatus.PendingManual : ConflictStatus.Detected;
        }
        else if (conflict.ResolutionNotes?.Contains("[IGNORED]") == true)
        {
            status = ConflictStatus.Ignored;
        }
        else if (conflict.ResolvedByUserId == null)
        {
            status = ConflictStatus.AutoResolved;
        }
        else
        {
            status = ConflictStatus.Resolved;
        }

        return new ConflictDetailDto
        {
            Id = conflict.Id,
            SyncBatchId = conflict.SyncBatchId,
            EntityType = conflict.EntityType.ToString(),
            EntityId = conflict.EntityId,
            LocalData = conflict.LocalData,
            RemoteData = conflict.RemoteData,
            LocalTimestamp = conflict.LocalTimestamp,
            RemoteTimestamp = conflict.RemoteTimestamp,
            Status = status,
            AppliedResolution = conflict.Resolution.HasValue ? MapWinnerToResolution(conflict.Resolution.Value) : null,
            ResolvedByUserId = conflict.ResolvedByUserId,
            ResolvedAt = conflict.ResolvedAt,
            ResolutionNotes = conflict.ResolutionNotes,
            DetectedAt = conflict.CreatedAt,
            ConflictingFields = JsonSerializer.Serialize(conflictingFields),
            ApplicableRule = rule
        };
    }

    private string MergeData(string localData, string remoteData)
    {
        try
        {
            using var localDoc = JsonDocument.Parse(localData);
            using var remoteDoc = JsonDocument.Parse(remoteData);

            var merged = new Dictionary<string, JsonElement>();

            // Start with remote properties
            foreach (var prop in remoteDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            // Override with local properties (simple merge strategy)
            foreach (var prop in localDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            return JsonSerializer.Serialize(merged, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to merge JSON data, using local data");
            return localData;
        }
    }

    private static ConflictWinner MapResolutionToWinner(ConflictResolutionType resolution, DateTime localTimestamp, DateTime remoteTimestamp)
    {
        return resolution switch
        {
            ConflictResolutionType.LocalWins => ConflictWinner.Store,
            ConflictResolutionType.RemoteWins => ConflictWinner.HQ,
            ConflictResolutionType.LastWriteWins => localTimestamp > remoteTimestamp ? ConflictWinner.Store : ConflictWinner.HQ,
            ConflictResolutionType.Manual => ConflictWinner.Manual,
            ConflictResolutionType.Merged => ConflictWinner.Manual,
            _ => ConflictWinner.HQ
        };
    }

    private static ConflictResolutionType MapWinnerToResolution(ConflictWinner winner)
    {
        return winner switch
        {
            ConflictWinner.Store => ConflictResolutionType.LocalWins,
            ConflictWinner.HQ => ConflictResolutionType.RemoteWins,
            ConflictWinner.LatestTimestamp => ConflictResolutionType.LastWriteWins,
            ConflictWinner.Manual => ConflictResolutionType.Manual,
            _ => ConflictResolutionType.RemoteWins
        };
    }

    #endregion
}
