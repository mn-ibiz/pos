// src/HospitalityPOS.Core/Interfaces/ICommissionService.cs
// Service interface for sales commission calculation and tracking
// Story 45-3: Commission Calculation

using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for sales commission calculation and tracking.
/// Handles commission rules, calculations, payouts, and reporting.
/// </summary>
public interface ICommissionService
{
    #region Commission Rules

    /// <summary>
    /// Creates a new commission rule.
    /// </summary>
    /// <param name="request">Rule creation request.</param>
    /// <returns>Created rule.</returns>
    Task<CommissionRule> CreateRuleAsync(CommissionRuleRequest request);

    /// <summary>
    /// Updates an existing commission rule.
    /// </summary>
    /// <param name="request">Rule update request.</param>
    /// <returns>Updated rule.</returns>
    Task<CommissionRule> UpdateRuleAsync(CommissionRuleRequest request);

    /// <summary>
    /// Deactivates a commission rule.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <returns>True if deactivated.</returns>
    Task<bool> DeactivateRuleAsync(int ruleId);

    /// <summary>
    /// Gets a commission rule by ID.
    /// </summary>
    /// <param name="ruleId">Rule ID.</param>
    /// <returns>Rule or null.</returns>
    Task<CommissionRule?> GetRuleAsync(int ruleId);

    /// <summary>
    /// Gets all active commission rules.
    /// </summary>
    /// <returns>List of active rules.</returns>
    Task<IReadOnlyList<CommissionRule>> GetActiveRulesAsync();

    /// <summary>
    /// Gets commission rules by type.
    /// </summary>
    /// <param name="ruleType">Type of rules to retrieve.</param>
    /// <returns>List of rules.</returns>
    Task<IReadOnlyList<CommissionRule>> GetRulesByTypeAsync(CommissionRuleType ruleType);

    /// <summary>
    /// Gets the applicable commission rule for a sale.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="productId">Product ID.</param>
    /// <param name="categoryId">Category ID.</param>
    /// <returns>Best matching rule or null.</returns>
    Task<CommissionRule?> GetApplicableRuleAsync(int employeeId, int? productId, int? categoryId);

    #endregion

    #region Commission Calculation

    /// <summary>
    /// Calculates commission for a sale.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Calculation result.</returns>
    Task<CommissionCalculationResult> CalculateCommissionAsync(int receiptId, int employeeId);

    /// <summary>
    /// Calculates commission for a specific amount (preview).
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="saleAmount">Sale amount.</param>
    /// <param name="productId">Optional product ID.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <returns>Estimated commission.</returns>
    Task<decimal> EstimateCommissionAsync(int employeeId, decimal saleAmount, int? productId = null, int? categoryId = null);

    /// <summary>
    /// Gets the commission rate for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="productId">Optional product ID.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <returns>Commission rate percentage.</returns>
    Task<decimal> GetCommissionRateAsync(int employeeId, int? productId = null, int? categoryId = null);

    /// <summary>
    /// Checks if employee qualifies for tier bonus.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="periodStart">Period start date.</param>
    /// <param name="periodEnd">Period end date.</param>
    /// <returns>Tier bonus information.</returns>
    Task<(bool Qualified, decimal BonusRate, decimal CurrentTotal, decimal Threshold)> CheckTierBonusAsync(
        int employeeId, DateOnly periodStart, DateOnly periodEnd);

    #endregion

    #region Commission Tracking

    /// <summary>
    /// Records commission for a completed sale.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Created transaction.</returns>
    Task<CommissionTransaction> RecordCommissionAsync(int receiptId, int employeeId);

    /// <summary>
    /// Reverses commission for a returned/voided sale.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="reason">Reversal reason.</param>
    /// <returns>Reversal transaction.</returns>
    Task<CommissionTransaction> ReverseCommissionAsync(int receiptId, string reason);

    /// <summary>
    /// Creates a manual commission adjustment.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="amount">Adjustment amount (positive or negative).</param>
    /// <param name="reason">Adjustment reason.</param>
    /// <param name="adjustedByUserId">User making adjustment.</param>
    /// <returns>Adjustment transaction.</returns>
    Task<CommissionTransaction> CreateAdjustmentAsync(int employeeId, decimal amount, string reason, int adjustedByUserId);

    /// <summary>
    /// Gets commission transactions for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of transactions.</returns>
    Task<IReadOnlyList<CommissionTransaction>> GetEmployeeTransactionsAsync(
        int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets commission transaction by receipt ID.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <returns>Transaction or null.</returns>
    Task<CommissionTransaction?> GetTransactionByReceiptAsync(int receiptId);

    #endregion

    #region Sales Attribution

    /// <summary>
    /// Gets sales attribution for a receipt.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <returns>Sales attribution.</returns>
    Task<SalesAttribution> GetAttributionAsync(int receiptId);

    /// <summary>
    /// Sets or updates sales attribution.
    /// </summary>
    /// <param name="request">Attribution request.</param>
    /// <returns>Updated attribution.</returns>
    Task<SalesAttribution> SetAttributionAsync(AttributionRequest request);

    /// <summary>
    /// Splits commission between employees.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="attributions">Employee attributions with split percentages.</param>
    /// <param name="overriddenByUserId">Manager overriding.</param>
    /// <param name="reason">Reason for split.</param>
    /// <returns>Updated attribution.</returns>
    Task<SalesAttribution> SplitCommissionAsync(
        int receiptId, List<EmployeeAttribution> attributions, int overriddenByUserId, string? reason = null);

    #endregion

    #region Reports

    /// <summary>
    /// Gets commission summary for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Commission summary.</returns>
    Task<EmployeeCommissionSummary> GetEmployeeSummaryAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Generates commission report for multiple employees.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="employeeIds">Optional filter by employees.</param>
    /// <returns>Commission report.</returns>
    Task<CommissionReport> GenerateReportAsync(DateOnly startDate, DateOnly endDate, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Gets top commission earners.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="top">Number of top earners to return.</param>
    /// <returns>List of top earners.</returns>
    Task<IReadOnlyList<EmployeeCommissionSummary>> GetTopEarnersAsync(DateOnly startDate, DateOnly endDate, int top = 10);

    #endregion

    #region Payouts

    /// <summary>
    /// Creates a payout request.
    /// </summary>
    /// <param name="request">Payout request.</param>
    /// <returns>Created payout.</returns>
    Task<PayoutResult> CreatePayoutAsync(PayoutRequest request);

    /// <summary>
    /// Approves a payout.
    /// </summary>
    /// <param name="payoutId">Payout ID.</param>
    /// <param name="approvedByUserId">Approving user ID.</param>
    /// <returns>Approved payout.</returns>
    Task<PayoutResult> ApprovePayoutAsync(int payoutId, int approvedByUserId);

    /// <summary>
    /// Marks a payout as paid.
    /// </summary>
    /// <param name="payoutId">Payout ID.</param>
    /// <param name="paymentReference">Payment reference.</param>
    /// <returns>Updated payout.</returns>
    Task<PayoutResult> MarkPayoutPaidAsync(int payoutId, string? paymentReference = null);

    /// <summary>
    /// Gets pending payouts.
    /// </summary>
    /// <returns>List of pending payouts.</returns>
    Task<IReadOnlyList<CommissionPayout>> GetPendingPayoutsAsync();

    /// <summary>
    /// Gets payout history for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>List of payouts.</returns>
    Task<IReadOnlyList<CommissionPayout>> GetEmployeePayoutsAsync(int employeeId);

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    /// <param name="payoutId">Payout ID.</param>
    /// <returns>Payout or null.</returns>
    Task<CommissionPayout?> GetPayoutAsync(int payoutId);

    #endregion

    #region Payroll Integration

    /// <summary>
    /// Exports commission data for payroll.
    /// </summary>
    /// <param name="periodStart">Period start date.</param>
    /// <param name="periodEnd">Period end date.</param>
    /// <param name="employeeIds">Optional filter by employees.</param>
    /// <returns>Payroll export data.</returns>
    Task<CommissionPayrollExport> ExportForPayrollAsync(
        DateOnly periodStart, DateOnly periodEnd, IEnumerable<int>? employeeIds = null);

    /// <summary>
    /// Gets unpaid commission for an employee.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>Unpaid commission amount.</returns>
    Task<decimal> GetUnpaidCommissionAsync(int employeeId);

    #endregion

    #region Settings

    /// <summary>
    /// Gets commission settings.
    /// </summary>
    /// <returns>Current settings.</returns>
    Task<CommissionSettings> GetSettingsAsync();

    /// <summary>
    /// Updates commission settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <returns>Updated settings.</returns>
    Task<CommissionSettings> UpdateSettingsAsync(CommissionSettings settings);

    #endregion

    #region Events

    /// <summary>Raised when commission is earned.</summary>
    event EventHandler<CommissionEventArgs>? CommissionEarned;

    /// <summary>Raised when commission is reversed.</summary>
    event EventHandler<CommissionEventArgs>? CommissionReversed;

    /// <summary>Raised when payout is processed.</summary>
    event EventHandler<PayoutEventArgs>? PayoutProcessed;

    #endregion
}
