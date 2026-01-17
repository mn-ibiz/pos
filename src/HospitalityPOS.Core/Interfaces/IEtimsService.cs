using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for Kenya eTIMS (electronic Tax Invoice Management System) operations.
/// </summary>
public interface IEtimsService
{
    // Device Management
    Task<EtimsDevice> RegisterDeviceAsync(EtimsDevice device, CancellationToken cancellationToken = default);
    Task<EtimsDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsDevice>> GetAllDevicesAsync(CancellationToken cancellationToken = default);
    Task<EtimsDevice> UpdateDeviceAsync(EtimsDevice device, CancellationToken cancellationToken = default);
    Task<bool> TestDeviceConnectionAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<bool> ActivateDeviceAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateDeviceAsync(int deviceId, CancellationToken cancellationToken = default);

    // Invoice Generation & Submission
    Task<EtimsInvoice> GenerateInvoiceAsync(int receiptId, CancellationToken cancellationToken = default);
    Task<EtimsInvoice> SubmitInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<EtimsInvoice?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EtimsInvoice?> GetInvoiceByReceiptIdAsync(int receiptId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsInvoice>> GetInvoicesByStatusAsync(EtimsSubmissionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsInvoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default);

    // Credit Note Submission
    Task<EtimsCreditNote> GenerateCreditNoteAsync(int originalInvoiceId, string reason, CancellationToken cancellationToken = default);
    Task<EtimsCreditNote> SubmitCreditNoteAsync(int creditNoteId, CancellationToken cancellationToken = default);
    Task<EtimsCreditNote?> GetCreditNoteByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsCreditNote>> GetCreditNotesByInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<string> GenerateCreditNoteNumberAsync(CancellationToken cancellationToken = default);

    // Offline Queue Management
    Task<EtimsQueueEntry> QueueForSubmissionAsync(EtimsDocumentType documentType, int documentId, int priority = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsQueueEntry>> GetPendingQueueEntriesAsync(int maxCount = 50, CancellationToken cancellationToken = default);
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
    Task<int> GetQueueCountAsync(CancellationToken cancellationToken = default);
    Task<EtimsQueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default);

    // Sync & Retry
    Task RetryFailedSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EtimsSyncLog>> GetSyncLogsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Dashboard & Reports
    Task<EtimsDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);
    Task<EtimsComplianceReport> GetComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Validation
    Task<bool> ValidateCustomerPinAsync(string pin, CancellationToken cancellationToken = default);
    Task<KraCustomerInfo?> LookupCustomerByPinAsync(string pin, CancellationToken cancellationToken = default);
}

/// <summary>
/// eTIMS queue statistics.
/// </summary>
public class EtimsQueueStats
{
    public int TotalPending { get; set; }
    public int TotalFailed { get; set; }
    public int TotalSubmitted { get; set; }
    public int FailedInvoices { get; set; }
    public int FailedCreditNotes { get; set; }
    public DateTime? OldestPendingDate { get; set; }
    public DateTime? LastSuccessfulSubmission { get; set; }
}

/// <summary>
/// eTIMS dashboard data.
/// </summary>
public class EtimsDashboardData
{
    public bool IsDeviceRegistered { get; set; }
    public bool IsDeviceActive { get; set; }
    public string? DeviceSerialNumber { get; set; }
    public DateTime? LastCommunication { get; set; }
    public EtimsDeviceStatus DeviceStatus { get; set; }

    public int TodayInvoicesSubmitted { get; set; }
    public int TodayInvoicesPending { get; set; }
    public int TodayInvoicesFailed { get; set; }
    public decimal TodayTotalAmount { get; set; }
    public decimal TodayTaxAmount { get; set; }

    public int QueuedCount { get; set; }
    public int FailedCount { get; set; }

    public decimal MonthTotalSales { get; set; }
    public decimal MonthTotalTax { get; set; }
    public int MonthInvoiceCount { get; set; }
}

/// <summary>
/// eTIMS compliance report.
/// </summary>
public class EtimsComplianceReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int TotalReceipts { get; set; }
    public int SubmittedInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int FailedInvoices { get; set; }
    public decimal ComplianceRate { get; set; }

    public decimal TotalSalesAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal StandardRatedAmount { get; set; }
    public decimal ZeroRatedAmount { get; set; }
    public decimal ExemptAmount { get; set; }

    public int CreditNotesIssued { get; set; }
    public decimal CreditNotesAmount { get; set; }

    public List<EtimsDailySubmission> DailySubmissions { get; set; } = [];
}

/// <summary>
/// Daily eTIMS submission summary.
/// </summary>
public class EtimsDailySubmission
{
    public DateTime Date { get; set; }
    public int InvoicesSubmitted { get; set; }
    public int InvoicesFailed { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

/// <summary>
/// KRA customer information lookup result.
/// </summary>
public class KraCustomerInfo
{
    public string Pin { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? PostalAddress { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsValidPin { get; set; }
    public string? TaxObligationStatus { get; set; }
}
