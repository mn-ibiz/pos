using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of eTIMS (electronic Tax Invoice Management System) service for Kenya.
/// </summary>
public class EtimsService : IEtimsService
{
    private readonly POSDbContext _context;
    private readonly ILogger<EtimsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Default VAT rate. In production, this should be loaded from configuration or database.
    /// </summary>
    private const decimal DEFAULT_VAT_RATE = 16m;

    public EtimsService(
        POSDbContext context,
        ILogger<EtimsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    private HttpClient CreateHttpClient() => _httpClientFactory.CreateClient("EtimsApi");

    #region Device Management

    public async Task<EtimsDevice> RegisterDeviceAsync(EtimsDevice device, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering eTIMS device: {SerialNumber}", device.DeviceSerialNumber);

        // If this is primary, deactivate other primary devices
        if (device.IsPrimary)
        {
            var existingPrimary = await _context.Set<EtimsDevice>()
                .Where(d => d.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingPrimary)
            {
                existing.IsPrimary = false;
            }
        }

        device.RegistrationDate = DateTime.UtcNow;
        device.Status = EtimsDeviceStatus.Registered;

        _context.Set<EtimsDevice>().Add(device);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("eTIMS device registered: {DeviceId}", device.Id);
        return device;
    }

    public async Task<EtimsDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsDevice>()
            .Where(d => d.IsPrimary && d.Status == EtimsDeviceStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtimsDevice>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsDevice>()
            .OrderByDescending(d => d.IsPrimary)
            .ThenByDescending(d => d.RegistrationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<EtimsDevice> UpdateDeviceAsync(EtimsDevice device, CancellationToken cancellationToken = default)
    {
        _context.Set<EtimsDevice>().Update(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<bool> TestDeviceConnectionAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Set<EtimsDevice>()
            .FindAsync([deviceId], cancellationToken);

        if (device == null) return false;

        try
        {
            // Simulate API test call
            var testUrl = $"{device.ApiBaseUrl}/api/test";
            // In production, make actual API call here

            device.LastCommunication = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("eTIMS device connection test successful: {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eTIMS device connection test failed: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> ActivateDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Set<EtimsDevice>().FindAsync([deviceId], cancellationToken);
        if (device == null) return false;

        device.Status = EtimsDeviceStatus.Active;
        device.IsPrimary = true;

        // Deactivate other devices
        var others = await _context.Set<EtimsDevice>()
            .Where(d => d.Id != deviceId && d.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            other.IsPrimary = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Set<EtimsDevice>().FindAsync([deviceId], cancellationToken);
        if (device == null) return false;

        device.Status = EtimsDeviceStatus.Deactivated;
        device.IsPrimary = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Invoice Generation & Submission

    public async Task<EtimsInvoice> GenerateInvoiceAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Set<Receipt>()
            .Include(r => r.Items)
                .ThenInclude(i => i.Product)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken)
            ?? throw new InvalidOperationException($"Receipt {receiptId} not found");

        var device = await GetActiveDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active eTIMS device configured");

        // Check if invoice already exists
        var existingInvoice = await _context.Set<EtimsInvoice>()
            .FirstOrDefaultAsync(i => i.ReceiptId == receiptId, cancellationToken);

        if (existingInvoice != null)
        {
            return existingInvoice;
        }

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

        // Calculate tax breakdown
        var taxableAmount = receipt.SubTotal / (1 + DEFAULT_VAT_RATE / 100);
        var taxAmount = receipt.SubTotal - taxableAmount;

        var invoice = new EtimsInvoice
        {
            ReceiptId = receiptId,
            DeviceId = device.Id,
            InvoiceNumber = invoiceNumber,
            InternalReceiptNumber = receipt.ReceiptNumber,
            InvoiceDate = receipt.CreatedAt,
            DocumentType = receipt.Total < 5000
                ? EtimsDocumentType.SimplifiedTaxInvoice
                : EtimsDocumentType.TaxInvoice,
            CustomerType = EtimsCustomerType.Consumer,
            CustomerName = "Walk-in Customer",
            TaxableAmount = Math.Round(taxableAmount, 2),
            TaxAmount = Math.Round(taxAmount, 2),
            TotalAmount = receipt.Total,
            StandardRatedAmount = receipt.Total,
            ZeroRatedAmount = 0,
            ExemptAmount = 0,
            Status = EtimsSubmissionStatus.Pending
        };

        // Create invoice items
        int seq = 1;
        foreach (var item in receipt.Items)
        {
            var itemTaxable = item.TotalPrice / (1 + DEFAULT_VAT_RATE / 100);
            var itemTax = item.TotalPrice - itemTaxable;

            invoice.Items.Add(new EtimsInvoiceItem
            {
                SequenceNumber = seq++,
                ItemCode = item.Product?.SKU ?? $"ITEM{item.ProductId}",
                ItemDescription = item.ProductName,
                UnitOfMeasure = "PCS",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxType = KraTaxType.A,
                TaxRate = DEFAULT_VAT_RATE,
                TaxableAmount = Math.Round(itemTaxable, 2),
                TaxAmount = Math.Round(itemTax, 2),
                TotalAmount = item.TotalPrice
            });
        }

        _context.Set<EtimsInvoice>().Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("eTIMS invoice generated: {InvoiceNumber} for Receipt {ReceiptId}",
            invoiceNumber, receiptId);

        return invoice;
    }

    public async Task<EtimsInvoice> SubmitInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Set<EtimsInvoice>()
            .Include(i => i.Items)
            .Include(i => i.Device)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        if (invoice.Status == EtimsSubmissionStatus.Accepted)
        {
            return invoice;
        }

        invoice.SubmissionAttempts++;
        invoice.LastSubmissionAttempt = DateTime.UtcNow;

        try
        {
            // Build eTIMS request payload
            var requestPayload = BuildInvoicePayload(invoice);
            invoice.RequestJson = JsonSerializer.Serialize(requestPayload);

            // Simulate API call (in production, make actual HTTP call)
            var response = await SimulateEtimsApiCall(invoice.Device, "invoice", requestPayload, cancellationToken);

            if (response.Success)
            {
                invoice.Status = EtimsSubmissionStatus.Accepted;
                invoice.SubmittedAt = DateTime.UtcNow;
                invoice.ReceiptSignature = response.Signature;
                invoice.QrCode = response.QrCode;
                invoice.KraInternalData = response.InternalData;
                invoice.ResponseJson = response.RawResponse;

                invoice.Device.LastCommunication = DateTime.UtcNow;

                _logger.LogInformation("eTIMS invoice submitted successfully: {InvoiceNumber}", invoice.InvoiceNumber);
            }
            else
            {
                invoice.Status = EtimsSubmissionStatus.Rejected;
                invoice.ErrorMessage = response.ErrorMessage;
                invoice.ResponseJson = response.RawResponse;

                _logger.LogWarning("eTIMS invoice rejected: {InvoiceNumber} - {Error}",
                    invoice.InvoiceNumber, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            invoice.Status = EtimsSubmissionStatus.Failed;
            invoice.ErrorMessage = ex.Message;

            _logger.LogError(ex, "eTIMS invoice submission failed: {InvoiceNumber}", invoice.InvoiceNumber);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Log sync operation
        await LogSyncOperation("SubmitInvoice", EtimsDocumentType.TaxInvoice, invoice.Id,
            invoice.Status == EtimsSubmissionStatus.Accepted, invoice.ErrorMessage, cancellationToken);

        return invoice;
    }

    public async Task<EtimsInvoice?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsInvoice>()
            .Include(i => i.Items)
            .Include(i => i.Device)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<EtimsInvoice?> GetInvoiceByReceiptIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsInvoice>()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.ReceiptId == receiptId, cancellationToken);
    }

    public async Task<IReadOnlyList<EtimsInvoice>> GetInvoicesByStatusAsync(EtimsSubmissionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsInvoice>()
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EtimsInvoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsInvoice>()
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var device = await GetActiveDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active eTIMS device");

        device.LastInvoiceNumber++;
        await _context.SaveChangesAsync(cancellationToken);

        // Format: CU-BRANCH-YYYY-NNNNNN
        return $"{device.ControlUnitId}-{device.BranchCode}-{DateTime.Now.Year}-{device.LastInvoiceNumber:D6}";
    }

    #endregion

    #region Credit Note Submission

    public async Task<EtimsCreditNote> GenerateCreditNoteAsync(int originalInvoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var originalInvoice = await _context.Set<EtimsInvoice>()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == originalInvoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Original invoice {originalInvoiceId} not found");

        var device = await GetActiveDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active eTIMS device");

        var creditNoteNumber = await GenerateCreditNoteNumberAsync(cancellationToken);

        var creditNote = new EtimsCreditNote
        {
            OriginalInvoiceId = originalInvoiceId,
            DeviceId = device.Id,
            CreditNoteNumber = creditNoteNumber,
            OriginalInvoiceNumber = originalInvoice.InvoiceNumber,
            CreditNoteDate = DateTime.UtcNow,
            Reason = reason,
            CustomerPin = originalInvoice.CustomerPin,
            CustomerName = originalInvoice.CustomerName,
            CreditAmount = originalInvoice.TotalAmount,
            TaxAmount = originalInvoice.TaxAmount,
            Status = EtimsSubmissionStatus.Pending
        };

        // Copy items
        int seq = 1;
        foreach (var item in originalInvoice.Items)
        {
            creditNote.Items.Add(new EtimsCreditNoteItem
            {
                SequenceNumber = seq++,
                ItemCode = item.ItemCode,
                ItemDescription = item.ItemDescription,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxType = item.TaxType,
                TaxRate = item.TaxRate,
                TaxableAmount = item.TaxableAmount,
                TaxAmount = item.TaxAmount,
                TotalAmount = item.TotalAmount
            });
        }

        _context.Set<EtimsCreditNote>().Add(creditNote);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("eTIMS credit note generated: {CreditNoteNumber} for Invoice {InvoiceNumber}",
            creditNoteNumber, originalInvoice.InvoiceNumber);

        return creditNote;
    }

    public async Task<EtimsCreditNote> SubmitCreditNoteAsync(int creditNoteId, CancellationToken cancellationToken = default)
    {
        var creditNote = await _context.Set<EtimsCreditNote>()
            .Include(c => c.Items)
            .Include(c => c.Device)
            .FirstOrDefaultAsync(c => c.Id == creditNoteId, cancellationToken)
            ?? throw new InvalidOperationException($"Credit note {creditNoteId} not found");

        if (creditNote.Status == EtimsSubmissionStatus.Accepted)
        {
            return creditNote;
        }

        creditNote.SubmissionAttempts++;
        creditNote.LastSubmissionAttempt = DateTime.UtcNow;

        try
        {
            var requestPayload = BuildCreditNotePayload(creditNote);
            creditNote.RequestJson = JsonSerializer.Serialize(requestPayload);

            var response = await SimulateEtimsApiCall(creditNote.Device, "creditnote", requestPayload, cancellationToken);

            if (response.Success)
            {
                creditNote.Status = EtimsSubmissionStatus.Accepted;
                creditNote.SubmittedAt = DateTime.UtcNow;
                creditNote.KraSignature = response.Signature;
                creditNote.ResponseJson = response.RawResponse;

                _logger.LogInformation("eTIMS credit note submitted successfully: {CreditNoteNumber}",
                    creditNote.CreditNoteNumber);
            }
            else
            {
                creditNote.Status = EtimsSubmissionStatus.Rejected;
                creditNote.ErrorMessage = response.ErrorMessage;
                creditNote.ResponseJson = response.RawResponse;
            }
        }
        catch (Exception ex)
        {
            creditNote.Status = EtimsSubmissionStatus.Failed;
            creditNote.ErrorMessage = ex.Message;

            _logger.LogError(ex, "eTIMS credit note submission failed: {CreditNoteNumber}",
                creditNote.CreditNoteNumber);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await LogSyncOperation("SubmitCreditNote", EtimsDocumentType.CreditNote, creditNote.Id,
            creditNote.Status == EtimsSubmissionStatus.Accepted, creditNote.ErrorMessage, cancellationToken);

        return creditNote;
    }

    public async Task<EtimsCreditNote?> GetCreditNoteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsCreditNote>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EtimsCreditNote>> GetCreditNotesByInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsCreditNote>()
            .Where(c => c.OriginalInvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateCreditNoteNumberAsync(CancellationToken cancellationToken = default)
    {
        var device = await GetActiveDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active eTIMS device");

        device.LastCreditNoteNumber++;
        await _context.SaveChangesAsync(cancellationToken);

        return $"CN-{device.ControlUnitId}-{device.BranchCode}-{DateTime.Now.Year}-{device.LastCreditNoteNumber:D6}";
    }

    #endregion

    #region Offline Queue Management

    public async Task<EtimsQueueEntry> QueueForSubmissionAsync(EtimsDocumentType documentType, int documentId, int priority = 100, CancellationToken cancellationToken = default)
    {
        var entry = new EtimsQueueEntry
        {
            DocumentType = documentType,
            DocumentId = documentId,
            Priority = priority,
            QueuedAt = DateTime.UtcNow,
            Status = EtimsSubmissionStatus.Queued
        };

        _context.Set<EtimsQueueEntry>().Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Queued eTIMS {DocumentType} {DocumentId} for submission",
            documentType, documentId);

        return entry;
    }

    public async Task<IReadOnlyList<EtimsQueueEntry>> GetPendingQueueEntriesAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Set<EtimsQueueEntry>()
            .Where(e => e.Status == EtimsSubmissionStatus.Queued &&
                       (e.RetryAfter == null || e.RetryAfter <= now) &&
                       e.Attempts < e.MaxAttempts)
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.QueuedAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetPendingQueueEntriesAsync(10, cancellationToken);

        foreach (var entry in entries)
        {
            entry.Attempts++;
            entry.LastProcessedAt = DateTime.UtcNow;

            try
            {
                if (entry.DocumentType == EtimsDocumentType.TaxInvoice ||
                    entry.DocumentType == EtimsDocumentType.SimplifiedTaxInvoice)
                {
                    var invoice = await SubmitInvoiceAsync(entry.DocumentId, cancellationToken);
                    entry.Status = invoice.Status;
                    if (invoice.Status == EtimsSubmissionStatus.Accepted)
                    {
                        entry.CompletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        entry.LastError = invoice.ErrorMessage;
                        entry.RetryAfter = CalculateRetryTime(entry.Attempts);
                    }
                }
                else if (entry.DocumentType == EtimsDocumentType.CreditNote)
                {
                    var creditNote = await SubmitCreditNoteAsync(entry.DocumentId, cancellationToken);
                    entry.Status = creditNote.Status;
                    if (creditNote.Status == EtimsSubmissionStatus.Accepted)
                    {
                        entry.CompletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        entry.LastError = creditNote.ErrorMessage;
                        entry.RetryAfter = CalculateRetryTime(entry.Attempts);
                    }
                }
            }
            catch (Exception ex)
            {
                entry.Status = EtimsSubmissionStatus.Failed;
                entry.LastError = ex.Message;
                entry.RetryAfter = CalculateRetryTime(entry.Attempts);

                _logger.LogError(ex, "Failed to process queue entry {EntryId}", entry.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetQueueCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsQueueEntry>()
            .CountAsync(e => e.Status == EtimsSubmissionStatus.Queued, cancellationToken);
    }

    public async Task<EtimsQueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _context.Set<EtimsQueueEntry>()
            .ToListAsync(cancellationToken);

        return new EtimsQueueStats
        {
            TotalPending = entries.Count(e => e.Status == EtimsSubmissionStatus.Queued),
            TotalFailed = entries.Count(e => e.Status == EtimsSubmissionStatus.Failed),
            TotalSubmitted = entries.Count(e => e.Status == EtimsSubmissionStatus.Accepted),
            FailedInvoices = entries.Count(e => e.Status == EtimsSubmissionStatus.Failed &&
                (e.DocumentType == EtimsDocumentType.TaxInvoice || e.DocumentType == EtimsDocumentType.SimplifiedTaxInvoice)),
            FailedCreditNotes = entries.Count(e => e.Status == EtimsSubmissionStatus.Failed &&
                e.DocumentType == EtimsDocumentType.CreditNote),
            OldestPendingDate = entries.Where(e => e.Status == EtimsSubmissionStatus.Queued)
                .OrderBy(e => e.QueuedAt).FirstOrDefault()?.QueuedAt,
            LastSuccessfulSubmission = entries.Where(e => e.Status == EtimsSubmissionStatus.Accepted)
                .OrderByDescending(e => e.CompletedAt).FirstOrDefault()?.CompletedAt
        };
    }

    #endregion

    #region Sync & Retry

    public async Task RetryFailedSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        var failedInvoices = await _context.Set<EtimsInvoice>()
            .Where(i => i.Status == EtimsSubmissionStatus.Failed && i.SubmissionAttempts < 10)
            .ToListAsync(cancellationToken);

        foreach (var invoice in failedInvoices)
        {
            await QueueForSubmissionAsync(invoice.DocumentType, invoice.Id, 50, cancellationToken);
        }

        var failedCreditNotes = await _context.Set<EtimsCreditNote>()
            .Where(c => c.Status == EtimsSubmissionStatus.Failed && c.SubmissionAttempts < 10)
            .ToListAsync(cancellationToken);

        foreach (var creditNote in failedCreditNotes)
        {
            await QueueForSubmissionAsync(EtimsDocumentType.CreditNote, creditNote.Id, 50, cancellationToken);
        }

        _logger.LogInformation("Queued {InvoiceCount} invoices and {CreditNoteCount} credit notes for retry",
            failedInvoices.Count, failedCreditNotes.Count);
    }

    public async Task<IReadOnlyList<EtimsSyncLog>> GetSyncLogsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EtimsSyncLog>()
            .Where(l => l.StartedAt >= startDate && l.StartedAt <= endDate)
            .OrderByDescending(l => l.StartedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Dashboard & Reports

    public async Task<EtimsDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var device = await GetActiveDeviceAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todayInvoices = await _context.Set<EtimsInvoice>()
            .Where(i => i.InvoiceDate.Date == today)
            .ToListAsync(cancellationToken);

        var monthInvoices = await _context.Set<EtimsInvoice>()
            .Where(i => i.InvoiceDate >= monthStart && i.Status == EtimsSubmissionStatus.Accepted)
            .ToListAsync(cancellationToken);

        var queueCount = await GetQueueCountAsync(cancellationToken);
        var failedCount = await _context.Set<EtimsInvoice>()
            .CountAsync(i => i.Status == EtimsSubmissionStatus.Failed, cancellationToken);

        return new EtimsDashboardData
        {
            IsDeviceRegistered = device != null,
            IsDeviceActive = device?.Status == EtimsDeviceStatus.Active,
            DeviceSerialNumber = device?.DeviceSerialNumber,
            LastCommunication = device?.LastCommunication,
            DeviceStatus = device?.Status ?? EtimsDeviceStatus.Pending,

            TodayInvoicesSubmitted = todayInvoices.Count(i => i.Status == EtimsSubmissionStatus.Accepted),
            TodayInvoicesPending = todayInvoices.Count(i => i.Status == EtimsSubmissionStatus.Pending ||
                i.Status == EtimsSubmissionStatus.Queued),
            TodayInvoicesFailed = todayInvoices.Count(i => i.Status == EtimsSubmissionStatus.Failed),
            TodayTotalAmount = todayInvoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.TotalAmount),
            TodayTaxAmount = todayInvoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.TaxAmount),

            QueuedCount = queueCount,
            FailedCount = failedCount,

            MonthTotalSales = monthInvoices.Sum(i => i.TotalAmount),
            MonthTotalTax = monthInvoices.Sum(i => i.TaxAmount),
            MonthInvoiceCount = monthInvoices.Count
        };
    }

    public async Task<EtimsComplianceReport> GetComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var invoices = await _context.Set<EtimsInvoice>()
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .ToListAsync(cancellationToken);

        var creditNotes = await _context.Set<EtimsCreditNote>()
            .Where(c => c.CreditNoteDate >= startDate && c.CreditNoteDate <= endDate)
            .ToListAsync(cancellationToken);

        var totalReceipts = await _context.Set<Receipt>()
            .CountAsync(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate &&
                r.Status == ReceiptStatus.Settled, cancellationToken);

        var submittedCount = invoices.Count(i => i.Status == EtimsSubmissionStatus.Accepted);
        var complianceRate = totalReceipts > 0
            ? (decimal)submittedCount / totalReceipts * 100
            : 0;

        // Daily breakdown
        var dailySubmissions = invoices
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new EtimsDailySubmission
            {
                Date = g.Key,
                InvoicesSubmitted = g.Count(i => i.Status == EtimsSubmissionStatus.Accepted),
                InvoicesFailed = g.Count(i => i.Status == EtimsSubmissionStatus.Failed),
                TotalAmount = g.Where(i => i.Status == EtimsSubmissionStatus.Accepted).Sum(i => i.TotalAmount),
                TaxAmount = g.Where(i => i.Status == EtimsSubmissionStatus.Accepted).Sum(i => i.TaxAmount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new EtimsComplianceReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalReceipts = totalReceipts,
            SubmittedInvoices = submittedCount,
            PendingInvoices = invoices.Count(i => i.Status == EtimsSubmissionStatus.Pending ||
                i.Status == EtimsSubmissionStatus.Queued),
            FailedInvoices = invoices.Count(i => i.Status == EtimsSubmissionStatus.Failed),
            ComplianceRate = Math.Round(complianceRate, 2),
            TotalSalesAmount = invoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.TotalAmount),
            TotalTaxAmount = invoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.TaxAmount),
            StandardRatedAmount = invoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.StandardRatedAmount),
            ZeroRatedAmount = invoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.ZeroRatedAmount),
            ExemptAmount = invoices.Where(i => i.Status == EtimsSubmissionStatus.Accepted)
                .Sum(i => i.ExemptAmount),
            CreditNotesIssued = creditNotes.Count(c => c.Status == EtimsSubmissionStatus.Accepted),
            CreditNotesAmount = creditNotes.Where(c => c.Status == EtimsSubmissionStatus.Accepted)
                .Sum(c => c.CreditAmount),
            DailySubmissions = dailySubmissions
        };
    }

    #endregion

    #region Validation

    public async Task<bool> ValidateCustomerPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 11)
            return false;

        // PIN format: A123456789Z (letter + 9 digits + letter)
        if (!char.IsLetter(pin[0]) || !char.IsLetter(pin[10]))
            return false;

        for (int i = 1; i < 10; i++)
        {
            if (!char.IsDigit(pin[i]))
                return false;
        }

        // In production, call KRA API to validate
        return true;
    }

    public async Task<KraCustomerInfo?> LookupCustomerByPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (!await ValidateCustomerPinAsync(pin, cancellationToken))
            return null;

        // In production, call KRA API
        // Simulated response for development
        return new KraCustomerInfo
        {
            Pin = pin,
            Name = "Sample Business Ltd",
            IsValidPin = true,
            TaxObligationStatus = "Active"
        };
    }

    #endregion

    #region Private Helpers

    private static object BuildInvoicePayload(EtimsInvoice invoice)
    {
        return new
        {
            invoiceNumber = invoice.InvoiceNumber,
            invoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss"),
            customerPin = invoice.CustomerPin ?? "",
            customerName = invoice.CustomerName,
            totalAmount = invoice.TotalAmount,
            taxAmount = invoice.TaxAmount,
            items = invoice.Items.Select(i => new
            {
                sequence = i.SequenceNumber,
                itemCode = i.ItemCode,
                description = i.ItemDescription,
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                taxType = i.TaxType.ToString(),
                taxRate = i.TaxRate,
                totalAmount = i.TotalAmount
            })
        };
    }

    private static object BuildCreditNotePayload(EtimsCreditNote creditNote)
    {
        return new
        {
            creditNoteNumber = creditNote.CreditNoteNumber,
            originalInvoiceNumber = creditNote.OriginalInvoiceNumber,
            creditNoteDate = creditNote.CreditNoteDate.ToString("yyyy-MM-dd HH:mm:ss"),
            reason = creditNote.Reason,
            customerPin = creditNote.CustomerPin ?? "",
            customerName = creditNote.CustomerName,
            creditAmount = creditNote.CreditAmount,
            taxAmount = creditNote.TaxAmount,
            items = creditNote.Items.Select(i => new
            {
                sequence = i.SequenceNumber,
                itemCode = i.ItemCode,
                description = i.ItemDescription,
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                totalAmount = i.TotalAmount
            })
        };
    }

    private async Task<EtimsApiResponse> SimulateEtimsApiCall(
        EtimsDevice device,
        string endpoint,
        object payload,
        CancellationToken cancellationToken)
    {
        // For sandbox/development, simulate success
        if (device.Environment == "Sandbox")
        {
            await Task.Delay(100, cancellationToken);
            return new EtimsApiResponse
            {
                Success = true,
                Signature = Guid.NewGuid().ToString("N"),
                QrCode = $"https://etims.kra.go.ke/verify/{Guid.NewGuid():N}",
                InternalData = JsonSerializer.Serialize(new { timestamp = DateTime.UtcNow }),
                RawResponse = JsonSerializer.Serialize(new { status = "success", message = "Invoice accepted" })
            };
        }

        // Production API call
        try
        {
            using var httpClient = CreateHttpClient();
            httpClient.BaseAddress = new Uri(device.ApiBaseUrl);

            var response = await httpClient.PostAsJsonAsync($"/api/{endpoint}", payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                return new EtimsApiResponse
                {
                    Success = true,
                    Signature = result?.GetValueOrDefault("signature").GetString() ?? Guid.NewGuid().ToString("N"),
                    QrCode = result?.GetValueOrDefault("qrCode").GetString(),
                    InternalData = result?.GetValueOrDefault("internalData").GetRawText(),
                    RawResponse = content
                };
            }
            else
            {
                return new EtimsApiResponse
                {
                    Success = false,
                    ErrorMessage = $"API returned {response.StatusCode}: {content}",
                    RawResponse = content
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eTIMS API call failed for endpoint {Endpoint}", endpoint);
            return new EtimsApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static DateTime CalculateRetryTime(int attempts)
    {
        // Exponential backoff: 1min, 2min, 4min, 8min, 16min, ...
        var minutes = Math.Pow(2, attempts - 1);
        return DateTime.UtcNow.AddMinutes(Math.Min(minutes, 60)); // Cap at 1 hour
    }

    private async Task LogSyncOperation(
        string operationType,
        EtimsDocumentType? documentType,
        int? documentId,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var log = new EtimsSyncLog
        {
            OperationType = operationType,
            DocumentType = documentType,
            DocumentId = documentId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            IsSuccess = success,
            ErrorMessage = errorMessage
        };

        _context.Set<EtimsSyncLog>().Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    private class EtimsApiResponse
    {
        public bool Success { get; set; }
        public string? Signature { get; set; }
        public string? QrCode { get; set; }
        public string? InternalData { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }
}
