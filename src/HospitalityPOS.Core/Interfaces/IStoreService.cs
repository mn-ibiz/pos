using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for store-related operations.
/// </summary>
public interface IStoreService
{
    /// <summary>
    /// Gets the current store.
    /// </summary>
    Task<Store?> GetCurrentStoreAsync();

    /// <summary>
    /// Gets a store by its ID.
    /// </summary>
    Task<Store?> GetStoreByIdAsync(int storeId);

    /// <summary>
    /// Gets all active stores.
    /// </summary>
    Task<IEnumerable<Store>> GetAllStoresAsync();
}
