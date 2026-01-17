using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing supplier credit and payments.
/// </summary>
public interface ISupplierCreditService
{
    #region Credit Terms Management

    /// <summary>
    /// Updates credit terms for a supplier.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="creditLimit">The credit limit amount.</param>
    /// <param name="paymentTermDays">Payment term in days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated supplier.</returns>
    Task<Supplier> UpdateCreditTermsAsync(int supplierId, decimal creditLimit, int paymentTermDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a supplier has exceeded their credit limit.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if credit limit is exceeded; otherwise, false.</returns>
    Task<bool> IsCreditLimitExceededAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available credit for a supplier (credit limit - current balance).
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available credit amount.</returns>
    Task<decimal> GetAvailableCreditAsync(int supplierId, CancellationToken cancellationToken = default);

    #endregion

    #region Invoice Management

    /// <summary>
    /// Creates a supplier invoice.
    /// </summary>
    /// <param name="invoice">The invoice to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created invoice.</returns>
    Task<SupplierInvoice> CreateInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invoices for a supplier.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="includeFullyPaid">Whether to include fully paid invoices.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of invoices.</returns>
    Task<IReadOnlyList<SupplierInvoice>> GetSupplierInvoicesAsync(int supplierId, bool includeFullyPaid = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice if found; otherwise, null.</returns>
    Task<SupplierInvoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unpaid or partially paid invoices.
    /// </summary>
    /// <param name="supplierId">Optional supplier ID to filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of outstanding invoices.</returns>
    Task<IReadOnlyList<SupplierInvoice>> GetOutstandingInvoicesAsync(int? supplierId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue invoices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of overdue invoices.</returns>
    Task<IReadOnlyList<SupplierInvoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates invoice status based on payment amount.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateInvoiceStatusAsync(int invoiceId, CancellationToken cancellationToken = default);

    #endregion

    #region Payment Management

    /// <summary>
    /// Records a payment to a supplier.
    /// </summary>
    /// <param name="payment">The payment to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created payment.</returns>
    Task<SupplierPayment> RecordPaymentAsync(SupplierPayment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payments for a supplier.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of payments.</returns>
    Task<IReadOnlyList<SupplierPayment>> GetSupplierPaymentsAsync(int supplierId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments for a specific invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of payments.</returns>
    Task<IReadOnlyList<SupplierPayment>> GetInvoicePaymentsAsync(int invoiceId, CancellationToken cancellationToken = default);

    #endregion

    #region Balance Management

    /// <summary>
    /// Recalculates and updates the current balance for a supplier.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated balance.</returns>
    Task<decimal> RecalculateBalanceAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aging summary for a supplier's outstanding invoices.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aging summary.</returns>
    Task<SupplierAgingSummary> GetAgingSummaryAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aging summary for all suppliers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of aging summaries.</returns>
    Task<IReadOnlyList<SupplierAgingSummary>> GetAllAgingSummariesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Statement Generation

    /// <summary>
    /// Generates a supplier statement for a date range.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="startDate">Statement start date.</param>
    /// <param name="endDate">Statement end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The supplier statement data.</returns>
    Task<SupplierStatement> GenerateStatementAsync(int supplierId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Aging summary for supplier invoices.
/// </summary>
public class SupplierAgingSummary
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string SupplierCode { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal Current { get; set; } // 0-30 days
    public decimal Days30 { get; set; } // 31-60 days
    public decimal Days60 { get; set; } // 61-90 days
    public decimal Days90Plus { get; set; } // 90+ days
    public int OverdueInvoiceCount { get; set; }
    public DateTime? OldestInvoiceDate { get; set; }
}

/// <summary>
/// Supplier statement data.
/// </summary>
public class SupplierStatement
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string SupplierCode { get; set; } = "";
    public string? SupplierAddress { get; set; }
    public string? SupplierPhone { get; set; }
    public string? SupplierEmail { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalInvoices { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public IList<SupplierStatementLine> Lines { get; set; } = new List<SupplierStatementLine>();
}

/// <summary>
/// A line item in the supplier statement.
/// </summary>
public class SupplierStatementLine
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Debit { get; set; } // Invoice amounts
    public decimal Credit { get; set; } // Payment amounts
    public decimal RunningBalance { get; set; }
    public string Type { get; set; } = ""; // "Invoice" or "Payment"
}
