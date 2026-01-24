using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing receipts.
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly POSDbContext _context;
    private readonly IOrderService _orderService;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly IEtimsService? _etimsService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptService"/> class.
    /// </summary>
    public ReceiptService(
        POSDbContext context,
        IOrderService orderService,
        IWorkPeriodService workPeriodService,
        ISessionService sessionService,
        IInventoryService inventoryService,
        ILogger logger,
        IEtimsService? etimsService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _etimsService = etimsService; // Optional - eTIMS integration
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Receipt> CreateReceiptFromOrderAsync(int orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            throw new InvalidOperationException("No user is currently logged in.");
        }

        var receiptNumber = await GenerateReceiptNumberAsync().ConfigureAwait(false);

        var receipt = new Receipt
        {
            ReceiptNumber = receiptNumber,
            OrderId = orderId,
            WorkPeriodId = order.WorkPeriodId,
            OwnerId = currentUserId,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Status = ReceiptStatus.Pending,
            PaidAmount = 0,
            ChangeAmount = 0
        };

        // Copy order items to receipt items
        foreach (var orderItem in order.OrderItems)
        {
            receipt.ReceiptItems.Add(new ReceiptItem
            {
                OrderItemId = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.Product?.Name ?? "Unknown Product",
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                DiscountAmount = orderItem.DiscountAmount,
                TaxAmount = orderItem.TaxAmount,
                TotalAmount = orderItem.TotalAmount,
                Modifiers = orderItem.Modifiers,
                Notes = orderItem.Notes
            });
        }

        _context.Receipts.Add(receipt);

        // Update order status to Sent (indicating it's been processed)
        order.Status = OrderStatus.Sent;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Created receipt {ReceiptNumber} from order {OrderNumber} with {ItemCount} items",
            receipt.ReceiptNumber, order.OrderNumber, receipt.ReceiptItems.Count);

        // Reload with navigation properties
        return (await GetByIdAsync(receipt.Id))!;
    }

    /// <inheritdoc />
    public async Task<Receipt?> GetByIdAsync(int id)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Order)
            .Include(r => r.Owner)
            .Include(r => r.WorkPeriod)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Order)
            .Include(r => r.Owner)
            .Include(r => r.WorkPeriod)
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetPendingReceiptsAsync()
    {
        var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync().ConfigureAwait(false);
        if (currentPeriod is null)
        {
            return Enumerable.Empty<Receipt>();
        }

        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Order)
            .Include(r => r.Owner)
            .Where(r => r.WorkPeriodId == currentPeriod.Id && r.Status == ReceiptStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetReceiptsByWorkPeriodAsync(int workPeriodId)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Order)
            .Include(r => r.Owner)
            .Include(r => r.Payments)
            .Where(r => r.WorkPeriodId == workPeriodId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetReceiptsByUserAsync(int userId)
    {
        var currentPeriod = await _workPeriodService.GetCurrentWorkPeriodAsync().ConfigureAwait(false);
        if (currentPeriod is null)
        {
            return Enumerable.Empty<Receipt>();
        }

        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Order)
            .Include(r => r.Payments)
            .Where(r => r.WorkPeriodId == currentPeriod.Id && r.OwnerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetReceiptsByOrderAsync(int orderId)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Payments)
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
    {
        receipt.UpdatedAt = DateTime.UtcNow;
        _context.Receipts.Update(receipt);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Debug("Updated receipt {ReceiptNumber}", receipt.ReceiptNumber);

        return receipt;
    }

    /// <inheritdoc />
    public async Task<string> GenerateReceiptNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"R-{today:yyyyMMdd}-";

        // Get the count of receipts for today (more reliable than parsing max sequence)
        var receiptCountToday = await _context.Receipts
            .CountAsync(r => r.ReceiptNumber.StartsWith(prefix));

        // Start sequence from count + 1
        int sequence = receiptCountToday + 1;

        // Handle potential gaps by finding the actual max sequence
        var lastReceipt = await _context.Receipts
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .FirstOrDefaultAsync();

        if (lastReceipt is not null)
        {
            var lastSequence = lastReceipt.ReceiptNumber[(prefix.Length)..];
            if (int.TryParse(lastSequence, out var parsed) && parsed >= sequence)
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync()
    {
        return await _context.PaymentMethods
            .Where(pm => pm.IsActive)
            .OrderBy(pm => pm.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Receipt> SettleReceiptAsync(int receiptId, IEnumerable<Payment> payments)
    {
        var receipt = await GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new InvalidOperationException($"Receipt with ID {receiptId} not found.");
        }

        if (receipt.Status != ReceiptStatus.Pending)
        {
            throw new InvalidOperationException($"Receipt {receipt.ReceiptNumber} is not pending.");
        }

        var paymentsList = payments.ToList();
        var totalPaid = paymentsList.Sum(p => p.Amount);

        if (totalPaid < receipt.TotalAmount)
        {
            throw new InvalidOperationException($"Total payment ({totalPaid:C}) is less than receipt total ({receipt.TotalAmount:C}).");
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            throw new InvalidOperationException("No user is currently logged in.");
        }

        // Add all payments
        foreach (var payment in paymentsList)
        {
            payment.ReceiptId = receiptId;
            payment.ProcessedByUserId = currentUserId;
            _context.Payments.Add(payment);
        }

        // Update receipt status
        receipt.Status = ReceiptStatus.Settled;
        receipt.SettledAt = DateTime.UtcNow;
        receipt.SettledByUserId = currentUserId;
        receipt.PaidAmount = totalPaid;
        receipt.ChangeAmount = paymentsList.Sum(p => p.ChangeAmount);
        receipt.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Deduct inventory for all items in the receipt
        var stockMovements = await _inventoryService.DeductStockForReceiptAsync(receipt).ConfigureAwait(false);

        _logger.Information("Settled receipt {ReceiptNumber} with {PaymentCount} payments totaling {TotalPaid:C}. Stock deducted for {StockCount} products.",
            receipt.ReceiptNumber, paymentsList.Count, totalPaid, stockMovements.Count());

        // Auto-submit to eTIMS (Kenya tax compliance)
        await SubmitToEtimsAsync(receiptId).ConfigureAwait(false);

        return (await GetByIdAsync(receiptId))!;
    }

    /// <summary>
    /// Submits a settled receipt to eTIMS for tax compliance.
    /// This operation is non-blocking - failures are queued for retry.
    /// </summary>
    private async Task SubmitToEtimsAsync(int receiptId)
    {
        if (_etimsService == null)
        {
            // eTIMS not configured - skip submission
            return;
        }

        try
        {
            // Check if device is configured
            var device = await _etimsService.GetActiveDeviceAsync().ConfigureAwait(false);
            if (device == null)
            {
                _logger.Warning("eTIMS device not configured - invoice not submitted for receipt {ReceiptId}", receiptId);
                return;
            }

            // Generate eTIMS invoice from receipt
            var invoice = await _etimsService.GenerateInvoiceAsync(receiptId).ConfigureAwait(false);

            // Attempt real-time submission
            var submittedInvoice = await _etimsService.SubmitInvoiceAsync(invoice.Id).ConfigureAwait(false);

            if (submittedInvoice.Status == Core.Enums.EtimsSubmissionStatus.Accepted)
            {
                _logger.Information("eTIMS invoice {InvoiceNumber} submitted successfully for receipt {ReceiptId}",
                    submittedInvoice.InvoiceNumber, receiptId);
            }
            else if (submittedInvoice.Status == Core.Enums.EtimsSubmissionStatus.Failed ||
                     submittedInvoice.Status == Core.Enums.EtimsSubmissionStatus.Rejected)
            {
                // Queue for retry
                await _etimsService.QueueForSubmissionAsync(
                    Core.Enums.EtimsDocumentType.TaxInvoice,
                    invoice.Id,
                    priority: 50).ConfigureAwait(false);

                _logger.Warning("eTIMS invoice {InvoiceNumber} submission failed, queued for retry: {Error}",
                    submittedInvoice.InvoiceNumber, submittedInvoice.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Don't block POS operations if eTIMS fails
            _logger.Error(ex, "eTIMS submission error for receipt {ReceiptId} - will retry later", receiptId);

            // Try to queue for later submission
            try
            {
                // Check if invoice was created
                var existingInvoice = await _etimsService.GetInvoiceByReceiptIdAsync(receiptId).ConfigureAwait(false);
                if (existingInvoice != null)
                {
                    await _etimsService.QueueForSubmissionAsync(
                        Core.Enums.EtimsDocumentType.TaxInvoice,
                        existingInvoice.Id,
                        priority: 100).ConfigureAwait(false);
                }
            }
            catch (Exception queueEx)
            {
                _logger.Error(queueEx, "Failed to queue eTIMS invoice for receipt {ReceiptId}", receiptId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Payment> AddPaymentAsync(Payment payment)
    {
        var receipt = await GetByIdAsync(payment.ReceiptId);
        if (receipt is null)
        {
            throw new InvalidOperationException($"Receipt with ID {payment.ReceiptId} not found.");
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            throw new InvalidOperationException("No user is currently logged in.");
        }

        payment.ProcessedByUserId = currentUserId;
        _context.Payments.Add(payment);

        // Update receipt paid amount
        receipt.PaidAmount += payment.Amount;
        receipt.ChangeAmount += payment.ChangeAmount;

        // Check if fully paid
        if (receipt.PaidAmount >= receipt.TotalAmount)
        {
            receipt.Status = ReceiptStatus.Settled;
            receipt.SettledAt = DateTime.UtcNow;
            receipt.SettledByUserId = currentUserId;
        }

        receipt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Added payment of {Amount:C} to receipt {ReceiptNumber}",
            payment.Amount, receipt.ReceiptNumber);

        return payment;
    }

    /// <inheritdoc />
    public async Task<Payment?> GetPaymentByReferenceAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return await _context.Payments
            .Include(p => p.Receipt)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p =>
                p.Reference == reference &&
                p.Receipt != null &&
                p.Receipt.Status != ReceiptStatus.Voided)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemovePaymentAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            .ConfigureAwait(false);

        if (payment is null)
        {
            _logger.Warning("Payment {PaymentId} not found for removal", paymentId);
            return false;
        }

        // Only allow removal if receipt is still pending
        if (payment.Receipt?.Status != ReceiptStatus.Pending)
        {
            _logger.Warning("Cannot remove payment {PaymentId} - receipt is not pending", paymentId);
            return false;
        }

        // Update receipt totals
        if (payment.Receipt is not null)
        {
            payment.Receipt.PaidAmount -= payment.Amount;
            payment.Receipt.ChangeAmount -= payment.ChangeAmount;
            payment.Receipt.UpdatedAt = DateTime.UtcNow;
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.Information("Removed payment {PaymentId} of {Amount:C} from receipt {ReceiptNumber}",
            paymentId, payment.Amount, payment.Receipt?.ReceiptNumber);

        return true;
    }

    /// <inheritdoc />
    public async Task<decimal> GetSalesTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var totalSales = await _context.Receipts
            .AsNoTracking()
            .Where(r => r.Status == ReceiptStatus.Settled)
            .Where(r => r.SettledAt >= startDate && r.SettledAt <= endDate)
            .SumAsync(r => r.TotalAmount, cancellationToken)
            .ConfigureAwait(false);

        _logger.Information("Retrieved sales total of {TotalSales:C} for period {StartDate:d} to {EndDate:d}",
            totalSales, startDate, endDate);

        return totalSales;
    }
}
