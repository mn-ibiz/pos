using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for recipe management.
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly IRepository<Recipe> _recipeRepository;
    private readonly IRepository<RecipeIngredient> _ingredientRepository;
    private readonly IRepository<RecipeSubRecipe> _subRecipeRepository;
    private readonly IRepository<RecipeCostHistory> _costHistoryRepository;
    private readonly IRepository<UnitConversion> _conversionRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Inventory> _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        IRepository<Recipe> recipeRepository,
        IRepository<RecipeIngredient> ingredientRepository,
        IRepository<RecipeSubRecipe> subRecipeRepository,
        IRepository<RecipeCostHistory> costHistoryRepository,
        IRepository<UnitConversion> conversionRepository,
        IRepository<Product> productRepository,
        IRepository<Inventory> inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<RecipeService> logger)
    {
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        _subRecipeRepository = subRecipeRepository ?? throw new ArgumentNullException(nameof(subRecipeRepository));
        _costHistoryRepository = costHistoryRepository ?? throw new ArgumentNullException(nameof(costHistoryRepository));
        _conversionRepository = conversionRepository ?? throw new ArgumentNullException(nameof(conversionRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Recipe CRUD

    public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
    {
        var validation = await ValidateCreateRecipeAsync(dto);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Recipe validation failed: {string.Join(", ", validation.Errors)}");
        }

        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product {dto.ProductId} not found");
        }

        var recipe = new Recipe
        {
            ProductId = dto.ProductId,
            Name = dto.Name,
            Instructions = dto.Instructions,
            RecipeType = dto.RecipeType,
            YieldQuantity = dto.YieldQuantity,
            YieldUnit = dto.YieldUnit,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookTimeMinutes = dto.CookTimeMinutes,
            Notes = dto.Notes,
            IsApproved = false,
            Version = 1
        };

        await _recipeRepository.AddAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        // Add ingredients if provided
        if (dto.Ingredients?.Any() == true)
        {
            foreach (var ingredientDto in dto.Ingredients)
            {
                await AddIngredientInternalAsync(recipe.Id, ingredientDto);
            }
        }

        // Add sub-recipes if provided
        if (dto.SubRecipes?.Any() == true)
        {
            foreach (var subRecipeDto in dto.SubRecipes)
            {
                await AddSubRecipeInternalAsync(recipe.Id, subRecipeDto);
            }
        }

        // Calculate initial cost
        await CalculateCostInternalAsync(recipe.Id);

        _logger.LogInformation("Created recipe {RecipeId} for product {ProductId}", recipe.Id, dto.ProductId);

        return (await GetRecipeByIdAsync(recipe.Id))!;
    }

    public async Task<RecipeDto?> GetRecipeByIdAsync(int id, bool includeIngredients = true)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null || !recipe.IsActive)
        {
            return null;
        }

        return await MapToRecipeDto(recipe, includeIngredients);
    }

    public async Task<RecipeDto?> GetRecipeByProductAsync(int productId)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.ProductId == productId && r.IsActive);
        var recipe = recipes.FirstOrDefault();

        if (recipe == null)
        {
            return null;
        }

        return await MapToRecipeDto(recipe, includeIngredients: true);
    }

    public async Task<RecipeDto> UpdateRecipeAsync(int id, UpdateRecipeDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null || !recipe.IsActive)
        {
            throw new InvalidOperationException($"Recipe {id} not found");
        }

        if (dto.Name != null) recipe.Name = dto.Name;
        if (dto.Instructions != null) recipe.Instructions = dto.Instructions;
        if (dto.RecipeType.HasValue) recipe.RecipeType = dto.RecipeType.Value;
        if (dto.YieldQuantity.HasValue) recipe.YieldQuantity = dto.YieldQuantity.Value;
        if (dto.YieldUnit != null) recipe.YieldUnit = dto.YieldUnit;
        if (dto.PrepTimeMinutes.HasValue) recipe.PrepTimeMinutes = dto.PrepTimeMinutes.Value;
        if (dto.CookTimeMinutes.HasValue) recipe.CookTimeMinutes = dto.CookTimeMinutes.Value;
        if (dto.Notes != null) recipe.Notes = dto.Notes;
        if (dto.IsActive.HasValue) recipe.IsActive = dto.IsActive.Value;

        recipe.Version++;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated recipe {RecipeId}", id);

        return (await GetRecipeByIdAsync(id))!;
    }

    public async Task<bool> DeleteRecipeAsync(int id)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
        {
            return false;
        }

        // Check if used as sub-recipe
        var usages = await _subRecipeRepository.FindAsync(sr => sr.SubRecipeId == id && sr.IsActive);
        if (usages.Any())
        {
            throw new InvalidOperationException($"Recipe is used as a sub-recipe in {usages.Count()} other recipes");
        }

        recipe.IsActive = false;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted recipe {RecipeId}", id);

        return true;
    }

    public async Task<List<RecipeListDto>> QueryRecipesAsync(RecipeQueryDto query)
    {
        var recipes = await _recipeRepository.FindAsync(r =>
            (query.IncludeInactive || r.IsActive) &&
            (!query.ProductId.HasValue || r.ProductId == query.ProductId.Value) &&
            (!query.RecipeType.HasValue || r.RecipeType == query.RecipeType.Value) &&
            (!query.IsApproved.HasValue || r.IsApproved == query.IsApproved.Value));

        var filteredRecipes = recipes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            filteredRecipes = filteredRecipes.Where(r =>
                r.Name.ToLower().Contains(term));
        }

        var sorted = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending
                ? filteredRecipes.OrderByDescending(r => r.Name)
                : filteredRecipes.OrderBy(r => r.Name),
            "cost" => query.SortDescending
                ? filteredRecipes.OrderByDescending(r => r.EstimatedCostPerPortion)
                : filteredRecipes.OrderBy(r => r.EstimatedCostPerPortion),
            "updated" => query.SortDescending
                ? filteredRecipes.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                : filteredRecipes.OrderBy(r => r.UpdatedAt ?? r.CreatedAt),
            _ => filteredRecipes.OrderBy(r => r.Name)
        };

        var result = new List<RecipeListDto>();
        foreach (var recipe in sorted.Skip(query.Skip).Take(query.Take))
        {
            result.Add(await MapToRecipeListDto(recipe));
        }

        return result;
    }

    public async Task<List<RecipeListDto>> GetAllRecipesAsync(bool includeInactive = false)
    {
        var recipes = await _recipeRepository.FindAsync(r => includeInactive || r.IsActive);

        var result = new List<RecipeListDto>();
        foreach (var recipe in recipes.OrderBy(r => r.Name))
        {
            result.Add(await MapToRecipeListDto(recipe));
        }

        return result;
    }

    #endregion

    #region Ingredient Management

    public async Task<RecipeIngredientDto> AddIngredientAsync(int recipeId, CreateRecipeIngredientDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null || !recipe.IsActive)
        {
            throw new InvalidOperationException($"Recipe {recipeId} not found");
        }

        var ingredient = await AddIngredientInternalAsync(recipeId, dto);
        await CalculateCostInternalAsync(recipeId);

        return await MapToIngredientDto(ingredient);
    }

    private async Task<RecipeIngredient> AddIngredientInternalAsync(int recipeId, CreateRecipeIngredientDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.IngredientProductId);
        if (product == null)
        {
            throw new InvalidOperationException($"Ingredient product {dto.IngredientProductId} not found");
        }

        var ingredient = new RecipeIngredient
        {
            RecipeId = recipeId,
            IngredientProductId = dto.IngredientProductId,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            CustomUnit = dto.CustomUnit,
            WastePercent = dto.WastePercent,
            IsOptional = dto.IsOptional,
            SortOrder = dto.SortOrder,
            PrepNotes = dto.PrepNotes
        };

        // Calculate cost
        ingredient.CalculatedCost = CalculateIngredientCost(ingredient, product.CostPrice ?? 0);

        await _ingredientRepository.AddAsync(ingredient);
        await _unitOfWork.SaveChangesAsync();

        return ingredient;
    }

    public async Task<RecipeIngredientDto> UpdateIngredientAsync(int recipeId, int ingredientId, UpdateRecipeIngredientDto dto)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
        if (ingredient == null || ingredient.RecipeId != recipeId || !ingredient.IsActive)
        {
            throw new InvalidOperationException($"Ingredient {ingredientId} not found in recipe {recipeId}");
        }

        if (dto.Quantity.HasValue) ingredient.Quantity = dto.Quantity.Value;
        if (dto.Unit.HasValue) ingredient.Unit = dto.Unit.Value;
        if (dto.CustomUnit != null) ingredient.CustomUnit = dto.CustomUnit;
        if (dto.WastePercent.HasValue) ingredient.WastePercent = dto.WastePercent.Value;
        if (dto.IsOptional.HasValue) ingredient.IsOptional = dto.IsOptional.Value;
        if (dto.SortOrder.HasValue) ingredient.SortOrder = dto.SortOrder.Value;
        if (dto.PrepNotes != null) ingredient.PrepNotes = dto.PrepNotes;

        var product = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
        ingredient.CalculatedCost = CalculateIngredientCost(ingredient, product?.CostPrice ?? 0);
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _ingredientRepository.UpdateAsync(ingredient);
        await _unitOfWork.SaveChangesAsync();

        await CalculateCostInternalAsync(recipeId);

        return await MapToIngredientDto(ingredient);
    }

    public async Task<bool> RemoveIngredientAsync(int recipeId, int ingredientId)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
        if (ingredient == null || ingredient.RecipeId != recipeId)
        {
            return false;
        }

        ingredient.IsActive = false;
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _ingredientRepository.UpdateAsync(ingredient);
        await _unitOfWork.SaveChangesAsync();

        await CalculateCostInternalAsync(recipeId);

        return true;
    }

    public async Task<List<RecipeIngredientDto>> GetIngredientsAsync(int recipeId)
    {
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipeId && i.IsActive);

        var result = new List<RecipeIngredientDto>();
        foreach (var ingredient in ingredients.OrderBy(i => i.SortOrder))
        {
            result.Add(await MapToIngredientDto(ingredient));
        }

        return result;
    }

    public async Task ReorderIngredientsAsync(int recipeId, List<int> ingredientIds)
    {
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipeId && i.IsActive);

        for (int i = 0; i < ingredientIds.Count; i++)
        {
            var ingredient = ingredients.FirstOrDefault(ing => ing.Id == ingredientIds[i]);
            if (ingredient != null)
            {
                ingredient.SortOrder = i;
                ingredient.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Sub-Recipe Management

    public async Task<RecipeSubRecipeDto> AddSubRecipeAsync(int recipeId, CreateRecipeSubRecipeDto dto)
    {
        if (await HasCircularDependencyAsync(recipeId, dto.SubRecipeId))
        {
            throw new InvalidOperationException("Adding this sub-recipe would create a circular dependency");
        }

        var subRecipe = await AddSubRecipeInternalAsync(recipeId, dto);
        await CalculateCostInternalAsync(recipeId);

        return await MapToSubRecipeDto(subRecipe);
    }

    private async Task<RecipeSubRecipe> AddSubRecipeInternalAsync(int recipeId, CreateRecipeSubRecipeDto dto)
    {
        var subRecipeEntity = await _recipeRepository.GetByIdAsync(dto.SubRecipeId);
        if (subRecipeEntity == null || !subRecipeEntity.IsActive)
        {
            throw new InvalidOperationException($"Sub-recipe {dto.SubRecipeId} not found");
        }

        var subRecipe = new RecipeSubRecipe
        {
            ParentRecipeId = recipeId,
            SubRecipeId = dto.SubRecipeId,
            Quantity = dto.Quantity,
            SortOrder = dto.SortOrder,
            Notes = dto.Notes,
            CalculatedCost = subRecipeEntity.EstimatedCostPerPortion * dto.Quantity
        };

        await _subRecipeRepository.AddAsync(subRecipe);
        await _unitOfWork.SaveChangesAsync();

        return subRecipe;
    }

    public async Task<bool> RemoveSubRecipeAsync(int recipeId, int subRecipeId)
    {
        var subRecipes = await _subRecipeRepository.FindAsync(sr =>
            sr.ParentRecipeId == recipeId && sr.SubRecipeId == subRecipeId && sr.IsActive);

        var subRecipe = subRecipes.FirstOrDefault();
        if (subRecipe == null)
        {
            return false;
        }

        subRecipe.IsActive = false;
        subRecipe.UpdatedAt = DateTime.UtcNow;

        await _subRecipeRepository.UpdateAsync(subRecipe);
        await _unitOfWork.SaveChangesAsync();

        await CalculateCostInternalAsync(recipeId);

        return true;
    }

    public async Task<List<RecipeSubRecipeDto>> GetSubRecipesAsync(int recipeId)
    {
        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipeId && sr.IsActive);

        var result = new List<RecipeSubRecipeDto>();
        foreach (var sr in subRecipes.OrderBy(s => s.SortOrder))
        {
            result.Add(await MapToSubRecipeDto(sr));
        }

        return result;
    }

    public async Task<List<RecipeListDto>> GetRecipesUsingSubRecipeAsync(int subRecipeId)
    {
        var usages = await _subRecipeRepository.FindAsync(sr => sr.SubRecipeId == subRecipeId && sr.IsActive);

        var result = new List<RecipeListDto>();
        foreach (var usage in usages)
        {
            var recipe = await _recipeRepository.GetByIdAsync(usage.ParentRecipeId);
            if (recipe != null && recipe.IsActive)
            {
                result.Add(await MapToRecipeListDto(recipe));
            }
        }

        return result;
    }

    #endregion

    #region Validation

    public async Task<RecipeValidationResultDto> ValidateRecipeAsync(int recipeId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return RecipeValidationResultDto.Failure("Recipe not found");
        }

        var result = new RecipeValidationResultDto { IsValid = true };

        // Check for ingredients
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipeId && i.IsActive);
        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipeId && sr.IsActive);

        if (!ingredients.Any() && !subRecipes.Any())
        {
            result.Warnings.Add("Recipe has no ingredients or sub-recipes");
        }

        // Check ingredient quantities
        foreach (var ingredient in ingredients)
        {
            if (ingredient.Quantity <= 0)
            {
                result.Errors.Add($"Ingredient has invalid quantity: {ingredient.Quantity}");
                result.IsValid = false;
            }

            var product = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
            if (product == null || !product.IsActive)
            {
                result.HasMissingIngredients = true;
                result.MissingIngredients.Add($"Product ID {ingredient.IngredientProductId}");
                result.Errors.Add($"Ingredient product not found: {ingredient.IngredientProductId}");
                result.IsValid = false;
            }
        }

        // Check for circular dependencies
        foreach (var sr in subRecipes)
        {
            if (await HasCircularDependencyAsync(recipeId, sr.SubRecipeId))
            {
                result.HasCircularDependency = true;
                result.Errors.Add($"Circular dependency detected with sub-recipe {sr.SubRecipeId}");
                result.IsValid = false;
            }
        }

        return result;
    }

    public async Task<RecipeValidationResultDto> ValidateCreateRecipeAsync(CreateRecipeDto dto)
    {
        var result = new RecipeValidationResultDto { IsValid = true };

        // Check product exists
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null)
        {
            result.Errors.Add("Product not found");
            result.IsValid = false;
        }

        // Check name
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.Errors.Add("Recipe name is required");
            result.IsValid = false;
        }

        // Check yield
        if (dto.YieldQuantity <= 0)
        {
            result.Errors.Add("Yield quantity must be positive");
            result.IsValid = false;
        }

        // Check existing recipe for product
        var existingRecipes = await _recipeRepository.FindAsync(r => r.ProductId == dto.ProductId && r.IsActive);
        if (existingRecipes.Any())
        {
            result.Warnings.Add("Product already has a recipe");
        }

        return result;
    }

    public async Task<bool> HasCircularDependencyAsync(int recipeId, int subRecipeId)
    {
        if (recipeId == subRecipeId)
        {
            return true;
        }

        var visited = new HashSet<int> { recipeId };
        return await CheckCircularDependency(subRecipeId, visited);
    }

    private async Task<bool> CheckCircularDependency(int recipeId, HashSet<int> visited)
    {
        if (visited.Contains(recipeId))
        {
            return true;
        }

        visited.Add(recipeId);

        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipeId && sr.IsActive);

        foreach (var sr in subRecipes)
        {
            if (await CheckCircularDependency(sr.SubRecipeId, visited))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Approval

    public async Task<RecipeDto> ApproveRecipeAsync(ApproveRecipeDto dto, int userId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(dto.RecipeId);
        if (recipe == null)
        {
            throw new InvalidOperationException($"Recipe {dto.RecipeId} not found");
        }

        recipe.IsApproved = dto.Approved;
        recipe.ApprovedByUserId = dto.Approved ? userId : null;
        recipe.ApprovedAt = dto.Approved ? DateTime.UtcNow : null;
        recipe.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.Notes))
        {
            recipe.Notes = (recipe.Notes ?? "") + $"\n[Approval: {dto.Notes}]";
        }

        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Recipe {RecipeId} {Action} by user {UserId}",
            dto.RecipeId, dto.Approved ? "approved" : "unapproved", userId);

        return (await GetRecipeByIdAsync(dto.RecipeId))!;
    }

    public async Task<List<RecipeListDto>> GetPendingApprovalAsync()
    {
        var recipes = await _recipeRepository.FindAsync(r => !r.IsApproved && r.IsActive);

        var result = new List<RecipeListDto>();
        foreach (var recipe in recipes)
        {
            result.Add(await MapToRecipeListDto(recipe));
        }

        return result;
    }

    #endregion

    #region Costing

    public async Task<RecipeCostDto> CalculateCostAsync(int recipeId)
    {
        return await CalculateCostInternalAsync(recipeId);
    }

    private async Task<RecipeCostDto> CalculateCostInternalAsync(int recipeId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new InvalidOperationException($"Recipe {recipeId} not found");
        }

        var costDto = new RecipeCostDto
        {
            RecipeId = recipeId,
            RecipeName = recipe.Name,
            YieldQuantity = recipe.YieldQuantity,
            CalculatedAt = DateTime.UtcNow
        };

        decimal totalCost = 0;

        // Calculate ingredient costs with unit conversion
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipeId && i.IsActive);
        foreach (var ingredient in ingredients)
        {
            var product = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
            var unitCost = product?.CostPrice ?? 0;
            var productUnit = product?.UnitOfMeasure ?? "Each";

            // Use unit conversion for accurate costing
            var ingredientCost = await CalculateIngredientCostWithConversionAsync(
                ingredient, unitCost, productUnit);

            ingredient.CalculatedCost = ingredientCost;
            totalCost += ingredientCost;

            costDto.IngredientCosts.Add(new IngredientCostDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                IngredientName = product?.Name ?? "Unknown",
                Quantity = ingredient.Quantity,
                EffectiveQuantity = ingredient.EffectiveQuantity,
                Unit = ingredient.UnitDisplayName,
                UnitCost = unitCost,
                TotalCost = ingredientCost
            });
        }

        // Calculate sub-recipe costs
        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipeId && sr.IsActive);
        foreach (var sr in subRecipes)
        {
            var subRecipe = await _recipeRepository.GetByIdAsync(sr.SubRecipeId);
            if (subRecipe != null)
            {
                var subRecipeCost = subRecipe.EstimatedCostPerPortion * sr.Quantity;
                sr.CalculatedCost = subRecipeCost;
                totalCost += subRecipeCost;

                costDto.SubRecipeCosts.Add(new SubRecipeCostDto
                {
                    SubRecipeId = sr.SubRecipeId,
                    SubRecipeName = subRecipe.Name,
                    Quantity = sr.Quantity,
                    CostPerPortion = subRecipe.EstimatedCostPerPortion,
                    TotalCost = subRecipeCost
                });
            }
        }

        costDto.TotalCost = totalCost;
        costDto.CostPerPortion = recipe.YieldQuantity > 0 ? totalCost / recipe.YieldQuantity : totalCost;

        // Get selling price
        var menuProduct = await _productRepository.GetByIdAsync(recipe.ProductId);
        if (menuProduct != null)
        {
            costDto.SellingPrice = menuProduct.SellingPrice;
            costDto.FoodCostPercent = menuProduct.SellingPrice > 0
                ? costDto.CostPerPortion / menuProduct.SellingPrice * 100
                : 0;
            costDto.GrossMargin = menuProduct.SellingPrice - costDto.CostPerPortion;
            costDto.GrossMarginPercent = menuProduct.SellingPrice > 0
                ? costDto.GrossMargin / menuProduct.SellingPrice * 100
                : 0;
        }

        // Calculate percentages
        foreach (var ic in costDto.IngredientCosts)
        {
            ic.PercentOfTotal = totalCost > 0 ? ic.TotalCost / totalCost * 100 : 0;
        }
        foreach (var sc in costDto.SubRecipeCosts)
        {
            sc.PercentOfTotal = totalCost > 0 ? sc.TotalCost / totalCost * 100 : 0;
        }

        // Update recipe
        recipe.TotalEstimatedCost = totalCost;
        recipe.EstimatedCostPerPortion = costDto.CostPerPortion;
        recipe.LastCostCalculation = DateTime.UtcNow;

        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        return costDto;
    }

    private async Task<decimal> CalculateIngredientCostWithConversionAsync(
        RecipeIngredient ingredient,
        decimal unitCost,
        string productUnitOfMeasure)
    {
        var conversionFactor = await GetConversionFactorAsync(
            ingredient.Unit,
            productUnitOfMeasure,
            ingredient.IngredientProductId);

        return ingredient.EffectiveQuantity * unitCost * conversionFactor;
    }

    private async Task<decimal> GetConversionFactorAsync(
        RecipeUnitOfMeasure ingredientUnit,
        string productUnitOfMeasure,
        int productId)
    {
        // Parse the product's unit of measure to a RecipeUnitOfMeasure
        var productUnit = ParseProductUnitOfMeasure(productUnitOfMeasure);

        // If units are the same, no conversion needed
        if (ingredientUnit == productUnit)
        {
            return 1m;
        }

        // Check for product-specific conversion first
        var conversions = await _conversionRepository.FindAsync(
            c => c.FromUnit == ingredientUnit &&
                 c.ToUnit == productUnit &&
                 (c.ProductId == productId || c.ProductId == null));

        var conversion = conversions
            .OrderByDescending(c => c.ProductId.HasValue) // Prefer product-specific
            .FirstOrDefault();

        if (conversion != null)
        {
            return conversion.ConversionFactor;
        }

        // Use standard conversions as fallback
        return GetStandardConversionFactor(ingredientUnit, productUnit);
    }

    private static RecipeUnitOfMeasure ParseProductUnitOfMeasure(string unitOfMeasure)
    {
        // Map common product unit strings to RecipeUnitOfMeasure enum
        return unitOfMeasure?.ToLowerInvariant() switch
        {
            "g" or "gram" or "grams" => RecipeUnitOfMeasure.Gram,
            "kg" or "kilogram" or "kilograms" => RecipeUnitOfMeasure.Kilogram,
            "ml" or "milliliter" or "milliliters" => RecipeUnitOfMeasure.Milliliter,
            "l" or "liter" or "liters" or "litre" or "litres" => RecipeUnitOfMeasure.Liter,
            "pc" or "pcs" or "piece" or "pieces" or "each" or "unit" or "units" => RecipeUnitOfMeasure.Piece,
            "tsp" or "teaspoon" or "teaspoons" => RecipeUnitOfMeasure.Teaspoon,
            "tbsp" or "tablespoon" or "tablespoons" => RecipeUnitOfMeasure.Tablespoon,
            "cup" or "cups" => RecipeUnitOfMeasure.Cup,
            "oz" or "ounce" or "ounces" => RecipeUnitOfMeasure.Ounce,
            "lb" or "pound" or "pounds" => RecipeUnitOfMeasure.Pound,
            "slice" or "slices" => RecipeUnitOfMeasure.Slice,
            "portion" or "portions" => RecipeUnitOfMeasure.Portion,
            _ => RecipeUnitOfMeasure.Piece // Default to piece for unknown units
        };
    }

    private static decimal GetStandardConversionFactor(RecipeUnitOfMeasure from, RecipeUnitOfMeasure to)
    {
        // Standard metric conversions (from -> to = multiply by factor)
        return (from, to) switch
        {
            // Weight conversions
            (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Kilogram) => 0.001m,
            (RecipeUnitOfMeasure.Kilogram, RecipeUnitOfMeasure.Gram) => 1000m,
            (RecipeUnitOfMeasure.Ounce, RecipeUnitOfMeasure.Gram) => 28.3495m,
            (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Ounce) => 0.035274m,
            (RecipeUnitOfMeasure.Pound, RecipeUnitOfMeasure.Gram) => 453.592m,
            (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Pound) => 0.00220462m,
            (RecipeUnitOfMeasure.Pound, RecipeUnitOfMeasure.Kilogram) => 0.453592m,
            (RecipeUnitOfMeasure.Kilogram, RecipeUnitOfMeasure.Pound) => 2.20462m,
            (RecipeUnitOfMeasure.Ounce, RecipeUnitOfMeasure.Kilogram) => 0.0283495m,
            (RecipeUnitOfMeasure.Kilogram, RecipeUnitOfMeasure.Ounce) => 35.274m,

            // Volume conversions
            (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Liter) => 0.001m,
            (RecipeUnitOfMeasure.Liter, RecipeUnitOfMeasure.Milliliter) => 1000m,
            (RecipeUnitOfMeasure.Teaspoon, RecipeUnitOfMeasure.Milliliter) => 4.92892m,
            (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Teaspoon) => 0.202884m,
            (RecipeUnitOfMeasure.Tablespoon, RecipeUnitOfMeasure.Milliliter) => 14.7868m,
            (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Tablespoon) => 0.067628m,
            (RecipeUnitOfMeasure.Cup, RecipeUnitOfMeasure.Milliliter) => 236.588m,
            (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Cup) => 0.00422675m,
            (RecipeUnitOfMeasure.Cup, RecipeUnitOfMeasure.Liter) => 0.236588m,
            (RecipeUnitOfMeasure.Liter, RecipeUnitOfMeasure.Cup) => 4.22675m,
            (RecipeUnitOfMeasure.Teaspoon, RecipeUnitOfMeasure.Tablespoon) => 0.333333m,
            (RecipeUnitOfMeasure.Tablespoon, RecipeUnitOfMeasure.Teaspoon) => 3m,

            // Small quantities (approximate)
            (RecipeUnitOfMeasure.Pinch, RecipeUnitOfMeasure.Teaspoon) => 0.0625m, // 1/16 tsp
            (RecipeUnitOfMeasure.Teaspoon, RecipeUnitOfMeasure.Pinch) => 16m,
            (RecipeUnitOfMeasure.Dash, RecipeUnitOfMeasure.Milliliter) => 0.616m,
            (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Dash) => 1.623m,

            // If no conversion found, return 1 and log warning
            _ => 1m
        };
    }

    /// <summary>
    /// Legacy method for backward compatibility - calls async version.
    /// </summary>
    private static decimal CalculateIngredientCost(RecipeIngredient ingredient, decimal unitCost)
    {
        // Simple multiplication without conversion - legacy fallback
        return ingredient.EffectiveQuantity * unitCost;
    }

    public async Task<int> RecalculateAllCostsAsync()
    {
        var recipes = await _recipeRepository.FindAsync(r => r.IsActive);
        var count = 0;

        foreach (var recipe in recipes)
        {
            try
            {
                await CalculateCostInternalAsync(recipe.Id);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to recalculate cost for recipe {RecipeId}", recipe.Id);
            }
        }

        _logger.LogInformation("Recalculated costs for {Count} recipes", count);

        return count;
    }

    public async Task<List<RecipeCostHistoryDto>> GetCostHistoryAsync(int recipeId, int limit = 20)
    {
        var history = await _costHistoryRepository.FindAsync(h => h.RecipeId == recipeId);

        var ordered = history.OrderByDescending(h => h.CreatedAt).Take(limit).ToList();
        var result = new List<RecipeCostHistoryDto>();

        RecipeCostHistoryDto? previous = null;
        foreach (var h in ordered)
        {
            var dto = new RecipeCostHistoryDto
            {
                Id = h.Id,
                RecipeId = h.RecipeId,
                CostPerPortion = h.CostPerPortion,
                TotalCost = h.TotalCost,
                CalculationReason = h.CalculationReason,
                CreatedAt = h.CreatedAt,
                ChangePercent = previous != null && previous.CostPerPortion > 0
                    ? (h.CostPerPortion - previous.CostPerPortion) / previous.CostPerPortion * 100
                    : 0
            };

            result.Add(dto);
            previous = dto;
        }

        return result;
    }

    #endregion

    #region Unit Conversions

    public async Task<List<UnitConversionDto>> GetUnitConversionsAsync(int? productId = null)
    {
        var conversions = await _conversionRepository.FindAsync(c =>
            c.IsActive && (!productId.HasValue || c.ProductId == productId || c.ProductId == null));

        var result = new List<UnitConversionDto>();
        foreach (var c in conversions)
        {
            var product = c.ProductId.HasValue ? await _productRepository.GetByIdAsync(c.ProductId.Value) : null;
            result.Add(new UnitConversionDto
            {
                Id = c.Id,
                FromUnit = c.FromUnit,
                ToUnit = c.ToUnit,
                ProductId = c.ProductId,
                ProductName = product?.Name,
                ConversionFactor = c.ConversionFactor
            });
        }

        return result;
    }

    public async Task<UnitConversionDto> CreateUnitConversionAsync(CreateUnitConversionDto dto)
    {
        var conversion = new UnitConversion
        {
            FromUnit = dto.FromUnit,
            ToUnit = dto.ToUnit,
            ProductId = dto.ProductId,
            ConversionFactor = dto.ConversionFactor
        };

        await _conversionRepository.AddAsync(conversion);
        await _unitOfWork.SaveChangesAsync();

        var product = dto.ProductId.HasValue ? await _productRepository.GetByIdAsync(dto.ProductId.Value) : null;

        return new UnitConversionDto
        {
            Id = conversion.Id,
            FromUnit = conversion.FromUnit,
            ToUnit = conversion.ToUnit,
            ProductId = conversion.ProductId,
            ProductName = product?.Name,
            ConversionFactor = conversion.ConversionFactor
        };
    }

    public async Task<decimal?> ConvertUnitsAsync(decimal quantity, RecipeUnitOfMeasure fromUnit, RecipeUnitOfMeasure toUnit, int? productId = null)
    {
        if (fromUnit == toUnit)
        {
            return quantity;
        }

        // Try product-specific conversion first
        if (productId.HasValue)
        {
            var productConversion = (await _conversionRepository.FindAsync(c =>
                c.IsActive && c.ProductId == productId &&
                c.FromUnit == fromUnit && c.ToUnit == toUnit)).FirstOrDefault();

            if (productConversion != null)
            {
                return quantity * productConversion.ConversionFactor;
            }
        }

        // Try global conversion
        var globalConversion = (await _conversionRepository.FindAsync(c =>
            c.IsActive && c.ProductId == null &&
            c.FromUnit == fromUnit && c.ToUnit == toUnit)).FirstOrDefault();

        if (globalConversion != null)
        {
            return quantity * globalConversion.ConversionFactor;
        }

        // Use standard conversions
        return GetStandardConversion(quantity, fromUnit, toUnit);
    }

    private static decimal? GetStandardConversion(decimal quantity, RecipeUnitOfMeasure from, RecipeUnitOfMeasure to)
    {
        // Weight conversions
        if (from == RecipeUnitOfMeasure.Gram && to == RecipeUnitOfMeasure.Kilogram)
            return quantity / 1000;
        if (from == RecipeUnitOfMeasure.Kilogram && to == RecipeUnitOfMeasure.Gram)
            return quantity * 1000;

        // Volume conversions
        if (from == RecipeUnitOfMeasure.Milliliter && to == RecipeUnitOfMeasure.Liter)
            return quantity / 1000;
        if (from == RecipeUnitOfMeasure.Liter && to == RecipeUnitOfMeasure.Milliliter)
            return quantity * 1000;

        // Teaspoon/Tablespoon conversions
        if (from == RecipeUnitOfMeasure.Teaspoon && to == RecipeUnitOfMeasure.Tablespoon)
            return quantity / 3;
        if (from == RecipeUnitOfMeasure.Tablespoon && to == RecipeUnitOfMeasure.Teaspoon)
            return quantity * 3;

        return null;
    }

    #endregion

    #region Summary

    public async Task<RecipeSummaryDto> GetRecipeSummaryAsync()
    {
        var recipes = (await _recipeRepository.FindAsync(r => r.IsActive)).ToList();
        var products = await _productRepository.FindAsync(p => p.IsActive);

        var summary = new RecipeSummaryDto
        {
            TotalRecipes = recipes.Count,
            ApprovedRecipes = recipes.Count(r => r.IsApproved),
            PendingApproval = recipes.Count(r => !r.IsApproved),
            StandardRecipes = recipes.Count(r => r.RecipeType == RecipeType.Standard),
            SubRecipes = recipes.Count(r => r.RecipeType == RecipeType.SubRecipe),
            BatchPrepRecipes = recipes.Count(r => r.RecipeType == RecipeType.BatchPrep),
            RecipesNeedingCostUpdate = recipes.Count(r =>
                !r.LastCostCalculation.HasValue ||
                r.LastCostCalculation.Value < DateTime.UtcNow.AddDays(-7))
        };

        // Calculate average food cost
        var recipesWithCost = recipes.Where(r => r.EstimatedCostPerPortion > 0);
        if (recipesWithCost.Any())
        {
            var productPrices = products.ToDictionary(p => p.Id, p => p.SellingPrice);
            var foodCosts = new List<decimal>();

            foreach (var r in recipesWithCost)
            {
                if (productPrices.TryGetValue(r.ProductId, out var price) && price > 0)
                {
                    foodCosts.Add(r.EstimatedCostPerPortion / price * 100);
                }
            }

            if (foodCosts.Any())
            {
                summary.AverageFoodCostPercent = foodCosts.Average();
            }
        }

        // Recently modified
        foreach (var recipe in recipes.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt).Take(5))
        {
            summary.RecentlyModified.Add(await MapToRecipeListDto(recipe));
        }

        return summary;
    }

    public async Task<List<ProductDto>> GetProductsWithoutRecipesAsync()
    {
        var products = await _productRepository.FindAsync(p => p.IsActive);
        var recipeProductIds = (await _recipeRepository.FindAsync(r => r.IsActive))
            .Select(r => r.ProductId)
            .ToHashSet();

        return products
            .Where(p => !recipeProductIds.Contains(p.Id))
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                SellingPrice = p.SellingPrice,
                CategoryId = p.CategoryId
            })
            .ToList();
    }

    #endregion

    #region Cloning

    public async Task<RecipeDto> CloneRecipeAsync(int recipeId, string newName, int? newProductId = null)
    {
        var original = await _recipeRepository.GetByIdAsync(recipeId);
        if (original == null)
        {
            throw new InvalidOperationException($"Recipe {recipeId} not found");
        }

        var createDto = new CreateRecipeDto
        {
            ProductId = newProductId ?? original.ProductId,
            Name = newName,
            Instructions = original.Instructions,
            RecipeType = original.RecipeType,
            YieldQuantity = original.YieldQuantity,
            YieldUnit = original.YieldUnit,
            PrepTimeMinutes = original.PrepTimeMinutes,
            CookTimeMinutes = original.CookTimeMinutes,
            Notes = $"Cloned from recipe: {original.Name}"
        };

        // Get original ingredients
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipeId && i.IsActive);
        createDto.Ingredients = ingredients.Select(i => new CreateRecipeIngredientDto
        {
            IngredientProductId = i.IngredientProductId,
            Quantity = i.Quantity,
            Unit = i.Unit,
            CustomUnit = i.CustomUnit,
            WastePercent = i.WastePercent,
            IsOptional = i.IsOptional,
            SortOrder = i.SortOrder,
            PrepNotes = i.PrepNotes
        }).ToList();

        // Get original sub-recipes
        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipeId && sr.IsActive);
        createDto.SubRecipes = subRecipes.Select(sr => new CreateRecipeSubRecipeDto
        {
            SubRecipeId = sr.SubRecipeId,
            Quantity = sr.Quantity,
            SortOrder = sr.SortOrder,
            Notes = sr.Notes
        }).ToList();

        var cloned = await CreateRecipeAsync(createDto);

        _logger.LogInformation("Cloned recipe {OriginalId} to {NewId}", recipeId, cloned.Id);

        return cloned;
    }

    #endregion

    #region Private Mapping Methods

    private async Task<RecipeDto> MapToRecipeDto(Recipe recipe, bool includeIngredients)
    {
        var product = await _productRepository.GetByIdAsync(recipe.ProductId);

        var dto = new RecipeDto
        {
            Id = recipe.Id,
            ProductId = recipe.ProductId,
            ProductName = product?.Name ?? "",
            ProductCode = product?.Code ?? "",
            Name = recipe.Name,
            Instructions = recipe.Instructions,
            RecipeType = recipe.RecipeType,
            YieldQuantity = recipe.YieldQuantity,
            YieldUnit = recipe.YieldUnit,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            TotalTimeMinutes = recipe.TotalTimeMinutes,
            EstimatedCostPerPortion = recipe.EstimatedCostPerPortion,
            TotalEstimatedCost = recipe.TotalEstimatedCost,
            LastCostCalculation = recipe.LastCostCalculation,
            Version = recipe.Version,
            Notes = recipe.Notes,
            IsApproved = recipe.IsApproved,
            ApprovedAt = recipe.ApprovedAt,
            IsActive = recipe.IsActive,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            SellingPrice = product?.SellingPrice ?? 0
        };

        if (includeIngredients)
        {
            dto.Ingredients = await GetIngredientsAsync(recipe.Id);
            dto.SubRecipes = await GetSubRecipesAsync(recipe.Id);
        }

        return dto;
    }

    private async Task<RecipeListDto> MapToRecipeListDto(Recipe recipe)
    {
        var product = await _productRepository.GetByIdAsync(recipe.ProductId);
        var ingredientCount = (await _ingredientRepository.FindAsync(i => i.RecipeId == recipe.Id && i.IsActive)).Count();
        var subRecipeCount = (await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipe.Id && sr.IsActive)).Count();

        var sellingPrice = product?.SellingPrice ?? 0;

        return new RecipeListDto
        {
            Id = recipe.Id,
            ProductId = recipe.ProductId,
            ProductName = product?.Name ?? "",
            ProductCode = product?.Code ?? "",
            Name = recipe.Name,
            RecipeType = recipe.RecipeType,
            YieldQuantity = recipe.YieldQuantity,
            YieldUnit = recipe.YieldUnit,
            EstimatedCostPerPortion = recipe.EstimatedCostPerPortion,
            SellingPrice = sellingPrice,
            FoodCostPercent = sellingPrice > 0 ? recipe.EstimatedCostPerPortion / sellingPrice * 100 : 0,
            IngredientCount = ingredientCount + subRecipeCount,
            IsApproved = recipe.IsApproved,
            IsActive = recipe.IsActive,
            Version = recipe.Version,
            LastCostCalculation = recipe.LastCostCalculation
        };
    }

    private async Task<RecipeIngredientDto> MapToIngredientDto(RecipeIngredient ingredient)
    {
        var product = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
        var inventory = (await _inventoryRepository.FindAsync(i => i.ProductId == ingredient.IngredientProductId)).FirstOrDefault();

        return new RecipeIngredientDto
        {
            Id = ingredient.Id,
            RecipeId = ingredient.RecipeId,
            IngredientProductId = ingredient.IngredientProductId,
            IngredientName = product?.Name ?? "",
            IngredientCode = product?.Code ?? "",
            Quantity = ingredient.Quantity,
            Unit = ingredient.Unit,
            CustomUnit = ingredient.CustomUnit,
            UnitDisplayName = ingredient.UnitDisplayName,
            WastePercent = ingredient.WastePercent,
            EffectiveQuantity = ingredient.EffectiveQuantity,
            IsOptional = ingredient.IsOptional,
            SortOrder = ingredient.SortOrder,
            PrepNotes = ingredient.PrepNotes,
            CalculatedCost = ingredient.CalculatedCost,
            IngredientCostPrice = product?.CostPrice,
            CurrentStock = inventory?.CurrentStock ?? 0,
            IsLowStock = product?.IsLowStock ?? false
        };
    }

    private async Task<RecipeSubRecipeDto> MapToSubRecipeDto(RecipeSubRecipe subRecipe)
    {
        var recipe = await _recipeRepository.GetByIdAsync(subRecipe.SubRecipeId);
        var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

        return new RecipeSubRecipeDto
        {
            Id = subRecipe.Id,
            ParentRecipeId = subRecipe.ParentRecipeId,
            SubRecipeId = subRecipe.SubRecipeId,
            SubRecipeName = recipe?.Name ?? "",
            SubRecipeProductName = product?.Name ?? "",
            Quantity = subRecipe.Quantity,
            YieldUnit = recipe?.YieldUnit ?? "",
            SortOrder = subRecipe.SortOrder,
            Notes = subRecipe.Notes,
            CalculatedCost = subRecipe.CalculatedCost,
            SubRecipeCostPerPortion = recipe?.EstimatedCostPerPortion ?? 0
        };
    }

    #endregion
}
