# Multi-Terminal POS System Feature Specification

> **Document Version:** 1.0
> **Created:** January 2026
> **Status:** Draft - Awaiting Implementation
> **Priority:** High - Core Infrastructure

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Business Requirements](#2-business-requirements)
3. [System Architecture Overview](#3-system-architecture-overview)
4. [Feature Specifications](#4-feature-specifications)
   - 4.1 [Terminal Registration & Configuration](#41-terminal-registration--configuration)
   - 4.2 [Local Terminal Configuration](#42-local-terminal-configuration)
   - 4.3 [Work Period Management per Terminal](#43-work-period-management-per-terminal)
   - 4.4 [Transaction Attribution](#44-transaction-attribution)
   - 4.5 [X-Report Generation](#45-x-report-generation)
   - 4.6 [Z-Report Generation](#46-z-report-generation)
   - 4.7 [Combined Multi-Register Reports](#47-combined-multi-register-reports)
   - 4.8 [Terminal Health Monitoring](#48-terminal-health-monitoring)
   - 4.9 [Admin Terminal Management UI](#49-admin-terminal-management-ui)
5. [Database Schema Changes](#5-database-schema-changes)
6. [Report Layouts & Formats](#6-report-layouts--formats)
7. [Deployment Guide](#7-deployment-guide)
8. [Security Considerations](#8-security-considerations)
9. [Testing Requirements](#9-testing-requirements)
10. [Future Enhancements](#10-future-enhancements)

---

## 1. Executive Summary

### 1.1 Purpose

This document specifies the features required to transform the HospitalityPOS system from a single-terminal application to a fully-functional multi-terminal Point of Sale system. The system will support deployment across multiple registers/tills connected via LAN to a centralized SQL Server database, with each terminal maintaining its own identity, configuration, and reporting capabilities.

### 1.2 Scope

The implementation covers:
- Terminal device registration and pairing
- Local per-machine configuration
- Terminal-aware transaction processing
- Individual and combined X/Z report generation
- Cashier-to-register assignment tracking
- Payment method breakdown per register
- Consolidated reporting with drill-down capabilities

### 1.3 Key Stakeholders

| Role | Responsibility |
|------|----------------|
| Supermarket Owner/Manager | Configure terminals, view combined reports |
| Supervisor | Manage shifts, approve variances, run reports |
| Cashier | Operate assigned register, perform cash counts |
| IT Administrator | Deploy and configure terminal machines |

### 1.4 Success Criteria

- [ ] Each terminal uniquely identified in the system
- [ ] All transactions tagged with terminal ID and cashier
- [ ] X-Reports generated per terminal showing real-time sales
- [ ] Z-Reports generated per terminal at shift/day end
- [ ] Combined reports showing all terminals with breakdown
- [ ] Payment method totals per terminal and overall
- [ ] Zero data loss during network interruptions (future phase)

---

## 2. Business Requirements

### 2.1 Deployment Scenario

```
┌─────────────────────────────────────────────────────────────────────┐
│                         SUPERMARKET LAN                              │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    DATABASE SERVER                            │   │
│  │              SQL Server (192.168.1.100)                       │   │
│  │                   Database: posdb                             │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│              ┌───────────────┼───────────────┐                      │
│              │               │               │                       │
│        ┌─────▼─────┐   ┌─────▼─────┐   ┌─────▼─────┐               │
│        │  REG-001  │   │  REG-002  │   │  REG-003  │               │
│        │  Cashier  │   │  Cashier  │   │  Cashier  │               │
│        │   Mode    │   │   Mode    │   │   Mode    │               │
│        │           │   │           │   │           │               │
│        │ [Printer] │   │ [Printer] │   │ [Printer] │               │
│        │ [Drawer]  │   │ [Drawer]  │   │ [Drawer]  │               │
│        │ [Scanner] │   │ [Scanner] │   │ [Scanner] │               │
│        └───────────┘   └───────────┘   └───────────┘               │
│                                                                      │
│        ┌─────────────┐   ┌─────────────┐                            │
│        │  TILL-001   │   │  ADMIN-001  │                            │
│        │   Hotel     │   │   Back      │                            │
│        │   Mode      │   │   Office    │                            │
│        └─────────────┘   └─────────────┘                            │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Terminal Naming Convention

| Terminal Type | Code Format | Example | Description |
|---------------|-------------|---------|-------------|
| Supermarket Register | `REG-XXX` | REG-001, REG-002 | Main checkout counters |
| Till (Hotel/Restaurant) | `TILL-XXX` | TILL-001, TILL-002 | Service point terminals |
| Admin Workstation | `ADMIN-XXX` | ADMIN-001 | Back office management |
| Kitchen Display | `KDS-XXX` | KDS-001 | Kitchen display stations |
| Mobile Terminal | `MOB-XXX` | MOB-001 | Handheld devices (future) |

### 2.3 User Stories

#### US-001: Terminal Registration
> **As a** System Administrator
> **I want to** register a new terminal when deploying the POS on a new machine
> **So that** the terminal is uniquely identified in the system and can track its own transactions

**Acceptance Criteria:**
- [ ] First launch on unregistered machine triggers Terminal Setup Wizard
- [ ] Administrator can select existing terminal or create new one
- [ ] Machine identifier (MAC address or generated GUID) is stored
- [ ] Local configuration file is created with terminal identity
- [ ] Terminal appears in admin Terminal Management screen

#### US-002: Cashier Login to Terminal
> **As a** Cashier
> **I want to** log into my assigned register
> **So that** all my sales are recorded against both my user ID and the register

**Acceptance Criteria:**
- [ ] Login screen displays terminal name (e.g., "REG-001")
- [ ] Successful login creates session with both UserId and TerminalId
- [ ] All subsequent transactions tagged with UserId + TerminalId
- [ ] Multiple cashiers can work same register across shifts (tracked separately)

#### US-003: X-Report Mid-Shift
> **As a** Supervisor
> **I want to** generate an X-Report for a specific register at any time
> **So that** I can monitor sales progress during the shift

**Acceptance Criteria:**
- [ ] X-Report available from register or supervisor station
- [ ] Shows sales from current work period start to now
- [ ] Displays cashier name(s) who worked the register
- [ ] Shows payment method breakdown (Cash, M-Pesa, Card)
- [ ] Does NOT close the work period or batch
- [ ] Can be generated multiple times

#### US-004: Z-Report End of Shift
> **As a** Supervisor
> **I want to** generate a Z-Report for a specific register at end of shift
> **So that** I can close out the register and reconcile cash

**Acceptance Criteria:**
- [ ] Z-Report requires cash count before generation
- [ ] Shows complete shift sales summary
- [ ] Displays all cashiers who worked the register with individual totals
- [ ] Shows payment method breakdown with amounts
- [ ] Calculates cash variance (expected vs actual)
- [ ] Closes work period for that specific terminal
- [ ] Creates immutable fiscal record
- [ ] Sequential report numbering per terminal

#### US-005: Combined Multi-Register Report
> **As a** Store Manager
> **I want to** generate a combined report showing all registers
> **So that** I can see total store performance with breakdown by register and payment method

**Acceptance Criteria:**
- [ ] Shows each register's total sales
- [ ] Shows cashier assigned to each register
- [ ] Shows payment method breakdown per register
- [ ] Shows grand totals for all payment methods
- [ ] Shows overall store total
- [ ] Can be generated for current day or date range
- [ ] Available as X (preview) or Z (closing) report

---

## 3. System Architecture Overview

### 3.1 Current State

```
┌─────────────────────────────────────────┐
│            Current Architecture          │
├─────────────────────────────────────────┤
│  • Single terminal assumption            │
│  • RegisterName hardcoded "REG-001"      │
│  • TerminalId fields exist but unused    │
│  • Z-Reports not terminal-aware          │
│  • Work periods not terminal-scoped      │
└─────────────────────────────────────────┘
```

### 3.2 Target State

```
┌─────────────────────────────────────────┐
│            Target Architecture           │
├─────────────────────────────────────────┤
│  • Multiple terminals supported          │
│  • Local config file per machine         │
│  • Terminal entity in database           │
│  • All transactions tagged with terminal │
│  • Terminal-scoped work periods          │
│  • Per-terminal and combined reports     │
│  • Terminal health monitoring            │
└─────────────────────────────────────────┘
```

### 3.3 Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         WPF Application                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐    ┌──────────────────────────────────┐   │
│  │  Terminal Setup  │    │        ITerminalService          │   │
│  │     Wizard       │───▶│  • GetCurrentTerminal()          │   │
│  └──────────────────┘    │  • RegisterTerminal()            │   │
│                          │  • ValidateTerminalConfig()      │   │
│  ┌──────────────────┐    │  • GetTerminalsByStore()         │   │
│  │  Local Config    │    └──────────────────────────────────┘   │
│  │  (terminal.json) │                   │                       │
│  └──────────────────┘                   ▼                       │
│                          ┌──────────────────────────────────┐   │
│                          │      ITerminalSessionService     │   │
│                          │  • CurrentTerminalId             │   │
│                          │  • CurrentTerminalCode           │   │
│                          │  • CurrentUserId                 │   │
│                          │  • CurrentWorkPeriodId           │   │
│                          └──────────────────────────────────┘   │
│                                         │                       │
│                                         ▼                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Transaction Layer                      │   │
│  │  All receipts, orders, payments tagged with:            │   │
│  │  • TerminalId                                           │   │
│  │  • UserId (Cashier)                                     │   │
│  │  • WorkPeriodId                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                         │                       │
│                                         ▼                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    Reporting Layer                       │   │
│  │  • GenerateXReportAsync(terminalId)                     │   │
│  │  • GenerateZReportAsync(terminalId, cashCount)          │   │
│  │  • GenerateCombinedReportAsync(storeId, terminalIds[])  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SQL Server Database                         │
│                                                                  │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐    │
│  │ Terminals │  │WorkPeriods│  │ Receipts  │  │ ZReports  │    │
│  │           │  │           │  │           │  │           │    │
│  │TerminalId │◀─│TerminalId │  │TerminalId │  │TerminalId │    │
│  │   Code    │  │  UserId   │  │  UserId   │  │  UserId   │    │
│  │  StoreId  │  │           │  │           │  │           │    │
│  └───────────┘  └───────────┘  └───────────┘  └───────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Feature Specifications

### 4.1 Terminal Registration & Configuration

#### 4.1.1 Overview

Terminal registration establishes a unique identity for each POS machine in the system. This is performed once during initial setup and binds the physical machine to a logical terminal record in the database.

#### 4.1.2 Terminal Entity

**Database Table: `Terminals`**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK, Identity | Unique terminal identifier |
| `StoreId` | int | FK, NOT NULL | Parent store reference |
| `Code` | nvarchar(20) | Unique, NOT NULL | Display code (REG-001) |
| `Name` | nvarchar(100) | NOT NULL | Friendly name (Register 1) |
| `Description` | nvarchar(500) | NULL | Optional description |
| `MachineIdentifier` | nvarchar(100) | Unique, NOT NULL | MAC address or GUID |
| `TerminalType` | int | NOT NULL | Enum: Register, Till, Admin, KDS |
| `BusinessMode` | int | NOT NULL | Enum: Supermarket, Restaurant, Admin |
| `IsActive` | bit | NOT NULL, Default 1 | Whether terminal is active |
| `IsMainRegister` | bit | NOT NULL, Default 0 | Primary register for store |
| `LastHeartbeat` | datetime2 | NULL | Last communication timestamp |
| `LastLoginUserId` | int | FK, NULL | Last logged-in user |
| `LastLoginAt` | datetime2 | NULL | Last login timestamp |
| `IpAddress` | nvarchar(45) | NULL | Current IP address |
| `PrinterConfiguration` | nvarchar(max) | NULL | JSON: printer settings |
| `HardwareConfiguration` | nvarchar(max) | NULL | JSON: drawer, display, scale |
| `CreatedAt` | datetime2 | NOT NULL | Creation timestamp |
| `CreatedByUserId` | int | FK, NOT NULL | Who created the terminal |
| `ModifiedAt` | datetime2 | NULL | Last modification |
| `ModifiedByUserId` | int | FK, NULL | Who modified |

#### 4.1.3 Terminal Types Enum

```csharp
public enum TerminalType
{
    Register = 1,      // Supermarket checkout register
    Till = 2,          // Hotel/Restaurant service point
    AdminWorkstation = 3, // Back office admin terminal
    KitchenDisplay = 4,   // KDS station
    MobileTerminal = 5,   // Handheld device (future)
    SelfCheckout = 6      // Self-service kiosk (future)
}
```

#### 4.1.4 Terminal Setup Wizard Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    TERMINAL SETUP WIZARD                         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Database Connection                                      │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│  Server Address: [192.168.1.100        ]                        │
│  Database Name:  [posdb                ]                        │
│  Authentication: (•) Windows  ( ) SQL Server                    │
│                                                                  │
│  [Test Connection]                     [Next →]                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Terminal Identity                                        │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│  ( ) Register existing terminal                                  │
│      ┌─────────────────────────────────────────┐                │
│      │ REG-001 - Register 1 (Unassigned)       │                │
│      │ REG-002 - Register 2 (Unassigned)       │                │
│      │ TILL-001 - Till 1 (Unassigned)          │                │
│      └─────────────────────────────────────────┘                │
│                                                                  │
│  (•) Create new terminal                                         │
│      Terminal Type: [Register          ▼]                        │
│      Terminal Code: [REG-003           ]                        │
│      Terminal Name: [Register 3        ]                        │
│                                                                  │
│  [← Back]                              [Next →]                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Hardware Configuration                                   │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│  Receipt Printer:                                                │
│  [EPSON TM-T88VI                      ▼] [Test Print]           │
│                                                                  │
│  Cash Drawer:                                                    │
│  Port: [COM1  ▼]  Trigger: [Printer ▼]  [Test Open]             │
│                                                                  │
│  Barcode Scanner:                                                │
│  Type: [USB HID ▼]  [✓] Auto-detected                           │
│                                                                  │
│  Customer Display:                                               │
│  [✓] Enable  Port: [COM2 ▼]  [Test Display]                     │
│                                                                  │
│  [← Back]                              [Next →]                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: Confirmation                                             │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│  Terminal Configuration Summary                                  │
│  ─────────────────────────────                                  │
│  Code:        REG-003                                           │
│  Name:        Register 3                                        │
│  Type:        Supermarket Register                              │
│  Machine ID:  A4:83:E7:2B:91:F0                                 │
│  Printer:     EPSON TM-T88VI                                    │
│  Cash Drawer: COM1 (Printer-triggered)                          │
│  Scanner:     USB HID (Auto-detected)                           │
│  Display:     COM2                                              │
│                                                                  │
│  [← Back]           [Complete Setup]                             │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.1.5 Machine Identifier Generation

The system must uniquely identify each physical machine. Priority order:

1. **Primary:** First non-virtual network adapter MAC address
2. **Fallback:** Windows Machine GUID from registry
3. **Last Resort:** Generated GUID (stored locally, less reliable)

```csharp
public interface IMachineIdentifierService
{
    /// <summary>
    /// Gets or generates a unique identifier for this machine.
    /// </summary>
    string GetMachineIdentifier();

    /// <summary>
    /// Validates if the stored machine identifier matches current machine.
    /// </summary>
    bool ValidateMachineIdentifier(string storedIdentifier);
}
```

#### 4.1.6 Acceptance Criteria

- [ ] Terminal Setup Wizard launches on first run or if local config missing
- [ ] Wizard validates database connectivity before proceeding
- [ ] Existing unassigned terminals listed for selection
- [ ] New terminal creation validates code uniqueness
- [ ] Machine identifier extracted and stored
- [ ] Hardware configuration saved to database and local file
- [ ] Terminal marked as active with creation timestamp
- [ ] Setup can be re-run from admin settings (with confirmation)

---

### 4.2 Local Terminal Configuration

#### 4.2.1 Overview

Each machine stores its terminal identity in a local configuration file. This allows the application to know its identity immediately on startup without querying the database.

#### 4.2.2 Configuration File Location

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\ProNetPOS\terminal.json` |
| Alternative | `C:\ProgramData\ProNetPOS\terminal.json` (shared machine) |

#### 4.2.3 Configuration File Schema

```json
{
  "$schema": "terminal-config-schema.json",
  "version": "1.0",
  "terminal": {
    "id": 3,
    "code": "REG-003",
    "name": "Register 3",
    "storeId": 1,
    "type": "Register",
    "businessMode": "Supermarket",
    "machineIdentifier": "A4:83:E7:2B:91:F0"
  },
  "database": {
    "server": "192.168.1.100",
    "database": "posdb",
    "integratedSecurity": true,
    "connectionTimeout": 30,
    "commandTimeout": 30
  },
  "hardware": {
    "receiptPrinter": {
      "name": "EPSON TM-T88VI",
      "type": "ESC/POS",
      "port": "USB",
      "paperWidth": 80
    },
    "cashDrawer": {
      "type": "PrinterTriggered",
      "port": "COM1",
      "openCode": "27,112,0,25,250"
    },
    "barcodeScanner": {
      "type": "USB_HID",
      "prefix": "",
      "suffix": "\r"
    },
    "customerDisplay": {
      "enabled": true,
      "type": "VFD",
      "port": "COM2"
    },
    "scale": {
      "enabled": false
    }
  },
  "settings": {
    "autoLogoutMinutes": 30,
    "requireCashCountOnZReport": true,
    "printReceiptAutomatically": true,
    "soundEnabled": true
  },
  "lastSync": "2026-01-24T10:30:00Z",
  "registeredAt": "2026-01-20T08:00:00Z"
}
```

#### 4.2.4 Terminal Configuration Service

```csharp
public interface ITerminalConfigurationService
{
    /// <summary>
    /// Gets the current terminal configuration from local file.
    /// Returns null if not configured.
    /// </summary>
    TerminalConfiguration? GetLocalConfiguration();

    /// <summary>
    /// Saves terminal configuration to local file.
    /// </summary>
    Task SaveLocalConfigurationAsync(TerminalConfiguration config);

    /// <summary>
    /// Validates local config matches database record.
    /// </summary>
    Task<TerminalValidationResult> ValidateConfigurationAsync();

    /// <summary>
    /// Checks if this machine is registered as a terminal.
    /// </summary>
    bool IsTerminalConfigured();

    /// <summary>
    /// Gets the current terminal ID (from local config).
    /// </summary>
    int? GetCurrentTerminalId();

    /// <summary>
    /// Gets the current terminal code (from local config).
    /// </summary>
    string? GetCurrentTerminalCode();
}
```

#### 4.2.5 Startup Flow

```
Application Start
        │
        ▼
┌───────────────────┐
│ Check terminal.json│
│     exists?        │
└─────────┬─────────┘
          │
    ┌─────┴─────┐
    │           │
   Yes          No
    │           │
    ▼           ▼
┌─────────┐  ┌─────────────────┐
│ Load    │  │ Launch Terminal │
│ Config  │  │ Setup Wizard    │
└────┬────┘  └────────┬────────┘
     │                │
     ▼                │
┌─────────────────┐   │
│ Validate with   │   │
│ Database        │   │
└────────┬────────┘   │
         │            │
    ┌────┴────┐       │
    │         │       │
  Valid    Invalid    │
    │         │       │
    ▼         ▼       │
┌───────┐  ┌─────────┐│
│Normal │  │Re-run   ││
│Startup│  │Wizard   │◀┘
└───────┘  └─────────┘
```

#### 4.2.6 Acceptance Criteria

- [ ] Local config file created during Terminal Setup Wizard
- [ ] Config file readable on application startup
- [ ] Missing config file triggers Setup Wizard
- [ ] Invalid/corrupted config triggers Setup Wizard with warning
- [ ] Machine identifier mismatch triggers re-registration prompt
- [ ] Config includes all hardware settings for offline reference
- [ ] Database connection string stored securely (consider encryption)

---

### 4.3 Work Period Management per Terminal

#### 4.3.1 Overview

Work periods (shifts) must be terminal-scoped. Each terminal maintains its own work period, allowing independent shift management across registers.

#### 4.3.2 Work Period Entity Changes

**Modified Table: `WorkPeriods`**

| Column | Type | Change | Description |
|--------|------|--------|-------------|
| `TerminalId` | int | ADD, FK, NOT NULL | Terminal this work period belongs to |
| `TerminalCode` | nvarchar(20) | ADD | Denormalized for reporting |

**New Index:**
```sql
CREATE INDEX IX_WorkPeriods_TerminalId_Status
ON WorkPeriods (TerminalId, Status)
INCLUDE (StartDateTime, EndDateTime);
```

#### 4.3.3 Work Period Rules

| Rule | Description |
|------|-------------|
| One Active per Terminal | Each terminal can have only one open work period |
| Terminal Isolation | Work periods are independent across terminals |
| Cashier Tracking | Multiple cashiers can work same terminal (tracked) |
| Cross-Terminal View | Supervisors can view all terminal work periods |

#### 4.3.4 Work Period Service Changes

```csharp
public interface IWorkPeriodService
{
    // Existing methods updated with terminal parameter
    Task<WorkPeriod> StartWorkPeriodAsync(
        int terminalId,
        int userId,
        decimal openingFloat);

    Task<WorkPeriod?> GetCurrentWorkPeriodAsync(int terminalId);

    Task<WorkPeriod> CloseWorkPeriodAsync(
        int terminalId,
        int userId,
        decimal closingCash);

    // New methods for multi-terminal
    Task<IReadOnlyList<WorkPeriod>> GetActiveWorkPeriodsAsync(int storeId);

    Task<IReadOnlyList<WorkPeriod>> GetWorkPeriodsByDateAsync(
        int storeId,
        DateTime date,
        int? terminalId = null);

    Task<bool> IsWorkPeriodOpenAsync(int terminalId);
}
```

#### 4.3.5 Cashier Session Tracking

**New Table: `WorkPeriodSessions`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `WorkPeriodId` | int | FK to WorkPeriods |
| `TerminalId` | int | FK to Terminals |
| `UserId` | int | FK to Users (Cashier) |
| `LoginAt` | datetime2 | When cashier logged in |
| `LogoutAt` | datetime2 | When cashier logged out |
| `SalesTotal` | decimal | Total sales during session |
| `TransactionCount` | int | Number of transactions |
| `CashReceived` | decimal | Cash collected |
| `CashPaidOut` | decimal | Cash refunded/paid out |

This allows tracking multiple cashiers per terminal per work period:

```
Work Period #1 (REG-001, Jan 24)
├── Session 1: John (08:00 - 12:00) - KES 50,000
├── Session 2: Jane (12:00 - 16:00) - KES 75,000
└── Session 3: John (16:00 - 20:00) - KES 45,000
```

#### 4.3.6 Acceptance Criteria

- [ ] Work periods are terminal-specific
- [ ] Starting work period requires terminal context
- [ ] Only one open work period per terminal allowed
- [ ] Multiple terminals can have concurrent open work periods
- [ ] Cashier login/logout tracked as sessions within work period
- [ ] Work period summary shows all cashier sessions
- [ ] Closing work period requires cash count for that terminal

---

### 4.4 Transaction Attribution

#### 4.4.1 Overview

All financial transactions must be tagged with terminal identification to enable accurate per-register reporting.

#### 4.4.2 Entities Requiring Terminal Attribution

| Entity | Status | Required Changes |
|--------|--------|------------------|
| `Receipt` | Partial | Add TerminalId (exists, needs wiring) |
| `Order` | Partial | Add TerminalId |
| `Payment` | New | Add TerminalId |
| `CashPayout` | Partial | Add TerminalId |
| `Refund` | New | Add TerminalId |
| `VoidTransaction` | New | Add TerminalId |
| `CashDrawerEvent` | New | Add TerminalId |

#### 4.4.3 Receipt Entity Changes

```csharp
public class Receipt : BaseEntity
{
    // Existing fields...

    /// <summary>
    /// Terminal where receipt was generated.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Terminal code (denormalized for reporting).
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Terminal Terminal { get; set; } = null!;
}
```

#### 4.4.4 Payment Recording

All payment methods must track terminal:

```csharp
public class ReceiptPayment : BaseEntity
{
    public int ReceiptId { get; set; }
    public int TerminalId { get; set; }        // ADD
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDateTime { get; set; }
}
```

#### 4.4.5 Terminal Session Context

The application must maintain terminal context throughout a session:

```csharp
public interface ITerminalSessionContext
{
    /// <summary>
    /// Current terminal ID (from local config).
    /// </summary>
    int TerminalId { get; }

    /// <summary>
    /// Current terminal code.
    /// </summary>
    string TerminalCode { get; }

    /// <summary>
    /// Current logged-in user ID.
    /// </summary>
    int? CurrentUserId { get; }

    /// <summary>
    /// Current active work period for this terminal.
    /// </summary>
    int? CurrentWorkPeriodId { get; }

    /// <summary>
    /// Store ID from terminal configuration.
    /// </summary>
    int StoreId { get; }
}
```

#### 4.4.6 Automatic Attribution

All service methods that create transactions must automatically inject terminal context:

```csharp
// Example: Receipt creation
public async Task<Receipt> CreateReceiptAsync(CreateReceiptRequest request)
{
    var receipt = new Receipt
    {
        // ... other fields
        TerminalId = _terminalSession.TerminalId,        // Auto-injected
        TerminalCode = _terminalSession.TerminalCode,    // Auto-injected
        UserId = _terminalSession.CurrentUserId!.Value,  // Auto-injected
        WorkPeriodId = _terminalSession.CurrentWorkPeriodId!.Value
    };

    // ...
}
```

#### 4.4.7 Acceptance Criteria

- [ ] All receipts tagged with TerminalId and TerminalCode
- [ ] All payments tagged with TerminalId
- [ ] All cash events (payout, drawer open) tagged with TerminalId
- [ ] Refunds and voids tagged with TerminalId
- [ ] Terminal context automatically injected by services
- [ ] Historical receipts without TerminalId handled gracefully
- [ ] Reports can filter by terminal

---

### 4.5 X-Report Generation

#### 4.5.1 Overview

The X-Report (eXamine Report) is a mid-shift snapshot of sales activity. It can be generated at any time, multiple times, and does NOT close the work period.

#### 4.5.2 X-Report Content

```
╔══════════════════════════════════════════════════════════════════╗
║                         X REPORT                                  ║
║                      (Mid-Shift Summary)                          ║
╠══════════════════════════════════════════════════════════════════╣
║  Business: SuperMart Nairobi                                      ║
║  Terminal: REG-001 (Register 1)                                   ║
║  Report #: X-2024-001-0042                                        ║
║  Generated: 24/01/2026 14:30:25                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  SHIFT INFORMATION                                                ║
║  ────────────────────────────────────────────────────────────────║
║  Shift Started:    24/01/2026 08:00:00                           ║
║  Current Time:     24/01/2026 14:30:25                           ║
║  Shift Duration:   6 hours 30 minutes                            ║
║                                                                   ║
║  CASHIERS THIS SHIFT                                              ║
║  ────────────────────────────────────────────────────────────────║
║  • John Mwangi (08:00 - 12:00)                                   ║
║  • Jane Wanjiku (12:00 - Current)                                ║
╠══════════════════════════════════════════════════════════════════╣
║  SALES SUMMARY                                                    ║
║  ────────────────────────────────────────────────────────────────║
║  Gross Sales:                              KES  125,450.00       ║
║  Discounts:                               (KES    5,230.00)      ║
║  Refunds:                                 (KES    2,100.00)      ║
║  Voids:                                   (KES      850.00)      ║
║                                            ──────────────────    ║
║  Net Sales:                                KES  117,270.00       ║
║  VAT (16%):                                KES   18,763.20       ║
║                                            ──────────────────    ║
║  GRAND TOTAL:                              KES  136,033.20       ║
╠══════════════════════════════════════════════════════════════════╣
║  PAYMENT METHOD BREAKDOWN                                         ║
║  ────────────────────────────────────────────────────────────────║
║  Cash:                                     KES   68,500.00       ║
║  M-Pesa:                                   KES   52,233.20       ║
║  Card:                                     KES   15,300.00       ║
║                                            ──────────────────    ║
║  Total Payments:                           KES  136,033.20       ║
╠══════════════════════════════════════════════════════════════════╣
║  CASH DRAWER STATUS                                               ║
║  ────────────────────────────────────────────────────────────────║
║  Opening Float:                            KES    5,000.00       ║
║  Cash Received:                            KES   68,500.00       ║
║  Cash Refunds:                            (KES    1,200.00)      ║
║  Cash Payouts:                            (KES    3,500.00)      ║
║                                            ──────────────────    ║
║  Expected in Drawer:                       KES   68,800.00       ║
╠══════════════════════════════════════════════════════════════════╣
║  TRANSACTION STATISTICS                                           ║
║  ────────────────────────────────────────────────────────────────║
║  Total Transactions:         142                                  ║
║  Average Transaction:        KES 957.98                          ║
║  Customers Served:           138                                  ║
║  Void Count:                 3                                    ║
║  Refund Count:               2                                    ║
║  Discount Count:             28                                   ║
║  Drawer Opens:               156                                  ║
╠══════════════════════════════════════════════════════════════════╣
║  * This is an X-Report - Work Period NOT closed                   ║
║  * Report can be regenerated at any time                          ║
╚══════════════════════════════════════════════════════════════════╝
```

#### 4.5.3 X-Report Service Interface

```csharp
public interface IXReportService
{
    /// <summary>
    /// Generates X-Report for a specific terminal's current work period.
    /// </summary>
    Task<XReportData> GenerateXReportAsync(int terminalId);

    /// <summary>
    /// Generates X-Report filtered by specific cashier.
    /// </summary>
    Task<XReportData> GenerateXReportForCashierAsync(
        int terminalId,
        int userId);

    /// <summary>
    /// Prints X-Report to terminal's configured printer.
    /// </summary>
    Task PrintXReportAsync(int terminalId, XReportData report);

    /// <summary>
    /// Exports X-Report to specified format.
    /// </summary>
    Task<byte[]> ExportXReportAsync(
        XReportData report,
        ExportFormat format);
}
```

#### 4.5.4 X-Report Data Transfer Object

```csharp
public class XReportData
{
    // Header
    public string BusinessName { get; set; }
    public string TerminalCode { get; set; }
    public string TerminalName { get; set; }
    public string ReportNumber { get; set; }
    public DateTime GeneratedAt { get; set; }

    // Shift Info
    public DateTime ShiftStarted { get; set; }
    public TimeSpan ShiftDuration { get; set; }
    public List<CashierSession> CashierSessions { get; set; }

    // Sales Summary
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal Voids { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }

    // Payment Breakdown
    public List<PaymentMethodSummary> PaymentBreakdown { get; set; }

    // Cash Drawer
    public decimal OpeningFloat { get; set; }
    public decimal CashReceived { get; set; }
    public decimal CashRefunds { get; set; }
    public decimal CashPayouts { get; set; }
    public decimal ExpectedCash { get; set; }

    // Statistics
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public int CustomerCount { get; set; }
    public int VoidCount { get; set; }
    public int RefundCount { get; set; }
    public int DiscountCount { get; set; }
    public int DrawerOpenCount { get; set; }
}

public class CashierSession
{
    public string CashierName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }  // null if current
    public decimal SalesTotal { get; set; }
    public int TransactionCount { get; set; }
}

public class PaymentMethodSummary
{
    public string PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}
```

#### 4.5.5 Acceptance Criteria

- [ ] X-Report generated for current terminal's active work period
- [ ] Shows all cashiers who worked the terminal during shift
- [ ] Displays complete sales summary with discounts/refunds/voids
- [ ] Shows payment method breakdown (Cash, M-Pesa, Card, etc.)
- [ ] Shows cash drawer expected balance
- [ ] Displays transaction statistics
- [ ] Can be printed to receipt printer
- [ ] Can be exported to PDF/Excel
- [ ] Does NOT close work period
- [ ] Can be generated multiple times
- [ ] Report number format: X-YYYY-TID-NNNN

---

### 4.6 Z-Report Generation

#### 4.6.1 Overview

The Z-Report (Zero-out Report) is a fiscal document generated at end of shift/day. It is an **immutable record** that closes the work period and resets transaction counters.

#### 4.6.2 Z-Report Requirements

| Requirement | Description |
|-------------|-------------|
| Cash Count Required | User must enter actual cash in drawer |
| Immutable | Cannot be edited after generation |
| Sequential | Report numbers never skip or repeat |
| Terminal-Specific | One Z-Report per terminal per work period |
| Audit Trail | Full logging of who/when/what |
| Integrity Hash | SHA256 hash of report data for tamper detection |

#### 4.6.3 Z-Report Content

```
╔══════════════════════════════════════════════════════════════════╗
║                         Z REPORT                                  ║
║                    (End of Shift Report)                          ║
║                    *** FISCAL DOCUMENT ***                        ║
╠══════════════════════════════════════════════════════════════════╣
║  Business: SuperMart Nairobi                                      ║
║  KRA PIN: P051234567X                                            ║
║  Address: Moi Avenue, Nairobi                                    ║
║  Phone: +254 712 345 678                                         ║
╠══════════════════════════════════════════════════════════════════╣
║  TERMINAL INFORMATION                                             ║
║  ────────────────────────────────────────────────────────────────║
║  Terminal:      REG-001 (Register 1)                             ║
║  Report #:      Z-2024-001-0156                                  ║
║  Work Period:   #1247                                            ║
║  Generated:     24/01/2026 20:15:00                              ║
║  Generated By:  Sarah Kimani (Supervisor)                        ║
╠══════════════════════════════════════════════════════════════════╣
║  SHIFT PERIOD                                                     ║
║  ────────────────────────────────────────────────────────────────║
║  Shift Start:   24/01/2026 08:00:00                              ║
║  Shift End:     24/01/2026 20:15:00                              ║
║  Duration:      12 hours 15 minutes                              ║
╠══════════════════════════════════════════════════════════════════╣
║  CASHIER SUMMARY                                                  ║
║  ────────────────────────────────────────────────────────────────║
║  Cashier              Time           Sales        Trans          ║
║  ─────────────────────────────────────────────────────────────   ║
║  John Mwangi      08:00-12:00    KES  62,500      68             ║
║  Jane Wanjiku     12:00-16:00    KES  85,230      92             ║
║  John Mwangi      16:00-20:15    KES  52,270      55             ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL                           KES 200,000     215             ║
╠══════════════════════════════════════════════════════════════════╣
║  SALES SUMMARY                                                    ║
║  ────────────────────────────────────────────────────────────────║
║  Gross Sales:                              KES  215,680.00       ║
║  Less: Discounts                          (KES   10,450.00)      ║
║  Less: Refunds                            (KES    3,230.00)      ║
║  Less: Voids                              (KES    2,000.00)      ║
║                                            ──────────────────    ║
║  Net Sales:                                KES  200,000.00       ║
║  VAT @ 16%:                                KES   32,000.00       ║
║                                            ──────────────────    ║
║  GRAND TOTAL:                              KES  232,000.00       ║
╠══════════════════════════════════════════════════════════════════╣
║  PAYMENT METHOD BREAKDOWN                                         ║
║  ────────────────────────────────────────────────────────────────║
║  Payment Method              Amount           Count              ║
║  ─────────────────────────────────────────────────────────────   ║
║  Cash                    KES  120,500.00         128             ║
║  M-Pesa                  KES   85,000.00          72             ║
║  Visa/Mastercard         KES   20,000.00          12             ║
║  Loyalty Points          KES    6,500.00           3             ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL                   KES  232,000.00         215             ║
╠══════════════════════════════════════════════════════════════════╣
║  CASH RECONCILIATION                                              ║
║  ────────────────────────────────────────────────────────────────║
║  Opening Float:                            KES    5,000.00       ║
║  Cash Sales Received:                      KES  120,500.00       ║
║  Cash Refunds Paid:                       (KES    2,100.00)      ║
║  Cash Payouts:                            (KES    8,000.00)      ║
║                                            ──────────────────    ║
║  EXPECTED CASH:                            KES  115,400.00       ║
║                                                                   ║
║  Actual Cash Counted:                      KES  115,350.00       ║
║                                            ──────────────────    ║
║  VARIANCE:                                (KES       50.00)      ║
║  Status: SHORT                                                    ║
║  Explanation: Coin counting rounding                             ║
╠══════════════════════════════════════════════════════════════════╣
║  TRANSACTION STATISTICS                                           ║
║  ────────────────────────────────────────────────────────────────║
║  Total Transactions:         215                                  ║
║  Average Transaction:        KES 1,079.07                        ║
║  Customers Served:           208                                  ║
║  Items Sold:                 1,247                                ║
║  Void Transactions:          4                                    ║
║  Refund Transactions:        3                                    ║
║  Discounts Applied:          52                                   ║
║  Cash Drawer Opens:          245                                  ║
║  No-Sale Opens:              12                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  TAX SUMMARY                                                      ║
║  ────────────────────────────────────────────────────────────────║
║  Tax Rate          Taxable Amount        Tax Collected           ║
║  ─────────────────────────────────────────────────────────────   ║
║  VAT 16%           KES 200,000.00        KES 32,000.00           ║
║  Exempt            KES       0.00        KES      0.00           ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL TAX                               KES 32,000.00           ║
╠══════════════════════════════════════════════════════════════════╣
║  REPORT VERIFICATION                                              ║
║  ────────────────────────────────────────────────────────────────║
║  Report Hash: 7a3f2b1c9e8d...4f5a6b7c                            ║
║  Sequence:    Verified (Previous: Z-2024-001-0155)               ║
║                                                                   ║
║  *** THIS IS AN OFFICIAL FISCAL DOCUMENT ***                     ║
║  *** RETAIN FOR TAX COMPLIANCE (6+ YEARS) ***                    ║
╚══════════════════════════════════════════════════════════════════╝
```

#### 4.6.4 Z-Report Generation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Z-REPORT GENERATION FLOW                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Validation                                               │
│ ─────────────────────────────────────────────────────────────── │
│ • Check work period is open                                      │
│ • Check no pending transactions                                  │
│ • Check no held orders                                          │
│ • Check user has permission                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Cash Count Entry                                         │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│  CASH DENOMINATION COUNT                                         │
│  ───────────────────────────────────────                        │
│  KES 1000 notes:  [  15  ] x 1000 =    15,000                   │
│  KES 500 notes:   [  42  ] x  500 =    21,000                   │
│  KES 200 notes:   [  85  ] x  200 =    17,000                   │
│  KES 100 notes:   [ 234  ] x  100 =    23,400                   │
│  KES 50 notes:    [ 156  ] x   50 =     7,800                   │
│  KES 40 coins:    [  28  ] x   40 =     1,120                   │
│  KES 20 coins:    [  45  ] x   20 =       900                   │
│  KES 10 coins:    [  63  ] x   10 =       630                   │
│  KES 5 coins:     [  40  ] x    5 =       200                   │
│  KES 1 coins:     [ 300  ] x    1 =       300                   │
│                            ─────────────────                     │
│  TOTAL COUNTED:                       115,350                    │
│                                                                  │
│  Expected:    KES 115,400.00                                    │
│  Counted:     KES 115,350.00                                    │
│  Variance:   (KES      50.00) SHORT                             │
│                                                                  │
│  Variance Explanation (required if > threshold):                 │
│  [ Coin counting rounding error                          ]       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Preview & Confirmation                                   │
│ ─────────────────────────────────────────────────────────────── │
│ • Display Z-Report preview                                       │
│ • User reviews all figures                                       │
│ • Confirm to generate (cannot be undone)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: Generation                                               │
│ ─────────────────────────────────────────────────────────────── │
│ • Create immutable ZReportRecord                                 │
│ • Generate sequential report number                              │
│ • Calculate integrity hash                                       │
│ • Close work period                                              │
│ • Log audit trail                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 5: Output                                                   │
│ ─────────────────────────────────────────────────────────────── │
│ • Print to receipt printer                                       │
│ • Save PDF copy                                                  │
│ • Email to configured recipients (optional)                      │
│ • Sync to cloud (if enabled)                                     │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.6.5 Z-Report Service Interface

```csharp
public interface IZReportService
{
    /// <summary>
    /// Validates if Z-Report can be generated for terminal.
    /// </summary>
    Task<ZReportValidationResult> ValidateCanGenerateAsync(int terminalId);

    /// <summary>
    /// Generates preview of Z-Report without committing.
    /// </summary>
    Task<ZReportPreview> PreviewZReportAsync(int terminalId);

    /// <summary>
    /// Generates and commits Z-Report, closing work period.
    /// </summary>
    Task<ZReportRecord> GenerateZReportAsync(
        int terminalId,
        decimal actualCashCounted,
        int generatedByUserId,
        string? varianceExplanation,
        List<CashDenominationCount>? denominationCounts);

    /// <summary>
    /// Gets Z-Report by ID.
    /// </summary>
    Task<ZReportRecord?> GetZReportAsync(int reportId);

    /// <summary>
    /// Gets Z-Reports for terminal within date range.
    /// </summary>
    Task<IReadOnlyList<ZReportRecord>> GetZReportsAsync(
        int terminalId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Prints Z-Report to terminal printer.
    /// </summary>
    Task PrintZReportAsync(ZReportRecord report);

    /// <summary>
    /// Exports Z-Report to specified format.
    /// </summary>
    Task<byte[]> ExportZReportAsync(
        ZReportRecord report,
        ExportFormat format);

    /// <summary>
    /// Verifies Z-Report integrity using hash.
    /// </summary>
    Task<bool> VerifyIntegrityAsync(int reportId);

    /// <summary>
    /// Gets the next sequential report number for terminal.
    /// </summary>
    Task<string> GetNextReportNumberAsync(int terminalId);
}
```

#### 4.6.6 Acceptance Criteria

- [ ] Z-Report requires open work period for terminal
- [ ] Validation checks for pending transactions
- [ ] Cash denomination count screen presented
- [ ] Variance calculated (expected vs actual)
- [ ] Variance explanation required if over threshold
- [ ] Preview shown before final generation
- [ ] Report number sequential per terminal (Z-YYYY-TID-NNNN)
- [ ] Work period closed after generation
- [ ] Immutable record created with hash
- [ ] Automatic print to receipt printer
- [ ] PDF copy saved to configured location
- [ ] All cashiers who worked shift listed with individual totals
- [ ] Payment method breakdown included
- [ ] Tax summary included
- [ ] Cannot be edited or deleted after generation

---

### 4.7 Combined Multi-Register Reports

#### 4.7.1 Overview

Combined reports aggregate data from multiple terminals to provide a store-wide view. These are essential for managers to see overall performance while maintaining per-register detail.

#### 4.7.2 Combined X-Report (Store Snapshot)

```
╔══════════════════════════════════════════════════════════════════╗
║                  COMBINED X REPORT                                ║
║               (Store-Wide Mid-Day Snapshot)                       ║
╠══════════════════════════════════════════════════════════════════╣
║  Business: SuperMart Nairobi                                      ║
║  Report #: CX-2024-0042                                          ║
║  Generated: 24/01/2026 14:30:00                                  ║
║  Generated By: Manager - Peter Ochieng                           ║
╠══════════════════════════════════════════════════════════════════╣
║  REGISTERS INCLUDED                                               ║
║  ────────────────────────────────────────────────────────────────║
║  [✓] REG-001 - Register 1 (Active: Jane Wanjiku)                 ║
║  [✓] REG-002 - Register 2 (Active: John Mwangi)                  ║
║  [✓] REG-003 - Register 3 (Active: Mary Akinyi)                  ║
║  [ ] REG-004 - Register 4 (Closed)                               ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REGISTER: REG-001 (Register 1)                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  Cashiers Today:                                                  ║
║    • John Mwangi (08:00 - 12:00)                                 ║
║    • Jane Wanjiku (12:00 - Current)                              ║
║                                                                   ║
║  Sales Summary:                                                   ║
║    Net Sales:           KES  85,000.00                           ║
║    VAT:                 KES  13,600.00                           ║
║    Total:               KES  98,600.00                           ║
║                                                                   ║
║  Payment Breakdown:                                               ║
║    Cash:                KES  52,000.00                           ║
║    M-Pesa:              KES  35,600.00                           ║
║    Card:                KES  11,000.00                           ║
║                                                                   ║
║  Transactions: 92   |   Avg: KES 1,071.74                        ║
║  ─────────────────────────────────────────────────────────────   ║
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REGISTER: REG-002 (Register 2)                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  Cashiers Today:                                                  ║
║    • Peter Kamau (08:00 - Current)                               ║
║                                                                   ║
║  Sales Summary:                                                   ║
║    Net Sales:           KES  72,500.00                           ║
║    VAT:                 KES  11,600.00                           ║
║    Total:               KES  84,100.00                           ║
║                                                                   ║
║  Payment Breakdown:                                               ║
║    Cash:                KES  45,000.00                           ║
║    M-Pesa:              KES  28,100.00                           ║
║    Card:                KES  11,000.00                           ║
║                                                                   ║
║  Transactions: 78   |   Avg: KES 1,078.21                        ║
║  ─────────────────────────────────────────────────────────────   ║
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REGISTER: REG-003 (Register 3)                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  Cashiers Today:                                                  ║
║    • Grace Njeri (08:00 - 12:00)                                 ║
║    • Mary Akinyi (12:00 - Current)                               ║
║                                                                   ║
║  Sales Summary:                                                   ║
║    Net Sales:           KES  62,500.00                           ║
║    VAT:                 KES  10,000.00                           ║
║    Total:               KES  72,500.00                           ║
║                                                                   ║
║  Payment Breakdown:                                               ║
║    Cash:                KES  38,500.00                           ║
║    M-Pesa:              KES  24,000.00                           ║
║    Card:                KES  10,000.00                           ║
║                                                                   ║
║  Transactions: 65   |   Avg: KES 1,115.38                        ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  ███████████████████████████████████████████████████████████████ ║
║  █                    STORE TOTALS                              █ ║
║  ███████████████████████████████████████████████████████████████ ║
║                                                                   ║
║  SALES BY REGISTER                                                ║
║  ────────────────────────────────────────────────────────────────║
║  Register          Net Sales         VAT            Total        ║
║  ─────────────────────────────────────────────────────────────   ║
║  REG-001       KES  85,000.00   KES 13,600.00   KES  98,600.00  ║
║  REG-002       KES  72,500.00   KES 11,600.00   KES  84,100.00  ║
║  REG-003       KES  62,500.00   KES 10,000.00   KES  72,500.00  ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL         KES 220,000.00   KES 35,200.00   KES 255,200.00  ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  TOTAL PAYMENTS BY METHOD                                         ║
║  ────────────────────────────────────────────────────────────────║
║                                                                   ║
║  Payment Method    REG-001      REG-002      REG-003     TOTAL   ║
║  ─────────────────────────────────────────────────────────────   ║
║  Cash          KES 52,000   KES 45,000   KES 38,500   KES 135,500║
║  M-Pesa        KES 35,600   KES 28,100   KES 24,000   KES  87,700║
║  Card          KES 11,000   KES 11,000   KES 10,000   KES  32,000║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL         KES 98,600   KES 84,100   KES 72,500   KES 255,200║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  PAYMENT METHOD SUMMARY                                           ║
║  ────────────────────────────────────────────────────────────────║
║                                                                   ║
║  ┌─────────────────────────────────────────────────────────────┐ ║
║  │ Cash                                                        │ ║
║  │ ████████████████████████████████████░░░░░░░   KES 135,500  │ ║
║  │ 53.1%                                                       │ ║
║  ├─────────────────────────────────────────────────────────────┤ ║
║  │ M-Pesa                                                      │ ║
║  │ ██████████████████████████░░░░░░░░░░░░░░░░░   KES  87,700  │ ║
║  │ 34.4%                                                       │ ║
║  ├─────────────────────────────────────────────────────────────┤ ║
║  │ Card                                                        │ ║
║  │ ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   KES  32,000  │ ║
║  │ 12.5%                                                       │ ║
║  └─────────────────────────────────────────────────────────────┘ ║
║                                                                   ║
║  GRAND TOTAL:                              KES 255,200.00        ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  STORE STATISTICS                                                 ║
║  ────────────────────────────────────────────────────────────────║
║  Total Transactions:          235                                 ║
║  Average Transaction:         KES 1,085.96                       ║
║  Customers Served:            228                                 ║
║  Active Registers:            3 of 4                              ║
╠══════════════════════════════════════════════════════════════════╣
║  * Combined X-Report - Work Periods NOT closed                    ║
║  * Individual register Z-Reports still required                   ║
╚══════════════════════════════════════════════════════════════════╝
```

#### 4.7.3 Combined Z-Report (End of Day)

The Combined Z-Report is generated **after** all individual terminal Z-Reports have been closed. It provides:

1. **Summary per Register** - Each register's final Z-Report figures
2. **All Cashiers** - Every cashier who worked each register
3. **Payment Breakdown per Register** - Cash, M-Pesa, Card per register
4. **Grand Totals** - Store-wide totals
5. **Cash Reconciliation** - Combined cash position

```
╔══════════════════════════════════════════════════════════════════╗
║                   COMBINED Z REPORT                               ║
║              (End of Day Store Summary)                           ║
║                 *** FISCAL DOCUMENT ***                           ║
╠══════════════════════════════════════════════════════════════════╣
║  Business: SuperMart Nairobi                                      ║
║  KRA PIN: P051234567X                                            ║
║  Date: 24/01/2026                                                ║
║  Report #: CZ-2024-0089                                          ║
║  Generated: 24/01/2026 21:30:00                                  ║
║  Generated By: Manager - Peter Ochieng                           ║
╠══════════════════════════════════════════════════════════════════╣
║  INCLUDED Z-REPORTS                                               ║
║  ────────────────────────────────────────────────────────────────║
║  Z-2024-001-0156  REG-001  Closed: 20:15  By: Sarah Kimani       ║
║  Z-2024-002-0142  REG-002  Closed: 20:30  By: James Oduor        ║
║  Z-2024-003-0098  REG-003  Closed: 20:45  By: Sarah Kimani       ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REG-001 - Register 1                          Z-2024-001-0156   ║
║  ═══════════════════════════════════════════════════════════════ ║
║                                                                   ║
║  CASHIERS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cashier            Time          Sales       Transactions  │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ John Mwangi    08:00-12:00   KES  62,500          68       │  ║
║  │ Jane Wanjiku   12:00-16:00   KES  85,230          92       │  ║
║  │ John Mwangi    16:00-20:15   KES  52,270          55       │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ REGISTER TOTAL               KES 200,000         215       │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  PAYMENTS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cash                              KES 120,500.00           │  ║
║  │ M-Pesa                            KES  85,000.00           │  ║
║  │ Card                              KES  26,500.00           │  ║
║  │ ────────────────────────────────────────────────────────── │  ║
║  │ TOTAL                             KES 232,000.00           │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  CASH RECONCILIATION:                                             ║
║    Expected: KES 115,400.00  |  Actual: KES 115,350.00           ║
║    Variance: (KES 50.00) SHORT                                   ║
║                                                                   ║
║  ─────────────────────────────────────────────────────────────   ║
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REG-002 - Register 2                          Z-2024-002-0142   ║
║  ═══════════════════════════════════════════════════════════════ ║
║                                                                   ║
║  CASHIERS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cashier            Time          Sales       Transactions  │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ Peter Kamau    08:00-14:00   KES  95,000          98       │  ║
║  │ Alice Wambui   14:00-20:30   KES  80,000          82       │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ REGISTER TOTAL               KES 175,000         180       │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  PAYMENTS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cash                              KES  98,000.00           │  ║
║  │ M-Pesa                            KES  72,500.00           │  ║
║  │ Card                              KES  32,500.00           │  ║
║  │ ────────────────────────────────────────────────────────── │  ║
║  │ TOTAL                             KES 203,000.00           │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  CASH RECONCILIATION:                                             ║
║    Expected: KES 98,500.00  |  Actual: KES 98,500.00             ║
║    Variance: KES 0.00 BALANCED                                   ║
║                                                                   ║
║  ─────────────────────────────────────────────────────────────   ║
║                                                                   ║
║  ═══════════════════════════════════════════════════════════════ ║
║  REG-003 - Register 3                          Z-2024-003-0098   ║
║  ═══════════════════════════════════════════════════════════════ ║
║                                                                   ║
║  CASHIERS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cashier            Time          Sales       Transactions  │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ Grace Njeri    08:00-16:00   KES  88,000          85       │  ║
║  │ Mary Akinyi    16:00-20:45   KES  62,000          60       │  ║
║  ├────────────────────────────────────────────────────────────┤  ║
║  │ REGISTER TOTAL               KES 150,000         145       │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  PAYMENTS:                                                        ║
║  ┌────────────────────────────────────────────────────────────┐  ║
║  │ Cash                              KES  82,000.00           │  ║
║  │ M-Pesa                            KES  58,500.00           │  ║
║  │ Card                              KES  33,500.00           │  ║
║  │ ────────────────────────────────────────────────────────── │  ║
║  │ TOTAL                             KES 174,000.00           │  ║
║  └────────────────────────────────────────────────────────────┘  ║
║                                                                   ║
║  CASH RECONCILIATION:                                             ║
║    Expected: KES 82,200.00  |  Actual: KES 82,100.00             ║
║    Variance: (KES 100.00) SHORT                                  ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  ███████████████████████████████████████████████████████████████ ║
║  █                  STORE GRAND TOTALS                          █ ║
║  ███████████████████████████████████████████████████████████████ ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  SALES BY REGISTER                                                ║
║  ────────────────────────────────────────────────────────────────║
║  Register       Net Sales          VAT           Total           ║
║  ─────────────────────────────────────────────────────────────   ║
║  REG-001     KES 200,000.00   KES 32,000.00   KES 232,000.00    ║
║  REG-002     KES 175,000.00   KES 28,000.00   KES 203,000.00    ║
║  REG-003     KES 150,000.00   KES 24,000.00   KES 174,000.00    ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL       KES 525,000.00   KES 84,000.00   KES 609,000.00    ║
╠══════════════════════════════════════════════════════════════════╣
║  PAYMENTS BY METHOD (PER REGISTER)                                ║
║  ────────────────────────────────────────────────────────────────║
║                                                                   ║
║  Method        REG-001       REG-002       REG-003       TOTAL   ║
║  ─────────────────────────────────────────────────────────────   ║
║  Cash      KES 120,500   KES  98,000   KES  82,000   KES 300,500 ║
║  M-Pesa    KES  85,000   KES  72,500   KES  58,500   KES 216,000 ║
║  Card      KES  26,500   KES  32,500   KES  33,500   KES  92,500 ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL     KES 232,000   KES 203,000   KES 174,000   KES 609,000 ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  PAYMENT METHOD TOTALS                                            ║
║  ────────────────────────────────────────────────────────────────║
║                                                                   ║
║  ┌─────────────────────────────────────────────────────────────┐ ║
║  │ CASH                                                        │ ║
║  │ ████████████████████████████████████████░░░░░   KES 300,500 │ ║
║  │ 49.3%    (128 + 98 + 82 = 308 transactions)                 │ ║
║  ├─────────────────────────────────────────────────────────────┤ ║
║  │ M-PESA                                                      │ ║
║  │ ███████████████████████████████░░░░░░░░░░░░░   KES 216,000 │ ║
║  │ 35.5%    (72 + 65 + 58 = 195 transactions)                  │ ║
║  ├─────────────────────────────────────────────────────────────┤ ║
║  │ CARD                                                        │ ║
║  │ ████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   KES  92,500 │ ║
║  │ 15.2%    (15 + 17 + 15 = 47 transactions)                   │ ║
║  └─────────────────────────────────────────────────────────────┘ ║
║                                                                   ║
║                    GRAND TOTAL: KES 609,000.00                   ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  COMBINED CASH RECONCILIATION                                     ║
║  ────────────────────────────────────────────────────────────────║
║                                                                   ║
║  Register      Expected        Actual        Variance            ║
║  ─────────────────────────────────────────────────────────────   ║
║  REG-001    KES 115,400   KES 115,350   (KES   50) SHORT        ║
║  REG-002    KES  98,500   KES  98,500    KES    0  BALANCED     ║
║  REG-003    KES  82,200   KES  82,100   (KES  100) SHORT        ║
║  ─────────────────────────────────────────────────────────────   ║
║  TOTAL      KES 296,100   KES 295,950   (KES  150) SHORT        ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  STORE STATISTICS                                                 ║
║  ────────────────────────────────────────────────────────────────║
║  Total Transactions:          540                                 ║
║  Average Transaction:         KES 1,127.78                       ║
║  Customers Served:            528                                 ║
║  Items Sold:                  3,245                               ║
║  Registers Operated:          3                                   ║
║  Cashiers Worked:             7                                   ║
║                                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  REPORT VERIFICATION                                              ║
║  ────────────────────────────────────────────────────────────────║
║  Combined Report Hash: 9c4e7d2a1b8f...3e6f5a4c                   ║
║  All individual Z-Reports verified                                ║
║                                                                   ║
║  *** THIS IS AN OFFICIAL FISCAL DOCUMENT ***                     ║
║  *** RETAIN FOR TAX COMPLIANCE (6+ YEARS) ***                    ║
╚══════════════════════════════════════════════════════════════════╝
```

#### 4.7.4 Combined Report Service Interface

```csharp
public interface ICombinedReportService
{
    /// <summary>
    /// Generates combined X-Report for multiple terminals (snapshot).
    /// </summary>
    Task<CombinedXReportData> GenerateCombinedXReportAsync(
        int storeId,
        int[]? terminalIds = null);  // null = all active terminals

    /// <summary>
    /// Generates combined Z-Report after all terminals closed.
    /// </summary>
    Task<CombinedZReportData> GenerateCombinedZReportAsync(
        int storeId,
        DateTime reportDate);

    /// <summary>
    /// Checks if all terminals have closed for the day.
    /// </summary>
    Task<TerminalClosureStatus> GetTerminalClosureStatusAsync(
        int storeId,
        DateTime date);

    /// <summary>
    /// Gets combined report history.
    /// </summary>
    Task<IReadOnlyList<CombinedZReportRecord>> GetCombinedReportsAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate);
}
```

#### 4.7.5 Combined Report DTOs

```csharp
public class CombinedXReportData
{
    public string BusinessName { get; set; }
    public string ReportNumber { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string GeneratedByName { get; set; }

    // Per-terminal data
    public List<TerminalXReportSummary> TerminalSummaries { get; set; }

    // Store totals
    public decimal TotalNetSales { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGrandSales { get; set; }

    // Payment breakdown per terminal
    public List<TerminalPaymentBreakdown> PaymentsByTerminal { get; set; }

    // Payment method totals
    public List<PaymentMethodTotal> PaymentMethodTotals { get; set; }

    // Statistics
    public int TotalTransactions { get; set; }
    public decimal AverageTransaction { get; set; }
    public int ActiveTerminalCount { get; set; }
    public int TotalTerminalCount { get; set; }
}

public class TerminalXReportSummary
{
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; }
    public string TerminalName { get; set; }
    public bool IsActive { get; set; }
    public string CurrentCashierName { get; set; }
    public List<CashierSession> CashierSessions { get; set; }
    public decimal NetSales { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
}

public class TerminalPaymentBreakdown
{
    public string TerminalCode { get; set; }
    public decimal CashAmount { get; set; }
    public decimal MpesaAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal Total { get; set; }
}

public class PaymentMethodTotal
{
    public string PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
```

#### 4.7.6 Combined Report Rules

| Rule | Description |
|------|-------------|
| X-Report Any Time | Combined X-Report can be generated while terminals are open |
| Z-Report After Close | Combined Z-Report requires all included terminals to have closed |
| Selective Inclusion | Manager can choose which terminals to include |
| No Double Counting | Combined report aggregates, not duplicates |
| Audit Trail | Combined report references all source Z-Reports |
| Immutable | Combined Z-Report cannot be modified after generation |

#### 4.7.7 Acceptance Criteria

- [ ] Combined X-Report shows all active terminals
- [ ] Combined X-Report can be generated any time (not closing)
- [ ] Per-terminal breakdown includes cashier names and sales
- [ ] Payment method breakdown shown per terminal
- [ ] Payment method totals shown for entire store
- [ ] Combined Z-Report requires all terminals closed
- [ ] Combined Z-Report references individual Z-Report numbers
- [ ] Cash reconciliation shown per register and combined
- [ ] Grand totals clearly displayed
- [ ] Visual payment method breakdown (percentages)
- [ ] Report can be printed, exported to PDF/Excel

---

### 4.8 Terminal Health Monitoring

#### 4.8.1 Overview

Terminal health monitoring ensures visibility into the status of all terminals across the store network. This is critical for identifying offline terminals, connection issues, or hardware problems.

#### 4.8.2 Heartbeat Mechanism

Each terminal sends periodic heartbeats to the database:

```csharp
public class TerminalHeartbeat
{
    public int TerminalId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsDatabaseConnected { get; set; }
    public int DatabaseLatencyMs { get; set; }
    public bool IsPrinterAvailable { get; set; }
    public bool IsCashDrawerAvailable { get; set; }
    public string CurrentUserId { get; set; }
    public bool IsWorkPeriodOpen { get; set; }
    public string IpAddress { get; set; }
    public string AppVersion { get; set; }
}
```

#### 4.8.3 Terminal Status Dashboard

```
┌─────────────────────────────────────────────────────────────────┐
│                    TERMINAL STATUS DASHBOARD                     │
│                    Last Updated: 14:30:25                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│  │ REG-001  │  │ REG-002  │  │ REG-003  │  │ REG-004  │        │
│  │  ● ONLINE │  │  ● ONLINE │  │  ● ONLINE │  │  ○ OFFLINE│       │
│  │          │  │          │  │          │  │          │        │
│  │ Jane W.  │  │ Peter K. │  │ Mary A.  │  │ No User  │        │
│  │          │  │          │  │          │  │          │        │
│  │ WP: Open │  │ WP: Open │  │ WP: Open │  │ WP: None │        │
│  │          │  │          │  │          │  │          │        │
│  │ [Printer]│  │ [Printer]│  │ [Printer]│  │ [!Print] │        │
│  │ [Drawer] │  │ [Drawer] │  │ [Drawer] │  │ [Drawer] │        │
│  │          │  │          │  │          │  │          │        │
│  │ 14:30:20 │  │ 14:30:22 │  │ 14:30:18 │  │ 12:45:00 │        │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │
│                                                                  │
│  Legend:  ● Online   ○ Offline   [!] Warning                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.8.4 Health Service Interface

```csharp
public interface ITerminalHealthService
{
    /// <summary>
    /// Records heartbeat for current terminal.
    /// </summary>
    Task RecordHeartbeatAsync(TerminalHeartbeat heartbeat);

    /// <summary>
    /// Gets status of all terminals in store.
    /// </summary>
    Task<IReadOnlyList<TerminalStatus>> GetTerminalStatusesAsync(int storeId);

    /// <summary>
    /// Checks if terminal is considered online (heartbeat within threshold).
    /// </summary>
    Task<bool> IsTerminalOnlineAsync(int terminalId);

    /// <summary>
    /// Gets terminals with issues (offline, hardware problems).
    /// </summary>
    Task<IReadOnlyList<TerminalAlert>> GetTerminalAlertsAsync(int storeId);
}
```

#### 4.8.5 Acceptance Criteria

- [ ] Heartbeat sent every 60 seconds from each terminal
- [ ] Terminal status dashboard shows all terminals
- [ ] Online/Offline status based on heartbeat timestamp
- [ ] Hardware status indicators (printer, drawer)
- [ ] Current user displayed for each terminal
- [ ] Work period status displayed
- [ ] Alerts for terminals offline > 5 minutes
- [ ] Alerts for hardware issues

---

### 4.9 Admin Terminal Management UI

#### 4.9.1 Overview

Administrators need a centralized interface to manage all terminals, including registration, configuration, and monitoring.

#### 4.9.2 Terminal Management View

```
┌─────────────────────────────────────────────────────────────────┐
│  TERMINAL MANAGEMENT                                [+ Add New]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Filter: [All Types ▼] [All Status ▼]    Search: [________]     │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Code     │ Name       │ Type    │ Status │ Last Seen │ User ││
│  ├─────────────────────────────────────────────────────────────┤│
│  │ REG-001  │ Register 1 │ Register│ Online │ Just now  │ Jane ││
│  │ REG-002  │ Register 2 │ Register│ Online │ 30s ago   │ Peter││
│  │ REG-003  │ Register 3 │ Register│ Online │ 45s ago   │ Mary ││
│  │ REG-004  │ Register 4 │ Register│ Offline│ 2h ago    │ -    ││
│  │ TILL-001 │ Till 1     │ Till    │ Online │ 20s ago   │ James││
│  │ ADMIN-001│ Back Office│ Admin   │ Online │ Just now  │ Admin││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Selected: REG-004                                               │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ TERMINAL DETAILS                                            ││
│  │ ───────────────────────────────────────────────────────────││
│  │ Code:           REG-004                                     ││
│  │ Name:           Register 4                                  ││
│  │ Type:           Supermarket Register                        ││
│  │ Machine ID:     B8:27:EB:4A:C3:E1                          ││
│  │ IP Address:     192.168.1.104                              ││
│  │ Status:         Offline (Last seen: 2 hours ago)           ││
│  │ Created:        15/01/2026 by Admin                        ││
│  │                                                             ││
│  │ Hardware Configuration:                                     ││
│  │   Printer: EPSON TM-T88VI (USB)                            ││
│  │   Drawer:  COM1 (Printer-triggered)                        ││
│  │   Scanner: USB HID                                         ││
│  │                                                             ││
│  │ [Edit] [View Reports] [Deactivate] [Delete]                ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.9.3 Terminal Configuration Editor

```
┌─────────────────────────────────────────────────────────────────┐
│  EDIT TERMINAL: REG-004                              [X] Close   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Basic Information                                               │
│  ───────────────────────────────────────────────────────────────│
│  Terminal Code:  [REG-004        ]  (cannot change after create)│
│  Terminal Name:  [Register 4     ]                              │
│  Description:    [Express checkout lane            ]            │
│  Terminal Type:  [Register         ▼]                           │
│  Business Mode:  [Supermarket      ▼]                           │
│                                                                  │
│  Hardware Configuration                                          │
│  ───────────────────────────────────────────────────────────────│
│  Receipt Printer:                                                │
│    Name: [EPSON TM-T88VI      ▼]                                │
│    Port: [USB                 ▼]                                │
│    Paper Width: (•) 80mm  ( ) 58mm                              │
│                                                                  │
│  Cash Drawer:                                                    │
│    Type: [Printer-Triggered   ▼]                                │
│    Port: [COM1                ▼]  (if serial)                   │
│                                                                  │
│  Barcode Scanner:                                                │
│    Type: [USB HID             ▼]                                │
│    Suffix: [\r (Enter)        ▼]                                │
│                                                                  │
│  Customer Display:                                               │
│    [✓] Enable                                                    │
│    Type: [VFD                 ▼]                                │
│    Port: [COM2                ▼]                                │
│                                                                  │
│  Settings                                                        │
│  ───────────────────────────────────────────────────────────────│
│  [✓] Print receipt automatically                                 │
│  [✓] Open drawer on cash sale                                    │
│  [ ] Is main register (for consolidated reports)                 │
│  Auto-logout after: [30  ] minutes of inactivity                │
│                                                                  │
│                          [Cancel]  [Save Changes]                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.9.4 Acceptance Criteria

- [ ] List all terminals with status indicators
- [ ] Filter by type and status
- [ ] Search by code or name
- [ ] View terminal details including hardware config
- [ ] Edit terminal configuration
- [ ] Add new terminal (pre-registration)
- [ ] Deactivate terminal (soft delete)
- [ ] View terminal's Z-Report history
- [ ] Force close work period (emergency)
- [ ] Clear machine binding (re-assign to different machine)

---

## 5. Database Schema Changes

### 5.1 New Tables

#### 5.1.1 Terminals Table

```sql
CREATE TABLE Terminals (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(Id),
    Code NVARCHAR(20) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    MachineIdentifier NVARCHAR(100) NOT NULL,
    TerminalType INT NOT NULL,  -- Enum: Register, Till, Admin, KDS
    BusinessMode INT NOT NULL,  -- Enum: Supermarket, Restaurant, Admin
    IsActive BIT NOT NULL DEFAULT 1,
    IsMainRegister BIT NOT NULL DEFAULT 0,
    LastHeartbeat DATETIME2 NULL,
    LastLoginUserId INT NULL FOREIGN KEY REFERENCES Users(Id),
    LastLoginAt DATETIME2 NULL,
    IpAddress NVARCHAR(45) NULL,
    PrinterConfiguration NVARCHAR(MAX) NULL,  -- JSON
    HardwareConfiguration NVARCHAR(MAX) NULL,  -- JSON
    Settings NVARCHAR(MAX) NULL,  -- JSON
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ModifiedAt DATETIME2 NULL,
    ModifiedByUserId INT NULL FOREIGN KEY REFERENCES Users(Id),

    CONSTRAINT UQ_Terminals_Code UNIQUE (StoreId, Code),
    CONSTRAINT UQ_Terminals_MachineIdentifier UNIQUE (MachineIdentifier)
);

CREATE INDEX IX_Terminals_StoreId ON Terminals (StoreId);
CREATE INDEX IX_Terminals_IsActive ON Terminals (IsActive);
CREATE INDEX IX_Terminals_LastHeartbeat ON Terminals (LastHeartbeat);
```

#### 5.1.2 WorkPeriodSessions Table

```sql
CREATE TABLE WorkPeriodSessions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    WorkPeriodId INT NOT NULL FOREIGN KEY REFERENCES WorkPeriods(Id),
    TerminalId INT NOT NULL FOREIGN KEY REFERENCES Terminals(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    LoginAt DATETIME2 NOT NULL,
    LogoutAt DATETIME2 NULL,
    SalesTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    TransactionCount INT NOT NULL DEFAULT 0,
    CashReceived DECIMAL(18,2) NOT NULL DEFAULT 0,
    CashPaidOut DECIMAL(18,2) NOT NULL DEFAULT 0,

    CONSTRAINT UQ_WorkPeriodSessions_Active UNIQUE (WorkPeriodId, UserId, LogoutAt)
);

CREATE INDEX IX_WorkPeriodSessions_WorkPeriodId ON WorkPeriodSessions (WorkPeriodId);
CREATE INDEX IX_WorkPeriodSessions_UserId ON WorkPeriodSessions (UserId);
```

#### 5.1.3 CombinedZReports Table

```sql
CREATE TABLE CombinedZReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StoreId INT NOT NULL FOREIGN KEY REFERENCES Stores(Id),
    ReportNumber NVARCHAR(30) NOT NULL,
    ReportDate DATE NOT NULL,
    GeneratedAt DATETIME2 NOT NULL,
    GeneratedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),

    -- Aggregated totals
    TotalNetSales DECIMAL(18,2) NOT NULL,
    TotalVat DECIMAL(18,2) NOT NULL,
    TotalGrandSales DECIMAL(18,2) NOT NULL,
    TotalTransactions INT NOT NULL,
    TotalCashExpected DECIMAL(18,2) NOT NULL,
    TotalCashActual DECIMAL(18,2) NOT NULL,
    TotalCashVariance DECIMAL(18,2) NOT NULL,

    -- Payment totals
    TotalCash DECIMAL(18,2) NOT NULL,
    TotalMpesa DECIMAL(18,2) NOT NULL,
    TotalCard DECIMAL(18,2) NOT NULL,
    TotalOther DECIMAL(18,2) NOT NULL,

    -- Report data (JSON)
    ReportDataJson NVARCHAR(MAX) NOT NULL,

    -- Integrity
    IntegrityHash NVARCHAR(128) NOT NULL,

    CONSTRAINT UQ_CombinedZReports_Number UNIQUE (StoreId, ReportNumber)
);

CREATE INDEX IX_CombinedZReports_StoreId_Date ON CombinedZReports (StoreId, ReportDate);
```

#### 5.1.4 CombinedZReportTerminals (Junction Table)

```sql
CREATE TABLE CombinedZReportTerminals (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CombinedZReportId INT NOT NULL FOREIGN KEY REFERENCES CombinedZReports(Id),
    ZReportRecordId INT NOT NULL FOREIGN KEY REFERENCES ZReportRecords(Id),
    TerminalId INT NOT NULL FOREIGN KEY REFERENCES Terminals(Id),

    CONSTRAINT UQ_CombinedZReportTerminals UNIQUE (CombinedZReportId, ZReportRecordId)
);
```

### 5.2 Modified Tables

#### 5.2.1 WorkPeriods

```sql
ALTER TABLE WorkPeriods
ADD TerminalId INT NULL FOREIGN KEY REFERENCES Terminals(Id),
    TerminalCode NVARCHAR(20) NULL;

-- Create index
CREATE INDEX IX_WorkPeriods_TerminalId ON WorkPeriods (TerminalId);

-- Add constraint: Only one open work period per terminal
CREATE UNIQUE INDEX UQ_WorkPeriods_OpenPerTerminal
ON WorkPeriods (TerminalId)
WHERE Status = 1;  -- Status.Open
```

#### 5.2.2 Receipts

```sql
-- TerminalId may already exist, ensure it's used
ALTER TABLE Receipts
ADD TerminalCode NVARCHAR(20) NULL;

-- Create index if not exists
CREATE INDEX IX_Receipts_TerminalId ON Receipts (TerminalId);
```

#### 5.2.3 ZReportRecords

```sql
-- TerminalId may already exist, ensure it's used
ALTER TABLE ZReportRecords
ADD TerminalCode NVARCHAR(20) NULL;

-- Update index
CREATE INDEX IX_ZReportRecords_TerminalId_Date
ON ZReportRecords (TerminalId, ReportDateTime);
```

### 5.3 Migration Script

```sql
-- Migration: AddMultiTerminalSupport
-- Version: 20260125000000

BEGIN TRANSACTION;

-- 1. Create Terminals table
-- (SQL from 5.1.1)

-- 2. Create WorkPeriodSessions table
-- (SQL from 5.1.2)

-- 3. Create CombinedZReports table
-- (SQL from 5.1.3)

-- 4. Create CombinedZReportTerminals table
-- (SQL from 5.1.4)

-- 5. Modify WorkPeriods
-- (SQL from 5.2.1)

-- 6. Create default terminal for existing data
INSERT INTO Terminals (StoreId, Code, Name, MachineIdentifier, TerminalType, BusinessMode, CreatedAt, CreatedByUserId)
SELECT TOP 1
    1,  -- Default store
    'REG-001',
    'Register 1 (Migrated)',
    'MIGRATED-' + CAST(NEWID() AS NVARCHAR(36)),
    1,  -- Register
    1,  -- Supermarket
    GETUTCDATE(),
    (SELECT TOP 1 Id FROM Users WHERE Username = 'admin')
FROM Stores;

-- 7. Link existing WorkPeriods to default terminal
UPDATE WorkPeriods
SET TerminalId = (SELECT TOP 1 Id FROM Terminals WHERE Code = 'REG-001'),
    TerminalCode = 'REG-001'
WHERE TerminalId IS NULL;

-- 8. Link existing Receipts to default terminal
UPDATE Receipts
SET TerminalId = (SELECT TOP 1 Id FROM Terminals WHERE Code = 'REG-001'),
    TerminalCode = 'REG-001'
WHERE TerminalId IS NULL;

-- 9. Link existing ZReportRecords to default terminal
UPDATE ZReportRecords
SET TerminalId = (SELECT TOP 1 Id FROM Terminals WHERE Code = 'REG-001'),
    TerminalCode = 'REG-001'
WHERE TerminalId IS NULL;

COMMIT TRANSACTION;
```

---

## 6. Report Layouts & Formats

### 6.1 Thermal Printer Format (80mm)

All reports should support 80mm thermal printer format (48 characters per line):

```
================================================
            SUPERMART NAIROBI
           Moi Avenue, Nairobi
           Tel: +254 712 345 678
           KRA PIN: P051234567X
================================================
          Z REPORT - END OF SHIFT
------------------------------------------------
Terminal:       REG-001 (Register 1)
Report #:       Z-2024-001-0156
Date:           24/01/2026
Time:           20:15:00
Cashier:        Sarah Kimani
------------------------------------------------
SHIFT: 08:00 - 20:15 (12h 15m)

CASHIERS THIS SHIFT:
  John Mwangi     08:00-12:00   KES 62,500
  Jane Wanjiku    12:00-16:00   KES 85,230
  John Mwangi     16:00-20:15   KES 52,270
                  ─────────────────────────
  TOTAL                         KES200,000
------------------------------------------------
SALES SUMMARY:
  Gross Sales:              KES 215,680.00
  Discounts:               (KES  10,450.00)
  Refunds:                 (KES   3,230.00)
  Voids:                   (KES   2,000.00)
                           ────────────────
  Net Sales:                KES 200,000.00
  VAT (16%):                KES  32,000.00
                           ────────────────
  GRAND TOTAL:              KES 232,000.00
------------------------------------------------
PAYMENT BREAKDOWN:
  Cash:                     KES 120,500.00
  M-Pesa:                   KES  85,000.00
  Card:                     KES  26,500.00
                           ────────────────
  TOTAL:                    KES 232,000.00
------------------------------------------------
CASH RECONCILIATION:
  Opening Float:            KES   5,000.00
  Cash Received:            KES 120,500.00
  Cash Refunds:            (KES   2,100.00)
  Cash Payouts:            (KES   8,000.00)
                           ────────────────
  EXPECTED:                 KES 115,400.00
  COUNTED:                  KES 115,350.00
                           ────────────────
  VARIANCE:                (KES      50.00)
  STATUS: SHORT
------------------------------------------------
TRANSACTIONS: 215  |  AVG: KES 1,079.07
================================================
      *** FISCAL DOCUMENT ***
      RETAIN FOR TAX COMPLIANCE
================================================
        Generated: 24/01/2026 20:15:00
        Hash: 7a3f2b1c...
================================================
```

### 6.2 PDF Export Format

PDF exports should include:
- Company letterhead with logo
- Full report details
- Digital signature/hash
- Page numbers
- Generation timestamp
- Suitable for printing on A4 paper

### 6.3 Excel Export Format

Excel exports should include:
- Summary sheet with totals
- Detailed breakdown sheet
- Pivot table-ready data
- Charts for payment method distribution
- Cashier performance comparison

---

## 7. Deployment Guide

### 7.1 Server Setup

1. **Database Server**
   - Install SQL Server (Express or Standard)
   - Create `posdb` database
   - Configure SQL Server for network access
   - Create login for application
   - Ensure firewall allows port 1433

2. **Network Configuration**
   - Static IP for database server
   - DHCP reservations for terminals (optional)
   - Ensure all terminals can reach server

### 7.2 Terminal Deployment

#### 7.2.1 First Terminal (Creates Default Data)

1. Install application
2. Run Terminal Setup Wizard
3. Configure database connection
4. Create first terminal (REG-001)
5. Configure hardware
6. Test connectivity

#### 7.2.2 Additional Terminals

1. Copy application folder or run installer
2. Run Terminal Setup Wizard
3. Connect to existing database
4. Create new terminal or select pre-registered
5. Configure hardware for this machine
6. Test connectivity

### 7.3 Pre-Registration Workflow

For easier deployment, admin can pre-register terminals:

1. Admin creates terminals in Terminal Management (without machine identifier)
2. Technician installs application on each machine
3. During setup, technician selects pre-registered terminal
4. System binds machine identifier to terminal record

### 7.4 Connection String Template

```
Server=192.168.1.100;Database=posdb;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=30;
```

Or with SQL Authentication:
```
Server=192.168.1.100;Database=posdb;User Id=posapp;Password=******;TrustServerCertificate=True;Connection Timeout=30;
```

---

## 8. Security Considerations

### 8.1 Terminal Authentication

- Machine identifier binding prevents unauthorized access
- Terminal must match registered machine identifier
- Failed binding attempts logged
- Admin can reset machine binding if hardware replaced

### 8.2 Local Configuration Security

- Local config file should be encrypted or obfuscated
- Connection string credentials protected
- File permissions restricted to application user

### 8.3 Database Access

- Application uses dedicated database user
- Minimum required permissions
- No direct SQL access from terminals
- Audit logging for sensitive operations

### 8.4 Report Integrity

- SHA256 hash of report data
- Hash verification on report retrieval
- Tampering detection alerts
- Immutable report storage

---

## 9. Testing Requirements

### 9.1 Unit Tests

| Test Area | Test Cases |
|-----------|------------|
| Terminal Registration | Create, validate, duplicate detection |
| Machine Identifier | Extraction, validation, binding |
| Work Period | Start, close, terminal isolation |
| X-Report Generation | Calculation accuracy, format |
| Z-Report Generation | Calculation, immutability, sequencing |
| Combined Reports | Aggregation accuracy, terminal inclusion |

### 9.2 Integration Tests

| Test Area | Test Cases |
|-----------|------------|
| Multi-Terminal Transactions | Simultaneous sales on different terminals |
| Work Period Isolation | Each terminal's work period independent |
| Report Generation | Cross-terminal data accuracy |
| Network Resilience | Behavior during network interruption |

### 9.3 End-to-End Tests

| Scenario | Steps |
|----------|-------|
| Full Day Operation | Open 3 registers, process sales, generate individual Z-Reports, generate combined report |
| Cashier Handover | Multiple cashiers on same register, verify session tracking |
| Cash Variance | Test variance calculation and explanation workflow |
| Terminal Failure Recovery | Simulate terminal going offline, verify data integrity |

### 9.4 Performance Tests

| Test | Target |
|------|--------|
| Concurrent Transactions | 10 terminals, 5 TPS each |
| Report Generation | < 5 seconds for Z-Report |
| Combined Report | < 10 seconds for 10 terminals |
| Heartbeat Overhead | < 1% CPU impact |

---

## 10. Future Enhancements

### 10.1 Phase 2: Offline Mode

- Local SQLite database for offline operation
- Queue transactions during network outage
- Sync when connection restored
- Conflict resolution for shared data

### 10.2 Phase 3: Cloud Sync

- Optional cloud backup of reports
- Multi-store consolidated reporting
- Remote terminal monitoring
- Mobile app for managers

### 10.3 Phase 4: Advanced Features

- Terminal grouping (express lanes, service desk)
- Dynamic cashier assignment
- Real-time sales leaderboard
- Predictive staffing based on historical data
- IoT integration for queue management

---

## Appendix A: Issue Breakdown

The following issues should be created in GitHub for implementation:

### A.1 Infrastructure Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-001 | Create Terminal Entity and Database Migration | High | 4h |
| MT-002 | Implement Terminal Configuration Service | High | 6h |
| MT-003 | Create Local Configuration File System | High | 4h |
| MT-004 | Implement Machine Identifier Service | High | 3h |
| MT-005 | Create Terminal Session Context | High | 4h |

### A.2 Setup & Registration Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-006 | Create Terminal Setup Wizard UI | High | 8h |
| MT-007 | Implement Terminal Registration Flow | High | 6h |
| MT-008 | Add Terminal Selection to Login Flow | High | 4h |
| MT-009 | Create Terminal Hardware Configuration UI | Medium | 6h |

### A.3 Work Period Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-010 | Update Work Period Service for Terminal Scope | High | 6h |
| MT-011 | Create WorkPeriodSessions Table and Service | High | 4h |
| MT-012 | Implement Cashier Session Tracking | High | 4h |

### A.4 Transaction Attribution Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-013 | Update Receipt Service with Terminal Context | High | 4h |
| MT-014 | Update Order Service with Terminal Context | High | 4h |
| MT-015 | Update Payment Recording with Terminal ID | High | 3h |
| MT-016 | Migrate Existing Data to Default Terminal | Medium | 2h |

### A.5 Reporting Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-017 | Implement X-Report Service | High | 8h |
| MT-018 | Update Z-Report Service for Terminal Scope | High | 8h |
| MT-019 | Create X-Report UI and Print Format | High | 6h |
| MT-020 | Update Z-Report UI with Cashier Breakdown | High | 6h |
| MT-021 | Implement Combined Report Service | High | 10h |
| MT-022 | Create Combined X-Report UI | Medium | 6h |
| MT-023 | Create Combined Z-Report UI | Medium | 8h |
| MT-024 | Add Payment Method Breakdown to All Reports | High | 4h |
| MT-025 | Implement Report Export (PDF, Excel) | Medium | 6h |
| MT-026 | Create Thermal Printer Report Formats | High | 4h |

### A.6 Admin & Monitoring Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-027 | Create Terminal Management Admin UI | Medium | 8h |
| MT-028 | Implement Terminal Health Monitoring Service | Medium | 6h |
| MT-029 | Create Terminal Status Dashboard | Medium | 6h |
| MT-030 | Add Terminal Configuration Editor | Medium | 4h |

### A.7 Testing Issues

| Issue # | Title | Priority | Estimate |
|---------|-------|----------|----------|
| MT-031 | Unit Tests for Terminal Services | High | 8h |
| MT-032 | Integration Tests for Multi-Terminal | High | 8h |
| MT-033 | End-to-End Tests for Report Generation | Medium | 6h |

**Total Estimated Effort: ~180 hours (22-23 working days)**

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| Terminal | A physical POS machine (computer) running the application |
| Register | A terminal used for checkout operations (supermarket) |
| Till | A terminal used for service point operations (hotel/restaurant) |
| Work Period | A shift or operating period for a terminal |
| X-Report | Mid-shift snapshot report (does not close period) |
| Z-Report | End-of-shift fiscal report (closes period) |
| Combined Report | Aggregated report across multiple terminals |
| Machine Identifier | Unique hardware ID (MAC address or GUID) |
| Heartbeat | Periodic status update from terminal to server |
| Cash Variance | Difference between expected and actual cash |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Jan 2026 | System | Initial specification |

---

*End of Document*
