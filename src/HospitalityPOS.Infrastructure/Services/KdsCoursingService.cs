using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of the KDS coursing service.
/// </summary>
public class KdsCoursingService : IKdsCoursingService
{
    private readonly IRepository<CourseDefinition> _courseDefRepository;
    private readonly IRepository<CourseConfiguration> _configRepository;
    private readonly IRepository<KdsCourseState> _courseStateRepository;
    private readonly IRepository<KdsOrder> _orderRepository;
    private readonly IRepository<KdsOrderItem> _orderItemRepository;
    private readonly IRepository<CourseFiringLog> _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KdsCoursingService> _logger;

    public KdsCoursingService(
        IRepository<CourseDefinition> courseDefRepository,
        IRepository<CourseConfiguration> configRepository,
        IRepository<KdsCourseState> courseStateRepository,
        IRepository<KdsOrder> orderRepository,
        IRepository<KdsOrderItem> orderItemRepository,
        IRepository<CourseFiringLog> logRepository,
        IUnitOfWork unitOfWork,
        ILogger<KdsCoursingService> logger)
    {
        _courseDefRepository = courseDefRepository;
        _configRepository = configRepository;
        _courseStateRepository = courseStateRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    #region Course Definition Management

    public async Task<List<CourseDefinitionDto>> GetCourseDefinitionsAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var courses = await _courseDefRepository.Query()
            .Where(c => c.StoreId == storeId && c.IsActive)
            .OrderBy(c => c.CourseNumber)
            .ToListAsync(cancellationToken);

        return courses.Select(MapCourseDefinitionToDto).ToList();
    }

    public async Task<CourseDefinitionDto?> GetCourseDefinitionAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _courseDefRepository.GetByIdAsync(courseId, cancellationToken);
        return course != null ? MapCourseDefinitionToDto(course) : null;
    }

    public async Task<CourseDefinitionDto> CreateCourseDefinitionAsync(CourseDefinitionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var course = new CourseDefinition
        {
            Name = dto.Name,
            CourseNumber = dto.CourseNumber,
            DefaultDelayMinutes = dto.DefaultDelayMinutes,
            IconUrl = dto.IconUrl,
            Color = dto.Color,
            Description = dto.Description,
            StoreId = dto.StoreId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _courseDefRepository.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created course definition {Name} (#{Number}) for store {StoreId}",
            course.Name, course.CourseNumber, course.StoreId);

        return MapCourseDefinitionToDto(course);
    }

    public async Task<CourseDefinitionDto> UpdateCourseDefinitionAsync(int courseId, CourseDefinitionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var course = await _courseDefRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            throw new InvalidOperationException($"Course definition {courseId} not found.");

        course.Name = dto.Name;
        course.CourseNumber = dto.CourseNumber;
        course.DefaultDelayMinutes = dto.DefaultDelayMinutes;
        course.IconUrl = dto.IconUrl;
        course.Color = dto.Color;
        course.Description = dto.Description;
        course.UpdatedAt = DateTime.UtcNow;

        _courseDefRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCourseDefinitionToDto(course);
    }

    public async Task DeleteCourseDefinitionAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _courseDefRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            throw new InvalidOperationException($"Course definition {courseId} not found.");

        course.IsActive = false;
        course.UpdatedAt = DateTime.UtcNow;

        _courseDefRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderCoursesAsync(List<int> courseIds, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < courseIds.Count; i++)
        {
            var course = await _courseDefRepository.GetByIdAsync(courseIds[i], cancellationToken);
            if (course != null)
            {
                course.CourseNumber = i + 1;
                course.UpdatedAt = DateTime.UtcNow;
                _courseDefRepository.Update(course);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Course Configuration

    public async Task<CourseConfigurationDto> GetConfigurationAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.Query()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.IsActive, cancellationToken);

        if (config == null)
        {
            // Return default configuration
            return new CourseConfigurationDto
            {
                StoreId = storeId,
                EnableCoursing = false,
                FireMode = CourseFireMode.AutoOnBump,
                DefaultCoursePacingMinutes = 10,
                AutoFireOnPreviousBump = true,
                AllowManualFireOverride = true,
                AllowRushMode = true,
                AutoFireFirstCourse = true,
                FireGracePeriodSeconds = 30,
                ShowCountdownToNextCourse = true,
                AlertOnReadyToFire = true
            };
        }

        return MapConfigToDto(config);
    }

    public async Task<CourseConfigurationDto> UpdateConfigurationAsync(int storeId, CourseConfigurationUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.Query()
            .FirstOrDefaultAsync(c => c.StoreId == storeId, cancellationToken);

        if (config == null)
        {
            config = new CourseConfiguration
            {
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _configRepository.AddAsync(config, cancellationToken);
        }

        config.EnableCoursing = dto.EnableCoursing;
        config.FireMode = dto.FireMode;
        config.DefaultCoursePacingMinutes = dto.DefaultCoursePacingMinutes;
        config.AutoFireOnPreviousBump = dto.AutoFireOnPreviousBump;
        config.ShowHeldCoursesOnPrepStation = dto.ShowHeldCoursesOnPrepStation;
        config.RequireExpoConfirmation = dto.RequireExpoConfirmation;
        config.AllowManualFireOverride = dto.AllowManualFireOverride;
        config.AllowRushMode = dto.AllowRushMode;
        config.AutoFireFirstCourse = dto.AutoFireFirstCourse;
        config.FireGracePeriodSeconds = dto.FireGracePeriodSeconds;
        config.ShowCountdownToNextCourse = dto.ShowCountdownToNextCourse;
        config.AlertOnReadyToFire = dto.AlertOnReadyToFire;
        config.FireAlertSound = dto.FireAlertSound;
        config.UpdatedAt = DateTime.UtcNow;

        _configRepository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapConfigToDto(config);
    }

    public async Task<bool> IsCoursingEnabledAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.Query()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.IsActive, cancellationToken);

        return config?.EnableCoursing ?? false;
    }

    #endregion

    #region Order Course Management

    public async Task<List<KdsCourseStateDto>> InitializeOrderCoursesAsync(int kdsOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.Query()
            .Include(o => o.Items)
            .Include(o => o.Store)
            .FirstOrDefaultAsync(o => o.Id == kdsOrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"KDS order {kdsOrderId} not found.");

        // Check if coursing is enabled
        var config = await GetConfigurationAsync(order.StoreId, cancellationToken);
        if (!config.EnableCoursing)
        {
            return new List<KdsCourseStateDto>();
        }

        // Get course definitions
        var courseDefs = await GetCourseDefinitionsAsync(order.StoreId, cancellationToken);
        if (!courseDefs.Any())
        {
            return new List<KdsCourseStateDto>();
        }

        // Group items by course number
        var itemsByCourse = order.Items
            .Where(i => i.CourseNumber.HasValue)
            .GroupBy(i => i.CourseNumber!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var courseStates = new List<KdsCourseState>();
        DateTime? previousCourseFire = null;

        foreach (var courseDef in courseDefs.OrderBy(c => c.CourseNumber))
        {
            var items = itemsByCourse.GetValueOrDefault(courseDef.CourseNumber, new List<KdsOrderItem>());
            if (!items.Any()) continue;

            // Calculate scheduled fire time
            DateTime? scheduledFire = null;
            if (courseDef.CourseNumber == 1 && config.AutoFireFirstCourse)
            {
                scheduledFire = now; // First course fires immediately
            }
            else if (previousCourseFire.HasValue)
            {
                scheduledFire = previousCourseFire.Value.AddMinutes(courseDef.DefaultDelayMinutes);
            }

            var courseState = new KdsCourseState
            {
                KdsOrderId = kdsOrderId,
                CourseDefinitionId = courseDef.Id,
                CourseNumber = courseDef.CourseNumber,
                CourseName = courseDef.Name,
                Status = courseDef.CourseNumber == 1 && config.AutoFireFirstCourse
                    ? CourseStatus.Fired
                    : CourseStatus.Pending,
                ScheduledFireAt = scheduledFire,
                FiredAt = courseDef.CourseNumber == 1 && config.AutoFireFirstCourse ? now : null,
                TargetMinutesAfterPrevious = courseDef.DefaultDelayMinutes,
                DisplayColor = courseDef.Color,
                TotalItems = items.Count,
                CompletedItems = 0,
                CreatedAt = now,
                IsActive = true
            };

            await _courseStateRepository.AddAsync(courseState, cancellationToken);
            courseStates.Add(courseState);

            // Update items with course state reference
            foreach (var item in items)
            {
                item.ItemFireStatus = courseDef.CourseNumber == 1 && config.AutoFireFirstCourse
                    ? ItemFireStatus.Fired
                    : ItemFireStatus.Waiting;
                item.ScheduledFireAt = scheduledFire;
                if (courseDef.CourseNumber == 1 && config.AutoFireFirstCourse)
                {
                    item.FiredAt = now;
                }
                _orderItemRepository.Update(item);
            }

            previousCourseFire = scheduledFire ?? previousCourseFire?.AddMinutes(courseDef.DefaultDelayMinutes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Now update course state IDs on items
        foreach (var state in courseStates)
        {
            var items = order.Items.Where(i => i.CourseNumber == state.CourseNumber);
            foreach (var item in items)
            {
                item.CourseStateId = state.Id;
                _orderItemRepository.Update(item);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Initialized {Count} courses for KDS order {OrderId}",
            courseStates.Count, kdsOrderId);

        return courseStates.Select(MapCourseStateToDto).ToList();
    }

    public async Task<List<KdsCourseStateDto>> GetOrderCoursesAsync(int kdsOrderId, CancellationToken cancellationToken = default)
    {
        var states = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.IsActive)
            .Include(s => s.Items)
            .OrderBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        return states.Select(MapCourseStateToDto).ToList();
    }

    public async Task<KdsCourseStateDto?> GetCourseStateAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.CourseNumber == courseNumber && s.IsActive)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(cancellationToken);

        return state != null ? MapCourseStateToDto(state) : null;
    }

    public async Task<CourseStatusSummary> GetCourseStatusSummaryAsync(int kdsOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(kdsOrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"KDS order {kdsOrderId} not found.");

        var states = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        var currentCourse = states.FirstOrDefault(s =>
            s.Status == CourseStatus.Fired ||
            s.Status == CourseStatus.InProgress);

        var nextPending = states.FirstOrDefault(s =>
            s.Status == CourseStatus.Pending ||
            s.Status == CourseStatus.Scheduled);

        return new CourseStatusSummary
        {
            KdsOrderId = kdsOrderId,
            OrderNumber = order.OrderNumber,
            TableNumber = order.TableNumber,
            TotalCourses = states.Count,
            PendingCourses = states.Count(s => s.Status == CourseStatus.Pending || s.Status == CourseStatus.Scheduled),
            FiredCourses = states.Count(s => s.Status == CourseStatus.Fired || s.Status == CourseStatus.InProgress),
            ReadyCourses = states.Count(s => s.Status == CourseStatus.Ready),
            ServedCourses = states.Count(s => s.Status == CourseStatus.Served),
            HeldCourses = states.Count(s => s.IsOnHold),
            CurrentCourseNumber = currentCourse?.CourseNumber,
            CurrentCourseStatus = currentCourse?.Status,
            NextCourseFireAt = nextPending?.ScheduledFireAt,
            Courses = states.Select(s => new CourseStatusEntry
            {
                CourseNumber = s.CourseNumber,
                CourseName = s.CourseName,
                Status = s.Status,
                IsOnHold = s.IsOnHold,
                CompletedItems = s.CompletedItems,
                TotalItems = s.TotalItems,
                ScheduledFireAt = s.ScheduledFireAt,
                FiredAt = s.FiredAt,
                ReadyAt = s.CompletedAt,
                ServedAt = s.ServedAt
            }).ToList()
        };
    }

    #endregion

    #region Course Firing

    public async Task<FireCourseResult> FireCourseAsync(int kdsOrderId, int courseNumber, int userId, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.CourseNumber == courseNumber && s.IsActive)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(cancellationToken);

        if (state == null)
        {
            return new FireCourseResult
            {
                Success = false,
                Message = $"Course {courseNumber} not found for order.",
                CourseNumber = courseNumber
            };
        }

        if (state.Status == CourseStatus.Fired || state.Status == CourseStatus.InProgress)
        {
            return new FireCourseResult
            {
                Success = false,
                Message = "Course is already fired.",
                CourseNumber = courseNumber
            };
        }

        if (state.IsOnHold)
        {
            return new FireCourseResult
            {
                Success = false,
                Message = "Course is on hold. Release hold before firing.",
                CourseNumber = courseNumber
            };
        }

        var now = DateTime.UtcNow;
        state.Status = CourseStatus.Fired;
        state.FiredAt = now;
        state.FiredByUserId = userId;
        state.UpdatedAt = now;

        // Update all items in this course
        var items = await _orderItemRepository.Query()
            .Where(i => i.KdsOrderId == kdsOrderId && i.CourseNumber == courseNumber)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.ItemFireStatus = ItemFireStatus.Fired;
            item.FiredAt = now;
            item.FiredByUserId = userId;
            item.Status = KdsItemStatus.Fired;
            _orderItemRepository.Update(item);
        }

        _courseStateRepository.Update(state);

        // Log the action
        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.ManualFire, state.Status, userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Check for next course
        var nextCourse = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId &&
                        s.CourseNumber > courseNumber &&
                        s.Status == CourseStatus.Pending &&
                        s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        _logger.LogInformation("Fired course {CourseNumber} for order {OrderId} by user {UserId}",
            courseNumber, kdsOrderId, userId);

        return new FireCourseResult
        {
            Success = true,
            Message = "Course fired successfully.",
            CourseNumber = courseNumber,
            FiredItems = items.Select(MapItemToDto).ToList(),
            FiredAt = now,
            NextCourseNumber = nextCourse?.CourseNumber,
            NextCourseScheduledAt = nextCourse?.ScheduledFireAt
        };
    }

    public async Task<FireCourseResult> FireNextCourseAsync(int kdsOrderId, int userId, CancellationToken cancellationToken = default)
    {
        var nextCourse = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId &&
                        (s.Status == CourseStatus.Pending || s.Status == CourseStatus.Scheduled) &&
                        !s.IsOnHold &&
                        s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextCourse == null)
        {
            return new FireCourseResult
            {
                Success = false,
                Message = "No pending courses to fire."
            };
        }

        return await FireCourseAsync(kdsOrderId, nextCourse.CourseNumber, userId, cancellationToken);
    }

    public async Task<FireCourseResult> FireAllCoursesAsync(int kdsOrderId, int userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var pendingCourses = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId &&
                        (s.Status == CourseStatus.Pending || s.Status == CourseStatus.Scheduled) &&
                        s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        if (!pendingCourses.Any())
        {
            return new FireCourseResult
            {
                Success = false,
                Message = "No pending courses to fire."
            };
        }

        var now = DateTime.UtcNow;
        var allFiredItems = new List<KdsCourseItemDto>();

        foreach (var state in pendingCourses)
        {
            // Release any holds
            if (state.IsOnHold)
            {
                state.IsOnHold = false;
                state.HoldReason = null;
            }

            state.Status = CourseStatus.Fired;
            state.FiredAt = now;
            state.FiredByUserId = userId;
            state.UpdatedAt = now;
            _courseStateRepository.Update(state);

            // Update items
            var items = await _orderItemRepository.Query()
                .Where(i => i.KdsOrderId == kdsOrderId && i.CourseNumber == state.CourseNumber)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.ItemFireStatus = ItemFireStatus.Fired;
                item.FiredAt = now;
                item.FiredByUserId = userId;
                item.Status = KdsItemStatus.Fired;
                item.IsOnHold = false;
                _orderItemRepository.Update(item);
                allFiredItems.Add(MapItemToDto(item));
            }

            // Log
            await LogCourseActionAsync(kdsOrderId, state.Id, state.CourseNumber,
                CourseFiringAction.Rushed, state.Status, userId, reason, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rushed all {Count} courses for order {OrderId} by user {UserId}. Reason: {Reason}",
            pendingCourses.Count, kdsOrderId, userId, reason ?? "None");

        return new FireCourseResult
        {
            Success = true,
            Message = $"Rushed {pendingCourses.Count} courses.",
            CourseNumber = pendingCourses.First().CourseNumber,
            FiredItems = allFiredItems,
            FiredAt = now
        };
    }

    public async Task<bool> CanFireCourseAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null) return false;
        if (state.Status == CourseStatus.Fired || state.Status == CourseStatus.InProgress) return false;
        if (state.IsOnHold) return false;

        return true;
    }

    #endregion

    #region Auto-Fire Processing

    public async Task ProcessCourseBumpAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(kdsOrderId, cancellationToken);
        if (order == null) return;

        var config = await GetConfigurationAsync(order.StoreId, cancellationToken);
        if (!config.EnableCoursing || !config.AutoFireOnPreviousBump) return;

        // Find next pending course
        var nextCourse = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId &&
                        s.CourseNumber > courseNumber &&
                        s.Status == CourseStatus.Pending &&
                        !s.IsOnHold &&
                        s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextCourse == null) return;

        var now = DateTime.UtcNow;

        // Check if we should fire immediately or schedule
        if (config.FireMode == CourseFireMode.AutoOnBump)
        {
            // Schedule to fire after the delay
            nextCourse.ScheduledFireAt = now.AddMinutes(nextCourse.TargetMinutesAfterPrevious);
            nextCourse.Status = CourseStatus.Scheduled;
            nextCourse.UpdatedAt = now;
            _courseStateRepository.Update(nextCourse);

            await LogCourseActionAsync(kdsOrderId, nextCourse.Id, nextCourse.CourseNumber,
                CourseFiringAction.Scheduled, nextCourse.Status, null, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Scheduled course {CourseNumber} for order {OrderId} at {Time}",
                nextCourse.CourseNumber, kdsOrderId, nextCourse.ScheduledFireAt);
        }
    }

    public async Task<int> ProcessScheduledFiresAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var scheduledCourses = await _courseStateRepository.Query()
            .Where(s => s.Status == CourseStatus.Scheduled &&
                        s.ScheduledFireAt.HasValue &&
                        s.ScheduledFireAt.Value <= now &&
                        !s.IsOnHold &&
                        s.IsActive)
            .Include(s => s.KdsOrder)
            .ToListAsync(cancellationToken);

        var firedCount = 0;

        foreach (var course in scheduledCourses)
        {
            try
            {
                course.Status = CourseStatus.Fired;
                course.FiredAt = now;
                course.UpdatedAt = now;
                _courseStateRepository.Update(course);

                // Update items
                var items = await _orderItemRepository.Query()
                    .Where(i => i.KdsOrderId == course.KdsOrderId && i.CourseNumber == course.CourseNumber)
                    .ToListAsync(cancellationToken);

                foreach (var item in items)
                {
                    item.ItemFireStatus = ItemFireStatus.Fired;
                    item.FiredAt = now;
                    item.Status = KdsItemStatus.Fired;
                    _orderItemRepository.Update(item);
                }

                await LogCourseActionAsync(course.KdsOrderId, course.Id, course.CourseNumber,
                    CourseFiringAction.AutoFiredOnTimer, course.Status, null, cancellationToken);

                firedCount++;

                _logger.LogInformation("Auto-fired scheduled course {CourseNumber} for order {OrderId}",
                    course.CourseNumber, course.KdsOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-firing course {CourseNumber} for order {OrderId}",
                    course.CourseNumber, course.KdsOrderId);
            }
        }

        if (firedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return firedCount;
    }

    #endregion

    #region Hold Management

    public async Task<HoldCourseResult> HoldCourseAsync(int kdsOrderId, int courseNumber, string reason, int userId, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null)
        {
            return new HoldCourseResult
            {
                Success = false,
                Message = $"Course {courseNumber} not found."
            };
        }

        if (state.Status == CourseStatus.Served)
        {
            return new HoldCourseResult
            {
                Success = false,
                Message = "Cannot hold a served course."
            };
        }

        var now = DateTime.UtcNow;
        state.IsOnHold = true;
        state.HoldReason = reason;
        state.HeldByUserId = userId;
        state.HeldAt = now;
        state.Status = CourseStatus.Held;
        state.UpdatedAt = now;

        _courseStateRepository.Update(state);

        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.Held, state.Status, userId, reason, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Held course {CourseNumber} for order {OrderId}. Reason: {Reason}",
            courseNumber, kdsOrderId, reason);

        return new HoldCourseResult
        {
            Success = true,
            Message = "Course held successfully.",
            CourseNumber = courseNumber,
            HoldReason = reason,
            HeldAt = now,
            HeldByUserId = userId
        };
    }

    public async Task<HoldCourseResult> ReleaseCourseHoldAsync(int kdsOrderId, int courseNumber, int userId, bool fireImmediately = false, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null)
        {
            return new HoldCourseResult
            {
                Success = false,
                Message = $"Course {courseNumber} not found."
            };
        }

        if (!state.IsOnHold)
        {
            return new HoldCourseResult
            {
                Success = false,
                Message = "Course is not on hold."
            };
        }

        var now = DateTime.UtcNow;
        state.IsOnHold = false;
        state.HoldReason = null;
        state.Status = fireImmediately ? CourseStatus.Fired : CourseStatus.Pending;
        if (fireImmediately)
        {
            state.FiredAt = now;
            state.FiredByUserId = userId;
        }
        state.UpdatedAt = now;

        _courseStateRepository.Update(state);

        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.HoldReleased, state.Status, userId, cancellationToken);

        if (fireImmediately)
        {
            // Update items
            var items = await _orderItemRepository.Query()
                .Where(i => i.KdsOrderId == kdsOrderId && i.CourseNumber == courseNumber)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.ItemFireStatus = ItemFireStatus.Fired;
                item.FiredAt = now;
                item.FiredByUserId = userId;
                item.Status = KdsItemStatus.Fired;
                item.IsOnHold = false;
                _orderItemRepository.Update(item);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Released hold on course {CourseNumber} for order {OrderId}. Fire immediately: {Fire}",
            courseNumber, kdsOrderId, fireImmediately);

        return new HoldCourseResult
        {
            Success = true,
            Message = fireImmediately ? "Course released and fired." : "Course hold released.",
            CourseNumber = courseNumber,
            HeldAt = now,
            HeldByUserId = userId
        };
    }

    public async Task<List<HeldCourse>> GetHeldCoursesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var heldCourses = await _courseStateRepository.Query()
            .Where(s => s.IsOnHold && s.IsActive)
            .Include(s => s.KdsOrder)
            .Include(s => s.HeldByUser)
            .Where(s => s.KdsOrder!.StoreId == storeId)
            .ToListAsync(cancellationToken);

        return heldCourses.Select(s => new HeldCourse
        {
            KdsOrderId = s.KdsOrderId,
            OrderNumber = s.KdsOrder?.OrderNumber ?? "",
            TableNumber = s.KdsOrder?.TableNumber,
            CourseNumber = s.CourseNumber,
            CourseName = s.CourseName,
            HoldReason = s.HoldReason,
            HeldAt = s.HeldAt ?? DateTime.UtcNow,
            HeldByUserName = s.HeldByUser?.DisplayName ?? "Unknown",
            ItemCount = s.TotalItems
        }).ToList();
    }

    #endregion

    #region Course Timing

    public async Task<CourseTiming> GetCourseTimingAsync(int kdsOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(kdsOrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"KDS order {kdsOrderId} not found.");

        var states = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.IsActive)
            .OrderBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        var entries = new List<CourseTimingEntry>();
        DateTime? previousFire = null;

        foreach (var state in states)
        {
            TimeSpan? targetDelay = state.CourseNumber == 1 ? null : TimeSpan.FromMinutes(state.TargetMinutesAfterPrevious);
            TimeSpan? actualDelay = null;

            if (previousFire.HasValue && state.FiredAt.HasValue)
            {
                actualDelay = state.FiredAt.Value - previousFire.Value;
            }

            var isOnTrack = true;
            if (targetDelay.HasValue && actualDelay.HasValue)
            {
                isOnTrack = actualDelay.Value <= targetDelay.Value.Add(TimeSpan.FromMinutes(2)); // 2 min grace
            }

            entries.Add(new CourseTimingEntry
            {
                CourseNumber = state.CourseNumber,
                CourseName = state.CourseName,
                Status = state.Status,
                ScheduledFireAt = state.ScheduledFireAt,
                ActualFiredAt = state.FiredAt,
                CompletedAt = state.CompletedAt,
                ServedAt = state.ServedAt,
                TargetDelay = targetDelay,
                ActualDelay = actualDelay,
                IsOnTrack = isOnTrack
            });

            previousFire = state.FiredAt ?? state.ScheduledFireAt;
        }

        var lastCourse = states.LastOrDefault();
        var estimatedCompletion = lastCourse?.ScheduledFireAt?.AddMinutes(15) ?? order.ReceivedAt.AddHours(1);

        return new CourseTiming
        {
            KdsOrderId = kdsOrderId,
            OrderNumber = order.OrderNumber,
            OrderReceivedAt = order.ReceivedAt,
            Courses = entries,
            EstimatedTotalTime = estimatedCompletion - order.ReceivedAt,
            EstimatedCompletionTime = estimatedCompletion,
            IsOnTrack = entries.All(e => e.IsOnTrack)
        };
    }

    public async Task SetCourseDelayAsync(int kdsOrderId, int courseNumber, int delayMinutes, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null)
            throw new InvalidOperationException($"Course {courseNumber} not found.");

        state.TargetMinutesAfterPrevious = delayMinutes;
        state.UpdatedAt = DateTime.UtcNow;

        // Recalculate scheduled fire time
        var previousCourse = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId &&
                        s.CourseNumber < courseNumber &&
                        s.IsActive)
            .OrderByDescending(s => s.CourseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousCourse?.FiredAt != null || previousCourse?.ScheduledFireAt != null)
        {
            var baseTime = previousCourse.FiredAt ?? previousCourse.ScheduledFireAt!.Value;
            state.ScheduledFireAt = baseTime.AddMinutes(delayMinutes);
        }

        _courseStateRepository.Update(state);

        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.DelayChanged, state.Status, null, $"Delay set to {delayMinutes} minutes", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CourseSchedule> CalculateCourseScheduleAsync(int kdsOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(kdsOrderId, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"KDS order {kdsOrderId} not found.");

        var states = await _courseStateRepository.Query()
            .Where(s => s.KdsOrderId == kdsOrderId && s.IsActive)
            .Include(s => s.Items)
            .OrderBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        var entries = new List<CourseScheduleEntry>();
        var baseTime = order.ReceivedAt;

        foreach (var state in states)
        {
            var fireTime = state.CourseNumber == 1 ? baseTime : baseTime.AddMinutes(state.TargetMinutesAfterPrevious);

            entries.Add(new CourseScheduleEntry
            {
                CourseNumber = state.CourseNumber,
                CourseName = state.CourseName,
                ScheduledFireAt = fireTime,
                DelayMinutesFromPrevious = state.TargetMinutesAfterPrevious,
                ItemCount = state.TotalItems,
                EstimatedPrepTimeMinutes = 10 // Could be calculated from item prep times
            });

            baseTime = fireTime;
        }

        return new CourseSchedule
        {
            KdsOrderId = kdsOrderId,
            Entries = entries,
            EstimatedOrderCompletion = entries.LastOrDefault()?.ScheduledFireAt.AddMinutes(15) ?? order.ReceivedAt.AddHours(1)
        };
    }

    #endregion

    #region Course Completion

    public async Task<CourseCompletionResult> MarkCourseReadyAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null)
        {
            return new CourseCompletionResult
            {
                Success = false,
                Message = $"Course {courseNumber} not found."
            };
        }

        var now = DateTime.UtcNow;
        state.Status = CourseStatus.Ready;
        state.CompletedAt = now;
        state.UpdatedAt = now;

        _courseStateRepository.Update(state);

        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.MarkedReady, state.Status, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseCompletionResult
        {
            Success = true,
            Message = "Course marked as ready.",
            CourseNumber = courseNumber,
            NewStatus = CourseStatus.Ready,
            CompletedAt = now,
            AllItemsComplete = state.CompletedItems >= state.TotalItems,
            CompletedItems = state.CompletedItems,
            TotalItems = state.TotalItems
        };
    }

    public async Task<CourseCompletionResult> MarkCourseServedAsync(int kdsOrderId, int courseNumber, int userId, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null)
        {
            return new CourseCompletionResult
            {
                Success = false,
                Message = $"Course {courseNumber} not found."
            };
        }

        var now = DateTime.UtcNow;
        state.Status = CourseStatus.Served;
        state.ServedAt = now;
        state.ServedByUserId = userId;
        state.UpdatedAt = now;

        _courseStateRepository.Update(state);

        await LogCourseActionAsync(kdsOrderId, state.Id, courseNumber,
            CourseFiringAction.MarkedServed, state.Status, userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Trigger auto-fire for next course
        await ProcessCourseBumpAsync(kdsOrderId, courseNumber, cancellationToken);

        return new CourseCompletionResult
        {
            Success = true,
            Message = "Course marked as served.",
            CourseNumber = courseNumber,
            NewStatus = CourseStatus.Served,
            ServedAt = now,
            AllItemsComplete = true,
            CompletedItems = state.CompletedItems,
            TotalItems = state.TotalItems
        };
    }

    public async Task UpdateCourseProgressAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default)
    {
        var state = await _courseStateRepository.Query()
            .FirstOrDefaultAsync(s => s.KdsOrderId == kdsOrderId &&
                                      s.CourseNumber == courseNumber &&
                                      s.IsActive, cancellationToken);

        if (state == null) return;

        // Count completed items
        var completedCount = await _orderItemRepository.Query()
            .CountAsync(i => i.KdsOrderId == kdsOrderId &&
                             i.CourseNumber == courseNumber &&
                             i.Status == KdsItemStatus.Done, cancellationToken);

        state.CompletedItems = completedCount;

        // Auto-mark ready if all items complete
        if (completedCount >= state.TotalItems && state.Status == CourseStatus.InProgress)
        {
            state.Status = CourseStatus.Ready;
            state.CompletedAt = DateTime.UtcNow;
        }
        else if (completedCount > 0 && state.Status == CourseStatus.Fired)
        {
            state.Status = CourseStatus.InProgress;
        }

        state.UpdatedAt = DateTime.UtcNow;
        _courseStateRepository.Update(state);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Queries

    public async Task<List<KdsCourseStateDto>> GetPendingCoursesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var states = await _courseStateRepository.Query()
            .Where(s => (s.Status == CourseStatus.Pending || s.Status == CourseStatus.Scheduled) && s.IsActive)
            .Include(s => s.KdsOrder)
            .Where(s => s.KdsOrder!.StoreId == storeId)
            .OrderBy(s => s.ScheduledFireAt)
            .ThenBy(s => s.KdsOrderId)
            .ThenBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        return states.Select(MapCourseStateToDto).ToList();
    }

    public async Task<List<KdsCourseStateDto>> GetFiredCoursesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var states = await _courseStateRepository.Query()
            .Where(s => (s.Status == CourseStatus.Fired || s.Status == CourseStatus.InProgress) && s.IsActive)
            .Include(s => s.KdsOrder)
            .Where(s => s.KdsOrder!.StoreId == storeId)
            .OrderBy(s => s.FiredAt)
            .ThenBy(s => s.KdsOrderId)
            .ThenBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        return states.Select(MapCourseStateToDto).ToList();
    }

    public async Task<List<KdsCourseStateDto>> GetReadyCoursesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var states = await _courseStateRepository.Query()
            .Where(s => s.Status == CourseStatus.Ready && s.IsActive)
            .Include(s => s.KdsOrder)
            .Where(s => s.KdsOrder!.StoreId == storeId)
            .OrderBy(s => s.CompletedAt)
            .ThenBy(s => s.KdsOrderId)
            .ThenBy(s => s.CourseNumber)
            .ToListAsync(cancellationToken);

        return states.Select(MapCourseStateToDto).ToList();
    }

    public async Task<List<KdsCourseStateDto>> GetCoursesReadyToFireAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var states = await _courseStateRepository.Query()
            .Where(s => s.Status == CourseStatus.Scheduled &&
                        s.ScheduledFireAt.HasValue &&
                        s.ScheduledFireAt.Value <= now &&
                        !s.IsOnHold &&
                        s.IsActive)
            .Include(s => s.KdsOrder)
            .Where(s => s.KdsOrder!.StoreId == storeId)
            .OrderBy(s => s.ScheduledFireAt)
            .ToListAsync(cancellationToken);

        return states.Select(MapCourseStateToDto).ToList();
    }

    #endregion

    #region Private Helpers

    private async Task LogCourseActionAsync(
        int kdsOrderId,
        int? courseStateId,
        int courseNumber,
        CourseFiringAction action,
        CourseStatus newStatus,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await LogCourseActionAsync(kdsOrderId, courseStateId, courseNumber, action, newStatus, userId, null, cancellationToken);
    }

    private async Task LogCourseActionAsync(
        int kdsOrderId,
        int? courseStateId,
        int courseNumber,
        CourseFiringAction action,
        CourseStatus newStatus,
        int? userId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var log = new CourseFiringLog
        {
            KdsOrderId = kdsOrderId,
            CourseStateId = courseStateId,
            CourseNumber = courseNumber,
            Action = action,
            NewStatus = newStatus,
            UserId = userId,
            ActionAt = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _logRepository.AddAsync(log, cancellationToken);
    }

    private static CourseDefinitionDto MapCourseDefinitionToDto(CourseDefinition course)
    {
        return new CourseDefinitionDto
        {
            Id = course.Id,
            Name = course.Name,
            CourseNumber = course.CourseNumber,
            DefaultDelayMinutes = course.DefaultDelayMinutes,
            IconUrl = course.IconUrl,
            Color = course.Color,
            Description = course.Description,
            IsActive = course.IsActive,
            StoreId = course.StoreId
        };
    }

    private static CourseConfigurationDto MapConfigToDto(CourseConfiguration config)
    {
        return new CourseConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            EnableCoursing = config.EnableCoursing,
            FireMode = config.FireMode,
            DefaultCoursePacingMinutes = config.DefaultCoursePacingMinutes,
            AutoFireOnPreviousBump = config.AutoFireOnPreviousBump,
            ShowHeldCoursesOnPrepStation = config.ShowHeldCoursesOnPrepStation,
            RequireExpoConfirmation = config.RequireExpoConfirmation,
            AllowManualFireOverride = config.AllowManualFireOverride,
            AllowRushMode = config.AllowRushMode,
            AutoFireFirstCourse = config.AutoFireFirstCourse,
            FireGracePeriodSeconds = config.FireGracePeriodSeconds,
            ShowCountdownToNextCourse = config.ShowCountdownToNextCourse,
            AlertOnReadyToFire = config.AlertOnReadyToFire,
            FireAlertSound = config.FireAlertSound
        };
    }

    private static KdsCourseStateDto MapCourseStateToDto(KdsCourseState state)
    {
        return new KdsCourseStateDto
        {
            Id = state.Id,
            KdsOrderId = state.KdsOrderId,
            CourseNumber = state.CourseNumber,
            CourseName = state.CourseName,
            Status = state.Status,
            ScheduledFireAt = state.ScheduledFireAt,
            FiredAt = state.FiredAt,
            CompletedAt = state.CompletedAt,
            ServedAt = state.ServedAt,
            IsOnHold = state.IsOnHold,
            HoldReason = state.HoldReason,
            HeldAt = state.HeldAt,
            TargetMinutesAfterPrevious = state.TargetMinutesAfterPrevious,
            DisplayColor = state.DisplayColor,
            TotalItems = state.TotalItems,
            CompletedItems = state.CompletedItems,
            ProgressPercentage = state.ProgressPercentage,
            IsCurrent = state.IsCurrent,
            Items = state.Items?.Select(MapItemToDto).ToList() ?? new List<KdsCourseItemDto>()
        };
    }

    private static KdsCourseItemDto MapItemToDto(KdsOrderItem item)
    {
        return new KdsCourseItemDto
        {
            Id = item.Id,
            KdsOrderItemId = item.Id,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Modifiers = item.Modifiers,
            SpecialInstructions = item.SpecialInstructions,
            FireStatus = item.ItemFireStatus,
            ItemStatus = item.Status,
            ScheduledFireAt = item.ScheduledFireAt,
            FiredAt = item.FiredAt,
            CompletedAt = item.CompletedAt,
            IsOnHold = item.IsOnHold,
            HoldReason = item.HoldReason,
            StationId = item.StationId,
            StationName = item.Station?.Name
        };
    }

    #endregion
}
