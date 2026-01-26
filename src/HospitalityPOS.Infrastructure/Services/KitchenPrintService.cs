using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Printing;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for printing Kitchen Order Tickets (KOT) to thermal printers.
/// Uses ESC/POS commands for direct printer communication.
/// </summary>
public class KitchenPrintService : IKitchenPrintService
{
    private readonly ILogger _logger;
    private readonly IPrinterCommunicationService _printerComm;
    private readonly IPrinterService _printerService;

    /// <summary>
    /// Standard kitchen stations.
    /// </summary>
    private static readonly string[] StandardStations =
    [
        "KITCHEN",
        "BAR",
        "COLD STATION",
        "PASTRY",
        "GENERAL"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="KitchenPrintService"/> class.
    /// </summary>
    public KitchenPrintService(
        ILogger logger,
        IPrinterCommunicationService printerComm,
        IPrinterService printerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _printerComm = printerComm ?? throw new ArgumentNullException(nameof(printerComm));
        _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
    }

    /// <inheritdoc />
    public async Task<bool> PrintKotAsync(Order order, IEnumerable<OrderItem> items)
    {
        var itemsList = items.ToList();
        if (!itemsList.Any())
        {
            _logger.Warning("No items to print for order {OrderNumber}", order.OrderNumber);
            return false;
        }

        _logger.Information(
            "Printing KOT for order {OrderNumber}: {ItemCount} items, Table: {Table}, Server: {Server}",
            order.OrderNumber,
            itemsList.Count,
            order.TableNumber ?? "N/A",
            order.User?.FullName ?? "Unknown");

        // Group items by station
        var groupedItems = itemsList.GroupBy(i =>
            string.IsNullOrEmpty(i.Product?.KitchenStation) ? "GENERAL" : i.Product.KitchenStation.ToUpperInvariant());

        var allSuccess = true;

        foreach (var stationGroup in groupedItems)
        {
            var success = await PrintStationKotAsync(order, stationGroup.Key, stationGroup.ToList());
            if (!success)
            {
                allSuccess = false;
                _logger.Warning("Failed to print KOT for station {Station}", stationGroup.Key);
            }
        }

        return allSuccess;
    }

    /// <summary>
    /// Prints KOT for a specific kitchen station.
    /// </summary>
    private async Task<bool> PrintStationKotAsync(Order order, string station, List<OrderItem> items)
    {
        try
        {
            // Get the kitchen printer for this station (or default kitchen printer)
            var printer = await GetKitchenPrinterForStationAsync(station);
            if (printer == null)
            {
                _logger.Warning("No kitchen printer configured for station {Station}, using default", station);
                printer = await _printerService.GetDefaultPrinterAsync(PrinterType.Kitchen);
            }

            if (printer == null)
            {
                _logger.Error("No kitchen printer available for station {Station}", station);
                return false;
            }

            // Build the KOT document
            var document = BuildKotDocument(order, station, items);
            var data = document.Build();

            // Send to printer
            var result = await _printerComm.SendRawDataAsync(printer.PrinterName, data, $"KOT-{order.OrderNumber}");

            if (result)
            {
                _logger.Information(
                    "KOT printed successfully for order {OrderNumber} at station {Station} ({ItemCount} items)",
                    order.OrderNumber, station, items.Count);
            }
            else
            {
                _logger.Error(
                    "Failed to send KOT to printer {PrinterName} for order {OrderNumber}",
                    printer.PrinterName, order.OrderNumber);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing KOT for station {Station}, order {OrderNumber}",
                station, order.OrderNumber);
            return false;
        }
    }

    /// <summary>
    /// Builds the ESC/POS document for a KOT.
    /// </summary>
    private static EscPosPrintDocument BuildKotDocument(Order order, string station, List<OrderItem> items)
    {
        var doc = EscPosPrintDocument.Create80mm()
            .Initialize();

        // Header with station name
        doc.AlignCenter()
            .DoubleSize()
            .Bold()
            .TextLine($"** {station} **")
            .NoBold()
            .NormalSize()
            .EmptyLine();

        // Order info
        doc.AlignLeft()
            .Bold()
            .TextLine($"ORDER: {order.OrderNumber}")
            .NoBold();

        if (!string.IsNullOrEmpty(order.TableNumber))
        {
            doc.DoubleHeight()
                .Bold()
                .TextLine($"TABLE: {order.TableNumber}")
                .NoBold()
                .NormalSize();
        }

        doc.TextLine($"Server: {order.User?.FullName ?? "Unknown"}")
            .TextLine($"Time: {DateTime.Now:HH:mm:ss}")
            .TextLine(new string('-', 42))
            .EmptyLine();

        // Order items
        doc.Bold();
        foreach (var item in items)
        {
            var productName = item.Product?.Name ?? $"Product #{item.ProductId}";

            // Quantity and product name (large for visibility)
            doc.DoubleHeight()
                .TextLine($"{item.Quantity}x {productName}");

            doc.NormalSize();

            // Modifiers
            if (!string.IsNullOrEmpty(item.Modifiers))
            {
                doc.TextLine($"   + {item.Modifiers}");
            }

            // Special notes (highlighted)
            if (!string.IsNullOrEmpty(item.Notes))
            {
                doc.Inverse()
                    .TextLine($"   NOTE: {item.Notes}")
                    .NoInverse();
            }

            // Course sequence if applicable
            if (item.CourseSequence > 0)
            {
                doc.TextLine($"   Course: {item.CourseSequence}");
            }

            doc.EmptyLine();
        }

        doc.NoBold();

        // Footer
        doc.TextLine(new string('-', 42))
            .AlignCenter()
            .TextLine($"Items: {items.Sum(i => i.Quantity)}")
            .TextLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            .EmptyLine()
            .EmptyLine()
            .Cut();

        return doc;
    }

    /// <summary>
    /// Gets the kitchen printer configured for a specific station.
    /// </summary>
    private async Task<Printer?> GetKitchenPrinterForStationAsync(string station)
    {
        try
        {
            var kitchenPrinters = await _printerService.GetPrintersAsync(PrinterType.Kitchen);

            // Try to find a printer mapped to this station
            foreach (var printer in kitchenPrinters.Where(p => p.IsActive))
            {
                var mappings = await _printerService.GetCategoryMappingsAsync(printer.Id);

                // Check if printer handles this station via its description or mappings
                // Station matching can be done via printer name or category mappings
                if (printer.Description?.Contains(station, StringComparison.OrdinalIgnoreCase) == true ||
                    printer.PrinterName.Contains(station, StringComparison.OrdinalIgnoreCase))
                {
                    return printer;
                }
            }

            // Return first active kitchen printer if no station match
            return kitchenPrinters.FirstOrDefault(p => p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error getting kitchen printer for station {Station}", station);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> PrintAdditionKotAsync(Order order, IEnumerable<OrderItem> newItems)
    {
        var itemsList = newItems.ToList();
        if (!itemsList.Any())
        {
            return false;
        }

        _logger.Information(
            "Printing ADDITION KOT for order {OrderNumber}: {ItemCount} new items",
            order.OrderNumber,
            itemsList.Count);

        // Mark these as additions in the document
        var groupedItems = itemsList.GroupBy(i =>
            string.IsNullOrEmpty(i.Product?.KitchenStation) ? "GENERAL" : i.Product.KitchenStation.ToUpperInvariant());

        var allSuccess = true;

        foreach (var stationGroup in groupedItems)
        {
            var success = await PrintAdditionStationKotAsync(order, stationGroup.Key, stationGroup.ToList());
            if (!success)
            {
                allSuccess = false;
            }
        }

        return allSuccess;
    }

    /// <summary>
    /// Prints an ADDITION KOT for a specific station.
    /// </summary>
    private async Task<bool> PrintAdditionStationKotAsync(Order order, string station, List<OrderItem> items)
    {
        try
        {
            var printer = await GetKitchenPrinterForStationAsync(station)
                          ?? await _printerService.GetDefaultPrinterAsync(PrinterType.Kitchen);

            if (printer == null)
            {
                _logger.Error("No kitchen printer available for addition KOT");
                return false;
            }

            var doc = EscPosPrintDocument.Create80mm()
                .Initialize()
                .AlignCenter()
                .DoubleSize()
                .Bold()
                .Inverse()
                .TextLine("*** ADDITION ***")
                .NoInverse()
                .TextLine($"** {station} **")
                .NoBold()
                .NormalSize()
                .EmptyLine()
                .AlignLeft()
                .Bold()
                .TextLine($"ORDER: {order.OrderNumber}")
                .NoBold();

            if (!string.IsNullOrEmpty(order.TableNumber))
            {
                doc.DoubleHeight()
                    .Bold()
                    .TextLine($"TABLE: {order.TableNumber}")
                    .NoBold()
                    .NormalSize();
            }

            doc.TextLine($"Server: {order.User?.FullName ?? "Unknown"}")
                .TextLine($"Time: {DateTime.Now:HH:mm:ss}")
                .TextLine(new string('-', 42))
                .EmptyLine()
                .Bold();

            foreach (var item in items)
            {
                var productName = item.Product?.Name ?? $"Product #{item.ProductId}";
                doc.DoubleHeight()
                    .TextLine($"{item.Quantity}x {productName}")
                    .NormalSize();

                if (!string.IsNullOrEmpty(item.Modifiers))
                    doc.TextLine($"   + {item.Modifiers}");

                if (!string.IsNullOrEmpty(item.Notes))
                    doc.Inverse().TextLine($"   NOTE: {item.Notes}").NoInverse();

                doc.EmptyLine();
            }

            doc.NoBold()
                .TextLine(new string('-', 42))
                .AlignCenter()
                .TextLine($"Added Items: {items.Sum(i => i.Quantity)}")
                .TextLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
                .EmptyLine()
                .EmptyLine()
                .Cut();

            var data = doc.Build();
            return await _printerComm.SendRawDataAsync(printer.PrinterName, data, $"KOT-ADD-{order.OrderNumber}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error printing addition KOT for station {Station}", station);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPrinterReadyAsync()
    {
        try
        {
            var defaultPrinter = await _printerService.GetDefaultPrinterAsync(PrinterType.Kitchen);
            if (defaultPrinter == null)
            {
                _logger.Debug("No default kitchen printer configured");
                return false;
            }

            var isReady = await _printerComm.IsPrinterReadyAsync(defaultPrinter.PrinterName);
            _logger.Debug("Kitchen printer {PrinterName} ready: {IsReady}", defaultPrinter.PrinterName, isReady);
            return isReady;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error checking kitchen printer status");
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetKitchenStations()
    {
        return StandardStations;
    }
}
