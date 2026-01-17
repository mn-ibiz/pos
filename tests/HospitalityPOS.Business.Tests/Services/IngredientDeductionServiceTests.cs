using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for IngredientDeductionService.
/// </summary>
public class IngredientDeductionServiceTests
{
    private readonly Mock<IRepository<Recipe>> _recipeRepoMock;
    private readonly Mock<IRepository<RecipeIngredient>> _ingredientRepoMock;
    private readonly Mock<IRepository<RecipeSubRecipe>> _subRecipeRepoMock;
    private readonly Mock<IRepository<IngredientDeductionLog>> _logRepoMock;
    private readonly Mock<IRepository<ReceiptDeductionBatch>> _batchRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IngredientDeductionService _service;

    public IngredientDeductionServiceTests()
    {
        _recipeRepoMock = new Mock<IRepository<Recipe>>();
        _ingredientRepoMock = new Mock<IRepository<RecipeIngredient>>();
        _subRecipeRepoMock = new Mock<IRepository<RecipeSubRecipe>>();
        _logRepoMock = new Mock<IRepository<IngredientDeductionLog>>();
        _batchRepoMock = new Mock<IRepository<ReceiptDeductionBatch>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new IngredientDeductionService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _logRepoMock.Object,
            _batchRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object);

        // Enable deductions by default
        _service.UpdateConfiguration(new DeductionConfigDto { Enabled = true });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRecipeRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngredientDeductionService(
            null!,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _logRepoMock.Object,
            _batchRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recipeRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngredientDeductionService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _logRepoMock.Object,
            _batchRepoMock.Object,
            _productRepoMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    #endregion

    #region DeductIngredientsAsync Tests

    [Fact]
    public async Task DeductIngredientsAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.DeductIngredientsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeductIngredientsAsync_WhenDisabled_ReturnsSuccessWithWarning()
    {
        // Arrange
        _service.UpdateConfiguration(new DeductionConfigDto { Enabled = false });
        var request = new DeductIngredientsRequestDto
        {
            ReceiptId = 1,
            Items = new List<OrderItemDeductionDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await _service.DeductIngredientsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("disabled"));
    }

    [Fact]
    public async Task DeductIngredientsAsync_WithRecipe_DeductsIngredients()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 1000);
        var product = CreateTestProduct(10, "Cake");

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        var request = new DeductIngredientsRequestDto
        {
            ReceiptId = 1,
            Items = new List<OrderItemDeductionDto>
            {
                new() { ProductId = 10, Quantity = 1, ProductName = "Cake" }
            }
        };

        // Act
        var result = await _service.DeductIngredientsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsWithRecipes.Should().Be(1);
        result.TotalIngredientsDeducted.Should().Be(1);
        result.SuccessfulDeductions.Should().Be(1);
    }

    [Fact]
    public async Task DeductIngredientsAsync_WithoutRecipe_SkipsItem()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        var product = CreateTestProduct(10, "No Recipe Product");
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ReceiptDeductionBatch>()))
            .Callback<ReceiptDeductionBatch>(b => b.Id = 1)
            .Returns(Task.CompletedTask);

        var request = new DeductIngredientsRequestDto
        {
            ReceiptId = 1,
            Items = new List<OrderItemDeductionDto>
            {
                new() { ProductId = 10, Quantity = 1 }
            }
        };

        // Act
        var result = await _service.DeductIngredientsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsWithRecipes.Should().Be(0);
        result.ItemsWithoutRecipes.Should().Be(1);
    }

    [Fact]
    public async Task DeductIngredientsAsync_WithMultipleItems_ProcessesAll()
    {
        // Arrange
        var recipe1 = CreateTestRecipe(1, "Recipe 1", productId: 10);
        var recipe2 = CreateTestRecipe(2, "Recipe 2", productId: 11);
        var ingredient1 = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 50);
        var ingredient2 = CreateTestIngredient(2, 2, ingredientProductId: 21, quantity: 75);

        _recipeRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe1 })
            .ReturnsAsync(new List<Recipe> { recipe2 });

        _ingredientRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient> { ingredient1 })
            .ReturnsAsync(new List<RecipeIngredient> { ingredient2 });

        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Product 1"));
        _productRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(CreateTestProduct(11, "Product 2"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(CreateTestProduct(20, "Ingredient 1", 1000));
        _productRepoMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(CreateTestProduct(21, "Ingredient 2", 1000));

        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ReceiptDeductionBatch>()))
            .Callback<ReceiptDeductionBatch>(b => b.Id = 1)
            .Returns(Task.CompletedTask);

        var request = new DeductIngredientsRequestDto
        {
            ReceiptId = 1,
            Items = new List<OrderItemDeductionDto>
            {
                new() { ProductId = 10, Quantity = 2 },
                new() { ProductId = 11, Quantity = 3 }
            }
        };

        // Act
        var result = await _service.DeductIngredientsAsync(request);

        // Assert
        result.TotalItems.Should().Be(2);
        result.ItemResults.Should().HaveCount(2);
    }

    #endregion

    #region DeductForItemAsync Tests

    [Fact]
    public async Task DeductForItemAsync_WithRecipe_DeductsCorrectQuantity()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10, yieldQuantity: 4);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 1000);
        var product = CreateTestProduct(10, "Cake");

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act - Sell 2 portions from a recipe that yields 4
        var result = await _service.DeductForItemAsync(10, 2, receiptId: 1);

        // Assert
        result.HasRecipe.Should().BeTrue();
        result.IngredientDeductions.Should().HaveCount(1);
        // 2 portions / 4 yield * 100g = 50g should be deducted
        result.IngredientDeductions[0].QuantityDeducted.Should().Be(50);
    }

    [Fact]
    public async Task DeductForItemAsync_WithWastePercent_DeductsMoreQuantity()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10, yieldQuantity: 1);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100, wastePercent: 10);
        var ingredientProduct = CreateTestProduct(20, "Vegetable", stockQuantity: 1000);
        var product = CreateTestProduct(10, "Salad");

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.DeductForItemAsync(10, 1, receiptId: 1);

        // Assert
        result.IngredientDeductions.Should().HaveCount(1);
        // 100g * (1 + 10% waste) = 110g
        result.IngredientDeductions[0].QuantityDeducted.Should().Be(110);
    }

    [Fact]
    public async Task DeductForItemAsync_InsufficientStock_Fails()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 50); // Only 50, need 100

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Cake"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.DeductForItemAsync(10, 1, receiptId: 1, allowNegativeStock: false);

        // Assert
        result.AllDeductionsSuccessful.Should().BeFalse();
        result.IngredientDeductions[0].Status.Should().Be(DeductionStatusDto.Failed);
    }

    [Fact]
    public async Task DeductForItemAsync_InsufficientStockAllowed_ForcesDeduction()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 50);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Cake"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.DeductForItemAsync(10, 1, receiptId: 1, allowNegativeStock: true);

        // Assert
        result.AllDeductionsSuccessful.Should().BeTrue();
        result.IngredientDeductions[0].Status.Should().Be(DeductionStatusDto.Warning);
        result.IngredientDeductions[0].WasForced.Should().BeTrue();
        result.IngredientDeductions[0].StockAfter.Should().Be(-50);
    }

    #endregion

    #region ReverseDeductionsAsync Tests

    [Fact]
    public async Task ReverseDeductionsAsync_WithValidDeductions_ReversesAll()
    {
        // Arrange
        var log = new IngredientDeductionLog
        {
            Id = 1,
            ReceiptId = 1,
            RecipeId = 1,
            IngredientProductId = 20,
            QuantityDeducted = 100,
            StockBefore = 1000,
            StockAfter = 900,
            Status = DeductionStatus.Success,
            IsActive = true
        };

        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 900);

        _logRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IngredientDeductionLog, bool>>>()))
            .ReturnsAsync(new List<IngredientDeductionLog> { log });
        _logRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        var request = new ReverseDeductionRequestDto
        {
            ReceiptId = 1,
            Reason = "Voided receipt"
        };

        // Act
        var result = await _service.ReverseDeductionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.DeductionsReversed.Should().Be(1);
        ingredientProduct.StockQuantity.Should().Be(1000); // Restored
    }

    [Fact]
    public async Task ReverseDeductionAsync_WithAlreadyReversed_ThrowsInvalidOperationException()
    {
        // Arrange
        var log = new IngredientDeductionLog
        {
            Id = 1,
            ReceiptId = 1,
            ReversedAt = DateTime.UtcNow,
            IsActive = true
        };

        _logRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);

        // Act
        var act = () => _service.ReverseDeductionAsync(1, "Test");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been reversed*");
    }

    #endregion

    #region ValidateDeductionAsync Tests

    [Fact]
    public async Task ValidateDeductionAsync_WithSufficientStock_ReturnsCanDeduct()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 500);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.ValidateDeductionAsync(10, 2);

        // Assert
        result.CanDeduct.Should().BeTrue();
        result.HasRecipe.Should().BeTrue();
        result.IngredientAvailability.Should().HaveCount(1);
        result.IngredientAvailability[0].IsSufficient.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDeductionAsync_WithInsufficientStock_ReturnsWarning()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 50);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.ValidateDeductionAsync(10, 1);

        // Assert
        result.CanDeduct.Should().BeFalse(); // Default config doesn't allow negative
        result.Warnings.Should().HaveCount(1);
        result.IngredientAvailability[0].IsSufficient.Should().BeFalse();
        result.IngredientAvailability[0].Shortage.Should().Be(50);
    }

    [Fact]
    public async Task ValidateDeductionAsync_WithNoRecipe_ReturnsNoRecipe()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var result = await _service.ValidateDeductionAsync(10, 1);

        // Assert
        result.HasRecipe.Should().BeFalse();
        result.CanDeduct.Should().BeTrue(); // No recipe = no deduction needed
    }

    #endregion

    #region GetDeductionSummaryAsync Tests

    [Fact]
    public async Task GetDeductionSummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var logs = new List<IngredientDeductionLog>
        {
            CreateTestLog(1, 1, 1, 20, 100, DeductionStatus.Success),
            CreateTestLog(2, 1, 1, 21, 50, DeductionStatus.Success),
            CreateTestLog(3, 2, 1, 20, 150, DeductionStatus.Warning, wasForced: true)
        };

        _logRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IngredientDeductionLog, bool>>>()))
            .ReturnsAsync(logs);
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(CreateTestProduct(20, "Flour"));
        _productRepoMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(CreateTestProduct(21, "Sugar"));
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { CreateTestRecipe(1, "Test Recipe") });

        // Act
        var result = await _service.GetDeductionSummaryAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);

        // Assert
        result.TotalDeductions.Should().Be(3);
        result.SuccessfulDeductions.Should().Be(2);
        result.WarningDeductions.Should().Be(1);
        result.ForcedDeductions.Should().Be(1);
        result.TotalQuantityDeducted.Should().Be(300);
        result.ByIngredient.Should().HaveCount(2);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void GetConfiguration_ReturnsCurrentConfig()
    {
        // Arrange
        var config = new DeductionConfigDto
        {
            Enabled = true,
            AllowNegativeStock = true,
            RetryAttempts = 5
        };
        _service.UpdateConfiguration(config);

        // Act
        var result = _service.GetConfiguration();

        // Assert
        result.Enabled.Should().BeTrue();
        result.AllowNegativeStock.Should().BeTrue();
        result.RetryAttempts.Should().Be(5);
    }

    [Fact]
    public void UpdateConfiguration_WithNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.UpdateConfiguration(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsEnabled_ReflectsConfiguration()
    {
        // Arrange
        _service.UpdateConfiguration(new DeductionConfigDto { Enabled = true });

        // Assert
        _service.IsEnabled.Should().BeTrue();

        // Arrange
        _service.UpdateConfiguration(new DeductionConfigDto { Enabled = false });

        // Assert
        _service.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task DeductIngredientsAsync_RaisesIngredientsDeductedEvent()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Cake"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(CreateTestProduct(20, "Flour", 1000));

        DeductionResultDto? eventResult = null;
        _service.IngredientsDeducted += (s, e) => eventResult = e;

        var request = new DeductIngredientsRequestDto
        {
            ReceiptId = 1,
            Items = new List<OrderItemDeductionDto>
            {
                new() { ProductId = 10, Quantity = 1 }
            }
        };

        // Act
        await _service.DeductIngredientsAsync(request);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.ReceiptId.Should().Be(1);
    }

    [Fact]
    public async Task DeductForItemAsync_WithFailure_RaisesDeductionFailedEvent()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 50);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Cake"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        IngredientDeductionResultDto? eventResult = null;
        _service.DeductionFailed += (s, e) => eventResult = e;

        // Act
        await _service.DeductForItemAsync(10, 1, receiptId: 1, allowNegativeStock: false);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.Status.Should().Be(DeductionStatusDto.Failed);
    }

    [Fact]
    public async Task DeductForItemAsync_WithLowStock_RaisesLowStockDetectedEvent()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 10);
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 20, quantity: 100);
        var ingredientProduct = CreateTestProduct(20, "Flour", stockQuantity: 200, reorderLevel: 150);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CreateTestProduct(10, "Cake"));
        _productRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ingredientProduct);

        DeductionLowStockWarningDto? eventResult = null;
        _service.LowStockDetected += (s, e) => eventResult = e;

        // Act
        await _service.DeductForItemAsync(10, 1, receiptId: 1);

        // Assert - Stock will be 100 after deduction, below reorder level 150
        eventResult.Should().NotBeNull();
        eventResult!.CurrentStock.Should().Be(100);
    }

    #endregion

    #region Helper Methods

    private static Recipe CreateTestRecipe(int id, string name, int productId = 1, decimal yieldQuantity = 1)
    {
        return new Recipe
        {
            Id = id,
            ProductId = productId,
            Name = name,
            RecipeType = RecipeType.Standard,
            YieldQuantity = yieldQuantity,
            YieldUnit = "portion",
            IsApproved = true,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            Ingredients = new List<RecipeIngredient>(),
            SubRecipes = new List<RecipeSubRecipe>()
        };
    }

    private static RecipeIngredient CreateTestIngredient(
        int id,
        int recipeId,
        int ingredientProductId,
        decimal quantity,
        decimal wastePercent = 0)
    {
        return new RecipeIngredient
        {
            Id = id,
            RecipeId = recipeId,
            IngredientProductId = ingredientProductId,
            Quantity = quantity,
            Unit = RecipeUnitOfMeasure.Gram,
            WastePercent = wastePercent,
            IsOptional = false,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product CreateTestProduct(int id, string name, decimal stockQuantity = 0, decimal reorderLevel = 0)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Code = $"P{id:D3}",
            StockQuantity = stockQuantity,
            ReorderLevel = reorderLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static IngredientDeductionLog CreateTestLog(
        int id,
        int receiptId,
        int recipeId,
        int ingredientProductId,
        decimal quantityDeducted,
        DeductionStatus status,
        bool wasForced = false)
    {
        return new IngredientDeductionLog
        {
            Id = id,
            ReceiptId = receiptId,
            RecipeId = recipeId,
            IngredientProductId = ingredientProductId,
            PortionsSold = 1,
            QuantityDeducted = quantityDeducted,
            Unit = RecipeUnitOfMeasure.Gram,
            StockBefore = 1000,
            StockAfter = 1000 - quantityDeducted,
            Status = status,
            WasForced = wasForced,
            DeductedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void SetupRecipeWithIngredients(Recipe recipe, IEnumerable<RecipeIngredient> ingredients)
    {
        recipe.Ingredients = ingredients.ToList();
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(ingredients.ToList());
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());
        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ReceiptDeductionBatch>()))
            .Callback<ReceiptDeductionBatch>(b => b.Id = 1)
            .Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<IngredientDeductionLog>()))
            .Callback<IngredientDeductionLog>(l => l.Id = 1)
            .Returns(Task.CompletedTask);
    }

    #endregion
}
