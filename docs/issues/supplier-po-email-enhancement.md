## Overview

Enhance the Supplier Management module to support automated Purchase Order (PO) email delivery to suppliers. While the supplier entity captures email addresses, there's no workflow for automatically sending POs via email.

## Current State Analysis

### Supplier Entity (COMPLETE)
Located: `HospitalityPOS.Core/Entities/Supplier.cs`

**Existing Contact Fields:**
- `Email` (string?) ✓
- `Phone` (string?) ✓
- `ContactPerson` (string?) ✓
- `Address`, `City`, `Country` ✓

### Purchase Order Entity (EXISTS)
Located: `HospitalityPOS.Core/Entities/PurchaseOrder.cs`

**Existing Fields:**
- `SupplierId` (int) - Links to supplier
- `Status` - Draft, Sent, PartiallyReceived, Complete, Cancelled
- Order details, items, etc.

### What's Missing
- PO email sending functionality
- Email template for POs
- Supplier email preferences
- Send history/audit trail
- Multiple contact support

## Requirements

### 1. Supplier Email Enhancements

```csharp
// Add to Supplier entity
public string OrderEmail { get; set; } // Specific email for orders
public string AccountsEmail { get; set; } // For invoices/payments
public bool SendPOByEmail { get; set; } // Auto-send preference
public string EmailCcAddresses { get; set; } // CC recipients (comma-separated)

// Multiple contacts support
public virtual ICollection<SupplierContact> Contacts { get; set; }
```

```csharp
public class SupplierContact : BaseEntity
{
    public int SupplierId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Mobile { get; set; }
    public string Position { get; set; } // Sales, Accounts, Manager
    public bool IsPrimaryContact { get; set; }
    public bool ReceivesPOEmails { get; set; }
    public bool ReceivesInvoiceEmails { get; set; }
    public string Notes { get; set; }

    public virtual Supplier Supplier { get; set; }
}
```

### 2. PO Email Sending Service

```csharp
public interface IPurchaseOrderEmailService
{
    Task<bool> SendPurchaseOrderAsync(
        int purchaseOrderId,
        string[] additionalRecipients = null,
        string customMessage = null,
        CancellationToken ct = default);

    Task<bool> ResendPurchaseOrderAsync(
        int purchaseOrderId,
        CancellationToken ct = default);

    Task<IEnumerable<POEmailLog>> GetEmailHistoryAsync(
        int purchaseOrderId,
        CancellationToken ct = default);

    Task<string> PreviewPOEmailAsync(
        int purchaseOrderId,
        CancellationToken ct = default);
}
```

### 3. PO Email Template

```html
Subject: Purchase Order #{PONumber} from {CompanyName}

Dear {SupplierContactName},

Please find attached Purchase Order #{PONumber} dated {PODate}.

ORDER SUMMARY
─────────────
PO Number: {PONumber}
Date: {PODate}
Expected Delivery: {ExpectedDeliveryDate}
Total Items: {ItemCount}
Order Total: {Currency} {TotalAmount}

DELIVERY ADDRESS
────────────────
{DeliveryAddress}

Please confirm receipt of this order and expected delivery date.

{CustomMessage}

Best regards,
{SenderName}
{CompanyName}
{CompanyPhone}
{CompanyEmail}

---
This is an automated message from {CompanyName} POS System.
To update your contact preferences, please contact us.
```

### 4. PO Email Log

```csharp
public class POEmailLog : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public string Recipients { get; set; } // To addresses
    public string CcRecipients { get; set; }
    public string Subject { get; set; }
    public DateTime SentAt { get; set; }
    public int SentByUserId { get; set; }
    public EmailStatus Status { get; set; } // Sent, Failed, Bounced
    public string ErrorMessage { get; set; }
    public string MessageId { get; set; } // From email provider

    public virtual PurchaseOrder PurchaseOrder { get; set; }
    public virtual User SentByUser { get; set; }
}

public enum EmailStatus
{
    Queued,
    Sent,
    Delivered,
    Failed,
    Bounced
}
```

### 5. UI Enhancements

#### Supplier Editor Dialog
- Add "Order Email" field (separate from main email)
- Add "Accounts Email" field
- Checkbox: "Send POs automatically when submitted"
- "Manage Contacts" button for multiple contacts

#### Purchase Order Actions
- "Email to Supplier" button
- Preview email before sending
- Status indicator showing if emailed
- Email history view

#### PO List View
- Column showing email sent status
- Quick action to resend email

### 6. Workflow Integration

When PO status changes to "Sent":
1. Check if supplier has `SendPOByEmail = true`
2. Get primary order email (or fallback to main email)
3. Generate PDF attachment of PO
4. Send email with template
5. Log email in POEmailLog
6. Update PO with email sent timestamp

### 7. Settings

- SMTP configuration (or email service API)
- Default email template customization
- Company signature settings
- Auto-send toggle (global setting)

## Acceptance Criteria

- [ ] Supplier can have separate order and accounts email addresses
- [ ] Multiple contacts can be added to a supplier
- [ ] Contacts can be flagged for PO or invoice emails
- [ ] PO can be emailed directly from PO detail view
- [ ] Email preview available before sending
- [ ] Auto-send option on PO submission
- [ ] Email history tracked per PO
- [ ] PDF attachment of PO included
- [ ] Email template is customizable
- [ ] Failed emails show error and allow retry

## Implementation Notes

### Existing Code to Modify
- `Supplier` entity - Add email preference fields
- `SupplierService` - Add contact management
- `SupplierEditorDialog` - Add contact/email UI
- `PurchaseOrderService` - Add email triggering

### New Components
- `SupplierContact` entity
- `POEmailLog` entity
- `IPurchaseOrderEmailService`
- `PurchaseOrderEmailService`
- `SupplierContactsDialog`
- Email template configuration

### Email Provider Options
- SMTP (SendGrid, Mailgun, AWS SES)
- Microsoft Graph API (for Office 365)
- Custom email service integration

---

**Priority**: Medium
**Estimated Complexity**: Medium
**Labels**: feature, suppliers, email, automation
