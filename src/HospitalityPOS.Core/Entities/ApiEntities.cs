namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of an API client.
/// </summary>
public enum ApiClientStatus
{
    /// <summary>Client is active.</summary>
    Active = 1,
    /// <summary>Client is suspended.</summary>
    Suspended = 2,
    /// <summary>Client is revoked/disabled.</summary>
    Revoked = 3
}

/// <summary>
/// Type of API authentication.
/// </summary>
public enum ApiAuthType
{
    /// <summary>API Key authentication.</summary>
    ApiKey = 1,
    /// <summary>OAuth 2.0 Bearer token.</summary>
    OAuth2 = 2,
    /// <summary>JWT token.</summary>
    JWT = 3,
    /// <summary>Basic authentication.</summary>
    Basic = 4
}

/// <summary>
/// API client/application registration.
/// </summary>
public class ApiClient : BaseEntity
{
    /// <summary>
    /// Client name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Client ID (public identifier).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (hashed).
    /// </summary>
    public string ClientSecretHash { get; set; } = string.Empty;

    /// <summary>
    /// API key hash for the client (never store plain text API keys).
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Authentication type.
    /// </summary>
    public ApiAuthType AuthType { get; set; } = ApiAuthType.ApiKey;

    /// <summary>
    /// Client status.
    /// </summary>
    public ApiClientStatus Status { get; set; } = ApiClientStatus.Active;

    /// <summary>
    /// Allowed IP addresses (comma-separated, null for any).
    /// </summary>
    public string? AllowedIPs { get; set; }

    /// <summary>
    /// Allowed origins for CORS.
    /// </summary>
    public string? AllowedOrigins { get; set; }

    /// <summary>
    /// Rate limit requests per minute.
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Rate limit requests per hour.
    /// </summary>
    public int RateLimitPerHour { get; set; } = 1000;

    /// <summary>
    /// Rate limit requests per day.
    /// </summary>
    public int RateLimitPerDay { get; set; } = 10000;

    /// <summary>
    /// Expiry date for client credentials.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Associated store ID (null for all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Associated user ID (for user-specific API access).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Last used timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Total request count.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Notes about the client.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? User { get; set; }
    public virtual ICollection<ApiClientScope> Scopes { get; set; } = new List<ApiClientScope>();
    public virtual ICollection<ApiAccessToken> AccessTokens { get; set; } = new List<ApiAccessToken>();
}

/// <summary>
/// API scope/permission for a client.
/// </summary>
public class ApiClientScope : BaseEntity
{
    /// <summary>
    /// Reference to API client.
    /// </summary>
    public int ApiClientId { get; set; }

    /// <summary>
    /// Reference to API scope.
    /// </summary>
    public int ApiScopeId { get; set; }

    // Navigation properties
    public virtual ApiClient ApiClient { get; set; } = null!;
    public virtual ApiScope ApiScope { get; set; } = null!;
}

/// <summary>
/// API scope/permission definition.
/// </summary>
public class ApiScope : BaseEntity
{
    /// <summary>
    /// Scope name (e.g., "products:read", "orders:write").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Scope description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Resource this scope applies to.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action allowed (read, write, delete, admin).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system scope.
    /// </summary>
    public bool IsSystem { get; set; }

    // Navigation properties
    public virtual ICollection<ApiClientScope> ClientScopes { get; set; } = new List<ApiClientScope>();
}

/// <summary>
/// API access token.
/// </summary>
public class ApiAccessToken : BaseEntity
{
    /// <summary>
    /// Reference to API client.
    /// </summary>
    public int ApiClientId { get; set; }

    /// <summary>
    /// Token hash.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token hash.
    /// </summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>
    /// Token type (Bearer, etc.).
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// When the token was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime? RefreshExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When the token was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Client IP that requested the token.
    /// </summary>
    public string? ClientIP { get; set; }

    /// <summary>
    /// User agent of the requesting client.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Scopes granted to this token.
    /// </summary>
    public string? GrantedScopes { get; set; }

    // Navigation properties
    public virtual ApiClient ApiClient { get; set; } = null!;
}

/// <summary>
/// API request log for auditing.
/// </summary>
public class ApiRequestLog : BaseEntity
{
    /// <summary>
    /// Reference to API client.
    /// </summary>
    public int? ApiClientId { get; set; }

    /// <summary>
    /// Request ID for tracing.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method.
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query string.
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Request body (may be truncated).
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Response status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body (may be truncated).
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Request duration in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIP { get; set; }

    /// <summary>
    /// User agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User ID if authenticated.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Store ID for the request.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual ApiClient? ApiClient { get; set; }
    public virtual User? User { get; set; }
}

/// <summary>
/// API rate limit tracking.
/// </summary>
public class ApiRateLimitEntry : BaseEntity
{
    /// <summary>
    /// Reference to API client.
    /// </summary>
    public int ApiClientId { get; set; }

    /// <summary>
    /// Time window type (Minute, Hour, Day).
    /// </summary>
    public string WindowType { get; set; } = string.Empty;

    /// <summary>
    /// Start of the window.
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// Request count in this window.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Last request timestamp.
    /// </summary>
    public DateTime LastRequestAt { get; set; }

    // Navigation properties
    public virtual ApiClient ApiClient { get; set; } = null!;
}

/// <summary>
/// Webhook configuration.
/// </summary>
public class WebhookConfig : BaseEntity
{
    /// <summary>
    /// Reference to API client.
    /// </summary>
    public int ApiClientId { get; set; }

    /// <summary>
    /// Webhook name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secret for signing payloads.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Events to trigger webhook (comma-separated).
    /// </summary>
    public string Events { get; set; } = string.Empty;

    /// <summary>
    /// Whether webhook is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Content type (application/json, etc.).
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// Custom headers as JSON.
    /// </summary>
    public string? Headers { get; set; }

    /// <summary>
    /// Retry count on failure.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Last triggered timestamp.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Success count.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failure count.
    /// </summary>
    public int FailureCount { get; set; }

    // Navigation properties
    public virtual ApiClient ApiClient { get; set; } = null!;
    public virtual ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}

/// <summary>
/// Webhook delivery log.
/// </summary>
public class WebhookDelivery : BaseEntity
{
    /// <summary>
    /// Reference to webhook config.
    /// </summary>
    public int WebhookConfigId { get; set; }

    /// <summary>
    /// Event that triggered the webhook.
    /// </summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Payload sent.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Response status code.
    /// </summary>
    public int? ResponseCode { get; set; }

    /// <summary>
    /// Response body.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Delivery status.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Attempt number.
    /// </summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>
    /// Time taken in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Delivered at timestamp.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    // Navigation properties
    public virtual WebhookConfig WebhookConfig { get; set; } = null!;
}
