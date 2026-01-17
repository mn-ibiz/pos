using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for label printing operations and job management.
/// </summary>
public class LabelPrintService : ILabelPrintService
{
    private readonly IRepository<LabelPrintJob> _jobRepository;
    private readonly IRepository<LabelPrintJobItem> _itemRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly ILabelPrinterService _printerService;
    private readonly ILabelTemplateService _templateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LabelPrintService> _logger;

    public event EventHandler<LabelPrintJobDto>? JobStarted;
    public event EventHandler<LabelPrintJobDto>? JobCompleted;
    public event EventHandler<LabelPrintJobDto>? JobFailed;
    public event EventHandler<LabelPrintJobDto>? JobProgressUpdated;
    public event EventHandler<LabelPrintJobItemDto>? LabelPrinted;

    public LabelPrintService(
        IRepository<LabelPrintJob> jobRepository,
        IRepository<LabelPrintJobItem> itemRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        ILabelPrinterService printerService,
        ILabelTemplateService templateService,
        IUnitOfWork unitOfWork,
        ILogger<LabelPrintService> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Single Label Printing

    public async Task<bool> PrintSingleLabelAsync(PrintSingleLabelRequestDto request, int storeId, int userId)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            throw new KeyNotFoundException($"Product {request.ProductId} not found");

        var data = await GetProductLabelDataAsync(request.ProductId);
        if (data == null)
            throw new InvalidOperationException("Failed to get product label data");

        // Resolve printer and template
        var printerId = request.PrinterId ?? (await _printerService.GetPrinterForCategoryAsync(product.CategoryId, storeId))?.Id;
        if (!printerId.HasValue)
        {
            var defaultPrinter = await _printerService.GetDefaultPrinterAsync(storeId);
            printerId = defaultPrinter?.Id;
        }

        if (!printerId.HasValue)
            throw new InvalidOperationException("No printer available");

        var templateId = request.TemplateId ?? (await _templateService.GetTemplateForCategoryAsync(product.CategoryId, storeId))?.Id;
        if (!templateId.HasValue)
        {
            var defaultTemplate = await _templateService.GetDefaultTemplateAsync(storeId);
            templateId = defaultTemplate?.Id;
        }

        if (!templateId.HasValue)
            throw new InvalidOperationException("No template available");

        return await PrintCustomLabelAsync(templateId.Value, printerId.Value, data, request.Copies);
    }

    public async Task<bool> PrintCustomLabelAsync(int templateId, int printerId, ProductLabelDataDto data, int copies = 1)
    {
        try
        {
            var labelContent = await _templateService.GenerateLabelContentAsync(templateId, data);

            for (int i = 0; i < copies; i++)
            {
                await _printerService.SendToPrinterAsync(printerId, labelContent);
            }

            _logger.LogInformation("Printed {Copies} label(s) for product {ProductId}", copies, data.ProductId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print label for product {ProductId}", data.ProductId);
            throw;
        }
    }

    #endregion

    #region Batch Printing

    public async Task<LabelPrintJobDto> PrintBatchLabelsAsync(LabelBatchRequestDto request, int storeId, int userId)
    {
        List<int> productIds;

        if (request.ProductIds != null && request.ProductIds.Count > 0)
        {
            productIds = request.ProductIds;
        }
        else if (request.CategoryId.HasValue)
        {
            var products = await _productRepository.FindAsync(p =>
                p.CategoryId == request.CategoryId.Value && p.IsActive);
            productIds = products.Select(p => p.Id).ToList();
        }
        else
        {
            throw new ArgumentException("Either ProductIds or CategoryId must be specified");
        }

        if (productIds.Count == 0)
            throw new InvalidOperationException("No products found for batch printing");

        // Resolve printer
        var printerId = request.PrinterId;
        if (!printerId.HasValue)
        {
            var defaultPrinter = await _printerService.GetDefaultPrinterAsync(storeId);
            printerId = defaultPrinter?.Id;
        }
        if (!printerId.HasValue)
            throw new InvalidOperationException("No printer available");

        // Resolve template
        var templateId = request.TemplateId;
        if (!templateId.HasValue)
        {
            var defaultTemplate = await _templateService.GetDefaultTemplateAsync(storeId);
            templateId = defaultTemplate?.Id;
        }
        if (!templateId.HasValue)
            throw new InvalidOperationException("No template available");

        // Create job
        var job = new LabelPrintJob
        {
            JobType = LabelPrintJobType.Batch,
            TotalLabels = productIds.Count * request.CopiesPerLabel,
            PrintedLabels = 0,
            FailedLabels = 0,
            SkippedLabels = 0,
            Status = LabelPrintJobStatus.Pending,
            PrinterId = printerId.Value,
            TemplateId = templateId.Value,
            CategoryId = request.CategoryId,
            StoreId = storeId,
            InitiatedByUserId = userId,
            StartedAt = DateTime.UtcNow,
            Notes = request.Notes,
            CopiesPerLabel = request.CopiesPerLabel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Create job items
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) continue;

            var item = new LabelPrintJobItem
            {
                LabelPrintJobId = job.Id,
                ProductId = productId,
                ProductName = product.Name,
                Barcode = product.Barcode,
                Price = product.Price,
                Status = LabelPrintItemStatus.Pending,
                CopiesPrinted = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        // Start processing in background
        _ = ProcessJobAsync(job.Id);

        var result = await GetPrintJobAsync(job.Id);
        JobStarted?.Invoke(this, result!);

        return result!;
    }

    public async Task<LabelPrintJobDto> PrintPriceChangeLabelsAsync(PrintPriceChangeLabelsRequestDto request, int storeId, int userId)
    {
        var changedProducts = await GetPriceChangedProductsAsync(request.Since, storeId, request.CategoryId);

        if (changedProducts.Count == 0)
            throw new InvalidOperationException("No products with price changes found");

        var batchRequest = new LabelBatchRequestDto
        {
            ProductIds = changedProducts.Select(p => p.ProductId).ToList(),
            TemplateId = request.TemplateId,
            PrinterId = request.PrinterId,
            CopiesPerLabel = request.CopiesPerLabel,
            Notes = $"Price change labels since {request.Since:yyyy-MM-dd}"
        };

        return await PrintBatchLabelsWithTypeAsync(batchRequest, LabelPrintJobType.PriceChange, storeId, userId);
    }

    public async Task<LabelPrintJobDto> PrintCategoryLabelsAsync(PrintCategoryLabelsRequestDto request, int storeId, int userId)
    {
        var products = await GetCategoryProductsAsync(request.CategoryId, request.IncludeSubcategories);

        if (products.Count == 0)
            throw new InvalidOperationException("No products found in category");

        var batchRequest = new LabelBatchRequestDto
        {
            ProductIds = products.Select(p => p.ProductId).ToList(),
            CategoryId = request.CategoryId,
            TemplateId = request.TemplateId,
            PrinterId = request.PrinterId,
            CopiesPerLabel = request.CopiesPerLabel
        };

        return await PrintBatchLabelsWithTypeAsync(batchRequest, LabelPrintJobType.Category, storeId, userId);
    }

    public async Task<LabelPrintJobDto> PrintNewProductLabelsAsync(DateTime since, int storeId, int userId, int? templateId = null, int? printerId = null)
    {
        var newProducts = await _productRepository.FindAsync(p =>
            p.IsActive && p.CreatedAt >= since);

        if (!newProducts.Any())
            throw new InvalidOperationException("No new products found");

        var batchRequest = new LabelBatchRequestDto
        {
            ProductIds = newProducts.Select(p => p.Id).ToList(),
            TemplateId = templateId,
            PrinterId = printerId,
            CopiesPerLabel = 1,
            Notes = $"New product labels since {since:yyyy-MM-dd}"
        };

        return await PrintBatchLabelsWithTypeAsync(batchRequest, LabelPrintJobType.NewProducts, storeId, userId);
    }

    #endregion

    #region Job Management

    public async Task<LabelPrintJobDto?> GetPrintJobAsync(int jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;

        var items = await _itemRepository.FindAsync(i => i.LabelPrintJobId == jobId);
        var printer = await _printerService.GetPrinterAsync(job.PrinterId);
        var template = job.TemplateId.HasValue ? await _templateService.GetTemplateAsync(job.TemplateId.Value) : null;

        return MapToDto(job, items.ToList(), printer, template);
    }

    public async Task<LabelPrintJobDto?> GetPrintJobStatusAsync(int jobId)
    {
        return await GetPrintJobAsync(jobId);
    }

    public async Task<List<LabelPrintJobDto>> GetPrintJobHistoryAsync(GetPrintJobHistoryRequestDto request, int storeId)
    {
        var jobs = await _jobRepository.FindAsync(j => j.StoreId == storeId && j.IsActive);

        if (request.From.HasValue)
            jobs = jobs.Where(j => j.StartedAt >= request.From.Value);
        if (request.To.HasValue)
            jobs = jobs.Where(j => j.StartedAt <= request.To.Value);
        if (request.JobType.HasValue)
            jobs = jobs.Where(j => j.JobType == (LabelPrintJobType)request.JobType.Value);
        if (request.Status.HasValue)
            jobs = jobs.Where(j => j.Status == (LabelPrintJobStatus)request.Status.Value);
        if (request.PrinterId.HasValue)
            jobs = jobs.Where(j => j.PrinterId == request.PrinterId.Value);
        if (request.InitiatedByUserId.HasValue)
            jobs = jobs.Where(j => j.InitiatedByUserId == request.InitiatedByUserId.Value);

        var result = new List<LabelPrintJobDto>();
        foreach (var job in jobs.OrderByDescending(j => j.StartedAt))
        {
            var items = await _itemRepository.FindAsync(i => i.LabelPrintJobId == job.Id);
            var printer = await _printerService.GetPrinterAsync(job.PrinterId);
            var template = job.TemplateId.HasValue ? await _templateService.GetTemplateAsync(job.TemplateId.Value) : null;
            result.Add(MapToDto(job, items.ToList(), printer, template));
        }

        return result;
    }

    public async Task<List<LabelPrintJobDto>> GetActiveJobsAsync(int storeId)
    {
        var jobs = await _jobRepository.FindAsync(j =>
            j.StoreId == storeId &&
            (j.Status == LabelPrintJobStatus.Pending || j.Status == LabelPrintJobStatus.InProgress) &&
            j.IsActive);

        var result = new List<LabelPrintJobDto>();
        foreach (var job in jobs.OrderBy(j => j.StartedAt))
        {
            var items = await _itemRepository.FindAsync(i => i.LabelPrintJobId == job.Id);
            var printer = await _printerService.GetPrinterAsync(job.PrinterId);
            var template = job.TemplateId.HasValue ? await _templateService.GetTemplateAsync(job.TemplateId.Value) : null;
            result.Add(MapToDto(job, items.ToList(), printer, template));
        }

        return result;
    }

    public async Task<bool> CancelJobAsync(int jobId, int userId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return false;

        if (job.Status == LabelPrintJobStatus.Completed || job.Status == LabelPrintJobStatus.Failed)
            throw new InvalidOperationException("Cannot cancel a completed or failed job");

        job.Status = LabelPrintJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job);

        // Mark pending items as skipped
        var pendingItems = await _itemRepository.FindAsync(i =>
            i.LabelPrintJobId == jobId && i.Status == LabelPrintItemStatus.Pending);

        foreach (var item in pendingItems)
        {
            item.Status = LabelPrintItemStatus.Skipped;
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item);
            job.SkippedLabels++;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cancelled print job {JobId} by user {UserId}", jobId, userId);
        return true;
    }

    public async Task<LabelPrintJobDto> RetryFailedItemsAsync(int jobId, int userId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null)
            throw new KeyNotFoundException($"Job {jobId} not found");

        var failedItems = await _itemRepository.FindAsync(i =>
            i.LabelPrintJobId == jobId && i.Status == LabelPrintItemStatus.Failed);

        if (!failedItems.Any())
            throw new InvalidOperationException("No failed items to retry");

        // Reset failed items
        foreach (var item in failedItems)
        {
            item.Status = LabelPrintItemStatus.Pending;
            item.ErrorMessage = null;
            item.UpdatedAt = DateTime.UtcNow;
            await _itemRepository.UpdateAsync(item);
        }

        job.FailedLabels = 0;
        job.Status = LabelPrintJobStatus.InProgress;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Re-process
        _ = ProcessJobAsync(jobId);

        return (await GetPrintJobAsync(jobId))!;
    }

    public async Task<List<LabelPrintJobItemDto>> GetFailedItemsAsync(int jobId)
    {
        var items = await _itemRepository.FindAsync(i =>
            i.LabelPrintJobId == jobId && i.Status == LabelPrintItemStatus.Failed);

        return items.Select(MapToItemDto).ToList();
    }

    #endregion

    #region Product Label Data

    public async Task<ProductLabelDataDto?> GetProductLabelDataAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;

        var category = await _categoryRepository.GetByIdAsync(product.CategoryId);

        return new ProductLabelDataDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Barcode = product.Barcode,
            Price = product.Price,
            UnitPrice = product.UnitOfMeasure != null ? $"KSh {product.Price:N2}/{product.UnitOfMeasure}" : null,
            Description = product.Description,
            SKU = product.SKU,
            CategoryName = category?.Name,
            UnitOfMeasure = product.UnitOfMeasure,
            EffectiveDate = DateTime.Today
        };
    }

    public async Task<List<ProductLabelDataDto>> GetProductsLabelDataAsync(List<int> productIds)
    {
        var result = new List<ProductLabelDataDto>();
        foreach (var productId in productIds)
        {
            var data = await GetProductLabelDataAsync(productId);
            if (data != null)
            {
                result.Add(data);
            }
        }
        return result;
    }

    public async Task<List<ProductLabelDataDto>> GetPriceChangedProductsAsync(DateTime since, int storeId, int? categoryId = null)
    {
        var products = await _productRepository.FindAsync(p =>
            p.IsActive && p.UpdatedAt >= since);

        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        var result = new List<ProductLabelDataDto>();
        foreach (var product in products)
        {
            var data = await GetProductLabelDataAsync(product.Id);
            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }

    public async Task<List<ProductLabelDataDto>> GetCategoryProductsAsync(int categoryId, bool includeSubcategories = false)
    {
        List<int> categoryIds = new() { categoryId };

        if (includeSubcategories)
        {
            var subcategories = await _categoryRepository.FindAsync(c => c.ParentCategoryId == categoryId && c.IsActive);
            categoryIds.AddRange(subcategories.Select(c => c.Id));
        }

        var products = await _productRepository.FindAsync(p =>
            categoryIds.Contains(p.CategoryId) && p.IsActive);

        var result = new List<ProductLabelDataDto>();
        foreach (var product in products)
        {
            var data = await GetProductLabelDataAsync(product.Id);
            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }

    #endregion

    #region Statistics

    public async Task<LabelPrintingStatisticsDto> GetStatisticsAsync(int storeId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todaysJobs = await _jobRepository.FindAsync(j =>
            j.StoreId == storeId && j.StartedAt >= today && j.StartedAt < tomorrow);

        var pendingJobs = await _jobRepository.FindAsync(j =>
            j.StoreId == storeId &&
            (j.Status == LabelPrintJobStatus.Pending || j.Status == LabelPrintJobStatus.InProgress));

        var completedJobs = todaysJobs.Where(j => j.CompletedAt.HasValue).ToList();

        // Get printer usage stats
        var printerUsage = await _printerService.GetPrinterUsageAsync(storeId, today, tomorrow);
        var printers = await _printerService.GetAllPrintersAsync(storeId);
        var labelsByPrinter = printerUsage.ToDictionary(
            kvp => printers.FirstOrDefault(p => p.Id == kvp.Key)?.Name ?? $"Printer {kvp.Key}",
            kvp => kvp.Value);

        // Get category stats from job items
        var labelsByCategory = new Dictionary<string, int>();
        foreach (var job in todaysJobs)
        {
            if (!string.IsNullOrEmpty(job.Category?.Name))
            {
                if (!labelsByCategory.ContainsKey(job.Category.Name))
                    labelsByCategory[job.Category.Name] = 0;
                labelsByCategory[job.Category.Name] += job.PrintedLabels;
            }
        }

        return new LabelPrintingStatisticsDto
        {
            TotalLabelsToday = todaysJobs.Sum(j => j.PrintedLabels),
            TotalJobsToday = todaysJobs.Count(),
            FailedLabelsToday = todaysJobs.Sum(j => j.FailedLabels),
            PendingJobs = pendingJobs.Count(),
            LabelsByPrinter = labelsByPrinter,
            LabelsByCategory = labelsByCategory,
            AverageJobDurationSeconds = completedJobs.Any()
                ? completedJobs.Average(j => (j.CompletedAt!.Value - j.StartedAt).TotalSeconds)
                : 0,
            LastPrintTime = todaysJobs.OrderByDescending(j => j.StartedAt).FirstOrDefault()?.StartedAt
        };
    }

    public async Task<Dictionary<DateTime, int>> GetDailyLabelCountsAsync(int storeId, DateTime from, DateTime to)
    {
        var jobs = await _jobRepository.FindAsync(j =>
            j.StoreId == storeId && j.StartedAt >= from && j.StartedAt <= to);

        return jobs
            .GroupBy(j => j.StartedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(j => j.PrintedLabels));
    }

    #endregion

    #region Private Methods

    private async Task<LabelPrintJobDto> PrintBatchLabelsWithTypeAsync(
        LabelBatchRequestDto request,
        LabelPrintJobType jobType,
        int storeId,
        int userId)
    {
        // Similar to PrintBatchLabelsAsync but with custom job type
        var productIds = request.ProductIds ?? new List<int>();

        if (productIds.Count == 0)
            throw new InvalidOperationException("No products found for batch printing");

        // Resolve printer
        var printerId = request.PrinterId;
        if (!printerId.HasValue)
        {
            var defaultPrinter = await _printerService.GetDefaultPrinterAsync(storeId);
            printerId = defaultPrinter?.Id;
        }
        if (!printerId.HasValue)
            throw new InvalidOperationException("No printer available");

        // Resolve template
        var templateId = request.TemplateId;
        if (!templateId.HasValue)
        {
            var defaultTemplate = await _templateService.GetDefaultTemplateAsync(storeId);
            templateId = defaultTemplate?.Id;
        }
        if (!templateId.HasValue)
            throw new InvalidOperationException("No template available");

        // Create job
        var job = new LabelPrintJob
        {
            JobType = jobType,
            TotalLabels = productIds.Count * request.CopiesPerLabel,
            PrintedLabels = 0,
            FailedLabels = 0,
            SkippedLabels = 0,
            Status = LabelPrintJobStatus.Pending,
            PrinterId = printerId.Value,
            TemplateId = templateId.Value,
            CategoryId = request.CategoryId,
            StoreId = storeId,
            InitiatedByUserId = userId,
            StartedAt = DateTime.UtcNow,
            Notes = request.Notes,
            CopiesPerLabel = request.CopiesPerLabel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Create job items
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) continue;

            var item = new LabelPrintJobItem
            {
                LabelPrintJobId = job.Id,
                ProductId = productId,
                ProductName = product.Name,
                Barcode = product.Barcode,
                Price = product.Price,
                Status = LabelPrintItemStatus.Pending,
                CopiesPrinted = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        // Start processing in background
        _ = ProcessJobAsync(job.Id);

        var result = await GetPrintJobAsync(job.Id);
        JobStarted?.Invoke(this, result!);

        return result!;
    }

    private async Task ProcessJobAsync(int jobId)
    {
        try
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null) return;

            job.Status = LabelPrintJobStatus.InProgress;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync();

            var pendingItems = await _itemRepository.FindAsync(i =>
                i.LabelPrintJobId == jobId && i.Status == LabelPrintItemStatus.Pending);

            foreach (var item in pendingItems)
            {
                // Check if job was cancelled
                var currentJob = await _jobRepository.GetByIdAsync(jobId);
                if (currentJob?.Status == LabelPrintJobStatus.Cancelled)
                    break;

                try
                {
                    var data = await GetProductLabelDataAsync(item.ProductId);
                    if (data == null)
                    {
                        item.Status = LabelPrintItemStatus.Skipped;
                        item.ErrorMessage = "Product not found";
                        job.SkippedLabels++;
                    }
                    else if (job.TemplateId.HasValue)
                    {
                        var labelContent = await _templateService.GenerateLabelContentAsync(job.TemplateId.Value, data);

                        for (int i = 0; i < job.CopiesPerLabel; i++)
                        {
                            await _printerService.SendToPrinterAsync(job.PrinterId, labelContent);
                            item.CopiesPrinted++;
                        }

                        item.Status = LabelPrintItemStatus.Printed;
                        item.PrintedAt = DateTime.UtcNow;
                        job.PrintedLabels += job.CopiesPerLabel;

                        LabelPrinted?.Invoke(this, MapToItemDto(item));
                    }
                }
                catch (Exception ex)
                {
                    item.Status = LabelPrintItemStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    job.FailedLabels++;
                    _logger.LogError(ex, "Failed to print label for item {ItemId}", item.Id);
                }

                item.UpdatedAt = DateTime.UtcNow;
                await _itemRepository.UpdateAsync(item);
                await _unitOfWork.SaveChangesAsync();

                // Update progress
                var jobDto = await GetPrintJobAsync(jobId);
                if (jobDto != null)
                {
                    JobProgressUpdated?.Invoke(this, jobDto);
                }
            }

            // Finalize job
            job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null && job.Status != LabelPrintJobStatus.Cancelled)
            {
                if (job.FailedLabels > 0 && job.PrintedLabels == 0)
                {
                    job.Status = LabelPrintJobStatus.Failed;
                    job.ErrorMessage = "All labels failed to print";
                }
                else if (job.FailedLabels > 0)
                {
                    job.Status = LabelPrintJobStatus.PartiallyCompleted;
                }
                else
                {
                    job.Status = LabelPrintJobStatus.Completed;
                }

                job.CompletedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                var finalDto = await GetPrintJobAsync(jobId);
                if (job.Status == LabelPrintJobStatus.Failed)
                {
                    JobFailed?.Invoke(this, finalDto!);
                }
                else
                {
                    JobCompleted?.Invoke(this, finalDto!);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing print job {JobId}", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null)
            {
                job.Status = LabelPrintJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                var dto = await GetPrintJobAsync(jobId);
                JobFailed?.Invoke(this, dto!);
            }
        }
    }

    private LabelPrintJobDto MapToDto(LabelPrintJob job, List<LabelPrintJobItem> items, LabelPrinterDto? printer, LabelTemplateDto? template)
    {
        var duration = job.CompletedAt.HasValue
            ? job.CompletedAt.Value - job.StartedAt
            : (TimeSpan?)null;

        return new LabelPrintJobDto
        {
            Id = job.Id,
            JobType = (LabelPrintJobTypeDto)job.JobType,
            TotalLabels = job.TotalLabels,
            PrintedLabels = job.PrintedLabels,
            FailedLabels = job.FailedLabels,
            SkippedLabels = job.SkippedLabels,
            Status = (LabelPrintJobStatusDto)job.Status,
            PrinterId = job.PrinterId,
            PrinterName = printer?.Name,
            TemplateId = job.TemplateId,
            TemplateName = template?.Name,
            CategoryId = job.CategoryId,
            CategoryName = job.Category?.Name,
            InitiatedByUserId = job.InitiatedByUserId,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage,
            Notes = job.Notes,
            CopiesPerLabel = job.CopiesPerLabel,
            ProgressPercent = job.TotalLabels > 0
                ? Math.Round((double)(job.PrintedLabels + job.FailedLabels + job.SkippedLabels) / job.TotalLabels * 100, 1)
                : 0,
            Duration = duration,
            Items = items.Select(MapToItemDto).ToList()
        };
    }

    private LabelPrintJobItemDto MapToItemDto(LabelPrintJobItem item)
    {
        return new LabelPrintJobItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Barcode = item.Barcode,
            Price = item.Price,
            OriginalPrice = item.OriginalPrice,
            Status = (LabelPrintItemStatusDto)item.Status,
            ErrorMessage = item.ErrorMessage,
            PrintedAt = item.PrintedAt,
            CopiesPrinted = item.CopiesPrinted
        };
    }

    #endregion
}
