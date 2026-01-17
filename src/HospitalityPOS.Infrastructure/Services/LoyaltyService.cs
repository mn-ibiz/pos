using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of the loyalty program service.
/// </summary>
public partial class LoyaltyService : ILoyaltyService
{
    private readonly ILoyaltyMemberRepository _memberRepository;
    private readonly IRepository<LoyaltyTransaction> _transactionRepository;
    private readonly IRepository<PointsConfiguration> _pointsConfigRepository;
    private readonly IRepository<TierConfiguration> _tierConfigRepository;
    private readonly ISmsService _smsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoyaltyService> _logger;

    // Tier bonus multipliers (fallback if no tier config)
    private static readonly Dictionary<MembershipTier, decimal> TierBonusMultipliers = new()
    {
        { MembershipTier.Bronze, 1.0m },   // No bonus
        { MembershipTier.Silver, 1.25m },  // 25% bonus
        { MembershipTier.Gold, 1.5m },     // 50% bonus
        { MembershipTier.Platinum, 2.0m }  // 100% bonus (double points)
    };

    public LoyaltyService(
        ILoyaltyMemberRepository memberRepository,
        IRepository<LoyaltyTransaction> transactionRepository,
        IRepository<PointsConfiguration> pointsConfigRepository,
        IRepository<TierConfiguration> tierConfigRepository,
        ISmsService smsService,
        IUnitOfWork unitOfWork,
        ILogger<LoyaltyService> logger)
    {
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _pointsConfigRepository = pointsConfigRepository ?? throw new ArgumentNullException(nameof(pointsConfigRepository));
        _tierConfigRepository = tierConfigRepository ?? throw new ArgumentNullException(nameof(tierConfigRepository));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EnrollmentResult> EnrollCustomerAsync(EnrollCustomerDto dto, int enrolledByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Validate phone number
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return EnrollmentResult.Failure("Phone number is required.", "PHONE_REQUIRED");
        }

        var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);
        if (normalizedPhone == null || !ValidatePhoneNumber(normalizedPhone))
        {
            return EnrollmentResult.Failure(
                "Invalid phone number format. Please enter a valid Kenya phone number (e.g., 0712345678).",
                "INVALID_PHONE");
        }

        // Check for duplicate
        var existingMember = await _memberRepository.GetByPhoneAsync(normalizedPhone, cancellationToken)
            .ConfigureAwait(false);

        if (existingMember != null)
        {
            _logger.LogWarning("Duplicate enrollment attempt for phone: {Phone}", normalizedPhone);
            return EnrollmentResult.Duplicate(MapToDto(existingMember));
        }

        try
        {
            // Generate membership number
            var membershipNumber = await GenerateMembershipNumberAsync(cancellationToken)
                .ConfigureAwait(false);

            // Create new member
            var member = new LoyaltyMember
            {
                PhoneNumber = normalizedPhone,
                Name = dto.Name?.Trim(),
                Email = dto.Email?.Trim(),
                MembershipNumber = membershipNumber,
                Tier = MembershipTier.Bronze,
                PointsBalance = 0,
                LifetimePoints = 0,
                LifetimeSpend = 0,
                EnrolledAt = DateTime.UtcNow,
                VisitCount = 0,
                IsActive = true,
                CreatedByUserId = enrolledByUserId
            };

            await _memberRepository.AddAsync(member, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "New loyalty member enrolled: {MembershipNumber}, Phone: {Phone}, By User: {UserId}",
                membershipNumber, normalizedPhone, enrolledByUserId);

            // Send welcome SMS (fire-and-forget, don't fail enrollment if SMS fails)
            // Using ThreadPool.QueueUserWorkItem for true fire-and-forget with proper exception handling
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    await _smsService.SendWelcomeSmsAsync(
                        normalizedPhone,
                        dto.Name,
                        membershipNumber,
                        0,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Log but don't propagate - this is intentionally fire-and-forget
                    _logger.LogWarning(ex, "Failed to send welcome SMS to {Phone}", normalizedPhone);
                }
            });

            return EnrollmentResult.Success(MapToDto(member));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enroll customer with phone: {Phone}", normalizedPhone);
            return EnrollmentResult.Failure("An error occurred while enrolling the customer. Please try again.", "ENROLLMENT_ERROR");
        }
    }

    /// <inheritdoc />
    public async Task<LoyaltyMemberDto?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (normalizedPhone == null) return null;

        var member = await _memberRepository.GetByPhoneAsync(normalizedPhone, cancellationToken)
            .ConfigureAwait(false);

        return member != null ? MapToDto(member) : null;
    }

    /// <inheritdoc />
    public async Task<LoyaltyMemberDto?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(membershipNumber)) return null;

        var member = await _memberRepository.GetByMembershipNumberAsync(membershipNumber, cancellationToken)
            .ConfigureAwait(false);

        return member != null ? MapToDto(member) : null;
    }

    /// <inheritdoc />
    public async Task<LoyaltyMemberDto?> GetByIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            .ConfigureAwait(false);

        return member != null ? MapToDto(member) : null;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (normalizedPhone == null) return false;

        return await _memberRepository.ExistsByPhoneAsync(normalizedPhone, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoyaltyMemberDto>> SearchMembersAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<LoyaltyMemberDto>();

        var members = await _memberRepository.SearchAsync(searchTerm, maxResults, cancellationToken)
            .ConfigureAwait(false);

        return members.Select(MapToDto);
    }

    /// <inheritdoc />
    public bool ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

        var normalized = NormalizePhoneNumber(phoneNumber);
        return normalized != null && KenyaPhoneRegex().IsMatch(normalized);
    }

    /// <inheritdoc />
    public string? NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        // Remove all non-digits
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle different formats
        // Already in 254XXXXXXXXX format
        if (digits.StartsWith("254") && digits.Length == 12)
            return digits;

        // Starts with 0 (e.g., 0712345678 or 0110123456)
        if (digits.StartsWith("0") && digits.Length == 10)
            return "254" + digits[1..];

        // Starts with 7 (e.g., 712345678) - Safaricom/Airtel
        if (digits.StartsWith("7") && digits.Length == 9)
            return "254" + digits;

        // Starts with 1 (e.g., 110123456) - Telkom
        if (digits.StartsWith("1") && digits.Length == 9)
            return "254" + digits;

        // Just 9 digits starting with valid prefix
        if (digits.Length == 9 && (digits.StartsWith("7") || digits.StartsWith("1")))
            return "254" + digits;

        // Invalid format
        return null;
    }

    /// <inheritdoc />
    public async Task<string> GenerateMembershipNumberAsync(CancellationToken cancellationToken = default)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequenceNumber = await _memberRepository.GetNextSequenceNumberAsync(datePrefix, cancellationToken)
            .ConfigureAwait(false);

        return $"LM-{datePrefix}-{sequenceNumber:D5}";
    }

    /// <inheritdoc />
    public async Task<bool> UpdateMemberAsync(int memberId, string? name, string? email, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            .ConfigureAwait(false);

        if (member == null) return false;

        // Validate name length if provided
        if (name != null)
        {
            var trimmedName = name.Trim();
            if (trimmedName.Length > 200)
            {
                _logger.LogWarning("UpdateMemberAsync: Name too long for member {MemberId}", memberId);
                return false;
            }
            member.Name = trimmedName;
        }

        // Validate email format if provided
        if (email != null)
        {
            var trimmedEmail = email.Trim();
            if (!string.IsNullOrEmpty(trimmedEmail) && !IsValidEmail(trimmedEmail))
            {
                _logger.LogWarning("UpdateMemberAsync: Invalid email format for member {MemberId}", memberId);
                return false;
            }
            member.Email = string.IsNullOrEmpty(trimmedEmail) ? null : trimmedEmail;
        }

        member.UpdatedByUserId = updatedByUserId;

        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Loyalty member {MemberId} updated by User {UserId}", memberId, updatedByUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateMemberAsync(int memberId, int deactivatedByUserId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            .ConfigureAwait(false);

        if (member == null) return false;

        member.IsActive = false;
        member.UpdatedByUserId = deactivatedByUserId;

        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Loyalty member {MemberId} deactivated by User {UserId}", memberId, deactivatedByUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateMemberAsync(int memberId, int reactivatedByUserId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            .ConfigureAwait(false);

        if (member == null) return false;

        member.IsActive = true;
        member.UpdatedByUserId = reactivatedByUserId;

        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Loyalty member {MemberId} reactivated by User {UserId}", memberId, reactivatedByUserId);
        return true;
    }

    /// <inheritdoc />
    public LoyaltyMemberDto MapToDto(LoyaltyMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        return new LoyaltyMemberDto
        {
            Id = member.Id,
            PhoneNumber = member.PhoneNumber,
            Name = member.Name,
            Email = member.Email,
            MembershipNumber = member.MembershipNumber,
            Tier = member.Tier,
            PointsBalance = member.PointsBalance,
            LifetimePoints = member.LifetimePoints,
            LifetimeSpend = member.LifetimeSpend,
            EnrolledAt = member.EnrolledAt,
            LastVisit = member.LastVisit,
            VisitCount = member.VisitCount,
            IsActive = member.IsActive
        };
    }

    // ================== Points Earning Methods ==================

    /// <inheritdoc />
    public async Task<PointsCalculationResult> CalculatePointsAsync(
        decimal transactionAmount,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        int? memberId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);

        // Use default values if no configuration exists
        var earningRate = config?.EarningRate ?? 100m;
        var earnOnDiscountedItems = config?.EarnOnDiscountedItems ?? true;
        var earnOnTax = config?.EarnOnTax ?? false;

        // Calculate eligible amount
        var eligibleAmount = transactionAmount;

        // Subtract discount if not earning on discounted items
        if (!earnOnDiscountedItems)
        {
            eligibleAmount -= discountAmount;
        }

        // Subtract tax if not earning on tax
        if (!earnOnTax)
        {
            eligibleAmount -= taxAmount;
        }

        // Ensure eligible amount is not negative
        eligibleAmount = Math.Max(0, eligibleAmount);

        // Calculate base points (amount / earning rate)
        var basePoints = Math.Floor(eligibleAmount / earningRate);

        // Get tier bonus multiplier if member is specified
        var bonusMultiplier = 1.0m;
        var bonusPoints = 0m;

        if (memberId.HasValue)
        {
            bonusMultiplier = await GetTierBonusMultiplierAsync(memberId.Value, cancellationToken).ConfigureAwait(false);

            if (bonusMultiplier > 1.0m)
            {
                // Calculate bonus points (multiplier applied to base, minus base)
                var totalWithBonus = Math.Floor(basePoints * bonusMultiplier);
                bonusPoints = totalWithBonus - basePoints;
            }
        }

        return new PointsCalculationResult
        {
            EligibleAmount = eligibleAmount,
            BasePoints = basePoints,
            BonusPoints = bonusPoints,
            BonusMultiplier = bonusMultiplier,
            EarningRate = earningRate,
            Description = $"Earned {basePoints + bonusPoints} points on KES {eligibleAmount:N0} spend"
        };
    }

    /// <inheritdoc />
    public async Task<PointsAwardResult> AwardPointsAsync(
        int memberId,
        int receiptId,
        string receiptNumber,
        decimal transactionAmount,
        decimal discountAmount,
        decimal taxAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the member
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
            if (member == null)
            {
                return PointsAwardResult.Failure("Loyalty member not found.");
            }

            if (!member.IsActive)
            {
                return PointsAwardResult.Failure("Loyalty member account is inactive.");
            }

            // Calculate points
            var calculation = await CalculatePointsAsync(
                transactionAmount,
                discountAmount,
                taxAmount,
                memberId,
                cancellationToken).ConfigureAwait(false);

            var totalPoints = calculation.TotalPoints;

            if (totalPoints <= 0)
            {
                _logger.LogInformation(
                    "No points earned for receipt {ReceiptNumber} - transaction amount too low",
                    receiptNumber);

                return PointsAwardResult.Success(
                    0, 0, 0, member.PointsBalance, member.PointsBalance, member.LifetimePoints);
            }

            var previousBalance = member.PointsBalance;

            // Update member points
            member.PointsBalance += totalPoints;
            member.LifetimePoints += totalPoints;
            member.LifetimeSpend += transactionAmount;
            member.LastVisit = DateTime.UtcNow;
            member.VisitCount++;

            // Create transaction record
            var transaction = new LoyaltyTransaction
            {
                LoyaltyMemberId = memberId,
                ReceiptId = receiptId,
                TransactionType = LoyaltyTransactionType.Earned,
                Points = totalPoints,
                MonetaryValue = transactionAmount,
                BalanceAfter = member.PointsBalance,
                BonusPoints = calculation.BonusPoints,
                BonusMultiplier = calculation.BonusMultiplier,
                TransactionDate = DateTime.UtcNow,
                ProcessedByUserId = processedByUserId,
                ReferenceNumber = receiptNumber,
                Description = calculation.Description,
                IsActive = true,
                CreatedByUserId = processedByUserId
            };

            await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
            await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Awarded {Points} points (bonus: {BonusPoints}) to member {MemberId} for receipt {ReceiptNumber}. New balance: {Balance}",
                totalPoints, calculation.BonusPoints, memberId, receiptNumber, member.PointsBalance);

            return PointsAwardResult.Success(
                transaction.Id,
                calculation.BasePoints,
                calculation.BonusPoints,
                previousBalance,
                member.PointsBalance,
                member.LifetimePoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to award points to member {MemberId} for receipt {ReceiptId}",
                memberId, receiptId);
            return PointsAwardResult.Failure("An error occurred while awarding points.");
        }
    }

    /// <inheritdoc />
    public async Task<PointsConfiguration?> GetPointsConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Get the default configuration
        var configs = await _pointsConfigRepository
            .FindAsync(c => c.IsDefault && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var defaultConfig = configs.FirstOrDefault();

        if (defaultConfig == null)
        {
            // Try to get any active configuration
            var anyConfigs = await _pointsConfigRepository
                .FindAsync(c => c.IsActive, cancellationToken)
                .ConfigureAwait(false);

            defaultConfig = anyConfigs.FirstOrDefault();
        }

        return defaultConfig;
    }

    /// <inheritdoc />
    public async Task<decimal> GetTierBonusMultiplierAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);

        if (member == null)
        {
            return 1.0m; // Default: no bonus
        }

        return TierBonusMultipliers.GetValueOrDefault(member.Tier, 1.0m);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateMemberVisitAsync(int memberId, decimal spendAmount, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);

        if (member == null)
        {
            _logger.LogWarning("Attempted to update visit for non-existent member {MemberId}", memberId);
            return false;
        }

        member.LastVisit = DateTime.UtcNow;
        member.VisitCount++;
        member.LifetimeSpend += spendAmount;

        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated visit for member {MemberId}. Visit count: {VisitCount}, Lifetime spend: {LifetimeSpend}",
            memberId, member.VisitCount, member.LifetimeSpend);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoyaltyTransactionDto>> GetTransactionHistoryAsync(
        int memberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _transactionRepository.QueryNoTracking()
            .Where(t => t.LoyaltyMemberId == memberId && t.IsActive);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return transactions.Select(MapTransactionToDto);
    }

    private static LoyaltyTransactionDto MapTransactionToDto(LoyaltyTransaction transaction)
    {
        return new LoyaltyTransactionDto
        {
            Id = transaction.Id,
            TransactionType = transaction.TransactionType,
            Points = transaction.Points,
            MonetaryValue = transaction.MonetaryValue,
            BalanceAfter = transaction.BalanceAfter,
            BonusPoints = transaction.BonusPoints,
            BonusMultiplier = transaction.BonusMultiplier,
            TransactionDate = transaction.TransactionDate,
            Description = transaction.Description,
            ReferenceNumber = transaction.ReferenceNumber
        };
    }

    // ================== Points Redemption Methods ==================

    /// <inheritdoc />
    public async Task<RedemptionPreviewResult> CalculateRedemptionAsync(
        int memberId,
        decimal transactionAmount,
        CancellationToken cancellationToken = default)
    {
        // Get the member
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null)
        {
            return RedemptionPreviewResult.Failure("Loyalty member not found.");
        }

        if (!member.IsActive)
        {
            return RedemptionPreviewResult.Failure("Loyalty member account is inactive.", member.PointsBalance);
        }

        // Get configuration
        var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);
        var redemptionValue = config?.RedemptionValue ?? 1m;
        var minimumPoints = config?.MinimumRedemptionPoints ?? 100;
        var maximumPoints = config?.MaximumRedemptionPoints ?? 0; // 0 = unlimited
        var maxPercentage = config?.MaxRedemptionPercentage ?? 50;

        var availablePoints = member.PointsBalance;
        var availableValue = availablePoints * redemptionValue;

        // Check if member has minimum points
        if (availablePoints < minimumPoints)
        {
            return RedemptionPreviewResult.Failure(
                $"Insufficient points. Minimum {minimumPoints} points required for redemption.",
                availablePoints,
                availableValue);
        }

        // Calculate maximum redeemable based on transaction amount percentage
        var maxValueByPercentage = transactionAmount * (maxPercentage / 100m);
        var maxPointsByPercentage = maxValueByPercentage / redemptionValue;

        // Apply maximum points limit if configured
        var maxPointsLimit = maximumPoints > 0 ? Math.Min(maximumPoints, availablePoints) : availablePoints;

        // The actual maximum is the minimum of all constraints
        var maxRedeemablePoints = Math.Min(Math.Min(maxPointsByPercentage, maxPointsLimit), availablePoints);
        maxRedeemablePoints = Math.Floor(maxRedeemablePoints); // Round down to whole points
        var maxRedeemableValue = maxRedeemablePoints * redemptionValue;

        // Ensure minimum threshold is met
        if (maxRedeemablePoints < minimumPoints)
        {
            return RedemptionPreviewResult.Failure(
                $"Transaction amount too low for redemption. Minimum {minimumPoints} points required.",
                availablePoints,
                availableValue);
        }

        // Suggest the maximum redeemable amount (rounded to nice numbers)
        var suggestedPoints = Math.Floor(maxRedeemablePoints / 10) * 10; // Round to nearest 10
        if (suggestedPoints < minimumPoints)
        {
            suggestedPoints = minimumPoints;
        }
        var suggestedValue = suggestedPoints * redemptionValue;

        return RedemptionPreviewResult.Success(
            availablePoints,
            availableValue,
            minimumPoints,
            maxRedeemablePoints,
            maxRedeemableValue,
            redemptionValue,
            suggestedPoints,
            suggestedValue);
    }

    /// <inheritdoc />
    public async Task<RedemptionResult> RedeemPointsAsync(
        int memberId,
        decimal pointsToRedeem,
        int receiptId,
        string receiptNumber,
        decimal transactionAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate points to redeem
            if (pointsToRedeem <= 0)
            {
                return RedemptionResult.Failure("Points to redeem must be greater than zero.");
            }

            // Get the member
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
            if (member == null)
            {
                return RedemptionResult.Failure("Loyalty member not found.");
            }

            if (!member.IsActive)
            {
                return RedemptionResult.Failure("Loyalty member account is inactive.");
            }

            // Get configuration
            var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);
            var redemptionValue = config?.RedemptionValue ?? 1m;
            var minimumPoints = config?.MinimumRedemptionPoints ?? 100;
            var maximumPoints = config?.MaximumRedemptionPoints ?? 0;
            var maxPercentage = config?.MaxRedemptionPercentage ?? 50;

            // Validate minimum points
            if (pointsToRedeem < minimumPoints)
            {
                return RedemptionResult.Failure($"Minimum {minimumPoints} points required for redemption.");
            }

            // Validate available points
            if (pointsToRedeem > member.PointsBalance)
            {
                return RedemptionResult.Failure(
                    $"Insufficient points. Available: {member.PointsBalance}, Requested: {pointsToRedeem}");
            }

            // Validate maximum points
            if (maximumPoints > 0 && pointsToRedeem > maximumPoints)
            {
                return RedemptionResult.Failure($"Maximum {maximumPoints} points can be redeemed per transaction.");
            }

            // Validate percentage limit
            var maxValueByPercentage = transactionAmount * (maxPercentage / 100m);
            var valueToRedeem = pointsToRedeem * redemptionValue;
            if (valueToRedeem > maxValueByPercentage)
            {
                return RedemptionResult.Failure(
                    $"Redemption value exceeds {maxPercentage}% of transaction amount. Maximum: KES {maxValueByPercentage:N0}");
            }

            var previousBalance = member.PointsBalance;

            // Update member points (deduct)
            member.PointsBalance -= pointsToRedeem;

            // Create transaction record
            var transaction = new LoyaltyTransaction
            {
                LoyaltyMemberId = memberId,
                ReceiptId = receiptId,
                TransactionType = LoyaltyTransactionType.Redeemed,
                Points = -pointsToRedeem, // Negative for redemption
                MonetaryValue = valueToRedeem,
                BalanceAfter = member.PointsBalance,
                BonusPoints = 0,
                BonusMultiplier = 1.0m,
                TransactionDate = DateTime.UtcNow,
                ProcessedByUserId = processedByUserId,
                ReferenceNumber = receiptNumber,
                Description = $"Redeemed {pointsToRedeem} points for KES {valueToRedeem:N0} discount",
                IsActive = true,
                CreatedByUserId = processedByUserId
            };

            await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
            await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Redeemed {Points} points (KES {Value}) from member {MemberId} for receipt {ReceiptNumber}. New balance: {Balance}",
                pointsToRedeem, valueToRedeem, memberId, receiptNumber, member.PointsBalance);

            return RedemptionResult.Success(
                transaction.Id,
                pointsToRedeem,
                valueToRedeem,
                previousBalance,
                member.PointsBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to redeem points for member {MemberId} on receipt {ReceiptId}",
                memberId, receiptId);
            return RedemptionResult.Failure("An error occurred while redeeming points.");
        }
    }

    /// <inheritdoc />
    public async Task<decimal> ConvertPointsToValueAsync(decimal points, CancellationToken cancellationToken = default)
    {
        var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);
        var redemptionValue = config?.RedemptionValue ?? 1m;
        return points * redemptionValue;
    }

    /// <inheritdoc />
    public async Task<decimal> ConvertValueToPointsAsync(decimal value, CancellationToken cancellationToken = default)
    {
        var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);
        var redemptionValue = config?.RedemptionValue ?? 1m;
        return redemptionValue > 0 ? value / redemptionValue : 0;
    }

    // ================== Tier Management Methods ==================

    /// <inheritdoc />
    public async Task<IEnumerable<TierConfigurationDto>> GetTierConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _tierConfigRepository
            .FindAsync(c => c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return configs
            .OrderBy(c => c.SortOrder)
            .Select(MapTierConfigToDto);
    }

    /// <inheritdoc />
    public async Task<TierConfigurationDto?> GetTierConfigurationAsync(MembershipTier tier, CancellationToken cancellationToken = default)
    {
        var configs = await _tierConfigRepository
            .FindAsync(c => c.Tier == tier && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var config = configs.FirstOrDefault();
        return config != null ? MapTierConfigToDto(config) : null;
    }

    /// <inheritdoc />
    public async Task<TierEvaluationResult?> EvaluateMemberTierAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return null;

        return await EvaluateTierInternal(member, member.LifetimeSpend, member.LifetimePoints, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TierEvaluationResult?> CheckAndUpgradeTierAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return null;

        var evaluation = await EvaluateTierInternal(member, member.LifetimeSpend, member.LifetimePoints, cancellationToken)
            .ConfigureAwait(false);

        // Only perform automatic upgrades, not downgrades
        if (evaluation.IsUpgrade)
        {
            member.Tier = evaluation.NewTier;
            await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Member {MemberId} upgraded from {OldTier} to {NewTier}. Lifetime spend: {Spend}",
                memberId, evaluation.PreviousTier, evaluation.NewTier, member.LifetimeSpend);
        }

        return evaluation;
    }

    /// <inheritdoc />
    public async Task<TierEvaluationResult?> PerformAnnualTierReviewAsync(
        int memberId,
        decimal periodSpend,
        decimal periodPoints,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return null;

        var previousTier = member.Tier;
        var evaluation = await EvaluateTierInternal(member, periodSpend, periodPoints, cancellationToken)
            .ConfigureAwait(false);

        // For annual review, apply both upgrades and downgrades
        if (evaluation.TierChanged)
        {
            member.Tier = evaluation.NewTier;
            await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var action = evaluation.IsUpgrade ? "upgraded" : "downgraded";
            _logger.LogInformation(
                "Annual review: Member {MemberId} {Action} from {OldTier} to {NewTier}. Period spend: {Spend}",
                memberId, action, previousTier, evaluation.NewTier, periodSpend);
        }

        return evaluation;
    }

    /// <inheritdoc />
    public async Task<bool> ManualTierUpgradeAsync(
        int memberId,
        MembershipTier newTier,
        string reason,
        int upgradedByUserId,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return false;

        var previousTier = member.Tier;
        member.Tier = newTier;
        member.UpdatedByUserId = upgradedByUserId;

        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Manual tier change: Member {MemberId} changed from {OldTier} to {NewTier} by User {UserId}. Reason: {Reason}",
            memberId, previousTier, newTier, upgradedByUserId, reason);

        return true;
    }

    /// <inheritdoc />
    public async Task<TierEvaluationResult?> GetTierProgressAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await EvaluateMemberTierAsync(memberId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TierEvaluationResult> EvaluateTierInternal(
        LoyaltyMember member,
        decimal evaluatedSpend,
        decimal evaluatedPoints,
        CancellationToken cancellationToken)
    {
        // Get all tier configurations
        var tierConfigs = (await _tierConfigRepository
            .FindAsync(c => c.IsActive, cancellationToken)
            .ConfigureAwait(false))
            .OrderBy(c => c.SpendThreshold)
            .ToList();

        // If no configs, use default behavior
        if (tierConfigs.Count == 0)
        {
            return TierEvaluationResult.NoChange(
                member.Tier, evaluatedSpend, evaluatedPoints, 0, 0, null);
        }

        // Determine which tier the member qualifies for based on spend/points
        var qualifiedTier = MembershipTier.Bronze;
        TierConfiguration? qualifiedConfig = null;

        foreach (var config in tierConfigs)
        {
            if (evaluatedSpend >= config.SpendThreshold || evaluatedPoints >= config.PointsThreshold)
            {
                qualifiedTier = config.Tier;
                qualifiedConfig = config;
            }
        }

        // Calculate progress towards next tier
        var nextTierConfig = tierConfigs
            .Where(c => (int)c.Tier > (int)qualifiedTier)
            .OrderBy(c => c.SpendThreshold)
            .FirstOrDefault();

        decimal nextTierProgress = 100m;
        decimal amountToNext = 0;
        MembershipTier? nextTier = null;

        if (nextTierConfig != null)
        {
            nextTier = nextTierConfig.Tier;
            var currentThreshold = qualifiedConfig?.SpendThreshold ?? 0;
            var nextThreshold = nextTierConfig.SpendThreshold;
            var range = nextThreshold - currentThreshold;

            if (range > 0)
            {
                var progressAmount = evaluatedSpend - currentThreshold;
                nextTierProgress = Math.Min(100, (progressAmount / range) * 100);
                amountToNext = Math.Max(0, nextThreshold - evaluatedSpend);
            }
        }

        // Create evaluation result
        if (qualifiedTier == member.Tier)
        {
            return TierEvaluationResult.NoChange(
                member.Tier,
                evaluatedSpend,
                evaluatedPoints,
                nextTierProgress,
                amountToNext,
                nextTier);
        }
        else if ((int)qualifiedTier > (int)member.Tier)
        {
            return TierEvaluationResult.Upgrade(
                member.Tier,
                qualifiedTier,
                qualifiedConfig != null ? MapTierConfigToDto(qualifiedConfig) : new TierConfigurationDto { Tier = qualifiedTier, Name = qualifiedTier.ToString() },
                evaluatedSpend,
                evaluatedPoints,
                nextTierProgress,
                amountToNext,
                nextTier);
        }
        else
        {
            return TierEvaluationResult.Downgrade(
                member.Tier,
                qualifiedTier,
                qualifiedConfig != null ? MapTierConfigToDto(qualifiedConfig) : new TierConfigurationDto { Tier = qualifiedTier, Name = qualifiedTier.ToString() },
                evaluatedSpend,
                evaluatedPoints);
        }
    }

    private static TierConfigurationDto MapTierConfigToDto(TierConfiguration config)
    {
        return new TierConfigurationDto
        {
            Tier = config.Tier,
            Name = config.Name,
            Description = config.Description,
            SpendThreshold = config.SpendThreshold,
            PointsThreshold = config.PointsThreshold,
            PointsMultiplier = config.PointsMultiplier,
            DiscountPercent = config.DiscountPercent,
            FreeDelivery = config.FreeDelivery,
            PriorityService = config.PriorityService,
            ColorCode = config.ColorCode,
            IconName = config.IconName
        };
    }

    // ================== Customer Analytics Methods ==================

    /// <inheritdoc />
    public async Task<CustomerAnalyticsDto?> GetCustomerAnalyticsAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return null;

        // Get tier configuration for display
        var tierConfig = await GetTierConfigurationAsync(member.Tier, cancellationToken).ConfigureAwait(false);

        // Calculate average basket
        var averageBasket = member.VisitCount > 0 ? member.LifetimeSpend / member.VisitCount : 0;

        // Calculate average days between visits
        decimal avgDaysBetweenVisits = 0;
        if (member.VisitCount > 1 && member.LastVisit.HasValue)
        {
            var totalDays = (member.LastVisit.Value - member.EnrolledAt).TotalDays;
            avgDaysBetweenVisits = (decimal)(totalDays / (member.VisitCount - 1));
        }

        // Calculate engagement score
        var engagementScore = await CalculateEngagementScoreAsync(memberId, cancellationToken).ConfigureAwait(false) ?? 0;

        // Get top categories
        var topCategories = await GetTopCategoriesAsync(memberId, 5, cancellationToken).ConfigureAwait(false);

        return new CustomerAnalyticsDto
        {
            MemberId = member.Id,
            Member = MapToDto(member),
            TotalSpend = member.LifetimeSpend,
            VisitCount = member.VisitCount,
            AverageBasket = averageBasket,
            AverageItemsPerTransaction = 0, // Would require receipt item analysis
            TopCategories = topCategories.ToList(),
            FirstVisit = member.EnrolledAt,
            LastVisit = member.LastVisit,
            AverageDaysBetweenVisits = avgDaysBetweenVisits,
            PointsBalance = member.PointsBalance,
            LifetimePoints = member.LifetimePoints,
            Tier = member.Tier,
            TierConfig = tierConfig,
            EngagementScore = engagementScore
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategorySpendDto>> GetTopCategoriesAsync(int memberId, int maxCategories = 5, CancellationToken cancellationToken = default)
    {
        // This would require access to Receipt and OrderItem repositories with Category data
        // For now, return an empty list as the data model needs to be connected
        // In a real implementation, this would:
        // 1. Get all receipts for the member via LoyaltyTransactions
        // 2. Sum up spending by category from OrderItems
        // 3. Return top N categories sorted by spend

        _logger.LogInformation("Getting top categories for member {MemberId}. Max: {MaxCategories}",
            memberId, maxCategories);

        // Placeholder implementation - would be expanded with receipt/order data
        return await Task.FromResult(new List<CategorySpendDto>());
    }

    /// <inheritdoc />
    public async Task<CustomerExportResult> ExportCustomerDataAsync(CustomerExportFilterDto filter, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build query based on filter
            var members = await _memberRepository
                .FindAsync(m => filter.IncludeInactive || m.IsActive, cancellationToken)
                .ConfigureAwait(false);

            var filteredMembers = members.AsEnumerable();

            // Apply additional filters
            if (filter.Tier.HasValue)
            {
                filteredMembers = filteredMembers.Where(m => m.Tier == filter.Tier.Value);
            }

            if (filter.MinSpend.HasValue)
            {
                filteredMembers = filteredMembers.Where(m => m.LifetimeSpend >= filter.MinSpend.Value);
            }

            if (filter.MinPoints.HasValue)
            {
                filteredMembers = filteredMembers.Where(m => m.PointsBalance >= filter.MinPoints.Value);
            }

            if (filter.StartDate.HasValue)
            {
                filteredMembers = filteredMembers.Where(m => m.EnrolledAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                filteredMembers = filteredMembers.Where(m => m.EnrolledAt <= filter.EndDate.Value);
            }

            var memberList = filteredMembers.ToList();

            if (memberList.Count == 0)
            {
                return CustomerExportResult.Failure("No customers match the specified filters.");
            }

            // Generate CSV content
            var csvContent = GenerateCsvExport(memberList);
            var fileName = $"loyalty_customers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation("Exported {Count} customers to CSV", memberList.Count);

            return CustomerExportResult.Success(csvContent, fileName, memberList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export customer data");
            return CustomerExportResult.Failure("An error occurred while exporting customer data.");
        }
    }

    /// <inheritdoc />
    public async Task<int?> CalculateEngagementScoreAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return null;

        // RFM Analysis (Recency, Frequency, Monetary)
        // Score from 0-100 based on:
        // - Recency: Days since last visit (max 30 days for full score)
        // - Frequency: Visits per month average
        // - Monetary: Average basket size

        int recencyScore = 0;
        int frequencyScore = 0;
        int monetaryScore = 0;

        // Recency Score (0-40 points)
        // Full score if visited within 7 days, zero if over 90 days
        if (member.LastVisit.HasValue)
        {
            var daysSince = (DateTime.UtcNow - member.LastVisit.Value).TotalDays;
            recencyScore = daysSince switch
            {
                <= 7 => 40,
                <= 14 => 35,
                <= 30 => 25,
                <= 60 => 15,
                <= 90 => 10,
                _ => 0
            };
        }

        // Frequency Score (0-30 points)
        // Based on average visits per month
        var monthsSinceEnrollment = Math.Max(1, (DateTime.UtcNow - member.EnrolledAt).TotalDays / 30);
        var visitsPerMonth = member.VisitCount / monthsSinceEnrollment;
        frequencyScore = visitsPerMonth switch
        {
            >= 8 => 30,
            >= 4 => 25,
            >= 2 => 20,
            >= 1 => 15,
            >= 0.5 => 10,
            _ => 5
        };

        // Monetary Score (0-30 points)
        // Based on average basket size
        var averageBasket = member.VisitCount > 0 ? member.LifetimeSpend / member.VisitCount : 0;
        monetaryScore = averageBasket switch
        {
            >= 10000 => 30,
            >= 5000 => 25,
            >= 2000 => 20,
            >= 1000 => 15,
            >= 500 => 10,
            _ => 5
        };

        return recencyScore + frequencyScore + monetaryScore;
    }

    private static byte[] GenerateCsvExport(List<LoyaltyMember> members)
    {
        var sb = new System.Text.StringBuilder();

        // Header row
        sb.AppendLine("MembershipNumber,Name,PhoneNumber,Email,Tier,PointsBalance,LifetimePoints,LifetimeSpend,VisitCount,EnrolledAt,LastVisit,IsActive");

        // Data rows
        foreach (var member in members)
        {
            sb.AppendLine($"\"{member.MembershipNumber}\",\"{EscapeCsvField(member.Name)}\",\"{member.PhoneNumber}\",\"{EscapeCsvField(member.Email)}\",\"{member.Tier}\",{member.PointsBalance},{member.LifetimePoints},{member.LifetimeSpend},{member.VisitCount},\"{member.EnrolledAt:yyyy-MM-dd}\",\"{member.LastVisit:yyyy-MM-dd}\",{member.IsActive}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // Escape double quotes
        var escaped = value.Replace("\"", "\"\"");

        // Prevent CSV formula injection by prefixing dangerous characters with a single quote
        // This prevents Excel from interpreting cell values as formulas
        if (escaped.Length > 0 && "=+-@\t\r".Contains(escaped[0]))
        {
            escaped = "'" + escaped;
        }

        return escaped;
    }

    /// <summary>
    /// Regex for validating Kenya phone numbers in 254XXXXXXXXX format.
    /// Matches numbers starting with 254 followed by 7 or 1 and 8 more digits.
    /// Covers: Safaricom (07xx), Airtel (07xx), Telkom (077x, 01xx).
    /// </summary>
    [GeneratedRegex(@"^254[17]\d{8}$")]
    private static partial Regex KenyaPhoneRegex();

    /// <summary>
    /// Regex for validating email addresses.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    /// <param name="email">The email to validate.</param>
    /// <returns>True if valid email format; otherwise, false.</returns>
    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true; // null/empty is acceptable (optional field)
        if (email.Length > 254) return false; // RFC 5321 limit
        return EmailRegex().IsMatch(email);
    }
}
