using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// M-Pesa Daraja API configuration.
/// </summary>
public class MpesaConfiguration : BaseEntity
{
    /// <summary>
    /// Configuration name/label.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Environment (Sandbox or Production).
    /// </summary>
    public MpesaEnvironment Environment { get; set; } = MpesaEnvironment.Sandbox;

    /// <summary>
    /// Consumer Key from Daraja portal.
    /// </summary>
    public string ConsumerKey { get; set; } = string.Empty;

    /// <summary>
    /// Consumer Secret from Daraja portal.
    /// </summary>
    public string ConsumerSecret { get; set; } = string.Empty;

    /// <summary>
    /// Business Short Code (Paybill or Till number).
    /// </summary>
    public string BusinessShortCode { get; set; } = string.Empty;

    /// <summary>
    /// Passkey for Lipa Na M-Pesa Online.
    /// </summary>
    public string Passkey { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type (PayBill or Buy Goods).
    /// </summary>
    public MpesaTransactionType TransactionType { get; set; } = MpesaTransactionType.CustomerBuyGoodsOnline;

    /// <summary>
    /// Callback URL for payment notifications.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// API base URL (changes between sandbox and production).
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://sandbox.safaricom.co.ke";

    /// <summary>
    /// Account reference prefix for transactions.
    /// </summary>
    public string AccountReferencePrefix { get; set; } = "POS";

    /// <summary>
    /// Default transaction description.
    /// </summary>
    public string DefaultDescription { get; set; } = "Payment for goods";

    /// <summary>
    /// Whether this is the active configuration.
    /// </summary>
    public new bool IsActive { get; set; }

    /// <summary>
    /// Last successful API call timestamp.
    /// </summary>
    public DateTime? LastSuccessfulCall { get; set; }

    /// <summary>
    /// Cached access token (short-lived).
    /// </summary>
    public string? CachedAccessToken { get; set; }

    /// <summary>
    /// Token expiry time.
    /// </summary>
    public DateTime? TokenExpiry { get; set; }
}
