using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Result of a PO consolidation operation.
/// </summary>
public class ConsolidationResult
{
    /// <summary>
    /// Whether the consolidation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of suggestions processed.
    /// </summary>
    public int SuggestionsProcessed { get; set; }

    /// <summary>
    /// Number of purchase orders created.
    /// </summary>
    public int PurchaseOrdersCreated { get; set; }

    /// <summary>
    /// Total value of all created POs.
    /// </summary>
    public decimal TotalOrderValue { get; set; }

    /// <summary>
    /// IDs of created purchase orders.
    /// </summary>
    public List<int> PurchaseOrderIds { get; set; } = new();

    /// <summary>
    /// Number of suggestions skipped (no supplier, below minimum, etc.).
    /// </summary>
    public int SuggestionsSkipped { get; set; }

    /// <summary>
    /// Warning messages.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Grouped suggestions by supplier.
/// </summary>
public class SupplierSuggestionGroup
{
    /// <summary>
    /// Supplier ID.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Supplier name.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Suggestions for this supplier.
    /// </summary>
    public List<ReorderSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Total estimated cost for this supplier.
    /// </summary>
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>
    /// Number of items.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Number of POs that will be created (based on MaxItemsPerPO).
    /// </summary>
    public int ProjectedPOCount { get; set; }
}

/// <summary>
/// Service for consolidating purchase orders by supplier.
/// </summary>
public interface IPurchaseOrderConsolidationService
{
    /// <summary>
    /// Creates consolidated purchase orders from approved suggestions.
    /// </summary>
    /// <param name="storeId">Store ID.</param>
    /// <param name="suggestionIds">Specific suggestion IDs (null for all approved).</param>
    /// <param name="sendImmediately">Whether to send POs immediately or create as draft.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ConsolidationResult> CreateConsolidatedPurchaseOrdersAsync(
        int storeId,
        IEnumerable<int>? suggestionIds = null,
        bool sendImmediately = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Groups suggestions by supplier for preview.
    /// </summary>
    Task<IEnumerable<SupplierSuggestionGroup>> GroupSuggestionsBySupplierAsync(
        int storeId,
        IEnumerable<int>? suggestionIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges multiple existing POs for the same supplier into one.
    /// </summary>
    /// <param name="purchaseOrderIds">IDs of POs to merge.</param>
    /// <param name="userId">User performing the merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PurchaseOrder> MergePurchaseOrdersAsync(
        IEnumerable<int> purchaseOrderIds,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the projected number of POs that would be created from pending suggestions.
    /// </summary>
    Task<int> GetProjectedPOCountAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that suggestions can be consolidated.
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateConsolidationAsync(
        int storeId,
        IEnumerable<int> suggestionIds,
        CancellationToken cancellationToken = default);
}
