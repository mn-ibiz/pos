using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for Hotel Property Management System integration.
/// </summary>
public interface IHotelPMSService
{
    #region Connection Configuration

    /// <summary>
    /// Creates a new PMS configuration.
    /// </summary>
    Task<PMSConfiguration> CreateConfigurationAsync(PMSConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a PMS configuration by ID.
    /// </summary>
    Task<PMSConfiguration?> GetConfigurationByIdAsync(int configId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default PMS configuration.
    /// </summary>
    Task<PMSConfiguration?> GetDefaultConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all PMS configurations.
    /// </summary>
    Task<IEnumerable<PMSConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a PMS configuration.
    /// </summary>
    Task<PMSConfiguration> UpdateConfigurationAsync(PMSConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a PMS configuration.
    /// </summary>
    Task DeleteConfigurationAsync(int configId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests PMS connection.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(int configId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes OAuth tokens if needed.
    /// </summary>
    Task RefreshTokensAsync(int configId, CancellationToken cancellationToken = default);

    #endregion

    #region Revenue Center Mapping

    /// <summary>
    /// Creates a revenue center mapping.
    /// </summary>
    Task<PMSRevenueCenter> CreateRevenueCenterAsync(PMSRevenueCenter revenueCenter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revenue centers for a configuration.
    /// </summary>
    Task<IEnumerable<PMSRevenueCenter>> GetRevenueCentersAsync(int configId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a revenue center mapping.
    /// </summary>
    Task<PMSRevenueCenter> UpdateRevenueCenterAsync(PMSRevenueCenter revenueCenter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a revenue center mapping.
    /// </summary>
    Task DeleteRevenueCenterAsync(int revenueCenterId, CancellationToken cancellationToken = default);

    #endregion

    #region Room Charge Posting

    /// <summary>
    /// Posts a room charge to PMS.
    /// </summary>
    Task<RoomChargeResult> PostRoomChargeAsync(RoomChargeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts multiple room charges in batch.
    /// </summary>
    Task<IEnumerable<RoomChargeResult>> PostRoomChargesBatchAsync(IEnumerable<RoomChargeRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a room charge posting by ID.
    /// </summary>
    Task<RoomChargePosting?> GetPostingByIdAsync(int postingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets room charge postings by status.
    /// </summary>
    Task<IEnumerable<RoomChargePosting>> GetPostingsByStatusAsync(PostingStatus status, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets room charge postings for a receipt.
    /// </summary>
    Task<IEnumerable<RoomChargePosting>> GetPostingsByReceiptAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed posting.
    /// </summary>
    Task<RoomChargeResult> RetryPostingAsync(int postingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending posting.
    /// </summary>
    Task CancelPostingAsync(int postingId, int userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids/reverses a posted charge.
    /// </summary>
    Task<RoomChargeResult> VoidPostingAsync(int postingId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Guest Lookup

    /// <summary>
    /// Looks up a guest by room number.
    /// </summary>
    Task<GuestLookupResult> LookupGuestByRoomAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a guest by name.
    /// </summary>
    Task<IEnumerable<GuestInfo>> SearchGuestsByNameAsync(string name, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a guest by confirmation number.
    /// </summary>
    Task<GuestLookupResult> LookupGuestByConfirmationAsync(string confirmationNumber, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all in-house guests.
    /// </summary>
    Task<IEnumerable<GuestInfo>> GetInHouseGuestsAsync(int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if room charges are allowed for a guest.
    /// </summary>
    Task<ChargeValidationResult> ValidateRoomChargeAsync(string roomNumber, decimal amount, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes guest cache for a room.
    /// </summary>
    Task RefreshGuestCacheAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Folio Integration

    /// <summary>
    /// Gets folio details for a guest.
    /// </summary>
    Task<FolioDetails?> GetFolioDetailsAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets folio transactions.
    /// </summary>
    Task<IEnumerable<FolioTransaction>> GetFolioTransactionsAsync(string folioNumber, int? configId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets POS charges posted to a folio.
    /// </summary>
    Task<IEnumerable<FolioTransaction>> GetPOSChargesOnFolioAsync(string folioNumber, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    #endregion

    #region Queue Management

    /// <summary>
    /// Adds a posting to the retry queue.
    /// </summary>
    Task AddToQueueAsync(int postingId, int priority = 5, DateTime? scheduledAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queue items ready for processing.
    /// </summary>
    Task<IEnumerable<PMSPostingQueue>> GetPendingQueueItemsAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes the posting queue.
    /// </summary>
    Task<QueueProcessingResult> ProcessQueueAsync(int batchSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queue status summary.
    /// </summary>
    Task<QueueStatusSummary> GetQueueStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears old/completed queue items.
    /// </summary>
    Task ClearOldQueueItemsAsync(int daysOld = 7, CancellationToken cancellationToken = default);

    #endregion

    #region Error Handling

    /// <summary>
    /// Gets error mappings.
    /// </summary>
    Task<IEnumerable<PMSErrorMapping>> GetErrorMappingsAsync(PMSType? pmsType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an error mapping.
    /// </summary>
    Task<PMSErrorMapping> CreateErrorMappingAsync(PMSErrorMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets friendly error message for a PMS error.
    /// </summary>
    Task<string> GetFriendlyErrorMessageAsync(string errorCode, PMSType pmsType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs PMS activity.
    /// </summary>
    Task LogActivityAsync(PMSActivityLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity logs.
    /// </summary>
    Task<IEnumerable<PMSActivityLog>> GetActivityLogsAsync(int? configId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default);

    #endregion

    #region Reports

    /// <summary>
    /// Gets posting summary report.
    /// </summary>
    Task<PostingSummaryReport> GetPostingSummaryAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed postings report.
    /// </summary>
    Task<IEnumerable<FailedPostingDetail>> GetFailedPostingsReportAsync(int? configId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Result of connection test.
/// </summary>
public class ConnectionTestResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int? ResponseTimeMs { get; set; }
    public string? PMSVersion { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request to post a room charge.
/// </summary>
public class RoomChargeRequest
{
    public int? ConfigId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? FolioNumber { get; set; }
    public RoomChargeType ChargeType { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceCharge { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ReceiptId { get; set; }
    public int? OrderId { get; set; }
    public int UserId { get; set; }
    public string? SignatureData { get; set; }
}

/// <summary>
/// Result of room charge posting.
/// </summary>
public class RoomChargeResult
{
    public bool IsSuccess { get; set; }
    public int PostingId { get; set; }
    public string? PMSTransactionId { get; set; }
    public PostingStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRetryable { get; set; }
}

/// <summary>
/// Result of guest lookup.
/// </summary>
public class GuestLookupResult
{
    public bool IsSuccess { get; set; }
    public GuestInfo? Guest { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCached { get; set; }
}

/// <summary>
/// Guest information.
/// </summary>
public class GuestInfo
{
    public string RoomNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? FolioNumber { get; set; }
    public string? ConfirmationNumber { get; set; }
    public string? PMSGuestId { get; set; }
    public GuestStatus Status { get; set; }
    public DateTime? CheckInDate { get; set; }
    public DateTime? CheckOutDate { get; set; }
    public string? VIPStatus { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? CurrentBalance { get; set; }
    public bool AllowRoomCharges { get; set; }
    public string? ChargeBlockReason { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Result of charge validation.
/// </summary>
public class ChargeValidationResult
{
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public decimal? AvailableCredit { get; set; }
    public decimal? CurrentBalance { get; set; }
    public GuestStatus GuestStatus { get; set; }
}

/// <summary>
/// Folio details.
/// </summary>
public class FolioDetails
{
    public string FolioNumber { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime? ArrivalDate { get; set; }
    public DateTime? DepartureDate { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public string? CompanyName { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Folio transaction.
/// </summary>
public class FolioTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? RevenueCenterCode { get; set; }
    public string? Reference { get; set; }
    public bool IsPostedFromPOS { get; set; }
}

/// <summary>
/// Result of queue processing.
/// </summary>
public class QueueProcessingResult
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int RequeueCount { get; set; }
    public TimeSpan Duration { get; set; }
    public IList<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Queue status summary.
/// </summary>
public class QueueStatusSummary
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int FailedCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? OldestPendingAt { get; set; }
    public DateTime? LastProcessedAt { get; set; }
}

/// <summary>
/// Posting summary report.
/// </summary>
public class PostingSummaryReport
{
    public int ConfigId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalPostings { get; set; }
    public int SuccessfulPostings { get; set; }
    public int FailedPostings { get; set; }
    public int PendingPostings { get; set; }
    public decimal TotalAmountPosted { get; set; }
    public decimal TotalAmountFailed { get; set; }
    public decimal TotalAmountPending { get; set; }
    public Dictionary<RoomChargeType, decimal> ByChargeType { get; set; } = new();
    public Dictionary<string, int> ErrorBreakdown { get; set; } = new();
}

/// <summary>
/// Failed posting detail.
/// </summary>
public class FailedPostingDetail
{
    public int PostingId { get; set; }
    public string PostingReference { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRetryable { get; set; }
}

#endregion
