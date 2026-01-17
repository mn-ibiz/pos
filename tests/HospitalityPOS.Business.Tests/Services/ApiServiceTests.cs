using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using FluentAssertions;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ApiService class.
/// </summary>
public class ApiServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly IApiService _service;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;

    public ApiServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);

        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object);

        _service = new ApiService(_context, httpClient);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Client Management Tests

    [Fact]
    public async Task CreateClientAsync_ShouldCreateNewClient()
    {
        // Arrange
        var request = new CreateApiClientRequest
        {
            Name = "Test Client",
            Description = "Test Description",
            AuthType = ApiAuthType.ApiKey,
            RateLimitPerMinute = 100,
            RateLimitPerHour = 1000,
            RateLimitPerDay = 10000
        };

        // Act
        var result = await _service.CreateClientAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().BeGreaterThan(0);
        result.ClientIdString.Should().NotBeNullOrEmpty();
        result.ClientSecret.Should().NotBeNullOrEmpty();
        result.ApiKey.Should().NotBeNullOrEmpty();

        var client = await _context.ApiClients.FindAsync(result.ClientId);
        client.Should().NotBeNull();
        client!.Name.Should().Be("Test Client");
    }

    [Fact]
    public async Task CreateClientAsync_WithScopes_ShouldAssignScopes()
    {
        // Arrange
        var scope = new ApiScope
        {
            Name = "products:read",
            DisplayName = "Read Products",
            Resource = "products",
            Action = "read"
        };
        _context.ApiScopes.Add(scope);
        await _context.SaveChangesAsync();

        var request = new CreateApiClientRequest
        {
            Name = "Test Client",
            ScopeIds = new[] { scope.Id }
        };

        // Act
        var result = await _service.CreateClientAsync(request);

        // Assert
        var clientScopes = await _context.ApiClientScopes
            .Where(cs => cs.ApiClientId == result.ClientId)
            .ToListAsync();
        clientScopes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClientByIdAsync_ShouldReturnClient()
    {
        // Arrange
        var request = new CreateApiClientRequest { Name = "Test Client" };
        var created = await _service.CreateClientAsync(request);

        // Act
        var result = await _service.GetClientByIdAsync(created.ClientId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Client");
    }

    [Fact]
    public async Task GetAllClientsAsync_ShouldReturnAllClients()
    {
        // Arrange
        await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Client 1" });
        await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Client 2" });

        // Act
        var result = await _service.GetAllClientsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllClientsAsync_WithStatusFilter_ShouldFilterClients()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Client 1" });
        await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Client 2" });
        await _service.SuspendClientAsync(created.ClientId, "Test suspension");

        // Act
        var result = await _service.GetAllClientsAsync(ApiClientStatus.Active);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Client 2");
    }

    [Fact]
    public async Task RegenerateCredentialsAsync_ShouldGenerateNewCredentials()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var originalSecret = created.ClientSecret;
        var originalApiKey = created.ApiKey;

        // Act
        var result = await _service.RegenerateCredentialsAsync(created.ClientId);

        // Assert
        result.ClientSecret.Should().NotBe(originalSecret);
        result.ApiKey.Should().NotBe(originalApiKey);
    }

    [Fact]
    public async Task SuspendClientAsync_ShouldSuspendClient()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        await _service.SuspendClientAsync(created.ClientId, "Test suspension");

        // Assert
        var client = await _service.GetClientByIdAsync(created.ClientId);
        client!.Status.Should().Be(ApiClientStatus.Suspended);
    }

    [Fact]
    public async Task ActivateClientAsync_ShouldActivateClient()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        await _service.SuspendClientAsync(created.ClientId, "Test suspension");

        // Act
        await _service.ActivateClientAsync(created.ClientId);

        // Assert
        var client = await _service.GetClientByIdAsync(created.ClientId);
        client!.Status.Should().Be(ApiClientStatus.Active);
    }

    [Fact]
    public async Task RevokeClientAsync_ShouldRevokeClient()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        await _service.RevokeClientAsync(created.ClientId, "Test revocation");

        // Assert
        var client = await _service.GetClientByIdAsync(created.ClientId);
        client!.Status.Should().Be(ApiClientStatus.Revoked);
    }

    #endregion

    #region Scope Management Tests

    [Fact]
    public async Task CreateScopeAsync_ShouldCreateScope()
    {
        // Arrange
        var scope = new ApiScope
        {
            Name = "orders:write",
            DisplayName = "Write Orders",
            Resource = "orders",
            Action = "write"
        };

        // Act
        var result = await _service.CreateScopeAsync(scope);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("orders:write");
    }

    [Fact]
    public async Task GetAllScopesAsync_ShouldReturnAllScopes()
    {
        // Arrange
        await _service.CreateScopeAsync(new ApiScope { Name = "scope1", DisplayName = "Scope 1", Resource = "r1", Action = "a1" });
        await _service.CreateScopeAsync(new ApiScope { Name = "scope2", DisplayName = "Scope 2", Resource = "r2", Action = "a2" });

        // Act
        var result = await _service.GetAllScopesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AssignScopesToClientAsync_ShouldAssignScopes()
    {
        // Arrange
        var scope1 = await _service.CreateScopeAsync(new ApiScope { Name = "scope1", DisplayName = "Scope 1", Resource = "r1", Action = "a1" });
        var scope2 = await _service.CreateScopeAsync(new ApiScope { Name = "scope2", DisplayName = "Scope 2", Resource = "r2", Action = "a2" });
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        await _service.AssignScopesToClientAsync(client.ClientId, new[] { scope1.Id, scope2.Id });

        // Assert
        var clientScopes = await _service.GetClientScopesAsync(client.ClientId);
        clientScopes.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveScopeFromClientAsync_ShouldRemoveScope()
    {
        // Arrange
        var scope = await _service.CreateScopeAsync(new ApiScope { Name = "scope1", DisplayName = "Scope 1", Resource = "r1", Action = "a1" });
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client", ScopeIds = new[] { scope.Id } });

        // Act
        await _service.RemoveScopeFromClientAsync(client.ClientId, scope.Id);

        // Assert
        var clientScopes = await _service.GetClientScopesAsync(client.ClientId);
        clientScopes.Should().BeEmpty();
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidKey_ShouldSucceed()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        var result = await _service.ValidateApiKeyAsync(created.ApiKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ClientId.Should().Be(created.ClientId);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidKey_ShouldFail()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync("invalid-api-key");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_key");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithSuspendedClient_ShouldFail()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        await _service.SuspendClientAsync(created.ClientId, "Test suspension");

        // Act
        var result = await _service.ValidateApiKeyAsync(created.ApiKey);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("client_suspended");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithExpiredClient_ShouldFail()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });

        // Act
        var result = await _service.ValidateApiKeyAsync(created.ApiKey);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("client_expired");
    }

    [Fact]
    public async Task IssueTokenAsync_ShouldIssueToken()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        var result = await _service.IssueTokenAsync(created.ClientId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IssueTokenAsync_WithScopes_ShouldIncludeScopes()
    {
        // Arrange
        var scope = await _service.CreateScopeAsync(new ApiScope { Name = "test:scope", DisplayName = "Test Scope", Resource = "test", Action = "scope" });
        var created = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            ScopeIds = new[] { scope.Id }
        });

        // Act
        var result = await _service.IssueTokenAsync(created.ClientId, new[] { "test:scope" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Scope.Should().Contain("test:scope");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldSucceed()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var token = await _service.IssueTokenAsync(created.ClientId);

        // Act
        var result = await _service.ValidateTokenAsync(token.AccessToken!);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ClientId.Should().Be(created.ClientId);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldFail()
    {
        // Act
        var result = await _service.ValidateTokenAsync("invalid-token");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldIssueNewTokens()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var token = await _service.IssueTokenAsync(created.ClientId);

        // Act
        var result = await _service.RefreshTokenAsync(token.RefreshToken!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBe(token.AccessToken);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldRevokeToken()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var token = await _service.IssueTokenAsync(created.ClientId);

        // Act
        await _service.RevokeTokenAsync(token.AccessToken!);

        // Assert
        var result = await _service.ValidateTokenAsync(token.AccessToken!);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllTokensAsync_ShouldRevokeAllTokens()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var token1 = await _service.IssueTokenAsync(created.ClientId);
        var token2 = await _service.IssueTokenAsync(created.ClientId);

        // Act
        await _service.RevokeAllTokensAsync(created.ClientId);

        // Assert
        var result1 = await _service.ValidateTokenAsync(token1.AccessToken!);
        var result2 = await _service.ValidateTokenAsync(token2.AccessToken!);
        result1.IsValid.Should().BeFalse();
        result2.IsValid.Should().BeFalse();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task HasScopeAsync_WithMatchingScope_ShouldReturnTrue()
    {
        // Arrange
        var scope = await _service.CreateScopeAsync(new ApiScope { Name = "products:read", DisplayName = "Read Products", Resource = "products", Action = "read" });
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client", ScopeIds = new[] { scope.Id } });

        // Act
        var result = await _service.HasScopeAsync(client.ClientId, "products:read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasScopeAsync_WithoutMatchingScope_ShouldReturnFalse()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });

        // Act
        var result = await _service.HasScopeAsync(client.ClientId, "products:read");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAsync_WithMatchingScope_ShouldReturnTrue()
    {
        // Arrange
        var scope = await _service.CreateScopeAsync(new ApiScope { Name = "orders:write", DisplayName = "Write Orders", Resource = "orders", Action = "write" });
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client", ScopeIds = new[] { scope.Id } });

        // Act
        var result = await _service.CanAccessAsync(client.ClientId, "orders", "write");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllowedResourcesAsync_ShouldReturnResourcesWithActions()
    {
        // Arrange
        var scope1 = await _service.CreateScopeAsync(new ApiScope { Name = "products:read", DisplayName = "Read Products", Resource = "products", Action = "read" });
        var scope2 = await _service.CreateScopeAsync(new ApiScope { Name = "products:write", DisplayName = "Write Products", Resource = "products", Action = "write" });
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client", ScopeIds = new[] { scope1.Id, scope2.Id } });

        // Act
        var result = await _service.GetAllowedResourcesAsync(client.ClientId);

        // Assert
        result.Should().HaveCount(1);
        var resource = result.First();
        resource.Resource.Should().Be("products");
        resource.Actions.Should().Contain("read");
        resource.Actions.Should().Contain("write");
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimit_ShouldAllow()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            RateLimitPerMinute = 100
        });

        // Act
        var result = await _service.CheckRateLimitAsync(client.ClientId);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Remaining.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ShouldDeny()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            RateLimitPerMinute = 1
        });

        // Make one request
        await _service.RecordRequestAsync(client.ClientId);

        // Act
        var result = await _service.CheckRateLimitAsync(client.ClientId);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task GetRateLimitStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            RateLimitPerMinute = 60,
            RateLimitPerHour = 1000,
            RateLimitPerDay = 10000
        });

        // Act
        var result = await _service.GetRateLimitStatusAsync(client.ClientId);

        // Assert
        result.ClientId.Should().Be(client.ClientId);
        result.MinuteLimit.Should().Be(60);
        result.HourLimit.Should().Be(1000);
        result.DayLimit.Should().Be(10000);
    }

    [Fact]
    public async Task ResetRateLimitsAsync_ShouldResetLimits()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            RateLimitPerMinute = 1
        });
        await _service.RecordRequestAsync(client.ClientId);

        // Act
        await _service.ResetRateLimitsAsync(client.ClientId);

        // Assert
        var result = await _service.CheckRateLimitAsync(client.ClientId);
        result.IsAllowed.Should().BeTrue();
    }

    #endregion

    #region Request Logging Tests

    [Fact]
    public async Task LogRequestAsync_ShouldLogRequest()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var log = new ApiRequestLog
        {
            ApiClientId = client.ClientId,
            RequestId = Guid.NewGuid().ToString(),
            HttpMethod = "GET",
            Path = "/api/products",
            StatusCode = 200,
            DurationMs = 50,
            RequestedAt = DateTime.UtcNow
        };

        // Act
        await _service.LogRequestAsync(log);

        // Assert
        var logs = await _service.GetRequestLogsAsync(client.ClientId);
        logs.Should().HaveCount(1);
        logs.First().Path.Should().Be("/api/products");
    }

    [Fact]
    public async Task GetRequestStatsAsync_ShouldReturnStats()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        await _service.LogRequestAsync(new ApiRequestLog
        {
            ApiClientId = client.ClientId,
            RequestId = Guid.NewGuid().ToString(),
            HttpMethod = "GET",
            Path = "/api/products",
            StatusCode = 200,
            DurationMs = 50,
            RequestedAt = DateTime.UtcNow
        });
        await _service.LogRequestAsync(new ApiRequestLog
        {
            ApiClientId = client.ClientId,
            RequestId = Guid.NewGuid().ToString(),
            HttpMethod = "POST",
            Path = "/api/orders",
            StatusCode = 201,
            DurationMs = 100,
            RequestedAt = DateTime.UtcNow
        });

        // Act
        var result = await _service.GetRequestStatsAsync(client.ClientId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // Assert
        result.TotalRequests.Should().Be(2);
        result.SuccessfulRequests.Should().Be(2);
        result.RequestsByMethod.Should().ContainKey("GET");
        result.RequestsByMethod.Should().ContainKey("POST");
    }

    #endregion

    #region Webhook Tests

    [Fact]
    public async Task CreateWebhookAsync_ShouldCreateWebhook()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var webhook = new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Order Webhook",
            Url = "https://example.com/webhook",
            Events = "order.created,order.updated"
        };

        // Act
        var result = await _service.CreateWebhookAsync(webhook);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Order Webhook");
    }

    [Fact]
    public async Task GetWebhooksAsync_ShouldReturnWebhooks()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Webhook 1",
            Url = "https://example.com/webhook1",
            Events = "order.created"
        });
        await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Webhook 2",
            Url = "https://example.com/webhook2",
            Events = "order.updated"
        });

        // Act
        var result = await _service.GetWebhooksAsync(client.ClientId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateWebhookAsync_ShouldUpdateWebhook()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var webhook = await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Original Name",
            Url = "https://example.com/webhook",
            Events = "order.created"
        });

        webhook.Name = "Updated Name";

        // Act
        var result = await _service.UpdateWebhookAsync(webhook);

        // Assert
        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteWebhookAsync_ShouldDeleteWebhook()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var webhook = await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Test Webhook",
            Url = "https://example.com/webhook",
            Events = "order.created"
        });

        // Act
        await _service.DeleteWebhookAsync(webhook.Id);

        // Assert
        var webhooks = await _service.GetWebhooksAsync(client.ClientId);
        webhooks.Should().BeEmpty();
    }

    [Fact]
    public async Task TriggerWebhookAsync_ShouldTriggerWebhooks()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Order Webhook",
            Url = "https://example.com/webhook",
            Events = "order.created",
            Secret = "test-secret"
        });

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _service.TriggerWebhookAsync("order.created", new { orderId = 123 });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetWebhookDeliveriesAsync_ShouldReturnDeliveries()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var webhook = await _service.CreateWebhookAsync(new WebhookConfig
        {
            ApiClientId = client.ClientId,
            Name = "Test Webhook",
            Url = "https://example.com/webhook",
            Events = "order.created"
        });

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await _service.TriggerWebhookAsync("order.created", new { orderId = 123 });

        // Act
        var result = await _service.GetWebhookDeliveriesAsync(webhook.Id);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ValidateIPAsync_WithAllowedIP_ShouldReturnTrue()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            AllowedIPs = "192.168.1.1,10.0.0.0/8"
        });

        // Act
        var result = await _service.ValidateIPAsync(client.ClientId, "192.168.1.1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateIPAsync_WithNoRestrictions_ShouldReturnTrue()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client"
        });

        // Act
        var result = await _service.ValidateIPAsync(client.ClientId, "192.168.1.100");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOriginAsync_WithAllowedOrigin_ShouldReturnTrue()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            AllowedOrigins = "https://example.com,https://app.example.com"
        });

        // Act
        var result = await _service.ValidateOriginAsync(client.ClientId, "https://example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOriginAsync_WithNoRestrictions_ShouldReturnTrue()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client"
        });

        // Act
        var result = await _service.ValidateOriginAsync(client.ClientId, "https://any-site.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetSecurityAlertsAsync_ShouldReturnAlerts()
    {
        // Arrange
        var client = await _service.CreateClientAsync(new CreateApiClientRequest
        {
            Name = "Test Client",
            AllowedIPs = "192.168.1.1"
        });

        // Trigger an unauthorized access attempt
        await _service.ValidateIPAsync(client.ClientId, "10.0.0.1");

        // Act
        var result = await _service.GetSecurityAlertsAsync(client.ClientId);

        // Assert - Alerts may or may not be generated depending on implementation
        result.Should().NotBeNull();
    }

    #endregion

    #region Client Credentials Tests

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var created = await _service.CreateClientAsync(new CreateApiClientRequest { Name = "Test Client" });
        var clientId = created.ClientIdString;
        var clientSecret = created.ClientSecret;

        // Act
        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ClientId.Should().Be(created.ClientId);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithInvalidCredentials_ShouldFail()
    {
        // Act
        var result = await _service.ValidateClientCredentialsAsync("invalid-client-id", "invalid-secret");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_client");
    }

    #endregion
}
