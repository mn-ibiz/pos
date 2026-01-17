using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Repositories;
using Xunit;

namespace HospitalityPOS.Business.Tests.Repositories;

/// <summary>
/// Unit tests for the Repository class.
/// </summary>
public class RepositoryTests : IDisposable
{
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestDbContext : POSDbContext
    {
        public TestDbContext(DbContextOptions<POSDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity>().ToTable("TestEntities");
        }
    }

    private readonly TestDbContext _context;
    private readonly Repository<TestEntity> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _repository = new Repository<TestEntity>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Repository<TestEntity>(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };

        // Act
        var result = await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        await _repository.AddAsync(new TestEntity { Name = "Test1" });
        await _repository.AddAsync(new TestEntity { Name = "Test2" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnMatchingEntities()
    {
        // Arrange
        await _repository.AddAsync(new TestEntity { Name = "Match" });
        await _repository.AddAsync(new TestEntity { Name = "NoMatch" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(e => e.Name == "Match");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Match");
    }

    [Fact]
    public async Task UpdateAsync_ShouldMarkEntityAsModified()
    {
        // Arrange
        var entity = new TestEntity { Name = "Original" };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        entity.Name = "Updated";
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(entity.Id);
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenEntityIsSoftDeletable()
    {
        // Arrange
        var entity = new TestEntity { Name = "ToDelete", IsActive = true };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity.Id);
        await _context.SaveChangesAsync();

        // Assert - Entity should be soft deleted (IsActive = false)
        var deletedEntity = await _context.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Act
        var action = async () => await _repository.DeleteAsync(999);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Entity with ID 999 not found.");
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnTrue_WhenMatchExists()
    {
        // Arrange
        await _repository.AddAsync(new TestEntity { Name = "Exists" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(e => e.Name == "Exists");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnFalse_WhenNoMatchExists()
    {
        // Act
        var result = await _repository.AnyAsync(e => e.Name == "DoesNotExist");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnTotalCount_WhenNoPredicateProvided()
    {
        // Arrange
        await _repository.AddAsync(new TestEntity { Name = "Test1" });
        await _repository.AddAsync(new TestEntity { Name = "Test2" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnFilteredCount_WhenPredicateProvided()
    {
        // Arrange
        await _repository.AddAsync(new TestEntity { Name = "Match" });
        await _repository.AddAsync(new TestEntity { Name = "Match" });
        await _repository.AddAsync(new TestEntity { Name = "NoMatch" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync(e => e.Name == "Match");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void Query_ShouldReturnQueryable()
    {
        // Act
        var query = _repository.Query();

        // Assert
        query.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void QueryNoTracking_ShouldReturnQueryable()
    {
        // Act
        var query = _repository.QueryNoTracking();

        // Assert
        query.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleEntities()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Test1" },
            new TestEntity { Name = "Test2" },
            new TestEntity { Name = "Test3" }
        };

        // Act
        await _repository.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(3);
    }
}
