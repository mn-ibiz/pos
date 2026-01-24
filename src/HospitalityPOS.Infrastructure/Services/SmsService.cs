using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// SMS service implementation with Africa's Talking integration.
/// Supports sandbox mode for testing and production mode for live SMS delivery.
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    // Africa's Talking API endpoints
    private const string AT_LIVE_URL = "https://api.africastalking.com/version1/messaging";
    private const string AT_SANDBOX_URL = "https://api.sandbox.africastalking.com/version1/messaging";

    /// <summary>
    /// Default business name if not configured.
    /// </summary>
    private const string DEFAULT_BUSINESS_NAME = "HospitalityPOS";

    public SmsService(
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendWelcomeSmsAsync(
        string phoneNumber,
        string? memberName,
        string membershipNumber,
        decimal pointsBalance,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);
        var businessName = config?.BusinessName ?? DEFAULT_BUSINESS_NAME;

        var greeting = string.IsNullOrWhiteSpace(memberName) ? "Welcome" : $"Hi {memberName}";
        var message = $"{greeting}! You've joined {businessName} loyalty program. " +
                      $"Your member ID: {membershipNumber}. " +
                      $"Points balance: {pointsBalance:N0}. " +
                      $"Earn points on every purchase!";

        return await SendSmsAsync(phoneNumber, message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return SmsResult.Failure("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return SmsResult.Failure("Message is required.");
        }

        // Normalize phone number to international format
        phoneNumber = NormalizePhoneNumber(phoneNumber);

        // Truncate message if too long (SMS limit is typically 160 chars for single message)
        if (message.Length > 160)
        {
            message = message[..157] + "...";
        }

        var config = await GetConfigurationAsync(cancellationToken);

        // Check if SMS is configured and enabled
        if (config == null || !config.IsEnabled)
        {
            _logger.LogInformation(
                "[SMS DEV MODE] To: {PhoneNumber}, Message: {Message}",
                phoneNumber,
                message);

            // Simulate async operation
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            return SmsResult.Success($"DEV-{Guid.NewGuid():N}");
        }

        // Check daily rate limit
        if (!await CheckAndUpdateDailyLimitAsync(config.Id, cancellationToken))
        {
            _logger.LogWarning("SMS daily limit ({Limit}) reached", config.DailyLimit);
            return SmsResult.Failure("Daily SMS limit reached. Please try again tomorrow.");
        }

        // Send via the configured provider
        return config.Provider switch
        {
            SmsProviders.AfricasTalking => await SendViaAfricasTalkingAsync(config, phoneNumber, message, cancellationToken),
            SmsProviders.Twilio => await SendViaTwilioAsync(config, phoneNumber, message, cancellationToken),
            _ => await SendViaAfricasTalkingAsync(config, phoneNumber, message, cancellationToken)
        };
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendPointsEarnedSmsAsync(
        string phoneNumber,
        decimal pointsEarned,
        decimal newBalance,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);
        var businessName = config?.BusinessName ?? DEFAULT_BUSINESS_NAME;

        var message = $"{businessName}: You've earned {pointsEarned:N0} points! " +
                      $"New balance: {newBalance:N0} points. " +
                      $"Thank you for shopping with us!";

        return await SendSmsAsync(phoneNumber, message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendPointsRedeemedSmsAsync(
        string phoneNumber,
        decimal pointsRedeemed,
        decimal amountSaved,
        decimal remainingBalance,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);
        var businessName = config?.BusinessName ?? DEFAULT_BUSINESS_NAME;

        var message = $"{businessName}: You redeemed {pointsRedeemed:N0} points and saved KES {amountSaved:N0}! " +
                      $"Remaining: {remainingBalance:N0} points.";

        return await SendSmsAsync(phoneNumber, message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool IsConfigured()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();
        var config = context.SmsConfigurations.FirstOrDefault();
        return config?.IsEnabled == true && !string.IsNullOrEmpty(config.ApiKey);
    }

    /// <summary>
    /// Sends SMS via Africa's Talking API.
    /// </summary>
    private async Task<SmsResult> SendViaAfricasTalkingAsync(
        SmsConfiguration config,
        string phoneNumber,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = config.UseSandbox ? AT_SANDBOX_URL : AT_LIVE_URL;
            var client = _httpClientFactory.CreateClient("SmsApi");

            // Prepare form data
            var formData = new Dictionary<string, string>
            {
                ["username"] = config.Username,
                ["to"] = phoneNumber,
                ["message"] = message
            };

            // Add sender ID if configured
            if (!string.IsNullOrWhiteSpace(config.SenderId))
            {
                formData["from"] = config.SenderId;
            }

            var content = new FormUrlEncodedContent(formData);

            // Set headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", config.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("Sending SMS via Africa's Talking to {PhoneNumber} (sandbox: {Sandbox})",
                phoneNumber, config.UseSandbox);

            var response = await client.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AfricasTalkingResponse>(responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var recipient = result?.SMSMessageData?.Recipients?.FirstOrDefault();
                var statusCode = recipient?.StatusCode;
                var messageId = recipient?.MessageId ?? $"AT-{Guid.NewGuid():N}";

                // Status codes: 100 = processed, 101 = sent, 102 = queued
                if (statusCode == "100" || statusCode == "101" || statusCode == "102")
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}, MessageId: {MessageId}, Cost: {Cost}",
                        phoneNumber, messageId, recipient?.Cost);
                    return SmsResult.Success(messageId);
                }

                // Other status codes indicate failure
                var errorStatus = recipient?.Status ?? "Unknown error";
                _logger.LogWarning("SMS delivery failed to {PhoneNumber}: {Status}", phoneNumber, errorStatus);
                return SmsResult.Failure($"SMS delivery failed: {errorStatus}");
            }

            _logger.LogError("Africa's Talking API error for {PhoneNumber}: {StatusCode} - {Response}",
                phoneNumber, response.StatusCode, responseBody);
            return SmsResult.Failure($"SMS API error: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending SMS to {PhoneNumber}", phoneNumber);
            return SmsResult.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SMS to {PhoneNumber}", phoneNumber);
            return SmsResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends SMS via Twilio API (placeholder for future implementation).
    /// </summary>
    private async Task<SmsResult> SendViaTwilioAsync(
        SmsConfiguration config,
        string phoneNumber,
        string message,
        CancellationToken cancellationToken)
    {
        // Twilio integration placeholder
        _logger.LogWarning("Twilio integration not yet implemented, falling back to Africa's Talking");
        return await SendViaAfricasTalkingAsync(config, phoneNumber, message, cancellationToken);
    }

    /// <summary>
    /// Normalizes phone number to international format (254XXXXXXXXX for Kenya).
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove spaces, dashes, and other formatting
        phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Convert 07XXXXXXXX to 254XXXXXXXXX
        if (phoneNumber.StartsWith("0") && phoneNumber.Length == 10)
        {
            phoneNumber = "254" + phoneNumber[1..];
        }

        // Add + prefix if not present (some APIs prefer it)
        if (!phoneNumber.StartsWith("+"))
        {
            phoneNumber = "+" + phoneNumber;
        }

        return phoneNumber;
    }

    /// <summary>
    /// Gets the SMS configuration from database.
    /// </summary>
    private async Task<SmsConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();
        return await context.SmsConfigurations.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Checks and updates the daily SMS limit.
    /// </summary>
    private async Task<bool> CheckAndUpdateDailyLimitAsync(int configId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var config = await context.SmsConfigurations.FindAsync(new object[] { configId }, cancellationToken);
        if (config == null) return false;

        var today = DateTime.Today;

        // Reset counter if it's a new day
        if (config.LastResetDate?.Date != today)
        {
            config.TodayCount = 0;
            config.LastResetDate = today;
        }

        // Check if limit reached
        if (config.TodayCount >= config.DailyLimit)
        {
            return false;
        }

        // Increment counter
        config.TodayCount++;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Tests the SMS configuration by sending a test message.
    /// </summary>
    public async Task<SmsResult> TestConfigurationAsync(
        string testPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        if (config == null)
        {
            return SmsResult.Failure("SMS configuration not found.");
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return SmsResult.Failure("API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(config.Username))
        {
            return SmsResult.Failure("Username is not configured.");
        }

        var testMessage = $"Test SMS from {config.BusinessName}. Configuration verified successfully.";

        // Temporarily enable for test
        var wasEnabled = config.IsEnabled;

        try
        {
            // For the test, we directly call the provider
            var result = config.Provider switch
            {
                SmsProviders.AfricasTalking => await SendViaAfricasTalkingAsync(
                    new SmsConfiguration
                    {
                        Provider = config.Provider,
                        Username = config.Username,
                        ApiKey = config.ApiKey,
                        SenderId = config.SenderId,
                        UseSandbox = config.UseSandbox,
                        IsEnabled = true,
                        BusinessName = config.BusinessName
                    },
                    testPhoneNumber, testMessage, cancellationToken),
                _ => await SendViaAfricasTalkingAsync(
                    new SmsConfiguration
                    {
                        Provider = config.Provider,
                        Username = config.Username,
                        ApiKey = config.ApiKey,
                        SenderId = config.SenderId,
                        UseSandbox = config.UseSandbox,
                        IsEnabled = true,
                        BusinessName = config.BusinessName
                    },
                    testPhoneNumber, testMessage, cancellationToken)
            };

            // Update test status in database
            await UpdateTestStatusAsync(config.Id, result.IsSuccess, result.ErrorMessage, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await UpdateTestStatusAsync(config.Id, false, ex.Message, cancellationToken);
            return SmsResult.Failure($"Test error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the test status in the database.
    /// </summary>
    private async Task UpdateTestStatusAsync(int configId, bool success, string? errorMessage, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var config = await context.SmsConfigurations.FindAsync(new object[] { configId }, cancellationToken);
        if (config != null)
        {
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccessful = success;
            config.LastTestError = success ? null : errorMessage;
            config.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
