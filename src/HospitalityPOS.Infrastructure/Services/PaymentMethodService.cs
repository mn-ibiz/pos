using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing payment methods.
/// </summary>
public class PaymentMethodService : IPaymentMethodService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Event raised when payment methods change.
    /// </summary>
    public event EventHandler? PaymentMethodsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentMethodService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public PaymentMethodService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .AsNoTracking()
            .OrderBy(pm => pm.DisplayOrder)
            .ThenBy(pm => pm.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentMethod>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .AsNoTracking()
            .Where(pm => pm.IsActive)
            .OrderBy(pm => pm.DisplayOrder)
            .ThenBy(pm => pm.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.Code == code, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentMethod> CreateAsync(PaymentMethodDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Code);

        // Validate code uniqueness
        if (!await IsCodeUniqueAsync(dto.Code, null, cancellationToken))
        {
            throw new InvalidOperationException($"A payment method with code '{dto.Code}' already exists.");
        }

        // Get next display order if not specified
        var displayOrder = dto.DisplayOrder;
        if (displayOrder == 0)
        {
            var maxOrder = await _context.PaymentMethods
                .MaxAsync(pm => (int?)pm.DisplayOrder, cancellationToken)
                .ConfigureAwait(false);
            displayOrder = (maxOrder ?? 0) + 1;
        }

        var paymentMethod = new PaymentMethod
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpperInvariant(),
            Type = dto.Type,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive,
            RequiresReference = dto.RequiresReference,
            ReferenceLabel = dto.ReferenceLabel?.Trim(),
            ReferenceMinLength = dto.ReferenceMinLength,
            ReferenceMaxLength = dto.ReferenceMaxLength,
            SupportsChange = dto.SupportsChange,
            OpensDrawer = dto.OpensDrawer,
            DisplayOrder = displayOrder,
            IconPath = dto.IconPath?.Trim(),
            BackgroundColor = dto.BackgroundColor?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.PaymentMethods.AddAsync(paymentMethod, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "PaymentMethodCreated",
            EntityType = nameof(PaymentMethod),
            EntityId = paymentMethod.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                paymentMethod.Id,
                paymentMethod.Name,
                paymentMethod.Code,
                paymentMethod.Type,
                paymentMethod.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Payment method '{Name}' ({Code}) created by user {UserId}",
            paymentMethod.Name, paymentMethod.Code, createdByUserId);

        OnPaymentMethodsChanged();
        return paymentMethod;
    }

    /// <inheritdoc />
    public async Task<PaymentMethod> UpdateAsync(int id, PaymentMethodDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Code);

        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (paymentMethod is null)
        {
            throw new InvalidOperationException($"Payment method with ID {id} not found.");
        }

        // Validate code uniqueness
        if (!await IsCodeUniqueAsync(dto.Code, id, cancellationToken))
        {
            throw new InvalidOperationException($"A payment method with code '{dto.Code}' already exists.");
        }

        var oldValues = new
        {
            paymentMethod.Name,
            paymentMethod.Code,
            paymentMethod.Type,
            paymentMethod.IsActive,
            paymentMethod.RequiresReference
        };

        paymentMethod.Name = dto.Name.Trim();
        paymentMethod.Code = dto.Code.Trim().ToUpperInvariant();
        paymentMethod.Type = dto.Type;
        paymentMethod.Description = dto.Description?.Trim();
        paymentMethod.IsActive = dto.IsActive;
        paymentMethod.RequiresReference = dto.RequiresReference;
        paymentMethod.ReferenceLabel = dto.ReferenceLabel?.Trim();
        paymentMethod.ReferenceMinLength = dto.ReferenceMinLength;
        paymentMethod.ReferenceMaxLength = dto.ReferenceMaxLength;
        paymentMethod.SupportsChange = dto.SupportsChange;
        paymentMethod.OpensDrawer = dto.OpensDrawer;
        paymentMethod.DisplayOrder = dto.DisplayOrder;
        paymentMethod.IconPath = dto.IconPath?.Trim();
        paymentMethod.BackgroundColor = dto.BackgroundColor?.Trim();
        paymentMethod.ModifiedAt = DateTime.UtcNow;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "PaymentMethodUpdated",
            EntityType = nameof(PaymentMethod),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                paymentMethod.Name,
                paymentMethod.Code,
                paymentMethod.Type,
                paymentMethod.IsActive,
                paymentMethod.RequiresReference
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Payment method '{Name}' ({Code}) updated by user {UserId}",
            paymentMethod.Name, paymentMethod.Code, modifiedByUserId);

        OnPaymentMethodsChanged();
        return paymentMethod;
    }

    /// <inheritdoc />
    public async Task<PaymentMethod> ToggleActiveAsync(int id, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (paymentMethod is null)
        {
            throw new InvalidOperationException($"Payment method with ID {id} not found.");
        }

        var oldActive = paymentMethod.IsActive;
        paymentMethod.IsActive = !paymentMethod.IsActive;
        paymentMethod.ModifiedAt = DateTime.UtcNow;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = paymentMethod.IsActive ? "PaymentMethodActivated" : "PaymentMethodDeactivated",
            EntityType = nameof(PaymentMethod),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = oldActive }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { paymentMethod.Name, paymentMethod.IsActive }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Payment method '{Name}' {Action} by user {UserId}",
            paymentMethod.Name, paymentMethod.IsActive ? "activated" : "deactivated", modifiedByUserId);

        OnPaymentMethodsChanged();
        return paymentMethod;
    }

    /// <inheritdoc />
    public async Task ReorderAsync(IEnumerable<PaymentMethodOrderDto> orderings, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderings);

        var orderingsList = orderings.ToList();
        if (orderingsList.Count == 0)
        {
            return;
        }

        var paymentMethodIds = orderingsList.Select(o => o.PaymentMethodId).ToList();
        var paymentMethods = await _context.PaymentMethods
            .Where(pm => paymentMethodIds.Contains(pm.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changes = new List<object>();

        foreach (var ordering in orderingsList)
        {
            var paymentMethod = paymentMethods.FirstOrDefault(pm => pm.Id == ordering.PaymentMethodId);
            if (paymentMethod is null)
            {
                continue;
            }

            if (paymentMethod.DisplayOrder != ordering.DisplayOrder)
            {
                changes.Add(new
                {
                    ordering.PaymentMethodId,
                    paymentMethod.Name,
                    OldOrder = paymentMethod.DisplayOrder,
                    NewOrder = ordering.DisplayOrder
                });

                paymentMethod.DisplayOrder = ordering.DisplayOrder;
                paymentMethod.ModifiedAt = DateTime.UtcNow;
            }
        }

        if (changes.Count > 0)
        {
            var auditLog = new AuditLog
            {
                UserId = modifiedByUserId,
                Action = "PaymentMethodsReordered",
                EntityType = nameof(PaymentMethod),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Changes = changes }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.Information("Payment methods reordered by user {UserId}. Changes: {ChangeCount}",
                modifiedByUserId, changes.Count);

            OnPaymentMethodsChanged();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (paymentMethod is null)
        {
            return false;
        }

        // Check for associated payments
        if (await HasPaymentsAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a payment method that has been used for payments. Deactivate it instead.");
        }

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "PaymentMethodDeleted",
            EntityType = nameof(PaymentMethod),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                paymentMethod.Name,
                paymentMethod.Code,
                paymentMethod.Type
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        _context.PaymentMethods.Remove(paymentMethod);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Payment method '{Name}' ({Code}) deleted by user {UserId}",
            paymentMethod.Name, paymentMethod.Code, deletedByUserId);

        OnPaymentMethodsChanged();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var query = _context.PaymentMethods
            .Where(pm => pm.Code == normalizedCode);

        if (excludeId.HasValue)
        {
            query = query.Where(pm => pm.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasPaymentsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AnyAsync(p => p.PaymentMethodId == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Raises the PaymentMethodsChanged event.
    /// </summary>
    protected virtual void OnPaymentMethodsChanged()
    {
        PaymentMethodsChanged?.Invoke(this, EventArgs.Empty);
    }
}
