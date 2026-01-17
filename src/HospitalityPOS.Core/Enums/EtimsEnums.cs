namespace HospitalityPOS.Core.Enums;

/// <summary>
/// eTIMS device registration status.
/// </summary>
public enum EtimsDeviceStatus
{
    Pending = 0,
    Registered = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4
}

/// <summary>
/// eTIMS invoice submission status.
/// </summary>
public enum EtimsSubmissionStatus
{
    Pending = 0,
    Queued = 1,
    Submitted = 2,
    Accepted = 3,
    Rejected = 4,
    Failed = 5
}

/// <summary>
/// KRA tax types used in eTIMS.
/// </summary>
public enum KraTaxType
{
    A = 0,  // Standard rated (16%)
    B = 1,  // Zero rated (0%)
    C = 2,  // Exempt
    D = 3,  // Out of scope
    E = 4   // Insurance premium levy
}

/// <summary>
/// eTIMS document type.
/// </summary>
public enum EtimsDocumentType
{
    TaxInvoice = 0,
    CreditNote = 1,
    DebitNote = 2,
    SimplifiedTaxInvoice = 3  // For transactions below KES 5,000
}

/// <summary>
/// eTIMS customer type.
/// </summary>
public enum EtimsCustomerType
{
    Business = 0,      // B2B
    Consumer = 1,      // B2C
    Government = 2,    // B2G
    Export = 3         // Export sales
}
