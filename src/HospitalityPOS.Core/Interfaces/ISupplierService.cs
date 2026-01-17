using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing suppliers.
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Gets all suppliers.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive suppliers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of suppliers.</returns>
    Task<IReadOnlyList<Supplier>> GetAllSuppliersAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a supplier by ID.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The supplier if found; otherwise, null.</returns>
    Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a supplier by code.
    /// </summary>
    /// <param name="code">The supplier code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The supplier if found; otherwise, null.</returns>
    Task<Supplier?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches suppliers by name or code.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="includeInactive">Whether to include inactive suppliers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching suppliers.</returns>
    Task<IReadOnlyList<Supplier>> SearchSuppliersAsync(string searchTerm, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="supplier">The supplier to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created supplier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a supplier with the same code already exists.</exception>
    Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    /// <param name="supplier">The supplier to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated supplier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a supplier with the same code already exists.</exception>
    Task<Supplier> UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a supplier.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the supplier was activated; otherwise, false.</returns>
    Task<bool> ActivateSupplierAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a supplier.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the supplier was deactivated; otherwise, false.</returns>
    Task<bool> DeactivateSupplierAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a supplier code is unique.
    /// </summary>
    /// <param name="code">The code to check.</param>
    /// <param name="excludeSupplierId">Optional supplier ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is unique; otherwise, false.</returns>
    Task<bool> IsCodeUniqueAsync(string code, int? excludeSupplierId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next supplier code.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next available supplier code.</returns>
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of suppliers.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive suppliers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count of suppliers.</returns>
    Task<int> GetSupplierCountAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suppliers with outstanding balances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of suppliers with outstanding balances.</returns>
    Task<IReadOnlyList<Supplier>> GetSuppliersWithBalanceAsync(CancellationToken cancellationToken = default);
}
