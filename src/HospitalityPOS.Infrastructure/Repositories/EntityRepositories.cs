using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace HospitalityPOS.Infrastructure.Repositories;

/// <summary>
/// Product repository implementation.
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await _dbSet
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        return await _dbSet
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        // Use EF.Functions.Like for case-insensitive search that translates to efficient SQL LIKE
        var pattern = $"%{searchTerm}%";
        return await _dbSet
            .Where(p => EF.Functions.Like(p.Name, pattern) ||
                        EF.Functions.Like(p.Code, pattern) ||
                        (p.Barcode != null && EF.Functions.Like(p.Barcode, pattern)))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Inventory)
            .Where(p => p.IsActive &&
                        p.MinStockLevel.HasValue &&
                        p.Inventory != null &&
                        p.Inventory.Quantity < p.MinStockLevel.Value)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Order repository implementation.
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        return await _dbSet
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByWorkPeriodAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.WorkPeriodId == workPeriodId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetByTableAsync(string tableNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableNumber);

        return await _dbSet
            .Where(o => o.TableNumber == tableNumber)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Order?> GetWithItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetOpenOrdersAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.WorkPeriodId == workPeriodId && o.Status == OrderStatus.Open)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Receipt repository implementation.
/// </summary>
public class ReceiptRepository : Repository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);

        return await _dbSet
            .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetByWorkPeriodAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.WorkPeriodId == workPeriodId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Receipt?> GetWithPaymentsAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetUnsettledReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.WorkPeriodId == workPeriodId &&
                        (r.Status == ReceiptStatus.Created || r.Status == ReceiptStatus.Pending))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetVoidedReceiptsAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.WorkPeriodId == workPeriodId && r.Status == ReceiptStatus.Voided)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// User repository implementation.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetByRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameForAuthAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Inventory repository implementation.
/// </summary>
public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Where(i => i.Product.MinStockLevel.HasValue &&
                        i.Product.TrackInventory &&
                        i.CurrentStock < i.Product.MinStockLevel.Value &&
                        i.CurrentStock > 0)
            .OrderBy(i => i.CurrentStock)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetOverstockedItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Where(i => i.Product.MaxStockLevel.HasValue &&
                        i.CurrentStock > i.Product.MaxStockLevel.Value)
            .OrderByDescending(i => i.CurrentStock)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetWithProductDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .OrderBy(i => i.Product.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateStockAsync(int productId, decimal quantityChange, CancellationToken cancellationToken = default)
    {
        var inventory = await GetByProductIdAsync(productId, cancellationToken).ConfigureAwait(false);

        if (inventory is null)
        {
            throw new InvalidOperationException($"No inventory record found for product ID {productId}");
        }

        var newQuantity = inventory.CurrentStock + quantityChange;
        if (newQuantity < 0)
        {
            newQuantity = 0; // Allow stock to go to zero but not negative
        }

        inventory.CurrentStock = newQuantity;
        inventory.LastUpdated = DateTime.UtcNow;
        _dbSet.Update(inventory);
    }
}

/// <summary>
/// Stock movement repository implementation.
/// </summary>
public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sm => sm.ProductId == productId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetByProductAndDateRangeAsync(
        int productId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sm => sm.ProductId == productId &&
                         sm.CreatedAt >= startDate &&
                         sm.CreatedAt <= endDate)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetByMovementTypeAsync(
        MovementType movementType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(sm => sm.MovementType == movementType);

        if (startDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetByReferenceAsync(
        string referenceType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sm => sm.ReferenceType == referenceType && sm.ReferenceId == referenceId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockMovement?> GetLatestByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sm => sm.ProductId == productId)
            .OrderByDescending(sm => sm.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockMovement>> GetWithProductDetailsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(sm => sm.Product).AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Category repository implementation.
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetSubcategoriesAsync(int parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Category?> GetWithProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Products.Where(p => p.IsActive))
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Work period repository implementation.
/// </summary>
public class WorkPeriodRepository : Repository<WorkPeriod>, IWorkPeriodRepository
{
    public WorkPeriodRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<WorkPeriod?> GetCurrentOpenPeriodAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(wp => wp.Status == WorkPeriodStatus.Open, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkPeriod>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(wp => wp.StartTime >= startDate && wp.StartTime <= endDate)
            .OrderByDescending(wp => wp.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkPeriod?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(wp => wp.StartTime)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Stock take repository implementation.
/// </summary>
public class StockTakeRepository : Repository<StockTake>, IStockTakeRepository
{
    public StockTakeRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<StockTake?> GetByStockTakeNumberAsync(string stockTakeNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stockTakeNumber);

        return await _dbSet
            .FirstOrDefaultAsync(st => st.StockTakeNumber == stockTakeNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake?> GetWithItemsAsync(int stockTakeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .FirstOrDefaultAsync(st => st.Id == stockTakeId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> GetByStatusAsync(StockTakeStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(st => st.StartedByUser)
            .Where(st => st.Status == status)
            .OrderByDescending(st => st.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTake?> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(st => st.Items)
                .ThenInclude(i => i.Product)
            .Include(st => st.StartedByUser)
            .FirstOrDefaultAsync(st => st.Status == StockTakeStatus.InProgress, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTake>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(st => st.StartedByUser)
            .Include(st => st.ApprovedByUser)
            .Where(st => st.StartedAt >= startDate && st.StartedAt <= endDate)
            .OrderByDescending(st => st.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetLatestStockTakeNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(st => st.StockTakeNumber.StartsWith(prefix))
            .OrderByDescending(st => st.StockTakeNumber)
            .Select(st => st.StockTakeNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Stock take item repository implementation.
/// </summary>
public class StockTakeItemRepository : Repository<StockTakeItem>, IStockTakeItemRepository
{
    public StockTakeItemRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> GetByStockTakeIdAsync(int stockTakeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Include(i => i.CountedByUser)
            .Where(i => i.StockTakeId == stockTakeId)
            .OrderBy(i => i.ProductName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockTakeItem?> GetByStockTakeAndProductAsync(int stockTakeId, int productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.StockTakeId == stockTakeId && i.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> GetUncountedItemsAsync(int stockTakeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Where(i => i.StockTakeId == stockTakeId && !i.IsCounted)
            .OrderBy(i => i.ProductName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StockTakeItem>> GetItemsWithVarianceAsync(int stockTakeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Product)
            .Include(i => i.CountedByUser)
            .Where(i => i.StockTakeId == stockTakeId && i.IsCounted && i.VarianceQuantity != 0)
            .OrderByDescending(i => Math.Abs(i.VarianceValue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Loyalty member repository implementation.
/// </summary>
public class LoyaltyMemberRepository : Repository<LoyaltyMember>, ILoyaltyMemberRepository
{
    public LoyaltyMemberRepository(POSDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<LoyaltyMember?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        return await _dbSet
            .FirstOrDefaultAsync(m => m.PhoneNumber == phoneNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LoyaltyMember?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(membershipNumber);

        return await _dbSet
            .FirstOrDefaultAsync(m => m.MembershipNumber == membershipNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        return await _dbSet
            .AnyAsync(m => m.PhoneNumber == phoneNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoyaltyMember>> SearchAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        var pattern = $"%{searchTerm}%";
        return await _dbSet
            .Where(m => EF.Functions.Like(m.PhoneNumber, pattern) ||
                        (m.Name != null && EF.Functions.Like(m.Name, pattern)) ||
                        EF.Functions.Like(m.MembershipNumber, pattern))
            .OrderBy(m => m.Name ?? m.PhoneNumber)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetNextSequenceNumberAsync(string datePrefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datePrefix);

        var prefix = $"LM-{datePrefix}-";
        var latestMembershipNumber = await _dbSet
            .Where(m => m.MembershipNumber.StartsWith(prefix))
            .OrderByDescending(m => m.MembershipNumber)
            .Select(m => m.MembershipNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(latestMembershipNumber))
        {
            return 1;
        }

        // Extract the sequence number from LM-YYYYMMDD-XXXXX format
        var sequencePart = latestMembershipNumber.Substring(prefix.Length);
        if (int.TryParse(sequencePart, out var lastSequence))
        {
            return lastSequence + 1;
        }

        return 1;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoyaltyMember>> GetByTierAsync(MembershipTier tier, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.Tier == tier)
            .OrderBy(m => m.Name ?? m.PhoneNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoyaltyMember>> GetActivesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.IsActive && m.LastVisit != null && m.LastVisit >= since)
            .OrderByDescending(m => m.LastVisit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
