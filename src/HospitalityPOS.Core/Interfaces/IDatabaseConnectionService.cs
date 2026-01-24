namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing database connection configuration.
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// Gets whether a connection configuration exists.
    /// </summary>
    bool HasConnectionConfiguration { get; }

    /// <summary>
    /// Gets the current connection string.
    /// </summary>
    string? GetConnectionString();

    /// <summary>
    /// Saves connection configuration.
    /// </summary>
    void SaveConnectionConfiguration(DatabaseConnectionConfig config);

    /// <summary>
    /// Loads connection configuration.
    /// </summary>
    DatabaseConnectionConfig? LoadConnectionConfiguration();

    /// <summary>
    /// Tests a database connection.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(DatabaseConnectionConfig config);

    /// <summary>
    /// Tests the current saved connection.
    /// </summary>
    Task<ConnectionTestResult> TestCurrentConnectionAsync();

    /// <summary>
    /// Discovers SQL Server instances on the network.
    /// </summary>
    Task<List<SqlServerInstance>> DiscoverSqlServersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets databases on a SQL Server instance.
    /// </summary>
    Task<List<string>> GetDatabasesAsync(DatabaseConnectionConfig config);

    /// <summary>
    /// Clears the saved connection configuration.
    /// </summary>
    void ClearConnectionConfiguration();
}

/// <summary>
/// Database connection configuration.
/// </summary>
public class DatabaseConnectionConfig
{
    /// <summary>
    /// SQL Server hostname or IP address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server instance name (optional).
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Database name.
    /// </summary>
    public string Database { get; set; } = "HospitalityPOS";

    /// <summary>
    /// Whether to use Windows Authentication.
    /// </summary>
    public bool UseWindowsAuthentication { get; set; }

    /// <summary>
    /// SQL Server username (when not using Windows Auth).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SQL Server password (when not using Windows Auth).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Whether to encrypt the connection.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Whether to trust the server certificate.
    /// </summary>
    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>
    /// Gets the full server name including instance.
    /// </summary>
    public string FullServerName => string.IsNullOrEmpty(InstanceName)
        ? Server
        : $"{Server}\\{InstanceName}";

    /// <summary>
    /// Builds a connection string from this configuration.
    /// </summary>
    public string BuildConnectionString()
    {
        var parts = new List<string>
        {
            $"Data Source={FullServerName}",
            $"Initial Catalog={Database}",
            $"Connect Timeout={ConnectionTimeout}",
            $"Encrypt={Encrypt}",
            $"TrustServerCertificate={TrustServerCertificate}",
            "MultipleActiveResultSets=True",
            "Application Name=HospitalityPOS"
        };

        if (UseWindowsAuthentication)
        {
            parts.Add("Integrated Security=True");
        }
        else
        {
            parts.Add($"User ID={Username}");
            parts.Add($"Password={Password}");
        }

        return string.Join(";", parts);
    }
}

/// <summary>
/// Result of a connection test.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Whether the connection was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if connection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// SQL Server version if connected.
    /// </summary>
    public string? ServerVersion { get; set; }

    /// <summary>
    /// Database exists flag.
    /// </summary>
    public bool DatabaseExists { get; set; }

    /// <summary>
    /// Connection time in milliseconds.
    /// </summary>
    public long ConnectionTimeMs { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConnectionTestResult Success(string serverVersion, bool databaseExists, long connectionTimeMs) => new()
    {
        IsSuccess = true,
        ServerVersion = serverVersion,
        DatabaseExists = databaseExists,
        ConnectionTimeMs = connectionTimeMs
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ConnectionTestResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Discovered SQL Server instance.
/// </summary>
public class SqlServerInstance
{
    /// <summary>
    /// Server name or IP.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Instance name (empty for default instance).
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a clustered instance.
    /// </summary>
    public bool IsClustered { get; set; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(InstanceName)
        ? ServerName
        : $"{ServerName}\\{InstanceName}";

    /// <summary>
    /// Discovery source (Network, Local, Manual).
    /// </summary>
    public string Source { get; set; } = "Network";
}
