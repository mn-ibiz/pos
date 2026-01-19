namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Represents the current system health status.
/// </summary>
public class SystemHealthDto
{
    /// <summary>
    /// Gets or sets whether the database connection is healthy.
    /// </summary>
    public bool IsDatabaseConnected { get; set; }

    /// <summary>
    /// Gets or sets the database latency in milliseconds.
    /// </summary>
    public int DatabaseLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets whether the default printer is available.
    /// </summary>
    public bool IsPrinterAvailable { get; set; }

    /// <summary>
    /// Gets or sets the printer status message.
    /// </summary>
    public string PrinterStatus { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the available disk space in GB.
    /// </summary>
    public double AvailableDiskSpaceGb { get; set; }

    /// <summary>
    /// Gets or sets whether disk space is low (below 1GB).
    /// </summary>
    public bool IsDiskSpaceLow => AvailableDiskSpaceGb < 1.0;

    /// <summary>
    /// Gets or sets the current memory usage percentage.
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets whether memory usage is high (above 80%).
    /// </summary>
    public bool IsMemoryHigh => MemoryUsagePercent > 80;

    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    public HealthStatus OverallStatus
    {
        get
        {
            if (!IsDatabaseConnected)
                return HealthStatus.Critical;
            if (IsDiskSpaceLow || IsMemoryHigh)
                return HealthStatus.Warning;
            return HealthStatus.Healthy;
        }
    }
}

/// <summary>
/// Represents the overall health status.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// System has warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// System has critical issues.
    /// </summary>
    Critical
}
