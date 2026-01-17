using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for KDS order management and routing.
/// </summary>
public class KdsOrderService : IKdsOrderService
{
    private readonly IRepository<KdsOrder> _kdsOrderRepository;
    private readonly IRepository<KdsOrderItem> _kdsOrderItemRepository;
    private readonly IRepository<KdsStation> _stationRepository;
    private readonly IRepository<KdsStationCategory> _stationCategoryRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<KdsDisplaySettings> _displaySettingsRepository;
    private readonly IRepository<AllCallMessage> _allCallRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KdsOrderService> _logger;

    public KdsOrderService(
        IRepository<KdsOrder> kdsOrderRepository,
        IRepository<KdsOrderItem> kdsOrderItemRepository,
        IRepository<KdsStation> stationRepository,
        IRepository<KdsStationCategory> stationCategoryRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Product> productRepository,
        IRepository<KdsDisplaySettings> displaySettingsRepository,
        IRepository<AllCallMessage> allCallRepository,
        IUnitOfWork unitOfWork,
        ILogger<KdsOrderService> logger)
    {
        _kdsOrderRepository = kdsOrderRepository ?? throw new ArgumentNullException(nameof(kdsOrderRepository));
        _kdsOrderItemRepository = kdsOrderItemRepository ?? throw new ArgumentNullException(nameof(kdsOrderItemRepository));
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _stationCategoryRepository = stationCategoryRepository ?? throw new ArgumentNullException(nameof(stationCategoryRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _orderItemRepository = orderItemRepository ?? throw new ArgumentNullException(nameof(orderItemRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _displaySettingsRepository = displaySettingsRepository ?? throw new ArgumentNullException(nameof(displaySettingsRepository));
        _allCallRepository = allCallRepository ?? throw new ArgumentNullException(nameof(allCallRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<KdsOrderDto>? OrderRouted;
    public event EventHandler<KdsOrderDto>? OrderUpdated;
    public event EventHandler<KdsOrderDto>? OrderVoided;

    #endregion

    #region Order Routing

    public async Task<RouteOrderResultDto> RouteOrderToStationsAsync(RouteOrderToKdsDto dto)
    {
        var result = new RouteOrderResultDto();

        // Get the order
        var order = await _orderRepository.GetByIdAsync(dto.OrderId);
        if (order == null || !order.IsActive)
        {
            result.Errors.Add($"Order with ID {dto.OrderId} not found.");
            return result;
        }

        // Check if already routed
        var existingKdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.OrderId == dto.OrderId && ko.IsActive);
        if (existingKdsOrders.Any())
        {
            result.Errors.Add("Order is already routed to KDS. Use ReRouteOrderAsync to update.");
            return result;
        }

        // Get order items
        var orderItems = await _orderItemRepository.FindAsync(oi =>
            oi.OrderId == dto.OrderId && oi.IsActive);

        if (!orderItems.Any())
        {
            result.Errors.Add("Order has no items to route.");
            return result;
        }

        // Get active stations for the store
        var stations = await _stationRepository.FindAsync(s =>
            s.StoreId == dto.StoreId && s.IsActive && !s.IsExpo);

        if (!stations.Any())
        {
            result.Errors.Add("No active KDS stations found for this store.");
            return result;
        }

        // Get all station-category mappings
        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.IsActive);

        // Create KDS order
        var kdsOrder = new KdsOrder
        {
            OrderId = dto.OrderId,
            OrderNumber = order.OrderNumber,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            GuestCount = 1, // Default, would come from order if available
            ReceivedAt = DateTime.UtcNow,
            Status = KdsOrderStatus.New,
            Priority = dto.Priority.HasValue ? MapToEntity(dto.Priority.Value) : OrderPriority.Normal,
            IsPriority = dto.Priority.HasValue && dto.Priority.Value != OrderPriorityDto.Normal,
            Notes = order.Notes,
            StoreId = dto.StoreId,
            IsActive = true
        };

        await _kdsOrderRepository.AddAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        // Route items to stations
        int sequenceNumber = 1;
        foreach (var orderItem in orderItems.OrderBy(oi => oi.Id))
        {
            var product = await _productRepository.GetByIdAsync(orderItem.ProductId);
            if (product == null) continue;

            // Find station for this item's category
            var stationForCategory = stationCategories
                .Where(sc => sc.CategoryId == product.CategoryId)
                .Join(stations, sc => sc.StationId, s => s.Id, (sc, s) => s)
                .FirstOrDefault();

            if (stationForCategory == null)
            {
                // Use first available station as fallback
                stationForCategory = stations.First();
                result.Warnings.Add($"No station found for category of product '{product.Name}'. Routed to '{stationForCategory.Name}'.");
            }

            var kdsOrderItem = new KdsOrderItem
            {
                KdsOrderId = kdsOrder.Id,
                OrderItemId = orderItem.Id,
                ProductName = product.Name,
                Quantity = orderItem.Quantity,
                Modifiers = orderItem.Modifiers,
                SpecialInstructions = orderItem.SpecialInstructions,
                StationId = stationForCategory.Id,
                Status = KdsItemStatus.Pending,
                SequenceNumber = sequenceNumber++,
                IsActive = true
            };

            await _kdsOrderItemRepository.AddAsync(kdsOrderItem);

            result.ItemRoutings.Add(new KdsOrderItemRoutingDto
            {
                OrderItemId = orderItem.Id,
                ProductName = product.Name,
                StationId = stationForCategory.Id,
                StationName = stationForCategory.Name
            });
        }

        await _unitOfWork.SaveChangesAsync();

        result.Success = true;
        result.KdsOrderId = kdsOrder.Id;
        result.OrderNumber = kdsOrder.OrderNumber;

        var kdsOrderDto = await GetOrderAsync(kdsOrder.Id);
        OrderRouted?.Invoke(this, kdsOrderDto!);

        _logger.LogInformation("Routed order {OrderId} to KDS as {KdsOrderId} with {ItemCount} items",
            dto.OrderId, kdsOrder.Id, result.ItemRoutings.Count);

        return result;
    }

    public async Task<RouteOrderResultDto> ReRouteOrderAsync(int kdsOrderId)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return new RouteOrderResultDto
            {
                Errors = new List<string> { $"KDS Order with ID {kdsOrderId} not found." }
            };
        }

        // Void existing items
        var existingItems = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId && oi.IsActive);

        foreach (var item in existingItems)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _kdsOrderItemRepository.UpdateAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        // Re-route with current order items
        var routeDto = new RouteOrderToKdsDto
        {
            OrderId = kdsOrder.OrderId,
            StoreId = kdsOrder.StoreId,
            Priority = MapToDto(kdsOrder.Priority)
        };

        // Remove existing KDS order
        kdsOrder.IsActive = false;
        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        return await RouteOrderToStationsAsync(routeDto);
    }

    public async Task<KdsOrderItemRoutingDto> RouteItemToStationAsync(int orderItemId, int storeId)
    {
        var orderItem = await _orderItemRepository.GetByIdAsync(orderItemId);
        if (orderItem == null || !orderItem.IsActive)
        {
            throw new InvalidOperationException($"Order item with ID {orderItemId} not found.");
        }

        var product = await _productRepository.GetByIdAsync(orderItem.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found for order item.");
        }

        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.CategoryId == product.CategoryId && sc.IsActive);

        var stations = await _stationRepository.FindAsync(s =>
            s.StoreId == storeId && s.IsActive && !s.IsExpo);

        var stationForCategory = stationCategories
            .Join(stations, sc => sc.StationId, s => s.Id, (sc, s) => s)
            .FirstOrDefault() ?? stations.FirstOrDefault();

        if (stationForCategory == null)
        {
            throw new InvalidOperationException("No active KDS station found.");
        }

        return new KdsOrderItemRoutingDto
        {
            OrderItemId = orderItemId,
            ProductName = product.Name,
            StationId = stationForCategory.Id,
            StationName = stationForCategory.Name
        };
    }

    #endregion

    #region Order Retrieval

    public async Task<KdsOrderDto?> GetOrderAsync(int id)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(id);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            return null;
        }

        return await MapToOrderDtoAsync(kdsOrder);
    }

    public async Task<KdsOrderDto?> GetOrderByOrderIdAsync(int orderId)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            ko.OrderId == orderId && ko.IsActive);

        var kdsOrder = kdsOrders.FirstOrDefault();
        if (kdsOrder == null)
        {
            return null;
        }

        return await MapToOrderDtoAsync(kdsOrder);
    }

    public async Task<List<KdsOrderDto>> GetStationOrdersAsync(int stationId, bool includeCompleted = false)
    {
        var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId && oi.IsActive);

        var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) && ko.IsActive);

        if (!includeCompleted)
        {
            kdsOrders = kdsOrders.Where(ko =>
                ko.Status != KdsOrderStatus.Served &&
                ko.Status != KdsOrderStatus.Voided);
        }

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.IsPriority)
                                          .ThenBy(o => o.ReceivedAt))
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    public async Task<List<KdsOrderListDto>> GetOrdersAsync(KdsOrderQueryDto query)
    {
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko => ko.IsActive);

        // Apply filters
        if (query.StoreId.HasValue)
        {
            kdsOrders = kdsOrders.Where(ko => ko.StoreId == query.StoreId.Value);
        }

        if (query.Status.HasValue)
        {
            var status = MapToEntity(query.Status.Value);
            kdsOrders = kdsOrders.Where(ko => ko.Status == status);
        }

        if (query.Statuses != null && query.Statuses.Any())
        {
            var statuses = query.Statuses.Select(s => MapToEntity(s)).ToList();
            kdsOrders = kdsOrders.Where(ko => statuses.Contains(ko.Status));
        }

        if (query.Priority.HasValue)
        {
            var priority = MapToEntity(query.Priority.Value);
            kdsOrders = kdsOrders.Where(ko => ko.Priority == priority);
        }

        if (query.FromDate.HasValue)
        {
            kdsOrders = kdsOrders.Where(ko => ko.ReceivedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            kdsOrders = kdsOrders.Where(ko => ko.ReceivedAt <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            kdsOrders = kdsOrders.Where(ko =>
                ko.OrderNumber.ToLower().Contains(term) ||
                (ko.TableNumber != null && ko.TableNumber.ToLower().Contains(term)));
        }

        // If station filter, only include orders with items at that station
        if (query.StationId.HasValue)
        {
            var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
                oi.StationId == query.StationId.Value && oi.IsActive);

            var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();
            kdsOrders = kdsOrders.Where(ko => kdsOrderIds.Contains(ko.Id));
        }

        var orderList = kdsOrders
            .OrderByDescending(ko => ko.IsPriority)
            .ThenBy(ko => ko.ReceivedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new List<KdsOrderListDto>();
        foreach (var kdsOrder in orderList)
        {
            var items = await _kdsOrderItemRepository.FindAsync(oi =>
                oi.KdsOrderId == kdsOrder.Id && oi.IsActive);

            result.Add(new KdsOrderListDto
            {
                Id = kdsOrder.Id,
                OrderNumber = kdsOrder.OrderNumber,
                TableNumber = kdsOrder.TableNumber,
                GuestCount = kdsOrder.GuestCount,
                ReceivedAt = kdsOrder.ReceivedAt,
                Status = MapToDto(kdsOrder.Status),
                Priority = MapToDto(kdsOrder.Priority),
                IsPriority = kdsOrder.IsPriority,
                ItemCount = items.Count(),
                CompletedItemCount = items.Count(i => i.Status == KdsItemStatus.Done),
                TimerColor = CalculateTimerColor(DateTime.UtcNow - kdsOrder.ReceivedAt),
                IsOverdue = (DateTime.UtcNow - kdsOrder.ReceivedAt).TotalMinutes > 15,
                ShouldFlash = (DateTime.UtcNow - kdsOrder.ReceivedAt).TotalMinutes > 15
            });
        }

        return result;
    }

    public async Task<List<KdsOrderDto>> GetActiveOrdersAsync(int stationId)
    {
        var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) &&
            ko.Status != KdsOrderStatus.Served &&
            ko.Status != KdsOrderStatus.Voided &&
            ko.IsActive);

        var result = new List<KdsOrderDto>();
        foreach (var kdsOrder in kdsOrders.OrderByDescending(o => o.IsPriority)
                                          .ThenBy(o => o.ReceivedAt))
        {
            result.Add(await MapToOrderDtoAsync(kdsOrder));
        }

        return result;
    }

    public async Task<List<KdsOrderDto>> GetReadyOrdersAsync(int stationId, int recallWindowMinutes = 10)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-recallWindowMinutes);

        var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status == KdsItemStatus.Done &&
            oi.CompletedAt >= cutoffTime &&
            oi.IsActive);

        var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();

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

    public async Task<List<KdsOrderDto>> GetOrderQueueAsync(int stationId)
    {
        return await GetActiveOrdersAsync(stationId);
    }

    #endregion

    #region Order Items

    public async Task<List<KdsOrderItemDto>> GetStationItemsAsync(int kdsOrderId, int stationId)
    {
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId &&
            oi.StationId == stationId &&
            oi.IsActive);

        return items.OrderBy(i => i.SequenceNumber)
                    .Select(i => MapToItemDto(i))
                    .ToList();
    }

    public async Task<List<KdsOrderItemDto>> GetOrderItemsAsync(int kdsOrderId)
    {
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId && oi.IsActive);

        var result = new List<KdsOrderItemDto>();
        foreach (var item in items.OrderBy(i => i.SequenceNumber))
        {
            var station = await _stationRepository.GetByIdAsync(item.StationId);
            result.Add(MapToItemDto(item, station?.Name));
        }

        return result;
    }

    #endregion

    #region Display State

    public async Task<KdsStationDisplayDto> GetStationDisplayAsync(int stationId)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        var orders = await GetOrderViewModelsAsync(stationId);

        var activeMessages = await _allCallRepository.FindAsync(m =>
            m.StoreId == station.StoreId &&
            !m.IsExpired &&
            (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow) &&
            m.IsActive);

        var settings = station.DisplaySettingsId.HasValue
            ? await _displaySettingsRepository.GetByIdAsync(station.DisplaySettingsId.Value)
            : null;

        return new KdsStationDisplayDto
        {
            StationId = stationId,
            StationName = station.Name,
            Status = MapStationStatusToDto(station.Status),
            Settings = settings != null ? MapSettingsToDto(settings) : GetDefaultSettings(),
            Orders = orders,
            ActiveMessages = activeMessages.Select(MapToAllCallDto).ToList(),
            LastRefreshTime = DateTime.UtcNow
        };
    }

    public async Task<List<KdsOrderViewModel>> GetOrderViewModelsAsync(int stationId)
    {
        var activeOrders = await GetActiveOrdersAsync(stationId);

        return activeOrders.Select(o => new KdsOrderViewModel
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            TableNumber = o.TableNumber,
            Items = o.Items.Select(i => new KdsOrderItemViewModel
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Modifiers = i.Modifiers,
                SpecialInstructions = i.SpecialInstructions,
                Status = i.Status,
                CourseNumber = i.CourseNumber
            }).ToList(),
            ElapsedTime = o.ElapsedTime,
            TimerColor = CalculateTimerColor(o.ElapsedTime),
            IsPriority = o.IsPriority,
            Priority = o.Priority,
            IsFlashing = o.ElapsedTime.TotalMinutes > 15,
            Status = o.Status
        }).ToList();
    }

    #endregion

    #region Order Management

    public async Task<KdsOrderDto> UpdateOrderPriorityAsync(int kdsOrderId, OrderPriorityDto priority)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {kdsOrderId} not found.");
        }

        kdsOrder.Priority = MapToEntity(priority);
        kdsOrder.IsPriority = priority != OrderPriorityDto.Normal;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        var result = await GetOrderAsync(kdsOrderId);
        OrderUpdated?.Invoke(this, result!);

        return result!;
    }

    public async Task<KdsOrderDto> VoidOrderAsync(int kdsOrderId, int? userId = null)
    {
        var kdsOrder = await _kdsOrderRepository.GetByIdAsync(kdsOrderId);
        if (kdsOrder == null || !kdsOrder.IsActive)
        {
            throw new InvalidOperationException($"KDS Order with ID {kdsOrderId} not found.");
        }

        kdsOrder.Status = KdsOrderStatus.Voided;
        kdsOrder.UpdatedAt = DateTime.UtcNow;

        // Void all items
        var items = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.KdsOrderId == kdsOrderId && oi.IsActive);

        foreach (var item in items)
        {
            item.Status = KdsItemStatus.Voided;
            item.UpdatedAt = DateTime.UtcNow;
            await _kdsOrderItemRepository.UpdateAsync(item);
        }

        await _kdsOrderRepository.UpdateAsync(kdsOrder);
        await _unitOfWork.SaveChangesAsync();

        var result = await GetOrderAsync(kdsOrderId);
        OrderVoided?.Invoke(this, result!);

        _logger.LogInformation("Voided KDS order {KdsOrderId} by user {UserId}", kdsOrderId, userId);

        return result!;
    }

    public async Task<KdsOrderItemDto> VoidOrderItemAsync(int kdsOrderItemId, int? userId = null)
    {
        var item = await _kdsOrderItemRepository.GetByIdAsync(kdsOrderItemId);
        if (item == null || !item.IsActive)
        {
            throw new InvalidOperationException($"KDS Order Item with ID {kdsOrderItemId} not found.");
        }

        item.Status = KdsItemStatus.Voided;
        item.UpdatedAt = DateTime.UtcNow;

        await _kdsOrderItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Voided KDS order item {KdsOrderItemId} by user {UserId}", kdsOrderItemId, userId);

        return MapToItemDto(item);
    }

    #endregion

    #region Statistics

    public async Task<Dictionary<KdsOrderStatusDto, int>> GetOrderCountByStatusAsync(int stationId)
    {
        var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId && oi.IsActive);

        var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();

        var kdsOrders = await _kdsOrderRepository.FindAsync(ko =>
            kdsOrderIds.Contains(ko.Id) && ko.IsActive);

        return kdsOrders
            .GroupBy(ko => MapToDto(ko.Status))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<TimeSpan> GetAverageWaitTimeAsync(int stationId, DateTime fromDate, DateTime toDate)
    {
        var orderItemsAtStation = await _kdsOrderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.CreatedAt >= fromDate &&
            oi.CreatedAt <= toDate &&
            oi.Status == KdsItemStatus.Done &&
            oi.StartedAt.HasValue &&
            oi.IsActive);

        if (!orderItemsAtStation.Any())
        {
            return TimeSpan.Zero;
        }

        var kdsOrderIds = orderItemsAtStation.Select(oi => oi.KdsOrderId).Distinct().ToList();
        var kdsOrders = await _kdsOrderRepository.FindAsync(ko => kdsOrderIds.Contains(ko.Id));

        var waitTimes = orderItemsAtStation
            .Join(kdsOrders, oi => oi.KdsOrderId, ko => ko.Id, (oi, ko) => oi.StartedAt!.Value - ko.ReceivedAt)
            .ToList();

        return waitTimes.Any()
            ? TimeSpan.FromTicks((long)waitTimes.Average(t => t.Ticks))
            : TimeSpan.Zero;
    }

    #endregion

    #region Private Helpers

    private async Task<KdsOrderDto> MapToOrderDtoAsync(KdsOrder kdsOrder)
    {
        var items = await GetOrderItemsAsync(kdsOrder.Id);

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
            Priority = MapToDto(kdsOrder.Priority),
            IsPriority = kdsOrder.IsPriority,
            Notes = kdsOrder.Notes,
            Items = items,
            TimerStatus = new KdsTimerStatusDto
            {
                Elapsed = DateTime.UtcNow - kdsOrder.ReceivedAt,
                Color = CalculateTimerColor(DateTime.UtcNow - kdsOrder.ReceivedAt),
                IsOverdue = (DateTime.UtcNow - kdsOrder.ReceivedAt).TotalMinutes > 15,
                ShouldFlash = (DateTime.UtcNow - kdsOrder.ReceivedAt).TotalMinutes > 15,
                ShouldPlayAudio = (DateTime.UtcNow - kdsOrder.ReceivedAt).TotalMinutes > 15,
                DisplayTime = FormatDisplayTime(DateTime.UtcNow - kdsOrder.ReceivedAt)
            }
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

    private static string FormatDisplayTime(TimeSpan elapsed)
    {
        return elapsed.TotalMinutes < 60
            ? $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}"
            : $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private static KdsOrderStatus MapToEntity(KdsOrderStatusDto dto)
    {
        return dto switch
        {
            KdsOrderStatusDto.New => KdsOrderStatus.New,
            KdsOrderStatusDto.InProgress => KdsOrderStatus.InProgress,
            KdsOrderStatusDto.Ready => KdsOrderStatus.Ready,
            KdsOrderStatusDto.Served => KdsOrderStatus.Served,
            KdsOrderStatusDto.Recalled => KdsOrderStatus.Recalled,
            KdsOrderStatusDto.Voided => KdsOrderStatus.Voided,
            _ => KdsOrderStatus.New
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

    private static KdsStationStatusDto MapStationStatusToDto(KdsStationStatus entity)
    {
        return entity switch
        {
            KdsStationStatus.Offline => KdsStationStatusDto.Offline,
            KdsStationStatus.Online => KdsStationStatusDto.Online,
            KdsStationStatus.Paused => KdsStationStatusDto.Paused,
            _ => KdsStationStatusDto.Offline
        };
    }

    private static KdsDisplaySettingsDto MapSettingsToDto(KdsDisplaySettings settings)
    {
        return new KdsDisplaySettingsDto
        {
            Id = settings.Id,
            ColumnsCount = settings.ColumnsCount,
            FontSize = settings.FontSize,
            WarningThresholdMinutes = settings.WarningThresholdMinutes,
            AlertThresholdMinutes = settings.AlertThresholdMinutes,
            GreenThresholdMinutes = settings.GreenThresholdMinutes,
            ShowModifiers = settings.ShowModifiers,
            ShowSpecialInstructions = settings.ShowSpecialInstructions,
            AudioAlerts = settings.AudioAlerts,
            FlashWhenOverdue = settings.FlashWhenOverdue,
            FlashIntervalSeconds = settings.FlashIntervalSeconds,
            AudioRepeatIntervalSeconds = settings.AudioRepeatIntervalSeconds,
            RecallWindowMinutes = settings.RecallWindowMinutes,
            ThemeName = settings.ThemeName,
            BackgroundColor = settings.BackgroundColor
        };
    }

    private static KdsDisplaySettingsDto GetDefaultSettings()
    {
        return new KdsDisplaySettingsDto
        {
            Id = 0,
            ColumnsCount = 4,
            FontSize = 16,
            WarningThresholdMinutes = 10,
            AlertThresholdMinutes = 15,
            GreenThresholdMinutes = 5,
            ShowModifiers = true,
            ShowSpecialInstructions = true,
            AudioAlerts = true,
            FlashWhenOverdue = true,
            FlashIntervalSeconds = 2,
            AudioRepeatIntervalSeconds = 30,
            RecallWindowMinutes = 10
        };
    }

    private static AllCallMessageDto MapToAllCallDto(AllCallMessage message)
    {
        return new AllCallMessageDto
        {
            Id = message.Id,
            Message = message.Message,
            SentByUserId = message.SentByUserId,
            SentAt = message.SentAt,
            Priority = message.Priority == AllCallPriority.Urgent
                ? AllCallPriorityDto.Urgent
                : AllCallPriorityDto.Normal,
            ExpiresAt = message.ExpiresAt,
            IsExpired = message.IsExpired,
            StoreId = message.StoreId
        };
    }

    #endregion
}
