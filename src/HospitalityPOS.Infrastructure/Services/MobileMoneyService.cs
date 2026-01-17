using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing mobile money payments (Airtel Money, T-Kash).
/// </summary>
public class MobileMoneyService : IMobileMoneyService
{
    private readonly POSDbContext _context;
    private readonly HttpClient _httpClient;

    // Kenyan phone number patterns
    private static readonly Regex AirtelPattern = new(@"^(?:\+?254|0)?(7[3-9][0-9]|755)[0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex TKashPattern = new(@"^(?:\+?254|0)?(77[0-9])[0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex MpesaPattern = new(@"^(?:\+?254|0)?(7[0-2][0-9]|74[0-8]|757|758|759)[0-9]{6}$", RegexOptions.Compiled);

    public MobileMoneyService(POSDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    #region Airtel Money Configuration

    /// <inheritdoc />
    public async Task<AirtelMoneyConfiguration?> GetAirtelMoneyConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.AirtelMoneyConfigurations
            .Where(c => c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AirtelMoneyConfiguration> SaveAirtelMoneyConfigurationAsync(AirtelMoneyConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration.Id == 0)
        {
            _context.AirtelMoneyConfigurations.Add(configuration);
        }
        else
        {
            _context.AirtelMoneyConfigurations.Update(configuration);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return configuration;
    }

    /// <inheritdoc />
    public async Task<MobileMoneyConnectionTestResult> TestAirtelMoneyConnectionAsync(int configurationId, CancellationToken cancellationToken = default)
    {
        var config = await _context.AirtelMoneyConfigurations.FindAsync(new object[] { configurationId }, cancellationToken).ConfigureAwait(false);
        if (config == null)
        {
            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = false,
                ErrorMessage = "Configuration not found",
                TestedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simulate API connection test
            // In production, this would call the Airtel Money API to get an access token
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            config.LastTestSuccessful = true;
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestError = null;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = true,
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ApiVersion = "v1"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            config.LastTestSuccessful = false;
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestError = ex.Message;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task SetAirtelMoneyEnabledAsync(int configurationId, bool enabled, CancellationToken cancellationToken = default)
    {
        var config = await _context.AirtelMoneyConfigurations.FindAsync(new object[] { configurationId }, cancellationToken).ConfigureAwait(false);
        if (config != null)
        {
            config.IsEnabled = enabled;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Airtel Money Payments

    /// <inheritdoc />
    public async Task<MobileMoneyPaymentResult> InitiateAirtelMoneyPaymentAsync(MobileMoneyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Validate phone number
        var phoneValidation = await ValidatePhoneNumberAsync(request.PhoneNumber, MobileMoneyProvider.AirtelMoney, cancellationToken).ConfigureAwait(false);
        if (!phoneValidation.IsValid)
        {
            return new MobileMoneyPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = phoneValidation.ErrorMessage ?? "Invalid phone number for Airtel Money",
                ErrorCode = "INVALID_PHONE"
            };
        }

        var config = await GetAirtelMoneyConfigurationAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        if (config == null || !config.IsEnabled)
        {
            return new MobileMoneyPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Airtel Money is not configured or enabled",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        var transactionReference = GenerateTransactionReference("AM");

        var airtelRequest = new AirtelMoneyRequest
        {
            ReceiptId = request.ReceiptId,
            StoreId = request.StoreId,
            TransactionReference = transactionReference,
            PhoneNumber = phoneValidation.NormalizedNumber!,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Status = MobileMoneyTransactionStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            UserId = request.UserId
        };

        _context.AirtelMoneyRequests.Add(airtelRequest);

        // Log the transaction
        await LogTransactionAsync(MobileMoneyProvider.AirtelMoney, airtelRequest.Id, request.StoreId,
            transactionReference, null, request.PhoneNumber, request.Amount, "KES",
            MobileMoneyTransactionStatus.Pending, "INITIATED", "Payment initiated", cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // In production, this would call the Airtel Money API to initiate the STK push
        // Simulate successful initiation
        return new MobileMoneyPaymentResult
        {
            IsSuccess = true,
            TransactionReference = transactionReference,
            Status = MobileMoneyTransactionStatus.Pending,
            RequestedAt = airtelRequest.RequestedAt,
            TimeoutSeconds = config.TimeoutSeconds
        };
    }

    /// <inheritdoc />
    public async Task<MobileMoneyPaymentStatus> CheckAirtelMoneyPaymentStatusAsync(string transactionReference, CancellationToken cancellationToken = default)
    {
        var request = await _context.AirtelMoneyRequests
            .FirstOrDefaultAsync(r => r.TransactionReference == transactionReference, cancellationToken)
            .ConfigureAwait(false);

        if (request == null)
        {
            return new MobileMoneyPaymentStatus
            {
                TransactionReference = transactionReference,
                Status = MobileMoneyTransactionStatus.Failed,
                ErrorMessage = "Transaction not found",
                IsFinal = true
            };
        }

        // Check if we need to query the API for status
        if (request.Status == MobileMoneyTransactionStatus.Pending)
        {
            request.StatusCheckAttempts++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            // In production, this would call the Airtel Money API to check status
        }

        return new MobileMoneyPaymentStatus
        {
            TransactionReference = transactionReference,
            ProviderTransactionId = request.AirtelTransactionId,
            Status = request.Status,
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            RequestedAt = request.RequestedAt,
            CompletedAt = request.CompletedAt,
            ErrorMessage = request.ErrorMessage,
            ErrorCode = request.ErrorCode,
            IsFinal = request.Status != MobileMoneyTransactionStatus.Pending
        };
    }

    /// <inheritdoc />
    public async Task<MobileMoneyCallbackResult> ProcessAirtelMoneyCallbackAsync(string callbackData, CancellationToken cancellationToken = default)
    {
        try
        {
            var callback = JsonSerializer.Deserialize<AirtelMoneyCallback>(callbackData);
            if (callback == null)
            {
                return new MobileMoneyCallbackResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid callback data"
                };
            }

            var request = await _context.AirtelMoneyRequests
                .FirstOrDefaultAsync(r => r.TransactionReference == callback.TransactionReference, cancellationToken)
                .ConfigureAwait(false);

            if (request == null)
            {
                return new MobileMoneyCallbackResult
                {
                    IsSuccess = false,
                    TransactionReference = callback.TransactionReference ?? "",
                    ErrorMessage = "Transaction not found"
                };
            }

            request.CallbackData = callbackData;
            request.CallbackReceivedAt = DateTime.UtcNow;
            request.AirtelTransactionId = callback.AirtelTransactionId;

            if (callback.IsSuccessful)
            {
                request.Status = MobileMoneyTransactionStatus.Completed;
                request.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                request.Status = MobileMoneyTransactionStatus.Failed;
                request.ErrorMessage = callback.ErrorMessage;
                request.ErrorCode = callback.ErrorCode;
            }

            await LogTransactionAsync(MobileMoneyProvider.AirtelMoney, request.Id, request.StoreId,
                request.TransactionReference, request.AirtelTransactionId, request.PhoneNumber, request.Amount, "KES",
                request.Status, "CALLBACK", callback.IsSuccessful ? "Payment completed" : $"Payment failed: {callback.ErrorMessage}", cancellationToken).ConfigureAwait(false);

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyCallbackResult
            {
                IsSuccess = true,
                TransactionReference = request.TransactionReference,
                Status = request.Status,
                ReceiptId = request.ReceiptId
            };
        }
        catch (Exception ex)
        {
            return new MobileMoneyCallbackResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<AirtelMoneyRequest?> GetAirtelMoneyTransactionAsync(string transactionReference, CancellationToken cancellationToken = default)
    {
        return await _context.AirtelMoneyRequests
            .FirstOrDefaultAsync(r => r.TransactionReference == transactionReference, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AirtelMoneyRequest>> GetAirtelMoneyTransactionsAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.AirtelMoneyRequests
            .Where(r => r.StoreId == storeId && r.RequestedAt >= startDate && r.RequestedAt <= endDate)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region T-Kash Configuration

    /// <inheritdoc />
    public async Task<TKashConfiguration?> GetTKashConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.TKashConfigurations
            .Where(c => c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TKashConfiguration> SaveTKashConfigurationAsync(TKashConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration.Id == 0)
        {
            _context.TKashConfigurations.Add(configuration);
        }
        else
        {
            _context.TKashConfigurations.Update(configuration);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return configuration;
    }

    /// <inheritdoc />
    public async Task<MobileMoneyConnectionTestResult> TestTKashConnectionAsync(int configurationId, CancellationToken cancellationToken = default)
    {
        var config = await _context.TKashConfigurations.FindAsync(new object[] { configurationId }, cancellationToken).ConfigureAwait(false);
        if (config == null)
        {
            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = false,
                ErrorMessage = "Configuration not found",
                TestedAt = DateTime.UtcNow
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simulate API connection test
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            config.LastTestSuccessful = true;
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestError = null;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = true,
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ApiVersion = "v1"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            config.LastTestSuccessful = false;
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestError = ex.Message;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyConnectionTestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                TestedAt = DateTime.UtcNow,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task SetTKashEnabledAsync(int configurationId, bool enabled, CancellationToken cancellationToken = default)
    {
        var config = await _context.TKashConfigurations.FindAsync(new object[] { configurationId }, cancellationToken).ConfigureAwait(false);
        if (config != null)
        {
            config.IsEnabled = enabled;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region T-Kash Payments

    /// <inheritdoc />
    public async Task<MobileMoneyPaymentResult> InitiateTKashPaymentAsync(MobileMoneyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Validate phone number
        var phoneValidation = await ValidatePhoneNumberAsync(request.PhoneNumber, MobileMoneyProvider.TKash, cancellationToken).ConfigureAwait(false);
        if (!phoneValidation.IsValid)
        {
            return new MobileMoneyPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = phoneValidation.ErrorMessage ?? "Invalid phone number for T-Kash",
                ErrorCode = "INVALID_PHONE"
            };
        }

        var config = await GetTKashConfigurationAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        if (config == null || !config.IsEnabled)
        {
            return new MobileMoneyPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "T-Kash is not configured or enabled",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        var transactionReference = GenerateTransactionReference("TK");

        var tkashRequest = new TKashRequest
        {
            ReceiptId = request.ReceiptId,
            StoreId = request.StoreId,
            TransactionReference = transactionReference,
            PhoneNumber = phoneValidation.NormalizedNumber!,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Status = MobileMoneyTransactionStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            UserId = request.UserId
        };

        _context.TKashRequests.Add(tkashRequest);

        await LogTransactionAsync(MobileMoneyProvider.TKash, tkashRequest.Id, request.StoreId,
            transactionReference, null, request.PhoneNumber, request.Amount, "KES",
            MobileMoneyTransactionStatus.Pending, "INITIATED", "Payment initiated", cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new MobileMoneyPaymentResult
        {
            IsSuccess = true,
            TransactionReference = transactionReference,
            Status = MobileMoneyTransactionStatus.Pending,
            RequestedAt = tkashRequest.RequestedAt,
            TimeoutSeconds = config.TimeoutSeconds
        };
    }

    /// <inheritdoc />
    public async Task<MobileMoneyPaymentStatus> CheckTKashPaymentStatusAsync(string transactionReference, CancellationToken cancellationToken = default)
    {
        var request = await _context.TKashRequests
            .FirstOrDefaultAsync(r => r.TransactionReference == transactionReference, cancellationToken)
            .ConfigureAwait(false);

        if (request == null)
        {
            return new MobileMoneyPaymentStatus
            {
                TransactionReference = transactionReference,
                Status = MobileMoneyTransactionStatus.Failed,
                ErrorMessage = "Transaction not found",
                IsFinal = true
            };
        }

        if (request.Status == MobileMoneyTransactionStatus.Pending)
        {
            request.StatusCheckAttempts++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new MobileMoneyPaymentStatus
        {
            TransactionReference = transactionReference,
            ProviderTransactionId = request.TKashTransactionId,
            Status = request.Status,
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            RequestedAt = request.RequestedAt,
            CompletedAt = request.CompletedAt,
            ErrorMessage = request.ErrorMessage,
            ErrorCode = request.ErrorCode,
            IsFinal = request.Status != MobileMoneyTransactionStatus.Pending
        };
    }

    /// <inheritdoc />
    public async Task<MobileMoneyCallbackResult> ProcessTKashCallbackAsync(string callbackData, CancellationToken cancellationToken = default)
    {
        try
        {
            var callback = JsonSerializer.Deserialize<TKashCallback>(callbackData);
            if (callback == null)
            {
                return new MobileMoneyCallbackResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid callback data"
                };
            }

            var request = await _context.TKashRequests
                .FirstOrDefaultAsync(r => r.TransactionReference == callback.TransactionReference, cancellationToken)
                .ConfigureAwait(false);

            if (request == null)
            {
                return new MobileMoneyCallbackResult
                {
                    IsSuccess = false,
                    TransactionReference = callback.TransactionReference ?? "",
                    ErrorMessage = "Transaction not found"
                };
            }

            request.CallbackData = callbackData;
            request.CallbackReceivedAt = DateTime.UtcNow;
            request.TKashTransactionId = callback.TKashTransactionId;

            if (callback.IsSuccessful)
            {
                request.Status = MobileMoneyTransactionStatus.Completed;
                request.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                request.Status = MobileMoneyTransactionStatus.Failed;
                request.ErrorMessage = callback.ErrorMessage;
                request.ErrorCode = callback.ErrorCode;
            }

            await LogTransactionAsync(MobileMoneyProvider.TKash, request.Id, request.StoreId,
                request.TransactionReference, request.TKashTransactionId, request.PhoneNumber, request.Amount, "KES",
                request.Status, "CALLBACK", callback.IsSuccessful ? "Payment completed" : $"Payment failed: {callback.ErrorMessage}", cancellationToken).ConfigureAwait(false);

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MobileMoneyCallbackResult
            {
                IsSuccess = true,
                TransactionReference = request.TransactionReference,
                Status = request.Status,
                ReceiptId = request.ReceiptId
            };
        }
        catch (Exception ex)
        {
            return new MobileMoneyCallbackResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<TKashRequest?> GetTKashTransactionAsync(string transactionReference, CancellationToken cancellationToken = default)
    {
        return await _context.TKashRequests
            .FirstOrDefaultAsync(r => r.TransactionReference == transactionReference, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TKashRequest>> GetTKashTransactionsAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.TKashRequests
            .Where(r => r.StoreId == storeId && r.RequestedAt >= startDate && r.RequestedAt <= endDate)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Common Operations

    /// <inheritdoc />
    public Task<PhoneValidationResult> ValidatePhoneNumberAsync(string phoneNumber, MobileMoneyProvider provider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return Task.FromResult(new PhoneValidationResult
            {
                IsValid = false,
                ErrorMessage = "Phone number is required"
            });
        }

        // Normalize phone number (remove spaces, dashes)
        var normalized = phoneNumber.Replace(" ", "").Replace("-", "").Trim();

        // Check pattern based on provider
        var isValid = provider switch
        {
            MobileMoneyProvider.AirtelMoney => AirtelPattern.IsMatch(normalized),
            MobileMoneyProvider.TKash => TKashPattern.IsMatch(normalized),
            MobileMoneyProvider.MPesa => MpesaPattern.IsMatch(normalized),
            _ => false
        };

        if (!isValid)
        {
            // Try to detect the actual provider
            var detectedProvider = DetectProvider(normalized);
            if (detectedProvider != null && detectedProvider != provider)
            {
                return Task.FromResult(new PhoneValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"This phone number belongs to {detectedProvider}, not {provider}",
                    DetectedProvider = detectedProvider
                });
            }

            return Task.FromResult(new PhoneValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid phone number format for {provider}"
            });
        }

        // Normalize to international format
        var normalizedNumber = NormalizePhoneNumber(normalized);

        return Task.FromResult(new PhoneValidationResult
        {
            IsValid = true,
            NormalizedNumber = normalizedNumber,
            DetectedProvider = provider
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MobileMoneyProviderInfo>> GetAvailableProvidersAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var providers = new List<MobileMoneyProviderInfo>();

        // M-Pesa
        var mpesaConfig = await _context.MpesaConfigurations
            .Where(c => c.StoreId == storeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        providers.Add(new MobileMoneyProviderInfo
        {
            Provider = MobileMoneyProvider.MPesa,
            Name = "M-Pesa",
            IsConfigured = mpesaConfig != null,
            IsEnabled = mpesaConfig?.IsEnabled ?? false,
            PhonePrefix = "07XX"
        });

        // Airtel Money
        var airtelConfig = await GetAirtelMoneyConfigurationAsync(storeId, cancellationToken).ConfigureAwait(false);
        providers.Add(new MobileMoneyProviderInfo
        {
            Provider = MobileMoneyProvider.AirtelMoney,
            Name = "Airtel Money",
            IsConfigured = airtelConfig != null,
            IsEnabled = airtelConfig?.IsEnabled ?? false,
            PhonePrefix = "0733/0755"
        });

        // T-Kash
        var tkashConfig = await GetTKashConfigurationAsync(storeId, cancellationToken).ConfigureAwait(false);
        providers.Add(new MobileMoneyProviderInfo
        {
            Provider = MobileMoneyProvider.TKash,
            Name = "T-Kash",
            IsConfigured = tkashConfig != null,
            IsEnabled = tkashConfig?.IsEnabled ?? false,
            PhonePrefix = "077X"
        });

        return providers;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MobileMoneyTransactionLog>> GetTransactionLogsAsync(int? storeId = null, MobileMoneyProvider? provider = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var query = _context.MobileMoneyTransactionLogs.AsQueryable();

        if (storeId.HasValue)
            query = query.Where(l => l.StoreId == storeId.Value);

        if (provider.HasValue)
            query = query.Where(l => l.Provider == provider.Value);

        if (startDate.HasValue)
            query = query.Where(l => l.LoggedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.LoggedAt <= endDate.Value);

        return await query
            .OrderByDescending(l => l.LoggedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MobileMoneyReconciliationReport> GetReconciliationReportAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var report = new MobileMoneyReconciliationReport
        {
            StoreId = storeId,
            StartDate = startDate,
            EndDate = endDate
        };

        // M-Pesa summary
        var mpesaTransactions = await _context.MpesaTransactions
            .Where(t => t.StoreId == storeId && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        report.MPesaSummary = new MobileMoneyProviderSummary
        {
            Provider = MobileMoneyProvider.MPesa,
            TotalTransactions = mpesaTransactions.Count,
            SuccessfulTransactions = mpesaTransactions.Count(t => t.ResultCode == "0"),
            FailedTransactions = mpesaTransactions.Count(t => t.ResultCode != "0"),
            TotalAmount = mpesaTransactions.Sum(t => t.Amount),
            SuccessfulAmount = mpesaTransactions.Where(t => t.ResultCode == "0").Sum(t => t.Amount),
            AverageTransactionAmount = mpesaTransactions.Any() ? mpesaTransactions.Average(t => t.Amount) : 0
        };

        // Airtel Money summary
        var airtelTransactions = await GetAirtelMoneyTransactionsAsync(storeId, startDate, endDate, cancellationToken).ConfigureAwait(false);
        var airtelList = airtelTransactions.ToList();

        report.AirtelMoneySummary = new MobileMoneyProviderSummary
        {
            Provider = MobileMoneyProvider.AirtelMoney,
            TotalTransactions = airtelList.Count,
            SuccessfulTransactions = airtelList.Count(t => t.Status == MobileMoneyTransactionStatus.Completed),
            FailedTransactions = airtelList.Count(t => t.Status == MobileMoneyTransactionStatus.Failed),
            PendingTransactions = airtelList.Count(t => t.Status == MobileMoneyTransactionStatus.Pending),
            TotalAmount = airtelList.Sum(t => t.Amount),
            SuccessfulAmount = airtelList.Where(t => t.Status == MobileMoneyTransactionStatus.Completed).Sum(t => t.Amount),
            AverageTransactionAmount = airtelList.Any() ? airtelList.Average(t => t.Amount) : 0
        };

        // T-Kash summary
        var tkashTransactions = await GetTKashTransactionsAsync(storeId, startDate, endDate, cancellationToken).ConfigureAwait(false);
        var tkashList = tkashTransactions.ToList();

        report.TKashSummary = new MobileMoneyProviderSummary
        {
            Provider = MobileMoneyProvider.TKash,
            TotalTransactions = tkashList.Count,
            SuccessfulTransactions = tkashList.Count(t => t.Status == MobileMoneyTransactionStatus.Completed),
            FailedTransactions = tkashList.Count(t => t.Status == MobileMoneyTransactionStatus.Failed),
            PendingTransactions = tkashList.Count(t => t.Status == MobileMoneyTransactionStatus.Pending),
            TotalAmount = tkashList.Sum(t => t.Amount),
            SuccessfulAmount = tkashList.Where(t => t.Status == MobileMoneyTransactionStatus.Completed).Sum(t => t.Amount),
            AverageTransactionAmount = tkashList.Any() ? tkashList.Average(t => t.Amount) : 0
        };

        return report;
    }

    /// <inheritdoc />
    public async Task<MobileMoneyPaymentResult> RetryPaymentAsync(string transactionReference, MobileMoneyProvider provider, CancellationToken cancellationToken = default)
    {
        switch (provider)
        {
            case MobileMoneyProvider.AirtelMoney:
                var airtelRequest = await GetAirtelMoneyTransactionAsync(transactionReference, cancellationToken).ConfigureAwait(false);
                if (airtelRequest == null || airtelRequest.Status != MobileMoneyTransactionStatus.Failed)
                {
                    return new MobileMoneyPaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Transaction not found or not eligible for retry"
                    };
                }

                return await InitiateAirtelMoneyPaymentAsync(new MobileMoneyPaymentRequest
                {
                    StoreId = airtelRequest.StoreId,
                    ReceiptId = airtelRequest.ReceiptId,
                    PhoneNumber = airtelRequest.PhoneNumber,
                    Amount = airtelRequest.Amount,
                    CurrencyCode = airtelRequest.CurrencyCode,
                    UserId = airtelRequest.UserId
                }, cancellationToken).ConfigureAwait(false);

            case MobileMoneyProvider.TKash:
                var tkashRequest = await GetTKashTransactionAsync(transactionReference, cancellationToken).ConfigureAwait(false);
                if (tkashRequest == null || tkashRequest.Status != MobileMoneyTransactionStatus.Failed)
                {
                    return new MobileMoneyPaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Transaction not found or not eligible for retry"
                    };
                }

                return await InitiateTKashPaymentAsync(new MobileMoneyPaymentRequest
                {
                    StoreId = tkashRequest.StoreId,
                    ReceiptId = tkashRequest.ReceiptId,
                    PhoneNumber = tkashRequest.PhoneNumber,
                    Amount = tkashRequest.Amount,
                    CurrencyCode = tkashRequest.CurrencyCode,
                    UserId = tkashRequest.UserId
                }, cancellationToken).ConfigureAwait(false);

            default:
                return new MobileMoneyPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unsupported provider for retry"
                };
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelPaymentAsync(string transactionReference, MobileMoneyProvider provider, CancellationToken cancellationToken = default)
    {
        switch (provider)
        {
            case MobileMoneyProvider.AirtelMoney:
                var airtelRequest = await GetAirtelMoneyTransactionAsync(transactionReference, cancellationToken).ConfigureAwait(false);
                if (airtelRequest == null || airtelRequest.Status != MobileMoneyTransactionStatus.Pending)
                    return false;

                airtelRequest.Status = MobileMoneyTransactionStatus.Cancelled;
                airtelRequest.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;

            case MobileMoneyProvider.TKash:
                var tkashRequest = await GetTKashTransactionAsync(transactionReference, cancellationToken).ConfigureAwait(false);
                if (tkashRequest == null || tkashRequest.Status != MobileMoneyTransactionStatus.Pending)
                    return false;

                tkashRequest.Status = MobileMoneyTransactionStatus.Cancelled;
                tkashRequest.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Private Methods

    private static string GenerateTransactionReference(string prefix)
    {
        return $"{prefix}{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove leading 0 or +254, then add 254 prefix
        if (phone.StartsWith("+254"))
            return phone[1..]; // Remove +
        if (phone.StartsWith("254"))
            return phone;
        if (phone.StartsWith("0"))
            return "254" + phone[1..];
        return "254" + phone;
    }

    private static MobileMoneyProvider? DetectProvider(string phone)
    {
        if (MpesaPattern.IsMatch(phone))
            return MobileMoneyProvider.MPesa;
        if (AirtelPattern.IsMatch(phone))
            return MobileMoneyProvider.AirtelMoney;
        if (TKashPattern.IsMatch(phone))
            return MobileMoneyProvider.TKash;
        return null;
    }

    private async Task LogTransactionAsync(MobileMoneyProvider provider, int requestId, int storeId,
        string transactionReference, string? providerTransactionId, string phoneNumber, decimal amount,
        string currencyCode, MobileMoneyTransactionStatus status, string entryType, string message,
        CancellationToken cancellationToken)
    {
        var log = new MobileMoneyTransactionLog
        {
            Provider = provider,
            RequestId = requestId,
            StoreId = storeId,
            TransactionReference = transactionReference,
            ProviderTransactionId = providerTransactionId,
            PhoneNumber = phoneNumber,
            Amount = amount,
            CurrencyCode = currencyCode,
            Status = status,
            EntryType = entryType,
            Message = message,
            LoggedAt = DateTime.UtcNow
        };

        _context.MobileMoneyTransactionLogs.Add(log);
    }

    #endregion
}

#region Internal DTOs

internal class AirtelMoneyCallback
{
    public string? TransactionReference { get; set; }
    public string? AirtelTransactionId { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

internal class TKashCallback
{
    public string? TransactionReference { get; set; }
    public string? TKashTransactionId { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

#endregion
