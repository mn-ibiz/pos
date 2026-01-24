using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of the referral service.
/// </summary>
public class ReferralService : IReferralService
{
    private readonly IRepository<ReferralCode> _codeRepository;
    private readonly IRepository<Referral> _referralRepository;
    private readonly IRepository<ReferralConfiguration> _configRepository;
    private readonly IRepository<ReferralMilestone> _milestoneRepository;
    private readonly IRepository<MemberReferralMilestone> _memberMilestoneRepository;
    private readonly IRepository<LoyaltyTransaction> _transactionRepository;
    private readonly ILoyaltyMemberRepository _memberRepository;
    private readonly ISmsService _smsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReferralService> _logger;

    private static readonly Random _random = new();

    public ReferralService(
        IRepository<ReferralCode> codeRepository,
        IRepository<Referral> referralRepository,
        IRepository<ReferralConfiguration> configRepository,
        IRepository<ReferralMilestone> milestoneRepository,
        IRepository<MemberReferralMilestone> memberMilestoneRepository,
        IRepository<LoyaltyTransaction> transactionRepository,
        ILoyaltyMemberRepository memberRepository,
        ISmsService smsService,
        IUnitOfWork unitOfWork,
        ILogger<ReferralService> logger)
    {
        _codeRepository = codeRepository ?? throw new ArgumentNullException(nameof(codeRepository));
        _referralRepository = referralRepository ?? throw new ArgumentNullException(nameof(referralRepository));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _milestoneRepository = milestoneRepository ?? throw new ArgumentNullException(nameof(milestoneRepository));
        _memberMilestoneRepository = memberMilestoneRepository ?? throw new ArgumentNullException(nameof(memberMilestoneRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Referral Code Management

    /// <inheritdoc />
    public async Task<ReferralCodeDto> GenerateReferralCodeAsync(int memberId, string? customCode = null, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null)
        {
            throw new InvalidOperationException($"Member {memberId} not found.");
        }

        // Check if member already has an active code
        var existingCode = await _codeRepository.Query()
            .FirstOrDefaultAsync(c => c.MemberId == memberId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (existingCode != null)
        {
            return MapCodeToDto(existingCode, member);
        }

        // Generate unique code
        var code = customCode ?? await GenerateUniqueCodeAsync(member, cancellationToken).ConfigureAwait(false);

        // Get config for shareable URL
        var config = await GetConfigurationInternalAsync(null, cancellationToken).ConfigureAwait(false);
        var shareableUrl = !string.IsNullOrEmpty(config.ShareableLinkBaseUrl)
            ? $"{config.ShareableLinkBaseUrl.TrimEnd('/')}/ref/{code}"
            : null;

        var referralCode = new ReferralCode
        {
            MemberId = memberId,
            Code = code,
            ShareableUrl = shareableUrl,
            TimesUsed = 0,
            TotalPointsEarned = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _codeRepository.AddAsync(referralCode, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Generated referral code {Code} for member {MemberId}", code, memberId);

        return MapCodeToDto(referralCode, member);
    }

    /// <inheritdoc />
    public async Task<ReferralCodeDto?> GetReferralCodeAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var code = await _codeRepository.Query()
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.MemberId == memberId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return code != null ? MapCodeToDto(code, code.Member) : null;
    }

    /// <inheritdoc />
    public async Task<ReferralCodeDto> GetOrCreateReferralCodeAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var existing = await GetReferralCodeAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        return await GenerateReferralCodeAsync(memberId, null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReferralCodeDto> RegenerateReferralCodeAsync(int memberId, string reason, CancellationToken cancellationToken = default)
    {
        // Deactivate existing code
        var existingCode = await _codeRepository.Query()
            .FirstOrDefaultAsync(c => c.MemberId == memberId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (existingCode != null)
        {
            existingCode.IsActive = false;
            existingCode.UpdatedAt = DateTime.UtcNow;
            _codeRepository.Update(existingCode);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deactivated referral code {Code} for member {MemberId}. Reason: {Reason}",
                existingCode.Code, memberId, reason);
        }

        // Generate new code
        return await GenerateReferralCodeAsync(memberId, null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReferralCodeValidation> ValidateReferralCodeAsync(string code, string? newMemberPhone = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return ReferralCodeValidation.Invalid("Referral code is required.");
        }

        var referralCode = await _codeRepository.Query()
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper() && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (referralCode == null)
        {
            return ReferralCodeValidation.Invalid("Invalid referral code.");
        }

        if (!referralCode.IsValid)
        {
            return ReferralCodeValidation.Invalid("This referral code has expired.");
        }

        // Check for self-referral
        if (!string.IsNullOrEmpty(newMemberPhone) && referralCode.Member != null)
        {
            var normalizedNew = NormalizePhone(newMemberPhone);
            var normalizedOwner = NormalizePhone(referralCode.Member.PhoneNumber);
            if (normalizedNew == normalizedOwner)
            {
                return ReferralCodeValidation.Invalid("You cannot use your own referral code.");
            }
        }

        // Check max referrals limit
        var config = await GetConfigurationInternalAsync(null, cancellationToken).ConfigureAwait(false);
        if (config.MaxReferralsPerMember.HasValue)
        {
            var successfulReferrals = await _referralRepository.Query()
                .CountAsync(r => r.ReferrerId == referralCode.MemberId && r.Status == ReferralStatus.Completed, cancellationToken)
                .ConfigureAwait(false);

            if (successfulReferrals >= config.MaxReferralsPerMember.Value)
            {
                return ReferralCodeValidation.Invalid("This referrer has reached their maximum referral limit.");
            }
        }

        var codeDto = MapCodeToDto(referralCode, referralCode.Member);
        return ReferralCodeValidation.Valid(codeDto, referralCode.Member?.Name ?? "Member", config.RefereeBonusPoints);
    }

    /// <inheritdoc />
    public async Task DeactivateReferralCodeAsync(int memberId, string reason, CancellationToken cancellationToken = default)
    {
        var code = await _codeRepository.Query()
            .FirstOrDefaultAsync(c => c.MemberId == memberId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (code != null)
        {
            code.IsActive = false;
            code.UpdatedAt = DateTime.UtcNow;
            _codeRepository.Update(code);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deactivated referral code {Code} for member {MemberId}. Reason: {Reason}",
                code.Code, memberId, reason);
        }
    }

    #endregion

    #region Referral Processing

    /// <inheritdoc />
    public async Task<ReferralSignupResult> ProcessReferralSignupAsync(string code, int newMemberId, CancellationToken cancellationToken = default)
    {
        // Validate the code
        var validation = await ValidateReferralCodeAsync(code, null, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return ReferralSignupResult.Failed(validation.ErrorMessage!);
        }

        var referralCode = await _codeRepository.Query()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper() && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (referralCode == null)
        {
            return ReferralSignupResult.Failed("Invalid referral code.");
        }

        // Check if referee is the same as referrer
        if (referralCode.MemberId == newMemberId)
        {
            return ReferralSignupResult.Failed("You cannot refer yourself.");
        }

        // Check if this member was already referred
        var existingReferral = await _referralRepository.Query()
            .FirstOrDefaultAsync(r => r.RefereeId == newMemberId, cancellationToken)
            .ConfigureAwait(false);

        if (existingReferral != null)
        {
            return ReferralSignupResult.Failed("This member has already been referred.");
        }

        // Get config
        var config = await GetConfigurationInternalAsync(null, cancellationToken).ConfigureAwait(false);

        // Create the referral
        var referral = new Referral
        {
            ReferrerId = referralCode.MemberId,
            RefereeId = newMemberId,
            ReferralCodeId = referralCode.Id,
            Status = ReferralStatus.Pending,
            ReferrerBonusPoints = 0, // Awarded on completion
            RefereeBonusPoints = 0,  // Awarded on completion
            ReferredAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(config.ExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _referralRepository.AddAsync(referral, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created referral: Referrer {ReferrerId} referred {RefereeId} using code {Code}",
            referralCode.MemberId, newMemberId, code);

        // Map to DTO
        var referralDto = await MapReferralToDtoAsync(referral, cancellationToken).ConfigureAwait(false);

        return ReferralSignupResult.Successful(referralDto, config.ExpiryDays, config.MinPurchaseAmount);
    }

    /// <inheritdoc />
    public async Task<ReferralCompletionResult> CompleteReferralAsync(int refereeId, int receiptId, decimal amount, CancellationToken cancellationToken = default)
    {
        // Find pending referral for this referee
        var referral = await _referralRepository.Query()
            .Include(r => r.Referrer)
            .Include(r => r.Referee)
            .Include(r => r.ReferralCode)
            .FirstOrDefaultAsync(r => r.RefereeId == refereeId && r.Status == ReferralStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        if (referral == null)
        {
            return ReferralCompletionResult.NoPendingReferral();
        }

        // Check if expired
        if (DateTime.UtcNow > referral.ExpiresAt)
        {
            referral.Status = ReferralStatus.Expired;
            _referralRepository.Update(referral);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ReferralCompletionResult.Failed("The referral has expired.");
        }

        // Get config to check minimum purchase
        var config = await GetConfigurationInternalAsync(null, cancellationToken).ConfigureAwait(false);
        if (amount < config.MinPurchaseAmount)
        {
            return ReferralCompletionResult.Failed($"Minimum purchase of KES {config.MinPurchaseAmount:N0} required to complete referral.");
        }

        // Complete the referral
        referral.Status = ReferralStatus.Completed;
        referral.CompletedAt = DateTime.UtcNow;
        referral.QualifyingReceiptId = receiptId;
        referral.QualifyingAmount = amount;
        referral.ReferrerBonusPoints = config.ReferrerBonusPoints;
        referral.RefereeBonusPoints = config.RefereeBonusPoints;
        referral.UpdatedAt = DateTime.UtcNow;

        _referralRepository.Update(referral);

        // Update referral code stats
        if (referral.ReferralCode != null)
        {
            referral.ReferralCode.TimesUsed++;
            referral.ReferralCode.TotalPointsEarned += config.ReferrerBonusPoints;
            _codeRepository.Update(referral.ReferralCode);
        }

        // Award points to both parties
        await AwardReferralPointsAsync(referral.ReferrerId, config.ReferrerBonusPoints, referral.Id, true, cancellationToken).ConfigureAwait(false);
        await AwardReferralPointsAsync(referral.RefereeId, config.RefereeBonusPoints, referral.Id, false, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Completed referral {ReferralId}: Referrer {ReferrerId} gets {ReferrerPoints} pts, Referee {RefereeId} gets {RefereePoints} pts",
            referral.Id, referral.ReferrerId, config.ReferrerBonusPoints, referral.RefereeId, config.RefereeBonusPoints);

        // Check for milestone achievements
        var milestoneResult = await CheckAndAwardMilestonesAsync(referral.ReferrerId, cancellationToken).ConfigureAwait(false);

        // Send notifications
        var referrerNotified = false;
        var refereeNotified = false;

        if (referral.Referrer != null && !string.IsNullOrEmpty(config.ReferrerSmsTemplate))
        {
            var message = config.ReferrerSmsTemplate
                .Replace("{points}", config.ReferrerBonusPoints.ToString())
                .Replace("{friend}", referral.Referee?.Name ?? "your friend");
            var result = await _smsService.SendSmsAsync(referral.Referrer.PhoneNumber, message).ConfigureAwait(false);
            referrerNotified = result.IsSuccess;
        }

        if (referral.Referee != null && !string.IsNullOrEmpty(config.RefereeSmsTemplate))
        {
            var message = config.RefereeSmsTemplate
                .Replace("{points}", config.RefereeBonusPoints.ToString())
                .Replace("{referrer}", referral.Referrer?.Name ?? "your friend");
            var result = await _smsService.SendSmsAsync(referral.Referee.PhoneNumber, message).ConfigureAwait(false);
            refereeNotified = result.IsSuccess;
        }

        var referralDto = await MapReferralToDtoAsync(referral, cancellationToken).ConfigureAwait(false);

        return new ReferralCompletionResult
        {
            Success = true,
            Message = "Referral completed successfully!",
            Referral = referralDto,
            ReferrerPointsAwarded = config.ReferrerBonusPoints,
            RefereePointsAwarded = config.RefereeBonusPoints,
            MilestoneResult = milestoneResult.MilestoneAchieved ? milestoneResult : null,
            ReferrerNotified = referrerNotified,
            RefereeNotified = refereeNotified
        };
    }

    /// <inheritdoc />
    public async Task<int> ExpireOldReferralsAsync(CancellationToken cancellationToken = default)
    {
        var expiredReferrals = await _referralRepository.Query()
            .Where(r => r.Status == ReferralStatus.Pending && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var referral in expiredReferrals)
        {
            referral.Status = ReferralStatus.Expired;
            referral.UpdatedAt = DateTime.UtcNow;
            _referralRepository.Update(referral);
        }

        if (expiredReferrals.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Expired {Count} referrals", expiredReferrals.Count);
        }

        return expiredReferrals.Count;
    }

    /// <inheritdoc />
    public async Task CancelReferralAsync(int referralId, string reason, int cancelledByUserId, CancellationToken cancellationToken = default)
    {
        var referral = await _referralRepository.GetByIdAsync(referralId, cancellationToken).ConfigureAwait(false);
        if (referral == null)
        {
            throw new InvalidOperationException($"Referral {referralId} not found.");
        }

        if (referral.Status != ReferralStatus.Pending)
        {
            throw new InvalidOperationException("Only pending referrals can be cancelled.");
        }

        referral.Status = ReferralStatus.Cancelled;
        referral.CancellationReason = reason;
        referral.UpdatedAt = DateTime.UtcNow;
        referral.UpdatedByUserId = cancelledByUserId;

        _referralRepository.Update(referral);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cancelled referral {ReferralId} by user {UserId}. Reason: {Reason}",
            referralId, cancelledByUserId, reason);
    }

    #endregion

    #region Referral Queries

    /// <inheritdoc />
    public async Task<List<ReferralDto>> GetMemberReferralsAsync(int memberId, bool asReferrer = true, ReferralStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _referralRepository.Query()
            .Include(r => r.Referrer)
            .Include(r => r.Referee)
            .Include(r => r.ReferralCode)
            .AsQueryable();

        if (asReferrer)
        {
            query = query.Where(r => r.ReferrerId == memberId);
        }
        else
        {
            query = query.Where(r => r.RefereeId == memberId);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var referrals = await query
            .OrderByDescending(r => r.ReferredAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = new List<ReferralDto>();
        foreach (var referral in referrals)
        {
            dtos.Add(await MapReferralToDtoAsync(referral, cancellationToken).ConfigureAwait(false));
        }

        return dtos;
    }

    /// <inheritdoc />
    public async Task<ReferralStats> GetMemberReferralStatsAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var referrals = await _referralRepository.Query()
            .Where(r => r.ReferrerId == memberId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stats = new ReferralStats
        {
            TotalReferrals = referrals.Count,
            SuccessfulReferrals = referrals.Count(r => r.Status == ReferralStatus.Completed),
            PendingReferrals = referrals.Count(r => r.Status == ReferralStatus.Pending),
            ExpiredReferrals = referrals.Count(r => r.Status == ReferralStatus.Expired),
            TotalPointsEarned = referrals.Where(r => r.Status == ReferralStatus.Completed).Sum(r => r.ReferrerBonusPoints)
        };

        // Get leaderboard rank
        stats.LeaderboardRank = await GetMemberLeaderboardRankAsync(memberId, LeaderboardPeriod.AllTime, cancellationToken).ConfigureAwait(false);

        // Get next milestone
        var milestoneProgress = await GetMilestoneProgressAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (milestoneProgress.NextMilestone != null)
        {
            stats.ReferralsToNextMilestone = milestoneProgress.NextMilestone.Remaining;
            stats.NextMilestoneName = milestoneProgress.NextMilestone.Milestone.Name;
        }

        return stats;
    }

    /// <inheritdoc />
    public async Task<ReferralDto?> GetPendingReferralAsync(int refereeId, CancellationToken cancellationToken = default)
    {
        var referral = await _referralRepository.Query()
            .Include(r => r.Referrer)
            .Include(r => r.Referee)
            .Include(r => r.ReferralCode)
            .FirstOrDefaultAsync(r => r.RefereeId == refereeId && r.Status == ReferralStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        return referral != null ? await MapReferralToDtoAsync(referral, cancellationToken).ConfigureAwait(false) : null;
    }

    /// <inheritdoc />
    public async Task<ReferralDto?> GetReferralAsync(int referralId, CancellationToken cancellationToken = default)
    {
        var referral = await _referralRepository.Query()
            .Include(r => r.Referrer)
            .Include(r => r.Referee)
            .Include(r => r.ReferralCode)
            .FirstOrDefaultAsync(r => r.Id == referralId, cancellationToken)
            .ConfigureAwait(false);

        return referral != null ? await MapReferralToDtoAsync(referral, cancellationToken).ConfigureAwait(false) : null;
    }

    #endregion

    #region Leaderboard

    /// <inheritdoc />
    public async Task<List<ReferralLeaderboardEntry>> GetReferralLeaderboardAsync(LeaderboardPeriod period = LeaderboardPeriod.AllTime, int top = 10, CancellationToken cancellationToken = default)
    {
        var query = _referralRepository.Query()
            .Where(r => r.Status == ReferralStatus.Completed);

        // Apply date filter based on period
        var startDate = GetPeriodStartDate(period);
        if (startDate.HasValue)
        {
            query = query.Where(r => r.CompletedAt >= startDate.Value);
        }

        var leaderboardData = await query
            .GroupBy(r => r.ReferrerId)
            .Select(g => new
            {
                MemberId = g.Key,
                SuccessfulReferrals = g.Count(),
                TotalPointsEarned = g.Sum(r => r.ReferrerBonusPoints)
            })
            .OrderByDescending(x => x.SuccessfulReferrals)
            .ThenByDescending(x => x.TotalPointsEarned)
            .Take(top)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var memberIds = leaderboardData.Select(x => x.MemberId).ToList();
        var members = await _memberRepository.Query()
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<ReferralLeaderboardEntry>();
        var rank = 1;
        foreach (var data in leaderboardData)
        {
            members.TryGetValue(data.MemberId, out var member);
            entries.Add(new ReferralLeaderboardEntry
            {
                Rank = rank++,
                MemberId = data.MemberId,
                MemberName = member?.Name,
                MembershipNumber = member?.MembershipNumber,
                SuccessfulReferrals = data.SuccessfulReferrals,
                TotalPointsEarned = data.TotalPointsEarned,
                Tier = member?.Tier ?? MembershipTier.Bronze
            });
        }

        return entries;
    }

    /// <inheritdoc />
    public async Task<int?> GetMemberLeaderboardRankAsync(int memberId, LeaderboardPeriod period, CancellationToken cancellationToken = default)
    {
        var query = _referralRepository.Query()
            .Where(r => r.Status == ReferralStatus.Completed);

        var startDate = GetPeriodStartDate(period);
        if (startDate.HasValue)
        {
            query = query.Where(r => r.CompletedAt >= startDate.Value);
        }

        var rankings = await query
            .GroupBy(r => r.ReferrerId)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var index = rankings.FindIndex(x => x.MemberId == memberId);
        return index >= 0 ? index + 1 : null;
    }

    #endregion

    #region Milestones

    /// <inheritdoc />
    public async Task<MilestoneCheckResult> CheckAndAwardMilestonesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        // Get member's successful referral count
        var successfulReferrals = await _referralRepository.Query()
            .CountAsync(r => r.ReferrerId == memberId && r.Status == ReferralStatus.Completed, cancellationToken)
            .ConfigureAwait(false);

        // Get milestones member hasn't achieved yet
        var achievedMilestoneIds = await _memberMilestoneRepository.Query()
            .Where(mm => mm.MemberId == memberId)
            .Select(mm => mm.MilestoneId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var unachievedMilestones = await _milestoneRepository.Query()
            .Where(m => m.IsActive && !achievedMilestoneIds.Contains(m.Id) && m.ReferralCount <= successfulReferrals)
            .OrderBy(m => m.ReferralCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!unachievedMilestones.Any())
        {
            return MilestoneCheckResult.NoMilestone();
        }

        // Award the highest milestone they've reached
        var milestoneToAward = unachievedMilestones.Last();

        var memberMilestone = new MemberReferralMilestone
        {
            MemberId = memberId,
            MilestoneId = milestoneToAward.Id,
            AchievedAt = DateTime.UtcNow,
            BonusPointsAwarded = milestoneToAward.BonusPoints,
            ReferralCountAtAchievement = successfulReferrals,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _memberMilestoneRepository.AddAsync(memberMilestone, cancellationToken).ConfigureAwait(false);

        // Award bonus points
        await AwardMilestonePointsAsync(memberId, milestoneToAward.BonusPoints, milestoneToAward.Name, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Member {MemberId} achieved milestone {MilestoneName} with {Count} referrals, awarded {Points} bonus points",
            memberId, milestoneToAward.Name, successfulReferrals, milestoneToAward.BonusPoints);

        return MilestoneCheckResult.Achieved(
            MapMilestoneToDto(milestoneToAward, true, DateTime.UtcNow),
            milestoneToAward.BonusPoints,
            successfulReferrals);
    }

    /// <inheritdoc />
    public async Task<List<ReferralMilestoneDto>> GetAvailableMilestonesAsync(CancellationToken cancellationToken = default)
    {
        var milestones = await _milestoneRepository.Query()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.ReferralCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return milestones.Select(m => MapMilestoneToDto(m, false, null)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ReferralMilestoneDto>> GetMemberMilestonesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var memberMilestones = await _memberMilestoneRepository.Query()
            .Include(mm => mm.Milestone)
            .Where(mm => mm.MemberId == memberId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return memberMilestones
            .Where(mm => mm.Milestone != null)
            .Select(mm => MapMilestoneToDto(mm.Milestone!, true, mm.AchievedAt))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MilestoneProgress> GetMilestoneProgressAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var successfulReferrals = await _referralRepository.Query()
            .CountAsync(r => r.ReferrerId == memberId && r.Status == ReferralStatus.Completed, cancellationToken)
            .ConfigureAwait(false);

        var achievedMilestoneIds = await _memberMilestoneRepository.Query()
            .Where(mm => mm.MemberId == memberId)
            .Select(mm => mm.MilestoneId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var allMilestones = await _milestoneRepository.Query()
            .Where(m => m.IsActive)
            .OrderBy(m => m.ReferralCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var progress = new MilestoneProgress
        {
            CurrentReferrals = successfulReferrals,
            Milestones = allMilestones.Select(m => new MilestoneProgressItem
            {
                Milestone = MapMilestoneToDto(m, achievedMilestoneIds.Contains(m.Id), null),
                CurrentCount = successfulReferrals,
                TargetCount = m.ReferralCount,
                IsAchieved = achievedMilestoneIds.Contains(m.Id)
            }).ToList()
        };

        progress.NextMilestone = progress.Milestones.FirstOrDefault(m => !m.IsAchieved);

        return progress;
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public async Task<ReferralConfigurationDto> GetConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationInternalAsync(storeId, cancellationToken).ConfigureAwait(false);
        return MapConfigToDto(config);
    }

    /// <inheritdoc />
    public async Task<ReferralConfigurationDto> UpdateConfigurationAsync(ReferralConfigurationDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.Query()
            .FirstOrDefaultAsync(c => c.StoreId == null && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (config == null)
        {
            config = new ReferralConfiguration
            {
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _configRepository.AddAsync(config, cancellationToken).ConfigureAwait(false);
        }

        config.ReferrerBonusPoints = dto.ReferrerBonusPoints;
        config.RefereeBonusPoints = dto.RefereeBonusPoints;
        config.MinPurchaseAmount = dto.MinPurchaseAmount;
        config.ExpiryDays = dto.ExpiryDays;
        config.MaxReferralsPerMember = dto.MaxReferralsPerMember;
        config.EnableLeaderboard = dto.EnableLeaderboard;
        config.RequireNewMember = dto.RequireNewMember;
        config.IsProgramActive = dto.IsProgramActive;
        config.ReferrerSmsTemplate = dto.ReferrerSmsTemplate;
        config.RefereeSmsTemplate = dto.RefereeSmsTemplate;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedByUserId = updatedByUserId;

        _configRepository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated referral configuration by user {UserId}", updatedByUserId);

        return MapConfigToDto(config);
    }

    #endregion

    #region Analytics

    /// <inheritdoc />
    public async Task<ReferralAnalytics> GetReferralAnalyticsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var referrals = await _referralRepository.Query()
            .Where(r => r.ReferredAt >= from && r.ReferredAt <= to)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var completedReferrals = referrals.Where(r => r.Status == ReferralStatus.Completed).ToList();

        var analytics = new ReferralAnalytics
        {
            TotalReferrals = referrals.Count,
            CompletedReferrals = completedReferrals.Count,
            PendingReferrals = referrals.Count(r => r.Status == ReferralStatus.Pending),
            ExpiredReferrals = referrals.Count(r => r.Status == ReferralStatus.Expired),
            TotalPointsDistributed = completedReferrals.Sum(r => r.ReferrerBonusPoints + r.RefereeBonusPoints),
            UniqueReferrers = referrals.Select(r => r.ReferrerId).Distinct().Count()
        };

        // Calculate average time to completion
        var completionTimes = completedReferrals
            .Where(r => r.CompletedAt.HasValue)
            .Select(r => (r.CompletedAt!.Value - r.ReferredAt).TotalDays)
            .ToList();

        analytics.AvgDaysToCompletion = completionTimes.Count > 0 ? (decimal)completionTimes.Average() : 0;

        // Get top referrers
        analytics.TopReferrers = await GetReferralLeaderboardAsync(LeaderboardPeriod.AllTime, 5, cancellationToken).ConfigureAwait(false);

        // Daily breakdown
        analytics.DailyBreakdown = referrals
            .GroupBy(r => DateOnly.FromDateTime(r.ReferredAt))
            .Select(g => new DailyReferralCount
            {
                Date = g.Key,
                NewReferrals = g.Count(),
                CompletedReferrals = g.Count(r => r.Status == ReferralStatus.Completed && r.CompletedAt.HasValue &&
                    DateOnly.FromDateTime(r.CompletedAt.Value) == g.Key)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return analytics;
    }

    #endregion

    #region Private Methods

    private async Task<ReferralConfiguration> GetConfigurationInternalAsync(int? storeId, CancellationToken cancellationToken)
    {
        var config = await _configRepository.Query()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        // Fall back to global config if store-specific not found
        if (config == null && storeId.HasValue)
        {
            config = await _configRepository.Query()
                .FirstOrDefaultAsync(c => c.StoreId == null && c.IsActive, cancellationToken)
                .ConfigureAwait(false);
        }

        // Return defaults if no config exists
        return config ?? new ReferralConfiguration
        {
            ReferrerBonusPoints = 500,
            RefereeBonusPoints = 200,
            MinPurchaseAmount = 500m,
            ExpiryDays = 30,
            EnableLeaderboard = true,
            RequireNewMember = true,
            IsProgramActive = true
        };
    }

    private async Task<string> GenerateUniqueCodeAsync(LoyaltyMember member, CancellationToken cancellationToken)
    {
        string code;
        var attempts = 0;
        const int maxAttempts = 10;

        do
        {
            // Try to create a personalized code first
            if (attempts == 0 && !string.IsNullOrEmpty(member.Name))
            {
                var namePart = new string(member.Name.ToUpper().Where(char.IsLetter).Take(4).ToArray());
                var yearPart = DateTime.UtcNow.Year.ToString();
                code = $"{namePart}{yearPart}";
            }
            else
            {
                // Generate random code
                code = GenerateRandomCode(8);
            }

            var exists = await _codeRepository.Query()
                .AnyAsync(c => c.Code.ToUpper() == code.ToUpper(), cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                return code;
            }

            attempts++;
        }
        while (attempts < maxAttempts);

        // Ultimate fallback: use member ID + random
        return $"REF{member.Id}{GenerateRandomCode(4)}";
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing characters
        return new string(Enumerable.Range(0, length).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }

    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return string.Empty;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("254")) return digits;
        if (digits.StartsWith("0")) return "254" + digits[1..];
        if (digits.Length == 9) return "254" + digits;
        return digits;
    }

    private static DateTime? GetPeriodStartDate(LeaderboardPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            LeaderboardPeriod.ThisWeek => now.AddDays(-(int)now.DayOfWeek),
            LeaderboardPeriod.ThisMonth => new DateTime(now.Year, now.Month, 1),
            LeaderboardPeriod.ThisYear => new DateTime(now.Year, 1, 1),
            LeaderboardPeriod.AllTime => null,
            _ => null
        };
    }

    private async Task AwardReferralPointsAsync(int memberId, int points, int referralId, bool isReferrer, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyMemberId = memberId,
            TransactionType = LoyaltyTransactionType.ReferralReward,
            Points = points,
            BalanceAfter = member.PointsBalance + points,
            TransactionDate = DateTime.UtcNow,
            Description = isReferrer ? "Referral bonus - friend made first purchase" : "Welcome bonus - referred by friend",
            ReferenceNumber = $"REF-{referralId}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);

        member.PointsBalance += points;
        member.LifetimePoints += points;
        member.UpdatedAt = DateTime.UtcNow;
        _memberRepository.Update(member);
    }

    private async Task AwardMilestonePointsAsync(int memberId, int points, string milestoneName, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null) return;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyMemberId = memberId,
            TransactionType = LoyaltyTransactionType.ReferralReward,
            Points = points,
            BalanceAfter = member.PointsBalance + points,
            TransactionDate = DateTime.UtcNow,
            Description = $"Milestone bonus - {milestoneName}",
            ReferenceNumber = $"MILESTONE-{milestoneName.ToUpper().Replace(" ", "-")}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);

        member.PointsBalance += points;
        member.LifetimePoints += points;
        member.UpdatedAt = DateTime.UtcNow;
        _memberRepository.Update(member);
    }

    private static ReferralCodeDto MapCodeToDto(ReferralCode code, LoyaltyMember? member)
    {
        return new ReferralCodeDto
        {
            Id = code.Id,
            MemberId = code.MemberId,
            MemberName = member?.Name,
            Code = code.Code,
            ShareableUrl = code.ShareableUrl,
            TimesUsed = code.TimesUsed,
            TotalPointsEarned = code.TotalPointsEarned,
            CreatedAt = code.CreatedAt,
            ExpiresAt = code.ExpiresAt,
            IsActive = code.IsActive
        };
    }

    private async Task<ReferralDto> MapReferralToDtoAsync(Referral referral, CancellationToken cancellationToken)
    {
        return new ReferralDto
        {
            Id = referral.Id,
            ReferrerId = referral.ReferrerId,
            ReferrerName = referral.Referrer?.Name,
            ReferrerPhone = referral.Referrer?.PhoneNumber,
            RefereeId = referral.RefereeId,
            RefereeName = referral.Referee?.Name,
            RefereePhone = referral.Referee?.PhoneNumber,
            ReferralCode = referral.ReferralCode?.Code ?? "",
            Status = referral.Status,
            ReferrerBonusPoints = referral.ReferrerBonusPoints,
            RefereeBonusPoints = referral.RefereeBonusPoints,
            ReferredAt = referral.ReferredAt,
            CompletedAt = referral.CompletedAt,
            ExpiresAt = referral.ExpiresAt,
            QualifyingAmount = referral.QualifyingAmount
        };
    }

    private static ReferralMilestoneDto MapMilestoneToDto(ReferralMilestone milestone, bool isAchieved, DateTime? achievedAt)
    {
        return new ReferralMilestoneDto
        {
            Id = milestone.Id,
            Name = milestone.Name,
            Description = milestone.Description,
            ReferralCount = milestone.ReferralCount,
            BonusPoints = milestone.BonusPoints,
            BadgeIcon = milestone.BadgeIcon,
            IsAchieved = isAchieved,
            AchievedAt = achievedAt
        };
    }

    private static ReferralConfigurationDto MapConfigToDto(ReferralConfiguration config)
    {
        return new ReferralConfigurationDto
        {
            Id = config.Id,
            ReferrerBonusPoints = config.ReferrerBonusPoints,
            RefereeBonusPoints = config.RefereeBonusPoints,
            MinPurchaseAmount = config.MinPurchaseAmount,
            ExpiryDays = config.ExpiryDays,
            MaxReferralsPerMember = config.MaxReferralsPerMember,
            EnableLeaderboard = config.EnableLeaderboard,
            RequireNewMember = config.RequireNewMember,
            IsProgramActive = config.IsProgramActive,
            ReferrerSmsTemplate = config.ReferrerSmsTemplate,
            RefereeSmsTemplate = config.RefereeSmsTemplate
        };
    }

    #endregion
}
