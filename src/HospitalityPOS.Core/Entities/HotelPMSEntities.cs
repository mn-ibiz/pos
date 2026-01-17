namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of PMS system.
/// </summary>
public enum PMSType
{
    /// <summary>Opera PMS by Oracle.</summary>
    Opera = 1,
    /// <summary>Protel PMS.</summary>
    Protel = 2,
    /// <summary>Fidelio PMS.</summary>
    Fidelio = 3,
    /// <summary>Mews PMS.</summary>
    Mews = 4,
    /// <summary>Cloudbeds PMS.</summary>
    Cloudbeds = 5,
    /// <summary>RoomRaccoon PMS.</summary>
    RoomRaccoon = 6,
    /// <summary>eZee Absolute PMS.</summary>
    eZee = 7,
    /// <summary>Hotelogix PMS.</summary>
    Hotelogix = 8,
    /// <summary>Custom/Generic PMS.</summary>
    Custom = 99
}

/// <summary>
/// Status of PMS connection.
/// </summary>
public enum PMSConnectionStatus
{
    /// <summary>Not configured.</summary>
    NotConfigured = 0,
    /// <summary>Connection active and working.</summary>
    Connected = 1,
    /// <summary>Connection configured but currently disconnected.</summary>
    Disconnected = 2,
    /// <summary>Connection failed due to authentication error.</summary>
    AuthenticationFailed = 3,
    /// <summary>Connection timed out.</summary>
    Timeout = 4,
    /// <summary>Connection disabled.</summary>
    Disabled = 5
}

/// <summary>
/// Type of room charge posting.
/// </summary>
public enum RoomChargeType
{
    /// <summary>Food and beverage charge.</summary>
    FoodAndBeverage = 1,
    /// <summary>Minibar charge.</summary>
    Minibar = 2,
    /// <summary>Room service charge.</summary>
    RoomService = 3,
    /// <summary>Spa charge.</summary>
    Spa = 4,
    /// <summary>Laundry charge.</summary>
    Laundry = 5,
    /// <summary>Miscellaneous charge.</summary>
    Miscellaneous = 6,
    /// <summary>Telephone charge.</summary>
    Telephone = 7,
    /// <summary>Business center charge.</summary>
    BusinessCenter = 8,
    /// <summary>Pool/Recreation charge.</summary>
    Recreation = 9,
    /// <summary>Other charge.</summary>
    Other = 99
}

/// <summary>
/// Status of a room charge posting.
/// </summary>
public enum PostingStatus
{
    /// <summary>Pending to be posted.</summary>
    Pending = 1,
    /// <summary>Currently posting.</summary>
    Processing = 2,
    /// <summary>Successfully posted to PMS.</summary>
    Posted = 3,
    /// <summary>Posting failed.</summary>
    Failed = 4,
    /// <summary>Posting cancelled.</summary>
    Cancelled = 5,
    /// <summary>Requires retry.</summary>
    Retry = 6,
    /// <summary>Manual intervention required.</summary>
    ManualRequired = 7
}

/// <summary>
/// Guest status in PMS.
/// </summary>
public enum GuestStatus
{
    /// <summary>Guest is checked in.</summary>
    InHouse = 1,
    /// <summary>Guest has checked out.</summary>
    CheckedOut = 2,
    /// <summary>Reserved but not checked in.</summary>
    Reserved = 3,
    /// <summary>Guest is due to arrive today.</summary>
    DueIn = 4,
    /// <summary>Guest is due to depart today.</summary>
    DueOut = 5,
    /// <summary>No show.</summary>
    NoShow = 6
}

/// <summary>
/// PMS connection configuration.
/// </summary>
public class PMSConfiguration : BaseEntity
{
    /// <summary>
    /// Configuration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of PMS system.
    /// </summary>
    public PMSType PMSType { get; set; }

    /// <summary>
    /// Hotel/Property code in PMS.
    /// </summary>
    public string PropertyCode { get; set; } = string.Empty;

    /// <summary>
    /// PMS API endpoint URL.
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// API username or client ID.
    /// </summary>
    public string? ApiUsername { get; set; }

    /// <summary>
    /// API password or client secret (encrypted).
    /// </summary>
    public string? ApiPassword { get; set; }

    /// <summary>
    /// API key if required.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// OAuth token endpoint if using OAuth.
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Current OAuth access token (encrypted).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// OAuth refresh token (encrypted).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiry date/time.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Whether automatic posting is enabled.
    /// </summary>
    public bool AutoPostEnabled { get; set; } = true;

    /// <summary>
    /// Current connection status.
    /// </summary>
    public PMSConnectionStatus Status { get; set; } = PMSConnectionStatus.NotConfigured;

    /// <summary>
    /// Last successful connection time.
    /// </summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// Last connection error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Whether this is the default configuration.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Store this configuration is associated with.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Additional configuration as JSON.
    /// </summary>
    public string? AdditionalSettings { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual ICollection<PMSRevenueCenter> RevenueCenters { get; set; } = new List<PMSRevenueCenter>();
}

/// <summary>
/// Mapping between POS outlet and PMS revenue center.
/// </summary>
public class PMSRevenueCenter : BaseEntity
{
    /// <summary>
    /// Reference to PMS configuration.
    /// </summary>
    public int PMSConfigurationId { get; set; }

    /// <summary>
    /// POS outlet/store ID.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Revenue center code in PMS.
    /// </summary>
    public string RevenueCenterCode { get; set; } = string.Empty;

    /// <summary>
    /// Revenue center name.
    /// </summary>
    public string RevenueCenterName { get; set; } = string.Empty;

    /// <summary>
    /// Default charge type for this revenue center.
    /// </summary>
    public RoomChargeType DefaultChargeType { get; set; } = RoomChargeType.FoodAndBeverage;

    /// <summary>
    /// Transaction code in PMS for charges.
    /// </summary>
    public string? TransactionCode { get; set; }

    /// <summary>
    /// Whether this mapping is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // Navigation properties
    public virtual PMSConfiguration PMSConfiguration { get; set; } = null!;
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Room charge posting record.
/// </summary>
public class RoomChargePosting : BaseEntity
{
    /// <summary>
    /// Reference to PMS configuration.
    /// </summary>
    public int PMSConfigurationId { get; set; }

    /// <summary>
    /// Posting reference number.
    /// </summary>
    public string PostingReference { get; set; } = string.Empty;

    /// <summary>
    /// Room number.
    /// </summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>
    /// Guest name.
    /// </summary>
    public string GuestName { get; set; } = string.Empty;

    /// <summary>
    /// Guest folio number in PMS.
    /// </summary>
    public string? FolioNumber { get; set; }

    /// <summary>
    /// Reservation confirmation number.
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Type of charge.
    /// </summary>
    public RoomChargeType ChargeType { get; set; }

    /// <summary>
    /// Charge amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Service charge amount.
    /// </summary>
    public decimal ServiceCharge { get; set; }

    /// <summary>
    /// Total amount including tax and service.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Charge description/narration.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to POS receipt.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Reference to POS order.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Revenue center code used.
    /// </summary>
    public string? RevenueCenterCode { get; set; }

    /// <summary>
    /// Transaction code in PMS.
    /// </summary>
    public string? TransactionCode { get; set; }

    /// <summary>
    /// Posting status.
    /// </summary>
    public PostingStatus Status { get; set; } = PostingStatus.Pending;

    /// <summary>
    /// Number of posting attempts.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Date/time of last attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Date/time when successfully posted.
    /// </summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>
    /// PMS transaction/posting ID.
    /// </summary>
    public string? PMSTransactionId { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User who initiated the posting.
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    /// <summary>
    /// Guest signature data (base64).
    /// </summary>
    public string? SignatureData { get; set; }

    /// <summary>
    /// Additional posting data as JSON.
    /// </summary>
    public string? AdditionalData { get; set; }

    // Navigation properties
    public virtual PMSConfiguration PMSConfiguration { get; set; } = null!;
    public virtual Receipt? Receipt { get; set; }
    public virtual Order? Order { get; set; }
    public virtual User? ProcessedByUser { get; set; }
}

/// <summary>
/// Guest lookup/cache record.
/// </summary>
public class PMSGuestLookup : BaseEntity
{
    /// <summary>
    /// Reference to PMS configuration.
    /// </summary>
    public int PMSConfigurationId { get; set; }

    /// <summary>
    /// Room number.
    /// </summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>
    /// Guest first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Guest last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Guest full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Folio number.
    /// </summary>
    public string? FolioNumber { get; set; }

    /// <summary>
    /// Reservation confirmation number.
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Guest ID in PMS.
    /// </summary>
    public string? PMSGuestId { get; set; }

    /// <summary>
    /// Guest status.
    /// </summary>
    public GuestStatus Status { get; set; }

    /// <summary>
    /// Check-in date.
    /// </summary>
    public DateTime? CheckInDate { get; set; }

    /// <summary>
    /// Check-out date.
    /// </summary>
    public DateTime? CheckOutDate { get; set; }

    /// <summary>
    /// VIP status/level.
    /// </summary>
    public string? VIPStatus { get; set; }

    /// <summary>
    /// Guest credit limit.
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Current folio balance.
    /// </summary>
    public decimal? CurrentBalance { get; set; }

    /// <summary>
    /// Whether room charges are allowed.
    /// </summary>
    public bool AllowRoomCharges { get; set; } = true;

    /// <summary>
    /// Reason if charges not allowed.
    /// </summary>
    public string? ChargeBlockReason { get; set; }

    /// <summary>
    /// Company/travel agent name.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Guest preferences/notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this record was cached/refreshed.
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cache expiry time.
    /// </summary>
    public DateTime CacheExpiresAt { get; set; }

    /// <summary>
    /// Raw response from PMS as JSON.
    /// </summary>
    public string? RawResponse { get; set; }

    // Navigation properties
    public virtual PMSConfiguration PMSConfiguration { get; set; } = null!;
}

/// <summary>
/// PMS posting queue for retry and batch processing.
/// </summary>
public class PMSPostingQueue : BaseEntity
{
    /// <summary>
    /// Reference to room charge posting.
    /// </summary>
    public int RoomChargePostingId { get; set; }

    /// <summary>
    /// Queue priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Scheduled processing time.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Whether item is being processed.
    /// </summary>
    public bool IsProcessing { get; set; }

    /// <summary>
    /// When processing started.
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Next retry time.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    // Navigation properties
    public virtual RoomChargePosting RoomChargePosting { get; set; } = null!;
}

/// <summary>
/// PMS integration activity log.
/// </summary>
public class PMSActivityLog : BaseEntity
{
    /// <summary>
    /// Reference to PMS configuration.
    /// </summary>
    public int PMSConfigurationId { get; set; }

    /// <summary>
    /// Activity type.
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Activity description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to posting if applicable.
    /// </summary>
    public int? RoomChargePostingId { get; set; }

    /// <summary>
    /// Request payload sent.
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Response received.
    /// </summary>
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// User who triggered the activity.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public string? IpAddress { get; set; }

    // Navigation properties
    public virtual PMSConfiguration PMSConfiguration { get; set; } = null!;
    public virtual RoomChargePosting? RoomChargePosting { get; set; }
    public virtual User? User { get; set; }
}

/// <summary>
/// PMS error mapping for better error handling.
/// </summary>
public class PMSErrorMapping : BaseEntity
{
    /// <summary>
    /// PMS type this mapping applies to.
    /// </summary>
    public PMSType? PMSType { get; set; }

    /// <summary>
    /// Error code from PMS.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Original error message pattern.
    /// </summary>
    public string? OriginalMessage { get; set; }

    /// <summary>
    /// User-friendly error message.
    /// </summary>
    public string FriendlyMessage { get; set; } = string.Empty;

    /// <summary>
    /// Suggested action.
    /// </summary>
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Whether this error is retryable.
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Severity level (1=Low, 2=Medium, 3=High, 4=Critical).
    /// </summary>
    public int Severity { get; set; } = 2;

    /// <summary>
    /// Whether to notify administrators.
    /// </summary>
    public bool NotifyAdmin { get; set; }
}
