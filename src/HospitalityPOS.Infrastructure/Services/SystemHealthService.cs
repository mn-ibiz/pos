using System.Diagnostics;
using System.Drawing.Printing;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for monitoring system health.
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthService"/> class.
    /// </summary>
    public SystemHealthService(POSDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SystemHealthDto> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var health = new SystemHealthDto
        {
            LastCheckTime = DateTime.Now
        };

        // Check database
        try
        {
            var sw = Stopwatch.StartNew();
            health.IsDatabaseConnected = await CheckDatabaseConnectionAsync(cancellationToken);
            sw.Stop();
            health.DatabaseLatencyMs = (int)sw.ElapsedMilliseconds;
        }
        catch
        {
            health.IsDatabaseConnected = false;
            health.DatabaseLatencyMs = -1;
        }

        // Check printer
        try
        {
            var printers = PrinterSettings.InstalledPrinters;
            health.IsPrinterAvailable = printers.Count > 0;
            health.PrinterStatus = health.IsPrinterAvailable
                ? $"{printers.Count} printer(s) available"
                : "No printers found";
        }
        catch (Exception ex)
        {
            health.IsPrinterAvailable = false;
            health.PrinterStatus = "Error checking printers";
            _logger.Warning(ex, "Failed to check printer status");
        }

        // Check disk space
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
            health.AvailableDiskSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
        }
        catch (Exception ex)
        {
            health.AvailableDiskSpaceGb = -1;
            _logger.Warning(ex, "Failed to check disk space");
        }

        // Check memory
        try
        {
            using var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;
            // Estimate percentage based on typical available memory
            health.MemoryUsagePercent = Math.Min(100, (workingSet / (1024.0 * 1024.0 * 1024.0)) / 8.0 * 100);
        }
        catch (Exception ex)
        {
            health.MemoryUsagePercent = -1;
            _logger.Warning(ex, "Failed to check memory usage");
        }

        return health;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Database connection check failed");
            return false;
        }
    }
}
