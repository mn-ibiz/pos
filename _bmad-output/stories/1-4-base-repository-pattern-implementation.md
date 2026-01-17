# Story 1.4: Base Repository Pattern Implementation

Status: done

## Story

As a developer,
I want a generic repository pattern implemented,
So that data access is consistent and testable across all entities.

## Acceptance Criteria

1. **Given** EF Core is configured
   **When** repository interfaces are defined
   **Then** IRepository<T> interface should provide:
   - GetByIdAsync(int id)
   - GetAllAsync()
   - AddAsync(T entity)
   - UpdateAsync(T entity)
   - DeleteAsync(int id)
   - Query() for custom LINQ queries

2. **Given** interfaces are defined
   **When** repositories are implemented
   **Then** concrete repositories should be created for each major entity

3. **Given** repositories exist
   **When** multiple operations need to be atomic
   **Then** unit of work pattern should be implemented for transaction management

## Tasks / Subtasks

- [x] Task 1: Create Generic Repository Interface (AC: #1)
  - [x] Create IRepository<T> interface in Core/Interfaces (from Story 1-1)
  - [x] Define all CRUD method signatures with CancellationToken support
  - [x] Add Query() and QueryNoTracking() methods
  - [x] Add FindAsync, AnyAsync, CountAsync methods

- [x] Task 2: Implement Base Repository (AC: #2)
  - [x] Create Repository<T> class in Infrastructure/Repositories (from Story 1-1)
  - [x] Implement all interface methods with ConfigureAwait(false)
  - [x] Use POSDbContext for database operations
  - [x] Implement soft delete support for ISoftDeletable entities

- [x] Task 3: Create Entity-Specific Repositories (AC: #2)
  - [x] Create IProductRepository and ProductRepository
  - [x] Create IOrderRepository and OrderRepository
  - [x] Create IReceiptRepository and ReceiptRepository
  - [x] Create IUserRepository and UserRepository
  - [x] Create IInventoryRepository and InventoryRepository
  - [x] Create ICategoryRepository and CategoryRepository
  - [x] Create IWorkPeriodRepository and WorkPeriodRepository
  - [x] Added comprehensive entity-specific query methods

- [x] Task 4: Implement Unit of Work (AC: #3)
  - [x] Create IUnitOfWork interface (from Story 1-1)
  - [x] Create UnitOfWork class in Infrastructure/Data
  - [x] Implement SaveChangesAsync method
  - [x] Implement transaction support (BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync)
  - [x] Implement proper disposal pattern

- [x] Task 5: Register Dependencies
  - [x] Update ServiceCollectionExtensions in Infrastructure/Extensions
  - [x] Register generic Repository<T> with Scoped lifetime
  - [x] Register all entity-specific repositories with Scoped lifetime
  - [x] Register UnitOfWork with Scoped lifetime

## Dev Notes

### IRepository Interface

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    IQueryable<T> Query();
}
```

### Base Repository Implementation

```csharp
public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly HospitalityDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(HospitalityDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}
```

### Unit of Work Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IReceiptRepository Receipts { get; }
    IUserRepository Users { get; }
    IInventoryRepository Inventories { get; }
    // ... other repositories

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

### Entity-Specific Repository Example

```csharp
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string code);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
}

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(HospitalityDbContext context) : base(context) { }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .Where(p => p.Name.Contains(searchTerm) || p.Code.Contains(searchTerm))
            .ToListAsync();
    }
}
```

### Dependency Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

### References
- [Source: _bmad-output/architecture.md#2-System-Architecture]
- [Source: _bmad-output/project-context.md#Critical-Patterns]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Base repository infrastructure (IRepository, Repository, IUnitOfWork, UnitOfWork) already existed from Story 1-1
- Created 7 entity-specific repository interfaces in IEntityRepositories.cs
- Created 7 entity-specific repository implementations in EntityRepositories.cs
- Each entity repository includes comprehensive query methods for common use cases
- Updated ServiceCollectionExtensions to register all entity-specific repositories
- All repositories support CancellationToken and ConfigureAwait(false) for async operations
- Soft delete pattern properly supported in base Repository class

### File List
**New Files:**
- src/HospitalityPOS.Core/Interfaces/IEntityRepositories.cs
- src/HospitalityPOS.Infrastructure/Repositories/EntityRepositories.cs

**Modified Files:**
- src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs (added entity repository registrations)

**Pre-existing Files (from Story 1-1):**
- src/HospitalityPOS.Core/Interfaces/IRepository.cs
- src/HospitalityPOS.Core/Interfaces/IUnitOfWork.cs
- src/HospitalityPOS.Infrastructure/Repositories/Repository.cs
- src/HospitalityPOS.Infrastructure/Data/UnitOfWork.cs

## Senior Developer Review

### Review Date
2025-12-30

### Review Agent
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Issues Found: 7 (2 High, 4 Medium, 1 Low)

#### HIGH Severity Issues

1. **ValidateCredentialsAsync incompatible with BCrypt** (IEntityRepositories.cs, EntityRepositories.cs)
   - Problem: Method compared passwordHash in database query. BCrypt hashes include unique salts, so this query would never find a matching user.
   - Fix: Renamed to GetByUsernameForAuthAsync - only retrieves user by username; password verification moved to service layer using BCrypt.Verify.

2. **UnitOfWork incorrectly disposing DI-injected DbContext** (UnitOfWork.cs)
   - Problem: Dispose method disposed _context, but DI container owns DbContext lifecycle with Scoped lifetime. This could cause ObjectDisposedException.
   - Fix: Removed _context.Dispose() call, added documentation explaining DI ownership.

#### MEDIUM Severity Issues

3. **IUnitOfWork missing IAsyncDisposable** (IUnitOfWork.cs)
   - Problem: UnitOfWork had async disposal internally but only implemented IDisposable.
   - Fix: Added IAsyncDisposable interface, implemented DisposeAsync() method.

4. **ToLower() in SearchAsync SQL inefficient** (EntityRepositories.cs)
   - Problem: Calling ToLower() on columns prevents SQL Server index usage.
   - Fix: Replaced with EF.Functions.Like for efficient SQL LIKE translation.

5. **Missing null/empty validation on string parameters** (EntityRepositories.cs)
   - Problem: Methods like GetByCodeAsync, SearchAsync didn't validate inputs.
   - Fix: Added ArgumentException.ThrowIfNullOrWhiteSpace() to all string parameter methods.

6. **UpdateStockAsync allows negative inventory** (EntityRepositories.cs)
   - Problem: No validation prevented inventory from going negative.
   - Fix: Added check that throws InvalidOperationException if result would be negative.

#### LOW Severity Issues

7. **Inconsistent soft delete handling** (EntityRepositories.cs)
   - Recommendation: Document which methods include/exclude soft-deleted records. Consider EF Core global query filters for consistency. (Not fixed - design decision)

### Files Modified During Review
- src/HospitalityPOS.Core/Interfaces/IEntityRepositories.cs (renamed ValidateCredentialsAsync)
- src/HospitalityPOS.Core/Interfaces/IUnitOfWork.cs (added IAsyncDisposable)
- src/HospitalityPOS.Infrastructure/Repositories/EntityRepositories.cs (multiple fixes)
- src/HospitalityPOS.Infrastructure/Data/UnitOfWork.cs (disposal pattern fix, added DisposeAsync)

### Review Outcome
✅ All HIGH and MEDIUM issues fixed. Story status: **done**

## Change Log
- 2025-12-30: Implementation completed - 7 entity-specific repositories created with comprehensive query methods
- 2025-12-30: Senior Developer Review - 6 issues fixed (2 HIGH, 4 MEDIUM)
