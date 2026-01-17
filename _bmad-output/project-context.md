# Project Context: Hospitality POS System

**Generated:** December 2025
**Purpose:** Critical rules and patterns for AI agents implementing code

---

## Project Overview

A Windows desktop Point of Sale (POS) system for hotels, bars, and restaurants built with C# and MS SQL Server Express. Touch-optimized interface for fast-paced hospitality environments.

---

## Technology Stack (MANDATORY)

| Component | Technology | Version |
|-----------|------------|---------|
| Language | C# | 14 |
| Framework | .NET | 10 (LTS) |
| UI Framework | WPF (Windows Presentation Foundation) | .NET 10 |
| Database | MS SQL Server Express | 2022+ |
| ORM | Entity Framework Core | 10 |
| UI Pattern | MVVM (Model-View-ViewModel) | CommunityToolkit.Mvvm 8.x |
| Printing | ESC/POS Protocol | 80mm thermal |

### .NET 10 Requirements
- **.NET 10 SDK** required to build (released November 11, 2025)
- **.NET 10 Runtime** required to run
- **LTS Support** through November 2028
- **C# 14** language version enabled by default

### C# 14 Features to Use
```csharp
// Extension members - for service extensions
public extension class StringExtensions for string
{
    public bool IsValidMpesaCode => this.Length == 10 && this.All(char.IsLetterOrDigit);
}

// Field-backed properties - simpler ViewModels
public string Name
{
    get => field;
    set => SetProperty(ref field, value);
}

// Partial constructors - for generated code
public partial class ProductViewModel
{
    partial ProductViewModel(); // Generated part
}
```

### EF Core 10 Features to Use
```csharp
// Named query filters
modelBuilder.Entity<Product>()
    .HasQueryFilter("SoftDelete", p => p.IsActive)
    .HasQueryFilter("CurrentWorkPeriod", p => p.WorkPeriodId == currentId);

// LeftJoin/RightJoin operators
var report = await context.Products
    .LeftJoin(context.OrderItems, p => p.Id, oi => oi.ProductId, (p, oi) => new { p, oi })
    .ToListAsync();

// JSON column mapping for audit data
modelBuilder.Entity<AuditLog>()
    .Property(a => a.AdditionalData)
    .HasColumnType("json");

// Simplified ExecuteUpdateAsync
await context.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteUpdateAsync(s => { s.IsActive = false; s.UpdatedAt = DateTime.UtcNow; });
```

---

## Project Structure (FOLLOW EXACTLY)

```
HospitalityPOS/
├── src/
│   ├── HospitalityPOS.Core/           # Domain entities, interfaces, enums, DTOs
│   ├── HospitalityPOS.Infrastructure/ # EF Core, repositories, printing, hardware
│   ├── HospitalityPOS.Business/       # Services, business logic, validators
│   └── HospitalityPOS.WPF/            # Views, ViewModels, Controls, Resources
├── tests/
│   ├── HospitalityPOS.Core.Tests/
│   ├── HospitalityPOS.Business.Tests/
│   └── HospitalityPOS.WPF.Tests/
└── docs/
```

---

## Naming Conventions (MUST FOLLOW)

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `OrderService`, `ProductRepository` |
| Interfaces | I + PascalCase | `IOrderService`, `IRepository<T>` |
| Async Methods | PascalCase + Async | `CreateOrderAsync`, `GetByIdAsync` |
| Properties | PascalCase | `TotalAmount`, `CreatedAt` |
| Private fields | _camelCase | `_orderRepository`, `_dbContext` |
| Constants | UPPER_SNAKE | `MAX_DISCOUNT_PERCENT` |
| ViewModels | Name + ViewModel | `POSViewModel`, `LoginViewModel` |
| Views | Name + View.xaml | `POSView.xaml`, `LoginView.xaml` |
| DB Tables | PascalCase, Plural | `Orders`, `OrderItems`, `Users` |
| DB Columns | PascalCase | `CreatedAt`, `TotalAmount`, `IsActive` |

---

## Critical Patterns

### 1. Repository Pattern
All data access MUST go through repositories:
```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    IQueryable<T> Query();
}
```

### 2. MVVM Pattern
- Views contain NO code-behind logic (except navigation)
- ViewModels expose properties and commands
- Use `CommunityToolkit.Mvvm` for base classes and commands

### 3. Service Layer Pattern
- Business logic in `*Service` classes
- Services depend on interfaces, not concrete implementations
- Dependency injection via constructor

### 4. Async/Await
- All database operations MUST be async
- Use `ConfigureAwait(false)` in library code
- Never use `.Result` or `.Wait()` - causes deadlocks

---

## Security Requirements (CRITICAL)

1. **Passwords**: BCrypt hash with cost factor 12 - NEVER store plain text
2. **Sessions**: Timeout after 30 minutes of inactivity
3. **Audit**: Log ALL transactions, voids, permission overrides
4. **RBAC**: Check permissions before EVERY protected action
5. **Input**: Validate and sanitize ALL user inputs

---

## Database Guidelines

1. **Connection String**: Use `Trusted_Connection=True` for Windows Auth
2. **Transactions**: Use Unit of Work for multi-table operations
3. **Migrations**: Use EF Core migrations for schema changes
4. **Indexes**: Create on frequently queried columns
5. **Soft Delete**: Use `IsActive` flag, never hard delete

---

## UI/UX Requirements

1. **Touch-First**: All buttons minimum 44x44 pixels
2. **High Contrast**: Clear visibility in various lighting
3. **Minimal Clicks**: Common actions within 2-3 taps
4. **Error Messages**: Clear, actionable, in plain language
5. **Confirmation**: Dialogs for destructive actions

---

## Printing (80mm Thermal)

- **Width**: 80mm paper, ~48 characters per line
- **Protocol**: ESC/POS commands
- **Receipt Template**: Header, items, totals, footer
- **KOT**: Large font, order number prominent
- **Reports**: Formatted for narrow width

---

## Error Handling

1. Wrap operations in try-catch
2. Log errors with Serilog
3. Show user-friendly messages
4. Never expose stack traces to users
5. Implement retry logic for transient failures

---

## DO NOT (Critical Restrictions)

- DO NOT use synchronous database calls
- DO NOT store passwords in plain text
- DO NOT skip permission checks
- DO NOT hard delete records (use soft delete)
- DO NOT put business logic in ViewModels
- DO NOT use code-behind in Views for logic
- DO NOT skip audit logging for transactions
- DO NOT ignore the 80mm print width constraint

---

## Key Entity Relationships

```
User (1) ─── (N) UserRoles (N) ─── (1) Role
Role (1) ─── (N) RolePermissions (N) ─── (1) Permission

WorkPeriod (1) ─── (N) Orders
WorkPeriod (1) ─── (N) Receipts

Order (1) ─── (N) OrderItems (N) ─── (1) Product
Order (1) ─── (1) Receipt
Receipt (1) ─── (N) Payments (N) ─── (1) PaymentMethod

Product (N) ─── (1) Category
Product (1) ─── (1) Inventory
Product (1) ─── (N) StockMovements
```

---

## Default Values

| Setting | Default |
|---------|---------|
| Tax Rate | 16% (VAT Kenya) |
| Currency | KES (Kenyan Shilling) |
| Currency Symbol | KSh |
| Session Timeout | 30 minutes |
| Max Login Attempts | 5 |
| Lockout Duration | 15 minutes |

---

## Testing Requirements

- Unit tests for all business logic
- Integration tests for database operations
- Use xUnit and Moq
- Target 80% code coverage for business layer
