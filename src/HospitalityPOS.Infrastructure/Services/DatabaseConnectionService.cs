using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing database connection configuration with network discovery.
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly ILogger<DatabaseConnectionService> _logger;
    private readonly string _configFilePath;
    private readonly byte[] _encryptionKey;
    private DatabaseConnectionConfig? _cachedConfig;

    // SQL Browser port for instance discovery
    private const int SqlBrowserPort = 1434;
    private const int DiscoveryTimeoutMs = 3000;

    public DatabaseConnectionService(ILogger<DatabaseConnectionService> logger)
    {
        _logger = logger;

        // Store config in AppData
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HospitalityPOS");

        Directory.CreateDirectory(appDataPath);
        _configFilePath = Path.Combine(appDataPath, "connection.config");

        // Generate machine-specific encryption key
        _encryptionKey = GenerateMachineKey();
    }

    public bool HasConnectionConfiguration => File.Exists(_configFilePath);

    public string? GetConnectionString()
    {
        var config = LoadConnectionConfiguration();
        return config?.BuildConnectionString();
    }

    public void SaveConnectionConfiguration(DatabaseConnectionConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var encrypted = EncryptString(json);
            File.WriteAllText(_configFilePath, encrypted);

            _cachedConfig = config;
            _logger.LogInformation("Database connection configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database connection configuration");
            throw;
        }
    }

    public DatabaseConnectionConfig? LoadConnectionConfiguration()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        if (!File.Exists(_configFilePath))
            return null;

        try
        {
            var encrypted = File.ReadAllText(_configFilePath);
            var json = DecryptString(encrypted);
            _cachedConfig = JsonSerializer.Deserialize<DatabaseConnectionConfig>(json);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load database connection configuration");
            return null;
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(DatabaseConnectionConfig config)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var connectionString = config.BuildConnectionString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            stopwatch.Stop();

            // Get server version
            var serverVersion = connection.ServerVersion;

            // Check if database exists
            var databaseExists = true; // If we connected, it exists

            _logger.LogInformation(
                "Connection test successful to {Server}, version {Version}, time {Time}ms",
                config.FullServerName, serverVersion, stopwatch.ElapsedMilliseconds);

            return ConnectionTestResult.Success(serverVersion, databaseExists, stopwatch.ElapsedMilliseconds);
        }
        catch (SqlException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Connection test failed to {Server}", config.FullServerName);

            var message = ex.Number switch
            {
                -1 => "Cannot connect to server. Please check the server name and ensure SQL Server is running.",
                18456 => "Login failed. Please check the username and password.",
                4060 => $"Database '{config.Database}' does not exist on the server.",
                _ => ex.Message
            };

            return ConnectionTestResult.Failure(message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Connection test error to {Server}", config.FullServerName);
            return ConnectionTestResult.Failure(ex.Message);
        }
    }

    public async Task<ConnectionTestResult> TestCurrentConnectionAsync()
    {
        var config = LoadConnectionConfiguration();
        if (config == null)
            return ConnectionTestResult.Failure("No connection configuration found");

        return await TestConnectionAsync(config);
    }

    public async Task<List<SqlServerInstance>> DiscoverSqlServersAsync(CancellationToken cancellationToken = default)
    {
        var instances = new List<SqlServerInstance>();

        _logger.LogInformation("Starting SQL Server discovery...");

        // Method 1: Use SqlDataSourceEnumerator (works for local and some network instances)
        try
        {
            var enumeratorInstances = await Task.Run(() => DiscoverUsingSqlEnumerator(), cancellationToken);
            instances.AddRange(enumeratorInstances);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SqlDataSourceEnumerator discovery failed");
        }

        // Method 2: Check local machine for SQL Server
        try
        {
            var localInstances = await DiscoverLocalInstancesAsync(cancellationToken);
            foreach (var instance in localInstances)
            {
                if (!instances.Any(i => i.DisplayName.Equals(instance.DisplayName, StringComparison.OrdinalIgnoreCase)))
                {
                    instances.Add(instance);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local instance discovery failed");
        }

        // Method 3: UDP broadcast to SQL Browser (finds instances on network)
        try
        {
            var networkInstances = await DiscoverUsingUdpBroadcastAsync(cancellationToken);
            foreach (var instance in networkInstances)
            {
                if (!instances.Any(i => i.DisplayName.Equals(instance.DisplayName, StringComparison.OrdinalIgnoreCase)))
                {
                    instances.Add(instance);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP broadcast discovery failed");
        }

        _logger.LogInformation("SQL Server discovery completed. Found {Count} instances", instances.Count);

        return instances.OrderBy(i => i.ServerName).ThenBy(i => i.InstanceName).ToList();
    }

    public async Task<List<string>> GetDatabasesAsync(DatabaseConnectionConfig config)
    {
        var databases = new List<string>();

        try
        {
            // Connect to master database to list all databases
            var masterConfig = new DatabaseConnectionConfig
            {
                Server = config.Server,
                InstanceName = config.InstanceName,
                Database = "master",
                UseWindowsAuthentication = config.UseWindowsAuthentication,
                Username = config.Username,
                Password = config.Password,
                ConnectionTimeout = config.ConnectionTimeout,
                Encrypt = config.Encrypt,
                TrustServerCertificate = config.TrustServerCertificate
            };

            await using var connection = new SqlConnection(masterConfig.BuildConnectionString());
            await connection.OpenAsync();

            await using var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list databases on {Server}", config.FullServerName);
        }

        return databases;
    }

    public void ClearConnectionConfiguration()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
        _cachedConfig = null;
        _logger.LogInformation("Database connection configuration cleared");
    }

    #region Private Methods - Discovery

    private List<SqlServerInstance> DiscoverUsingSqlEnumerator()
    {
        var instances = new List<SqlServerInstance>();

        try
        {
            var enumerator = System.Data.Sql.SqlDataSourceEnumerator.Instance;
            var table = enumerator.GetDataSources();

            foreach (DataRow row in table.Rows)
            {
                var serverName = row["ServerName"]?.ToString() ?? string.Empty;
                var instanceName = row["InstanceName"]?.ToString() ?? string.Empty;
                var version = row["Version"]?.ToString() ?? string.Empty;
                var isClustered = row["IsClustered"]?.ToString()?.Equals("Yes", StringComparison.OrdinalIgnoreCase) ?? false;

                if (!string.IsNullOrEmpty(serverName))
                {
                    instances.Add(new SqlServerInstance
                    {
                        ServerName = serverName,
                        InstanceName = instanceName,
                        Version = version,
                        IsClustered = isClustered,
                        Source = "SqlEnumerator"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SqlDataSourceEnumerator failed");
        }

        return instances;
    }

    private async Task<List<SqlServerInstance>> DiscoverLocalInstancesAsync(CancellationToken cancellationToken)
    {
        var instances = new List<SqlServerInstance>();
        var machineName = Environment.MachineName;

        // Check common instance names
        var commonInstances = new[] { "", "SQLEXPRESS", "MSSQLSERVER", "SQL2019", "SQL2022" };

        foreach (var instanceName in commonInstances)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var config = new DatabaseConnectionConfig
                {
                    Server = machineName,
                    InstanceName = string.IsNullOrEmpty(instanceName) ? null : instanceName,
                    Database = "master",
                    UseWindowsAuthentication = true,
                    ConnectionTimeout = 2
                };

                var result = await TestConnectionAsync(config);
                if (result.IsSuccess)
                {
                    instances.Add(new SqlServerInstance
                    {
                        ServerName = machineName,
                        InstanceName = instanceName,
                        Version = result.ServerVersion ?? "",
                        Source = "Local"
                    });
                }
            }
            catch
            {
                // Instance doesn't exist, continue
            }
        }

        return instances;
    }

    private async Task<List<SqlServerInstance>> DiscoverUsingUdpBroadcastAsync(CancellationToken cancellationToken)
    {
        var instances = new List<SqlServerInstance>();

        try
        {
            using var udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            udpClient.Client.ReceiveTimeout = DiscoveryTimeoutMs;

            // SQL Browser discovery message
            var requestMessage = new byte[] { 0x02 };

            // Broadcast to all network interfaces
            var broadcastAddresses = GetBroadcastAddresses();

            foreach (var broadcastAddress in broadcastAddresses)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await udpClient.SendAsync(requestMessage, requestMessage.Length,
                        new IPEndPoint(broadcastAddress, SqlBrowserPort));
                }
                catch
                {
                    // Interface may not support broadcast
                }
            }

            // Also send to localhost
            try
            {
                await udpClient.SendAsync(requestMessage, requestMessage.Length,
                    new IPEndPoint(IPAddress.Loopback, SqlBrowserPort));
            }
            catch { }

            // Receive responses
            var endTime = DateTime.UtcNow.AddMilliseconds(DiscoveryTimeoutMs);
            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (udpClient.Available > 0)
                    {
                        IPEndPoint? remoteEndPoint = null;
                        var response = udpClient.Receive(ref remoteEndPoint);

                        if (response.Length > 3)
                        {
                            var responseStr = Encoding.ASCII.GetString(response, 3, response.Length - 3);
                            var parsedInstances = ParseSqlBrowserResponse(responseStr, remoteEndPoint?.Address.ToString());
                            instances.AddRange(parsedInstances);
                        }
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }
                catch (SocketException)
                {
                    // Timeout or other socket error
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UDP broadcast discovery failed");
        }

        return instances;
    }

    private List<SqlServerInstance> ParseSqlBrowserResponse(string response, string? serverIp)
    {
        var instances = new List<SqlServerInstance>();

        // Response format: ServerName;SERVERNAME;InstanceName;INSTANCE;IsClustered;No;Version;16.0.1000.6;;
        var parts = response.Split(';');

        string? serverName = null;
        string? instanceName = null;
        string? version = null;
        bool isClustered = false;

        for (int i = 0; i < parts.Length - 1; i += 2)
        {
            var key = parts[i];
            var value = parts[i + 1];

            switch (key.ToLowerInvariant())
            {
                case "servername":
                    serverName = value;
                    break;
                case "instancename":
                    instanceName = value;
                    break;
                case "version":
                    version = value;
                    break;
                case "isclustered":
                    isClustered = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(serverName))
        {
            instances.Add(new SqlServerInstance
            {
                ServerName = serverName,
                InstanceName = instanceName ?? "",
                Version = version ?? "",
                IsClustered = isClustered,
                Source = "Network"
            });
        }

        return instances;
    }

    private List<IPAddress> GetBroadcastAddresses()
    {
        var addresses = new List<IPAddress>();

        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var properties = nic.GetIPProperties();
                foreach (var unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var ip = unicast.Address;
                    var mask = unicast.IPv4Mask;

                    if (mask != null)
                    {
                        var ipBytes = ip.GetAddressBytes();
                        var maskBytes = mask.GetAddressBytes();
                        var broadcastBytes = new byte[4];

                        for (int i = 0; i < 4; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        }

                        addresses.Add(new IPAddress(broadcastBytes));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get broadcast addresses");
        }

        // Always include common broadcast
        addresses.Add(IPAddress.Broadcast);

        return addresses.Distinct().ToList();
    }

    #endregion

    #region Private Methods - Encryption

    private byte[] GenerateMachineKey()
    {
        // Create a machine-specific key based on machine name and a salt
        var machineId = Environment.MachineName + "-HospitalityPOS-ConnectionConfig";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
    }

    private string EncryptString(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string DecryptString(string encryptedText)
    {
        var fullData = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        // Extract IV from beginning
        var iv = new byte[16];
        Buffer.BlockCopy(fullData, 0, iv, 0, 16);
        aes.IV = iv;

        // Extract encrypted data
        var encryptedBytes = new byte[fullData.Length - 16];
        Buffer.BlockCopy(fullData, 16, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    #endregion
}
