using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for routing order items to appropriate kitchen printers.
/// </summary>
public class KitchenOrderRoutingService : IKitchenOrderRoutingService
{
    private readonly POSDbContext _context;
    private readonly IPrinterService _printerService;
    private readonly IPrinterDiscoveryService _discoveryService;
    private readonly ILogger _logger;

    public KitchenOrderRoutingService(
        POSDbContext context,
        IPrinterService printerService,
        IPrinterDiscoveryService discoveryService,
        ILogger logger)
    {
        _context = context;
        _printerService = printerService;
        _discoveryService = discoveryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Printer, List<OrderItem>>> RouteOrderItemsAsync(
        IEnumerable<OrderItem> items,
        CancellationToken cancellationToken = default)
    {
        var routes = new Dictionary<Printer, List<OrderItem>>();

        // Get all kitchen printers with their category mappings
        var kitchenPrinters = await GetKitchenPrintersWithMappingsAsync(cancellationToken);

        // Get default kitchen printer
        var defaultPrinter = kitchenPrinters.FirstOrDefault(p => p.IsDefault)
            ?? kitchenPrinters.FirstOrDefault();

        foreach (var item in items)
        {
            // Find printer for this item's category
            Printer? printer = null;

            if (item.Product?.CategoryId != null)
            {
                printer = kitchenPrinters.FirstOrDefault(p =>
                    p.CategoryMappings.Any(m =>
                        m.CategoryId == item.Product.CategoryId && m.IsActive));
            }

            // Fall back to default if no mapping found
            printer ??= defaultPrinter;

            if (printer != null)
            {
                if (!routes.ContainsKey(printer))
                {
                    routes[printer] = new List<OrderItem>();
                }
                routes[printer].Add(item);
            }
        }

        _logger.Debug("Routed {ItemCount} items to {PrinterCount} printers",
            items.Count(), routes.Count);

        return routes;
    }

    /// <inheritdoc />
    public async Task<KitchenRoutingResult> PrintKOTsAsync(
        int orderId,
        bool isIncremental = false,
        CancellationToken cancellationToken = default)
    {
        var result = new KitchenRoutingResult();

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                result.ErrorMessage = "Order not found.";
                return result;
            }

            // Get items to print (all or only new)
            var itemsToPrint = isIncremental
                ? order.OrderItems.Where(i => !i.PrintedToKitchen).ToList()
                : order.OrderItems.ToList();

            if (!itemsToPrint.Any())
            {
                result.Success = true;
                result.ErrorMessage = "No items to print.";
                return result;
            }

            // Route items to printers
            var routes = await RouteOrderItemsAsync(itemsToPrint, cancellationToken);

            if (!routes.Any())
            {
                result.ErrorMessage = "No kitchen printers configured.";
                return result;
            }

            // Print to each printer
            foreach (var route in routes)
            {
                var printer = route.Key;
                var items = route.Value;

                var routeInfo = new PrinterRouteInfo
                {
                    PrinterName = printer.Name,
                    ItemCount = items.Count
                };

                try
                {
                    // Generate KOT data
                    var kotData = GenerateKOTData(order, items, isIncremental);

                    // Print KOT
                    var printResult = await PrintKOTAsync(printer, kotData, cancellationToken);

                    routeInfo.Printed = printResult.Success;
                    if (!printResult.Success)
                    {
                        routeInfo.Error = printResult.ErrorMessage;
                    }
                }
                catch (Exception ex)
                {
                    routeInfo.Printed = false;
                    routeInfo.Error = ex.Message;
                    _logger.Error(ex, "Error printing KOT to {PrinterName}", printer.Name);
                }

                result.Routes.Add(routeInfo);
            }

            // Mark items as sent to kitchen
            foreach (var item in itemsToPrint)
            {
                item.PrintedToKitchen = true;
            }
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = result.Routes.All(r => r.Printed);
            result.PrinterCount = routes.Count;
            result.ItemCount = itemsToPrint.Count;

            if (!result.Success)
            {
                result.ErrorMessage = "Some printers failed to print. Check individual printer results.";
            }

            _logger.Information("Printed KOTs for order {OrderId}: {Success}, {PrinterCount} printers, {ItemCount} items",
                orderId, result.Success, result.PrinterCount, result.ItemCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing KOTs for order {OrderId}", orderId);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<Printer?> GetPrinterForCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        // Find printer with mapping for this category
        var printer = await _context.Printers
            .Include(p => p.CategoryMappings)
            .Where(p => p.Type == PrinterType.Kitchen && p.IsActive)
            .Where(p => p.CategoryMappings.Any(m => m.CategoryId == categoryId && m.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        // Fall back to default kitchen printer
        if (printer == null)
        {
            printer = await _context.Printers
                .Where(p => p.Type == PrinterType.Kitchen && p.IsActive && p.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return printer;
    }

    /// <inheritdoc />
    public KOTData GenerateKOTData(Order order, IEnumerable<OrderItem> items, bool isIncremental)
    {
        var kotData = new KOTData
        {
            OrderNumber = order.OrderNumber,
            TableNumber = order.TableNumber,
            WaiterName = order.User?.FullName,
            OrderTime = order.CreatedAt,
            IsIncremental = isIncremental,
            BatchNumber = items.FirstOrDefault()?.BatchNumber ?? 1,
            Items = items.Select(i => new KOTItemData
            {
                ProductName = i.Product?.Name ?? "Unknown",
                Quantity = i.Quantity,
                CategoryName = i.Product?.Category?.Name,
                Modifiers = ParseModifiers(i.Modifiers),
                Notes = i.Notes,
                IsVoided = false // OrderItem doesn't have IsVoided, but we can track this separately
            }).ToList()
        };

        return kotData;
    }

    /// <inheritdoc />
    public async Task<PrintTestResult> PrintKOTAsync(
        Printer printer,
        KOTData kotData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First test the connection
            var isConnected = await _discoveryService.TestConnectionAsync(printer);
            if (!isConnected)
            {
                return PrintTestResult.Failed("Printer is not connected or not responding.");
            }

            // Get KOT settings
            var kotSettings = await _printerService.GetKOTSettingsAsync(printer.Id, cancellationToken);

            // Generate ESC/POS content
            var content = GenerateKOTContent(printer, kotData, kotSettings);

            // Print copies
            var copies = kotSettings?.CopiesPerOrder ?? 1;
            for (int i = 0; i < copies; i++)
            {
                // Send to printer based on connection type
                PrintTestResult printResult;
                switch (printer.ConnectionType)
                {
                    case PrinterConnectionType.WindowsDriver:
                        printResult = await PrintViaWindowsDriverAsync(printer, content);
                        break;

                    case PrinterConnectionType.Network:
                        printResult = await PrintViaNetworkAsync(printer, content);
                        break;

                    case PrinterConnectionType.Serial:
                        return PrintTestResult.Failed("Serial port printing not yet implemented.");

                    case PrinterConnectionType.USB:
                        return PrintTestResult.Failed("USB direct printing not yet implemented.");

                    default:
                        return PrintTestResult.Failed("Unknown connection type.");
                }

                if (!printResult.Success)
                {
                    return printResult;
                }
            }

            _logger.Information("KOT printed to {PrinterName}: Order {OrderNumber}",
                printer.Name, kotData.OrderNumber);
            return PrintTestResult.Successful("KOT printed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing KOT to {PrinterName}", printer.Name);
            return PrintTestResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<Printer>> GetKitchenPrintersWithMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Printers
            .Include(p => p.CategoryMappings)
                .ThenInclude(m => m.Category)
            .Include(p => p.Settings)
            .Include(p => p.KOTSettings)
            .Where(p => p.Type == PrinterType.Kitchen && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    private byte[] GenerateKOTContent(Printer printer, KOTData kotData, KOTSettings? kotSettings)
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

        // Title
        ApplyFontSize(commands, kotSettings?.TitleFontSize ?? KOTFontSize.Large);
        commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on

        if (kotData.IsIncremental)
        {
            commands.AddRange(Encoding.ASCII.GetBytes("** ORDER ADDITION **\n"));
        }
        else
        {
            commands.AddRange(Encoding.ASCII.GetBytes("** KITCHEN ORDER **\n"));
        }

        commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Reset font size

        commands.AddRange(Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n"));

        // Left alignment for details
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // ESC a 0

        // Order details based on settings
        if (kotSettings?.ShowTableNumber != false && !string.IsNullOrEmpty(kotData.TableNumber))
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"TABLE: {kotData.TableNumber}\n"));
        }

        if (kotSettings?.ShowWaiterName != false && !string.IsNullOrEmpty(kotData.WaiterName))
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"WAITER: {kotData.WaiterName}\n"));
        }

        if (kotSettings?.ShowOrderTime != false)
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"TIME: {kotData.OrderTime:HH:mm}\n"));
        }

        if (kotSettings?.ShowOrderNumber != false)
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"ORDER: {kotData.OrderNumber}\n"));
        }

        if (kotData.IsIncremental)
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"BATCH: #{kotData.BatchNumber}\n"));
        }

        commands.AddRange(Encoding.ASCII.GetBytes(new string('-', charsPerLine) + "\n"));

        // Print items, grouped by category if enabled
        if (kotSettings?.GroupByCategory == true)
        {
            var groupedItems = kotData.Items.GroupBy(i => i.CategoryName ?? "Other");
            foreach (var group in groupedItems)
            {
                if (kotSettings?.ShowCategoryHeader == true)
                {
                    commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on
                    commands.AddRange(Encoding.ASCII.GetBytes($"** {group.Key.ToUpperInvariant()} **\n"));
                    commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
                }

                foreach (var item in group)
                {
                    PrintKOTItem(commands, item, kotSettings, charsPerLine);
                }
                commands.AddRange(Encoding.ASCII.GetBytes("\n"));
            }
        }
        else
        {
            foreach (var item in kotData.Items)
            {
                PrintKOTItem(commands, item, kotSettings, charsPerLine);
            }
        }

        commands.AddRange(Encoding.ASCII.GetBytes(new string('=', charsPerLine) + "\n"));

        // Center alignment for footer
        commands.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1

        var footer = kotData.IsIncremental ? "ADDITION" : "NEW ORDER";
        commands.AddRange(Encoding.ASCII.GetBytes($"ITEMS: {kotData.TotalItemCount}              {footer}\n"));

        // Feed and cut
        commands.AddRange(Encoding.ASCII.GetBytes("\n\n\n"));
        if (printer.Settings?.AutoCut == true)
        {
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

    private void PrintKOTItem(List<byte> commands, KOTItemData item, KOTSettings? kotSettings, int charsPerLine)
    {
        // Quantity and product name
        ApplyFontSize(commands, kotSettings?.ItemFontSize ?? KOTFontSize.Normal);

        if (kotSettings?.ShowQuantityLarge == true)
        {
            // Large quantity
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x10 }); // GS ! 16 (Double width)
            commands.AddRange(Encoding.ASCII.GetBytes($"{(int)item.Quantity}x "));
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Reset
            ApplyFontSize(commands, kotSettings?.ItemFontSize ?? KOTFontSize.Normal);
        }
        else
        {
            commands.AddRange(Encoding.ASCII.GetBytes($"{(int)item.Quantity}x "));
        }

        commands.AddRange(Encoding.ASCII.GetBytes($"{item.ProductName}\n"));
        commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Reset font size

        // Modifiers
        if (item.Modifiers.Any())
        {
            ApplyFontSize(commands, kotSettings?.ModifierFontSize ?? KOTFontSize.Small);
            foreach (var modifier in item.Modifiers)
            {
                var prefix = kotSettings?.ShowModifiersIndented == true ? "    - " : "- ";
                commands.AddRange(Encoding.ASCII.GetBytes($"{prefix}{modifier}\n"));
            }
            commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // Reset
        }

        // Notes
        if (!string.IsNullOrEmpty(item.Notes))
        {
            if (kotSettings?.ShowNotesHighlighted == true)
            {
                commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on
                commands.AddRange(Encoding.ASCII.GetBytes($"    NOTE: {item.Notes}\n"));
                commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
            }
            else
            {
                commands.AddRange(Encoding.ASCII.GetBytes($"    Note: {item.Notes}\n"));
            }
        }

        // Allergy info
        if (!string.IsNullOrEmpty(item.AllergyInfo) && kotSettings?.HighlightAllergies == true)
        {
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // Bold on
            commands.AddRange(new byte[] { 0x1D, 0x42, 0x01 }); // Reverse print on
            commands.AddRange(Encoding.ASCII.GetBytes($" *** {item.AllergyInfo.ToUpperInvariant()} *** \n"));
            commands.AddRange(new byte[] { 0x1D, 0x42, 0x00 }); // Reverse print off
            commands.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // Bold off
        }
    }

    private void ApplyFontSize(List<byte> commands, KOTFontSize fontSize)
    {
        switch (fontSize)
        {
            case KOTFontSize.Small:
                // Normal size (no scaling)
                commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // GS ! 0
                break;
            case KOTFontSize.Normal:
                // Normal size
                commands.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // GS ! 0
                break;
            case KOTFontSize.Large:
                // Double width
                commands.AddRange(new byte[] { 0x1D, 0x21, 0x10 }); // GS ! 16
                break;
            case KOTFontSize.ExtraLarge:
                // Double width and height
                commands.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); // GS ! 17
                break;
        }
    }

    private List<string> ParseModifiers(string? modifiersString)
    {
        if (string.IsNullOrEmpty(modifiersString))
        {
            return new List<string>();
        }

        // Modifiers are stored as comma-separated or JSON array
        if (modifiersString.StartsWith("["))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(modifiersString)
                    ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return modifiersString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    private async Task<PrintTestResult> PrintViaWindowsDriverAsync(Printer printer, byte[] content)
    {
        try
        {
            if (string.IsNullOrEmpty(printer.WindowsPrinterName))
            {
                return PrintTestResult.Failed("Windows printer name is not configured.");
            }

            // Use RawPrinterHelper for ESC/POS printers
            var success = RawPrinterHelper.SendBytesToPrinter(printer.WindowsPrinterName, content);

            if (success)
            {
                return PrintTestResult.Successful("Printed successfully.");
            }
            else
            {
                return PrintTestResult.Failed("Failed to send data to printer.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing via Windows driver");
            return PrintTestResult.Failed(ex.Message);
        }
    }

    private async Task<PrintTestResult> PrintViaNetworkAsync(Printer printer, byte[] content)
    {
        try
        {
            if (string.IsNullOrEmpty(printer.IpAddress))
            {
                return PrintTestResult.Failed("IP address is not configured.");
            }

            var port = printer.Port ?? 9100;

            using var client = new TcpClient();
            await client.ConnectAsync(printer.IpAddress, port);

            using var stream = client.GetStream();
            await stream.WriteAsync(content, 0, content.Length);
            await stream.FlushAsync();

            return PrintTestResult.Successful("Printed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing via network");
            return PrintTestResult.Failed(ex.Message);
        }
    }
}
