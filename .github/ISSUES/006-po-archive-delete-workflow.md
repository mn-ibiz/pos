# feat: Add PO Archive and Delete Workflow

**Labels:** `enhancement` `backend` `frontend` `purchase-orders` `priority-medium`

## Overview

Implement functionality for managers to archive or delete purchase orders they don't intend to send. This includes adding an "Archived" status, soft delete capability, and UI for managing archived POs.

## Background

Currently, the `PurchaseOrderStatus` enum has:
- Draft (1)
- Sent (2)
- PartiallyReceived (3)
- Complete (4)
- Cancelled (5)

Managers need the ability to:
1. **Archive** POs they've decided not to send (preserves record)
2. **Delete** draft POs entirely (removes from system)
3. **View** archived POs for reference
4. **Restore** archived POs if needed

## Requirements

### Status Enum Update

```csharp
public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Complete = 4,
    Cancelled = 5,
    Archived = 6  // NEW
}
```

### Soft Delete Support

Add to `PurchaseOrder` entity:
```csharp
public class PurchaseOrder
{
    // Existing properties...

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }
    public string? DeletionReason { get; set; }

    // Archive tracking
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedByUserId { get; set; }
    public string? ArchiveReason { get; set; }
}
```

### Service Methods

Add to `IPurchaseOrderService`:
```csharp
public interface IPurchaseOrderService
{
    // Existing methods...

    /// <summary>
    /// Archive a PO - moves to Archived status but preserves record
    /// </summary>
    Task<bool> ArchivePurchaseOrderAsync(int poId, int userId, string? reason = null);

    /// <summary>
    /// Restore an archived PO back to Draft status
    /// </summary>
    Task<bool> RestorePurchaseOrderAsync(int poId, int userId);

    /// <summary>
    /// Soft delete a PO - only allowed for Draft and Archived status
    /// </summary>
    Task<bool> DeletePurchaseOrderAsync(int poId, int userId, string? reason = null);

    /// <summary>
    /// Permanently delete (hard delete) - admin only, for cleanup
    /// </summary>
    Task<bool> PermanentlyDeletePurchaseOrderAsync(int poId);

    /// <summary>
    /// Get archived POs
    /// </summary>
    Task<List<PurchaseOrder>> GetArchivedPurchaseOrdersAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get deleted POs (for admin recovery)
    /// </summary>
    Task<List<PurchaseOrder>> GetDeletedPurchaseOrdersAsync();

    /// <summary>
    /// Recover a soft-deleted PO
    /// </summary>
    Task<bool> RecoverPurchaseOrderAsync(int poId, int userId);
}
```

### Business Rules

| Current Status | Can Archive | Can Delete | Can Restore |
|---------------|-------------|------------|-------------|
| Draft | Yes | Yes | N/A |
| Sent | No | No | N/A |
| PartiallyReceived | No | No | N/A |
| Complete | Yes | No | N/A |
| Cancelled | Yes | Yes | N/A |
| Archived | N/A | Yes | Yes |
| Deleted (soft) | N/A | N/A | Yes (admin) |

### UI Updates

#### Archive Button in PO List

Add context menu or button:
```
[Archive] - For Draft/Complete/Cancelled POs
[Delete] - For Draft/Archived/Cancelled POs
```

#### Archive Dialog

```
+------------------------------------------+
| ARCHIVE PURCHASE ORDER                    |
+------------------------------------------+
|                                          |
| Are you sure you want to archive         |
| PO-20250123-001?                         |
|                                          |
| This PO will be moved to the archive and |
| will no longer appear in the main list.  |
| You can restore it later if needed.      |
|                                          |
| Reason (optional):                       |
| [________________________________]       |
|                                          |
| [Cancel]              [Archive PO]       |
+------------------------------------------+
```

#### Delete Confirmation Dialog

```
+------------------------------------------+
| DELETE PURCHASE ORDER                     |
+------------------------------------------+
|                                          |
| Are you sure you want to delete          |
| PO-20250123-001?                         |
|                                          |
| [!] This action cannot be easily undone. |
|                                          |
| Reason (required):                       |
| [________________________________]       |
|                                          |
| [Cancel]              [Delete PO]        |
+------------------------------------------+
```

#### Archived POs View

Add a tab or filter to view archived POs:

```
+------------------------------------------+
| PURCHASE ORDERS                           |
+------------------------------------------+
| [All] [Draft] [Sent] [Received] [Archived]|
+------------------------------------------+
| Status filter: Archived                   |
+------------------------------------------+
| PO#        | Supplier | Total | Archived |
|------------|----------|-------|----------|
| PO-...-001 | ABC Ltd  | $500  | Jan 20   |
| PO-...-002 | XYZ Inc  | $300  | Jan 15   |
+------------------------------------------+
| Selected: PO-...-001                      |
| [View] [Restore] [Delete Permanently]     |
+------------------------------------------+
```

## Acceptance Criteria

### Archive Functionality
- [ ] `Archived` status added to enum
- [ ] `ArchivePurchaseOrderAsync` method implemented
- [ ] Only Draft, Complete, and Cancelled POs can be archived
- [ ] Archived POs have `ArchivedAt`, `ArchivedByUserId`, and optional `ArchiveReason`
- [ ] Archived POs don't appear in default PO list
- [ ] Archived POs visible in dedicated archive view/filter
- [ ] Archive count shown on Archive tab

### Restore Functionality
- [ ] `RestorePurchaseOrderAsync` method implemented
- [ ] Restored POs return to Draft status
- [ ] Restore clears archive fields but logs the action
- [ ] Restored POs appear in main PO list

### Delete Functionality
- [ ] Soft delete fields added to entity
- [ ] `DeletePurchaseOrderAsync` method implemented (soft delete)
- [ ] Only Draft, Archived, and Cancelled POs can be deleted
- [ ] Delete requires a reason
- [ ] Deleted POs don't appear anywhere in normal UI
- [ ] Admin can view/recover deleted POs
- [ ] `PermanentlyDeletePurchaseOrderAsync` for hard delete (admin only)

### Global Query Filter
- [ ] EF Core global query filter excludes soft-deleted records:
```csharp
modelBuilder.Entity<PurchaseOrder>()
    .HasQueryFilter(po => !po.IsDeleted);
```
- [ ] Filter can be bypassed for admin recovery views

### UI
- [ ] Archive button visible for eligible POs
- [ ] Delete button visible for eligible POs
- [ ] Confirmation dialogs for both actions
- [ ] Archive tab/filter in PO list
- [ ] Restore button in archive view
- [ ] Admin-only delete recovery view

### Audit Trail
- [ ] Archive action logged
- [ ] Restore action logged
- [ ] Delete action logged with reason
- [ ] Permanent delete logged

## Technical Notes

### Global Query Filter

```csharp
// In ApplicationDbContext.OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<PurchaseOrder>()
        .HasQueryFilter(po => !po.IsDeleted);

    // To bypass filter when needed:
    // _context.PurchaseOrders.IgnoreQueryFilters().Where(...)
}
```

### Archive Flow

```csharp
public async Task<bool> ArchivePurchaseOrderAsync(int poId, int userId, string? reason)
{
    var po = await _context.PurchaseOrders.FindAsync(poId);
    if (po == null) return false;

    // Validate status
    if (po.Status != PurchaseOrderStatus.Draft &&
        po.Status != PurchaseOrderStatus.Complete &&
        po.Status != PurchaseOrderStatus.Cancelled)
    {
        throw new InvalidOperationException(
            $"Cannot archive PO with status {po.Status}");
    }

    po.Status = PurchaseOrderStatus.Archived;
    po.ArchivedAt = DateTime.UtcNow;
    po.ArchivedByUserId = userId;
    po.ArchiveReason = reason;

    await _context.SaveChangesAsync();

    _logger.LogInformation(
        "PO {PONumber} archived by user {UserId}. Reason: {Reason}",
        po.PONumber, userId, reason ?? "Not specified");

    return true;
}
```

### Migration

```csharp
public partial class AddPOArchiveAndDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "PurchaseOrders",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeletedAt",
            table: "PurchaseOrders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DeletedByUserId",
            table: "PurchaseOrders",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeletionReason",
            table: "PurchaseOrders",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ArchivedAt",
            table: "PurchaseOrders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ArchivedByUserId",
            table: "PurchaseOrders",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ArchiveReason",
            table: "PurchaseOrders",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        // Add index for soft delete queries
        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_IsDeleted",
            table: "PurchaseOrders",
            column: "IsDeleted");
    }
}
```

## Test Cases

1. **Archive draft PO** - Status changes to Archived, fields populated
2. **Archive sent PO** - Should fail with error
3. **Archive partially received PO** - Should fail
4. **Restore archived PO** - Returns to Draft, archive fields cleared
5. **Delete draft PO** - Soft deleted, not visible in UI
6. **Delete sent PO** - Should fail
7. **Recover deleted PO** - Returns to Draft, delete fields cleared
8. **Filter by archived** - Only archived POs shown
9. **Default list** - Excludes archived and deleted POs
10. **Permanent delete** - Record completely removed from database

## Dependencies
- None

## Blocked By
- None

## Blocks
- None

## Estimated Complexity
**Low-Medium** - Straightforward status management with UI updates
