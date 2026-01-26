using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for birthday and one-time rewards management.
/// </summary>
public class BirthdayRewardService : IBirthdayRewardService
{
    private readonly POSDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BirthdayRewardService> _logger;
    private static readonly Random _random = new();

    public BirthdayRewardService(
        POSDbContext context,
        ILoyaltyService loyaltyService,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<BirthdayRewardService> logger)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    #region One-Time Reward Template Management

    public async Task<IEnumerable<OneTimeRewardDto>> GetRewardTemplatesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OneTimeRewards.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        var rewards = await query
            .OrderBy(r => r.RewardType)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rewards.Select(MapToDto);
    }

    public async Task<OneTimeRewardDto?> GetRewardTemplateByIdAsync(
        int rewardId,
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.Id == rewardId, cancellationToken);

        return reward != null ? MapToDto(reward) : null;
    }

    public async Task<OneTimeRewardDto?> GetBirthdayRewardTemplateAsync(
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.RewardType == OneTimeRewardType.Birthday && r.IsActive, cancellationToken);

        return reward != null ? MapToDto(reward) : null;
    }

    public async Task<OneTimeRewardDto> CreateRewardTemplateAsync(
        OneTimeRewardDto dto,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var reward = new OneTimeReward
        {
            Name = dto.Name,
            Description = dto.Description,
            RewardType = dto.RewardType,
            ValueType = dto.ValueType,
            Value = dto.Value,
            MinimumTier = dto.MinimumTier,
            ValidityDays = dto.ValidityDays,
            MinimumPurchaseAmount = dto.MinimumPurchaseAmount,
            MaximumDiscountAmount = dto.MaximumDiscountAmount,
            FreeItemProductId = dto.FreeItemProductId,
            SmsTemplate = dto.SmsTemplate,
            EmailTemplate = dto.EmailTemplate,
            SendSmsNotification = dto.SendSmsNotification,
            SendEmailNotification = dto.SendEmailNotification,
            DaysBeforeToIssue = dto.DaysBeforeToIssue,
            DaysAfterEventValid = dto.DaysAfterEventValid,
            IsActive = dto.IsActive,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.OneTimeRewards.Add(reward);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created one-time reward template {RewardId} ({Name}) of type {Type}",
            reward.Id, reward.Name, reward.RewardType);

        return MapToDto(reward);
    }

    public async Task<OneTimeRewardDto?> UpdateRewardTemplateAsync(
        OneTimeRewardDto dto,
        int updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken);

        if (reward == null) return null;

        reward.Name = dto.Name;
        reward.Description = dto.Description;
        reward.RewardType = dto.RewardType;
        reward.ValueType = dto.ValueType;
        reward.Value = dto.Value;
        reward.MinimumTier = dto.MinimumTier;
        reward.ValidityDays = dto.ValidityDays;
        reward.MinimumPurchaseAmount = dto.MinimumPurchaseAmount;
        reward.MaximumDiscountAmount = dto.MaximumDiscountAmount;
        reward.FreeItemProductId = dto.FreeItemProductId;
        reward.SmsTemplate = dto.SmsTemplate;
        reward.EmailTemplate = dto.EmailTemplate;
        reward.SendSmsNotification = dto.SendSmsNotification;
        reward.SendEmailNotification = dto.SendEmailNotification;
        reward.DaysBeforeToIssue = dto.DaysBeforeToIssue;
        reward.DaysAfterEventValid = dto.DaysAfterEventValid;
        reward.IsActive = dto.IsActive;
        reward.UpdatedByUserId = updatedByUserId;
        reward.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated one-time reward template {RewardId} ({Name})",
            reward.Id, reward.Name);

        return MapToDto(reward);
    }

    public async Task<bool> DeactivateRewardTemplateAsync(
        int rewardId,
        int deactivatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.Id == rewardId, cancellationToken);

        if (reward == null) return false;

        reward.IsActive = false;
        reward.UpdatedByUserId = deactivatedByUserId;
        reward.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deactivated one-time reward template {RewardId} ({Name})",
            reward.Id, reward.Name);

        return true;
    }

    #endregion

    #region Birthday Reward Processing

    public async Task<BirthdayRewardResult> IssueBirthdayRewardAsync(
        int memberId,
        int processedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Get the member
        var member = await _context.LoyaltyMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive, cancellationToken);

        if (member == null)
        {
            return BirthdayRewardResult.Failure("Member not found or inactive");
        }

        if (!member.DateOfBirth.HasValue)
        {
            return BirthdayRewardResult.Failure("Member does not have a date of birth on record");
        }

        // Get the active birthday reward template
        var template = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.RewardType == OneTimeRewardType.Birthday && r.IsActive, cancellationToken);

        if (template == null)
        {
            return BirthdayRewardResult.Failure("No active birthday reward template configured");
        }

        // Check tier requirement
        if (template.MinimumTier.HasValue && (int)member.Tier < (int)template.MinimumTier.Value)
        {
            return BirthdayRewardResult.Failure($"Member tier {member.Tier} does not qualify. Minimum tier: {template.MinimumTier.Value}");
        }

        // Check if already issued this year
        var currentYear = DateTime.UtcNow.Year;
        var alreadyIssued = await HasReceivedBirthdayRewardAsync(memberId, currentYear, cancellationToken);

        if (alreadyIssued)
        {
            return BirthdayRewardResult.AlreadyIssued(currentYear);
        }

        // Create the member reward
        var redemptionCode = await GenerateRedemptionCodeAsync(cancellationToken);
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddDays(template.ValidityDays);

        // If there's a days-after-event-valid setting, use the birthday + that many days
        var birthdayThisYear = new DateOnly(currentYear, member.DateOfBirth.Value.Month, member.DateOfBirth.Value.Day);
        if (template.DaysAfterEventValid > 0)
        {
            var eventBasedExpiry = birthdayThisYear.ToDateTime(TimeOnly.MinValue).AddDays(template.DaysAfterEventValid);
            expiresAt = eventBasedExpiry > expiresAt ? eventBasedExpiry : expiresAt;
        }

        var memberReward = new MemberReward
        {
            LoyaltyMemberId = memberId,
            OneTimeRewardId = template.Id,
            RedemptionCode = redemptionCode,
            Status = MemberRewardStatus.Active,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            RewardYear = currentYear,
            EventDate = birthdayThisYear,
            CreatedByUserId = processedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.MemberRewards.Add(memberReward);
        await _context.SaveChangesAsync(cancellationToken);

        // Send notifications
        var smsSent = false;
        var emailSent = false;

        if (template.SendSmsNotification && !string.IsNullOrEmpty(template.SmsTemplate) && !string.IsNullOrEmpty(member.PhoneNumber))
        {
            try
            {
                var smsMessage = FormatNotificationTemplate(template.SmsTemplate, member, memberReward, template);
                var smsResult = await _smsService.SendSmsAsync(member.PhoneNumber, smsMessage, cancellationToken);
                if (smsResult.Success)
                {
                    memberReward.SmsNotificationSent = true;
                    memberReward.SmsNotificationSentAt = DateTime.UtcNow;
                    smsSent = true;
                    _logger.LogInformation(
                        "Birthday reward SMS sent to member {MemberId} ({Phone})",
                        memberId, member.PhoneNumber);
                }
                else
                {
                    _logger.LogWarning(
                        "Birthday reward SMS failed for member {MemberId}: {Error}",
                        memberId, smsResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send birthday reward SMS to member {MemberId}",
                    memberId);
            }
        }

        if (template.SendEmailNotification && !string.IsNullOrEmpty(member.Email) && !string.IsNullOrEmpty(template.EmailTemplate))
        {
            try
            {
                var emailContent = FormatNotificationTemplate(template.EmailTemplate, member, memberReward, template);
                var emailResult = await _emailService.SendEmailAsync(
                    member.Email,
                    member.Name ?? "Valued Customer",
                    "Happy Birthday! 🎂 Your Special Reward Awaits",
                    emailContent,
                    isHtml: true,
                    cancellationToken: cancellationToken);
                if (emailResult.Success)
                {
                    memberReward.EmailNotificationSent = true;
                    memberReward.EmailNotificationSentAt = DateTime.UtcNow;
                    emailSent = true;
                    _logger.LogInformation(
                        "Birthday reward email sent to member {MemberId} ({Email})",
                        memberId, member.Email);
                }
                else
                {
                    _logger.LogWarning(
                        "Birthday reward email failed for member {MemberId}: {Error}",
                        memberId, emailResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send birthday reward email to member {MemberId}",
                    memberId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Issued birthday reward {RewardId} to member {MemberId} ({Phone}) for year {Year}. Code: {Code}",
            memberReward.Id, memberId, member.PhoneNumber, currentYear, redemptionCode);

        // Reload with template for DTO mapping
        await _context.Entry(memberReward).Reference(r => r.OneTimeReward).LoadAsync(cancellationToken);
        await _context.Entry(memberReward).Reference(r => r.LoyaltyMember).LoadAsync(cancellationToken);

        return BirthdayRewardResult.Success(MapToDto(memberReward), smsSent, emailSent);
    }

    public async Task<BirthdayRewardJobSummary> ProcessBirthdayRewardsAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var summary = new BirthdayRewardJobSummary
        {
            ProcessedAt = DateTime.UtcNow
        };

        // Get the active birthday reward template
        var template = await _context.OneTimeRewards
            .FirstOrDefaultAsync(r => r.RewardType == OneTimeRewardType.Birthday && r.IsActive, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("No active birthday reward template configured");
            return summary;
        }

        // Get members with birthdays on this date (considering days before issue)
        var members = await GetMembersWithBirthdayAsync(targetDate, template.DaysBeforeToIssue, cancellationToken);
        summary.TotalMembersWithBirthdays = members.Count();

        foreach (var memberDto in members)
        {
            try
            {
                var result = await IssueBirthdayRewardAsync(memberDto.Id, 0, cancellationToken);

                if (result.IsSuccess)
                {
                    summary.RewardsIssued++;
                    if (result.SmsSent) summary.SmsSent++;
                    if (result.EmailSent) summary.EmailsSent++;
                }
                else if (result.ErrorMessage?.Contains("already issued", StringComparison.OrdinalIgnoreCase) == true)
                {
                    summary.RewardsSkipped++;
                }
                else
                {
                    summary.RewardsFailed++;
                    _logger.LogWarning(
                        "Failed to issue birthday reward to member {MemberId}: {Error}",
                        memberDto.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                summary.RewardsFailed++;
                _logger.LogError(ex,
                    "Error processing birthday reward for member {MemberId}",
                    memberDto.Id);
            }
        }

        _logger.LogInformation(
            "Birthday rewards processing completed for {Date}. Issued: {Issued}, Skipped: {Skipped}, Failed: {Failed}",
            targetDate, summary.RewardsIssued, summary.RewardsSkipped, summary.RewardsFailed);

        return summary;
    }

    public async Task<IEnumerable<LoyaltyMemberDto>> GetMembersWithBirthdayAsync(
        DateOnly targetDate,
        int daysBeforeIssue = 0,
        CancellationToken cancellationToken = default)
    {
        // Calculate the effective birthday date (target date + days before issue)
        var effectiveDate = targetDate.AddDays(daysBeforeIssue);
        var month = effectiveDate.Month;
        var day = effectiveDate.Day;

        var members = await _context.LoyaltyMembers
            .Where(m => m.IsActive &&
                        m.DateOfBirth.HasValue &&
                        m.DateOfBirth.Value.Month == month &&
                        m.DateOfBirth.Value.Day == day)
            .ToListAsync(cancellationToken);

        return members.Select(_loyaltyService.MapToDto);
    }

    public async Task<bool> HasReceivedBirthdayRewardAsync(
        int memberId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.MemberRewards
            .AnyAsync(r => r.LoyaltyMemberId == memberId &&
                          r.OneTimeReward.RewardType == OneTimeRewardType.Birthday &&
                          r.RewardYear == year &&
                          r.Status != MemberRewardStatus.Cancelled,
                     cancellationToken);
    }

    #endregion

    #region Member Reward Management

    public async Task<IEnumerable<MemberRewardDto>> GetMemberRewardsAsync(
        int memberId,
        MemberRewardStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MemberRewards
            .Include(r => r.OneTimeReward)
            .Include(r => r.LoyaltyMember)
            .Where(r => r.LoyaltyMemberId == memberId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var rewards = await query
            .OrderByDescending(r => r.IssuedAt)
            .ToListAsync(cancellationToken);

        return rewards.Select(MapToDto);
    }

    public async Task<IEnumerable<MemberRewardDto>> GetActiveRewardsAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var rewards = await _context.MemberRewards
            .Include(r => r.OneTimeReward)
            .Include(r => r.LoyaltyMember)
            .Where(r => r.LoyaltyMemberId == memberId &&
                       r.Status == MemberRewardStatus.Active &&
                       r.ExpiresAt > now)
            .OrderBy(r => r.ExpiresAt)
            .ToListAsync(cancellationToken);

        return rewards.Select(MapToDto);
    }

    public async Task<MemberRewardDto?> GetRewardByCodeAsync(
        string redemptionCode,
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.MemberRewards
            .Include(r => r.OneTimeReward)
            .Include(r => r.LoyaltyMember)
            .FirstOrDefaultAsync(r => r.RedemptionCode == redemptionCode, cancellationToken);

        return reward != null ? MapToDto(reward) : null;
    }

    public async Task<(bool IsValid, string? ErrorMessage, MemberRewardDto? Reward)> ValidateRewardRedemptionAsync(
        string redemptionCode,
        decimal transactionAmount,
        CancellationToken cancellationToken = default)
    {
        var reward = await _context.MemberRewards
            .Include(r => r.OneTimeReward)
            .Include(r => r.LoyaltyMember)
            .FirstOrDefaultAsync(r => r.RedemptionCode == redemptionCode, cancellationToken);

        if (reward == null)
        {
            return (false, "Invalid redemption code", null);
        }

        if (reward.Status != MemberRewardStatus.Active)
        {
            return (false, $"Reward is {reward.Status}", MapToDto(reward));
        }

        if (DateTime.UtcNow > reward.ExpiresAt)
        {
            return (false, "Reward has expired", MapToDto(reward));
        }

        var template = reward.OneTimeReward;
        if (template.MinimumPurchaseAmount.HasValue && transactionAmount < template.MinimumPurchaseAmount.Value)
        {
            return (false, $"Minimum purchase of KES {template.MinimumPurchaseAmount.Value:N0} required", MapToDto(reward));
        }

        return (true, null, MapToDto(reward));
    }

    public async Task<RewardRedemptionResult> RedeemRewardAsync(
        string redemptionCode,
        int receiptId,
        decimal transactionAmount,
        int processedByUserId,
        CancellationToken cancellationToken = default)
    {
        var (isValid, errorMessage, rewardDto) = await ValidateRewardRedemptionAsync(
            redemptionCode, transactionAmount, cancellationToken);

        if (!isValid)
        {
            return RewardRedemptionResult.Failure(errorMessage!);
        }

        var reward = await _context.MemberRewards
            .Include(r => r.OneTimeReward)
            .Include(r => r.LoyaltyMember)
            .FirstAsync(r => r.RedemptionCode == redemptionCode, cancellationToken);

        var discountApplied = CalculateRewardDiscount(rewardDto!, transactionAmount);
        decimal pointsAwarded = 0;

        // Handle point rewards
        if (reward.OneTimeReward.ValueType == RewardValueType.FixedPoints)
        {
            pointsAwarded = reward.OneTimeReward.Value;

            // Award points to member
            var member = reward.LoyaltyMember;
            member.PointsBalance += pointsAwarded;
            member.LifetimePoints += pointsAwarded;
            member.UpdatedAt = DateTime.UtcNow;

            // Create loyalty transaction
            var transaction = new LoyaltyTransaction
            {
                LoyaltyMemberId = reward.LoyaltyMemberId,
                ReceiptId = receiptId,
                TransactionType = LoyaltyTransactionType.BirthdayReward,
                Points = pointsAwarded,
                BalanceAfter = member.PointsBalance,
                Description = $"Birthday reward: {reward.OneTimeReward.Name}",
                ReferenceNumber = redemptionCode,
                TransactionDate = DateTime.UtcNow,
                ProcessedByUserId = processedByUserId
            };

            _context.LoyaltyTransactions.Add(transaction);
        }

        // Update reward status
        reward.Status = MemberRewardStatus.Redeemed;
        reward.RedeemedAt = DateTime.UtcNow;
        reward.RedeemedOnReceiptId = receiptId;
        reward.RedeemedValue = discountApplied;
        reward.PointsAwarded = pointsAwarded;
        reward.UpdatedAt = DateTime.UtcNow;
        reward.UpdatedByUserId = processedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Redeemed reward {RewardId} (Code: {Code}) for member {MemberId}. Discount: KES {Discount}, Points: {Points}",
            reward.Id, redemptionCode, reward.LoyaltyMemberId, discountApplied, pointsAwarded);

        return RewardRedemptionResult.Success(MapToDto(reward), discountApplied, pointsAwarded);
    }

    public decimal CalculateRewardDiscount(MemberRewardDto reward, decimal transactionAmount)
    {
        var template = reward;

        return template.ValueType switch
        {
            RewardValueType.FixedPoints => 0, // Points don't give direct discount
            RewardValueType.PercentageDiscount => CalculatePercentageDiscount(template.Value, transactionAmount, null),
            RewardValueType.FixedDiscount => Math.Min(template.Value, transactionAmount),
            RewardValueType.FreeItem => 0, // Handled separately at item level
            RewardValueType.PointsMultiplier => 0, // Handled during points earning
            _ => 0
        };
    }

    private decimal CalculatePercentageDiscount(decimal percentage, decimal amount, decimal? maxDiscount)
    {
        var discount = amount * (percentage / 100);
        if (maxDiscount.HasValue)
        {
            discount = Math.Min(discount, maxDiscount.Value);
        }
        return discount;
    }

    #endregion

    #region Expiry Processing

    public async Task<int> ProcessExpiredRewardsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredRewards = await _context.MemberRewards
            .Where(r => r.Status == MemberRewardStatus.Active && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var reward in expiredRewards)
        {
            reward.Status = MemberRewardStatus.Expired;
            reward.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} rewards as expired", expiredRewards.Count);

        return expiredRewards.Count;
    }

    public async Task<int> SendExpiryWarningsAsync(
        int daysBeforeExpiry = 3,
        CancellationToken cancellationToken = default)
    {
        var warningDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        var expiringRewards = await _context.MemberRewards
            .Include(r => r.LoyaltyMember)
            .Include(r => r.OneTimeReward)
            .Where(r => r.Status == MemberRewardStatus.Active &&
                       r.ExpiresAt <= warningDate &&
                       r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var warnedCount = 0;

        foreach (var reward in expiringRewards)
        {
            try
            {
                var member = reward.LoyaltyMember;
                var daysLeft = (reward.ExpiresAt!.Value - DateTime.UtcNow).Days;
                var expiryDateStr = reward.ExpiresAt.Value.ToString("MMM dd");

                // Send SMS warning
                if (!string.IsNullOrEmpty(member?.PhoneNumber))
                {
                    var smsMessage = $"Your {reward.OneTimeReward?.Name ?? "reward"} expires in {daysLeft} days ({expiryDateStr}). " +
                                     $"Don't forget to redeem it! Code: {reward.RedemptionCode}";
                    await _smsService.SendSmsAsync(member.PhoneNumber, smsMessage, cancellationToken);
                }

                // Send email warning
                if (!string.IsNullOrEmpty(member?.Email))
                {
                    var emailContent = $"<p>Hi {member.Name ?? "Valued Customer"},</p>" +
                                       $"<p>Your <strong>{reward.OneTimeReward?.Name ?? "reward"}</strong> is expiring in {daysLeft} days on {expiryDateStr}!</p>" +
                                       $"<p>Redemption Code: <strong>{reward.RedemptionCode}</strong></p>" +
                                       $"<p>Visit us before it expires to enjoy your special reward.</p>";
                    await _emailService.SendEmailAsync(
                        member.Email,
                        member.Name ?? "Valued Customer",
                        "⏰ Your Reward is Expiring Soon!",
                        emailContent,
                        isHtml: true,
                        cancellationToken: cancellationToken);
                }

                _logger.LogInformation(
                    "Expiry warning sent: Reward {RewardId} for member {MemberId} expires on {ExpiresAt}",
                    reward.Id, reward.LoyaltyMemberId, reward.ExpiresAt);
                warnedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send expiry warning for reward {RewardId}", reward.Id);
            }
        }

        return warnedCount;
    }

    #endregion

    #region Utility Methods

    public async Task<string> GenerateRedemptionCodeAsync(CancellationToken cancellationToken = default)
    {
        string code;
        var attempts = 0;
        const int maxAttempts = 10;

        do
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = _random.Next(10000, 99999);
            code = $"RWD-{date}-{random}";
            attempts++;

            var exists = await _context.MemberRewards
                .AnyAsync(r => r.RedemptionCode == code, cancellationToken);

            if (!exists) return code;

        } while (attempts < maxAttempts);

        // Fallback with GUID component
        return $"RWD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";
    }

    public OneTimeRewardDto MapToDto(OneTimeReward entity)
    {
        return new OneTimeRewardDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            RewardType = entity.RewardType,
            ValueType = entity.ValueType,
            Value = entity.Value,
            MinimumTier = entity.MinimumTier,
            ValidityDays = entity.ValidityDays,
            MinimumPurchaseAmount = entity.MinimumPurchaseAmount,
            MaximumDiscountAmount = entity.MaximumDiscountAmount,
            FreeItemProductId = entity.FreeItemProductId,
            SmsTemplate = entity.SmsTemplate,
            EmailTemplate = entity.EmailTemplate,
            SendSmsNotification = entity.SendSmsNotification,
            SendEmailNotification = entity.SendEmailNotification,
            DaysBeforeToIssue = entity.DaysBeforeToIssue,
            DaysAfterEventValid = entity.DaysAfterEventValid,
            IsActive = entity.IsActive
        };
    }

    public MemberRewardDto MapToDto(MemberReward entity)
    {
        return new MemberRewardDto
        {
            Id = entity.Id,
            LoyaltyMemberId = entity.LoyaltyMemberId,
            MemberName = entity.LoyaltyMember?.Name,
            MemberPhone = entity.LoyaltyMember?.PhoneNumber ?? string.Empty,
            OneTimeRewardId = entity.OneTimeRewardId,
            RewardName = entity.OneTimeReward?.Name ?? string.Empty,
            RewardType = entity.OneTimeReward?.RewardType ?? OneTimeRewardType.Birthday,
            ValueType = entity.OneTimeReward?.ValueType ?? RewardValueType.FixedPoints,
            Value = entity.OneTimeReward?.Value ?? 0,
            RedemptionCode = entity.RedemptionCode,
            Status = entity.Status,
            IssuedAt = entity.IssuedAt,
            ExpiresAt = entity.ExpiresAt,
            RedeemedAt = entity.RedeemedAt,
            RewardYear = entity.RewardYear,
            EventDate = entity.EventDate
        };
    }

    private string FormatNotificationTemplate(
        string template,
        LoyaltyMember member,
        MemberReward reward,
        OneTimeReward rewardTemplate)
    {
        return template
            .Replace("{Name}", member.Name ?? "Valued Customer")
            .Replace("{RewardName}", rewardTemplate.Name)
            .Replace("{Value}", rewardTemplate.Value.ToString("N0"))
            .Replace("{ExpiryDate}", reward.ExpiresAt.ToString("dd MMM yyyy"))
            .Replace("{Code}", reward.RedemptionCode)
            .Replace("{MembershipNumber}", member.MembershipNumber)
            .Replace("{Tier}", member.Tier.ToString());
    }

    #endregion
}
