using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for batch preparation management and ingredient usage tracking.
/// </summary>
public class BatchPrepService : IBatchPrepService
{
    private readonly IRepository<BatchPrep> _batchPrepRepository;
    private readonly IRepository<BatchPrepIngredient> _batchPrepIngredientRepository;
    private readonly IRepository<Recipe> _recipeRepository;
    private readonly IRepository<RecipeIngredient> _recipeIngredientRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<IngredientDeductionLog> _deductionLogRepository;
    private readonly IRepository<IngredientUsageSummary> _usageSummaryRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchPrepService> _logger;

    public BatchPrepService(
        IRepository<BatchPrep> batchPrepRepository,
        IRepository<BatchPrepIngredient> batchPrepIngredientRepository,
        IRepository<Recipe> recipeRepository,
        IRepository<RecipeIngredient> recipeIngredientRepository,
        IRepository<Product> productRepository,
        IRepository<IngredientDeductionLog> deductionLogRepository,
        IRepository<IngredientUsageSummary> usageSummaryRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        ILogger<BatchPrepService> logger)
    {
        _batchPrepRepository = batchPrepRepository ?? throw new ArgumentNullException(nameof(batchPrepRepository));
        _batchPrepIngredientRepository = batchPrepIngredientRepository ?? throw new ArgumentNullException(nameof(batchPrepIngredientRepository));
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _recipeIngredientRepository = recipeIngredientRepository ?? throw new ArgumentNullException(nameof(recipeIngredientRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _deductionLogRepository = deductionLogRepository ?? throw new ArgumentNullException(nameof(deductionLogRepository));
        _usageSummaryRepository = usageSummaryRepository ?? throw new ArgumentNullException(nameof(usageSummaryRepository));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Events

    public event EventHandler<BatchPrepDto>? BatchPrepStarted;
    public event EventHandler<BatchPrepCompleteResultDto>? BatchPrepCompleted;
    public event EventHandler<BatchPrepDto>? BatchPrepCancelled;
    public event EventHandler<List<DeductionLowStockWarningDto>>? LowStockDetected;

    #endregion

    #region Batch Prep CRUD Operations

    public async Task<BatchPrepDto> CreateBatchPrepAsync(CreateBatchPrepDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(dto.RecipeId);
        if (recipe == null || !recipe.IsActive)
        {
            throw new InvalidOperationException($"Recipe with ID {dto.RecipeId} not found or is inactive.");
        }

        var expectedYield = recipe.YieldQuantity * dto.BatchSize;

        var batchPrep = new BatchPrep
        {
            RecipeId = dto.RecipeId,
            StoreId = dto.StoreId,
            BatchSize = dto.BatchSize,
            ExpectedYield = expectedYield,
            YieldUnit = recipe.YieldUnit,
            PlannedAt = dto.PlannedAt ?? DateTime.UtcNow,
            Status = BatchPrepStatus.Planned,
            Notes = dto.Notes
        };

        await _batchPrepRepository.AddAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created batch prep {BatchPrepId} for recipe {RecipeId}", batchPrep.Id, recipe.Id);

        if (dto.StartImmediately)
        {
            await StartBatchPrepAsync(new StartBatchPrepDto
            {
                BatchPrepId = batchPrep.Id,
                DeductIngredients = true
            });
        }

        return await GetBatchPrepAsync(batchPrep.Id) ?? throw new InvalidOperationException("Failed to retrieve created batch prep.");
    }

    public async Task<BatchPrepDto?> GetBatchPrepAsync(int id)
    {
        var batchPrep = await _batchPrepRepository.GetByIdAsync(id);
        if (batchPrep == null) return null;

        var recipe = await _recipeRepository.GetByIdAsync(batchPrep.RecipeId);
        var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

        var ingredients = (await _batchPrepIngredientRepository.GetAllAsync())
            .Where(i => i.BatchPrepId == id && i.IsActive)
            .ToList();

        var ingredientDtos = new List<BatchPrepIngredientDto>();
        foreach (var ing in ingredients)
        {
            var ingProduct = await _productRepository.GetByIdAsync(ing.IngredientProductId);
            ingredientDtos.Add(new BatchPrepIngredientDto
            {
                Id = ing.Id,
                BatchPrepId = ing.BatchPrepId,
                IngredientProductId = ing.IngredientProductId,
                IngredientName = ingProduct?.Name ?? "Unknown",
                IngredientCode = ingProduct?.Code ?? "",
                PlannedQuantity = ing.ExpectedQuantity,
                ActualQuantity = ing.ActualQuantity,
                Unit = ing.Unit.ToString().ToLower(),
                UnitCost = ing.UnitCost,
                TotalCost = ing.TotalCost,
                WasDeducted = ing.DeductionSuccessful,
                Notes = ing.DeductionError
            });
        }

        return new BatchPrepDto
        {
            Id = batchPrep.Id,
            RecipeId = batchPrep.RecipeId,
            RecipeName = recipe?.Name ?? "Unknown",
            ProductName = product?.Name ?? "Unknown",
            StoreId = batchPrep.StoreId,
            BatchSize = batchPrep.BatchSize,
            ExpectedYield = batchPrep.ExpectedYield,
            ActualYield = batchPrep.ActualYield,
            YieldUnit = batchPrep.YieldUnit,
            Status = (BatchPrepStatusDto)batchPrep.Status,
            PreparedByUserId = batchPrep.PreparedByUserId,
            PlannedAt = batchPrep.PlannedAt,
            StartedAt = batchPrep.StartedAt,
            CompletedAt = batchPrep.CompletedAt,
            IngredientsDeducted = batchPrep.IngredientsDeducted,
            AddedToInventory = batchPrep.AddedToInventory,
            Notes = batchPrep.Notes,
            CreatedAt = batchPrep.CreatedAt,
            Ingredients = ingredientDtos,
            EstimatedCost = recipe?.TotalEstimatedCost * batchPrep.BatchSize ?? 0,
            ActualCost = batchPrep.IngredientCost
        };
    }

    public async Task<List<BatchPrepListDto>> GetBatchPrepsAsync(BatchPrepQueryDto query)
    {
        var batchPreps = (await _batchPrepRepository.GetAllAsync())
            .Where(bp => bp.IsActive);

        if (query.RecipeId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.RecipeId == query.RecipeId.Value);

        if (query.StoreId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.StoreId == query.StoreId.Value);

        if (query.Status.HasValue)
            batchPreps = batchPreps.Where(bp => bp.Status == (BatchPrepStatus)query.Status.Value);

        if (query.PreparedByUserId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.PreparedByUserId == query.PreparedByUserId.Value);

        if (query.FromDate.HasValue)
            batchPreps = batchPreps.Where(bp => bp.CreatedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            batchPreps = batchPreps.Where(bp => bp.CreatedAt <= query.ToDate.Value);

        if (!query.IncludeCancelled)
            batchPreps = batchPreps.Where(bp => bp.Status != BatchPrepStatus.Cancelled);

        batchPreps = query.SortBy?.ToLower() switch
        {
            "recipe" => query.SortDescending
                ? batchPreps.OrderByDescending(bp => bp.RecipeId)
                : batchPreps.OrderBy(bp => bp.RecipeId),
            "status" => query.SortDescending
                ? batchPreps.OrderByDescending(bp => bp.Status)
                : batchPreps.OrderBy(bp => bp.Status),
            "plannedat" => query.SortDescending
                ? batchPreps.OrderByDescending(bp => bp.PlannedAt)
                : batchPreps.OrderBy(bp => bp.PlannedAt),
            _ => query.SortDescending
                ? batchPreps.OrderByDescending(bp => bp.CreatedAt)
                : batchPreps.OrderBy(bp => bp.CreatedAt)
        };

        var list = batchPreps.Skip(query.Skip).Take(query.Take).ToList();

        var result = new List<BatchPrepListDto>();
        foreach (var bp in list)
        {
            var recipe = await _recipeRepository.GetByIdAsync(bp.RecipeId);
            var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

            result.Add(new BatchPrepListDto
            {
                Id = bp.Id,
                RecipeId = bp.RecipeId,
                RecipeName = recipe?.Name ?? "Unknown",
                ProductName = product?.Name ?? "Unknown",
                StoreId = bp.StoreId,
                BatchSize = bp.BatchSize,
                ExpectedYield = bp.ExpectedYield,
                ActualYield = bp.ActualYield > 0 ? bp.ActualYield : null,
                YieldUnit = bp.YieldUnit,
                Status = (BatchPrepStatusDto)bp.Status,
                PlannedAt = bp.PlannedAt,
                CompletedAt = bp.CompletedAt,
                EstimatedCost = recipe?.TotalEstimatedCost * bp.BatchSize ?? 0,
                CreatedAt = bp.CreatedAt
            });
        }

        return result;
    }

    public async Task<BatchPrepDto> UpdateBatchPrepAsync(int id, UpdateBatchPrepDto dto)
    {
        var batchPrep = await _batchPrepRepository.GetByIdAsync(id);
        if (batchPrep == null || !batchPrep.IsActive)
        {
            throw new InvalidOperationException($"Batch prep with ID {id} not found.");
        }

        if (batchPrep.Status != BatchPrepStatus.Planned)
        {
            throw new InvalidOperationException("Only planned batch preps can be updated.");
        }

        if (dto.PlannedAt.HasValue)
            batchPrep.PlannedAt = dto.PlannedAt.Value;

        if (dto.BatchSize.HasValue)
        {
            batchPrep.BatchSize = dto.BatchSize.Value;
            var recipe = await _recipeRepository.GetByIdAsync(batchPrep.RecipeId);
            if (recipe != null)
            {
                batchPrep.ExpectedYield = recipe.YieldQuantity * dto.BatchSize.Value;
            }
        }

        if (dto.Notes != null)
            batchPrep.Notes = dto.Notes;

        batchPrep.UpdatedAt = DateTime.UtcNow;
        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        return await GetBatchPrepAsync(id) ?? throw new InvalidOperationException("Failed to retrieve updated batch prep.");
    }

    public async Task<bool> DeleteBatchPrepAsync(int id)
    {
        var batchPrep = await _batchPrepRepository.GetByIdAsync(id);
        if (batchPrep == null) return false;

        if (batchPrep.Status != BatchPrepStatus.Planned)
        {
            throw new InvalidOperationException("Only planned batch preps can be deleted.");
        }

        batchPrep.IsActive = false;
        batchPrep.UpdatedAt = DateTime.UtcNow;
        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted batch prep {BatchPrepId}", id);
        return true;
    }

    #endregion

    #region Batch Prep Workflow

    public async Task<BatchPrepStartResultDto> StartBatchPrepAsync(StartBatchPrepDto dto)
    {
        var result = new BatchPrepStartResultDto
        {
            BatchPrepId = dto.BatchPrepId
        };

        var batchPrep = await _batchPrepRepository.GetByIdAsync(dto.BatchPrepId);
        if (batchPrep == null || !batchPrep.IsActive)
        {
            result.Errors.Add($"Batch prep with ID {dto.BatchPrepId} not found.");
            return result;
        }

        if (batchPrep.Status != BatchPrepStatus.Planned)
        {
            result.Errors.Add($"Batch prep is not in Planned status. Current status: {batchPrep.Status}");
            return result;
        }

        var recipe = await _recipeRepository.GetByIdAsync(batchPrep.RecipeId);
        if (recipe == null)
        {
            result.Errors.Add($"Recipe with ID {batchPrep.RecipeId} not found.");
            return result;
        }

        // Get recipe ingredients
        var recipeIngredients = (await _recipeIngredientRepository.GetAllAsync())
            .Where(ri => ri.RecipeId == batchPrep.RecipeId && ri.IsActive)
            .ToList();

        var lowStockWarnings = new List<DeductionLowStockWarningDto>();
        var totalCost = 0m;

        // Process ingredients
        foreach (var ri in recipeIngredients)
        {
            var ingredientProduct = await _productRepository.GetByIdAsync(ri.IngredientProductId);
            if (ingredientProduct == null) continue;

            var requiredQty = ri.EffectiveQuantity * batchPrep.BatchSize;
            var currentStock = await _inventoryService.GetStockLevelAsync(ri.IngredientProductId);
            var unitCost = ingredientProduct.CostPrice;
            var lineCost = requiredQty * unitCost;
            totalCost += lineCost;

            var bpIngredient = new BatchPrepIngredient
            {
                BatchPrepId = batchPrep.Id,
                IngredientProductId = ri.IngredientProductId,
                ExpectedQuantity = requiredQty,
                ActualQuantity = requiredQty,
                Unit = ri.Unit,
                UnitCost = unitCost,
                TotalCost = lineCost,
                StockBefore = currentStock
            };

            if (dto.DeductIngredients)
            {
                if (currentStock < requiredQty)
                {
                    lowStockWarnings.Add(new DeductionLowStockWarningDto
                    {
                        IngredientProductId = ri.IngredientProductId,
                        IngredientName = ingredientProduct.Name,
                        CurrentStock = currentStock,
                        QuantityDeducted = requiredQty,
                        IsNegative = currentStock - requiredQty < 0,
                        WarningMessage = $"Insufficient stock for {ingredientProduct.Name}: need {requiredQty}, have {currentStock}"
                    });
                    result.Warnings.Add($"Low stock warning: {ingredientProduct.Name}");
                }

                try
                {
                    var movement = await _inventoryService.DeductStockAsync(
                        ri.IngredientProductId,
                        requiredQty,
                        $"Batch Prep #{batchPrep.Id}: {recipe.Name}",
                        batchPrep.Id);

                    bpIngredient.StockAfter = await _inventoryService.GetStockLevelAsync(ri.IngredientProductId);
                    bpIngredient.DeductionSuccessful = true;

                    result.DeductionResults.Add(new IngredientDeductionResultDto
                    {
                        IngredientProductId = ri.IngredientProductId,
                        IngredientName = ingredientProduct.Name,
                        QuantityDeducted = requiredQty,
                        Unit = ri.Unit.ToString().ToLower(),
                        StockBefore = currentStock,
                        StockAfter = bpIngredient.StockAfter,
                        Status = DeductionStatusDto.Success
                    });
                }
                catch (Exception ex)
                {
                    bpIngredient.DeductionSuccessful = false;
                    bpIngredient.DeductionError = ex.Message;
                    bpIngredient.StockAfter = currentStock;

                    result.DeductionResults.Add(new IngredientDeductionResultDto
                    {
                        IngredientProductId = ri.IngredientProductId,
                        IngredientName = ingredientProduct.Name,
                        QuantityDeducted = 0,
                        StockBefore = currentStock,
                        StockAfter = currentStock,
                        Status = DeductionStatusDto.Failed,
                        Error = ex.Message
                    });

                    result.Warnings.Add($"Failed to deduct {ingredientProduct.Name}: {ex.Message}");
                }
            }

            await _batchPrepIngredientRepository.AddAsync(bpIngredient);
        }

        batchPrep.Status = BatchPrepStatus.InProgress;
        batchPrep.StartedAt = DateTime.UtcNow;
        batchPrep.PreparedByUserId = dto.PreparedByUserId;
        batchPrep.IngredientsDeducted = dto.DeductIngredients;
        batchPrep.IngredientCost = totalCost;
        batchPrep.UpdatedAt = DateTime.UtcNow;

        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        result.Success = true;
        result.IngredientsDeducted = dto.DeductIngredients;

        _logger.LogInformation("Started batch prep {BatchPrepId} for recipe {RecipeId}", batchPrep.Id, recipe.Id);

        var batchPrepDto = await GetBatchPrepAsync(batchPrep.Id);
        BatchPrepStarted?.Invoke(this, batchPrepDto!);

        if (lowStockWarnings.Count > 0)
        {
            LowStockDetected?.Invoke(this, lowStockWarnings);
        }

        return result;
    }

    public async Task<BatchPrepCompleteResultDto> CompleteBatchPrepAsync(CompleteBatchPrepDto dto)
    {
        var result = new BatchPrepCompleteResultDto
        {
            BatchPrepId = dto.BatchPrepId
        };

        var batchPrep = await _batchPrepRepository.GetByIdAsync(dto.BatchPrepId);
        if (batchPrep == null || !batchPrep.IsActive)
        {
            result.Errors.Add($"Batch prep with ID {dto.BatchPrepId} not found.");
            return result;
        }

        if (batchPrep.Status != BatchPrepStatus.InProgress)
        {
            result.Errors.Add($"Batch prep is not in InProgress status. Current status: {batchPrep.Status}");
            return result;
        }

        var recipe = await _recipeRepository.GetByIdAsync(batchPrep.RecipeId);
        if (recipe == null)
        {
            result.Errors.Add($"Recipe with ID {batchPrep.RecipeId} not found.");
            return result;
        }

        batchPrep.ActualYield = dto.ActualYield;
        batchPrep.CompletedAt = DateTime.UtcNow;
        batchPrep.Status = dto.WastedQuantity.HasValue && dto.WastedQuantity > 0
            ? BatchPrepStatus.Partial
            : BatchPrepStatus.Completed;

        if (dto.Notes != null)
        {
            batchPrep.Notes = string.IsNullOrEmpty(batchPrep.Notes)
                ? dto.Notes
                : $"{batchPrep.Notes}\n{dto.Notes}";
        }

        // Add prepped item to inventory
        if (dto.AddToInventory && recipe.ProductId > 0)
        {
            try
            {
                var costPerUnit = batchPrep.IngredientCost / (dto.ActualYield > 0 ? dto.ActualYield : 1);
                batchPrep.CostPerUnit = costPerUnit;

                var movement = await _inventoryService.ReceiveStockAsync(
                    recipe.ProductId,
                    dto.ActualYield,
                    costPerUnit,
                    $"Batch Prep #{batchPrep.Id}: {recipe.Name}",
                    batchPrep.Id);

                batchPrep.AddedToInventory = true;
                batchPrep.InventoryTransactionId = movement?.Id;

                result.AddedToInventory = true;
                result.InventoryTransactionId = movement?.Id;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to add to inventory: {ex.Message}");
                _logger.LogError(ex, "Failed to add batch prep {BatchPrepId} to inventory", batchPrep.Id);
            }
        }

        batchPrep.UpdatedAt = DateTime.UtcNow;
        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        result.Success = true;
        result.ActualYield = dto.ActualYield;
        result.YieldVariance = dto.ActualYield - batchPrep.ExpectedYield;
        result.YieldVariancePercent = batchPrep.ExpectedYield > 0
            ? (result.YieldVariance / batchPrep.ExpectedYield) * 100
            : 0;
        result.TotalCost = batchPrep.IngredientCost;
        result.CostPerUnit = batchPrep.CostPerUnit;

        _logger.LogInformation("Completed batch prep {BatchPrepId} with yield {ActualYield}", batchPrep.Id, dto.ActualYield);

        BatchPrepCompleted?.Invoke(this, result);

        return result;
    }

    public async Task<BatchPrepDto> CancelBatchPrepAsync(CancelBatchPrepDto dto)
    {
        var batchPrep = await _batchPrepRepository.GetByIdAsync(dto.BatchPrepId);
        if (batchPrep == null || !batchPrep.IsActive)
        {
            throw new InvalidOperationException($"Batch prep with ID {dto.BatchPrepId} not found.");
        }

        if (batchPrep.Status == BatchPrepStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed batch prep.");
        }

        // Reverse deductions if requested and ingredients were deducted
        if (dto.ReverseDeductions && batchPrep.IngredientsDeducted)
        {
            var ingredients = (await _batchPrepIngredientRepository.GetAllAsync())
                .Where(i => i.BatchPrepId == batchPrep.Id && i.DeductionSuccessful)
                .ToList();

            foreach (var ing in ingredients)
            {
                try
                {
                    await _inventoryService.RestoreStockAsync(
                        ing.IngredientProductId,
                        ing.ActualQuantity,
                        Core.Enums.MovementType.Adjustment,
                        $"Batch Prep #{batchPrep.Id} cancelled: {dto.Reason}",
                        batchPrep.Id);

                    ing.DeductionSuccessful = false;
                    ing.DeductionError = "Reversed due to cancellation";
                    await _batchPrepIngredientRepository.UpdateAsync(ing);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reverse deduction for ingredient {IngredientId} in batch prep {BatchPrepId}",
                        ing.IngredientProductId, batchPrep.Id);
                }
            }

            batchPrep.IngredientsDeducted = false;
        }

        batchPrep.Status = BatchPrepStatus.Cancelled;
        batchPrep.Notes = string.IsNullOrEmpty(batchPrep.Notes)
            ? $"Cancelled: {dto.Reason}"
            : $"{batchPrep.Notes}\nCancelled: {dto.Reason}";
        batchPrep.UpdatedAt = DateTime.UtcNow;

        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cancelled batch prep {BatchPrepId}: {Reason}", batchPrep.Id, dto.Reason);

        var batchPrepDto = await GetBatchPrepAsync(batchPrep.Id);
        BatchPrepCancelled?.Invoke(this, batchPrepDto!);

        return batchPrepDto!;
    }

    public async Task<BatchPrepDto> RecordWasteAsync(int id, decimal wastedQuantity, string reason)
    {
        var batchPrep = await _batchPrepRepository.GetByIdAsync(id);
        if (batchPrep == null || !batchPrep.IsActive)
        {
            throw new InvalidOperationException($"Batch prep with ID {id} not found.");
        }

        if (batchPrep.Status != BatchPrepStatus.InProgress && batchPrep.Status != BatchPrepStatus.Completed)
        {
            throw new InvalidOperationException("Can only record waste for in-progress or completed batch preps.");
        }

        batchPrep.Status = BatchPrepStatus.Wasted;
        batchPrep.Notes = string.IsNullOrEmpty(batchPrep.Notes)
            ? $"Wasted: {wastedQuantity} - {reason}"
            : $"{batchPrep.Notes}\nWasted: {wastedQuantity} - {reason}";
        batchPrep.UpdatedAt = DateTime.UtcNow;

        await _batchPrepRepository.UpdateAsync(batchPrep);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning("Recorded waste for batch prep {BatchPrepId}: {WastedQuantity} - {Reason}",
            id, wastedQuantity, reason);

        return await GetBatchPrepAsync(id) ?? throw new InvalidOperationException("Failed to retrieve updated batch prep.");
    }

    #endregion

    #region Batch Prep Queries

    public async Task<List<BatchPrepListDto>> GetBatchPrepHistoryAsync(int recipeId, DateTime fromDate, DateTime toDate)
    {
        return await GetBatchPrepsAsync(new BatchPrepQueryDto
        {
            RecipeId = recipeId,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeCancelled = true
        });
    }

    public async Task<List<BatchPrepListDto>> GetPendingBatchPrepsAsync(int? storeId = null)
    {
        var batchPreps = (await _batchPrepRepository.GetAllAsync())
            .Where(bp => bp.IsActive &&
                         (bp.Status == BatchPrepStatus.Planned || bp.Status == BatchPrepStatus.InProgress));

        if (storeId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.StoreId == storeId.Value);

        var list = batchPreps.OrderBy(bp => bp.PlannedAt).ToList();

        var result = new List<BatchPrepListDto>();
        foreach (var bp in list)
        {
            var recipe = await _recipeRepository.GetByIdAsync(bp.RecipeId);
            var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

            result.Add(new BatchPrepListDto
            {
                Id = bp.Id,
                RecipeId = bp.RecipeId,
                RecipeName = recipe?.Name ?? "Unknown",
                ProductName = product?.Name ?? "Unknown",
                StoreId = bp.StoreId,
                BatchSize = bp.BatchSize,
                ExpectedYield = bp.ExpectedYield,
                YieldUnit = bp.YieldUnit,
                Status = (BatchPrepStatusDto)bp.Status,
                PlannedAt = bp.PlannedAt,
                EstimatedCost = recipe?.TotalEstimatedCost * bp.BatchSize ?? 0,
                CreatedAt = bp.CreatedAt
            });
        }

        return result;
    }

    public async Task<BatchPrepSummaryDto> GetBatchPrepSummaryAsync(DateTime fromDate, DateTime toDate, int? storeId = null)
    {
        var batchPreps = (await _batchPrepRepository.GetAllAsync())
            .Where(bp => bp.IsActive && bp.CreatedAt >= fromDate && bp.CreatedAt <= toDate);

        if (storeId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.StoreId == storeId.Value);

        var list = batchPreps.ToList();

        var completed = list.Where(bp => bp.Status == BatchPrepStatus.Completed || bp.Status == BatchPrepStatus.Partial).ToList();
        var yieldVariances = completed.Where(bp => bp.ExpectedYield > 0)
            .Select(bp => bp.YieldVariancePercent)
            .ToList();

        var summary = new BatchPrepSummaryDto
        {
            TotalBatchPreps = list.Count,
            PlannedPreps = list.Count(bp => bp.Status == BatchPrepStatus.Planned),
            InProgressPreps = list.Count(bp => bp.Status == BatchPrepStatus.InProgress),
            CompletedPreps = list.Count(bp => bp.Status == BatchPrepStatus.Completed),
            CancelledPreps = list.Count(bp => bp.Status == BatchPrepStatus.Cancelled),
            WastedPreps = list.Count(bp => bp.Status == BatchPrepStatus.Wasted),
            TotalYieldProduced = completed.Sum(bp => bp.ActualYield),
            AverageYieldVariancePercent = yieldVariances.Count > 0 ? yieldVariances.Average() : 0,
            TotalIngredientCost = list.Sum(bp => bp.IngredientCost),
            AverageBatchCost = list.Count > 0 ? list.Average(bp => bp.IngredientCost) : 0
        };

        // Recent preps
        var recentPreps = list.OrderByDescending(bp => bp.CreatedAt).Take(10).ToList();
        foreach (var bp in recentPreps)
        {
            var recipe = await _recipeRepository.GetByIdAsync(bp.RecipeId);
            var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

            summary.RecentPreps.Add(new BatchPrepListDto
            {
                Id = bp.Id,
                RecipeId = bp.RecipeId,
                RecipeName = recipe?.Name ?? "Unknown",
                ProductName = product?.Name ?? "Unknown",
                BatchSize = bp.BatchSize,
                ExpectedYield = bp.ExpectedYield,
                ActualYield = bp.ActualYield > 0 ? bp.ActualYield : null,
                YieldUnit = bp.YieldUnit,
                Status = (BatchPrepStatusDto)bp.Status,
                PlannedAt = bp.PlannedAt,
                CompletedAt = bp.CompletedAt,
                EstimatedCost = bp.IngredientCost,
                CreatedAt = bp.CreatedAt
            });
        }

        // By recipe
        var byRecipe = list.GroupBy(bp => bp.RecipeId);
        foreach (var group in byRecipe)
        {
            var recipe = await _recipeRepository.GetByIdAsync(group.Key);
            var recipeBatches = group.ToList();
            var recipeCompleted = recipeBatches.Where(bp => bp.Status == BatchPrepStatus.Completed || bp.Status == BatchPrepStatus.Partial).ToList();

            summary.ByRecipe.Add(new RecipePrepSummaryDto
            {
                RecipeId = group.Key,
                RecipeName = recipe?.Name ?? "Unknown",
                PrepCount = recipeBatches.Count,
                TotalBatches = recipeBatches.Sum(bp => bp.BatchSize),
                TotalYield = recipeCompleted.Sum(bp => bp.ActualYield),
                AverageYieldVariancePercent = recipeCompleted.Count > 0
                    ? recipeCompleted.Where(bp => bp.ExpectedYield > 0).Average(bp => bp.YieldVariancePercent)
                    : 0,
                TotalCost = recipeBatches.Sum(bp => bp.IngredientCost)
            });
        }

        return summary;
    }

    public async Task<List<BatchPrepIngredientDto>> GetRequiredIngredientsAsync(int recipeId, decimal batchSize)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new InvalidOperationException($"Recipe with ID {recipeId} not found.");
        }

        var recipeIngredients = (await _recipeIngredientRepository.GetAllAsync())
            .Where(ri => ri.RecipeId == recipeId && ri.IsActive)
            .ToList();

        var result = new List<BatchPrepIngredientDto>();
        foreach (var ri in recipeIngredients)
        {
            var ingredientProduct = await _productRepository.GetByIdAsync(ri.IngredientProductId);
            if (ingredientProduct == null) continue;

            var requiredQty = ri.EffectiveQuantity * batchSize;
            var currentStock = await _inventoryService.GetStockLevelAsync(ri.IngredientProductId);

            result.Add(new BatchPrepIngredientDto
            {
                IngredientProductId = ri.IngredientProductId,
                IngredientName = ingredientProduct.Name,
                IngredientCode = ingredientProduct.Code,
                PlannedQuantity = requiredQty,
                ActualQuantity = requiredQty,
                Unit = ri.Unit.ToString().ToLower(),
                UnitCost = ingredientProduct.CostPrice,
                TotalCost = requiredQty * ingredientProduct.CostPrice
            });
        }

        return result;
    }

    public async Task<BatchPrepValidationResultDto> ValidateBatchPrepAsync(int recipeId, decimal batchSize, int? storeId = null)
    {
        var result = new BatchPrepValidationResultDto();

        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null || !recipe.IsActive)
        {
            result.Errors.Add($"Recipe with ID {recipeId} not found or is inactive.");
            return result;
        }

        result.HasRecipe = true;
        result.RecipeId = recipeId;
        result.RecipeName = recipe.Name;
        result.ExpectedYield = recipe.YieldQuantity * batchSize;
        result.YieldUnit = recipe.YieldUnit;

        var recipeIngredients = (await _recipeIngredientRepository.GetAllAsync())
            .Where(ri => ri.RecipeId == recipeId && ri.IsActive)
            .ToList();

        var totalCost = 0m;
        var canStart = true;

        foreach (var ri in recipeIngredients)
        {
            var ingredientProduct = await _productRepository.GetByIdAsync(ri.IngredientProductId);
            if (ingredientProduct == null)
            {
                result.Errors.Add($"Ingredient product {ri.IngredientProductId} not found.");
                canStart = false;
                continue;
            }

            var requiredQty = ri.EffectiveQuantity * batchSize;
            var currentStock = await _inventoryService.GetStockLevelAsync(ri.IngredientProductId);
            var lineCost = requiredQty * ingredientProduct.CostPrice;
            totalCost += lineCost;

            var availability = new BatchPrepIngredientAvailabilityDto
            {
                IngredientProductId = ri.IngredientProductId,
                IngredientName = ingredientProduct.Name,
                RequiredQuantity = requiredQty,
                AvailableStock = currentStock,
                Unit = ri.Unit.ToString().ToLower(),
                IsSufficient = currentStock >= requiredQty,
                Shortage = currentStock < requiredQty ? requiredQty - currentStock : 0,
                UnitCost = ingredientProduct.CostPrice,
                TotalCost = lineCost
            };

            result.IngredientAvailability.Add(availability);

            if (!availability.IsSufficient)
            {
                result.Warnings.Add($"Insufficient stock for {ingredientProduct.Name}: need {requiredQty}, have {currentStock}");
            }
        }

        result.CanStart = canStart;
        result.EstimatedCost = totalCost;

        return result;
    }

    #endregion

    #region Ingredient Usage Reporting

    public async Task<IngredientUsageReportDto> GetIngredientUsageReportAsync(IngredientUsageQueryDto query)
    {
        var report = new IngredientUsageReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            StoreId = query.StoreId,
            GeneratedAt = DateTime.UtcNow
        };

        // Get deductions from sales
        var salesDeductions = new Dictionary<int, (decimal qty, decimal cost, int count)>();
        if (query.IncludeSales)
        {
            var deductions = (await _deductionLogRepository.GetAllAsync())
                .Where(d => d.DeductedAt >= query.FromDate && d.DeductedAt <= query.ToDate &&
                            d.Status == DeductionStatus.Success && d.ReversedAt == null);

            if (query.StoreId.HasValue)
                deductions = deductions.Where(d => d.StoreId == query.StoreId.Value);

            if (query.RecipeId.HasValue)
                deductions = deductions.Where(d => d.RecipeId == query.RecipeId.Value);

            if (query.IngredientProductId.HasValue)
                deductions = deductions.Where(d => d.IngredientProductId == query.IngredientProductId.Value);

            foreach (var d in deductions)
            {
                if (!salesDeductions.ContainsKey(d.IngredientProductId))
                    salesDeductions[d.IngredientProductId] = (0, 0, 0);

                var (qty, cost, count) = salesDeductions[d.IngredientProductId];
                var product = await _productRepository.GetByIdAsync(d.IngredientProductId);
                var unitCost = product?.CostPrice ?? 0;
                salesDeductions[d.IngredientProductId] = (qty + d.QuantityDeducted, cost + (d.QuantityDeducted * unitCost), count + 1);
            }
        }

        // Get usage from batch preps
        var prepDeductions = new Dictionary<int, (decimal qty, decimal cost, int count)>();
        if (query.IncludeBatchPreps)
        {
            var batchPreps = (await _batchPrepRepository.GetAllAsync())
                .Where(bp => bp.CreatedAt >= query.FromDate && bp.CreatedAt <= query.ToDate &&
                             bp.IngredientsDeducted && bp.Status != BatchPrepStatus.Cancelled);

            if (query.StoreId.HasValue)
                batchPreps = batchPreps.Where(bp => bp.StoreId == query.StoreId.Value);

            if (query.RecipeId.HasValue)
                batchPreps = batchPreps.Where(bp => bp.RecipeId == query.RecipeId.Value);

            foreach (var bp in batchPreps)
            {
                var ingredients = (await _batchPrepIngredientRepository.GetAllAsync())
                    .Where(i => i.BatchPrepId == bp.Id && i.DeductionSuccessful);

                if (query.IngredientProductId.HasValue)
                    ingredients = ingredients.Where(i => i.IngredientProductId == query.IngredientProductId.Value);

                foreach (var ing in ingredients)
                {
                    if (!prepDeductions.ContainsKey(ing.IngredientProductId))
                        prepDeductions[ing.IngredientProductId] = (0, 0, 0);

                    var (qty, cost, count) = prepDeductions[ing.IngredientProductId];
                    prepDeductions[ing.IngredientProductId] = (qty + ing.ActualQuantity, cost + ing.TotalCost, count + 1);
                }
            }
        }

        // Combine all ingredient IDs
        var allIngredientIds = salesDeductions.Keys.Union(prepDeductions.Keys).Distinct();

        foreach (var ingredientId in allIngredientIds)
        {
            var product = await _productRepository.GetByIdAsync(ingredientId);
            if (product == null) continue;

            var salesUsage = salesDeductions.GetValueOrDefault(ingredientId);
            var prepUsage = prepDeductions.GetValueOrDefault(ingredientId);
            var totalQty = salesUsage.qty + prepUsage.qty;
            var totalCost = salesUsage.cost + prepUsage.cost;

            var line = new IngredientUsageLineDto
            {
                IngredientProductId = ingredientId,
                IngredientName = product.Name,
                IngredientCode = product.Code,
                TotalQuantityUsed = totalQty,
                Unit = "unit",
                AverageUnitCost = totalQty > 0 ? totalCost / totalQty : 0,
                TotalCost = totalCost,
                SalesUsage = salesUsage.qty,
                PrepUsage = prepUsage.qty,
                CurrentStock = await _inventoryService.GetStockLevelAsync(ingredientId)
            };

            var days = (query.ToDate - query.FromDate).Days;
            if (days > 0 && totalQty > 0)
            {
                var avgDailyUsage = totalQty / days;
                line.DaysOfStock = avgDailyUsage > 0 ? (int)(line.CurrentStock / avgDailyUsage) : 999;
            }

            report.Lines.Add(line);
            report.TotalUsageQuantity += totalQty;
            report.TotalUsageCost += totalCost;
            report.TotalSalesUsage += salesUsage.qty;
            report.TotalPrepUsage += prepUsage.qty;
        }

        report.TotalIngredients = report.Lines.Count;

        // Calculate percent of total
        foreach (var line in report.Lines)
        {
            line.PercentOfTotal = report.TotalUsageCost > 0
                ? (line.TotalCost / report.TotalUsageCost) * 100
                : 0;
        }

        return report;
    }

    public async Task<List<TopIngredientUsageDto>> GetTopIngredientsAsync(DateTime fromDate, DateTime toDate, int top = 10, int? storeId = null)
    {
        var report = await GetIngredientUsageReportAsync(new IngredientUsageQueryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            StoreId = storeId
        });

        var days = (toDate - fromDate).Days;
        if (days < 1) days = 1;

        return report.Lines
            .OrderByDescending(l => l.TotalCost)
            .Take(top)
            .Select((l, i) => new TopIngredientUsageDto
            {
                Rank = i + 1,
                IngredientProductId = l.IngredientProductId,
                IngredientName = l.IngredientName,
                TotalQuantity = l.TotalQuantityUsed,
                Unit = l.Unit,
                TotalCost = l.TotalCost,
                PercentOfTotalCost = l.PercentOfTotal,
                AverageDailyUsage = l.TotalQuantityUsed / days
            })
            .ToList();
    }

    public async Task<IngredientUsageTrendDto> GetIngredientUsageTrendAsync(int ingredientProductId, DateTime fromDate, DateTime toDate, int? storeId = null)
    {
        var product = await _productRepository.GetByIdAsync(ingredientProductId);
        if (product == null)
        {
            throw new InvalidOperationException($"Ingredient product with ID {ingredientProductId} not found.");
        }

        var trend = new IngredientUsageTrendDto
        {
            IngredientProductId = ingredientProductId,
            IngredientName = product.Name
        };

        // Get daily usage
        var currentDate = fromDate.Date;
        var totalUsage = 0m;
        var peakUsage = 0m;
        DateTime peakDate = fromDate;

        while (currentDate <= toDate.Date)
        {
            var dayStart = currentDate;
            var dayEnd = currentDate.AddDays(1).AddSeconds(-1);

            var dayReport = await GetIngredientUsageReportAsync(new IngredientUsageQueryDto
            {
                FromDate = dayStart,
                ToDate = dayEnd,
                StoreId = storeId,
                IngredientProductId = ingredientProductId
            });

            var dayUsage = dayReport.Lines.FirstOrDefault();
            var point = new UsageTrendPointDto
            {
                Date = currentDate,
                Quantity = dayUsage?.TotalQuantityUsed ?? 0,
                Cost = dayUsage?.TotalCost ?? 0
            };

            trend.TrendPoints.Add(point);
            totalUsage += point.Quantity;

            if (point.Quantity > peakUsage)
            {
                peakUsage = point.Quantity;
                peakDate = currentDate;
            }

            currentDate = currentDate.AddDays(1);
        }

        var days = trend.TrendPoints.Count;
        trend.AverageDailyUsage = days > 0 ? totalUsage / days : 0;
        trend.PeakUsage = peakUsage;
        trend.PeakDate = peakDate;

        // Determine trend direction
        if (trend.TrendPoints.Count >= 2)
        {
            var firstHalf = trend.TrendPoints.Take(trend.TrendPoints.Count / 2).Average(p => p.Quantity);
            var secondHalf = trend.TrendPoints.Skip(trend.TrendPoints.Count / 2).Average(p => p.Quantity);
            var change = firstHalf > 0 ? ((secondHalf - firstHalf) / firstHalf) * 100 : 0;

            trend.PercentChangeOverPeriod = change;
            trend.TrendDirection = change > 5 ? "Increasing" : change < -5 ? "Decreasing" : "Stable";
        }

        return trend;
    }

    public async Task<UsageComparisonDto> CompareUsagePeriodsAsync(
        DateTime currentStart, DateTime currentEnd,
        DateTime previousStart, DateTime previousEnd,
        int? storeId = null)
    {
        var currentReport = await GetIngredientUsageReportAsync(new IngredientUsageQueryDto
        {
            FromDate = currentStart,
            ToDate = currentEnd,
            StoreId = storeId
        });

        var previousReport = await GetIngredientUsageReportAsync(new IngredientUsageQueryDto
        {
            FromDate = previousStart,
            ToDate = previousEnd,
            StoreId = storeId
        });

        var comparison = new UsageComparisonDto
        {
            CurrentPeriodStart = currentStart,
            CurrentPeriodEnd = currentEnd,
            PreviousPeriodStart = previousStart,
            PreviousPeriodEnd = previousEnd,
            CurrentPeriodTotal = currentReport.TotalUsageCost,
            PreviousPeriodTotal = previousReport.TotalUsageCost
        };

        comparison.ChangeAmount = comparison.CurrentPeriodTotal - comparison.PreviousPeriodTotal;
        comparison.ChangePercent = comparison.PreviousPeriodTotal > 0
            ? (comparison.ChangeAmount / comparison.PreviousPeriodTotal) * 100
            : 0;

        // Compare by ingredient
        var allIngredientIds = currentReport.Lines.Select(l => l.IngredientProductId)
            .Union(previousReport.Lines.Select(l => l.IngredientProductId))
            .Distinct();

        foreach (var ingredientId in allIngredientIds)
        {
            var currentLine = currentReport.Lines.FirstOrDefault(l => l.IngredientProductId == ingredientId);
            var previousLine = previousReport.Lines.FirstOrDefault(l => l.IngredientProductId == ingredientId);

            var change = new IngredientUsageChangeDto
            {
                IngredientProductId = ingredientId,
                IngredientName = currentLine?.IngredientName ?? previousLine?.IngredientName ?? "Unknown",
                CurrentQuantity = currentLine?.TotalQuantityUsed ?? 0,
                PreviousQuantity = previousLine?.TotalQuantityUsed ?? 0,
                CurrentCost = currentLine?.TotalCost ?? 0,
                PreviousCost = previousLine?.TotalCost ?? 0
            };

            change.ChangeAmount = change.CurrentQuantity - change.PreviousQuantity;
            change.ChangePercent = change.PreviousQuantity > 0
                ? (change.ChangeAmount / change.PreviousQuantity) * 100
                : 0;
            change.CostChange = change.CurrentCost - change.PreviousCost;

            comparison.IngredientChanges.Add(change);
        }

        return comparison;
    }

    public async Task<List<RecipeUsageSummaryDto>> GetUsageByRecipeAsync(DateTime fromDate, DateTime toDate, int? storeId = null)
    {
        var result = new List<RecipeUsageSummaryDto>();

        // Get deductions grouped by recipe
        var deductions = (await _deductionLogRepository.GetAllAsync())
            .Where(d => d.DeductedAt >= fromDate && d.DeductedAt <= toDate &&
                        d.Status == DeductionStatus.Success && d.ReversedAt == null);

        if (storeId.HasValue)
            deductions = deductions.Where(d => d.StoreId == storeId.Value);

        var byRecipe = deductions.GroupBy(d => d.RecipeId);

        foreach (var group in byRecipe)
        {
            var recipe = await _recipeRepository.GetByIdAsync(group.Key);
            var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

            var recipeDeductions = group.ToList();
            var uniqueIngredients = recipeDeductions.Select(d => d.IngredientProductId).Distinct().Count();
            var portionsSold = (int)recipeDeductions.Sum(d => d.PortionsSold);

            var summary = new RecipeUsageSummaryDto
            {
                RecipeId = group.Key,
                RecipeName = recipe?.Name ?? "Unknown",
                ProductName = product?.Name ?? "Unknown",
                PortionsSold = portionsSold,
                UniqueIngredientsUsed = uniqueIngredients
            };

            // Get ingredient usage
            var byIngredient = recipeDeductions.GroupBy(d => d.IngredientProductId);
            foreach (var ingGroup in byIngredient)
            {
                var ingProduct = await _productRepository.GetByIdAsync(ingGroup.Key);
                var totalQty = ingGroup.Sum(d => d.QuantityDeducted);
                var unitCost = ingProduct?.CostPrice ?? 0;

                summary.Ingredients.Add(new IngredientUsageByRecipeDto
                {
                    IngredientProductId = ingGroup.Key,
                    IngredientName = ingProduct?.Name ?? "Unknown",
                    QuantityUsed = totalQty,
                    Unit = "unit",
                    TotalCost = totalQty * unitCost
                });

                summary.TotalIngredientCost += totalQty * unitCost;
            }

            result.Add(summary);
        }

        // Add batch prep counts
        var batchPreps = (await _batchPrepRepository.GetAllAsync())
            .Where(bp => bp.CreatedAt >= fromDate && bp.CreatedAt <= toDate &&
                         (bp.Status == BatchPrepStatus.Completed || bp.Status == BatchPrepStatus.Partial));

        if (storeId.HasValue)
            batchPreps = batchPreps.Where(bp => bp.StoreId == storeId.Value);

        var prepsByRecipe = batchPreps.GroupBy(bp => bp.RecipeId);
        foreach (var group in prepsByRecipe)
        {
            var existing = result.FirstOrDefault(r => r.RecipeId == group.Key);
            if (existing != null)
            {
                existing.BatchesPrepped = group.Count();
            }
            else
            {
                var recipe = await _recipeRepository.GetByIdAsync(group.Key);
                var product = recipe != null ? await _productRepository.GetByIdAsync(recipe.ProductId) : null;

                result.Add(new RecipeUsageSummaryDto
                {
                    RecipeId = group.Key,
                    RecipeName = recipe?.Name ?? "Unknown",
                    ProductName = product?.Name ?? "Unknown",
                    BatchesPrepped = group.Count()
                });
            }
        }

        return result.OrderByDescending(r => r.TotalIngredientCost).ToList();
    }

    public async Task<List<DailyUsageSummaryDto>> GetDailyUsageAsync(DateTime fromDate, DateTime toDate, int? storeId = null)
    {
        var result = new List<DailyUsageSummaryDto>();
        var currentDate = fromDate.Date;

        while (currentDate <= toDate.Date)
        {
            var dayStart = currentDate;
            var dayEnd = currentDate.AddDays(1).AddSeconds(-1);

            var dayReport = await GetIngredientUsageReportAsync(new IngredientUsageQueryDto
            {
                FromDate = dayStart,
                ToDate = dayEnd,
                StoreId = storeId
            });

            // Get batch preps for the day
            var batchPreps = (await _batchPrepRepository.GetAllAsync())
                .Where(bp => bp.CreatedAt >= dayStart && bp.CreatedAt <= dayEnd &&
                             (bp.Status == BatchPrepStatus.Completed || bp.Status == BatchPrepStatus.Partial));

            if (storeId.HasValue)
                batchPreps = batchPreps.Where(bp => bp.StoreId == storeId.Value);

            // Get portions sold
            var deductions = (await _deductionLogRepository.GetAllAsync())
                .Where(d => d.DeductedAt >= dayStart && d.DeductedAt <= dayEnd &&
                            d.Status == DeductionStatus.Success && d.ReversedAt == null);

            if (storeId.HasValue)
                deductions = deductions.Where(d => d.StoreId == storeId.Value);

            result.Add(new DailyUsageSummaryDto
            {
                Date = currentDate,
                TotalQuantity = dayReport.TotalUsageQuantity,
                TotalCost = dayReport.TotalUsageCost,
                UniqueIngredients = dayReport.Lines.Count,
                PortionsSold = (int)deductions.Sum(d => d.PortionsSold),
                BatchesPrepped = batchPreps.Count()
            });

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    #endregion
}
