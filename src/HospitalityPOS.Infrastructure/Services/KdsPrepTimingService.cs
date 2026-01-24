using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for KDS prep timing and sequencing.
/// Ensures all items in an order/course are ready simultaneously by firing items
/// with longer prep times before those with shorter prep times.
/// </summary>
public class KdsPrepTimingService : IKdsPrepTimingService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<KdsPrepTimingService> _logger;

    public KdsPrepTimingService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<KdsPrepTimingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Configuration

    public async Task<PrepTimingConfigurationDto> GetConfigurationAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.Set<PrepTimingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == storeId);

        if (config == null)
        {
            // Return default configuration
            return new PrepTimingConfigurationDto
            {
                StoreId = storeId,
                EnablePrepTiming = false,
                DefaultPrepTimeSeconds = 300,
                MinPrepTimeSeconds = 60,
                TargetReadyBufferSeconds = 60,
                AllowManualFireOverride = true,
                ShowWaitingItemsOnStation = true,
                Mode = PrepTimingMode.CourseLevel,
                AutoFireEnabled = true,
                OverdueThresholdSeconds = 120,
                AlertOnOverdue = true
            };
        }

        return MapToConfigurationDto(config);
    }

    public async Task<PrepTimingConfigurationDto> UpdateConfigurationAsync(PrepTimingConfigurationDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.Set<PrepTimingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == dto.StoreId);

        if (config == null)
        {
            config = new PrepTimingConfiguration
            {
                StoreId = dto.StoreId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<PrepTimingConfiguration>().Add(config);
        }

        config.EnablePrepTiming = dto.EnablePrepTiming;
        config.DefaultPrepTimeSeconds = dto.DefaultPrepTimeSeconds;
        config.MinPrepTimeSeconds = dto.MinPrepTimeSeconds;
        config.TargetReadyBufferSeconds = dto.TargetReadyBufferSeconds;
        config.AllowManualFireOverride = dto.AllowManualFireOverride;
        config.ShowWaitingItemsOnStation = dto.ShowWaitingItemsOnStation;
        config.Mode = dto.Mode;
        config.AutoFireEnabled = dto.AutoFireEnabled;
        config.OverdueThresholdSeconds = dto.OverdueThresholdSeconds;
        config.AlertOnOverdue = dto.AlertOnOverdue;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        dto.Id = config.Id;
        return dto;
    }

    public async Task<bool> IsPrepTimingEnabledAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.Set<PrepTimingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == storeId);

        return config?.EnablePrepTiming ?? false;
    }

    #endregion

    #region Product Prep Times

    public async Task<int> GetProductPrepTimeAsync(int productId, int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check for product-specific prep time
        var productConfig = await context.Set<ProductPrepTimeConfig>()
            .Where(p => p.ProductId == productId)
            .Where(p => storeId == null ? p.StoreId == null : p.StoreId == storeId || p.StoreId == null)
            .OrderByDescending(p => p.StoreId) // Store-specific takes priority
            .FirstOrDefaultAsync();

        if (productConfig != null)
        {
            return productConfig.TotalPrepTimeSeconds;
        }

        // Check category default
        var product = await context.Products.FindAsync(productId);
        if (product?.CategoryId != null)
        {
            var categoryDefault = await context.Set<CategoryPrepTimeDefault>()
                .Where(c => c.CategoryId == product.CategoryId)
                .Where(c => storeId == null ? c.StoreId == null : c.StoreId == storeId || c.StoreId == null)
                .OrderByDescending(c => c.StoreId)
                .FirstOrDefaultAsync();

            if (categoryDefault != null)
            {
                return categoryDefault.TotalPrepTimeSeconds;
            }
        }

        // Return store default or system default
        if (storeId.HasValue)
        {
            var storeConfig = await context.Set<PrepTimingConfiguration>()
                .FirstOrDefaultAsync(c => c.StoreId == storeId);
            if (storeConfig != null)
            {
                return storeConfig.DefaultPrepTimeSeconds;
            }
        }

        return 300; // 5 minutes default
    }

    public async Task SetProductPrepTimeAsync(SetProductPrepTimeRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Set<ProductPrepTimeConfig>()
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId &&
                                      p.StoreId == request.StoreId);

        if (existing == null)
        {
            existing = new ProductPrepTimeConfig
            {
                ProductId = request.ProductId,
                StoreId = request.StoreId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<ProductPrepTimeConfig>().Add(existing);
        }

        existing.PrepTimeMinutes = request.PrepTimeMinutes;
        existing.PrepTimeSeconds = request.PrepTimeSeconds;
        existing.UsesPrepTiming = request.UsesPrepTiming;
        existing.IsTimingIntegral = request.IsTimingIntegral;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<int> CalculateItemPrepTimeAsync(int productId, List<int> modifierIds, int? storeId = null)
    {
        var basePrepTime = await GetProductPrepTimeAsync(productId, storeId);

        if (modifierIds.Count == 0)
        {
            return basePrepTime;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var modifierAdjustments = await context.Set<ModifierPrepTimeAdjustment>()
            .Where(m => modifierIds.Contains(m.ModifierItemId))
            .Where(m => storeId == null ? m.StoreId == null : m.StoreId == storeId || m.StoreId == null)
            .ToListAsync();

        foreach (var adj in modifierAdjustments)
        {
            if (adj.AdjustmentType == PrepTimeAdjustmentType.Integral)
            {
                basePrepTime += adj.AdjustmentSeconds;
            }
            // Independent modifiers are handled separately
            // Ignored modifiers don't affect timing
        }

        return Math.Max(basePrepTime, 60); // Minimum 1 minute
    }

    public async Task<List<ProductPrepTimeDto>> GetAllProductPrepTimesAsync(int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Set<ProductPrepTimeConfig>()
            .Include(p => p.Product)
            .ThenInclude(p => p!.Category)
            .Where(p => storeId == null || p.StoreId == storeId || p.StoreId == null);

        var configs = await query.ToListAsync();

        return configs.Select(c => new ProductPrepTimeDto
        {
            ProductId = c.ProductId,
            ProductName = c.Product?.Name ?? "",
            CategoryId = c.Product?.CategoryId,
            CategoryName = c.Product?.Category?.Name,
            PrepTimeMinutes = c.PrepTimeMinutes,
            PrepTimeSeconds = c.PrepTimeSeconds,
            TotalPrepTimeSeconds = c.TotalPrepTimeSeconds,
            UsesPrepTiming = c.UsesPrepTiming,
            IsTimingIntegral = c.IsTimingIntegral,
            StoreId = c.StoreId
        }).ToList();
    }

    public async Task BulkUpdatePrepTimesAsync(BulkPrepTimeUpdateRequest request)
    {
        foreach (var product in request.Products)
        {
            await SetProductPrepTimeAsync(product);
        }
    }

    public async Task<List<CategoryPrepTimeDefaultDto>> GetCategoryPrepTimeDefaultsAsync(int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var defaults = await context.Set<CategoryPrepTimeDefault>()
            .Include(c => c.Category)
            .Where(c => storeId == null || c.StoreId == storeId || c.StoreId == null)
            .ToListAsync();

        return defaults.Select(d => new CategoryPrepTimeDefaultDto
        {
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name ?? "",
            DefaultPrepTimeMinutes = d.DefaultPrepTimeMinutes,
            DefaultPrepTimeSeconds = d.DefaultPrepTimeSeconds,
            TotalPrepTimeSeconds = d.TotalPrepTimeSeconds,
            StoreId = d.StoreId
        }).ToList();
    }

    public async Task SetCategoryPrepTimeDefaultAsync(int categoryId, int minutes, int seconds, int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Set<CategoryPrepTimeDefault>()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.StoreId == storeId);

        if (existing == null)
        {
            existing = new CategoryPrepTimeDefault
            {
                CategoryId = categoryId,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<CategoryPrepTimeDefault>().Add(existing);
        }

        existing.DefaultPrepTimeMinutes = minutes;
        existing.DefaultPrepTimeSeconds = seconds;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<List<ModifierPrepTimeDto>> GetModifierPrepTimeAdjustmentsAsync(int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var adjustments = await context.Set<ModifierPrepTimeAdjustment>()
            .Include(m => m.ModifierItem)
            .Where(m => storeId == null || m.StoreId == storeId || m.StoreId == null)
            .ToListAsync();

        return adjustments.Select(a => new ModifierPrepTimeDto
        {
            ModifierItemId = a.ModifierItemId,
            ModifierItemName = a.ModifierItem?.Name ?? "",
            AdjustmentSeconds = a.AdjustmentSeconds,
            AdjustmentType = a.AdjustmentType
        }).ToList();
    }

    public async Task SetModifierPrepTimeAdjustmentAsync(int modifierItemId, int adjustmentSeconds, PrepTimeAdjustmentType type, int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Set<ModifierPrepTimeAdjustment>()
            .FirstOrDefaultAsync(m => m.ModifierItemId == modifierItemId && m.StoreId == storeId);

        if (existing == null)
        {
            existing = new ModifierPrepTimeAdjustment
            {
                ModifierItemId = modifierItemId,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<ModifierPrepTimeAdjustment>().Add(existing);
        }

        existing.AdjustmentSeconds = adjustmentSeconds;
        existing.AdjustmentType = type;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    #endregion

    #region Fire Schedule Calculation

    public async Task<List<ItemFireScheduleDto>> CalculateFireScheduleAsync(int kdsOrderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.KdsOrders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == kdsOrderId);

        if (order == null)
        {
            return new List<ItemFireScheduleDto>();
        }

        var config = await GetConfigurationAsync(order.StoreId);
        var targetReadyTime = order.CreatedAt.AddMinutes(15); // Default 15 min service time

        var schedules = new List<ItemFireScheduleDto>();

        foreach (var item in order.Items)
        {
            var prepTime = await GetProductPrepTimeAsync(item.ProductId, order.StoreId);
            var scheduledFireAt = targetReadyTime.AddSeconds(-prepTime - config.TargetReadyBufferSeconds);

            // Ensure minimum fire delay
            var minFireTime = order.CreatedAt.AddSeconds(config.MinPrepTimeSeconds);
            if (scheduledFireAt < minFireTime)
            {
                scheduledFireAt = minFireTime;
            }

            schedules.Add(new ItemFireScheduleDto
            {
                KdsOrderItemId = item.Id,
                KdsOrderId = kdsOrderId,
                CourseNumber = item.CourseNumber,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? item.ProductName,
                StationId = item.StationId,
                PrepTimeSeconds = prepTime,
                OrderReceivedAt = order.CreatedAt,
                TargetReadyAt = targetReadyTime,
                ScheduledFireAt = scheduledFireAt,
                Status = ItemFireStatus.Scheduled,
                TimeUntilFire = scheduledFireAt > DateTime.UtcNow ? scheduledFireAt - DateTime.UtcNow : null,
                TimeUntilReady = targetReadyTime > DateTime.UtcNow ? targetReadyTime - DateTime.UtcNow : null,
                IsOverdue = DateTime.UtcNow > targetReadyTime.AddSeconds(config.OverdueThresholdSeconds)
            });
        }

        return schedules.OrderBy(s => s.ScheduledFireAt).ToList();
    }

    public async Task<List<ItemFireScheduleDto>> CalculateCourseFireScheduleAsync(int kdsOrderId, int courseNumber)
    {
        var allSchedules = await CalculateFireScheduleAsync(kdsOrderId);
        return allSchedules.Where(s => s.CourseNumber == courseNumber).ToList();
    }

    public async Task<ItemFireScheduleDto> RecalculateItemFireTimeAsync(int kdsOrderItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedule = await context.Set<ItemFireSchedule>()
            .Include(s => s.KdsOrderItem)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.KdsOrderItemId == kdsOrderItemId);

        if (schedule == null)
        {
            throw new InvalidOperationException($"No fire schedule found for item {kdsOrderItemId}");
        }

        var prepTime = await GetProductPrepTimeAsync(schedule.ProductId, schedule.StoreId);
        var config = await GetConfigurationAsync(schedule.StoreId);

        schedule.PrepTimeSeconds = prepTime;
        schedule.ScheduledFireAt = schedule.TargetReadyAt.AddSeconds(-prepTime - config.TargetReadyBufferSeconds);
        schedule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return MapToFireScheduleDto(schedule);
    }

    public async Task UpdateTargetReadyTimeAsync(int kdsOrderId, DateTime newTargetTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Where(s => s.KdsOrderId == kdsOrderId)
            .Where(s => s.Status == ItemFireStatus.Waiting || s.Status == ItemFireStatus.Scheduled)
            .ToListAsync();

        if (!schedules.Any())
        {
            return;
        }

        var config = await GetConfigurationAsync(schedules.First().StoreId);

        foreach (var schedule in schedules)
        {
            schedule.TargetReadyAt = newTargetTime;
            schedule.ScheduledFireAt = newTargetTime.AddSeconds(-schedule.PrepTimeSeconds - config.TargetReadyBufferSeconds);
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<ItemFireScheduleDto>> CreateFireSchedulesForOrderAsync(int kdsOrderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.KdsOrders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == kdsOrderId);

        if (order == null)
        {
            return new List<ItemFireScheduleDto>();
        }

        var config = await GetConfigurationAsync(order.StoreId);
        if (!config.EnablePrepTiming)
        {
            return new List<ItemFireScheduleDto>();
        }

        // Calculate target ready time based on mode
        var targetReadyTime = CalculateTargetReadyTime(order, config);

        var createdSchedules = new List<ItemFireSchedule>();

        foreach (var item in order.Items)
        {
            // Check if schedule already exists
            var existingSchedule = await context.Set<ItemFireSchedule>()
                .FirstOrDefaultAsync(s => s.KdsOrderItemId == item.Id);

            if (existingSchedule != null)
            {
                continue;
            }

            var prepTime = await GetProductPrepTimeAsync(item.ProductId, order.StoreId);
            var scheduledFireAt = targetReadyTime.AddSeconds(-prepTime - config.TargetReadyBufferSeconds);

            // Ensure minimum fire delay
            var minFireTime = order.CreatedAt.AddSeconds(config.MinPrepTimeSeconds);
            if (scheduledFireAt < minFireTime)
            {
                scheduledFireAt = minFireTime;
            }

            var schedule = new ItemFireSchedule
            {
                KdsOrderItemId = item.Id,
                KdsOrderId = kdsOrderId,
                CourseNumber = item.CourseNumber,
                ProductId = item.ProductId,
                StationId = item.StationId,
                StoreId = order.StoreId,
                PrepTimeSeconds = prepTime,
                OrderReceivedAt = order.CreatedAt,
                TargetReadyAt = targetReadyTime,
                ScheduledFireAt = scheduledFireAt,
                Status = ItemFireStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            context.Set<ItemFireSchedule>().Add(schedule);
            createdSchedules.Add(schedule);
        }

        await context.SaveChangesAsync();

        return createdSchedules.Select(MapToFireScheduleDto).ToList();
    }

    #endregion

    #region Schedule Execution

    public async Task<PrepTimingJobResult> ProcessScheduledFiresAsync()
    {
        var result = new PrepTimingJobResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.UtcNow;

            // Get items ready to fire
            var readyToFire = await context.Set<ItemFireSchedule>()
                .Where(s => s.Status == ItemFireStatus.Scheduled)
                .Where(s => s.ScheduledFireAt <= now)
                .ToListAsync();

            foreach (var schedule in readyToFire)
            {
                var config = await GetConfigurationAsync(schedule.StoreId);

                if (config.AutoFireEnabled)
                {
                    schedule.Status = ItemFireStatus.Fired;
                    schedule.ActualFiredAt = now;
                    result.ItemsFired++;
                }
                else
                {
                    schedule.Status = ItemFireStatus.ReadyToFire;
                }

                schedule.UpdatedAt = now;
                result.ItemsProcessed++;
            }

            // Check for overdue items
            var overdueThreshold = now.AddSeconds(-120); // 2 minutes
            var overdueItems = await context.Set<ItemFireSchedule>()
                .Where(s => s.Status == ItemFireStatus.Fired || s.Status == ItemFireStatus.Preparing)
                .Where(s => s.TargetReadyAt < overdueThreshold)
                .Where(s => s.ActualReadyAt == null)
                .ToListAsync();

            result.ItemsMarkedOverdue = overdueItems.Count;

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled fires");
            result.Errors.Add(ex.Message);
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    public async Task<FireResult> FireItemAsync(int kdsOrderItemId, int userId, string? notes = null)
    {
        return await FireItemsAsync(new List<int> { kdsOrderItemId }, userId, notes);
    }

    public async Task<FireResult> FireItemsAsync(List<int> kdsOrderItemIds, int userId, string? notes = null)
    {
        var result = new FireResult
        {
            FiredAt = DateTime.UtcNow
        };

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var schedules = await context.Set<ItemFireSchedule>()
                .Where(s => kdsOrderItemIds.Contains(s.KdsOrderItemId))
                .Where(s => s.Status == ItemFireStatus.Waiting ||
                           s.Status == ItemFireStatus.Scheduled ||
                           s.Status == ItemFireStatus.ReadyToFire ||
                           s.Status == ItemFireStatus.Held)
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                var config = await GetConfigurationAsync(schedule.StoreId);

                if (!config.AllowManualFireOverride && schedule.Status == ItemFireStatus.Waiting)
                {
                    result.Errors.Add($"Manual fire not allowed for item {schedule.KdsOrderItemId}");
                    continue;
                }

                schedule.Status = ItemFireStatus.Fired;
                schedule.ActualFiredAt = result.FiredAt;
                schedule.WasManuallyFired = true;
                schedule.FiredByUserId = userId;
                schedule.Notes = notes;
                schedule.UpdatedAt = result.FiredAt;

                result.FiredItemIds.Add(schedule.KdsOrderItemId);
            }

            await context.SaveChangesAsync();

            result.Success = result.FiredItemIds.Any();
            result.Message = result.Success
                ? $"Fired {result.FiredItemIds.Count} item(s)"
                : "No items were fired";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing items manually");
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<FireResult> FireAllOrderItemsAsync(int kdsOrderId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var itemIds = await context.Set<ItemFireSchedule>()
            .Where(s => s.KdsOrderId == kdsOrderId)
            .Where(s => s.Status != ItemFireStatus.Fired &&
                       s.Status != ItemFireStatus.Preparing &&
                       s.Status != ItemFireStatus.Done &&
                       s.Status != ItemFireStatus.Cancelled)
            .Select(s => s.KdsOrderItemId)
            .ToListAsync();

        return await FireItemsAsync(itemIds, userId, "Fire all");
    }

    public async Task HoldItemAsync(int kdsOrderItemId, string? reason = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedule = await context.Set<ItemFireSchedule>()
            .FirstOrDefaultAsync(s => s.KdsOrderItemId == kdsOrderItemId);

        if (schedule == null)
        {
            throw new InvalidOperationException($"No fire schedule found for item {kdsOrderItemId}");
        }

        schedule.Status = ItemFireStatus.Held;
        schedule.Notes = reason;
        schedule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task ReleaseHeldItemAsync(int kdsOrderItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedule = await context.Set<ItemFireSchedule>()
            .FirstOrDefaultAsync(s => s.KdsOrderItemId == kdsOrderItemId && s.Status == ItemFireStatus.Held);

        if (schedule == null)
        {
            throw new InvalidOperationException($"No held item found for {kdsOrderItemId}");
        }

        // Determine new status based on scheduled fire time
        if (DateTime.UtcNow >= schedule.ScheduledFireAt)
        {
            schedule.Status = ItemFireStatus.ReadyToFire;
        }
        else
        {
            schedule.Status = ItemFireStatus.Scheduled;
        }

        schedule.Notes = null;
        schedule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task MarkItemDoneAsync(int kdsOrderItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedule = await context.Set<ItemFireSchedule>()
            .FirstOrDefaultAsync(s => s.KdsOrderItemId == kdsOrderItemId);

        if (schedule == null)
        {
            return; // No schedule for this item
        }

        schedule.Status = ItemFireStatus.Done;
        schedule.ActualReadyAt = DateTime.UtcNow;
        schedule.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Update accuracy data
        await UpdateProductAccuracyDataAsync(schedule.ProductId, schedule.StoreId);
    }

    #endregion

    #region Status Tracking

    public async Task<PrepTimingStatus> GetOrderPrepTimingStatusAsync(int kdsOrderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.KdsOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == kdsOrderId);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {kdsOrderId} not found");
        }

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Where(s => s.KdsOrderId == kdsOrderId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var targetReady = schedules.FirstOrDefault()?.TargetReadyAt ?? now.AddMinutes(15);

        return new PrepTimingStatus
        {
            KdsOrderId = kdsOrderId,
            OrderNumber = order.OrderNumber,
            OrderReceivedAt = order.CreatedAt,
            TargetReadyAt = targetReady,
            TimeUntilReady = targetReady > now ? targetReady - now : TimeSpan.Zero,
            TotalItems = schedules.Count,
            WaitingItems = schedules.Count(s => s.Status == ItemFireStatus.Waiting || s.Status == ItemFireStatus.Scheduled),
            FiredItems = schedules.Count(s => s.Status == ItemFireStatus.Fired || s.Status == ItemFireStatus.ReadyToFire),
            PreparingItems = schedules.Count(s => s.Status == ItemFireStatus.Preparing),
            CompletedItems = schedules.Count(s => s.Status == ItemFireStatus.Done),
            LongestPrepTime = TimeSpan.FromSeconds(schedules.Max(s => s.PrepTimeSeconds)),
            IsOnTrack = schedules.All(s => s.Status == ItemFireStatus.Done || s.TargetReadyAt > now),
            CompletionPercentage = schedules.Count > 0
                ? (decimal)schedules.Count(s => s.Status == ItemFireStatus.Done) / schedules.Count * 100
                : 0,
            Items = schedules.Select(s => new ItemTimingStatus
            {
                KdsOrderItemId = s.KdsOrderItemId,
                ProductName = s.Product?.Name ?? "",
                Quantity = 1,
                PrepTimeSeconds = s.PrepTimeSeconds,
                Status = s.Status,
                ScheduledFireAt = s.ScheduledFireAt,
                ActualFiredAt = s.ActualFiredAt,
                TargetReadyAt = s.TargetReadyAt,
                TimeUntilFire = s.ScheduledFireAt > now ? s.ScheduledFireAt - now : null,
                TimeUntilReady = s.TargetReadyAt > now ? s.TargetReadyAt - now : null,
                IsOverdue = s.TargetReadyAt < now && s.ActualReadyAt == null,
                StationName = s.Station?.Name,
                ProgressPercentage = CalculateProgress(s, now)
            }).ToList()
        };
    }

    public async Task<List<ItemFireScheduleDto>> GetScheduledItemsAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Where(s => s.StoreId == storeId)
            .Where(s => s.Status == ItemFireStatus.Scheduled || s.Status == ItemFireStatus.ReadyToFire)
            .OrderBy(s => s.ScheduledFireAt)
            .ToListAsync();

        return schedules.Select(MapToFireScheduleDto).ToList();
    }

    public async Task<List<ItemFireScheduleDto>> GetWaitingItemsAsync(int stationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Where(s => s.StationId == stationId)
            .Where(s => s.Status == ItemFireStatus.Waiting || s.Status == ItemFireStatus.Scheduled)
            .OrderBy(s => s.ScheduledFireAt)
            .ToListAsync();

        return schedules.Select(MapToFireScheduleDto).ToList();
    }

    public async Task<List<ItemFireScheduleDto>> GetReadyToFireItemsAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var now = DateTime.UtcNow;

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Where(s => s.StoreId == storeId)
            .Where(s => s.Status == ItemFireStatus.Scheduled || s.Status == ItemFireStatus.ReadyToFire)
            .Where(s => s.ScheduledFireAt <= now)
            .OrderBy(s => s.ScheduledFireAt)
            .ToListAsync();

        return schedules.Select(MapToFireScheduleDto).ToList();
    }

    public async Task<List<ItemFireScheduleDto>> GetOverdueItemsAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        var overdueThreshold = DateTime.UtcNow.AddSeconds(-config.OverdueThresholdSeconds);

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Where(s => s.StoreId == storeId)
            .Where(s => s.Status != ItemFireStatus.Done && s.Status != ItemFireStatus.Cancelled)
            .Where(s => s.TargetReadyAt < overdueThreshold)
            .OrderBy(s => s.TargetReadyAt)
            .ToListAsync();

        return schedules.Select(MapToFireScheduleDto).ToList();
    }

    public async Task<ItemFireScheduleDto?> GetItemFireScheduleAsync(int kdsOrderItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedule = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.Station)
            .Include(s => s.FiredByUser)
            .FirstOrDefaultAsync(s => s.KdsOrderItemId == kdsOrderItemId);

        return schedule != null ? MapToFireScheduleDto(schedule) : null;
    }

    public async Task<List<KdsItemTimingDisplay>> GetStationItemsWithTimingAsync(int stationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Product)
            .Include(s => s.KdsOrderItem)
            .ThenInclude(i => i!.Modifiers)
            .Where(s => s.StationId == stationId)
            .Where(s => s.Status != ItemFireStatus.Done && s.Status != ItemFireStatus.Cancelled)
            .OrderBy(s => s.Status == ItemFireStatus.Fired || s.Status == ItemFireStatus.Preparing ? 0 : 1)
            .ThenBy(s => s.ScheduledFireAt)
            .ToListAsync();

        var now = DateTime.UtcNow;

        return schedules.Select(s => new KdsItemTimingDisplay
        {
            ItemId = s.KdsOrderItemId,
            ProductName = s.Product?.Name ?? "",
            Quantity = s.KdsOrderItem?.Quantity ?? 1,
            FireStatus = s.Status,
            TimeUntilFire = s.Status == ItemFireStatus.Scheduled && s.ScheduledFireAt > now
                ? s.ScheduledFireAt - now
                : null,
            TimeRemaining = s.ActualFiredAt.HasValue
                ? TimeSpan.FromSeconds(s.PrepTimeSeconds) - (now - s.ActualFiredAt.Value)
                : null,
            DisplayStyle = GetDisplayStyle(s, now),
            IsActionable = s.Status == ItemFireStatus.ReadyToFire ||
                          s.Status == ItemFireStatus.Fired ||
                          s.Status == ItemFireStatus.Preparing,
            StatusText = GetStatusText(s, now),
            Modifiers = s.KdsOrderItem?.Modifiers?.Select(m => m.ModifierItemName).ToList() ?? new List<string>()
        }).ToList();
    }

    #endregion

    #region Analytics

    public async Task<PrepTimingAccuracyReport> GetPrepTimingAccuracyAsync(int storeId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Where(s => s.StoreId == storeId)
            .Where(s => s.Status == ItemFireStatus.Done)
            .Where(s => s.ActualReadyAt >= from && s.ActualReadyAt <= to)
            .ToListAsync();

        var totalItems = schedules.Count;
        var onTime = schedules.Count(s => Math.Abs((s.ActualFiredAt!.Value - s.ScheduledFireAt).TotalSeconds) <= 30);
        var late = schedules.Count(s => s.ActualFiredAt > s.ScheduledFireAt.AddSeconds(30));
        var manual = schedules.Count(s => s.WasManuallyFired);

        var completedOnTarget = schedules.Count(s =>
            s.ActualReadyAt.HasValue &&
            Math.Abs((s.ActualReadyAt.Value - s.TargetReadyAt).TotalSeconds) <= 30);
        var completedEarly = schedules.Count(s =>
            s.ActualReadyAt.HasValue &&
            s.ActualReadyAt.Value < s.TargetReadyAt.AddSeconds(-30));
        var completedLate = schedules.Count(s =>
            s.ActualReadyAt.HasValue &&
            s.ActualReadyAt.Value > s.TargetReadyAt.AddSeconds(30));

        var avgDeviation = totalItems > 0
            ? (int)schedules
                .Where(s => s.ActualReadyAt.HasValue)
                .Average(s => Math.Abs((s.ActualReadyAt!.Value - s.TargetReadyAt).TotalSeconds))
            : 0;

        // Daily metrics
        var dailyMetrics = await context.Set<PrepTimingDailyMetrics>()
            .Where(m => m.StoreId == storeId)
            .Where(m => m.Date >= from.Date && m.Date <= to.Date)
            .OrderBy(m => m.Date)
            .Select(m => new DailyAccuracyMetrics
            {
                Date = m.Date,
                TotalItems = m.TotalItemsScheduled,
                AccuracyRate = m.AccuracyRate,
                AverageDeviationSeconds = m.AverageDeviationSeconds
            })
            .ToListAsync();

        return new PrepTimingAccuracyReport
        {
            FromDate = from,
            ToDate = to,
            TotalItemsScheduled = totalItems,
            ItemsFiredOnTime = onTime,
            ItemsFiredLate = late,
            ItemsManuallyFired = manual,
            OnTimeFireRate = totalItems > 0 ? (decimal)onTime / totalItems : 0,
            ItemsCompletedOnTarget = completedOnTarget,
            ItemsCompletedEarly = completedEarly,
            ItemsCompletedLate = completedLate,
            CompletionAccuracyRate = totalItems > 0 ? (decimal)completedOnTarget / totalItems : 0,
            AverageDeviationSeconds = avgDeviation,
            DailyMetrics = dailyMetrics
        };
    }

    public async Task<List<ProductAccuracyReport>> GetProductAccuracyAsync(int storeId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var accuracyData = await context.Set<ProductPrepTimeAccuracy>()
            .Include(a => a.Product)
            .Where(a => a.StoreId == storeId)
            .Where(a => a.LastCalculatedAt >= from)
            .ToListAsync();

        return accuracyData.Select(a => new ProductAccuracyReport
        {
            ProductId = a.ProductId,
            ProductName = a.Product?.Name ?? "",
            ConfiguredPrepTimeSeconds = a.ConfiguredPrepTimeSeconds,
            AverageActualPrepTimeSeconds = a.AverageActualPrepTimeSeconds,
            SampleCount = a.SampleCount,
            StandardDeviationSeconds = a.StandardDeviationSeconds,
            MinPrepTimeSeconds = a.MinPrepTimeSeconds,
            MaxPrepTimeSeconds = a.MaxPrepTimeSeconds,
            SuggestedPrepTimeSeconds = a.SuggestedPrepTimeSeconds,
            AccuracyRate = a.AccuracyRate,
            VarianceSeconds = Math.Abs(a.AverageActualPrepTimeSeconds - a.ConfiguredPrepTimeSeconds),
            NeedsAdjustment = Math.Abs(a.AverageActualPrepTimeSeconds - a.ConfiguredPrepTimeSeconds) > a.ConfiguredPrepTimeSeconds * 0.2m
        }).ToList();
    }

    public async Task<PrepTimingPerformanceSummary> GetPerformanceSummaryAsync(int storeId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var schedules = await context.Set<ItemFireSchedule>()
            .Include(s => s.Station)
            .Where(s => s.StoreId == storeId)
            .Where(s => s.Status == ItemFireStatus.Done)
            .Where(s => s.ActualReadyAt >= from && s.ActualReadyAt <= to)
            .ToListAsync();

        var orderIds = schedules.Select(s => s.KdsOrderId).Distinct().ToList();
        var totalOrders = orderIds.Count;

        // Calculate on-time completion
        var orderCompletions = schedules
            .GroupBy(s => s.KdsOrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                AllOnTime = g.All(s => s.ActualReadyAt.HasValue &&
                                       s.ActualReadyAt.Value <= s.TargetReadyAt.AddSeconds(30))
            })
            .ToList();

        var onTimeOrders = orderCompletions.Count(o => o.AllOnTime);

        // Station performance
        var byStation = schedules
            .Where(s => s.StationId.HasValue)
            .GroupBy(s => new { s.StationId, StationName = s.Station?.Name ?? "Unknown" })
            .Select(g => new StationPerformance
            {
                StationId = g.Key.StationId!.Value,
                StationName = g.Key.StationName,
                TotalItems = g.Count(),
                AccuracyRate = (decimal)g.Count(s =>
                    s.ActualReadyAt.HasValue &&
                    Math.Abs((s.ActualReadyAt.Value - s.TargetReadyAt).TotalSeconds) <= 30) / g.Count(),
                AverageDeviationSeconds = (int)g
                    .Where(s => s.ActualReadyAt.HasValue)
                    .Average(s => Math.Abs((s.ActualReadyAt!.Value - s.TargetReadyAt).TotalSeconds))
            })
            .ToList();

        var productsNeedingAdjustment = await GetProductsNeedingAdjustmentAsync(storeId);

        return new PrepTimingPerformanceSummary
        {
            FromDate = from,
            ToDate = to,
            TotalOrders = totalOrders,
            OrdersCompletedOnTime = onTimeOrders,
            OnTimeCompletionRate = totalOrders > 0 ? (decimal)onTimeOrders / totalOrders : 0,
            AverageOrderPrepTime = schedules.Any()
                ? TimeSpan.FromSeconds(schedules.Average(s => s.PrepTimeSeconds))
                : TimeSpan.Zero,
            AverageDeviation = schedules.Any()
                ? TimeSpan.FromSeconds(schedules
                    .Where(s => s.ActualReadyAt.HasValue)
                    .Average(s => Math.Abs((s.ActualReadyAt!.Value - s.TargetReadyAt).TotalSeconds)))
                : TimeSpan.Zero,
            ByStation = byStation,
            ProductsNeedingAdjustment = productsNeedingAdjustment
        };
    }

    public async Task UpdateProductAccuracyDataAsync(int productId, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Get recent completions for this product
        var recentCompletions = await context.Set<ItemFireSchedule>()
            .Where(s => s.ProductId == productId && s.StoreId == storeId)
            .Where(s => s.Status == ItemFireStatus.Done)
            .Where(s => s.ActualFiredAt.HasValue && s.ActualReadyAt.HasValue)
            .OrderByDescending(s => s.ActualReadyAt)
            .Take(100) // Last 100 completions
            .ToListAsync();

        if (!recentCompletions.Any())
        {
            return;
        }

        var prepTimes = recentCompletions
            .Select(s => (s.ActualReadyAt!.Value - s.ActualFiredAt!.Value).TotalSeconds)
            .ToList();

        var avg = prepTimes.Average();
        var min = prepTimes.Min();
        var max = prepTimes.Max();
        var stdDev = CalculateStandardDeviation(prepTimes);

        var configuredPrepTime = await GetProductPrepTimeAsync(productId, storeId);
        var onTimeCount = recentCompletions.Count(s =>
            Math.Abs((s.ActualReadyAt!.Value - s.TargetReadyAt).TotalSeconds) <= 30);

        var accuracy = await context.Set<ProductPrepTimeAccuracy>()
            .FirstOrDefaultAsync(a => a.ProductId == productId && a.StoreId == storeId);

        if (accuracy == null)
        {
            accuracy = new ProductPrepTimeAccuracy
            {
                ProductId = productId,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<ProductPrepTimeAccuracy>().Add(accuracy);
        }

        accuracy.ConfiguredPrepTimeSeconds = configuredPrepTime;
        accuracy.AverageActualPrepTimeSeconds = (int)avg;
        accuracy.SampleCount = recentCompletions.Count;
        accuracy.StandardDeviationSeconds = (decimal)stdDev;
        accuracy.MinPrepTimeSeconds = (int)min;
        accuracy.MaxPrepTimeSeconds = (int)max;
        accuracy.SuggestedPrepTimeSeconds = (int)(avg + stdDev); // Avg + 1 std dev for safety
        accuracy.AccuracyRate = (decimal)onTimeCount / recentCompletions.Count;
        accuracy.LastCalculatedAt = DateTime.UtcNow;
        accuracy.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<List<ProductAccuracyReport>> GetProductsNeedingAdjustmentAsync(int storeId, decimal varianceThreshold = 0.2m)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var products = await context.Set<ProductPrepTimeAccuracy>()
            .Include(a => a.Product)
            .Where(a => a.StoreId == storeId)
            .Where(a => a.SampleCount >= 10) // Minimum samples
            .ToListAsync();

        return products
            .Where(a =>
            {
                var variance = Math.Abs(a.AverageActualPrepTimeSeconds - a.ConfiguredPrepTimeSeconds);
                return variance > a.ConfiguredPrepTimeSeconds * (double)varianceThreshold;
            })
            .Select(a => new ProductAccuracyReport
            {
                ProductId = a.ProductId,
                ProductName = a.Product?.Name ?? "",
                ConfiguredPrepTimeSeconds = a.ConfiguredPrepTimeSeconds,
                AverageActualPrepTimeSeconds = a.AverageActualPrepTimeSeconds,
                SampleCount = a.SampleCount,
                StandardDeviationSeconds = a.StandardDeviationSeconds,
                MinPrepTimeSeconds = a.MinPrepTimeSeconds,
                MaxPrepTimeSeconds = a.MaxPrepTimeSeconds,
                SuggestedPrepTimeSeconds = a.SuggestedPrepTimeSeconds,
                AccuracyRate = a.AccuracyRate,
                VarianceSeconds = Math.Abs(a.AverageActualPrepTimeSeconds - a.ConfiguredPrepTimeSeconds),
                NeedsAdjustment = true
            })
            .OrderByDescending(p => p.VarianceSeconds)
            .ToList();
    }

    #endregion

    #region Private Helpers

    private static PrepTimingConfigurationDto MapToConfigurationDto(PrepTimingConfiguration config)
    {
        return new PrepTimingConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            EnablePrepTiming = config.EnablePrepTiming,
            DefaultPrepTimeSeconds = config.DefaultPrepTimeSeconds,
            MinPrepTimeSeconds = config.MinPrepTimeSeconds,
            TargetReadyBufferSeconds = config.TargetReadyBufferSeconds,
            AllowManualFireOverride = config.AllowManualFireOverride,
            ShowWaitingItemsOnStation = config.ShowWaitingItemsOnStation,
            Mode = config.Mode,
            AutoFireEnabled = config.AutoFireEnabled,
            OverdueThresholdSeconds = config.OverdueThresholdSeconds,
            AlertOnOverdue = config.AlertOnOverdue
        };
    }

    private static ItemFireScheduleDto MapToFireScheduleDto(ItemFireSchedule schedule)
    {
        var now = DateTime.UtcNow;
        return new ItemFireScheduleDto
        {
            Id = schedule.Id,
            KdsOrderItemId = schedule.KdsOrderItemId,
            KdsOrderId = schedule.KdsOrderId,
            CourseNumber = schedule.CourseNumber,
            ProductId = schedule.ProductId,
            ProductName = schedule.Product?.Name ?? "",
            StationId = schedule.StationId,
            StationName = schedule.Station?.Name,
            PrepTimeSeconds = schedule.PrepTimeSeconds,
            OrderReceivedAt = schedule.OrderReceivedAt,
            TargetReadyAt = schedule.TargetReadyAt,
            ScheduledFireAt = schedule.ScheduledFireAt,
            ActualFiredAt = schedule.ActualFiredAt,
            ActualReadyAt = schedule.ActualReadyAt,
            Status = schedule.Status,
            WasManuallyFired = schedule.WasManuallyFired,
            FiredByUserId = schedule.FiredByUserId,
            FiredByUserName = schedule.FiredByUser?.FullName,
            TimeUntilFire = schedule.ScheduledFireAt > now ? schedule.ScheduledFireAt - now : null,
            TimeUntilReady = schedule.TargetReadyAt > now ? schedule.TargetReadyAt - now : null,
            IsOverdue = schedule.TargetReadyAt < now && schedule.ActualReadyAt == null
        };
    }

    private static DateTime CalculateTargetReadyTime(KdsOrder order, PrepTimingConfigurationDto config)
    {
        // Default: 15 minutes from order time
        // In production, this would consider table service time, delivery estimates, etc.
        return order.CreatedAt.AddMinutes(15);
    }

    private static decimal CalculateProgress(ItemFireSchedule schedule, DateTime now)
    {
        if (schedule.Status == ItemFireStatus.Done)
        {
            return 100;
        }

        if (!schedule.ActualFiredAt.HasValue)
        {
            return 0;
        }

        var elapsed = (now - schedule.ActualFiredAt.Value).TotalSeconds;
        var progress = (elapsed / schedule.PrepTimeSeconds) * 100;
        return Math.Min((decimal)progress, 99); // Cap at 99% until done
    }

    private static string GetDisplayStyle(ItemFireSchedule schedule, DateTime now)
    {
        if (schedule.Status == ItemFireStatus.Waiting || schedule.Status == ItemFireStatus.Scheduled)
        {
            return "waiting";
        }

        if (schedule.TargetReadyAt < now && schedule.ActualReadyAt == null)
        {
            return "overdue";
        }

        if (schedule.Status == ItemFireStatus.Fired || schedule.Status == ItemFireStatus.Preparing)
        {
            return "preparing";
        }

        return "normal";
    }

    private static string GetStatusText(ItemFireSchedule schedule, DateTime now)
    {
        return schedule.Status switch
        {
            ItemFireStatus.Waiting => "Waiting",
            ItemFireStatus.Scheduled => schedule.ScheduledFireAt > now
                ? $"Fire in {(int)(schedule.ScheduledFireAt - now).TotalMinutes}m"
                : "Ready to fire",
            ItemFireStatus.ReadyToFire => "Ready to fire",
            ItemFireStatus.Fired when schedule.ActualFiredAt.HasValue =>
                $"{(int)(now - schedule.ActualFiredAt.Value).TotalMinutes}m elapsed",
            ItemFireStatus.Preparing => "Preparing",
            ItemFireStatus.Done => "Done",
            ItemFireStatus.Held => "On hold",
            ItemFireStatus.Cancelled => "Cancelled",
            _ => ""
        };
    }

    private static double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;

        var avg = values.Average();
        var sumSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    #endregion
}
