using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of API management service.
/// </summary>
public class ApiService : IApiService
{
    private readonly POSDbContext _context;
    private readonly HttpClient _httpClient;
    private const int TokenExpirationMinutes = 60;
    private const int RefreshTokenExpirationDays = 30;

    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(30);

    public ApiService(POSDbContext context, IHttpClientFactory? httpClientFactory = null)
    {
        _context = context;
        _httpClient = httpClientFactory?.CreateClient("Webhook") ?? new HttpClient { Timeout = DefaultHttpTimeout };
    }

    #region Client Management

    public async Task<ApiClientCreatedResult> CreateClientAsync(CreateApiClientRequest request, CancellationToken cancellationToken = default)
    {
        var clientId = GenerateClientId();
        var clientSecret = GenerateSecret(32);
        var apiKey = GenerateApiKey();

        var client = new ApiClient
        {
            Name = request.Name,
            Description = request.Description,
            ClientId = clientId,
            ClientSecretHash = HashValue(clientSecret),
            ApiKeyHash = HashValue(apiKey), // Hash the API key for storage
            AuthType = request.AuthType,
            Status = ApiClientStatus.Active,
            StoreId = request.StoreId,
            UserId = request.UserId,
            AllowedIPs = request.AllowedIPs,
            AllowedOrigins = request.AllowedOrigins,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerHour = request.RateLimitPerHour,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };

        _context.ApiClients.Add(client);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Assign scopes
        if (request.ScopeIds?.Any() == true)
        {
            await AssignScopesToClientAsync(client.Id, request.ScopeIds, cancellationToken).ConfigureAwait(false);
        }

        return new ApiClientCreatedResult
        {
            ClientId = client.Id,
            ClientIdString = clientId,
            ClientSecret = clientSecret,
            ApiKey = apiKey,
            CreatedAt = client.CreatedAt,
            ExpiresAt = client.ExpiresAt
        };
    }

    public async Task<ApiClient?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiClients
            .Include(c => c.Scopes).ThenInclude(s => s.ApiScope)
            .FirstOrDefaultAsync(c => c.Id == clientId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiClient?> GetClientByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiClients
            .Include(c => c.Scopes).ThenInclude(s => s.ApiScope)
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ApiClient>> GetAllClientsAsync(ApiClientStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ApiClients.Where(c => c.IsActive);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiClient> UpdateClientAsync(ApiClient client, CancellationToken cancellationToken = default)
    {
        _context.ApiClients.Update(client);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return client;
    }

    public async Task<ApiClientCreatedResult> RegenerateCredentialsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        var newSecret = GenerateSecret(32);
        var newApiKey = GenerateApiKey();

        client.ClientSecretHash = HashValue(newSecret);
        client.ApiKeyHash = HashValue(newApiKey); // Hash the API key for secure storage

        // Revoke all existing tokens
        await RevokeAllTokensAsync(clientId, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApiClientCreatedResult
        {
            ClientId = client.Id,
            ClientIdString = client.ClientId,
            ClientSecret = newSecret,
            ApiKey = newApiKey,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task SuspendClientAsync(int clientId, string reason, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        client.Status = ApiClientStatus.Suspended;
        client.Notes = $"Suspended: {reason}";

        await RevokeAllTokensAsync(clientId, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ActivateClientAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        client.Status = ApiClientStatus.Active;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeClientAsync(int clientId, string reason, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        client.Status = ApiClientStatus.Revoked;
        client.Notes = $"Revoked: {reason}";

        await RevokeAllTokensAsync(clientId, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Scope Management

    public async Task<IEnumerable<ApiScope>> GetAllScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiScopes
            .Where(s => s.IsActive)
            .OrderBy(s => s.Resource)
            .ThenBy(s => s.Action)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiScope> CreateScopeAsync(ApiScope scope, CancellationToken cancellationToken = default)
    {
        _context.ApiScopes.Add(scope);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return scope;
    }

    public async Task<IEnumerable<ApiScope>> GetClientScopesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiClientScopes
            .Where(cs => cs.ApiClientId == clientId && cs.IsActive)
            .Select(cs => cs.ApiScope)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AssignScopesToClientAsync(int clientId, IEnumerable<int> scopeIds, CancellationToken cancellationToken = default)
    {
        foreach (var scopeId in scopeIds)
        {
            var existing = await _context.ApiClientScopes
                .FirstOrDefaultAsync(cs => cs.ApiClientId == clientId && cs.ApiScopeId == scopeId, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.ApiClientScopes.Add(new ApiClientScope
                {
                    ApiClientId = clientId,
                    ApiScopeId = scopeId,
                    IsActive = true
                });
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveScopeFromClientAsync(int clientId, int scopeId, CancellationToken cancellationToken = default)
    {
        var clientScope = await _context.ApiClientScopes
            .FirstOrDefaultAsync(cs => cs.ApiClientId == clientId && cs.ApiScopeId == scopeId && cs.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (clientScope != null)
        {
            clientScope.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Authentication

    public async Task<AuthenticationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // Hash the provided API key for comparison (API keys should never be stored in plain text)
        var apiKeyHash = HashValue(apiKey);

        var client = await _context.ApiClients
            .FirstOrDefaultAsync(c => c.ApiKeyHash == apiKeyHash && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (client == null)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "invalid_key",
                ErrorMessage = "Invalid API key"
            };
        }

        var validationResult = ValidateClientStatus(client);
        if (!validationResult.IsSuccess)
            return validationResult;

        client.LastUsedAt = DateTime.UtcNow;
        client.TotalRequests++;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult
        {
            IsSuccess = true,
            ClientId = client.Id,
            Client = client
        };
    }

    public async Task<AuthenticationResult> ValidateClientCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var client = await _context.ApiClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (client == null)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "invalid_client",
                ErrorMessage = "Invalid client credentials"
            };
        }

        var secretHash = HashValue(clientSecret);
        if (client.ClientSecretHash != secretHash)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "invalid_client",
                ErrorMessage = "Invalid client credentials"
            };
        }

        var validationResult = ValidateClientStatus(client);
        if (!validationResult.IsSuccess)
            return validationResult;

        return new AuthenticationResult
        {
            IsSuccess = true,
            ClientId = client.Id,
            Client = client
        };
    }

    public async Task<TokenResult> IssueTokenAsync(int clientId, IEnumerable<string>? scopes = null, string? clientIP = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        var accessToken = GenerateSecret(48);
        var refreshToken = GenerateSecret(64);

        var grantedScopes = scopes != null
            ? string.Join(" ", scopes)
            : string.Join(" ", client.Scopes.Select(s => s.ApiScope.Name));

        var tokenEntry = new ApiAccessToken
        {
            ApiClientId = clientId,
            TokenHash = HashValue(accessToken),
            RefreshTokenHash = HashValue(refreshToken),
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            ClientIP = clientIP,
            UserAgent = userAgent,
            GrantedScopes = grantedScopes,
            IsActive = true
        };

        _context.ApiAccessTokens.Add(tokenEntry);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TokenResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = TokenExpirationMinutes * 60,
            Scope = grantedScopes
        };
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashValue(token);

        var tokenEntry = await _context.ApiAccessTokens
            .Include(t => t.ApiClient)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.IsActive && !t.IsRevoked, cancellationToken)
            .ConfigureAwait(false);

        if (tokenEntry == null)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }

        if (tokenEntry.ExpiresAt < DateTime.UtcNow)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }

        var clientValidation = ValidateClientStatus(tokenEntry.ApiClient);
        if (!clientValidation.IsSuccess)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = clientValidation.ErrorMessage
            };
        }

        return new TokenValidationResult
        {
            IsValid = true,
            ClientId = tokenEntry.ApiClientId,
            Scopes = tokenEntry.GrantedScopes?.Split(' ') ?? Enumerable.Empty<string>(),
            ExpiresAt = tokenEntry.ExpiresAt
        };
    }

    public async Task<TokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashValue(refreshToken);

        var tokenEntry = await _context.ApiAccessTokens
            .Include(t => t.ApiClient)
            .FirstOrDefaultAsync(t => t.RefreshTokenHash == tokenHash && t.IsActive && !t.IsRevoked, cancellationToken)
            .ConfigureAwait(false);

        if (tokenEntry == null || tokenEntry.RefreshExpiresAt < DateTime.UtcNow)
        {
            return new TokenResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid or expired refresh token"
            };
        }

        // Revoke old token
        tokenEntry.IsRevoked = true;
        tokenEntry.RevokedAt = DateTime.UtcNow;

        // Issue new token
        var scopes = tokenEntry.GrantedScopes?.Split(' ');
        var result = await IssueTokenAsync(tokenEntry.ApiClientId, scopes, tokenEntry.ClientIP, tokenEntry.UserAgent, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashValue(token);

        var tokenEntry = await _context.ApiAccessTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (tokenEntry != null)
        {
            tokenEntry.IsRevoked = true;
            tokenEntry.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RevokeAllTokensAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.ApiAccessTokens
            .Where(t => t.ApiClientId == clientId && t.IsActive && !t.IsRevoked)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Authorization

    public async Task<bool> HasScopeAsync(int clientId, string scope, CancellationToken cancellationToken = default)
    {
        return await _context.ApiClientScopes
            .AnyAsync(cs => cs.ApiClientId == clientId &&
                           cs.ApiScope.Name == scope &&
                           cs.IsActive,
                      cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> CanAccessAsync(int clientId, string resource, string action, CancellationToken cancellationToken = default)
    {
        return await _context.ApiClientScopes
            .AnyAsync(cs => cs.ApiClientId == clientId &&
                           cs.ApiScope.Resource == resource &&
                           (cs.ApiScope.Action == action || cs.ApiScope.Action == "admin") &&
                           cs.IsActive,
                      cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ResourceAccess>> GetAllowedResourcesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var scopes = await GetClientScopesAsync(clientId, cancellationToken).ConfigureAwait(false);

        return scopes
            .GroupBy(s => s.Resource)
            .Select(g => new ResourceAccess
            {
                Resource = g.Key,
                Actions = g.Select(s => s.Action).Distinct()
            });
    }

    #endregion

    #region Rate Limiting

    public async Task<RateLimitResult> CheckRateLimitAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        var now = DateTime.UtcNow;

        // Check minute limit
        var minuteEntry = await GetOrCreateRateLimitEntry(clientId, "Minute", now, cancellationToken).ConfigureAwait(false);
        if (minuteEntry.RequestCount >= client.RateLimitPerMinute)
        {
            var resetAt = minuteEntry.WindowStart.AddMinutes(1);
            return new RateLimitResult
            {
                IsAllowed = false,
                LimitType = "Minute",
                Limit = client.RateLimitPerMinute,
                Remaining = 0,
                ResetAt = resetAt,
                RetryAfterSeconds = (int)(resetAt - now).TotalSeconds
            };
        }

        // Check hour limit
        var hourEntry = await GetOrCreateRateLimitEntry(clientId, "Hour", now, cancellationToken).ConfigureAwait(false);
        if (hourEntry.RequestCount >= client.RateLimitPerHour)
        {
            var resetAt = hourEntry.WindowStart.AddHours(1);
            return new RateLimitResult
            {
                IsAllowed = false,
                LimitType = "Hour",
                Limit = client.RateLimitPerHour,
                Remaining = 0,
                ResetAt = resetAt,
                RetryAfterSeconds = (int)(resetAt - now).TotalSeconds
            };
        }

        // Check day limit
        var dayEntry = await GetOrCreateRateLimitEntry(clientId, "Day", now, cancellationToken).ConfigureAwait(false);
        if (dayEntry.RequestCount >= client.RateLimitPerDay)
        {
            var resetAt = dayEntry.WindowStart.AddDays(1);
            return new RateLimitResult
            {
                IsAllowed = false,
                LimitType = "Day",
                Limit = client.RateLimitPerDay,
                Remaining = 0,
                ResetAt = resetAt,
                RetryAfterSeconds = (int)(resetAt - now).TotalSeconds
            };
        }

        return new RateLimitResult
        {
            IsAllowed = true,
            Limit = client.RateLimitPerMinute,
            Remaining = client.RateLimitPerMinute - minuteEntry.RequestCount - 1,
            ResetAt = minuteEntry.WindowStart.AddMinutes(1)
        };
    }

    public async Task RecordRequestAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var minuteEntry = await GetOrCreateRateLimitEntry(clientId, "Minute", now, cancellationToken).ConfigureAwait(false);
        minuteEntry.RequestCount++;
        minuteEntry.LastRequestAt = now;

        var hourEntry = await GetOrCreateRateLimitEntry(clientId, "Hour", now, cancellationToken).ConfigureAwait(false);
        hourEntry.RequestCount++;
        hourEntry.LastRequestAt = now;

        var dayEntry = await GetOrCreateRateLimitEntry(clientId, "Day", now, cancellationToken).ConfigureAwait(false);
        dayEntry.RequestCount++;
        dayEntry.LastRequestAt = now;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"API client {clientId} not found.");

        var now = DateTime.UtcNow;

        var minuteEntry = await GetOrCreateRateLimitEntry(clientId, "Minute", now, cancellationToken).ConfigureAwait(false);
        var hourEntry = await GetOrCreateRateLimitEntry(clientId, "Hour", now, cancellationToken).ConfigureAwait(false);
        var dayEntry = await GetOrCreateRateLimitEntry(clientId, "Day", now, cancellationToken).ConfigureAwait(false);

        return new RateLimitStatus
        {
            ClientId = clientId,
            MinuteLimit = client.RateLimitPerMinute,
            MinuteUsed = minuteEntry.RequestCount,
            MinuteRemaining = Math.Max(0, client.RateLimitPerMinute - minuteEntry.RequestCount),
            MinuteResetAt = minuteEntry.WindowStart.AddMinutes(1),
            HourLimit = client.RateLimitPerHour,
            HourUsed = hourEntry.RequestCount,
            HourRemaining = Math.Max(0, client.RateLimitPerHour - hourEntry.RequestCount),
            HourResetAt = hourEntry.WindowStart.AddHours(1),
            DayLimit = client.RateLimitPerDay,
            DayUsed = dayEntry.RequestCount,
            DayRemaining = Math.Max(0, client.RateLimitPerDay - dayEntry.RequestCount),
            DayResetAt = dayEntry.WindowStart.AddDays(1)
        };
    }

    public async Task ResetRateLimitsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var entries = await _context.ApiRateLimitEntries
            .Where(e => e.ApiClientId == clientId && e.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in entries)
        {
            entry.RequestCount = 0;
            entry.WindowStart = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Request Logging

    public async Task LogRequestAsync(ApiRequestLog log, CancellationToken cancellationToken = default)
    {
        _context.ApiRequestLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ApiRequestLog>> GetRequestLogsAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var query = _context.ApiRequestLogs.Where(l => l.IsActive);

        if (clientId.HasValue)
            query = query.Where(l => l.ApiClientId == clientId.Value);
        if (startDate.HasValue)
            query = query.Where(l => l.RequestedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(l => l.RequestedAt <= endDate.Value);

        return await query
            .OrderByDescending(l => l.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiRequestStats> GetRequestStatsAsync(int? clientId = null, DateTime startDate = default, DateTime endDate = default, CancellationToken cancellationToken = default)
    {
        if (startDate == default) startDate = DateTime.UtcNow.AddDays(-7);
        if (endDate == default) endDate = DateTime.UtcNow;

        var query = _context.ApiRequestLogs
            .Where(l => l.RequestedAt >= startDate && l.RequestedAt <= endDate && l.IsActive);

        if (clientId.HasValue)
            query = query.Where(l => l.ApiClientId == clientId.Value);

        var logs = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var stats = new ApiRequestStats
        {
            TotalRequests = logs.Count,
            SuccessfulRequests = logs.Count(l => l.StatusCode >= 200 && l.StatusCode < 300),
            FailedRequests = logs.Count(l => l.StatusCode >= 400),
            AverageDurationMs = logs.Any() ? logs.Average(l => l.DurationMs) : 0
        };

        if (logs.Any())
        {
            var orderedDurations = logs.OrderBy(l => l.DurationMs).ToList();
            var p95Index = (int)(orderedDurations.Count * 0.95);
            stats.P95DurationMs = orderedDurations[Math.Min(p95Index, orderedDurations.Count - 1)].DurationMs;
        }

        stats.RequestsByPath = logs.GroupBy(l => l.Path).ToDictionary(g => g.Key, g => g.Count());
        stats.RequestsByMethod = logs.GroupBy(l => l.HttpMethod).ToDictionary(g => g.Key, g => g.Count());
        stats.RequestsByStatusCode = logs.GroupBy(l => l.StatusCode).ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    #endregion

    #region Webhooks

    public async Task<WebhookConfig> CreateWebhookAsync(WebhookConfig webhook, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(webhook.Secret))
        {
            webhook.Secret = GenerateSecret(32);
        }

        _context.WebhookConfigs.Add(webhook);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return webhook;
    }

    public async Task<IEnumerable<WebhookConfig>> GetWebhooksAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookConfigs
            .Where(w => w.ApiClientId == clientId && w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WebhookConfig> UpdateWebhookAsync(WebhookConfig webhook, CancellationToken cancellationToken = default)
    {
        _context.WebhookConfigs.Update(webhook);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return webhook;
    }

    public async Task DeleteWebhookAsync(int webhookId, CancellationToken cancellationToken = default)
    {
        var webhook = await _context.WebhookConfigs
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (webhook != null)
        {
            webhook.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Whitelist of valid webhook event names to prevent injection attacks
    private static readonly HashSet<string> ValidWebhookEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "order.created", "order.updated", "order.completed", "order.cancelled",
        "product.created", "product.updated", "product.deleted",
        "inventory.low", "inventory.updated", "inventory.adjusted",
        "payment.received", "payment.failed", "payment.refunded",
        "receipt.created", "receipt.voided",
        "customer.created", "customer.updated",
        "loyalty.points_earned", "loyalty.points_redeemed",
        "workperiod.opened", "workperiod.closed"
    };

    public async Task<WebhookDeliveryResult> TriggerWebhookAsync(string eventName, object payload, int? storeId = null, CancellationToken cancellationToken = default)
    {
        // Validate event name against whitelist to prevent injection attacks
        if (string.IsNullOrWhiteSpace(eventName) || !ValidWebhookEvents.Contains(eventName))
        {
            return new WebhookDeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = $"Invalid or unsupported webhook event: {eventName}"
            };
        }

        var webhooks = await _context.WebhookConfigs
            .Include(w => w.ApiClient)
            .Where(w => w.IsEnabled && w.IsActive && w.Events.Contains(eventName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (storeId.HasValue)
        {
            webhooks = webhooks.Where(w => w.ApiClient.StoreId == null || w.ApiClient.StoreId == storeId).ToList();
        }

        foreach (var webhook in webhooks)
        {
            await DeliverWebhookAsync(webhook, eventName, payload, cancellationToken).ConfigureAwait(false);
        }

        return new WebhookDeliveryResult
        {
            IsSuccess = true
        };
    }

    public async Task<IEnumerable<WebhookDelivery>> GetWebhookDeliveriesAsync(int webhookId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookDeliveries
            .Where(d => d.WebhookConfigId == webhookId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WebhookDeliveryResult> RetryWebhookDeliveryAsync(int deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await _context.WebhookDeliveries
            .Include(d => d.WebhookConfig)
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.IsActive, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Webhook delivery {deliveryId} not found.");

        return await DeliverWebhookAsync(delivery.WebhookConfig, delivery.Event, delivery.Payload, cancellationToken, delivery.AttemptNumber + 1).ConfigureAwait(false);
    }

    #endregion

    #region Security

    public async Task<bool> ValidateIPAsync(int clientId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false);
        if (client == null) return false;

        if (string.IsNullOrEmpty(client.AllowedIPs)) return true;

        var allowedIPs = client.AllowedIPs.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return allowedIPs.Any(ip => ip.Trim() == ipAddress || ip.Trim() == "*");
    }

    public async Task<bool> ValidateOriginAsync(int clientId, string origin, CancellationToken cancellationToken = default)
    {
        var client = await GetClientByIdAsync(clientId, cancellationToken).ConfigureAwait(false);
        if (client == null) return false;

        if (string.IsNullOrEmpty(client.AllowedOrigins)) return true;

        var allowedOrigins = client.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return allowedOrigins.Any(o => o.Trim() == origin || o.Trim() == "*");
    }

    public async Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(int? clientId = null, DateTime? startDate = null, CancellationToken cancellationToken = default)
    {
        // This would typically query a security alerts table
        return await Task.FromResult(Enumerable.Empty<SecurityAlert>());
    }

    #endregion

    #region Private Methods

    private static string GenerateClientId()
    {
        return $"cli_{Guid.NewGuid():N}".ToLower();
    }

    private static string GenerateApiKey()
    {
        return $"pk_{GenerateSecret(32)}";
    }

    private static string GenerateSecret(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..length];
    }

    private static string HashValue(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static AuthenticationResult ValidateClientStatus(ApiClient client)
    {
        if (client.Status == ApiClientStatus.Suspended)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "client_suspended",
                ErrorMessage = "API client is suspended"
            };
        }

        if (client.Status == ApiClientStatus.Revoked)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "client_revoked",
                ErrorMessage = "API client has been revoked"
            };
        }

        if (client.ExpiresAt.HasValue && client.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorCode = "client_expired",
                ErrorMessage = "API client credentials have expired"
            };
        }

        return new AuthenticationResult { IsSuccess = true };
    }

    private async Task<ApiRateLimitEntry> GetOrCreateRateLimitEntry(int clientId, string windowType, DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = windowType switch
        {
            "Minute" => new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc),
            "Hour" => new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc),
            "Day" => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc),
            _ => now
        };

        // Use a transaction to prevent race conditions when creating rate limit entries
        // Multiple concurrent requests could both find no entry and try to create one
        using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);

        try
        {
            var entry = await _context.ApiRateLimitEntries
                .FirstOrDefaultAsync(e => e.ApiClientId == clientId &&
                                          e.WindowType == windowType &&
                                          e.WindowStart == windowStart &&
                                          e.IsActive,
                                     cancellationToken).ConfigureAwait(false);

            if (entry == null)
            {
                entry = new ApiRateLimitEntry
                {
                    ApiClientId = clientId,
                    WindowType = windowType,
                    WindowStart = windowStart,
                    RequestCount = 0,
                    LastRequestAt = now,
                    IsActive = true
                };
                _context.ApiRateLimitEntries.Add(entry);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return entry;
        }
        catch (DbUpdateException)
        {
            // If a concurrent request created the entry, rollback and fetch it
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return await _context.ApiRateLimitEntries
                .FirstAsync(e => e.ApiClientId == clientId &&
                                 e.WindowType == windowType &&
                                 e.WindowStart == windowStart &&
                                 e.IsActive,
                            cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<WebhookDeliveryResult> DeliverWebhookAsync(WebhookConfig webhook, string eventName, object payload, CancellationToken cancellationToken, int attemptNumber = 1)
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            @event = eventName,
            timestamp = DateTime.UtcNow,
            data = payload
        });

        var delivery = new WebhookDelivery
        {
            WebhookConfigId = webhook.Id,
            Event = eventName,
            Payload = payloadJson,
            AttemptNumber = attemptNumber,
            Status = "Pending",
            IsActive = true
        };

        _context.WebhookDeliveries.Add(delivery);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, webhook.ContentType);

            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = ComputeHmacSignature(payloadJson, webhook.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(webhook.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var response = await _httpClient.SendAsync(request, linkedCts.Token).ConfigureAwait(false);

            stopwatch.Stop();

            delivery.ResponseCode = (int)response.StatusCode;
            delivery.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            delivery.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.Status = response.IsSuccessStatusCode ? "Delivered" : "Failed";

            webhook.LastTriggeredAt = DateTime.UtcNow;
            if (response.IsSuccessStatusCode)
                webhook.SuccessCount++;
            else
                webhook.FailureCount++;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new WebhookDeliveryResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                DeliveryId = delivery.Id,
                StatusCode = (int)response.StatusCode,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.ErrorMessage = ex.Message;
            webhook.FailureCount++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new WebhookDeliveryResult
            {
                IsSuccess = false,
                DeliveryId = delivery.Id,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLower()}";
    }

    #endregion
}
