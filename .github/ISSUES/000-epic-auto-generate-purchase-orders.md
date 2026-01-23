# Epic: Auto-Generate Purchase Orders to Suppliers

**Labels:** `epic` `enhancement` `purchase-orders` `inventory` `priority-high`

## Overview

Implement a comprehensive automatic purchase order generation system that monitors inventory levels, generates POs when stock falls below reorder points, consolidates orders by supplier, and provides robust notification and review workflows.

## Business Value

- **Prevent Stockouts**: Automatically detect low stock before it becomes critical
- **Reduce Manual Work**: Eliminate repetitive PO creation for recurring orders
- **Optimize Purchasing**: Consolidate orders by supplier to reduce PO count and potentially qualify for volume discounts
- **Improve Visibility**: Real-time notifications and dashboards keep managers informed
- **Maintain Control**: Draft mode allows manager review before sending to suppliers

## Current State Analysis

The codebase has foundational elements but missing key implementations:

| Component | Status | Notes |
|-----------|--------|-------|
| PurchaseOrder Entity | ✅ Ready | Full entity with statuses |
| Supplier Entity | ✅ Ready | Linked to products |
| ReorderRule Entity | ✅ Ready | Schema exists, no service |
| ReorderSuggestion Entity | ✅ Ready | Schema exists, no service |
| IInventoryAnalyticsService | ⚠️ Interface Only | No implementation |
| Background Job | ❌ Missing | No stock monitoring |
| PO Settings | ❌ Missing | No auto-generation config |
| Notifications | ❌ Missing | No in-app or email |
| Archive Workflow | ❌ Missing | Cannot archive POs |

## Feature Breakdown

### Phase 1: Core Services (Foundation)
| # | Issue | Priority | Complexity | Dependencies |
|---|-------|----------|------------|--------------|
| 1 | [Implement IInventoryAnalyticsService](001-implement-inventory-analytics-service.md) | High | Medium-High | None |
| 3 | [Auto-Generate PO System Configuration](003-auto-po-system-configuration.md) | High | Low-Medium | None |

### Phase 2: Automation & Processing
| # | Issue | Priority | Complexity | Dependencies |
|---|-------|----------|------------|--------------|
| 2 | [Stock Monitoring Background Service](002-stock-monitoring-background-service.md) | High | Medium | #1, #3 |
| 4 | [PO Consolidation by Supplier](004-po-consolidation-by-supplier.md) | High | Medium | #1, #3 |

### Phase 3: User Experience
| # | Issue | Priority | Complexity | Dependencies |
|---|-------|----------|------------|--------------|
| 5 | [In-App Notification System](005-in-app-notification-system.md) | High | Medium-High | #3 |
| 6 | [PO Archive/Delete Workflow](006-po-archive-delete-workflow.md) | Medium | Low-Medium | None |
| 7 | [Manager PO Review Dashboard](007-manager-po-review-dashboard.md) | High | High | #4, #6 |

### Phase 4: Communication
| # | Issue | Priority | Complexity | Dependencies |
|---|-------|----------|------------|--------------|
| 8 | [Email Notification Digest](008-email-notification-digest.md) | Medium | Medium | #3, #5 |

## Dependency Graph

```
                    ┌──────────────┐
                    │   #3 Config  │
                    │   Settings   │
                    └──────┬───────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ▼                 ▼                 ▼
┌────────────────┐  ┌──────────────┐  ┌──────────────┐
│ #1 Inventory   │  │ #5 In-App    │  │ #6 Archive   │
│ Analytics Svc  │  │ Notifications│  │ Workflow     │
└───────┬────────┘  └──────┬───────┘  └──────┬───────┘
        │                  │                 │
        ▼                  │                 │
┌────────────────┐         │                 │
│ #2 Background  │         │                 │
│ Stock Monitor  │         │                 │
└───────┬────────┘         │                 │
        │                  │                 │
        ▼                  │                 │
┌────────────────┐         │                 │
│ #4 PO          │         │                 │
│ Consolidation  │         │                 │
└───────┬────────┘         │                 │
        │                  │                 │
        └──────────┬───────┴─────────────────┘
                   │
                   ▼
          ┌────────────────┐
          │ #7 Manager     │
          │ Review Dashboard│
          └───────┬────────┘
                  │
                  ▼
          ┌────────────────┐
          │ #8 Email       │
          │ Digest         │
          └────────────────┘
```

## Implementation Order (Recommended)

### Sprint 1: Foundation
1. **Issue #3**: System Configuration Settings
2. **Issue #1**: IInventoryAnalyticsService Implementation

### Sprint 2: Core Automation
3. **Issue #4**: PO Consolidation by Supplier
4. **Issue #2**: Stock Monitoring Background Service

### Sprint 3: User Experience
5. **Issue #6**: PO Archive/Delete Workflow
6. **Issue #5**: In-App Notification System
7. **Issue #7**: Manager PO Review Dashboard

### Sprint 4: Polish & Communication
8. **Issue #8**: Email Notification Digest
9. Testing & Bug Fixes
10. Documentation

## End-to-End User Flow

### Automatic PO Generation (Happy Path)

```
1. Stock Monitoring Job runs every 15 minutes
   └─► Calls IInventoryAnalyticsService.GenerateReorderSuggestionsAsync()

2. Service checks all products with TrackInventory = true
   └─► Compares CurrentStock vs ReorderPoint
   └─► Creates ReorderSuggestion for each low-stock product

3. Suggestions are grouped by supplier
   └─► PO Consolidation Service groups suggestions
   └─► Creates one PO per supplier

4. Based on settings:
   ├─► If AutoSendPurchaseOrders = true AND total < AutoApprovalThreshold
   │   └─► PO status = Sent, email sent to supplier
   │
   └─► If AutoSendPurchaseOrders = false OR total >= AutoApprovalThreshold
       └─► PO status = Draft

5. Notification sent to managers
   └─► In-app toast: "5 new POs require review"
   └─► Badge updated on sidebar

6. Manager opens Review Dashboard
   └─► Reviews pending POs
   └─► Can modify quantities
   └─► Approves, Rejects, or Archives each PO

7. Approved POs sent to suppliers
   └─► Status = Sent
   └─► Email sent with PO details

8. Daily digest email sent
   └─► Summary of pending POs
   └─► Low stock alerts
   └─► Activity summary
```

## Acceptance Criteria for Epic

### Minimum Viable Product (MVP)
- [ ] Products below reorder point automatically generate POs
- [ ] POs are grouped by supplier (one PO per supplier)
- [ ] Managers can review draft POs before sending
- [ ] Basic notification when POs are generated

### Full Feature Set
- [ ] Configurable auto-send vs draft mode
- [ ] Approval threshold for auto-send
- [ ] In-app toast notifications
- [ ] Notification center with history
- [ ] Email digests (daily/weekly)
- [ ] PO archive and delete workflow
- [ ] Manager review dashboard with bulk actions
- [ ] PO merge functionality
- [ ] Comprehensive logging and audit trail

## Configuration Options Summary

| Setting | Default | Description |
|---------|---------|-------------|
| AutoGeneratePurchaseOrders | false | Enable automatic PO creation |
| AutoSendPurchaseOrders | false | Send POs immediately vs keep as draft |
| AutoApprovalThreshold | 0 | Auto-approve POs under this amount |
| StockCheckIntervalMinutes | 15 | How often to check stock levels |
| ConsolidatePOsBySupplier | true | Group items into single PO per supplier |
| NotifyOnPOGenerated | true | Show notification when PO created |
| EnableDailyDigest | true | Send daily email summary |
| MaxItemsPerPO | 50 | Split large orders into multiple POs |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Over-ordering due to incorrect reorder points | High | Require manager review (draft mode by default) |
| Duplicate POs generated | Medium | Check for existing pending suggestions before creating |
| Email spam from too many notifications | Low | Batch notifications, configurable settings |
| Performance impact from frequent stock checks | Medium | Configurable interval, batch processing |
| Managers overwhelmed by review queue | Medium | Priority sorting, bulk actions, auto-approve threshold |

## Success Metrics

- **Stockout Reduction**: Track products that reach zero stock (should decrease)
- **PO Creation Time**: Measure time from low stock detection to PO creation
- **Review Time**: Average time POs spend in draft status
- **Manager Efficiency**: Number of POs reviewed per session
- **Email Engagement**: Open rates for digest emails

## Related Documentation

- [Reorder Point Calculation Best Practices](https://www.bluelinkerp.com/blog/blue-links-data-driven-inventory-software/)
- [Purchase Order Workflow Guide](https://ziphq.com/blog/purchase-order-approval-workflow)
- [Order Consolidation Strategies](https://parabola.io/processes/the-complete-guide-to-order-consolidation)

## Notes

This epic transforms the POS system from reactive (manual PO creation) to proactive (automatic stock monitoring and PO generation). The phased approach ensures core functionality is delivered first while allowing for iterative improvements.

---

**Total Estimated Effort**: 4-6 Sprints
**Recommended Team**: 1-2 Backend Developers, 1 Frontend Developer
**Tech Stack Impact**: Background services, notification infrastructure, email templates
