using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing inventory operations including stock deduction, restoration, and adjustments.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly POSDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryService"/> class.
    /// </summary>
    public InventoryService(
        POSDbContext context,
        ISessionService sessionService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<StockMovement?> DeductStockAsync(
        int productId,
        decimal quantity,
        string reference,
        int? referenceId = null)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.Warning("Product {ProductId} not found for stock deduction", productId);
            return null;
        }

        // Skip if inventory tracking is disabled
        if (!product.TrackInventory)
        {
            _logger.Debug("Skipping stock deduction for product {ProductId} - tracking disabled", productId);
            return null;
        }

        var inventory = product.Inventory;
        if (inventory is null)
        {
            // Create inventory record if it doesn't exist
            inventory = new Inventory
            {
                ProductId = productId,
                CurrentStock = 0,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var previousStock = inventory.CurrentStock;
        var newStock = previousStock - quantity;

        // Don't allow negative stock
        if (newStock < 0)
        {
            _logger.Warning(
                "Stock deduction would result in negative stock for product {ProductId}. Current: {Current}, Deducting: {Deducting}. Setting to 0.",
                productId, previousStock, quantity);
            newStock = 0;
        }

        inventory.CurrentStock = newStock;
        inventory.LastUpdated = DateTime.UtcNow;

        // Create movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = MovementType.Sale,
            Quantity = -quantity, // Negative for deduction
            PreviousStock = previousStock,
            NewStock = newStock,
            ReferenceType = "Receipt",
            ReferenceId = referenceId,
            Reason = reference,
            UserId = _sessionService.CurrentUserId
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock deducted for product {ProductId}: {Previous} -> {New} (Quantity: {Quantity}, Reference: {Reference})",
            productId, previousStock, newStock, quantity, reference);

        // Log if product is now out of stock or low stock
        if (newStock <= 0)
        {
            _logger.Warning("Product {ProductId} ({ProductName}) is now OUT OF STOCK", productId, product.Name);
        }
        else if (product.MinStockLevel.HasValue && newStock <= product.MinStockLevel.Value)
        {
            _logger.Warning(
                "Product {ProductId} ({ProductName}) is now LOW STOCK: {CurrentStock} <= {MinStock}",
                productId, product.Name, newStock, product.MinStockLevel.Value);
        }

        return movement;
    }

    /// <inheritdoc />
    public async Task<StockMovement?> RestoreStockAsync(
        int productId,
        decimal quantity,
        MovementType movementType,
        string reference,
        int? referenceId = null)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.Warning("Product {ProductId} not found for stock restoration", productId);
            return null;
        }

        // Skip if inventory tracking is disabled
        if (!product.TrackInventory)
        {
            _logger.Debug("Skipping stock restoration for product {ProductId} - tracking disabled", productId);
            return null;
        }

        var inventory = product.Inventory;
        if (inventory is null)
        {
            // Create inventory record if it doesn't exist
            inventory = new Inventory
            {
                ProductId = productId,
                CurrentStock = 0,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var previousStock = inventory.CurrentStock;
        var newStock = previousStock + quantity;

        inventory.CurrentStock = newStock;
        inventory.LastUpdated = DateTime.UtcNow;

        // Create movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = movementType,
            Quantity = quantity, // Positive for restoration
            PreviousStock = previousStock,
            NewStock = newStock,
            ReferenceType = movementType == MovementType.Void ? "Void" : "Return",
            ReferenceId = referenceId,
            Reason = reference,
            UserId = _sessionService.CurrentUserId
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock restored for product {ProductId}: {Previous} -> {New} (Quantity: {Quantity}, Type: {Type}, Reference: {Reference})",
            productId, previousStock, newStock, quantity, movementType, reference);

        return movement;
    }

    /// <inheritdoc />
    public async Task<StockMovement> AdjustStockAsync(
        int productId,
        decimal newQuantity,
        string reason,
        string? notes = null)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        var inventory = product.Inventory;
        if (inventory is null)
        {
            // Create inventory record if it doesn't exist
            inventory = new Inventory
            {
                ProductId = productId,
                CurrentStock = 0,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var previousStock = inventory.CurrentStock;
        var difference = newQuantity - previousStock;

        inventory.CurrentStock = newQuantity;
        inventory.LastUpdated = DateTime.UtcNow;

        // Create movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = MovementType.Adjustment,
            Quantity = difference,
            PreviousStock = previousStock,
            NewStock = newQuantity,
            ReferenceType = "Adjustment",
            Reason = reason,
            Notes = notes,
            UserId = _sessionService.CurrentUserId
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock adjusted for product {ProductId} ({ProductName}): {Previous} -> {New} (Reason: {Reason})",
            productId, product.Name, previousStock, newQuantity, reason);

        return movement;
    }

    /// <inheritdoc />
    public async Task<StockMovement> AdjustStockAsync(
        int productId,
        decimal newQuantity,
        int adjustmentReasonId,
        string? notes = null)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        var adjustmentReason = await _context.AdjustmentReasons
            .FirstOrDefaultAsync(ar => ar.Id == adjustmentReasonId && ar.IsActive)
            .ConfigureAwait(false);

        if (adjustmentReason is null)
        {
            throw new InvalidOperationException($"Adjustment reason with ID {adjustmentReasonId} not found or is inactive.");
        }

        var inventory = product.Inventory;
        if (inventory is null)
        {
            // Create inventory record if it doesn't exist
            inventory = new Inventory
            {
                ProductId = productId,
                CurrentStock = 0,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var previousStock = inventory.CurrentStock;
        var difference = newQuantity - previousStock;

        inventory.CurrentStock = newQuantity;
        inventory.LastUpdated = DateTime.UtcNow;

        // Create movement record with adjustment reason
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = MovementType.Adjustment,
            Quantity = difference,
            PreviousStock = previousStock,
            NewStock = newQuantity,
            ReferenceType = "Adjustment",
            Reason = adjustmentReason.Name,
            Notes = notes,
            AdjustmentReasonId = adjustmentReasonId,
            UserId = _sessionService.CurrentUserId
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock adjusted for product {ProductId} ({ProductName}): {Previous} -> {New} (Reason: {Reason}, ReasonId: {ReasonId})",
            productId, product.Name, previousStock, newQuantity, adjustmentReason.Name, adjustmentReasonId);

        return movement;
    }

    /// <inheritdoc />
    public async Task<decimal> GetStockLevelAsync(int productId)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId)
            .ConfigureAwait(false);

        return inventory?.CurrentStock ?? 0;
    }

    /// <inheritdoc />
    public async Task<bool> CheckAvailabilityAsync(int productId, decimal requiredQuantity)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            return false;
        }

        // If tracking is disabled, always available
        if (!product.TrackInventory)
        {
            return true;
        }

        var currentStock = product.Inventory?.CurrentStock ?? 0;
        return currentStock >= requiredQuantity;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> DeductStockForReceiptAsync(Receipt receipt)
    {
        var movements = new List<StockMovement>();

        if (receipt.ReceiptItems is null || !receipt.ReceiptItems.Any())
        {
            _logger.Debug("No items in receipt {ReceiptNumber} for stock deduction", receipt.ReceiptNumber);
            return movements;
        }

        foreach (var item in receipt.ReceiptItems)
        {
            var movement = await DeductStockAsync(
                item.ProductId,
                item.Quantity,
                receipt.ReceiptNumber,
                receipt.Id)
                .ConfigureAwait(false);

            if (movement is not null)
            {
                movements.Add(movement);
            }
        }

        _logger.Information(
            "Deducted stock for receipt {ReceiptNumber}: {MovementCount} products affected",
            receipt.ReceiptNumber, movements.Count);

        return movements;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> RestoreStockForVoidAsync(Receipt receipt)
    {
        var movements = new List<StockMovement>();

        if (receipt.ReceiptItems is null || !receipt.ReceiptItems.Any())
        {
            _logger.Debug("No items in receipt {ReceiptNumber} for stock restoration", receipt.ReceiptNumber);
            return movements;
        }

        // Check if stock was already restored for this void (idempotency)
        var existingVoidMovements = await _context.StockMovements
            .Where(sm => sm.ReferenceType == "Void" && sm.ReferenceId == receipt.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        if (existingVoidMovements.Any())
        {
            _logger.Warning(
                "Stock has already been restored for receipt {ReceiptId}. Skipping to prevent double restoration.",
                receipt.Id);
            return existingVoidMovements;
        }

        foreach (var item in receipt.ReceiptItems)
        {
            var movement = await RestoreStockAsync(
                item.ProductId,
                item.Quantity,
                MovementType.Void,
                $"Void: {receipt.ReceiptNumber}",
                receipt.Id)
                .ConfigureAwait(false);

            if (movement is not null)
            {
                movements.Add(movement);
            }
        }

        _logger.Information(
            "Restored stock for voided receipt {ReceiptNumber}: {MovementCount} products affected",
            receipt.ReceiptNumber, movements.Count);

        return movements;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                        p.TrackInventory &&
                        p.MinStockLevel.HasValue &&
                        p.Inventory != null &&
                        p.Inventory.CurrentStock <= p.MinStockLevel.Value &&
                        p.Inventory.CurrentStock > 0)
            .OrderBy(p => p.Inventory!.CurrentStock)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetOutOfStockProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                        p.TrackInventory &&
                        (p.Inventory == null || p.Inventory.CurrentStock <= 0))
            .OrderBy(p => p.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetStockMovementsAsync(
        int productId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.StockMovements
            .Include(sm => sm.User)
            .Where(sm => sm.ProductId == productId);

        if (startDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetStockMovementsByReferenceAsync(
        string referenceType,
        int referenceId)
    {
        return await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .Where(sm => sm.ReferenceType == referenceType && sm.ReferenceId == referenceId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdjustmentReason>> GetAdjustmentReasonsAsync()
    {
        return await _context.AdjustmentReasons
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockMovement?> ReceiveStockAsync(
        int productId,
        decimal quantity,
        decimal unitCost,
        string reference,
        int? referenceId = null)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == productId)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.Warning("Product {ProductId} not found for stock receiving", productId);
            return null;
        }

        // Skip if inventory tracking is disabled
        if (!product.TrackInventory)
        {
            _logger.Debug("Skipping stock receiving for product {ProductId} - tracking disabled", productId);
            return null;
        }

        var inventory = product.Inventory;
        if (inventory is null)
        {
            // Create inventory record if it doesn't exist
            inventory = new Inventory
            {
                ProductId = productId,
                CurrentStock = 0,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var previousStock = inventory.CurrentStock;
        var newStock = previousStock + quantity;

        inventory.CurrentStock = newStock;
        inventory.LastUpdated = DateTime.UtcNow;

        // Update product cost price (use latest cost)
        product.CostPrice = unitCost;

        // Create movement record
        var movement = new StockMovement
        {
            ProductId = productId,
            MovementType = MovementType.PurchaseReceive,
            Quantity = quantity, // Positive for receiving
            PreviousStock = previousStock,
            NewStock = newStock,
            ReferenceType = "GRN",
            ReferenceId = referenceId,
            Reason = reference,
            UserId = _sessionService.CurrentUserId
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information(
            "Stock received for product {ProductId} ({ProductName}): {Previous} -> {New} (Quantity: {Quantity}, UnitCost: {UnitCost}, Reference: {Reference})",
            productId, product.Name, previousStock, newStock, quantity, unitCost, reference);

        return movement;
    }
}
