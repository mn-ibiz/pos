using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing supplier credit and payments.
/// </summary>
public class SupplierCreditService : ISupplierCreditService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    public SupplierCreditService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Credit Terms Management

    public async Task<Supplier> UpdateCreditTermsAsync(int supplierId, decimal creditLimit, int paymentTermDays, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
        }

        supplier.CreditLimit = creditLimit;
        supplier.PaymentTermDays = paymentTermDays;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Updated credit terms for supplier {SupplierCode}: Limit={CreditLimit}, Terms={PaymentTermDays} days",
            supplier.Code, creditLimit, paymentTermDays);

        return supplier;
    }

    public async Task<bool> IsCreditLimitExceededAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            return false;
        }

        // If credit limit is 0, it means unlimited credit
        if (supplier.CreditLimit == 0)
        {
            return false;
        }

        return supplier.CurrentBalance >= supplier.CreditLimit;
    }

    public async Task<decimal> GetAvailableCreditAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            return 0;
        }

        // If credit limit is 0, it means unlimited credit
        if (supplier.CreditLimit == 0)
        {
            return decimal.MaxValue;
        }

        return Math.Max(0, supplier.CreditLimit - supplier.CurrentBalance);
    }

    #endregion

    #region Invoice Management

    public async Task<SupplierInvoice> CreateInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == invoice.SupplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {invoice.SupplierId} not found.");
        }

        // Set default due date based on payment terms if not set
        if (invoice.DueDate == default)
        {
            invoice.DueDate = invoice.InvoiceDate.AddDays(supplier.PaymentTermDays);
        }

        invoice.Status = InvoiceStatus.Unpaid;
        invoice.PaidAmount = 0;

        _context.SupplierInvoices.Add(invoice);

        // Update supplier balance
        supplier.CurrentBalance += invoice.TotalAmount;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Created invoice {InvoiceNumber} for supplier {SupplierCode}: Amount={Amount}",
            invoice.InvoiceNumber, supplier.Code, invoice.TotalAmount);

        return invoice;
    }

    public async Task<IReadOnlyList<SupplierInvoice>> GetSupplierInvoicesAsync(int supplierId, bool includeFullyPaid = true, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Where(i => i.SupplierId == supplierId);

        if (!includeFullyPaid)
        {
            query = query.Where(i => i.Status != InvoiceStatus.Paid);
        }

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<SupplierInvoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.SupplierPayments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SupplierInvoice>> GetOutstandingInvoicesAsync(int? supplierId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Where(i => i.Status != InvoiceStatus.Paid);

        if (supplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        return await query
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SupplierInvoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.SupplierInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Where(i => i.Status != InvoiceStatus.Paid && i.DueDate < today)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateInvoiceStatusAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.SupplierInvoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return;
        }

        var today = DateTime.UtcNow.Date;

        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = invoice.DueDate < today ? InvoiceStatus.Overdue : InvoiceStatus.PartiallyPaid;
        }
        else
        {
            invoice.Status = invoice.DueDate < today ? InvoiceStatus.Overdue : InvoiceStatus.Unpaid;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Payment Management

    public async Task<SupplierPayment> RecordPaymentAsync(SupplierPayment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == payment.SupplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {payment.SupplierId} not found.");
        }

        // If payment is for a specific invoice
        if (payment.SupplierInvoiceId.HasValue)
        {
            var invoice = await _context.SupplierInvoices
                .FirstOrDefaultAsync(i => i.Id == payment.SupplierInvoiceId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                throw new InvalidOperationException($"Invoice with ID {payment.SupplierInvoiceId} not found.");
            }

            if (invoice.SupplierId != payment.SupplierId)
            {
                throw new InvalidOperationException("Invoice does not belong to the specified supplier.");
            }

            // Update invoice paid amount
            invoice.PaidAmount += payment.Amount;

            // Update invoice status
            await UpdateInvoiceStatusAsync(invoice.Id, cancellationToken).ConfigureAwait(false);
        }

        _context.SupplierPayments.Add(payment);

        // Update supplier balance
        supplier.CurrentBalance -= payment.Amount;
        if (supplier.CurrentBalance < 0)
        {
            supplier.CurrentBalance = 0;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Recorded payment of {Amount} to supplier {SupplierCode}",
            payment.Amount, supplier.Code);

        return payment;
    }

    public async Task<IReadOnlyList<SupplierPayment>> GetSupplierPaymentsAsync(int supplierId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierPayments
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.SupplierInvoice)
            .Include(p => p.ProcessedByUser)
            .Where(p => p.SupplierId == supplierId);

        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SupplierPayment>> GetInvoicePaymentsAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierPayments
            .AsNoTracking()
            .Include(p => p.ProcessedByUser)
            .Where(p => p.SupplierInvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Balance Management

    public async Task<decimal> RecalculateBalanceAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
        }

        // Sum of all unpaid invoice amounts
        var totalOutstanding = await _context.SupplierInvoices
            .Where(i => i.SupplierId == supplierId && i.Status != InvoiceStatus.Paid)
            .SumAsync(i => i.TotalAmount - i.PaidAmount, cancellationToken)
            .ConfigureAwait(false);

        supplier.CurrentBalance = totalOutstanding;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Recalculated balance for supplier {SupplierCode}: {Balance}",
            supplier.Code, totalOutstanding);

        return totalOutstanding;
    }

    public async Task<SupplierAgingSummary> GetAgingSummaryAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
        }

        var today = DateTime.UtcNow.Date;
        var invoices = await _context.SupplierInvoices
            .AsNoTracking()
            .Where(i => i.SupplierId == supplierId && i.Status != InvoiceStatus.Paid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summary = new SupplierAgingSummary
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            SupplierCode = supplier.Code,
            CurrentBalance = supplier.CurrentBalance,
            CreditLimit = supplier.CreditLimit,
            AvailableCredit = supplier.CreditLimit > 0 ? Math.Max(0, supplier.CreditLimit - supplier.CurrentBalance) : decimal.MaxValue
        };

        foreach (var invoice in invoices)
        {
            var outstanding = invoice.TotalAmount - invoice.PaidAmount;
            var daysOld = (today - invoice.InvoiceDate).Days;

            if (daysOld <= 30)
            {
                summary.Current += outstanding;
            }
            else if (daysOld <= 60)
            {
                summary.Days30 += outstanding;
            }
            else if (daysOld <= 90)
            {
                summary.Days60 += outstanding;
            }
            else
            {
                summary.Days90Plus += outstanding;
            }

            if (invoice.DueDate < today)
            {
                summary.OverdueInvoiceCount++;
            }

            if (!summary.OldestInvoiceDate.HasValue || invoice.InvoiceDate < summary.OldestInvoiceDate)
            {
                summary.OldestInvoiceDate = invoice.InvoiceDate;
            }
        }

        return summary;
    }

    public async Task<IReadOnlyList<SupplierAgingSummary>> GetAllAgingSummariesAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = new List<SupplierAgingSummary>();

        foreach (var supplier in suppliers)
        {
            var summary = await GetAgingSummaryAsync(supplier.Id, cancellationToken).ConfigureAwait(false);
            if (summary.CurrentBalance > 0)
            {
                summaries.Add(summary);
            }
        }

        return summaries.OrderByDescending(s => s.CurrentBalance).ToList();
    }

    #endregion

    #region Statement Generation

    public async Task<SupplierStatement> GenerateStatementAsync(int supplierId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
        }

        var statement = new SupplierStatement
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            SupplierCode = supplier.Code,
            SupplierAddress = $"{supplier.Address}, {supplier.City}, {supplier.Country}".Trim().Trim(','),
            SupplierPhone = supplier.Phone,
            SupplierEmail = supplier.Email,
            StartDate = startDate,
            EndDate = endDate,
            CreditLimit = supplier.CreditLimit,
            PaymentTermDays = supplier.PaymentTermDays
        };

        // Calculate opening balance (balance before start date)
        var invoicesBeforeStart = await _context.SupplierInvoices
            .Where(i => i.SupplierId == supplierId && i.InvoiceDate < startDate)
            .SumAsync(i => i.TotalAmount, cancellationToken)
            .ConfigureAwait(false);

        var paymentsBeforeStart = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.PaymentDate < startDate)
            .SumAsync(p => p.Amount, cancellationToken)
            .ConfigureAwait(false);

        statement.OpeningBalance = invoicesBeforeStart - paymentsBeforeStart;

        // Get invoices in period
        var invoices = await _context.SupplierInvoices
            .AsNoTracking()
            .Where(i => i.SupplierId == supplierId && i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get payments in period
        var payments = await _context.SupplierPayments
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Combine and sort by date
        var lines = new List<SupplierStatementLine>();
        decimal runningBalance = statement.OpeningBalance;

        // Add opening balance line
        lines.Add(new SupplierStatementLine
        {
            Date = startDate,
            Reference = "B/F",
            Description = "Opening Balance",
            Debit = 0,
            Credit = 0,
            RunningBalance = runningBalance,
            Type = "Opening"
        });

        // Add invoices
        foreach (var invoice in invoices)
        {
            runningBalance += invoice.TotalAmount;
            statement.TotalInvoices += invoice.TotalAmount;

            lines.Add(new SupplierStatementLine
            {
                Date = invoice.InvoiceDate,
                Reference = invoice.InvoiceNumber,
                Description = $"Invoice - {(invoice.PurchaseOrderId.HasValue ? $"PO Linked" : "Direct")}",
                Debit = invoice.TotalAmount,
                Credit = 0,
                RunningBalance = runningBalance,
                Type = "Invoice"
            });
        }

        // Add payments
        foreach (var payment in payments)
        {
            runningBalance -= payment.Amount;
            statement.TotalPayments += payment.Amount;

            lines.Add(new SupplierStatementLine
            {
                Date = payment.PaymentDate,
                Reference = payment.Reference ?? $"PMT-{payment.Id}",
                Description = $"Payment - {payment.PaymentMethod ?? "N/A"}",
                Debit = 0,
                Credit = payment.Amount,
                RunningBalance = runningBalance,
                Type = "Payment"
            });
        }

        // Sort by date
        statement.Lines = lines.OrderBy(l => l.Date).ThenBy(l => l.Type == "Opening" ? 0 : 1).ToList();

        // Recalculate running balance in sorted order
        runningBalance = statement.OpeningBalance;
        foreach (var line in statement.Lines.Skip(1)) // Skip opening balance line
        {
            runningBalance += line.Debit - line.Credit;
            line.RunningBalance = runningBalance;
        }

        statement.ClosingBalance = runningBalance;

        return statement;
    }

    #endregion
}
