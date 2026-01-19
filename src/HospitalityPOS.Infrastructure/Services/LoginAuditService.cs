using System.Net;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for login audit operations.
/// </summary>
public class LoginAuditService : ILoginAuditService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginAuditService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public LoginAuditService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task RecordLoginAttemptAsync(
        int? userId,
        string username,
        bool success,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        var audit = new LoginAudit
        {
            UserId = userId,
            Username = username,
            Success = success,
            FailureReason = failureReason,
            IpAddress = GetLocalIpAddress(),
            MachineName = Environment.MachineName,
            DeviceInfo = GetDeviceInfo(),
            Timestamp = DateTime.UtcNow,
            IsLogout = false
        };

        _context.LoginAudits.Add(audit);
        await _context.SaveChangesAsync(cancellationToken);

        if (success)
        {
            _logger.Information("Login recorded for user {Username} (ID: {UserId})", username, userId);
        }
        else
        {
            _logger.Warning("Failed login attempt for user {Username}: {Reason}", username, failureReason);
        }
    }

    /// <inheritdoc />
    public async Task RecordLogoutAsync(
        int userId,
        string username,
        DateTime? loginTime = null,
        CancellationToken cancellationToken = default)
    {
        int? sessionDuration = null;
        if (loginTime.HasValue)
        {
            sessionDuration = (int)(DateTime.UtcNow - loginTime.Value).TotalMinutes;
        }

        var audit = new LoginAudit
        {
            UserId = userId,
            Username = username,
            Success = true,
            IpAddress = GetLocalIpAddress(),
            MachineName = Environment.MachineName,
            DeviceInfo = GetDeviceInfo(),
            Timestamp = DateTime.UtcNow,
            IsLogout = true,
            SessionDurationMinutes = sessionDuration
        };

        _context.LoginAudits.Add(audit);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.Information("Logout recorded for user {Username} (ID: {UserId}), session duration: {Duration} minutes",
            username, userId, sessionDuration ?? 0);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoginAudit>> GetUserLoginHistoryAsync(
        int userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LoginAudits
            .Where(l => l.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            query = query.Where(l => l.Timestamp < endOfDay);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Include(l => l.User)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoginAudit>> GetLoginHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? successOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LoginAudits.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            query = query.Where(l => l.Timestamp < endOfDay);
        }

        if (successOnly.HasValue)
        {
            query = query.Where(l => l.Success == successOnly.Value);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Include(l => l.User)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, int>> GetSuspiciousActivityAsync(
        int minutes = 30,
        int threshold = 3,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

        var suspiciousActivity = await _context.LoginAudits
            .Where(l => l.Timestamp >= cutoff && !l.Success && !l.IsLogout)
            .GroupBy(l => l.Username)
            .Select(g => new { Username = g.Key, Count = g.Count() })
            .Where(x => x.Count >= threshold)
            .ToDictionaryAsync(x => x.Username, x => x.Count, cancellationToken);

        return suspiciousActivity;
    }

    /// <inheritdoc />
    public async Task<int> GetRecentFailedAttemptsAsync(
        string username,
        int minutes = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

        return await _context.LoginAudits
            .CountAsync(l => l.Username == username
                && l.Timestamp >= cutoff
                && !l.Success
                && !l.IsLogout,
                cancellationToken);
    }

    private static string? GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipv4 = host.AddressList.FirstOrDefault(ip =>
                ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ipv4?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string GetDeviceInfo()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    }
}
