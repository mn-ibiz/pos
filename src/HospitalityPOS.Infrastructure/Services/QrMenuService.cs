// src/HospitalityPOS.Infrastructure/Services/QrMenuService.cs
// Implementation of QR Menu and Contactless Ordering service
// Story 44-1: QR Menu and Contactless Ordering

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.QrMenu;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for QR menu and contactless ordering functionality.
/// Handles QR code generation, menu management, and order processing.
/// </summary>
public class QrMenuService : IQrMenuService
{
    #region Fields

    private QrMenuConfiguration _configuration;
    private readonly Dictionary<int, TableQrCode> _tableQrCodes = new();
    private readonly Dictionary<int, QrMenuCategory> _categories = new();
    private readonly Dictionary<int, QrMenuProduct> _products = new();
    private readonly Dictionary<int, QrOrderData> _orders = new();
    private readonly Dictionary<string, int> _confirmationCodeToOrderId = new();
    private readonly List<QrScanRecord> _scanRecords = new();
    private int _nextOrderId = 1000;

    #endregion

    #region Properties

    /// <inheritdoc />
    public bool IsEnabled => _configuration.IsEnabled;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of QrMenuService.
    /// </summary>
    public QrMenuService()
    {
        _configuration = CreateDefaultConfiguration();
        LoadSampleData();
    }

    private QrMenuConfiguration CreateDefaultConfiguration()
    {
        return new QrMenuConfiguration
        {
            Id = 1,
            IsEnabled = true,
            BaseUrl = "https://menu.example.com",
            StoreName = "The Good Restaurant",
            WelcomeMessage = "Welcome! Browse our menu and order from your table.",
            PrimaryColor = "#1a1a2e",
            SecondaryColor = "#22C55E",
            CurrencySymbol = "KSh",
            CurrencyCode = "KES",
            ShowEstimatedWaitTime = true,
            DefaultWaitTimeMinutes = 15,
            RequireCustomerPhone = false,
            AllowedPaymentOptions = new List<QrPaymentOption>
            {
                QrPaymentOption.PayAtCounter,
                QrPaymentOption.MpesaQr
            }
        };
    }

    private void LoadSampleData()
    {
        // Sample categories
        var categories = new[]
        {
            new QrMenuCategory { Id = 1, Name = "Starters", Description = "Begin your meal", SortOrder = 1, IsAvailable = true },
            new QrMenuCategory { Id = 2, Name = "Main Course", Description = "Signature dishes", SortOrder = 2, IsAvailable = true },
            new QrMenuCategory { Id = 3, Name = "Beverages", Description = "Drinks and refreshments", SortOrder = 3, IsAvailable = true },
            new QrMenuCategory { Id = 4, Name = "Desserts", Description = "Sweet endings", SortOrder = 4, IsAvailable = true }
        };
        foreach (var cat in categories) _categories[cat.Id] = cat;

        // Sample products
        var products = new[]
        {
            // Starters
            new QrMenuProduct
            {
                Id = 1, CategoryId = 1, CategoryName = "Starters", Name = "Chicken Wings",
                Description = "Crispy fried wings with spicy sauce", Price = 650,
                FormattedPrice = "KSh 650", IsAvailable = true, InStock = true,
                IsFeatured = true, PrepTimeMinutes = 15, SpicyLevel = 2,
                Allergens = new List<string> { "Gluten" }
            },
            new QrMenuProduct
            {
                Id = 2, CategoryId = 1, CategoryName = "Starters", Name = "Samosas",
                Description = "Crispy pastry with spiced filling (4 pcs)", Price = 350,
                FormattedPrice = "KSh 350", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 10, DietaryTags = new List<string> { "Vegetarian" }
            },
            new QrMenuProduct
            {
                Id = 3, CategoryId = 1, CategoryName = "Starters", Name = "Soup of the Day",
                Description = "Ask your server for today's special", Price = 400,
                FormattedPrice = "KSh 400", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 5
            },
            // Main Course
            new QrMenuProduct
            {
                Id = 4, CategoryId = 2, CategoryName = "Main Course", Name = "Beef Burger",
                Description = "Juicy beef patty with fries and salad", Price = 850,
                FormattedPrice = "KSh 850", IsAvailable = true, InStock = true,
                IsFeatured = true, PrepTimeMinutes = 20,
                Modifiers = new List<QrMenuModifier>
                {
                    new()
                    {
                        GroupId = 1, GroupName = "Add-ons", IsRequired = false, MaxSelections = 3,
                        Options = new List<QrMenuModifierOption>
                        {
                            new() { Id = 1, Name = "Extra Cheese", PriceAdjustment = 100 },
                            new() { Id = 2, Name = "Bacon", PriceAdjustment = 150 },
                            new() { Id = 3, Name = "Egg", PriceAdjustment = 80 }
                        }
                    }
                }
            },
            new QrMenuProduct
            {
                Id = 5, CategoryId = 2, CategoryName = "Main Course", Name = "Grilled Chicken",
                Description = "Marinated chicken breast with vegetables", Price = 950,
                FormattedPrice = "KSh 950", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 25, DietaryTags = new List<string> { "High Protein" }
            },
            new QrMenuProduct
            {
                Id = 6, CategoryId = 2, CategoryName = "Main Course", Name = "Fish & Chips",
                Description = "Beer-battered fish with crispy fries", Price = 1100,
                FormattedPrice = "KSh 1,100", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 20, Allergens = new List<string> { "Gluten", "Fish" }
            },
            new QrMenuProduct
            {
                Id = 7, CategoryId = 2, CategoryName = "Main Course", Name = "Vegetable Curry",
                Description = "Mixed vegetables in aromatic curry sauce", Price = 700,
                FormattedPrice = "KSh 700", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 15, SpicyLevel = 2,
                DietaryTags = new List<string> { "Vegetarian", "Vegan" }
            },
            // Beverages
            new QrMenuProduct
            {
                Id = 8, CategoryId = 3, CategoryName = "Beverages", Name = "Fresh Juice",
                Description = "Choice of mango, passion, or orange", Price = 250,
                FormattedPrice = "KSh 250", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 5
            },
            new QrMenuProduct
            {
                Id = 9, CategoryId = 3, CategoryName = "Beverages", Name = "Soda",
                Description = "Coca-Cola, Fanta, Sprite", Price = 150,
                FormattedPrice = "KSh 150", IsAvailable = true, InStock = true
            },
            new QrMenuProduct
            {
                Id = 10, CategoryId = 3, CategoryName = "Beverages", Name = "Coffee",
                Description = "Freshly brewed Kenyan coffee", Price = 200,
                FormattedPrice = "KSh 200", IsAvailable = true, InStock = true,
                PrepTimeMinutes = 3
            },
            // Desserts
            new QrMenuProduct
            {
                Id = 11, CategoryId = 4, CategoryName = "Desserts", Name = "Chocolate Cake",
                Description = "Rich chocolate layer cake", Price = 450,
                FormattedPrice = "KSh 450", IsAvailable = true, InStock = true,
                IsFeatured = true, Allergens = new List<string> { "Gluten", "Dairy" }
            },
            new QrMenuProduct
            {
                Id = 12, CategoryId = 4, CategoryName = "Desserts", Name = "Ice Cream",
                Description = "3 scoops, choice of flavors", Price = 350,
                FormattedPrice = "KSh 350", IsAvailable = true, InStock = true,
                Allergens = new List<string> { "Dairy" }
            }
        };
        foreach (var prod in products) _products[prod.Id] = prod;

        // Update category item counts
        foreach (var cat in _categories.Values)
        {
            cat.ItemCount = _products.Values.Count(p => p.CategoryId == cat.Id);
        }
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public Task<QrMenuConfiguration> GetConfigurationAsync()
    {
        return Task.FromResult(_configuration);
    }

    /// <inheritdoc />
    public Task<QrMenuConfiguration> SaveConfigurationAsync(QrMenuConfiguration configuration)
    {
        configuration.UpdatedAt = DateTime.UtcNow;
        _configuration = configuration;
        return Task.FromResult(_configuration);
    }

    #endregion

    #region QR Code Generation

    /// <inheritdoc />
    public Task<TableQrCode> GenerateTableQrCodeAsync(int tableId, string tableName, string? location = null)
    {
        var menuUrl = BuildMenuUrl(tableId, location);
        var qrCodeBase64 = GenerateQrCodeImage(menuUrl);

        var qrCode = new TableQrCode
        {
            TableId = tableId,
            TableName = tableName,
            Location = location,
            MenuUrl = menuUrl,
            QrCodeBase64 = qrCodeBase64,
            GeneratedAt = DateTime.UtcNow
        };

        _tableQrCodes[tableId] = qrCode;

        return Task.FromResult(qrCode);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableQrCode>> GenerateAllTableQrCodesAsync(
        IEnumerable<(int Id, string Name, string? Location)> tables)
    {
        var qrCodes = new List<TableQrCode>();

        foreach (var (id, name, location) in tables)
        {
            var qrCode = await GenerateTableQrCodeAsync(id, name, location);
            qrCodes.Add(qrCode);
        }

        return qrCodes;
    }

    /// <inheritdoc />
    public Task<TableQrCode?> GetTableQrCodeAsync(int tableId)
    {
        _tableQrCodes.TryGetValue(tableId, out var qrCode);
        return Task.FromResult(qrCode);
    }

    /// <inheritdoc />
    public Task RecordQrScanAsync(int tableId, string? sessionId = null)
    {
        _scanRecords.Add(new QrScanRecord
        {
            TableId = tableId,
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            ScanTime = DateTime.UtcNow
        });

        if (_tableQrCodes.TryGetValue(tableId, out var qrCode))
        {
            qrCode.ScanCount++;
        }

        QrCodeScanned?.Invoke(this, (tableId, sessionId));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<QrCodePrintTemplate?> GetPrintTemplateAsync(int tableId)
    {
        var qrCode = await GetTableQrCodeAsync(tableId);
        if (qrCode == null) return null;

        return new QrCodePrintTemplate
        {
            TableName = qrCode.TableName,
            QrCodeBase64 = qrCode.QrCodeBase64,
            StoreName = _configuration.StoreName,
            InstructionText = "Scan to view menu and order",
            LogoUrl = _configuration.LogoUrl
        };
    }

    private string BuildMenuUrl(int tableId, string? location)
    {
        var url = $"{_configuration.BaseUrl}/menu?table={tableId}";
        if (!string.IsNullOrEmpty(location))
        {
            url += $"&location={Uri.EscapeDataString(location)}";
        }
        return url;
    }

    private string GenerateQrCodeImage(string content)
    {
        // In production, use QRCoder NuGet package:
        // var qrGenerator = new QRCodeGenerator();
        // var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        // var qrCode = new PngByteQRCode(qrCodeData);
        // return Convert.ToBase64String(qrCode.GetGraphic(20));

        // For now, return a placeholder that indicates what would be generated
        var placeholder = $"QR_CODE_FOR:{content}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(placeholder));
    }

    #endregion

    #region Menu Management

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuCategory>> GetCategoriesAsync()
    {
        var available = _categories.Values
            .Where(c => c.IsAvailable && IsWithinAvailabilityWindow(c.AvailableFrom, c.AvailableUntil))
            .OrderBy(c => c.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuCategory>>(available);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuProduct>> GetProductsByCategoryAsync(int categoryId)
    {
        var products = _products.Values
            .Where(p => p.CategoryId == categoryId && p.IsAvailable)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuProduct>>(products);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuProduct>> GetAllProductsAsync()
    {
        var products = _products.Values
            .Where(p => p.IsAvailable)
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuProduct>>(products);
    }

    /// <inheritdoc />
    public Task<QrMenuProduct?> GetProductAsync(int productId)
    {
        _products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuProduct>> GetFeaturedProductsAsync(int limit = 10)
    {
        var featured = _products.Values
            .Where(p => p.IsAvailable && p.InStock && p.IsFeatured)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuProduct>>(featured);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuProduct>> SearchProductsAsync(string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant();
        var matches = _products.Values
            .Where(p => p.IsAvailable &&
                (p.Name.ToLowerInvariant().Contains(term) ||
                 (p.Description?.ToLowerInvariant().Contains(term) ?? false)))
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuProduct>>(matches);
    }

    /// <inheritdoc />
    public Task SetProductAvailabilityAsync(int productId, bool isAvailable)
    {
        if (_products.TryGetValue(productId, out var product))
        {
            product.IsAvailable = isAvailable;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetProductStockStatusAsync(int productId, bool inStock)
    {
        if (_products.TryGetValue(productId, out var product))
        {
            product.InStock = inStock;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> SyncProductsFromCatalogAsync()
    {
        // In production: Fetch from main product repository and sync
        // For now, return count of existing products
        return Task.FromResult(_products.Count);
    }

    private bool IsWithinAvailabilityWindow(TimeOnly? from, TimeOnly? until)
    {
        if (!from.HasValue && !until.HasValue) return true;

        var now = TimeOnly.FromDateTime(DateTime.Now);

        if (from.HasValue && now < from.Value) return false;
        if (until.HasValue && now > until.Value) return false;

        return true;
    }

    #endregion

    #region Order Management

    /// <inheritdoc />
    public Task<(bool IsValid, List<string> Errors)> ValidateOrderAsync(QrMenuOrderRequest request)
    {
        var errors = new List<string>();

        if (request.TableId <= 0)
        {
            errors.Add("Invalid table ID");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            errors.Add("Order must contain at least one item");
        }
        else
        {
            foreach (var item in request.Items)
            {
                if (!_products.TryGetValue(item.ProductId, out var product))
                {
                    errors.Add($"Product not found: {item.ProductId}");
                }
                else if (!product.IsAvailable)
                {
                    errors.Add($"Product not available: {product.Name}");
                }
                else if (!product.InStock)
                {
                    errors.Add($"Product out of stock: {product.Name}");
                }

                if (item.Quantity <= 0)
                {
                    errors.Add($"Invalid quantity for product: {item.ProductId}");
                }
            }
        }

        if (_configuration.RequireCustomerPhone && string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            errors.Add("Customer phone number is required");
        }

        if (_configuration.MinimumOrderAmount > 0)
        {
            var total = CalculateOrderTotal(request.Items ?? []);
            if (total < _configuration.MinimumOrderAmount)
            {
                errors.Add($"Minimum order amount is {_configuration.CurrencySymbol} {_configuration.MinimumOrderAmount:N0}");
            }
        }

        if (!_configuration.AllowedPaymentOptions.Contains(request.PaymentOption))
        {
            errors.Add($"Payment option {request.PaymentOption} is not available");
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    /// <inheritdoc />
    public async Task<QrMenuOrderResponse> SubmitOrderAsync(QrMenuOrderRequest request)
    {
        var (isValid, errors) = await ValidateOrderAsync(request);

        if (!isValid)
        {
            return QrMenuOrderResponse.Failed(string.Join("; ", errors));
        }

        var orderId = _nextOrderId++;
        var confirmationCode = GenerateConfirmationCode();
        var total = CalculateOrderTotal(request.Items);

        // Populate product names in items
        foreach (var item in request.Items)
        {
            if (_products.TryGetValue(item.ProductId, out var product))
            {
                item.ProductName = product.Name;
                item.UnitPrice = product.Price;
            }
        }

        var orderData = new QrOrderData
        {
            OrderId = orderId,
            ConfirmationCode = confirmationCode,
            Request = request,
            Status = QrOrderStatus.Received,
            Total = total,
            OrderTime = DateTime.UtcNow,
            EstimatedWaitMinutes = _configuration.DefaultWaitTimeMinutes
        };

        _orders[orderId] = orderData;
        _confirmationCodeToOrderId[confirmationCode] = orderId;

        // Raise event for POS notification
        var notification = new QrOrderNotification
        {
            OrderId = orderId,
            ConfirmationCode = confirmationCode,
            TableId = request.TableId,
            TableName = request.TableName,
            CustomerName = request.CustomerName,
            ItemCount = request.Items.Count,
            OrderTotal = total,
            OrderTime = orderData.OrderTime,
            HasSpecialInstructions = !string.IsNullOrEmpty(request.OrderNotes) ||
                request.Items.Any(i => !string.IsNullOrEmpty(i.Notes))
        };

        OrderReceived?.Invoke(this, notification);

        return QrMenuOrderResponse.Successful(
            orderId,
            confirmationCode,
            total,
            _configuration.DefaultWaitTimeMinutes
        );
    }

    /// <inheritdoc />
    public Task<QrOrderStatusUpdate?> GetOrderStatusAsync(int orderId)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult<QrOrderStatusUpdate?>(null);
        }

        return Task.FromResult<QrOrderStatusUpdate?>(new QrOrderStatusUpdate
        {
            OrderId = orderId,
            ConfirmationCode = order.ConfirmationCode,
            Status = order.Status,
            StatusMessage = GetStatusMessage(order.Status),
            EstimatedWaitMinutes = order.EstimatedWaitMinutes,
            UpdatedAt = order.LastUpdated ?? order.OrderTime
        });
    }

    /// <inheritdoc />
    public Task<QrOrderStatusUpdate?> GetOrderStatusByCodeAsync(string confirmationCode)
    {
        if (!_confirmationCodeToOrderId.TryGetValue(confirmationCode, out var orderId))
        {
            return Task.FromResult<QrOrderStatusUpdate?>(null);
        }

        return GetOrderStatusAsync(orderId);
    }

    /// <inheritdoc />
    public Task UpdateOrderStatusAsync(int orderId, QrOrderStatus status, int? estimatedWaitMinutes = null)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return Task.CompletedTask;
        }

        order.Status = status;
        order.LastUpdated = DateTime.UtcNow;

        if (estimatedWaitMinutes.HasValue)
        {
            order.EstimatedWaitMinutes = estimatedWaitMinutes.Value;
        }

        var statusUpdate = new QrOrderStatusUpdate
        {
            OrderId = orderId,
            ConfirmationCode = order.ConfirmationCode,
            Status = status,
            StatusMessage = GetStatusMessage(status),
            EstimatedWaitMinutes = order.EstimatedWaitMinutes,
            UpdatedAt = order.LastUpdated.Value
        };

        OrderStatusChanged?.Invoke(this, statusUpdate);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> CancelOrderAsync(int orderId, string? reason = null)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult(false);
        }

        if (order.Status == QrOrderStatus.Served || order.Status == QrOrderStatus.Paid)
        {
            return Task.FromResult(false);
        }

        order.Status = QrOrderStatus.Cancelled;
        order.CancellationReason = reason;
        order.LastUpdated = DateTime.UtcNow;

        OrderStatusChanged?.Invoke(this, new QrOrderStatusUpdate
        {
            OrderId = orderId,
            ConfirmationCode = order.ConfirmationCode,
            Status = QrOrderStatus.Cancelled,
            StatusMessage = "Order has been cancelled",
            UpdatedAt = order.LastUpdated.Value
        });

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrOrderNotification>> GetPendingOrdersAsync()
    {
        var pending = _orders.Values
            .Where(o => o.Status != QrOrderStatus.Served &&
                        o.Status != QrOrderStatus.Paid &&
                        o.Status != QrOrderStatus.Cancelled)
            .OrderBy(o => o.OrderTime)
            .Select(o => new QrOrderNotification
            {
                OrderId = o.OrderId,
                ConfirmationCode = o.ConfirmationCode,
                TableId = o.Request.TableId,
                TableName = o.Request.TableName,
                CustomerName = o.Request.CustomerName,
                ItemCount = o.Request.Items.Count,
                OrderTotal = o.Total,
                OrderTime = o.OrderTime,
                HasSpecialInstructions = !string.IsNullOrEmpty(o.Request.OrderNotes)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<QrOrderNotification>>(pending);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrOrderNotification>> GetRecentOrdersAsync(int limit = 20)
    {
        var recent = _orders.Values
            .OrderByDescending(o => o.OrderTime)
            .Take(limit)
            .Select(o => new QrOrderNotification
            {
                OrderId = o.OrderId,
                ConfirmationCode = o.ConfirmationCode,
                TableId = o.Request.TableId,
                TableName = o.Request.TableName,
                CustomerName = o.Request.CustomerName,
                ItemCount = o.Request.Items.Count,
                OrderTotal = o.Total,
                OrderTime = o.OrderTime
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<QrOrderNotification>>(recent);
    }

    private decimal CalculateOrderTotal(List<QrCartItem> items)
    {
        decimal total = 0;

        foreach (var item in items)
        {
            if (_products.TryGetValue(item.ProductId, out var product))
            {
                var lineTotal = (product.Price + item.ModifierTotal) * item.Quantity;
                total += lineTotal;
            }
        }

        return total;
    }

    private string GenerateConfirmationCode()
    {
        // Generate a 6-character alphanumeric code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GetStatusMessage(QrOrderStatus status)
    {
        return status switch
        {
            QrOrderStatus.Pending => "Order is pending confirmation",
            QrOrderStatus.Received => "Order received by kitchen",
            QrOrderStatus.Preparing => "Your order is being prepared",
            QrOrderStatus.Ready => "Your order is ready!",
            QrOrderStatus.Served => "Order has been served",
            QrOrderStatus.Paid => "Order complete. Thank you!",
            QrOrderStatus.Cancelled => "Order has been cancelled",
            _ => "Unknown status"
        };
    }

    #endregion

    #region Analytics

    /// <inheritdoc />
    public Task<QrMenuAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var ordersInRange = _orders.Values
            .Where(o => o.OrderTime >= startDate && o.OrderTime <= endDate)
            .ToList();

        var scansInRange = _scanRecords
            .Where(s => s.ScanTime >= startDate && s.ScanTime <= endDate)
            .ToList();

        var uniqueSessions = scansInRange.Select(s => s.SessionId).Distinct().Count();

        var totalOrders = ordersInRange.Count;
        var totalRevenue = ordersInRange.Sum(o => o.Total);

        var analytics = new QrMenuAnalytics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalScans = scansInRange.Count,
            UniqueVisitors = uniqueSessions,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            ConversionRate = scansInRange.Count > 0
                ? (decimal)totalOrders / scansInRange.Count * 100 : 0,
            PopularItems = GetPopularItemsFromOrders(ordersInRange),
            OrdersByHour = GetOrdersByHour(ordersInRange),
            OrdersByDayOfWeek = GetOrdersByDayOfWeek(ordersInRange),
            AverageWaitTime = CalculateAverageWaitTime(ordersInRange)
        };

        return Task.FromResult(analytics);
    }

    /// <inheritdoc />
    public Task<QrMenuAnalytics> GetTodayAnalyticsAsync()
    {
        var today = DateTime.Today;
        return GetAnalyticsAsync(today, today.AddDays(1).AddTicks(-1));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrMenuPopularItem>> GetPopularItemsAsync(
        DateTime startDate, DateTime endDate, int limit = 10)
    {
        var ordersInRange = _orders.Values
            .Where(o => o.OrderTime >= startDate && o.OrderTime <= endDate)
            .ToList();

        var items = GetPopularItemsFromOrders(ordersInRange)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrMenuPopularItem>>(items);
    }

    private List<QrMenuPopularItem> GetPopularItemsFromOrders(List<QrOrderData> orders)
    {
        var itemCounts = new Dictionary<int, (int Count, decimal Revenue, string Name)>();

        foreach (var order in orders)
        {
            foreach (var item in order.Request.Items)
            {
                if (!itemCounts.ContainsKey(item.ProductId))
                {
                    itemCounts[item.ProductId] = (0, 0, item.ProductName);
                }

                var (count, revenue, name) = itemCounts[item.ProductId];
                itemCounts[item.ProductId] = (count + item.Quantity, revenue + item.LineTotal, name);
            }
        }

        return itemCounts
            .OrderByDescending(kv => kv.Value.Count)
            .Select(kv => new QrMenuPopularItem
            {
                ProductId = kv.Key,
                ProductName = kv.Value.Name,
                OrderCount = kv.Value.Count,
                Revenue = kv.Value.Revenue
            })
            .ToList();
    }

    private Dictionary<int, int> GetOrdersByHour(List<QrOrderData> orders)
    {
        return orders
            .GroupBy(o => o.OrderTime.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<DayOfWeek, int> GetOrdersByDayOfWeek(List<QrOrderData> orders)
    {
        return orders
            .GroupBy(o => o.OrderTime.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private decimal CalculateAverageWaitTime(List<QrOrderData> orders)
    {
        var completedOrders = orders
            .Where(o => o.Status == QrOrderStatus.Served || o.Status == QrOrderStatus.Paid)
            .ToList();

        if (completedOrders.Count == 0) return 0;

        // Use estimated wait time as proxy since we don't track actual completion
        return (decimal)completedOrders.Average(o => o.EstimatedWaitMinutes);
    }

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<QrOrderNotification>? OrderReceived;

    /// <inheritdoc />
    public event EventHandler<QrOrderStatusUpdate>? OrderStatusChanged;

    /// <inheritdoc />
    public event EventHandler<(int TableId, string? SessionId)>? QrCodeScanned;

    #endregion

    #region Internal Classes

    private class QrOrderData
    {
        public int OrderId { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public QrMenuOrderRequest Request { get; set; } = new();
        public QrOrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderTime { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int EstimatedWaitMinutes { get; set; }
        public string? CancellationReason { get; set; }
    }

    private class QrScanRecord
    {
        public int TableId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
    }

    #endregion

    #region Test/Simulation Methods

    /// <summary>
    /// Simulates a QR code scan for testing.
    /// </summary>
    public async Task SimulateScanAsync(int tableId)
    {
        await RecordQrScanAsync(tableId, Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Gets all orders for testing.
    /// </summary>
    public IReadOnlyList<(int OrderId, QrOrderStatus Status, decimal Total)> GetAllOrders()
    {
        return _orders.Values
            .Select(o => (o.OrderId, o.Status, o.Total))
            .ToList();
    }

    /// <summary>
    /// Gets order details for testing.
    /// </summary>
    public QrMenuOrderRequest? GetOrderDetails(int orderId)
    {
        return _orders.TryGetValue(orderId, out var order) ? order.Request : null;
    }

    #endregion
}
