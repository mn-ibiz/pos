using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing terminal registration flow.
/// </summary>
public interface ITerminalRegistrationService
{
    /// <summary>
    /// Validates all prerequisites for registration.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any errors.</returns>
    Task<RegistrationValidationResult> ValidateRegistrationAsync(
        TerminalRegistrationInfo request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the complete registration flow.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="userId">The user performing registration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result.</returns>
    Task<TerminalRegistrationResult> RegisterTerminalAsync(
        TerminalRegistrationInfo request,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-registers a terminal with a new machine (admin action).
    /// </summary>
    /// <param name="terminalId">The terminal ID to rebind.</param>
    /// <param name="newMachineIdentifier">The new machine identifier.</param>
    /// <param name="userId">The user performing rebind.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result.</returns>
    Task<TerminalRegistrationResult> RebindTerminalAsync(
        int terminalId,
        string newMachineIdentifier,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current machine is already registered.
    /// </summary>
    /// <param name="machineIdentifier">The machine identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing terminal if registered.</returns>
    Task<Terminal?> GetExistingRegistrationAsync(
        string machineIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unbinds a terminal from its machine for re-assignment.
    /// </summary>
    /// <param name="terminalId">The terminal ID to unbind.</param>
    /// <param name="userId">The user performing unbind.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if unbind was successful.</returns>
    Task<bool> UnbindTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a database connection.
    /// </summary>
    /// <param name="connectionInfo">The connection information to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection test result.</returns>
    Task<ConnectionTestResult> TestDatabaseConnectionAsync(
        DatabaseConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available unassigned terminals for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unassigned terminals.</returns>
    Task<IReadOnlyList<Terminal>> GetUnassignedTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Complete terminal registration information.
/// </summary>
public class TerminalRegistrationInfo
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

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
    public TerminalType TerminalType { get; set; } = TerminalType.Register;

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; } = BusinessMode.Supermarket;

    /// <summary>
    /// Gets or sets the machine identifier.
    /// </summary>
    public string MachineIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use an existing terminal ID.
    /// </summary>
    public int? ExistingTerminalId { get; set; }

    /// <summary>
    /// Gets or sets the database connection info.
    /// </summary>
    public DatabaseConnectionInfo Database { get; set; } = new();

    /// <summary>
    /// Gets or sets the hardware configuration.
    /// </summary>
    public HardwareSettings Hardware { get; set; } = new();
}

/// <summary>
/// Database connection information.
/// </summary>
public class DatabaseConnectionInfo
{
    /// <summary>
    /// Gets or sets the server address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use Windows authentication.
    /// </summary>
    public bool UseWindowsAuth { get; set; } = true;

    /// <summary>
    /// Gets or sets the SQL username.
    /// </summary>
    public string? SqlUsername { get; set; }

    /// <summary>
    /// Gets or sets the SQL password.
    /// </summary>
    public string? SqlPassword { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Builds the connection string.
    /// </summary>
    public string ToConnectionString()
    {
        var builder = new System.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = UseWindowsAuth,
            ConnectTimeout = ConnectionTimeout,
            TrustServerCertificate = true
        };

        if (!UseWindowsAuth && !string.IsNullOrEmpty(SqlUsername))
        {
            builder.UserID = SqlUsername;
            builder.Password = SqlPassword;
        }

        return builder.ConnectionString;
    }
}

/// <summary>
/// Result of registration validation.
/// </summary>
public class RegistrationValidationResult
{
    /// <summary>
    /// Gets or sets whether validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets any warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static RegistrationValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static RegistrationValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Result of terminal registration.
/// </summary>
public class TerminalRegistrationResult
{
    /// <summary>
    /// Gets or sets whether registration succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the registered terminal.
    /// </summary>
    public Terminal? Terminal { get; set; }

    /// <summary>
    /// Gets or sets the local configuration.
    /// </summary>
    public TerminalConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets any warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TerminalRegistrationResult Successful(Terminal terminal, TerminalConfiguration config) => new()
    {
        Success = true,
        Terminal = terminal,
        Configuration = config
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TerminalRegistrationResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Result of database connection test.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Gets or sets whether connection succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the database version if connected.
    /// </summary>
    public string? DatabaseVersion { get; set; }

    /// <summary>
    /// Gets or sets the available stores.
    /// </summary>
    public List<StoreInfo> AvailableStores { get; set; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConnectionTestResult Successful(string version, List<StoreInfo> stores) => new()
    {
        Success = true,
        DatabaseVersion = version,
        AvailableStores = stores
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ConnectionTestResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Basic store information.
/// </summary>
public class StoreInfo
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
