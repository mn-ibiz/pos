# Story 6.1: Receipt Creation

Status: done

## Story

As the system,
I want receipts to be created from orders,
So that payments can be tracked and recorded.

## Acceptance Criteria

1. **Given** an order has been created and printed
   **When** a receipt is generated
   **Then** receipt should be created with unique receipt number

2. **Given** a receipt is created
   **When** linking to source order
   **Then** receipt should be linked to the order and work period

3. **Given** a new receipt
   **When** setting initial status
   **Then** receipt should have status "Pending" (in manual settlement mode)

4. **Given** receipt ownership
   **When** assigning owner
   **Then** receipt should be assigned to the user who created it (owner)

5. **Given** receipt totals
   **When** storing financial data
   **Then** receipt should store: subtotal, tax, discounts, total amount

## Tasks / Subtasks

- [x] Task 1: Create Receipt Entity and Configuration
  - [x] Create Receipt entity class (updated existing with TableNumber, CustomerName, SettledByUserId, ReceiptItems)
  - [x] Create ReceiptItem entity class
  - [x] Configure EF Core mappings (ReceiptConfiguration, ReceiptItemConfiguration)
  - [x] Add ReceiptItems DbSet to POSDbContext
  - [ ] Create database migration (manual: dotnet ef migrations add AddReceiptItemEntity)

- [x] Task 2: Implement Receipt Number Generation
  - [x] Create receipt number format: R-{yyyyMMdd}-{sequence}
  - [x] Ensure thread-safe sequence generation (via GenerateReceiptNumberAsync)
  - [x] Reset sequence daily (prefix-based daily reset)

- [x] Task 3: Create Receipt Service (simplified from Repository pattern)
  - [x] Create IReceiptService interface with all query methods
  - [x] Implement GetByWorkPeriodAsync method (GetReceiptsByWorkPeriodAsync)
  - [x] Implement GetByUserAsync method (GetReceiptsByUserAsync)
  - [x] Implement GetPendingAsync method (GetPendingReceiptsAsync)

- [x] Task 4: Implement Receipt Creation Service
  - [x] Create IReceiptService interface
  - [x] Implement CreateReceiptFromOrderAsync method
  - [x] Calculate and store totals (copies from order)
  - [x] Link to order and work period

- [x] Task 5: Update Order Print Flow
  - [x] Create receipt when order is printed (in POSViewModel.SubmitOrderAsync)
  - [x] Update order status to "Sent" (changed from Printed per OrderStatus enum)
  - [ ] Navigate to receipt settlement (deferred to Story 6-2)

## Dev Notes

### Receipt Entity

```csharp
public class Receipt
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public int WorkPeriodId { get; set; }
    public int UserId { get; set; }  // Owner
    public string? TableNumber { get; set; }
    public string? CustomerName { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";  // Pending, Settled, Voided

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }
    public int? SettledByUserId { get; set; }

    // Split/Merge tracking
    public int? ParentReceiptId { get; set; }
    public bool IsSplit { get; set; } = false;
    public bool IsMerged { get; set; } = false;

    // Navigation
    public Order Order { get; set; } = null!;
    public WorkPeriod WorkPeriod { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? SettledByUser { get; set; }
    public Receipt? ParentReceipt { get; set; }
    public ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
```

### ReceiptItem Entity

```csharp
public class ReceiptItem
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Modifiers { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Receipt Receipt { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

### Receipt Status Values
- **Pending**: Created, awaiting payment
- **Settled**: Payment received, complete
- **Voided**: Cancelled with reason
- **Partially Paid**: Partial payment received (split scenario)

### Receipt Number Generation

```csharp
public class ReceiptNumberGenerator
{
    private readonly ApplicationDbContext _context;
    private static readonly object _lock = new();

    public async Task<string> GenerateNextAsync()
    {
        var today = DateTime.Today;
        var prefix = $"R-{today:yyyyMMdd}-";

        lock (_lock)
        {
            var lastReceipt = _context.Receipts
                .Where(r => r.ReceiptNumber.StartsWith(prefix))
                .OrderByDescending(r => r.ReceiptNumber)
                .FirstOrDefault();

            int sequence = 1;
            if (lastReceipt != null)
            {
                var lastSeq = lastReceipt.ReceiptNumber.Split('-').Last();
                sequence = int.Parse(lastSeq) + 1;
            }

            return $"{prefix}{sequence:D4}";
        }
    }
}
```

### Create Receipt Service

```csharp
public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receiptRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ReceiptNumberGenerator _numberGenerator;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Receipt> CreateReceiptFromOrderAsync(int orderId)
    {
        var order = await _orderRepo.GetByIdWithItemsAsync(orderId);
        if (order == null)
            throw new NotFoundException("Order not found");

        var currentUser = await _authService.GetCurrentUserAsync();

        var receipt = new Receipt
        {
            ReceiptNumber = await _numberGenerator.GenerateNextAsync(),
            OrderId = orderId,
            WorkPeriodId = order.WorkPeriodId,
            UserId = currentUser.Id,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Status = "Pending"
        };

        // Copy order items to receipt items
        foreach (var orderItem in order.OrderItems)
        {
            receipt.ReceiptItems.Add(new ReceiptItem
            {
                OrderItemId = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.Product.Name,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                DiscountAmount = orderItem.DiscountAmount,
                TaxAmount = orderItem.TaxAmount,
                TotalAmount = orderItem.TotalAmount,
                Modifiers = orderItem.Modifiers,
                Notes = orderItem.Notes
            });
        }

        await _receiptRepo.AddAsync(receipt);

        // Update order status
        order.Status = "Printed";
        await _orderRepo.UpdateAsync(order);

        await _unitOfWork.SaveChangesAsync();

        return receipt;
    }
}
```

### Receipt Creation Flow

```
[Order Created]
      |
      v
[Print Order Button]
      |
      v
[Print KOT to Kitchen]
      |
      v
[Create Receipt]
      |
      +-- Generate Receipt Number
      +-- Copy Order Data
      +-- Set Status = "Pending"
      +-- Set Owner = Current User
      |
      v
[Navigate to Payment Screen]
```

### EF Core Configuration

```csharp
public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(r => r.ReceiptNumber).IsUnique();

        builder.Property(r => r.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(r => r.Order)
            .WithMany(o => o.Receipts)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.WorkPeriod)
            .WithMany(wp => wp.Receipts)
            .HasForeignKey(r => r.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.OwnedReceipts)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.3-Receipt-Management]
- [Source: _bmad-output/architecture.md#Receipt-Entity]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- AC#1 (Receipt created with unique number): CreateReceiptFromOrderAsync generates receipt number with format R-yyyyMMdd-sequence
- AC#2 (Linked to order and work period): Receipt.OrderId and Receipt.WorkPeriodId are set from the source order
- AC#3 (Initial status Pending): Receipt.Status defaults to ReceiptStatus.Pending
- AC#4 (Assigned to owner): Receipt.OwnerId set to current session user via ISessionService.CurrentUserId
- AC#5 (Stores financial data): Receipt.Subtotal, TaxAmount, DiscountAmount, TotalAmount copied from order

### Implementation Details
- Updated Receipt entity: Added TableNumber, CustomerName, SettledByUserId, SettledByUser, ReceiptItems collection
- Created ReceiptItem entity class (ReceiptItem.cs) for line items
- Added ReceiptItemConfiguration to ReceiptConfiguration.cs with FK relationships
- Added SettledByUser navigation to ReceiptConfiguration (User.SettledReceipts collection)
- Updated User entity with SettledReceipts collection
- Created IReceiptService interface with CRUD and query methods
- Implemented ReceiptService with CreateReceiptFromOrderAsync, GetPendingReceiptsAsync, etc.
- Updated POSViewModel: Added IReceiptService injection, CurrentReceiptId, CurrentReceiptNumber properties
- Modified SubmitOrderAsync to create receipt after successful order printing
- Receipt creation is non-blocking (order submission continues if receipt fails)

### File List
- src/HospitalityPOS.Core/Entities/Receipt.cs (modified - added TableNumber, CustomerName, SettledByUserId, ReceiptItems)
- src/HospitalityPOS.Core/Entities/ReceiptItem.cs (new - line item entity)
- src/HospitalityPOS.Core/Entities/User.cs (modified - added SettledReceipts collection)
- src/HospitalityPOS.Core/Interfaces/IReceiptService.cs (new - service interface)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (modified - added ReceiptItems DbSet)
- src/HospitalityPOS.Infrastructure/Data/Configurations/ReceiptConfiguration.cs (modified - added ReceiptItemConfiguration)
- src/HospitalityPOS.Infrastructure/Services/ReceiptService.cs (new - service implementation)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered IReceiptService)
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (modified - receipt creation on order submit)
