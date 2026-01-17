// tests/HospitalityPOS.Business.Tests/Services/QrMenuServiceTests.cs
// Unit tests for QrMenuService
// Story 44-1: QR Menu and Contactless Ordering

using FluentAssertions;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.QrMenu;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for QrMenuService.
/// </summary>
public class QrMenuServiceTests
{
    private readonly QrMenuService _service;

    public QrMenuServiceTests()
    {
        _service = new QrMenuService();
    }

    #region Configuration Tests

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnDefaultConfig()
    {
        // Act
        var config = await _service.GetConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.IsEnabled.Should().BeTrue();
        config.CurrencySymbol.Should().Be("KSh");
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldUpdateConfiguration()
    {
        // Arrange
        var config = await _service.GetConfigurationAsync();
        config.StoreName = "New Restaurant Name";
        config.DefaultWaitTimeMinutes = 20;

        // Act
        var saved = await _service.SaveConfigurationAsync(config);

        // Assert
        saved.StoreName.Should().Be("New Restaurant Name");
        saved.DefaultWaitTimeMinutes.Should().Be(20);
        saved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsEnabled_ShouldReturnConfigState()
    {
        // Assert
        _service.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region QR Code Generation Tests

    [Fact]
    public async Task GenerateTableQrCodeAsync_ShouldCreateQrCode()
    {
        // Act
        var qrCode = await _service.GenerateTableQrCodeAsync(5, "Table 5", "Main Hall");

        // Assert
        qrCode.Should().NotBeNull();
        qrCode.TableId.Should().Be(5);
        qrCode.TableName.Should().Be("Table 5");
        qrCode.Location.Should().Be("Main Hall");
        qrCode.MenuUrl.Should().Contain("table=5");
        qrCode.QrCodeBase64.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAllTableQrCodesAsync_ShouldCreateAllQrCodes()
    {
        // Arrange
        var tables = new[]
        {
            (1, "Table 1", (string?)"Indoor"),
            (2, "Table 2", (string?)"Indoor"),
            (3, "Table 3", (string?)"Outdoor")
        };

        // Act
        var qrCodes = await _service.GenerateAllTableQrCodesAsync(tables);

        // Assert
        qrCodes.Should().HaveCount(3);
        qrCodes.Should().Contain(q => q.TableId == 1);
        qrCodes.Should().Contain(q => q.TableId == 2);
        qrCodes.Should().Contain(q => q.TableId == 3);
    }

    [Fact]
    public async Task GetTableQrCodeAsync_AfterGeneration_ShouldReturnQrCode()
    {
        // Arrange
        await _service.GenerateTableQrCodeAsync(10, "Table 10");

        // Act
        var qrCode = await _service.GetTableQrCodeAsync(10);

        // Assert
        qrCode.Should().NotBeNull();
        qrCode!.TableId.Should().Be(10);
    }

    [Fact]
    public async Task GetTableQrCodeAsync_NotGenerated_ShouldReturnNull()
    {
        // Act
        var qrCode = await _service.GetTableQrCodeAsync(999);

        // Assert
        qrCode.Should().BeNull();
    }

    [Fact]
    public async Task RecordQrScanAsync_ShouldIncrementScanCount()
    {
        // Arrange
        var qrCode = await _service.GenerateTableQrCodeAsync(7, "Table 7");
        var initialCount = qrCode.ScanCount;

        // Act
        await _service.RecordQrScanAsync(7, "session-1");
        await _service.RecordQrScanAsync(7, "session-2");

        // Assert
        var updated = await _service.GetTableQrCodeAsync(7);
        updated!.ScanCount.Should().Be(initialCount + 2);
    }

    [Fact]
    public async Task RecordQrScanAsync_ShouldRaiseEvent()
    {
        // Arrange
        (int TableId, string? SessionId)? eventData = null;
        _service.QrCodeScanned += (s, e) => eventData = e;

        // Act
        await _service.RecordQrScanAsync(3, "test-session");

        // Assert
        eventData.Should().NotBeNull();
        eventData!.Value.TableId.Should().Be(3);
        eventData.Value.SessionId.Should().Be("test-session");
    }

    [Fact]
    public async Task GetPrintTemplateAsync_ShouldReturnTemplate()
    {
        // Arrange
        await _service.GenerateTableQrCodeAsync(6, "Table 6");

        // Act
        var template = await _service.GetPrintTemplateAsync(6);

        // Assert
        template.Should().NotBeNull();
        template!.TableName.Should().Be("Table 6");
        template.StoreName.Should().NotBeNullOrEmpty();
        template.InstructionText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Menu Management Tests

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnCategories()
    {
        // Act
        var categories = await _service.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeEmpty();
        categories.Should().Contain(c => c.Name == "Starters");
        categories.Should().Contain(c => c.Name == "Main Course");
        categories.Should().BeInAscendingOrder(c => c.SortOrder);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_ShouldReturnProducts()
    {
        // Act
        var products = await _service.GetProductsByCategoryAsync(1); // Starters

        // Assert
        products.Should().NotBeEmpty();
        products.Should().OnlyContain(p => p.CategoryId == 1);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Act
        var products = await _service.GetAllProductsAsync();

        // Assert
        products.Should().NotBeEmpty();
        products.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public async Task GetProductAsync_WithValidId_ShouldReturnProduct()
    {
        // Act
        var product = await _service.GetProductAsync(1);

        // Assert
        product.Should().NotBeNull();
        product!.Name.Should().Be("Chicken Wings");
        product.Price.Should().Be(650);
    }

    [Fact]
    public async Task GetProductAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var product = await _service.GetProductAsync(9999);

        // Assert
        product.Should().BeNull();
    }

    [Fact]
    public async Task GetFeaturedProductsAsync_ShouldReturnFeaturedItems()
    {
        // Act
        var featured = await _service.GetFeaturedProductsAsync(5);

        // Assert
        featured.Should().NotBeEmpty();
        featured.Should().OnlyContain(p => p.IsFeatured);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldFindMatches()
    {
        // Act
        var results = await _service.SearchProductsAsync("chicken");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(p => p.Name.Contains("Chicken", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SetProductAvailabilityAsync_ShouldUpdateAvailability()
    {
        // Arrange
        var product = await _service.GetProductAsync(1);
        product.Should().NotBeNull();

        // Act
        await _service.SetProductAvailabilityAsync(1, false);

        // Assert
        var updated = await _service.GetProductAsync(1);
        updated!.IsAvailable.Should().BeFalse();

        // Cleanup - restore availability
        await _service.SetProductAvailabilityAsync(1, true);
    }

    [Fact]
    public async Task SetProductStockStatusAsync_ShouldUpdateStock()
    {
        // Arrange
        var product = await _service.GetProductAsync(2);
        product.Should().NotBeNull();

        // Act
        await _service.SetProductStockStatusAsync(2, false);

        // Assert
        var updated = await _service.GetProductAsync(2);
        updated!.InStock.Should().BeFalse();

        // Cleanup
        await _service.SetProductStockStatusAsync(2, true);
    }

    #endregion

    #region Order Validation Tests

    [Fact]
    public async Task ValidateOrderAsync_ValidOrder_ShouldBeValid()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 5,
            TableName = "Table 5",
            Items = new List<QrCartItem>
            {
                new() { ProductId = 1, Quantity = 2 },
                new() { ProductId = 8, Quantity = 1 }
            }
        };

        // Act
        var (isValid, errors) = await _service.ValidateOrderAsync(request);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateOrderAsync_EmptyOrder_ShouldBeInvalid()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 5,
            TableName = "Table 5",
            Items = new List<QrCartItem>()
        };

        // Act
        var (isValid, errors) = await _service.ValidateOrderAsync(request);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("at least one item"));
    }

    [Fact]
    public async Task ValidateOrderAsync_InvalidTableId_ShouldBeInvalid()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 0,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var (isValid, errors) = await _service.ValidateOrderAsync(request);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Invalid table"));
    }

    [Fact]
    public async Task ValidateOrderAsync_InvalidProductId_ShouldBeInvalid()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 5,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 9999, Quantity = 1 }
            }
        };

        // Act
        var (isValid, errors) = await _service.ValidateOrderAsync(request);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateOrderAsync_UnavailableProduct_ShouldBeInvalid()
    {
        // Arrange
        await _service.SetProductAvailabilityAsync(3, false);
        var request = new QrMenuOrderRequest
        {
            TableId = 5,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 3, Quantity = 1 }
            }
        };

        // Act
        var (isValid, errors) = await _service.ValidateOrderAsync(request);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("not available"));

        // Cleanup
        await _service.SetProductAvailabilityAsync(3, true);
    }

    #endregion

    #region Order Submission Tests

    [Fact]
    public async Task SubmitOrderAsync_ValidOrder_ShouldSucceed()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 5,
            TableName = "Table 5",
            CustomerName = "John Doe",
            Items = new List<QrCartItem>
            {
                new() { ProductId = 4, Quantity = 1 }, // Beef Burger 850
                new() { ProductId = 8, Quantity = 2 }  // Fresh Juice 250 × 2 = 500
            }
        };

        // Act
        var response = await _service.SubmitOrderAsync(request);

        // Assert
        response.Success.Should().BeTrue();
        response.OrderId.Should().BeGreaterThan(0);
        response.ConfirmationCode.Should().HaveLength(6);
        response.OrderTotal.Should().Be(1350); // 850 + 500
        response.Status.Should().Be(QrOrderStatus.Received);
        response.EstimatedWaitMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitOrderAsync_InvalidOrder_ShouldFail()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 0,
            Items = new List<QrCartItem>()
        };

        // Act
        var response = await _service.SubmitOrderAsync(request);

        // Assert
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitOrderAsync_ShouldRaiseOrderReceivedEvent()
    {
        // Arrange
        QrOrderNotification? notification = null;
        _service.OrderReceived += (s, e) => notification = e;

        var request = new QrMenuOrderRequest
        {
            TableId = 3,
            TableName = "Table 3",
            Items = new List<QrCartItem>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _service.SubmitOrderAsync(request);

        // Assert
        notification.Should().NotBeNull();
        notification!.OrderId.Should().Be(response.OrderId);
        notification.TableId.Should().Be(3);
        notification.ItemCount.Should().Be(1);
    }

    #endregion

    #region Order Status Tests

    [Fact]
    public async Task GetOrderStatusAsync_AfterSubmit_ShouldReturnStatus()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 7,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };
        var submitResponse = await _service.SubmitOrderAsync(request);

        // Act
        var status = await _service.GetOrderStatusAsync(submitResponse.OrderId!.Value);

        // Assert
        status.Should().NotBeNull();
        status!.OrderId.Should().Be(submitResponse.OrderId);
        status.Status.Should().Be(QrOrderStatus.Received);
        status.ConfirmationCode.Should().Be(submitResponse.ConfirmationCode);
    }

    [Fact]
    public async Task GetOrderStatusByCodeAsync_ShouldReturnStatus()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 8,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 2, Quantity = 1 }
            }
        };
        var submitResponse = await _service.SubmitOrderAsync(request);

        // Act
        var status = await _service.GetOrderStatusByCodeAsync(submitResponse.ConfirmationCode!);

        // Assert
        status.Should().NotBeNull();
        status!.ConfirmationCode.Should().Be(submitResponse.ConfirmationCode);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 9,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 5, Quantity = 1 }
            }
        };
        var submitResponse = await _service.SubmitOrderAsync(request);

        // Act
        await _service.UpdateOrderStatusAsync(submitResponse.OrderId!.Value, QrOrderStatus.Preparing, 10);

        // Assert
        var status = await _service.GetOrderStatusAsync(submitResponse.OrderId!.Value);
        status!.Status.Should().Be(QrOrderStatus.Preparing);
        status.EstimatedWaitMinutes.Should().Be(10);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldRaiseEvent()
    {
        // Arrange
        QrOrderStatusUpdate? statusUpdate = null;
        _service.OrderStatusChanged += (s, e) => statusUpdate = e;

        var request = new QrMenuOrderRequest
        {
            TableId = 10,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 6, Quantity = 1 }
            }
        };
        var submitResponse = await _service.SubmitOrderAsync(request);

        // Act
        await _service.UpdateOrderStatusAsync(submitResponse.OrderId!.Value, QrOrderStatus.Ready);

        // Assert
        statusUpdate.Should().NotBeNull();
        statusUpdate!.Status.Should().Be(QrOrderStatus.Ready);
    }

    [Fact]
    public async Task CancelOrderAsync_PendingOrder_ShouldSucceed()
    {
        // Arrange
        var request = new QrMenuOrderRequest
        {
            TableId = 11,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 7, Quantity = 1 }
            }
        };
        var submitResponse = await _service.SubmitOrderAsync(request);

        // Act
        var result = await _service.CancelOrderAsync(submitResponse.OrderId!.Value, "Customer changed mind");

        // Assert
        result.Should().BeTrue();

        var status = await _service.GetOrderStatusAsync(submitResponse.OrderId!.Value);
        status!.Status.Should().Be(QrOrderStatus.Cancelled);
    }

    #endregion

    #region Order List Tests

    [Fact]
    public async Task GetPendingOrdersAsync_ShouldReturnPendingOrders()
    {
        // Arrange - Create a few orders
        for (int i = 0; i < 3; i++)
        {
            var request = new QrMenuOrderRequest
            {
                TableId = 20 + i,
                TableName = $"Table {20 + i}",
                Items = new List<QrCartItem>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            };
            await _service.SubmitOrderAsync(request);
        }

        // Act
        var pending = await _service.GetPendingOrdersAsync();

        // Assert
        pending.Should().NotBeEmpty();
        pending.Should().OnlyContain(o =>
            o.OrderId > 0 &&
            !string.IsNullOrEmpty(o.ConfirmationCode));
    }

    [Fact]
    public async Task GetRecentOrdersAsync_ShouldReturnRecentOrders()
    {
        // Arrange - Create an order
        var request = new QrMenuOrderRequest
        {
            TableId = 25,
            Items = new List<QrCartItem>
            {
                new() { ProductId = 8, Quantity = 1 }
            }
        };
        await _service.SubmitOrderAsync(request);

        // Act
        var recent = await _service.GetRecentOrdersAsync(5);

        // Assert
        recent.Should().NotBeEmpty();
        recent.First().OrderTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Analytics Tests

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnAnalytics()
    {
        // Arrange - Create some orders for analytics
        for (int i = 0; i < 5; i++)
        {
            await _service.GenerateTableQrCodeAsync(30 + i, $"Table {30 + i}");
            await _service.RecordQrScanAsync(30 + i);

            var request = new QrMenuOrderRequest
            {
                TableId = 30 + i,
                Items = new List<QrCartItem>
                {
                    new() { ProductId = 4, Quantity = 1 }
                }
            };
            await _service.SubmitOrderAsync(request);
        }

        // Act
        var analytics = await _service.GetAnalyticsAsync(
            DateTime.Today,
            DateTime.Today.AddDays(1)
        );

        // Assert
        analytics.Should().NotBeNull();
        analytics.TotalScans.Should().BeGreaterThan(0);
        analytics.TotalOrders.Should().BeGreaterThan(0);
        analytics.TotalRevenue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTodayAnalyticsAsync_ShouldReturnTodaysData()
    {
        // Act
        var analytics = await _service.GetTodayAnalyticsAsync();

        // Assert
        analytics.Should().NotBeNull();
        analytics.StartDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task GetPopularItemsAsync_ShouldReturnPopularItems()
    {
        // Arrange - Create orders with specific items
        for (int i = 0; i < 3; i++)
        {
            var request = new QrMenuOrderRequest
            {
                TableId = 40 + i,
                Items = new List<QrCartItem>
                {
                    new() { ProductId = 1, Quantity = 2 }, // Order Chicken Wings multiple times
                    new() { ProductId = 8, Quantity = 1 }
                }
            };
            await _service.SubmitOrderAsync(request);
        }

        // Act
        var popular = await _service.GetPopularItemsAsync(
            DateTime.Today,
            DateTime.Today.AddDays(1),
            5
        );

        // Assert
        popular.Should().NotBeEmpty();
        popular.Should().Contain(p => p.ProductId == 1); // Chicken Wings should be popular
    }

    #endregion

    #region Response Helper Tests

    [Fact]
    public void QrMenuOrderResponse_Successful_ShouldCreateSuccessResponse()
    {
        // Act
        var response = QrMenuOrderResponse.Successful(123, "ABC123", 1500, 15);

        // Assert
        response.Success.Should().BeTrue();
        response.OrderId.Should().Be(123);
        response.ConfirmationCode.Should().Be("ABC123");
        response.OrderTotal.Should().Be(1500);
        response.EstimatedWaitMinutes.Should().Be(15);
        response.Status.Should().Be(QrOrderStatus.Received);
    }

    [Fact]
    public void QrMenuOrderResponse_Failed_ShouldCreateFailureResponse()
    {
        // Act
        var response = QrMenuOrderResponse.Failed("Validation error");

        // Assert
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Be("Validation error");
        response.OrderId.Should().BeNull();
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void QrCartItem_LineTotal_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = new QrCartItem
        {
            UnitPrice = 850,
            ModifierTotal = 100,
            Quantity = 2
        };

        // Assert
        item.LineTotal.Should().Be(1900); // (850 + 100) × 2
    }

    [Fact]
    public void QrVsPosComparison_QrPercentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var comparison = new QrVsPosComparison
        {
            QrOrderCount = 30,
            PosOrderCount = 70
        };

        // Assert
        comparison.QrPercentage.Should().Be(30);
    }

    [Fact]
    public void QrVsPosComparison_ZeroOrders_ShouldReturnZeroPercent()
    {
        // Arrange
        var comparison = new QrVsPosComparison
        {
            QrOrderCount = 0,
            PosOrderCount = 0
        };

        // Assert
        comparison.QrPercentage.Should().Be(0);
    }

    #endregion
}
