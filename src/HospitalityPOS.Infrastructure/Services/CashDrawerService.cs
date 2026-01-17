using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Printing;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for cash drawer operations with logging and printer integration.
/// </summary>
public class CashDrawerService : ICashDrawerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPrinterCommunicationService _printerCommunicationService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<CashDrawerService> _logger;

    /// <inheritdoc />
    public event EventHandler<CashDrawerEventArgs>? DrawerOpened;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashDrawerService"/> class.
    /// </summary>
    public CashDrawerService(
        IServiceScopeFactory scopeFactory,
        IPrinterCommunicationService printerCommunicationService,
        ISessionService sessionService,
        ILogger<CashDrawerService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _printerCommunicationService = printerCommunicationService ?? throw new ArgumentNullException(nameof(printerCommunicationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> OpenDrawerAsync(string reason)
    {
        var drawer = await GetDefaultDrawerAsync();
        if (drawer == null)
        {
            _logger.LogWarning("No default cash drawer configured");
            return false;
        }

        var openReason = ParseOpenReason(reason);
        return await OpenDrawerAsync(drawer.Id, openReason, notes: reason);
    }

    /// <inheritdoc />
    public async Task<bool> OpenDrawerAsync(
        int drawerId,
        CashDrawerOpenReason reason,
        string? reference = null,
        string? notes = null,
        int? authorizedByUserId = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var drawer = await context.CashDrawers
            .Include(d => d.LinkedPrinter)
            .FirstOrDefaultAsync(d => d.Id == drawerId && d.IsActive);

        if (drawer == null)
        {
            _logger.LogWarning("Cash drawer {DrawerId} not found or inactive", drawerId);
            return false;
        }

        if (drawer.LinkedPrinter == null)
        {
            _logger.LogWarning("Cash drawer {DrawerId} has no linked printer", drawerId);
            return false;
        }

        // Generate drawer kick command based on pin configuration
        var command = GetDrawerKickCommand(drawer.DrawerPin);
        var printerName = drawer.LinkedPrinter.WindowsPrinterName ?? drawer.LinkedPrinter.Name;

        // Send command to printer
        var success = await _printerCommunicationService.SendRawDataAsync(printerName, command, "Cash Drawer");

        // Create log entry
        var log = new CashDrawerLog
        {
            CashDrawerId = drawerId,
            UserId = _sessionService.CurrentUserId,
            Reason = reason,
            Reference = reference,
            Notes = notes,
            OpenedAt = DateTime.UtcNow,
            AuthorizedByUserId = authorizedByUserId,
            Success = success,
            ErrorMessage = success ? null : "Failed to send command to printer"
        };

        context.CashDrawerLogs.Add(log);

        if (success)
        {
            // Update drawer status
            drawer.Status = CashDrawerStatus.Open;
            drawer.LastOpenedAt = DateTime.UtcNow;
            drawer.LastOpenedByUserId = _sessionService.CurrentUserId;
        }

        await context.SaveChangesAsync();

        // Raise event
        var args = new CashDrawerEventArgs
        {
            DrawerId = drawerId,
            Reason = reason.ToString(),
            ReasonType = reason,
            ReceiptNumber = reference,
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = _sessionService.CurrentUserId,
            Success = success
        };

        OnDrawerOpened(args);

        if (success)
        {
            _logger.LogInformation(
                "Cash drawer {DrawerId} opened by user {UserId}. Reason: {Reason}, Reference: {Reference}",
                drawerId, _sessionService.CurrentUserId, reason, reference ?? "N/A");
        }
        else
        {
            _logger.LogError(
                "Failed to open cash drawer {DrawerId}. Reason: {Reason}",
                drawerId, reason);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> OpenDrawerForPaymentAsync(string receiptNumber, string paymentMethodCode, decimal amount)
    {
        // Only open for cash payments
        if (!paymentMethodCode.Equals("CASH", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Not a cash payment, no drawer open needed
        }

        var drawer = await GetDefaultDrawerAsync();
        if (drawer == null || !drawer.AutoOpenOnCashPayment)
        {
            return true; // No drawer or auto-open disabled
        }

        var success = await OpenDrawerAsync(
            drawer.Id,
            CashDrawerOpenReason.CashPayment,
            reference: receiptNumber);

        if (success)
        {
            _logger.LogInformation(
                "Cash drawer opened for payment. Receipt: {ReceiptNumber}, Amount: {Amount:N2}",
                receiptNumber, amount);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> IsDrawerReadyAsync()
    {
        var drawer = await GetDefaultDrawerAsync();
        if (drawer == null)
        {
            return false;
        }

        // Check if printer is ready
        if (drawer.LinkedPrinter != null)
        {
            var printerName = drawer.LinkedPrinter.WindowsPrinterName ?? drawer.LinkedPrinter.Name;
            return await _printerCommunicationService.IsPrinterReadyAsync(printerName);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<CashDrawer?> GetDefaultDrawerAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        return await context.CashDrawers
            .Include(d => d.LinkedPrinter)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Id)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<List<CashDrawer>> GetAllDrawersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        return await context.CashDrawers
            .Include(d => d.LinkedPrinter)
            .Include(d => d.LastOpenedByUser)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CashDrawer?> GetDrawerAsync(int drawerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        return await context.CashDrawers
            .Include(d => d.LinkedPrinter)
            .Include(d => d.LastOpenedByUser)
            .FirstOrDefaultAsync(d => d.Id == drawerId);
    }

    /// <inheritdoc />
    public async Task<CashDrawer> CreateDrawerAsync(CashDrawer drawer)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        drawer.CreatedAt = DateTime.UtcNow;
        drawer.UpdatedAt = DateTime.UtcNow;

        context.CashDrawers.Add(drawer);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created cash drawer: {DrawerName} (ID: {DrawerId})", drawer.Name, drawer.Id);

        return drawer;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDrawerAsync(CashDrawer drawer)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var existing = await context.CashDrawers.FindAsync(drawer.Id);
        if (existing == null)
        {
            return false;
        }

        existing.Name = drawer.Name;
        existing.LinkedPrinterId = drawer.LinkedPrinterId;
        existing.DrawerPin = drawer.DrawerPin;
        existing.AutoOpenOnCashPayment = drawer.AutoOpenOnCashPayment;
        existing.AutoOpenOnCashRefund = drawer.AutoOpenOnCashRefund;
        existing.AutoOpenOnDrawerCount = drawer.AutoOpenOnDrawerCount;
        existing.IsActive = drawer.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated cash drawer: {DrawerName} (ID: {DrawerId})", drawer.Name, drawer.Id);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDrawerAsync(int drawerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var drawer = await context.CashDrawers.FindAsync(drawerId);
        if (drawer == null)
        {
            return false;
        }

        // Soft delete
        drawer.IsActive = false;
        drawer.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted cash drawer: {DrawerName} (ID: {DrawerId})", drawer.Name, drawerId);

        return true;
    }

    /// <inheritdoc />
    public async Task<CashDrawerStatus> GetStatusAsync(int drawerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var drawer = await context.CashDrawers.FindAsync(drawerId);
        return drawer?.Status ?? CashDrawerStatus.Unknown;
    }

    /// <inheritdoc />
    public async Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime date)
    {
        return await GetLogsAsync(drawerId, date.Date, date.Date.AddDays(1).AddTicks(-1));
    }

    /// <inheritdoc />
    public async Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime startDate, DateTime endDate)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        return await context.CashDrawerLogs
            .Include(l => l.User)
            .Include(l => l.AuthorizedByUser)
            .Where(l => l.CashDrawerId == drawerId)
            .Where(l => l.OpenedAt >= startDate && l.OpenedAt <= endDate)
            .OrderByDescending(l => l.OpenedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> TestDrawerAsync(int drawerId)
    {
        _logger.LogInformation("Testing cash drawer {DrawerId}", drawerId);
        return await OpenDrawerAsync(drawerId, CashDrawerOpenReason.Test, notes: "Test open");
    }

    /// <summary>
    /// Gets the ESC/POS drawer kick command for the specified pin.
    /// </summary>
    private static byte[] GetDrawerKickCommand(CashDrawerPin pin)
    {
        return pin switch
        {
            CashDrawerPin.Pin2 => EscPosCommands.OpenCashDrawer,
            CashDrawerPin.Pin5 => EscPosCommands.OpenCashDrawer2,
            _ => EscPosCommands.OpenCashDrawer
        };
    }

    /// <summary>
    /// Parses a reason string to an enum value.
    /// </summary>
    private static CashDrawerOpenReason ParseOpenReason(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return CashDrawerOpenReason.Other;

        var lower = reason.ToLowerInvariant();

        if (lower.Contains("payment") || lower.Contains("cash"))
            return CashDrawerOpenReason.CashPayment;
        if (lower.Contains("refund"))
            return CashDrawerOpenReason.CashRefund;
        if (lower.Contains("count") || lower.Contains("drawer count"))
            return CashDrawerOpenReason.DrawerCount;
        if (lower.Contains("float") || lower.Contains("opening"))
            return CashDrawerOpenReason.OpeningFloat;
        if (lower.Contains("closing") || lower.Contains("end of day"))
            return CashDrawerOpenReason.ClosingCount;
        if (lower.Contains("drop"))
            return CashDrawerOpenReason.CashDrop;
        if (lower.Contains("petty"))
            return CashDrawerOpenReason.PettyCash;
        if (lower.Contains("manual"))
            return CashDrawerOpenReason.ManualOpen;
        if (lower.Contains("test"))
            return CashDrawerOpenReason.Test;

        return CashDrawerOpenReason.Other;
    }

    /// <summary>
    /// Raises the DrawerOpened event.
    /// </summary>
    protected virtual void OnDrawerOpened(CashDrawerEventArgs e)
    {
        DrawerOpened?.Invoke(this, e);
    }
}
