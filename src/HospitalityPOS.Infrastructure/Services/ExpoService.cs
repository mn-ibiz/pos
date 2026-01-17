using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for expo station management and all-call messaging.
/// </summary>
public class ExpoService : IExpoService
{
    private readonly IRepository<KdsOrder> _kdsOrderRepository;
    private readonly IRepository<KdsOrderItem> _kdsOrderItemRepository;
    private readonly IRepository<KdsStation> _stationRepository;
    private readonly IRepository<AllCallMessage> _allCallRepository;
    private readonly IRepository<AllCallMessageTarget> _allCallTargetRepository;
    private readonly IRepository<AllCallMessageDismissal> _allCallDismissalRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<KdsOrderStatusLog> _statusLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpoService> _logger;

    public ExpoService(
        IRepository<KdsOrder> kdsOrderRepository,
        IRepository<KdsOrderItem> kdsOrderItemRepository,
        IRepository<KdsStation> stationRepository,
        IRepository<AllCallMessage> allCallRepository,
        IRepository<AllCallMessageTarget> allCallTargetRepository,
        IRepository<AllCallMessageDismissal> allCallDismissalRepository,
        IRepository<User> userRepository,
        IRepository<KdsOrderStatusLog> statusLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExpoService> logger)
    {
        _kdsOrderRepository = kdsOrderRepository ?? throw new ArgumentNullException(nameof(kdsOrderRepository));
        _kdsOrderItemRepository = kdsOrderItemRepository ?? throw new ArgumentNullException(nameof(kdsOrderItemRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _allCallRepository = allCallRepository ?? throw new ArgumentNullException(nameof(allCallRepository));
        _allCallTargetRepository = allCallTargetRepository ?? throw new ArgumentNullException(nameof(allCallTargetRepository));
        _allCallDismissalRepository = allCallDismissalRepository ?? throw new ArgumentNullException(nameof(allCallDismissalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _statusLogRepository = statusLogRepository ?? throw new ArgumentNullException(nameof(statusLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<ExpoOrderViewDto>? OrderComplete;
    public event EventHandler<KdsOrderDto>? OrderServed;
    public event EventHandler<AllCallMessageDto>? AllCallSent;
    public event EventHandler<KdsOrderDto>? OrderSentBack;

    #endregion

    #region Expo Order View

    public async Task<List<ExpoOrderViewDto>> GetAllOrdersAsync(int storeId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var result = new List<ExpoOrderViewDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.IsPriority)
                                          .ThenBy(o => o.ReceivedAt))
        {
            result.Add(await MapToExpoOrderViewAsync(kdsOrder));
        }

        return result;
    }

    public async Task<ExpoOrderViewDto?> GetOrderWithStationStatusAsync(int kdsOrderId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return null;
        }

        return await MapToExpoOrderViewAsync(kdsOrder);
    }

    public async Task<ExpoDisplayDto> GetExpoDisplayAsync(int storeId)
    {
        var allOrders = await GetAllOrdersAsync(storeId);

        var pendingOrders = allOrders
            .Where(o => !o.IsComplete && o.Status != KdsOrderStatusDto.Ready)
            .ToList();

        var readyOrders = allOrders
            .Where(o => o.IsComplete || o.Status == KdsOrderStatusDto.Ready)
            .ToList();

        var activeMessages = await GetStoreMessagesAsync(storeId);

        var stations = await _stationRepository.FindAsync(s =>
            s.StoreId == storeId && s.IsActive);

        return new ExpoDisplayDto
        {
            PendingOrders = pendingOrders,
            ReadyOrders = readyOrders,
            ActiveMessages = activeMessages,
            StationNames = stations.ToDictionary(s => s.Id, s => s.Name),
            LastRefreshTime = DateTime.UtcNow
        };
    }

    public async Task<List<ExpoOrderViewDto>> GetPendingOrdersAsync(int storeId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            (ko.Status == KdsOrderStatus.New || ko.Status == KdsOrderStatus.InProgress) &&
            ko.IsActive);

        var result = new List<ExpoOrderViewDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.IsPriority)
                                          .ThenBy(o => o.ReceivedAt))
        {
            var view = await MapToExpoOrderViewAsync(kdsOrder);
            if (!view.IsComplete)
            {
                result.Add(view);
            }
        }

        return result;
    }

    public async Task<List<ExpoOrderViewDto>> GetReadyOrdersAsync(int storeId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status == KdsOrderStatus.Ready &&
            ko.IsActive);

        var result = new List<ExpoOrderViewDto>();
        foreach (var kdsOrder in kdsOrders.OrderBy(o => o.CompletedAt))
        {
            result.Add(await MapToExpoOrderViewAsync(kdsOrder));
        }

        return result;
    }

    public async Task<List<ExpoOrderViewDto>> GetCompleteOrdersAsync(int storeId)
    {
        var allOrders = await GetAllOrdersAsync(storeId);
        return allOrders.Where(o => o.IsComplete).ToList();
    }

    #endregion

    #region Order Actions

    public async Task<KdsOrderDto> MarkOrderServedAsync(MarkOrderServedDto dto)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(dto.KdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {dto.KdsOrderId} not found.");
        }

        if (kdsOrder.Status != KdsOrderStatus.Ready)
        {
            throw new InvalidOperationException("Can only mark ready orders as served.");
        }

        var previousStatus = kdsOrder.Status;
        kdsOrder.Status = KdsOrderStatus.Served;
        kdsOrder.ServedAt = DateTime.UtcNow;
        kdsOrder.ServedByUserId = dto.UserId;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);

        // Log status change
        await LogStatusChangeAsync(dto.KdsOrderId, null, previousStatus, KdsOrderStatus.Served, dto.UserId);

        await _unitOfWork.SaveChangesAsync();

        var orderDto = await MapToOrderDtoAsync(kdsOrder);

        OrderServed?.Invoke(this, orderDto);

        _logger.LogInformation("Marked KDS order {KdsOrderId} as served by user {UserId}",
            dto.KdsOrderId, dto.UserId);

        return orderDto;
    }

    public async Task<int> BulkMarkServedAsync(List<int> kdsOrderIds, int? userId = null)
    {
        var servedCount = 0;

        foreach (var kdsOrderId in kdsOrderIds)
        {
            try
            {
                await MarkOrderServedAsync(new MarkOrderServedDto
                {
                    KdsOrderId = kdsOrderId,
                    UserId = userId
                });
                servedCount++;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Could not mark order {KdsOrderId} as served: {Message}",
                    kdsOrderId, ex.Message);
            }
        }

        return servedCount;
    }

    public async Task<KdsOrderDto> SendBackToStationAsync(int kdsOrderId, int stationId, string? reason = null, int? userId = null)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {kdsOrderId} not found.");
        }

        var previousStatus = kdsOrder.Status;
        kdsOrder.Status = KdsOrderStatus.Recalled;
        kdsOrder.CompletedAt = null;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        // Reset items at the specified station
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId &&
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        foreach (var item in stationItems)
        {
            item.Status = KdsItemStatus.Pending;
            item.CompletedAt = null;
            item.CompletedByUserId = null;
            item.UpdatedAt = DateTime.UtcNow;
            await _kdsOrderItemRepository.UpdateAsync(item);
        }

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await LogStatusChangeAsync(kdsOrderId, stationId, previousStatus, KdsOrderStatus.Recalled, userId, reason);
        await _unitOfWork.SaveChangesAsync();

        var orderDto = await MapToOrderDtoAsync(kdsOrder);

        OrderSentBack?.Invoke(this, orderDto);

        _logger.LogInformation("Sent KDS order {KdsOrderId} back to station {StationId}. Reason: {Reason}",
            kdsOrderId, stationId, reason);

        return orderDto;
    }

    #endregion

    #region All-Call Messaging

    public async Task<AllCallMessageDto> SendAllCallAsync(SendAllCallDto dto)
    {
        var message = new AllCallMessage
        {
            Message = dto.Message,
            SentByUserId = dto.UserId,
            SentAt = DateTime.UtcNow,
            Priority = dto.Priority == AllCallPriorityDto.Urgent
                ? AllCallPriority.Urgent
                : AllCallPriority.Normal,
            ExpiresAt = dto.ExpirationMinutes.HasValue
                ? DateTime.UtcNow.AddMinutes(dto.ExpirationMinutes.Value)
                : DateTime.UtcNow.AddMinutes(5), // Default 5 minutes
            IsExpired = false,
            StoreId = dto.StoreId,
            IsActive = true
        };

        await _allCallRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        // Add target stations if specified
        if (dto.StationIds != null && dto.StationIds.Any())
        {
            foreach (var stationId in dto.StationIds)
            {
                var target = new AllCallMessageTarget
                {
                    MessageId = message.Id,
                    StationId = stationId,
                    IsActive = true
                };
                await _allCallTargetRepository.AddAsync(target);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var user = await _userRepository.GetByIdAsync(dto.UserId);

        var messageDto = new AllCallMessageDto
        {
            Id = message.Id,
            Message = message.Message,
            SentByUserId = message.SentByUserId,
            SentByUserName = user?.Username,
            SentAt = message.SentAt,
            Priority = dto.Priority,
            ExpiresAt = message.ExpiresAt,
            IsExpired = false,
            StoreId = dto.StoreId,
            TargetStationIds = dto.StationIds ?? new List<int>()
        };

        AllCallSent?.Invoke(this, messageDto);

        _logger.LogInformation("Sent all-call message {MessageId} to {TargetCount} stations in store {StoreId}",
            message.Id,
            dto.StationIds?.Count ?? 0,
            dto.StoreId);

        return messageDto;
    }

    public async Task<AllCallDismissalDto> DismissAllCallAsync(DismissAllCallDto dto)
    {
        var message = await _allCallRepository.GetByIdAsync(dto.MessageId);
        if (message == null || !message.IsActive)
        {
            throw new InvalidOperationException($"All-call message with ID {dto.MessageId} not found.");
        }

        // Check if already dismissed at this station
        var existingDismissals = await _allCallDismissalRepository.FindAsync(d =>
            d.MessageId == dto.MessageId &&
            d.StationId == dto.StationId &&
            d.IsActive);

        if (existingDismissals.Any())
        {
            throw new InvalidOperationException("Message already dismissed at this station.");
        }

        var dismissal = new AllCallMessageDismissal
        {
            MessageId = dto.MessageId,
            StationId = dto.StationId,
            DismissedAt = DateTime.UtcNow,
            DismissedByUserId = dto.UserId,
            IsActive = true
        };

        await _allCallDismissalRepository.AddAsync(dismissal);
        await _unitOfWork.SaveChangesAsync();

        var station = await _stationRepository.GetByIdAsync(dto.StationId);
        var user = dto.UserId.HasValue ? await _userRepository.GetByIdAsync(dto.UserId.Value) : null;

        _logger.LogInformation("Dismissed all-call message {MessageId} at station {StationId}",
            dto.MessageId, dto.StationId);

        return new AllCallDismissalDto
        {
            Id = dismissal.Id,
            MessageId = dto.MessageId,
            StationId = dto.StationId,
            StationName = station?.Name,
            DismissedAt = dismissal.DismissedAt,
            DismissedByUserId = dto.UserId,
            DismissedByUserName = user?.Username
        };
    }

    public async Task<List<AllCallMessageDto>> GetActiveMessagesAsync(int stationId)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            return new List<AllCallMessageDto>();
        }

        // Get messages for this store that are not expired
        var messages = await _allCallRepository.FindAsync(m =>
            m.StoreId == station.StoreId &&
            !m.IsExpired &&
            (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow) &&
            m.IsActive);

        var result = new List<AllCallMessageDto>();
        foreach (var message in messages.OrderByDescending(m => m.Priority)
                                        .ThenByDescending(m => m.SentAt))
        {
            // Check if targeted to this station or all stations
            var targets = await _allCallTargetRepository.FindAsync(t =>
                t.MessageId == message.Id && t.IsActive);

            var targetStationIds = targets.Where(t => t.StationId.HasValue)
                                         .Select(t => t.StationId!.Value)
                                         .ToList();

            // If no targets, it's for all stations
            // If there are targets, check if this station is included
            if (targetStationIds.Any() && !targetStationIds.Contains(stationId))
            {
                continue;
            }

            // Check if dismissed at this station
            var dismissals = await _allCallDismissalRepository.FindAsync(d =>
                d.MessageId == message.Id &&
                d.StationId == stationId &&
                d.IsActive);

            if (dismissals.Any())
            {
                continue;
            }

            var user = await _userRepository.GetByIdAsync(message.SentByUserId);

            result.Add(new AllCallMessageDto
            {
                Id = message.Id,
                Message = message.Message,
                SentByUserId = message.SentByUserId,
                SentByUserName = user?.Username,
                SentAt = message.SentAt,
                Priority = message.Priority == AllCallPriority.Urgent
                    ? AllCallPriorityDto.Urgent
                    : AllCallPriorityDto.Normal,
                ExpiresAt = message.ExpiresAt,
                IsExpired = message.IsExpired,
                StoreId = message.StoreId,
                TargetStationIds = targetStationIds
            });
        }

        return result;
    }

    public async Task<List<AllCallMessageDto>> GetStoreMessagesAsync(int storeId)
    {
        var messages = await _allCallRepository.FindAsync(m =>
            m.StoreId == storeId &&
            !m.IsExpired &&
            (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow) &&
            m.IsActive);

        var result = new List<AllCallMessageDto>();
        foreach (var message in messages.OrderByDescending(m => m.Priority)
                                        .ThenByDescending(m => m.SentAt))
        {
            var user = await _userRepository.GetByIdAsync(message.SentByUserId);
            var targets = await _allCallTargetRepository.FindAsync(t =>
                t.MessageId == message.Id && t.IsActive);

            result.Add(new AllCallMessageDto
            {
                Id = message.Id,
                Message = message.Message,
                SentByUserId = message.SentByUserId,
                SentByUserName = user?.Username,
                SentAt = message.SentAt,
                Priority = message.Priority == AllCallPriority.Urgent
                    ? AllCallPriorityDto.Urgent
                    : AllCallPriorityDto.Normal,
                ExpiresAt = message.ExpiresAt,
                IsExpired = message.IsExpired,
                StoreId = message.StoreId,
                TargetStationIds = targets.Where(t => t.StationId.HasValue)
                                         .Select(t => t.StationId!.Value)
                                         .ToList()
            });
        }

        return result;
    }

    public async Task<List<AllCallMessageDto>> GetMessageHistoryAsync(int storeId, DateTime fromDate, DateTime toDate)
    {
        var messages = await _allCallRepository.FindAsync(m =>
            m.StoreId == storeId &&
            m.SentAt >= fromDate &&
            m.SentAt <= toDate &&
            m.IsActive);

        var result = new List<AllCallMessageDto>();
        foreach (var message in messages.OrderByDescending(m => m.SentAt))
        {
            var user = await _userRepository.GetByIdAsync(message.SentByUserId);
            var targets = await _allCallTargetRepository.FindAsync(t =>
                t.MessageId == message.Id && t.IsActive);

            result.Add(new AllCallMessageDto
            {
                Id = message.Id,
                Message = message.Message,
                SentByUserId = message.SentByUserId,
                SentByUserName = user?.Username,
                SentAt = message.SentAt,
                Priority = message.Priority == AllCallPriority.Urgent
                    ? AllCallPriorityDto.Urgent
                    : AllCallPriorityDto.Normal,
                ExpiresAt = message.ExpiresAt,
                IsExpired = message.IsExpired,
                StoreId = message.StoreId,
                TargetStationIds = targets.Where(t => t.StationId.HasValue)
                                         .Select(t => t.StationId!.Value)
                                         .ToList()
            });
        }

        return result;
    }

    public async Task<int> ExpireOldMessagesAsync()
    {
        var expiredMessages = await _allCallRepository.FindAsync(m =>
            !m.IsExpired &&
            m.ExpiresAt.HasValue &&
            m.ExpiresAt <= DateTime.UtcNow &&
            m.IsActive);

        var expiredCount = 0;
        foreach (var message in expiredMessages)
        {
            message.IsExpired = true;
            message.UpdatedAt = DateTime.UtcNow;
            await _allCallRepository.UpdateAsync(message);
            expiredCount++;
        }

        if (expiredCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Expired {ExpiredCount} all-call messages", expiredCount);
        }

        return expiredCount;
    }

    public async Task<List<AllCallDismissalDto>> GetMessageDismissalsAsync(int messageId)
    {
        var dismissals = await _allCallDismissalRepository.FindAsync(d =>
            d.MessageId == messageId && d.IsActive);

        var result = new List<AllCallDismissalDto>();
        foreach (var dismissal in dismissals.OrderBy(d => d.DismissedAt))
        {
            var station = await _stationRepository.GetByIdAsync(dismissal.StationId);
            var user = dismissal.DismissedByUserId.HasValue
                ? await _userRepository.GetByIdAsync(dismissal.DismissedByUserId.Value)
                : null;

            result.Add(new AllCallDismissalDto
            {
                Id = dismissal.Id,
                MessageId = dismissal.MessageId,
                StationId = dismissal.StationId,
                StationName = station?.Name,
                DismissedAt = dismissal.DismissedAt,
                DismissedByUserId = dismissal.DismissedByUserId,
                DismissedByUserName = user?.Username
            });
        }

        return result;
    }

    #endregion

    #region Statistics

    public async Task<ExpoSummaryDto> GetExpoSummaryAsync(int storeId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var stations = await _stationRepository.FindAsync(s =>
            s.StoreId == storeId && !s.IsExpo && s.IsActive);

        var ordersByStation = new Dictionary<int, int>();
        foreach (var station in stations)
        {
            var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
                oi.StationId == station.Id &&
                oi.Status != KdsItemStatus.Done &&
                oi.Status != KdsItemStatus.Voided &&
                oi.IsActive);

            ordersByStation[station.Id] = stationItems.Select(oi => oi.KdsOrderId).Distinct().Count();
        }

        var overdueCount = kdsOrders.Count(ko =>
            (DateTime.UtcNow - ko.ReceivedAt).TotalMinutes > 15);

        // Calculate average wait time for completed orders today
        var todayStart = DateTime.UtcNow.Date;
        var completedToday = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status == KdsOrderStatus.Served &&
            ko.ServedAt >= todayStart &&
            ko.IsActive);

        var avgWaitTime = TimeSpan.Zero;
        var avgPrepTime = TimeSpan.Zero;

        if (completedToday.Any())
        {
            var waitTimes = completedToday
                .Where(ko => ko.StartedAt.HasValue)
                .Select(ko => ko.StartedAt!.Value - ko.ReceivedAt)
                .ToList();

            if (waitTimes.Any())
            {
                avgWaitTime = TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks));
            }

            var prepTimes = completedToday
                .Where(ko => ko.StartedAt.HasValue && ko.CompletedAt.HasValue)
                .Select(ko => ko.CompletedAt!.Value - ko.StartedAt!.Value)
                .ToList();

            if (prepTimes.Any())
            {
                avgPrepTime = TimeSpan.FromTicks((long)prepTimes.Average(t => t.Ticks));
            }
        }

        return new ExpoSummaryDto
        {
            TotalActiveOrders = kdsOrders.Count(),
            TotalPendingOrders = kdsOrders.Count(ko => ko.Status == KdsOrderStatus.New),
            TotalInProgressOrders = kdsOrders.Count(ko => ko.Status == KdsOrderStatus.InProgress),
            TotalReadyOrders = kdsOrders.Count(ko => ko.Status == KdsOrderStatus.Ready),
            TotalOverdueOrders = overdueCount,
            AverageWaitTime = avgWaitTime,
            AveragePrepTime = avgPrepTime,
            OrdersByStation = ordersByStation
        };
    }

    public async Task<KdsPerformanceSummaryDto> GetPerformanceSummaryAsync(int storeId, DateTime fromDate, DateTime toDate)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.ReceivedAt >= fromDate &&
            ko.ReceivedAt <= toDate &&
            ko.IsActive);

        var completedOrders = kdsOrders.Where(ko => ko.Status == KdsOrderStatus.Served).ToList();
        var overdueOrders = completedOrders.Count(ko =>
            ko.CompletedAt.HasValue &&
            (ko.CompletedAt.Value - ko.ReceivedAt).TotalMinutes > 15);

        var avgPrepTime = TimeSpan.Zero;
        var avgWaitTime = TimeSpan.Zero;

        if (completedOrders.Any())
        {
            var prepTimes = completedOrders
                .Where(ko => ko.StartedAt.HasValue && ko.CompletedAt.HasValue)
                .Select(ko => ko.CompletedAt!.Value - ko.StartedAt!.Value)
                .ToList();

            if (prepTimes.Any())
            {
                avgPrepTime = TimeSpan.FromTicks((long)prepTimes.Average(t => t.Ticks));
            }

            var waitTimes = completedOrders
                .Where(ko => ko.StartedAt.HasValue)
                .Select(ko => ko.StartedAt!.Value - ko.ReceivedAt)
                .ToList();

            if (waitTimes.Any())
            {
                avgWaitTime = TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks));
            }
        }

        var onTimePercentage = completedOrders.Any()
            ? (decimal)(completedOrders.Count - overdueOrders) / completedOrders.Count * 100
            : 100m;

        // Get station stats
        var stations = await _stationRepository.FindAsync(s =>
            s.StoreId == storeId && !s.IsExpo && s.IsActive);

        var stationStats = new List<KdsStationStatsDto>();
        foreach (var station in stations)
        {
            var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
                oi.StationId == station.Id &&
                oi.CreatedAt >= fromDate &&
                oi.CreatedAt <= toDate &&
                oi.IsActive);

            var completedItems = stationItems.Where(oi =>
                oi.Status == KdsItemStatus.Done &&
                oi.StartedAt.HasValue &&
                oi.CompletedAt.HasValue).ToList();

            var stationPrepTimes = completedItems
                .Select(oi => oi.CompletedAt!.Value - oi.StartedAt!.Value)
                .ToList();

            stationStats.Add(new KdsStationStatsDto
            {
                StationId = station.Id,
                StationName = station.Name,
                TotalOrdersToday = stationItems.Select(oi => oi.KdsOrderId).Distinct().Count(),
                CompletedOrdersToday = completedItems.Select(oi => oi.KdsOrderId).Distinct().Count(),
                AveragePrepTime = stationPrepTimes.Any()
                    ? TimeSpan.FromTicks((long)stationPrepTimes.Average(t => t.Ticks))
                    : TimeSpan.Zero,
                FastestPrepTime = stationPrepTimes.Any() ? stationPrepTimes.Min() : TimeSpan.Zero,
                SlowestPrepTime = stationPrepTimes.Any() ? stationPrepTimes.Max() : TimeSpan.Zero,
                CurrentActiveOrders = 0, // Would need current state
                CurrentOverdueOrders = 0
            });
        }

        return new KdsPerformanceSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalOrders = kdsOrders.Count(),
            CompletedOrders = completedOrders.Count,
            OverdueOrders = overdueOrders,
            OnTimePercentage = onTimePercentage,
            AveragePrepTime = avgPrepTime,
            AverageWaitTime = avgWaitTime,
            StationStats = stationStats
        };
    }

    public async Task<TimeSpan> GetAverageServiceTimeAsync(int storeId, DateTime fromDate, DateTime toDate)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status == KdsOrderStatus.Served &&
            ko.ServedAt >= fromDate &&
            ko.ServedAt <= toDate &&
            ko.IsActive);

        if (!kdsOrders.Any())
        {
            return TimeSpan.Zero;
        }

        var serviceTimes = kdsOrders
            .Select(ko => ko.ServedAt!.Value - ko.ReceivedAt)
            .ToList();

        return TimeSpan.FromTicks((long)serviceTimes.Average(t => t.Ticks));
    }

    #endregion

    #region Private Helpers

    private async Task LogStatusChangeAsync(int kdsOrderId, int? stationId, KdsOrderStatus previousStatus,
        KdsOrderStatus newStatus, int? userId, string? notes = null)
    {
        var log = new KdsOrderStatusLog
        {
            KdsOrderId = kdsOrderId,
            StationId = stationId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Notes = notes,
            IsActive = true
        };

        await _statusLogRepository.AddAsync(log);
    }

    private async Task<ExpoOrderViewDto> MapToExpoOrderViewAsync(KdsOrder kdsOrder)
    {
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrder.Id && oi.IsActive);

        var stationStatuses = items
            .GroupBy(i => i.StationId)
            .Select(g => new { StationId = g.Key, Items = g.ToList() })
            .ToList();

        var stationStatusDtos = new List<ExpoStationStatusDto>();
        foreach (var group in stationStatuses)
        {
            var station = await _stationRepository.GetByIdAsync(group.StationId);
            var totalItems = group.Items.Count;
            var completedItems = group.Items.Count(i => i.Status == KdsItemStatus.Done);

            stationStatusDtos.Add(new ExpoStationStatusDto
            {
                StationId = group.StationId,
                StationName = station?.Name ?? "Unknown",
                TotalItems = totalItems,
                CompletedItems = completedItems,
                Status = completedItems == totalItems
                    ? KdsItemStatusDto.Done
                    : group.Items.Any(i => i.Status == KdsItemStatus.Preparing)
                        ? KdsItemStatusDto.Preparing
                        : KdsItemStatusDto.Pending
            });
        }

        var isComplete = stationStatusDtos.All(s => s.IsComplete);

        return new ExpoOrderViewDto
        {
            KdsOrderId = kdsOrder.Id,
            OrderNumber = kdsOrder.OrderNumber,
            TableNumber = kdsOrder.TableNumber,
            GuestCount = kdsOrder.GuestCount,
            ReceivedAt = kdsOrder.ReceivedAt,
            IsComplete = isComplete,
            Status = MapOrderStatusToDto(kdsOrder.Status),
            Priority = MapPriorityToDto(kdsOrder.Priority),
            TimerColor = CalculateTimerColor(DateTime.UtcNow - kdsOrder.ReceivedAt),
            StationStatuses = stationStatusDtos
        };
    }

    private async Task<KdsOrderDto> MapToOrderDtoAsync(KdsOrder kdsOrder)
    {
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrder.Id && oi.IsActive);

        var itemDtos = new List<KdsOrderItemDto>();
        foreach (var item in items.OrderBy(i => i.SequenceNumber))
        {
            var station = await _stationRepository.GetByIdAsync(item.StationId);
            itemDtos.Add(MapToItemDto(item, station?.Name));
        }

        return new KdsOrderDto
        {
            Id = kdsOrder.Id,
            OrderId = kdsOrder.OrderId,
            OrderNumber = kdsOrder.OrderNumber,
            TableNumber = kdsOrder.TableNumber,
            CustomerName = kdsOrder.CustomerName,
            GuestCount = kdsOrder.GuestCount,
            ReceivedAt = kdsOrder.ReceivedAt,
            Status = MapOrderStatusToDto(kdsOrder.Status),
            StartedAt = kdsOrder.StartedAt,
            CompletedAt = kdsOrder.CompletedAt,
            ServedAt = kdsOrder.ServedAt,
            Priority = MapPriorityToDto(kdsOrder.Priority),
            IsPriority = kdsOrder.IsPriority,
            Notes = kdsOrder.Notes,
            Items = itemDtos
        };
    }

    private static KdsOrderItemDto MapToItemDto(KdsOrderItem item, string? stationName = null)
    {
        return new KdsOrderItemDto
        {
            Id = item.Id,
            KdsOrderId = item.KdsOrderId,
            OrderItemId = item.OrderItemId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Modifiers = ParseModifiers(item.Modifiers),
            SpecialInstructions = item.SpecialInstructions,
            StationId = item.StationId,
            StationName = stationName,
            Status = MapItemStatusToDto(item.Status),
            StartedAt = item.StartedAt,
            CompletedAt = item.CompletedAt,
            SequenceNumber = item.SequenceNumber,
            CourseNumber = item.CourseNumber
        };
    }

    private static List<string> ParseModifiers(string? modifiers)
    {
        if (string.IsNullOrWhiteSpace(modifiers))
        {
            return new List<string>();
        }
        return modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(m => m.Trim())
                       .ToList();
    }

    private static TimerColorDto CalculateTimerColor(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes <= 5)
            return TimerColorDto.Green;
        if (elapsed.TotalMinutes <= 10)
            return TimerColorDto.Yellow;
        return TimerColorDto.Red;
    }

    private static KdsOrderStatusDto MapOrderStatusToDto(KdsOrderStatus entity)
    {
        return entity switch
        {
            KdsOrderStatus.New => KdsOrderStatusDto.New,
            KdsOrderStatus.InProgress => KdsOrderStatusDto.InProgress,
            KdsOrderStatus.Ready => KdsOrderStatusDto.Ready,
            KdsOrderStatus.Served => KdsOrderStatusDto.Served,
            KdsOrderStatus.Recalled => KdsOrderStatusDto.Recalled,
            KdsOrderStatus.Voided => KdsOrderStatusDto.Voided,
            _ => KdsOrderStatusDto.New
        };
    }

    private static OrderPriorityDto MapPriorityToDto(OrderPriority entity)
    {
        return entity switch
        {
            OrderPriority.Normal => OrderPriorityDto.Normal,
            OrderPriority.Rush => OrderPriorityDto.Rush,
            OrderPriority.VIP => OrderPriorityDto.VIP,
            _ => OrderPriorityDto.Normal
        };
    }

    private static KdsItemStatusDto MapItemStatusToDto(KdsItemStatus entity)
    {
        return entity switch
        {
            KdsItemStatus.Pending => KdsItemStatusDto.Pending,
            KdsItemStatus.Preparing => KdsItemStatusDto.Preparing,
            KdsItemStatus.Done => KdsItemStatusDto.Done,
            KdsItemStatus.Voided => KdsItemStatusDto.Voided,
            _ => KdsItemStatusDto.Pending
        };
    }

    #endregion
}
