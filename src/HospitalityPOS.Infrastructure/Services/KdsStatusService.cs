using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for KDS order status management.
/// </summary>
public class KdsStatusService : IKdsStatusService
{
    private readonly IRepository<KdsOrder> _kdsOrderRepository;
    private readonly IRepository<KdsOrderItem> _kdsOrderItemRepository;
    private readonly IRepository<KdsOrderStatusLog> _statusLogRepository;
    private readonly IRepository<KdsStation> _stationRepository;
    private readonly IRepository<KdsDisplaySettings> _displaySettingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KdsStatusService> _logger;

    public KdsStatusService(
        IRepository<KdsOrder> kdsOrderRepository,
        IRepository<KdsOrderItem> kdsOrderItemRepository,
        IRepository<KdsOrderStatusLog> statusLogRepository,
        IRepository<KdsStation> stationRepository,
        IRepository<KdsDisplaySettings> displaySettingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<KdsStatusService> logger)
    {
        _kdsOrderRepository = kdsOrderRepository ?? throw new ArgumentNullException(nameof(kdsOrderRepository));
        _kdsOrderItemRepository = kdsOrderItemRepository ?? throw new ArgumentNullException(nameof(kdsOrderItemRepository));
        _statusLogRepository = statusLogRepository ?? throw new ArgumentNullException(nameof(statusLogRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _displaySettingsRepository = displaySettingsRepository ?? throw new ArgumentNullException(nameof(displaySettingsRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<KdsOrderStatusChangeEventArgs>? OrderStatusChanged;
    public event EventHandler<BumpOrderEventArgs>? OrderBumped;
    public event EventHandler<KdsOrderDto>? OrderRecalled;
    public event EventHandler<KdsOrderDto>? OrderReady;
    public event EventHandler<KdsOrderDto>? OrderServed;

    #endregion

    #region Status Transitions

    public async Task<KdsOrderDto> StartPreparationAsync(StartPreparationDto dto)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(dto.KdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {dto.KdsOrderId} not found.");
        }

        if (kdsOrder.Status != KdsOrderStatus.New && kdsOrder.Status != KdsOrderStatus.Recalled)
        {
            throw new InvalidOperationException($"Cannot start preparation for order with status {kdsOrder.Status}.");
        }

        var previousStatus = kdsOrder.Status;
        kdsOrder.Status = KdsOrderStatus.InProgress;
        kdsOrder.StartedAt ??= DateTime.UtcNow;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);

        // Mark items at this station as preparing
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == dto.KdsOrderId &&
            oi.StationId == dto.StationId &&
            oi.Status == KdsItemStatus.Pending &&
            oi.IsActive);

        foreach (var item in items)
        {
            item.Status = KdsItemStatus.Preparing;
            item.StartedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _kdsOrderItemRepository.UpdateAsync(item);
        }

        // Log status change
        await LogStatusChangeAsync(dto.KdsOrderId, dto.StationId, previousStatus, KdsOrderStatus.InProgress, dto.UserId);

        await _unitOfWork.SaveChangesAsync();

        var orderDto = await MapToOrderDtoAsync(kdsOrder);

        OrderStatusChanged?.Invoke(this, new KdsOrderStatusChangeEventArgs
        {
            KdsOrderId = dto.KdsOrderId,
            StationId = dto.StationId,
            PreviousStatus = MapToDto(previousStatus),
            NewStatus = KdsOrderStatusDto.InProgress,
            UserId = dto.UserId,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Started preparation for KDS order {KdsOrderId} at station {StationId}",
            dto.KdsOrderId, dto.StationId);

        return orderDto;
    }

    public async Task<KdsOrderItemDto> MarkItemDoneAsync(MarkItemDoneDto dto)
    {
        var item = await _kdsOrderItemRepository.GetByIdAsync(dto.KdsOrderItemId);
        if (item == null || !item.IsActive)
        {
            throw new InvalidOperationException($"KDS Order Item with ID {dto.KdsOrderItemId} not found.");
        }

        if (item.Status == KdsItemStatus.Done)
        {
            throw new InvalidOperationException("Item is already marked as done.");
        }

        item.Status = KdsItemStatus.Done;
        item.CompletedAt = DateTime.UtcNow;
        item.CompletedByUserId = dto.UserId;
        item.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Marked KDS order item {KdsOrderItemId} as done by user {UserId}",
            dto.KdsOrderItemId, dto.UserId);

        return MapToItemDto(item);
    }

    public async Task<BumpOrderResultDto> BumpOrderAsync(BumpOrderDto dto)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(dto.KdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return new BumpOrderResultDto
            {
                Success = false,
                KdsOrderId = dto.KdsOrderId,
                Messages = new List<string> { $"KDS Order with ID {dto.KdsOrderId} not found." }
            };
        }

        // Mark all items at this station as done
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == dto.KdsOrderId &&
            oi.StationId == dto.StationId &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        foreach (var item in stationItems)
        {
            if (item.Status != KdsItemStatus.Done)
            {
                item.Status = KdsItemStatus.Done;
                item.CompletedAt = DateTime.UtcNow;
                item.CompletedByUserId = dto.UserId;
                item.UpdatedAt = DateTime.UtcNow;
                await _kdsOrderItemRepository.UpdateAsync(item);
            }
        }

        // Check if all items across all stations are done
        var allItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == dto.KdsOrderId &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var allDone = allItems.All(i => i.Status == KdsItemStatus.Done);

        var previousStatus = kdsOrder.Status;
        kdsOrder.Status = allDone ? KdsOrderStatus.Ready : KdsOrderStatus.InProgress;
        kdsOrder.CompletedAt = allDone ? DateTime.UtcNow : null;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await LogStatusChangeAsync(dto.KdsOrderId, dto.StationId, previousStatus, kdsOrder.Status, dto.UserId);
        await _unitOfWork.SaveChangesAsync();

        var result = new BumpOrderResultDto
        {
            Success = true,
            KdsOrderId = dto.KdsOrderId,
            NewStatus = MapToDto(kdsOrder.Status),
            AllItemsDone = allDone,
            OrderComplete = allDone,
            Messages = new List<string>()
        };

        if (!allDone)
        {
            var pendingStations = allItems
                .Where(i => i.Status != KdsItemStatus.Done)
                .Select(i => i.StationId)
                .Distinct()
                .ToList();
            result.Messages.Add($"Order still has pending items at {pendingStations.Count} station(s).");
        }

        OrderBumped?.Invoke(this, new BumpOrderEventArgs
        {
            KdsOrderId = dto.KdsOrderId,
            StationId = dto.StationId,
            AllItemsDone = allDone,
            OrderComplete = allDone,
            PlayAudio = dto.PlayAudio
        });

        if (allDone)
        {
            var orderDto = await MapToOrderDtoAsync(kdsOrder);
            OrderReady?.Invoke(this, orderDto);
        }

        _logger.LogInformation("Bumped KDS order {KdsOrderId} at station {StationId}. AllDone: {AllDone}",
            dto.KdsOrderId, dto.StationId, allDone);

        return result;
    }

    public async Task<RecallOrderResultDto> RecallOrderAsync(RecallOrderDto dto)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(dto.KdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return new RecallOrderResultDto
            {
                Success = false,
                KdsOrderId = dto.KdsOrderId,
                Errors = new List<string> { $"KDS Order with ID {dto.KdsOrderId} not found." }
            };
        }

        // Check recall window
        var canRecall = await CanRecallOrderAsync(dto.KdsOrderId, dto.StationId);
        if (!canRecall)
        {
            return new RecallOrderResultDto
            {
                Success = false,
                KdsOrderId = dto.KdsOrderId,
                Errors = new List<string> { "Order recall window has expired." }
            };
        }

        var previousStatus = kdsOrder.Status;
        kdsOrder.Status = KdsOrderStatus.Recalled;
        kdsOrder.CompletedAt = null;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        // Reset items at this station
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == dto.KdsOrderId &&
            oi.StationId == dto.StationId &&
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
        await LogStatusChangeAsync(dto.KdsOrderId, dto.StationId, previousStatus, KdsOrderStatus.Recalled, dto.UserId, dto.Reason);
        await _unitOfWork.SaveChangesAsync();

        var orderDto = await MapToOrderDtoAsync(kdsOrder);

        OrderRecalled?.Invoke(this, orderDto);

        _logger.LogInformation("Recalled KDS order {KdsOrderId} at station {StationId}. Reason: {Reason}",
            dto.KdsOrderId, dto.StationId, dto.Reason);

        return new RecallOrderResultDto
        {
            Success = true,
            KdsOrderId = dto.KdsOrderId,
            NewStatus = KdsOrderStatusDto.Recalled,
            Message = "Order recalled successfully."
        };
    }

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
        await LogStatusChangeAsync(dto.KdsOrderId, null, previousStatus, KdsOrderStatus.Served, dto.UserId);
        await _unitOfWork.SaveChangesAsync();

        var orderDto = await MapToOrderDtoAsync(kdsOrder);

        OrderServed?.Invoke(this, orderDto);

        _logger.LogInformation("Marked KDS order {KdsOrderId} as served by user {UserId}",
            dto.KdsOrderId, dto.UserId);

        return orderDto;
    }

    #endregion

    #region Item Status

    public async Task<KdsOrderItemDto> StartItemPreparationAsync(int kdsOrderItemId, int? userId = null)
    {
        var item = await _kdsOrderItemRepository.GetByIdAsync(kdsOrderItemId);
        if (item == null || !item.IsActive)
        {
            throw new InvalidOperationException($"KDS Order Item with ID {kdsOrderItemId} not found.");
        }

        if (item.Status != KdsItemStatus.Pending)
        {
            throw new InvalidOperationException("Can only start preparation for pending items.");
        }

        item.Status = KdsItemStatus.Preparing;
        item.StartedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Started preparation for KDS order item {KdsOrderItemId}", kdsOrderItemId);

        return MapToItemDto(item);
    }

    public async Task<KdsOrderItemDto> RevertItemToPendingAsync(int kdsOrderItemId, int? userId = null)
    {
        var item = await _kdsOrderItemRepository.GetByIdAsync(kdsOrderItemId);
        if (item == null || !item.IsActive)
        {
            throw new InvalidOperationException($"KDS Order Item with ID {kdsOrderItemId} not found.");
        }

        item.Status = KdsItemStatus.Pending;
        item.StartedAt = null;
        item.CompletedAt = null;
        item.CompletedByUserId = null;
        item.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Reverted KDS order item {KdsOrderItemId} to pending", kdsOrderItemId);

        return MapToItemDto(item);
    }

    public async Task<List<KdsOrderItemDto>> GetItemsByStatusAsync(int stationId, KdsItemStatusDto status)
    {
        var entityStatus = MapToEntity(status);
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status == entityStatus &&
            oi.IsActive);

        return items.OrderBy(i => i.SequenceNumber)
                    .Select(MapToItemDto)
                    .ToList();
    }

    #endregion

    #region Ready Orders

    public async Task<List<KdsOrderDto>> GetReadyOrdersAsync(int stationId)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status == KdsItemStatus.Done &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status == KdsOrderStatus.Ready &&
            ko.IsActive);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.CompletedAt))
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    public async Task<List<KdsOrderDto>> GetRecallableOrdersAsync(int stationId, int recallWindowMinutes = 10)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-recallWindowMinutes);

        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status == KdsItemStatus.Done &&
            oi.CompletedAt >= cutoffTime &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            (ko.Status == KdsOrderStatus.Ready || ko.Status == KdsOrderStatus.InProgress) &&
            ko.IsActive);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.CompletedAt ?? o.UpdatedAt))
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    public async Task<bool> CanRecallOrderAsync(int kdsOrderId, int stationId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return false;
        }

        if (kdsOrder.Status != KdsOrderStatus.Ready && kdsOrder.Status != KdsOrderStatus.InProgress)
        {
            return false;
        }

        // Get recall window from station settings
        var station = await _stationRepository.GetByIdAsync(stationId);
        var recallWindowMinutes = 10; // Default

        if (station?.DisplaySettingsId != null)
        {
            var settings = await _displaySettingsRepository.GetByIdAsync(station.DisplaySettingsId.Value);
            if (settings != null)
            {
                recallWindowMinutes = settings.RecallWindowMinutes;
            }
        }

        // Check if completed within recall window
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId &&
            oi.StationId == stationId &&
            oi.CompletedAt.HasValue &&
            oi.IsActive);

        if (!stationItems.Any())
        {
            return true; // No completed items, can start fresh
        }

        var latestCompletion = stationItems.Max(i => i.CompletedAt!.Value);
        return DateTime.UtcNow - latestCompletion <= TimeSpan.FromMinutes(recallWindowMinutes);
    }

    #endregion

    #region Status Logs

    public async Task<List<KdsOrderStatusLogDto>> GetOrderStatusLogsAsync(int kdsOrderId)
    {
        var logs = await _statusLogRepository.FindAsync(l =>
            l.KdsOrderId == kdsOrderId && l.IsActive);

        var result = new List<KdsOrderStatusLogDto>();
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            var station = log.StationId.HasValue
                ? await _stationRepository.GetByIdAsync(log.StationId.Value)
                : null;

            result.Add(new KdsOrderStatusLogDto
            {
                Id = log.Id,
                KdsOrderId = log.KdsOrderId,
                StationId = log.StationId,
                StationName = station?.Name,
                PreviousStatus = MapToDto(log.PreviousStatus),
                NewStatus = MapToDto(log.NewStatus),
                UserId = log.UserId,
                Timestamp = log.Timestamp,
                Notes = log.Notes
            });
        }

        return result;
    }

    public async Task<List<KdsOrderStatusLogDto>> GetStationStatusLogsAsync(int stationId, DateTime fromDate, DateTime toDate)
    {
        var logs = await _statusLogRepository.FindAsync(l =>
            l.StationId == stationId &&
            l.Timestamp >= fromDate &&
            l.Timestamp <= toDate &&
            l.IsActive);

        var station = await _stationRepository.GetByIdAsync(stationId);

        return logs.OrderByDescending(l => l.Timestamp)
                   .Select(log => new KdsOrderStatusLogDto
                   {
                       Id = log.Id,
                       KdsOrderId = log.KdsOrderId,
                       StationId = log.StationId,
                       StationName = station?.Name,
                       PreviousStatus = MapToDto(log.PreviousStatus),
                       NewStatus = MapToDto(log.NewStatus),
                       UserId = log.UserId,
                       Timestamp = log.Timestamp,
                       Notes = log.Notes
                   }).ToList();
    }

    #endregion

    #region Bulk Operations

    public async Task<int> BumpAllReadyOrdersAsync(int stationId, int? userId = null)
    {
        var stationItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status == KdsItemStatus.Done &&
            oi.IsActive);

        var kdsOrderIds = stationItems.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status == KdsOrderStatus.InProgress &&
            ko.IsActive);

        var bumpedCount = 0;
        foreach (var kdsOrder in kdsOrders)
        {
            await BumpOrderAsync(new BumpOrderDto
            {
                KdsOrderId = kdsOrder.Id,
                StationId = stationId,
                UserId = userId,
                PlayAudio = false
            });
            bumpedCount++;
        }

        return bumpedCount;
    }

    public async Task<int> MarkAllStationItemsDoneAsync(int kdsOrderId, int stationId, int? userId = null)
    {
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId &&
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var markedCount = 0;
        foreach (var item in items)
        {
            item.Status = KdsItemStatus.Done;
            item.CompletedAt = DateTime.UtcNow;
            item.CompletedByUserId = userId;
            item.UpdatedAt = DateTime.UtcNow;
            await _kdsOrderItemRepository.UpdateAsync(item);
            markedCount++;
        }

        await _unitOfWork.SaveChangesAsync();

        return markedCount;
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
            Status = MapToDto(kdsOrder.Status),
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

    private static KdsItemStatus MapToEntity(KdsItemStatusDto dto)
    {
        return dto switch
        {
            KdsItemStatusDto.Pending => KdsItemStatus.Pending,
            KdsItemStatusDto.Preparing => KdsItemStatus.Preparing,
            KdsItemStatusDto.Done => KdsItemStatus.Done,
            KdsItemStatusDto.Voided => KdsItemStatus.Voided,
            _ => KdsItemStatus.Pending
        };
    }

    private static KdsOrderStatusDto MapToDto(KdsOrderStatus entity)
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

    #endregion
}
