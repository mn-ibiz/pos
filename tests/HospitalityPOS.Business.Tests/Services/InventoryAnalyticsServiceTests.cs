using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Repositories;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for InventoryAnalyticsService.
/// </summary>
public class InventoryAnalyticsServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly IInventoryAnalyticsService _service;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<StockMovement> _stockMovementRepository;
    private readonly IRepository<StockValuationConfig> _valuationConfigRepository;
    private readonly IRepository<StockValuationSnapshot> _valuationSnapshotRepository;
    private readonly IRepository<StockValuationDetail> _valuationDetailRepository;
    private readonly IRepository<ReorderRule> _reorderRuleRepository;
    private readonly IRepository<ReorderSuggestion> _reorderSuggestionRepository;
    private readonly IRepository<ShrinkageRecord> _shrinkageRecordRepository;
    private readonly IRepository<ShrinkageAnalysisPeriod> _shrinkageAnalysisPeriodRepository;
    private readonly IRepository<DeadStockItem> _deadStockItemRepository;
    private readonly IRepository<DeadStockConfig> _deadStockConfigRepository;
    private readonly IRepository<InventoryTurnoverAnalysis> _turnoverAnalysisRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;

    public InventoryAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);

        _productRepository = new Repository<Product>(_context);
        _inventoryRepository = new Repository<InventoryItem>(_context);
        _stockMovementRepository = new Repository<StockMovement>(_context);
        _valuationConfigRepository = new Repository<StockValuationConfig>(_context);
        _valuationSnapshotRepository = new Repository<StockValuationSnapshot>(_context);
        _valuationDetailRepository = new Repository<StockValuationDetail>(_context);
        _reorderRuleRepository = new Repository<ReorderRule>(_context);
        _reorderSuggestionRepository = new Repository<ReorderSuggestion>(_context);
        _shrinkageRecordRepository = new Repository<ShrinkageRecord>(_context);
        _shrinkageAnalysisPeriodRepository = new Repository<ShrinkageAnalysisPeriod>(_context);
        _deadStockItemRepository = new Repository<DeadStockItem>(_context);
        _deadStockConfigRepository = new Repository<DeadStockConfig>(_context);
        _turnoverAnalysisRepository = new Repository<InventoryTurnoverAnalysis>(_context);
        _orderItemRepository = new Repository<OrderItem>(_context);

        _service = new InventoryAnalyticsService(
            _productRepository,
            _inventoryRepository,
            _stockMovementRepository,
            _valuationConfigRepository,
            _valuationSnapshotRepository,
            _valuationDetailRepository,
            _reorderRuleRepository,
            _reorderSuggestionRepository,
            _shrinkageRecordRepository,
            _shrinkageAnalysisPeriodRepository,
            _deadStockItemRepository,
            _deadStockConfigRepository,
            _turnoverAnalysisRepository,
            _orderItemRepository);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Stock Valuation Tests

    [Fact]
    public async Task ConfigureValuationMethodAsync_ShouldCreateNewConfig()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var config = await _service.ConfigureValuationMethodAsync(
            productId,
            StockValuationMethod.WeightedAverage);

        // Assert
        config.Should().NotBeNull();
        config.ProductId.Should().Be(productId);
        config.Method.Should().Be(StockValuationMethod.WeightedAverage);
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ConfigureValuationMethodAsync_ShouldUpdateExistingConfig()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await _service.ConfigureValuationMethodAsync(productId, StockValuationMethod.FIFO);

        // Act
        var config = await _service.ConfigureValuationMethodAsync(
            productId,
            StockValuationMethod.LIFO);

        // Assert
        config.Method.Should().Be(StockValuationMethod.LIFO);

        var allConfigs = await _valuationConfigRepository.GetAllAsync();
        allConfigs.Count(c => c.ProductId == productId).Should().Be(1);
    }

    [Fact]
    public async Task GetValuationMethodAsync_ShouldReturnConfiguredMethod()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await _service.ConfigureValuationMethodAsync(productId, StockValuationMethod.StandardCost);

        // Act
        var method = await _service.GetValuationMethodAsync(productId);

        // Assert
        method.Should().Be(StockValuationMethod.StandardCost);
    }

    [Fact]
    public async Task GetValuationMethodAsync_ShouldReturnWeightedAverageAsDefault()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var method = await _service.GetValuationMethodAsync(productId);

        // Assert
        method.Should().Be(StockValuationMethod.WeightedAverage);
    }

    [Fact]
    public async Task CalculateProductCostAsync_FIFO_ShouldCalculateCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            SKU = "TEST-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        await _service.ConfigureValuationMethodAsync(product.Id, StockValuationMethod.FIFO);

        // Add stock movements (purchases)
        var movements = new List<StockMovement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MovementType = StockMovementType.Purchase,
                Quantity = 10,
                UnitCost = 40m,
                MovementDate = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MovementType = StockMovementType.Purchase,
                Quantity = 15,
                UnitCost = 45m,
                MovementDate = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MovementType = StockMovementType.Purchase,
                Quantity = 20,
                UnitCost = 50m,
                MovementDate = DateTime.UtcNow.AddDays(-1)
            }
        };

        foreach (var movement in movements)
        {
            await _stockMovementRepository.AddAsync(movement);
        }

        // Act
        var result = await _service.CalculateProductCostAsync(product.Id, 12);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(product.Id);
        result.Method.Should().Be(StockValuationMethod.FIFO);
        // FIFO: 10 units @ 40 + 2 units @ 45 = 400 + 90 = 490
        // Average: 490 / 12 = 40.83
        result.TotalCost.Should().BeApproximately(490m, 0.01m);
    }

    [Fact]
    public async Task CalculateWeightedAverageCostAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            SKU = "TEST-002",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var movements = new List<StockMovement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = StockMovementType.Purchase,
                Quantity = 100,
                UnitCost = 10m,
                MovementDate = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = StockMovementType.Purchase,
                Quantity = 200,
                UnitCost = 15m,
                MovementDate = DateTime.UtcNow.AddDays(-5)
            }
        };

        foreach (var movement in movements)
        {
            await _stockMovementRepository.AddAsync(movement);
        }

        // Act
        var avgCost = await _service.CalculateWeightedAverageCostAsync(productId);

        // Assert
        // Total cost: (100 * 10) + (200 * 15) = 1000 + 3000 = 4000
        // Total quantity: 300
        // Average: 4000 / 300 = 13.33
        avgCost.Should().BeApproximately(13.33m, 0.01m);
    }

    [Fact]
    public async Task CreateValuationSnapshotAsync_ShouldCreateSnapshot()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Snapshot Product",
            SKU = "SNAP-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 50,
            ReorderLevel = 10,
            MaxStock = 100
        };
        await _inventoryRepository.AddAsync(inventory);

        // Act
        var snapshot = await _service.CreateValuationSnapshotAsync("Monthly Valuation");

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.SnapshotName.Should().Be("Monthly Valuation");
        snapshot.SnapshotDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetValuationSnapshotsAsync_ShouldReturnSnapshotsInDateRange()
    {
        // Arrange
        var snapshot1 = new StockValuationSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotName = "January",
            SnapshotDate = DateTime.UtcNow.AddDays(-30),
            TotalInventoryValue = 10000m,
            TotalItemCount = 100
        };
        var snapshot2 = new StockValuationSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotName = "February",
            SnapshotDate = DateTime.UtcNow.AddDays(-10),
            TotalInventoryValue = 12000m,
            TotalItemCount = 120
        };

        await _valuationSnapshotRepository.AddAsync(snapshot1);
        await _valuationSnapshotRepository.AddAsync(snapshot2);

        // Act
        var snapshots = await _service.GetValuationSnapshotsAsync(
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow);

        // Assert
        snapshots.Should().HaveCount(1);
        snapshots.First().SnapshotName.Should().Be("February");
    }

    #endregion

    #region Reorder Management Tests

    [Fact]
    public async Task CreateReorderRuleAsync_ShouldCreateRule()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateReorderRuleRequest
        {
            ProductId = productId,
            RuleName = "Auto Reorder Rule",
            MinimumStockLevel = 10,
            ReorderPoint = 20,
            ReorderQuantity = 50,
            MaximumStockLevel = 100,
            LeadTimeDays = 7,
            SafetyStockDays = 3,
            IsAutomatic = true,
            PreferredSupplierId = Guid.NewGuid()
        };

        // Act
        var rule = await _service.CreateReorderRuleAsync(request);

        // Assert
        rule.Should().NotBeNull();
        rule.ProductId.Should().Be(productId);
        rule.RuleName.Should().Be("Auto Reorder Rule");
        rule.MinimumStockLevel.Should().Be(10);
        rule.ReorderPoint.Should().Be(20);
        rule.ReorderQuantity.Should().Be(50);
        rule.IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateReorderRuleAsync_ShouldUpdateRule()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var rule = new ReorderRule
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            RuleName = "Original Rule",
            MinimumStockLevel = 10,
            ReorderPoint = 20,
            ReorderQuantity = 50,
            IsActive = true
        };
        await _reorderRuleRepository.AddAsync(rule);

        var request = new UpdateReorderRuleRequest
        {
            RuleName = "Updated Rule",
            MinimumStockLevel = 15,
            ReorderPoint = 25,
            ReorderQuantity = 75
        };

        // Act
        var updated = await _service.UpdateReorderRuleAsync(rule.Id, request);

        // Assert
        updated.Should().NotBeNull();
        updated!.RuleName.Should().Be("Updated Rule");
        updated.MinimumStockLevel.Should().Be(15);
        updated.ReorderPoint.Should().Be(25);
        updated.ReorderQuantity.Should().Be(75);
    }

    [Fact]
    public async Task CalculateEOQAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // EOQ = sqrt((2 * D * S) / H)
        // D = Annual demand = 1000
        // S = Order cost = 50
        // H = Holding cost = 5
        // EOQ = sqrt((2 * 1000 * 50) / 5) = sqrt(20000) = 141.42

        // Act
        var result = await _service.CalculateEOQAsync(
            productId,
            annualDemand: 1000,
            orderingCost: 50m,
            holdingCostPerUnit: 5m);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.EconomicOrderQuantity.Should().BeApproximately(141.42m, 0.5m);
        result.AnnualDemand.Should().Be(1000);
        result.OrderingCost.Should().Be(50m);
        result.HoldingCostPerUnit.Should().Be(5m);
    }

    [Fact]
    public async Task CalculateReorderPointAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Reorder Point = (Daily Demand * Lead Time) + Safety Stock
        // Daily Demand = 10
        // Lead Time = 5 days
        // Safety Stock Days = 3
        // Safety Stock = 10 * 3 = 30
        // Reorder Point = (10 * 5) + 30 = 80

        // Act
        var result = await _service.CalculateReorderPointAsync(
            productId,
            averageDailyDemand: 10,
            leadTimeDays: 5,
            safetyStockDays: 3);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.ReorderPoint.Should().Be(80);
        result.SafetyStock.Should().Be(30);
        result.AverageDailyDemand.Should().Be(10);
        result.LeadTimeDays.Should().Be(5);
    }

    [Fact]
    public async Task GenerateReorderSuggestionsAsync_ShouldGenerateSuggestions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Low Stock Product",
            SKU = "LOW-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 5,
            ReorderLevel = 20,
            MaxStock = 100
        };
        await _inventoryRepository.AddAsync(inventory);

        var rule = new ReorderRule
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            RuleName = "Auto Reorder",
            MinimumStockLevel = 10,
            ReorderPoint = 20,
            ReorderQuantity = 50,
            IsActive = true,
            IsAutomatic = true
        };
        await _reorderRuleRepository.AddAsync(rule);

        // Act
        var suggestions = await _service.GenerateReorderSuggestionsAsync();

        // Assert
        suggestions.Should().NotBeEmpty();
        var suggestion = suggestions.First(s => s.ProductId == productId);
        suggestion.SuggestedQuantity.Should().Be(50);
        suggestion.Priority.Should().Be(ReorderPriority.High); // Below minimum
    }

    [Fact]
    public async Task ApproveReorderSuggestionAsync_ShouldApproveSuggestion()
    {
        // Arrange
        var suggestion = new ReorderSuggestion
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            CurrentStock = 5,
            ReorderPoint = 20,
            SuggestedQuantity = 50,
            EstimatedCost = 2500m,
            Priority = ReorderPriority.High,
            Status = ReorderSuggestionStatus.Pending,
            GeneratedAt = DateTime.UtcNow
        };
        await _reorderSuggestionRepository.AddAsync(suggestion);

        // Act
        var approved = await _service.ApproveReorderSuggestionAsync(
            suggestion.Id,
            Guid.NewGuid(),
            "Approved for ordering");

        // Assert
        approved.Should().NotBeNull();
        approved!.Status.Should().Be(ReorderSuggestionStatus.Approved);
        approved.ApprovedById.Should().NotBeNull();
        approved.ApprovedAt.Should().NotBeNull();
        approved.Notes.Should().Be("Approved for ordering");
    }

    #endregion

    #region Shrinkage Analysis Tests

    [Fact]
    public async Task RecordShrinkageAsync_ShouldCreateRecord()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new RecordShrinkageRequest
        {
            ProductId = productId,
            ShrinkageType = ShrinkageType.Damage,
            Quantity = 5,
            UnitCost = 25m,
            Reason = "Dropped during handling",
            DiscoveredAt = DateTime.UtcNow,
            DiscoveredById = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Notes = "Glass container broke"
        };

        // Act
        var record = await _service.RecordShrinkageAsync(request);

        // Assert
        record.Should().NotBeNull();
        record.ProductId.Should().Be(productId);
        record.ShrinkageType.Should().Be(ShrinkageType.Damage);
        record.Quantity.Should().Be(5);
        record.TotalLoss.Should().Be(125m); // 5 * 25
    }

    [Fact]
    public async Task GetShrinkageSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var records = new List<ShrinkageRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShrinkageType = ShrinkageType.Theft,
                Quantity = 10,
                UnitCost = 50m,
                TotalLoss = 500m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShrinkageType = ShrinkageType.Damage,
                Quantity = 3,
                UnitCost = 50m,
                TotalLoss = 150m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ShrinkageType = ShrinkageType.Spoilage,
                Quantity = 20,
                UnitCost = 10m,
                TotalLoss = 200m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        foreach (var record in records)
        {
            await _shrinkageRecordRepository.AddAsync(record);
        }

        // Act
        var summary = await _service.GetShrinkageSummaryAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);

        // Assert
        summary.Should().NotBeNull();
        summary.TotalLoss.Should().Be(850m);
        summary.TotalQuantity.Should().Be(33);
        summary.RecordCount.Should().Be(3);
        summary.ByType.Should().ContainKey(ShrinkageType.Theft);
        summary.ByType[ShrinkageType.Theft].Should().Be(500m);
    }

    [Fact]
    public async Task AnalyzeShrinkagePatternsAsync_ShouldReturnAnalysis()
    {
        // Arrange
        var productId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            var record = new ShrinkageRecord
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShrinkageType = ShrinkageType.Theft,
                Quantity = 5,
                UnitCost = 20m,
                TotalLoss = 100m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-i)
            };
            await _shrinkageRecordRepository.AddAsync(record);
        }

        // Act
        var analysis = await _service.AnalyzeShrinkagePatternsAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        // Assert
        analysis.Should().NotBeNull();
        analysis.TotalLoss.Should().Be(1000m);
        analysis.PatternsIdentified.Should().NotBeEmpty();
        analysis.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShrinkageByProductAsync_ShouldReturnProductShrinkage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var records = new List<ShrinkageRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShrinkageType = ShrinkageType.Damage,
                Quantity = 5,
                UnitCost = 10m,
                TotalLoss = 50m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShrinkageType = ShrinkageType.Spoilage,
                Quantity = 10,
                UnitCost = 10m,
                TotalLoss = 100m,
                DiscoveredAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        foreach (var record in records)
        {
            await _shrinkageRecordRepository.AddAsync(record);
        }

        // Act
        var shrinkage = await _service.GetShrinkageByProductAsync(
            productId,
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);

        // Assert
        shrinkage.Should().HaveCount(2);
        shrinkage.Sum(s => s.TotalLoss).Should().Be(150m);
    }

    #endregion

    #region Dead Stock Analysis Tests

    [Fact]
    public async Task ConfigureDeadStockThresholdsAsync_ShouldCreateConfig()
    {
        // Arrange
        var request = new DeadStockConfigRequest
        {
            SlowMovingDays = 60,
            DeadStockDays = 120,
            ObsoleteDays = 365,
            IncludeSeasonalProducts = false,
            MinimumValueThreshold = 100m
        };

        // Act
        var config = await _service.ConfigureDeadStockThresholdsAsync(request);

        // Assert
        config.Should().NotBeNull();
        config.SlowMovingDays.Should().Be(60);
        config.DeadStockDays.Should().Be(120);
        config.ObsoleteDays.Should().Be(365);
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task IdentifyDeadStockAsync_ShouldIdentifyDeadStock()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Dead Stock Product",
            SKU = "DEAD-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            CurrentStock = 100,
            ReorderLevel = 10,
            MaxStock = 200,
            LastMovementDate = DateTime.UtcNow.AddDays(-150) // No movement for 150 days
        };
        await _inventoryRepository.AddAsync(inventory);

        // Configure thresholds
        var config = new DeadStockConfig
        {
            Id = Guid.NewGuid(),
            SlowMovingDays = 60,
            DeadStockDays = 90,
            ObsoleteDays = 180,
            IsActive = true
        };
        await _deadStockConfigRepository.AddAsync(config);

        // Act
        var deadStock = await _service.IdentifyDeadStockAsync();

        // Assert
        deadStock.Should().NotBeEmpty();
        var item = deadStock.First(d => d.ProductId == product.Id);
        item.Classification.Should().Be(DeadStockClassification.DeadStock);
        item.DaysSinceLastMovement.Should().BeGreaterOrEqualTo(150);
        item.InventoryValue.Should().Be(5000m); // 100 * 50
    }

    [Fact]
    public async Task GetDeadStockSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var deadStockItems = new List<DeadStockItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Classification = DeadStockClassification.SlowMoving,
                CurrentStock = 50,
                InventoryValue = 500m,
                DaysSinceLastMovement = 70,
                LastMovementDate = DateTime.UtcNow.AddDays(-70),
                IdentifiedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Classification = DeadStockClassification.DeadStock,
                CurrentStock = 100,
                InventoryValue = 2000m,
                DaysSinceLastMovement = 120,
                LastMovementDate = DateTime.UtcNow.AddDays(-120),
                IdentifiedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Classification = DeadStockClassification.Obsolete,
                CurrentStock = 200,
                InventoryValue = 4000m,
                DaysSinceLastMovement = 200,
                LastMovementDate = DateTime.UtcNow.AddDays(-200),
                IdentifiedAt = DateTime.UtcNow
            }
        };

        foreach (var item in deadStockItems)
        {
            await _deadStockItemRepository.AddAsync(item);
        }

        // Act
        var summary = await _service.GetDeadStockSummaryAsync();

        // Assert
        summary.Should().NotBeNull();
        summary.TotalDeadStockValue.Should().Be(6500m);
        summary.TotalItems.Should().Be(3);
        summary.SlowMovingValue.Should().Be(500m);
        summary.DeadStockValue.Should().Be(2000m);
        summary.ObsoleteValue.Should().Be(4000m);
    }

    [Fact]
    public async Task SuggestClearancePricesAsync_ShouldSuggestPrices()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Clearance Product",
            SKU = "CLR-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var deadStockItem = new DeadStockItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Classification = DeadStockClassification.Obsolete,
            CurrentStock = 50,
            InventoryValue = 2500m,
            DaysSinceLastMovement = 200,
            LastMovementDate = DateTime.UtcNow.AddDays(-200),
            IdentifiedAt = DateTime.UtcNow
        };
        await _deadStockItemRepository.AddAsync(deadStockItem);

        // Act
        var suggestions = await _service.SuggestClearancePricesAsync(product.Id);

        // Assert
        suggestions.Should().NotBeEmpty();
        var suggestion = suggestions.First();
        suggestion.ProductId.Should().Be(product.Id);
        suggestion.OriginalPrice.Should().Be(100m);
        suggestion.SuggestedClearancePrice.Should().BeLessThan(100m);
        suggestion.DiscountPercentage.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MarkAsWrittenOffAsync_ShouldMarkItemAsWrittenOff()
    {
        // Arrange
        var deadStockItem = new DeadStockItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Classification = DeadStockClassification.Obsolete,
            CurrentStock = 100,
            InventoryValue = 5000m,
            DaysSinceLastMovement = 400,
            LastMovementDate = DateTime.UtcNow.AddDays(-400),
            IdentifiedAt = DateTime.UtcNow.AddDays(-30),
            IsWrittenOff = false
        };
        await _deadStockItemRepository.AddAsync(deadStockItem);

        // Act
        var result = await _service.MarkAsWrittenOffAsync(
            deadStockItem.Id,
            Guid.NewGuid(),
            "Item no longer saleable");

        // Assert
        result.Should().NotBeNull();
        result!.IsWrittenOff.Should().BeTrue();
        result.WrittenOffAt.Should().NotBeNull();
        result.WrittenOffById.Should().NotBeNull();
        result.WriteOffReason.Should().Be("Item no longer saleable");
    }

    #endregion

    #region ABC Analysis Tests

    [Fact]
    public async Task PerformABCAnalysisAsync_ShouldCategorizeProducts()
    {
        // Arrange - Create products with different sales volumes
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "High Value A", SKU = "HVA-001", Price = 1000m, Cost = 500m },
            new() { Id = Guid.NewGuid(), Name = "High Value B", SKU = "HVB-001", Price = 800m, Cost = 400m },
            new() { Id = Guid.NewGuid(), Name = "Medium Value", SKU = "MV-001", Price = 200m, Cost = 100m },
            new() { Id = Guid.NewGuid(), Name = "Low Value A", SKU = "LVA-001", Price = 50m, Cost = 25m },
            new() { Id = Guid.NewGuid(), Name = "Low Value B", SKU = "LVB-001", Price = 30m, Cost = 15m }
        };

        foreach (var product in products)
        {
            await _productRepository.AddAsync(product);
        }

        // Create orders with varying quantities
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            Status = OrderStatus.Completed,
            OrderDate = DateTime.UtcNow.AddDays(-10),
            SubTotal = 10000m,
            Total = 10000m
        };
        await _context.Orders.AddAsync(order);

        var orderItems = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = products[0].Id, Quantity = 50, UnitPrice = 1000m, TotalPrice = 50000m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = products[1].Id, Quantity = 40, UnitPrice = 800m, TotalPrice = 32000m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = products[2].Id, Quantity = 30, UnitPrice = 200m, TotalPrice = 6000m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = products[3].Id, Quantity = 20, UnitPrice = 50m, TotalPrice = 1000m },
            new() { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = products[4].Id, Quantity = 10, UnitPrice = 30m, TotalPrice = 300m }
        };

        foreach (var item in orderItems)
        {
            await _orderItemRepository.AddAsync(item);
        }

        // Act
        var result = await _service.PerformABCAnalysisAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.AnalysisDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.TotalProducts.Should().Be(5);
        result.CategoryA.Should().NotBeEmpty();
        result.CategoryB.Should().NotBeEmpty();
        result.CategoryC.Should().NotBeEmpty();

        // High value products should be in Category A
        result.CategoryA.Should().Contain(p => p.ProductId == products[0].Id);
    }

    #endregion

    #region Inventory Turnover Tests

    [Fact]
    public async Task CalculateTurnoverRatioAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Turnover Product",
            SKU = "TURN-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        // Create stock movements
        var movements = new List<StockMovement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = StockMovementType.Sale,
                Quantity = 100,
                UnitCost = 50m,
                MovementDate = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = StockMovementType.Sale,
                Quantity = 150,
                UnitCost = 50m,
                MovementDate = DateTime.UtcNow.AddDays(-15)
            }
        };

        foreach (var movement in movements)
        {
            await _stockMovementRepository.AddAsync(movement);
        }

        // Inventory snapshot
        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 50,
            ReorderLevel = 20,
            MaxStock = 200
        };
        await _inventoryRepository.AddAsync(inventory);

        // Act
        var analysis = await _service.CalculateTurnoverRatioAsync(
            productId,
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow);

        // Assert
        analysis.Should().NotBeNull();
        analysis.ProductId.Should().Be(productId);
        analysis.CostOfGoodsSold.Should().Be(12500m); // 250 * 50
        analysis.TurnoverRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetInventoryTurnoverReportAsync_ShouldReturnReport()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Fast Moving", SKU = "FM-001", Price = 100m, Cost = 50m },
            new() { Id = Guid.NewGuid(), Name = "Slow Moving", SKU = "SM-001", Price = 200m, Cost = 100m }
        };

        foreach (var product in products)
        {
            await _productRepository.AddAsync(product);

            var inventory = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                CurrentStock = 100,
                ReorderLevel = 20,
                MaxStock = 200
            };
            await _inventoryRepository.AddAsync(inventory);
        }

        // Add sales for fast moving product
        for (int i = 0; i < 10; i++)
        {
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = products[0].Id,
                MovementType = StockMovementType.Sale,
                Quantity = 50,
                UnitCost = 50m,
                MovementDate = DateTime.UtcNow.AddDays(-i * 3)
            };
            await _stockMovementRepository.AddAsync(movement);
        }

        // Act
        var report = await _service.GetInventoryTurnoverReportAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        // Assert
        report.Should().NotBeEmpty();
        var fastMoving = report.FirstOrDefault(r => r.ProductId == products[0].Id);
        fastMoving.Should().NotBeNull();
        fastMoving!.TurnoverRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSlowMovingProductsAsync_ShouldIdentifySlowMovers()
    {
        // Arrange
        var slowProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Very Slow Product",
            SKU = "VSP-001",
            Price = 500m,
            Cost = 250m
        };
        await _productRepository.AddAsync(slowProduct);

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = slowProduct.Id,
            CurrentStock = 100,
            ReorderLevel = 10,
            MaxStock = 200,
            LastMovementDate = DateTime.UtcNow.AddDays(-90)
        };
        await _inventoryRepository.AddAsync(inventory);

        // Only one small sale
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = slowProduct.Id,
            MovementType = StockMovementType.Sale,
            Quantity = 2,
            UnitCost = 250m,
            MovementDate = DateTime.UtcNow.AddDays(-60)
        };
        await _stockMovementRepository.AddAsync(movement);

        // Act
        var slowMovers = await _service.GetSlowMovingProductsAsync(
            turnoverThreshold: 1.0m,
            daysPeriod: 90);

        // Assert
        slowMovers.Should().NotBeEmpty();
        slowMovers.Should().Contain(p => p.ProductId == slowProduct.Id);
    }

    [Fact]
    public async Task GetDaysOfSupplyAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Supply Product",
            SKU = "SUP-001",
            Price = 100m,
            Cost = 50m
        };
        await _productRepository.AddAsync(product);

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 100,
            ReorderLevel = 20,
            MaxStock = 200
        };
        await _inventoryRepository.AddAsync(inventory);

        // Add consistent sales
        for (int i = 0; i < 30; i++)
        {
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = StockMovementType.Sale,
                Quantity = 10,
                UnitCost = 50m,
                MovementDate = DateTime.UtcNow.AddDays(-i)
            };
            await _stockMovementRepository.AddAsync(movement);
        }

        // Act
        var daysOfSupply = await _service.GetDaysOfSupplyAsync(productId);

        // Assert
        // Current stock: 100, Average daily sales: 10, Days of supply: 10
        daysOfSupply.Should().BeApproximately(10, 1);
    }

    #endregion
}
