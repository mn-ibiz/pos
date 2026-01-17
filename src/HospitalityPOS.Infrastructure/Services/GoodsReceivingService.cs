using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing goods receiving operations.
/// Handles both PO-based receiving and direct receiving.
/// </summary>
public class GoodsReceivingService : IGoodsReceivingService
{
    private readonly POSDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoodsReceivingService"/> class.
    /// </summary>
    public GoodsReceivingService(
        POSDbContext context,
        IInventoryService inventoryService,
        ISessionService sessionService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetPendingPurchaseOrdersAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Product)
            .Where(po => po.IsActive &&
                         (po.Status == PurchaseOrderStatus.Sent ||
                          po.Status == PurchaseOrderStatus.PartiallyReceived))
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder?> GetPurchaseOrderForReceivingAsync(int purchaseOrderId)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Product)
                    .ThenInclude(p => p!.Inventory)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && po.IsActive)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GoodsReceivedNote> ReceiveGoodsAsync(
        int purchaseOrderId,
        string? deliveryNote,
        string? notes,
        IEnumerable<GRNItemInput> items)
    {
        var itemsList = items.ToList();

        if (!itemsList.Any())
        {
            throw new ArgumentException("At least one item is required for receiving.", nameof(items));
        }

        var purchaseOrder = await GetPurchaseOrderForReceivingAsync(purchaseOrderId);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Complete)
        {
            throw new InvalidOperationException("This purchase order has already been fully received.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot receive against a cancelled purchase order.");
        }

        // Generate GRN number
        var grnNumber = await GenerateGRNNumberAsync().ConfigureAwait(false);

        // Calculate total amount
        var totalAmount = itemsList.Sum(i => i.ReceivedQuantity * i.UnitCost);

        // Create GRN
        var grn = new GoodsReceivedNote
        {
            GRNNumber = grnNumber,
            PurchaseOrderId = purchaseOrderId,
            SupplierId = purchaseOrder.SupplierId,
            ReceivedDate = DateTime.UtcNow,
            DeliveryNote = deliveryNote,
            TotalAmount = totalAmount,
            Notes = notes,
            ReceivedByUserId = _sessionService.CurrentUserId
        };

        // Add GRN items and update stock
        foreach (var item in itemsList.Where(i => i.ReceivedQuantity > 0))
        {
            var grnItem = new GRNItem
            {
                PurchaseOrderItemId = item.PurchaseOrderItemId,
                ProductId = item.ProductId,
                OrderedQuantity = item.OrderedQuantity,
                ReceivedQuantity = item.ReceivedQuantity,
                UnitCost = item.UnitCost,
                TotalCost = item.ReceivedQuantity * item.UnitCost,
                Notes = item.Notes
            };

            grn.Items.Add(grnItem);

            // Update stock via InventoryService
            await _inventoryService.ReceiveStockAsync(
                item.ProductId,
                item.ReceivedQuantity,
                item.UnitCost,
                grnNumber,
                null) // Will be updated after GRN is saved
                .ConfigureAwait(false);

            // Update PO item received quantity
            if (item.PurchaseOrderItemId.HasValue)
            {
                var poItem = purchaseOrder.PurchaseOrderItems
                    .FirstOrDefault(poi => poi.Id == item.PurchaseOrderItemId.Value);

                if (poItem != null)
                {
                    poItem.ReceivedQuantity += item.ReceivedQuantity;
                }
            }
        }

        // Update PO status
        UpdatePurchaseOrderStatus(purchaseOrder);

        _context.GoodsReceivedNotes.Add(grn);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Goods received - GRN: {GRNNumber}, PO: {PONumber}, Supplier: {Supplier}, Items: {ItemCount}, Total: {Total:C}",
            grnNumber, purchaseOrder.PONumber, purchaseOrder.Supplier?.Name, grn.Items.Count, totalAmount);

        return grn;
    }

    /// <inheritdoc />
    public async Task<GoodsReceivedNote> ReceiveDirectAsync(
        int? supplierId,
        string? deliveryNote,
        string? notes,
        IEnumerable<GRNItemInput> items)
    {
        var itemsList = items.ToList();

        if (!itemsList.Any())
        {
            throw new ArgumentException("At least one item is required for receiving.", nameof(items));
        }

        // Validate supplier if provided
        if (supplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.Id == supplierId.Value && s.IsActive)
                .ConfigureAwait(false);

            if (!supplierExists)
            {
                throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
            }
        }

        // Generate GRN number
        var grnNumber = await GenerateGRNNumberAsync().ConfigureAwait(false);

        // Calculate total amount
        var totalAmount = itemsList.Sum(i => i.ReceivedQuantity * i.UnitCost);

        // Create GRN
        var grn = new GoodsReceivedNote
        {
            GRNNumber = grnNumber,
            PurchaseOrderId = null, // Direct receiving - no PO
            SupplierId = supplierId,
            ReceivedDate = DateTime.UtcNow,
            DeliveryNote = deliveryNote,
            TotalAmount = totalAmount,
            Notes = string.IsNullOrWhiteSpace(notes) ? "Direct Receiving" : notes,
            ReceivedByUserId = _sessionService.CurrentUserId
        };

        // Add GRN items and update stock
        foreach (var item in itemsList.Where(i => i.ReceivedQuantity > 0))
        {
            var grnItem = new GRNItem
            {
                PurchaseOrderItemId = null, // Direct receiving - no PO item
                ProductId = item.ProductId,
                OrderedQuantity = 0, // Direct receiving has no ordered quantity
                ReceivedQuantity = item.ReceivedQuantity,
                UnitCost = item.UnitCost,
                TotalCost = item.ReceivedQuantity * item.UnitCost,
                Notes = item.Notes
            };

            grn.Items.Add(grnItem);

            // Update stock via InventoryService
            await _inventoryService.ReceiveStockAsync(
                item.ProductId,
                item.ReceivedQuantity,
                item.UnitCost,
                grnNumber,
                null)
                .ConfigureAwait(false);
        }

        _context.GoodsReceivedNotes.Add(grn);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        var supplierName = supplierId.HasValue
            ? await _context.Suppliers.Where(s => s.Id == supplierId).Select(s => s.Name).FirstOrDefaultAsync().ConfigureAwait(false)
            : "No Supplier";

        _logger.Information(
            "Direct goods received - GRN: {GRNNumber}, Supplier: {Supplier}, Items: {ItemCount}, Total: {Total:C}",
            grnNumber, supplierName, grn.Items.Count, totalAmount);

        return grn;
    }

    /// <inheritdoc />
    public async Task<string> GenerateGRNNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var datePrefix = today.ToString("yyyyMMdd");
        var pattern = $"GRN-{datePrefix}-";

        // Get the count of GRNs for today
        var todayCount = await _context.GoodsReceivedNotes
            .CountAsync(g => g.GRNNumber.StartsWith(pattern))
            .ConfigureAwait(false);

        var sequence = (todayCount + 1).ToString("D3");
        return $"{pattern}{sequence}";
    }

    /// <inheritdoc />
    public async Task<GoodsReceivedNote?> GetByIdAsync(int id)
    {
        return await _context.GoodsReceivedNotes
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Supplier)
            .Include(g => g.ReceivedByUser)
            .Include(g => g.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GoodsReceivedNote?> GetByNumberAsync(string grnNumber)
    {
        return await _context.GoodsReceivedNotes
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Supplier)
            .Include(g => g.ReceivedByUser)
            .Include(g => g.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(g => g.GRNNumber == grnNumber && g.IsActive)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GoodsReceivedNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.GoodsReceivedNotes
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Supplier)
            .Include(g => g.ReceivedByUser)
            .Include(g => g.Items)
            .Where(g => g.IsActive &&
                        g.ReceivedDate >= startDate &&
                        g.ReceivedDate <= endDate)
            .OrderByDescending(g => g.ReceivedDate)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync()
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> SearchProductsAsync(string searchText, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            return Array.Empty<Product>();
        }

        var searchLower = searchText.ToLowerInvariant();

        return await _context.Products
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                        (p.Name.ToLower().Contains(searchLower) ||
                         p.Code.ToLower().Contains(searchLower) ||
                         (p.Barcode != null && p.Barcode.Contains(searchText))))
            .OrderBy(p => p.Name)
            .Take(maxResults)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the purchase order status based on received quantities.
    /// </summary>
    private static void UpdatePurchaseOrderStatus(PurchaseOrder purchaseOrder)
    {
        var totalOrdered = purchaseOrder.PurchaseOrderItems.Sum(i => i.OrderedQuantity);
        var totalReceived = purchaseOrder.PurchaseOrderItems.Sum(i => i.ReceivedQuantity);

        if (totalReceived >= totalOrdered)
        {
            purchaseOrder.Status = PurchaseOrderStatus.Complete;
            purchaseOrder.ReceivedAt = DateTime.UtcNow;
        }
        else if (totalReceived > 0)
        {
            purchaseOrder.Status = PurchaseOrderStatus.PartiallyReceived;
        }
    }
}
