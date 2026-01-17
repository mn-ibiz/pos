using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the CheckoutEnhancementService class.
/// </summary>
public class CheckoutEnhancementServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly ICheckoutEnhancementService _service;

    public CheckoutEnhancementServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _service = new CheckoutEnhancementService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Store
        _context.Stores.Add(new Store { Id = 1, Name = "Test Store", Code = "TST001" });

        // Users
        _context.Users.AddRange(
            new User { Id = 1, Username = "cashier1", PasswordHash = "hash", FullName = "Cashier One" },
            new User { Id = 2, Username = "cashier2", PasswordHash = "hash", FullName = "Cashier Two" }
        );

        // Products
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Widget A", Code = "WA001", Price = 100, Cost = 60 },
            new Product { Id = 2, Name = "Widget B", Code = "WB001", Price = 150, Cost = 90 },
            new Product { Id = 3, Name = "Widget C", Code = "WC001", Price = 200, Cost = 120 }
        );

        // Receipt for split payment testing
        var receipt = new Receipt
        {
            Id = 1,
            ReceiptNumber = "R001",
            StoreId = 1,
            ReceiptDate = DateTime.UtcNow,
            TotalAmount = 450,
            IsPaid = false
        };
        _context.Receipts.Add(receipt);

        _context.ReceiptItems.AddRange(
            new ReceiptItem { Id = 1, ReceiptId = 1, ProductId = 1, Quantity = 1, UnitPrice = 100 },
            new ReceiptItem { Id = 2, ReceiptId = 1, ProductId = 2, Quantity = 1, UnitPrice = 150 },
            new ReceiptItem { Id = 3, ReceiptId = 1, ProductId = 3, Quantity = 1, UnitPrice = 200 }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Suspended Transactions Tests

    [Fact]
    public async Task ParkTransactionAsync_ShouldParkTransaction()
    {
        // Arrange
        var request = new ParkTransactionRequest
        {
            StoreId = 1,
            ParkedByUserId = 1,
            CustomerName = "John Doe",
            TableNumber = "T5",
            OrderType = "Dine-In",
            Notes = "Customer stepped out",
            Items = new List<ParkTransactionItem>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Widget A",
                    ProductCode = "WA001",
                    Quantity = 2,
                    UnitPrice = 100,
                    DiscountAmount = 0,
                    TaxAmount = 32
                }
            }
        };

        // Act
        var result = await _service.ParkTransactionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ReferenceNumber.Should().StartWith("PKD-");
        result.CustomerName.Should().Be("John Doe");
        result.Status.Should().Be(SuspendedTransactionStatus.Parked);
        result.ItemCount.Should().Be(1);
        result.TotalAmount.Should().Be(232); // (100 * 2) + 32
    }

    [Fact]
    public async Task GetSuspendedTransactionAsync_ShouldReturnTransaction()
    {
        // Arrange
        var parked = await CreateTestParkedTransactionAsync();

        // Act
        var result = await _service.GetSuspendedTransactionAsync(parked.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSuspendedTransactionByReferenceAsync_ShouldReturnTransaction()
    {
        // Arrange
        var parked = await CreateTestParkedTransactionAsync();

        // Act
        var result = await _service.GetSuspendedTransactionByReferenceAsync(parked.ReferenceNumber);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(parked.Id);
    }

    [Fact]
    public async Task GetParkedTransactionsAsync_ShouldReturnParkedTransactions()
    {
        // Arrange
        await CreateTestParkedTransactionAsync();
        await CreateTestParkedTransactionAsync();

        // Act
        var result = await _service.GetParkedTransactionsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecallTransactionAsync_ShouldRecallTransaction()
    {
        // Arrange
        var parked = await CreateTestParkedTransactionAsync();

        // Act
        var result = await _service.RecallTransactionAsync(parked.Id, userId: 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Transaction!.Status.Should().Be(SuspendedTransactionStatus.Recalled);
        result.Transaction.RecalledByUserId.Should().Be(2);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task RecallTransactionAsync_ShouldFailForNonexistentTransaction()
    {
        // Act
        var result = await _service.RecallTransactionAsync(999, userId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RecallTransactionAsync_ShouldFailForExpiredTransaction()
    {
        // Arrange
        var request = new ParkTransactionRequest
        {
            StoreId = 1,
            ParkedByUserId = 1,
            ExpirationMinutes = -1, // Already expired
            Items = new List<ParkTransactionItem>
            {
                new() { ProductId = 1, ProductName = "Widget A", ProductCode = "WA001", Quantity = 1, UnitPrice = 100 }
            }
        };
        var parked = await _service.ParkTransactionAsync(request);

        // Manually set expiration to past
        var transaction = await _context.SuspendedTransactions.FindAsync(parked.Id);
        transaction!.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RecallTransactionAsync(parked.Id, userId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task VoidSuspendedTransactionAsync_ShouldVoidTransaction()
    {
        // Arrange
        var parked = await CreateTestParkedTransactionAsync();

        // Act
        await _service.VoidSuspendedTransactionAsync(parked.Id, userId: 1, "Customer left");

        // Assert
        var result = await _service.GetSuspendedTransactionAsync(parked.Id);
        result!.Status.Should().Be(SuspendedTransactionStatus.Voided);
        result.Notes.Should().Contain("Customer left");
    }

    [Fact]
    public async Task SearchSuspendedTransactionsAsync_ShouldFilterByCustomerName()
    {
        // Arrange
        await CreateTestParkedTransactionAsync("Alice");
        await CreateTestParkedTransactionAsync("Bob");

        var searchRequest = new SuspendedTransactionSearchRequest
        {
            StoreId = 1,
            CustomerName = "Alice"
        };

        // Act
        var result = await _service.SearchSuspendedTransactionsAsync(searchRequest);

        // Assert
        result.Should().HaveCount(1);
        result.First().CustomerName.Should().Be("Alice");
    }

    [Fact]
    public async Task ProcessExpiredTransactionsAsync_ShouldMarkExpiredTransactions()
    {
        // Arrange
        var parked = await CreateTestParkedTransactionAsync();
        var transaction = await _context.SuspendedTransactions.FindAsync(parked.Id);
        transaction!.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.ProcessExpiredTransactionsAsync();

        // Assert
        count.Should().Be(1);
        var result = await _service.GetSuspendedTransactionAsync(parked.Id);
        result!.Status.Should().Be(SuspendedTransactionStatus.Expired);
    }

    #endregion

    #region Customer-Facing Display Tests

    [Fact]
    public async Task SaveCustomerDisplayConfigAsync_ShouldSaveConfig()
    {
        // Arrange
        var config = new CustomerDisplayConfig
        {
            StoreId = 1,
            Name = "Main Display",
            DisplayType = "SecondaryMonitor",
            IsEnabled = true,
            BackgroundColor = "#FFFFFF",
            WelcomeMessage = "Welcome!",
            ThankYouMessage = "Thank you for shopping!"
        };

        // Act
        var result = await _service.SaveCustomerDisplayConfigAsync(config);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Main Display");
    }

    [Fact]
    public async Task GetCustomerDisplayConfigAsync_ShouldReturnConfig()
    {
        // Arrange
        var config = await CreateTestDisplayConfigAsync();

        // Act
        var result = await _service.GetCustomerDisplayConfigAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Display");
    }

    [Fact]
    public async Task SavePromotionalMessageAsync_ShouldSaveMessage()
    {
        // Arrange
        var config = await CreateTestDisplayConfigAsync();
        var message = new CustomerDisplayMessage
        {
            DisplayConfigId = config.Id,
            Title = "Holiday Sale!",
            Content = "Get 20% off all items",
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        var result = await _service.SavePromotionalMessageAsync(message);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Holiday Sale!");
    }

    [Fact]
    public async Task GetActivePromotionalMessagesAsync_ShouldReturnActiveMessages()
    {
        // Arrange
        var config = await CreateTestDisplayConfigAsync();
        await _service.SavePromotionalMessageAsync(new CustomerDisplayMessage
        {
            DisplayConfigId = config.Id,
            Title = "Active Message",
            Content = "Content",
            IsActive = true
        });
        await _service.SavePromotionalMessageAsync(new CustomerDisplayMessage
        {
            DisplayConfigId = config.Id,
            Title = "Inactive Message",
            Content = "Content",
            IsActive = false
        });

        // Act
        var result = await _service.GetActivePromotionalMessagesAsync(config.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Active Message");
    }

    [Fact]
    public async Task GetCustomerDisplayStateAsync_ShouldReturnState()
    {
        // Act
        var result = await _service.GetCustomerDisplayStateAsync(1, null);

        // Assert
        result.Should().NotBeNull();
        result.State.Should().Be("Idle");
    }

    [Fact]
    public async Task UpdateCustomerDisplayStateAsync_ShouldUpdateState()
    {
        // Arrange
        var newState = new CustomerDisplayState
        {
            State = "Transaction",
            Subtotal = 100,
            TaxAmount = 16,
            TotalAmount = 116,
            Items = new List<CustomerDisplayItem>
            {
                new() { Name = "Widget A", Quantity = 1, UnitPrice = 100, LineTotal = 100 }
            }
        };

        // Act
        await _service.UpdateCustomerDisplayStateAsync(1, null, newState);
        var result = await _service.GetCustomerDisplayStateAsync(1, null);

        // Assert
        result.State.Should().Be("Transaction");
        result.TotalAmount.Should().Be(116);
    }

    #endregion

    #region Split Payment Tests

    [Fact]
    public async Task SaveSplitPaymentConfigAsync_ShouldSaveConfig()
    {
        // Arrange
        var config = new SplitPaymentConfig
        {
            StoreId = 1,
            MaxSplitWays = 10,
            MinSplitAmount = 50,
            AllowMixedPaymentMethods = true,
            AllowItemSplit = true
        };

        // Act
        var result = await _service.SaveSplitPaymentConfigAsync(config);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.MaxSplitWays.Should().Be(10);
    }

    [Fact]
    public async Task GetSplitPaymentConfigAsync_ShouldReturnConfig()
    {
        // Arrange
        await _service.SaveSplitPaymentConfigAsync(new SplitPaymentConfig
        {
            StoreId = 1,
            MaxSplitWays = 5
        });

        // Act
        var result = await _service.GetSplitPaymentConfigAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.MaxSplitWays.Should().Be(5);
    }

    [Fact]
    public async Task InitiateSplitPaymentAsync_ShouldCreateSession()
    {
        // Arrange
        var request = new InitiateSplitPaymentRequest
        {
            ReceiptId = 1,
            InitiatedByUserId = 1,
            SplitMethod = SplitPaymentMethodType.EqualSplit,
            NumberOfSplits = 3
        };

        // Act
        var result = await _service.InitiateSplitPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(450);
        result.NumberOfSplits.Should().Be(3);
        result.Parts.Should().HaveCount(3);
    }

    [Fact]
    public async Task CalculateEqualSplitAsync_ShouldCalculateCorrectly()
    {
        // Act
        var result = await _service.CalculateEqualSplitAsync(100, 3);

        // Assert
        result.TotalAmount.Should().Be(100);
        result.NumberOfSplits.Should().Be(3);
        result.SplitAmounts.Should().HaveCount(3);
        result.SplitAmounts.Sum().Should().Be(100);
        // First split gets remainder
        result.SplitAmounts[0].Should().Be(33.34m);
        result.SplitAmounts[1].Should().Be(33.33m);
        result.SplitAmounts[2].Should().Be(33.33m);
    }

    [Fact]
    public async Task ProcessSplitPartPaymentAsync_ShouldProcessPayment()
    {
        // Arrange
        var session = await _service.InitiateSplitPaymentAsync(new InitiateSplitPaymentRequest
        {
            ReceiptId = 1,
            InitiatedByUserId = 1,
            SplitMethod = SplitPaymentMethodType.EqualSplit,
            NumberOfSplits = 2
        });

        var request = new ProcessSplitPartRequest
        {
            SplitSessionId = session.Id,
            PartNumber = 1,
            PaymentMethod = "Cash",
            Amount = 225
        };

        // Act
        var result = await _service.ProcessSplitPartPaymentAsync(request);

        // Assert
        result.IsPaid.Should().BeTrue();
        result.PaymentMethod.Should().Be("Cash");
    }

    [Fact]
    public async Task CompleteSplitSessionAsync_ShouldCompleteWhenAllPaid()
    {
        // Arrange
        var session = await _service.InitiateSplitPaymentAsync(new InitiateSplitPaymentRequest
        {
            ReceiptId = 1,
            InitiatedByUserId = 1,
            SplitMethod = SplitPaymentMethodType.EqualSplit,
            NumberOfSplits = 2
        });

        // Pay both parts
        await _service.ProcessSplitPartPaymentAsync(new ProcessSplitPartRequest
        {
            SplitSessionId = session.Id,
            PartNumber = 1,
            PaymentMethod = "Cash",
            Amount = 225
        });
        await _service.ProcessSplitPartPaymentAsync(new ProcessSplitPartRequest
        {
            SplitSessionId = session.Id,
            PartNumber = 2,
            PaymentMethod = "Card",
            Amount = 225
        });

        // Act
        var result = await _service.CompleteSplitSessionAsync(session.Id);

        // Assert
        result.IsComplete.Should().BeTrue();
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateItemSplitAsync_ShouldCalculateByItems()
    {
        // Arrange
        var assignments = new List<ItemSplitAssignment>
        {
            new() { ItemId = 1, PartNumber = 1 },
            new() { ItemId = 2, PartNumber = 1 },
            new() { ItemId = 3, PartNumber = 2 }
        };

        // Act
        var result = await _service.CalculateItemSplitAsync(1, assignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Parts.Should().HaveCount(2);
        result.Parts.First(p => p.PartNumber == 1).Amount.Should().Be(250); // 100 + 150
        result.Parts.First(p => p.PartNumber == 2).Amount.Should().Be(200);
    }

    #endregion

    #region Quick Amount Buttons Tests

    [Fact]
    public async Task SaveQuickAmountButtonAsync_ShouldSaveButton()
    {
        // Arrange
        var button = new QuickAmountButton
        {
            StoreId = 1,
            Label = "KES 1000",
            Amount = 1000,
            ButtonType = "Fixed",
            DisplayOrder = 1,
            IsEnabled = true
        };

        // Act
        var result = await _service.SaveQuickAmountButtonAsync(button);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Amount.Should().Be(1000);
    }

    [Fact]
    public async Task GetQuickAmountButtonsAsync_ShouldReturnButtons()
    {
        // Arrange
        await _service.SaveQuickAmountButtonAsync(new QuickAmountButton
        {
            StoreId = 1,
            Label = "KES 500",
            Amount = 500,
            IsEnabled = true
        });
        await _service.SaveQuickAmountButtonAsync(new QuickAmountButton
        {
            StoreId = 1,
            Label = "KES 1000",
            Amount = 1000,
            IsEnabled = true
        });

        // Act
        var result = await _service.GetQuickAmountButtonsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteQuickAmountButtonAsync_ShouldSoftDelete()
    {
        // Arrange
        var button = await _service.SaveQuickAmountButtonAsync(new QuickAmountButton
        {
            StoreId = 1,
            Label = "KES 500",
            Amount = 500,
            IsEnabled = true
        });

        // Act
        await _service.DeleteQuickAmountButtonAsync(button.Id);

        // Assert
        var result = await _service.GetQuickAmountButtonsAsync(1);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateDefaultQuickAmountButtonsAsync_ShouldGenerateKESButtons()
    {
        // Act
        var result = await _service.GenerateDefaultQuickAmountButtonsAsync(1, "KES");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(b => b.Amount == 50);
        result.Should().Contain(b => b.Amount == 100);
        result.Should().Contain(b => b.Amount == 1000);
    }

    [Fact]
    public async Task GenerateDefaultQuickAmountButtonsAsync_ShouldGenerateUSDButtons()
    {
        // Act
        var result = await _service.GenerateDefaultQuickAmountButtonsAsync(1, "USD");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(b => b.Amount == 1);
        result.Should().Contain(b => b.Amount == 20);
        result.Should().Contain(b => b.Amount == 100);
    }

    [Fact]
    public async Task CalculateRoundUpAmountAsync_ShouldRoundUp()
    {
        // Act
        var result1 = await _service.CalculateRoundUpAmountAsync(17.50m, 10);
        var result2 = await _service.CalculateRoundUpAmountAsync(123.45m, 50);
        var result3 = await _service.CalculateRoundUpAmountAsync(100, 100);

        // Assert
        result1.Should().Be(20);
        result2.Should().Be(150);
        result3.Should().Be(100);
    }

    [Fact]
    public async Task SaveQuickAmountButtonSetAsync_ShouldSaveSet()
    {
        // Arrange
        var buttonSet = new QuickAmountButtonSet
        {
            StoreId = 1,
            Name = "Cash Buttons",
            Description = "Quick amounts for cash payments",
            IsActive = true,
            PaymentMethod = "Cash"
        };

        // Act
        var result = await _service.SaveQuickAmountButtonSetAsync(buttonSet);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Cash Buttons");
    }

    [Fact]
    public async Task GetQuickAmountButtonSetsAsync_ShouldReturnSets()
    {
        // Arrange
        await _service.SaveQuickAmountButtonSetAsync(new QuickAmountButtonSet
        {
            StoreId = 1,
            Name = "Cash Set",
            IsActive = true
        });

        // Act
        var result = await _service.GetQuickAmountButtonSetsAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private async Task<SuspendedTransaction> CreateTestParkedTransactionAsync(string? customerName = null)
    {
        var request = new ParkTransactionRequest
        {
            StoreId = 1,
            ParkedByUserId = 1,
            CustomerName = customerName ?? "Test Customer",
            Items = new List<ParkTransactionItem>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Widget A",
                    ProductCode = "WA001",
                    Quantity = 1,
                    UnitPrice = 100,
                    DiscountAmount = 0,
                    TaxAmount = 16
                }
            }
        };

        return await _service.ParkTransactionAsync(request);
    }

    private async Task<CustomerDisplayConfig> CreateTestDisplayConfigAsync()
    {
        var config = new CustomerDisplayConfig
        {
            StoreId = 1,
            Name = "Test Display",
            DisplayType = "SecondaryMonitor",
            IsEnabled = true
        };

        return await _service.SaveCustomerDisplayConfigAsync(config);
    }

    #endregion
}
