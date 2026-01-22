using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing purchase orders.
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrderService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public PurchaseOrderService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetAllPurchaseOrdersAsync(bool includeItems = false, CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseOrder> query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser);

        if (includeItems)
        {
            query = _context.PurchaseOrders
                .AsNoTracking()
                .Include(po => po.Supplier)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product);
        }

        return await query
            .OrderByDescending(po => po.OrderDate)
            .ThenByDescending(po => po.PONumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Where(po => po.Status == status);

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Where(po => po.SupplierId == supplierId);

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id, bool includeItems = true, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Where(po => po.Id == id);

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder?> GetPurchaseOrderByNumberAsync(string poNumber, bool includeItems = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            return null;
        }

        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Where(po => po.PONumber == poNumber);

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);

        // Generate PO number if not provided
        if (string.IsNullOrWhiteSpace(purchaseOrder.PONumber))
        {
            purchaseOrder.PONumber = await GeneratePONumberAsync(purchaseOrder.OrderDate, cancellationToken).ConfigureAwait(false);
        }

        purchaseOrder.CreatedByUserId = createdByUserId;
        purchaseOrder.Status = PurchaseOrderStatus.Draft;

        // Calculate totals from items
        if (purchaseOrder.PurchaseOrderItems.Count > 0)
        {
            CalculateTotals(purchaseOrder);
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Created purchase order {PONumber} for supplier {SupplierId}", purchaseOrder.PONumber, purchaseOrder.SupplierId);

        return purchaseOrder;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> UpdatePurchaseOrderAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);

        var existingPO = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrder.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existingPO is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrder.Id} not found.");
        }

        // Only Draft POs can be fully modified
        if (existingPO.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot modify purchase order with status '{existingPO.Status}'.");
        }

        // Update properties
        existingPO.SupplierId = purchaseOrder.SupplierId;
        existingPO.OrderDate = purchaseOrder.OrderDate;
        existingPO.ExpectedDate = purchaseOrder.ExpectedDate;
        existingPO.Notes = purchaseOrder.Notes;

        // Recalculate totals
        CalculateTotals(existingPO);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Updated purchase order {PONumber}", existingPO.PONumber);

        return existingPO;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderItem> AddItemAsync(int purchaseOrderId, PurchaseOrderItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add items to purchase order with status '{purchaseOrder.Status}'.");
        }

        // Calculate total cost
        item.TotalCost = item.OrderedQuantity * item.UnitCost;
        item.PurchaseOrderId = purchaseOrderId;

        purchaseOrder.PurchaseOrderItems.Add(item);

        // Recalculate totals
        CalculateTotals(purchaseOrder);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Added item to purchase order {PONumber}: Product {ProductId}, Qty {Quantity}", purchaseOrder.PONumber, item.ProductId, item.OrderedQuantity);

        return item;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderItem> UpdateItemAsync(PurchaseOrderItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existingItem = await _context.PurchaseOrderItems
            .Include(i => i.PurchaseOrder)
            .FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existingItem is null)
        {
            throw new InvalidOperationException($"Purchase order item with ID {item.Id} not found.");
        }

        if (existingItem.PurchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot modify items in purchase order with status '{existingItem.PurchaseOrder.Status}'.");
        }

        existingItem.ProductId = item.ProductId;
        existingItem.OrderedQuantity = item.OrderedQuantity;
        existingItem.UnitCost = item.UnitCost;
        existingItem.TotalCost = item.OrderedQuantity * item.UnitCost;
        existingItem.Notes = item.Notes;

        // Recalculate PO totals
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == existingItem.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is not null)
        {
            CalculateTotals(purchaseOrder);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existingItem;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.PurchaseOrderItems
            .Include(i => i.PurchaseOrder)
            .ThenInclude(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return false;
        }

        if (item.PurchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot remove items from purchase order with status '{item.PurchaseOrder.Status}'.");
        }

        var purchaseOrder = item.PurchaseOrder;
        _context.PurchaseOrderItems.Remove(item);

        // Recalculate totals
        purchaseOrder.PurchaseOrderItems.Remove(item);
        CalculateTotals(purchaseOrder);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Removed item {ItemId} from purchase order {PONumber}", itemId, purchaseOrder.PONumber);

        return true;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        var oldStatus = purchaseOrder.Status;
        purchaseOrder.Status = status;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Updated purchase order {PONumber} status from {OldStatus} to {NewStatus}", purchaseOrder.PONumber, oldStatus, status);

        return purchaseOrder;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> SendToSupplierAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot send purchase order with status '{purchaseOrder.Status}'. Only Draft POs can be sent.");
        }

        if (purchaseOrder.PurchaseOrderItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot send a purchase order with no items.");
        }

        purchaseOrder.Status = PurchaseOrderStatus.Sent;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Sent purchase order {PONumber} to supplier", purchaseOrder.PONumber);

        return purchaseOrder;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> CancelPurchaseOrderAsync(int purchaseOrderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Complete)
        {
            throw new InvalidOperationException("Cannot cancel a completed purchase order.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Purchase order is already cancelled.");
        }

        purchaseOrder.Status = PurchaseOrderStatus.Cancelled;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            purchaseOrder.Notes = string.IsNullOrWhiteSpace(purchaseOrder.Notes)
                ? $"Cancelled: {reason}"
                : $"{purchaseOrder.Notes}\nCancelled: {reason}";
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Cancelled purchase order {PONumber}. Reason: {Reason}", purchaseOrder.PONumber, reason ?? "Not specified");

        return purchaseOrder;
    }

    /// <inheritdoc />
    public async Task<string> GeneratePONumberAsync(DateTime orderDate, CancellationToken cancellationToken = default)
    {
        // Format: PO-{yyyyMMdd}-{sequence}
        var datePrefix = $"PO-{orderDate:yyyyMMdd}-";

        var lastPOToday = await _context.PurchaseOrders
            .Where(po => po.PONumber.StartsWith(datePrefix))
            .OrderByDescending(po => po.PONumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        int nextSequence = 1;

        if (lastPOToday is not null)
        {
            var lastSequence = lastPOToday.PONumber.Replace(datePrefix, "");
            if (int.TryParse(lastSequence, out var seq))
            {
                nextSequence = seq + 1;
            }
        }

        return $"{datePrefix}{nextSequence:D4}";
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> RecalculateTotalsAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (purchaseOrder is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        CalculateTotals(purchaseOrder);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return purchaseOrder;
    }

    /// <inheritdoc />
    public async Task<int> GetCountByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .CountAsync(po => po.Status == status, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Where(po => po.OrderDate >= startDate && po.OrderDate <= endDate);

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> SearchPurchaseOrdersAsync(string searchTerm, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            query = query.Where(po =>
                po.PONumber.ToLower().Contains(searchLower) ||
                po.Supplier.Name.ToLower().Contains(searchLower) ||
                po.Supplier.Code.ToLower().Contains(searchLower));
        }

        if (includeItems)
        {
            query = query.Include(po => po.PurchaseOrderItems)
                         .ThenInclude(i => i.Product);
        }

        return await query
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder> DuplicatePurchaseOrderAsync(int purchaseOrderId, int createdByUserId, CancellationToken cancellationToken = default)
    {
        // Get the source PO with items
        var sourcePO = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.PurchaseOrderItems)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (sourcePO is null)
        {
            throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found.");
        }

        // Create new PO as Draft
        var newPO = new PurchaseOrder
        {
            SupplierId = sourcePO.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = null, // User should set a new expected date
            Status = PurchaseOrderStatus.Draft,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedByUserId = createdByUserId,
            Notes = $"Duplicated from {sourcePO.PONumber}"
        };

        // Generate new PO number
        newPO.PONumber = await GeneratePONumberAsync(newPO.OrderDate, cancellationToken).ConfigureAwait(false);

        // Copy items
        foreach (var sourceItem in sourcePO.PurchaseOrderItems)
        {
            var newItem = new PurchaseOrderItem
            {
                ProductId = sourceItem.ProductId,
                OrderedQuantity = sourceItem.OrderedQuantity,
                ReceivedQuantity = 0, // Reset received qty
                UnitCost = sourceItem.UnitCost,
                TotalCost = sourceItem.OrderedQuantity * sourceItem.UnitCost,
                Notes = sourceItem.Notes
            };
            newPO.PurchaseOrderItems.Add(newItem);
        }

        // Calculate totals
        CalculateTotals(newPO);

        // Save
        _context.PurchaseOrders.Add(newPO);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Duplicated purchase order {SourcePO} to {NewPO}", sourcePO.PONumber, newPO.PONumber);

        return newPO;
    }

    /// <summary>
    /// Calculates and updates the totals for a purchase order.
    /// </summary>
    private static void CalculateTotals(PurchaseOrder purchaseOrder)
    {
        // Ensure each item has correct total cost
        foreach (var item in purchaseOrder.PurchaseOrderItems)
        {
            item.TotalCost = item.OrderedQuantity * item.UnitCost;
        }

        // SubTotal is sum of all item costs
        purchaseOrder.SubTotal = purchaseOrder.PurchaseOrderItems.Sum(i => i.TotalCost);

        // For now, assume 16% VAT on all items (can be made configurable)
        purchaseOrder.TaxAmount = purchaseOrder.SubTotal * 0.16m;

        // Total = SubTotal + Tax
        purchaseOrder.TotalAmount = purchaseOrder.SubTotal + purchaseOrder.TaxAmount;
    }
}
