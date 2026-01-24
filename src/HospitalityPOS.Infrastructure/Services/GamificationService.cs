using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for gamification features (badges, challenges, streaks).
/// </summary>
public class GamificationService : IGamificationService
{
    private readonly POSDbContext _context;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(POSDbContext context, ILogger<GamificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Badge Management

    public async Task<List<BadgeDto>> GetAllBadgesAsync(int? storeId = null, bool includeSecret = false)
    {
        var query = _context.Badges
            .Where(b => b.IsActive)
            .Where(b => !storeId.HasValue || b.StoreId == null || b.StoreId == storeId);

        if (!includeSecret)
            query = query.Where(b => !b.IsSecret);

        var badges = await query
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .Select(b => MapToBadgeDto(b))
            .ToListAsync();

        return badges;
    }

    public async Task<BadgeDto?> GetBadgeByIdAsync(int badgeId)
    {
        var badge = await _context.Badges
            .FirstOrDefaultAsync(b => b.Id == badgeId && b.IsActive);

        return badge != null ? MapToBadgeDto(badge) : null;
    }

    public async Task<List<BadgeDto>> GetBadgesByCategoryAsync(BadgeCategory category, int? storeId = null)
    {
        return await _context.Badges
            .Where(b => b.IsActive && b.Category == category)
            .Where(b => !storeId.HasValue || b.StoreId == null || b.StoreId == storeId)
            .OrderBy(b => b.DisplayOrder)
            .Select(b => MapToBadgeDto(b))
            .ToListAsync();
    }

    public async Task<BadgeDto> CreateBadgeAsync(BadgeDto dto)
    {
        var badge = new Badge
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            TriggerType = dto.TriggerType,
            Rarity = dto.Rarity,
            IconUrl = dto.IconUrl,
            Color = dto.Color,
            PointsAwarded = dto.PointsAwarded,
            IsSecret = dto.IsSecret,
            IsRepeatable = dto.IsRepeatable,
            MaxEarnings = dto.MaxEarnings,
            ThresholdValue = dto.ThresholdValue,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DisplayOrder = dto.DisplayOrder,
            StoreId = dto.StoreId,
            IsActive = true
        };

        _context.Badges.Add(badge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created badge {BadgeId}: {BadgeName}", badge.Id, badge.Name);
        return MapToBadgeDto(badge);
    }

    public async Task<BadgeDto> UpdateBadgeAsync(BadgeDto dto)
    {
        var badge = await _context.Badges.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"Badge {dto.Id} not found");

        badge.Name = dto.Name;
        badge.Description = dto.Description;
        badge.Category = dto.Category;
        badge.TriggerType = dto.TriggerType;
        badge.Rarity = dto.Rarity;
        badge.IconUrl = dto.IconUrl;
        badge.Color = dto.Color;
        badge.PointsAwarded = dto.PointsAwarded;
        badge.IsSecret = dto.IsSecret;
        badge.IsRepeatable = dto.IsRepeatable;
        badge.MaxEarnings = dto.MaxEarnings;
        badge.ThresholdValue = dto.ThresholdValue;
        badge.StartDate = dto.StartDate;
        badge.EndDate = dto.EndDate;
        badge.DisplayOrder = dto.DisplayOrder;
        badge.StoreId = dto.StoreId;
        badge.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToBadgeDto(badge);
    }

    public async Task<bool> DeleteBadgeAsync(int badgeId)
    {
        var badge = await _context.Badges.FindAsync(badgeId);
        if (badge == null) return false;

        badge.IsActive = false;
        badge.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted badge {BadgeId}", badgeId);
        return true;
    }

    #endregion

    #region Member Badges

    public async Task<List<MemberBadgeDto>> GetMemberBadgesAsync(int memberId)
    {
        return await _context.MemberBadges
            .Include(mb => mb.Badge)
            .Include(mb => mb.Store)
            .Where(mb => mb.MemberId == memberId && mb.IsActive)
            .OrderByDescending(mb => mb.EarnedAt)
            .Select(mb => MapToMemberBadgeDto(mb))
            .ToListAsync();
    }

    public async Task<BadgeCollectionSummary> GetBadgeCollectionSummaryAsync(int memberId)
    {
        var memberBadges = await _context.MemberBadges
            .Include(mb => mb.Badge)
            .Where(mb => mb.MemberId == memberId && mb.IsActive)
            .ToListAsync();

        var allBadges = await _context.Badges
            .Where(b => b.IsActive && !b.IsSecret)
            .ToListAsync();

        var summary = new BadgeCollectionSummary
        {
            MemberId = memberId,
            TotalBadgesEarned = memberBadges.Sum(mb => mb.TimesEarned),
            UniqueBadgesEarned = memberBadges.Select(mb => mb.BadgeId).Distinct().Count(),
            TotalBadgesAvailable = allBadges.Count,
            TotalPointsFromBadges = memberBadges.Sum(mb => mb.PointsAwarded),
            SecretBadgesUnlocked = memberBadges.Count(mb => mb.Badge.IsSecret)
        };

        // Badges by category
        foreach (var category in Enum.GetValues<BadgeCategory>())
        {
            summary.BadgesByCategory[category] = memberBadges
                .Where(mb => mb.Badge.Category == category)
                .Select(mb => mb.BadgeId)
                .Distinct()
                .Count();
        }

        // Badges by rarity
        foreach (var rarity in Enum.GetValues<BadgeRarity>())
        {
            summary.BadgesByRarity[rarity] = memberBadges
                .Where(mb => mb.Badge.Rarity == rarity)
                .Select(mb => mb.BadgeId)
                .Distinct()
                .Count();
        }

        // Recent badges
        summary.RecentBadges = memberBadges
            .OrderByDescending(mb => mb.EarnedAt)
            .Take(5)
            .Select(mb => MapToMemberBadgeDto(mb))
            .ToList();

        // Pinned badges
        summary.PinnedBadges = memberBadges
            .Where(mb => mb.IsPinned)
            .Select(mb => MapToMemberBadgeDto(mb))
            .ToList();

        // Next badges to earn
        var earnedBadgeIds = memberBadges.Select(mb => mb.BadgeId).ToHashSet();
        summary.NextBadgesToEarn = allBadges
            .Where(b => !earnedBadgeIds.Contains(b.Id))
            .OrderBy(b => b.DisplayOrder)
            .Take(5)
            .Select(b => MapToBadgeDto(b))
            .ToList();

        return summary;
    }

    public async Task<BadgeAwardResult> AwardBadgeAsync(AwardBadgeRequest request)
    {
        var badge = await _context.Badges.FindAsync(request.BadgeId);
        if (badge == null || !badge.IsActive)
        {
            return new BadgeAwardResult { Success = false, Error = "Badge not found or inactive" };
        }

        // Check if badge is available (date range)
        if (badge.StartDate.HasValue && badge.StartDate > DateTime.UtcNow)
        {
            return new BadgeAwardResult { Success = false, Error = "Badge is not yet available" };
        }
        if (badge.EndDate.HasValue && badge.EndDate < DateTime.UtcNow)
        {
            return new BadgeAwardResult { Success = false, Error = "Badge is no longer available" };
        }

        // Check existing earnings
        var existingBadge = await _context.MemberBadges
            .FirstOrDefaultAsync(mb => mb.MemberId == request.MemberId && mb.BadgeId == request.BadgeId && mb.IsActive);

        if (existingBadge != null)
        {
            if (!badge.IsRepeatable)
            {
                return new BadgeAwardResult { Success = false, Error = "Badge already earned and is not repeatable" };
            }

            if (badge.MaxEarnings > 0 && existingBadge.TimesEarned >= badge.MaxEarnings)
            {
                return new BadgeAwardResult { Success = false, Error = "Maximum badge earnings reached" };
            }

            // Increment times earned
            existingBadge.TimesEarned++;
            existingBadge.PointsAwarded += badge.PointsAwarded;
            existingBadge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new BadgeAwardResult
            {
                Success = true,
                Badge = MapToMemberBadgeDto(existingBadge),
                PointsAwarded = badge.PointsAwarded,
                IsNewBadge = false,
                IsRepeatEarning = true,
                Message = $"Earned {badge.Name} again! ({existingBadge.TimesEarned}x)"
            };
        }

        // Create new member badge
        var memberBadge = new MemberBadge
        {
            MemberId = request.MemberId,
            BadgeId = request.BadgeId,
            EarnedAt = DateTime.UtcNow,
            TimesEarned = 1,
            PointsAwarded = badge.PointsAwarded,
            TriggeredByOrderId = request.OrderId,
            StoreId = request.StoreId,
            Notes = request.Notes,
            IsActive = true
        };

        _context.MemberBadges.Add(memberBadge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Awarded badge {BadgeId} to member {MemberId}", badge.Id, request.MemberId);

        return new BadgeAwardResult
        {
            Success = true,
            Badge = MapToMemberBadgeDto(memberBadge),
            PointsAwarded = badge.PointsAwarded,
            IsNewBadge = true,
            IsRepeatEarning = false,
            Message = $"Earned new badge: {badge.Name}!"
        };
    }

    public async Task<BadgeCheckResult> CheckAndAwardBadgesAsync(int memberId, int? orderId, int? storeId)
    {
        var result = new BadgeCheckResult { MemberId = memberId };

        // Get member stats for badge checking
        var member = await _context.LoyaltyMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive);

        if (member == null) return result;

        // Get all automatic badges
        var automaticBadges = await _context.Badges
            .Where(b => b.IsActive && b.TriggerType == BadgeTriggerType.Automatic)
            .Where(b => !b.StartDate.HasValue || b.StartDate <= DateTime.UtcNow)
            .Where(b => !b.EndDate.HasValue || b.EndDate >= DateTime.UtcNow)
            .Where(b => !b.StoreId.HasValue || b.StoreId == storeId)
            .ToListAsync();

        // Get member's existing badges
        var existingBadgeIds = await _context.MemberBadges
            .Where(mb => mb.MemberId == memberId && mb.IsActive)
            .Select(mb => mb.BadgeId)
            .ToListAsync();

        // Get member stats
        var totalVisits = member.TotalVisits;
        var totalSpend = member.TotalSpend;

        foreach (var badge in automaticBadges)
        {
            // Skip if already earned and not repeatable
            if (existingBadgeIds.Contains(badge.Id) && !badge.IsRepeatable)
                continue;

            bool shouldAward = false;

            // Check criteria based on category
            switch (badge.Category)
            {
                case BadgeCategory.Visits:
                    if (badge.ThresholdValue.HasValue && totalVisits >= badge.ThresholdValue)
                        shouldAward = true;
                    break;

                case BadgeCategory.Spending:
                    if (badge.ThresholdValue.HasValue && totalSpend >= badge.ThresholdValue)
                        shouldAward = true;
                    break;

                case BadgeCategory.Loyalty:
                    // Check tier-based badges
                    if (badge.ThresholdValue.HasValue && (int)member.Tier >= (int)badge.ThresholdValue)
                        shouldAward = true;
                    break;
            }

            if (shouldAward)
            {
                var awardResult = await AwardBadgeAsync(new AwardBadgeRequest
                {
                    MemberId = memberId,
                    BadgeId = badge.Id,
                    OrderId = orderId,
                    StoreId = storeId
                });

                result.AwardedBadges.Add(awardResult);
                if (awardResult.Success)
                    result.TotalPointsAwarded += awardResult.PointsAwarded;
            }
        }

        return result;
    }

    public async Task<bool> MarkBadgeViewedAsync(int memberBadgeId)
    {
        var memberBadge = await _context.MemberBadges.FindAsync(memberBadgeId);
        if (memberBadge == null) return false;

        memberBadge.IsViewed = true;
        memberBadge.ViewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleBadgePinAsync(int memberBadgeId)
    {
        var memberBadge = await _context.MemberBadges.FindAsync(memberBadgeId);
        if (memberBadge == null) return false;

        memberBadge.IsPinned = !memberBadge.IsPinned;
        memberBadge.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MemberBadgeDto>> GetUnviewedBadgesAsync(int memberId)
    {
        return await _context.MemberBadges
            .Include(mb => mb.Badge)
            .Where(mb => mb.MemberId == memberId && mb.IsActive && !mb.IsViewed)
            .OrderByDescending(mb => mb.EarnedAt)
            .Select(mb => MapToMemberBadgeDto(mb))
            .ToListAsync();
    }

    #endregion

    #region Challenge Management

    public async Task<List<ChallengeDto>> GetActiveChallengesAsync(int? storeId = null, LoyaltyTier? memberTier = null)
    {
        var now = DateTime.UtcNow;

        var query = _context.Challenges
            .Include(c => c.RewardBadge)
            .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now)
            .Where(c => !c.StoreId.HasValue || c.StoreId == storeId);

        if (memberTier.HasValue)
        {
            query = query.Where(c => !c.MinimumTier.HasValue || c.MinimumTier <= memberTier);
        }

        return await query
            .OrderBy(c => c.EndDate)
            .Select(c => MapToChallengeDto(c))
            .ToListAsync();
    }

    public async Task<ChallengeDto?> GetChallengeByIdAsync(int challengeId)
    {
        var challenge = await _context.Challenges
            .Include(c => c.RewardBadge)
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.IsActive);

        return challenge != null ? MapToChallengeDto(challenge) : null;
    }

    public async Task<List<ChallengeDto>> GetChallengesByPeriodAsync(ChallengePeriod period, int? storeId = null)
    {
        var now = DateTime.UtcNow;

        return await _context.Challenges
            .Include(c => c.RewardBadge)
            .Where(c => c.IsActive && c.Period == period)
            .Where(c => c.StartDate <= now && c.EndDate >= now)
            .Where(c => !c.StoreId.HasValue || c.StoreId == storeId)
            .OrderBy(c => c.EndDate)
            .Select(c => MapToChallengeDto(c))
            .ToListAsync();
    }

    public async Task<ChallengeDto> CreateChallengeAsync(ChallengeDto dto)
    {
        var challenge = new Challenge
        {
            Name = dto.Name,
            Description = dto.Description,
            Period = dto.Period,
            GoalType = dto.GoalType,
            TargetValue = dto.TargetValue,
            RewardPoints = dto.RewardPoints,
            RewardBadgeId = dto.RewardBadgeId,
            BonusMultiplier = dto.BonusMultiplier,
            IconUrl = dto.IconUrl,
            Color = dto.Color,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsRecurring = dto.IsRecurring,
            ProductId = dto.ProductId,
            CategoryId = dto.CategoryId,
            MinimumTier = dto.MinimumTier,
            MaxParticipants = dto.MaxParticipants,
            ShowLeaderboard = dto.ShowLeaderboard,
            StoreId = dto.StoreId,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created challenge {ChallengeId}: {ChallengeName}", challenge.Id, challenge.Name);
        return MapToChallengeDto(challenge);
    }

    public async Task<ChallengeDto> UpdateChallengeAsync(ChallengeDto dto)
    {
        var challenge = await _context.Challenges.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"Challenge {dto.Id} not found");

        challenge.Name = dto.Name;
        challenge.Description = dto.Description;
        challenge.Period = dto.Period;
        challenge.GoalType = dto.GoalType;
        challenge.TargetValue = dto.TargetValue;
        challenge.RewardPoints = dto.RewardPoints;
        challenge.RewardBadgeId = dto.RewardBadgeId;
        challenge.BonusMultiplier = dto.BonusMultiplier;
        challenge.IconUrl = dto.IconUrl;
        challenge.Color = dto.Color;
        challenge.StartDate = dto.StartDate;
        challenge.EndDate = dto.EndDate;
        challenge.IsRecurring = dto.IsRecurring;
        challenge.ProductId = dto.ProductId;
        challenge.CategoryId = dto.CategoryId;
        challenge.MinimumTier = dto.MinimumTier;
        challenge.MaxParticipants = dto.MaxParticipants;
        challenge.ShowLeaderboard = dto.ShowLeaderboard;
        challenge.StoreId = dto.StoreId;
        challenge.DisplayOrder = dto.DisplayOrder;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToChallengeDto(challenge);
    }

    public async Task<bool> DeleteChallengeAsync(int challengeId)
    {
        var challenge = await _context.Challenges.FindAsync(challengeId);
        if (challenge == null) return false;

        challenge.IsActive = false;
        challenge.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted challenge {ChallengeId}", challengeId);
        return true;
    }

    #endregion

    #region Member Challenges

    public async Task<List<MemberChallengeDto>> GetMemberActiveChallengesAsync(int memberId)
    {
        return await _context.MemberChallenges
            .Include(mc => mc.Challenge)
                .ThenInclude(c => c.RewardBadge)
            .Where(mc => mc.MemberId == memberId && mc.IsActive && mc.Status == ChallengeStatus.Active)
            .Where(mc => mc.Challenge.EndDate >= DateTime.UtcNow)
            .OrderBy(mc => mc.Challenge.EndDate)
            .Select(mc => MapToMemberChallengeDto(mc))
            .ToListAsync();
    }

    public async Task<List<MemberChallengeDto>> GetMemberCompletedChallengesAsync(int memberId)
    {
        return await _context.MemberChallenges
            .Include(mc => mc.Challenge)
            .Where(mc => mc.MemberId == memberId && mc.IsActive && mc.Status == ChallengeStatus.Completed)
            .OrderByDescending(mc => mc.CompletedAt)
            .Select(mc => MapToMemberChallengeDto(mc))
            .ToListAsync();
    }

    public async Task<List<MemberChallengeDto>> GetMemberChallengeHistoryAsync(int memberId, int? limit = null)
    {
        var query = _context.MemberChallenges
            .Include(mc => mc.Challenge)
            .Where(mc => mc.MemberId == memberId && mc.IsActive)
            .OrderByDescending(mc => mc.JoinedAt);

        if (limit.HasValue)
            query = (IOrderedQueryable<MemberChallenge>)query.Take(limit.Value);

        return await query.Select(mc => MapToMemberChallengeDto(mc)).ToListAsync();
    }

    public async Task<ChallengeJoinResult> JoinChallengeAsync(int memberId, int challengeId)
    {
        var challenge = await _context.Challenges.FindAsync(challengeId);
        if (challenge == null || !challenge.IsActive)
        {
            return new ChallengeJoinResult { Success = false, Error = "Challenge not found or inactive" };
        }

        // Check if challenge is active
        var now = DateTime.UtcNow;
        if (now < challenge.StartDate || now > challenge.EndDate)
        {
            return new ChallengeJoinResult { Success = false, Error = "Challenge is not currently active" };
        }

        // Check if already joined
        var existing = await _context.MemberChallenges
            .FirstOrDefaultAsync(mc => mc.MemberId == memberId && mc.ChallengeId == challengeId && mc.IsActive);

        if (existing != null)
        {
            return new ChallengeJoinResult
            {
                Success = true,
                MemberChallenge = MapToMemberChallengeDto(existing),
                Message = "Already enrolled in this challenge"
            };
        }

        // Check max participants
        if (challenge.MaxParticipants > 0)
        {
            var currentCount = await _context.MemberChallenges
                .CountAsync(mc => mc.ChallengeId == challengeId && mc.IsActive);

            if (currentCount >= challenge.MaxParticipants)
            {
                return new ChallengeJoinResult { Success = false, Error = "Challenge is full" };
            }
        }

        // Check tier requirement
        if (challenge.MinimumTier.HasValue)
        {
            var member = await _context.LoyaltyMembers.FindAsync(memberId);
            if (member == null || member.Tier < challenge.MinimumTier)
            {
                return new ChallengeJoinResult { Success = false, Error = "Tier requirement not met" };
            }
        }

        // Create member challenge
        var memberChallenge = new MemberChallenge
        {
            MemberId = memberId,
            ChallengeId = challengeId,
            CurrentProgress = 0,
            Status = ChallengeStatus.Active,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.MemberChallenges.Add(memberChallenge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member {MemberId} joined challenge {ChallengeId}", memberId, challengeId);

        return new ChallengeJoinResult
        {
            Success = true,
            MemberChallenge = MapToMemberChallengeDto(memberChallenge),
            Message = $"Joined challenge: {challenge.Name}"
        };
    }

    public async Task<ChallengeProgressResult> UpdateChallengeProgressAsync(int memberId, int? orderId, decimal? spendAmount, int? storeId)
    {
        var result = new ChallengeProgressResult { MemberId = memberId };

        // Get active challenges for member
        var activeChallenges = await _context.MemberChallenges
            .Include(mc => mc.Challenge)
                .ThenInclude(c => c.RewardBadge)
            .Where(mc => mc.MemberId == memberId && mc.IsActive && mc.Status == ChallengeStatus.Active)
            .Where(mc => mc.Challenge.EndDate >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var mc in activeChallenges)
        {
            decimal progressIncrement = 0;

            switch (mc.Challenge.GoalType)
            {
                case ChallengeGoalType.VisitCount:
                    progressIncrement = 1;
                    break;

                case ChallengeGoalType.SpendAmount:
                    progressIncrement = spendAmount ?? 0;
                    break;

                case ChallengeGoalType.PointsEarned:
                    // Would need to track points earned
                    break;
            }

            if (progressIncrement > 0)
            {
                mc.CurrentProgress += progressIncrement;
                mc.LastProgressAt = DateTime.UtcNow;

                // Check if completed
                if (mc.CurrentProgress >= mc.Challenge.TargetValue)
                {
                    mc.Status = ChallengeStatus.Completed;
                    mc.CompletedAt = DateTime.UtcNow;
                    mc.PointsAwarded = mc.Challenge.RewardPoints;

                    var completionResult = new ChallengeCompletionResult
                    {
                        Success = true,
                        ChallengeId = mc.ChallengeId,
                        ChallengeName = mc.Challenge.Name,
                        PointsAwarded = mc.Challenge.RewardPoints
                    };

                    // Award badge if configured
                    if (mc.Challenge.RewardBadgeId.HasValue)
                    {
                        var badgeResult = await AwardBadgeAsync(new AwardBadgeRequest
                        {
                            MemberId = memberId,
                            BadgeId = mc.Challenge.RewardBadgeId.Value,
                            OrderId = orderId,
                            StoreId = storeId,
                            Notes = $"Completed challenge: {mc.Challenge.Name}"
                        });

                        completionResult.BadgeAwarded = badgeResult;
                        if (badgeResult.Success)
                            completionResult.PointsAwarded += badgeResult.PointsAwarded;
                    }

                    result.CompletedChallenges.Add(completionResult);
                    result.TotalPointsAwarded += completionResult.PointsAwarded;

                    _logger.LogInformation("Member {MemberId} completed challenge {ChallengeId}", memberId, mc.ChallengeId);
                }

                result.UpdatedChallenges.Add(MapToMemberChallengeDto(mc));
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<ChallengeLeaderboard> GetChallengeLeaderboardAsync(int challengeId, int? memberId = null, int topCount = 10)
    {
        var challenge = await _context.Challenges.FindAsync(challengeId);
        if (challenge == null)
        {
            return new ChallengeLeaderboard { ChallengeId = challengeId };
        }

        var participants = await _context.MemberChallenges
            .Include(mc => mc.Member)
            .Where(mc => mc.ChallengeId == challengeId && mc.IsActive)
            .OrderByDescending(mc => mc.CurrentProgress)
            .ThenBy(mc => mc.CompletedAt)
            .ToListAsync();

        var leaderboard = new ChallengeLeaderboard
        {
            ChallengeId = challengeId,
            ChallengeName = challenge.Name,
            TotalParticipants = participants.Count,
            CompletedCount = participants.Count(p => p.Status == ChallengeStatus.Completed)
        };

        int rank = 1;
        foreach (var p in participants.Take(topCount))
        {
            leaderboard.Entries.Add(new ChallengeLeaderboardEntry
            {
                Rank = rank++,
                MemberId = p.MemberId,
                MemberName = $"{p.Member.FirstName} {p.Member.LastName}".Trim(),
                Tier = p.Member.Tier,
                Progress = p.CurrentProgress,
                ProgressPercentage = challenge.TargetValue > 0 ? (p.CurrentProgress / challenge.TargetValue) * 100 : 0,
                IsCompleted = p.Status == ChallengeStatus.Completed,
                CompletedAt = p.CompletedAt
            });
        }

        // Add current member's rank if specified
        if (memberId.HasValue)
        {
            var memberEntry = participants.FirstOrDefault(p => p.MemberId == memberId);
            if (memberEntry != null)
            {
                var memberRank = participants.IndexOf(memberEntry) + 1;
                leaderboard.CurrentMemberRank = new ChallengeLeaderboardEntry
                {
                    Rank = memberRank,
                    MemberId = memberEntry.MemberId,
                    MemberName = $"{memberEntry.Member.FirstName} {memberEntry.Member.LastName}".Trim(),
                    Tier = memberEntry.Member.Tier,
                    Progress = memberEntry.CurrentProgress,
                    ProgressPercentage = challenge.TargetValue > 0 ? (memberEntry.CurrentProgress / challenge.TargetValue) * 100 : 0,
                    IsCompleted = memberEntry.Status == ChallengeStatus.Completed,
                    CompletedAt = memberEntry.CompletedAt
                };
            }
        }

        return leaderboard;
    }

    public async Task<List<ChallengeJoinResult>> AutoEnrollMemberInChallengesAsync(int memberId, int? storeId = null)
    {
        var results = new List<ChallengeJoinResult>();

        var member = await _context.LoyaltyMembers.FindAsync(memberId);
        if (member == null) return results;

        var activeChallenges = await GetActiveChallengesAsync(storeId, member.Tier);

        // Get already joined challenges
        var joinedChallengeIds = await _context.MemberChallenges
            .Where(mc => mc.MemberId == memberId && mc.IsActive)
            .Select(mc => mc.ChallengeId)
            .ToListAsync();

        foreach (var challenge in activeChallenges.Where(c => !joinedChallengeIds.Contains(c.Id)))
        {
            var result = await JoinChallengeAsync(memberId, challenge.Id);
            results.Add(result);
        }

        return results;
    }

    #endregion

    #region Streak Management

    public async Task<List<StreakMilestoneDefinitionDto>> GetStreakMilestoneDefinitionsAsync(StreakType? streakType = null)
    {
        var query = _context.StreakMilestoneDefinitions.Where(s => s.IsActive);

        if (streakType.HasValue)
            query = query.Where(s => s.StreakType == streakType);

        return await query
            .OrderBy(s => s.StreakType)
            .ThenBy(s => s.StreakCount)
            .Select(s => new StreakMilestoneDefinitionDto
            {
                Id = s.Id,
                StreakType = s.StreakType,
                StreakCount = s.StreakCount,
                Name = s.Name,
                Description = s.Description,
                RewardPoints = s.RewardPoints,
                RewardBadgeId = s.RewardBadgeId,
                FreezeTokensAwarded = s.FreezeTokensAwarded,
                IconUrl = s.IconUrl
            })
            .ToListAsync();
    }

    public async Task<StreakMilestoneDefinitionDto> CreateStreakMilestoneDefinitionAsync(StreakMilestoneDefinitionDto dto)
    {
        var milestone = new StreakMilestoneDefinition
        {
            StreakType = dto.StreakType,
            StreakCount = dto.StreakCount,
            Name = dto.Name,
            Description = dto.Description,
            RewardPoints = dto.RewardPoints,
            RewardBadgeId = dto.RewardBadgeId,
            FreezeTokensAwarded = dto.FreezeTokensAwarded,
            IconUrl = dto.IconUrl,
            IsActive = true
        };

        _context.StreakMilestoneDefinitions.Add(milestone);
        await _context.SaveChangesAsync();

        dto.Id = milestone.Id;
        return dto;
    }

    public async Task<StreakMilestoneDefinitionDto> UpdateStreakMilestoneDefinitionAsync(StreakMilestoneDefinitionDto dto)
    {
        var milestone = await _context.StreakMilestoneDefinitions.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"Streak milestone {dto.Id} not found");

        milestone.StreakType = dto.StreakType;
        milestone.StreakCount = dto.StreakCount;
        milestone.Name = dto.Name;
        milestone.Description = dto.Description;
        milestone.RewardPoints = dto.RewardPoints;
        milestone.RewardBadgeId = dto.RewardBadgeId;
        milestone.FreezeTokensAwarded = dto.FreezeTokensAwarded;
        milestone.IconUrl = dto.IconUrl;
        milestone.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return dto;
    }

    #endregion

    #region Member Streaks

    public async Task<List<MemberStreakDto>> GetMemberStreaksAsync(int memberId)
    {
        return await _context.MemberStreaks
            .Include(ms => ms.Milestones)
                .ThenInclude(m => m.MilestoneDefinition)
            .Where(ms => ms.MemberId == memberId && ms.IsActive)
            .Select(ms => MapToMemberStreakDto(ms))
            .ToListAsync();
    }

    public async Task<StreakSummary> GetStreakSummaryAsync(int memberId)
    {
        var streaks = await _context.MemberStreaks
            .Include(ms => ms.Milestones)
            .Where(ms => ms.MemberId == memberId && ms.IsActive)
            .ToListAsync();

        var summary = new StreakSummary
        {
            MemberId = memberId,
            ActiveStreaks = streaks.Where(s => s.CurrentStreak > 0).Select(s => MapToMemberStreakDto(s)).ToList(),
            AtRiskStreaks = streaks.Where(s => s.IsAtRisk).Select(s => MapToMemberStreakDto(s)).ToList(),
            TotalFreezeTokens = streaks.Sum(s => s.FreezeTokensRemaining),
            TotalMilestonesAchieved = streaks.Sum(s => s.Milestones.Count),
            TotalPointsFromStreaks = streaks.SelectMany(s => s.Milestones).Sum(m => m.PointsAwarded)
        };

        foreach (var streak in streaks)
        {
            summary.LongestStreaks[streak.StreakType] = streak.LongestStreak;
        }

        return summary;
    }

    public async Task<MemberStreakDto?> GetMemberStreakAsync(int memberId, StreakType streakType, int? storeId = null)
    {
        var streak = await _context.MemberStreaks
            .Include(ms => ms.Milestones)
                .ThenInclude(m => m.MilestoneDefinition)
            .FirstOrDefaultAsync(ms => ms.MemberId == memberId && ms.StreakType == streakType && ms.IsActive
                && (!storeId.HasValue || ms.StoreId == null || ms.StoreId == storeId));

        return streak != null ? MapToMemberStreakDto(streak) : null;
    }

    public async Task<StreakCheckResult> UpdateStreaksAsync(int memberId, DateTime activityTime, int? storeId = null)
    {
        var result = new StreakCheckResult { MemberId = memberId };

        // Get or create streaks for all streak types
        foreach (var streakType in Enum.GetValues<StreakType>())
        {
            var streakResult = await UpdateSingleStreakAsync(memberId, streakType, activityTime, storeId);
            result.StreakUpdates.Add(streakResult);

            if (streakResult.MilestoneAchieved != null)
            {
                result.TotalPointsAwarded += streakResult.PointsAwarded;
                result.TotalFreezeTokensAwarded += streakResult.FreezeTokensAwarded;
            }
        }

        return result;
    }

    private async Task<StreakUpdateResult> UpdateSingleStreakAsync(int memberId, StreakType streakType, DateTime activityTime, int? storeId)
    {
        var result = new StreakUpdateResult();

        // Check if activity qualifies for this streak type
        if (!ActivityQualifiesForStreak(streakType, activityTime))
        {
            result.Message = "Activity does not qualify for this streak type";
            return result;
        }

        // Get or create streak
        var streak = await _context.MemberStreaks
            .Include(ms => ms.Milestones)
            .FirstOrDefaultAsync(ms => ms.MemberId == memberId && ms.StreakType == streakType && ms.IsActive);

        if (streak == null)
        {
            // Create new streak
            streak = new MemberStreak
            {
                MemberId = memberId,
                StreakType = streakType,
                CurrentStreak = 1,
                LongestStreak = 1,
                StreakStartedAt = activityTime,
                LastActivityAt = activityTime,
                StoreId = storeId,
                FreezeTokensRemaining = 3, // Default
                IsActive = true
            };

            SetNextDeadline(streak);
            _context.MemberStreaks.Add(streak);

            result.IsNewStreak = true;
            result.NewStreak = 1;
        }
        else
        {
            result.PreviousStreak = streak.CurrentStreak;

            // Check if streak was broken or should be extended
            if (streak.IsFrozen && streak.FreezeExpiresAt > activityTime)
            {
                // Unfreeze and continue
                streak.IsFrozen = false;
                streak.FreezeExpiresAt = null;
            }
            else if (streak.NextActivityDeadline.HasValue && activityTime > streak.NextActivityDeadline)
            {
                // Streak broken, reset
                streak.TimesReset++;
                streak.CurrentStreak = 1;
                streak.StreakStartedAt = activityTime;
                result.StreakExtended = false;
            }
            else if (IsNewStreakPeriod(streak, activityTime))
            {
                // Extend streak
                streak.CurrentStreak++;
                result.StreakExtended = true;

                if (streak.CurrentStreak > streak.LongestStreak)
                    streak.LongestStreak = streak.CurrentStreak;
            }
            else
            {
                // Same period, no change
                result.Message = "Activity already counted for current period";
            }

            result.NewStreak = streak.CurrentStreak;
            streak.LastActivityAt = activityTime;
            streak.IsAtRisk = false;
            SetNextDeadline(streak);
        }

        // Check for milestone
        var milestone = await CheckStreakMilestoneAsync(streak);
        if (milestone != null)
        {
            result.MilestoneAchieved = milestone;
            result.PointsAwarded = milestone.PointsAwarded;
            result.FreezeTokensAwarded = milestone.FreezeTokensAwarded;
            streak.FreezeTokensRemaining += milestone.FreezeTokensAwarded;
        }

        await _context.SaveChangesAsync();

        result.Success = true;
        result.Streak = MapToMemberStreakDto(streak);
        return result;
    }

    private bool ActivityQualifiesForStreak(StreakType streakType, DateTime activityTime)
    {
        var hour = activityTime.Hour;

        return streakType switch
        {
            StreakType.MorningVisit => hour >= 6 && hour < 11,
            StreakType.LunchVisit => hour >= 11 && hour < 14,
            StreakType.DinnerVisit => hour >= 17 && hour < 22,
            StreakType.WeekendVisit => activityTime.DayOfWeek == DayOfWeek.Saturday || activityTime.DayOfWeek == DayOfWeek.Sunday,
            _ => true
        };
    }

    private bool IsNewStreakPeriod(MemberStreak streak, DateTime activityTime)
    {
        return streak.StreakType switch
        {
            StreakType.DailyVisit => activityTime.Date > streak.LastActivityAt.Date,
            StreakType.WeeklyVisit => GetWeekNumber(activityTime) > GetWeekNumber(streak.LastActivityAt),
            StreakType.WeekendVisit => activityTime.Date > streak.LastActivityAt.Date,
            StreakType.MorningVisit => activityTime.Date > streak.LastActivityAt.Date,
            StreakType.LunchVisit => activityTime.Date > streak.LastActivityAt.Date,
            StreakType.DinnerVisit => activityTime.Date > streak.LastActivityAt.Date,
            _ => activityTime > streak.LastActivityAt
        };
    }

    private void SetNextDeadline(MemberStreak streak)
    {
        var now = streak.LastActivityAt;

        streak.NextActivityDeadline = streak.StreakType switch
        {
            StreakType.DailyVisit => now.Date.AddDays(2),
            StreakType.WeeklyVisit => GetStartOfWeek(now).AddDays(14),
            StreakType.WeekendVisit => GetNextWeekend(now).AddDays(2),
            StreakType.MorningVisit => now.Date.AddDays(2),
            StreakType.LunchVisit => now.Date.AddDays(2),
            StreakType.DinnerVisit => now.Date.AddDays(2),
            _ => now.AddDays(7)
        };
    }

    private int GetWeekNumber(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private DateTime GetNextWeekend(DateTime date)
    {
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)date.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0) daysUntilSaturday = 7;
        return date.AddDays(daysUntilSaturday).Date;
    }

    private async Task<StreakMilestoneDto?> CheckStreakMilestoneAsync(MemberStreak streak)
    {
        // Get milestones for this streak type at current count
        var milestoneDefinition = await _context.StreakMilestoneDefinitions
            .FirstOrDefaultAsync(sm => sm.IsActive && sm.StreakType == streak.StreakType && sm.StreakCount == streak.CurrentStreak);

        if (milestoneDefinition == null) return null;

        // Check if already achieved
        var alreadyAchieved = streak.Milestones.Any(m => m.MilestoneDefinitionId == milestoneDefinition.Id);
        if (alreadyAchieved) return null;

        // Award milestone
        var milestone = new StreakMilestone
        {
            MemberId = streak.MemberId,
            MemberStreakId = streak.Id,
            MilestoneDefinitionId = milestoneDefinition.Id,
            AchievedAtStreak = streak.CurrentStreak,
            AchievedAt = DateTime.UtcNow,
            PointsAwarded = milestoneDefinition.RewardPoints,
            FreezeTokensAwarded = milestoneDefinition.FreezeTokensAwarded,
            IsActive = true
        };

        _context.StreakMilestones.Add(milestone);

        // Award badge if configured
        if (milestoneDefinition.RewardBadgeId.HasValue)
        {
            await AwardBadgeAsync(new AwardBadgeRequest
            {
                MemberId = streak.MemberId,
                BadgeId = milestoneDefinition.RewardBadgeId.Value,
                Notes = $"Achieved {streak.CurrentStreak}-{streak.StreakType} streak"
            });
        }

        _logger.LogInformation("Member {MemberId} achieved streak milestone: {StreakCount} {StreakType}",
            streak.MemberId, streak.CurrentStreak, streak.StreakType);

        return new StreakMilestoneDto
        {
            Id = milestone.Id,
            MilestoneDefinitionId = milestoneDefinition.Id,
            Milestone = new StreakMilestoneDefinitionDto
            {
                Id = milestoneDefinition.Id,
                StreakType = milestoneDefinition.StreakType,
                StreakCount = milestoneDefinition.StreakCount,
                Name = milestoneDefinition.Name,
                Description = milestoneDefinition.Description,
                RewardPoints = milestoneDefinition.RewardPoints,
                FreezeTokensAwarded = milestoneDefinition.FreezeTokensAwarded
            },
            AchievedAtStreak = milestone.AchievedAtStreak,
            AchievedAt = milestone.AchievedAt,
            PointsAwarded = milestone.PointsAwarded,
            FreezeTokensAwarded = milestone.FreezeTokensAwarded
        };
    }

    public async Task<StreakFreezeResult> FreezeStreakAsync(int memberId, StreakType streakType, int? storeId = null)
    {
        var streak = await _context.MemberStreaks
            .FirstOrDefaultAsync(ms => ms.MemberId == memberId && ms.StreakType == streakType && ms.IsActive);

        if (streak == null)
        {
            return new StreakFreezeResult { Success = false, Error = "Streak not found" };
        }

        if (streak.FreezeTokensRemaining <= 0)
        {
            return new StreakFreezeResult { Success = false, Error = "No freeze tokens available" };
        }

        if (streak.IsFrozen)
        {
            return new StreakFreezeResult { Success = false, Error = "Streak is already frozen" };
        }

        streak.IsFrozen = true;
        streak.FreezeExpiresAt = DateTime.UtcNow.AddDays(1); // 24 hour freeze
        streak.FreezeTokensRemaining--;
        streak.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Member {MemberId} froze {StreakType} streak", memberId, streakType);

        return new StreakFreezeResult
        {
            Success = true,
            Streak = MapToMemberStreakDto(streak),
            TokensUsed = 1,
            TokensRemaining = streak.FreezeTokensRemaining,
            FreezeExpiresAt = streak.FreezeExpiresAt,
            Message = "Streak frozen for 24 hours"
        };
    }

    public async Task<bool> UnfreezeStreakAsync(int memberStreakId)
    {
        var streak = await _context.MemberStreaks.FindAsync(memberStreakId);
        if (streak == null || !streak.IsFrozen) return false;

        streak.IsFrozen = false;
        streak.FreezeExpiresAt = null;
        streak.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MemberStreakDto>> GetAtRiskStreaksAsync(int memberId)
    {
        return await _context.MemberStreaks
            .Where(ms => ms.MemberId == memberId && ms.IsActive && ms.IsAtRisk)
            .Select(ms => MapToMemberStreakDto(ms))
            .ToListAsync();
    }

    public async Task<StreakCheckJobSummary> ProcessBrokenStreaksAsync()
    {
        var summary = new StreakCheckJobSummary();

        try
        {
            var now = DateTime.UtcNow;
            var config = await GetConfigurationAsync();

            // Get all active streaks
            var streaks = await _context.MemberStreaks
                .Where(ms => ms.IsActive && ms.CurrentStreak > 0)
                .ToListAsync();

            summary.TotalMembersChecked = streaks.Select(s => s.MemberId).Distinct().Count();

            foreach (var streak in streaks)
            {
                try
                {
                    // Check if frozen
                    if (streak.IsFrozen)
                    {
                        if (streak.FreezeExpiresAt.HasValue && streak.FreezeExpiresAt < now)
                        {
                            streak.IsFrozen = false;
                            streak.FreezeExpiresAt = null;
                        }
                        else
                        {
                            continue; // Still frozen, skip
                        }
                    }

                    // Check if broken
                    if (streak.NextActivityDeadline.HasValue && now > streak.NextActivityDeadline)
                    {
                        streak.CurrentStreak = 0;
                        streak.TimesReset++;
                        streak.IsAtRisk = false;
                        summary.BrokenStreaks++;
                    }
                    // Check if at risk
                    else if (streak.NextActivityDeadline.HasValue)
                    {
                        var hoursUntilDeadline = (streak.NextActivityDeadline.Value - now).TotalHours;
                        if (hoursUntilDeadline <= config.StreakAtRiskHours && !streak.IsAtRisk)
                        {
                            streak.IsAtRisk = true;
                            summary.AtRiskWarningsSent++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    summary.ErrorCount++;
                    summary.Errors.Add($"Error processing streak {streak.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            summary.ErrorCount++;
            summary.Errors.Add($"Fatal error: {ex.Message}");
            _logger.LogError(ex, "Error in ProcessBrokenStreaksAsync");
        }

        summary.CompletedAt = DateTime.UtcNow;
        return summary;
    }

    #endregion

    #region Configuration

    public async Task<GamificationConfigurationDto> GetConfigurationAsync(int? storeId = null)
    {
        var config = await _context.GamificationConfigurations
            .FirstOrDefaultAsync(c => c.IsActive && (c.StoreId == storeId || (storeId.HasValue && c.StoreId == null)));

        if (config == null)
        {
            // Return default config
            return new GamificationConfigurationDto
            {
                IsEnabled = true,
                BadgesEnabled = true,
                ChallengesEnabled = true,
                StreaksEnabled = true,
                DefaultFreezeTokens = 3,
                StreakAtRiskHours = 12,
                ShowBadgesOnReceipt = true,
                MaxBadgesOnReceipt = 3,
                NotifyOnBadgeEarned = true,
                NotifyOnChallengeProgress = true,
                ProgressNotificationThresholds = "50,75,90",
                NotifyOnStreakAtRisk = true,
                AutoEnrollInChallenges = true
            };
        }

        return new GamificationConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            IsEnabled = config.IsEnabled,
            BadgesEnabled = config.BadgesEnabled,
            ChallengesEnabled = config.ChallengesEnabled,
            StreaksEnabled = config.StreaksEnabled,
            DefaultFreezeTokens = config.DefaultFreezeTokens,
            StreakAtRiskHours = config.StreakAtRiskHours,
            ShowBadgesOnReceipt = config.ShowBadgesOnReceipt,
            MaxBadgesOnReceipt = config.MaxBadgesOnReceipt,
            NotifyOnBadgeEarned = config.NotifyOnBadgeEarned,
            NotifyOnChallengeProgress = config.NotifyOnChallengeProgress,
            ProgressNotificationThresholds = config.ProgressNotificationThresholds,
            NotifyOnStreakAtRisk = config.NotifyOnStreakAtRisk,
            AutoEnrollInChallenges = config.AutoEnrollInChallenges
        };
    }

    public async Task<GamificationConfigurationDto> UpdateConfigurationAsync(GamificationConfigurationDto dto)
    {
        var config = await _context.GamificationConfigurations.FindAsync(dto.Id);

        if (config == null)
        {
            config = new GamificationConfiguration { IsActive = true };
            _context.GamificationConfigurations.Add(config);
        }

        config.StoreId = dto.StoreId;
        config.IsEnabled = dto.IsEnabled;
        config.BadgesEnabled = dto.BadgesEnabled;
        config.ChallengesEnabled = dto.ChallengesEnabled;
        config.StreaksEnabled = dto.StreaksEnabled;
        config.DefaultFreezeTokens = dto.DefaultFreezeTokens;
        config.StreakAtRiskHours = dto.StreakAtRiskHours;
        config.ShowBadgesOnReceipt = dto.ShowBadgesOnReceipt;
        config.MaxBadgesOnReceipt = dto.MaxBadgesOnReceipt;
        config.NotifyOnBadgeEarned = dto.NotifyOnBadgeEarned;
        config.NotifyOnChallengeProgress = dto.NotifyOnChallengeProgress;
        config.ProgressNotificationThresholds = dto.ProgressNotificationThresholds;
        config.NotifyOnStreakAtRisk = dto.NotifyOnStreakAtRisk;
        config.AutoEnrollInChallenges = dto.AutoEnrollInChallenges;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        dto.Id = config.Id;
        return dto;
    }

    #endregion

    #region Profile & Transaction Processing

    public async Task<MemberGamificationProfile> GetMemberGamificationProfileAsync(int memberId)
    {
        var member = await _context.LoyaltyMembers.FindAsync(memberId);
        if (member == null) throw new InvalidOperationException($"Member {memberId} not found");

        var profile = new MemberGamificationProfile
        {
            MemberId = memberId,
            MemberName = $"{member.FirstName} {member.LastName}".Trim(),
            Tier = member.Tier,
            BadgeCollection = await GetBadgeCollectionSummaryAsync(memberId),
            ActiveChallenges = await GetMemberActiveChallengesAsync(memberId),
            CompletedChallenges = await GetMemberCompletedChallengesAsync(memberId),
            StreakSummary = await GetStreakSummaryAsync(memberId)
        };

        profile.TotalChallengesCompleted = profile.CompletedChallenges.Count;
        profile.TotalGamificationPoints = profile.BadgeCollection.TotalPointsFromBadges +
            profile.CompletedChallenges.Sum(c => c.PointsAwarded) +
            profile.StreakSummary.TotalPointsFromStreaks;

        // Calculate gamification level (simple formula: 1 level per 1000 XP)
        profile.GamificationXP = profile.TotalGamificationPoints;
        profile.GamificationLevel = Math.Max(1, profile.GamificationXP / 1000);
        profile.XPToNextLevel = ((profile.GamificationLevel + 1) * 1000) - profile.GamificationXP;

        return profile;
    }

    public async Task<TransactionGamificationResult> ProcessTransactionAsync(int memberId, int orderId, decimal spendAmount, int? storeId)
    {
        var result = new TransactionGamificationResult
        {
            MemberId = memberId,
            OrderId = orderId
        };

        var config = await GetConfigurationAsync(storeId);
        if (!config.IsEnabled) return result;

        // Process badges
        if (config.BadgesEnabled)
        {
            result.BadgeResults = await CheckAndAwardBadgesAsync(memberId, orderId, storeId);
            result.TotalBadgesEarned = result.BadgeResults.BadgesEarned;
            result.TotalPointsAwarded += result.BadgeResults.TotalPointsAwarded;

            foreach (var badge in result.BadgeResults.AwardedBadges.Where(b => b.Success))
            {
                result.Notifications.Add($"Badge earned: {badge.Badge?.Badge.Name}");
            }
        }

        // Process challenges
        if (config.ChallengesEnabled)
        {
            // Auto-enroll if configured
            if (config.AutoEnrollInChallenges)
            {
                await AutoEnrollMemberInChallengesAsync(memberId, storeId);
            }

            result.ChallengeResults = await UpdateChallengeProgressAsync(memberId, orderId, spendAmount, storeId);
            result.TotalChallengesCompleted = result.ChallengeResults.CompletedChallenges.Count;
            result.TotalPointsAwarded += result.ChallengeResults.TotalPointsAwarded;

            foreach (var completed in result.ChallengeResults.CompletedChallenges)
            {
                result.Notifications.Add($"Challenge completed: {completed.ChallengeName}");
            }
        }

        // Process streaks
        if (config.StreaksEnabled)
        {
            result.StreakResults = await UpdateStreaksAsync(memberId, DateTime.UtcNow, storeId);
            result.TotalStreakMilestones = result.StreakResults.StreakUpdates.Count(s => s.MilestoneAchieved != null);
            result.TotalPointsAwarded += result.StreakResults.TotalPointsAwarded;

            foreach (var streak in result.StreakResults.StreakUpdates.Where(s => s.MilestoneAchieved != null))
            {
                result.Notifications.Add($"Streak milestone: {streak.MilestoneAchieved!.Milestone.Name}");
            }
        }

        _logger.LogInformation("Processed transaction gamification for member {MemberId}: {BadgesEarned} badges, {ChallengesCompleted} challenges, {Points} points",
            memberId, result.TotalBadgesEarned, result.TotalChallengesCompleted, result.TotalPointsAwarded);

        return result;
    }

    #endregion

    #region Challenge Expiry

    public async Task<ChallengeExpiryJobSummary> ProcessExpiredChallengesAsync()
    {
        var summary = new ChallengeExpiryJobSummary();

        try
        {
            var now = DateTime.UtcNow;

            // Get expired challenges that are still marked as active
            var expiredMemberChallenges = await _context.MemberChallenges
                .Include(mc => mc.Challenge)
                .Where(mc => mc.IsActive && mc.Status == ChallengeStatus.Active)
                .Where(mc => mc.Challenge.EndDate < now)
                .ToListAsync();

            foreach (var mc in expiredMemberChallenges)
            {
                try
                {
                    if (mc.CurrentProgress >= mc.Challenge.TargetValue)
                    {
                        // Should have been completed - mark as completed
                        mc.Status = ChallengeStatus.Completed;
                        mc.CompletedAt = mc.Challenge.EndDate;
                    }
                    else
                    {
                        // Failed to complete in time
                        mc.Status = ChallengeStatus.Failed;
                        summary.FailedChallengesCount++;
                    }

                    summary.ExpiredChallengesCount++;
                }
                catch (Exception ex)
                {
                    summary.ErrorCount++;
                    summary.Errors.Add($"Error processing member challenge {mc.Id}: {ex.Message}");
                }
            }

            // Create recurring challenge instances
            var recurringChallenges = await _context.Challenges
                .Where(c => c.IsActive && c.IsRecurring && c.EndDate < now)
                .ToListAsync();

            foreach (var challenge in recurringChallenges)
            {
                try
                {
                    var newChallenge = new Challenge
                    {
                        Name = challenge.Name,
                        Description = challenge.Description,
                        Period = challenge.Period,
                        GoalType = challenge.GoalType,
                        TargetValue = challenge.TargetValue,
                        RewardPoints = challenge.RewardPoints,
                        RewardBadgeId = challenge.RewardBadgeId,
                        BonusMultiplier = challenge.BonusMultiplier,
                        IconUrl = challenge.IconUrl,
                        Color = challenge.Color,
                        IsRecurring = true,
                        ProductId = challenge.ProductId,
                        CategoryId = challenge.CategoryId,
                        MinimumTier = challenge.MinimumTier,
                        MaxParticipants = challenge.MaxParticipants,
                        ShowLeaderboard = challenge.ShowLeaderboard,
                        StoreId = challenge.StoreId,
                        DisplayOrder = challenge.DisplayOrder,
                        IsActive = true
                    };

                    // Calculate new dates based on period
                    var periodDays = challenge.Period switch
                    {
                        ChallengePeriod.Daily => 1,
                        ChallengePeriod.Weekly => 7,
                        ChallengePeriod.Monthly => 30,
                        ChallengePeriod.Quarterly => 90,
                        _ => (challenge.EndDate - challenge.StartDate).Days
                    };

                    newChallenge.StartDate = challenge.EndDate;
                    newChallenge.EndDate = challenge.EndDate.AddDays(periodDays);

                    // Deactivate old challenge
                    challenge.IsActive = false;
                    challenge.UpdatedAt = now;

                    _context.Challenges.Add(newChallenge);
                    summary.RecurringChallengesCreated++;
                }
                catch (Exception ex)
                {
                    summary.ErrorCount++;
                    summary.Errors.Add($"Error creating recurring challenge from {challenge.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            summary.ErrorCount++;
            summary.Errors.Add($"Fatal error: {ex.Message}");
            _logger.LogError(ex, "Error in ProcessExpiredChallengesAsync");
        }

        summary.CompletedAt = DateTime.UtcNow;
        return summary;
    }

    #endregion

    #region Mapping Helpers

    private static BadgeDto MapToBadgeDto(Badge badge)
    {
        return new BadgeDto
        {
            Id = badge.Id,
            Name = badge.Name,
            Description = badge.Description,
            Category = badge.Category,
            TriggerType = badge.TriggerType,
            Rarity = badge.Rarity,
            IconUrl = badge.IconUrl,
            Color = badge.Color,
            PointsAwarded = badge.PointsAwarded,
            IsSecret = badge.IsSecret,
            IsRepeatable = badge.IsRepeatable,
            MaxEarnings = badge.MaxEarnings,
            ThresholdValue = badge.ThresholdValue,
            StartDate = badge.StartDate,
            EndDate = badge.EndDate,
            DisplayOrder = badge.DisplayOrder,
            StoreId = badge.StoreId
        };
    }

    private static MemberBadgeDto MapToMemberBadgeDto(MemberBadge mb)
    {
        return new MemberBadgeDto
        {
            Id = mb.Id,
            MemberId = mb.MemberId,
            BadgeId = mb.BadgeId,
            Badge = mb.Badge != null ? MapToBadgeDto(mb.Badge) : null!,
            EarnedAt = mb.EarnedAt,
            TimesEarned = mb.TimesEarned,
            PointsAwarded = mb.PointsAwarded,
            TriggeredByOrderId = mb.TriggeredByOrderId,
            StoreId = mb.StoreId,
            StoreName = mb.Store?.Name,
            IsViewed = mb.IsViewed,
            IsPinned = mb.IsPinned,
            Notes = mb.Notes
        };
    }

    private static ChallengeDto MapToChallengeDto(Challenge challenge)
    {
        return new ChallengeDto
        {
            Id = challenge.Id,
            Name = challenge.Name,
            Description = challenge.Description,
            Period = challenge.Period,
            GoalType = challenge.GoalType,
            TargetValue = challenge.TargetValue,
            RewardPoints = challenge.RewardPoints,
            RewardBadgeId = challenge.RewardBadgeId,
            RewardBadge = challenge.RewardBadge != null ? MapToBadgeDto(challenge.RewardBadge) : null,
            BonusMultiplier = challenge.BonusMultiplier,
            IconUrl = challenge.IconUrl,
            Color = challenge.Color,
            StartDate = challenge.StartDate,
            EndDate = challenge.EndDate,
            IsRecurring = challenge.IsRecurring,
            ProductId = challenge.ProductId,
            CategoryId = challenge.CategoryId,
            MinimumTier = challenge.MinimumTier,
            MaxParticipants = challenge.MaxParticipants,
            ShowLeaderboard = challenge.ShowLeaderboard,
            StoreId = challenge.StoreId,
            IsEnabled = challenge.IsActive
        };
    }

    private static MemberChallengeDto MapToMemberChallengeDto(MemberChallenge mc)
    {
        return new MemberChallengeDto
        {
            Id = mc.Id,
            MemberId = mc.MemberId,
            ChallengeId = mc.ChallengeId,
            Challenge = mc.Challenge != null ? MapToChallengeDto(mc.Challenge) : null!,
            CurrentProgress = mc.CurrentProgress,
            Status = mc.Status,
            JoinedAt = mc.JoinedAt,
            CompletedAt = mc.CompletedAt,
            PointsAwarded = mc.PointsAwarded,
            AwardedBadgeId = mc.AwardedBadgeId,
            LastProgressAt = mc.LastProgressAt
        };
    }

    private static MemberStreakDto MapToMemberStreakDto(MemberStreak streak)
    {
        return new MemberStreakDto
        {
            Id = streak.Id,
            MemberId = streak.MemberId,
            StreakType = streak.StreakType,
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            StreakStartedAt = streak.StreakStartedAt,
            LastActivityAt = streak.LastActivityAt,
            NextActivityDeadline = streak.NextActivityDeadline,
            IsAtRisk = streak.IsAtRisk,
            StoreId = streak.StoreId,
            IsFrozen = streak.IsFrozen,
            FreezeExpiresAt = streak.FreezeExpiresAt,
            FreezeTokensRemaining = streak.FreezeTokensRemaining,
            AchievedMilestones = streak.Milestones?.Select(m => new StreakMilestoneDto
            {
                Id = m.Id,
                MilestoneDefinitionId = m.MilestoneDefinitionId,
                AchievedAtStreak = m.AchievedAtStreak,
                AchievedAt = m.AchievedAt,
                PointsAwarded = m.PointsAwarded,
                FreezeTokensAwarded = m.FreezeTokensAwarded
            }).ToList() ?? new List<StreakMilestoneDto>()
        };
    }

    #endregion
}
