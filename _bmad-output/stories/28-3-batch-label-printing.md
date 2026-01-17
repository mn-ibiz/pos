# Story 28.3: Individual and Batch Label Printing

## Story
**As a** store clerk,
**I want to** print labels for single products or batches,
**So that** shelves are properly labeled.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 28: Shelf Label Printing**

## Acceptance Criteria

### AC1: Single Label Printing
**Given** product selected
**When** printing single label
**Then** prints one label with current product data

### AC2: Price Change Batch Print
**Given** price changes occur
**When** running batch print
**Then** prints labels for all changed products

### AC3: Category Batch Print
**Given** category selected
**When** batch printing
**Then** prints labels for all products in category

## Technical Notes
```csharp
public interface ILabelPrintService
{
    Task<bool> PrintSingleLabelAsync(Guid productId, Guid? templateId = null, int copies = 1);
    Task<LabelPrintJob> PrintBatchLabelsAsync(LabelBatchRequest request);
    Task<LabelPrintJob> PrintPriceChangeLabelsAsync(DateTime since);
    Task<LabelPrintJob> PrintCategoryLabelsAsync(Guid categoryId);
    Task<List<LabelPrintJob>> GetPrintJobHistoryAsync(DateTime from, DateTime to);
    Task<LabelPrintJob> GetPrintJobStatusAsync(Guid jobId);
}

public class LabelBatchRequest
{
    public List<Guid> ProductIds { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TemplateId { get; set; }
    public int CopiesPerLabel { get; set; } = 1;
    public bool IncludeInactive { get; set; } = false;
}

public class LabelPrintJob
{
    public Guid Id { get; set; }
    public LabelPrintJobType JobType { get; set; }
    public int TotalLabels { get; set; }
    public int PrintedLabels { get; set; }
    public int FailedLabels { get; set; }
    public LabelPrintJobStatus Status { get; set; }
    public Guid PrinterId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; }
    public List<LabelPrintJobItem> Items { get; set; }
}

public class LabelPrintJobItem
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string Barcode { get; set; }
    public decimal Price { get; set; }
    public LabelPrintItemStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}

public enum LabelPrintJobType
{
    Single,
    Batch,
    PriceChange,
    Category
}

public enum LabelPrintJobStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

public enum LabelPrintItemStatus
{
    Pending,
    Printed,
    Failed,
    Skipped
}

public class LabelPrintService : ILabelPrintService
{
    private readonly AppDbContext _context;
    private readonly ILabelPrinterService _printerService;
    private readonly ILabelTemplateService _templateService;
    private readonly ILogger<LabelPrintService> _logger;

    public async Task<bool> PrintSingleLabelAsync(Guid productId, Guid? templateId = null, int copies = 1)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return false;

        var printer = await GetPrinterForCategoryAsync(product.CategoryId);
        var template = await GetTemplateAsync(templateId, product.CategoryId);

        var labelData = MapProductToLabelData(product);
        var labelContent = _labelGenerator.GenerateLabel(template, labelData);

        for (int i = 0; i < copies; i++)
        {
            await _printerService.SendToPrinterAsync(printer, labelContent);
        }

        return true;
    }

    public async Task<LabelPrintJob> PrintPriceChangeLabelsAsync(DateTime since)
    {
        // Find products with price changes since specified date
        var changedProducts = await _context.Products
            .Where(p => p.IsActive && p.LastPriceChangeAt >= since)
            .ToListAsync();

        if (!changedProducts.Any())
        {
            return new LabelPrintJob
            {
                Status = LabelPrintJobStatus.Completed,
                TotalLabels = 0
            };
        }

        return await PrintBatchLabelsAsync(new LabelBatchRequest
        {
            ProductIds = changedProducts.Select(p => p.Id).ToList()
        });
    }

    public async Task<LabelPrintJob> PrintCategoryLabelsAsync(Guid categoryId)
    {
        var products = await _context.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        return await PrintBatchLabelsAsync(new LabelBatchRequest
        {
            CategoryId = categoryId,
            ProductIds = products
        });
    }

    public async Task<LabelPrintJob> PrintBatchLabelsAsync(LabelBatchRequest request)
    {
        var job = new LabelPrintJob
        {
            Id = Guid.NewGuid(),
            JobType = request.CategoryId.HasValue
                ? LabelPrintJobType.Category
                : LabelPrintJobType.Batch,
            TotalLabels = request.ProductIds.Count * request.CopiesPerLabel,
            Status = LabelPrintJobStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            Items = new List<LabelPrintJobItem>()
        };

        await _context.LabelPrintJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Process labels in background
        _ = ProcessBatchPrintAsync(job.Id, request);

        return job;
    }

    private async Task ProcessBatchPrintAsync(Guid jobId, LabelBatchRequest request)
    {
        var job = await _context.LabelPrintJobs.FindAsync(jobId);

        foreach (var productId in request.ProductIds)
        {
            try
            {
                await PrintSingleLabelAsync(productId, request.TemplateId, request.CopiesPerLabel);
                job.PrintedLabels += request.CopiesPerLabel;

                job.Items.Add(new LabelPrintJobItem
                {
                    ProductId = productId,
                    Status = LabelPrintItemStatus.Printed
                });
            }
            catch (Exception ex)
            {
                job.FailedLabels += request.CopiesPerLabel;
                job.Items.Add(new LabelPrintJobItem
                {
                    ProductId = productId,
                    Status = LabelPrintItemStatus.Failed,
                    ErrorMessage = ex.Message
                });
                _logger.LogError(ex, "Failed to print label for product {ProductId}", productId);
            }

            await _context.SaveChangesAsync();
        }

        job.Status = job.FailedLabels == 0
            ? LabelPrintJobStatus.Completed
            : job.PrintedLabels > 0
                ? LabelPrintJobStatus.PartiallyCompleted
                : LabelPrintJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

// Product entity extension for price tracking
public partial class Product
{
    public DateTime? LastPriceChangeAt { get; set; }
}
```

## Definition of Done
- [x] Single label printing from product view
- [x] Batch label selection UI
- [x] Price change detection and batch print
- [x] Category batch printing
- [x] Print job tracking/history
- [x] Progress indicator for batch jobs
- [x] Error handling and retry
- [x] Print job audit log
- [x] Unit tests passing

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Core/Entities/LabelPrintingEntities.cs` - LabelPrintJob, LabelPrintJobItem entities with job types and statuses
- `src/HospitalityPOS.Core/DTOs/LabelPrintingDtos.cs` - Job DTOs, request DTOs, statistics DTOs
- `src/HospitalityPOS.Core/Interfaces/ILabelPrintService.cs` - Service interface with print events
- `src/HospitalityPOS.Infrastructure/Services/LabelPrintService.cs` - Full implementation with:
  - Single label printing with automatic printer/template resolution
  - Batch printing with customizable copies per label
  - Price change label printing (since date)
  - Category label printing with subcategory support
  - New product label printing
  - Job management (status, cancel, retry failed)
  - Product label data retrieval
  - Printing statistics and daily counts
  - Background job processing with progress events
- `tests/HospitalityPOS.Business.Tests/Services/LabelPrintServiceTests.cs` - Comprehensive unit tests
