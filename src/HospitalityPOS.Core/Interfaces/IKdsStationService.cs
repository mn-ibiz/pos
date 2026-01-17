using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for KDS station management.
/// </summary>
public interface IKdsStationService
{
    #region Station CRUD Operations

    /// <summary>
    /// Creates a new KDS station.
    /// </summary>
    /// <param name="dto">Station creation data.</param>
    /// <returns>The created station.</returns>
    Task<KdsStationDto> CreateStationAsync(CreateKdsStationDto dto);

    /// <summary>
    /// Gets a station by ID.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>The station or null if not found.</returns>
    Task<KdsStationDto?> GetStationAsync(int id);

    /// <summary>
    /// Gets a station by device identifier.
    /// </summary>
    /// <param name="deviceIdentifier">The device identifier (IP/hostname).</param>
    /// <returns>The station or null if not found.</returns>
    Task<KdsStationDto?> GetStationByDeviceAsync(string deviceIdentifier);

    /// <summary>
    /// Gets all stations based on query parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of stations.</returns>
    Task<List<KdsStationListDto>> GetStationsAsync(KdsStationQueryDto query);

    /// <summary>
    /// Gets all active stations for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of stations.</returns>
    Task<List<KdsStationListDto>> GetStoreStationsAsync(int storeId);

    /// <summary>
    /// Updates a station.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <param name="dto">Update data.</param>
    /// <returns>The updated station.</returns>
    Task<KdsStationDto> UpdateStationAsync(int id, UpdateKdsStationDto dto);

    /// <summary>
    /// Deletes a station (soft delete).
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteStationAsync(int id);

    #endregion

    #region Station Status Management

    /// <summary>
    /// Sets a station as online.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>The updated station.</returns>
    Task<KdsStationDto> SetStationOnlineAsync(int id);

    /// <summary>
    /// Sets a station as offline.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>The updated station.</returns>
    Task<KdsStationDto> SetStationOfflineAsync(int id);

    /// <summary>
    /// Sets a station as paused.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>The updated station.</returns>
    Task<KdsStationDto> SetStationPausedAsync(int id);

    /// <summary>
    /// Updates the last connected timestamp for a station.
    /// </summary>
    /// <param name="id">The station ID.</param>
    /// <returns>The updated station.</returns>
    Task<KdsStationDto> UpdateLastConnectedAsync(int id);

    #endregion

    #region Category Management

    /// <summary>
    /// Assigns a category to a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="displayOrder">Optional display order.</param>
    /// <returns>The station category mapping.</returns>
    Task<KdsStationCategoryDto> AssignCategoryAsync(int stationId, int categoryId, int? displayOrder = null);

    /// <summary>
    /// Removes a category from a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>True if removed.</returns>
    Task<bool> RemoveCategoryAsync(int stationId, int categoryId);

    /// <summary>
    /// Gets categories assigned to a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>List of category mappings.</returns>
    Task<List<KdsStationCategoryDto>> GetStationCategoriesAsync(int stationId);

    /// <summary>
    /// Updates the display order of categories on a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="categoryOrders">Dictionary of categoryId to displayOrder.</param>
    /// <returns>Updated category mappings.</returns>
    Task<List<KdsStationCategoryDto>> UpdateCategoryOrdersAsync(int stationId, Dictionary<int, int> categoryOrders);

    /// <summary>
    /// Gets stations that handle a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>List of stations.</returns>
    Task<List<KdsStationListDto>> GetStationsForCategoryAsync(int categoryId, int? storeId = null);

    #endregion

    #region Display Settings

    /// <summary>
    /// Gets the display settings for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <returns>Display settings or defaults if not configured.</returns>
    Task<KdsDisplaySettingsDto> GetDisplaySettingsAsync(int stationId);

    /// <summary>
    /// Updates the display settings for a station.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="dto">Update data.</param>
    /// <returns>Updated display settings.</returns>
    Task<KdsDisplaySettingsDto> UpdateDisplaySettingsAsync(int stationId, UpdateKdsDisplaySettingsDto dto);

    /// <summary>
    /// Creates display settings.
    /// </summary>
    /// <param name="dto">Display settings data.</param>
    /// <returns>Created display settings.</returns>
    Task<KdsDisplaySettingsDto> CreateDisplaySettingsAsync(CreateKdsDisplaySettingsDto dto);

    #endregion

    #region Validation

    /// <summary>
    /// Validates a device identifier is unique.
    /// </summary>
    /// <param name="deviceIdentifier">The device identifier.</param>
    /// <param name="excludeStationId">Optional station ID to exclude (for updates).</param>
    /// <returns>True if unique.</returns>
    Task<bool> IsDeviceIdentifierUniqueAsync(string deviceIdentifier, int? excludeStationId = null);

    /// <summary>
    /// Validates a station name is unique within a store.
    /// </summary>
    /// <param name="name">The station name.</param>
    /// <param name="storeId">The store ID.</param>
    /// <param name="excludeStationId">Optional station ID to exclude (for updates).</param>
    /// <returns>True if unique.</returns>
    Task<bool> IsStationNameUniqueAsync(string name, int storeId, int? excludeStationId = null);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets station statistics.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Station statistics.</returns>
    Task<KdsStationStatsDto> GetStationStatsAsync(int stationId, DateTime fromDate, DateTime toDate);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a station comes online.
    /// </summary>
    event EventHandler<KdsStationDto>? StationOnline;

    /// <summary>
    /// Event raised when a station goes offline.
    /// </summary>
    event EventHandler<KdsStationDto>? StationOffline;

    /// <summary>
    /// Event raised when station categories are updated.
    /// </summary>
    event EventHandler<KdsStationDto>? StationCategoriesUpdated;

    #endregion
}
