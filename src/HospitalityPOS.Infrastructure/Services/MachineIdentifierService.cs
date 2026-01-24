using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for extracting and validating unique machine identifiers.
/// </summary>
public class MachineIdentifierService : IMachineIdentifierService
{
    private readonly ILogger _logger;
    private string? _cachedIdentifier;
    private MachineIdentifierType? _cachedIdentifierType;
    private readonly object _cacheLock = new();

    // Virtual adapter descriptions to filter out
    private static readonly string[] VirtualAdapterKeywords = new[]
    {
        "virtual", "vmware", "virtualbox", "hyper-v", "vethernet",
        "vpn", "tunnel", "loopback", "pseudo", "bluetooth",
        "microsoft wi-fi direct", "microsoft hosted network"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineIdentifierService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MachineIdentifierService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GetMachineIdentifier()
    {
        lock (_cacheLock)
        {
            if (_cachedIdentifier is not null)
            {
                return _cachedIdentifier;
            }

            // Try MAC address first
            var macAddresses = GetAvailableMacAddresses();
            if (macAddresses.Count > 0)
            {
                _cachedIdentifier = macAddresses[0];
                _cachedIdentifierType = MachineIdentifierType.MacAddress;
                _logger.Information("Using MAC address as machine identifier: {Identifier}", _cachedIdentifier);
                return _cachedIdentifier;
            }

            // Try Windows Machine GUID
            var windowsGuid = GetWindowsMachineGuid();
            if (!string.IsNullOrEmpty(windowsGuid))
            {
                _cachedIdentifier = windowsGuid;
                _cachedIdentifierType = MachineIdentifierType.WindowsMachineGuid;
                _logger.Information("Using Windows Machine GUID as machine identifier: {Identifier}", _cachedIdentifier);
                return _cachedIdentifier;
            }

            // Fall back to generated GUID
            _cachedIdentifier = GenerateFallbackIdentifier();
            _cachedIdentifierType = MachineIdentifierType.GeneratedGuid;
            _logger.Warning("Using generated GUID as machine identifier: {Identifier}", _cachedIdentifier);
            return _cachedIdentifier;
        }
    }

    /// <inheritdoc />
    public bool ValidateMachineIdentifier(string storedIdentifier)
    {
        if (string.IsNullOrWhiteSpace(storedIdentifier))
        {
            return false;
        }

        var currentIdentifier = GetMachineIdentifier();

        // Direct match
        if (string.Equals(currentIdentifier, storedIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // If stored is MAC, check if it's any of our available MACs
        if (storedIdentifier.Contains(':'))
        {
            var availableMacs = GetAvailableMacAddresses();
            return availableMacs.Any(mac =>
                string.Equals(mac, storedIdentifier, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableMacAddresses()
    {
        var macAddresses = new List<string>();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var nic in interfaces)
            {
                // Skip non-physical adapters
                if (!IsPhysicalAdapter(nic))
                {
                    continue;
                }

                var physicalAddress = nic.GetPhysicalAddress();
                var addressBytes = physicalAddress.GetAddressBytes();

                if (addressBytes.Length == 6 && addressBytes.Any(b => b != 0))
                {
                    var macAddress = FormatMacAddress(addressBytes);
                    if (!macAddresses.Contains(macAddress))
                    {
                        macAddresses.Add(macAddress);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to retrieve MAC addresses");
        }

        return macAddresses.AsReadOnly();
    }

    /// <inheritdoc />
    public string? GetWindowsMachineGuid()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var value = key?.GetValue("MachineGuid") as string;

            if (!string.IsNullOrEmpty(value))
            {
                return value.ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to retrieve Windows Machine GUID from registry");
        }

        return null;
    }

    /// <inheritdoc />
    public string GenerateFallbackIdentifier()
    {
        return $"gen-{Guid.NewGuid().ToString().ToLowerInvariant()}";
    }

    /// <inheritdoc />
    public MachineIdentifierType GetIdentifierType()
    {
        // Ensure identifier is resolved
        GetMachineIdentifier();

        lock (_cacheLock)
        {
            return _cachedIdentifierType ?? MachineIdentifierType.GeneratedGuid;
        }
    }

    /// <inheritdoc />
    public MachineIdentifierInfo GetMachineIdentifierInfo()
    {
        return new MachineIdentifierInfo
        {
            Identifier = GetMachineIdentifier(),
            IdentifierType = GetIdentifierType(),
            AvailableMacAddresses = GetAvailableMacAddresses(),
            WindowsMachineGuid = GetWindowsMachineGuid(),
            MachineName = Environment.MachineName,
            OsVersion = RuntimeInformation.OSDescription
        };
    }

    private static bool IsPhysicalAdapter(NetworkInterface nic)
    {
        // Must be up or dormant
        if (nic.OperationalStatus != OperationalStatus.Up &&
            nic.OperationalStatus != OperationalStatus.Dormant)
        {
            return false;
        }

        // Only ethernet or wireless
        if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
            nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
            nic.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet)
        {
            return false;
        }

        // Filter out virtual adapters by description
        var description = nic.Description.ToLowerInvariant();
        if (VirtualAdapterKeywords.Any(keyword => description.Contains(keyword)))
        {
            return false;
        }

        // Filter out virtual adapters by name
        var name = nic.Name.ToLowerInvariant();
        if (VirtualAdapterKeywords.Any(keyword => name.Contains(keyword)))
        {
            return false;
        }

        return true;
    }

    private static string FormatMacAddress(byte[] bytes)
    {
        return string.Join(":", bytes.Select(b => b.ToString("X2")));
    }
}
