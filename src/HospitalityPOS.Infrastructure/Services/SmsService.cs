using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// SMS service implementation.
/// In development mode, logs SMS messages instead of sending.
/// For production, integrate with Africa's Talking, Twilio, or similar SMS provider.
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly bool _isConfigured;

    /// <summary>
    /// Business name to use in SMS messages.
    /// </summary>
    private const string BUSINESS_NAME = "HospitalityPOS";

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // TODO: Check actual SMS provider configuration
        _isConfigured = false; // Set to true when SMS provider is configured
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendWelcomeSmsAsync(
        string phoneNumber,
        string? memberName,
        string membershipNumber,
        decimal pointsBalance,
        CancellationToken cancellationToken = default)
    {
        var greeting = string.IsNullOrWhiteSpace(memberName) ? "Welcome" : $"Hi {memberName}";
        var message = $"{greeting}! You've joined {BUSINESS_NAME} loyalty program. " +
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

        // Truncate message if too long (SMS limit is typically 160 chars for single message)
        if (message.Length > 160)
        {
            message = message[..157] + "...";
        }

        // Development mode: Log the SMS instead of sending
        if (!_isConfigured)
        {
            _logger.LogInformation(
                "[SMS DEV MODE] To: {PhoneNumber}, Message: {Message}",
                phoneNumber,
                message);

            // Simulate async operation
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            return SmsResult.Success($"DEV-{Guid.NewGuid():N}");
        }

        // TODO: Implement actual SMS sending via provider (Africa's Talking, Twilio, etc.)
        // Example Africa's Talking integration:
        // var client = new AfricasTalkingGateway(username, apiKey);
        // var result = await client.SendSmsAsync(phoneNumber, message);

        try
        {
            // Placeholder for actual SMS provider integration
            _logger.LogInformation(
                "Sending SMS to {PhoneNumber}: {Message}",
                phoneNumber,
                message);

            // Simulate API call
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

            var messageId = $"SMS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
            _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}", messageId);

            return SmsResult.Success(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return SmsResult.Failure($"Failed to send SMS: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<SmsResult> SendPointsEarnedSmsAsync(
        string phoneNumber,
        decimal pointsEarned,
        decimal newBalance,
        CancellationToken cancellationToken = default)
    {
        var message = $"{BUSINESS_NAME}: You've earned {pointsEarned:N0} points! " +
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
        var message = $"{BUSINESS_NAME}: You redeemed {pointsRedeemed:N0} points and saved KES {amountSaved:N0}! " +
                      $"Remaining: {remainingBalance:N0} points.";

        return await SendSmsAsync(phoneNumber, message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool IsConfigured()
    {
        return _isConfigured;
    }
}
