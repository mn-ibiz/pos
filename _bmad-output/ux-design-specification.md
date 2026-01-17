---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]
inputDocuments:
  - docs/PRD_Unified_POS_System.md
  - _bmad-output/architecture.md
  - _bmad-output/research/pos-design-research.md
  - _bmad-output/epics.md
workflowType: 'ux-design'
lastStep: 14
project_name: 'POS'
user_name: 'Linuxlab'
date: '2025-12-28'
status: 'complete'
---

# UX Design Specification POS

**Author:** Linuxlab
**Date:** 2025-12-28

---

## Executive Summary

### Project Vision

Create a unified POS platform for Kenya & East Africa that serves both hospitality and retail businesses from a single codebase, with mode-specific interfaces that feel native to each industry while sharing core infrastructure.

### Target Users

| User Type | Primary Mode | Key Characteristics |
|-----------|--------------|---------------------|
| **Cashiers** | Retail | Speed-focused, barcode scanning, minimal touches per transaction |
| **Waiters/Servers** | Hospitality | Touch-oriented, table management, modifier selection |
| **Kitchen Staff** | Hospitality | KDS viewing, order status updates, hands potentially occupied |
| **Managers** | Both | Reporting, authorizations, oversight |
| **Administrators** | Both | System configuration, user management |

### Design Direction

**Hospitality Mode: SambaPOS v5 Paradigm**
- Three-panel touch interface (Ticket | Categories | Products)
- Large product images in scrollable grid with pagination
- Color-coded payment buttons (Cash=Orange, Card=Blue)
- Category sidebar with highlight selection
- Touch-first, visual browsing workflow

**Retail Mode: Microsoft Dynamics RMS Paradigm**
- Transaction grid (spreadsheet-style line items)
- Large totals display at bottom
- Function key row (F1-F12) for keyboard power users
- Scanner/keyboard-optimized workflow
- Familiar to existing RMS users (key migration target)

### Key Design Challenges

1. **Dual-Interface Architecture** - Two distinct UI paradigms in one codebase
2. **Mode Detection** - Seamless switching based on deployment configuration
3. **Shared Components** - Payment, reporting, and admin screens work for both modes
4. **Touch vs Keyboard Balance** - Hospitality touch-first, Retail keyboard-first
5. **RMS Migration Familiarity** - Retail mode must feel familiar to RMS veterans

### Design Opportunities

1. **Best of Both Worlds** - Modern touch capabilities with RMS-style efficiency
2. **Kenya-Native Integration** - M-Pesa feels as natural as Cash button
3. **Offline Seamlessness** - No "disconnected" anxiety in either mode
4. **Progressive Modernization** - RMS users get familiar layout with modern features

---

## Core User Experience

### Defining Experience

**Hospitality Mode Core Loop:**
```
Select Table → Add Items (touch product tiles) → Send to Kitchen → Settle Payment
```
- Primary user action: Tap category → Tap product → Item added to order
- Target: <2 seconds per item addition
- Modifiers auto-popup for items that require them

**Retail Mode Core Loop:**
```
Scan → Scan → Scan → Total → Tender → Receipt
```
- Primary user action: Barcode scan → Item auto-adds to transaction
- Target: <100ms barcode scan to display (rhythm-preserving speed)
- Scanner auto-focus always ready, no field selection needed

### Platform Strategy

| Aspect | Specification |
|--------|---------------|
| **Platform** | Windows Desktop (WPF) |
| **Primary Input - Hospitality** | Touch screen (10-15" displays) |
| **Primary Input - Retail** | Barcode scanner + Keyboard + Optional touch |
| **Offline Capability** | Full operation required, queue-and-sync on reconnect |
| **Hardware Integration** | Receipt printers (ESC/POS), cash drawers, scales, barcode scanners |
| **Visual Modernization** | RMS layout preserved, modernized feel (cleaner fonts, subtle shadows) |

### Effortless Interactions

| Interaction | Must Feel Effortless |
|-------------|---------------------|
| **Item Addition (Hospitality)** | Tap product tile → immediately in order, no confirmation |
| **Barcode Scanning (Retail)** | Scan → beep → item appears, continuous scanning ready |
| **Payment Selection** | M-Pesa prominent (Kenya primary), Cash, Card equally accessible |
| **User Switching** | PIN pad always accessible, <3 second login |
| **Offline Operation** | No visible difference from online mode |
| **Receipt Printing** | Automatic on settlement, no extra button press |
| **Error Recovery** | ONE button returns to ready state from any error condition |

### Critical Success Moments

| Moment | Success Criteria | Failure Impact |
|--------|------------------|----------------|
| **First Item Scan/Tap** | Instant response, correct item displayed | User loses confidence |
| **M-Pesa Payment** | STK Push sent, confirmation received seamlessly | Lost sales, customer frustration |
| **End of Day Close** | Z-Report prints, cash reconciles, <15 minutes total | Staff overtime, errors |
| **Offline Transaction** | Completes normally, syncs transparently later | Lost revenue, compliance issues |
| **KRA eTIMS Invoice** | Auto-generated, valid QR code, no user intervention | Tax compliance failure |
| **Manager Quick Check** | Tap once → see current shift totals, voids, sales | Managers interrupt cashiers for info |

### Experience Principles

1. **Speed IS the Feature** - Every 10 seconds saved × 500 transactions = 83 minutes/day ROI
2. **Mode-Native Feel** - Hospitality feels like SambaPOS; Retail feels like RMS (modernized)
3. **Invisible Complexity** - M-Pesa, eTIMS, offline sync happen without user awareness
4. **Graceful Degradation** - Features fail independently (printer down? still sell. eTIMS down? queue invoice)
5. **Forgiveness Built-In** - Easy void, edit quantity, change payment method (with appropriate authorization)
6. **Always Ready** - No loading states during transaction, scanner always listening

---

## Desired Emotional Response

### Primary Emotional Goals

| User Type | Primary Emotion | Supporting Feeling |
|-----------|-----------------|-------------------|
| **Cashiers** | Confident & In Control | "This system has my back during rush hour" |
| **Waiters** | Fluid & Uninterrupted | "I can serve tables without fighting the system" |
| **Kitchen Staff** | Informed & Efficient | "I always know what's coming and what's urgent" |
| **Managers** | Aware & Empowered | "I can see everything without disrupting operations" |
| **Administrators** | Capable & Secure | "Configuration is straightforward, nothing breaks unexpectedly" |

### Emotional Design Principles

1. **Never Leave Them Hanging** - Every action gets immediate visual + audio feedback
2. **Errors Are Recoverable, Not Fatal** - One button to return to ready state
3. **Trust Through Transparency** - Show what happened, show it's saved, show the math
4. **Match Their Speed** - System responds faster than human reaction time
5. **Professional Tool, Professional Feel** - Polished visuals that staff are proud to use

### Emotional Journey Mapping

**Transaction Flow Emotional States:**
```
START: Ready & Calm (neutral green indicator)
  ↓
SCANNING/ADDING: Rhythm & Flow (satisfying audio feedback per item)
  ↓
SUBTOTAL: Anticipation (clear total, payment options visible)
  ↓
PAYMENT: Confidence (progress indicators for M-Pesa, card processing)
  ↓
COMPLETION: Satisfaction (receipt prints, drawer opens, "success" tone)
  ↓
RESET: Ready Again (clean slate, next customer)
```

### Stress Reduction Strategies

| Stressor | Mitigation |
|----------|------------|
| Long queue behind | Large, clear totals visible to customers reduce "how much?" questions |
| Item not scanning | Quick PLU lookup, recent items list, manual barcode entry |
| Payment failure | Clear error message, auto-retry option, alternative payment one-tap away |
| Printer jam | Transaction completes anyway, reprint available, queue continues |
| Manager needed | One-button manager call, authorization PIN overlay (no screen change) |
| Network down | Seamless offline mode, green indicator stays green, no panic |

---

## UX Inspiration & Reference Analysis

### Primary Inspiration: SambaPOS v5 (Hospitality Mode)

**Key Elements to Adopt:**
- Three-column layout: Left (current ticket), Center (categories), Right (products)
- Large product tiles (120×120px minimum) with images
- Category list as vertical sidebar with colored highlights
- Order ticket shows items with quantities, modifiers inline
- Payment buttons prominently positioned: Cash (Orange), Card (Blue), M-Pesa (Green)
- Table layout view for restaurant floor management
- Dark theme option for ambient restaurant lighting

**Adaptations for Kenya Market:**
- M-Pesa button as prominent as Cash (not hidden in "Other")
- KRA eTIMS QR code on receipt preview
- Swahili language option in UI strings

### Primary Inspiration: Microsoft Dynamics RMS (Retail Mode)

**Key Elements to Adopt:**
- Transaction grid (line items in spreadsheet format)
- Large numeric totals display at bottom (Subtotal, Tax, Total)
- Function key toolbar (F1-F12) always visible
- Customer display output format
- Fast PLU entry via keyboard
- Receipt journal on left side (scrollable history)
- Gray/blue professional color scheme

**Adaptations for Modern Era:**
- Touch-friendly row heights (minimum 44px)
- Modernized icons (flat design, not 3D buttons)
- Optional product thumbnails in transaction grid
- M-Pesa integrated alongside cash/card tender
- USB barcode scanner + keyboard + optional touch

### Shared Component Patterns

| Component | Hospitality Style | Retail Style |
|-----------|-------------------|--------------|
| **Item Display** | Image tile with name below | Text row with SKU, Name, Price, Qty |
| **Category Nav** | Visual sidebar with icons | Function keys + dropdown |
| **Payment** | Large colored buttons | Tender dialog + function key shortcuts |
| **Receipt** | Full preview with items | Compact journal format |
| **Search** | Touch keyboard popup | Hardware keyboard focused |

---

## Design System

### Framework: Custom WPF Design System

**Why Custom (Not Material/Fluent):**
- RMS veterans expect specific visual patterns
- Touch target sizes need POS-specific optimization
- Offline-first requires custom loading states
- Hardware integration needs native Windows controls

### Color Palette

**Core Colors:**
```
Primary Blue:     #1565C0 (Headers, primary actions)
Success Green:    #2E7D32 (Completed states, M-Pesa)
Warning Orange:   #EF6C00 (Cash button, attention items)
Error Red:        #C62828 (Errors, voids, deletions)
Neutral Gray:     #424242 (Text, borders)
Background Light: #FAFAFA (Main backgrounds - Retail)
Background Dark:  #1E1E1E (Main backgrounds - Hospitality optional)
```

**Mode-Specific Accents:**
```
Hospitality Accent:  #FF6F00 (Warm, food-service feel)
Retail Accent:       #0277BD (Cool, professional commerce)
```

### Typography

**Font Stack:**
```
Primary:    Segoe UI (Windows native, excellent legibility)
Monospace:  Consolas (Receipt preview, PLU codes)
Fallback:   Arial, sans-serif
```

**Scale:**
```
Display:     32px (Dashboard totals, payment amounts)
Headline:    24px (Section headers, dialog titles)
Title:       18px (Card headers, category names)
Body:        14px (Default text, form labels)
Caption:     12px (Secondary info, timestamps)
Overline:    10px (Status badges, tiny labels)
```

### Spacing System

**Base Unit: 8px**
```
xs:   4px   (Tight inline spacing)
sm:   8px   (Related element spacing)
md:   16px  (Section padding)
lg:   24px  (Card margins)
xl:   32px  (Major section gaps)
xxl:  48px  (Page margins)
```

### Touch Target Sizes

| Context | Minimum Size | Recommended |
|---------|--------------|-------------|
| Primary Actions | 48×48px | 56×56px |
| Product Tiles | 100×100px | 120×120px |
| List Items | 44px height | 52px height |
| Function Keys | 60×40px | 80×48px |
| Payment Buttons | 120×60px | 160×80px |

### Component Library

**Core Components:**
1. **TransactionGrid** - Retail mode line items
2. **ProductTileGrid** - Hospitality mode products
3. **CategorySidebar** - Hospitality navigation
4. **FunctionKeyBar** - Retail mode F1-F12 row
5. **PaymentButtonPanel** - Cash/Card/M-Pesa buttons
6. **NumericKeypad** - PIN entry, quantity input
7. **ReceiptPreview** - Scrollable receipt view
8. **StatusIndicator** - Online/Offline/Syncing states
9. **QuickSearch** - Product lookup overlay
10. **ModifierSelector** - Hospitality item customization
11. **TableLayout** - Restaurant floor plan view
12. **KDSOrderCard** - Kitchen display order unit

---

## Screen Layouts

### Hospitality Mode - Main POS Screen

```
┌─────────────────────────────────────────────────────────────────────┐
│  [≡] Table 12        SERVED BY: John          [Clock] [User] [⚙]   │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌──────────────┐ ┌───────────────────────────────┐ │
│ │   TICKET    │ │  CATEGORIES  │ │          PRODUCTS             │ │
│ │             │ │              │ │                               │ │
│ │ 2× Burger   │ │ [🍔] Burgers │ │ ┌─────┐ ┌─────┐ ┌─────┐ ┌───┐ │ │
│ │    +Cheese  │ │ [🍕] Pizza   │ │ │     │ │     │ │     │ │   │ │ │
│ │    +Bacon   │ │ [🥤] Drinks  │ │ │ Img │ │ Img │ │ Img │ │Img│ │ │
│ │ 1× Fries    │ │ [🍰] Dessert │ │ │     │ │     │ │     │ │   │ │ │
│ │ 3× Soda     │ │ [🍺] Bar     │ │ │Name │ │Name │ │Name │ │Nm │ │ │
│ │             │ │ [⚙] More...  │ │ │KSh  │ │KSh  │ │KSh  │ │KSh│ │ │
│ │             │ │              │ │ └─────┘ └─────┘ └─────┘ └───┘ │ │
│ │             │ │              │ │ ┌─────┐ ┌─────┐ ┌─────┐ ┌───┐ │ │
│ │             │ │              │ │ │     │ │     │ │     │ │   │ │ │
│ │─────────────│ │              │ │ │ ... │ │ ... │ │ ... │ │...│ │ │
│ │ Subtotal:   │ │              │ │ └─────┘ └─────┘ └─────┘ └───┘ │ │
│ │   KSh 1,450 │ │              │ │         [<] Page 1/3 [>]      │ │
│ └─────────────┘ └──────────────┘ └───────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────┤
│ [🗑 VOID] [📋 HOLD] [🖨 SEND] │ [💵 CASH] [💳 CARD] [📱 M-PESA] [SETTLE] │
└─────────────────────────────────────────────────────────────────────┘
```

### Retail Mode - Main POS Screen

```
┌─────────────────────────────────────────────────────────────────────┐
│  POS Terminal 1              [Clock]     [User: Mary K.]    [⚙]    │
├─────────────────────────────────────────────────────────────────────┤
│ ┌───────────────────────────────────────────────────────────────┐   │
│ │ F1:Void │ F2:Qty │ F3:Price │ F4:Disc │ F5:Hold │ ... │ F12:Exit│ │
│ └───────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ PLU/Barcode: [                                        ] [🔍]    │ │
│ └─────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────┤
│ │ SKU          │ Description           │ Price    │ Qty │ Total   │ │
│ ├──────────────┼───────────────────────┼──────────┼─────┼─────────│ │
│ │ 5901234123   │ Coca-Cola 500ml       │ KSh 80   │  2  │ KSh 160 │ │
│ │ 6901234567   │ Bread White Large     │ KSh 65   │  1  │ KSh 65  │ │
│ │ 8901234890   │ Milk Fresh 500ml      │ KSh 75   │  3  │ KSh 225 │ │
│ │ 4901234456   │ Sugar 1kg             │ KSh 180  │  1  │ KSh 180 │ │
│ │              │                       │          │     │         │ │
│ │              │                       │          │     │         │ │
│ │              │                       │          │     │         │ │
│ ├──────────────┴───────────────────────┴──────────┴─────┴─────────┤ │
│ │                                                                 │ │
│ │                    SUBTOTAL:              KSh 630.00            │ │
│ │                    VAT (16%):             KSh 100.80            │ │
│ │                    ═══════════════════════════════════          │ │
│ │                    TOTAL:                 KSh 730.80            │ │
│ │                                                                 │ │
│ └─────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────┤
│      [💵 CASH (F9)]      [💳 CARD (F10)]      [📱 M-PESA (F11)]     │
└─────────────────────────────────────────────────────────────────────┘
```

### Kitchen Display System (KDS)

```
┌─────────────────────────────────────────────────────────────────────┐
│  KITCHEN DISPLAY          Active: 8    Waiting: 3         [⚙]      │
├─────────────────────────────────────────────────────────────────────┤
│ ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐            │
│ │ ORDER #47 │ │ ORDER #48 │ │ ORDER #49 │ │ ORDER #50 │            │
│ │ Table 5   │ │ Table 12  │ │ Takeaway  │ │ Table 3   │            │
│ │ 00:03:22  │ │ 00:02:45  │ │ 00:01:30  │ │ 00:00:45  │            │
│ │ ───────── │ │ ───────── │ │ ───────── │ │ ───────── │            │
│ │ 2× Burger │ │ 1× Pizza  │ │ 3× Wrap   │ │ 1× Salad  │            │
│ │   +Cheese │ │   +Xlarge │ │   +Spicy  │ │ 2× Juice  │            │
│ │ 1× Fries  │ │ 2× Wings  │ │ 1× Soda   │ │           │            │
│ │           │ │           │ │           │ │           │            │
│ │ [🔵 NEW]  │ │ [🟡 PREP] │ │ [🟡 PREP] │ │ [🔵 NEW]  │            │
│ │           │ │           │ │           │ │           │            │
│ │ [BUMP ✓]  │ │ [BUMP ✓]  │ │ [BUMP ✓]  │ │ [BUMP ✓]  │            │
│ └───────────┘ └───────────┘ └───────────┘ └───────────┘            │
│                                                                     │
│ Status: 🟢 Connected    Avg Prep Time: 8:32    Today's Orders: 147 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## User Journeys

### Journey 1: Retail Cashier Morning Rush

```
SCENARIO: Sarah opens register at 7AM, handles 50+ customers by 9AM

07:00 - SHIFT START
├── Tap user icon → Enter PIN (4 digits) → Dashboard loads
├── See yesterday's handover note (if any)
└── Status bar shows: 🟢 Online | Printer Ready | Drawer: KSh 5,000 float

07:02 - FIRST CUSTOMER (Bread + Milk + Airtime)
├── Scan bread → *beep* → appears in grid
├── Scan milk → *beep* → appears in grid
├── Customer: "100 bob airtime Safaricom"
├── Press F6 (Airtime) → Select Safaricom → Enter 100 → Enter phone
├── Total shows: KSh 245
├── Customer pays KSh 300 cash
├── Press F9 (Cash) → Enter 300 → Change: KSh 55 displayed
├── Drawer opens → Receipt prints → Reset to ready
└── Time: 45 seconds total

07:15 - M-PESA PAYMENT
├── Customer scans items → Total: KSh 1,250
├── Press F11 (M-Pesa) → Phone number entry
├── STK Push sent → Customer phone rings
├── Progress bar: "Waiting for confirmation..."
├── ✓ Payment confirmed → Receipt prints with M-Pesa reference
└── Total time: ~20 seconds after STK push

07:45 - VOID NEEDED (Wrong item scanned)
├── Customer: "That's not my bread, I wanted the other one"
├── Highlight wrong item → Press F1 (Void Line) → Item removed
├── Scan correct item → Continue transaction
└── No manager needed for single line void

08:30 - OFFLINE MODE (Internet drops)
├── Status bar changes: 🟡 Offline (Syncing when back)
├── Continue scanning, processing cash/card normally
├── M-Pesa button shows: "Offline - Manual entry available"
├── Transactions queue locally
├── Internet returns → 🟢 Back online → Queued items sync
└── Sarah never interrupted, customers unaware
```

### Journey 2: Restaurant Waiter Dinner Service

```
SCENARIO: John serves 6 tables during dinner rush

18:00 - NEW TABLE SEATED
├── Tap floor plan icon → See Table 7 highlighted (new guests)
├── Tap Table 7 → New ticket opens
├── Ticket header shows: Table 7 | Guests: 4 | Server: John
└── Product categories visible, ready for order

18:05 - TAKING ORDER
├── Tap "Starters" category → Tile grid shows starter items
├── Tap "Soup of Day" → Added to ticket
├── Customer asks: "Is the soup vegetarian?"
├── Long-press soup tile → Info popup shows ingredients ✓
├── Tap "Main Course" → Tap "Grilled Chicken"
├── Modifier popup auto-appears (required modifier)
├── Select: "Well done" + "Extra sauce"
├── Repeat for 3 more guests
├── Tap [SEND] → Kitchen ticket prints/displays on KDS
├── Ticket status: 🟡 Sent to Kitchen
└── Time: 2 minutes for 4-person order

18:20 - ADDITIONAL ORDER
├── Tap Table 7 from floor plan (existing ticket)
├── Add 2× Wine, 1× Dessert
├── Tap [SEND] → Only new items sent to bar/kitchen
└── Original items not reprinted

18:45 - PAYMENT TIME
├── Customer signals for bill
├── Tap Table 7 → Tap [SETTLE]
├── Receipt preview shows all items + KRA QR code
├── Customer: "Can we split? Two couples."
├── Tap [SPLIT] → Select items for Bill A, rest auto-to Bill B
├── Bill A: Card payment → Tap [CARD] → Terminal prompt
├── Bill B: M-Pesa payment → Tap [M-PESA] → Phone entry → Confirm
├── Both settled → Table 7 clears from floor plan
└── Table ready for next guests

19:00 - FLOOR OVERVIEW
├── Glance at floor plan: 4 green (paid), 2 orange (occupied), 2 gray (empty)
├── See Table 12 is 🔴 (kitchen delay warning > 15 min)
├── Tap Table 12 → See order waiting, check with kitchen
└── Proactive service, no customer complaints
```

### Journey 3: Manager End of Day

```
SCENARIO: Grace closes the store at 22:00

21:45 - PRE-CLOSE CHECKS
├── Tap Manager Menu (requires PIN)
├── Dashboard shows: Today's Sales, Transactions, Voids
├── Check void report → 3 voids today (all < KSh 500, no concerns)
├── Review hourly sales graph → Peak was 18:00-19:00 as expected
└── No anomalies flagged

22:00 - CLOSE REGISTERS
├── Announce to cashiers: "Close your tills"
├── Cashiers tap [End Shift]
├── Blind count prompt: Enter counted cash
├── System compares: Expected KSh 45,230 | Counted KSh 45,200
├── Variance: -KSh 30 (within tolerance ✓)
├── X-Report prints for each cashier
└── Drawers locked, shifts closed

22:05 - Z-REPORT
├── Tap [Z-Report] → Confirmation: "This will close the day. Continue?"
├── Confirm → Z-Report generates
├── Prints: Total sales, payment breakdown, tax summary, KRA submission status
├── All 147 transactions synced to cloud ✓
├── eTIMS invoices: 147 submitted, 0 pending ✓
└── Day officially closed

22:10 - END
├── Review tomorrow's schedule (if integrated)
├── Set offline mode for overnight (optional)
├── Exit application
└── Total close time: 25 minutes (target < 30)
```

---

## Component Strategy

### Shared Components (Both Modes)

| Component | Purpose | Variants |
|-----------|---------|----------|
| **UserLoginOverlay** | PIN entry, user switching | Standard, Manager PIN |
| **PaymentPanel** | Cash/Card/M-Pesa selection | Horizontal (Retail), Vertical (Hospitality) |
| **ReceiptViewer** | Receipt preview and reprint | Full (Hospitality), Compact (Retail) |
| **NumPad** | Numeric input | Quantity, PIN, Phone number |
| **StatusBar** | Online/Offline, Printer, User | Always visible in header |
| **ManagerAuthDialog** | Authorization for voids, discounts | Overlay, doesn't navigate away |
| **SearchOverlay** | Product lookup by name/SKU | Full-screen with keyboard |
| **PrinterStatus** | Printer connection indicator | Icon + tooltip |
| **SyncIndicator** | Cloud sync status | Animated during sync |
| **ErrorBanner** | Dismissible error messages | Toast style, auto-hide |

### Hospitality-Specific Components

| Component | Purpose |
|-----------|---------|
| **TicketPanel** | Current order display with modifiers |
| **CategorySidebar** | Vertical category navigation with icons |
| **ProductTileGrid** | Image-based product selection |
| **ModifierPopup** | Item customization (size, additions, etc.) |
| **FloorPlanView** | Table layout with status colors |
| **TableCard** | Individual table status display |
| **KDSOrderCard** | Kitchen order display unit |
| **CourseFiringPanel** | Multi-course timing control |
| **SplitBillDialog** | Bill splitting interface |

### Retail-Specific Components

| Component | Purpose |
|-----------|---------|
| **TransactionGrid** | Spreadsheet-style line items |
| **FunctionKeyBar** | F1-F12 keyboard shortcuts |
| **TotalsPanel** | Large subtotal/tax/total display |
| **BarcodeInput** | Scanner-focused input field |
| **QuickPLUPanel** | Frequent item buttons |
| **CashTenderDialog** | Cash amount entry with change calc |
| **ScaleIntegration** | Weight display from scale |
| **LabelPrintButton** | Shelf label printing trigger |

### Component State Management

```
                    ┌─────────────────────┐
                    │   AppState (Root)   │
                    └─────────┬───────────┘
                              │
          ┌───────────────────┼───────────────────┐
          │                   │                   │
  ┌───────▼───────┐   ┌───────▼───────┐   ┌──────▼──────┐
  │  SessionState │   │ TransactionState│   │ ConfigState │
  │  - User       │   │ - Items        │   │ - Mode      │
  │  - Shift      │   │ - Payments     │   │ - Printers  │
  │  - Register   │   │ - Status       │   │ - Tax rates │
  └───────────────┘   └───────────────┘   └─────────────┘
```

---

## UX Patterns

### Pattern 1: Optimistic UI Updates

**Problem:** Network latency makes operations feel slow
**Solution:** Update UI immediately, sync in background

```
User taps "Add Item" →
  1. Item appears in ticket IMMEDIATELY (optimistic)
  2. Background: Send to server
  3. If success: Item confirmed (no visible change)
  4. If failure: Show error, offer retry (rare case)
```

### Pattern 2: Progressive Disclosure

**Problem:** Too many options overwhelm new users
**Solution:** Show essentials first, reveal advanced on demand

- **Default View:** Common categories, frequent items, standard payment
- **Long Press:** Additional options, item details, alternate actions
- **Manager Menu:** Advanced functions, reports, configuration

### Pattern 3: Forgiving Input

**Problem:** Mistakes during rush cause frustration
**Solution:** Multiple correction paths, no dead ends

| Mistake | Recovery |
|---------|----------|
| Wrong item added | Tap to select → Delete/Void |
| Wrong quantity | Tap quantity → Edit inline |
| Wrong payment started | Cancel → Choose different method |
| Accidental navigation | Back button always available |

### Pattern 4: Contextual Defaults

**Problem:** Repetitive selections slow users down
**Solution:** Smart defaults based on context

- **Payment Default:** Last-used method for this customer (if known)
- **Modifier Default:** "Regular" size, "No" for optional additions
- **Print Default:** Always print receipt (one-tap skip if needed)
- **Table Default:** Auto-select next empty table number

### Pattern 5: Ambient Awareness

**Problem:** Users miss important status changes
**Solution:** Persistent status indicators, non-intrusive alerts

- **Status Bar:** Always visible sync, printer, user status
- **Color Coding:** Table status colors visible at a glance
- **Audio Cues:** Distinct sounds for scan success, payment complete, error
- **Badge Counts:** Pending orders, held transactions, alerts

### Pattern 6: One-Tap Recovery

**Problem:** Errors trap users in bad states
**Solution:** Universal "back to ready" escape

- Any error dialog: [OK] returns to transaction screen
- Any stuck state: Home button resets to ready (with warning if data loss)
- Keyboard shortcut: ESC always dismisses overlays

---

## Accessibility Considerations

### Visual Accessibility

| Feature | Implementation |
|---------|----------------|
| **Color Contrast** | WCAG AA minimum (4.5:1 text, 3:1 UI) |
| **Color Independence** | Never use color alone (add icons, text) |
| **Text Scaling** | Support 100%-150% system font scaling |
| **High Contrast Mode** | Respect Windows high contrast settings |
| **Focus Indicators** | Visible focus ring on all interactive elements |

### Motor Accessibility

| Feature | Implementation |
|---------|----------------|
| **Touch Targets** | Minimum 48×48px, recommended 56×56px |
| **Gesture Alternatives** | All swipe actions have button alternatives |
| **Keyboard Navigation** | Full functionality via keyboard (Retail mode) |
| **Adjustable Timing** | No time-limited interactions (except security timeouts) |
| **Error Tolerance** | Confirmation for destructive actions |

### Cognitive Accessibility

| Feature | Implementation |
|---------|----------------|
| **Consistent Layout** | Same elements in same positions across screens |
| **Clear Labels** | Action buttons describe outcome ("Pay Cash" not "Continue") |
| **Error Messages** | Plain language, specific fix instructions |
| **Progress Indication** | Show steps for multi-part processes |
| **Undo Support** | Recoverable actions where possible |

### Hardware Considerations

```
SUPPORTED DISPLAYS:
├── Minimum: 1024×768 (legacy compatibility)
├── Recommended: 1280×1024 or 1920×1080
├── Touch: Capacitive multi-touch (up to 10 points)
└── Scaling: 100%, 125%, 150% Windows DPI settings

INPUT DEVICES:
├── Touch screen (Hospitality primary)
├── Keyboard + Mouse (Retail primary)
├── Barcode Scanner (USB HID or Serial)
├── Receipt Printer (ESC/POS USB/Serial/Network)
├── Cash Drawer (Printer-triggered or serial)
├── Customer Display (Serial 2×20 or USB)
└── Scale (Serial with continuous weight)
```

---

## Responsive Design (Resolution Adaptation)

### Resolution Breakpoints

| Resolution | Use Case | Adaptations |
|------------|----------|-------------|
| **1024×768** | Legacy POS terminals | Single column layouts, larger touch targets |
| **1280×1024** | Standard POS monitors | Full layouts, optimal component sizes |
| **1920×1080** | Modern displays | Extended views, more visible items |
| **1920×1200+** | Large format / dual | Multi-window potential, KDS optimization |

### Hospitality Mode Scaling

```
1024×768:
├── 2-column layout (Categories + Products combined panel)
├── Ticket as slide-out drawer
├── Fewer product tiles per page (3×3 grid)
└── Payment buttons in bottom bar

1280×1024+:
├── 3-column layout (Ticket | Categories | Products)
├── More product tiles (4×4 or 5×4 grid)
└── Inline payment panel
```

### Retail Mode Scaling

```
1024×768:
├── Transaction grid shows 8-10 lines
├── Function keys as icons (no text labels)
├── Compact totals display
└── Payment buttons horizontal

1280×1024+:
├── Transaction grid shows 12-15 lines
├── Function keys with text labels
├── Large totals display with tax breakdown
└── Full tender dialogs
```

---

## Animation & Motion

### Motion Principles

1. **Purpose-Driven:** Animations guide attention, not decorate
2. **Performance:** Never drop frames during transaction
3. **Reducible:** Respect "Reduce Motion" accessibility setting
4. **Quick:** Max 200ms for micro-interactions, 300ms for transitions

### Animation Inventory

| Animation | Duration | Easing | Purpose |
|-----------|----------|--------|---------|
| Item added to list | 150ms | ease-out | Confirm addition |
| Item removed | 150ms | ease-in | Confirm deletion |
| Screen transition | 250ms | ease-in-out | Context change |
| Modal appear | 200ms | ease-out | Draw attention |
| Modal dismiss | 150ms | ease-in | Quick exit |
| Loading spinner | continuous | linear | Activity indicator |
| Success checkmark | 300ms | spring | Completion feedback |
| Error shake | 200ms | linear | Attention to error |

### Feedback Sounds

| Action | Sound | Volume |
|--------|-------|--------|
| Barcode scanned | Short beep | Medium |
| Item added | Soft click | Low |
| Payment complete | Pleasant chime | Medium |
| Error occurred | Distinct tone | Medium-High |
| Drawer opened | Mechanical click | Actual hardware |
| Print started | None (printer noise) | N/A |

---

## Offline Experience

### Offline Capability Matrix

| Feature | Offline Support | Notes |
|---------|-----------------|-------|
| Cash transactions | ✅ Full | Queued for sync |
| Card transactions | ✅ Full | Store & forward |
| M-Pesa transactions | ⚠️ Manual | Manual reference entry |
| Receipt printing | ✅ Full | Local printer |
| Product lookup | ✅ Full | Local SQLite cache |
| KRA eTIMS | ⚠️ Queued | Submitted when online |
| Reports | ⚠️ Local only | May miss other terminals |
| User login | ✅ Full | Cached credentials |
| Price updates | ❌ Blocked | Requires sync |
| New products | ❌ Blocked | Requires sync |

### Offline UI Indicators

```
ONLINE STATE:
├── Status bar: 🟢 green dot + "Online"
├── All features enabled
└── Real-time sync active

OFFLINE STATE:
├── Status bar: 🟡 yellow dot + "Offline"
├── Subtle banner: "Working offline - changes will sync when connected"
├── M-Pesa shows: "Manual entry mode"
├── Queued transaction counter: "3 pending sync"
└── No error dialogs (seamless experience)

SYNCING STATE:
├── Status bar: 🔄 animated + "Syncing..."
├── Progress: "Syncing 3 of 12 transactions"
└── On complete: 🟢 "All synced" (fades after 3s)
```

---

## Error Handling UX

### Error Message Guidelines

**Do:**
- Use plain language ("Printer not connected" not "ESC/POS Error 0x03")
- Offer specific action ("Check printer power and cable")
- Provide escape route ("Continue without printing")

**Don't:**
- Show technical codes to users
- Use alarming language ("CRITICAL FAILURE")
- Trap users with no way forward

### Error Categories & Responses

| Category | Display | User Action |
|----------|---------|-------------|
| **Recoverable** | Inline warning, yellow | Retry or alternative |
| **Blocking** | Modal dialog | Must resolve or cancel |
| **Background** | Toast notification | Acknowledge only |
| **Critical** | Full-screen | Requires manager |

### Example Error Dialogs

**Recoverable (Payment declined):**
```
┌─────────────────────────────────────┐
│     ⚠️ Card Payment Declined        │
│                                     │
│  The card was declined by the bank. │
│                                     │
│  [Try Again]  [Use Different Card]  │
│              [Cancel]               │
└─────────────────────────────────────┘
```

**Blocking (Printer error mid-transaction):**
```
┌─────────────────────────────────────┐
│     🖨️ Printer Not Responding       │
│                                     │
│  Check that printer is:            │
│  • Powered on                      │
│  • Paper loaded                    │
│  • Cable connected                 │
│                                     │
│  [Retry Print]  [Skip & Continue]  │
└─────────────────────────────────────┘
```

---

## Final Checklist

### Design Completeness

- [x] Executive Summary with vision and target users
- [x] Dual-mode design direction (SambaPOS + RMS)
- [x] Core user experience and interaction patterns
- [x] Emotional response and stress reduction strategies
- [x] UX inspiration analysis (SambaPOS v5 + Microsoft RMS)
- [x] Design system (colors, typography, spacing, components)
- [x] Screen layouts for all three modes (Hospitality, Retail, KDS)
- [x] User journey maps (3 detailed scenarios)
- [x] Component strategy and state management
- [x] UX patterns (6 core patterns documented)
- [x] Accessibility considerations (visual, motor, cognitive)
- [x] Responsive design guidelines
- [x] Animation and motion specifications
- [x] Offline experience design
- [x] Error handling UX

### Ready for Development

This UX specification provides implementation-ready guidelines for:

1. **WPF Component Development** - Clear component inventory with state requirements
2. **Screen Implementation** - ASCII wireframes with element positioning
3. **Interaction Development** - Defined touch targets, keyboard shortcuts, feedback
4. **Accessibility Compliance** - WCAG-aligned specifications
5. **Error Handling** - Categorized error types with UI patterns
6. **Offline Support** - Clear capability matrix and UI indicators

---

**Document Version:** 1.0
**Last Updated:** 2025-12-28
**Author:** Linuxlab
**Workflow:** create-ux-design (Steps 1-14 Complete)
