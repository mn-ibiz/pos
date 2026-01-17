using System.Drawing.Printing;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for discovering printers on the system and network.
/// </summary>
public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    private readonly ILogger _logger;
    private const int DefaultPrinterPort = 9100;
    private const int ConnectionTimeoutMs = 500;

    public PrinterDiscoveryService(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<string>> GetWindowsPrintersAsync()
    {
        var printers = new List<string>();

        try
        {
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printerName);
            }

            _logger.Debug("Found {Count} Windows printers", printers.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting Windows printers");
        }

        return Task.FromResult(printers);
    }

    /// <inheritdoc />
    public Task<List<string>> GetSerialPortsAsync()
    {
        var ports = new List<string>();

        try
        {
            ports.AddRange(SerialPort.GetPortNames().OrderBy(p => p));
            _logger.Debug("Found {Count} serial ports: {Ports}",
                ports.Count, string.Join(", ", ports));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting serial ports");
        }

        return Task.FromResult(ports);
    }

    /// <inheritdoc />
    public async Task<List<DiscoveredPrinter>> DiscoverNetworkPrintersAsync()
    {
        var discovered = new List<DiscoveredPrinter>();

        try
        {
            var localIp = GetLocalIPAddress();
            if (string.IsNullOrEmpty(localIp))
            {
                _logger.Warning("Could not determine local IP address for network scan");
                return discovered;
            }

            var lastDotIndex = localIp.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                return discovered;
            }

            var subnet = localIp.Substring(0, lastDotIndex + 1);
            _logger.Information("Scanning network {Subnet}x for printers on port {Port}",
                subnet, DefaultPrinterPort);

            var tasks = new List<Task<DiscoveredPrinter?>>();

            // Scan common IP ranges (1-254)
            for (int i = 1; i <= 254; i++)
            {
                var ip = $"{subnet}{i}";
                tasks.Add(TryDiscoverPrinterAsync(ip, DefaultPrinterPort));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var printer in results)
            {
                if (printer != null)
                {
                    discovered.Add(printer);
                }
            }

            _logger.Information("Network scan complete. Found {Count} printers.", discovered.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error discovering network printers");
        }

        return discovered;
    }

    private async Task<DiscoveredPrinter?> TryDiscoverPrinterAsync(string ip, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(ConnectionTimeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == connectTask && client.Connected)
            {
                _logger.Debug("Found printer at {IP}:{Port}", ip, port);
                return new DiscoveredPrinter
                {
                    IpAddress = ip,
                    Port = port,
                    ConnectionType = PrinterConnectionType.Network
                };
            }
        }
        catch
        {
            // Ignore connection errors during scanning
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(Printer printer)
    {
        try
        {
            switch (printer.ConnectionType)
            {
                case PrinterConnectionType.WindowsDriver:
                    return TestWindowsDriverConnection(printer);

                case PrinterConnectionType.Network:
                    return await TestNetworkConnectionAsync(printer);

                case PrinterConnectionType.Serial:
                    return TestSerialConnection(printer);

                case PrinterConnectionType.USB:
                    // USB printers typically appear as Windows printers
                    return TestWindowsDriverConnection(printer);

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing connection for printer {PrinterName}", printer.Name);
            return false;
        }
    }

    private bool TestWindowsDriverConnection(Printer printer)
    {
        if (string.IsNullOrEmpty(printer.WindowsPrinterName))
        {
            return false;
        }

        try
        {
            // Check if the printer exists in the installed printers
            var printerSettings = new PrinterSettings
            {
                PrinterName = printer.WindowsPrinterName
            };

            var isValid = printerSettings.IsValid;
            _logger.Debug("Windows printer {PrinterName} valid: {IsValid}",
                printer.WindowsPrinterName, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing Windows printer {PrinterName}", printer.WindowsPrinterName);
            return false;
        }
    }

    private async Task<bool> TestNetworkConnectionAsync(Printer printer)
    {
        if (string.IsNullOrEmpty(printer.IpAddress))
        {
            return false;
        }

        var port = printer.Port ?? DefaultPrinterPort;

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(printer.IpAddress, port);
            var timeoutTask = Task.Delay(ConnectionTimeoutMs * 2); // Longer timeout for status check

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            var isConnected = completedTask == connectTask && client.Connected;
            _logger.Debug("Network printer {IP}:{Port} connected: {IsConnected}",
                printer.IpAddress, port, isConnected);

            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing network printer {IP}:{Port}",
                printer.IpAddress, port);
            return false;
        }
    }

    private bool TestSerialConnection(Printer printer)
    {
        if (string.IsNullOrEmpty(printer.PortName))
        {
            return false;
        }

        try
        {
            // Check if the port exists
            var availablePorts = SerialPort.GetPortNames();
            var exists = availablePorts.Contains(printer.PortName);

            _logger.Debug("Serial port {PortName} exists: {Exists}",
                printer.PortName, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing serial port {PortName}", printer.PortName);
            return false;
        }
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            // Get the first non-loopback IPv4 address
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                    && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                var unicastAddresses = ipProperties.UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

                foreach (var unicastAddress in unicastAddresses)
                {
                    var ip = unicastAddress.Address.ToString();
                    if (!ip.StartsWith("169.254")) // Skip APIPA addresses
                    {
                        return ip;
                    }
                }
            }

            // Fallback: use DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            return ipAddress?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
