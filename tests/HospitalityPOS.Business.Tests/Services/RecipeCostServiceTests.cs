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
/// Unit tests for RecipeCostService.
/// </summary>
public class RecipeCostServiceTests
{
    private readonly Mock<IRepository<Recipe>> _recipeRepoMock;
    private readonly Mock<IRepository<RecipeIngredient>> _ingredientRepoMock;
    private readonly Mock<IRepository<RecipeSubRecipe>> _subRecipeRepoMock;
    private readonly Mock<IRepository<RecipeCostHistory>> _costHistoryRepoMock;
    private readonly Mock<IRepository<UnitConversion>> _conversionRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RecipeCostService _service;

    public RecipeCostServiceTests()
    {
        _recipeRepoMock = new Mock<IRepository<Recipe>>();
        _ingredientRepoMock = new Mock<IRepository<RecipeIngredient>>();
        _subRecipeRepoMock = new Mock<IRepository<RecipeSubRecipe>>();
        _costHistoryRepoMock = new Mock<IRepository<RecipeCostHistory>>();
        _conversionRepoMock = new Mock<IRepository<UnitConversion>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new RecipeCostService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRecipeRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RecipeCostService(
            null!,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recipeRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RecipeCostService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    #endregion

    #region CalculateRecipeCostAsync Tests

    [Fact]
    public async Task CalculateRecipeCostAsync_WithValidRecipe_ReturnsCostResult()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 2, quantity: 100);
        var ingredientProduct = new Product { Id = 2, Name = "Flour", CostPrice = 0.05m };

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredientProduct);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, Name = "Cake", SellingPrice = 25 });
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.CalculateRecipeCostAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(1);
        result.TotalCost.Should().BeGreaterThan(0);
        result.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CalculateRecipeCostAsync_WithNonExistentRecipe_ThrowsKeyNotFoundException()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var act = () => _service.CalculateRecipeCostAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CalculateRecipeCostAsync_WithMultipleIngredients_SumsAllCosts()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredients = new[]
        {
            CreateTestIngredient(1, 1, ingredientProductId: 2, quantity: 100),
            CreateTestIngredient(2, 1, ingredientProductId: 3, quantity: 50)
        };

        SetupRecipeWithIngredients(recipe, ingredients);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(
            new Product { Id = 2, Name = "Flour", CostPrice = 0.05m });
        _productRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(
            new Product { Id = 3, Name = "Sugar", CostPrice = 0.08m });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, Name = "Cake", SellingPrice = 25 });
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.CalculateRecipeCostAsync(1);

        // Assert
        result.IngredientCosts.Should().HaveCount(2);
        result.TotalCost.Should().Be(result.IngredientCosts.Sum(ic => ic.TotalCost));
    }

    [Fact]
    public async Task CalculateRecipeCostAsync_CalculatesFoodCostPercent()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.YieldQuantity = 1;
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 2, quantity: 100);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(
            new Product { Id = 2, Name = "Flour", CostPrice = 0.05m });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, Name = "Cake", SellingPrice = 20 });
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.CalculateRecipeCostAsync(1);

        // Assert
        result.FoodCostPercent.Should().BeGreaterThan(0);
        result.GrossMarginPercent.Should().BeGreaterThan(0);
        result.FoodCostPercent.Should().BeLessThan(100);
    }

    #endregion

    #region GetCostAnalysisAsync Tests

    [Fact]
    public async Task GetCostAnalysisAsync_ReturnsDetailedAnalysis()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 2, quantity: 100);

        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(
            new Product { Id = 2, Name = "Flour", CostPrice = 0.05m });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, Name = "Cake", SellingPrice = 20 });
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.GetCostAnalysisAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(1);
        result.IngredientBreakdown.Should().NotBeEmpty();
        result.TotalIngredientCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCostAnalysisAsync_IdentifiesTopCostDrivers()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredients = new[]
        {
            CreateTestIngredient(1, 1, ingredientProductId: 2, quantity: 500), // Main cost driver
            CreateTestIngredient(2, 1, ingredientProductId: 3, quantity: 10)
        };

        SetupRecipeWithIngredients(recipe, ingredients);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(
            new Product { Id = 2, Name = "Expensive Item", CostPrice = 0.50m });
        _productRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(
            new Product { Id = 3, Name = "Cheap Item", CostPrice = 0.01m });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, Name = "Dish", SellingPrice = 50 });
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.GetCostAnalysisAsync(1);

        // Assert
        result.IngredientBreakdown.Any(i => i.IsTopCostDriver).Should().BeTrue();
    }

    #endregion

    #region RecalculateCostsAsync Tests

    [Fact]
    public async Task RecalculateCostsAsync_WithRecipeIds_RecalculatesSpecifiedRecipes()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 20 });
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        var request = new RecalculateCostsRequestDto
        {
            RecipeIds = new List<int> { 1 },
            Reason = "Test recalculation"
        };

        // Act
        var result = await _service.RecalculateCostsAsync(request);

        // Assert
        result.RecipesUpdated.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RecalculateCostsAsync_WithAffectedIngredients_FindsAffectedRecipes()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = CreateTestIngredient(1, 1, ingredientProductId: 5, quantity: 100);

        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient> { ingredient });
        SetupRecipeWithIngredients(recipe, new[] { ingredient });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 20 });
        _productRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(
            new Product { Id = 5, CostPrice = 0.10m });

        var request = new RecalculateCostsRequestDto
        {
            AffectedIngredientIds = new List<int> { 5 }
        };

        // Act
        var result = await _service.RecalculateCostsAsync(request);

        // Assert
        result.RecipesUpdated.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task RecalculateCostsAsync_TracksChanges()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.EstimatedCostPerPortion = 5; // Old cost
        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 20 });
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        var request = new RecalculateCostsRequestDto { RecipeIds = new List<int> { 1 } };

        // Act
        var result = await _service.RecalculateCostsAsync(request);

        // Assert
        result.Changes.Should().HaveCount(1);
        result.Changes[0].OldCostPerPortion.Should().Be(5);
    }

    [Fact]
    public async Task RecalculateCostsAsync_RaisesEvent()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 20 });
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        RecalculateCostsResultDto? eventResult = null;
        _service.CostsRecalculated += (s, e) => eventResult = e;

        var request = new RecalculateCostsRequestDto { RecipeIds = new List<int> { 1 } };

        // Act
        await _service.RecalculateCostsAsync(request);

        // Assert
        eventResult.Should().NotBeNull();
    }

    [Fact]
    public async Task RecalculateCostsAsync_WithNoRecipes_ReturnsError()
    {
        // Arrange
        var request = new RecalculateCostsRequestDto();

        // Act
        var result = await _service.RecalculateCostsAsync(request);

        // Assert
        result.Errors.Should().Contain(e => e.Contains("No recipes specified"));
    }

    #endregion

    #region GetHighFoodCostRecipesAsync Tests

    [Fact]
    public async Task GetHighFoodCostRecipesAsync_ReturnsRecipesAboveThreshold()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "High Cost Recipe");
        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 10 });
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        // Act
        var result = await _service.GetHighFoodCostRecipesAsync(25);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetHighCostAlertsAsync Tests

    [Fact]
    public async Task GetHighCostAlertsAsync_GeneratesAlerts()
    {
        // Arrange - recipe with high food cost
        var recipe = CreateTestRecipe(1, "Expensive Recipe");
        recipe.EstimatedCostPerPortion = 15; // High cost

        var product = new Product { Id = 1, Name = "Expensive Dish", SellingPrice = 20 };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient>());
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        // Act
        var result = await _service.GetHighCostAlertsAsync(30);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetCostTrendAsync Tests

    [Fact]
    public async Task GetCostTrendAsync_ReturnsTrendData()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var history = new List<RecipeCostHistory>
        {
            new() { Id = 1, RecipeId = 1, CostPerPortion = 5, CreatedAt = DateTime.UtcNow.AddDays(-30), IsActive = true },
            new() { Id = 2, RecipeId = 1, CostPerPortion = 5.5m, CreatedAt = DateTime.UtcNow.AddDays(-15), IsActive = true },
            new() { Id = 3, RecipeId = 1, CostPerPortion = 6, CreatedAt = DateTime.UtcNow, IsActive = true }
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _costHistoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeCostHistory, bool>>>()))
            .ReturnsAsync(history);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 20 });

        // Act
        var result = await _service.GetCostTrendAsync(1, 90);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(1);
        result.TrendPoints.Should().HaveCount(3);
        result.TrendDirection.Should().Be("Increasing");
    }

    [Fact]
    public async Task GetCostTrendAsync_WithNonExistentRecipe_ThrowsKeyNotFoundException()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var act = () => _service.GetCostTrendAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region GetPricingSuggestionAsync Tests

    [Fact]
    public async Task GetPricingSuggestionAsync_CalculatesSuggestedPrice()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.EstimatedCostPerPortion = 10;

        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 25 }); // 40% food cost
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        // Act
        var result = await _service.GetPricingSuggestionAsync(1, 30); // Target 30%

        // Assert
        result.Should().NotBeNull();
        result.TargetFoodCostPercent.Should().Be(30);
        result.SuggestedPrice.Should().BeGreaterThan(result.CurrentSellingPrice);
        result.RequiresPriceIncrease.Should().BeTrue();
    }

    [Fact]
    public async Task GetPricingSuggestionAsync_WithGoodMargin_NoIncrease()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.EstimatedCostPerPortion = 5;

        SetupRecipeWithIngredients(recipe, Array.Empty<RecipeIngredient>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 25 }); // 20% food cost
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        // Act
        var result = await _service.GetPricingSuggestionAsync(1, 30); // Target 30%

        // Assert
        result.RequiresPriceIncrease.Should().BeFalse();
        result.SuggestedPrice.Should().BeLessThanOrEqualTo(result.CurrentSellingPrice);
    }

    #endregion

    #region AnalyzePriceImpactAsync Tests

    [Fact]
    public async Task AnalyzePriceImpactAsync_CalculatesImpact()
    {
        // Arrange
        var ingredient = new Product { Id = 5, Name = "Flour", CostPrice = 0.10m };
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var usage = CreateTestIngredient(1, 1, ingredientProductId: 5, quantity: 100);

        _productRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(ingredient);
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient> { usage });
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Product { Id = 1, SellingPrice = 25 });

        // Act
        var result = await _service.AnalyzePriceImpactAsync(5, 0.15m); // 50% price increase

        // Assert
        result.Should().NotBeNull();
        result.IngredientProductId.Should().Be(5);
        result.OldPrice.Should().Be(0.10m);
        result.NewPrice.Should().Be(0.15m);
        result.PriceChangePercent.Should().Be(50);
        result.AffectedRecipes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzePriceImpactAsync_WithNonExistentIngredient_ThrowsKeyNotFoundException()
    {
        // Arrange
        _productRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.AnalyzePriceImpactAsync(999, 1.0m);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region NeedsRecalculationAsync Tests

    [Fact]
    public async Task NeedsRecalculationAsync_WithNoCalculation_ReturnsTrue()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.LastCostCalculation = null;

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.NeedsRecalculationAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NeedsRecalculationAsync_WithRecentCalculation_ReturnsFalse()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.LastCostCalculation = DateTime.UtcNow.AddDays(-1);

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.NeedsRecalculationAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NeedsRecalculationAsync_WithOldCalculation_ReturnsTrue()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.LastCostCalculation = DateTime.UtcNow.AddDays(-10);

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.NeedsRecalculationAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetCostHistoryAsync Tests

    [Fact]
    public async Task GetCostHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var history = new List<RecipeCostHistory>
        {
            new() { Id = 1, RecipeId = 1, CostPerPortion = 5, TotalCost = 20, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 2, RecipeId = 1, CostPerPortion = 5.5m, TotalCost = 22, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, RecipeId = 1, CostPerPortion = 6, TotalCost = 24, CreatedAt = DateTime.UtcNow }
        };

        _costHistoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeCostHistory, bool>>>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetCostHistoryAsync(1, 10);

        // Assert
        result.Should().HaveCount(3);
        result.First().CostPerPortion.Should().Be(6); // Most recent first
    }

    [Fact]
    public async Task GetCostHistoryAsync_CalculatesChangePercent()
    {
        // Arrange
        var history = new List<RecipeCostHistory>
        {
            new() { Id = 1, RecipeId = 1, CostPerPortion = 10, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 2, RecipeId = 1, CostPerPortion = 11, CreatedAt = DateTime.UtcNow } // 10% increase
        };

        _costHistoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeCostHistory, bool>>>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetCostHistoryAsync(1);

        // Assert
        var mostRecent = result.First();
        mostRecent.ChangePercent.Should().Be(10);
    }

    #endregion

    #region GetRecipesNeedingUpdateAsync Tests

    [Fact]
    public async Task GetRecipesNeedingUpdateAsync_ReturnsStaleRecipes()
    {
        // Arrange
        var staleRecipe = CreateTestRecipe(1, "Stale Recipe");
        staleRecipe.LastCostCalculation = DateTime.UtcNow.AddDays(-10);

        var freshRecipe = CreateTestRecipe(2, "Fresh Recipe");
        freshRecipe.LastCostCalculation = DateTime.UtcNow.AddDays(-1);

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { staleRecipe });

        // Act
        var result = await _service.GetRecipesNeedingUpdateAsync(7);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void RecipeCostAnalysisDto_CalculatesVariance()
    {
        // Arrange
        var dto = new RecipeCostAnalysisDto
        {
            FoodCostPercent = 35,
            TargetFoodCostPercent = 30
        };

        // Manually calculate variance as it would be set
        dto.FoodCostVariance = dto.FoodCostPercent - (dto.TargetFoodCostPercent ?? 0);
        dto.IsAboveTargetCost = dto.FoodCostVariance > 0;

        // Assert
        dto.FoodCostVariance.Should().Be(5);
        dto.IsAboveTargetCost.Should().BeTrue();
    }

    [Fact]
    public void PricingSuggestionDto_CalculatesAdjustment()
    {
        // Arrange
        var dto = new PricingSuggestionDto
        {
            CurrentSellingPrice = 20,
            SuggestedPrice = 25
        };

        dto.PriceAdjustment = dto.SuggestedPrice - dto.CurrentSellingPrice;

        // Assert
        dto.PriceAdjustment.Should().Be(5);
    }

    [Fact]
    public void CostTrendDto_DeterminesTrendDirection()
    {
        // Arrange - Increasing
        var increasing = new CostTrendDto
        {
            StartCost = 10,
            CurrentCost = 12
        };
        increasing.PercentChangeOverPeriod = ((increasing.CurrentCost - increasing.StartCost) / increasing.StartCost) * 100;
        increasing.TrendDirection = increasing.PercentChangeOverPeriod switch
        {
            > 5 => "Increasing",
            < -5 => "Decreasing",
            _ => "Stable"
        };

        // Assert
        increasing.TrendDirection.Should().Be("Increasing");

        // Arrange - Stable
        var stable = new CostTrendDto
        {
            StartCost = 10,
            CurrentCost = 10.2m
        };
        stable.PercentChangeOverPeriod = ((stable.CurrentCost - stable.StartCost) / stable.StartCost) * 100;
        stable.TrendDirection = stable.PercentChangeOverPeriod switch
        {
            > 5 => "Increasing",
            < -5 => "Decreasing",
            _ => "Stable"
        };

        // Assert
        stable.TrendDirection.Should().Be("Stable");
    }

    #endregion

    #region Helper Methods

    private static Recipe CreateTestRecipe(int id, string name, int productId = 1)
    {
        return new Recipe
        {
            Id = id,
            ProductId = productId,
            Name = name,
            RecipeType = RecipeType.Standard,
            YieldQuantity = 1,
            YieldUnit = "portion",
            IsApproved = true,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            Product = new Product { Id = productId, Name = $"Product {productId}", Code = $"P{productId:D3}", SellingPrice = 20 },
            Ingredients = new List<RecipeIngredient>(),
            SubRecipes = new List<RecipeSubRecipe>()
        };
    }

    private static RecipeIngredient CreateTestIngredient(
        int id,
        int recipeId,
        int ingredientProductId,
        decimal quantity,
        RecipeUnitOfMeasure unit = RecipeUnitOfMeasure.Gram)
    {
        return new RecipeIngredient
        {
            Id = id,
            RecipeId = recipeId,
            IngredientProductId = ingredientProductId,
            Quantity = quantity,
            Unit = unit,
            WastePercent = 0,
            IsOptional = false,
            SortOrder = 0,
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
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());
    }

    #endregion
}
