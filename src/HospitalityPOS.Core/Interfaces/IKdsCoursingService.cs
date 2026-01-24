using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing KDS course firing and pacing.
/// </summary>
public interface IKdsCoursingService
{
    #region Course Definition Management

    /// <summary>
    /// Gets all course definitions for a store.
    /// </summary>
    Task<List<CourseDefinitionDto>> GetCourseDefinitionsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a course definition by ID.
    /// </summary>
    Task<CourseDefinitionDto?> GetCourseDefinitionAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new course definition.
    /// </summary>
    Task<CourseDefinitionDto> CreateCourseDefinitionAsync(CourseDefinitionCreateDto course, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a course definition.
    /// </summary>
    Task<CourseDefinitionDto> UpdateCourseDefinitionAsync(int courseId, CourseDefinitionCreateDto course, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a course definition.
    /// </summary>
    Task DeleteCourseDefinitionAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders course definitions.
    /// </summary>
    Task ReorderCoursesAsync(List<int> courseIds, CancellationToken cancellationToken = default);

    #endregion

    #region Course Configuration

    /// <summary>
    /// Gets the course configuration for a store.
    /// </summary>
    Task<CourseConfigurationDto> GetConfigurationAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the course configuration for a store.
    /// </summary>
    Task<CourseConfigurationDto> UpdateConfigurationAsync(int storeId, CourseConfigurationUpdateDto config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if coursing is enabled for a store.
    /// </summary>
    Task<bool> IsCoursingEnabledAsync(int storeId, CancellationToken cancellationToken = default);

    #endregion

    #region Order Course Management

    /// <summary>
    /// Initializes courses for a new KDS order.
    /// </summary>
    Task<List<KdsCourseStateDto>> InitializeOrderCoursesAsync(int kdsOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all course states for an order.
    /// </summary>
    Task<List<KdsCourseStateDto>> GetOrderCoursesAsync(int kdsOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific course state.
    /// </summary>
    Task<KdsCourseStateDto?> GetCourseStateAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the course status summary for an order.
    /// </summary>
    Task<CourseStatusSummary> GetCourseStatusSummaryAsync(int kdsOrderId, CancellationToken cancellationToken = default);

    #endregion

    #region Course Firing

    /// <summary>
    /// Fires a specific course.
    /// </summary>
    Task<FireCourseResult> FireCourseAsync(int kdsOrderId, int courseNumber, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fires the next pending course in sequence.
    /// </summary>
    Task<FireCourseResult> FireNextCourseAsync(int kdsOrderId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fires all remaining courses immediately (rush order).
    /// </summary>
    Task<FireCourseResult> FireAllCoursesAsync(int kdsOrderId, int userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a course can be fired.
    /// </summary>
    Task<bool> CanFireCourseAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default);

    #endregion

    #region Auto-Fire Processing

    /// <summary>
    /// Called when a course is bumped to trigger auto-fire if configured.
    /// </summary>
    Task ProcessCourseBumpAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes scheduled course fires (background job).
    /// </summary>
    Task<int> ProcessScheduledFiresAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Hold Management

    /// <summary>
    /// Puts a course on hold.
    /// </summary>
    Task<HoldCourseResult> HoldCourseAsync(int kdsOrderId, int courseNumber, string reason, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a course hold.
    /// </summary>
    Task<HoldCourseResult> ReleaseCourseHoldAsync(int kdsOrderId, int courseNumber, int userId, bool fireImmediately = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all held courses for a store.
    /// </summary>
    Task<List<HeldCourse>> GetHeldCoursesAsync(int storeId, CancellationToken cancellationToken = default);

    #endregion

    #region Course Timing

    /// <summary>
    /// Gets course timing information for an order.
    /// </summary>
    Task<CourseTiming> GetCourseTimingAsync(int kdsOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the delay for a specific course.
    /// </summary>
    Task SetCourseDelayAsync(int kdsOrderId, int courseNumber, int delayMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the course schedule for an order.
    /// </summary>
    Task<CourseSchedule> CalculateCourseScheduleAsync(int kdsOrderId, CancellationToken cancellationToken = default);

    #endregion

    #region Course Completion

    /// <summary>
    /// Marks a course as ready (all items complete).
    /// </summary>
    Task<CourseCompletionResult> MarkCourseReadyAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a course as served.
    /// </summary>
    Task<CourseCompletionResult> MarkCourseServedAsync(int kdsOrderId, int courseNumber, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates course progress when an item is completed.
    /// </summary>
    Task UpdateCourseProgressAsync(int kdsOrderId, int courseNumber, CancellationToken cancellationToken = default);

    #endregion

    #region Queries

    /// <summary>
    /// Gets pending courses for a store.
    /// </summary>
    Task<List<KdsCourseStateDto>> GetPendingCoursesAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fired courses for a store.
    /// </summary>
    Task<List<KdsCourseStateDto>> GetFiredCoursesAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ready courses for a store.
    /// </summary>
    Task<List<KdsCourseStateDto>> GetReadyCoursesAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets courses ready to fire based on schedule.
    /// </summary>
    Task<List<KdsCourseStateDto>> GetCoursesReadyToFireAsync(int storeId, CancellationToken cancellationToken = default);

    #endregion
}
