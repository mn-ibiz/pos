using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class BatchPrepServiceTests
{
    private readonly Mock<IRepository<BatchPrep>> _batchPrepRepositoryMock;
    private readonly Mock<IRepository<BatchPrepIngredient>> _batchPrepIngredientRepositoryMock;
    private readonly Mock<IRepository<Recipe>> _recipeRepositoryMock;
    private readonly Mock<IRepository<RecipeIngredient>> _recipeIngredientRepositoryMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IRepository<IngredientDeductionLog>> _deductionLogRepositoryMock;
    private readonly Mock<IRepository<IngredientUsageSummary>> _usageSummaryRepositoryMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BatchPrepService>> _loggerMock;
    private readonly BatchPrepService _service;

    public BatchPrepServiceTests()
    {
        _batchPrepRepositoryMock = new Mock<IRepository<BatchPrep>>();
        _batchPrepIngredientRepositoryMock = new Mock<IRepository<BatchPrepIngredient>>();
        _recipeRepositoryMock = new Mock<IRepository<Recipe>>();
        _recipeIngredientRepositoryMock = new Mock<IRepository<RecipeIngredient>>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _deductionLogRepositoryMock = new Mock<IRepository<IngredientDeductionLog>>();
        _usageSummaryRepositoryMock = new Mock<IRepository<IngredientUsageSummary>>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BatchPrepService>>();

        _service = new BatchPrepService(
            _batchPrepRepositoryMock.Object,
            _batchPrepIngredientRepositoryMock.Object,
            _recipeRepositoryMock.Object,
            _recipeIngredientRepositoryMock.Object,
            _productRepositoryMock.Object,
            _deductionLogRepositoryMock.Object,
            _usageSummaryRepositoryMock.Object,
            _inventoryServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBatchPrepRepository_ThrowsArgumentNullException()
    {
        var act = () => new BatchPrepService(
            null!,
            _batchPrepIngredientRepositoryMock.Object,
            _recipeRepositoryMock.Object,
            _recipeIngredientRepositoryMock.Object,
            _productRepositoryMock.Object,
            _deductionLogRepositoryMock.Object,
            _usageSummaryRepositoryMock.Object,
            _inventoryServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("batchPrepRepository");
    }

    [Fact]
    public void Constructor_WithNullRecipeRepository_ThrowsArgumentNullException()
    {
        var act = () => new BatchPrepService(
            _batchPrepRepositoryMock.Object,
            _batchPrepIngredientRepositoryMock.Object,
            null!,
            _recipeIngredientRepositoryMock.Object,
            _productRepositoryMock.Object,
            _deductionLogRepositoryMock.Object,
            _usageSummaryRepositoryMock.Object,
            _inventoryServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("recipeRepository");
    }

    [Fact]
    public void Constructor_WithNullInventoryService_ThrowsArgumentNullException()
    {
        var act = () => new BatchPrepService(
            _batchPrepRepositoryMock.Object,
            _batchPrepIngredientRepositoryMock.Object,
            _recipeRepositoryMock.Object,
            _recipeIngredientRepositoryMock.Object,
            _productRepositoryMock.Object,
            _deductionLogRepositoryMock.Object,
            _usageSummaryRepositoryMock.Object,
            null!,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inventoryService");
    }

    #endregion

    #region CreateBatchPrepAsync Tests

    [Fact]
    public async Task CreateBatchPrepAsync_WithValidRecipe_CreatesBatchPrep()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4,
            YieldUnit = "portion",
            IsActive = true
        };

        var dto = new CreateBatchPrepDto
        {
            RecipeId = 1,
            BatchSize = 2,
            Notes = "Test batch"
        };

        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _batchPrepRepositoryMock.Setup(r => r.AddAsync(It.IsAny<BatchPrep>()))
            .Callback<BatchPrep>(bp => bp.Id = 1)
            .Returns(Task.CompletedTask);
        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BatchPrep
            {
                Id = 1,
                RecipeId = 1,
                BatchSize = 2,
                ExpectedYield = 8,
                YieldUnit = "portion",
                Status = BatchPrepStatus.Planned
            });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });

        // Act
        var result = await _service.CreateBatchPrepAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(1);
        result.BatchSize.Should().Be(2);
        result.ExpectedYield.Should().Be(8);
        _batchPrepRepositoryMock.Verify(r => r.AddAsync(It.Is<BatchPrep>(bp =>
            bp.RecipeId == 1 && bp.BatchSize == 2 && bp.ExpectedYield == 8)), Times.Once);
    }

    [Fact]
    public async Task CreateBatchPrepAsync_WithInvalidRecipe_ThrowsException()
    {
        // Arrange
        var dto = new CreateBatchPrepDto { RecipeId = 999, BatchSize = 1 };
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateBatchPrepAsync(dto));
    }

    [Fact]
    public async Task CreateBatchPrepAsync_WithStartImmediately_StartsPrep()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4,
            YieldUnit = "portion",
            IsActive = true
        };

        var dto = new CreateBatchPrepDto
        {
            RecipeId = 1,
            BatchSize = 1,
            StartImmediately = true
        };

        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            BatchSize = 1,
            ExpectedYield = 4,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _batchPrepRepositoryMock.Setup(r => r.AddAsync(It.IsAny<BatchPrep>()))
            .Callback<BatchPrep>(bp => bp.Id = 1)
            .Returns(Task.CompletedTask);
        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient>());
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });

        // Act
        var result = await _service.CreateBatchPrepAsync(dto);

        // Assert
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.Status == BatchPrepStatus.InProgress)), Times.Once);
    }

    #endregion

    #region StartBatchPrepAsync Tests

    [Fact]
    public async Task StartBatchPrepAsync_WithValidBatchPrep_StartsPrep()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            BatchSize = 2,
            ExpectedYield = 8,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4
        };

        var recipeIngredient = new RecipeIngredient
        {
            Id = 1,
            RecipeId = 1,
            IngredientProductId = 20,
            Quantity = 100,
            Unit = RecipeUnitOfMeasure.Gram,
            WastePercent = 0,
            IsActive = true
        };

        var ingredientProduct = new Product
        {
            Id = 20,
            Name = "Flour",
            CostPrice = 2.50m
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient> { recipeIngredient });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(20)).ReturnsAsync(500m);
        _inventoryServiceMock.Setup(s => s.DeductStockAsync(20, 200, It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new StartBatchPrepDto
        {
            BatchPrepId = 1,
            DeductIngredients = true
        };

        // Act
        var result = await _service.StartBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.IngredientsDeducted.Should().BeTrue();
        result.DeductionResults.Should().HaveCount(1);
        _inventoryServiceMock.Verify(s => s.DeductStockAsync(20, 200, It.IsAny<string>(), 1), Times.Once);
    }

    [Fact]
    public async Task StartBatchPrepAsync_WithLowStock_ReturnsWarnings()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            BatchSize = 2,
            ExpectedYield = 8,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        var recipeIngredient = new RecipeIngredient
        {
            Id = 1,
            RecipeId = 1,
            IngredientProductId = 20,
            Quantity = 100,
            Unit = RecipeUnitOfMeasure.Gram,
            WastePercent = 0,
            IsActive = true
        };

        var ingredientProduct = new Product { Id = 20, Name = "Flour", CostPrice = 2.50m };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient> { recipeIngredient });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(20)).ReturnsAsync(50m); // Less than needed
        _inventoryServiceMock.Setup(s => s.DeductStockAsync(20, 200, It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new StartBatchPrepDto { BatchPrepId = 1, DeductIngredients = true };

        // Act
        var result = await _service.StartBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Low stock"));
    }

    [Fact]
    public async Task StartBatchPrepAsync_WithNonPlannedStatus_ReturnsError()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        var dto = new StartBatchPrepDto { BatchPrepId = 1 };

        // Act
        var result = await _service.StartBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not in Planned status"));
    }

    #endregion

    #region CompleteBatchPrepAsync Tests

    [Fact]
    public async Task CompleteBatchPrepAsync_WithValidBatchPrep_CompletesPrep()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            BatchSize = 2,
            ExpectedYield = 8,
            IngredientCost = 100,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };
        var product = new Product { Id = 10, Name = "Test Product" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _inventoryServiceMock.Setup(s => s.ReceiveStockAsync(10, 7.5m, It.IsAny<decimal>(), It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new CompleteBatchPrepDto
        {
            BatchPrepId = 1,
            ActualYield = 7.5m,
            AddToInventory = true
        };

        // Act
        var result = await _service.CompleteBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.ActualYield.Should().Be(7.5m);
        result.YieldVariance.Should().Be(-0.5m);
        result.AddedToInventory.Should().BeTrue();
        _inventoryServiceMock.Verify(s => s.ReceiveStockAsync(10, 7.5m, It.IsAny<decimal>(), It.IsAny<string>(), 1), Times.Once);
    }

    [Fact]
    public async Task CompleteBatchPrepAsync_WithWaste_SetsPartialStatus()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            ExpectedYield = 8,
            IngredientCost = 100,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _inventoryServiceMock.Setup(s => s.ReceiveStockAsync(10, 6m, It.IsAny<decimal>(), It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new CompleteBatchPrepDto
        {
            BatchPrepId = 1,
            ActualYield = 6m,
            WastedQuantity = 2m,
            WasteReason = "Spilled",
            AddToInventory = true
        };

        // Act
        var result = await _service.CompleteBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.Status == BatchPrepStatus.Partial)), Times.Once);
    }

    [Fact]
    public async Task CompleteBatchPrepAsync_WithNonInProgressStatus_ReturnsError()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        var dto = new CompleteBatchPrepDto { BatchPrepId = 1, ActualYield = 8 };

        // Act
        var result = await _service.CompleteBatchPrepAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not in InProgress status"));
    }

    #endregion

    #region CancelBatchPrepAsync Tests

    [Fact]
    public async Task CancelBatchPrepAsync_WithValidBatchPrep_CancelsBatchPrep()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            Status = BatchPrepStatus.Planned,
            IngredientsDeducted = false,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new CancelBatchPrepDto
        {
            BatchPrepId = 1,
            Reason = "No longer needed"
        };

        // Act
        var result = await _service.CancelBatchPrepAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.Status == BatchPrepStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task CancelBatchPrepAsync_WithDeductedIngredients_ReversesDeductions()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            Status = BatchPrepStatus.InProgress,
            IngredientsDeducted = true,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        var batchIngredient = new BatchPrepIngredient
        {
            Id = 1,
            BatchPrepId = 1,
            IngredientProductId = 20,
            ActualQuantity = 100,
            DeductionSuccessful = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient> { batchIngredient });
        _inventoryServiceMock.Setup(s => s.RestoreStockAsync(20, 100, MovementType.Adjustment, It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });

        var dto = new CancelBatchPrepDto
        {
            BatchPrepId = 1,
            Reason = "Cancelled",
            ReverseDeductions = true
        };

        // Act
        var result = await _service.CancelBatchPrepAsync(dto);

        // Assert
        _inventoryServiceMock.Verify(s => s.RestoreStockAsync(20, 100, MovementType.Adjustment, It.IsAny<string>(), 1), Times.Once);
    }

    [Fact]
    public async Task CancelBatchPrepAsync_CompletedBatchPrep_ThrowsException()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            Status = BatchPrepStatus.Completed,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        var dto = new CancelBatchPrepDto { BatchPrepId = 1, Reason = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelBatchPrepAsync(dto));
    }

    #endregion

    #region GetBatchPrepsAsync Tests

    [Fact]
    public async Task GetBatchPrepsAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var batchPreps = new List<BatchPrep>
        {
            new BatchPrep { Id = 1, RecipeId = 1, StoreId = 1, Status = BatchPrepStatus.Planned, IsActive = true },
            new BatchPrep { Id = 2, RecipeId = 1, StoreId = 2, Status = BatchPrepStatus.Completed, IsActive = true },
            new BatchPrep { Id = 3, RecipeId = 2, StoreId = 1, Status = BatchPrepStatus.Cancelled, IsActive = true }
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };
        var product = new Product { Id = 10, Name = "Test Product" };

        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(batchPreps);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

        var query = new BatchPrepQueryDto
        {
            RecipeId = 1,
            IncludeCancelled = false
        };

        // Act
        var result = await _service.GetBatchPrepsAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(bp => bp.RecipeId == 1);
    }

    [Fact]
    public async Task GetBatchPrepsAsync_WithStoreFilter_ReturnsStoreResults()
    {
        // Arrange
        var batchPreps = new List<BatchPrep>
        {
            new BatchPrep { Id = 1, RecipeId = 1, StoreId = 1, Status = BatchPrepStatus.Planned, IsActive = true },
            new BatchPrep { Id = 2, RecipeId = 1, StoreId = 2, Status = BatchPrepStatus.Completed, IsActive = true }
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };
        var product = new Product { Id = 10, Name = "Test Product" };

        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(batchPreps);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

        var query = new BatchPrepQueryDto { StoreId = 1 };

        // Act
        var result = await _service.GetBatchPrepsAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].StoreId.Should().Be(1);
    }

    #endregion

    #region ValidateBatchPrepAsync Tests

    [Fact]
    public async Task ValidateBatchPrepAsync_WithSufficientStock_ReturnsCanStart()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4,
            YieldUnit = "portion",
            IsActive = true
        };

        var recipeIngredient = new RecipeIngredient
        {
            Id = 1,
            RecipeId = 1,
            IngredientProductId = 20,
            Quantity = 100,
            Unit = RecipeUnitOfMeasure.Gram,
            WastePercent = 0,
            IsActive = true
        };

        var ingredientProduct = new Product { Id = 20, Name = "Flour", CostPrice = 2.50m };

        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient> { recipeIngredient });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(20)).ReturnsAsync(500m);

        // Act
        var result = await _service.ValidateBatchPrepAsync(1, 2);

        // Assert
        result.CanStart.Should().BeTrue();
        result.HasRecipe.Should().BeTrue();
        result.RecipeName.Should().Be("Test Recipe");
        result.ExpectedYield.Should().Be(8);
        result.Errors.Should().BeEmpty();
        result.IngredientAvailability.Should().HaveCount(1);
        result.IngredientAvailability[0].IsSufficient.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBatchPrepAsync_WithInsufficientStock_ReturnsWarnings()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4,
            IsActive = true
        };

        var recipeIngredient = new RecipeIngredient
        {
            Id = 1,
            RecipeId = 1,
            IngredientProductId = 20,
            Quantity = 100,
            Unit = RecipeUnitOfMeasure.Gram,
            WastePercent = 0,
            IsActive = true
        };

        var ingredientProduct = new Product { Id = 20, Name = "Flour", CostPrice = 2.50m };

        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient> { recipeIngredient });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(20)).ReturnsAsync(50m); // Insufficient

        // Act
        var result = await _service.ValidateBatchPrepAsync(1, 2);

        // Assert
        result.CanStart.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Insufficient stock"));
        result.IngredientAvailability[0].IsSufficient.Should().BeFalse();
        result.IngredientAvailability[0].Shortage.Should().Be(150m); // Need 200, have 50
    }

    [Fact]
    public async Task ValidateBatchPrepAsync_WithInvalidRecipe_ReturnsError()
    {
        // Arrange
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

        // Act
        var result = await _service.ValidateBatchPrepAsync(999, 1);

        // Assert
        result.CanStart.Should().BeFalse();
        result.HasRecipe.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    #endregion

    #region GetRequiredIngredientsAsync Tests

    [Fact]
    public async Task GetRequiredIngredientsAsync_CalculatesCorrectQuantities()
    {
        // Arrange
        var recipe = new Recipe
        {
            Id = 1,
            ProductId = 10,
            Name = "Test Recipe",
            YieldQuantity = 4
        };

        var recipeIngredients = new List<RecipeIngredient>
        {
            new RecipeIngredient
            {
                Id = 1,
                RecipeId = 1,
                IngredientProductId = 20,
                Quantity = 100,
                Unit = RecipeUnitOfMeasure.Gram,
                WastePercent = 10, // 10% waste
                IsActive = true
            },
            new RecipeIngredient
            {
                Id = 2,
                RecipeId = 1,
                IngredientProductId = 21,
                Quantity = 50,
                Unit = RecipeUnitOfMeasure.Milliliter,
                WastePercent = 0,
                IsActive = true
            }
        };

        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(recipeIngredients);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20))
            .ReturnsAsync(new Product { Id = 20, Name = "Flour", Code = "FLOUR", CostPrice = 2.50m });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(21))
            .ReturnsAsync(new Product { Id = 21, Name = "Oil", Code = "OIL", CostPrice = 5.00m });
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(It.IsAny<int>())).ReturnsAsync(1000m);

        // Act
        var result = await _service.GetRequiredIngredientsAsync(1, 2);

        // Assert
        result.Should().HaveCount(2);

        var flour = result.First(r => r.IngredientProductId == 20);
        flour.PlannedQuantity.Should().Be(220m); // 100 * 1.1 (waste) * 2 (batch size)

        var oil = result.First(r => r.IngredientProductId == 21);
        oil.PlannedQuantity.Should().Be(100m); // 50 * 1.0 * 2
    }

    #endregion

    #region GetBatchPrepSummaryAsync Tests

    [Fact]
    public async Task GetBatchPrepSummaryAsync_ReturnsSummary()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var batchPreps = new List<BatchPrep>
        {
            new BatchPrep { Id = 1, RecipeId = 1, Status = BatchPrepStatus.Completed, ExpectedYield = 10, ActualYield = 9.5m, IngredientCost = 100, CreatedAt = now, IsActive = true },
            new BatchPrep { Id = 2, RecipeId = 1, Status = BatchPrepStatus.Planned, ExpectedYield = 10, CreatedAt = now, IsActive = true },
            new BatchPrep { Id = 3, RecipeId = 1, Status = BatchPrepStatus.InProgress, ExpectedYield = 10, CreatedAt = now, IsActive = true },
            new BatchPrep { Id = 4, RecipeId = 2, Status = BatchPrepStatus.Cancelled, ExpectedYield = 10, CreatedAt = now, IsActive = true }
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };
        var product = new Product { Id = 10, Name = "Test Product" };

        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(batchPreps);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

        // Act
        var result = await _service.GetBatchPrepSummaryAsync(now.AddDays(-1), now.AddDays(1));

        // Assert
        result.TotalBatchPreps.Should().Be(4);
        result.PlannedPreps.Should().Be(1);
        result.InProgressPreps.Should().Be(1);
        result.CompletedPreps.Should().Be(1);
        result.CancelledPreps.Should().Be(1);
        result.TotalYieldProduced.Should().Be(9.5m);
    }

    #endregion

    #region Ingredient Usage Report Tests

    [Fact]
    public async Task GetIngredientUsageReportAsync_ReturnsUsageData()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var deductions = new List<IngredientDeductionLog>
        {
            new IngredientDeductionLog
            {
                Id = 1,
                RecipeId = 1,
                IngredientProductId = 20,
                QuantityDeducted = 100,
                Status = DeductionStatus.Success,
                DeductedAt = now,
                PortionsSold = 2
            }
        };

        var product = new Product { Id = 20, Name = "Flour", Code = "FLOUR", CostPrice = 2.50m };

        _deductionLogRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(deductions);
        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BatchPrep>());
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(product);
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(20)).ReturnsAsync(500m);

        var query = new IngredientUsageQueryDto
        {
            FromDate = now.AddDays(-1),
            ToDate = now.AddDays(1),
            IncludeSales = true,
            IncludeBatchPreps = true
        };

        // Act
        var result = await _service.GetIngredientUsageReportAsync(query);

        // Assert
        result.Lines.Should().HaveCount(1);
        result.Lines[0].IngredientProductId.Should().Be(20);
        result.Lines[0].SalesUsage.Should().Be(100);
        result.TotalSalesUsage.Should().Be(100);
    }

    [Fact]
    public async Task GetTopIngredientsAsync_ReturnsTopIngredients()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var deductions = new List<IngredientDeductionLog>
        {
            new IngredientDeductionLog { Id = 1, IngredientProductId = 20, QuantityDeducted = 100, Status = DeductionStatus.Success, DeductedAt = now, PortionsSold = 2 },
            new IngredientDeductionLog { Id = 2, IngredientProductId = 21, QuantityDeducted = 50, Status = DeductionStatus.Success, DeductedAt = now, PortionsSold = 1 }
        };

        var flour = new Product { Id = 20, Name = "Flour", CostPrice = 2.50m };
        var sugar = new Product { Id = 21, Name = "Sugar", CostPrice = 3.00m };

        _deductionLogRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(deductions);
        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BatchPrep>());
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(flour);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(sugar);
        _inventoryServiceMock.Setup(s => s.GetStockLevelAsync(It.IsAny<int>())).ReturnsAsync(500m);

        // Act
        var result = await _service.GetTopIngredientsAsync(now.AddDays(-1), now.AddDays(1), 10);

        // Assert
        result.Should().HaveCount(2);
        result[0].Rank.Should().Be(1);
        result[0].TotalCost.Should().BeGreaterOrEqualTo(result[1].TotalCost);
    }

    #endregion

    #region GetUsageByRecipeAsync Tests

    [Fact]
    public async Task GetUsageByRecipeAsync_ReturnsRecipeUsage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var deductions = new List<IngredientDeductionLog>
        {
            new IngredientDeductionLog { Id = 1, RecipeId = 1, IngredientProductId = 20, QuantityDeducted = 100, Status = DeductionStatus.Success, DeductedAt = now, PortionsSold = 2 },
            new IngredientDeductionLog { Id = 2, RecipeId = 1, IngredientProductId = 21, QuantityDeducted = 50, Status = DeductionStatus.Success, DeductedAt = now, PortionsSold = 2 }
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };
        var product = new Product { Id = 10, Name = "Test Product" };
        var flour = new Product { Id = 20, Name = "Flour", CostPrice = 2.50m };
        var sugar = new Product { Id = 21, Name = "Sugar", CostPrice = 3.00m };

        _deductionLogRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(deductions);
        _batchPrepRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BatchPrep>());
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(flour);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(sugar);

        // Act
        var result = await _service.GetUsageByRecipeAsync(now.AddDays(-1), now.AddDays(1));

        // Assert
        result.Should().HaveCount(1);
        result[0].RecipeId.Should().Be(1);
        result[0].PortionsSold.Should().Be(4);
        result[0].Ingredients.Should().HaveCount(2);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task StartBatchPrepAsync_RaisesBatchPrepStartedEvent()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _recipeIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<RecipeIngredient>());
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });

        BatchPrepDto? eventResult = null;
        _service.BatchPrepStarted += (sender, dto) => eventResult = dto;

        var dto = new StartBatchPrepDto { BatchPrepId = 1 };

        // Act
        await _service.StartBatchPrepAsync(dto);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.Id.Should().Be(1);
    }

    [Fact]
    public async Task CompleteBatchPrepAsync_RaisesBatchPrepCompletedEvent()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            ExpectedYield = 10,
            IngredientCost = 100,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _inventoryServiceMock.Setup(s => s.ReceiveStockAsync(10, 10m, It.IsAny<decimal>(), It.IsAny<string>(), 1))
            .ReturnsAsync(new StockMovement { Id = 1 });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        BatchPrepCompleteResultDto? eventResult = null;
        _service.BatchPrepCompleted += (sender, dto) => eventResult = dto;

        var dto = new CompleteBatchPrepDto { BatchPrepId = 1, ActualYield = 10, AddToInventory = true };

        // Act
        await _service.CompleteBatchPrepAsync(dto);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.Success.Should().BeTrue();
    }

    #endregion

    #region UpdateBatchPrepAsync Tests

    [Fact]
    public async Task UpdateBatchPrepAsync_WithValidUpdate_UpdatesBatchPrep()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            BatchSize = 1,
            ExpectedYield = 4,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe", YieldQuantity = 4 };
        var product = new Product { Id = 10, Name = "Test Product" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        var dto = new UpdateBatchPrepDto
        {
            BatchSize = 3,
            Notes = "Updated notes"
        };

        // Act
        var result = await _service.UpdateBatchPrepAsync(1, dto);

        // Assert
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.BatchSize == 3 && bp.ExpectedYield == 12 && bp.Notes == "Updated notes")), Times.Once);
    }

    [Fact]
    public async Task UpdateBatchPrepAsync_NonPlannedStatus_ThrowsException()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        var dto = new UpdateBatchPrepDto { Notes = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateBatchPrepAsync(1, dto));
    }

    #endregion

    #region DeleteBatchPrepAsync Tests

    [Fact]
    public async Task DeleteBatchPrepAsync_WithPlannedBatchPrep_SoftDeletes()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            Status = BatchPrepStatus.Planned,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        // Act
        var result = await _service.DeleteBatchPrepAsync(1);

        // Assert
        result.Should().BeTrue();
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task DeleteBatchPrepAsync_WithNonPlannedBatchPrep_ThrowsException()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            Status = BatchPrepStatus.InProgress,
            IsActive = true
        };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteBatchPrepAsync(1));
    }

    #endregion

    #region RecordWasteAsync Tests

    [Fact]
    public async Task RecordWasteAsync_UpdatesStatusToWasted()
    {
        // Arrange
        var batchPrep = new BatchPrep
        {
            Id = 1,
            RecipeId = 1,
            Status = BatchPrepStatus.Completed,
            IsActive = true
        };

        var recipe = new Recipe { Id = 1, ProductId = 10, Name = "Test Recipe" };

        _batchPrepRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batchPrep);
        _recipeRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Product { Id = 10, Name = "Test Product" });
        _batchPrepIngredientRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BatchPrepIngredient>());

        // Act
        var result = await _service.RecordWasteAsync(1, 2.5m, "Spoiled");

        // Assert
        _batchPrepRepositoryMock.Verify(r => r.UpdateAsync(It.Is<BatchPrep>(bp =>
            bp.Status == BatchPrepStatus.Wasted &&
            bp.Notes != null && bp.Notes.Contains("Wasted: 2.5 - Spoiled"))), Times.Once);
    }

    #endregion
}
