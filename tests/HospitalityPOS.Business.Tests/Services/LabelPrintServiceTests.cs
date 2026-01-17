using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class LabelPrintServiceTests
{
    private readonly Mock<IRepository<LabelPrintJob>> _jobRepoMock;
    private readonly Mock<IRepository<LabelPrintJobItem>> _itemRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<ILabelPrinterService> _printerServiceMock;
    private readonly Mock<ILabelTemplateService> _templateServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LabelPrintService>> _loggerMock;
    private readonly LabelPrintService _service;

    public LabelPrintServiceTests()
    {
        _jobRepoMock = new Mock<IRepository<LabelPrintJob>>();
        _itemRepoMock = new Mock<IRepository<LabelPrintJobItem>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _printerServiceMock = new Mock<ILabelPrinterService>();
        _templateServiceMock = new Mock<ILabelTemplateService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LabelPrintService>>();

        _service = new LabelPrintService(
            _jobRepoMock.Object,
            _itemRepoMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _printerServiceMock.Object,
            _templateServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Single Label Printing Tests

    [Fact]
    public async Task PrintSingleLabelAsync_ValidRequest_PrintsLabel()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", CategoryId = 10, Price = 99.99m, Barcode = "123456", IsActive = true };
        var printer = new LabelPrinterDto { Id = 1, Name = "Printer 1" };
        var template = new LabelTemplateDto { Id = 1, Name = "Template 1" };

        var request = new PrintSingleLabelRequestDto { ProductId = 1, Copies = 2 };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Category { Id = 10, Name = "Category" });
        _printerServiceMock.Setup(s => s.GetPrinterForCategoryAsync(10, 1)).ReturnsAsync(printer);
        _templateServiceMock.Setup(s => s.GetTemplateForCategoryAsync(10, 1)).ReturnsAsync(template);
        _templateServiceMock.Setup(s => s.GenerateLabelContentAsync(1, It.IsAny<ProductLabelDataDto>()))
            .ReturnsAsync("^XA^FDTest^FS^XZ");
        _printerServiceMock.Setup(s => s.SendToPrinterAsync(1, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _service.PrintSingleLabelAsync(request, 1, 1);

        // Assert
        result.Should().BeTrue();
        _printerServiceMock.Verify(s => s.SendToPrinterAsync(1, It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PrintSingleLabelAsync_ProductNotFound_ThrowsException()
    {
        // Arrange
        var request = new PrintSingleLabelRequestDto { ProductId = 999 };
        _productRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act & Assert
        await _service.Invoking(s => s.PrintSingleLabelAsync(request, 1, 1))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PrintSingleLabelAsync_NoPrinterAvailable_ThrowsException()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test", CategoryId = 10, IsActive = true };
        var request = new PrintSingleLabelRequestDto { ProductId = 1 };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Category { Id = 10, Name = "Cat" });
        _printerServiceMock.Setup(s => s.GetPrinterForCategoryAsync(10, 1)).ReturnsAsync((LabelPrinterDto?)null);
        _printerServiceMock.Setup(s => s.GetDefaultPrinterAsync(1)).ReturnsAsync((LabelPrinterDto?)null);

        // Act & Assert
        await _service.Invoking(s => s.PrintSingleLabelAsync(request, 1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No printer available*");
    }

    [Fact]
    public async Task PrintCustomLabelAsync_ValidRequest_PrintsLabel()
    {
        // Arrange
        var data = new ProductLabelDataDto { ProductId = 1, ProductName = "Test", Price = 50m };

        _templateServiceMock.Setup(s => s.GenerateLabelContentAsync(1, data))
            .ReturnsAsync("^XA^FDTest^FS^XZ");
        _printerServiceMock.Setup(s => s.SendToPrinterAsync(1, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _service.PrintCustomLabelAsync(1, 1, data, 3);

        // Assert
        result.Should().BeTrue();
        _printerServiceMock.Verify(s => s.SendToPrinterAsync(1, It.IsAny<string>()), Times.Exactly(3));
    }

    #endregion

    #region Batch Printing Tests

    [Fact]
    public async Task PrintBatchLabelsAsync_ValidRequest_CreatesJobWithItems()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Price = 10m, IsActive = true },
            new() { Id = 2, Name = "Product 2", Price = 20m, IsActive = true }
        };
        var request = new LabelBatchRequestDto
        {
            ProductIds = new List<int> { 1, 2 },
            CopiesPerLabel = 1
        };

        _printerServiceMock.Setup(s => s.GetDefaultPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });
        _templateServiceMock.Setup(s => s.GetDefaultTemplateAsync(1, null))
            .ReturnsAsync(new LabelTemplateDto { Id = 1, Name = "Template" });
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(products[0]);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(products[1]);
        _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelPrintJob>()))
            .Callback<LabelPrintJob>(j => j.Id = 1)
            .Returns(Task.CompletedTask);
        _jobRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.Pending, PrinterId = 1, TotalLabels = 2 });
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());

        // Act
        var result = await _service.PrintBatchLabelsAsync(request, 1, 1);

        // Assert
        result.Should().NotBeNull();
        result.TotalLabels.Should().Be(2);
        _itemRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelPrintJobItem>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PrintBatchLabelsAsync_NoProducts_ThrowsException()
    {
        // Arrange
        var request = new LabelBatchRequestDto { ProductIds = new List<int>() };

        // Act & Assert
        await _service.Invoking(s => s.PrintBatchLabelsAsync(request, 1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No products found*");
    }

    [Fact]
    public async Task PrintPriceChangeLabelsAsync_ProductsWithChanges_CreatesJob()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Changed Product", Price = 100m, UpdatedAt = DateTime.UtcNow, IsActive = true }
        };
        var request = new PrintPriceChangeLabelsRequestDto
        {
            Since = DateTime.UtcNow.AddDays(-1),
            CopiesPerLabel = 1
        };

        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(products);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(products[0]);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Category { Id = 10, Name = "Cat" });
        _printerServiceMock.Setup(s => s.GetDefaultPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });
        _templateServiceMock.Setup(s => s.GetDefaultTemplateAsync(1, null))
            .ReturnsAsync(new LabelTemplateDto { Id = 1, Name = "Template" });
        _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelPrintJob>()))
            .Callback<LabelPrintJob>(j => j.Id = 1)
            .Returns(Task.CompletedTask);
        _jobRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelPrintJob { Id = 1, JobType = LabelPrintJobType.PriceChange, Status = LabelPrintJobStatus.Pending, PrinterId = 1 });
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());

        // Act
        var result = await _service.PrintPriceChangeLabelsAsync(request, 1, 1);

        // Assert
        result.Should().NotBeNull();
        result.JobType.Should().Be(LabelPrintJobTypeDto.PriceChange);
    }

    [Fact]
    public async Task PrintCategoryLabelsAsync_ValidCategory_CreatesJob()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Cat Product 1", CategoryId = 10, Price = 50m, IsActive = true }
        };
        var request = new PrintCategoryLabelsRequestDto
        {
            CategoryId = 10,
            CopiesPerLabel = 1
        };

        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(products);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(products[0]);
        _categoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(new List<Category>());
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Category { Id = 10, Name = "Category" });
        _printerServiceMock.Setup(s => s.GetDefaultPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });
        _templateServiceMock.Setup(s => s.GetDefaultTemplateAsync(1, null))
            .ReturnsAsync(new LabelTemplateDto { Id = 1, Name = "Template" });
        _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelPrintJob>()))
            .Callback<LabelPrintJob>(j => j.Id = 1)
            .Returns(Task.CompletedTask);
        _jobRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelPrintJob { Id = 1, JobType = LabelPrintJobType.Category, Status = LabelPrintJobStatus.Pending, PrinterId = 1 });
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());

        // Act
        var result = await _service.PrintCategoryLabelsAsync(request, 1, 1);

        // Assert
        result.Should().NotBeNull();
        result.JobType.Should().Be(LabelPrintJobTypeDto.Category);
    }

    #endregion

    #region Job Management Tests

    [Fact]
    public async Task GetPrintJobAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var job = new LabelPrintJob
        {
            Id = 1,
            JobType = LabelPrintJobType.Batch,
            Status = LabelPrintJobStatus.InProgress,
            PrinterId = 1,
            TotalLabels = 10,
            PrintedLabels = 5,
            StartedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());
        _printerServiceMock.Setup(s => s.GetPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });

        // Act
        var result = await _service.GetPrintJobAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(LabelPrintJobStatusDto.InProgress);
        result.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public async Task GetPrintJobHistoryAsync_WithFilters_ReturnsFilteredJobs()
    {
        // Arrange
        var jobs = new List<LabelPrintJob>
        {
            new() { Id = 1, JobType = LabelPrintJobType.Batch, Status = LabelPrintJobStatus.Completed, StoreId = 1, PrinterId = 1, StartedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = 2, JobType = LabelPrintJobType.PriceChange, Status = LabelPrintJobStatus.Completed, StoreId = 1, PrinterId = 1, StartedAt = DateTime.UtcNow.AddHours(-1), IsActive = true }
        };
        var request = new GetPrintJobHistoryRequestDto { JobType = LabelPrintJobTypeDto.Batch };

        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(jobs);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());
        _printerServiceMock.Setup(s => s.GetPrinterAsync(It.IsAny<int>()))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });

        // Act
        var result = await _service.GetPrintJobHistoryAsync(request, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].JobType.Should().Be(LabelPrintJobTypeDto.Batch);
    }

    [Fact]
    public async Task GetActiveJobsAsync_ReturnsInProgressJobs()
    {
        // Arrange
        var jobs = new List<LabelPrintJob>
        {
            new() { Id = 1, Status = LabelPrintJobStatus.InProgress, StoreId = 1, PrinterId = 1, StartedAt = DateTime.UtcNow, IsActive = true }
        };

        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(jobs);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());
        _printerServiceMock.Setup(s => s.GetPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });

        // Act
        var result = await _service.GetActiveJobsAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CancelJobAsync_PendingJob_CancelsSuccessfully()
    {
        // Arrange
        var job = new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.Pending, PrinterId = 1 };
        var pendingItems = new List<LabelPrintJobItem>
        {
            new() { Id = 1, LabelPrintJobId = 1, Status = LabelPrintItemStatus.Pending }
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(pendingItems);

        // Act
        var result = await _service.CancelJobAsync(1, 1);

        // Assert
        result.Should().BeTrue();
        job.Status.Should().Be(LabelPrintJobStatus.Cancelled);
    }

    [Fact]
    public async Task CancelJobAsync_CompletedJob_ThrowsException()
    {
        // Arrange
        var job = new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.Completed };
        _jobRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);

        // Act & Assert
        await _service.Invoking(s => s.CancelJobAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public async Task RetryFailedItemsAsync_HasFailedItems_RetriesSuccessfully()
    {
        // Arrange
        var job = new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.PartiallyCompleted, FailedLabels = 2, PrinterId = 1 };
        var failedItems = new List<LabelPrintJobItem>
        {
            new() { Id = 1, LabelPrintJobId = 1, Status = LabelPrintItemStatus.Failed },
            new() { Id = 2, LabelPrintJobId = 1, Status = LabelPrintItemStatus.Failed }
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(failedItems);
        _printerServiceMock.Setup(s => s.GetPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });

        // Act
        var result = await _service.RetryFailedItemsAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        job.Status.Should().Be(LabelPrintJobStatus.InProgress);
        failedItems.Should().OnlyContain(i => i.Status == LabelPrintItemStatus.Pending);
    }

    [Fact]
    public async Task RetryFailedItemsAsync_NoFailedItems_ThrowsException()
    {
        // Arrange
        var job = new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.Completed };
        _jobRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());

        // Act & Assert
        await _service.Invoking(s => s.RetryFailedItemsAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No failed items*");
    }

    [Fact]
    public async Task GetFailedItemsAsync_ReturnsFailedItems()
    {
        // Arrange
        var failedItems = new List<LabelPrintJobItem>
        {
            new() { Id = 1, ProductId = 1, ProductName = "Failed Product", Status = LabelPrintItemStatus.Failed, ErrorMessage = "Connection error" }
        };

        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(failedItems);

        // Act
        var result = await _service.GetFailedItemsAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(LabelPrintItemStatusDto.Failed);
        result[0].ErrorMessage.Should().Be("Connection error");
    }

    #endregion

    #region Product Label Data Tests

    [Fact]
    public async Task GetProductLabelDataAsync_ValidProduct_ReturnsData()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Barcode = "123456",
            Price = 99.99m,
            CategoryId = 10,
            SKU = "SKU001",
            Description = "Test description",
            UnitOfMeasure = "kg",
            IsActive = true
        };
        var category = new Category { Id = 10, Name = "Test Category" };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(category);

        // Act
        var result = await _service.GetProductLabelDataAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Test Product");
        result.Barcode.Should().Be("123456");
        result.Price.Should().Be(99.99m);
        result.CategoryName.Should().Be("Test Category");
        result.UnitPrice.Should().Contain("kg");
    }

    [Fact]
    public async Task GetProductsLabelDataAsync_MultipleProducts_ReturnsAll()
    {
        // Arrange
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10m, CategoryId = 10, IsActive = true };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20m, CategoryId = 10, IsActive = true };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Category { Id = 10, Name = "Cat" });

        // Act
        var result = await _service.GetProductsLabelDataAsync(new List<int> { 1, 2 });

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategoryProductsAsync_WithSubcategories_ReturnsAllProducts()
    {
        // Arrange
        var subcategories = new List<Category>
        {
            new() { Id = 11, ParentCategoryId = 10, Name = "Subcategory", IsActive = true }
        };
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Parent Cat Product", CategoryId = 10, IsActive = true },
            new() { Id = 2, Name = "Sub Cat Product", CategoryId = 11, IsActive = true }
        };

        _categoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(subcategories);
        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(products);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(products[0]);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(products[1]);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Category { Id = 10, Name = "Category" });

        // Act
        var result = await _service.GetCategoryProductsAsync(10, includeSubcategories: true);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var todayStart = DateTime.UtcNow.Date;
        var jobs = new List<LabelPrintJob>
        {
            new() { Id = 1, PrintedLabels = 50, FailedLabels = 2, StoreId = 1, PrinterId = 1, StartedAt = todayStart.AddHours(1), CompletedAt = todayStart.AddHours(2), IsActive = true },
            new() { Id = 2, PrintedLabels = 30, FailedLabels = 0, StoreId = 1, PrinterId = 1, StartedAt = todayStart.AddHours(3), CompletedAt = todayStart.AddHours(4), IsActive = true }
        };
        var printers = new List<LabelPrinterDto>
        {
            new() { Id = 1, Name = "Printer 1" }
        };

        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(jobs);
        _printerServiceMock.Setup(s => s.GetPrinterUsageAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<int, int> { { 1, 80 } });
        _printerServiceMock.Setup(s => s.GetAllPrintersAsync(1)).ReturnsAsync(printers);

        // Act
        var result = await _service.GetStatisticsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalLabelsToday.Should().Be(80);
        result.TotalJobsToday.Should().Be(2);
        result.FailedLabelsToday.Should().Be(2);
    }

    [Fact]
    public async Task GetDailyLabelCountsAsync_ReturnsCountsByDate()
    {
        // Arrange
        var jobs = new List<LabelPrintJob>
        {
            new() { Id = 1, PrintedLabels = 50, StoreId = 1, StartedAt = DateTime.UtcNow.Date, IsActive = true },
            new() { Id = 2, PrintedLabels = 30, StoreId = 1, StartedAt = DateTime.UtcNow.Date.AddDays(-1), IsActive = true }
        };

        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _service.GetDailyLabelCountsAsync(1, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result[DateTime.UtcNow.Date].Should().Be(50);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task PrintBatchLabelsAsync_RaisesJobStartedEvent()
    {
        // Arrange
        var request = new LabelBatchRequestDto { ProductIds = new List<int> { 1 }, CopiesPerLabel = 1 };
        var product = new Product { Id = 1, Name = "Test", Price = 10m, IsActive = true };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _printerServiceMock.Setup(s => s.GetDefaultPrinterAsync(1))
            .ReturnsAsync(new LabelPrinterDto { Id = 1, Name = "Printer" });
        _templateServiceMock.Setup(s => s.GetDefaultTemplateAsync(1, null))
            .ReturnsAsync(new LabelTemplateDto { Id = 1, Name = "Template" });
        _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelPrintJob>()))
            .Callback<LabelPrintJob>(j => j.Id = 1)
            .Returns(Task.CompletedTask);
        _jobRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelPrintJob { Id = 1, Status = LabelPrintJobStatus.Pending, PrinterId = 1 });
        _itemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJobItem, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJobItem>());

        LabelPrintJobDto? eventJob = null;
        _service.JobStarted += (s, j) => eventJob = j;

        // Act
        await _service.PrintBatchLabelsAsync(request, 1, 1);

        // Assert
        eventJob.Should().NotBeNull();
    }

    #endregion
}
