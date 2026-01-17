using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing work periods (shifts) in the POS system.
/// </summary>
public interface IWorkPeriodService
{
    /// <summary>
    /// Gets the currently open work period, if any.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current open work period, or null if none is open.</returns>
    Task<WorkPeriod?> GetCurrentWorkPeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there is an active (open) work period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a work period is open; otherwise, false.</returns>
    Task<bool> IsWorkPeriodOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a new work period with the specified opening float.
    /// </summary>
    /// <param name="openingFloat">The opening cash float amount.</param>
    /// <param name="userId">The ID of the user opening the work period.</param>
    /// <param name="notes">Optional notes for the work period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created work period.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a work period is already open.</exception>
    Task<WorkPeriod> OpenWorkPeriodAsync(decimal openingFloat, int userId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the current work period.
    /// </summary>
    /// <param name="closingCash">The actual closing cash amount counted.</param>
    /// <param name="userId">The ID of the user closing the work period.</param>
    /// <param name="notes">Optional closing notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The closed work period with calculated variance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no work period is currently open.</exception>
    Task<WorkPeriod> CloseWorkPeriodAsync(decimal closingCash, int userId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last closed work period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recently closed work period, or null if none exists.</returns>
    Task<WorkPeriod?> GetLastClosedWorkPeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work period history for a date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of work periods in the date range.</returns>
    Task<IReadOnlyList<WorkPeriod>> GetWorkPeriodHistoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the expected cash for the current work period.
    /// This includes opening float plus all cash payments minus cash refunds.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The expected cash amount.</returns>
    Task<decimal> CalculateExpectedCashAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work period by ID.
    /// </summary>
    /// <param name="id">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work period if found; otherwise, null.</returns>
    Task<WorkPeriod?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an X-Report for the specified work period.
    /// X-Reports show running totals without resetting counters.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="generatedByUserId">The ID of the user generating the report.</param>
    /// <param name="generatedByUserName">The name of the user generating the report.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated X-Report.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the work period is not found.</exception>
    Task<XReport> GenerateXReportAsync(int workPeriodId, int generatedByUserId, string generatedByUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Z-Report for a closed work period.
    /// Z-Reports are final end-of-day reports generated at work period close.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated Z-Report.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the work period is not found or not closed.</exception>
    Task<ZReport> GenerateZReportAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unsettled receipts for the specified work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of unsettled receipts.</returns>
    Task<IReadOnlyList<Receipt>> GetUnsettledReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unsettled receipts for the specified work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of unsettled receipts.</returns>
    Task<int> GetUnsettledReceiptsCountAsync(int workPeriodId, CancellationToken cancellationToken = default);
}
