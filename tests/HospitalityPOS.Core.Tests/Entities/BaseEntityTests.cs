using FluentAssertions;
using HospitalityPOS.Core.Entities;
using Xunit;

namespace HospitalityPOS.Core.Tests.Entities;

/// <summary>
/// Unit tests for the BaseEntity class.
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void NewEntity_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().Be(0);
        entity.IsActive.Should().BeTrue();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeNull();
        entity.CreatedByUserId.Should().BeNull();
        entity.UpdatedByUserId.Should().BeNull();
    }

    [Fact]
    public void Entity_ShouldImplementIEntity()
    {
        // Arrange & Act
        var entity = new TestEntity { Id = 42 };

        // Assert
        entity.Id.Should().Be(42);
    }

    [Fact]
    public void Entity_ShouldImplementISoftDeletable()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.IsActive = false;

        // Assert
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Entity_ShouldImplementIAuditable()
    {
        // Arrange
        var entity = new TestEntity();
        var now = DateTime.UtcNow;

        // Act
        entity.CreatedAt = now;
        entity.UpdatedAt = now.AddHours(1);
        entity.CreatedByUserId = 1;
        entity.UpdatedByUserId = 2;

        // Assert
        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now.AddHours(1));
        entity.CreatedByUserId.Should().Be(1);
        entity.UpdatedByUserId.Should().Be(2);
    }
}
