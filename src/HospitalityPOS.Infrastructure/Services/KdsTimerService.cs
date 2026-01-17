using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for KDS order timing and priority management.
/// </summary>
public class KdsTimerService : IKdsTimerService
{
    private readonly IRepository<KdsOrder> _kdsOrderRepository;
    private readonly IRepository<KdsOrderItem> _kdsOrderItemRepository;
    private readonly IRepository<KdsTimerConfig> _timerConfigRepository;
    private readonly IRepository<KdsStation> _stationRepository;
    private readonly IRepository<KdsDisplaySettings> _displaySettingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KdsTimerService> _logger;

    // Default configuration values
    private const int DefaultGreenMinutes = 5;
    private const int DefaultYellowMinutes = 10;
    private const int DefaultRedMinutes = 15;

    public KdsTimerService(
        IRepository<KdsOrder> kdsOrderRepository,
        IRepository<KdsOrderItem> kdsOrderItemRepository,
        IRepository<KdsTimerConfig> timerConfigRepository,
        IRepository<KdsStation> stationRepository,
        IRepository<KdsDisplaySettings> displaySettingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<KdsTimerService> logger)
    {
        _kdsOrderRepository = kdsOrderRepository ?? throw new ArgumentNullException(nameof(kdsOrderRepository));
        _kdsOrderItemRepository = kdsOrderItemRepository ?? throw new ArgumentNullException(nameof(kdsOrderItemRepository));
        _timerConfigRepository = timerConfigRepository ?? throw new ArgumentNullException(nameof(timerConfigRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _displaySettingsRepository = displaySettingsRepository ?? throw new ArgumentNullException(nameof(displaySettingsRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<KdsOrderDto>? OrderOverdue;
    public event EventHandler<KdsOrderPriorityChangeEventArgs>? PriorityChanged;
    public event EventHandler<AudioAlertEventArgs>? AudioAlertNeeded;

    #endregion

    #region Timer Status

    public async Task<KdsTimerStatusDto> GetOrderTimerStatusAsync(int kdsOrderId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {kdsOrderId} not found.");
        }

        return GetOrderTimerStatus(kdsOrder);
    }

    public KdsTimerStatusDto GetOrderTimerStatus(KdsOrder order)
    {
        var elapsed = DateTime.UtcNow - order.ReceivedAt;

        var status = new KdsTimerStatusDto
        {
            Elapsed = elapsed,
            Color = CalculateTimerColorInternal(elapsed),
            DisplayTime = FormatDisplayTime(elapsed)
        };

        status.IsOverdue = status.Color == TimerColorDto.Red;
        status.ShouldFlash = status.IsOverdue;
        status.ShouldPlayAudio = status.IsOverdue;

        return status;
    }

    public async Task<Dictionary<int, KdsTimerStatusDto>> GetOrderTimerStatusesAsync(List<int> kdsOrderIds)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) && ko.IsActive);

        return kdsOrders.ToDictionary(
            ko => ko.Id,
            ko => GetOrderTimerStatus(ko));
    }

    public TimerColorDto CalculateTimerColor(TimeSpan elapsed, int? storeId = null)
    {
        // For now, use default thresholds
        // In a full implementation, would fetch store-specific config
        return CalculateTimerColorInternal(elapsed);
    }

    private static TimerColorDto CalculateTimerColorInternal(TimeSpan elapsed,
        int greenThreshold = DefaultGreenMinutes,
        int yellowThreshold = DefaultYellowMinutes)
    {
        if (elapsed.TotalMinutes <= greenThreshold)
            return TimerColorDto.Green;
        if (elapsed.TotalMinutes <= yellowThreshold)
            return TimerColorDto.Yellow;
        return TimerColorDto.Red;
    }

    #endregion

    #region Priority Management

    public async Task<KdsOrderDto> MarkAsRushAsync(MarkRushOrderDto dto)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(dto.KdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {dto.KdsOrderId} not found.");
        }

        var previousPriority = kdsOrder.Priority;
        kdsOrder.Priority = MapToEntity(dto.Priority);
        kdsOrder.IsPriority = dto.Priority != OrderPriorityDto.Normal;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        PriorityChanged?.Invoke(this, new KdsOrderPriorityChangeEventArgs
        {
            KdsOrderId = dto.KdsOrderId,
            PreviousPriority = MapToDto(previousPriority),
            NewPriority = dto.Priority,
            UserId = dto.UserId
        });

        _logger.LogInformation("Marked KDS order {KdsOrderId} as {Priority} by user {UserId}",
            dto.KdsOrderId, dto.Priority, dto.UserId);

        return await MapToOrderDtoAsync(kdsOrder);
    }

    public async Task<KdsOrderDto> ClearRushAsync(int kdsOrderId, int? userId = null)
    {
        return await UpdatePriorityAsync(kdsOrderId, OrderPriorityDto.Normal, userId);
    }

    public async Task<KdsOrderDto> UpdatePriorityAsync(int kdsOrderId, OrderPriorityDto priority, int? userId = null)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {kdsOrderId} not found.");
        }

        var previousPriority = kdsOrder.Priority;
        kdsOrder.Priority = MapToEntity(priority);
        kdsOrder.IsPriority = priority != OrderPriorityDto.Normal;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        PriorityChanged?.Invoke(this, new KdsOrderPriorityChangeEventArgs
        {
            KdsOrderId = kdsOrderId,
            PreviousPriority = MapToDto(previousPriority),
            NewPriority = priority,
            UserId = userId
        });

        _logger.LogInformation("Updated KDS order {KdsOrderId} priority to {Priority}", kdsOrderId, priority);

        return await MapToOrderDtoAsync(kdsOrder);
    }

    public async Task<List<KdsOrderDto>> GetPriorityOrdersAsync(int stationId)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.IsPriority &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.Priority)
                                          .ThenBy(o => o.ReceivedAt))
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    #endregion

    #region Overdue Detection

    public async Task<List<KdsOrderDto>> GetOverdueOrdersAsync(int stationId)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var overdueOrders = kdsOrders
            .Where(ko => (DateTime.UtcNow - ko.ReceivedAt).TotalMinutes > DefaultRedMinutes)
            .OrderBy(ko => ko.ReceivedAt);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in overdueOrders)
        {
            var dto = await MapToOrderDtoAsync(kdsOrder);
            result.Add(dto);
        }

        return result;
    }

    public async Task<OverdueOrdersSummaryDto> GetOverdueOrdersSummaryAsync(int stationId)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var now = DateTime.UtcNow;
        var overdueOrders = new List<KdsOrderListDto>();
        var yellowCount = 0;
        var redCount = 0;
        TimeSpan oldestAge = TimeSpan.Zero;

        foreach (var kdsOrder in kdsOrders)
        {
            var elapsed = now - kdsOrder.ReceivedAt;
            var color = CalculateTimerColorInternal(elapsed);

            if (color == TimerColorDto.Yellow)
                yellowCount++;
            else if (color == TimerColorDto.Red)
                redCount++;

            if (elapsed > oldestAge)
                oldestAge = elapsed;

            if (color == TimerColorDto.Red)
            {
                var items = await _kdsOrderItemRepository.FindAsync(oi =>
                    oi.KdsOrderId == kdsOrder.Id && oi.IsActive);

                overdueOrders.Add(new KdsOrderListDto
                {
                    Id = kdsOrder.Id,
                    OrderNumber = kdsOrder.OrderNumber,
                    TableNumber = kdsOrder.TableNumber,
                    GuestCount = kdsOrder.GuestCount,
                    ReceivedAt = kdsOrder.ReceivedAt,
                    Status = MapOrderStatusToDto(kdsOrder.Status),
                    Priority = MapToDto(kdsOrder.Priority),
                    IsPriority = kdsOrder.IsPriority,
                    ItemCount = items.Count(),
                    CompletedItemCount = items.Count(i => i.Status == KdsItemStatus.Done),
                    TimerColor = TimerColorDto.Red,
                    IsOverdue = true,
                    ShouldFlash = true
                });
            }
        }

        return new OverdueOrdersSummaryDto
        {
            StationId = stationId,
            StationName = station.Name,
            TotalOverdue = redCount,
            YellowCount = yellowCount,
            RedCount = redCount,
            OldestOrderAge = oldestAge,
            OverdueOrders = overdueOrders.OrderBy(o => o.ReceivedAt).ToList()
        };
    }

    public async Task<bool> IsOrderOverdueAsync(int kdsOrderId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - kdsOrder.ReceivedAt;
        return elapsed.TotalMinutes > DefaultRedMinutes;
    }

    public async Task<List<KdsOrderDto>> GetAllOverdueOrdersAsync(int storeId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.StoreId == storeId &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var overdueOrders = kdsOrders
            .Where(ko => (DateTime.UtcNow - ko.ReceivedAt).TotalMinutes > DefaultRedMinutes)
            .OrderBy(ko => ko.ReceivedAt);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in overdueOrders)
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    #endregion

    #region Timer Configuration

    public async Task<KdsTimerConfigDto> GetTimerConfigAsync(int storeId)
    {
        var configs = await _timerConfigRepository.FindAsync(c =>
            c.StoreId == storeId && c.IsActive);

        var config = configs.FirstOrDefault();
        if (config == null)
        {
            return GetDefaultTimerConfig();
        }

        return MapConfigToDto(config);
    }

    public async Task<KdsTimerConfigDto> UpdateTimerConfigAsync(int storeId, UpdateKdsTimerConfigDto dto)
    {
        var configs = await _timerConfigRepository.FindAsync(c =>
            c.StoreId == storeId && c.IsActive);

        var config = configs.FirstOrDefault();
        if (config == null)
        {
            return await CreateTimerConfigAsync(storeId);
        }

        if (dto.GreenThresholdMinutes.HasValue) config.GreenThresholdMinutes = dto.GreenThresholdMinutes.Value;
        if (dto.YellowThresholdMinutes.HasValue) config.YellowThresholdMinutes = dto.YellowThresholdMinutes.Value;
        if (dto.RedThresholdMinutes.HasValue) config.RedThresholdMinutes = dto.RedThresholdMinutes.Value;
        if (dto.FlashWhenOverdue.HasValue) config.FlashWhenOverdue = dto.FlashWhenOverdue.Value;
        if (dto.FlashIntervalSeconds.HasValue) config.FlashIntervalSeconds = dto.FlashIntervalSeconds.Value;
        if (dto.AudioAlertOnOverdue.HasValue) config.AudioAlertOnOverdue = dto.AudioAlertOnOverdue.Value;
        if (dto.AudioRepeatIntervalSeconds.HasValue) config.AudioRepeatIntervalSeconds = dto.AudioRepeatIntervalSeconds.Value;

        config.UpdatedAt = DateTime.UtcNow;
        await _timerConfigRepository.UpdateAsync(config);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated timer configuration for store {StoreId}", storeId);

        return MapConfigToDto(config);
    }

    public async Task<KdsTimerConfigDto> CreateTimerConfigAsync(int storeId)
    {
        var config = new KdsTimerConfig
        {
            StoreId = storeId,
            GreenThresholdMinutes = DefaultGreenMinutes,
            YellowThresholdMinutes = DefaultYellowMinutes,
            RedThresholdMinutes = DefaultRedMinutes,
            FlashWhenOverdue = true,
            FlashIntervalSeconds = 2,
            AudioAlertOnOverdue = true,
            AudioRepeatIntervalSeconds = 30,
            IsActive = true
        };

        await _timerConfigRepository.AddAsync(config);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created timer configuration for store {StoreId}", storeId);

        return MapConfigToDto(config);
    }

    public KdsTimerConfigDto GetDefaultTimerConfig()
    {
        return new KdsTimerConfigDto
        {
            Id = 0,
            GreenThresholdMinutes = DefaultGreenMinutes,
            YellowThresholdMinutes = DefaultYellowMinutes,
            RedThresholdMinutes = DefaultRedMinutes,
            FlashWhenOverdue = true,
            FlashIntervalSeconds = 2,
            AudioAlertOnOverdue = true,
            AudioRepeatIntervalSeconds = 30,
            StoreId = 0
        };
    }

    #endregion

    #region Alert Management

    public async Task<bool> ShouldPlayAudioAlertAsync(int kdsOrderId, DateTime? lastAlertTime = null)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - kdsOrder.ReceivedAt;
        if (elapsed.TotalMinutes <= DefaultRedMinutes)
        {
            return false;
        }

        // Get config for audio repeat interval
        var config = await GetTimerConfigAsync(kdsOrder.StoreId);
        if (!config.AudioAlertOnOverdue)
        {
            return false;
        }

        if (!lastAlertTime.HasValue)
        {
            return true;
        }

        var timeSinceLastAlert = DateTime.UtcNow - lastAlertTime.Value;
        return timeSinceLastAlert.TotalSeconds >= config.AudioRepeatIntervalSeconds;
    }

    public async Task<bool> ShouldFlashOrderAsync(int kdsOrderId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - kdsOrder.ReceivedAt;
        if (elapsed.TotalMinutes <= DefaultRedMinutes)
        {
            return false;
        }

        var config = await GetTimerConfigAsync(kdsOrder.StoreId);
        return config.FlashWhenOverdue;
    }

    public async Task<List<int>> GetOrdersNeedingAlertsAsync(int stationId)
    {
        var overdueOrders = await GetOverdueOrdersAsync(stationId);

        // For now, return all overdue order IDs
        // In a full implementation, would track last alert times
        return overdueOrders.Select(o => o.Id).ToList();
    }

    #endregion

    #region Statistics

    public async Task<Dictionary<KdsOrderStatusDto, TimeSpan>> GetAverageTimeByStatusAsync(
        int stationId, DateTime fromDate, DateTime toDate)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.CreatedAt >= fromDate &&
            oi.CreatedAt <= toDate &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status == KdsOrderStatus.Served &&
            ko.IsActive);

        var result = new Dictionary<KdsOrderStatusDto, TimeSpan>();

        // Time in New status (waiting to start)
        var waitTimes = kdsOrders
            .Where(ko => ko.StartedAt.HasValue)
            .Select(ko => ko.StartedAt!.Value - ko.ReceivedAt)
            .ToList();

        if (waitTimes.Any())
        {
            result[KdsOrderStatusDto.New] = TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks));
        }

        // Time in InProgress status (prep time)
        var prepTimes = kdsOrders
            .Where(ko => ko.StartedAt.HasValue && ko.CompletedAt.HasValue)
            .Select(ko => ko.CompletedAt!.Value - ko.StartedAt!.Value)
            .ToList();

        if (prepTimes.Any())
        {
            result[KdsOrderStatusDto.InProgress] = TimeSpan.FromTicks((long)prepTimes.Average(t => t.Ticks));
        }

        // Time in Ready status (waiting to serve)
        var serveTimes = kdsOrders
            .Where(ko => ko.CompletedAt.HasValue && ko.ServedAt.HasValue)
            .Select(ko => ko.ServedAt!.Value - ko.CompletedAt!.Value)
            .ToList();

        if (serveTimes.Any())
        {
            result[KdsOrderStatusDto.Ready] = TimeSpan.FromTicks((long)serveTimes.Average(t => t.Ticks));
        }

        return result;
    }

    public async Task<decimal> GetOnTimePercentageAsync(int stationId, DateTime fromDate, DateTime toDate)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.CreatedAt >= fromDate &&
            oi.CreatedAt <= toDate &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status == KdsOrderStatus.Served &&
            ko.CompletedAt.HasValue &&
            ko.IsActive);

        if (!kdsOrders.Any())
        {
            return 100m; // No orders, 100% on time
        }

        var totalOrders = kdsOrders.Count();
        var onTimeOrders = kdsOrders.Count(ko =>
            (ko.CompletedAt!.Value - ko.ReceivedAt).TotalMinutes <= DefaultRedMinutes);

        return (decimal)onTimeOrders / totalOrders * 100;
    }

    #endregion

    #region Private Helpers

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

        var timerStatus = GetOrderTimerStatus(kdsOrder);

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
            Priority = MapToDto(kdsOrder.Priority),
            IsPriority = kdsOrder.IsPriority,
            Notes = kdsOrder.Notes,
            Items = itemDtos,
            TimerStatus = timerStatus
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

    private static string FormatDisplayTime(TimeSpan elapsed)
    {
        return elapsed.TotalMinutes < 60
            ? $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}"
            : $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private static OrderPriority MapToEntity(OrderPriorityDto dto)
    {
        return dto switch
        {
            OrderPriorityDto.Normal => OrderPriority.Normal,
            OrderPriorityDto.Rush => OrderPriority.Rush,
            OrderPriorityDto.VIP => OrderPriority.VIP,
            _ => OrderPriority.Normal
        };
    }

    private static OrderPriorityDto MapToDto(OrderPriority entity)
    {
        return entity switch
        {
            OrderPriority.Normal => OrderPriorityDto.Normal,
            OrderPriority.Rush => OrderPriorityDto.Rush,
            OrderPriority.VIP => OrderPriorityDto.VIP,
            _ => OrderPriorityDto.Normal
        };
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

    private static KdsTimerConfigDto MapConfigToDto(KdsTimerConfig config)
    {
        return new KdsTimerConfigDto
        {
            Id = config.Id,
            GreenThresholdMinutes = config.GreenThresholdMinutes,
            YellowThresholdMinutes = config.YellowThresholdMinutes,
            RedThresholdMinutes = config.RedThresholdMinutes,
            FlashWhenOverdue = config.FlashWhenOverdue,
            FlashIntervalSeconds = config.FlashIntervalSeconds,
            AudioAlertOnOverdue = config.AudioAlertOnOverdue,
            AudioRepeatIntervalSeconds = config.AudioRepeatIntervalSeconds,
            StoreId = config.StoreId
        };
    }

    #endregion
}
