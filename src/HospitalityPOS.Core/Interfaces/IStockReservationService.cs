using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing stock reservations.
/// </summary>
public interface IStockReservationService
{
    #region Reservation Management

    /// <summary>
    /// Creates a single stock reservation.
    /// </summary>
    Task<StockReservationDto> CreateReservationAsync(CreateStockReservationDto dto, int userId);

    /// <summary>
    /// Creates multiple reservations for a transfer or order.
    /// </summary>
    Task<List<StockReservationDto>> CreateBatchReservationsAsync(CreateBatchReservationsDto dto, int userId);

    /// <summary>
    /// Gets a reservation by ID.
    /// </summary>
    Task<StockReservationDto?> GetReservationAsync(int reservationId);

    /// <summary>
    /// Gets reservations for a reference (e.g., transfer request).
    /// </summary>
    Task<List<StockReservationDto>> GetReservationsByReferenceAsync(ReservationType referenceType, int referenceId);

    /// <summary>
    /// Gets reservations by query parameters.
    /// </summary>
    Task<List<StockReservationDto>> GetReservationsAsync(ReservationQueryDto query);

    /// <summary>
    /// Gets active reservations for a product at a location.
    /// </summary>
    Task<List<StockReservationDto>> GetActiveReservationsAsync(int locationId, int productId);

    /// <summary>
    /// Gets total reserved quantity for a product at a location.
    /// </summary>
    Task<int> GetReservedQuantityAsync(int locationId, int productId);

    /// <summary>
    /// Gets available quantity (stock on hand - reserved).
    /// </summary>
    Task<int> GetAvailableQuantityAsync(int locationId, int productId);

    #endregion

    #region Reservation Lifecycle

    /// <summary>
    /// Fulfills a reservation (marks as used, typically when transfer ships).
    /// </summary>
    Task<StockReservationDto> FulfillReservationAsync(int reservationId, int userId);

    /// <summary>
    /// Fulfills all reservations for a reference.
    /// </summary>
    Task<List<StockReservationDto>> FulfillByReferenceAsync(ReservationType referenceType, int referenceId, int userId);

    /// <summary>
    /// Releases a reservation (cancels without use).
    /// </summary>
    Task<StockReservationDto> ReleaseReservationAsync(int reservationId, int userId, string? reason = null);

    /// <summary>
    /// Releases all reservations for a reference.
    /// </summary>
    Task<List<StockReservationDto>> ReleaseByReferenceAsync(ReservationType referenceType, int referenceId, int userId, string? reason = null);

    /// <summary>
    /// Updates reservation quantity.
    /// </summary>
    Task<StockReservationDto> UpdateReservationQuantityAsync(int reservationId, int newQuantity, int userId);

    /// <summary>
    /// Extends reservation expiration.
    /// </summary>
    Task<StockReservationDto> ExtendReservationAsync(int reservationId, int additionalHours, int userId);

    #endregion

    #region Expiration Management

    /// <summary>
    /// Gets expired reservations that need to be cleaned up.
    /// </summary>
    Task<List<StockReservationDto>> GetExpiredReservationsAsync();

    /// <summary>
    /// Gets reservations expiring within specified hours.
    /// </summary>
    Task<List<StockReservationDto>> GetExpiringReservationsAsync(int withinHours = 24);

    /// <summary>
    /// Expires all overdue reservations.
    /// </summary>
    Task<int> ExpireOverdueReservationsAsync();

    /// <summary>
    /// Checks if any reservations for a reference have expired.
    /// </summary>
    Task<bool> HasExpiredReservationsAsync(ReservationType referenceType, int referenceId);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if quantity can be reserved at a location.
    /// </summary>
    Task<bool> CanReserveAsync(int locationId, int productId, int quantity);

    /// <summary>
    /// Validates and checks multiple products for reservation.
    /// </summary>
    Task<List<ReservationValidationResult>> ValidateBatchReservationAsync(int locationId, List<ReservationLineDto> lines);

    #endregion

    #region Summaries and Reports

    /// <summary>
    /// Gets reservation summary by location.
    /// </summary>
    Task<LocationReservationSummaryDto> GetLocationSummaryAsync(int locationId);

    /// <summary>
    /// Gets reservation summary for a product.
    /// </summary>
    Task<ProductReservationSummaryDto> GetProductSummaryAsync(int locationId, int productId);

    /// <summary>
    /// Gets all location summaries.
    /// </summary>
    Task<List<LocationReservationSummaryDto>> GetAllLocationSummariesAsync();

    #endregion
}

/// <summary>
/// Result of reservation validation for a single line.
/// </summary>
public class ReservationValidationResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int StockOnHand { get; set; }
    public int CurrentlyReserved { get; set; }
    public bool CanReserve => RequestedQuantity <= AvailableQuantity;
    public string? ValidationMessage { get; set; }
}
