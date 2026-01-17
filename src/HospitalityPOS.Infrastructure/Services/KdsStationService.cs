using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for KDS station management.
/// </summary>
public class KdsStationService : IKdsStationService
{
    private readonly IRepository<KdsStation> _stationRepository;
    private readonly IRepository<KdsStationCategory> _stationCategoryRepository;
    private readonly IRepository<KdsDisplaySettings> _displaySettingsRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<KdsOrderItem> _orderItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KdsStationService> _logger;

    public KdsStationService(
        IRepository<KdsStation> stationRepository,
        IRepository<KdsStationCategory> stationCategoryRepository,
        IRepository<KdsDisplaySettings> displaySettingsRepository,
        IRepository<Category> categoryRepository,
        IRepository<Store> storeRepository,
        IRepository<KdsOrderItem> orderItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<KdsStationService> logger)
    {
        _stationRepository = stationRepository ?? throw new ArgumentNullException(nameof(stationRepository));
        _stationCategoryRepository = stationCategoryRepository ?? throw new ArgumentNullException(nameof(stationCategoryRepository));
        _displaySettingsRepository = displaySettingsRepository ?? throw new ArgumentNullException(nameof(displaySettingsRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _orderItemRepository = orderItemRepository ?? throw new ArgumentNullException(nameof(orderItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<KdsStationDto>? StationOnline;
    public event EventHandler<KdsStationDto>? StationOffline;
    public event EventHandler<KdsStationDto>? StationCategoriesUpdated;

    #endregion

    #region Station CRUD Operations

    public async Task<KdsStationDto> CreateStationAsync(CreateKdsStationDto dto)
    {
        // Validate store exists
        var store = await _storeRepository.GetByIdAsync(dto.StoreId);
        if (store == null || !store.IsActive)
        {
            throw new InvalidOperationException($"Store with ID {dto.StoreId} not found or is inactive.");
        }

        // Validate device identifier is unique
        if (!await IsDeviceIdentifierUniqueAsync(dto.DeviceIdentifier))
        {
            throw new InvalidOperationException($"Device identifier '{dto.DeviceIdentifier}' is already in use.");
        }

        // Validate station name is unique within store
        if (!await IsStationNameUniqueAsync(dto.Name, dto.StoreId))
        {
            throw new InvalidOperationException($"Station name '{dto.Name}' already exists in this store.");
        }

        // Create display settings if provided
        int? displaySettingsId = null;
        if (dto.DisplaySettings != null)
        {
            var settings = await CreateDisplaySettingsAsync(dto.DisplaySettings);
            displaySettingsId = settings.Id;
        }

        var station = new KdsStation
        {
            Name = dto.Name,
            DeviceIdentifier = dto.DeviceIdentifier,
            StationType = MapToEntity(dto.StationType),
            Status = KdsStationStatus.Offline,
            DisplayOrder = dto.DisplayOrder,
            IsExpo = dto.IsExpo || dto.StationType == KdsStationTypeDto.Expo,
            StoreId = dto.StoreId,
            Description = dto.Description,
            DisplaySettingsId = displaySettingsId,
            IsActive = true
        };

        await _stationRepository.AddAsync(station);
        await _unitOfWork.SaveChangesAsync();

        // Assign categories if provided
        if (dto.CategoryIds != null && dto.CategoryIds.Any())
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                await AssignCategoryAsync(station.Id, categoryId);
            }
        }

        _logger.LogInformation("Created KDS station {StationId} '{StationName}' for store {StoreId}",
            station.Id, station.Name, dto.StoreId);

        return await GetStationAsync(station.Id) ?? throw new InvalidOperationException("Failed to retrieve created station.");
    }

    public async Task<KdsStationDto?> GetStationAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            return null;
        }

        var categories = await GetStationCategoriesAsync(id);
        var displaySettings = station.DisplaySettingsId.HasValue
            ? await GetDisplaySettingsAsync(id)
            : null;

        return MapToDto(station, categories, displaySettings);
    }

    public async Task<KdsStationDto?> GetStationByDeviceAsync(string deviceIdentifier)
    {
        var stations = await _stationRepository.FindAsync(s =>
            s.DeviceIdentifier == deviceIdentifier && s.IsActive);

        var station = stations.FirstOrDefault();
        if (station == null)
        {
            return null;
        }

        return await GetStationAsync(station.Id);
    }

    public async Task<List<KdsStationListDto>> GetStationsAsync(KdsStationQueryDto query)
    {
        var stations = await _stationRepository.FindAsync(s => s.IsActive);

        // Apply filters
        if (query.StoreId.HasValue)
        {
            stations = stations.Where(s => s.StoreId == query.StoreId.Value);
        }

        if (query.StationType.HasValue)
        {
            var stationType = MapToEntity(query.StationType.Value);
            stations = stations.Where(s => s.StationType == stationType);
        }

        if (query.Status.HasValue)
        {
            var status = MapToEntity(query.Status.Value);
            stations = stations.Where(s => s.Status == status);
        }

        if (query.IsExpo.HasValue)
        {
            stations = stations.Where(s => s.IsExpo == query.IsExpo.Value);
        }

        if (query.IsActive.HasValue)
        {
            stations = stations.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            stations = stations.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.DeviceIdentifier.ToLower().Contains(term));
        }

        var stationList = stations
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new List<KdsStationListDto>();
        foreach (var station in stationList)
        {
            var categories = await _stationCategoryRepository.FindAsync(sc =>
                sc.StationId == station.Id && sc.IsActive);

            var activeOrders = await _orderItemRepository.FindAsync(oi =>
                oi.StationId == station.Id &&
                oi.Status != KdsItemStatus.Done &&
                oi.Status != KdsItemStatus.Voided &&
                oi.IsActive);

            result.Add(new KdsStationListDto
            {
                Id = station.Id,
                Name = station.Name,
                DeviceIdentifier = station.DeviceIdentifier,
                StationType = MapToDto(station.StationType),
                Status = MapToDto(station.Status),
                DisplayOrder = station.DisplayOrder,
                IsExpo = station.IsExpo,
                CategoryCount = categories.Count(),
                ActiveOrderCount = activeOrders.Select(oi => oi.KdsOrderId).Distinct().Count(),
                LastConnectedAt = station.LastConnectedAt,
                IsActive = station.IsActive
            });
        }

        return result;
    }

    public async Task<List<KdsStationListDto>> GetStoreStationsAsync(int storeId)
    {
        return await GetStationsAsync(new KdsStationQueryDto
        {
            StoreId = storeId,
            IsActive = true,
            PageSize = 100
        });
    }

    public async Task<KdsStationDto> UpdateStationAsync(int id, UpdateKdsStationDto dto)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {id} not found.");
        }

        // Validate device identifier if being changed
        if (!string.IsNullOrEmpty(dto.DeviceIdentifier) &&
            dto.DeviceIdentifier != station.DeviceIdentifier &&
            !await IsDeviceIdentifierUniqueAsync(dto.DeviceIdentifier, id))
        {
            throw new InvalidOperationException($"Device identifier '{dto.DeviceIdentifier}' is already in use.");
        }

        // Validate station name if being changed
        if (!string.IsNullOrEmpty(dto.Name) &&
            dto.Name != station.Name &&
            !await IsStationNameUniqueAsync(dto.Name, station.StoreId, id))
        {
            throw new InvalidOperationException($"Station name '{dto.Name}' already exists in this store.");
        }

        // Update fields
        if (!string.IsNullOrEmpty(dto.Name)) station.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.DeviceIdentifier)) station.DeviceIdentifier = dto.DeviceIdentifier;
        if (dto.StationType.HasValue) station.StationType = MapToEntity(dto.StationType.Value);
        if (dto.Status.HasValue) station.Status = MapToEntity(dto.Status.Value);
        if (dto.DisplayOrder.HasValue) station.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsExpo.HasValue) station.IsExpo = dto.IsExpo.Value;
        if (dto.Description != null) station.Description = dto.Description;

        station.UpdatedAt = DateTime.UtcNow;
        await _stationRepository.UpdateAsync(station);

        // Update display settings if provided
        if (dto.DisplaySettings != null && station.DisplaySettingsId.HasValue)
        {
            await UpdateDisplaySettingsAsync(id, dto.DisplaySettings);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated KDS station {StationId} '{StationName}'", station.Id, station.Name);

        return await GetStationAsync(id) ?? throw new InvalidOperationException("Failed to retrieve updated station.");
    }

    public async Task<bool> DeleteStationAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            return false;
        }

        // Check for active orders
        var activeOrders = await _orderItemRepository.FindAsync(oi =>
            oi.StationId == id &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        if (activeOrders.Any())
        {
            throw new InvalidOperationException("Cannot delete station with active orders. Complete or void orders first.");
        }

        // Soft delete
        station.IsActive = false;
        station.UpdatedAt = DateTime.UtcNow;
        await _stationRepository.UpdateAsync(station);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted KDS station {StationId} '{StationName}'", station.Id, station.Name);

        return true;
    }

    #endregion

    #region Station Status Management

    public async Task<KdsStationDto> SetStationOnlineAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {id} not found.");
        }

        station.Status = KdsStationStatus.Online;
        station.LastConnectedAt = DateTime.UtcNow;
        station.UpdatedAt = DateTime.UtcNow;

        await _stationRepository.UpdateAsync(station);
        await _unitOfWork.SaveChangesAsync();

        var result = await GetStationAsync(id);
        StationOnline?.Invoke(this, result!);

        _logger.LogInformation("KDS station {StationId} '{StationName}' is now online", station.Id, station.Name);

        return result!;
    }

    public async Task<KdsStationDto> SetStationOfflineAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {id} not found.");
        }

        station.Status = KdsStationStatus.Offline;
        station.UpdatedAt = DateTime.UtcNow;

        await _stationRepository.UpdateAsync(station);
        await _unitOfWork.SaveChangesAsync();

        var result = await GetStationAsync(id);
        StationOffline?.Invoke(this, result!);

        _logger.LogInformation("KDS station {StationId} '{StationName}' is now offline", station.Id, station.Name);

        return result!;
    }

    public async Task<KdsStationDto> SetStationPausedAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {id} not found.");
        }

        station.Status = KdsStationStatus.Paused;
        station.UpdatedAt = DateTime.UtcNow;

        await _stationRepository.UpdateAsync(station);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("KDS station {StationId} '{StationName}' is now paused", station.Id, station.Name);

        return await GetStationAsync(id) ?? throw new InvalidOperationException("Failed to retrieve station.");
    }

    public async Task<KdsStationDto> UpdateLastConnectedAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {id} not found.");
        }

        station.LastConnectedAt = DateTime.UtcNow;
        await _stationRepository.UpdateAsync(station);
        await _unitOfWork.SaveChangesAsync();

        return await GetStationAsync(id) ?? throw new InvalidOperationException("Failed to retrieve station.");
    }

    #endregion

    #region Category Management

    public async Task<KdsStationCategoryDto> AssignCategoryAsync(int stationId, int categoryId, int? displayOrder = null)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null || !category.IsActive)
        {
            throw new InvalidOperationException($"Category with ID {categoryId} not found.");
        }

        // Check if already assigned
        var existing = await _stationCategoryRepository.FindAsync(sc =>
            sc.StationId == stationId && sc.CategoryId == categoryId && sc.IsActive);

        if (existing.Any())
        {
            throw new InvalidOperationException($"Category '{category.Name}' is already assigned to this station.");
        }

        // Get max display order if not provided
        if (!displayOrder.HasValue)
        {
            var categories = await _stationCategoryRepository.FindAsync(sc =>
                sc.StationId == stationId && sc.IsActive);
            displayOrder = categories.Any() ? categories.Max(c => c.DisplayOrder) + 1 : 1;
        }

        var stationCategory = new KdsStationCategory
        {
            StationId = stationId,
            CategoryId = categoryId,
            DisplayOrder = displayOrder.Value,
            IsActive = true
        };

        await _stationCategoryRepository.AddAsync(stationCategory);
        await _unitOfWork.SaveChangesAsync();

        var stationDto = await GetStationAsync(stationId);
        StationCategoriesUpdated?.Invoke(this, stationDto!);

        _logger.LogInformation("Assigned category {CategoryId} '{CategoryName}' to station {StationId}",
            categoryId, category.Name, stationId);

        return new KdsStationCategoryDto
        {
            Id = stationCategory.Id,
            StationId = stationId,
            CategoryId = categoryId,
            CategoryName = category.Name,
            DisplayOrder = stationCategory.DisplayOrder
        };
    }

    public async Task<bool> RemoveCategoryAsync(int stationId, int categoryId)
    {
        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.StationId == stationId && sc.CategoryId == categoryId && sc.IsActive);

        var stationCategory = stationCategories.FirstOrDefault();
        if (stationCategory == null)
        {
            return false;
        }

        stationCategory.IsActive = false;
        stationCategory.UpdatedAt = DateTime.UtcNow;
        await _stationCategoryRepository.UpdateAsync(stationCategory);
        await _unitOfWork.SaveChangesAsync();

        var stationDto = await GetStationAsync(stationId);
        StationCategoriesUpdated?.Invoke(this, stationDto!);

        _logger.LogInformation("Removed category {CategoryId} from station {StationId}", categoryId, stationId);

        return true;
    }

    public async Task<List<KdsStationCategoryDto>> GetStationCategoriesAsync(int stationId)
    {
        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.StationId == stationId && sc.IsActive);

        var result = new List<KdsStationCategoryDto>();
        foreach (var sc in stationCategories.OrderBy(c => c.DisplayOrder))
        {
            var category = await _categoryRepository.GetByIdAsync(sc.CategoryId);
            result.Add(new KdsStationCategoryDto
            {
                Id = sc.Id,
                StationId = sc.StationId,
                CategoryId = sc.CategoryId,
                CategoryName = category?.Name ?? "Unknown",
                DisplayOrder = sc.DisplayOrder
            });
        }

        return result;
    }

    public async Task<List<KdsStationCategoryDto>> UpdateCategoryOrdersAsync(int stationId, Dictionary<int, int> categoryOrders)
    {
        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.StationId == stationId && sc.IsActive);

        foreach (var sc in stationCategories)
        {
            if (categoryOrders.TryGetValue(sc.CategoryId, out var newOrder))
            {
                sc.DisplayOrder = newOrder;
                sc.UpdatedAt = DateTime.UtcNow;
                await _stationCategoryRepository.UpdateAsync(sc);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetStationCategoriesAsync(stationId);
    }

    public async Task<List<KdsStationListDto>> GetStationsForCategoryAsync(int categoryId, int? storeId = null)
    {
        var stationCategories = await _stationCategoryRepository.FindAsync(sc =>
            sc.CategoryId == categoryId && sc.IsActive);

        var stationIds = stationCategories.Select(sc => sc.StationId).Distinct().ToList();

        var query = new KdsStationQueryDto
        {
            StoreId = storeId,
            IsActive = true,
            PageSize = 100
        };

        var allStations = await GetStationsAsync(query);
        return allStations.Where(s => stationIds.Contains(s.Id)).ToList();
    }

    #endregion

    #region Display Settings

    public async Task<KdsDisplaySettingsDto> GetDisplaySettingsAsync(int stationId)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        if (!station.DisplaySettingsId.HasValue)
        {
            // Return default settings
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

        var settings = await _displaySettingsRepository.GetByIdAsync(station.DisplaySettingsId.Value);
        if (settings == null)
        {
            throw new InvalidOperationException("Display settings not found.");
        }

        return MapSettingsToDto(settings);
    }

    public async Task<KdsDisplaySettingsDto> UpdateDisplaySettingsAsync(int stationId, UpdateKdsDisplaySettingsDto dto)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        KdsDisplaySettings settings;
        if (station.DisplaySettingsId.HasValue)
        {
            settings = await _displaySettingsRepository.GetByIdAsync(station.DisplaySettingsId.Value)
                ?? throw new InvalidOperationException("Display settings not found.");
        }
        else
        {
            settings = new KdsDisplaySettings();
            await _displaySettingsRepository.AddAsync(settings);
            await _unitOfWork.SaveChangesAsync();

            station.DisplaySettingsId = settings.Id;
            await _stationRepository.UpdateAsync(station);
        }

        // Update fields
        if (dto.ColumnsCount.HasValue) settings.ColumnsCount = dto.ColumnsCount.Value;
        if (dto.FontSize.HasValue) settings.FontSize = dto.FontSize.Value;
        if (dto.WarningThresholdMinutes.HasValue) settings.WarningThresholdMinutes = dto.WarningThresholdMinutes.Value;
        if (dto.AlertThresholdMinutes.HasValue) settings.AlertThresholdMinutes = dto.AlertThresholdMinutes.Value;
        if (dto.GreenThresholdMinutes.HasValue) settings.GreenThresholdMinutes = dto.GreenThresholdMinutes.Value;
        if (dto.ShowModifiers.HasValue) settings.ShowModifiers = dto.ShowModifiers.Value;
        if (dto.ShowSpecialInstructions.HasValue) settings.ShowSpecialInstructions = dto.ShowSpecialInstructions.Value;
        if (dto.AudioAlerts.HasValue) settings.AudioAlerts = dto.AudioAlerts.Value;
        if (dto.FlashWhenOverdue.HasValue) settings.FlashWhenOverdue = dto.FlashWhenOverdue.Value;
        if (dto.FlashIntervalSeconds.HasValue) settings.FlashIntervalSeconds = dto.FlashIntervalSeconds.Value;
        if (dto.AudioRepeatIntervalSeconds.HasValue) settings.AudioRepeatIntervalSeconds = dto.AudioRepeatIntervalSeconds.Value;
        if (dto.RecallWindowMinutes.HasValue) settings.RecallWindowMinutes = dto.RecallWindowMinutes.Value;
        if (dto.ThemeName != null) settings.ThemeName = dto.ThemeName;
        if (dto.BackgroundColor != null) settings.BackgroundColor = dto.BackgroundColor;

        settings.UpdatedAt = DateTime.UtcNow;
        await _displaySettingsRepository.UpdateAsync(settings);
        await _unitOfWork.SaveChangesAsync();

        return MapSettingsToDto(settings);
    }

    public async Task<KdsDisplaySettingsDto> CreateDisplaySettingsAsync(CreateKdsDisplaySettingsDto dto)
    {
        var settings = new KdsDisplaySettings
        {
            ColumnsCount = dto.ColumnsCount,
            FontSize = dto.FontSize,
            WarningThresholdMinutes = dto.WarningThresholdMinutes,
            AlertThresholdMinutes = dto.AlertThresholdMinutes,
            GreenThresholdMinutes = dto.GreenThresholdMinutes,
            ShowModifiers = dto.ShowModifiers,
            ShowSpecialInstructions = dto.ShowSpecialInstructions,
            AudioAlerts = dto.AudioAlerts,
            FlashWhenOverdue = dto.FlashWhenOverdue,
            FlashIntervalSeconds = dto.FlashIntervalSeconds,
            AudioRepeatIntervalSeconds = dto.AudioRepeatIntervalSeconds,
            RecallWindowMinutes = dto.RecallWindowMinutes,
            ThemeName = dto.ThemeName,
            BackgroundColor = dto.BackgroundColor,
            IsActive = true
        };

        await _displaySettingsRepository.AddAsync(settings);
        await _unitOfWork.SaveChangesAsync();

        return MapSettingsToDto(settings);
    }

    #endregion

    #region Validation

    public async Task<bool> IsDeviceIdentifierUniqueAsync(string deviceIdentifier, int? excludeStationId = null)
    {
        var stations = await _stationRepository.FindAsync(s =>
            s.DeviceIdentifier == deviceIdentifier && s.IsActive);

        if (excludeStationId.HasValue)
        {
            stations = stations.Where(s => s.Id != excludeStationId.Value);
        }

        return !stations.Any();
    }

    public async Task<bool> IsStationNameUniqueAsync(string name, int storeId, int? excludeStationId = null)
    {
        var stations = await _stationRepository.FindAsync(s =>
            s.Name == name && s.StoreId == storeId && s.IsActive);

        if (excludeStationId.HasValue)
        {
            stations = stations.Where(s => s.Id != excludeStationId.Value);
        }

        return !stations.Any();
    }

    #endregion

    #region Statistics

    public async Task<KdsStationStatsDto> GetStationStatsAsync(int stationId, DateTime fromDate, DateTime toDate)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null || !station.IsActive)
        {
            throw new InvalidOperationException($"Station with ID {stationId} not found.");
        }

        var orderItems = await _orderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.CreatedAt >= fromDate &&
            oi.CreatedAt <= toDate &&
            oi.IsActive);

        var completedItems = orderItems.Where(oi => oi.Status == KdsItemStatus.Done && oi.CompletedAt.HasValue).ToList();

        var prepTimes = completedItems
            .Where(oi => oi.StartedAt.HasValue)
            .Select(oi => (oi.CompletedAt!.Value - oi.StartedAt!.Value))
            .ToList();

        var activeOrders = await _orderItemRepository.FindAsync(oi =>
            oi.StationId == stationId &&
            oi.Status != KdsItemStatus.Done &&
            oi.Status != KdsItemStatus.Voided &&
            oi.IsActive);

        return new KdsStationStatsDto
        {
            StationId = stationId,
            StationName = station.Name,
            TotalOrdersToday = orderItems.Select(oi => oi.KdsOrderId).Distinct().Count(),
            CompletedOrdersToday = completedItems.Select(oi => oi.KdsOrderId).Distinct().Count(),
            AveragePrepTime = prepTimes.Any() ? TimeSpan.FromTicks((long)prepTimes.Average(t => t.Ticks)) : TimeSpan.Zero,
            FastestPrepTime = prepTimes.Any() ? prepTimes.Min() : TimeSpan.Zero,
            SlowestPrepTime = prepTimes.Any() ? prepTimes.Max() : TimeSpan.Zero,
            CurrentActiveOrders = activeOrders.Select(oi => oi.KdsOrderId).Distinct().Count(),
            CurrentOverdueOrders = 0 // Calculated elsewhere
        };
    }

    #endregion

    #region Private Helpers

    private static KdsStationType MapToEntity(KdsStationTypeDto dto)
    {
        return dto switch
        {
            KdsStationTypeDto.PrepStation => KdsStationType.PrepStation,
            KdsStationTypeDto.Expo => KdsStationType.Expo,
            KdsStationTypeDto.Bar => KdsStationType.Bar,
            KdsStationTypeDto.Dessert => KdsStationType.Dessert,
            _ => KdsStationType.PrepStation
        };
    }

    private static KdsStationTypeDto MapToDto(KdsStationType entity)
    {
        return entity switch
        {
            KdsStationType.PrepStation => KdsStationTypeDto.PrepStation,
            KdsStationType.Expo => KdsStationTypeDto.Expo,
            KdsStationType.Bar => KdsStationTypeDto.Bar,
            KdsStationType.Dessert => KdsStationTypeDto.Dessert,
            _ => KdsStationTypeDto.PrepStation
        };
    }

    private static KdsStationStatus MapToEntity(KdsStationStatusDto dto)
    {
        return dto switch
        {
            KdsStationStatusDto.Offline => KdsStationStatus.Offline,
            KdsStationStatusDto.Online => KdsStationStatus.Online,
            KdsStationStatusDto.Paused => KdsStationStatus.Paused,
            _ => KdsStationStatus.Offline
        };
    }

    private static KdsStationStatusDto MapToDto(KdsStationStatus entity)
    {
        return entity switch
        {
            KdsStationStatus.Offline => KdsStationStatusDto.Offline,
            KdsStationStatus.Online => KdsStationStatusDto.Online,
            KdsStationStatus.Paused => KdsStationStatusDto.Paused,
            _ => KdsStationStatusDto.Offline
        };
    }

    private static KdsStationDto MapToDto(KdsStation station, List<KdsStationCategoryDto> categories, KdsDisplaySettingsDto? settings)
    {
        return new KdsStationDto
        {
            Id = station.Id,
            Name = station.Name,
            DeviceIdentifier = station.DeviceIdentifier,
            StationType = MapToDto(station.StationType),
            Status = MapToDto(station.Status),
            DisplayOrder = station.DisplayOrder,
            IsExpo = station.IsExpo,
            StoreId = station.StoreId,
            Description = station.Description,
            LastConnectedAt = station.LastConnectedAt,
            DisplaySettings = settings,
            Categories = categories,
            IsActive = station.IsActive,
            CreatedAt = station.CreatedAt,
            UpdatedAt = station.UpdatedAt
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

    #endregion
}
