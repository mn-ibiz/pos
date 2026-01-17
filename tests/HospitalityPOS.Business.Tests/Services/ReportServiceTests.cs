using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ReportService class.
/// Tests cover daily sales summary, product/category/cashier/payment method breakdowns, and hourly analysis.
/// </summary>
public class ReportServiceTests : IAsyncLifetime, IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly ReportService _reportService;
    private const int TestUserId = 1;
    private const int TestWorkPeriodId = 1;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger>();

        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(TestUserId);
        _sessionServiceMock.Setup(s => s.CurrentUserDisplayName).Returns("Test User");

        _reportService = new ReportService(
            _context,
            _sessionServiceMock.Object,
            _loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        await SeedTestData();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = TestUserId,
            Username = "testuser",
            PasswordHash = "hash",
            FullName = "Test User",
            IsActive = true
        };
        _context.Users.Add(user);

        var user2 = new User
        {
            Id = 2,
            Username = "cashier2",
            PasswordHash = "hash",
            FullName = "Cashier Two",
            IsActive = true
        };
        _context.Users.Add(user2);

        // Create test work period
        var workPeriod = new WorkPeriod
        {
            Id = TestWorkPeriodId,
            OpenedAt = DateTime.UtcNow.AddHours(-8),
            OpenedByUserId = TestUserId,
            Status = WorkPeriodStatus.Open,
            OpeningFloat = 5000m,
            IsActive = true
        };
        _context.WorkPeriods.Add(workPeriod);

        // Create categories
        var category1 = new Category { Id = 1, Name = "Beverages", IsActive = true };
        var category2 = new Category { Id = 2, Name = "Food", IsActive = true };
        _context.Categories.AddRange(category1, category2);

        // Create products
        var product1 = new Product
        {
            Id = 1,
            Code = "BEV-001",
            Name = "Coca Cola",
            CategoryId = 1,
            SellingPrice = 100m,
            CostPrice = 50m,
            IsActive = true
        };
        var product2 = new Product
        {
            Id = 2,
            Code = "FOOD-001",
            Name = "Grilled Chicken",
            CategoryId = 2,
            SellingPrice = 500m,
            CostPrice = 200m,
            IsActive = true
        };
        var product3 = new Product
        {
            Id = 3,
            Code = "BEV-002",
            Name = "Tusker Lager",
            CategoryId = 1,
            SellingPrice = 250m,
            CostPrice = 150m,
            MinStockLevel = 20,
            MaxStockLevel = 100,
            TrackInventory = true,
            IsActive = true
        };
        var product4 = new Product
        {
            Id = 4,
            Code = "FOOD-002",
            Name = "Dead Stock Item",
            CategoryId = 2,
            SellingPrice = 300m,
            CostPrice = 100m,
            MinStockLevel = 5,
            MaxStockLevel = 50,
            TrackInventory = true,
            IsActive = true
        };
        _context.Products.AddRange(product1, product2, product3, product4);

        // Add inventory records
        var inventory1 = new Inventory { ProductId = 1, CurrentStock = 100, LastUpdated = DateTime.UtcNow };
        var inventory2 = new Inventory { ProductId = 2, CurrentStock = 50, LastUpdated = DateTime.UtcNow };
        var inventory3 = new Inventory { ProductId = 3, CurrentStock = 5, LastUpdated = DateTime.UtcNow }; // Low stock
        var inventory4 = new Inventory { ProductId = 4, CurrentStock = 30, LastUpdated = DateTime.UtcNow }; // Dead stock (no movement)
        _context.Inventories.AddRange(inventory1, inventory2, inventory3, inventory4);

        // Create payment methods
        var cashMethod = new PaymentMethod { Id = 1, Name = "Cash", PaymentMethodType = PaymentMethodType.Cash, IsActive = true };
        var mpesaMethod = new PaymentMethod { Id = 2, Name = "M-Pesa", PaymentMethodType = PaymentMethodType.MPesa, IsActive = true };
        _context.PaymentMethods.AddRange(cashMethod, mpesaMethod);

        await _context.SaveChangesAsync();

        // Create receipts with items - spread across different hours
        await CreateTestReceiptAsync(
            receiptNumber: "R-001",
            ownerId: TestUserId,
            settledAt: DateTime.UtcNow.Date.AddHours(9), // 9 AM
            items: [(1, 2, 100m), (2, 1, 500m)], // 2x Coca Cola, 1x Grilled Chicken
            paymentMethodId: 1,
            paymentAmount: 700m);

        await CreateTestReceiptAsync(
            receiptNumber: "R-002",
            ownerId: TestUserId,
            settledAt: DateTime.UtcNow.Date.AddHours(10), // 10 AM
            items: [(3, 3, 250m)], // 3x Tusker
            paymentMethodId: 2,
            paymentAmount: 750m);

        await CreateTestReceiptAsync(
            receiptNumber: "R-003",
            ownerId: 2, // Different cashier
            settledAt: DateTime.UtcNow.Date.AddHours(14), // 2 PM
            items: [(1, 5, 100m), (2, 2, 500m)], // 5x Coca Cola, 2x Grilled Chicken
            paymentMethodId: 1,
            paymentAmount: 1500m);

        // Create a voided receipt
        await CreateVoidedReceiptAsync(
            receiptNumber: "R-VOID-001",
            ownerId: TestUserId,
            amount: 300m,
            voidedAt: DateTime.UtcNow.Date.AddHours(11));

        // Create stock movements for products 1, 2, 3 (not product 4 - dead stock)
        var movement1 = new StockMovement
        {
            ProductId = 1,
            MovementType = MovementType.Sale,
            Quantity = 5,
            PreviousStock = 105,
            NewStock = 100,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };
        var movement2 = new StockMovement
        {
            ProductId = 2,
            MovementType = MovementType.PurchaseReceive,
            Quantity = 20,
            PreviousStock = 30,
            NewStock = 50,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IsActive = true
        };
        var movement3 = new StockMovement
        {
            ProductId = 3,
            MovementType = MovementType.Sale,
            Quantity = 10,
            PreviousStock = 15,
            NewStock = 5,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            IsActive = true
        };
        // Product 4 has no movements - will be dead stock after threshold
        _context.StockMovements.AddRange(movement1, movement2, movement3);
        await _context.SaveChangesAsync();

        // Create audit log entries for audit trail report tests
        var auditLogs = new List<AuditLog>
        {
            // Login/Logout events
            new AuditLog
            {
                Id = 1,
                UserId = TestUserId,
                Action = "Login",
                EntityType = "Session",
                EntityId = "session-123",
                CreatedAt = DateTime.UtcNow.Date.AddHours(8),
                MachineName = "POS-TERMINAL-1",
                IpAddress = "192.168.1.100"
            },
            new AuditLog
            {
                Id = 2,
                UserId = TestUserId,
                Action = "Logout",
                EntityType = "Session",
                EntityId = "session-123",
                CreatedAt = DateTime.UtcNow.Date.AddHours(17),
                MachineName = "POS-TERMINAL-1",
                IpAddress = "192.168.1.100"
            },
            // User 2 login
            new AuditLog
            {
                Id = 3,
                UserId = 2,
                Action = "Login",
                EntityType = "Session",
                EntityId = "session-456",
                CreatedAt = DateTime.UtcNow.Date.AddHours(9),
                MachineName = "POS-TERMINAL-2",
                IpAddress = "192.168.1.101"
            },
            // Product update (price change)
            new AuditLog
            {
                Id = 4,
                UserId = TestUserId,
                Action = "ProductUpdated",
                EntityType = "Product",
                EntityId = "1",
                OldValues = "{\"SellingPrice\":80.00,\"Name\":\"Coca Cola\"}",
                NewValues = "{\"SellingPrice\":100.00,\"Name\":\"Coca Cola\"}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(10),
                MachineName = "POS-TERMINAL-1"
            },
            // Receipt void
            new AuditLog
            {
                Id = 5,
                UserId = TestUserId,
                Action = "ReceiptVoided",
                EntityType = "Receipt",
                EntityId = "R-VOID-001",
                OldValues = "{\"Status\":\"Open\",\"TotalAmount\":300.00}",
                NewValues = "{\"Status\":\"Voided\",\"VoidReason\":\"Customer cancelled\",\"ReceiptNumber\":\"R-VOID-001\"}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(11),
                MachineName = "POS-TERMINAL-1"
            },
            // Permission override
            new AuditLog
            {
                Id = 6,
                UserId = TestUserId,
                Action = "PermissionOverride",
                EntityType = "Authorization",
                EntityId = "override-789",
                OldValues = "{\"Permission\":\"Receipts.Void\",\"RequestedByUserId\":2}",
                NewValues = "{\"Granted\":true,\"AuthorizedByUserId\":1}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(11).AddMinutes(-5),
                MachineName = "POS-TERMINAL-2"
            },
            // Discount applied
            new AuditLog
            {
                Id = 7,
                UserId = 2,
                Action = "DiscountApplied",
                EntityType = "Receipt",
                EntityId = "R-003",
                OldValues = "{\"DiscountAmount\":0}",
                NewValues = "{\"DiscountAmount\":150.00,\"DiscountReason\":\"Loyal customer\",\"ReceiptNumber\":\"R-003\"}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(14).AddMinutes(5),
                MachineName = "POS-TERMINAL-2"
            },
            // Work period opened
            new AuditLog
            {
                Id = 8,
                UserId = TestUserId,
                Action = "WorkPeriodOpened",
                EntityType = "WorkPeriod",
                EntityId = "1",
                NewValues = "{\"OpeningFloat\":5000.00}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(7).AddMinutes(55),
                MachineName = "POS-TERMINAL-1"
            },
            // Receipt settled
            new AuditLog
            {
                Id = 9,
                UserId = TestUserId,
                Action = "ReceiptSettled",
                EntityType = "Receipt",
                EntityId = "R-001",
                NewValues = "{\"TotalAmount\":700.00,\"PaymentMethod\":\"Cash\",\"ReceiptNumber\":\"R-001\"}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(9).AddMinutes(10),
                MachineName = "POS-TERMINAL-1"
            },
            // Stock adjustment
            new AuditLog
            {
                Id = 10,
                UserId = TestUserId,
                Action = "StockAdjustment",
                EntityType = "Inventory",
                EntityId = "3",
                OldValues = "{\"CurrentStock\":15}",
                NewValues = "{\"CurrentStock\":5,\"AdjustmentReason\":\"Damaged goods\"}",
                CreatedAt = DateTime.UtcNow.Date.AddHours(12),
                MachineName = "POS-TERMINAL-1"
            }
        };
        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
    }

    private async Task CreateTestReceiptAsync(
        string receiptNumber,
        int ownerId,
        DateTime settledAt,
        (int productId, decimal quantity, decimal unitPrice)[] items,
        int paymentMethodId,
        decimal paymentAmount)
    {
        decimal subtotal = items.Sum(i => i.quantity * i.unitPrice);
        decimal taxAmount = subtotal * 0.16m; // 16% VAT
        decimal totalAmount = subtotal + taxAmount;

        var receipt = new Receipt
        {
            ReceiptNumber = receiptNumber,
            OwnerId = ownerId,
            WorkPeriodId = TestWorkPeriodId,
            Status = ReceiptStatus.Settled,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            DiscountAmount = 0,
            TotalAmount = totalAmount,
            PaidAmount = paymentAmount,
            SettledAt = settledAt,
            SettledByUserId = ownerId,
            IsActive = true
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var (productId, quantity, unitPrice) in items)
        {
            var product = await _context.Products.FindAsync(productId);
            var receiptItem = new ReceiptItem
            {
                ReceiptId = receipt.Id,
                ProductId = productId,
                ProductName = product?.Name ?? "Unknown",
                Quantity = quantity,
                UnitPrice = unitPrice,
                TaxAmount = quantity * unitPrice * 0.16m,
                TotalAmount = quantity * unitPrice * 1.16m,
                DiscountAmount = 0,
                IsActive = true
            };
            _context.ReceiptItems.Add(receiptItem);
        }

        var payment = new Payment
        {
            ReceiptId = receipt.Id,
            PaymentMethodId = paymentMethodId,
            Amount = paymentAmount,
            TenderedAmount = paymentAmount,
            ProcessedByUserId = ownerId,
            IsActive = true
        };
        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();
    }

    private async Task CreateVoidedReceiptAsync(
        string receiptNumber,
        int ownerId,
        decimal amount,
        DateTime voidedAt)
    {
        var receipt = new Receipt
        {
            ReceiptNumber = receiptNumber,
            OwnerId = ownerId,
            WorkPeriodId = TestWorkPeriodId,
            Status = ReceiptStatus.Voided,
            Subtotal = amount,
            TaxAmount = amount * 0.16m,
            TotalAmount = amount * 1.16m,
            VoidedAt = voidedAt,
            VoidedByUserId = ownerId,
            VoidReason = "Customer cancelled",
            IsActive = true
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReportService(null!, _sessionServiceMock.Object, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullSessionService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReportService(_context, null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReportService(_context, _sessionServiceMock.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GenerateDailySummaryAsync Tests

    [Fact]
    public async Task GenerateDailySummaryAsync_ShouldCalculateCorrectTotals()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDailySummaryAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TransactionCount.Should().Be(3); // 3 settled receipts
        result.GrossSales.Should().BeGreaterThan(0);
        result.NetSales.Should().BeGreaterThan(0);
        result.TaxCollected.Should().BeGreaterThan(0);
        result.VoidedCount.Should().Be(1); // 1 voided receipt
    }

    [Fact]
    public async Task GenerateDailySummaryAsync_WithNoTransactions_ShouldReturnZeros()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = DateTime.UtcNow.Date.AddDays(-29),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDailySummaryAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TransactionCount.Should().Be(0);
        result.GrossSales.Should().Be(0);
        result.NetSales.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDailySummaryAsync_ShouldCalculateAverageTransaction()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDailySummaryAsync(parameters);

        // Assert
        result.AverageTransaction.Should().BeGreaterThan(0);
        if (result.TransactionCount > 0)
        {
            result.AverageTransaction.Should().Be(result.TotalRevenue / result.TransactionCount);
        }
    }

    #endregion

    #region GenerateProductSalesAsync Tests

    [Fact]
    public async Task GenerateProductSalesAsync_ShouldReturnProductBreakdown()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateProductSalesAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().AllSatisfy(p =>
        {
            p.ProductName.Should().NotBeNullOrEmpty();
            p.QuantitySold.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task GenerateProductSalesAsync_ShouldOrderByNetSalesDescending()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateProductSalesAsync(parameters);

        // Assert
        result.Should().BeInDescendingOrder(p => p.NetSales);
    }

    [Fact]
    public async Task GenerateProductSalesAsync_ShouldCalculatePercentages()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateProductSalesAsync(parameters);

        // Assert
        if (result.Count > 0)
        {
            var totalPercentage = result.Sum(p => p.Percentage);
            totalPercentage.Should().BeApproximately(100m, 0.1m);
        }
    }

    #endregion

    #region GenerateCategorySalesAsync Tests

    [Fact]
    public async Task GenerateCategorySalesAsync_ShouldReturnCategoryBreakdown()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateCategorySalesAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(c => c.CategoryName == "Beverages");
        result.Should().Contain(c => c.CategoryName == "Food");
    }

    [Fact]
    public async Task GenerateCategorySalesAsync_ShouldCalculateItemCounts()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateCategorySalesAsync(parameters);

        // Assert
        result.Should().AllSatisfy(c =>
        {
            c.ItemCount.Should().BeGreaterThan(0);
            c.QuantitySold.Should().BeGreaterThan(0);
        });
    }

    #endregion

    #region GenerateCashierSalesAsync Tests

    [Fact]
    public async Task GenerateCashierSalesAsync_ShouldReturnCashierBreakdown()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateCashierSalesAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(c => c.CashierName == "Test User");
        result.Should().Contain(c => c.CashierName == "Cashier Two");
    }

    [Fact]
    public async Task GenerateCashierSalesAsync_ShouldCalculateAverageTransaction()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateCashierSalesAsync(parameters);

        // Assert
        result.Should().AllSatisfy(c =>
        {
            c.TransactionCount.Should().BeGreaterThan(0);
            c.AverageTransaction.Should().Be(c.TotalSales / c.TransactionCount);
        });
    }

    [Fact]
    public async Task GenerateCashierSalesAsync_ShouldIncludeVoidCounts()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateCashierSalesAsync(parameters);

        // Assert
        var testUserReport = result.FirstOrDefault(c => c.CashierName == "Test User");
        testUserReport.Should().NotBeNull();
        testUserReport!.VoidCount.Should().Be(1); // Test user has 1 void
    }

    #endregion

    #region GeneratePaymentMethodSalesAsync Tests

    [Fact]
    public async Task GeneratePaymentMethodSalesAsync_ShouldReturnPaymentMethodBreakdown()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GeneratePaymentMethodSalesAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(pm => pm.PaymentMethodName == "Cash");
        result.Should().Contain(pm => pm.PaymentMethodName == "M-Pesa");
    }

    [Fact]
    public async Task GeneratePaymentMethodSalesAsync_ShouldCalculatePercentages()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GeneratePaymentMethodSalesAsync(parameters);

        // Assert
        if (result.Count > 0)
        {
            var totalPercentage = result.Sum(pm => pm.Percentage);
            totalPercentage.Should().BeApproximately(100m, 0.1m);
        }
    }

    #endregion

    #region GenerateHourlySalesAsync Tests

    [Fact]
    public async Task GenerateHourlySalesAsync_ShouldReturnHourlyBreakdown()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateHourlySalesAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(h => h.Hour >= 0 && h.Hour <= 23);
    }

    [Fact]
    public async Task GenerateHourlySalesAsync_ShouldHaveFormattedHourDisplay()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateHourlySalesAsync(parameters);

        // Assert
        result.Should().AllSatisfy(h =>
        {
            h.HourDisplay.Should().NotBeNullOrEmpty();
            h.HourDisplay.Should().Contain("-"); // e.g., "09:00 - 10:00"
        });
    }

    #endregion

    #region GenerateSalesReportAsync Tests

    [Fact]
    public async Task GenerateSalesReportAsync_DailySummary_ShouldReturnSummaryOnly()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.DailySummary, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Summary.TransactionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateSalesReportAsync_ByProduct_ShouldReturnProductSales()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.ByProduct, parameters);

        // Assert
        result.Should().NotBeNull();
        result.ProductSales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSalesReportAsync_ByCategory_ShouldReturnCategorySales()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.ByCategory, parameters);

        // Assert
        result.Should().NotBeNull();
        result.CategorySales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSalesReportAsync_ByCashier_ShouldReturnCashierSales()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.ByCashier, parameters);

        // Assert
        result.Should().NotBeNull();
        result.CashierSales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSalesReportAsync_ByPaymentMethod_ShouldReturnPaymentMethodSales()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.ByPaymentMethod, parameters);

        // Assert
        result.Should().NotBeNull();
        result.PaymentMethodSales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSalesReportAsync_HourlySales_ShouldReturnHourlySales()
    {
        // Arrange
        var parameters = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateSalesReportAsync(SalesReportType.HourlySales, parameters);

        // Assert
        result.Should().NotBeNull();
        result.HourlySales.Should().NotBeEmpty();
    }

    #endregion

    #region Date Range Filtering Tests

    [Fact]
    public async Task GenerateDailySummaryAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var yesterday = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-1),
            EndDate = DateTime.UtcNow.Date,
            GeneratedByUserId = TestUserId
        };

        var today = new SalesReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var yesterdayResult = await _reportService.GenerateDailySummaryAsync(yesterday);
        var todayResult = await _reportService.GenerateDailySummaryAsync(today);

        // Assert
        yesterdayResult.TransactionCount.Should().Be(0); // No transactions yesterday
        todayResult.TransactionCount.Should().BeGreaterThan(0); // Transactions today
    }

    #endregion

    #region GenerateVoidReportAsync Tests

    [Fact]
    public async Task GenerateVoidReportAsync_ShouldReturnVoidedReceipts()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateVoidReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1); // 1 voided receipt in test data
        result.TotalAmount.Should().BeGreaterThan(0);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateVoidReportAsync_ShouldCalculateAverageAmount()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateVoidReportAsync(parameters);

        // Assert
        if (result.TotalCount > 0)
        {
            result.AverageAmount.Should().Be(result.TotalAmount / result.TotalCount);
        }
    }

    [Fact]
    public async Task GenerateVoidReportAsync_WithNoVoids_ShouldReturnEmptyResult()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = DateTime.UtcNow.Date.AddDays(-29),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateVoidReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateVoidReportAsync_ShouldIncludeVoidDetails()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateVoidReportAsync(parameters);

        // Assert
        if (result.Items.Count > 0)
        {
            var item = result.Items.First();
            item.ReceiptNumber.Should().NotBeNullOrEmpty();
            item.VoidedAmount.Should().BeGreaterThan(0);
            item.VoidedBy.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region GenerateDiscountReportAsync Tests

    [Fact]
    public async Task GenerateDiscountReportAsync_WithNoDiscounts_ShouldReturnEmptyResult()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDiscountReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalDiscounts.Should().Be(0); // No discounts in test data
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateDiscountReportAsync_ShouldCalculateDiscountRate()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDiscountReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        // Discount rate should be 0 if no discounts
        if (result.TotalDiscounts == 0)
        {
            result.DiscountRate.Should().Be(0);
        }
        else if (result.TotalSales > 0)
        {
            result.DiscountRate.Should().Be(Math.Round(result.TotalDiscounts / result.TotalSales * 100, 2));
        }
    }

    [Fact]
    public async Task GenerateDiscountReportAsync_ShouldIncludeGenerationMetadata()
    {
        // Arrange
        var parameters = new ExceptionReportParameters
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateDiscountReportAsync(parameters);

        // Assert
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.GeneratedBy.Should().Be("Test User");
    }

    #endregion

    #region GenerateCurrentStockReportAsync Tests

    [Fact]
    public async Task GenerateCurrentStockReportAsync_ShouldReturnAllActiveProducts()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            IncludeOutOfStock = true
        };

        // Act
        var result = await _reportService.GenerateCurrentStockReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalSkuCount.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateCurrentStockReportAsync_ShouldCalculateTotals()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            IncludeOutOfStock = true
        };

        // Act
        var result = await _reportService.GenerateCurrentStockReportAsync(parameters);

        // Assert
        result.TotalStockValue.Should().BeGreaterThan(0);
        result.TotalRetailValue.Should().BeGreaterThan(0);
        result.PotentialProfit.Should().Be(result.TotalRetailValue - result.TotalStockValue);
    }

    [Fact]
    public async Task GenerateCurrentStockReportAsync_ShouldIdentifyLowStockItems()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            IncludeOutOfStock = true
        };

        // Act
        var result = await _reportService.GenerateCurrentStockReportAsync(parameters);

        // Assert
        result.LowStockCount.Should().BeGreaterThan(0); // Product 3 is low stock
        var lowStockItem = result.Items.FirstOrDefault(i => i.Status == "LOW");
        lowStockItem.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateCurrentStockReportAsync_WithCategoryFilter_ShouldFilterResults()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            CategoryId = 1, // Beverages only
            IncludeOutOfStock = true
        };

        // Act
        var result = await _reportService.GenerateCurrentStockReportAsync(parameters);

        // Assert
        result.Items.Should().OnlyContain(i => i.CategoryName == "Beverages");
    }

    #endregion

    #region GenerateLowStockReportAsync Tests

    [Fact]
    public async Task GenerateLowStockReportAsync_ShouldReturnLowStockItems()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateLowStockReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(i => i.Status == "LOW" || i.Status == "CRITICAL");
    }

    [Fact]
    public async Task GenerateLowStockReportAsync_ShouldCalculateReorderQuantities()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateLowStockReportAsync(parameters);

        // Assert
        result.Items.Should().AllSatisfy(i =>
        {
            i.ReorderQty.Should().BeGreaterThanOrEqualTo(0);
            // Reorder qty should bring stock to max level
            (i.CurrentStock + i.ReorderQty).Should().BeApproximately(i.MaxStock, 0.01m);
        });
    }

    [Fact]
    public async Task GenerateLowStockReportAsync_ShouldCalculateTotalReorderValue()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateLowStockReportAsync(parameters);

        // Assert
        var expectedTotal = result.Items.Sum(i => i.ReorderValue);
        result.TotalReorderValue.Should().Be(expectedTotal);
    }

    #endregion

    #region GenerateStockMovementReportAsync Tests

    [Fact]
    public async Task GenerateStockMovementReportAsync_ShouldReturnMovements()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockMovementReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateStockMovementReportAsync_ShouldCalculateNetMovement()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockMovementReportAsync(parameters);

        // Assert
        result.NetMovement.Should().Be(result.TotalReceived - result.TotalSold + result.TotalAdjusted);
    }

    [Fact]
    public async Task GenerateStockMovementReportAsync_ShouldOrderByDateDescending()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockMovementReportAsync(parameters);

        // Assert
        result.Items.Should().BeInDescendingOrder(i => i.Date);
    }

    [Fact]
    public async Task GenerateStockMovementReportAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            StartDate = DateTime.UtcNow.AddDays(-1).Date,
            EndDate = DateTime.UtcNow.AddDays(1),
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockMovementReportAsync(parameters);

        // Assert
        result.Items.Should().OnlyContain(i => i.Date >= parameters.StartDate && i.Date < parameters.EndDate);
    }

    #endregion

    #region GenerateStockValuationReportAsync Tests

    [Fact]
    public async Task GenerateStockValuationReportAsync_ShouldReturnCategoryBreakdown()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockValuationReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateStockValuationReportAsync_ShouldCalculateTotals()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockValuationReportAsync(parameters);

        // Assert
        result.TotalCostValue.Should().BeGreaterThan(0);
        result.TotalRetailValue.Should().BeGreaterThan(0);
        result.PotentialProfit.Should().Be(result.TotalRetailValue - result.TotalCostValue);
    }

    [Fact]
    public async Task GenerateStockValuationReportAsync_ShouldCalculateMarginPercentage()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockValuationReportAsync(parameters);

        // Assert
        if (result.TotalRetailValue > 0)
        {
            var expectedMargin = Math.Round((result.TotalRetailValue - result.TotalCostValue) / result.TotalRetailValue * 100, 2);
            result.MarginPercentage.Should().Be(expectedMargin);
        }
    }

    [Fact]
    public async Task GenerateStockValuationReportAsync_CategoriesShouldHaveValidData()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateStockValuationReportAsync(parameters);

        // Assert
        result.Categories.Should().AllSatisfy(c =>
        {
            c.CategoryName.Should().NotBeNullOrEmpty();
            c.ItemCount.Should().BeGreaterThan(0);
            c.CostValue.Should().BeGreaterThanOrEqualTo(0);
            c.RetailValue.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    #endregion

    #region GenerateDeadStockReportAsync Tests

    [Fact]
    public async Task GenerateDeadStockReportAsync_ShouldReturnDeadStockItems()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            DeadStockDaysThreshold = 0 // Products with no movement at all
        };

        // Act
        var result = await _reportService.GenerateDeadStockReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        // Product 4 has no movements
        result.Items.Should().Contain(i => i.ProductCode == "FOOD-002");
    }

    [Fact]
    public async Task GenerateDeadStockReportAsync_ShouldCalculateTotalValue()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            DeadStockDaysThreshold = 0
        };

        // Act
        var result = await _reportService.GenerateDeadStockReportAsync(parameters);

        // Assert
        var expectedTotal = result.Items.Sum(i => i.StockValue);
        result.TotalValue.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task GenerateDeadStockReportAsync_ShouldIncludeDaysThreshold()
    {
        // Arrange
        var threshold = 60;
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            DeadStockDaysThreshold = threshold
        };

        // Act
        var result = await _reportService.GenerateDeadStockReportAsync(parameters);

        // Assert
        result.DaysThreshold.Should().Be(threshold);
    }

    [Fact]
    public async Task GenerateDeadStockReportAsync_ShouldOrderByDaysSinceMovementDescending()
    {
        // Arrange
        var parameters = new InventoryReportParameters
        {
            GeneratedByUserId = TestUserId,
            DeadStockDaysThreshold = 0
        };

        // Act
        var result = await _reportService.GenerateDeadStockReportAsync(parameters);

        // Assert
        result.Items.Should().BeInDescendingOrder(i => i.DaysSinceMovement);
    }

    #endregion

    #region GenerateUserActivityReportAsync Tests

    [Fact]
    public async Task GenerateUserActivityReportAsync_ShouldReturnAllUsersWhenNoFilter()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateUserActivityReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(2); // Both users have activity
        result.Items.Should().Contain(i => i.Username == "testuser");
        result.Items.Should().Contain(i => i.Username == "cashier2");
    }

    [Fact]
    public async Task GenerateUserActivityReportAsync_ShouldFilterByUserId()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1),
            UserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateUserActivityReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(i => i.UserId == TestUserId);
    }

    [Fact]
    public async Task GenerateUserActivityReportAsync_ShouldCalculateActionCounts()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1),
            UserId = TestUserId
        };

        // Act
        var result = await _reportService.GenerateUserActivityReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        var user1Activity = result.Items.FirstOrDefault(i => i.UserId == TestUserId);
        user1Activity.Should().NotBeNull();
        user1Activity!.LoginCount.Should().BeGreaterOrEqualTo(1);
        user1Activity.ActionCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GenerateUserActivityReportAsync_ShouldCalculateSummary()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateUserActivityReportAsync(parameters);

        // Assert
        result.TotalActiveUsers.Should().BeGreaterOrEqualTo(2);
        result.TotalLogins.Should().BeGreaterOrEqualTo(2);
        result.TotalActions.Should().BeGreaterOrEqualTo(8); // All audit log entries
    }

    #endregion

    #region GenerateTransactionLogReportAsync Tests

    [Fact]
    public async Task GenerateTransactionLogReportAsync_ShouldReturnTransactionLogs()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateTransactionLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(1);
        result.Items.Should().Contain(i => i.Action.Contains("Settled")); // Receipt settled
    }

    [Fact]
    public async Task GenerateTransactionLogReportAsync_ShouldIncludeReceiptDetails()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateTransactionLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        var settledReceipt = result.Items.FirstOrDefault(i => i.Action.Contains("Settled"));
        settledReceipt.Should().NotBeNull();
        settledReceipt!.ReceiptNumber.Should().Be("R-001");
        settledReceipt.Amount.Should().Be(700m);
    }

    [Fact]
    public async Task GenerateTransactionLogReportAsync_ShouldCalculateTotals()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateTransactionLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalTransactions.Should().BeGreaterOrEqualTo(1);
        result.TotalAmount.Should().BeGreaterOrEqualTo(700m);
    }

    #endregion

    #region GenerateVoidRefundLogReportAsync Tests

    [Fact]
    public async Task GenerateVoidRefundLogReportAsync_ShouldReturnVoidEntries()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateVoidRefundLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(1);
        result.Items.Should().Contain(i => i.Action.Contains("Void"));
    }

    [Fact]
    public async Task GenerateVoidRefundLogReportAsync_ShouldIncludeVoidDetails()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateVoidRefundLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        var voidEntry = result.Items.FirstOrDefault(i => i.Action.Contains("Void"));
        voidEntry.Should().NotBeNull();
        voidEntry!.ReceiptNumber.Should().Be("R-VOID-001");
        voidEntry.Reason.Should().Contain("Customer cancelled");
    }

    [Fact]
    public async Task GenerateVoidRefundLogReportAsync_ShouldCalculateTotalAmount()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateVoidRefundLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalVoidedAmount.Should().BeGreaterOrEqualTo(300m);
    }

    #endregion

    #region GeneratePriceChangeLogReportAsync Tests

    [Fact]
    public async Task GeneratePriceChangeLogReportAsync_ShouldReturnPriceChanges()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GeneratePriceChangeLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GeneratePriceChangeLogReportAsync_ShouldIncludePriceDetails()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GeneratePriceChangeLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        var priceChange = result.Items.FirstOrDefault(i => i.ProductName?.Contains("Coca") == true);
        priceChange.Should().NotBeNull();
        priceChange!.OldPrice.Should().Be(80m);
        priceChange.NewPrice.Should().Be(100m);
    }

    #endregion

    #region GeneratePermissionOverrideLogReportAsync Tests

    [Fact]
    public async Task GeneratePermissionOverrideLogReportAsync_ShouldReturnOverrides()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GeneratePermissionOverrideLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GeneratePermissionOverrideLogReportAsync_ShouldIncludePermissionDetails()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GeneratePermissionOverrideLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        var overrideEntry = result.Items.FirstOrDefault();
        overrideEntry.Should().NotBeNull();
        overrideEntry!.Permission.Should().Contain("Void");
    }

    [Fact]
    public async Task GeneratePermissionOverrideLogReportAsync_ShouldCalculateTotalOverrides()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GeneratePermissionOverrideLogReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalOverrides.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region GenerateAuditTrailReportAsync Tests

    [Fact]
    public async Task GenerateAuditTrailReportAsync_ShouldReturnAllAuditEntries()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateAuditTrailReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10); // All 10 audit log entries
    }

    [Fact]
    public async Task GenerateAuditTrailReportAsync_ShouldFilterByAction()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1),
            Action = "Login"
        };

        // Act
        var result = await _reportService.GenerateAuditTrailReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(i => i.Action.Contains("Login"));
        result.Items.Should().HaveCount(2); // 2 login entries
    }

    [Fact]
    public async Task GenerateAuditTrailReportAsync_ShouldFilterByEntityType()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1),
            EntityType = "Receipt"
        };

        // Act
        var result = await _reportService.GenerateAuditTrailReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(i => i.EntityType == "Receipt");
    }

    [Fact]
    public async Task GenerateAuditTrailReportAsync_ShouldOrderByTimestampDescending()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = await _reportService.GenerateAuditTrailReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeInDescendingOrder(i => i.Timestamp);
    }

    [Fact]
    public async Task GenerateAuditTrailReportAsync_ShouldRespectMaxRecords()
    {
        // Arrange
        var parameters = new AuditReportParameters
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date.AddDays(1),
            MaxRecords = 5
        };

        // Act
        var result = await _reportService.GenerateAuditTrailReportAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessOrEqualTo(5);
    }

    #endregion

    #region GetDistinctAuditActionsAsync Tests

    [Fact]
    public async Task GetDistinctAuditActionsAsync_ShouldReturnDistinctActions()
    {
        // Act
        var result = await _reportService.GetDistinctAuditActionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Login");
        result.Should().Contain("Logout");
        result.Should().Contain("ProductUpdated");
        result.Should().Contain("ReceiptVoided");
        result.Should().Contain("PermissionOverride");
    }

    [Fact]
    public async Task GetDistinctAuditActionsAsync_ShouldBeOrdered()
    {
        // Act
        var result = await _reportService.GetDistinctAuditActionsAsync();

        // Assert
        result.Should().BeInAscendingOrder();
    }

    #endregion

    #region GetDistinctEntityTypesAsync Tests

    [Fact]
    public async Task GetDistinctEntityTypesAsync_ShouldReturnDistinctEntityTypes()
    {
        // Act
        var result = await _reportService.GetDistinctEntityTypesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Session");
        result.Should().Contain("Product");
        result.Should().Contain("Receipt");
        result.Should().Contain("Authorization");
    }

    [Fact]
    public async Task GetDistinctEntityTypesAsync_ShouldBeOrdered()
    {
        // Act
        var result = await _reportService.GetDistinctEntityTypesAsync();

        // Assert
        result.Should().BeInAscendingOrder();
    }

    #endregion
}
