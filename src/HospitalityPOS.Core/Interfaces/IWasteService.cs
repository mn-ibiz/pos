// src/HospitalityPOS.Core/Interfaces/IWasteService.cs
// Service interface for waste and shrinkage tracking
// Story 46-1: Waste and Shrinkage Tracking

using HospitalityPOS.Core.Models.Inventory;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for waste and shrinkage tracking.
/// Handles waste recording, shrinkage calculation, variance analysis, and loss prevention alerts.
/// </summary>
public interface IWasteService
{
    #region Waste Reasons

    /// <summary>
    /// Creates a new waste reason.
    /// </summary>
    /// <param name="request">Waste reason request.</param>
    /// <returns>Created waste reason.</returns>
    Task<WasteReason> CreateWasteReasonAsync(WasteReasonRequest request);

    /// <summary>
    /// Updates an existing waste reason.
    /// </summary>
    /// <param name="request">Waste reason request.</param>
    /// <returns>Updated waste reason.</returns>
    Task<WasteReason> UpdateWasteReasonAsync(WasteReasonRequest request);

    /// <summary>
    /// Deactivates a waste reason.
    /// </summary>
    /// <param name="reasonId">Reason ID.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateWasteReasonAsync(int reasonId);

    /// <summary>
    /// Gets a waste reason by ID.
    /// </summary>
    /// <param name="reasonId">Reason ID.</param>
    /// <returns>Waste reason or null.</returns>
    Task<WasteReason?> GetWasteReasonAsync(int reasonId);

    /// <summary>
    /// Gets all active waste reasons.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <returns>List of waste reasons.</returns>
    Task<IReadOnlyList<WasteReason>> GetActiveWasteReasonsAsync(WasteReasonCategory? category = null);

    /// <summary>
    /// Gets waste reasons with usage statistics.
    /// </summary>
    /// <param name="startDate">Start date for stats.</param>
    /// <param name="endDate">End date for stats.</param>
    /// <returns>List of reasons with stats.</returns>
    Task<IReadOnlyList<WasteReason>> GetWasteReasonsWithStatsAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Waste Recording

    /// <summary>
    /// Records a waste event.
    /// </summary>
    /// <param name="request">Waste record request.</param>
    /// <returns>Result of recording.</returns>
    Task<WasteResult> RecordWasteAsync(WasteRecordRequest request);

    /// <summary>
    /// Records multiple waste events.
    /// </summary>
    /// <param name="requests">List of waste record requests.</param>
    /// <returns>List of results.</returns>
    Task<IReadOnlyList<WasteResult>> RecordBatchWasteAsync(IEnumerable<WasteRecordRequest> requests);

    /// <summary>
    /// Processes approval for a waste record.
    /// </summary>
    /// <param name="request">Approval request.</param>
    /// <returns>Result of approval.</returns>
    Task<WasteResult> ProcessApprovalAsync(WasteApprovalRequest request);

    /// <summary>
    /// Reverses a waste record.
    /// </summary>
    /// <param name="recordId">Record ID.</param>
    /// <param name="userId">User performing reversal.</param>
    /// <param name="reason">Reversal reason.</param>
    /// <returns>Result of reversal.</returns>
    Task<WasteResult> ReverseWasteAsync(int recordId, int userId, string reason);

    /// <summary>
    /// Gets a waste record by ID.
    /// </summary>
    /// <param name="recordId">Record ID.</param>
    /// <returns>Waste record or null.</returns>
    Task<WasteRecord?> GetWasteRecordAsync(int recordId);

    /// <summary>
    /// Gets waste records pending approval.
    /// </summary>
    /// <returns>List of pending records.</returns>
    Task<IReadOnlyList<WasteRecord>> GetPendingApprovalsAsync();

    /// <summary>
    /// Gets waste records for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="categoryId">Optional product category filter.</param>
    /// <param name="reasonCategory">Optional reason category filter.</param>
    /// <returns>List of waste records.</returns>
    Task<IReadOnlyList<WasteRecord>> GetWasteRecordsAsync(
        DateOnly startDate,
        DateOnly endDate,
        int? categoryId = null,
        WasteReasonCategory? reasonCategory = null);

    /// <summary>
    /// Gets waste records for a product.
    /// </summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="startDate">Optional start date.</param>
    /// <param name="endDate">Optional end date.</param>
    /// <returns>List of waste records.</returns>
    Task<IReadOnlyList<WasteRecord>> GetProductWasteRecordsAsync(
        int productId, DateOnly? startDate = null, DateOnly? endDate = null);

    #endregion

    #region Shrinkage Calculation

    /// <summary>
    /// Calculates shrinkage metrics for a period.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>Shrinkage metrics.</returns>
    Task<ShrinkageMetrics> CalculateShrinkageAsync(DateOnly startDate, DateOnly endDate, int? categoryId = null);

    /// <summary>
    /// Creates a shrinkage snapshot.
    /// </summary>
    /// <param name="date">Snapshot date.</param>
    /// <param name="productId">Optional product filter.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>Created snapshots.</returns>
    Task<IReadOnlyList<ShrinkageSnapshot>> CreateShrinkageSnapshotAsync(
        DateOnly date, int? productId = null, int? categoryId = null);

    /// <summary>
    /// Gets shrinkage snapshots for a period.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of snapshots.</returns>
    Task<IReadOnlyList<ShrinkageSnapshot>> GetShrinkageSnapshotsAsync(DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets shrinkage trend data.
    /// </summary>
    /// <param name="months">Number of months.</param>
    /// <returns>List of monthly shrinkage summaries.</returns>
    Task<IReadOnlyList<MonthlyShrinkage>> GetShrinkageTrendAsync(int months = 12);

    #endregion

    #region Stock Variance

    /// <summary>
    /// Records stock variance from stock take.
    /// </summary>
    /// <param name="stockTakeId">Stock take ID.</param>
    /// <param name="productId">Product ID.</param>
    /// <param name="systemQuantity">System quantity.</param>
    /// <param name="countedQuantity">Counted quantity.</param>
    /// <param name="unitCost">Unit cost.</param>
    /// <returns>Variance record.</returns>
    Task<StockVarianceRecord> RecordVarianceAsync(
        int stockTakeId,
        int productId,
        decimal systemQuantity,
        decimal countedQuantity,
        decimal unitCost);

    /// <summary>
    /// Gets variance records for a stock take.
    /// </summary>
    /// <param name="stockTakeId">Stock take ID.</param>
    /// <returns>List of variance records.</returns>
    Task<IReadOnlyList<StockVarianceRecord>> GetStockTakeVariancesAsync(int stockTakeId);

    /// <summary>
    /// Gets significant variances.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="minVariancePercent">Minimum variance percentage.</param>
    /// <returns>List of significant variances.</returns>
    Task<IReadOnlyList<StockVarianceRecord>> GetSignificantVariancesAsync(
        DateOnly startDate, DateOnly endDate, decimal minVariancePercent = 5m);

    /// <summary>
    /// Updates variance investigation status.
    /// </summary>
    /// <param name="varianceId">Variance ID.</param>
    /// <param name="status">New status.</param>
    /// <param name="notes">Investigation notes.</param>
    /// <returns>Updated variance record.</returns>
    Task<StockVarianceRecord> UpdateVarianceInvestigationAsync(
        int varianceId, VarianceInvestigationStatus status, string? notes);

    /// <summary>
    /// Creates waste record from variance.
    /// </summary>
    /// <param name="varianceId">Variance ID.</param>
    /// <param name="wasteReasonId">Waste reason ID.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="notes">Notes.</param>
    /// <returns>Created waste record.</returns>
    Task<WasteResult> CreateWasteFromVarianceAsync(int varianceId, int wasteReasonId, int userId, string? notes);

    #endregion

    #region Reports

    /// <summary>
    /// Generates waste report.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>Waste report.</returns>
    Task<WasteReport> GenerateWasteReportAsync(DateOnly startDate, DateOnly endDate, int? categoryId = null);

    /// <summary>
    /// Generates shrinkage report.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Shrinkage report.</returns>
    Task<ShrinkageReport> GenerateShrinkageReportAsync(DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets top shrinkage products.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="limit">Number of products to return.</param>
    /// <returns>List of top shrinkage products.</returns>
    Task<IReadOnlyList<ShrinkageByProduct>> GetTopShrinkageProductsAsync(
        DateOnly startDate, DateOnly endDate, int limit = 10);

    /// <summary>
    /// Gets waste summary by reason.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of waste by reason.</returns>
    Task<IReadOnlyList<WasteByReason>> GetWasteByReasonAsync(DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets waste summary by category.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of waste by category.</returns>
    Task<IReadOnlyList<WasteByCategory>> GetWasteByCategoryAsync(DateOnly startDate, DateOnly endDate);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets shrinkage dashboard data.
    /// </summary>
    /// <returns>Dashboard data.</returns>
    Task<ShrinkageDashboard> GetDashboardAsync();

    /// <summary>
    /// Gets daily waste totals for charting.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of daily totals.</returns>
    Task<IReadOnlyList<DailyWaste>> GetDailyWasteTotalsAsync(DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets shrinkage trend data for charting.
    /// </summary>
    /// <param name="weeks">Number of weeks.</param>
    /// <returns>List of trend points.</returns>
    Task<IReadOnlyList<ShrinkageTrendPoint>> GetShrinkageTrendDataAsync(int weeks = 12);

    #endregion

    #region Alerts

    /// <summary>
    /// Creates a loss prevention alert.
    /// </summary>
    /// <param name="alertType">Alert type.</param>
    /// <param name="title">Alert title.</param>
    /// <param name="message">Alert message.</param>
    /// <param name="severity">Severity level.</param>
    /// <param name="productId">Optional product ID.</param>
    /// <param name="wasteRecordId">Optional waste record ID.</param>
    /// <param name="userId">Optional user ID.</param>
    /// <param name="value">Optional value.</param>
    /// <param name="threshold">Optional threshold.</param>
    /// <returns>Created alert.</returns>
    Task<LossPreventionAlert> CreateAlertAsync(
        LossPreventionAlertType alertType,
        string title,
        string message,
        AlertSeverity severity = AlertSeverity.Warning,
        int? productId = null,
        int? wasteRecordId = null,
        int? userId = null,
        decimal? value = null,
        decimal? threshold = null);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <param name="includeAcknowledged">Include acknowledged alerts.</param>
    /// <returns>List of alerts.</returns>
    Task<IReadOnlyList<LossPreventionAlert>> GetActiveAlertsAsync(bool includeAcknowledged = false);

    /// <summary>
    /// Gets alerts for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="alertType">Optional alert type filter.</param>
    /// <returns>List of alerts.</returns>
    Task<IReadOnlyList<LossPreventionAlert>> GetAlertsAsync(
        DateOnly startDate, DateOnly endDate, LossPreventionAlertType? alertType = null);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    /// <param name="alertId">Alert ID.</param>
    /// <param name="userId">User acknowledging.</param>
    /// <returns>True if acknowledged.</returns>
    Task<bool> AcknowledgeAlertAsync(int alertId, int userId);

    /// <summary>
    /// Checks for unusual void patterns.
    /// </summary>
    /// <param name="date">Date to check.</param>
    /// <returns>List of users with unusual patterns.</returns>
    Task<IReadOnlyList<(int UserId, string UserName, int VoidCount)>> CheckUnusualVoidPatternsAsync(DateOnly date);

    /// <summary>
    /// Runs alert checks and creates alerts as needed.
    /// </summary>
    /// <returns>Number of alerts created.</returns>
    Task<int> RunAlertChecksAsync();

    #endregion

    #region Alert Rules

    /// <summary>
    /// Gets all alert rules.
    /// </summary>
    /// <returns>List of alert rules.</returns>
    Task<IReadOnlyList<AlertRuleConfig>> GetAlertRulesAsync();

    /// <summary>
    /// Updates an alert rule.
    /// </summary>
    /// <param name="rule">Alert rule configuration.</param>
    /// <returns>Updated rule.</returns>
    Task<AlertRuleConfig> UpdateAlertRuleAsync(AlertRuleConfig rule);

    /// <summary>
    /// Enables or disables an alert rule.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <param name="enabled">Enabled state.</param>
    /// <returns>True if updated.</returns>
    Task<bool> SetAlertRuleEnabledAsync(int ruleId, bool enabled);

    #endregion

    #region Settings

    /// <summary>
    /// Gets waste tracking settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<WasteTrackingSettings> GetSettingsAsync();

    /// <summary>
    /// Updates waste tracking settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<WasteTrackingSettings> UpdateSettingsAsync(WasteTrackingSettings settings);

    #endregion

    #region Events

    /// <summary>Raised when waste is recorded.</summary>
    event EventHandler<WasteEventArgs>? WasteRecorded;

    /// <summary>Raised when waste is approved.</summary>
    event EventHandler<WasteEventArgs>? WasteApproved;

    /// <summary>Raised when waste is rejected.</summary>
    event EventHandler<WasteEventArgs>? WasteRejected;

    /// <summary>Raised when waste is reversed.</summary>
    event EventHandler<WasteEventArgs>? WasteReversed;

    /// <summary>Raised when an alert is created.</summary>
    event EventHandler<AlertEventArgs>? AlertCreated;

    #endregion
}
