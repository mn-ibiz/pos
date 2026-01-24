using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for extracting and validating unique machine identifiers.
/// </summary>
public interface IMachineIdentifierService
{
    /// <summary>
    /// Gets the unique identifier for this machine.
    /// Priority: MAC address > Windows Machine GUID > Generated GUID.
    /// </summary>
    /// <returns>The machine identifier.</returns>
    string GetMachineIdentifier();

    /// <summary>
    /// Validates if the stored identifier matches the current machine.
    /// </summary>
    /// <param name="storedIdentifier">The stored identifier to validate.</param>
    /// <returns>True if the identifier matches.</returns>
    bool ValidateMachineIdentifier(string storedIdentifier);

    /// <summary>
    /// Gets all available network adapter MAC addresses.
    /// </summary>
    /// <returns>List of MAC addresses.</returns>
    IReadOnlyList<string> GetAvailableMacAddresses();

    /// <summary>
    /// Gets the Windows Machine GUID from registry.
    /// </summary>
    /// <returns>The Windows Machine GUID, or null if unavailable.</returns>
    string? GetWindowsMachineGuid();

    /// <summary>
    /// Generates a new GUID for machines without other identifiers.
    /// </summary>
    /// <returns>A generated fallback identifier.</returns>
    string GenerateFallbackIdentifier();

    /// <summary>
    /// Gets the identifier type used for this machine.
    /// </summary>
    /// <returns>The type of identifier used.</returns>
    MachineIdentifierType GetIdentifierType();

    /// <summary>
    /// Gets detailed information about the machine identifier.
    /// </summary>
    /// <returns>Machine identifier details.</returns>
    MachineIdentifierInfo GetMachineIdentifierInfo();
}

/// <summary>
/// Contains detailed information about a machine identifier.
/// </summary>
public class MachineIdentifierInfo
{
    /// <summary>
    /// Gets or sets the identifier value.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of identifier.
    /// </summary>
    public MachineIdentifierType IdentifierType { get; set; }

    /// <summary>
    /// Gets or sets all available MAC addresses.
    /// </summary>
    public IReadOnlyList<string> AvailableMacAddresses { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the Windows Machine GUID if available.
    /// </summary>
    public string? WindowsMachineGuid { get; set; }

    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OS version.
    /// </summary>
    public string OsVersion { get; set; } = string.Empty;
}
