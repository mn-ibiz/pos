using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing terminal registration flow.
/// </summary>
public class TerminalRegistrationService : ITerminalRegistrationService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;
    private readonly ITerminalService _terminalService;
    private readonly ITerminalConfigurationService _configurationService;
    private readonly IMachineIdentifierService _machineIdentifierService;
    private readonly ITerminalSessionContext _sessionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalRegistrationService"/> class.
    /// </summary>
    public TerminalRegistrationService(
        POSDbContext context,
        ILogger logger,
        ITerminalService terminalService,
        ITerminalConfigurationService configurationService,
        IMachineIdentifierService machineIdentifierService,
        ITerminalSessionContext sessionContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _machineIdentifierService = machineIdentifierService ?? throw new ArgumentNullException(nameof(machineIdentifierService));
        _sessionContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
    }

    /// <inheritdoc />
    public async Task<RegistrationValidationResult> ValidateRegistrationAsync(
        TerminalRegistrationInfo request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate required fields
        if (request.StoreId <= 0)
        {
            errors.Add("Store must be selected.");
        }

        if (string.IsNullOrWhiteSpace(request.MachineIdentifier))
        {
            errors.Add("Machine identifier is required.");
        }

        // Check if registering existing terminal
        if (request.ExistingTerminalId.HasValue)
        {
            var existingTerminal = await _terminalService.GetTerminalByIdAsync(
                request.ExistingTerminalId.Value, cancellationToken).ConfigureAwait(false);

            if (existingTerminal is null)
            {
                errors.Add($"Terminal ID {request.ExistingTerminalId} not found.");
            }
            else if (!string.IsNullOrEmpty(existingTerminal.MachineIdentifier) &&
                     existingTerminal.MachineIdentifier != request.MachineIdentifier)
            {
                errors.Add("This terminal is already assigned to another machine.");
            }
        }
        else
        {
            // Creating new terminal - validate code and name
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add("Terminal code is required.");
            }
            else if (!await _terminalService.IsTerminalCodeUniqueAsync(
                request.StoreId, request.Code, null, cancellationToken).ConfigureAwait(false))
            {
                errors.Add($"Terminal code '{request.Code}' already exists in this store.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Terminal name is required.");
            }
        }

        // Check if machine is already registered
        if (!string.IsNullOrWhiteSpace(request.MachineIdentifier))
        {
            var existingByMachine = await GetExistingRegistrationAsync(
                request.MachineIdentifier, cancellationToken).ConfigureAwait(false);

            if (existingByMachine is not null &&
                existingByMachine.Id != request.ExistingTerminalId)
            {
                errors.Add($"This machine is already registered as terminal '{existingByMachine.Code}'.");
            }
        }

        return new RegistrationValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <inheritdoc />
    public async Task<TerminalRegistrationResult> RegisterTerminalAsync(
        TerminalRegistrationInfo request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting terminal registration for machine {MachineId}",
            request.MachineIdentifier);

        // Validate first
        var validation = await ValidateRegistrationAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return TerminalRegistrationResult.Failed(string.Join("; ", validation.Errors));
        }

        try
        {
            Terminal terminal;

            // Use existing or create new terminal
            if (request.ExistingTerminalId.HasValue)
            {
                // Bind to existing terminal
                var success = await _terminalService.BindMachineAsync(
                    request.ExistingTerminalId.Value,
                    request.MachineIdentifier,
                    userId,
                    cancellationToken).ConfigureAwait(false);

                if (!success)
                {
                    return TerminalRegistrationResult.Failed("Failed to bind machine to terminal.");
                }

                terminal = (await _terminalService.GetTerminalByIdAsync(
                    request.ExistingTerminalId.Value, cancellationToken).ConfigureAwait(false))!;
            }
            else
            {
                // Create new terminal
                terminal = await _terminalService.RegisterTerminalAsync(
                    new TerminalRegistrationRequest
                    {
                        StoreId = request.StoreId,
                        Code = request.Code,
                        Name = request.Name,
                        TerminalType = request.TerminalType,
                        BusinessMode = request.BusinessMode,
                        MachineIdentifier = request.MachineIdentifier,
                        IpAddress = GetLocalIpAddress()
                    },
                    userId,
                    cancellationToken).ConfigureAwait(false);
            }

            // Get store info
            var store = await _context.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == terminal.StoreId, cancellationToken)
                .ConfigureAwait(false);

            // Create local configuration
            var config = new TerminalConfiguration
            {
                Version = "1.0",
                Terminal = new TerminalIdentity
                {
                    Id = terminal.Id,
                    Code = terminal.Code,
                    Name = terminal.Name,
                    StoreId = terminal.StoreId,
                    StoreName = store?.Name ?? string.Empty,
                    Type = terminal.TerminalType,
                    BusinessMode = terminal.BusinessMode,
                    MachineIdentifier = terminal.MachineIdentifier,
                    IsMainRegister = terminal.IsMainRegister
                },
                Database = new DatabaseSettings
                {
                    Server = request.Database.Server,
                    Database = request.Database.Database,
                    IntegratedSecurity = request.Database.UseWindowsAuth,
                    Username = request.Database.SqlUsername,
                    ConnectionTimeout = request.Database.ConnectionTimeout
                },
                Hardware = request.Hardware,
                Settings = new TerminalSettings
                {
                    AutoLogoutMinutes = 30,
                    RequireCashCountOnZReport = true,
                    PrintReceiptAutomatically = true,
                    SoundEnabled = true
                },
                RegisteredAt = DateTime.UtcNow,
                LastSync = DateTime.UtcNow
            };

            // Save local configuration
            await _configurationService.SaveLocalConfigurationAsync(config, cancellationToken).ConfigureAwait(false);

            // Initialize session context
            await _sessionContext.InitializeAsync(cancellationToken).ConfigureAwait(false);

            _logger.Information("Terminal registration completed: {Code} ({Name}) - Machine: {MachineId}",
                terminal.Code, terminal.Name, terminal.MachineIdentifier);

            return TerminalRegistrationResult.Successful(terminal, config);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Terminal registration failed for machine {MachineId}", request.MachineIdentifier);
            return TerminalRegistrationResult.Failed($"Registration failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TerminalRegistrationResult> RebindTerminalAsync(
        int terminalId,
        string newMachineIdentifier,
        int userId,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Rebinding terminal {TerminalId} to new machine {MachineId}",
            terminalId, newMachineIdentifier);

        try
        {
            // Get existing terminal
            var terminal = await _terminalService.GetTerminalByIdAsync(terminalId, cancellationToken)
                .ConfigureAwait(false);

            if (terminal is null)
            {
                return TerminalRegistrationResult.Failed($"Terminal ID {terminalId} not found.");
            }

            // Check if new machine is already registered
            var existingByMachine = await GetExistingRegistrationAsync(newMachineIdentifier, cancellationToken)
                .ConfigureAwait(false);

            if (existingByMachine is not null && existingByMachine.Id != terminalId)
            {
                return TerminalRegistrationResult.Failed(
                    $"Machine is already registered as terminal '{existingByMachine.Code}'.");
            }

            // Unbind old machine and bind new
            await _terminalService.UnbindMachineAsync(terminalId, userId, cancellationToken).ConfigureAwait(false);
            await _terminalService.BindMachineAsync(terminalId, newMachineIdentifier, userId, cancellationToken)
                .ConfigureAwait(false);

            // Refresh terminal
            terminal = await _terminalService.GetTerminalByIdAsync(terminalId, cancellationToken)
                .ConfigureAwait(false);

            _logger.Information("Terminal {Code} rebound to machine {MachineId}",
                terminal!.Code, newMachineIdentifier);

            return new TerminalRegistrationResult
            {
                Success = true,
                Terminal = terminal
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to rebind terminal {TerminalId}", terminalId);
            return TerminalRegistrationResult.Failed($"Rebind failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Terminal?> GetExistingRegistrationAsync(
        string machineIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _terminalService.GetTerminalByMachineIdAsync(machineIdentifier, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UnbindTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _terminalService.UnbindMachineAsync(terminalId, userId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ConnectionTestResult> TestDatabaseConnectionAsync(
        DatabaseConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = connectionInfo.ToConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Get database version
            using var versionCommand = new SqlCommand("SELECT @@VERSION", connection);
            var version = await versionCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;

            // Get available stores
            var stores = new List<StoreInfo>();
            using var storesCommand = new SqlCommand(
                "SELECT Id, Name, ISNULL(StoreCode, '') as StoreCode FROM Stores WHERE IsActive = 1 ORDER BY Name",
                connection);

            using var reader = await storesCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                stores.Add(new StoreInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Code = reader.GetString(2)
                });
            }

            _logger.Information("Database connection test successful. Found {StoreCount} stores.", stores.Count);

            return ConnectionTestResult.Successful(
                version?.Split('\n').FirstOrDefault() ?? "Unknown",
                stores);
        }
        catch (SqlException ex)
        {
            _logger.Warning(ex, "Database connection test failed");
            return ConnectionTestResult.Failed($"Connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during database connection test");
            return ConnectionTestResult.Failed($"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Terminal>> GetUnassignedTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _terminalService.GetUnassignedTerminalsAsync(storeId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string? GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Ignore errors getting IP
        }
        return null;
    }
}
