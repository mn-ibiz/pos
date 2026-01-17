using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for checking entity ownership and authorizing access.
/// </summary>
public class OwnershipService : IOwnershipService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnershipService"/> class.
    /// </summary>
    public OwnershipService(
        IServiceScopeFactory scopeFactory,
        ISessionService sessionService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> IsReceiptOwnerAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.IsLoggedIn)
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var receipt = await context.Receipts
            .AsNoTracking()
            .Where(r => r.Id == receiptId)
            .Select(r => new { r.OwnerId })
            .FirstOrDefaultAsync(cancellationToken);

        return receipt?.OwnerId == _sessionService.CurrentUserId;
    }

    /// <inheritdoc />
    public async Task<bool> CanModifyReceiptAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.IsLoggedIn)
        {
            return false;
        }

        // Check if user has ModifyAny permission (managers)
        if (_sessionService.HasPermission(PermissionNames.Receipts.ModifyAny))
        {
            return true;
        }

        // Check if user owns the receipt
        return await IsReceiptOwnerAsync(receiptId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OwnershipCheckResult> ValidateReceiptOwnershipAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.IsLoggedIn)
        {
            return OwnershipCheckResult.NotFound("Session");
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var receipt = await context.Receipts
            .AsNoTracking()
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (receipt is null)
        {
            _logger.Warning("Receipt not found during ownership validation. ReceiptId: {ReceiptId}, UserId: {UserId}",
                receiptId, _sessionService.CurrentUserId);
            return OwnershipCheckResult.NotFound("Receipt");
        }

        var currentUserId = _sessionService.CurrentUserId;

        // Check if current user is the owner
        if (receipt.OwnerId == currentUserId)
        {
            _logger.Debug("User {UserId} is the owner of receipt {ReceiptId}", currentUserId, receiptId);
            return OwnershipCheckResult.Valid();
        }

        // Check if user has ModifyAny permission (managers/admins)
        if (_sessionService.HasPermission(PermissionNames.Receipts.ModifyAny))
        {
            _logger.Information("User {UserId} has ModifyAny permission for receipt {ReceiptId} owned by {OwnerId}",
                currentUserId, receiptId, receipt.OwnerId);

            // Log this implicit override
            await LogOwnershipOverrideAsync(
                context,
                receiptId,
                receipt.OwnerId,
                currentUserId,
                null,
                "Implicit access via ModifyAny permission",
                "Accessing receipt with elevated permissions",
                cancellationToken);

            return OwnershipCheckResult.ValidWithOverride(currentUserId, _sessionService.CurrentUserDisplayName);
        }

        // User is not the owner and doesn't have ModifyAny permission
        _logger.Warning("User {UserId} denied access to receipt {ReceiptId} owned by {OwnerId} ({OwnerName})",
            currentUserId, receiptId, receipt.OwnerId, receipt.Owner?.FullName ?? "Unknown");

        return OwnershipCheckResult.Invalid(
            receipt.OwnerId,
            receipt.Owner?.FullName ?? "Unknown User",
            "Not Authorized - Owner Only");
    }

    /// <inheritdoc />
    public async Task<OwnershipCheckResult> AuthorizeWithOverrideAsync(
        int receiptId,
        int authorizingUserId,
        string authorizingUserName,
        string reason,
        string actionDescription,
        CancellationToken cancellationToken = default)
    {
        if (!_sessionService.IsLoggedIn)
        {
            return OwnershipCheckResult.NotFound("Session");
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var receipt = await context.Receipts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (receipt is null)
        {
            return OwnershipCheckResult.NotFound("Receipt");
        }

        // Log the override
        await LogOwnershipOverrideAsync(
            context,
            receiptId,
            receipt.OwnerId,
            _sessionService.CurrentUserId,
            authorizingUserId,
            reason,
            actionDescription,
            cancellationToken);

        _logger.Information(
            "Receipt ownership override authorized. ReceiptId: {ReceiptId}, OwnerId: {OwnerId}, " +
            "AttemptingUser: {AttemptingUserId}, AuthorizingUser: {AuthorizingUserId} ({AuthorizingUserName}), " +
            "Reason: {Reason}, Action: {Action}",
            receiptId, receipt.OwnerId, _sessionService.CurrentUserId, authorizingUserId, authorizingUserName,
            reason, actionDescription);

        return OwnershipCheckResult.ValidWithOverride(authorizingUserId, authorizingUserName);
    }

    /// <inheritdoc />
    public async Task LogOwnershipDenialAsync(
        int receiptId,
        int ownerId,
        int attemptingUserId,
        string actionDescription,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var auditLog = new AuditLog
        {
            UserId = attemptingUserId,
            Action = "ReceiptOwnershipDenied",
            EntityType = "Receipt",
            EntityId = receiptId,
            NewValues = JsonSerializer.Serialize(new
            {
                OwnerId = ownerId,
                AttemptingUserId = attemptingUserId,
                ActionAttempted = actionDescription,
                Timestamp = DateTime.UtcNow
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.Warning("Receipt ownership denied. ReceiptId: {ReceiptId}, OwnerId: {OwnerId}, " +
            "AttemptingUser: {AttemptingUserId}, Action: {Action}",
            receiptId, ownerId, attemptingUserId, actionDescription);
    }

    private static async Task LogOwnershipOverrideAsync(
        POSDbContext context,
        int receiptId,
        int ownerId,
        int attemptingUserId,
        int? authorizingUserId,
        string reason,
        string actionDescription,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            UserId = attemptingUserId,
            Action = "ReceiptOwnershipOverride",
            EntityType = "Receipt",
            EntityId = receiptId,
            NewValues = JsonSerializer.Serialize(new
            {
                OwnerId = ownerId,
                AttemptingUserId = attemptingUserId,
                AuthorizingUserId = authorizingUserId,
                Reason = reason,
                ActionAttempted = actionDescription,
                Timestamp = DateTime.UtcNow
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
