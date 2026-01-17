using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of M-Pesa Daraja API service.
/// </summary>
public partial class MpesaService : IMpesaService
{
    private readonly POSDbContext _context;
    private readonly ILogger<MpesaService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MpesaService(
        POSDbContext context,
        ILogger<MpesaService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    private HttpClient CreateHttpClient() => _httpClientFactory.CreateClient("MpesaApi");

    /// <summary>
    /// Masks a phone number for logging (shows first 3 and last 2 digits).
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 6)
            return "***";
        return $"{phoneNumber[..3]}****{phoneNumber[^2..]}";
    }

    #region Configuration

    public async Task<MpesaConfiguration> SaveConfigurationAsync(MpesaConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            _context.Set<MpesaConfiguration>().Add(config);
        }
        else
        {
            _context.Set<MpesaConfiguration>().Update(config);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<MpesaConfiguration?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaConfiguration>()
            .Where(c => c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MpesaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaConfiguration>()
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ActivateConfigurationAsync(int configId, CancellationToken cancellationToken = default)
    {
        var configs = await _context.Set<MpesaConfiguration>().ToListAsync(cancellationToken);

        foreach (var config in configs)
        {
            config.IsActive = config.Id == configId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TestConfigurationAsync(int configId, CancellationToken cancellationToken = default)
    {
        var config = await _context.Set<MpesaConfiguration>().FindAsync([configId], cancellationToken);
        if (config == null) return false;

        try
        {
            var token = await GetAccessTokenAsync(config, cancellationToken);
            return !string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "M-Pesa configuration test failed: {ConfigId}", configId);
            return false;
        }
    }

    #endregion

    #region STK Push

    public async Task<MpesaStkPushResult> InitiateStkPushAsync(
        string phoneNumber,
        decimal amount,
        string accountReference,
        string description,
        int? receiptId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetActiveConfigurationAsync(cancellationToken);
        if (config == null)
        {
            return new MpesaStkPushResult
            {
                Success = false,
                ErrorMessage = "No active M-Pesa configuration found"
            };
        }

        // Format and validate phone number
        var formattedPhone = FormatPhoneNumber(phoneNumber);
        if (!await ValidatePhoneNumberAsync(formattedPhone))
        {
            return new MpesaStkPushResult
            {
                Success = false,
                ErrorMessage = "Invalid phone number format"
            };
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(config, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new MpesaStkPushResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain access token"
                };
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.BusinessShortCode}{config.Passkey}{timestamp}"));

            var request = new
            {
                BusinessShortCode = config.BusinessShortCode,
                Password = password,
                Timestamp = timestamp,
                TransactionType = config.TransactionType == MpesaTransactionType.CustomerBuyGoodsOnline
                    ? "CustomerBuyGoodsOnline"
                    : "CustomerPayBillOnline",
                Amount = (int)Math.Ceiling(amount),
                PartyA = formattedPhone,
                PartyB = config.BusinessShortCode,
                PhoneNumber = formattedPhone,
                CallBackURL = config.CallbackUrl,
                AccountReference = $"{config.AccountReferencePrefix}{accountReference}",
                TransactionDesc = description.Length > 20 ? description[..20] : description
            };

            // Create request record
            var stkRequest = new MpesaStkPushRequest
            {
                ConfigurationId = config.Id,
                ReceiptId = receiptId,
                PhoneNumber = formattedPhone,
                Amount = amount,
                AccountReference = request.AccountReference,
                TransactionDescription = description,
                Status = MpesaStkStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                RequestJson = JsonSerializer.Serialize(request)
            };

            _context.Set<MpesaStkPushRequest>().Add(stkRequest);
            await _context.SaveChangesAsync(cancellationToken);

            // Make API call with per-request headers (thread-safe)
            using var httpClient = CreateHttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiBaseUrl}/mpesa/stkpush/v1/processrequest");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = JsonContent.Create(request);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            stkRequest.ResponseJson = responseContent;

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<StkPushResponse>(responseContent);

                if (result?.ResponseCode == "0")
                {
                    stkRequest.Status = MpesaStkStatus.Processing;
                    stkRequest.MerchantRequestId = result.MerchantRequestID ?? "";
                    stkRequest.CheckoutRequestId = result.CheckoutRequestID ?? "";
                    stkRequest.ResponseCode = result.ResponseCode;
                    stkRequest.ResponseDescription = result.ResponseDescription;

                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("STK Push initiated: {CheckoutRequestId} for {Phone}",
                        stkRequest.CheckoutRequestId, MaskPhoneNumber(formattedPhone));

                    return new MpesaStkPushResult
                    {
                        Success = true,
                        RequestId = stkRequest.Id,
                        MerchantRequestId = stkRequest.MerchantRequestId,
                        CheckoutRequestId = stkRequest.CheckoutRequestId,
                        ResponseCode = result.ResponseCode,
                        ResponseDescription = result.ResponseDescription,
                        CustomerMessage = result.CustomerMessage
                    };
                }
                else
                {
                    stkRequest.Status = MpesaStkStatus.Failed;
                    stkRequest.ResponseCode = result?.ResponseCode;
                    stkRequest.ResponseDescription = result?.ResponseDescription;

                    await _context.SaveChangesAsync(cancellationToken);

                    return new MpesaStkPushResult
                    {
                        Success = false,
                        ResponseCode = result?.ResponseCode,
                        ErrorMessage = result?.ResponseDescription ?? "STK Push request failed"
                    };
                }
            }
            else
            {
                stkRequest.Status = MpesaStkStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);

                return new MpesaStkPushResult
                {
                    Success = false,
                    ErrorMessage = $"API request failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STK Push initiation failed for {Phone}", MaskPhoneNumber(phoneNumber));
            return new MpesaStkPushResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<MpesaStkPushRequest?> GetStkPushRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaStkPushRequest>()
            .Include(r => r.Configuration)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<MpesaStkPushRequest?> GetStkPushByCheckoutIdAsync(string checkoutRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaStkPushRequest>()
            .FirstOrDefaultAsync(r => r.CheckoutRequestId == checkoutRequestId, cancellationToken);
    }

    public async Task<IReadOnlyList<MpesaStkPushRequest>> GetPendingStkRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaStkPushRequest>()
            .Where(r => r.Status == MpesaStkStatus.Pending || r.Status == MpesaStkStatus.Processing)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Callback Processing

    public async Task ProcessStkCallbackAsync(string callbackJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var callback = JsonSerializer.Deserialize<StkCallback>(callbackJson);
            if (callback?.Body?.StkCallback == null) return;

            var stkData = callback.Body.StkCallback;
            var request = await GetStkPushByCheckoutIdAsync(stkData.CheckoutRequestID, cancellationToken);

            if (request == null)
            {
                _logger.LogWarning("Received callback for unknown checkout: {CheckoutId}", stkData.CheckoutRequestID);
                return;
            }

            request.CallbackReceivedAt = DateTime.UtcNow;
            request.CallbackJson = callbackJson;
            request.ResultCode = stkData.ResultCode.ToString();
            request.ResultDescription = stkData.ResultDesc;

            if (stkData.ResultCode == 0)
            {
                request.Status = MpesaStkStatus.Success;

                // Extract metadata
                if (stkData.CallbackMetadata?.Item != null)
                {
                    foreach (var item in stkData.CallbackMetadata.Item)
                    {
                        switch (item.Name)
                        {
                            case "MpesaReceiptNumber":
                                request.MpesaReceiptNumber = item.Value?.ToString();
                                break;
                            case "TransactionDate":
                                if (DateTime.TryParseExact(item.Value?.ToString(), "yyyyMMddHHmmss",
                                    null, System.Globalization.DateTimeStyles.None, out var date))
                                {
                                    request.TransactionDate = date;
                                }
                                break;
                            case "PhoneNumber":
                                request.PhoneNumberUsed = item.Value?.ToString();
                                break;
                        }
                    }
                }

                // Create transaction record
                var transaction = new MpesaTransaction
                {
                    StkPushRequestId = request.Id,
                    PaymentId = request.PaymentId,
                    MpesaReceiptNumber = request.MpesaReceiptNumber ?? "",
                    Amount = request.Amount,
                    PhoneNumber = request.PhoneNumberUsed ?? request.PhoneNumber,
                    TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
                    Status = MpesaTransactionStatus.Completed,
                    IsManualEntry = false,
                    IsVerified = true
                };

                _context.Set<MpesaTransaction>().Add(transaction);

                _logger.LogInformation("M-Pesa payment successful: {Receipt} for KES {Amount}",
                    request.MpesaReceiptNumber, request.Amount);
            }
            else if (stkData.ResultCode == 1032)
            {
                request.Status = MpesaStkStatus.Cancelled;
                _logger.LogInformation("M-Pesa payment cancelled by user: {CheckoutId}", stkData.CheckoutRequestID);
            }
            else
            {
                request.Status = MpesaStkStatus.Failed;
                _logger.LogWarning("M-Pesa payment failed: {CheckoutId} - {Error}",
                    stkData.CheckoutRequestID, stkData.ResultDesc);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing STK callback");
        }
    }

    #endregion

    #region Transaction Status Query

    public async Task<MpesaQueryResult> QueryTransactionStatusAsync(string checkoutRequestId, CancellationToken cancellationToken = default)
    {
        var request = await GetStkPushByCheckoutIdAsync(checkoutRequestId, cancellationToken);
        if (request == null)
        {
            return new MpesaQueryResult
            {
                Success = false,
                ErrorMessage = "Request not found"
            };
        }

        var config = await _context.Set<MpesaConfiguration>()
            .FindAsync([request.ConfigurationId], cancellationToken);

        if (config == null)
        {
            return new MpesaQueryResult
            {
                Success = false,
                ErrorMessage = "Configuration not found"
            };
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(config, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new MpesaQueryResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain access token"
                };
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.BusinessShortCode}{config.Passkey}{timestamp}"));

            var queryRequest = new
            {
                BusinessShortCode = config.BusinessShortCode,
                Password = password,
                Timestamp = timestamp,
                CheckoutRequestID = checkoutRequestId
            };

            using var httpClient = CreateHttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.ApiBaseUrl}/mpesa/stkpushquery/v1/query");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = JsonContent.Create(queryRequest);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            request.QueryAttempts++;
            request.LastQueryAt = DateTime.UtcNow;

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<StkQueryResponse>(responseContent);

                if (result?.ResultCode == "0")
                {
                    request.Status = MpesaStkStatus.Success;
                    request.ResultCode = result.ResultCode;
                    request.ResultDescription = result.ResultDesc;

                    await _context.SaveChangesAsync(cancellationToken);

                    return new MpesaQueryResult
                    {
                        Success = true,
                        Status = MpesaStkStatus.Success,
                        ResultCode = result.ResultCode,
                        ResultDescription = result.ResultDesc
                    };
                }
                else if (result?.ResultCode == "1032")
                {
                    request.Status = MpesaStkStatus.Cancelled;
                    await _context.SaveChangesAsync(cancellationToken);

                    return new MpesaQueryResult
                    {
                        Success = true,
                        Status = MpesaStkStatus.Cancelled,
                        ResultCode = result.ResultCode,
                        ResultDescription = "Transaction cancelled by user"
                    };
                }
                else
                {
                    request.Status = MpesaStkStatus.Failed;
                    request.ResultCode = result?.ResultCode;
                    request.ResultDescription = result?.ResultDesc;

                    await _context.SaveChangesAsync(cancellationToken);

                    return new MpesaQueryResult
                    {
                        Success = false,
                        Status = MpesaStkStatus.Failed,
                        ResultCode = result?.ResultCode,
                        ErrorMessage = result?.ResultDesc
                    };
                }
            }

            return new MpesaQueryResult
            {
                Success = false,
                Status = MpesaStkStatus.Pending,
                ErrorMessage = "Query failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction status query failed: {CheckoutId}", checkoutRequestId);
            return new MpesaQueryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task QueryPendingTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var pendingRequests = await GetPendingStkRequestsAsync(cancellationToken);
        var timeout = TimeSpan.FromMinutes(5);

        foreach (var request in pendingRequests)
        {
            if (DateTime.UtcNow - request.RequestedAt > timeout)
            {
                request.Status = MpesaStkStatus.Timeout;
                continue;
            }

            if (request.QueryAttempts < 5)
            {
                await QueryTransactionStatusAsync(request.CheckoutRequestId, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Manual Entry

    public async Task<MpesaTransaction> RecordManualTransactionAsync(
        string mpesaReceiptNumber,
        decimal amount,
        string phoneNumber,
        DateTime transactionDate,
        string? notes,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicates
        var existing = await GetTransactionByReceiptNumberAsync(mpesaReceiptNumber, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Transaction with receipt number {mpesaReceiptNumber} already exists");
        }

        var transaction = new MpesaTransaction
        {
            MpesaReceiptNumber = mpesaReceiptNumber.ToUpper(),
            Amount = amount,
            PhoneNumber = FormatPhoneNumber(phoneNumber),
            TransactionDate = transactionDate,
            Status = MpesaTransactionStatus.Completed,
            IsManualEntry = true,
            RecordedByUserId = userId,
            Notes = notes,
            IsVerified = false
        };

        _context.Set<MpesaTransaction>().Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Manual M-Pesa transaction recorded: {Receipt} by User {UserId}",
            mpesaReceiptNumber, userId);

        return transaction;
    }

    public async Task<bool> VerifyTransactionAsync(int transactionId, int verifiedByUserId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Set<MpesaTransaction>()
            .FindAsync([transactionId], cancellationToken);

        if (transaction == null) return false;

        transaction.IsVerified = true;
        transaction.VerifiedByUserId = verifiedByUserId;
        transaction.VerifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Transaction History

    public async Task<MpesaTransaction?> GetTransactionByReceiptNumberAsync(string mpesaReceiptNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaTransaction>()
            .FirstOrDefaultAsync(t => t.MpesaReceiptNumber == mpesaReceiptNumber.ToUpper(), cancellationToken);
    }

    public async Task<IReadOnlyList<MpesaTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaTransaction>()
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MpesaTransaction>> GetUnverifiedTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<MpesaTransaction>()
            .Where(t => t.IsManualEntry && !t.IsVerified)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Validation

    public Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
    {
        // Kenya phone number format: 254XXXXXXXXX (12 digits)
        var formatted = FormatPhoneNumber(phoneNumber);
        return Task.FromResult(PhoneNumberRegex().IsMatch(formatted));
    }

    public string FormatPhoneNumber(string phoneNumber)
    {
        // Remove all non-digits
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle different formats
        if (digits.StartsWith("254") && digits.Length == 12)
            return digits;

        if (digits.StartsWith("0") && digits.Length == 10)
            return "254" + digits[1..];

        // Handle numbers starting with 7 or 1 (Telkom)
        if ((digits.StartsWith("7") || digits.StartsWith("1")) && digits.Length == 9)
            return "254" + digits;

        if (digits.Length == 9)
            return "254" + digits;

        return digits;
    }

    /// <summary>
    /// Validates Kenya mobile phone numbers.
    /// Supports:
    /// - Safaricom: 2547XX... (07XX local)
    /// - Airtel: 25473X, 25475X, 25478X
    /// - Telkom: 25477X (077X local)
    /// - Safaricom (new): 25411X (011X local)
    /// </summary>
    [GeneratedRegex(@"^254(7[0-9]|1[0-1])\d{7}$")]
    private static partial Regex PhoneNumberRegex();

    #endregion

    #region Dashboard

    public async Task<MpesaDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetActiveConfigurationAsync(cancellationToken);
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todayTransactions = await _context.Set<MpesaTransaction>()
            .Where(t => t.TransactionDate.Date == today && t.Status == MpesaTransactionStatus.Completed)
            .ToListAsync(cancellationToken);

        var todayRequests = await _context.Set<MpesaStkPushRequest>()
            .Where(r => r.RequestedAt.Date == today)
            .ToListAsync(cancellationToken);

        var monthTransactions = await _context.Set<MpesaTransaction>()
            .Where(t => t.TransactionDate >= monthStart && t.Status == MpesaTransactionStatus.Completed)
            .ToListAsync(cancellationToken);

        var unverified = await _context.Set<MpesaTransaction>()
            .CountAsync(t => t.IsManualEntry && !t.IsVerified, cancellationToken);

        // Hourly stats for today
        var hourlyStats = todayTransactions
            .GroupBy(t => t.TransactionDate.Hour)
            .Select(g => new MpesaHourlyStats
            {
                Hour = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount)
            })
            .OrderBy(s => s.Hour)
            .ToList();

        return new MpesaDashboardData
        {
            IsConfigured = config != null,
            IsTestMode = config?.Environment == MpesaEnvironment.Sandbox,
            ShortCode = config?.BusinessShortCode,
            LastSuccessfulTransaction = todayTransactions.OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault()?.TransactionDate,

            TodayTransactions = todayTransactions.Count,
            TodayAmount = todayTransactions.Sum(t => t.Amount),
            TodayPending = todayRequests.Count(r => r.Status == MpesaStkStatus.Pending || r.Status == MpesaStkStatus.Processing),
            TodayFailed = todayRequests.Count(r => r.Status == MpesaStkStatus.Failed),

            MonthTransactions = monthTransactions.Count,
            MonthAmount = monthTransactions.Sum(t => t.Amount),

            UnverifiedManualEntries = unverified,
            TodayHourlyStats = hourlyStats
        };
    }

    #endregion

    #region Private Helpers

    private async Task<string?> GetAccessTokenAsync(MpesaConfiguration config, CancellationToken cancellationToken)
    {
        // Check cached token
        if (!string.IsNullOrEmpty(config.CachedAccessToken) &&
            config.TokenExpiry.HasValue &&
            config.TokenExpiry.Value > DateTime.UtcNow.AddMinutes(1))
        {
            return config.CachedAccessToken;
        }

        try
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.ConsumerKey}:{config.ConsumerSecret}"));

            using var httpClient = CreateHttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{config.ApiBaseUrl}/oauth/v1/generate?grant_type=client_credentials");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

                if (tokenResponse != null)
                {
                    config.CachedAccessToken = tokenResponse.AccessToken;
                    config.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
                    config.LastSuccessfulCall = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);

                    return tokenResponse.AccessToken;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain M-Pesa access token");
        }

        return null;
    }

    #endregion

    #region DTOs

    private class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresIn { get; set; }
    }

    private class StkPushResponse
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? CustomerMessage { get; set; }
    }

    private class StkQueryResponse
    {
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResultCode { get; set; }
        public string? ResultDesc { get; set; }
    }

    private class StkCallback
    {
        public StkCallbackBody? Body { get; set; }
    }

    private class StkCallbackBody
    {
        public StkCallbackData? StkCallback { get; set; }
    }

    private class StkCallbackData
    {
        public string MerchantRequestID { get; set; } = "";
        public string CheckoutRequestID { get; set; } = "";
        public int ResultCode { get; set; }
        public string ResultDesc { get; set; } = "";
        public CallbackMetadata? CallbackMetadata { get; set; }
    }

    private class CallbackMetadata
    {
        public List<CallbackItem>? Item { get; set; }
    }

    private class CallbackItem
    {
        public string Name { get; set; } = "";
        public object? Value { get; set; }
    }

    #endregion
}
