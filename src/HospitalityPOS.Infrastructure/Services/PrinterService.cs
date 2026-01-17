using System.Drawing.Printing;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing printer configurations.
/// </summary>
public class PrinterService : IPrinterService
{
    private readonly POSDbContext _context;
    private readonly IPrinterDiscoveryService _discoveryService;
    private readonly ILogger _logger;

    public PrinterService(
        POSDbContext context,
        IPrinterDiscoveryService discoveryService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<Printer>> GetPrintersAsync(PrinterType type, CancellationToken cancellationToken = default)
    {
        return await _context.Printers
            .Include(p => p.Settings)
            .Where(p => p.Type == type && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Printer>> GetAllPrintersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Printers
            .Include(p => p.Settings)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Type)
            .ThenByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Printer?> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Printers
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Printer?> GetDefaultPrinterAsync(PrinterType type, CancellationToken cancellationToken = default)
    {
        return await _context.Printers
            .Include(p => p.Settings)
            .Where(p => p.Type == type && p.IsDefault && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Printer> SavePrinterAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        if (printer.Id == 0)
        {
            // New printer
            printer.CreatedAt = DateTime.UtcNow;
            _context.Printers.Add(printer);
            _logger.Information("Creating new printer: {PrinterName}", printer.Name);
        }
        else
        {
            // Update existing
            printer.UpdatedAt = DateTime.UtcNow;
            _context.Printers.Update(printer);
            _logger.Information("Updating printer: {PrinterId} - {PrinterName}", printer.Id, printer.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return printer;
    }

    /// <inheritdoc />
    public async Task SetDefaultPrinterAsync(int printerId, PrinterType type, CancellationToken cancellationToken = default)
    {
        // Clear existing defaults for this type
        var existingDefaults = await _context.Printers
            .Where(p => p.Type == type && p.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var printer in existingDefaults)
        {
            printer.IsDefault = false;
            printer.UpdatedAt = DateTime.UtcNow;
        }

        // Set new default
        var newDefault = await _context.Printers
            .FirstOrDefaultAsync(p => p.Id == printerId, cancellationToken);

        if (newDefault != null)
        {
            newDefault.IsDefault = true;
            newDefault.UpdatedAt = DateTime.UtcNow;
            _logger.Information("Set printer {PrinterId} as default for type {Type}", printerId, type);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeletePrinterAsync(int id, CancellationToken cancellationToken = default)
    {
        var printer = await _context.Printers
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (printer != null)
        {
            printer.IsActive = false;
            printer.UpdatedAt = DateTime.UtcNow;
            _logger.Information("Deleted printer: {PrinterId} - {PrinterName}", id, printer.Name);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<PrintTestResult> TestPrintAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Testing printer: {PrinterName} ({ConnectionType})",
                printer.Name, printer.ConnectionType);

            // First test the connection
            var isConnected = await _discoveryService.TestConnectionAsync(printer);
            if (!isConnected)
            {
                return PrintTestResult.Failed("Printer is not connected or not responding.");
            }

            // Generate test page content
            var testContent = GenerateTestPageContent(printer);

            // Send to printer based on connection type
            switch (printer.ConnectionType)
            {
                case PrinterConnectionType.WindowsDriver:
                    return await PrintViaWindowsDriverAsync(printer, testContent);

                case PrinterConnectionType.Network:
                    return await PrintViaNetworkAsync(printer, testContent);

                case PrinterConnectionType.Serial:
                    return PrintTestResult.Failed("Serial port printing not yet implemented.");

                case PrinterConnectionType.USB:
                    return PrintTestResult.Failed("USB direct printing not yet implemented.");

                default:
                    return PrintTestResult.Failed("Unknown connection type.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing printer {PrinterName}", printer.Name);
            return PrintTestResult.Failed(ex.Message);
        }
    }

    private byte[] GenerateTestPageContent(Printer printer)
    {
        var commands = new List<byte>();
        var charsPerLine = printer.CharsPerLine > 0 ? printer.CharsPerLine : 48;

        // ESC/POS Initialize
        commands.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @

        // Center alignment
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1

        // Bold on
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // ESC E 1
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("PRINTER TEST PAGE\n"));
        // Bold off
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // ESC E 0

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n\n"));

        // Left alignment
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // ESC a 0

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes($"Printer: {printer.Name}\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes($"Connection: {printer.ConnectionType}\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes($"Paper Width: {printer.PaperWidth}mm ({charsPerLine} chars)\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n"));

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("FONT STYLES TEST\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Normal text\n"));

        // Bold
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Bold text\n"));
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

        // Double width
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x10 }); // GS ! 16
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("DOUBLE WIDTH\n"));
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // GS ! 0

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("\n" + new string('-', charsPerLine) + "\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("ALIGNMENT TEST\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));

        // Left
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Left aligned\n"));

        // Right
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x02 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Right aligned\n"));

        // Center
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Centered\n\n"));

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("CURRENCY TEST\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));

        commands.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Amount: KSh 1,234.56\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("VAT 16%: KSh 197.53\n\n"));

        // Center for footer
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n"));

        commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("TEST COMPLETE\n"));
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("Printer is working correctly!\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n\n\n"));

        // Feed and cut
        if (printer.Settings?.AutoCut == true)
        {
            // Feed lines
            var feedLines = printer.Settings?.CutFeedLines ?? 3;
            for (int i = 0; i < feedLines; i++)
            {
                commands.Add(0x0A); // Line feed
            }

            // Cut paper
            if (printer.Settings?.PartialCut == true)
            {
                commands.AddRange(new byte[] { 0x1D, 0x56, 0x01 }); // Partial cut
            }
            else
            {
                commands.AddRange(new byte[] { 0x1D, 0x56, 0x00 }); // Full cut
            }
        }

        return commands.ToArray();
    }

    private Task<PrintTestResult> PrintViaWindowsDriverAsync(Printer printer, byte[] content)
    {
        try
        {
            if (string.IsNullOrEmpty(printer.WindowsPrinterName))
            {
                return Task.FromResult(PrintTestResult.Failed("Windows printer name is not configured."));
            }

#if WINDOWS
            // For Windows driver, we use the PrintDocument class
            // This is a simplified implementation - in production, you'd use RawPrinterHelper
            var printerSettings = new System.Drawing.Printing.PrinterSettings
            {
                PrinterName = printer.WindowsPrinterName
            };

            if (!printerSettings.IsValid)
            {
                return Task.FromResult(PrintTestResult.Failed($"Printer '{printer.WindowsPrinterName}' not found."));
            }

            // For ESC/POS printers, we need to send raw bytes
            // Using RawPrinterHelper pattern
            var success = RawPrinterHelper.SendBytesToPrinter(printer.WindowsPrinterName, content);

            if (success)
            {
                _logger.Information("Test print successful for {PrinterName}", printer.Name);
                return Task.FromResult(PrintTestResult.Successful("Test page printed successfully."));
            }
            else
            {
                return Task.FromResult(PrintTestResult.Failed("Failed to send data to printer."));
            }
#else
            _logger.Warning("Windows driver printing is only available on Windows platform");
            return Task.FromResult(PrintTestResult.Failed("Windows driver printing is only available on Windows platform."));
#endif
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing via Windows driver");
            return Task.FromResult(PrintTestResult.Failed(ex.Message));
        }
    }

    private async Task<PrintTestResult> PrintViaNetworkAsync(Printer printer, byte[] content)
    {
        const int ConnectionTimeoutMs = 5000;
        const int WriteTimeoutMs = 10000;

        try
        {
            if (string.IsNullOrEmpty(printer.IpAddress))
            {
                return PrintTestResult.Failed("IP address is not configured.");
            }

            var port = printer.Port ?? 9100;

            using var client = new TcpClient();

            // Use timeout for connection to avoid hanging indefinitely
            using var connectionCts = new CancellationTokenSource(ConnectionTimeoutMs);
            try
            {
                await client.ConnectAsync(printer.IpAddress, port, connectionCts.Token);
            }
            catch (OperationCanceledException)
            {
                return PrintTestResult.Failed($"Connection to {printer.IpAddress}:{port} timed out after {ConnectionTimeoutMs / 1000} seconds.");
            }

            using var stream = client.GetStream();
            stream.WriteTimeout = WriteTimeoutMs;

            // Use Memory<byte> overload instead of deprecated offset/length overload
            await stream.WriteAsync(content.AsMemory());
            await stream.FlushAsync();

            _logger.Information("Test print sent to network printer {IpAddress}:{Port}",
                printer.IpAddress, port);
            return PrintTestResult.Successful("Test page sent to printer.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing via network to {IpAddress}", printer.IpAddress);
            return PrintTestResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<PrinterStatusResult> CheckPrinterStatusAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _discoveryService.TestConnectionAsync(printer);

            // Update the printer status in the database
            printer.Status = isConnected ? PrinterStatus.Online : PrinterStatus.Offline;
            printer.LastStatusCheck = DateTime.UtcNow;
            printer.LastError = isConnected ? null : "Printer not responding";
            printer.UpdatedAt = DateTime.UtcNow;

            _context.Printers.Update(printer);
            await _context.SaveChangesAsync(cancellationToken);

            return isConnected
                ? PrinterStatusResult.Online()
                : PrinterStatusResult.Offline("Printer not responding");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking printer status for {PrinterName}", printer.Name);
            return PrinterStatusResult.Error(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<ReceiptTemplate>> GetReceiptTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReceiptTemplate?> GetReceiptTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReceiptTemplate?> GetDefaultReceiptTemplateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptTemplates
            .Where(t => t.IsDefault && t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReceiptTemplate> SaveReceiptTemplateAsync(ReceiptTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.Id == 0)
        {
            template.CreatedAt = DateTime.UtcNow;
            _context.ReceiptTemplates.Add(template);
            _logger.Information("Creating new receipt template: {TemplateName}", template.Name);
        }
        else
        {
            template.UpdatedAt = DateTime.UtcNow;
            _context.ReceiptTemplates.Update(template);
            _logger.Information("Updating receipt template: {TemplateId} - {TemplateName}",
                template.Id, template.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    /// <inheritdoc />
    public async Task SetDefaultReceiptTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        // Clear existing defaults
        var existingDefaults = await _context.ReceiptTemplates
            .Where(t => t.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var template in existingDefaults)
        {
            template.IsDefault = false;
            template.UpdatedAt = DateTime.UtcNow;
        }

        // Set new default
        var newDefault = await _context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (newDefault != null)
        {
            newDefault.IsDefault = true;
            newDefault.UpdatedAt = DateTime.UtcNow;
            _logger.Information("Set template {TemplateId} as default", templateId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteReceiptTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await _context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template != null)
        {
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;
            _logger.Information("Deleted receipt template: {TemplateId} - {TemplateName}", id, template.Name);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #region Kitchen Printer Methods

    /// <inheritdoc />
    public async Task<List<PrinterCategoryMapping>> GetCategoryMappingsAsync(int printerId, CancellationToken cancellationToken = default)
    {
        return await _context.PrinterCategoryMappings
            .Include(m => m.Category)
            .Where(m => m.PrinterId == printerId && m.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveCategoryMappingsAsync(int printerId, List<int> categoryIds, CancellationToken cancellationToken = default)
    {
        // Get existing mappings
        var existingMappings = await _context.PrinterCategoryMappings
            .Where(m => m.PrinterId == printerId)
            .ToListAsync(cancellationToken);

        // Remove mappings no longer selected
        var toRemove = existingMappings
            .Where(m => !categoryIds.Contains(m.CategoryId))
            .ToList();
        _context.PrinterCategoryMappings.RemoveRange(toRemove);

        // Add new mappings
        var existingCategoryIds = existingMappings.Select(m => m.CategoryId).ToHashSet();
        foreach (var categoryId in categoryIds.Where(id => !existingCategoryIds.Contains(id)))
        {
            _context.PrinterCategoryMappings.Add(new PrinterCategoryMapping
            {
                PrinterId = printerId,
                CategoryId = categoryId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Reactivate any previously deactivated mappings
        foreach (var mapping in existingMappings.Where(m => categoryIds.Contains(m.CategoryId) && !m.IsActive))
        {
            mapping.IsActive = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.Information("Updated category mappings for printer {PrinterId}: {Count} categories",
            printerId, categoryIds.Count);
    }

    /// <inheritdoc />
    public async Task<KOTSettings?> GetKOTSettingsAsync(int printerId, CancellationToken cancellationToken = default)
    {
        return await _context.KOTSettings
            .FirstOrDefaultAsync(k => k.PrinterId == printerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveKOTSettingsAsync(KOTSettings kotSettings, CancellationToken cancellationToken = default)
    {
        var existing = await _context.KOTSettings
            .FirstOrDefaultAsync(k => k.PrinterId == kotSettings.PrinterId, cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.TitleFontSize = kotSettings.TitleFontSize;
            existing.ItemFontSize = kotSettings.ItemFontSize;
            existing.ModifierFontSize = kotSettings.ModifierFontSize;
            existing.ShowTableNumber = kotSettings.ShowTableNumber;
            existing.ShowWaiterName = kotSettings.ShowWaiterName;
            existing.ShowOrderTime = kotSettings.ShowOrderTime;
            existing.ShowOrderNumber = kotSettings.ShowOrderNumber;
            existing.ShowCategoryHeader = kotSettings.ShowCategoryHeader;
            existing.GroupByCategory = kotSettings.GroupByCategory;
            existing.ShowQuantityLarge = kotSettings.ShowQuantityLarge;
            existing.ShowModifiersIndented = kotSettings.ShowModifiersIndented;
            existing.ShowNotesHighlighted = kotSettings.ShowNotesHighlighted;
            existing.PrintRushOrders = kotSettings.PrintRushOrders;
            existing.HighlightAllergies = kotSettings.HighlightAllergies;
            existing.BeepOnPrint = kotSettings.BeepOnPrint;
            existing.BeepCount = kotSettings.BeepCount;
            existing.CopiesPerOrder = kotSettings.CopiesPerOrder;

            _context.KOTSettings.Update(existing);
            _logger.Information("Updated KOT settings for printer {PrinterId}", kotSettings.PrinterId);
        }
        else
        {
            // Create new
            _context.KOTSettings.Add(kotSettings);
            _logger.Information("Created KOT settings for printer {PrinterId}", kotSettings.PrinterId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PrintTestResult> PrintTestKOTAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Printing test KOT to {PrinterName}", printer.Name);

            // First test the connection
            var isConnected = await _discoveryService.TestConnectionAsync(printer);
            if (!isConnected)
            {
                return PrintTestResult.Failed("Printer is not connected or not responding.");
            }

            // Get KOT settings
            var kotSettings = await GetKOTSettingsAsync(printer.Id, cancellationToken);

            // Generate test KOT content
            var testContent = GenerateTestKOTContent(printer, kotSettings);

            // Send to printer based on connection type
            switch (printer.ConnectionType)
            {
                case PrinterConnectionType.WindowsDriver:
                    return await PrintViaWindowsDriverAsync(printer, testContent);

                case PrinterConnectionType.Network:
                    return await PrintViaNetworkAsync(printer, testContent);

                case PrinterConnectionType.Serial:
                    return PrintTestResult.Failed("Serial port printing not yet implemented.");

                case PrinterConnectionType.USB:
                    return PrintTestResult.Failed("USB direct printing not yet implemented.");

                default:
                    return PrintTestResult.Failed("Unknown connection type.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing test KOT to {PrinterName}", printer.Name);
            return PrintTestResult.Failed(ex.Message);
        }
    }

    private byte[] GenerateTestKOTContent(Printer printer, KOTSettings? kotSettings)
    {
        var commands = new List<byte>();
        var charsPerLine = printer.CharsPerLine > 0 ? printer.CharsPerLine : 48;

        // ESC/POS Initialize
        commands.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @

        // Beep if enabled
        if (kotSettings?.BeepOnPrint == true)
        {
            var beepCount = kotSettings.BeepCount > 0 ? kotSettings.BeepCount : 2;
            for (int i = 0; i < beepCount; i++)
            {
                commands.AddRange(new byte[] { 0x1B, 0x42, 0x02, 0x02 }); // ESC B 2 2
            }
        }

        // Center alignment
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1

        // Double width for title
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x10 }); // GS ! 16
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("** KITCHEN ORDER TICKET **\n"));
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // GS ! 0

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n"));

        // Left alignment for details
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // ESC a 0

        if (kotSettings?.ShowTableNumber != false)
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("TABLE: 07\n"));

        if (kotSettings?.ShowWaiterName != false)
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("WAITER: Test User\n"));

        if (kotSettings?.ShowOrderTime != false)
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes($"TIME: {DateTime.Now:HH:mm}\n"));

        if (kotSettings?.ShowOrderNumber != false)
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("ORDER: O-0042\n"));

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n\n"));

        // Category header
        if (kotSettings?.ShowCategoryHeader != false)
        {
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("** FOOD **\n"));
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
        }

        // Sample items
        if (kotSettings?.ShowQuantityLarge != false)
        {
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x10 }); // Double width for qty
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("2x"));
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Normal
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("  Grilled Chicken\n"));
        }
        else
        {
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("2x  Grilled Chicken\n"));
        }

        // Modifiers
        if (kotSettings?.ShowModifiersIndented != false)
        {
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("    - Extra spicy\n"));
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("    - No onions\n"));
        }

        // Notes
        if (kotSettings?.ShowNotesHighlighted != false)
        {
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("    NOTE: Well done\n"));
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
        }

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("\n"));

        // Another item with allergy
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("1x  Fish & Chips\n"));
        if (kotSettings?.HighlightAllergies != false)
        {
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); // Double height+width
            commands.AddRange(System.Text.Encoding.ASCII.GetBytes("*** GLUTEN FREE ***\n"));
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Normal
        }

        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("\n" + new string('=', charsPerLine) + "\n"));

        // Center for footer
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes("ITEMS: 3              TEST\n"));
        commands.AddRange(System.Text.Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n\n\n"));

        // Feed and cut
        if (printer.Settings?.AutoCut == true)
        {
            var feedLines = printer.Settings?.CutFeedLines ?? 3;
            for (int i = 0; i < feedLines; i++)
            {
                commands.Add(0x0A);
            }
            commands.AddRange(new byte[] { 0x1D, 0x56, 0x01 }); // Partial cut
        }

        return commands.ToArray();
    }

    #endregion
}

#if WINDOWS
/// <summary>
/// Helper class for sending raw bytes to Windows printers.
/// Only available on Windows platform.
/// </summary>
internal static class RawPrinterHelper
{
    [System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private struct DOCINFO
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string pDocName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string? pOutputFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string pDataType;
    }

    public static bool SendBytesToPrinter(string printerName, byte[] bytes)
    {
        IntPtr hPrinter = IntPtr.Zero;
        IntPtr pBytes = IntPtr.Zero;
        bool success = false;

        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                return false;
            }

            var docInfo = new DOCINFO
            {
                pDocName = "POS Receipt",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                ClosePrinter(hPrinter);
                return false;
            }

            if (!StartPagePrinter(hPrinter))
            {
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                return false;
            }

            pBytes = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(bytes.Length);
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, pBytes, bytes.Length);

            success = WritePrinter(hPrinter, pBytes, bytes.Length, out _);

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
        }
        finally
        {
            if (pBytes != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }

        return success;
    }
}
#endif
