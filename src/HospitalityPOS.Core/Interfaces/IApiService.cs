using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing API clients, authentication, and access control.
/// </summary>
public interface IApiService
{
    #region Client Management

    /// <summary>
    /// Creates a new API client.
    /// </summary>
    Task<ApiClientCreatedResult> CreateClientAsync(CreateApiClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API client by ID.
    /// </summary>
    Task<ApiClient?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API client by client ID.
    /// </summary>
    Task<ApiClient?> GetClientByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API clients.
    /// </summary>
    Task<IEnumerable<ApiClient>> GetAllClientsAsync(ApiClientStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an API client.
    /// </summary>
    Task<ApiClient> UpdateClientAsync(ApiClient client, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates client credentials.
    /// </summary>
    Task<ApiClientCreatedResult> RegenerateCredentialsAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends an API client.
    /// </summary>
    Task SuspendClientAsync(int clientId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an API client.
    /// </summary>
    Task ActivateClientAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API client.
    /// </summary>
    Task RevokeClientAsync(int clientId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Scope Management

    /// <summary>
    /// Gets all available API scopes.
    /// </summary>
    Task<IEnumerable<ApiScope>> GetAllScopesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an API scope.
    /// </summary>
    Task<ApiScope> CreateScopeAsync(ApiScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scopes for a client.
    /// </summary>
    Task<IEnumerable<ApiScope>> GetClientScopesAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns scopes to a client.
    /// </summary>
    Task AssignScopesToClientAsync(int clientId, IEnumerable<int> scopeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a scope from a client.
    /// </summary>
    Task RemoveScopeFromClientAsync(int clientId, int scopeId, CancellationToken cancellationToken = default);

    #endregion

    #region Authentication

    /// <summary>
    /// Validates API key authentication.
    /// </summary>
    Task<AuthenticationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates client credentials.
    /// </summary>
    Task<AuthenticationResult> ValidateClientCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues an access token.
    /// </summary>
    Task<TokenResult> IssueTokenAsync(int clientId, IEnumerable<string>? scopes = null, string? clientIP = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token.
    /// </summary>
    Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token.
    /// </summary>
    Task<TokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a token.
    /// </summary>
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a client.
    /// </summary>
    Task RevokeAllTokensAsync(int clientId, CancellationToken cancellationToken = default);

    #endregion

    #region Authorization

    /// <summary>
    /// Checks if a client has a specific scope.
    /// </summary>
    Task<bool> HasScopeAsync(int clientId, string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a client has access to a resource action.
    /// </summary>
    Task<bool> CanAccessAsync(int clientId, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets allowed resources for a client.
    /// </summary>
    Task<IEnumerable<ResourceAccess>> GetAllowedResourcesAsync(int clientId, CancellationToken cancellationToken = default);

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Checks if request is allowed under rate limits.
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for rate limiting.
    /// </summary>
    Task RecordRequestAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current rate limit status for a client.
    /// </summary>
    Task<RateLimitStatus> GetRateLimitStatusAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets rate limits for a client.
    /// </summary>
    Task ResetRateLimitsAsync(int clientId, CancellationToken cancellationToken = default);

    #endregion

    #region Request Logging

    /// <summary>
    /// Logs an API request.
    /// </summary>
    Task LogRequestAsync(ApiRequestLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets request logs for a client.
    /// </summary>
    Task<IEnumerable<ApiRequestLog>> GetRequestLogsAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets request statistics.
    /// </summary>
    Task<ApiRequestStats> GetRequestStatsAsync(int? clientId = null, DateTime startDate = default, DateTime endDate = default, CancellationToken cancellationToken = default);

    #endregion

    #region Webhooks

    /// <summary>
    /// Creates a webhook configuration.
    /// </summary>
    Task<WebhookConfig> CreateWebhookAsync(WebhookConfig webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets webhooks for a client.
    /// </summary>
    Task<IEnumerable<WebhookConfig>> GetWebhooksAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a webhook configuration.
    /// </summary>
    Task<WebhookConfig> UpdateWebhookAsync(WebhookConfig webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook configuration.
    /// </summary>
    Task DeleteWebhookAsync(int webhookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a webhook for an event.
    /// </summary>
    Task<WebhookDeliveryResult> TriggerWebhookAsync(string eventName, object payload, int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets webhook delivery history.
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetWebhookDeliveriesAsync(int webhookId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed webhook delivery.
    /// </summary>
    Task<WebhookDeliveryResult> RetryWebhookDeliveryAsync(int deliveryId, CancellationToken cancellationToken = default);

    #endregion

    #region Security

    /// <summary>
    /// Validates IP address is allowed.
    /// </summary>
    Task<bool> ValidateIPAsync(int clientId, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates origin for CORS.
    /// </summary>
    Task<bool> ValidateOriginAsync(int clientId, string origin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security alerts for a client.
    /// </summary>
    Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(int? clientId = null, DateTime? startDate = null, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Request to create an API client.
/// </summary>
public class CreateApiClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApiAuthType AuthType { get; set; } = ApiAuthType.ApiKey;
    public int? StoreId { get; set; }
    public int? UserId { get; set; }
    public string? AllowedIPs { get; set; }
    public string? AllowedOrigins { get; set; }
    public int RateLimitPerMinute { get; set; } = 60;
    public int RateLimitPerHour { get; set; } = 1000;
    public int RateLimitPerDay { get; set; } = 10000;
    public DateTime? ExpiresAt { get; set; }
    public IEnumerable<int>? ScopeIds { get; set; }
}

/// <summary>
/// Result of creating an API client.
/// </summary>
public class ApiClientCreatedResult
{
    public int ClientId { get; set; }
    public string ClientIdString { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Result of authentication.
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public int? ClientId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public ApiClient? Client { get; set; }
}

/// <summary>
/// Token issuance result.
/// </summary>
public class TokenResult
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Token validation result.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public int? ClientId { get; set; }
    public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Resource access information.
/// </summary>
public class ResourceAccess
{
    public string Resource { get; set; } = string.Empty;
    public IEnumerable<string> Actions { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// Rate limit check result.
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string? LimitType { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetAt { get; set; }
    public int RetryAfterSeconds { get; set; }
}

/// <summary>
/// Current rate limit status.
/// </summary>
public class RateLimitStatus
{
    public int ClientId { get; set; }

    public int MinuteLimit { get; set; }
    public int MinuteUsed { get; set; }
    public int MinuteRemaining { get; set; }
    public DateTime MinuteResetAt { get; set; }

    public int HourLimit { get; set; }
    public int HourUsed { get; set; }
    public int HourRemaining { get; set; }
    public DateTime HourResetAt { get; set; }

    public int DayLimit { get; set; }
    public int DayUsed { get; set; }
    public int DayRemaining { get; set; }
    public DateTime DayResetAt { get; set; }
}

/// <summary>
/// API request statistics.
/// </summary>
public class ApiRequestStats
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageDurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public Dictionary<string, int> RequestsByPath { get; set; } = new();
    public Dictionary<string, int> RequestsByMethod { get; set; } = new();
    public Dictionary<int, int> RequestsByStatusCode { get; set; } = new();
    public Dictionary<string, int> RequestsByClient { get; set; } = new();
}

/// <summary>
/// Webhook delivery result.
/// </summary>
public class WebhookDeliveryResult
{
    public bool IsSuccess { get; set; }
    public int DeliveryId { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}

/// <summary>
/// Security alert.
/// </summary>
public class SecurityAlert
{
    public int? ClientId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IPAddress { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Severity { get; set; } = "Warning";
}

#endregion
