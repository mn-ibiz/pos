using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Product repository interface with product-specific query methods.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Gets a product by its unique code.
    /// </summary>
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its barcode.
    /// </summary>
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products in a specific category.
    /// </summary>
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active products.
    /// </summary>
    Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, code, or barcode.
    /// </summary>
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with stock below minimum level.
    /// </summary>
    Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Order repository interface with order-specific query methods.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Gets an order by its order number.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a specific work period.
    /// </summary>
    Task<IEnumerable<Order>> GetByWorkPeriodAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a specific user.
    /// </summary>
    Task<IEnumerable<Order>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a specific table.
    /// </summary>
    Task<IEnumerable<Order>> GetByTableAsync(string tableNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order with all its items included.
    /// </summary>
    Task<Order?> GetWithItemsAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets open orders for a work period.
    /// </summary>
    Task<IEnumerable<Order>> GetOpenOrdersAsync(int workPeriodId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Receipt repository interface with receipt-specific query methods.
/// </summary>
public interface IReceiptRepository : IRepository<Receipt>
{
    /// <summary>
    /// Gets a receipt by its receipt number.
    /// </summary>
    Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all receipts for a specific work period.
    /// </summary>
    Task<IEnumerable<Receipt>> GetByWorkPeriodAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a receipt with all its payments included.
    /// </summary>
    Task<Receipt?> GetWithPaymentsAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unsettled receipts for a work period.
    /// </summary>
    Task<IEnumerable<Receipt>> GetUnsettledReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets voided receipts for a work period.
    /// </summary>
    Task<IEnumerable<Receipt>> GetVoidedReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default);
}

/// <summary>
/// User repository interface with user-specific query methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with all roles included.
    /// </summary>
    Task<User?> GetWithRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users.
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role.
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username for authentication purposes.
    /// Note: Password verification should be done at the service layer using BCrypt.Verify,
    /// not by comparing hashes in the database (BCrypt hashes are salted and unique).
    /// </summary>
    Task<User?> GetByUsernameForAuthAsync(string username, CancellationToken cancellationToken = default);
}

/// <summary>
/// Inventory repository interface with inventory-specific query methods.
/// </summary>
public interface IInventoryRepository : IRepository<Inventory>
{
    /// <summary>
    /// Gets inventory for a specific product.
    /// </summary>
    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inventory items below minimum stock level.
    /// </summary>
    Task<IEnumerable<Inventory>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inventory items above maximum stock level.
    /// </summary>
    Task<IEnumerable<Inventory>> GetOverstockedItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory with product details included.
    /// </summary>
    Task<IEnumerable<Inventory>> GetWithProductDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates stock quantity for a product.
    /// </summary>
    Task UpdateStockAsync(int productId, decimal quantityChange, CancellationToken cancellationToken = default);
}

/// <summary>
/// Category repository interface with category-specific query methods.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets all root categories (no parent).
    /// </summary>
    Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subcategories of a parent category.
    /// </summary>
    Task<IEnumerable<Category>> GetSubcategoriesAsync(int parentCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category with all its products.
    /// </summary>
    Task<Category?> GetWithProductsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active categories.
    /// </summary>
    Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Work period repository interface with work period-specific query methods.
/// </summary>
public interface IWorkPeriodRepository : IRepository<WorkPeriod>
{
    /// <summary>
    /// Gets the current open work period.
    /// </summary>
    Task<WorkPeriod?> GetCurrentOpenPeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work periods for a date range.
    /// </summary>
    Task<IEnumerable<WorkPeriod>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent work period.
    /// </summary>
    Task<WorkPeriod?> GetLatestAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Stock movement repository interface with stock movement-specific query methods.
/// </summary>
public interface IStockMovementRepository : IRepository<StockMovement>
{
    /// <summary>
    /// Gets stock movements for a specific product.
    /// </summary>
    Task<IEnumerable<StockMovement>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements for a product within a date range.
    /// </summary>
    Task<IEnumerable<StockMovement>> GetByProductAndDateRangeAsync(
        int productId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by movement type.
    /// </summary>
    Task<IEnumerable<StockMovement>> GetByMovementTypeAsync(
        Enums.MovementType movementType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by reference.
    /// </summary>
    Task<IEnumerable<StockMovement>> GetByReferenceAsync(
        string referenceType,
        int referenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent stock movement for a product.
    /// </summary>
    Task<StockMovement?> GetLatestByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements with product details included.
    /// </summary>
    Task<IEnumerable<StockMovement>> GetWithProductDetailsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stock take repository interface with stock take-specific query methods.
/// </summary>
public interface IStockTakeRepository : IRepository<StockTake>
{
    /// <summary>
    /// Gets a stock take by its stock take number.
    /// </summary>
    Task<StockTake?> GetByStockTakeNumberAsync(string stockTakeNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stock take with all items included.
    /// </summary>
    Task<StockTake?> GetWithItemsAsync(int stockTakeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock takes by status.
    /// </summary>
    Task<IEnumerable<StockTake>> GetByStatusAsync(Enums.StockTakeStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current in-progress stock take if any.
    /// </summary>
    Task<StockTake?> GetInProgressAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock takes within a date range.
    /// </summary>
    Task<IEnumerable<StockTake>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest stock take number for generating new numbers.
    /// </summary>
    Task<string?> GetLatestStockTakeNumberAsync(string prefix, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stock take item repository interface.
/// </summary>
public interface IStockTakeItemRepository : IRepository<StockTakeItem>
{
    /// <summary>
    /// Gets all items for a stock take.
    /// </summary>
    Task<IEnumerable<StockTakeItem>> GetByStockTakeIdAsync(int stockTakeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stock take item by stock take and product.
    /// </summary>
    Task<StockTakeItem?> GetByStockTakeAndProductAsync(int stockTakeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets uncounted items for a stock take.
    /// </summary>
    Task<IEnumerable<StockTakeItem>> GetUncountedItemsAsync(int stockTakeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets items with variance for a stock take.
    /// </summary>
    Task<IEnumerable<StockTakeItem>> GetItemsWithVarianceAsync(int stockTakeId, CancellationToken cancellationToken = default);
}
