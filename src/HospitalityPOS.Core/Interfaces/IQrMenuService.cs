// src/HospitalityPOS.Core/Interfaces/IQrMenuService.cs
// Interface for QR Menu and Contactless Ordering service
// Story 44-1: QR Menu and Contactless Ordering

using HospitalityPOS.Core.Models.QrMenu;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for QR menu and contactless ordering functionality.
/// Supports QR code generation, menu management, and order processing.
/// </summary>
public interface IQrMenuService
{
    #region Configuration

    /// <summary>
    /// Gets the current QR menu configuration.
    /// </summary>
    /// <returns>Current configuration.</returns>
    Task<QrMenuConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the QR menu configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <returns>Saved configuration.</returns>
    Task<QrMenuConfiguration> SaveConfigurationAsync(QrMenuConfiguration configuration);

    /// <summary>
    /// Gets whether QR menu ordering is enabled.
    /// </summary>
    bool IsEnabled { get; }

    #endregion

    #region QR Code Generation

    /// <summary>
    /// Generates a QR code for a specific table.
    /// </summary>
    /// <param name="tableId">Table identifier.</param>
    /// <param name="tableName">Table name/number.</param>
    /// <param name="location">Optional location/section.</param>
    /// <returns>QR code data including image.</returns>
    Task<TableQrCode> GenerateTableQrCodeAsync(int tableId, string tableName, string? location = null);

    /// <summary>
    /// Generates QR codes for all tables.
    /// </summary>
    /// <param name="tables">Table IDs and names.</param>
    /// <returns>List of QR codes for all tables.</returns>
    Task<IReadOnlyList<TableQrCode>> GenerateAllTableQrCodesAsync(IEnumerable<(int Id, string Name, string? Location)> tables);

    /// <summary>
    /// Gets the QR code for a table if previously generated.
    /// </summary>
    /// <param name="tableId">Table identifier.</param>
    /// <returns>QR code or null if not generated.</returns>
    Task<TableQrCode?> GetTableQrCodeAsync(int tableId);

    /// <summary>
    /// Records a QR code scan for analytics.
    /// </summary>
    /// <param name="tableId">Table identifier.</param>
    /// <param name="sessionId">Session identifier.</param>
    Task RecordQrScanAsync(int tableId, string? sessionId = null);

    /// <summary>
    /// Gets print template for table QR code.
    /// </summary>
    /// <param name="tableId">Table identifier.</param>
    /// <returns>Print template.</returns>
    Task<QrCodePrintTemplate?> GetPrintTemplateAsync(int tableId);

    #endregion

    #region Menu Management

    /// <summary>
    /// Gets all categories available for QR menu.
    /// </summary>
    /// <returns>List of categories.</returns>
    Task<IReadOnlyList<QrMenuCategory>> GetCategoriesAsync();

    /// <summary>
    /// Gets products for a specific category.
    /// </summary>
    /// <param name="categoryId">Category identifier.</param>
    /// <returns>List of products in category.</returns>
    Task<IReadOnlyList<QrMenuProduct>> GetProductsByCategoryAsync(int categoryId);

    /// <summary>
    /// Gets all products available for QR menu.
    /// </summary>
    /// <returns>List of all QR menu products.</returns>
    Task<IReadOnlyList<QrMenuProduct>> GetAllProductsAsync();

    /// <summary>
    /// Gets a specific product by ID.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <returns>Product details or null.</returns>
    Task<QrMenuProduct?> GetProductAsync(int productId);

    /// <summary>
    /// Gets featured/popular products.
    /// </summary>
    /// <param name="limit">Maximum number to return.</param>
    /// <returns>List of featured products.</returns>
    Task<IReadOnlyList<QrMenuProduct>> GetFeaturedProductsAsync(int limit = 10);

    /// <summary>
    /// Searches products by name or description.
    /// </summary>
    /// <param name="searchTerm">Search term.</param>
    /// <returns>Matching products.</returns>
    Task<IReadOnlyList<QrMenuProduct>> SearchProductsAsync(string searchTerm);

    /// <summary>
    /// Sets whether a product is available for QR menu.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="isAvailable">Whether available.</param>
    Task SetProductAvailabilityAsync(int productId, bool isAvailable);

    /// <summary>
    /// Sets product stock status.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="inStock">Whether in stock.</param>
    Task SetProductStockStatusAsync(int productId, bool inStock);

    /// <summary>
    /// Syncs products from main product catalog to QR menu.
    /// </summary>
    /// <returns>Number of products synced.</returns>
    Task<int> SyncProductsFromCatalogAsync();

    #endregion

    #region Order Management

    /// <summary>
    /// Validates an order before submission.
    /// </summary>
    /// <param name="request">Order request.</param>
    /// <returns>Validation result with any error messages.</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateOrderAsync(QrMenuOrderRequest request);

    /// <summary>
    /// Submits a QR menu order.
    /// </summary>
    /// <param name="request">Order request.</param>
    /// <returns>Order response with confirmation.</returns>
    Task<QrMenuOrderResponse> SubmitOrderAsync(QrMenuOrderRequest request);

    /// <summary>
    /// Gets the status of an order.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <returns>Order status update.</returns>
    Task<QrOrderStatusUpdate?> GetOrderStatusAsync(int orderId);

    /// <summary>
    /// Gets the status of an order by confirmation code.
    /// </summary>
    /// <param name="confirmationCode">Confirmation code.</param>
    /// <returns>Order status update.</returns>
    Task<QrOrderStatusUpdate?> GetOrderStatusByCodeAsync(string confirmationCode);

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="status">New status.</param>
    /// <param name="estimatedWaitMinutes">Updated wait time.</param>
    Task UpdateOrderStatusAsync(int orderId, QrOrderStatus status, int? estimatedWaitMinutes = null);

    /// <summary>
    /// Cancels a QR menu order.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="reason">Cancellation reason.</param>
    /// <returns>Whether cancellation was successful.</returns>
    Task<bool> CancelOrderAsync(int orderId, string? reason = null);

    /// <summary>
    /// Gets pending QR orders (not yet served).
    /// </summary>
    /// <returns>List of pending orders.</returns>
    Task<IReadOnlyList<QrOrderNotification>> GetPendingOrdersAsync();

    /// <summary>
    /// Gets recent QR orders.
    /// </summary>
    /// <param name="limit">Maximum number to return.</param>
    /// <returns>List of recent orders.</returns>
    Task<IReadOnlyList<QrOrderNotification>> GetRecentOrdersAsync(int limit = 20);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets QR menu analytics for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Analytics data.</returns>
    Task<QrMenuAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets today's QR menu analytics.
    /// </summary>
    /// <returns>Today's analytics.</returns>
    Task<QrMenuAnalytics> GetTodayAnalyticsAsync();

    /// <summary>
    /// Gets popular items from QR orders.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="limit">Maximum items to return.</param>
    /// <returns>Popular items list.</returns>
    Task<IReadOnlyList<QrMenuPopularItem>> GetPopularItemsAsync(DateTime startDate, DateTime endDate, int limit = 10);

    #endregion

    #region Events

    /// <summary>
    /// Raised when a new QR order is received.
    /// </summary>
    event EventHandler<QrOrderNotification>? OrderReceived;

    /// <summary>
    /// Raised when an order status changes.
    /// </summary>
    event EventHandler<QrOrderStatusUpdate>? OrderStatusChanged;

    /// <summary>
    /// Raised when a QR code is scanned.
    /// </summary>
    event EventHandler<(int TableId, string? SessionId)>? QrCodeScanned;

    #endregion
}
