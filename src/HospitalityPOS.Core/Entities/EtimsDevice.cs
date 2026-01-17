using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// eTIMS Control Unit device registration and configuration.
/// </summary>
public class EtimsDevice : BaseEntity
{
    /// <summary>
    /// KRA-assigned device serial number.
    /// </summary>
    public string DeviceSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// KRA-assigned Control Unit ID (CU ID).
    /// </summary>
    public string ControlUnitId { get; set; } = string.Empty;

    /// <summary>
    /// Business PIN (Personal Identification Number).
    /// </summary>
    public string BusinessPin { get; set; } = string.Empty;

    /// <summary>
    /// Business name as registered with KRA.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Branch code (001 for main branch).
    /// </summary>
    public string BranchCode { get; set; } = "001";

    /// <summary>
    /// Branch name.
    /// </summary>
    public string BranchName { get; set; } = "Main Branch";

    /// <summary>
    /// eTIMS API base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://etims.kra.go.ke";

    /// <summary>
    /// eTIMS API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// eTIMS API secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Device registration date.
    /// </summary>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Last successful communication with eTIMS.
    /// </summary>
    public DateTime? LastCommunication { get; set; }

    /// <summary>
    /// Current device status.
    /// </summary>
    public EtimsDeviceStatus Status { get; set; } = EtimsDeviceStatus.Pending;

    /// <summary>
    /// Last invoice number issued by this device.
    /// </summary>
    public int LastInvoiceNumber { get; set; }

    /// <summary>
    /// Last credit note number issued by this device.
    /// </summary>
    public int LastCreditNoteNumber { get; set; }

    /// <summary>
    /// Is this the primary/active device.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Environment (Sandbox/Production).
    /// </summary>
    public string Environment { get; set; } = "Sandbox";
}
