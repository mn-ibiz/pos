using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of Hotel PMS integration service.
/// </summary>
public class HotelPMSService : IHotelPMSService
{
    private readonly POSDbContext _context;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(30);

    public HotelPMSService(POSDbContext context, IHttpClientFactory? httpClientFactory = null)
    {
        _context = context;
        _httpClient = httpClientFactory?.CreateClient("PMS") ?? new HttpClient { Timeout = DefaultHttpTimeout };
    }

    #region Connection Configuration

    public async Task<PMSConfiguration> CreateConfigurationAsync(PMSConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config.IsDefault)
        {
            // Clear other defaults
            var existingDefaults = await _context.PMSConfigurations
                .Where(c => c.IsDefault && c.IsActive)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        _context.PMSConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    public async Task<PMSConfiguration?> GetConfigurationByIdAsync(int configId, CancellationToken cancellationToken = default)
    {
        return await _context.PMSConfigurations
            .Include(c => c.RevenueCenters)
            .FirstOrDefaultAsync(c => c.Id == configId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PMSConfiguration?> GetDefaultConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PMSConfigurations
            .Include(c => c.RevenueCenters)
            .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<PMSConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PMSConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PMSConfiguration> UpdateConfigurationAsync(PMSConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config.IsDefault)
        {
            var existingDefaults = await _context.PMSConfigurations
                .Where(c => c.IsDefault && c.Id != config.Id && c.IsActive)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        _context.PMSConfigurations.Update(config);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    public async Task DeleteConfigurationAsync(int configId, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationByIdAsync(configId, cancellationToken).ConfigureAwait(false);
        if (config != null)
        {
            config.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(int configId, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationByIdAsync(configId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PMS configuration {configId} not found.");

        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResult();

        try
        {
            // For demo, simulate connection test based on PMS type
            var testEndpoint = GetTestEndpoint(config);

            using var request = new HttpRequestMessage(HttpMethod.Get, testEndpoint);
            AddAuthHeaders(request, config);

            // In production, this would actually call the PMS API
            // For now, simulate success
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            result.IsSuccess = true;
            result.Message = "Connection successful";
            result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            result.PMSVersion = GetPMSVersion(config.PMSType);

            config.Status = PMSConnectionStatus.Connected;
            config.LastConnectedAt = DateTime.UtcNow;
            config.LastErrorMessage = null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.Message = ex.Message;
            result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            config.Status = PMSConnectionStatus.Disconnected;
            config.LastErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task RefreshTokensAsync(int configId, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationByIdAsync(configId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PMS configuration {configId} not found.");

        if (string.IsNullOrEmpty(config.TokenEndpoint) || string.IsNullOrEmpty(config.RefreshToken))
        {
            return;
        }

        // In production, this would refresh the OAuth token
        config.TokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Revenue Center Mapping

    public async Task<PMSRevenueCenter> CreateRevenueCenterAsync(PMSRevenueCenter revenueCenter, CancellationToken cancellationToken = default)
    {
        _context.PMSRevenueCenters.Add(revenueCenter);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return revenueCenter;
    }

    public async Task<IEnumerable<PMSRevenueCenter>> GetRevenueCentersAsync(int configId, CancellationToken cancellationToken = default)
    {
        return await _context.PMSRevenueCenters
            .Where(r => r.PMSConfigurationId == configId && r.IsActive)
            .OrderBy(r => r.RevenueCenterName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PMSRevenueCenter> UpdateRevenueCenterAsync(PMSRevenueCenter revenueCenter, CancellationToken cancellationToken = default)
    {
        _context.PMSRevenueCenters.Update(revenueCenter);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return revenueCenter;
    }

    public async Task DeleteRevenueCenterAsync(int revenueCenterId, CancellationToken cancellationToken = default)
    {
        var revenueCenter = await _context.PMSRevenueCenters
            .FirstOrDefaultAsync(r => r.Id == revenueCenterId && r.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (revenueCenter != null)
        {
            revenueCenter.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Room Charge Posting

    public async Task<RoomChargeResult> PostRoomChargeAsync(RoomChargeRequest request, CancellationToken cancellationToken = default)
    {
        var config = request.ConfigId.HasValue
            ? await GetConfigurationByIdAsync(request.ConfigId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null)
        {
            return new RoomChargeResult
            {
                IsSuccess = false,
                Status = PostingStatus.Failed,
                ErrorMessage = "No PMS configuration found"
            };
        }

        // Validate guest and room charges allowed
        var validation = await ValidateRoomChargeAsync(request.RoomNumber, request.Amount + request.TaxAmount + request.ServiceCharge, config.Id, cancellationToken).ConfigureAwait(false);
        if (!validation.IsAllowed)
        {
            return new RoomChargeResult
            {
                IsSuccess = false,
                Status = PostingStatus.Failed,
                ErrorMessage = validation.DenialReason
            };
        }

        var postingRef = $"POS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var posting = new RoomChargePosting
        {
            PMSConfigurationId = config.Id,
            PostingReference = postingRef,
            RoomNumber = request.RoomNumber,
            GuestName = request.GuestName,
            FolioNumber = request.FolioNumber,
            ChargeType = request.ChargeType,
            Amount = request.Amount,
            TaxAmount = request.TaxAmount,
            ServiceCharge = request.ServiceCharge,
            TotalAmount = request.Amount + request.TaxAmount + request.ServiceCharge,
            Description = request.Description,
            ReceiptId = request.ReceiptId,
            OrderId = request.OrderId,
            ProcessedByUserId = request.UserId,
            SignatureData = request.SignatureData,
            Status = PostingStatus.Processing,
            AttemptCount = 1,
            LastAttemptAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.RoomChargePostings.Add(posting);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Attempt to post to PMS
            var pmsResult = await PostToPMSAsync(posting, config, cancellationToken).ConfigureAwait(false);

            if (pmsResult.IsSuccess)
            {
                posting.Status = PostingStatus.Posted;
                posting.PostedAt = DateTime.UtcNow;
                posting.PMSTransactionId = pmsResult.PMSTransactionId;

                await LogActivityAsync(new PMSActivityLog
                {
                    PMSConfigurationId = config.Id,
                    ActivityType = "RoomChargePosted",
                    Description = $"Room charge posted: {posting.PostingReference} to room {posting.RoomNumber}",
                    RoomChargePostingId = posting.Id,
                    IsSuccess = true,
                    UserId = request.UserId,
                    IsActive = true
                }, cancellationToken).ConfigureAwait(false);

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return new RoomChargeResult
                {
                    IsSuccess = true,
                    PostingId = posting.Id,
                    PMSTransactionId = pmsResult.PMSTransactionId,
                    Status = PostingStatus.Posted
                };
            }
            else
            {
                posting.Status = pmsResult.IsRetryable ? PostingStatus.Retry : PostingStatus.Failed;
                posting.ErrorMessage = pmsResult.ErrorMessage;

                if (pmsResult.IsRetryable && config.AutoPostEnabled)
                {
                    await AddToQueueAsync(posting.Id, 5, DateTime.UtcNow.AddSeconds(config.RetryDelaySeconds), cancellationToken).ConfigureAwait(false);
                }

                await LogActivityAsync(new PMSActivityLog
                {
                    PMSConfigurationId = config.Id,
                    ActivityType = "RoomChargePostingFailed",
                    Description = $"Room charge posting failed: {posting.PostingReference}",
                    RoomChargePostingId = posting.Id,
                    IsSuccess = false,
                    ErrorMessage = pmsResult.ErrorMessage,
                    UserId = request.UserId,
                    IsActive = true
                }, cancellationToken).ConfigureAwait(false);

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return new RoomChargeResult
                {
                    IsSuccess = false,
                    PostingId = posting.Id,
                    Status = posting.Status,
                    ErrorCode = pmsResult.ErrorCode,
                    ErrorMessage = pmsResult.ErrorMessage,
                    IsRetryable = pmsResult.IsRetryable
                };
            }
        }
        catch (Exception ex)
        {
            posting.Status = PostingStatus.Failed;
            posting.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RoomChargeResult
            {
                IsSuccess = false,
                PostingId = posting.Id,
                Status = PostingStatus.Failed,
                ErrorMessage = ex.Message,
                IsRetryable = true
            };
        }
    }

    public async Task<IEnumerable<RoomChargeResult>> PostRoomChargesBatchAsync(IEnumerable<RoomChargeRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<RoomChargeResult>();
        var requestList = requests.ToList();

        foreach (var request in requestList)
        {
            try
            {
                var result = await PostRoomChargeAsync(request, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Propagate cancellation to stop batch processing
                throw;
            }
            catch (Exception ex)
            {
                // Continue processing remaining items but record the failure
                results.Add(new RoomChargeResult
                {
                    IsSuccess = false,
                    Status = PostingStatus.Failed,
                    ErrorCode = "BATCH_ITEM_FAILED",
                    ErrorMessage = $"Failed to post charge for room {request.RoomNumber}: {ex.Message}",
                    IsRetryable = true
                });
            }
        }

        return results;
    }

    public async Task<RoomChargePosting?> GetPostingByIdAsync(int postingId, CancellationToken cancellationToken = default)
    {
        return await _context.RoomChargePostings
            .Include(p => p.PMSConfiguration)
            .FirstOrDefaultAsync(p => p.Id == postingId && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    // Default page size to prevent unbounded result sets
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 1000;

    public async Task<IEnumerable<RoomChargePosting>> GetPostingsByStatusAsync(PostingStatus status, int? configId = null, CancellationToken cancellationToken = default)
    {
        // Use pagination with sensible defaults to prevent memory issues with large datasets
        return await GetPostingsByStatusAsync(status, configId, skip: 0, take: DefaultPageSize, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets postings by status with pagination support.
    /// </summary>
    public async Task<IEnumerable<RoomChargePosting>> GetPostingsByStatusAsync(
        PostingStatus status,
        int? configId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Enforce maximum page size to prevent memory issues
        take = Math.Min(take, MaxPageSize);

        var query = _context.RoomChargePostings.Where(p => p.Status == status && p.IsActive);

        if (configId.HasValue)
            query = query.Where(p => p.PMSConfigurationId == configId.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<RoomChargePosting>> GetPostingsByReceiptAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await _context.RoomChargePostings
            .Where(p => p.ReceiptId == receiptId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RoomChargeResult> RetryPostingAsync(int postingId, CancellationToken cancellationToken = default)
    {
        var posting = await GetPostingByIdAsync(postingId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Posting {postingId} not found.");

        if (posting.Status == PostingStatus.Posted)
        {
            return new RoomChargeResult
            {
                IsSuccess = true,
                PostingId = postingId,
                Status = PostingStatus.Posted,
                PMSTransactionId = posting.PMSTransactionId
            };
        }

        posting.AttemptCount++;
        posting.LastAttemptAt = DateTime.UtcNow;
        posting.Status = PostingStatus.Processing;

        var config = posting.PMSConfiguration;
        var result = await PostToPMSAsync(posting, config, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            posting.Status = PostingStatus.Posted;
            posting.PostedAt = DateTime.UtcNow;
            posting.PMSTransactionId = result.PMSTransactionId;
            posting.ErrorMessage = null;
        }
        else
        {
            posting.Status = result.IsRetryable && posting.AttemptCount < config.MaxRetries
                ? PostingStatus.Retry
                : PostingStatus.Failed;
            posting.ErrorMessage = result.ErrorMessage;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RoomChargeResult
        {
            IsSuccess = result.IsSuccess,
            PostingId = postingId,
            Status = posting.Status,
            PMSTransactionId = result.PMSTransactionId,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            IsRetryable = result.IsRetryable
        };
    }

    public async Task CancelPostingAsync(int postingId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var posting = await GetPostingByIdAsync(postingId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Posting {postingId} not found.");

        if (posting.Status == PostingStatus.Posted)
        {
            throw new InvalidOperationException("Cannot cancel a posted charge. Use VoidPostingAsync instead.");
        }

        posting.Status = PostingStatus.Cancelled;
        posting.ErrorMessage = $"Cancelled by user: {reason}";

        await LogActivityAsync(new PMSActivityLog
        {
            PMSConfigurationId = posting.PMSConfigurationId,
            ActivityType = "RoomChargeCancelled",
            Description = $"Room charge cancelled: {posting.PostingReference}",
            RoomChargePostingId = posting.Id,
            IsSuccess = true,
            UserId = userId,
            IsActive = true
        }, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomChargeResult> VoidPostingAsync(int postingId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var posting = await GetPostingByIdAsync(postingId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Posting {postingId} not found.");

        if (posting.Status != PostingStatus.Posted)
        {
            throw new InvalidOperationException("Can only void posted charges.");
        }

        // In production, this would call the PMS to reverse the charge
        posting.Status = PostingStatus.Cancelled;
        posting.ErrorMessage = $"Voided by user: {reason}";

        await LogActivityAsync(new PMSActivityLog
        {
            PMSConfigurationId = posting.PMSConfigurationId,
            ActivityType = "RoomChargeVoided",
            Description = $"Room charge voided: {posting.PostingReference}",
            RoomChargePostingId = posting.Id,
            IsSuccess = true,
            UserId = userId,
            IsActive = true
        }, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RoomChargeResult
        {
            IsSuccess = true,
            PostingId = postingId,
            Status = PostingStatus.Cancelled
        };
    }

    #endregion

    #region Guest Lookup

    public async Task<GuestLookupResult> LookupGuestByRoomAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default)
    {
        var config = configId.HasValue
            ? await GetConfigurationByIdAsync(configId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null)
        {
            return new GuestLookupResult
            {
                IsSuccess = false,
                ErrorMessage = "No PMS configuration found"
            };
        }

        // Check cache first
        var cached = await _context.PMSGuestLookups
            .FirstOrDefaultAsync(g => g.PMSConfigurationId == config.Id &&
                                      g.RoomNumber == roomNumber &&
                                      g.CacheExpiresAt > DateTime.UtcNow &&
                                      g.IsActive,
                                 cancellationToken).ConfigureAwait(false);

        if (cached != null)
        {
            return new GuestLookupResult
            {
                IsSuccess = true,
                IsCached = true,
                Guest = MapToGuestInfo(cached)
            };
        }

        // Fetch from PMS (simulated)
        var guestInfo = await FetchGuestFromPMSAsync(config, roomNumber, cancellationToken).ConfigureAwait(false);

        if (guestInfo != null)
        {
            // Cache the result
            var cacheEntry = new PMSGuestLookup
            {
                PMSConfigurationId = config.Id,
                RoomNumber = roomNumber,
                FirstName = guestInfo.FirstName,
                LastName = guestInfo.LastName,
                FolioNumber = guestInfo.FolioNumber,
                ConfirmationNumber = guestInfo.ConfirmationNumber,
                PMSGuestId = guestInfo.PMSGuestId,
                Status = guestInfo.Status,
                CheckInDate = guestInfo.CheckInDate,
                CheckOutDate = guestInfo.CheckOutDate,
                VIPStatus = guestInfo.VIPStatus,
                CreditLimit = guestInfo.CreditLimit,
                CurrentBalance = guestInfo.CurrentBalance,
                AllowRoomCharges = guestInfo.AllowRoomCharges,
                ChargeBlockReason = guestInfo.ChargeBlockReason,
                CompanyName = guestInfo.CompanyName,
                CacheExpiresAt = DateTime.UtcNow.Add(DefaultCacheDuration),
                IsActive = true
            };

            _context.PMSGuestLookups.Add(cacheEntry);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new GuestLookupResult
            {
                IsSuccess = true,
                IsCached = false,
                Guest = guestInfo
            };
        }

        return new GuestLookupResult
        {
            IsSuccess = false,
            ErrorMessage = $"No guest found in room {roomNumber}"
        };
    }

    public async Task<IEnumerable<GuestInfo>> SearchGuestsByNameAsync(string name, int? configId = null, CancellationToken cancellationToken = default)
    {
        var config = configId.HasValue
            ? await GetConfigurationByIdAsync(configId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null)
            return Enumerable.Empty<GuestInfo>();

        // Search in cache first
        var cached = await _context.PMSGuestLookups
            .Where(g => g.PMSConfigurationId == config.Id &&
                       (g.FirstName.Contains(name) || g.LastName.Contains(name)) &&
                       g.Status == GuestStatus.InHouse &&
                       g.CacheExpiresAt > DateTime.UtcNow &&
                       g.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return cached.Select(MapToGuestInfo);
    }

    public async Task<GuestLookupResult> LookupGuestByConfirmationAsync(string confirmationNumber, int? configId = null, CancellationToken cancellationToken = default)
    {
        var config = configId.HasValue
            ? await GetConfigurationByIdAsync(configId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null)
        {
            return new GuestLookupResult
            {
                IsSuccess = false,
                ErrorMessage = "No PMS configuration found"
            };
        }

        var cached = await _context.PMSGuestLookups
            .FirstOrDefaultAsync(g => g.PMSConfigurationId == config.Id &&
                                      g.ConfirmationNumber == confirmationNumber &&
                                      g.CacheExpiresAt > DateTime.UtcNow &&
                                      g.IsActive,
                                 cancellationToken).ConfigureAwait(false);

        if (cached != null)
        {
            return new GuestLookupResult
            {
                IsSuccess = true,
                IsCached = true,
                Guest = MapToGuestInfo(cached)
            };
        }

        return new GuestLookupResult
        {
            IsSuccess = false,
            ErrorMessage = $"No guest found with confirmation {confirmationNumber}"
        };
    }

    public async Task<IEnumerable<GuestInfo>> GetInHouseGuestsAsync(int? configId = null, CancellationToken cancellationToken = default)
    {
        var config = configId.HasValue
            ? await GetConfigurationByIdAsync(configId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null)
            return Enumerable.Empty<GuestInfo>();

        var cached = await _context.PMSGuestLookups
            .Where(g => g.PMSConfigurationId == config.Id &&
                       g.Status == GuestStatus.InHouse &&
                       g.CacheExpiresAt > DateTime.UtcNow &&
                       g.IsActive)
            .OrderBy(g => g.RoomNumber)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return cached.Select(MapToGuestInfo);
    }

    public async Task<ChargeValidationResult> ValidateRoomChargeAsync(string roomNumber, decimal amount, int? configId = null, CancellationToken cancellationToken = default)
    {
        var guestResult = await LookupGuestByRoomAsync(roomNumber, configId, cancellationToken).ConfigureAwait(false);

        if (!guestResult.IsSuccess || guestResult.Guest == null)
        {
            return new ChargeValidationResult
            {
                IsAllowed = false,
                DenialReason = guestResult.ErrorMessage ?? "Guest not found",
                GuestStatus = GuestStatus.CheckedOut
            };
        }

        var guest = guestResult.Guest;

        if (guest.Status != GuestStatus.InHouse)
        {
            return new ChargeValidationResult
            {
                IsAllowed = false,
                DenialReason = $"Guest is not checked in (status: {guest.Status})",
                GuestStatus = guest.Status
            };
        }

        if (!guest.AllowRoomCharges)
        {
            return new ChargeValidationResult
            {
                IsAllowed = false,
                DenialReason = guest.ChargeBlockReason ?? "Room charges not allowed",
                GuestStatus = guest.Status,
                CurrentBalance = guest.CurrentBalance
            };
        }

        if (guest.CreditLimit.HasValue && guest.CurrentBalance.HasValue)
        {
            var available = guest.CreditLimit.Value - guest.CurrentBalance.Value;
            if (amount > available)
            {
                return new ChargeValidationResult
                {
                    IsAllowed = false,
                    DenialReason = $"Insufficient credit. Available: {available:N2}",
                    GuestStatus = guest.Status,
                    AvailableCredit = available,
                    CurrentBalance = guest.CurrentBalance
                };
            }
        }

        return new ChargeValidationResult
        {
            IsAllowed = true,
            GuestStatus = guest.Status,
            AvailableCredit = guest.CreditLimit.HasValue && guest.CurrentBalance.HasValue
                ? guest.CreditLimit.Value - guest.CurrentBalance.Value
                : null,
            CurrentBalance = guest.CurrentBalance
        };
    }

    public async Task RefreshGuestCacheAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default)
    {
        var config = configId.HasValue
            ? await GetConfigurationByIdAsync(configId.Value, cancellationToken).ConfigureAwait(false)
            : await GetDefaultConfigurationAsync(cancellationToken).ConfigureAwait(false);

        if (config == null) return;

        // Invalidate existing cache
        var existing = await _context.PMSGuestLookups
            .Where(g => g.PMSConfigurationId == config.Id && g.RoomNumber == roomNumber && g.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var entry in existing)
        {
            entry.CacheExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fetch fresh data
        await LookupGuestByRoomAsync(roomNumber, config.Id, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Folio Integration

    public async Task<FolioDetails?> GetFolioDetailsAsync(string roomNumber, int? configId = null, CancellationToken cancellationToken = default)
    {
        var guestResult = await LookupGuestByRoomAsync(roomNumber, configId, cancellationToken).ConfigureAwait(false);

        if (!guestResult.IsSuccess || guestResult.Guest == null)
            return null;

        var guest = guestResult.Guest;

        return new FolioDetails
        {
            FolioNumber = guest.FolioNumber ?? string.Empty,
            RoomNumber = guest.RoomNumber,
            GuestName = guest.FullName,
            ArrivalDate = guest.CheckInDate,
            DepartureDate = guest.CheckOutDate,
            Balance = guest.CurrentBalance ?? 0,
            CreditLimit = guest.CreditLimit ?? 0,
            CompanyName = guest.CompanyName,
            Status = guest.Status.ToString()
        };
    }

    public async Task<IEnumerable<FolioTransaction>> GetFolioTransactionsAsync(string folioNumber, int? configId = null, CancellationToken cancellationToken = default)
    {
        // In production, this would fetch from PMS
        return await Task.FromResult(Enumerable.Empty<FolioTransaction>());
    }

    public async Task<IEnumerable<FolioTransaction>> GetPOSChargesOnFolioAsync(string folioNumber, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RoomChargePostings
            .Where(p => p.FolioNumber == folioNumber &&
                       p.Status == PostingStatus.Posted &&
                       p.IsActive);

        if (startDate.HasValue)
            query = query.Where(p => p.PostedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.PostedAt <= endDate.Value);

        var postings = await query.OrderBy(p => p.PostedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

        return postings.Select(p => new FolioTransaction
        {
            TransactionId = p.PMSTransactionId ?? p.PostingReference,
            TransactionDate = p.PostedAt ?? p.CreatedAt,
            TransactionCode = p.TransactionCode ?? "POS",
            Description = p.Description,
            Amount = p.TotalAmount,
            RevenueCenterCode = p.RevenueCenterCode,
            Reference = p.PostingReference,
            IsPostedFromPOS = true
        });
    }

    #endregion

    #region Queue Management

    public async Task AddToQueueAsync(int postingId, int priority = 5, DateTime? scheduledAt = null, CancellationToken cancellationToken = default)
    {
        var queueItem = new PMSPostingQueue
        {
            RoomChargePostingId = postingId,
            Priority = priority,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            IsActive = true
        };

        _context.PMSPostingQueues.Add(queueItem);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PMSPostingQueue>> GetPendingQueueItemsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PMSPostingQueues
            .Include(q => q.RoomChargePosting)
            .Where(q => !q.IsProcessing &&
                       q.ScheduledAt <= DateTime.UtcNow &&
                       q.Attempts < q.MaxAttempts &&
                       q.IsActive)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.ScheduledAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<QueueProcessingResult> ProcessQueueAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueueProcessingResult();

        var queueItems = await GetPendingQueueItemsAsync(batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var item in queueItems)
        {
            item.IsProcessing = true;
            item.ProcessingStartedAt = DateTime.UtcNow;
            item.Attempts++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            result.ProcessedCount++;

            try
            {
                var postingResult = await RetryPostingAsync(item.RoomChargePostingId, cancellationToken).ConfigureAwait(false);

                if (postingResult.IsSuccess)
                {
                    result.SuccessCount++;
                    item.IsActive = false;
                }
                else if (postingResult.IsRetryable && item.Attempts < item.MaxAttempts)
                {
                    result.RequeueCount++;
                    item.IsProcessing = false;
                    item.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.Attempts));
                    item.ScheduledAt = item.NextRetryAt.Value;
                    item.LastError = postingResult.ErrorMessage;
                }
                else
                {
                    result.FailedCount++;
                    result.Errors.Add($"Posting {item.RoomChargePostingId}: {postingResult.ErrorMessage}");
                    item.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Posting {item.RoomChargePostingId}: {ex.Message}");
                item.IsProcessing = false;
                item.LastError = ex.Message;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        return result;
    }

    public async Task<QueueStatusSummary> GetQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.PMSPostingQueues
            .Where(q => q.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new QueueStatusSummary
        {
            PendingCount = items.Count(q => !q.IsProcessing && q.Attempts < q.MaxAttempts),
            ProcessingCount = items.Count(q => q.IsProcessing),
            FailedCount = items.Count(q => q.Attempts >= q.MaxAttempts),
            RetryCount = items.Count(q => q.Attempts > 0 && q.Attempts < q.MaxAttempts),
            OldestPendingAt = items.Where(q => !q.IsProcessing).Min(q => q.ScheduledAt as DateTime?),
            LastProcessedAt = items.Max(q => q.ProcessingStartedAt)
        };
    }

    public async Task ClearOldQueueItemsAsync(int daysOld = 7, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);

        var oldItems = await _context.PMSPostingQueues
            .Where(q => q.CreatedAt < cutoff && !q.IsProcessing)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var item in oldItems)
        {
            item.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Error Handling

    public async Task<IEnumerable<PMSErrorMapping>> GetErrorMappingsAsync(PMSType? pmsType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PMSErrorMappings.Where(e => e.IsActive);

        if (pmsType.HasValue)
            query = query.Where(e => e.PMSType == null || e.PMSType == pmsType.Value);

        return await query.OrderBy(e => e.ErrorCode).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PMSErrorMapping> CreateErrorMappingAsync(PMSErrorMapping mapping, CancellationToken cancellationToken = default)
    {
        _context.PMSErrorMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return mapping;
    }

    public async Task<string> GetFriendlyErrorMessageAsync(string errorCode, PMSType pmsType, CancellationToken cancellationToken = default)
    {
        var mapping = await _context.PMSErrorMappings
            .FirstOrDefaultAsync(e => e.ErrorCode == errorCode &&
                                      (e.PMSType == null || e.PMSType == pmsType) &&
                                      e.IsActive,
                                 cancellationToken).ConfigureAwait(false);

        return mapping?.FriendlyMessage ?? $"An error occurred (code: {errorCode})";
    }

    public async Task LogActivityAsync(PMSActivityLog log, CancellationToken cancellationToken = default)
    {
        _context.PMSActivityLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PMSActivityLog>> GetActivityLogsAsync(int? configId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var query = _context.PMSActivityLogs.Where(l => l.IsActive);

        if (configId.HasValue)
            query = query.Where(l => l.PMSConfigurationId == configId.Value);
        if (startDate.HasValue)
            query = query.Where(l => l.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(l => l.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Reports

    public async Task<PostingSummaryReport> GetPostingSummaryAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var postings = await _context.RoomChargePostings
            .Where(p => p.PMSConfigurationId == configId &&
                       p.CreatedAt >= startDate &&
                       p.CreatedAt <= endDate &&
                       p.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var report = new PostingSummaryReport
        {
            ConfigId = configId,
            StartDate = startDate,
            EndDate = endDate,
            TotalPostings = postings.Count,
            SuccessfulPostings = postings.Count(p => p.Status == PostingStatus.Posted),
            FailedPostings = postings.Count(p => p.Status == PostingStatus.Failed),
            PendingPostings = postings.Count(p => p.Status is PostingStatus.Pending or PostingStatus.Processing or PostingStatus.Retry),
            TotalAmountPosted = postings.Where(p => p.Status == PostingStatus.Posted).Sum(p => p.TotalAmount),
            TotalAmountFailed = postings.Where(p => p.Status == PostingStatus.Failed).Sum(p => p.TotalAmount),
            TotalAmountPending = postings.Where(p => p.Status is PostingStatus.Pending or PostingStatus.Processing or PostingStatus.Retry).Sum(p => p.TotalAmount)
        };

        foreach (var group in postings.Where(p => p.Status == PostingStatus.Posted).GroupBy(p => p.ChargeType))
        {
            report.ByChargeType[group.Key] = group.Sum(p => p.TotalAmount);
        }

        foreach (var group in postings.Where(p => p.Status == PostingStatus.Failed && p.ErrorMessage != null).GroupBy(p => p.ErrorMessage))
        {
            report.ErrorBreakdown[group.Key!] = group.Count();
        }

        return report;
    }

    public async Task<IEnumerable<FailedPostingDetail>> GetFailedPostingsReportAsync(int? configId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RoomChargePostings
            .Where(p => p.Status == PostingStatus.Failed && p.IsActive);

        if (configId.HasValue)
            query = query.Where(p => p.PMSConfigurationId == configId.Value);
        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value);

        var postings = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

        return postings.Select(p => new FailedPostingDetail
        {
            PostingId = p.Id,
            PostingReference = p.PostingReference,
            RoomNumber = p.RoomNumber,
            GuestName = p.GuestName,
            Amount = p.TotalAmount,
            CreatedAt = p.CreatedAt,
            AttemptCount = p.AttemptCount,
            ErrorMessage = p.ErrorMessage,
            IsRetryable = true
        });
    }

    #endregion

    #region Private Methods

    private static string GetTestEndpoint(PMSConfiguration config)
    {
        return config.PMSType switch
        {
            PMSType.Opera => $"{config.ApiEndpoint}/ows/ping",
            PMSType.Mews => $"{config.ApiEndpoint}/api/connector/v1/general/ping",
            _ => $"{config.ApiEndpoint}/ping"
        };
    }

    private static string GetPMSVersion(PMSType pmsType)
    {
        return pmsType switch
        {
            PMSType.Opera => "Opera Cloud 22.1",
            PMSType.Mews => "Mews Commander",
            PMSType.Protel => "Protel Air",
            _ => "Unknown"
        };
    }

    private static void AddAuthHeaders(HttpRequestMessage request, PMSConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
        }
        else if (!string.IsNullOrEmpty(config.ApiKey))
        {
            request.Headers.Add("X-API-Key", config.ApiKey);
        }
    }

    private async Task<RoomChargeResult> PostToPMSAsync(RoomChargePosting posting, PMSConfiguration config, CancellationToken cancellationToken)
    {
        // This is a simulated PMS posting
        // In production, this would call the actual PMS API based on config.PMSType

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        // Simulate success for demo
        return new RoomChargeResult
        {
            IsSuccess = true,
            PMSTransactionId = $"PMS-{Guid.NewGuid().ToString()[..10].ToUpper()}"
        };
    }

    private async Task<GuestInfo?> FetchGuestFromPMSAsync(PMSConfiguration config, string roomNumber, CancellationToken cancellationToken)
    {
        // This is a simulated PMS guest lookup
        // In production, this would call the actual PMS API

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        // Return simulated guest data
        return new GuestInfo
        {
            RoomNumber = roomNumber,
            FirstName = "John",
            LastName = "Doe",
            FolioNumber = $"F{DateTime.UtcNow:yyyyMMdd}{roomNumber}",
            ConfirmationNumber = $"CONF{Guid.NewGuid().ToString()[..6].ToUpper()}",
            PMSGuestId = Guid.NewGuid().ToString(),
            Status = GuestStatus.InHouse,
            CheckInDate = DateTime.UtcNow.AddDays(-1),
            CheckOutDate = DateTime.UtcNow.AddDays(3),
            CreditLimit = 100000m,
            CurrentBalance = 15000m,
            AllowRoomCharges = true
        };
    }

    private static GuestInfo MapToGuestInfo(PMSGuestLookup cached)
    {
        return new GuestInfo
        {
            RoomNumber = cached.RoomNumber,
            FirstName = cached.FirstName,
            LastName = cached.LastName,
            FolioNumber = cached.FolioNumber,
            ConfirmationNumber = cached.ConfirmationNumber,
            PMSGuestId = cached.PMSGuestId,
            Status = cached.Status,
            CheckInDate = cached.CheckInDate,
            CheckOutDate = cached.CheckOutDate,
            VIPStatus = cached.VIPStatus,
            CreditLimit = cached.CreditLimit,
            CurrentBalance = cached.CurrentBalance,
            AllowRoomCharges = cached.AllowRoomCharges,
            ChargeBlockReason = cached.ChargeBlockReason,
            CompanyName = cached.CompanyName
        };
    }

    #endregion
}
