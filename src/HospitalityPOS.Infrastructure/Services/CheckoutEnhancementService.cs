using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Collections.Concurrent;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for checkout enhancement features.
/// </summary>
public class CheckoutEnhancementService : ICheckoutEnhancementService
{
    private readonly POSDbContext _context;

    // TTL-based cache entry with expiration tracking
    private record DisplayStateEntry(CustomerDisplayState State, DateTime ExpiresAt);

    // Display states cache with TTL support to prevent memory leaks
    private static readonly ConcurrentDictionary<string, DisplayStateEntry> _displayStates = new();
    private static readonly TimeSpan DisplayStateTTL = TimeSpan.FromHours(4); // Expire idle states after 4 hours
    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(30);
    private static readonly object _cleanupLock = new();

    public CheckoutEnhancementService(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Trigger cleanup on construction if needed (non-blocking check)
        CleanupExpiredEntriesIfNeeded();
    }

    /// <summary>
    /// Removes expired display state entries to prevent memory leaks.
    /// </summary>
    private static void CleanupExpiredEntriesIfNeeded()
    {
        var now = DateTime.UtcNow;

        // Only attempt cleanup every 30 minutes
        if (now - _lastCleanup < CleanupInterval)
            return;

        // Use lock to prevent multiple simultaneous cleanups
        if (!Monitor.TryEnter(_cleanupLock))
            return;

        try
        {
            if (now - _lastCleanup < CleanupInterval)
                return; // Double-check after acquiring lock

            var expiredKeys = _displayStates
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _displayStates.TryRemove(key, out _);
            }

            _lastCleanup = now;
        }
        finally
        {
            Monitor.Exit(_cleanupLock);
        }
    }

    #region Suspended Transactions (Park/Recall)

    /// <inheritdoc />
    public async Task<SuspendedTransaction> ParkTransactionAsync(
        ParkTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var referenceNumber = GenerateReferenceNumber();

        var transaction = new SuspendedTransaction
        {
            StoreId = request.StoreId,
            TerminalId = request.TerminalId,
            ParkedByUserId = request.ParkedByUserId,
            ReferenceNumber = referenceNumber,
            CustomerName = request.CustomerName,
            TableNumber = request.TableNumber,
            OrderType = request.OrderType,
            Notes = request.Notes,
            LoyaltyMemberId = request.LoyaltyMemberId,
            AppliedPromotionIds = request.AppliedPromotionIds,
            Status = SuspendedTransactionStatus.Parked,
            ParkedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (request.ExpirationMinutes.HasValue)
        {
            transaction.ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes.Value);
        }

        // Calculate totals and add items
        decimal subtotal = 0;
        decimal taxAmount = 0;
        decimal discountAmount = 0;
        int displayOrder = 0;

        foreach (var item in request.Items)
        {
            var lineTotal = (item.UnitPrice * item.Quantity) - item.DiscountAmount + item.TaxAmount;
            subtotal += item.UnitPrice * item.Quantity;
            taxAmount += item.TaxAmount;
            discountAmount += item.DiscountAmount;

            transaction.Items.Add(new SuspendedTransactionItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductCode = item.ProductCode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = item.TaxAmount,
                LineTotal = lineTotal,
                Notes = item.Notes,
                ModifiersJson = item.ModifiersJson,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            });
        }

        transaction.Subtotal = subtotal;
        transaction.TaxAmount = taxAmount;
        transaction.DiscountAmount = discountAmount;
        transaction.TotalAmount = subtotal - discountAmount + taxAmount;
        transaction.ItemCount = request.Items.Count;

        _context.SuspendedTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<SuspendedTransaction?> GetSuspendedTransactionAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SuspendedTransactions
            .Include(t => t.Items)
                .ThenInclude(i => i.Product)
            .Include(t => t.Store)
            .Include(t => t.ParkedByUser)
            .Include(t => t.LoyaltyMember)
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SuspendedTransaction?> GetSuspendedTransactionByReferenceAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.SuspendedTransactions
            .Include(t => t.Items)
                .ThenInclude(i => i.Product)
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.ReferenceNumber == referenceNumber && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SuspendedTransaction>> GetParkedTransactionsAsync(
        int storeId,
        int? terminalId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SuspendedTransactions
            .Include(t => t.Items)
            .Include(t => t.ParkedByUser)
            .Where(t => t.StoreId == storeId &&
                       t.Status == SuspendedTransactionStatus.Parked &&
                       !t.IsDeleted);

        if (terminalId.HasValue)
            query = query.Where(t => t.TerminalId == terminalId.Value);

        return await query
            .OrderByDescending(t => t.ParkedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecallTransactionResult> RecallTransactionAsync(
        int transactionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await GetSuspendedTransactionAsync(transactionId, cancellationToken)
            .ConfigureAwait(false);

        if (transaction == null)
        {
            return new RecallTransactionResult
            {
                Success = false,
                Message = "Transaction not found."
            };
        }

        if (transaction.Status != SuspendedTransactionStatus.Parked)
        {
            return new RecallTransactionResult
            {
                Success = false,
                Message = $"Transaction cannot be recalled. Status: {transaction.Status}"
            };
        }

        if (transaction.ExpiresAt.HasValue && transaction.ExpiresAt < DateTime.UtcNow)
        {
            transaction.Status = SuspendedTransactionStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RecallTransactionResult
            {
                Success = false,
                Message = "Transaction has expired."
            };
        }

        // Mark as recalled
        transaction.Status = SuspendedTransactionStatus.Recalled;
        transaction.RecalledAt = DateTime.UtcNow;
        transaction.RecalledByUserId = userId;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Build recalled items with current availability/pricing
        var items = new List<RecallTransactionItem>();
        foreach (var item in transaction.Items.OrderBy(i => i.DisplayOrder))
        {
            var currentProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && !p.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new RecallTransactionItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductCode = item.ProductCode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = item.TaxAmount,
                Notes = item.Notes,
                ModifiersJson = item.ModifiersJson,
                IsAvailable = currentProduct != null,
                CurrentPrice = currentProduct?.Price
            });
        }

        return new RecallTransactionResult
        {
            Success = true,
            Transaction = transaction,
            Items = items,
            Message = "Transaction recalled successfully."
        };
    }

    /// <inheritdoc />
    public async Task VoidSuspendedTransactionAsync(
        int transactionId,
        int userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.SuspendedTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (transaction == null)
            throw new InvalidOperationException($"Suspended transaction {transactionId} not found.");

        if (transaction.Status != SuspendedTransactionStatus.Parked)
            throw new InvalidOperationException("Only parked transactions can be voided.");

        transaction.Status = SuspendedTransactionStatus.Voided;
        transaction.Notes = $"Voided: {reason}\n{transaction.Notes}";
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CompleteSuspendedTransactionAsync(
        int transactionId,
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.SuspendedTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (transaction == null)
            throw new InvalidOperationException($"Suspended transaction {transactionId} not found.");

        transaction.CompletedReceiptId = receiptId;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> ProcessExpiredTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredTransactions = await _context.SuspendedTransactions
            .Where(t => t.Status == SuspendedTransactionStatus.Parked &&
                       t.ExpiresAt.HasValue &&
                       t.ExpiresAt < DateTime.UtcNow &&
                       !t.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var transaction in expiredTransactions)
        {
            transaction.Status = SuspendedTransactionStatus.Expired;
            transaction.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return expiredTransactions.Count;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SuspendedTransaction>> SearchSuspendedTransactionsAsync(
        SuspendedTransactionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SuspendedTransactions
            .Include(t => t.Items)
            .Include(t => t.ParkedByUser)
            .Where(t => t.StoreId == request.StoreId && !t.IsDeleted);

        if (request.TerminalId.HasValue)
            query = query.Where(t => t.TerminalId == request.TerminalId.Value);

        if (!string.IsNullOrEmpty(request.CustomerName))
            query = query.Where(t => t.CustomerName != null && t.CustomerName.Contains(request.CustomerName));

        if (!string.IsNullOrEmpty(request.TableNumber))
            query = query.Where(t => t.TableNumber == request.TableNumber);

        if (!string.IsNullOrEmpty(request.ReferenceNumber))
            query = query.Where(t => t.ReferenceNumber.Contains(request.ReferenceNumber));

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.ParkedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.ParkedAt <= request.ToDate.Value);

        if (request.ParkedByUserId.HasValue)
            query = query.Where(t => t.ParkedByUserId == request.ParkedByUserId.Value);

        return await query
            .OrderByDescending(t => t.ParkedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Customer-Facing Display

    /// <inheritdoc />
    public async Task<CustomerDisplayConfig?> GetCustomerDisplayConfigAsync(
        int storeId,
        int? terminalId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerDisplayConfigs
            .Include(c => c.Messages.Where(m => m.IsActive))
            .Where(c => c.StoreId == storeId && !c.IsDeleted);

        if (terminalId.HasValue)
            query = query.Where(c => c.TerminalId == terminalId.Value);
        else
            query = query.Where(c => c.TerminalId == null);

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CustomerDisplayConfig> SaveCustomerDisplayConfigAsync(
        CustomerDisplayConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.CustomerDisplayConfigs.Add(config);
        }
        else
        {
            var existing = await _context.CustomerDisplayConfigs
                .FirstOrDefaultAsync(c => c.Id == config.Id && !c.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Customer display config {config.Id} not found.");

            existing.Name = config.Name;
            existing.DisplayType = config.DisplayType;
            existing.IsEnabled = config.IsEnabled;
            existing.ScreenWidth = config.ScreenWidth;
            existing.ScreenHeight = config.ScreenHeight;
            existing.BackgroundColor = config.BackgroundColor;
            existing.PrimaryTextColor = config.PrimaryTextColor;
            existing.AccentColor = config.AccentColor;
            existing.FontFamily = config.FontFamily;
            existing.HeaderFontSize = config.HeaderFontSize;
            existing.ItemFontSize = config.ItemFontSize;
            existing.TotalFontSize = config.TotalFontSize;
            existing.ShowItemImages = config.ShowItemImages;
            existing.ShowPromotionalMessages = config.ShowPromotionalMessages;
            existing.PromotionalRotationSeconds = config.PromotionalRotationSeconds;
            existing.ShowStoreLogo = config.ShowStoreLogo;
            existing.LogoUrl = config.LogoUrl;
            existing.WelcomeMessage = config.WelcomeMessage;
            existing.ThankYouMessage = config.ThankYouMessage;
            existing.IdleScreenType = config.IdleScreenType;
            existing.IdleTimeoutSeconds = config.IdleTimeoutSeconds;
            existing.ShowCurrencySymbol = config.ShowCurrencySymbol;
            existing.CurrencySymbol = config.CurrencySymbol;
            existing.LayoutTemplate = config.LayoutTemplate;
            existing.UpdatedAt = DateTime.UtcNow;

            config = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CustomerDisplayMessage>> GetActivePromotionalMessagesAsync(
        int displayConfigId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.CustomerDisplayMessages
            .Where(m => m.DisplayConfigId == displayConfigId &&
                       m.IsActive &&
                       !m.IsDeleted &&
                       (m.StartDate == null || m.StartDate <= now) &&
                       (m.EndDate == null || m.EndDate >= now))
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CustomerDisplayMessage> SavePromotionalMessageAsync(
        CustomerDisplayMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message.Id == 0)
        {
            message.CreatedAt = DateTime.UtcNow;
            _context.CustomerDisplayMessages.Add(message);
        }
        else
        {
            var existing = await _context.CustomerDisplayMessages
                .FirstOrDefaultAsync(m => m.Id == message.Id && !m.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Promotional message {message.Id} not found.");

            existing.Title = message.Title;
            existing.Content = message.Content;
            existing.ImageUrl = message.ImageUrl;
            existing.DisplayOrder = message.DisplayOrder;
            existing.IsActive = message.IsActive;
            existing.StartDate = message.StartDate;
            existing.EndDate = message.EndDate;
            existing.UpdatedAt = DateTime.UtcNow;

            message = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return message;
    }

    /// <inheritdoc />
    public async Task DeletePromotionalMessageAsync(int messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.CustomerDisplayMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (message == null)
            throw new InvalidOperationException($"Promotional message {messageId} not found.");

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<CustomerDisplayState> GetCustomerDisplayStateAsync(
        int storeId,
        int? terminalId,
        CancellationToken cancellationToken = default)
    {
        // Trigger cleanup opportunistically
        CleanupExpiredEntriesIfNeeded();

        var key = $"{storeId}_{terminalId ?? 0}";
        var now = DateTime.UtcNow;

        var entry = _displayStates.GetOrAdd(key, _ =>
            new DisplayStateEntry(new CustomerDisplayState { State = "Idle" }, now.Add(DisplayStateTTL)));

        // Check if entry is expired and refresh
        if (entry.ExpiresAt < now)
        {
            var newEntry = new DisplayStateEntry(new CustomerDisplayState { State = "Idle" }, now.Add(DisplayStateTTL));
            _displayStates[key] = newEntry;
            return Task.FromResult(newEntry.State);
        }

        return Task.FromResult(entry.State);
    }

    /// <inheritdoc />
    public Task UpdateCustomerDisplayStateAsync(
        int storeId,
        int? terminalId,
        CustomerDisplayState state,
        CancellationToken cancellationToken = default)
    {
        var key = $"{storeId}_{terminalId ?? 0}";
        var now = DateTime.UtcNow;
        state.LastUpdated = now;

        // Update with refreshed TTL
        _displayStates[key] = new DisplayStateEntry(state, now.Add(DisplayStateTTL));
        return Task.CompletedTask;
    }

    #endregion

    #region Enhanced Split Payment

    /// <inheritdoc />
    public async Task<SplitPaymentConfig?> GetSplitPaymentConfigAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SplitPaymentConfigs
            .FirstOrDefaultAsync(c => c.StoreId == storeId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SplitPaymentConfig> SaveSplitPaymentConfigAsync(
        SplitPaymentConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.Id == 0)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.SplitPaymentConfigs.Add(config);
        }
        else
        {
            var existing = await _context.SplitPaymentConfigs
                .FirstOrDefaultAsync(c => c.Id == config.Id && !c.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Split payment config {config.Id} not found.");

            existing.MaxSplitWays = config.MaxSplitWays;
            existing.MinSplitAmount = config.MinSplitAmount;
            existing.AllowMixedPaymentMethods = config.AllowMixedPaymentMethods;
            existing.AllowItemSplit = config.AllowItemSplit;
            existing.AllowEqualSplit = config.AllowEqualSplit;
            existing.AllowCustomAmountSplit = config.AllowCustomAmountSplit;
            existing.AllowPercentageSplit = config.AllowPercentageSplit;
            existing.DefaultSplitMethod = config.DefaultSplitMethod;
            existing.RequireAllPartiesPresent = config.RequireAllPartiesPresent;
            existing.PrintSeparateReceipts = config.PrintSeparateReceipts;
            existing.UpdatedAt = DateTime.UtcNow;

            config = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return config;
    }

    /// <inheritdoc />
    public async Task<SplitPaymentSession> InitiateSplitPaymentAsync(
        InitiateSplitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == request.ReceiptId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (receipt == null)
            throw new InvalidOperationException($"Receipt {request.ReceiptId} not found.");

        var session = new SplitPaymentSession
        {
            ReceiptId = request.ReceiptId,
            SplitMethod = request.SplitMethod,
            NumberOfSplits = request.NumberOfSplits,
            TotalAmount = receipt.TotalAmount,
            PaidAmount = 0,
            IsComplete = false,
            InitiatedByUserId = request.InitiatedByUserId,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SplitPaymentSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create split parts
        if (request.Parts != null && request.Parts.Any())
        {
            foreach (var partDef in request.Parts)
            {
                var part = new SplitPaymentPart
                {
                    SplitSessionId = session.Id,
                    PartNumber = partDef.PartNumber,
                    PayerName = partDef.PayerName,
                    Amount = partDef.Amount ?? 0,
                    Percentage = partDef.Percentage,
                    IncludedItemIds = partDef.ItemIds != null ? string.Join(",", partDef.ItemIds) : null,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };
                session.Parts.Add(part);
            }
        }
        else
        {
            // Create equal split parts
            var equalSplit = await CalculateEqualSplitAsync(receipt.TotalAmount, request.NumberOfSplits, cancellationToken)
                .ConfigureAwait(false);

            for (int i = 0; i < request.NumberOfSplits; i++)
            {
                var part = new SplitPaymentPart
                {
                    SplitSessionId = session.Id,
                    PartNumber = i + 1,
                    Amount = equalSplit.SplitAmounts[i],
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };
                session.Parts.Add(part);
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }

    /// <inheritdoc />
    public async Task<SplitPaymentSession?> GetSplitPaymentSessionAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SplitPaymentSessions
            .Include(s => s.Parts)
            .Include(s => s.Receipt)
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SplitPaymentSession>> GetActiveSplitSessionsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SplitPaymentSessions
            .Include(s => s.Parts)
            .Include(s => s.Receipt)
            .Where(s => s.Receipt.StoreId == storeId && !s.IsComplete && !s.IsDeleted)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SplitPaymentPart> ProcessSplitPartPaymentAsync(
        ProcessSplitPartRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSplitPaymentSessionAsync(request.SplitSessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session == null)
            throw new InvalidOperationException($"Split session {request.SplitSessionId} not found.");

        if (session.IsComplete)
            throw new InvalidOperationException("Split session is already complete.");

        var part = session.Parts.FirstOrDefault(p => p.PartNumber == request.PartNumber);
        if (part == null)
            throw new InvalidOperationException($"Split part {request.PartNumber} not found.");

        if (part.IsPaid)
            throw new InvalidOperationException($"Split part {request.PartNumber} is already paid.");

        // Security validation: Ensure payment amount is reasonable
        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.");

        // Validate against allocated part amount (allow small tolerance for rounding)
        const decimal tolerance = 0.01m;
        if (part.Amount > 0 && Math.Abs(request.Amount - part.Amount) > tolerance)
            throw new InvalidOperationException(
                $"Payment amount {request.Amount:F2} does not match allocated part amount {part.Amount:F2}.");

        // Validate total doesn't exceed receipt total
        var projectedTotal = session.PaidAmount + request.Amount;
        if (projectedTotal > session.TotalAmount + tolerance)
            throw new InvalidOperationException(
                $"Payment would exceed receipt total. Paid: {session.PaidAmount:F2}, This payment: {request.Amount:F2}, Total allowed: {session.TotalAmount:F2}");

        part.PaymentMethod = request.PaymentMethod;
        part.PaymentReference = request.PaymentReference;
        part.Amount = request.Amount;
        part.IsPaid = true;
        part.PaidAt = DateTime.UtcNow;
        part.UpdatedAt = DateTime.UtcNow;

        session.PaidAmount += request.Amount;

        // Check if all parts are paid
        if (session.Parts.All(p => p.IsPaid))
        {
            session.IsComplete = true;
            session.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return part;
    }

    /// <inheritdoc />
    public Task<EqualSplitResult> CalculateEqualSplitAsync(
        decimal totalAmount,
        int numberOfSplits,
        CancellationToken cancellationToken = default)
    {
        if (numberOfSplits <= 0)
            throw new ArgumentException("Number of splits must be greater than 0.");

        var baseAmount = Math.Floor(totalAmount / numberOfSplits * 100) / 100; // Round down to 2 decimal places
        var remainder = totalAmount - (baseAmount * numberOfSplits);

        var splitAmounts = new List<decimal>();
        for (int i = 0; i < numberOfSplits; i++)
        {
            // Add remainder to first split
            var amount = i == 0 ? baseAmount + remainder : baseAmount;
            splitAmounts.Add(amount);
        }

        return Task.FromResult(new EqualSplitResult
        {
            TotalAmount = totalAmount,
            NumberOfSplits = numberOfSplits,
            AmountPerSplit = baseAmount,
            Remainder = remainder,
            SplitAmounts = splitAmounts
        });
    }

    /// <inheritdoc />
    public async Task<ItemSplitResult> CalculateItemSplitAsync(
        int receiptId,
        IEnumerable<ItemSplitAssignment> assignments,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == receiptId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (receipt == null)
            throw new InvalidOperationException($"Receipt {receiptId} not found.");

        var assignmentList = assignments.ToList();
        var result = new ItemSplitResult
        {
            ReceiptId = receiptId,
            TotalAmount = receipt.TotalAmount
        };

        // Validate all items are assigned
        var assignedItemIds = assignmentList.Select(a => a.ItemId).ToHashSet();
        var receiptItemIds = receipt.Items.Select(i => i.Id).ToHashSet();

        if (!assignedItemIds.SetEquals(receiptItemIds))
        {
            result.IsValid = false;
            result.ValidationMessage = "All items must be assigned to a split.";
            return result;
        }

        // Group assignments by part
        var partGroups = assignmentList.GroupBy(a => a.PartNumber);

        foreach (var group in partGroups.OrderBy(g => g.Key))
        {
            var partNumber = group.Key;
            var itemIds = group.Select(a => a.ItemId).ToList();
            var items = receipt.Items.Where(i => itemIds.Contains(i.Id)).ToList();

            var partAmount = items.Sum(i => i.UnitPrice * i.Quantity);

            var part = new ItemSplitPart
            {
                PartNumber = partNumber,
                ItemIds = itemIds,
                Amount = partAmount,
                Items = items.Select(i => new ItemSplitDetail
                {
                    ItemId = i.Id,
                    ProductName = i.Product?.Name ?? "Unknown",
                    Quantity = i.Quantity,
                    Amount = i.UnitPrice * i.Quantity
                }).ToList()
            };

            result.Parts.Add(part);
        }

        result.IsValid = true;
        return result;
    }

    /// <inheritdoc />
    public async Task CancelSplitSessionAsync(
        int sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.SplitPaymentSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (session == null)
            throw new InvalidOperationException($"Split session {sessionId} not found.");

        session.IsDeleted = true;
        session.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SplitPaymentSession> CompleteSplitSessionAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSplitPaymentSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session == null)
            throw new InvalidOperationException($"Split session {sessionId} not found.");

        if (!session.Parts.All(p => p.IsPaid))
            throw new InvalidOperationException("Cannot complete split session with unpaid parts.");

        session.IsComplete = true;
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }

    #endregion

    #region Quick Amount Buttons

    /// <inheritdoc />
    public async Task<IEnumerable<QuickAmountButton>> GetQuickAmountButtonsAsync(
        int storeId,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.QuickAmountButtons
            .Where(b => b.StoreId == storeId && b.IsEnabled && !b.IsDeleted);

        if (!string.IsNullOrEmpty(paymentMethod))
            query = query.Where(b => b.PaymentMethod == null || b.PaymentMethod == paymentMethod);

        return await query
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QuickAmountButton> SaveQuickAmountButtonAsync(
        QuickAmountButton button,
        CancellationToken cancellationToken = default)
    {
        if (button.Id == 0)
        {
            button.CreatedAt = DateTime.UtcNow;
            _context.QuickAmountButtons.Add(button);
        }
        else
        {
            var existing = await _context.QuickAmountButtons
                .FirstOrDefaultAsync(b => b.Id == button.Id && !b.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Quick amount button {button.Id} not found.");

            existing.Label = button.Label;
            existing.Amount = button.Amount;
            existing.ButtonType = button.ButtonType;
            existing.DisplayOrder = button.DisplayOrder;
            existing.IsEnabled = button.IsEnabled;
            existing.ButtonColor = button.ButtonColor;
            existing.TextColor = button.TextColor;
            existing.KeyboardShortcut = button.KeyboardShortcut;
            existing.PaymentMethod = button.PaymentMethod;
            existing.UpdatedAt = DateTime.UtcNow;

            button = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return button;
    }

    /// <inheritdoc />
    public async Task DeleteQuickAmountButtonAsync(int buttonId, CancellationToken cancellationToken = default)
    {
        var button = await _context.QuickAmountButtons
            .FirstOrDefaultAsync(b => b.Id == buttonId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (button == null)
            throw new InvalidOperationException($"Quick amount button {buttonId} not found.");

        button.IsDeleted = true;
        button.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuickAmountButtonSet>> GetQuickAmountButtonSetsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuickAmountButtonSets
            .Where(s => s.StoreId == storeId && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QuickAmountButtonSet> SaveQuickAmountButtonSetAsync(
        QuickAmountButtonSet buttonSet,
        CancellationToken cancellationToken = default)
    {
        if (buttonSet.Id == 0)
        {
            buttonSet.CreatedAt = DateTime.UtcNow;
            _context.QuickAmountButtonSets.Add(buttonSet);
        }
        else
        {
            var existing = await _context.QuickAmountButtonSets
                .FirstOrDefaultAsync(s => s.Id == buttonSet.Id && !s.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Quick amount button set {buttonSet.Id} not found.");

            existing.Name = buttonSet.Name;
            existing.Description = buttonSet.Description;
            existing.IsActive = buttonSet.IsActive;
            existing.PaymentMethod = buttonSet.PaymentMethod;
            existing.ButtonIds = buttonSet.ButtonIds;
            existing.UpdatedAt = DateTime.UtcNow;

            buttonSet = existing;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return buttonSet;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuickAmountButton>> GenerateDefaultQuickAmountButtonsAsync(
        int storeId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<QuickAmountButton>();

        // Define common denominations based on currency
        var denominations = currency.ToUpper() switch
        {
            "KES" => new[] { 50m, 100m, 200m, 500m, 1000m, 2000m, 5000m },
            "USD" => new[] { 1m, 5m, 10m, 20m, 50m, 100m },
            "EUR" => new[] { 5m, 10m, 20m, 50m, 100m, 200m },
            "GBP" => new[] { 5m, 10m, 20m, 50m, 100m },
            _ => new[] { 10m, 20m, 50m, 100m, 200m, 500m, 1000m }
        };

        var displayOrder = 0;
        foreach (var amount in denominations)
        {
            var button = new QuickAmountButton
            {
                StoreId = storeId,
                Label = $"{currency} {amount:N0}",
                Amount = amount,
                ButtonType = "Fixed",
                DisplayOrder = displayOrder++,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.QuickAmountButtons.Add(button);
            buttons.Add(button);
        }

        // Add special buttons
        var exactButton = new QuickAmountButton
        {
            StoreId = storeId,
            Label = "Exact",
            Amount = 0,
            ButtonType = "Custom",
            DisplayOrder = displayOrder++,
            IsEnabled = true,
            ButtonColor = "#28a745",
            CreatedAt = DateTime.UtcNow
        };
        _context.QuickAmountButtons.Add(exactButton);
        buttons.Add(exactButton);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return buttons;
    }

    /// <inheritdoc />
    public Task<decimal> CalculateRoundUpAmountAsync(
        decimal amount,
        decimal roundTo,
        CancellationToken cancellationToken = default)
    {
        if (roundTo <= 0)
            return Task.FromResult(amount);

        var rounded = Math.Ceiling(amount / roundTo) * roundTo;
        return Task.FromResult(rounded);
    }

    #endregion

    #region Private Methods

    private static string GenerateReferenceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        // Use Random.Shared for thread-safe random number generation
        var random = Random.Shared.Next(1000, 9999);
        return $"PKD-{timestamp}-{random}";
    }

    #endregion
}
