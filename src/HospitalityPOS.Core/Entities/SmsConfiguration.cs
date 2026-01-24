using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Configuration for SMS provider integration.
/// Supports Africa's Talking and other SMS providers.
/// </summary>
public class SmsConfiguration : BaseEntity
{
    /// <summary>
    /// The SMS provider name (e.g., "AfricasTalking", "Twilio").
    /// </summary>
    [StringLength(50)]
    public string Provider { get; set; } = "AfricasTalking";

    /// <summary>
    /// API username for the SMS provider.
    /// </summary>
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication (stored encrypted).
    /// </summary>
    [StringLength(500)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Sender ID (alphanumeric or shortcode).
    /// </summary>
    [StringLength(20)]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Whether SMS functionality is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Whether to use sandbox/test mode.
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Business name to use in SMS messages.
    /// </summary>
    [StringLength(100)]
    public string BusinessName { get; set; } = "HospitalityPOS";

    /// <summary>
    /// Maximum SMS messages per day (rate limiting).
    /// </summary>
    public int DailyLimit { get; set; } = 1000;

    /// <summary>
    /// Count of SMS sent today (reset daily).
    /// </summary>
    public int TodayCount { get; set; } = 0;

    /// <summary>
    /// Date when TodayCount was last reset.
    /// </summary>
    public DateTime? LastResetDate { get; set; }

    /// <summary>
    /// Last time configuration was tested.
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of last configuration test.
    /// </summary>
    public bool? LastTestSuccessful { get; set; }

    /// <summary>
    /// Error message from last test (if failed).
    /// </summary>
    [StringLength(500)]
    public string? LastTestError { get; set; }
}

/// <summary>
/// SMS provider options for dropdown selection.
/// </summary>
public static class SmsProviders
{
    public const string AfricasTalking = "AfricasTalking";
    public const string Twilio = "Twilio";
    public const string Infobip = "Infobip";

    public static readonly IReadOnlyList<string> All = new[]
    {
        AfricasTalking,
        Twilio,
        Infobip
    };
}

/// <summary>
/// Response model for Africa's Talking SMS API.
/// </summary>
public class AfricasTalkingResponse
{
    public AfricasTalkingSmsData? SMSMessageData { get; set; }
}

/// <summary>
/// SMS data from Africa's Talking response.
/// </summary>
public class AfricasTalkingSmsData
{
    public string? Message { get; set; }
    public List<AfricasTalkingRecipient>? Recipients { get; set; }
}

/// <summary>
/// Individual recipient result from Africa's Talking.
/// </summary>
public class AfricasTalkingRecipient
{
    public string? StatusCode { get; set; }
    public string? Number { get; set; }
    public string? Status { get; set; }
    public string? Cost { get; set; }
    public string? MessageId { get; set; }
}
