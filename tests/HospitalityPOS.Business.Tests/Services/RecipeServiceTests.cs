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
/// Unit tests for RecipeService.
/// </summary>
public class RecipeServiceTests
{
    private readonly Mock<IRepository<Recipe>> _recipeRepoMock;
    private readonly Mock<IRepository<RecipeIngredient>> _ingredientRepoMock;
    private readonly Mock<IRepository<RecipeSubRecipe>> _subRecipeRepoMock;
    private readonly Mock<IRepository<RecipeCostHistory>> _costHistoryRepoMock;
    private readonly Mock<IRepository<UnitConversion>> _conversionRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RecipeService _service;

    public RecipeServiceTests()
    {
        _recipeRepoMock = new Mock<IRepository<Recipe>>();
        _ingredientRepoMock = new Mock<IRepository<RecipeIngredient>>();
        _subRecipeRepoMock = new Mock<IRepository<RecipeSubRecipe>>();
        _costHistoryRepoMock = new Mock<IRepository<RecipeCostHistory>>();
        _conversionRepoMock = new Mock<IRepository<UnitConversion>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _userRepoMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new RecipeService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRecipeRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RecipeService(
            null!,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recipeRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RecipeService(
            _recipeRepoMock.Object,
            _ingredientRepoMock.Object,
            _subRecipeRepoMock.Object,
            _costHistoryRepoMock.Object,
            _conversionRepoMock.Object,
            _productRepoMock.Object,
            _userRepoMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    #endregion

    #region CreateRecipeAsync Tests

    [Fact]
    public async Task CreateRecipeAsync_WithValidDto_CreatesRecipe()
    {
        // Arrange
        var dto = new CreateRecipeDto
        {
            ProductId = 1,
            Name = "Test Recipe",
            Instructions = "Test instructions",
            RecipeType = RecipeType.Standard,
            YieldQuantity = 4,
            YieldUnit = "portion"
        };

        var product = new Product { Id = 1, Name = "Test Product", Code = "TST001", IsActive = true };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        Recipe? capturedRecipe = null;
        _recipeRepoMock.Setup(r => r.AddAsync(It.IsAny<Recipe>()))
            .Callback<Recipe>(r => { r.Id = 1; capturedRecipe = r; })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateRecipeAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Recipe");
        result.ProductId.Should().Be(1);
        result.YieldQuantity.Should().Be(4);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateRecipeAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.CreateRecipeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("dto");
    }

    [Fact]
    public async Task CreateRecipeAsync_WithNonExistentProduct_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateRecipeDto { ProductId = 999, Name = "Test Recipe" };
        _productRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.CreateRecipeAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Product*999*not found*");
    }

    [Fact]
    public async Task CreateRecipeAsync_WithDuplicateProductRecipe_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateRecipeDto { ProductId = 1, Name = "Test Recipe" };
        var product = new Product { Id = 1, Name = "Test Product", IsActive = true };
        var existingRecipe = new Recipe { Id = 1, ProductId = 1, IsActive = true };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { existingRecipe });

        // Act
        var act = () => _service.CreateRecipeAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has a recipe*");
    }

    [Fact]
    public async Task CreateRecipeAsync_WithIngredients_CreatesRecipeWithIngredients()
    {
        // Arrange
        var dto = new CreateRecipeDto
        {
            ProductId = 1,
            Name = "Recipe with Ingredients",
            Ingredients = new List<CreateRecipeIngredientDto>
            {
                new() { IngredientProductId = 2, Quantity = 100, Unit = RecipeUnitOfMeasure.Gram },
                new() { IngredientProductId = 3, Quantity = 50, Unit = RecipeUnitOfMeasure.Milliliter }
            }
        };

        var product = new Product { Id = 1, Name = "Test Product", IsActive = true };
        var ingredient1 = new Product { Id = 2, Name = "Ingredient 1", CostPrice = 10, IsActive = true };
        var ingredient2 = new Product { Id = 3, Name = "Ingredient 2", CostPrice = 5, IsActive = true };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredient1);
        _productRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(ingredient2);
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        _recipeRepoMock.Setup(r => r.AddAsync(It.IsAny<Recipe>()))
            .Callback<Recipe>(r => r.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateRecipeAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _ingredientRepoMock.Verify(r => r.AddAsync(It.IsAny<RecipeIngredient>()), Times.Exactly(2));
    }

    #endregion

    #region GetRecipeByIdAsync Tests

    [Fact]
    public async Task GetRecipeByIdAsync_WithExistingRecipe_ReturnsRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.GetRecipeByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Recipe");
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithNonExistentRecipe_ReturnsNull()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var result = await _service.GetRecipeByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecipeByIdAsync_WithIngredients_IncludesIngredients()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 100, Unit = RecipeUnitOfMeasure.Gram }
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.GetRecipeByIdAsync(1, includeIngredients: true);

        // Assert
        result.Should().NotBeNull();
        result!.Ingredients.Should().HaveCount(1);
    }

    #endregion

    #region GetRecipeByProductAsync Tests

    [Fact]
    public async Task GetRecipeByProductAsync_WithExistingRecipe_ReturnsRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe", productId: 5);
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.GetRecipeByProductAsync(5);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(5);
    }

    [Fact]
    public async Task GetRecipeByProductAsync_WithNoRecipe_ReturnsNull()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe>());

        // Act
        var result = await _service.GetRecipeByProductAsync(5);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateRecipeAsync Tests

    [Fact]
    public async Task UpdateRecipeAsync_WithValidData_UpdatesRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Original Recipe");
        var dto = new UpdateRecipeDto { Name = "Updated Recipe", YieldQuantity = 8 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

        // Act
        var result = await _service.UpdateRecipeAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Recipe");
        result.YieldQuantity.Should().Be(8);
        _recipeRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRecipeAsync_WithNonExistentRecipe_ThrowsKeyNotFoundException()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

        // Act
        var act = () => _service.UpdateRecipeAsync(999, new UpdateRecipeDto());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateRecipeAsync_IncrementsVersion()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.Version = 1;
        var dto = new UpdateRecipeDto { Name = "Updated Recipe" };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

        // Act
        var result = await _service.UpdateRecipeAsync(1, dto);

        // Assert
        result.Version.Should().Be(2);
    }

    #endregion

    #region DeleteRecipeAsync Tests

    [Fact]
    public async Task DeleteRecipeAsync_WithExistingRecipe_SoftDeletes()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        // Act
        var result = await _service.DeleteRecipeAsync(1);

        // Assert
        result.Should().BeTrue();
        recipe.IsActive.Should().BeFalse();
        _recipeRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_WithNonExistentRecipe_ReturnsFalse()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

        // Act
        var result = await _service.DeleteRecipeAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRecipeAsync_WhenUsedAsSubRecipe_ThrowsInvalidOperationException()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Sub Recipe");
        var parentUsage = new RecipeSubRecipe { Id = 1, ParentRecipeId = 2, SubRecipeId = 1 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe> { parentUsage });

        // Act
        var act = () => _service.DeleteRecipeAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*used as a sub-recipe*");
    }

    #endregion

    #region AddIngredientAsync Tests

    [Fact]
    public async Task AddIngredientAsync_WithValidData_AddsIngredient()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredientProduct = new Product { Id = 2, Name = "Flour", CostPrice = 5, IsActive = true };
        var dto = new CreateRecipeIngredientDto
        {
            IngredientProductId = 2,
            Quantity = 500,
            Unit = RecipeUnitOfMeasure.Gram
        };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredientProduct);
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient>());

        RecipeIngredient? capturedIngredient = null;
        _ingredientRepoMock.Setup(r => r.AddAsync(It.IsAny<RecipeIngredient>()))
            .Callback<RecipeIngredient>(i => { i.Id = 1; capturedIngredient = i; })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddIngredientAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.IngredientProductId.Should().Be(2);
        result.Quantity.Should().Be(500);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddIngredientAsync_WithNonExistentRecipe_ThrowsKeyNotFoundException()
    {
        // Arrange
        _recipeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

        // Act
        var act = () => _service.AddIngredientAsync(999, new CreateRecipeIngredientDto());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AddIngredientAsync_WithDuplicateIngredient_ThrowsInvalidOperationException()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var existingIngredient = new RecipeIngredient { Id = 1, RecipeId = 1, IngredientProductId = 2 };
        var dto = new CreateRecipeIngredientDto { IngredientProductId = 2, Quantity = 100 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(new List<RecipeIngredient> { existingIngredient });

        // Act
        var act = () => _service.AddIngredientAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region UpdateIngredientAsync Tests

    [Fact]
    public async Task UpdateIngredientAsync_WithValidData_UpdatesIngredient()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = new RecipeIngredient
        {
            Id = 1,
            RecipeId = 1,
            IngredientProductId = 2,
            Quantity = 100,
            Unit = RecipeUnitOfMeasure.Gram,
            IsActive = true
        };
        var dto = new UpdateRecipeIngredientDto { Quantity = 200 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _ingredientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ingredient);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Product { Id = 2, CostPrice = 5 });

        // Act
        var result = await _service.UpdateIngredientAsync(1, 1, dto);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(200);
        _ingredientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RecipeIngredient>()), Times.Once);
    }

    [Fact]
    public async Task UpdateIngredientAsync_WithWrongRecipe_ThrowsInvalidOperationException()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = new RecipeIngredient { Id = 1, RecipeId = 999, IngredientProductId = 2 }; // Different recipe

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _ingredientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ingredient);

        // Act
        var act = () => _service.UpdateIngredientAsync(1, 1, new UpdateRecipeIngredientDto());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong*");
    }

    #endregion

    #region RemoveIngredientAsync Tests

    [Fact]
    public async Task RemoveIngredientAsync_WithValidData_RemovesIngredient()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredient = new RecipeIngredient { Id = 1, RecipeId = 1, IngredientProductId = 2, IsActive = true };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _ingredientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ingredient);

        // Act
        var result = await _service.RemoveIngredientAsync(1, 1);

        // Assert
        result.Should().BeTrue();
        _ingredientRepoMock.Verify(r => r.DeleteAsync(It.IsAny<RecipeIngredient>()), Times.Once);
    }

    #endregion

    #region AddSubRecipeAsync Tests

    [Fact]
    public async Task AddSubRecipeAsync_WithValidData_AddsSubRecipe()
    {
        // Arrange
        var parentRecipe = CreateTestRecipe(1, "Parent Recipe");
        var subRecipe = CreateTestRecipe(2, "Sub Recipe", recipeType: RecipeType.SubRecipe);
        var dto = new CreateRecipeSubRecipeDto { SubRecipeId = 2, Quantity = 1 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parentRecipe);
        _recipeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(subRecipe);
        _subRecipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeSubRecipe, bool>>>()))
            .ReturnsAsync(new List<RecipeSubRecipe>());

        RecipeSubRecipe? capturedSubRecipe = null;
        _subRecipeRepoMock.Setup(r => r.AddAsync(It.IsAny<RecipeSubRecipe>()))
            .Callback<RecipeSubRecipe>(sr => { sr.Id = 1; capturedSubRecipe = sr; })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddSubRecipeAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.SubRecipeId.Should().Be(2);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddSubRecipeAsync_WithSameRecipe_ThrowsInvalidOperationException()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var dto = new CreateRecipeSubRecipeDto { SubRecipeId = 1, Quantity = 1 };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

        // Act
        var act = () => _service.AddSubRecipeAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot use itself*");
    }

    #endregion

    #region HasCircularDependencyAsync Tests

    [Fact]
    public async Task HasCircularDependencyAsync_WithNoCircularDependency_ReturnsFalse()
    {
        // Arrange
        var recipe1 = CreateTestRecipe(1, "Recipe 1");
        var recipe2 = CreateTestRecipe(2, "Recipe 2");
        recipe2.SubRecipes = new List<RecipeSubRecipe>();

        _recipeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipe2);

        // Act
        var result = await _service.HasCircularDependencyAsync(1, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_WithDirectCircularDependency_ReturnsTrue()
    {
        // Arrange
        var subRecipeUsage = new RecipeSubRecipe { Id = 1, ParentRecipeId = 2, SubRecipeId = 1 };
        var recipe2 = CreateTestRecipe(2, "Recipe 2");
        recipe2.SubRecipes = new List<RecipeSubRecipe> { subRecipeUsage };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipe2);

        // Act
        var result = await _service.HasCircularDependencyAsync(1, 2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateRecipeAsync Tests

    [Fact]
    public async Task ValidateRecipeAsync_WithValidRecipe_ReturnsSuccess()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Valid Recipe");
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 100, IsActive = true }
        };
        var ingredientProduct = new Product { Id = 2, Name = "Flour", IsActive = true };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredientProduct);

        // Act
        var result = await _service.ValidateRecipeAsync(1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRecipeAsync_WithNoIngredients_ReturnsError()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Empty Recipe");
        recipe.Ingredients = new List<RecipeIngredient>();
        recipe.SubRecipes = new List<RecipeSubRecipe>();

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.ValidateRecipeAsync(1);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no ingredients"));
    }

    [Fact]
    public async Task ValidateRecipeAsync_WithInactiveIngredient_ReturnsWarning()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Recipe with Inactive Ingredient");
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 100, IsActive = true }
        };
        var inactiveProduct = new Product { Id = 2, Name = "Inactive Flour", IsActive = false };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(inactiveProduct);

        // Act
        var result = await _service.ValidateRecipeAsync(1);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("inactive"));
    }

    #endregion

    #region CalculateCostAsync Tests

    [Fact]
    public async Task CalculateCostAsync_WithIngredients_CalculatesTotalCost()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.YieldQuantity = 4;
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 1000, Unit = RecipeUnitOfMeasure.Gram, WastePercent = 0, IsActive = true }
        };
        var ingredient = new Product { Id = 2, Name = "Flour", CostPrice = 10, IsActive = true };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredient);
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.CalculateCostAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RecipeId.Should().Be(1);
        result.TotalCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateCostAsync_WithWastePercent_IncludesWasteInCost()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.YieldQuantity = 1;
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 100, Unit = RecipeUnitOfMeasure.Gram, WastePercent = 10, IsActive = true }
        };
        var ingredient = new Product { Id = 2, Name = "Vegetable", CostPrice = 5, IsActive = true };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ingredient);
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act
        var result = await _service.CalculateCostAsync(1);

        // Assert
        // With 10% waste, effective quantity is 110g instead of 100g
        var ingredientCost = result.IngredientCosts.First();
        ingredientCost.EffectiveQuantity.Should().Be(110);
    }

    #endregion

    #region ApproveRecipeAsync Tests

    [Fact]
    public async Task ApproveRecipeAsync_WithValidData_ApprovesRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.IsApproved = false;
        var user = new User { Id = 1, FirstName = "Admin", LastName = "User" };
        var dto = new ApproveRecipeDto { RecipeId = 1, Approved = true, Notes = "Approved" };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _service.ApproveRecipeAsync(dto, 1);

        // Assert
        result.IsApproved.Should().BeTrue();
        result.ApprovedByUserName.Should().Be("Admin User");
    }

    [Fact]
    public async Task ApproveRecipeAsync_WithUnapprove_UnaprovesRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        recipe.IsApproved = true;
        recipe.ApprovedByUserId = 1;
        var dto = new ApproveRecipeDto { RecipeId = 1, Approved = false };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });

        // Act
        var result = await _service.ApproveRecipeAsync(dto, 1);

        // Assert
        result.IsApproved.Should().BeFalse();
        result.ApprovedByUserName.Should().BeNull();
    }

    #endregion

    #region CloneRecipeAsync Tests

    [Fact]
    public async Task CloneRecipeAsync_WithValidRecipe_ClonesRecipe()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Original Recipe");
        recipe.Instructions = "Test instructions";
        recipe.Ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, IngredientProductId = 2, Quantity = 100, Unit = RecipeUnitOfMeasure.Gram, IsActive = true }
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Product { Id = 1, Name = "Product", IsActive = true });

        _recipeRepoMock.Setup(r => r.AddAsync(It.IsAny<Recipe>()))
            .Callback<Recipe>(r => r.Id = 2)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CloneRecipeAsync(1, "Cloned Recipe");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Cloned Recipe");
        result.Id.Should().NotBe(1);
    }

    [Fact]
    public async Task CloneRecipeAsync_WithNewProduct_LinksToNewProduct()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Original Recipe", productId: 1);
        var newProduct = new Product { Id = 5, Name = "New Product", IsActive = true };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(new List<Recipe> { recipe });
        _productRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(newProduct);
        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Original Product", IsActive = true });

        _recipeRepoMock.Setup(r => r.AddAsync(It.IsAny<Recipe>()))
            .Callback<Recipe>(r => r.Id = 2)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CloneRecipeAsync(1, "Cloned Recipe", newProductId: 5);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(5);
    }

    #endregion

    #region GetAllRecipesAsync Tests

    [Fact]
    public async Task GetAllRecipesAsync_ReturnsActiveRecipes()
    {
        // Arrange
        var recipes = new List<Recipe>
        {
            CreateTestRecipe(1, "Recipe 1"),
            CreateTestRecipe(2, "Recipe 2")
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(recipes);

        // Act
        var result = await _service.GetAllRecipesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllRecipesAsync_IncludeInactive_ReturnsAllRecipes()
    {
        // Arrange
        var recipes = new List<Recipe>
        {
            CreateTestRecipe(1, "Active Recipe"),
            CreateTestRecipe(2, "Inactive Recipe", isActive: false)
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(recipes);

        // Act
        var result = await _service.GetAllRecipesAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Unit Conversion Tests

    [Fact]
    public async Task CreateUnitConversionAsync_WithValidData_CreatesConversion()
    {
        // Arrange
        var dto = new CreateUnitConversionDto
        {
            FromUnit = RecipeUnitOfMeasure.Gram,
            ToUnit = RecipeUnitOfMeasure.Kilogram,
            ConversionFactor = 0.001m
        };

        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());
        _conversionRepoMock.Setup(r => r.AddAsync(It.IsAny<UnitConversion>()))
            .Callback<UnitConversion>(c => c.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateUnitConversionAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FromUnit.Should().Be(RecipeUnitOfMeasure.Gram);
        result.ToUnit.Should().Be(RecipeUnitOfMeasure.Kilogram);
        result.ConversionFactor.Should().Be(0.001m);
    }

    [Fact]
    public async Task ConvertUnitsAsync_WithKnownConversion_ConvertsCorrectly()
    {
        // Arrange
        _conversionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnitConversion, bool>>>()))
            .ReturnsAsync(new List<UnitConversion>());

        // Act - Using standard conversion (Gram to Kilogram = 0.001)
        var result = await _service.ConvertUnitsAsync(1000, RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Kilogram);

        // Assert
        result.Should().Be(1); // 1000g = 1kg
    }

    [Fact]
    public async Task ConvertUnitsAsync_WithSameUnit_ReturnsSameValue()
    {
        // Act
        var result = await _service.ConvertUnitsAsync(500, RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Gram);

        // Assert
        result.Should().Be(500);
    }

    #endregion

    #region GetRecipeSummaryAsync Tests

    [Fact]
    public async Task GetRecipeSummaryAsync_ReturnsCompleteSummary()
    {
        // Arrange
        var recipes = new List<Recipe>
        {
            CreateTestRecipe(1, "Standard Recipe", recipeType: RecipeType.Standard, isApproved: true),
            CreateTestRecipe(2, "Sub Recipe", recipeType: RecipeType.SubRecipe, isApproved: false),
            CreateTestRecipe(3, "Batch Recipe", recipeType: RecipeType.BatchPrep, isApproved: true)
        };

        _recipeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
            .ReturnsAsync(recipes);

        // Act
        var result = await _service.GetRecipeSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRecipes.Should().Be(3);
        result.ApprovedRecipes.Should().Be(2);
        result.PendingApproval.Should().Be(1);
        result.StandardRecipes.Should().Be(1);
        result.SubRecipes.Should().Be(1);
        result.BatchPrepRecipes.Should().Be(1);
    }

    #endregion

    #region ReorderIngredientsAsync Tests

    [Fact]
    public async Task ReorderIngredientsAsync_WithValidOrder_UpdatesSortOrder()
    {
        // Arrange
        var recipe = CreateTestRecipe(1, "Test Recipe");
        var ingredients = new List<RecipeIngredient>
        {
            new() { Id = 1, RecipeId = 1, SortOrder = 0, IsActive = true },
            new() { Id = 2, RecipeId = 1, SortOrder = 1, IsActive = true },
            new() { Id = 3, RecipeId = 1, SortOrder = 2, IsActive = true }
        };

        _recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
        _ingredientRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecipeIngredient, bool>>>()))
            .ReturnsAsync(ingredients);

        // Act - Reorder to 3, 1, 2
        await _service.ReorderIngredientsAsync(1, new List<int> { 3, 1, 2 });

        // Assert
        _ingredientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RecipeIngredient>()), Times.Exactly(3));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void RecipeDto_FoodCostPercent_CalculatesCorrectly()
    {
        // Arrange
        var dto = new RecipeDto
        {
            EstimatedCostPerPortion = 5,
            SellingPrice = 20
        };

        // Act & Assert
        dto.FoodCostPercent.Should().Be(25); // 5/20 * 100 = 25%
    }

    [Fact]
    public void RecipeDto_FoodCostPercent_WithZeroPrice_ReturnsZero()
    {
        // Arrange
        var dto = new RecipeDto
        {
            EstimatedCostPerPortion = 5,
            SellingPrice = 0
        };

        // Act & Assert
        dto.FoodCostPercent.Should().Be(0);
    }

    [Fact]
    public void RecipeIngredientDto_EffectiveQuantity_CalculatesCorrectly()
    {
        // Arrange
        var dto = new RecipeIngredientDto
        {
            Quantity = 100,
            WastePercent = 15
        };

        // Act & Assert - Note: DTO has EffectiveQuantity as property, not calculated
        // Entity calculates: Quantity * (1 + WastePercent/100) = 100 * 1.15 = 115
        var entity = new RecipeIngredient { Quantity = 100, WastePercent = 15 };
        entity.EffectiveQuantity.Should().Be(115);
    }

    [Fact]
    public void RecipeValidationResultDto_Success_CreatesValidResult()
    {
        // Act
        var result = RecipeValidationResultDto.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void RecipeValidationResultDto_Failure_CreatesInvalidResult()
    {
        // Act
        var result = RecipeValidationResultDto.Failure("Error 1", "Error 2");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    #endregion

    #region Helper Methods

    private static Recipe CreateTestRecipe(
        int id,
        string name,
        int productId = 1,
        RecipeType recipeType = RecipeType.Standard,
        bool isApproved = true,
        bool isActive = true)
    {
        return new Recipe
        {
            Id = id,
            ProductId = productId,
            Name = name,
            RecipeType = recipeType,
            YieldQuantity = 1,
            YieldUnit = "portion",
            IsApproved = isApproved,
            IsActive = isActive,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            Product = new Product { Id = productId, Name = $"Product {productId}", Code = $"P{productId:D3}", SellingPrice = 10, CostPrice = 5 },
            Ingredients = new List<RecipeIngredient>(),
            SubRecipes = new List<RecipeSubRecipe>()
        };
    }

    #endregion
}
