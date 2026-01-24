using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Request DTO for creating a new terminal.
/// </summary>
public class CreateTerminalRequest
{
    /// <summary>
    /// Gets or sets the parent store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the terminal display code (e.g., REG-001).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal friendly name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; } = TerminalType.Register;

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; } = BusinessMode.Supermarket;

    /// <summary>
    /// Gets or sets the machine identifier (optional at creation).
    /// </summary>
    public string? MachineIdentifier { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main register.
    /// </summary>
    public bool IsMainRegister { get; set; }
}

/// <summary>
/// Request DTO for updating a terminal.
/// </summary>
public class UpdateTerminalRequest
{
    /// <summary>
    /// Gets or sets the terminal display code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the terminal friendly name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType? TerminalType { get; set; }

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode? BusinessMode { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main register.
    /// </summary>
    public bool? IsMainRegister { get; set; }

    /// <summary>
    /// Gets or sets the printer configuration JSON.
    /// </summary>
    public string? PrinterConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the hardware configuration JSON.
    /// </summary>
    public string? HardwareConfiguration { get; set; }
}

/// <summary>
/// Request DTO for terminal registration with hardware binding.
/// </summary>
public class TerminalRegistrationRequest
{
    /// <summary>
    /// Gets or sets the parent store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the terminal display code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal friendly name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; } = TerminalType.Register;

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; } = BusinessMode.Supermarket;

    /// <summary>
    /// Gets or sets the machine identifier (MAC address or GUID).
    /// </summary>
    public string MachineIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the hardware configuration.
    /// </summary>
    public TerminalHardwareConfig? Hardware { get; set; }
}

/// <summary>
/// Hardware configuration for a terminal.
/// </summary>
public class TerminalHardwareConfig
{
    /// <summary>
    /// Gets or sets the receipt printer ID.
    /// </summary>
    public int? ReceiptPrinterId { get; set; }

    /// <summary>
    /// Gets or sets the kitchen printer ID.
    /// </summary>
    public int? KitchenPrinterId { get; set; }

    /// <summary>
    /// Gets or sets the cash drawer ID.
    /// </summary>
    public int? CashDrawerId { get; set; }

    /// <summary>
    /// Gets or sets the customer display ID.
    /// </summary>
    public int? CustomerDisplayId { get; set; }

    /// <summary>
    /// Gets or sets the scale ID.
    /// </summary>
    public int? ScaleId { get; set; }

    /// <summary>
    /// Gets or sets whether the barcode scanner is enabled.
    /// </summary>
    public bool BarcodeScannerEnabled { get; set; } = true;
}

/// <summary>
/// Heartbeat data from a terminal.
/// </summary>
public class TerminalHeartbeat
{
    /// <summary>
    /// Gets or sets the current IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the currently logged-in user ID.
    /// </summary>
    public int? CurrentUserId { get; set; }

    /// <summary>
    /// Gets or sets whether a work period is currently open.
    /// </summary>
    public bool IsWorkPeriodOpen { get; set; }

    /// <summary>
    /// Gets or sets whether the receipt printer is available.
    /// </summary>
    public bool IsPrinterAvailable { get; set; }

    /// <summary>
    /// Gets or sets whether the cash drawer is available.
    /// </summary>
    public bool IsCashDrawerAvailable { get; set; }

    /// <summary>
    /// Gets or sets the current order count.
    /// </summary>
    public int CurrentOrderCount { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string? AppVersion { get; set; }
}

/// <summary>
/// Result of terminal validation.
/// </summary>
public class TerminalValidationResult
{
    /// <summary>
    /// Gets or sets whether the terminal is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets whether the machine identifier matches.
    /// </summary>
    public bool MachineIdentifierMatches { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static TerminalValidationResult Success() => new()
    {
        IsValid = true,
        MachineIdentifierMatches = true,
        IsActive = true,
        IsOnline = true
    };

    /// <summary>
    /// Creates a failure validation result.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public static TerminalValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// DTO for terminal status information.
/// </summary>
public class TerminalStatusDto
{
    /// <summary>
    /// Gets or sets the terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Gets or sets the terminal code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the last heartbeat timestamp.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Gets or sets the current user's name.
    /// </summary>
    public string? CurrentUserName { get; set; }

    /// <summary>
    /// Gets or sets whether a work period is open.
    /// </summary>
    public bool IsWorkPeriodOpen { get; set; }

    /// <summary>
    /// Gets or sets whether the printer is available.
    /// </summary>
    public bool IsPrinterAvailable { get; set; }

    /// <summary>
    /// Gets or sets whether the cash drawer is available.
    /// </summary>
    public bool IsCashDrawerAvailable { get; set; }

    /// <summary>
    /// Gets or sets the current IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for displaying terminal information.
/// </summary>
public class TerminalDisplayDto
{
    /// <summary>
    /// Gets or sets the terminal ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; }

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main register.
    /// </summary>
    public bool IsMainRegister { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is assigned (has machine identifier).
    /// </summary>
    public bool IsAssigned { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
