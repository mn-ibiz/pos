using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for recipe management.
/// </summary>
public interface IRecipeService
{
    #region Recipe CRUD

    /// <summary>
    /// Creates a new recipe.
    /// </summary>
    /// <param name="dto">The recipe creation data.</param>
    /// <returns>The created recipe.</returns>
    Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto);

    /// <summary>
    /// Gets a recipe by ID.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="includeIngredients">Whether to include ingredients.</param>
    /// <returns>The recipe, or null if not found.</returns>
    Task<RecipeDto?> GetRecipeByIdAsync(int id, bool includeIngredients = true);

    /// <summary>
    /// Gets a recipe by product ID.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The recipe, or null if not found.</returns>
    Task<RecipeDto?> GetRecipeByProductAsync(int productId);

    /// <summary>
    /// Updates a recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="dto">The update data.</param>
    /// <returns>The updated recipe.</returns>
    Task<RecipeDto> UpdateRecipeAsync(int id, UpdateRecipeDto dto);

    /// <summary>
    /// Deletes a recipe (soft delete).
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteRecipeAsync(int id);

    /// <summary>
    /// Queries recipes with filters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>List of matching recipes.</returns>
    Task<List<RecipeListDto>> QueryRecipesAsync(RecipeQueryDto query);

    /// <summary>
    /// Gets all recipes.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive recipes.</param>
    /// <returns>List of all recipes.</returns>
    Task<List<RecipeListDto>> GetAllRecipesAsync(bool includeInactive = false);

    #endregion

    #region Ingredient Management

    /// <summary>
    /// Adds an ingredient to a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="dto">The ingredient data.</param>
    /// <returns>The added ingredient.</returns>
    Task<RecipeIngredientDto> AddIngredientAsync(int recipeId, CreateRecipeIngredientDto dto);

    /// <summary>
    /// Updates a recipe ingredient.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="ingredientId">The ingredient ID.</param>
    /// <param name="dto">The update data.</param>
    /// <returns>The updated ingredient.</returns>
    Task<RecipeIngredientDto> UpdateIngredientAsync(int recipeId, int ingredientId, UpdateRecipeIngredientDto dto);

    /// <summary>
    /// Removes an ingredient from a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="ingredientId">The ingredient ID.</param>
    /// <returns>True if removed.</returns>
    Task<bool> RemoveIngredientAsync(int recipeId, int ingredientId);

    /// <summary>
    /// Gets ingredients for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>List of ingredients.</returns>
    Task<List<RecipeIngredientDto>> GetIngredientsAsync(int recipeId);

    /// <summary>
    /// Reorders ingredients in a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="ingredientIds">Ingredient IDs in new order.</param>
    Task ReorderIngredientsAsync(int recipeId, List<int> ingredientIds);

    #endregion

    #region Sub-Recipe Management

    /// <summary>
    /// Adds a sub-recipe to a recipe.
    /// </summary>
    /// <param name="recipeId">The parent recipe ID.</param>
    /// <param name="dto">The sub-recipe data.</param>
    /// <returns>The added sub-recipe reference.</returns>
    Task<RecipeSubRecipeDto> AddSubRecipeAsync(int recipeId, CreateRecipeSubRecipeDto dto);

    /// <summary>
    /// Removes a sub-recipe from a recipe.
    /// </summary>
    /// <param name="recipeId">The parent recipe ID.</param>
    /// <param name="subRecipeId">The sub-recipe ID.</param>
    /// <returns>True if removed.</returns>
    Task<bool> RemoveSubRecipeAsync(int recipeId, int subRecipeId);

    /// <summary>
    /// Gets sub-recipes used by a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>List of sub-recipes.</returns>
    Task<List<RecipeSubRecipeDto>> GetSubRecipesAsync(int recipeId);

    /// <summary>
    /// Gets recipes that use a specific sub-recipe.
    /// </summary>
    /// <param name="subRecipeId">The sub-recipe ID.</param>
    /// <returns>List of parent recipes.</returns>
    Task<List<RecipeListDto>> GetRecipesUsingSubRecipeAsync(int subRecipeId);

    #endregion

    #region Validation

    /// <summary>
    /// Validates a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>Validation result.</returns>
    Task<RecipeValidationResultDto> ValidateRecipeAsync(int recipeId);

    /// <summary>
    /// Validates recipe creation data.
    /// </summary>
    /// <param name="dto">The recipe data.</param>
    /// <returns>Validation result.</returns>
    Task<RecipeValidationResultDto> ValidateCreateRecipeAsync(CreateRecipeDto dto);

    /// <summary>
    /// Checks for circular dependencies in sub-recipes.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="subRecipeId">The sub-recipe to add.</param>
    /// <returns>True if would create circular dependency.</returns>
    Task<bool> HasCircularDependencyAsync(int recipeId, int subRecipeId);

    #endregion

    #region Approval

    /// <summary>
    /// Approves or unapproves a recipe.
    /// </summary>
    /// <param name="dto">The approval data.</param>
    /// <param name="userId">The approving user ID.</param>
    /// <returns>The updated recipe.</returns>
    Task<RecipeDto> ApproveRecipeAsync(ApproveRecipeDto dto, int userId);

    /// <summary>
    /// Gets recipes pending approval.
    /// </summary>
    /// <returns>List of recipes pending approval.</returns>
    Task<List<RecipeListDto>> GetPendingApprovalAsync();

    #endregion

    #region Costing

    /// <summary>
    /// Calculates the cost for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>Cost calculation result.</returns>
    Task<RecipeCostDto> CalculateCostAsync(int recipeId);

    /// <summary>
    /// Recalculates costs for all recipes.
    /// </summary>
    /// <returns>Number of recipes updated.</returns>
    Task<int> RecalculateAllCostsAsync();

    /// <summary>
    /// Gets cost history for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="limit">Maximum entries to return.</param>
    /// <returns>Cost history entries.</returns>
    Task<List<RecipeCostHistoryDto>> GetCostHistoryAsync(int recipeId, int limit = 20);

    #endregion

    #region Unit Conversions

    /// <summary>
    /// Gets all unit conversions.
    /// </summary>
    /// <param name="productId">Optional product filter.</param>
    /// <returns>List of unit conversions.</returns>
    Task<List<UnitConversionDto>> GetUnitConversionsAsync(int? productId = null);

    /// <summary>
    /// Creates a unit conversion.
    /// </summary>
    /// <param name="dto">The conversion data.</param>
    /// <returns>The created conversion.</returns>
    Task<UnitConversionDto> CreateUnitConversionAsync(CreateUnitConversionDto dto);

    /// <summary>
    /// Converts a quantity between units.
    /// </summary>
    /// <param name="quantity">The quantity to convert.</param>
    /// <param name="fromUnit">Source unit.</param>
    /// <param name="toUnit">Target unit.</param>
    /// <param name="productId">Optional product for product-specific conversions.</param>
    /// <returns>Converted quantity, or null if conversion not available.</returns>
    Task<decimal?> ConvertUnitsAsync(decimal quantity, RecipeUnitOfMeasure fromUnit, RecipeUnitOfMeasure toUnit, int? productId = null);

    #endregion

    #region Summary

    /// <summary>
    /// Gets a summary of all recipes for dashboard.
    /// </summary>
    /// <returns>Recipe summary.</returns>
    Task<RecipeSummaryDto> GetRecipeSummaryAsync();

    /// <summary>
    /// Gets products that don't have recipes.
    /// </summary>
    /// <returns>List of products without recipes.</returns>
    Task<List<ProductDto>> GetProductsWithoutRecipesAsync();

    #endregion

    #region Cloning

    /// <summary>
    /// Clones a recipe with a new name.
    /// </summary>
    /// <param name="recipeId">The recipe to clone.</param>
    /// <param name="newName">The new recipe name.</param>
    /// <param name="newProductId">Optional new product to link to.</param>
    /// <returns>The cloned recipe.</returns>
    Task<RecipeDto> CloneRecipeAsync(int recipeId, string newName, int? newProductId = null);

    #endregion
}
