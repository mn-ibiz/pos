using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO for enrolling a new customer in the loyalty program.
/// </summary>
public class EnrollCustomerDto
{
    /// <summary>
    /// Customer's phone number. Required.
    /// Accepts formats: 254712345678, 0712345678, or 712345678.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer's name. Optional.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Customer's email address. Optional.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Customer's date of birth. Optional but needed for birthday rewards.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>
/// Result of a customer enrollment operation.
/// </summary>
public class EnrollmentResult
{
    /// <summary>
    /// Indicates whether the enrollment was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if enrollment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// The enrolled member if successful.
    /// </summary>
    public LoyaltyMemberDto? Member { get; set; }

    /// <summary>
    /// Indicates if a duplicate was found.
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// The existing member if a duplicate was found.
    /// </summary>
    public LoyaltyMemberDto? ExistingMember { get; set; }

    /// <summary>
    /// Welcome bonus points awarded (if any).
    /// </summary>
    public int WelcomeBonusPoints { get; set; }

    /// <summary>
    /// Creates a successful enrollment result.
    /// </summary>
    public static EnrollmentResult Success(LoyaltyMemberDto member, int welcomeBonus = 0) => new()
    {
        IsSuccess = true,
        Member = member,
        WelcomeBonusPoints = welcomeBonus
    };

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    public static EnrollmentResult Failure(string errorMessage, string? errorCode = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };

    /// <summary>
    /// Creates a duplicate found result.
    /// </summary>
    public static EnrollmentResult Duplicate(LoyaltyMemberDto existingMember) => new()
    {
        IsSuccess = false,
        IsDuplicate = true,
        Member = existingMember,           // Set for consistent API access
        ExistingMember = existingMember,   // Also set for explicit duplicate handling
        ErrorMessage = "A customer with this phone number is already enrolled.",
        ErrorCode = "DUPLICATE_PHONE"
    };
}

/// <summary>
/// DTO representing a loyalty member for display and transfer.
/// </summary>
public class LoyaltyMemberDto
{
    /// <summary>
    /// The member's unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The member's phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The member's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The member's email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The member's date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// The membership number.
    /// </summary>
    public string MembershipNumber { get; set; } = string.Empty;

    /// <summary>
    /// The current membership tier.
    /// </summary>
    public MembershipTier Tier { get; set; }

    /// <summary>
    /// Display name for the tier.
    /// </summary>
    public string TierName => Tier.ToString();

    /// <summary>
    /// Current points balance.
    /// </summary>
    public decimal PointsBalance { get; set; }

    /// <summary>
    /// Total lifetime points earned.
    /// </summary>
    public decimal LifetimePoints { get; set; }

    /// <summary>
    /// Total lifetime spend (KES).
    /// </summary>
    public decimal LifetimeSpend { get; set; }

    /// <summary>
    /// Date of enrollment.
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Date of last visit.
    /// </summary>
    public DateTime? LastVisit { get; set; }

    /// <summary>
    /// Total visit count.
    /// </summary>
    public int VisitCount { get; set; }

    /// <summary>
    /// Whether the member is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display name (Name or formatted phone number).
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : FormatPhoneForDisplay(PhoneNumber);

    private static string FormatPhoneForDisplay(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 9)
            return phone;

        // Format: 0712 345 678
        var normalized = phone.StartsWith("254") ? "0" + phone[3..] : phone;
        if (normalized.Length == 10)
            return $"{normalized[..4]} {normalized[4..7]} {normalized[7..]}";
        return phone;
    }
}

/// <summary>
/// Result of sending an SMS.
/// </summary>
public class SmsResult
{
    /// <summary>
    /// Indicates whether the SMS was sent successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if sending failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Provider-specific message ID.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Creates a successful SMS result.
    /// </summary>
    public static SmsResult Success(string? messageId = null) => new()
    {
        IsSuccess = true,
        MessageId = messageId
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static SmsResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of points calculation.
/// </summary>
public class PointsCalculationResult
{
    /// <summary>
    /// The base amount used for points calculation.
    /// </summary>
    public decimal EligibleAmount { get; set; }

    /// <summary>
    /// Base points earned (before bonus).
    /// </summary>
    public decimal BasePoints { get; set; }

    /// <summary>
    /// Bonus points earned (tier bonus, promotion, etc.).
    /// </summary>
    public decimal BonusPoints { get; set; }

    /// <summary>
    /// The bonus multiplier applied.
    /// </summary>
    public decimal BonusMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Total points to be earned.
    /// </summary>
    public decimal TotalPoints => BasePoints + BonusPoints;

    /// <summary>
    /// The earning rate used (KES per point).
    /// </summary>
    public decimal EarningRate { get; set; }

    /// <summary>
    /// Description of how points were calculated.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Result of awarding points to a member.
/// </summary>
public class PointsAwardResult
{
    /// <summary>
    /// Indicates whether the points were awarded successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if awarding failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The loyalty transaction ID created.
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// Points earned in this transaction.
    /// </summary>
    public decimal PointsEarned { get; set; }

    /// <summary>
    /// Bonus points earned.
    /// </summary>
    public decimal BonusPoints { get; set; }

    /// <summary>
    /// Member's new points balance.
    /// </summary>
    public decimal NewBalance { get; set; }

    /// <summary>
    /// Member's previous points balance.
    /// </summary>
    public decimal PreviousBalance { get; set; }

    /// <summary>
    /// Member's new lifetime points total.
    /// </summary>
    public decimal NewLifetimePoints { get; set; }

    /// <summary>
    /// Creates a successful points award result.
    /// </summary>
    public static PointsAwardResult Success(
        int transactionId,
        decimal pointsEarned,
        decimal bonusPoints,
        decimal previousBalance,
        decimal newBalance,
        decimal newLifetimePoints) => new()
    {
        IsSuccess = true,
        TransactionId = transactionId,
        PointsEarned = pointsEarned,
        BonusPoints = bonusPoints,
        PreviousBalance = previousBalance,
        NewBalance = newBalance,
        NewLifetimePoints = newLifetimePoints
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static PointsAwardResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// DTO representing a loyalty transaction for display.
/// </summary>
public class LoyaltyTransactionDto
{
    /// <summary>
    /// The transaction ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The transaction type.
    /// </summary>
    public LoyaltyTransactionType TransactionType { get; set; }

    /// <summary>
    /// Display name for the transaction type.
    /// </summary>
    public string TransactionTypeName => TransactionType.ToString();

    /// <summary>
    /// Points amount (positive for earned, negative for redeemed).
    /// </summary>
    public decimal Points { get; set; }

    /// <summary>
    /// Monetary value associated with this transaction.
    /// </summary>
    public decimal MonetaryValue { get; set; }

    /// <summary>
    /// Points balance after this transaction.
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Bonus points in this transaction.
    /// </summary>
    public decimal BonusPoints { get; set; }

    /// <summary>
    /// Bonus multiplier applied.
    /// </summary>
    public decimal BonusMultiplier { get; set; }

    /// <summary>
    /// Transaction date and time.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Transaction description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference number (e.g., receipt number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Whether this was a credit (points added) or debit (points removed).
    /// </summary>
    public bool IsCredit => Points > 0;

    /// <summary>
    /// Name of user who processed the transaction.
    /// </summary>
    public string? ProcessedByUserName { get; set; }

    /// <summary>
    /// Receipt ID if linked to a transaction.
    /// </summary>
    public int? ReceiptId { get; set; }
}

/// <summary>
/// Paginated result of loyalty transaction history.
/// </summary>
public class PagedTransactionHistoryResult
{
    /// <summary>
    /// The list of transactions for the current page.
    /// </summary>
    public List<LoyaltyTransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of transactions matching the filter.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there's a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Whether there's a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;
}

/// <summary>
/// Result of a redemption preview/calculation.
/// </summary>
public class RedemptionPreviewResult
{
    /// <summary>
    /// Indicates whether redemption is allowed.
    /// </summary>
    public bool CanRedeem { get; set; }

    /// <summary>
    /// Error message if redemption is not allowed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The member's current points balance.
    /// </summary>
    public decimal AvailablePoints { get; set; }

    /// <summary>
    /// The KES value of available points.
    /// </summary>
    public decimal AvailableValue { get; set; }

    /// <summary>
    /// Minimum points required for redemption.
    /// </summary>
    public int MinimumRedemptionPoints { get; set; }

    /// <summary>
    /// Maximum points that can be redeemed for this transaction.
    /// </summary>
    public decimal MaxRedeemablePoints { get; set; }

    /// <summary>
    /// Maximum KES value that can be redeemed for this transaction.
    /// </summary>
    public decimal MaxRedeemableValue { get; set; }

    /// <summary>
    /// The redemption rate (KES per point).
    /// </summary>
    public decimal RedemptionRate { get; set; }

    /// <summary>
    /// Suggested redemption points (within limits).
    /// </summary>
    public decimal SuggestedPoints { get; set; }

    /// <summary>
    /// Suggested redemption value (within limits).
    /// </summary>
    public decimal SuggestedValue { get; set; }

    /// <summary>
    /// Creates a successful preview result.
    /// </summary>
    public static RedemptionPreviewResult Success(
        decimal availablePoints,
        decimal availableValue,
        int minimumPoints,
        decimal maxRedeemablePoints,
        decimal maxRedeemableValue,
        decimal redemptionRate,
        decimal suggestedPoints,
        decimal suggestedValue) => new()
    {
        CanRedeem = true,
        AvailablePoints = availablePoints,
        AvailableValue = availableValue,
        MinimumRedemptionPoints = minimumPoints,
        MaxRedeemablePoints = maxRedeemablePoints,
        MaxRedeemableValue = maxRedeemableValue,
        RedemptionRate = redemptionRate,
        SuggestedPoints = suggestedPoints,
        SuggestedValue = suggestedValue
    };

    /// <summary>
    /// Creates a failure result indicating redemption is not allowed.
    /// </summary>
    public static RedemptionPreviewResult Failure(string errorMessage, decimal availablePoints = 0, decimal availableValue = 0) => new()
    {
        CanRedeem = false,
        ErrorMessage = errorMessage,
        AvailablePoints = availablePoints,
        AvailableValue = availableValue
    };
}

/// <summary>
/// Result of a points redemption operation.
/// </summary>
public class RedemptionResult
{
    /// <summary>
    /// Indicates whether the redemption was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if redemption failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The loyalty transaction ID created.
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// Points redeemed in this transaction.
    /// </summary>
    public decimal PointsRedeemed { get; set; }

    /// <summary>
    /// KES value of points redeemed.
    /// </summary>
    public decimal ValueRedeemed { get; set; }

    /// <summary>
    /// Member's new points balance.
    /// </summary>
    public decimal NewBalance { get; set; }

    /// <summary>
    /// Member's previous points balance.
    /// </summary>
    public decimal PreviousBalance { get; set; }

    /// <summary>
    /// Creates a successful redemption result.
    /// </summary>
    public static RedemptionResult Success(
        int transactionId,
        decimal pointsRedeemed,
        decimal valueRedeemed,
        decimal previousBalance,
        decimal newBalance) => new()
    {
        IsSuccess = true,
        TransactionId = transactionId,
        PointsRedeemed = pointsRedeemed,
        ValueRedeemed = valueRedeemed,
        PreviousBalance = previousBalance,
        NewBalance = newBalance
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static RedemptionResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// DTO representing a tier configuration for display.
/// </summary>
public class TierConfigurationDto
{
    /// <summary>
    /// The tier level.
    /// </summary>
    public MembershipTier Tier { get; set; }

    /// <summary>
    /// Display name of the tier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the tier and benefits.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Minimum lifetime spend to reach this tier (KES).
    /// </summary>
    public decimal SpendThreshold { get; set; }

    /// <summary>
    /// Minimum lifetime points to reach this tier.
    /// </summary>
    public decimal PointsThreshold { get; set; }

    /// <summary>
    /// Points earning multiplier (e.g., 1.5 = 50% bonus).
    /// </summary>
    public decimal PointsMultiplier { get; set; }

    /// <summary>
    /// Base discount percentage for this tier.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Whether members get free delivery.
    /// </summary>
    public bool FreeDelivery { get; set; }

    /// <summary>
    /// Whether members get priority service.
    /// </summary>
    public bool PriorityService { get; set; }

    /// <summary>
    /// Color code for UI display.
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon name for UI display.
    /// </summary>
    public string? IconName { get; set; }
}

/// <summary>
/// Result of a tier evaluation for a member.
/// </summary>
public class TierEvaluationResult
{
    /// <summary>
    /// The member's current tier before evaluation.
    /// </summary>
    public MembershipTier PreviousTier { get; set; }

    /// <summary>
    /// The member's new tier after evaluation.
    /// </summary>
    public MembershipTier NewTier { get; set; }

    /// <summary>
    /// Whether the tier changed.
    /// </summary>
    public bool TierChanged => PreviousTier != NewTier;

    /// <summary>
    /// Whether this was an upgrade.
    /// </summary>
    public bool IsUpgrade => (int)NewTier > (int)PreviousTier;

    /// <summary>
    /// Whether this was a downgrade.
    /// </summary>
    public bool IsDowngrade => (int)NewTier < (int)PreviousTier;

    /// <summary>
    /// The new tier configuration details.
    /// </summary>
    public TierConfigurationDto? NewTierConfig { get; set; }

    /// <summary>
    /// Spend amount used for evaluation.
    /// </summary>
    public decimal EvaluatedSpend { get; set; }

    /// <summary>
    /// Points balance used for evaluation.
    /// </summary>
    public decimal EvaluatedPoints { get; set; }

    /// <summary>
    /// Progress towards next tier (0-100%).
    /// </summary>
    public decimal NextTierProgress { get; set; }

    /// <summary>
    /// Amount needed to reach next tier.
    /// </summary>
    public decimal AmountToNextTier { get; set; }

    /// <summary>
    /// The next tier available (null if at highest).
    /// </summary>
    public MembershipTier? NextTier { get; set; }

    /// <summary>
    /// Message describing the tier change.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a result where tier stayed the same.
    /// </summary>
    public static TierEvaluationResult NoChange(
        MembershipTier currentTier,
        decimal spend,
        decimal points,
        decimal progress,
        decimal amountToNext,
        MembershipTier? nextTier) => new()
    {
        PreviousTier = currentTier,
        NewTier = currentTier,
        EvaluatedSpend = spend,
        EvaluatedPoints = points,
        NextTierProgress = progress,
        AmountToNextTier = amountToNext,
        NextTier = nextTier,
        Message = $"Maintaining {currentTier} status"
    };

    /// <summary>
    /// Creates an upgrade result.
    /// </summary>
    public static TierEvaluationResult Upgrade(
        MembershipTier previousTier,
        MembershipTier newTier,
        TierConfigurationDto newConfig,
        decimal spend,
        decimal points,
        decimal progress,
        decimal amountToNext,
        MembershipTier? nextTier) => new()
    {
        PreviousTier = previousTier,
        NewTier = newTier,
        NewTierConfig = newConfig,
        EvaluatedSpend = spend,
        EvaluatedPoints = points,
        NextTierProgress = progress,
        AmountToNextTier = amountToNext,
        NextTier = nextTier,
        Message = $"Congratulations! Upgraded from {previousTier} to {newTier}"
    };

    /// <summary>
    /// Creates a downgrade result.
    /// </summary>
    public static TierEvaluationResult Downgrade(
        MembershipTier previousTier,
        MembershipTier newTier,
        TierConfigurationDto newConfig,
        decimal spend,
        decimal points) => new()
    {
        PreviousTier = previousTier,
        NewTier = newTier,
        NewTierConfig = newConfig,
        EvaluatedSpend = spend,
        EvaluatedPoints = points,
        Message = $"Tier changed from {previousTier} to {newTier}"
    };
}

/// <summary>
/// DTO representing spending by category for analytics.
/// </summary>
public class CategorySpendDto
{
    /// <summary>
    /// The category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// The category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Total amount spent in this category (KES).
    /// </summary>
    public decimal TotalSpend { get; set; }

    /// <summary>
    /// Number of items purchased in this category.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Percentage of total spending in this category.
    /// </summary>
    public decimal SpendPercentage { get; set; }
}

/// <summary>
/// DTO representing customer analytics and purchase behavior.
/// </summary>
public class CustomerAnalyticsDto
{
    /// <summary>
    /// The loyalty member ID.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The member details.
    /// </summary>
    public LoyaltyMemberDto? Member { get; set; }

    /// <summary>
    /// Total lifetime spend (KES).
    /// </summary>
    public decimal TotalSpend { get; set; }

    /// <summary>
    /// Total number of visits/transactions.
    /// </summary>
    public int VisitCount { get; set; }

    /// <summary>
    /// Average basket/transaction value (KES).
    /// </summary>
    public decimal AverageBasket { get; set; }

    /// <summary>
    /// Average items per transaction.
    /// </summary>
    public decimal AverageItemsPerTransaction { get; set; }

    /// <summary>
    /// Top spending categories.
    /// </summary>
    public List<CategorySpendDto> TopCategories { get; set; } = new();

    /// <summary>
    /// Date of first visit.
    /// </summary>
    public DateTime FirstVisit { get; set; }

    /// <summary>
    /// Date of last visit.
    /// </summary>
    public DateTime? LastVisit { get; set; }

    /// <summary>
    /// Days since last visit.
    /// </summary>
    public int DaysSinceLastVisit => LastVisit.HasValue
        ? (int)(DateTime.UtcNow - LastVisit.Value).TotalDays
        : 0;

    /// <summary>
    /// Average days between visits.
    /// </summary>
    public decimal AverageDaysBetweenVisits { get; set; }

    /// <summary>
    /// Current points balance.
    /// </summary>
    public decimal PointsBalance { get; set; }

    /// <summary>
    /// Total lifetime points earned.
    /// </summary>
    public decimal LifetimePoints { get; set; }

    /// <summary>
    /// Current membership tier.
    /// </summary>
    public MembershipTier Tier { get; set; }

    /// <summary>
    /// Tier configuration details.
    /// </summary>
    public TierConfigurationDto? TierConfig { get; set; }

    /// <summary>
    /// Customer engagement score (0-100).
    /// Based on recency, frequency, and monetary value.
    /// </summary>
    public int EngagementScore { get; set; }

    /// <summary>
    /// Customer engagement level.
    /// </summary>
    public string EngagementLevel => EngagementScore switch
    {
        >= 80 => "Champion",
        >= 60 => "Loyal",
        >= 40 => "Regular",
        >= 20 => "At Risk",
        _ => "Dormant"
    };
}

/// <summary>
/// Filter options for customer purchase history export.
/// </summary>
public class CustomerExportFilterDto
{
    /// <summary>
    /// Start date for transaction filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for transaction filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by tier.
    /// </summary>
    public MembershipTier? Tier { get; set; }

    /// <summary>
    /// Minimum spend filter.
    /// </summary>
    public decimal? MinSpend { get; set; }

    /// <summary>
    /// Minimum points balance filter.
    /// </summary>
    public decimal? MinPoints { get; set; }

    /// <summary>
    /// Whether to include inactive members.
    /// </summary>
    public bool IncludeInactive { get; set; }

    /// <summary>
    /// Export format (CSV, Excel, etc.).
    /// </summary>
    public string ExportFormat { get; set; } = "CSV";
}

/// <summary>
/// Result of a customer data export operation.
/// </summary>
public class CustomerExportResult
{
    /// <summary>
    /// Whether the export was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Path to the exported file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Number of records exported.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// The file content as bytes (for direct download).
    /// </summary>
    public byte[]? FileContent { get; set; }

    /// <summary>
    /// MIME type of the exported file.
    /// </summary>
    public string ContentType { get; set; } = "text/csv";

    /// <summary>
    /// Suggested filename for download.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful export result.
    /// </summary>
    public static CustomerExportResult Success(byte[] content, string fileName, int recordCount, string contentType = "text/csv") => new()
    {
        IsSuccess = true,
        FileContent = content,
        FileName = fileName,
        RecordCount = recordCount,
        ContentType = contentType
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static CustomerExportResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

// ================== Birthday Reward DTOs ==================

/// <summary>
/// DTO for creating or updating a one-time reward template.
/// </summary>
public class OneTimeRewardDto
{
    /// <summary>
    /// The reward ID (0 for new rewards).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The reward name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The reward description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of one-time reward.
    /// </summary>
    public OneTimeRewardType RewardType { get; set; }

    /// <summary>
    /// How the reward value is applied.
    /// </summary>
    public RewardValueType ValueType { get; set; }

    /// <summary>
    /// The reward value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Minimum tier required (null for all tiers).
    /// </summary>
    public MembershipTier? MinimumTier { get; set; }

    /// <summary>
    /// Days the reward is valid after issuance.
    /// </summary>
    public int ValidityDays { get; set; } = 30;

    /// <summary>
    /// Minimum purchase amount required to redeem (KES).
    /// </summary>
    public decimal? MinimumPurchaseAmount { get; set; }

    /// <summary>
    /// Maximum discount amount for percentage discounts (KES).
    /// </summary>
    public decimal? MaximumDiscountAmount { get; set; }

    /// <summary>
    /// Product ID for free item rewards.
    /// </summary>
    public int? FreeItemProductId { get; set; }

    /// <summary>
    /// SMS template for notification.
    /// </summary>
    public string? SmsTemplate { get; set; }

    /// <summary>
    /// Email template for notification.
    /// </summary>
    public string? EmailTemplate { get; set; }

    /// <summary>
    /// Whether to send SMS notification.
    /// </summary>
    public bool SendSmsNotification { get; set; } = true;

    /// <summary>
    /// Whether to send email notification.
    /// </summary>
    public bool SendEmailNotification { get; set; } = true;

    /// <summary>
    /// Days before the event to issue the reward.
    /// </summary>
    public int DaysBeforeToIssue { get; set; } = 0;

    /// <summary>
    /// Days after the event the reward remains valid.
    /// </summary>
    public int DaysAfterEventValid { get; set; } = 7;

    /// <summary>
    /// Whether the reward is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO representing a member's reward for display.
/// </summary>
public class MemberRewardDto
{
    /// <summary>
    /// The reward instance ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The loyalty member ID.
    /// </summary>
    public int LoyaltyMemberId { get; set; }

    /// <summary>
    /// The member's name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// The member's phone number.
    /// </summary>
    public string MemberPhone { get; set; } = string.Empty;

    /// <summary>
    /// The reward template ID.
    /// </summary>
    public int OneTimeRewardId { get; set; }

    /// <summary>
    /// The reward name.
    /// </summary>
    public string RewardName { get; set; } = string.Empty;

    /// <summary>
    /// The reward type.
    /// </summary>
    public OneTimeRewardType RewardType { get; set; }

    /// <summary>
    /// The reward type display name.
    /// </summary>
    public string RewardTypeName => RewardType.ToString();

    /// <summary>
    /// How the value is applied.
    /// </summary>
    public RewardValueType ValueType { get; set; }

    /// <summary>
    /// The reward value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// The unique redemption code.
    /// </summary>
    public string RedemptionCode { get; set; } = string.Empty;

    /// <summary>
    /// The current status.
    /// </summary>
    public MemberRewardStatus Status { get; set; }

    /// <summary>
    /// The status display name.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// When the reward was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// When the reward expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the reward was redeemed (if applicable).
    /// </summary>
    public DateTime? RedeemedAt { get; set; }

    /// <summary>
    /// The year this reward is for.
    /// </summary>
    public int RewardYear { get; set; }

    /// <summary>
    /// The event date (e.g., birthday).
    /// </summary>
    public DateOnly? EventDate { get; set; }

    /// <summary>
    /// Days until expiry.
    /// </summary>
    public int DaysUntilExpiry => Status == MemberRewardStatus.Active
        ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalDays)
        : 0;

    /// <summary>
    /// Whether the reward is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Display value with appropriate formatting.
    /// </summary>
    public string DisplayValue => ValueType switch
    {
        RewardValueType.FixedPoints => $"{Value:N0} points",
        RewardValueType.PercentageDiscount => $"{Value}% off",
        RewardValueType.FixedDiscount => $"KES {Value:N0} off",
        RewardValueType.FreeItem => "Free item",
        RewardValueType.PointsMultiplier => $"{Value}x points",
        _ => Value.ToString("N0")
    };
}

/// <summary>
/// Result of issuing a birthday reward.
/// </summary>
public class BirthdayRewardResult
{
    /// <summary>
    /// Whether the reward was issued successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The issued reward.
    /// </summary>
    public MemberRewardDto? Reward { get; set; }

    /// <summary>
    /// Whether SMS notification was sent.
    /// </summary>
    public bool SmsSent { get; set; }

    /// <summary>
    /// Whether email notification was sent.
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static BirthdayRewardResult Success(MemberRewardDto reward, bool smsSent, bool emailSent) => new()
    {
        IsSuccess = true,
        Reward = reward,
        SmsSent = smsSent,
        EmailSent = emailSent
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static BirthdayRewardResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a skipped result (already issued this year).
    /// </summary>
    public static BirthdayRewardResult AlreadyIssued(int year) => new()
    {
        IsSuccess = false,
        ErrorMessage = $"Birthday reward already issued for {year}"
    };
}

/// <summary>
/// Result of redeeming a member reward.
/// </summary>
public class RewardRedemptionResult
{
    /// <summary>
    /// Whether the redemption was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The discount amount applied (KES).
    /// </summary>
    public decimal DiscountApplied { get; set; }

    /// <summary>
    /// Points awarded (for point rewards).
    /// </summary>
    public decimal PointsAwarded { get; set; }

    /// <summary>
    /// The redeemed reward.
    /// </summary>
    public MemberRewardDto? Reward { get; set; }

    /// <summary>
    /// Creates a successful redemption result.
    /// </summary>
    public static RewardRedemptionResult Success(MemberRewardDto reward, decimal discountApplied, decimal pointsAwarded = 0) => new()
    {
        IsSuccess = true,
        Reward = reward,
        DiscountApplied = discountApplied,
        PointsAwarded = pointsAwarded
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static RewardRedemptionResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Summary of birthday rewards processing job.
/// </summary>
public class BirthdayRewardJobSummary
{
    /// <summary>
    /// Total members with birthdays in the processing window.
    /// </summary>
    public int TotalMembersWithBirthdays { get; set; }

    /// <summary>
    /// Number of rewards successfully issued.
    /// </summary>
    public int RewardsIssued { get; set; }

    /// <summary>
    /// Number of rewards skipped (already issued).
    /// </summary>
    public int RewardsSkipped { get; set; }

    /// <summary>
    /// Number of rewards that failed to issue.
    /// </summary>
    public int RewardsFailed { get; set; }

    /// <summary>
    /// Number of SMS notifications sent.
    /// </summary>
    public int SmsSent { get; set; }

    /// <summary>
    /// Number of SMS notifications failed.
    /// </summary>
    public int SmsFailed { get; set; }

    /// <summary>
    /// Number of email notifications sent.
    /// </summary>
    public int EmailsSent { get; set; }

    /// <summary>
    /// Number of email notifications failed.
    /// </summary>
    public int EmailsFailed { get; set; }

    /// <summary>
    /// Processing date.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

// ================== Points Multiplier DTOs ==================

/// <summary>
/// Represents an item in a transaction for points calculation.
/// </summary>
public class TransactionItemDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The product name (for display).
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// The category ID (if available).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// The quantity purchased.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// The line total amount (after any line discounts).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The discount applied to this item.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// The tax amount for this item.
    /// </summary>
    public decimal TaxAmount { get; set; }
}

/// <summary>
/// Result of item-level points calculation.
/// </summary>
public class ItemPointsResult
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The product name.
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// The amount used for points calculation.
    /// </summary>
    public decimal EligibleAmount { get; set; }

    /// <summary>
    /// Base points earned from this item.
    /// </summary>
    public decimal BasePoints { get; set; }

    /// <summary>
    /// The multiplier applied (product, category, or promotional).
    /// </summary>
    public decimal MultiplierApplied { get; set; } = 1.0m;

    /// <summary>
    /// Bonus points from multiplier.
    /// </summary>
    public decimal BonusPoints { get; set; }

    /// <summary>
    /// Total points for this item.
    /// </summary>
    public decimal TotalPoints => BasePoints + BonusPoints;

    /// <summary>
    /// The source of the multiplier (Product, Category, Promotion, etc.).
    /// </summary>
    public string? MultiplierSource { get; set; }

    /// <summary>
    /// The promotion or rule name if applicable.
    /// </summary>
    public string? PromotionName { get; set; }

    /// <summary>
    /// Whether this item was excluded from points earning.
    /// </summary>
    public bool IsExcluded { get; set; }

    /// <summary>
    /// Reason for exclusion (if excluded).
    /// </summary>
    public string? ExclusionReason { get; set; }
}

/// <summary>
/// Extended points calculation result with item-level breakdown.
/// </summary>
public class DetailedPointsCalculationResult
{
    /// <summary>
    /// Total eligible amount across all items.
    /// </summary>
    public decimal TotalEligibleAmount { get; set; }

    /// <summary>
    /// Total base points earned.
    /// </summary>
    public decimal TotalBasePoints { get; set; }

    /// <summary>
    /// Total bonus points from multipliers and promotions.
    /// </summary>
    public decimal TotalBonusPoints { get; set; }

    /// <summary>
    /// Tier bonus points (separate from item multipliers).
    /// </summary>
    public decimal TierBonusPoints { get; set; }

    /// <summary>
    /// The member's tier multiplier.
    /// </summary>
    public decimal TierMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Grand total points to be earned.
    /// </summary>
    public decimal GrandTotalPoints => TotalBasePoints + TotalBonusPoints + TierBonusPoints;

    /// <summary>
    /// Item-level breakdown of points.
    /// </summary>
    public List<ItemPointsResult> ItemBreakdown { get; set; } = new();

    /// <summary>
    /// Active promotional rules applied.
    /// </summary>
    public List<AppliedMultiplierRuleDto> AppliedRules { get; set; } = new();

    /// <summary>
    /// The base earning rate used.
    /// </summary>
    public decimal EarningRate { get; set; }

    /// <summary>
    /// Summary description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of items that earned bonus points.
    /// </summary>
    public int ItemsWithBonusCount => ItemBreakdown.Count(i => i.BonusPoints > 0);

    /// <summary>
    /// Number of items excluded from points.
    /// </summary>
    public int ExcludedItemsCount => ItemBreakdown.Count(i => i.IsExcluded);
}

/// <summary>
/// DTO for a points multiplier rule that was applied.
/// </summary>
public class AppliedMultiplierRuleDto
{
    /// <summary>
    /// The rule ID.
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// The rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// The rule type.
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// The multiplier applied.
    /// </summary>
    public decimal Multiplier { get; set; }

    /// <summary>
    /// Bonus points earned from this rule.
    /// </summary>
    public decimal BonusPointsEarned { get; set; }

    /// <summary>
    /// Products/items this rule applied to.
    /// </summary>
    public List<int> AppliedToProductIds { get; set; } = new();
}

/// <summary>
/// DTO for creating or updating a points multiplier rule.
/// </summary>
public class PointsMultiplierRuleDto
{
    /// <summary>
    /// The rule ID (0 for new rules).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The rule type.
    /// </summary>
    public PointsMultiplierRuleTypeDto RuleType { get; set; }

    /// <summary>
    /// The multiplier to apply.
    /// </summary>
    public decimal Multiplier { get; set; } = 1.0m;

    /// <summary>
    /// Whether this rule stacks with others.
    /// </summary>
    public bool IsStackable { get; set; }

    /// <summary>
    /// Evaluation priority (higher = first).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Target product ID (for Product rules).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Target product name (for display).
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Target category ID (for Category rules).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Target category name (for display).
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Minimum tier required.
    /// </summary>
    public MembershipTier? MinimumTier { get; set; }

    /// <summary>
    /// Rule start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Rule end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Days of week when active (comma-separated).
    /// </summary>
    public string? DaysOfWeek { get; set; }

    /// <summary>
    /// Start time of day.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// End time of day.
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Minimum purchase amount required.
    /// </summary>
    public decimal? MinimumPurchaseAmount { get; set; }

    /// <summary>
    /// Minimum quantity required.
    /// </summary>
    public int? MinimumQuantity { get; set; }

    /// <summary>
    /// Maximum bonus points per transaction.
    /// </summary>
    public decimal? MaxBonusPointsPerTransaction { get; set; }

    /// <summary>
    /// Maximum total usages allowed.
    /// </summary>
    public int? MaxTotalUsages { get; set; }

    /// <summary>
    /// Current usage count.
    /// </summary>
    public int CurrentUsageCount { get; set; }

    /// <summary>
    /// Maximum usages per member.
    /// </summary>
    public int? MaxUsagesPerMember { get; set; }

    /// <summary>
    /// Target store ID.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether the rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the rule is currently active based on dates and status.
    /// </summary>
    public bool IsCurrentlyActive { get; set; }

    /// <summary>
    /// Remaining usages (if limited).
    /// </summary>
    public int? RemainingUsages => MaxTotalUsages.HasValue ? MaxTotalUsages.Value - CurrentUsageCount : null;
}

/// <summary>
/// Rule type enum for DTOs (mirrors entity enum).
/// </summary>
public enum PointsMultiplierRuleTypeDto
{
    Product = 1,
    Category = 2,
    Global = 3,
    TierBased = 4,
    DayOfWeek = 5,
    TimeOfDay = 6
}

/// <summary>
/// Summary of product/category points configuration.
/// </summary>
public class ProductPointsConfigDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The product SKU.
    /// </summary>
    public string? SKU { get; set; }

    /// <summary>
    /// The category ID.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// The category name.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Product-level multiplier (null = use category/global).
    /// </summary>
    public decimal? ProductMultiplier { get; set; }

    /// <summary>
    /// Category-level multiplier (null = use global).
    /// </summary>
    public decimal? CategoryMultiplier { get; set; }

    /// <summary>
    /// The effective multiplier that will be used.
    /// </summary>
    public decimal EffectiveMultiplier => ProductMultiplier ?? CategoryMultiplier ?? 1.0m;

    /// <summary>
    /// Source of the effective multiplier.
    /// </summary>
    public string MultiplierSource => ProductMultiplier.HasValue ? "Product"
        : CategoryMultiplier.HasValue ? "Category"
        : "Global";

    /// <summary>
    /// Whether this product is excluded from points.
    /// </summary>
    public bool IsExcluded { get; set; }

    /// <summary>
    /// Active promotional rules for this product.
    /// </summary>
    public List<PointsMultiplierRuleDto> ActivePromotions { get; set; } = new();
}

/// <summary>
/// Request to update product points configuration.
/// </summary>
public class UpdateProductPointsDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The points multiplier (null to use category/global default).
    /// </summary>
    public decimal? PointsMultiplier { get; set; }

    /// <summary>
    /// Whether to exclude from loyalty points.
    /// </summary>
    public bool ExcludeFromLoyaltyPoints { get; set; }
}

/// <summary>
/// Request to update category points configuration.
/// </summary>
public class UpdateCategoryPointsDto
{
    /// <summary>
    /// The category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// The points multiplier (null to use global default).
    /// </summary>
    public decimal? PointsMultiplier { get; set; }

    /// <summary>
    /// Whether to exclude from loyalty points by default.
    /// </summary>
    public bool ExcludeFromLoyaltyPoints { get; set; }
}

/// <summary>
/// Result of creating/updating a multiplier rule.
/// </summary>
public class MultiplierRuleResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The rule that was created/updated.
    /// </summary>
    public PointsMultiplierRuleDto? Rule { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static MultiplierRuleResult Success(PointsMultiplierRuleDto rule) => new()
    {
        IsSuccess = true,
        Rule = rule
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static MultiplierRuleResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
