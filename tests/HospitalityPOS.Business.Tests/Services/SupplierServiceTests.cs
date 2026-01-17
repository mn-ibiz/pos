using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the SupplierService class.
/// Tests cover supplier CRUD operations, code generation, and search functionality.
/// </summary>
public class SupplierServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly SupplierService _supplierService;
    private const int TestUserId = 1;

    public SupplierServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _supplierService = new SupplierService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<Supplier> CreateTestSupplierAsync(
        string code = "SUP-0001",
        string name = "Test Supplier",
        string? contactPerson = "John Contact",
        string? email = "contact@supplier.com",
        string? phone = "+254700000000",
        decimal currentBalance = 0,
        bool isActive = true)
    {
        var supplier = new Supplier
        {
            Code = code,
            Name = name,
            ContactPerson = contactPerson,
            Email = email,
            Phone = phone,
            CurrentBalance = currentBalance,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return supplier;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SupplierService(null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SupplierService(_context, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetAllSuppliersAsync Tests

    [Fact]
    public async Task GetAllSuppliersAsync_WithNoSuppliers_ShouldReturnEmptyList()
    {
        // Act
        var suppliers = await _supplierService.GetAllSuppliersAsync();

        // Assert
        suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllSuppliersAsync_ShouldReturnOnlyActiveSuppliers()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Active Supplier 1", isActive: true);
        await CreateTestSupplierAsync("SUP-0002", "Active Supplier 2", isActive: true);
        await CreateTestSupplierAsync("SUP-0003", "Inactive Supplier", isActive: false);

        // Act
        var suppliers = await _supplierService.GetAllSuppliersAsync(includeInactive: false);

        // Assert
        suppliers.Should().HaveCount(2);
        suppliers.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetAllSuppliersAsync_IncludeInactive_ShouldReturnAllSuppliers()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Active Supplier", isActive: true);
        await CreateTestSupplierAsync("SUP-0002", "Inactive Supplier", isActive: false);

        // Act
        var suppliers = await _supplierService.GetAllSuppliersAsync(includeInactive: true);

        // Assert
        suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllSuppliersAsync_ShouldOrderByName()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Zebra Supplies");
        await CreateTestSupplierAsync("SUP-0002", "Alpha Traders");
        await CreateTestSupplierAsync("SUP-0003", "Metro Distributors");

        // Act
        var suppliers = await _supplierService.GetAllSuppliersAsync();

        // Assert
        suppliers.Should().HaveCount(3);
        suppliers[0].Name.Should().Be("Alpha Traders");
        suppliers[1].Name.Should().Be("Metro Distributors");
        suppliers[2].Name.Should().Be("Zebra Supplies");
    }

    #endregion

    #region GetSupplierByIdAsync Tests

    [Fact]
    public async Task GetSupplierByIdAsync_WithValidId_ShouldReturnSupplier()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(supplier.Id);
        result.Code.Should().Be(supplier.Code);
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierByIdAsync_WithInactiveSupplier_ShouldStillReturn()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync(isActive: false);

        // Act
        var result = await _supplierService.GetSupplierByIdAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetSupplierByCodeAsync Tests

    [Fact]
    public async Task GetSupplierByCodeAsync_WithValidCode_ShouldReturnSupplier()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-TEST", "Test Supplier");

        // Act
        var result = await _supplierService.GetSupplierByCodeAsync("SUP-TEST");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("SUP-TEST");
    }

    [Fact]
    public async Task GetSupplierByCodeAsync_WithLowercaseCode_ShouldReturnSupplier()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-TEST", "Test Supplier");

        // Act
        var result = await _supplierService.GetSupplierByCodeAsync("sup-test");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSupplierByCodeAsync_WithInvalidCode_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByCodeAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierByCodeAsync_WithNullCode_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByCodeAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierByCodeAsync_WithEmptyCode_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByCodeAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierByCodeAsync_WithWhitespaceCode_ShouldReturnNull()
    {
        // Act
        var result = await _supplierService.GetSupplierByCodeAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SearchSuppliersAsync Tests

    [Fact]
    public async Task SearchSuppliersAsync_ByName_ShouldReturnMatches()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Metro Distributors");
        await CreateTestSupplierAsync("SUP-0002", "Alpha Traders");
        await CreateTestSupplierAsync("SUP-0003", "Metro Wholesale");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("Metro");

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(s => s.Name.Should().Contain("Metro"));
    }

    [Fact]
    public async Task SearchSuppliersAsync_ByCode_ShouldReturnMatches()
    {
        // Arrange
        await CreateTestSupplierAsync("ABC-0001", "Supplier One");
        await CreateTestSupplierAsync("ABC-0002", "Supplier Two");
        await CreateTestSupplierAsync("XYZ-0001", "Supplier Three");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("ABC");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSuppliersAsync_ByContactPerson_ShouldReturnMatches()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Supplier One", contactPerson: "John Smith");
        await CreateTestSupplierAsync("SUP-0002", "Supplier Two", contactPerson: "Jane Doe");
        await CreateTestSupplierAsync("SUP-0003", "Supplier Three", contactPerson: "John Brown");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("John");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSuppliersAsync_ByEmail_ShouldReturnMatches()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Supplier One", email: "sales@acme.com");
        await CreateTestSupplierAsync("SUP-0002", "Supplier Two", email: "info@other.com");
        await CreateTestSupplierAsync("SUP-0003", "Supplier Three", email: "orders@acme.com");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("acme");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSuppliersAsync_CaseInsensitive_ShouldWork()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "UPPERCASE SUPPLIER");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("uppercase");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchSuppliersAsync_WithEmptySearchTerm_ShouldReturnAll()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Supplier One");
        await CreateTestSupplierAsync("SUP-0002", "Supplier Two");

        // Act
        var results = await _supplierService.SearchSuppliersAsync("");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSuppliersAsync_IncludeInactive_ShouldReturnInactiveMatches()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Active Metro", isActive: true);
        await CreateTestSupplierAsync("SUP-0002", "Inactive Metro", isActive: false);

        // Act
        var resultsWithInactive = await _supplierService.SearchSuppliersAsync("Metro", includeInactive: true);
        var resultsActiveOnly = await _supplierService.SearchSuppliersAsync("Metro", includeInactive: false);

        // Assert
        resultsWithInactive.Should().HaveCount(2);
        resultsActiveOnly.Should().HaveCount(1);
    }

    #endregion

    #region CreateSupplierAsync Tests

    [Fact]
    public async Task CreateSupplierAsync_WithValidSupplier_ShouldCreate()
    {
        // Arrange
        var supplier = new Supplier
        {
            Code = "NEW-0001",
            Name = "New Supplier",
            ContactPerson = "Contact Person",
            Email = "email@test.com",
            CreatedByUserId = TestUserId
        };

        // Act
        var result = await _supplierService.CreateSupplierAsync(supplier);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Code.Should().Be("NEW-0001");

        var savedSupplier = await _context.Suppliers.FindAsync(result.Id);
        savedSupplier.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSupplierAsync_ShouldUppercaseCode()
    {
        // Arrange
        var supplier = new Supplier
        {
            Code = "lowercase-code",
            Name = "Test Supplier",
            CreatedByUserId = TestUserId
        };

        // Act
        var result = await _supplierService.CreateSupplierAsync(supplier);

        // Assert
        result.Code.Should().Be("LOWERCASE-CODE");
    }

    [Fact]
    public async Task CreateSupplierAsync_WithDuplicateCode_ShouldThrow()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Existing Supplier");

        var newSupplier = new Supplier
        {
            Code = "SUP-0001",
            Name = "Duplicate Supplier",
            CreatedByUserId = TestUserId
        };

        // Act
        var action = () => _supplierService.CreateSupplierAsync(newSupplier);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateSupplierAsync_WithNullSupplier_ShouldThrow()
    {
        // Act
        var action = () => _supplierService.CreateSupplierAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateSupplierAsync Tests

    [Fact]
    public async Task UpdateSupplierAsync_WithValidSupplier_ShouldUpdate()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync("SUP-0001", "Original Name");

        var updatedSupplier = new Supplier
        {
            Id = supplier.Id,
            Code = "SUP-0001",
            Name = "Updated Name",
            ContactPerson = "New Contact",
            Email = "new@email.com"
        };

        // Act
        var result = await _supplierService.UpdateSupplierAsync(updatedSupplier);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.ContactPerson.Should().Be("New Contact");
        result.Email.Should().Be("new@email.com");
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = 99999,
            Code = "SUP-0001",
            Name = "Test"
        };

        // Act
        var action = () => _supplierService.UpdateSupplierAsync(supplier);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateSupplierAsync_WithDuplicateCode_ShouldThrow()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "First Supplier");
        var secondSupplier = await CreateTestSupplierAsync("SUP-0002", "Second Supplier");

        var updateAttempt = new Supplier
        {
            Id = secondSupplier.Id,
            Code = "SUP-0001", // Trying to use first supplier's code
            Name = "Updated Second"
        };

        // Act
        var action = () => _supplierService.UpdateSupplierAsync(updateAttempt);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateSupplierAsync_SameCodeDifferentCase_ShouldSucceed()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync("SUP-0001", "Test Supplier");

        var updatedSupplier = new Supplier
        {
            Id = supplier.Id,
            Code = "sup-0001", // Same code, different case
            Name = "Updated Name"
        };

        // Act
        var result = await _supplierService.UpdateSupplierAsync(updatedSupplier);

        // Assert
        result.Code.Should().Be("SUP-0001"); // Uppercased
        result.Name.Should().Be("Updated Name");
    }

    #endregion

    #region ActivateSupplierAsync Tests

    [Fact]
    public async Task ActivateSupplierAsync_WithValidId_ShouldActivate()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync(isActive: false);

        // Act
        var result = await _supplierService.ActivateSupplierAsync(supplier.Id);

        // Assert
        result.Should().BeTrue();

        var activatedSupplier = await _context.Suppliers.FindAsync(supplier.Id);
        activatedSupplier!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateSupplierAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierService.ActivateSupplierAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateSupplierAsync_AlreadyActive_ShouldStillSucceed()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync(isActive: true);

        // Act
        var result = await _supplierService.ActivateSupplierAsync(supplier.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region DeactivateSupplierAsync Tests

    [Fact]
    public async Task DeactivateSupplierAsync_WithValidId_ShouldDeactivate()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync(isActive: true);

        // Act
        var result = await _supplierService.DeactivateSupplierAsync(supplier.Id);

        // Assert
        result.Should().BeTrue();

        var deactivatedSupplier = await _context.Suppliers.FindAsync(supplier.Id);
        deactivatedSupplier!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateSupplierAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierService.DeactivateSupplierAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsCodeUniqueAsync Tests

    [Fact]
    public async Task IsCodeUniqueAsync_WithUniqueCode_ShouldReturnTrue()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Existing Supplier");

        // Act
        var result = await _supplierService.IsCodeUniqueAsync("SUP-0002");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCodeUniqueAsync_WithExistingCode_ShouldReturnFalse()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Existing Supplier");

        // Act
        var result = await _supplierService.IsCodeUniqueAsync("SUP-0001");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCodeUniqueAsync_CaseInsensitive_ShouldWork()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Existing Supplier");

        // Act
        var result = await _supplierService.IsCodeUniqueAsync("sup-0001");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCodeUniqueAsync_WithExcludedId_ShouldExclude()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync("SUP-0001", "Existing Supplier");

        // Act
        var result = await _supplierService.IsCodeUniqueAsync("SUP-0001", supplier.Id);

        // Assert
        result.Should().BeTrue(); // Same code is allowed for same supplier
    }

    [Fact]
    public async Task IsCodeUniqueAsync_WithNullCode_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierService.IsCodeUniqueAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCodeUniqueAsync_WithEmptyCode_ShouldReturnFalse()
    {
        // Act
        var result = await _supplierService.IsCodeUniqueAsync("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateNextCodeAsync Tests

    [Fact]
    public async Task GenerateNextCodeAsync_WithNoSuppliers_ShouldReturnFirst()
    {
        // Act
        var code = await _supplierService.GenerateNextCodeAsync();

        // Assert
        code.Should().Be("SUP-0001");
    }

    [Fact]
    public async Task GenerateNextCodeAsync_WithExistingSuppliers_ShouldIncrement()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "First");
        await CreateTestSupplierAsync("SUP-0002", "Second");
        await CreateTestSupplierAsync("SUP-0003", "Third");

        // Act
        var code = await _supplierService.GenerateNextCodeAsync();

        // Assert
        code.Should().Be("SUP-0004");
    }

    [Fact]
    public async Task GenerateNextCodeAsync_WithGapsInSequence_ShouldUseNext()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "First");
        await CreateTestSupplierAsync("SUP-0005", "Fifth"); // Gap in sequence

        // Act
        var code = await _supplierService.GenerateNextCodeAsync();

        // Assert
        code.Should().Be("SUP-0006"); // Should be after highest number
    }

    [Fact]
    public async Task GenerateNextCodeAsync_WithNonStandardCodes_ShouldFallback()
    {
        // Arrange
        await CreateTestSupplierAsync("CUSTOM-ABC", "Custom Code Supplier");

        // Act
        var code = await _supplierService.GenerateNextCodeAsync();

        // Assert
        code.Should().Be("SUP-0001"); // Should use standard pattern
    }

    #endregion

    #region GetSupplierCountAsync Tests

    [Fact]
    public async Task GetSupplierCountAsync_WithNoSuppliers_ShouldReturnZero()
    {
        // Act
        var count = await _supplierService.GetSupplierCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetSupplierCountAsync_ShouldCountOnlyActive()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Active 1", isActive: true);
        await CreateTestSupplierAsync("SUP-0002", "Active 2", isActive: true);
        await CreateTestSupplierAsync("SUP-0003", "Inactive", isActive: false);

        // Act
        var count = await _supplierService.GetSupplierCountAsync(includeInactive: false);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetSupplierCountAsync_IncludeInactive_ShouldCountAll()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Active", isActive: true);
        await CreateTestSupplierAsync("SUP-0002", "Inactive", isActive: false);

        // Act
        var count = await _supplierService.GetSupplierCountAsync(includeInactive: true);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region GetSuppliersWithBalanceAsync Tests

    [Fact]
    public async Task GetSuppliersWithBalanceAsync_WithNoBalances_ShouldReturnEmpty()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Zero Balance", currentBalance: 0);

        // Act
        var suppliers = await _supplierService.GetSuppliersWithBalanceAsync();

        // Assert
        suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliersWithBalanceAsync_ShouldReturnOnlyWithBalance()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "With Balance", currentBalance: 1000);
        await CreateTestSupplierAsync("SUP-0002", "Zero Balance", currentBalance: 0);
        await CreateTestSupplierAsync("SUP-0003", "Also With Balance", currentBalance: 500);

        // Act
        var suppliers = await _supplierService.GetSuppliersWithBalanceAsync();

        // Assert
        suppliers.Should().HaveCount(2);
        suppliers.Should().AllSatisfy(s => s.CurrentBalance.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetSuppliersWithBalanceAsync_ShouldOrderByBalanceDescending()
    {
        // Arrange
        await CreateTestSupplierAsync("SUP-0001", "Low Balance", currentBalance: 100);
        await CreateTestSupplierAsync("SUP-0002", "High Balance", currentBalance: 5000);
        await CreateTestSupplierAsync("SUP-0003", "Medium Balance", currentBalance: 1000);

        // Act
        var suppliers = await _supplierService.GetSuppliersWithBalanceAsync();

        // Assert
        suppliers.Should().HaveCount(3);
        suppliers[0].CurrentBalance.Should().Be(5000);
        suppliers[1].CurrentBalance.Should().Be(1000);
        suppliers[2].CurrentBalance.Should().Be(100);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetAllSuppliersAsync_WithCancelledToken_ShouldThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _supplierService.GetAllSuppliersAsync(cancellationToken: cts.Token));
    }

    #endregion
}
