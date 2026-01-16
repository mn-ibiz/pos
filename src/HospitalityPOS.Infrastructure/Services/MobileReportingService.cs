// src/HospitalityPOS.Infrastructure/Services/MobileReportingService.cs
// Service implementation for mobile reporting app functionality
// Story 41-1: Mobile Reporting App

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Mobile;
using Microsoft.IdentityModel.Tokens;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of mobile reporting service.
/// Handles authentication, dashboard, reports, alerts, and device management.
/// </summary>
public class MobileReportingService : IMobileReportingService
{
    // In-memory storage (replace with database in production)
    private readonly Dictionary<int, MobileSession> _sessions = new();
    private readonly Dictionary<string, DeviceRegistration> _deviceRegistrations = new();
    private readonly Dictionary<int, NotificationPreferences> _notificationPreferences = new();
    private readonly List<MobileAlert> _alerts = new();
    private readonly Dictionary<string, string> _refreshTokens = new();

    private int _sessionIdCounter;
    private int _deviceIdCounter;
    private int _alertIdCounter;

    // Configuration (would come from IConfiguration in production)
    private readonly string _jwtSecret = "MobileReportingSecretKey_ThisShouldBeAtLeast32CharsLong!";
    private readonly int _accessTokenExpiryMinutes = 60;
    private readonly int _refreshTokenExpiryDays = 30;
    private readonly int _sessionTimeoutMinutes = 1440; // 24 hours

    // Sample data for demonstration
    private readonly List<MobileBranchSummary> _sampleBranches;
    private readonly Dictionary<int, MobileUserInfo> _sampleUsers;

    public MobileReportingService()
    {
        // Initialize sample data
        _sampleBranches = new List<MobileBranchSummary>
        {
            new() { Id = 1, Name = "Main Store", Address = "123 Main St", WorkPeriodOpen = true, IsOnline = true },
            new() { Id = 2, Name = "Branch 2", Address = "456 Oak Ave", WorkPeriodOpen = true, IsOnline = true },
            new() { Id = 3, Name = "Branch 3", Address = "789 Pine Rd", WorkPeriodOpen = false, IsOnline = true }
        };

        _sampleUsers = new Dictionary<int, MobileUserInfo>
        {
            [1] = new MobileUserInfo
            {
                UserId = 1,
                Username = "admin",
                DisplayName = "System Administrator",
                Email = "admin@hospitalitypos.com",
                Role = "Administrator",
                Permissions = new List<string> { "ViewReports", "ManageUsers", "ViewAllBranches" },
                AccessibleBranchIds = new List<int> { 1, 2, 3 }
            },
            [2] = new MobileUserInfo
            {
                UserId = 2,
                Username = "manager",
                DisplayName = "Store Manager",
                Email = "manager@hospitalitypos.com",
                Role = "Manager",
                Permissions = new List<string> { "ViewReports", "ViewBranchData" },
                AccessibleBranchIds = new List<int> { 1 }
            }
        };

        // Initialize default notification preferences
        foreach (var user in _sampleUsers.Values)
        {
            _notificationPreferences[user.UserId] = new NotificationPreferences
            {
                UserId = user.UserId,
                DailySummaryEnabled = true,
                LowStockAlertsEnabled = true,
                ExpiryAlertsEnabled = true,
                ZReportNotificationsEnabled = true
            };
        }
    }

    #region Events

    public event EventHandler<MobileSessionEventArgs>? SessionCreated;
    public event EventHandler<MobileSessionEventArgs>? SessionRevoked;

    protected virtual void OnSessionCreated(MobileSessionEventArgs e) => SessionCreated?.Invoke(this, e);
    protected virtual void OnSessionRevoked(MobileSessionEventArgs e) => SessionRevoked?.Invoke(this, e);

    #endregion

    #region Authentication

    public async Task<MobileLoginResponse> LoginAsync(MobileLoginRequest request)
    {
        await Task.CompletedTask;

        // Validate credentials (simplified - would check against database)
        var user = _sampleUsers.Values.FirstOrDefault(u =>
            u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return new MobileLoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }

        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);

        // Store refresh token
        _refreshTokens[refreshToken] = $"{user.UserId}:{request.DeviceId}";

        // Create session
        var session = new MobileSession
        {
            Id = ++_sessionIdCounter,
            UserId = user.UserId,
            DeviceId = request.DeviceId,
            Platform = request.Platform,
            DeviceModel = request.DeviceModel,
            OsVersion = request.OsVersion,
            AppVersion = request.AppVersion,
            PushToken = request.PushToken,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_sessionTimeoutMinutes)
        };
        _sessions[session.Id] = session;

        // Register device for push notifications if token provided
        if (!string.IsNullOrEmpty(request.PushToken))
        {
            await RegisterDeviceAsync(new DeviceRegistration
            {
                UserId = user.UserId,
                DeviceId = request.DeviceId,
                PushToken = request.PushToken,
                Platform = request.Platform,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            });
        }

        // Get notification preferences
        user.NotificationPrefs = _notificationPreferences.GetValueOrDefault(user.UserId) ?? new NotificationPreferences();

        OnSessionCreated(new MobileSessionEventArgs
        {
            UserId = user.UserId,
            DeviceId = request.DeviceId,
            Status = SessionStatus.Active,
            Timestamp = DateTime.UtcNow
        });

        return new MobileLoginResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _accessTokenExpiryMinutes * 60,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    public async Task<TokenRefreshResponse> RefreshTokenAsync(TokenRefreshRequest request)
    {
        await Task.CompletedTask;

        if (!_refreshTokens.TryGetValue(request.RefreshToken, out var tokenData))
        {
            return new TokenRefreshResponse
            {
                Success = false,
                ErrorMessage = "Invalid refresh token"
            };
        }

        var parts = tokenData.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var userId))
        {
            return new TokenRefreshResponse
            {
                Success = false,
                ErrorMessage = "Invalid token data"
            };
        }

        if (!_sampleUsers.TryGetValue(userId, out var user))
        {
            return new TokenRefreshResponse
            {
                Success = false,
                ErrorMessage = "User not found"
            };
        }

        // Generate new tokens
        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);

        // Revoke old refresh token and store new one
        _refreshTokens.Remove(request.RefreshToken);
        _refreshTokens[newRefreshToken] = tokenData;

        return new TokenRefreshResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _accessTokenExpiryMinutes * 60,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> LogoutAsync(int userId, string deviceId)
    {
        await Task.CompletedTask;

        var session = _sessions.Values.FirstOrDefault(s =>
            s.UserId == userId && s.DeviceId == deviceId && s.Status == SessionStatus.Active);

        if (session == null) return false;

        session.Status = SessionStatus.Revoked;

        // Remove refresh tokens for this device
        var toRemove = _refreshTokens
            .Where(kvp => kvp.Value == $"{userId}:{deviceId}")
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _refreshTokens.Remove(key);
        }

        OnSessionRevoked(new MobileSessionEventArgs
        {
            UserId = userId,
            DeviceId = deviceId,
            Status = SessionStatus.Revoked,
            Timestamp = DateTime.UtcNow
        });

        return true;
    }

    public async Task<int> LogoutAllDevicesAsync(int userId)
    {
        await Task.CompletedTask;

        var sessions = _sessions.Values
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToList();

        foreach (var session in sessions)
        {
            session.Status = SessionStatus.Revoked;
            OnSessionRevoked(new MobileSessionEventArgs
            {
                UserId = userId,
                DeviceId = session.DeviceId,
                Status = SessionStatus.Revoked,
                Timestamp = DateTime.UtcNow
            });
        }

        // Remove all refresh tokens for this user
        var toRemove = _refreshTokens
            .Where(kvp => kvp.Value.StartsWith($"{userId}:"))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _refreshTokens.Remove(key);
        }

        return sessions.Count;
    }

    public async Task<IReadOnlyList<MobileSession>> GetActiveSessionsAsync(int userId)
    {
        await Task.CompletedTask;

        return _sessions.Values
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastActiveAt)
            .ToList();
    }

    public async Task<MobileUserInfo?> ValidateTokenAsync(string accessToken)
    {
        await Task.CompletedTask;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out _);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return _sampleUsers.GetValueOrDefault(userId);
            }
        }
        catch
        {
            // Token validation failed
        }

        return null;
    }

    private string GenerateAccessToken(MobileUserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    #endregion

    #region Dashboard

    public async Task<MobileDashboard> GetDashboardAsync(int userId, int? branchId = null)
    {
        var branch = branchId.HasValue
            ? _sampleBranches.FirstOrDefault(b => b.Id == branchId.Value)
            : _sampleBranches.First();

        if (branch == null)
        {
            branch = _sampleBranches.First();
        }

        var todaySales = await GetTodaySalesAsync(branch.Id);
        var comparison = await GetSalesComparisonAsync(branch.Id);
        var quickStats = await GetQuickStatsAsync(branch.Id);
        var alertsResponse = await GetAlertsAsync(userId, branch.Id);

        return new MobileDashboard
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            AsOf = DateTime.UtcNow,
            TodaySales = todaySales,
            Comparison = comparison,
            Alerts = alertsResponse.RecentAlerts,
            CurrentWorkPeriod = branch.WorkPeriodOpen ? new WorkPeriodInfo
            {
                Id = 1,
                OpenedAt = DateTime.Today.AddHours(8),
                OpenedBy = "Manager",
                IsOpen = true
            } : null,
            QuickStats = quickStats
        };
    }

    public async Task<DailySalesSummary> GetTodaySalesAsync(int? branchId = null)
    {
        await Task.CompletedTask;

        // Generate sample data
        var random = new Random(DateTime.Today.DayOfYear + (branchId ?? 0));
        var baseSales = random.Next(80000, 150000);

        return new DailySalesSummary
        {
            TotalSales = baseSales,
            TransactionCount = random.Next(100, 200),
            CashSales = baseSales * 0.4m,
            MpesaSales = baseSales * 0.45m,
            CardSales = baseSales * 0.1m,
            OtherSales = baseSales * 0.05m,
            ItemsSold = random.Next(300, 600),
            DiscountsGiven = baseSales * 0.02m,
            VoidCount = random.Next(0, 5),
            VoidAmount = random.Next(500, 2000)
        };
    }

    public async Task<SalesComparison> GetSalesComparisonAsync(int? branchId = null, string comparisonPeriod = "Yesterday")
    {
        await Task.CompletedTask;

        var random = new Random(DateTime.Today.DayOfYear + (branchId ?? 0));
        var changePercent = (random.NextDouble() - 0.3) * 40; // -12% to +28%

        return new SalesComparison
        {
            SalesChangePercent = (decimal)changePercent,
            TransactionChangePercent = (decimal)(changePercent * 0.8),
            AvgTicketChangePercent = (decimal)(changePercent * 0.2),
            ComparisonPeriod = comparisonPeriod,
            PreviousSales = random.Next(70000, 140000),
            PreviousTransactions = random.Next(90, 180)
        };
    }

    public async Task<QuickStats> GetQuickStatsAsync(int? branchId = null)
    {
        await Task.CompletedTask;

        var random = new Random(DateTime.Today.DayOfYear + (branchId ?? 0));

        return new QuickStats
        {
            ActiveTables = random.Next(5, 15),
            PendingOrders = random.Next(0, 8),
            LowStockItems = random.Next(3, 12),
            ExpiringItems = random.Next(0, 5),
            CashInDrawer = random.Next(5000, 25000)
        };
    }

    #endregion

    #region Sales Reports

    public async Task<MobileSalesReport> GetSalesReportAsync(MobileSalesReportRequest request)
    {
        var categorySales = request.IncludeCategoryBreakdown
            ? await GetSalesByCategoryAsync(request.DateFrom, request.DateTo, request.BranchId)
            : new List<CategorySalesItem>();

        var paymentSales = request.IncludePaymentBreakdown
            ? await GetSalesByPaymentMethodAsync(request.DateFrom, request.DateTo, request.BranchId)
            : new List<PaymentMethodSalesItem>();

        var dailyBreakdown = request.IncludeDailyBreakdown
            ? await GetDailyBreakdownAsync(request.DateFrom, request.DateTo, request.BranchId)
            : new List<DailySalesItem>();

        var hourlyBreakdown = request.IncludeHourlyBreakdown
            ? await GetHourlySalesAsync(request.DateFrom, request.BranchId)
            : null;

        var topProducts = request.TopProductsCount.HasValue
            ? await GetTopProductsAsync(request.DateFrom, request.DateTo, request.TopProductsCount.Value, request.BranchId)
            : null;

        var totalSales = dailyBreakdown.Sum(d => d.Sales);
        var totalTransactions = dailyBreakdown.Sum(d => d.Transactions);

        return new MobileSalesReport
        {
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            BranchId = request.BranchId,
            BranchName = request.BranchId.HasValue
                ? _sampleBranches.FirstOrDefault(b => b.Id == request.BranchId)?.Name
                : null,
            TotalSales = totalSales,
            TotalTransactions = totalTransactions,
            TotalItemsSold = totalTransactions * 3, // Approximate
            TotalDiscounts = totalSales * 0.02m,
            GrossMargin = totalSales * 0.35m,
            GrossMarginPercent = 35m,
            ByCategory = categorySales.ToList(),
            ByPaymentMethod = paymentSales.ToList(),
            DailyBreakdown = dailyBreakdown.ToList(),
            HourlyBreakdown = hourlyBreakdown?.ToList(),
            TopProducts = topProducts?.ToList()
        };
    }

    public async Task<IReadOnlyList<CategorySalesItem>> GetSalesByCategoryAsync(
        DateOnly dateFrom, DateOnly dateTo, int? branchId = null)
    {
        await Task.CompletedTask;

        var categories = new[] { "Food", "Beverages", "Desserts", "Snacks", "Alcohol" };
        var random = new Random(dateFrom.DayNumber + (branchId ?? 0));
        var totalSales = random.Next(200000, 500000);

        var items = new List<CategorySalesItem>();
        var remainingPercent = 100m;

        for (int i = 0; i < categories.Length; i++)
        {
            var percent = i < categories.Length - 1
                ? (decimal)(random.NextDouble() * (double)remainingPercent * 0.6)
                : remainingPercent;
            remainingPercent -= percent;

            items.Add(new CategorySalesItem
            {
                CategoryId = i + 1,
                CategoryName = categories[i],
                Sales = totalSales * percent / 100,
                Quantity = random.Next(50, 500),
                Percentage = percent
            });
        }

        return items.OrderByDescending(c => c.Sales).ToList();
    }

    public async Task<IReadOnlyList<PaymentMethodSalesItem>> GetSalesByPaymentMethodAsync(
        DateOnly dateFrom, DateOnly dateTo, int? branchId = null)
    {
        await Task.CompletedTask;

        var random = new Random(dateFrom.DayNumber + (branchId ?? 0));
        var totalSales = random.Next(200000, 500000);

        return new List<PaymentMethodSalesItem>
        {
            new() { PaymentMethod = "M-Pesa", Amount = totalSales * 0.45m, TransactionCount = random.Next(200, 400), Percentage = 45m },
            new() { PaymentMethod = "Cash", Amount = totalSales * 0.35m, TransactionCount = random.Next(150, 300), Percentage = 35m },
            new() { PaymentMethod = "Card", Amount = totalSales * 0.15m, TransactionCount = random.Next(50, 150), Percentage = 15m },
            new() { PaymentMethod = "Credit", Amount = totalSales * 0.05m, TransactionCount = random.Next(10, 50), Percentage = 5m }
        };
    }

    public async Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(
        DateOnly dateFrom, DateOnly dateTo, int count = 10, int? branchId = null)
    {
        await Task.CompletedTask;

        var products = new[]
        {
            "Chicken Wings", "Caesar Salad", "Beef Burger", "Fish & Chips", "Pizza Margherita",
            "Coca-Cola", "Fresh Juice", "Beer (500ml)", "Coffee Latte", "Mineral Water"
        };

        var random = new Random(dateFrom.DayNumber + (branchId ?? 0));
        var items = new List<TopProductItem>();

        for (int i = 0; i < Math.Min(count, products.Length); i++)
        {
            var quantity = random.Next(50, 200) * (products.Length - i);
            items.Add(new TopProductItem
            {
                ProductId = i + 1,
                ProductName = products[i],
                QuantitySold = quantity,
                Revenue = quantity * random.Next(100, 500),
                Rank = i + 1
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<HourlySalesItem>> GetHourlySalesAsync(DateOnly date, int? branchId = null)
    {
        await Task.CompletedTask;

        var random = new Random(date.DayNumber + (branchId ?? 0));
        var items = new List<HourlySalesItem>();

        for (int hour = 8; hour <= 22; hour++)
        {
            var peakMultiplier = hour switch
            {
                >= 12 and <= 14 => 2.0, // Lunch peak
                >= 18 and <= 20 => 2.5, // Dinner peak
                _ => 1.0
            };

            var baseSales = random.Next(3000, 8000);
            items.Add(new HourlySalesItem
            {
                Hour = hour,
                HourLabel = hour < 12 ? $"{hour} AM" : hour == 12 ? "12 PM" : $"{hour - 12} PM",
                Sales = (decimal)(baseSales * peakMultiplier),
                Transactions = (int)(random.Next(5, 15) * peakMultiplier)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<DailySalesItem>> GetDailyBreakdownAsync(
        DateOnly dateFrom, DateOnly dateTo, int? branchId = null)
    {
        await Task.CompletedTask;

        var items = new List<DailySalesItem>();
        var random = new Random(dateFrom.DayNumber + (branchId ?? 0));

        for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
        {
            var weekendMultiplier = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1.3 : 1.0;
            var baseSales = random.Next(80000, 150000);

            items.Add(new DailySalesItem
            {
                Date = date,
                DayName = date.DayOfWeek.ToString(),
                Sales = (decimal)(baseSales * weekendMultiplier),
                Transactions = (int)(random.Next(100, 200) * weekendMultiplier)
            });
        }

        return items;
    }

    #endregion

    #region Alerts

    public async Task<MobileAlertsResponse> GetAlertsAsync(int userId, int? branchId = null)
    {
        var lowStock = await GetLowStockItemsAsync(branchId);
        var expiring = await GetExpiringItemsAsync(7, branchId);

        var userAlerts = _alerts
            .Where(a => !a.IsDismissed)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToList();

        return new MobileAlertsResponse
        {
            LowStock = lowStock.ToList(),
            Expiring = expiring.ToList(),
            RecentAlerts = userAlerts,
            TotalLowStockCount = lowStock.Count,
            TotalExpiringCount = expiring.Count,
            UnreadAlertCount = userAlerts.Count(a => !a.IsRead)
        };
    }

    public async Task<IReadOnlyList<StockAlertItem>> GetLowStockItemsAsync(int? branchId = null)
    {
        await Task.CompletedTask;

        var random = new Random(DateTime.Today.DayOfYear + (branchId ?? 0));
        var products = new[]
        {
            ("Milk (1L)", "Dairy", 5m, 20m),
            ("Bread", "Bakery", 8m, 15m),
            ("Sugar (1kg)", "Groceries", 3m, 10m),
            ("Cooking Oil", "Groceries", 2m, 8m),
            ("Coffee Beans", "Beverages", 4m, 12m)
        };

        return products.Select((p, i) => new StockAlertItem
        {
            ProductId = i + 1,
            ProductName = p.Item1,
            Barcode = $"690{random.Next(1000000, 9999999)}",
            CurrentStock = p.Item3,
            ReorderLevel = p.Item4,
            ReorderQuantity = p.Item4 * 2,
            CategoryName = p.Item2,
            Unit = "pcs",
            LastPurchasePrice = random.Next(100, 500)
        }).ToList();
    }

    public async Task<IReadOnlyList<ExpiryAlertItem>> GetExpiringItemsAsync(int daysAhead = 7, int? branchId = null)
    {
        await Task.CompletedTask;

        var random = new Random(DateTime.Today.DayOfYear + (branchId ?? 0));
        var products = new[]
        {
            ("Fresh Milk", "Dairy", 2),
            ("Yogurt", "Dairy", 3),
            ("Bread Rolls", "Bakery", 1),
            ("Fresh Juice", "Beverages", 5),
            ("Cheese Slices", "Dairy", 4)
        };

        return products.Select((p, i) => new ExpiryAlertItem
        {
            ProductId = i + 100,
            ProductName = p.Item1,
            BatchNumber = $"B{DateTime.Today:yyyyMM}{random.Next(100, 999)}",
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(p.Item3)),
            DaysUntilExpiry = p.Item3,
            Quantity = random.Next(5, 25),
            EstimatedValue = random.Next(500, 3000),
            CategoryName = p.Item2
        }).ToList();
    }

    public async Task<bool> MarkAlertAsReadAsync(int alertId)
    {
        await Task.CompletedTask;

        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null) return false;

        alert.IsRead = true;
        return true;
    }

    public async Task<int> MarkAllAlertsAsReadAsync(int userId)
    {
        await Task.CompletedTask;

        var unreadAlerts = _alerts.Where(a => !a.IsRead).ToList();
        foreach (var alert in unreadAlerts)
        {
            alert.IsRead = true;
        }

        return unreadAlerts.Count;
    }

    public async Task<bool> DismissAlertAsync(int alertId)
    {
        await Task.CompletedTask;

        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null) return false;

        alert.IsDismissed = true;
        return true;
    }

    #endregion

    #region Branches

    public async Task<IReadOnlyList<MobileBranchSummary>> GetBranchesAsync(int userId)
    {
        await Task.CompletedTask;

        var user = _sampleUsers.GetValueOrDefault(userId);
        if (user == null) return new List<MobileBranchSummary>();

        var accessibleBranches = _sampleBranches
            .Where(b => user.AccessibleBranchIds.Contains(b.Id))
            .ToList();

        // Update with today's sales data
        var random = new Random(DateTime.Today.DayOfYear);
        foreach (var branch in accessibleBranches)
        {
            branch.TodaySales = random.Next(50000, 150000);
            branch.TodayTransactions = random.Next(80, 200);
            branch.LowStockCount = random.Next(2, 10);
            branch.ExpiringItemsCount = random.Next(0, 5);
            branch.LastSyncAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 30));
        }

        return accessibleBranches;
    }

    public async Task<AllBranchesSummary> GetAllBranchesSummaryAsync(int userId)
    {
        var branches = await GetBranchesAsync(userId);

        var summary = new AllBranchesSummary
        {
            TotalSales = branches.Sum(b => b.TodaySales),
            TotalTransactions = branches.Sum(b => b.TodayTransactions),
            TotalBranches = branches.Count,
            BranchesOpen = branches.Count(b => b.WorkPeriodOpen),
            Branches = branches.ToList(),
            TopPerformingBranch = branches.OrderByDescending(b => b.TodaySales).FirstOrDefault()
        };

        return summary;
    }

    public async Task<MobileBranchSummary?> GetBranchSummaryAsync(int branchId)
    {
        await Task.CompletedTask;

        var branch = _sampleBranches.FirstOrDefault(b => b.Id == branchId);
        if (branch == null) return null;

        var random = new Random(DateTime.Today.DayOfYear + branchId);
        branch.TodaySales = random.Next(50000, 150000);
        branch.TodayTransactions = random.Next(80, 200);
        branch.LowStockCount = random.Next(2, 10);
        branch.ExpiringItemsCount = random.Next(0, 5);
        branch.LastSyncAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 30));

        return branch;
    }

    #endregion

    #region Device Registration

    public async Task<int> RegisterDeviceAsync(DeviceRegistration registration)
    {
        await Task.CompletedTask;

        // Check if device already registered
        if (_deviceRegistrations.ContainsKey(registration.DeviceId))
        {
            var existing = _deviceRegistrations[registration.DeviceId];
            existing.PushToken = registration.PushToken;
            existing.LastUsedAt = DateTime.UtcNow;
            existing.IsActive = true;
            return existing.Id;
        }

        registration.Id = ++_deviceIdCounter;
        registration.RegisteredAt = DateTime.UtcNow;
        _deviceRegistrations[registration.DeviceId] = registration;

        return registration.Id;
    }

    public async Task<bool> UpdatePushTokenAsync(string deviceId, string pushToken)
    {
        await Task.CompletedTask;

        if (!_deviceRegistrations.TryGetValue(deviceId, out var registration))
            return false;

        registration.PushToken = pushToken;
        registration.LastUsedAt = DateTime.UtcNow;
        return true;
    }

    public async Task<bool> UnregisterDeviceAsync(string deviceId)
    {
        await Task.CompletedTask;

        if (!_deviceRegistrations.TryGetValue(deviceId, out var registration))
            return false;

        registration.IsActive = false;
        return true;
    }

    public async Task<IReadOnlyList<DeviceRegistration>> GetRegisteredDevicesAsync(int userId)
    {
        await Task.CompletedTask;

        return _deviceRegistrations.Values
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderByDescending(d => d.LastUsedAt ?? d.RegisteredAt)
            .ToList();
    }

    #endregion

    #region Notification Preferences

    public async Task<NotificationPreferences> GetNotificationPreferencesAsync(int userId)
    {
        await Task.CompletedTask;

        return _notificationPreferences.GetValueOrDefault(userId) ?? new NotificationPreferences { UserId = userId };
    }

    public async Task<NotificationPreferences> UpdateNotificationPreferencesAsync(NotificationPreferences preferences)
    {
        await Task.CompletedTask;

        _notificationPreferences[preferences.UserId] = preferences;
        return preferences;
    }

    #endregion

    #region Cached Data

    public async Task<MobileCachedData> GetCachedDataAsync(int userId, int? branchId = null)
    {
        var dashboard = await GetDashboardAsync(userId, branchId);
        var alerts = await GetAlertsAsync(userId, branchId);
        var branches = await GetBranchesAsync(userId);

        return new MobileCachedData
        {
            UserId = userId,
            BranchId = branchId,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Dashboard = dashboard,
            Alerts = alerts,
            Branches = branches.ToList()
        };
    }

    public async Task<MobileSyncStatus> GetSyncStatusAsync(string deviceId)
    {
        await Task.CompletedTask;

        return new MobileSyncStatus
        {
            LastSyncAt = DateTime.UtcNow.AddMinutes(-5),
            IsSyncing = false,
            PendingChanges = 0,
            SyncError = null,
            IsOnline = true
        };
    }

    #endregion
}
