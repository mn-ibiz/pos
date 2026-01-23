# feat: Implement Manager PO Review Dashboard

**Labels:** `enhancement` `frontend` `purchase-orders` `ux` `priority-high`

## Overview

Create a dedicated dashboard for managers to review, approve, edit, and send automatically generated purchase orders. This provides a streamlined workflow for handling draft POs created by the auto-generation system.

## Background

When `AutoSendPurchaseOrders` is disabled (the recommended default), POs are created as drafts requiring manager review. Managers need an efficient interface to:
1. See all pending POs at a glance
2. Review individual PO details and items
3. Modify quantities if needed
4. Approve and send, or reject/archive POs
5. Track approval statistics

## Requirements

### Dashboard Overview Panel

```
+------------------------------------------------------------------+
| PO REVIEW DASHBOARD                                               |
+------------------------------------------------------------------+
| PENDING REVIEW         | TODAY'S ACTIVITY      | THIS WEEK       |
|------------------------|------------------------|-----------------|
|    12                  |    5 Approved          |   23 Approved   |
|   Draft POs            |    2 Rejected          |    4 Rejected   |
|   $15,420 total        |    1 Modified          |    8 Pending    |
|                        |                        |                 |
| [Review All]           | [View History]         | [Export Report] |
+------------------------------------------------------------------+
```

### PO Review Queue

```
+------------------------------------------------------------------+
| PENDING PURCHASE ORDERS                                     [Filters]|
+------------------------------------------------------------------+
| Priority: [All v] | Supplier: [All v] | Sort: [Oldest First v]    |
+------------------------------------------------------------------+
|                                                                   |
| [!] CRITICAL - PO-20250123-001                                    |
| +---------------------------------------------------------------+ |
| | Supplier: ABC Distributors          Created: 10 min ago       | |
| | Items: 8 products                   Total: $2,450.00          | |
| | Reason: 3 products critical low stock                         | |
| |                                                                 | |
| | Quick Actions: [Approve & Send] [Review Details] [Reject]      | |
| +---------------------------------------------------------------+ |
|                                                                   |
| [!] HIGH - PO-20250123-002                                        |
| +---------------------------------------------------------------+ |
| | Supplier: XYZ Supplies              Created: 45 min ago       | |
| | Items: 15 products                  Total: $5,890.00          | |
| | Reason: Reorder level reached                                  | |
| |                                                                 | |
| | Quick Actions: [Approve & Send] [Review Details] [Reject]      | |
| +---------------------------------------------------------------+ |
|                                                                   |
| Showing 2 of 12 pending POs                      [Load More]      |
+------------------------------------------------------------------+
```

### Detailed PO Review Dialog

```
+------------------------------------------------------------------+
| REVIEW PURCHASE ORDER: PO-20250123-001                    [X Close]|
+------------------------------------------------------------------+
| Supplier: ABC Distributors                                        |
| Expected Delivery: January 26, 2025 (3 days lead time)           |
| Created: January 23, 2025 10:15 AM (Auto-generated)              |
+------------------------------------------------------------------+
| ITEMS                                                             |
+------------------------------------------------------------------+
| Product          | Current | Reorder | Suggested | Edit | Cost   |
|                  | Stock   | Point   | Qty       |      |        |
|------------------|---------|---------|-----------|------|--------|
| Widget A         |    5    |   20    |    25     | [25] | $250.00|
| Gadget B         |    0    |   15    |    20     | [20] | $400.00|
| Component C      |   12    |   50    |    50     | [50] | $500.00|
| Part D [CRITICAL]|    2    |   30    |    35     | [35] | $350.00|
+------------------------------------------------------------------+
| Subtotal: $2,300.00                                               |
| Tax (16%): $368.00                                                |
| TOTAL: $2,668.00                                                  |
+------------------------------------------------------------------+
| Notes to Supplier:                                                |
| [                                                               ] |
+------------------------------------------------------------------+
|                                                                   |
| [Archive]  [Reject with Reason]  [Save Changes]  [Approve & Send] |
+------------------------------------------------------------------+
```

### Bulk Actions

Allow selecting multiple POs for batch operations:

```
+------------------------------------------------------------------+
| [ ] Select All                                                    |
+------------------------------------------------------------------+
| [x] PO-20250123-001 - ABC Distributors - $2,450.00               |
| [x] PO-20250123-002 - ABC Distributors - $5,890.00               |
| [ ] PO-20250123-003 - XYZ Supplies - $1,200.00                   |
+------------------------------------------------------------------+
| 2 selected | Total: $8,340.00                                     |
|                                                                   |
| Bulk Actions: [Approve & Send All] [Merge Selected] [Archive All] |
+------------------------------------------------------------------+
```

### Merge POs Feature

When multiple POs exist for the same supplier, offer to merge:

```
+------------------------------------------------------------------+
| MERGE PURCHASE ORDERS                                             |
+------------------------------------------------------------------+
| You have selected 2 POs for the same supplier (ABC Distributors). |
|                                                                   |
| Merging will:                                                     |
| - Combine all items into a single PO                              |
| - Delete the original POs                                         |
| - Create PO-20250123-NEW with 23 items                            |
| - New total: $8,340.00                                            |
|                                                                   |
| [Cancel]                                        [Merge POs]       |
+------------------------------------------------------------------+
```

### ViewModel

```csharp
public class POReviewDashboardViewModel : ViewModelBase
{
    // Summary stats
    public int PendingCount { get; set; }
    public decimal PendingTotal { get; set; }
    public int ApprovedTodayCount { get; set; }
    public int RejectedTodayCount { get; set; }

    // PO List
    public ObservableCollection<PurchaseOrderReviewItem> PendingPOs { get; set; }
    public PurchaseOrderReviewItem? SelectedPO { get; set; }

    // Filters
    public ReorderPriority? PriorityFilter { get; set; }
    public int? SupplierFilter { get; set; }
    public string SortOrder { get; set; } = "OldestFirst";

    // Commands
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand<int> ApproveAndSendCommand { get; }
    public IAsyncRelayCommand<int> ReviewDetailsCommand { get; }
    public IAsyncRelayCommand<int> RejectCommand { get; }
    public IAsyncRelayCommand<int> ArchiveCommand { get; }
    public IAsyncRelayCommand ApproveSelectedCommand { get; }
    public IAsyncRelayCommand MergeSelectedCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
}

public class PurchaseOrderReviewItem
{
    public int Id { get; set; }
    public string PONumber { get; set; }
    public string SupplierName { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo => GetTimeAgo(CreatedAt);
    public ReorderPriority Priority { get; set; }
    public string PriorityReason { get; set; }
    public bool IsSelected { get; set; }
    public List<PurchaseOrderItemReview> Items { get; set; }
}
```

## Acceptance Criteria

### Dashboard Overview
- [ ] Shows count of pending POs
- [ ] Shows total value of pending POs
- [ ] Shows today's approval/rejection stats
- [ ] Shows this week's summary
- [ ] Refreshes automatically every 30 seconds
- [ ] Manual refresh button available

### PO Queue
- [ ] Lists all draft POs requiring review
- [ ] Shows priority indicator (Critical, High, Medium, Low)
- [ ] Shows supplier name, item count, total
- [ ] Shows creation time (relative: "10 min ago")
- [ ] Shows reason for auto-generation
- [ ] Sortable by priority, date, amount, supplier
- [ ] Filterable by priority, supplier
- [ ] Pagination or infinite scroll for large lists

### Individual PO Review
- [ ] Shows all PO details
- [ ] Shows each item with current stock, reorder point, suggested quantity
- [ ] Allows editing suggested quantities
- [ ] Highlights critical items (stock = 0 or very low)
- [ ] Shows calculated totals updating in real-time
- [ ] Allows adding notes for supplier
- [ ] Validate quantity changes (min 1, max reasonable limit)

### Actions
- [ ] **Approve & Send**: Changes status to Sent, emails supplier
- [ ] **Save Changes**: Updates quantities without sending
- [ ] **Reject**: Opens dialog for rejection reason, archives PO
- [ ] **Archive**: Moves to archive without rejection reason

### Bulk Operations
- [ ] Select multiple POs with checkboxes
- [ ] "Select All" option
- [ ] Shows selected count and total value
- [ ] "Approve & Send All" for batch approval
- [ ] "Archive All" for batch archive
- [ ] "Merge Selected" for same-supplier POs

### Merge Feature
- [ ] Only enabled when 2+ POs selected for same supplier
- [ ] Preview shows combined item count and total
- [ ] Creates new PO with combined items
- [ ] Deletes original POs
- [ ] Logs merge action for audit

### Access Control
- [ ] Only users with Manager or Admin role can access
- [ ] Actions logged with user ID

### Real-Time Updates
- [ ] New POs appear automatically (polling or SignalR)
- [ ] PO count badge updates on sidebar
- [ ] Toast notification for new high-priority POs

## Technical Notes

### Priority Calculation for Display

```csharp
public ReorderPriority GetHighestPriority(PurchaseOrder po)
{
    // Check related ReorderSuggestions for priority
    var suggestions = _context.ReorderSuggestions
        .Where(s => s.PurchaseOrderId == po.Id)
        .ToList();

    if (!suggestions.Any())
        return ReorderPriority.Low;

    return suggestions.Max(s => s.Priority);
}

public string GetPriorityReason(PurchaseOrder po)
{
    var criticalCount = GetCriticalItemCount(po);
    if (criticalCount > 0)
        return $"{criticalCount} products at critical low stock";

    var highCount = GetHighPriorityItemCount(po);
    if (highCount > 0)
        return $"{highCount} products below reorder point";

    return "Scheduled reorder";
}
```

### Merge POs Logic

```csharp
public async Task<PurchaseOrder> MergePurchaseOrdersAsync(List<int> poIds)
{
    var pos = await _context.PurchaseOrders
        .Include(p => p.Items)
        .Where(p => poIds.Contains(p.Id))
        .ToListAsync();

    // Validate all same supplier
    var suppliers = pos.Select(p => p.SupplierId).Distinct().ToList();
    if (suppliers.Count != 1)
        throw new InvalidOperationException("Can only merge POs from same supplier");

    // Create new PO
    var mergedPO = new PurchaseOrder
    {
        SupplierId = suppliers.First(),
        Status = PurchaseOrderStatus.Draft,
        OrderDate = DateTime.UtcNow,
        Items = new List<PurchaseOrderItem>()
    };

    // Combine items, merging same products
    var itemsByProduct = pos
        .SelectMany(p => p.Items)
        .GroupBy(i => i.ProductId)
        .Select(g => new PurchaseOrderItem
        {
            ProductId = g.Key,
            OrderedQuantity = g.Sum(i => i.OrderedQuantity),
            UnitCost = g.First().UnitCost,
            TotalCost = g.Sum(i => i.TotalCost)
        })
        .ToList();

    mergedPO.Items = itemsByProduct;
    mergedPO.PONumber = await GeneratePONumberAsync();

    // Calculate totals
    mergedPO.SubTotal = mergedPO.Items.Sum(i => i.TotalCost);
    mergedPO.TaxAmount = CalculateTax(mergedPO.SubTotal);
    mergedPO.TotalAmount = mergedPO.SubTotal + mergedPO.TaxAmount;

    // Save merged PO
    await _context.PurchaseOrders.AddAsync(mergedPO);

    // Delete original POs
    foreach (var po in pos)
    {
        po.IsDeleted = true;
        po.DeletedAt = DateTime.UtcNow;
        po.DeletionReason = $"Merged into {mergedPO.PONumber}";
    }

    await _context.SaveChangesAsync();

    return mergedPO;
}
```

### Navigation

Add to sidebar under "Purchase Orders":
```
- Purchase Orders
  - All POs
  - Review Queue (12)  <-- Badge with pending count
  - Suppliers
```

## Test Cases

1. **View pending POs** - All draft POs displayed correctly
2. **Filter by priority** - Only matching priority shown
3. **Sort by amount** - Correctly ordered
4. **Approve single PO** - Status changes, email sent
5. **Reject with reason** - PO archived, reason saved
6. **Edit quantities** - Totals recalculate, changes saved
7. **Approve all** - All selected POs processed
8. **Merge same supplier** - New combined PO created
9. **Merge different suppliers** - Should fail with error
10. **Real-time refresh** - New POs appear without manual refresh

## Dependencies
- Issue #001: IInventoryAnalyticsService (provides suggestions linked to POs)
- Issue #004: PO Consolidation (creates draft POs to review)
- Issue #006: Archive/Delete (for rejection workflow)

## Blocked By
- Issue #004: PO Consolidation by Supplier

## Blocks
- None

## Estimated Complexity
**High** - Full dashboard with multiple views, bulk actions, and real-time updates
