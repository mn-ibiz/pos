using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using Xunit;

namespace HospitalityPOS.Core.Tests.Entities;

/// <summary>
/// Unit tests for the LoyaltyMember entity.
/// </summary>
public class LoyaltyMemberTests
{
    [Fact]
    public void NewLoyaltyMember_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var member = new LoyaltyMember();

        // Assert
        member.Id.Should().Be(0);
        member.IsActive.Should().BeTrue();
        member.PhoneNumber.Should().BeEmpty();
        member.Name.Should().BeNull();
        member.Email.Should().BeNull();
        member.MembershipNumber.Should().BeEmpty();
        member.Tier.Should().Be(MembershipTier.Bronze);
        member.PointsBalance.Should().Be(0);
        member.LifetimePoints.Should().Be(0);
        member.LifetimeSpend.Should().Be(0);
        member.EnrolledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        member.LastVisit.Should().BeNull();
        member.VisitCount.Should().Be(0);
        member.Notes.Should().BeNull();
    }

    [Fact]
    public void LoyaltyMember_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var member = new LoyaltyMember();

        // Assert
        member.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void LoyaltyMember_ShouldSetPhoneNumber()
    {
        // Arrange
        var member = new LoyaltyMember();
        const string phoneNumber = "254712345678";

        // Act
        member.PhoneNumber = phoneNumber;

        // Assert
        member.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public void LoyaltyMember_ShouldSetOptionalName()
    {
        // Arrange
        var member = new LoyaltyMember();
        const string name = "John Doe";

        // Act
        member.Name = name;

        // Assert
        member.Name.Should().Be(name);
    }

    [Fact]
    public void LoyaltyMember_ShouldSetOptionalEmail()
    {
        // Arrange
        var member = new LoyaltyMember();
        const string email = "john@example.com";

        // Act
        member.Email = email;

        // Assert
        member.Email.Should().Be(email);
    }

    [Fact]
    public void LoyaltyMember_ShouldSetMembershipNumber()
    {
        // Arrange
        var member = new LoyaltyMember();
        const string membershipNumber = "LM-20251230-00001";

        // Act
        member.MembershipNumber = membershipNumber;

        // Assert
        member.MembershipNumber.Should().Be(membershipNumber);
    }

    [Theory]
    [InlineData(MembershipTier.Bronze)]
    [InlineData(MembershipTier.Silver)]
    [InlineData(MembershipTier.Gold)]
    [InlineData(MembershipTier.Platinum)]
    public void LoyaltyMember_ShouldSetTier(MembershipTier tier)
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.Tier = tier;

        // Assert
        member.Tier.Should().Be(tier);
    }

    [Fact]
    public void LoyaltyMember_ShouldTrackPointsBalance()
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.PointsBalance = 150.50m;

        // Assert
        member.PointsBalance.Should().Be(150.50m);
    }

    [Fact]
    public void LoyaltyMember_ShouldTrackLifetimePoints()
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.LifetimePoints = 1500.75m;

        // Assert
        member.LifetimePoints.Should().Be(1500.75m);
    }

    [Fact]
    public void LoyaltyMember_ShouldTrackLifetimeSpend()
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.LifetimeSpend = 50000.00m;

        // Assert
        member.LifetimeSpend.Should().Be(50000.00m);
    }

    [Fact]
    public void LoyaltyMember_ShouldTrackLastVisit()
    {
        // Arrange
        var member = new LoyaltyMember();
        var visitDate = DateTime.UtcNow;

        // Act
        member.LastVisit = visitDate;

        // Assert
        member.LastVisit.Should().Be(visitDate);
    }

    [Fact]
    public void LoyaltyMember_ShouldTrackVisitCount()
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.VisitCount = 42;

        // Assert
        member.VisitCount.Should().Be(42);
    }

    [Fact]
    public void LoyaltyMember_ShouldSupportSoftDelete()
    {
        // Arrange
        var member = new LoyaltyMember();

        // Act
        member.IsActive = false;

        // Assert
        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void LoyaltyMember_ShouldSupportAuditFields()
    {
        // Arrange
        var member = new LoyaltyMember();
        var now = DateTime.UtcNow;

        // Act
        member.CreatedAt = now;
        member.UpdatedAt = now.AddHours(1);
        member.CreatedByUserId = 1;
        member.UpdatedByUserId = 2;

        // Assert
        member.CreatedAt.Should().Be(now);
        member.UpdatedAt.Should().Be(now.AddHours(1));
        member.CreatedByUserId.Should().Be(1);
        member.UpdatedByUserId.Should().Be(2);
    }

    [Fact]
    public void LoyaltyMember_FullEnrollment_ShouldSetAllRequiredFields()
    {
        // Arrange & Act
        var member = new LoyaltyMember
        {
            PhoneNumber = "254712345678",
            Name = "Jane Smith",
            Email = "jane@example.com",
            MembershipNumber = "LM-20260102-00001",
            Tier = MembershipTier.Bronze,
            PointsBalance = 0,
            LifetimePoints = 0,
            LifetimeSpend = 0,
            EnrolledAt = DateTime.UtcNow,
            VisitCount = 0
        };

        // Assert
        member.PhoneNumber.Should().NotBeEmpty();
        member.MembershipNumber.Should().StartWith("LM-");
        member.Tier.Should().Be(MembershipTier.Bronze);
        member.PointsBalance.Should().Be(0);
        member.IsActive.Should().BeTrue();
    }
}
