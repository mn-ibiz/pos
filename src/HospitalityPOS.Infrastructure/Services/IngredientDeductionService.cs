using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using System.Diagnostics;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for automatic ingredient deduction when items are sold.
/// </summary>
public class IngredientDeductionService : IIngredientDeductionService
{
    private readonly IRepository<Recipe> _recipeRepository;
    private readonly IRepository<RecipeIngredient> _ingredientRepository;
    private readonly IRepository<RecipeSubRecipe> _subRecipeRepository;
    private readonly IRepository<IngredientDeductionLog> _logRepository;
    private readonly IRepository<ReceiptDeductionBatch> _batchRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private DeductionConfigDto _config;

    public event EventHandler<DeductionResultDto>? IngredientsDeducted;
    public event EventHandler<IngredientDeductionResultDto>? DeductionFailed;
    public event EventHandler<DeductionLowStockWarningDto>? LowStockDetected;
    public event EventHandler<ReverseDeductionResultDto>? DeductionsReversed;

    public bool IsEnabled => _config.Enabled;

    public IngredientDeductionService(
        IRepository<Recipe> recipeRepository,
        IRepository<RecipeIngredient> ingredientRepository,
        IRepository<RecipeSubRecipe> subRecipeRepository,
        IRepository<IngredientDeductionLog> logRepository,
        IRepository<ReceiptDeductionBatch> batchRepository,
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        _subRecipeRepository = subRecipeRepository ?? throw new ArgumentNullException(nameof(subRecipeRepository));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        _config = new DeductionConfigDto();
    }

    #region Deduction Operations

    public async Task<DeductionResultDto> DeductIngredientsAsync(DeductIngredientsRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var result = new DeductionResultDto
        {
            ReceiptId = request.ReceiptId,
            TotalItems = request.Items.Count
        };

        if (!_config.Enabled)
        {
            result.Success = true;
            result.Warnings.Add("Ingredient deduction is disabled.");
            return result;
        }

        // Create batch record
        var batch = new ReceiptDeductionBatch
        {
            ReceiptId = request.ReceiptId,
            StoreId = request.StoreId,
            StartedAt = DateTime.UtcNow,
            TotalItems = request.Items.Count,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _batchRepository.AddAsync(batch);
        await _unitOfWork.SaveChangesAsync();
        result.BatchId = batch.Id;

        foreach (var item in request.Items)
        {
            try
            {
                var itemResult = await DeductForItemAsync(
                    item.ProductId,
                    item.Quantity,
                    request.ReceiptId,
                    item.ReceiptLineId,
                    request.StoreId,
                    request.AllowNegativeStock);

                result.ItemResults.Add(itemResult);

                if (itemResult.HasRecipe)
                {
                    result.ItemsWithRecipes++;
                    result.TotalIngredientsDeducted += itemResult.IngredientDeductions.Count;
                    result.SuccessfulDeductions += itemResult.IngredientDeductions.Count(d => d.Status == DeductionStatusDto.Success);
                    result.FailedDeductions += itemResult.IngredientDeductions.Count(d => d.Status == DeductionStatusDto.Failed);
                    result.WarningDeductions += itemResult.IngredientDeductions.Count(d => d.Status == DeductionStatusDto.Warning);

                    foreach (var deduction in itemResult.IngredientDeductions.Where(d => d.Status == DeductionStatusDto.Failed))
                    {
                        result.Errors.Add($"{item.ProductName}: {deduction.Error}");
                    }
                }
                else
                {
                    result.ItemsWithoutRecipes++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {item.ProductName}: {ex.Message}");

                if (!request.SkipOnError)
                    break;
            }
        }

        // Update batch record
        batch.CompletedAt = DateTime.UtcNow;
        batch.ItemsWithRecipes = result.ItemsWithRecipes;
        batch.TotalIngredientsDeducted = result.TotalIngredientsDeducted;
        batch.SuccessfulDeductions = result.SuccessfulDeductions;
        batch.FailedDeductions = result.FailedDeductions;
        batch.WarningDeductions = result.WarningDeductions;
        batch.IsComplete = true;
        batch.ErrorSummary = result.Errors.Any() ? string.Join("; ", result.Errors.Take(5)) : null;

        await _batchRepository.UpdateAsync(batch);
        await _unitOfWork.SaveChangesAsync();

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.Success = result.FailedDeductions == 0 || request.SkipOnError;

        IngredientsDeducted?.Invoke(this, result);

        return result;
    }

    public async Task<ItemDeductionResultDto> DeductForItemAsync(
        int productId,
        decimal quantity,
        int receiptId,
        int? receiptLineId = null,
        int? storeId = null,
        bool allowNegativeStock = false)
    {
        var result = new ItemDeductionResultDto
        {
            ProductId = productId,
            QuantitySold = quantity
        };

        // Get product info
        var product = await _productRepository.GetByIdAsync(productId);
        result.ProductName = product?.Name ?? $"Product {productId}";

        // Get recipe for product
        var recipes = await _recipeRepository.FindAsync(r =>
            r.ProductId == productId && r.IsActive && r.IsApproved);
        var recipe = recipes.FirstOrDefault();

        if (recipe == null)
        {
            result.HasRecipe = false;
            result.AllDeductionsSuccessful = true;
            return result;
        }

        result.HasRecipe = true;
        result.RecipeId = recipe.Id;
        result.RecipeName = recipe.Name;

        // Calculate multiplier (portions sold / recipe yield)
        var multiplier = quantity / (recipe.YieldQuantity > 0 ? recipe.YieldQuantity : 1);

        // Get ingredients
        var ingredients = await _ingredientRepository.FindAsync(i =>
            i.RecipeId == recipe.Id && i.IsActive);

        // Deduct each ingredient
        foreach (var ingredient in ingredients)
        {
            var deductionResult = await DeductIngredientAsync(
                ingredient,
                multiplier,
                receiptId,
                receiptLineId,
                recipe.Id,
                storeId,
                allowNegativeStock);

            result.IngredientDeductions.Add(deductionResult);

            if (deductionResult.Status == DeductionStatusDto.Failed)
            {
                DeductionFailed?.Invoke(this, deductionResult);
            }
        }

        // Also process sub-recipes
        var subRecipes = await _subRecipeRepository.FindAsync(sr =>
            sr.ParentRecipeId == recipe.Id && sr.IsActive);

        foreach (var subRecipeUsage in subRecipes)
        {
            var subMultiplier = multiplier * subRecipeUsage.Quantity;
            await DeductSubRecipeIngredientsAsync(
                subRecipeUsage.SubRecipeId,
                subMultiplier,
                receiptId,
                receiptLineId,
                storeId,
                allowNegativeStock,
                result);
        }

        result.AllDeductionsSuccessful = result.IngredientDeductions.All(d =>
            d.Status == DeductionStatusDto.Success || d.Status == DeductionStatusDto.Warning);

        return result;
    }

    public async Task<ReverseDeductionResultDto> ReverseDeductionsAsync(ReverseDeductionRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ReverseDeductionResultDto
        {
            ReceiptId = request.ReceiptId
        };

        // Get all non-reversed deductions for this receipt
        var deductions = await _logRepository.FindAsync(log =>
            log.ReceiptId == request.ReceiptId &&
            log.Status != DeductionStatus.Reversed &&
            log.ReversedAt == null &&
            log.IsActive);

        foreach (var deduction in deductions)
        {
            try
            {
                var reversalResult = await ReverseDeductionAsync(deduction.Id, request.Reason);
                result.ReversalDetails.Add(reversalResult);

                if (reversalResult.Status == DeductionStatusDto.Success)
                    result.DeductionsReversed++;
                else
                    result.ReversalsFailed++;
            }
            catch (Exception ex)
            {
                result.ReversalsFailed++;
                result.Errors.Add($"Failed to reverse deduction {deduction.Id}: {ex.Message}");
            }
        }

        result.Success = result.ReversalsFailed == 0;
        DeductionsReversed?.Invoke(this, result);

        return result;
    }

    public async Task<IngredientDeductionResultDto> ReverseDeductionAsync(int logId, string reason)
    {
        var log = await _logRepository.GetByIdAsync(logId);
        if (log == null)
            throw new KeyNotFoundException($"Deduction log {logId} not found.");

        if (log.ReversedAt.HasValue)
            throw new InvalidOperationException($"Deduction {logId} has already been reversed.");

        var ingredientProduct = await _productRepository.GetByIdAsync(log.IngredientProductId);
        if (ingredientProduct == null)
            throw new InvalidOperationException($"Ingredient product {log.IngredientProductId} not found.");

        var result = new IngredientDeductionResultDto
        {
            LogId = logId,
            IngredientProductId = log.IngredientProductId,
            IngredientName = ingredientProduct.Name,
            QuantityDeducted = -log.QuantityDeducted, // Negative for reversal
            Unit = log.Unit.ToString(),
            StockBefore = ingredientProduct.StockQuantity
        };

        // Add stock back
        ingredientProduct.StockQuantity += log.QuantityDeducted;
        await _productRepository.UpdateAsync(ingredientProduct);

        // Create reversal log
        var reversalLog = new IngredientDeductionLog
        {
            ReceiptId = log.ReceiptId,
            ReceiptLineId = log.ReceiptLineId,
            RecipeId = log.RecipeId,
            IngredientProductId = log.IngredientProductId,
            PortionsSold = -log.PortionsSold,
            QuantityDeducted = -log.QuantityDeducted,
            Unit = log.Unit,
            StockBefore = result.StockBefore,
            StockAfter = ingredientProduct.StockQuantity,
            Status = DeductionStatus.Reversed,
            WasForced = false,
            DeductedAt = DateTime.UtcNow,
            StoreId = log.StoreId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _logRepository.AddAsync(reversalLog);

        // Update original log
        log.ReversedByLogId = reversalLog.Id;
        log.ReversedAt = DateTime.UtcNow;
        log.Status = DeductionStatus.Reversed;
        await _logRepository.UpdateAsync(log);

        await _unitOfWork.SaveChangesAsync();

        result.StockAfter = ingredientProduct.StockQuantity;
        result.Status = DeductionStatusDto.Success;

        return result;
    }

    #endregion

    #region Query Operations

    public async Task<List<IngredientDeductionLogDto>> GetDeductionLogsAsync(DeductionLogQueryDto query)
    {
        var logs = await _logRepository.FindAsync(log =>
            log.IsActive &&
            (!query.ReceiptId.HasValue || log.ReceiptId == query.ReceiptId) &&
            (!query.RecipeId.HasValue || log.RecipeId == query.RecipeId) &&
            (!query.IngredientProductId.HasValue || log.IngredientProductId == query.IngredientProductId) &&
            (!query.StoreId.HasValue || log.StoreId == query.StoreId) &&
            (!query.FromDate.HasValue || log.DeductedAt >= query.FromDate) &&
            (!query.ToDate.HasValue || log.DeductedAt <= query.ToDate) &&
            (query.IncludeReversed || log.Status != DeductionStatus.Reversed));

        var orderedLogs = logs.OrderByDescending(l => l.DeductedAt)
            .Skip(query.Skip)
            .Take(query.Take);

        return await MapLogsToDto(orderedLogs);
    }

    public async Task<List<IngredientDeductionLogDto>> GetDeductionsForReceiptAsync(int receiptId)
    {
        var logs = await _logRepository.FindAsync(log =>
            log.ReceiptId == receiptId && log.IsActive);

        return await MapLogsToDto(logs.OrderByDescending(l => l.DeductedAt));
    }

    public async Task<List<IngredientDeductionLogDto>> GetDeductionsForIngredientAsync(
        int ingredientProductId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = new DeductionLogQueryDto
        {
            IngredientProductId = ingredientProductId,
            FromDate = fromDate,
            ToDate = toDate
        };

        return await GetDeductionLogsAsync(query);
    }

    public async Task<DeductionSummaryDto> GetDeductionSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        int? storeId = null)
    {
        var logs = await _logRepository.FindAsync(log =>
            log.IsActive &&
            log.DeductedAt >= fromDate &&
            log.DeductedAt <= toDate &&
            (!storeId.HasValue || log.StoreId == storeId) &&
            log.Status != DeductionStatus.Reversed);

        var summary = new DeductionSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalDeductions = logs.Count(),
            SuccessfulDeductions = logs.Count(l => l.Status == DeductionStatus.Success),
            FailedDeductions = logs.Count(l => l.Status == DeductionStatus.Failed),
            WarningDeductions = logs.Count(l => l.Status == DeductionStatus.Warning),
            ForcedDeductions = logs.Count(l => l.WasForced),
            ReversedDeductions = logs.Count(l => l.ReversedAt.HasValue),
            TotalQuantityDeducted = logs.Sum(l => l.QuantityDeducted)
        };

        // Group by ingredient
        var byIngredient = logs.GroupBy(l => l.IngredientProductId);
        foreach (var group in byIngredient)
        {
            var ingredientProduct = await _productRepository.GetByIdAsync(group.Key);
            summary.ByIngredient.Add(new IngredientDeductionSummaryDto
            {
                IngredientProductId = group.Key,
                IngredientName = ingredientProduct?.Name ?? $"Product {group.Key}",
                TotalQuantityDeducted = group.Sum(l => l.QuantityDeducted),
                Unit = group.First().Unit.ToString(),
                DeductionCount = group.Count(),
                AverageDeduction = group.Average(l => l.QuantityDeducted)
            });
        }

        // Group by recipe
        var byRecipe = logs.GroupBy(l => l.RecipeId);
        foreach (var group in byRecipe)
        {
            var recipes = await _recipeRepository.FindAsync(r => r.Id == group.Key);
            var recipe = recipes.FirstOrDefault();
            var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

            summary.ByRecipe.Add(new RecipeDeductionSummaryDto
            {
                RecipeId = group.Key,
                RecipeName = recipe?.Name ?? $"Recipe {group.Key}",
                ProductName = product?.Name ?? string.Empty,
                TotalPortionsDeducted = group.Sum(l => l.PortionsSold),
                DeductionCount = group.Count(),
                UniqueIngredients = group.Select(l => l.IngredientProductId).Distinct().Count()
            });
        }

        return summary;
    }

    #endregion

    #region Validation

    public async Task<DeductionValidationResultDto> ValidateDeductionAsync(int productId, decimal quantity)
    {
        var result = new DeductionValidationResultDto { CanDeduct = true };

        // Get recipe
        var recipes = await _recipeRepository.FindAsync(r =>
            r.ProductId == productId && r.IsActive && r.IsApproved);
        var recipe = recipes.FirstOrDefault();

        if (recipe == null)
        {
            result.HasRecipe = false;
            return result;
        }

        result.HasRecipe = true;
        result.RecipeId = recipe.Id;
        result.RecipeName = recipe.Name;

        // Calculate multiplier
        var multiplier = quantity / (recipe.YieldQuantity > 0 ? recipe.YieldQuantity : 1);

        // Check each ingredient
        var ingredients = await _ingredientRepository.FindAsync(i =>
            i.RecipeId == recipe.Id && i.IsActive);

        foreach (var ingredient in ingredients)
        {
            var ingredientProduct = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
            if (ingredientProduct == null)
            {
                result.Errors.Add($"Ingredient product {ingredient.IngredientProductId} not found.");
                result.CanDeduct = false;
                continue;
            }

            var requiredQuantity = ingredient.Quantity * (1 + ingredient.WastePercent / 100) * multiplier;

            var availability = new IngredientAvailabilityDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                IngredientName = ingredientProduct.Name,
                RequiredQuantity = requiredQuantity,
                AvailableStock = ingredientProduct.StockQuantity,
                Unit = ingredient.Unit.ToString(),
                IsSufficient = ingredientProduct.StockQuantity >= requiredQuantity,
                Shortage = Math.Max(0, requiredQuantity - ingredientProduct.StockQuantity)
            };

            result.IngredientAvailability.Add(availability);

            if (!availability.IsSufficient)
            {
                var warning = new DeductionLowStockWarningDto
                {
                    IngredientProductId = ingredient.IngredientProductId,
                    IngredientName = ingredientProduct.Name,
                    CurrentStock = ingredientProduct.StockQuantity,
                    ReorderLevel = ingredientProduct.ReorderLevel,
                    QuantityDeducted = requiredQuantity,
                    IsNegative = ingredientProduct.StockQuantity - requiredQuantity < 0,
                    WarningMessage = $"Insufficient stock for {ingredientProduct.Name}: need {requiredQuantity:F2}, have {ingredientProduct.StockQuantity:F2}"
                };

                result.Warnings.Add(warning);

                if (!_config.AllowNegativeStock)
                {
                    result.CanDeduct = false;
                }
            }
        }

        return result;
    }

    public async Task<List<DeductionLowStockWarningDto>> GetLowStockWarningsAsync(int productId, decimal quantity)
    {
        var validation = await ValidateDeductionAsync(productId, quantity);
        return validation.Warnings;
    }

    #endregion

    #region Configuration

    public DeductionConfigDto GetConfiguration() => _config;

    public void UpdateConfiguration(DeductionConfigDto config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    #endregion

    #region Private Methods

    private async Task<IngredientDeductionResultDto> DeductIngredientAsync(
        RecipeIngredient ingredient,
        decimal multiplier,
        int receiptId,
        int? receiptLineId,
        int recipeId,
        int? storeId,
        bool allowNegativeStock)
    {
        var ingredientProduct = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
        if (ingredientProduct == null)
        {
            return new IngredientDeductionResultDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                Status = DeductionStatusDto.Failed,
                Error = $"Ingredient product {ingredient.IngredientProductId} not found."
            };
        }

        var result = new IngredientDeductionResultDto
        {
            IngredientProductId = ingredient.IngredientProductId,
            IngredientName = ingredientProduct.Name,
            Unit = ingredient.UnitDisplayName,
            StockBefore = ingredientProduct.StockQuantity
        };

        // Calculate quantity to deduct (with waste factor)
        var deductQuantity = ingredient.Quantity * (1 + ingredient.WastePercent / 100) * multiplier;
        result.QuantityDeducted = deductQuantity;

        // Check stock availability
        var newStock = ingredientProduct.StockQuantity - deductQuantity;
        var isInsufficientStock = newStock < 0;

        if (isInsufficientStock && !allowNegativeStock && !_config.AllowNegativeStock)
        {
            // Log the failure
            var failLog = await CreateDeductionLogAsync(
                receiptId, receiptLineId, recipeId, ingredient,
                multiplier, deductQuantity, ingredientProduct.StockQuantity,
                ingredientProduct.StockQuantity, // Stock unchanged
                DeductionStatus.Failed,
                $"Insufficient stock: need {deductQuantity:F2}, have {ingredientProduct.StockQuantity:F2}",
                false, storeId);

            result.LogId = failLog.Id;
            result.StockAfter = ingredientProduct.StockQuantity;
            result.Status = DeductionStatusDto.Failed;
            result.Error = $"Insufficient stock for {ingredientProduct.Name}";
            return result;
        }

        // Perform deduction
        ingredientProduct.StockQuantity = newStock;
        await _productRepository.UpdateAsync(ingredientProduct);

        // Determine status
        var status = DeductionStatus.Success;
        var wasForced = false;
        string? errorMsg = null;

        if (isInsufficientStock)
        {
            status = DeductionStatus.Warning;
            wasForced = true;
            errorMsg = $"Forced deduction: stock went negative ({newStock:F2})";

            // Raise low stock event
            LowStockDetected?.Invoke(this, new DeductionLowStockWarningDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                IngredientName = ingredientProduct.Name,
                CurrentStock = newStock,
                ReorderLevel = ingredientProduct.ReorderLevel,
                QuantityDeducted = deductQuantity,
                IsNegative = true,
                WarningMessage = errorMsg
            });
        }
        else if (newStock <= ingredientProduct.ReorderLevel)
        {
            // Low stock warning
            LowStockDetected?.Invoke(this, new DeductionLowStockWarningDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                IngredientName = ingredientProduct.Name,
                CurrentStock = newStock,
                ReorderLevel = ingredientProduct.ReorderLevel,
                QuantityDeducted = deductQuantity,
                IsNegative = false,
                WarningMessage = $"Stock below reorder level: {newStock:F2} <= {ingredientProduct.ReorderLevel:F2}"
            });
        }

        // Create log entry
        var log = await CreateDeductionLogAsync(
            receiptId, receiptLineId, recipeId, ingredient,
            multiplier, deductQuantity, result.StockBefore,
            newStock, status, errorMsg, wasForced, storeId);

        await _unitOfWork.SaveChangesAsync();

        result.LogId = log.Id;
        result.StockAfter = newStock;
        result.WasForced = wasForced;
        result.Status = status == DeductionStatus.Success ? DeductionStatusDto.Success : DeductionStatusDto.Warning;
        result.Error = errorMsg;

        return result;
    }

    private async Task DeductSubRecipeIngredientsAsync(
        int subRecipeId,
        decimal multiplier,
        int receiptId,
        int? receiptLineId,
        int? storeId,
        bool allowNegativeStock,
        ItemDeductionResultDto result)
    {
        var subRecipes = await _recipeRepository.FindAsync(r => r.Id == subRecipeId && r.IsActive);
        var subRecipe = subRecipes.FirstOrDefault();
        if (subRecipe == null) return;

        // Get sub-recipe ingredients
        var ingredients = await _ingredientRepository.FindAsync(i =>
            i.RecipeId == subRecipeId && i.IsActive);

        // Adjust multiplier for sub-recipe yield
        var subMultiplier = multiplier / (subRecipe.YieldQuantity > 0 ? subRecipe.YieldQuantity : 1);

        foreach (var ingredient in ingredients)
        {
            var deductionResult = await DeductIngredientAsync(
                ingredient,
                subMultiplier,
                receiptId,
                receiptLineId,
                subRecipeId,
                storeId,
                allowNegativeStock);

            result.IngredientDeductions.Add(deductionResult);
        }

        // Recursively process nested sub-recipes
        var nestedSubRecipes = await _subRecipeRepository.FindAsync(sr =>
            sr.ParentRecipeId == subRecipeId && sr.IsActive);

        foreach (var nested in nestedSubRecipes)
        {
            var nestedMultiplier = subMultiplier * nested.Quantity;
            await DeductSubRecipeIngredientsAsync(
                nested.SubRecipeId,
                nestedMultiplier,
                receiptId,
                receiptLineId,
                storeId,
                allowNegativeStock,
                result);
        }
    }

    private async Task<IngredientDeductionLog> CreateDeductionLogAsync(
        int receiptId,
        int? receiptLineId,
        int recipeId,
        RecipeIngredient ingredient,
        decimal multiplier,
        decimal quantityDeducted,
        decimal stockBefore,
        decimal stockAfter,
        DeductionStatus status,
        string? errorMessage,
        bool wasForced,
        int? storeId)
    {
        var log = new IngredientDeductionLog
        {
            ReceiptId = receiptId,
            ReceiptLineId = receiptLineId,
            RecipeId = recipeId,
            IngredientProductId = ingredient.IngredientProductId,
            PortionsSold = multiplier,
            QuantityDeducted = quantityDeducted,
            Unit = ingredient.Unit,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            Status = status,
            ErrorMessage = errorMessage,
            WasForced = wasForced,
            DeductedAt = DateTime.UtcNow,
            StoreId = storeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _logRepository.AddAsync(log);
        return log;
    }

    private async Task<List<IngredientDeductionLogDto>> MapLogsToDto(IEnumerable<IngredientDeductionLog> logs)
    {
        var dtos = new List<IngredientDeductionLogDto>();

        foreach (var log in logs)
        {
            var recipes = await _recipeRepository.FindAsync(r => r.Id == log.RecipeId);
            var recipe = recipes.FirstOrDefault();
            var ingredientProduct = await _productRepository.GetByIdAsync(log.IngredientProductId);

            dtos.Add(new IngredientDeductionLogDto
            {
                Id = log.Id,
                ReceiptId = log.ReceiptId,
                ReceiptLineId = log.ReceiptLineId,
                RecipeId = log.RecipeId,
                RecipeName = recipe?.Name ?? $"Recipe {log.RecipeId}",
                IngredientProductId = log.IngredientProductId,
                IngredientName = ingredientProduct?.Name ?? $"Product {log.IngredientProductId}",
                PortionsSold = log.PortionsSold,
                QuantityDeducted = log.QuantityDeducted,
                Unit = log.Unit.ToString(),
                StockBefore = log.StockBefore,
                StockAfter = log.StockAfter,
                Status = (DeductionStatusDto)log.Status,
                ErrorMessage = log.ErrorMessage,
                WasForced = log.WasForced,
                DeductedAt = log.DeductedAt,
                IsReversed = log.ReversedAt.HasValue,
                ReversedAt = log.ReversedAt
            });
        }

        return dtos;
    }

    #endregion
}
