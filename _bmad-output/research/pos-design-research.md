# POS System Design Research

**Research Date:** December 2025
**Purpose:** Design insights from SambaPOS, Aronium, Floreant, QuickBooks POS, and industry best practices

---

## 1. Main Sales Screen Design

### 1.1 SambaPOS Screen Layout

**SambaPOS Actual Layout (From Screenshot):**
```
+---------------------------+----------------+----------------------------------+
| ORDER TICKET (Left)       | CATEGORIES     | PRODUCTS GRID (Right)            |
|                           | (Middle)       |                                  |
+---------------------------+----------------+----------------------------------+
| [SambaPOS Logo]           |                | [1] [2] [3] [4] [5]  <- Pages    |
| Change Table              |   [Pizza]      |                                  |
|                           |   [Pide]       | +--------+ +--------+ +--------+ |
| Table: Inside 01          |   [Burgers]    | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
| Status: New Orders        |   [Sandwich]*  | | Spicy  | | Turkey | |Chicken | |
|                           |   [Snacks]     | | Italian| | Breast | |& Bacon | |
| [Select Customer]         |   [Salads]     | +--------+ +--------+ +--------+ |
| [Ticket Note]             |   [Cakes]      |                                  |
| [Add Ticket]              |   [Dessert]    | +--------+ +--------+ +--------+ |
| [Print Bill]              |   [Junk Food]  | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
|                           |   [Coffee]     | |Meatball| |Italian | | Steak  | |
| ─────────────────────     |   [Iced Drinks]| |Marinara| | B.M.T. | |& Cheese| |
| 1 Meatball Marinara  7.50 |   [Drinks]     | +--------+ +--------+ +--------+ |
| 1 Coca Cola 33cl     9.50 |                |                                  |
| 1 Chocolate Souffle 14.50 |   (* = selected| +--------+ +--------+ +--------+ |
| ─────────────────────     |    highlighted | |[IMAGE] | |[IMAGE] | |[IMAGE] | |
|                           |    in green)   | | Ham    | |Chicken | | Tuna   | |
| Balance:            31.50 |                | |        | |Teriyaki| |        | |
|                           |                | +--------+ +--------+ +--------+ |
| [Cash]     [Credit Card]  |                |                                  |
|  orange       blue        |                | +--------+ +--------+            |
|                           |                | |[IMAGE] | |[IMAGE] |            |
| [Settle]     [Close]      |                | |Cheesy  | | Roast  |            |
|                           |                | |Pepper. | |Chicken |            |
+---------------------------+----------------+----------------------------------+
| [Keyboard] [Delivery] [Takeaway]           | Administrator  Main Menu         |
+--------------------------------------------+----------------------------------+
```

**Key Design Elements from SambaPOS Screenshot:**

| Element | Position | Details |
|---------|----------|---------|
| **Order Ticket** | LEFT | Items list, totals, payment & action buttons |
| **Categories** | MIDDLE | Vertical scrollable list, selected = green highlight |
| **Products Grid** | RIGHT | 3-4 columns, large images, pagination tabs (1-5) |
| **Payment Buttons** | Bottom-left | Color-coded: Cash=Orange, Card=Blue |
| **Table Info** | Top-left | Table name, status |
| **Pagination** | Top-right | Numbered tabs for product pages |

**Key SambaPOS Design Features:**

| Feature | Description |
|---------|-------------|
| **Fast Menu** | Fixed bar at top for frequently-sold items |
| **Category Column Width** | Configurable percentage (e.g., 33% of screen) |
| **Category Column Count** | Supports 1-2 columns for many categories |
| **Product Sorting** | Drag-and-drop within categories |
| **Product Images** | Configurable per product |
| **Auto Select** | Opens modifiers automatically on product tap |
| **Ticket Lister** | Customizable columns, widths, status indicators |

**Sources:**
- [SambaPOS Menu Design](https://kb.sambapos.com/en/2-3-8-a-how-to-design-detailed-menu/)
- [SambaPOS Layout Entity Screens](https://kb.sambapos.com/en/2-3-2-d-how-to-add-layout-entity-screens/)

---

### 1.2 Aronium POS Layout

**Dual Layout Support:**
- **Touch Screen Layout** - Large buttons, grid-based
- **Keyboard Layout** - Compact, keyboard-optimized

**Key Aronium Features:**

| Feature | Description |
|---------|-------------|
| **Simple Interface** | Clean, no complicated procedures |
| **Customer Display** | Shows items as they're added |
| **Analytics Dashboard** | Built-in sales insights |
| **Barcode + Touch** | Supports both input methods |

**Sources:**
- [Aronium Features](https://www.aronium.com/en/features)
- [Aronium Help Center](https://help.aronium.com/hc/en-us/articles/115001610232-Customer-display)

---

### 1.3 Floreant POS Touch Interface

**Steven Hoober's "Thumb Zone" Implementation:**

```
+------------------------------------------+
|                                          |
|     COMFORTABLE ZONE                     |  <- Primary actions here
|     (Bottom center)                      |
|                                          |
|  STRETCH ZONE        STRETCH ZONE        |  <- Secondary actions
|  (Bottom corners)    (Top areas)         |
|                                          |
+------------------------------------------+
```

**Key Floreant Design Principles:**

| Principle | Implementation |
|-----------|----------------|
| **Minimum Touch Target** | 6x6 mm for accurate finger interaction |
| **Spacing** | Adequate gaps between buttons |
| **No Tooltips** | Removed (useless on touch screens) |
| **No Hover Effects** | Direct activation on touch |
| **Clear Affordances** | Visual distinction for interactive elements |
| **Keyboard Integration** | Full keyboards for precise data entry |

**Sources:**
- [Floreant Touch Interface](https://floreant.org/features/touch-screen-interface/)

---

## 2. Auto-Logout Feature Design

### 2.1 Configurable Auto-Logout Options

Based on research from multiple POS systems, implement these three logout modes:

```
+------------------------------------------+
|        AUTO-LOGOUT SETTINGS              |
+------------------------------------------+
|                                          |
|  Enable Auto-Logout: [Toggle ON/OFF]     |
|                                          |
|  LOGOUT TRIGGER OPTIONS:                 |
|  ─────────────────────────────────────   |
|  (x) After Each Transaction              |
|      (Logout when receipt is settled)    |
|                                          |
|  ( ) After Inactivity Timeout            |
|      Timeout: [5 minutes___] [v]         |
|                                          |
|  ( ) After Both                          |
|                                          |
|  ADVANCED OPTIONS:                       |
|  ─────────────────────────────────────   |
|  [x] Show warning before timeout         |
|      Warning time: [30] seconds          |
|                                          |
|  [x] Allow "Stay Logged In" button       |
|                                          |
|  [x] Log out on ticket close             |
|      (For waiter accountability)         |
|                                          |
|  [Save Settings]                         |
+------------------------------------------+
```

### 2.2 Implementation Patterns

**After Transaction Logout (Square, Lightspeed, SambaPOS):**
```csharp
public class AutoLogoutService
{
    // Configuration options
    public bool EnableAutoLogout { get; set; }
    public AutoLogoutTrigger Trigger { get; set; }
    public int InactivityTimeoutMinutes { get; set; } = 5;
    public int WarningBeforeLogoutSeconds { get; set; } = 30;
    public bool AllowStayLoggedIn { get; set; } = true;

    // Triggered after payment is fully processed
    public async Task OnPaymentProcessedAsync(Payment payment)
    {
        if (!EnableAutoLogout) return;

        if (Trigger == AutoLogoutTrigger.AfterTransaction ||
            Trigger == AutoLogoutTrigger.Both)
        {
            // Small delay to allow receipt printing
            await Task.Delay(TimeSpan.FromSeconds(2));
            await LogoutCurrentUserAsync();
        }
    }

    // Triggered on inactivity
    public async Task OnInactivityTimeoutAsync()
    {
        if (!EnableAutoLogout) return;

        if (Trigger == AutoLogoutTrigger.AfterInactivity ||
            Trigger == AutoLogoutTrigger.Both)
        {
            if (AllowStayLoggedIn)
            {
                var stayLoggedIn = await ShowWarningDialogAsync();
                if (stayLoggedIn)
                {
                    ResetInactivityTimer();
                    return;
                }
            }
            await LogoutCurrentUserAsync();
        }
    }
}

public enum AutoLogoutTrigger
{
    AfterTransaction,
    AfterInactivity,
    Both,
    Disabled
}
```

**Inactivity Detection (Robotill, Impos):**
```csharp
public class InactivityMonitor
{
    private DateTime _lastActivityTime;
    private Timer _inactivityTimer;

    // Track all user inputs
    public void OnUserActivity()
    {
        _lastActivityTime = DateTime.Now;
    }

    // Check periodically
    private async void CheckInactivity(object? state)
    {
        var inactiveMinutes = (DateTime.Now - _lastActivityTime).TotalMinutes;

        if (inactiveMinutes >= _settings.InactivityTimeoutMinutes)
        {
            await _autoLogoutService.OnInactivityTimeoutAsync();
        }
        else if (inactiveMinutes >= _settings.InactivityTimeoutMinutes -
                 (_settings.WarningBeforeLogoutSeconds / 60.0))
        {
            await ShowTimeoutWarningAsync();
        }
    }
}
```

**Key Implementation Notes (from SambaPOS forum):**

| Issue | Solution |
|-------|----------|
| Ticket shows * for user after logout | Add 2-second delay before logout |
| Message box conflicts with logout | Use Ticket Closed rule instead of Payment Processed |
| Fast logout interrupts workflow | Create custom Logout button action |

**Sources:**
- [SambaPOS Auto Logout Discussion](https://forum.sambapos.com/t/auto-log-out-waiters-users/27396)
- [SambaPOS Auto Logout After Transaction](https://forum.sambapos.com/t/auto-logout-after-every-tranasaction-change-due-message-box/2086)
- [Cloud Retailer Auto Logout](https://helpdesk.cloudretailer.com/support/solutions/articles/67000669411-pos-auto-logout-time-force-logout-after-each-transaction)
- [Lightspeed Auto Sign Out](https://shopkeep-support.lightspeedhq.com/support/advanced/auto-sign-out)

---

## 3. Fast User Switching & Security

### 3.1 PIN Login Design

```
+------------------------------------------+
|           STAFF LOGIN                     |
+------------------------------------------+
|                                           |
|   [John]  [Mary]  [Peter]  [Admin]        |
|                                           |
|   Enter PIN:                              |
|   +---+---+---+---+---+---+              |
|   | * | * | * | * | _ | _ |              |
|   +---+---+---+---+---+---+              |
|                                           |
|   +---+  +---+  +---+                    |
|   | 1 |  | 2 |  | 3 |                    |
|   +---+  +---+  +---+                    |
|   +---+  +---+  +---+                    |
|   | 4 |  | 5 |  | 6 |                    |
|   +---+  +---+  +---+                    |
|   +---+  +---+  +---+                    |
|   | 7 |  | 8 |  | 9 |                    |
|   +---+  +---+  +---+                    |
|   +---+  +---+  +---+                    |
|   |CLR|  | 0 |  | < |                    |
|   +---+  +---+  +---+                    |
|                                           |
|   [ ] Remember me on this device          |
|                                           |
+------------------------------------------+
```

### 3.2 Waiter Accountability Features

| Feature | Purpose | Implementation |
|---------|---------|----------------|
| **Unique PIN per User** | Links every action to a person | 4-6 digit numeric |
| **Role-Based Access** | Limits high-risk actions | Cashier, Supervisor, Manager, Admin |
| **Action Logging** | Complete audit trail | Every void, discount, override logged |
| **Ticket Ownership** | Tracks who created order | User ID stored with order |
| **Own Tickets Only** | Prevents accessing others' orders | Filter tickets by current user |
| **Shared Drawer Prevention** | Reduces cash loss by 50%+ | Drawer assigned to user |

### 3.3 Security Statistics

| Metric | Impact |
|--------|--------|
| Internal theft (retail shrink) | 29% of losses |
| Multi-user tracking reduction | Up to 30% less internal theft |
| Eliminating shared drawers | 50%+ reduction in cash loss |

**Sources:**
- [Multi-User POS Systems](https://www.szzcs.com/News/how-a-multi-user-pos-system-streamlines-cashier-operations-across-retail-chains.html)
- [POS Fraud Prevention](https://www.onehubpos.com/blog/pos-fraud-prevention-how-to-stop-employee-theft-before-profits-disappear)
- [POS Employee Theft Prevention](https://goftx.com/blog/how-pos-system-improve-employee-theft-prevention/)

---

## 4. Touch Screen UI/UX Best Practices

### 4.1 Button Size & Touch Targets

| Recommendation | Value |
|----------------|-------|
| **Minimum touch target** | 6x6 mm (6mm = ~23px at 96 DPI) |
| **Recommended touch target** | 44x44 pixels (Apple HIG) |
| **Spacing between buttons** | Minimum 8px gap |
| **Frequent action buttons** | Larger and more prominent |
| **Less common buttons** | Smaller, less prominent |

### 4.2 Color & Contrast

| Element | Guideline |
|---------|-----------|
| **Action buttons** | High contrast accent color |
| **Destructive actions** | Red/warning color |
| **Confirm/Positive** | Green/success color |
| **Text on buttons** | High contrast with background |
| **Colorblind safe** | Don't rely only on color |

### 4.3 Layout Principles

**Grid-Based Design (Microsoft Dynamics):**
- Uses 4-pixel snap grid for alignment
- Supports both centered and right-aligned layouts
- Receipt panel: configurable columns and widths
- Totals panel: 1 or 2 column modes

**Progressive Disclosure:**
- Show only essential information initially
- Expand details on demand
- Reduce cognitive load

**Key Design Rules:**

| Rule | Rationale |
|------|-----------|
| No tooltips | Useless on touch screens |
| No hover effects | Direct activation only |
| Clear affordances | Buttons look like buttons |
| Minimal animations | Only for feedback, not decoration |
| Reduce cognitive load | Staff under stress forget easily |

**Sources:**
- [POS Design Principles (Agente Studio)](https://agentestudio.com/blog/design-principles-pos-interface)
- [Shopify POS UI](https://www.shopify.com/retail/pos-ui)
- [Bright Inventions Payment UI/UX](https://brightinventions.pl/blog/payment-point-of-sale-design-ui-ux/)
- [Microsoft Dynamics POS Layouts](https://learn.microsoft.com/en-us/dynamics365/commerce/pos-screen-layouts)

---

## 5. Inventory/Items List Page Design

### 5.1 QuickBooks POS Style Items List

```
+------------------------------------------------------------------+
|  INVENTORY MANAGEMENT                          [+ Add Item]       |
+------------------------------------------------------------------+
|  Search: [________________________] [Scan Barcode]               |
|                                                                   |
|  Filters: [All Categories v] [In Stock v] [Active v]   [Clear]  |
+------------------------------------------------------------------+
|  [Grid View] [List View]                    Sort: [Name v]        |
+------------------------------------------------------------------+
|                                                                   |
|  +-------------+  +-------------+  +-------------+  +----------+  |
|  | [Image]     |  | [Image]     |  | [Image]     |  | [Image]  |  |
|  | Tusker      |  | Heineken    |  | Soda 500ml  |  | Juice    |  |
|  | KSh 350     |  | KSh 400     |  | KSh 50      |  | KSh 250  |  |
|  | Stock: 48   |  | Stock: 24   |  | Stock: 120  |  | Stock: 5 |  |
|  | [!] Low     |  |             |  |             |  | [!] Low  |  |
|  +-------------+  +-------------+  +-------------+  +----------+  |
|                                                                   |
|  +-------------+  +-------------+  +-------------+  +----------+  |
|  | [Image]     |  | [Image]     |  | [Image]     |  | [Image]  |  |
|  | Chicken     |  | Fish Fillet |  | Chips       |  | Rice     |  |
|  | KSh 850     |  | KSh 950     |  | KSh 150     |  | KSh 100  |  |
|  | Stock: 15   |  | Stock: 8    |  | Stock: 200  |  | Stock: 0 |  |
|  |             |  |             |  |             |  | [X] Out  |  |
|  +-------------+  +-------------+  +-------------+  +----------+  |
|                                                                   |
+------------------------------------------------------------------+
|  Showing 1-8 of 156 items          [<] [1] [2] [3] ... [20] [>]  |
+------------------------------------------------------------------+
```

### 5.2 List View Alternative

```
+------------------------------------------------------------------+
|  [Grid View] [List View *]                  Sort: [Name v]        |
+------------------------------------------------------------------+
| Code    | Product Name     | Category | Price   | Stock | Status |
|---------|------------------|----------|---------|-------|--------|
| TUS001  | Tusker Lager     | Drinks   | 350.00  | 48    | [!]Low |
| HEI001  | Heineken         | Drinks   | 400.00  | 24    | OK     |
| SOD001  | Soda 500ml       | Drinks   | 50.00   | 120   | OK     |
| JUI001  | Fresh Juice      | Drinks   | 250.00  | 5     | [!]Low |
| CHI001  | Grilled Chicken  | Food     | 850.00  | 15    | OK     |
| FIS001  | Fish Fillet      | Food     | 950.00  | 8     | OK     |
| CHP001  | Chips Regular    | Food     | 150.00  | 200   | OK     |
| RIC001  | Plain Rice       | Food     | 100.00  | 0     | [X]Out |
+------------------------------------------------------------------+
|  [Edit] [Duplicate] [Delete]  Selected: 0 items                   |
+------------------------------------------------------------------+
```

### 5.3 Key Inventory UI Elements

| Element | Purpose |
|---------|---------|
| **Grid/List Toggle** | User preference for display mode |
| **Search Bar** | Quick product lookup |
| **Barcode Scan Button** | Hardware integration |
| **Category Filter** | Narrow down products |
| **Stock Status Filter** | Find low/out-of-stock items |
| **Color-Coded Status** | Visual alerts (red for out, yellow for low) |
| **Bulk Actions** | Edit, delete, adjust multiple items |
| **Pagination** | Handle large product catalogs |
| **Quick Edit** | Inline editing for fast updates |

### 5.4 Visual Cues for Stock Status

```csharp
public enum StockStatus
{
    InStock,      // Green or no indicator
    LowStock,     // Yellow/Orange warning
    OutOfStock,   // Red indicator
    Discontinued  // Gray, strikethrough
}

// UI color mapping
public static Color GetStockStatusColor(Product product)
{
    if (!product.TrackInventory) return Colors.Gray;
    if (product.CurrentStock <= 0) return Colors.Red;
    if (product.CurrentStock <= product.MinStockLevel) return Colors.Orange;
    return Colors.Green;
}
```

**Sources:**
- [QuickBooks POS Inventory](https://www.connectpos.com/quickbooks-point-of-sale-inventory-management/)
- [Inventory App Design (UXPin)](https://www.uxpin.com/studio/blog/inventory-app-design/)
- [Inventory UI Design (Design Peeps)](https://blog.designpeeps.net/blog/user-interface-design-for-inventory-management-system/)

---

## 6. Recommended Design Updates for HospitalityPOS

### 6.1 Main Sales Screen Layout

**Recommended Three-Panel Layout:**

```
+------------------------------------------------------------------+
|  [Logo]  Current User: John  |  Table: 12  |  [Logout] [Settings] |
+------------------------------------------------------------------+
|          |                                    |                   |
| FAST     |        PRODUCT GRID                |    ORDER          |
| MENU     |        (Touch Grid)                |    TICKET         |
| (Top)    |                                    |                   |
|----------|   +------+  +------+  +------+     |    Item 1   350   |
|          |   |Tusker|  |Soda  |  |Juice |     |    Item 2   850   |
| CATEGORIES   +------+  +------+  +------+     |    Item 3   150   |
| (Left)   |                                    |                   |
|          |   +------+  +------+  +------+     |----------------- |
| [Drinks] |   |Chicken| |Fish  |  |Steak |     |  Subtotal: 1,350  |
| [Food]   |   +------+  +------+  +------+     |  Tax (16%):  216  |
| [Dessert]|                                    |  TOTAL:    1,566  |
| [Special]|   +------+  +------+  +------+     |                   |
|          |   |Chips |  |Rice  |  |Salad |     | [SETTLE] [PRINT]  |
|          |   +------+  +------+  +------+     | [HOLD]   [VOID]   |
+----------+------------------------------------+-------------------+
```

### 6.2 Auto-Logout Configuration

**Settings Screen:**

```csharp
public class SessionSecuritySettings
{
    // Master toggle
    public bool EnableAutoLogout { get; set; } = true;

    // Logout triggers
    public bool LogoutAfterTransaction { get; set; } = true;
    public bool LogoutAfterInactivity { get; set; } = true;

    // Timing
    public int InactivityTimeoutMinutes { get; set; } = 5;
    public int WarningBeforeLogoutSeconds { get; set; } = 30;

    // UI options
    public bool ShowTimeoutWarning { get; set; } = true;
    public bool AllowStayLoggedIn { get; set; } = true;

    // Waiter-specific
    public bool EnforceOwnTicketsOnly { get; set; } = true;
    public bool RequirePinForVoid { get; set; } = true;
    public bool RequirePinForDiscount { get; set; } = true;
}
```

### 6.3 Touch Screen Button Sizing

**Recommended Sizes:**

| Button Type | Minimum Size | Recommended Size |
|-------------|-------------|------------------|
| Product buttons | 60x60 px | 80x80 px |
| Action buttons | 44x44 px | 60x44 px |
| Category buttons | 44x60 px | Full width x 60 px |
| Number pad | 50x50 px | 70x70 px |
| Settlement buttons | 80x60 px | 100x80 px |

### 6.4 Items List Page Design

**Key Features to Implement:**

1. **View Toggle**: Grid view (images) / List view (table)
2. **Search**: Real-time search with barcode support
3. **Filters**: Category, stock status, active/inactive
4. **Color Coding**: Red (out), Orange (low), Green (ok)
5. **Bulk Actions**: Edit, adjust, activate/deactivate
6. **Quick Stats**: Total items, low stock count, out of stock count
7. **Pagination**: Handle large catalogs efficiently

---

## 7. Summary of Key Findings

### Design Priorities

1. **Touch-First**: All buttons 44px+ minimum
2. **Three-Panel Layout**: Categories | Products | Order Ticket
3. **Fast Menu**: Popular items always visible
4. **Clear Visual Hierarchy**: Primary actions larger and colored
5. **Minimal Cognitive Load**: Simple, uncluttered interface

### Auto-Logout Must-Haves

1. **Toggle On/Off**: Global enable/disable
2. **After Transaction**: Force login after each sale
3. **After Inactivity**: Configurable timeout (1-30 minutes)
4. **Warning Dialog**: With countdown and "Stay Logged In"
5. **Audit Trail**: Log all logout events

### Security Essentials

1. **Unique PIN per User**: 4-6 digits
2. **Role-Based Permissions**: Limit sensitive actions
3. **Own Tickets Only**: Waiter can only see their orders
4. **Action Logging**: Every void, discount, override
5. **Cash Drawer Assignment**: One user per drawer
