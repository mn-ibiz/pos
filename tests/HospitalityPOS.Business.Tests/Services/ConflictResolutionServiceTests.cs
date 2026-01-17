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

/// <summary>
/// Unit tests for ConflictResolutionService.
/// </summary>
public class ConflictResolutionServiceTests
{
    private readonly Mock<IRepository<SyncConflict>> _conflictRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ConflictResolutionService>> _loggerMock;
    private readonly ConflictResolutionService _service;

    public ConflictResolutionServiceTests()
    {
        _conflictRepositoryMock = new Mock<IRepository<SyncConflict>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ConflictResolutionService>>();

        _service = new ConflictResolutionService(
            _conflictRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var action = () => new ConflictResolutionService(
            null!,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflictRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        var action = () => new ConflictResolutionService(
            _conflictRepositoryMock.Object,
            null!,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ConflictResolutionService(
            _conflictRepositoryMock.Object,
            _unitOfWorkMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        var service = new ConflictResolutionService(
            _conflictRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        service.Should().NotBeNull();
        service.GetAllRules().Should().NotBeEmpty();
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public async Task DetectConflictAsync_WithNullEntityType_ThrowsArgumentNullException()
    {
        var action = async () => await _service.DetectConflictAsync(
            null!, 1, "{}", "{}", DateTime.UtcNow, DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DetectConflictAsync_WithEmptyEntityType_ThrowsArgumentNullException()
    {
        var action = async () => await _service.DetectConflictAsync(
            "", 1, "{}", "{}", DateTime.UtcNow, DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DetectConflictAsync_WithIdenticalData_ReturnsNull()
    {
        var data = "{\"name\":\"Test\",\"price\":10.00}";

        var result = await _service.DetectConflictAsync(
            "Product", 1, data, data, DateTime.UtcNow, DateTime.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectConflictAsync_WithDifferentData_CreatesConflict()
    {
        var localData = "{\"name\":\"Local Product\",\"price\":10.00}";
        var remoteData = "{\"name\":\"Remote Product\",\"price\":15.00}";

        _conflictRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SyncConflict>()))
            .ReturnsAsync((SyncConflict c) => { c.Id = 1; return c; });

        var result = await _service.DetectConflictAsync(
            "Product", 1, localData, remoteData, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        result.Should().NotBeNull();
        result!.EntityType.Should().Be("Product");
        result.EntityId.Should().Be(1);
        result.Status.Should().Be(ConflictStatus.Detected);

        _conflictRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SyncConflict>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public void GetConflictingFields_WithDifferentProperties_ReturnsConflictingFields()
    {
        var localData = "{\"name\":\"Local\",\"price\":10,\"stock\":100}";
        var remoteData = "{\"name\":\"Remote\",\"price\":15,\"stock\":100}";

        var fields = _service.GetConflictingFields(localData, remoteData);

        fields.Should().Contain("name");
        fields.Should().Contain("price");
        fields.Should().NotContain("stock");
    }

    [Fact]
    public void GetConflictingFields_WithMissingProperties_IncludesThem()
    {
        var localData = "{\"name\":\"Local\",\"localOnly\":true}";
        var remoteData = "{\"name\":\"Remote\",\"remoteOnly\":true}";

        var fields = _service.GetConflictingFields(localData, remoteData);

        fields.Should().Contain("name");
        fields.Should().Contain("localOnly");
        fields.Should().Contain("remoteOnly");
    }

    [Fact]
    public void HasMeaningfulDifference_WithIdenticalData_ReturnsFalse()
    {
        var data = "{\"name\":\"Test\"}";

        var result = _service.HasMeaningfulDifference(data, data);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasMeaningfulDifference_WithDifferentFormatting_ReturnsFalse()
    {
        var data1 = "{\"name\":\"Test\",\"value\":1}";
        var data2 = "{ \"name\" : \"Test\" , \"value\" : 1 }";

        var result = _service.HasMeaningfulDifference(data1, data2);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasMeaningfulDifference_WithDifferentValues_ReturnsTrue()
    {
        var data1 = "{\"name\":\"Test1\"}";
        var data2 = "{\"name\":\"Test2\"}";

        var result = _service.HasMeaningfulDifference(data1, data2);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasMeaningfulDifference_WithBothEmpty_ReturnsFalse()
    {
        var result = _service.HasMeaningfulDifference("", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasMeaningfulDifference_WithOneEmpty_ReturnsTrue()
    {
        var result = _service.HasMeaningfulDifference("{}", "");

        result.Should().BeTrue();
    }

    #endregion

    #region Conflict Resolution Tests

    [Fact]
    public async Task ResolveAsync_WithNullConflict_ThrowsArgumentNullException()
    {
        var action = async () => await _service.ResolveAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ResolveAsync_WithReceiptConflict_UsesLocalWins()
    {
        var conflict = CreateTestConflict(SyncEntityType.Receipt);

        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ResolveAsync(conflict);

        result.Success.Should().BeTrue();
        result.AppliedResolution.Should().Be(ConflictResolutionType.LocalWins);
        result.WasAutoResolved.Should().BeTrue();
        result.ResultingData.Should().Be(conflict.LocalData);
    }

    [Fact]
    public async Task ResolveAsync_WithProductConflict_UsesRemoteWins()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);

        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ResolveAsync(conflict);

        result.Success.Should().BeTrue();
        result.AppliedResolution.Should().Be(ConflictResolutionType.RemoteWins);
        result.ResultingData.Should().Be(conflict.RemoteData);
    }

    [Fact]
    public async Task ResolveAsync_WithInventoryConflict_UsesLastWriteWins()
    {
        var conflict = CreateTestConflict(SyncEntityType.Inventory);
        conflict.LocalTimestamp = DateTime.UtcNow.AddHours(1);
        conflict.RemoteTimestamp = DateTime.UtcNow;

        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ResolveAsync(conflict);

        result.Success.Should().BeTrue();
        result.AppliedResolution.Should().Be(ConflictResolutionType.LastWriteWins);
        result.ResultingData.Should().Be(conflict.LocalData); // Local is newer
    }

    [Fact]
    public async Task ResolveAsync_WithCustomerPointsConflict_RequiresManualResolution()
    {
        // Add customer points rule that requires manual review
        _service.AddOrUpdateRule(new ConflictResolutionRuleDto
        {
            EntityType = "Customer",
            PropertyName = "PointsBalance",
            DefaultResolution = ConflictResolutionType.Manual,
            RequireManualReview = true
        });

        var conflict = CreateTestConflict(SyncEntityType.Customer);

        var result = await _service.ResolveAsync(conflict);

        result.Success.Should().BeFalse();
        result.NewStatus.Should().Be(ConflictStatus.PendingManual);
    }

    [Fact]
    public async Task ResolveByIdAsync_WithNonExistentId_ReturnsFailed()
    {
        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncConflict?)null);

        var result = await _service.ResolveByIdAsync(999);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ManualResolveAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        var action = async () => await _service.ManualResolveAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ManualResolveAsync_WithNonExistentConflict_ReturnsFailed()
    {
        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncConflict?)null);

        var request = new ManualResolveConflictDto
        {
            ConflictId = 999,
            Resolution = ConflictResolutionType.LocalWins,
            ResolvedByUserId = 1
        };

        var result = await _service.ManualResolveAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ManualResolveAsync_WithAlreadyResolved_ReturnsFailed()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);
        conflict.IsResolved = true;

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);

        var request = new ManualResolveConflictDto
        {
            ConflictId = 1,
            Resolution = ConflictResolutionType.LocalWins,
            ResolvedByUserId = 1
        };

        var result = await _service.ManualResolveAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already resolved");
    }

    [Fact]
    public async Task ManualResolveAsync_WithValidRequest_ResolvesSuccessfully()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var request = new ManualResolveConflictDto
        {
            ConflictId = 1,
            Resolution = ConflictResolutionType.LocalWins,
            ResolvedByUserId = 1,
            Notes = "Manager decision"
        };

        var result = await _service.ManualResolveAsync(request);

        result.Success.Should().BeTrue();
        result.WasAutoResolved.Should().BeFalse();
        result.AppliedResolution.Should().Be(ConflictResolutionType.LocalWins);
        result.ResultingData.Should().Be(conflict.LocalData);
    }

    [Fact]
    public async Task ManualResolveAsync_WithMergedResolution_UsesProvidedMergedData()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);
        var mergedData = "{\"name\":\"Merged\",\"price\":12.50}";

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var request = new ManualResolveConflictDto
        {
            ConflictId = 1,
            Resolution = ConflictResolutionType.Merged,
            ResolvedByUserId = 1,
            MergedData = mergedData
        };

        var result = await _service.ManualResolveAsync(request);

        result.Success.Should().BeTrue();
        result.ResultingData.Should().Be(mergedData);
    }

    #endregion

    #region Ignore Conflict Tests

    [Fact]
    public async Task IgnoreConflictAsync_WithNonExistentConflict_ReturnsFalse()
    {
        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncConflict?)null);

        var result = await _service.IgnoreConflictAsync(999, 1, "Test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IgnoreConflictAsync_WithValidConflict_IgnoresAndReturnsTrue()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var result = await _service.IgnoreConflictAsync(1, 1, "Not relevant");

        result.Should().BeTrue();
        conflict.IsResolved.Should().BeTrue();
        conflict.ResolutionNotes.Should().Contain("[IGNORED]");
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetPendingConflictsAsync_ReturnsUnresolvedConflicts()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product),
            CreateTestConflict(SyncEntityType.Category),
            CreateTestConflict(SyncEntityType.Order)
        };
        conflicts[2].IsResolved = true;

        _conflictRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncConflict, bool>>>()))
            .ReturnsAsync(conflicts.Where(c => !c.IsResolved && c.IsActive));

        var result = await _service.GetPendingConflictsAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetConflictByIdAsync_WithValidId_ReturnsConflict()
    {
        var conflict = CreateTestConflict(SyncEntityType.Product);

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);

        var result = await _service.GetConflictByIdAsync(1);

        result.Should().NotBeNull();
        result!.EntityType.Should().Be("Product");
    }

    [Fact]
    public async Task GetConflictByIdAsync_WithInvalidId_ReturnsNull()
    {
        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncConflict?)null);

        var result = await _service.GetConflictByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryConflictsAsync_WithEntityTypeFilter_FiltersResults()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product),
            CreateTestConflict(SyncEntityType.Category),
            CreateTestConflict(SyncEntityType.Product)
        };

        _conflictRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncConflict, bool>>>()))
            .ReturnsAsync(conflicts);

        var query = new ConflictQueryDto { EntityType = "Product" };
        var result = await _service.QueryConflictsAsync(query);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.EntityType == "Product");
    }

    [Fact]
    public async Task GetConflictSummaryAsync_ReturnsCorrectCounts()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product), // Pending
            CreateTestConflict(SyncEntityType.Product), // Pending
            CreateTestConflict(SyncEntityType.Category) // Auto-resolved
        };
        conflicts[2].IsResolved = true;
        conflicts[2].ResolvedByUserId = null;

        _conflictRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncConflict, bool>>>()))
            .ReturnsAsync(conflicts);

        var result = await _service.GetConflictSummaryAsync();

        result.TotalConflicts.Should().Be(3);
        result.PendingManual.Should().Be(2);
        result.AutoResolved.Should().Be(1);
    }

    #endregion

    #region Resolution Rules Tests

    [Fact]
    public void GetApplicableRule_ForReceipt_ReturnsLocalWins()
    {
        var rule = _service.GetApplicableRule("Receipt");

        rule.DefaultResolution.Should().Be(ConflictResolutionType.LocalWins);
    }

    [Fact]
    public void GetApplicableRule_ForProductPrice_ReturnsRemoteWins()
    {
        var rule = _service.GetApplicableRule("Product", "Price");

        rule.DefaultResolution.Should().Be(ConflictResolutionType.RemoteWins);
    }

    [Fact]
    public void GetApplicableRule_ForInventory_ReturnsLastWriteWins()
    {
        var rule = _service.GetApplicableRule("Inventory");

        rule.DefaultResolution.Should().Be(ConflictResolutionType.LastWriteWins);
    }

    [Fact]
    public void GetApplicableRule_ForUnknownEntity_ReturnsDefaultRemoteWins()
    {
        var rule = _service.GetApplicableRule("UnknownEntity");

        rule.DefaultResolution.Should().Be(ConflictResolutionType.RemoteWins);
        rule.Description.Should().Contain("Default");
    }

    [Fact]
    public void GetAllRules_ReturnsDefaultRules()
    {
        var rules = _service.GetAllRules();

        rules.Should().NotBeEmpty();
        rules.Should().Contain(r => r.EntityType == "Receipt");
        rules.Should().Contain(r => r.EntityType == "Product");
    }

    [Fact]
    public void AddOrUpdateRule_AddsNewRule()
    {
        var newRule = new ConflictResolutionRuleDto
        {
            EntityType = "CustomEntity",
            DefaultResolution = ConflictResolutionType.Merged,
            Description = "Custom rule"
        };

        _service.AddOrUpdateRule(newRule);

        var rule = _service.GetApplicableRule("CustomEntity");
        rule.DefaultResolution.Should().Be(ConflictResolutionType.Merged);
    }

    [Fact]
    public void AddOrUpdateRule_WithNullRule_ThrowsArgumentNullException()
    {
        var action = () => _service.AddOrUpdateRule(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOrUpdateRule_UpdatesExistingRule()
    {
        var updatedRule = new ConflictResolutionRuleDto
        {
            EntityType = "Product",
            DefaultResolution = ConflictResolutionType.LocalWins,
            Description = "Updated rule"
        };

        _service.AddOrUpdateRule(updatedRule);

        var rule = _service.GetApplicableRule("Product");
        rule.DefaultResolution.Should().Be(ConflictResolutionType.LocalWins);
    }

    [Fact]
    public void RemoveRule_WithExistingRule_ReturnsTrue()
    {
        var result = _service.RemoveRule("Receipt");

        result.Should().BeTrue();
        var rule = _service.GetApplicableRule("Receipt");
        rule.Description.Should().Contain("Default");  // Falls back to default
    }

    [Fact]
    public void RemoveRule_WithNonExistentRule_ReturnsFalse()
    {
        var result = _service.RemoveRule("NonExistent");

        result.Should().BeFalse();
    }

    [Fact]
    public void ResetToDefaultRules_RestoresDefaults()
    {
        // Modify a rule
        _service.AddOrUpdateRule(new ConflictResolutionRuleDto
        {
            EntityType = "Receipt",
            DefaultResolution = ConflictResolutionType.RemoteWins
        });

        // Reset
        _service.ResetToDefaultRules();

        // Verify reset
        var rule = _service.GetApplicableRule("Receipt");
        rule.DefaultResolution.Should().Be(ConflictResolutionType.LocalWins);
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public async Task GetConflictAuditTrailAsync_ReturnsAuditEntries()
    {
        // Trigger some audit entries by detecting a conflict
        _conflictRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SyncConflict>()))
            .ReturnsAsync((SyncConflict c) => { c.Id = 1; return c; });

        await _service.DetectConflictAsync("Product", 1, "{\"a\":1}", "{\"b\":2}", DateTime.UtcNow, DateTime.UtcNow);

        var audits = await _service.GetConflictAuditTrailAsync(1);

        audits.Should().NotBeEmpty();
        audits.Should().Contain(a => a.Action == "Detected");
    }

    [Fact]
    public async Task LogAuditAsync_CreatesAuditEntry()
    {
        await _service.LogAuditAsync(1, "TestAction", ConflictStatus.Detected, ConflictStatus.Resolved, 1, "Test details");

        var audits = await _service.GetConflictAuditTrailAsync(1);

        audits.Should().Contain(a => a.Action == "TestAction" && a.UserId == 1);
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task AutoResolveAllAsync_ResolvesAutoResolvableConflicts()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product), // Auto-resolvable (RemoteWins)
            CreateTestConflict(SyncEntityType.Receipt)  // Auto-resolvable (LocalWins)
        };

        _conflictRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncConflict, bool>>>()))
            .ReturnsAsync(conflicts);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var count = await _service.AutoResolveAllAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task BulkResolveAsync_ResolvesMultipleConflicts()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product),
            CreateTestConflict(SyncEntityType.Category)
        };
        conflicts[0].Id = 1;
        conflicts[1].Id = 2;

        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflicts[0]);
        _conflictRepositoryMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(conflicts[1]);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var count = await _service.BulkResolveAsync(
            new[] { 1, 2 },
            ConflictResolutionType.RemoteWins,
            1,
            "Bulk update");

        count.Should().Be(2);
    }

    [Fact]
    public async Task PurgeResolvedConflictsAsync_SoftDeletesOldConflicts()
    {
        var oldDate = DateTime.UtcNow.AddMonths(-3);
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict(SyncEntityType.Product)
        };
        conflicts[0].IsResolved = true;
        conflicts[0].ResolvedAt = oldDate;

        _conflictRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncConflict, bool>>>()))
            .ReturnsAsync(conflicts);
        _conflictRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        var count = await _service.PurgeResolvedConflictsAsync(DateTime.UtcNow.AddMonths(-1));

        count.Should().Be(1);
        conflicts[0].IsActive.Should().BeFalse();
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void ConflictResolutionResultDto_Succeeded_SetsCorrectValues()
    {
        var result = ConflictResolutionResultDto.Succeeded(1, ConflictResolutionType.LocalWins, "{}", true);

        result.Success.Should().BeTrue();
        result.ConflictId.Should().Be(1);
        result.AppliedResolution.Should().Be(ConflictResolutionType.LocalWins);
        result.WasAutoResolved.Should().BeTrue();
        result.NewStatus.Should().Be(ConflictStatus.AutoResolved);
    }

    [Fact]
    public void ConflictResolutionResultDto_Failed_SetsCorrectValues()
    {
        var result = ConflictResolutionResultDto.Failed(1, "Test error");

        result.Success.Should().BeFalse();
        result.ConflictId.Should().Be(1);
        result.ErrorMessage.Should().Be("Test error");
        result.NewStatus.Should().Be(ConflictStatus.PendingManual);
    }

    [Fact]
    public void DefaultConflictRules_ContainsExpectedRules()
    {
        var rules = DefaultConflictRules.Rules;

        rules.Should().Contain(r => r.EntityType == "Receipt" && r.DefaultResolution == ConflictResolutionType.LocalWins);
        rules.Should().Contain(r => r.EntityType == "Product" && r.PropertyName == "Price" && r.DefaultResolution == ConflictResolutionType.RemoteWins);
        rules.Should().Contain(r => r.EntityType == "Inventory" && r.DefaultResolution == ConflictResolutionType.LastWriteWins);
        rules.Should().Contain(r => r.RequireManualReview && r.DefaultResolution == ConflictResolutionType.Manual);
    }

    [Fact]
    public void ConflictDetailDto_CalculatesConflictingFieldsCorrectly()
    {
        var detail = new ConflictDetailDto
        {
            LocalData = "{\"a\":1}",
            RemoteData = "{\"b\":2}",
            ConflictingFields = "[\"a\",\"b\"]"
        };

        detail.ConflictingFields.Should().Contain("a");
        detail.ConflictingFields.Should().Contain("b");
    }

    #endregion

    #region Helper Methods

    private static SyncConflict CreateTestConflict(SyncEntityType entityType)
    {
        return new SyncConflict
        {
            Id = 1,
            SyncBatchId = 1,
            EntityType = entityType,
            EntityId = 1,
            LocalData = "{\"name\":\"Local\",\"price\":10}",
            RemoteData = "{\"name\":\"Remote\",\"price\":15}",
            LocalTimestamp = DateTime.UtcNow.AddMinutes(-30),
            RemoteTimestamp = DateTime.UtcNow.AddMinutes(-15),
            IsResolved = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
