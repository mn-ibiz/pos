using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the LoyaltyService class.
/// </summary>
public class LoyaltyServiceTests
{
    private readonly Mock<ILoyaltyMemberRepository> _memberRepositoryMock;
    private readonly Mock<IRepository<LoyaltyTransaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<PointsConfiguration>> _pointsConfigRepositoryMock;
    private readonly Mock<IRepository<TierConfiguration>> _tierConfigRepositoryMock;
    private readonly Mock<ISmsService> _smsServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LoyaltyService>> _loggerMock;
    private readonly LoyaltyService _service;

    public LoyaltyServiceTests()
    {
        _memberRepositoryMock = new Mock<ILoyaltyMemberRepository>();
        _transactionRepositoryMock = new Mock<IRepository<LoyaltyTransaction>>();
        _pointsConfigRepositoryMock = new Mock<IRepository<PointsConfiguration>>();
        _tierConfigRepositoryMock = new Mock<IRepository<TierConfiguration>>();
        _smsServiceMock = new Mock<ISmsService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LoyaltyService>>();

        _service = new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMemberRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            null!,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("memberRepository");
    }

    [Fact]
    public void Constructor_WithNullTransactionRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            null!,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("transactionRepository");
    }

    [Fact]
    public void Constructor_WithNullPointsConfigRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            null!,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("pointsConfigRepository");
    }

    [Fact]
    public void Constructor_WithNullTierConfigRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            null!,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("tierConfigRepository");
    }

    [Fact]
    public void Constructor_WithNullSmsService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            null!,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("smsService");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new LoyaltyService(
            _memberRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _pointsConfigRepositoryMock.Object,
            _tierConfigRepositoryMock.Object,
            _smsServiceMock.Object,
            _unitOfWorkMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Phone Number Normalization Tests

    [Theory]
    [InlineData("0712345678", "254712345678")]
    [InlineData("712345678", "254712345678")]
    [InlineData("254712345678", "254712345678")]
    [InlineData("+254712345678", "254712345678")]
    [InlineData("0 712 345 678", "254712345678")]
    [InlineData("0110123456", "254110123456")]   // Telkom Kenya
    [InlineData("110123456", "254110123456")]    // Telkom Kenya without leading 0
    [InlineData("254110123456", "254110123456")] // Telkom Kenya with country code
    public void NormalizePhoneNumber_WithValidFormats_ShouldReturnNormalizedNumber(
        string input,
        string expected)
    {
        // Act
        var result = _service.NormalizePhoneNumber(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("0812345678")] // Invalid prefix
    public void NormalizePhoneNumber_WithInvalidFormats_ShouldReturnNull(string? input)
    {
        // Act
        var result = _service.NormalizePhoneNumber(input!);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Phone Number Validation Tests

    [Theory]
    [InlineData("0712345678", true)]
    [InlineData("254712345678", true)]
    [InlineData("+254712345678", true)]
    [InlineData("712345678", true)]
    [InlineData("0110123456", true)]    // Telkom Kenya
    [InlineData("254110123456", true)]  // Telkom Kenya with country code
    public void ValidatePhoneNumber_WithValidKenyaNumbers_ShouldReturnTrue(
        string phoneNumber,
        bool expected)
    {
        // Act
        var result = _service.ValidatePhoneNumber(phoneNumber);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("0812345678")] // Invalid prefix
    [InlineData("254812345678")] // Invalid prefix
    public void ValidatePhoneNumber_WithInvalidNumbers_ShouldReturnFalse(string phoneNumber)
    {
        // Act
        var result = _service.ValidatePhoneNumber(phoneNumber);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Enrollment Tests

    [Fact]
    public async Task EnrollCustomerAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => _service.EnrollCustomerAsync(null!, 1);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("dto");
    }

    [Fact]
    public async Task EnrollCustomerAsync_WithEmptyPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var dto = new EnrollCustomerDto { PhoneNumber = "" };

        // Act
        var result = await _service.EnrollCustomerAsync(dto, 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PHONE_REQUIRED");
    }

    [Fact]
    public async Task EnrollCustomerAsync_WithInvalidPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var dto = new EnrollCustomerDto { PhoneNumber = "123" };

        // Act
        var result = await _service.EnrollCustomerAsync(dto, 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PHONE");
    }

    [Fact]
    public async Task EnrollCustomerAsync_WithDuplicatePhone_ShouldReturnDuplicate()
    {
        // Arrange
        var dto = new EnrollCustomerDto { PhoneNumber = "0712345678" };
        var existingMember = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            MembershipNumber = "LM-20250101-00001",
            Tier = MembershipTier.Bronze
        };

        _memberRepositoryMock
            .Setup(r => r.GetByPhoneAsync("254712345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMember);

        // Act
        var result = await _service.EnrollCustomerAsync(dto, 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsDuplicate.Should().BeTrue();
        result.Member.Should().NotBeNull();
        result.Member!.MembershipNumber.Should().Be("LM-20250101-00001");
    }

    [Fact]
    public async Task EnrollCustomerAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var dto = new EnrollCustomerDto
        {
            PhoneNumber = "0712345678",
            Name = "John Doe",
            Email = "john@example.com"
        };

        _memberRepositoryMock
            .Setup(r => r.GetByPhoneAsync("254712345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        _memberRepositoryMock
            .Setup(r => r.GetNextSequenceNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _memberRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _smsServiceMock
            .Setup(s => s.SendWelcomeSmsAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmsResult.Success("MSG-123"));

        // Act
        var result = await _service.EnrollCustomerAsync(dto, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsDuplicate.Should().BeFalse();
        result.Member.Should().NotBeNull();
        result.Member!.PhoneNumber.Should().Be("254712345678");
        result.Member.Name.Should().Be("John Doe");

        _memberRepositoryMock.Verify(
            r => r.AddAsync(It.Is<LoyaltyMember>(m =>
                m.PhoneNumber == "254712345678" &&
                m.Name == "John Doe" &&
                m.Tier == MembershipTier.Bronze),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Lookup Tests

    [Fact]
    public async Task GetByPhoneAsync_WithValidPhone_ShouldReturnMember()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            MembershipNumber = "LM-20250101-00001",
            Tier = MembershipTier.Silver,
            PointsBalance = 500
        };

        _memberRepositoryMock
            .Setup(r => r.GetByPhoneAsync("254712345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.GetByPhoneAsync("0712345678");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.PointsBalance.Should().Be(500);
        result.Tier.Should().Be(MembershipTier.Silver);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithNonExistentPhone_ShouldReturnNull()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.GetByPhoneAsync("0712345678");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByPhoneAsync_WithExistingPhone_ShouldReturnTrue()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.ExistsByPhoneAsync("254712345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsByPhoneAsync("0712345678");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Membership Number Generation Tests

    [Fact]
    public async Task GenerateMembershipNumberAsync_ShouldReturnProperlyFormattedNumber()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetNextSequenceNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _service.GenerateMembershipNumberAsync();

        // Assert
        result.Should().StartWith("LM-");
        result.Should().EndWith("-00042");
        result.Should().MatchRegex(@"^LM-\d{8}-\d{5}$");
    }

    #endregion

    #region Member Update Tests

    [Fact]
    public async Task UpdateMemberAsync_WithValidData_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            MembershipNumber = "LM-20250101-00001",
            Name = "Old Name"
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateMemberAsync(1, "New Name", "new@example.com", 2);

        // Assert
        result.Should().BeTrue();
        member.Name.Should().Be("New Name");
        member.Email.Should().Be("new@example.com");
        member.UpdatedByUserId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateMemberAsync_WithNonExistentMember_ShouldReturnFalse()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.UpdateMemberAsync(999, "Name", null, 1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Member Activation/Deactivation Tests

    [Fact]
    public async Task DeactivateMemberAsync_WithValidMember_ShouldDeactivateAndReturnTrue()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            IsActive = true
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeactivateMemberAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateMemberAsync_WithValidMember_ShouldReactivateAndReturnTrue()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            IsActive = false
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ReactivateMemberAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        member.IsActive.Should().BeTrue();
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public void MapToDto_WithValidMember_ShouldMapAllProperties()
    {
        // Arrange
        var enrolledAt = DateTime.UtcNow.AddDays(-30);
        var lastVisit = DateTime.UtcNow.AddDays(-1);

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            Name = "Jane Doe",
            Email = "jane@example.com",
            MembershipNumber = "LM-20250101-00001",
            Tier = MembershipTier.Gold,
            PointsBalance = 1500,
            LifetimePoints = 5000,
            LifetimeSpend = 50000,
            EnrolledAt = enrolledAt,
            LastVisit = lastVisit,
            VisitCount = 25,
            IsActive = true
        };

        // Act
        var dto = _service.MapToDto(member);

        // Assert
        dto.Id.Should().Be(1);
        dto.PhoneNumber.Should().Be("254712345678");
        dto.Name.Should().Be("Jane Doe");
        dto.Email.Should().Be("jane@example.com");
        dto.MembershipNumber.Should().Be("LM-20250101-00001");
        dto.Tier.Should().Be(MembershipTier.Gold);
        dto.PointsBalance.Should().Be(1500);
        dto.LifetimePoints.Should().Be(5000);
        dto.LifetimeSpend.Should().Be(50000);
        dto.EnrolledAt.Should().Be(enrolledAt);
        dto.LastVisit.Should().Be(lastVisit);
        dto.VisitCount.Should().Be(25);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MapToDto_WithNullMember_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => _service.MapToDto(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Points Calculation Tests

    [Fact]
    public async Task CalculatePointsAsync_WithDefaultConfig_ShouldCalculateCorrectPoints()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            IsDefault = true,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        // Act - Transaction of KSh 1,000 should earn 10 points
        var result = await _service.CalculatePointsAsync(1000m, 0, 0, null);

        // Assert
        result.EligibleAmount.Should().Be(1000m);
        result.BasePoints.Should().Be(10m);
        result.TotalPoints.Should().Be(10m);
        result.EarningRate.Should().Be(100m);
    }

    [Fact]
    public async Task CalculatePointsAsync_NotEarningOnTax_ShouldExcludeTax()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            IsDefault = true,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        // Act - Transaction of KSh 1,160 with KSh 160 tax
        var result = await _service.CalculatePointsAsync(1160m, 0, 160m, null);

        // Assert
        result.EligibleAmount.Should().Be(1000m); // 1160 - 160 tax
        result.BasePoints.Should().Be(10m);
    }

    [Fact]
    public async Task CalculatePointsAsync_NotEarningOnDiscounts_ShouldExcludeDiscount()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = false,
            EarnOnTax = true,
            IsDefault = true,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        // Act - Transaction of KSh 1,000 with KSh 200 discount
        var result = await _service.CalculatePointsAsync(1000m, 200m, 0, null);

        // Assert
        result.EligibleAmount.Should().Be(800m); // 1000 - 200 discount
        result.BasePoints.Should().Be(8m);
    }

    [Fact]
    public async Task CalculatePointsAsync_WithNoConfig_ShouldUseDefaults()
    {
        // Arrange - No configuration found
        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration>());

        // Act
        var result = await _service.CalculatePointsAsync(1000m, 0, 0, null);

        // Assert - Default is 100 KSh per point
        result.BasePoints.Should().Be(10m);
        result.EarningRate.Should().Be(100m);
    }

    [Fact]
    public async Task CalculatePointsAsync_WithSilverTier_ShouldApplyBonusMultiplier()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Silver // 25% bonus
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculatePointsAsync(1000m, 0, 0, 1);

        // Assert
        result.BasePoints.Should().Be(10m);
        result.BonusMultiplier.Should().Be(1.25m);
        result.BonusPoints.Should().Be(2m); // Floor(10 * 1.25) - 10 = 2
        result.TotalPoints.Should().Be(12m);
    }

    [Fact]
    public async Task CalculatePointsAsync_WithPlatinumTier_ShouldDoublePoints()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Platinum // 100% bonus (double)
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculatePointsAsync(1000m, 0, 0, 1);

        // Assert
        result.BasePoints.Should().Be(10m);
        result.BonusMultiplier.Should().Be(2.0m);
        result.BonusPoints.Should().Be(10m); // Floor(10 * 2) - 10 = 10
        result.TotalPoints.Should().Be(20m);
    }

    #endregion

    #region Points Award Tests

    [Fact]
    public async Task AwardPointsAsync_WithValidMember_ShouldAwardPointsAndReturnSuccess()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            EarnOnDiscountedItems = true,
            EarnOnTax = false,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            MembershipNumber = "LM-20250101-00001",
            Tier = MembershipTier.Bronze,
            PointsBalance = 100m,
            LifetimePoints = 500m,
            LifetimeSpend = 50000m,
            VisitCount = 10,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyTransaction t, CancellationToken _) => { t.Id = 1; return t; });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AwardPointsAsync(
            memberId: 1,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            discountAmount: 0,
            taxAmount: 0,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PointsEarned.Should().Be(10m);
        result.PreviousBalance.Should().Be(100m);
        result.NewBalance.Should().Be(110m);

        // Verify member was updated
        member.PointsBalance.Should().Be(110m);
        member.LifetimePoints.Should().Be(510m);
        member.LifetimeSpend.Should().Be(51000m);
        member.VisitCount.Should().Be(11);
    }

    [Fact]
    public async Task AwardPointsAsync_WithNonExistentMember_ShouldReturnFailure()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.AwardPointsAsync(
            memberId: 999,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            discountAmount: 0,
            taxAmount: 0,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task AwardPointsAsync_WithInactiveMember_ShouldReturnFailure()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            IsActive = false
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.AwardPointsAsync(
            memberId: 1,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            discountAmount: 0,
            taxAmount: 0,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task AwardPointsAsync_WithSmallTransaction_ShouldEarnZeroPoints()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            EarningRate = 100m,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            Tier = MembershipTier.Bronze,
            PointsBalance = 100m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Transaction of 50 KSh is less than 100 KSh needed for 1 point
        var result = await _service.AwardPointsAsync(
            memberId: 1,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 50m,
            discountAmount: 0,
            taxAmount: 0,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PointsEarned.Should().Be(0);
        result.NewBalance.Should().Be(100m); // Unchanged
    }

    #endregion

    #region Tier Bonus Multiplier Tests

    [Theory]
    [InlineData(MembershipTier.Bronze, 1.0)]
    [InlineData(MembershipTier.Silver, 1.25)]
    [InlineData(MembershipTier.Gold, 1.5)]
    [InlineData(MembershipTier.Platinum, 2.0)]
    public async Task GetTierBonusMultiplierAsync_WithDifferentTiers_ShouldReturnCorrectMultiplier(
        MembershipTier tier,
        double expectedMultiplier)
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = tier
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.GetTierBonusMultiplierAsync(1);

        // Assert
        result.Should().Be((decimal)expectedMultiplier);
    }

    [Fact]
    public async Task GetTierBonusMultiplierAsync_WithNonExistentMember_ShouldReturnDefaultMultiplier()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.GetTierBonusMultiplierAsync(999);

        // Assert
        result.Should().Be(1.0m);
    }

    #endregion

    #region Points Redemption Preview Tests

    [Fact]
    public async Task CalculateRedemptionAsync_WithValidMember_ShouldReturnSuccessfulPreview()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m, // 1 point = KES 1
            MinimumRedemptionPoints = 100,
            MaximumRedemptionPoints = 0, // Unlimited
            MaxRedemptionPercentage = 50,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 500m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Transaction of KES 1000, max 50% = 500 KES = 500 points
        var result = await _service.CalculateRedemptionAsync(1, 1000m);

        // Assert
        result.CanRedeem.Should().BeTrue();
        result.AvailablePoints.Should().Be(500m);
        result.AvailableValue.Should().Be(500m);
        result.MinimumRedemptionPoints.Should().Be(100);
        result.MaxRedeemablePoints.Should().Be(500m);
        result.MaxRedeemableValue.Should().Be(500m);
        result.RedemptionRate.Should().Be(1m);
    }

    [Fact]
    public async Task CalculateRedemptionAsync_WithInsufficientPoints_ShouldReturnFailure()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 50m, // Less than minimum 100
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculateRedemptionAsync(1, 1000m);

        // Assert
        result.CanRedeem.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient points");
        result.AvailablePoints.Should().Be(50m);
    }

    [Fact]
    public async Task CalculateRedemptionAsync_WithNonExistentMember_ShouldReturnFailure()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.CalculateRedemptionAsync(999, 1000m);

        // Assert
        result.CanRedeem.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task CalculateRedemptionAsync_WithInactiveMember_ShouldReturnFailure()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 500m,
            IsActive = false
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculateRedemptionAsync(1, 1000m);

        // Assert
        result.CanRedeem.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task CalculateRedemptionAsync_WithMaxPercentageLimit_ShouldRespectLimit()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            MaximumRedemptionPoints = 0, // Unlimited
            MaxRedemptionPercentage = 30, // 30% max
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 1000m, // More than enough
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Transaction of KES 1000, max 30% = 300 KES = 300 points
        var result = await _service.CalculateRedemptionAsync(1, 1000m);

        // Assert
        result.CanRedeem.Should().BeTrue();
        result.MaxRedeemablePoints.Should().Be(300m);
        result.MaxRedeemableValue.Should().Be(300m);
    }

    [Fact]
    public async Task CalculateRedemptionAsync_WithMaxPointsLimit_ShouldRespectLimit()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            MaximumRedemptionPoints = 200, // Max 200 points per transaction
            MaxRedemptionPercentage = 50,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 1000m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Transaction of KES 1000, 50% = 500 points, but max is 200
        var result = await _service.CalculateRedemptionAsync(1, 1000m);

        // Assert
        result.CanRedeem.Should().BeTrue();
        result.MaxRedeemablePoints.Should().Be(200m);
        result.MaxRedeemableValue.Should().Be(200m);
    }

    #endregion

    #region Points Redemption Tests

    [Fact]
    public async Task RedeemPointsAsync_WithValidRequest_ShouldRedeemAndReturnSuccess()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            MaximumRedemptionPoints = 0,
            MaxRedemptionPercentage = 50,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 500m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyTransaction t, CancellationToken _) => { t.Id = 1; return t; });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act - Redeem 200 points for KES 200
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 200m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PointsRedeemed.Should().Be(200m);
        result.ValueRedeemed.Should().Be(200m);
        result.PreviousBalance.Should().Be(500m);
        result.NewBalance.Should().Be(300m);

        // Verify member balance was updated
        member.PointsBalance.Should().Be(300m);

        // Verify transaction was created with negative points
        _transactionRepositoryMock.Verify(
            r => r.AddAsync(It.Is<LoyaltyTransaction>(t =>
                t.Points == -200m &&
                t.TransactionType == LoyaltyTransactionType.Redeemed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RedeemPointsAsync_WithZeroPoints_ShouldReturnFailure()
    {
        // Act
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 0m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task RedeemPointsAsync_WithNonExistentMember_ShouldReturnFailure()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.RedeemPointsAsync(
            memberId: 999,
            pointsToRedeem: 100m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RedeemPointsAsync_WithInactiveMember_ShouldReturnFailure()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 500m,
            IsActive = false
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 100m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task RedeemPointsAsync_WithBelowMinimumPoints_ShouldReturnFailure()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 500m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Try to redeem 50 points (below minimum 100)
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 50m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Minimum 100 points");
    }

    [Fact]
    public async Task RedeemPointsAsync_WithInsufficientBalance_ShouldReturnFailure()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 150m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Try to redeem 200 points (more than available 150)
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 200m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient points");
    }

    [Fact]
    public async Task RedeemPointsAsync_ExceedingPercentageLimit_ShouldReturnFailure()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1m,
            MinimumRedemptionPoints = 100,
            MaxRedemptionPercentage = 50,
            IsDefault = true,
            IsActive = true
        };

        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            PointsBalance = 1000m,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act - Try to redeem 600 points on 1000 KES transaction (60% > 50% max)
        var result = await _service.RedeemPointsAsync(
            memberId: 1,
            pointsToRedeem: 600m,
            receiptId: 123,
            receiptNumber: "R-20250101-00001",
            transactionAmount: 1000m,
            processedByUserId: 2);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds 50%");
    }

    #endregion

    #region Points Conversion Tests

    [Fact]
    public async Task ConvertPointsToValueAsync_ShouldConvertCorrectly()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1.5m, // 1 point = KES 1.50
            IsDefault = true,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        // Act
        var result = await _service.ConvertPointsToValueAsync(100m);

        // Assert
        result.Should().Be(150m); // 100 points * 1.5 = 150 KES
    }

    [Fact]
    public async Task ConvertValueToPointsAsync_ShouldConvertCorrectly()
    {
        // Arrange
        var config = new PointsConfiguration
        {
            RedemptionValue = 1.5m, // 1 point = KES 1.50
            IsDefault = true,
            IsActive = true
        };

        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration> { config });

        // Act
        var result = await _service.ConvertValueToPointsAsync(150m);

        // Assert
        result.Should().Be(100m); // 150 KES / 1.5 = 100 points
    }

    [Fact]
    public async Task ConvertPointsToValueAsync_WithNoConfig_ShouldUseDefaultRate()
    {
        // Arrange
        _pointsConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PointsConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PointsConfiguration>());

        // Act
        var result = await _service.ConvertPointsToValueAsync(100m);

        // Assert - Default is 1 point = 1 KES
        result.Should().Be(100m);
    }

    #endregion

    #region Tier Configuration Tests

    [Fact]
    public async Task GetTierConfigurationsAsync_ShouldReturnAllActiveTiers()
    {
        // Arrange
        var tierConfigs = new List<TierConfiguration>
        {
            new() { Tier = MembershipTier.Bronze, Name = "Bronze", SpendThreshold = 0, PointsMultiplier = 1.0m, SortOrder = 1, IsActive = true },
            new() { Tier = MembershipTier.Silver, Name = "Silver", SpendThreshold = 25000, PointsMultiplier = 1.25m, SortOrder = 2, IsActive = true },
            new() { Tier = MembershipTier.Gold, Name = "Gold", SpendThreshold = 75000, PointsMultiplier = 1.5m, SortOrder = 3, IsActive = true },
            new() { Tier = MembershipTier.Platinum, Name = "Platinum", SpendThreshold = 150000, PointsMultiplier = 2.0m, SortOrder = 4, IsActive = true }
        };

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act
        var result = await _service.GetTierConfigurationsAsync();

        // Assert
        var configs = result.ToList();
        configs.Should().HaveCount(4);
        configs.Should().BeInAscendingOrder(c => c.SpendThreshold);
    }

    [Fact]
    public async Task GetTierConfigurationAsync_WithValidTier_ShouldReturnConfig()
    {
        // Arrange
        var silverConfig = new TierConfiguration
        {
            Tier = MembershipTier.Silver,
            Name = "Silver",
            SpendThreshold = 25000,
            PointsThreshold = 250,
            PointsMultiplier = 1.25m,
            DiscountPercent = 5m,
            IsActive = true
        };

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TierConfiguration> { silverConfig });

        // Act
        var result = await _service.GetTierConfigurationAsync(MembershipTier.Silver);

        // Assert
        result.Should().NotBeNull();
        result!.Tier.Should().Be(MembershipTier.Silver);
        result.Name.Should().Be("Silver");
        result.PointsMultiplier.Should().Be(1.25m);
        result.DiscountPercent.Should().Be(5m);
    }

    [Fact]
    public async Task GetTierConfigurationAsync_WithNoConfig_ShouldReturnNull()
    {
        // Arrange
        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TierConfiguration>());

        // Act
        var result = await _service.GetTierConfigurationAsync(MembershipTier.Silver);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Tier Evaluation Tests

    [Fact]
    public async Task EvaluateMemberTierAsync_WithNonExistentMember_ShouldReturnNull()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.EvaluateMemberTierAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateMemberTierAsync_WithBronzeMemberBelowSilverThreshold_ShouldShowNoUpgrade()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Bronze,
            LifetimeSpend = 10000m,
            LifetimePoints = 100m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act
        var result = await _service.EvaluateMemberTierAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PreviousTier.Should().Be(MembershipTier.Bronze);
        result.NewTier.Should().Be(MembershipTier.Bronze);
        result.TierChanged.Should().BeFalse();
        result.NextTier.Should().Be(MembershipTier.Silver);
        result.AmountToNextTier.Should().Be(15000m); // 25000 - 10000 = 15000
    }

    [Fact]
    public async Task EvaluateMemberTierAsync_WithBronzeMemberAboveSilverThreshold_ShouldQualifyForSilver()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Bronze,
            LifetimeSpend = 30000m,
            LifetimePoints = 300m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act
        var result = await _service.EvaluateMemberTierAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PreviousTier.Should().Be(MembershipTier.Bronze);
        result.NewTier.Should().Be(MembershipTier.Silver);
        result.TierChanged.Should().BeTrue();
        result.IsUpgrade.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAndUpgradeTierAsync_WithQualifyingMember_ShouldUpgradeTier()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Bronze,
            LifetimeSpend = 80000m,
            LifetimePoints = 800m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CheckAndUpgradeTierAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PreviousTier.Should().Be(MembershipTier.Bronze);
        result.NewTier.Should().Be(MembershipTier.Gold);
        result.TierChanged.Should().BeTrue();
        result.IsUpgrade.Should().BeTrue();

        // Verify member was updated
        member.Tier.Should().Be(MembershipTier.Gold);
        _memberRepositoryMock.Verify(
            r => r.UpdateAsync(It.Is<LoyaltyMember>(m => m.Tier == MembershipTier.Gold), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndUpgradeTierAsync_WithNonQualifyingMember_ShouldNotChangeTier()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Silver,
            LifetimeSpend = 50000m,
            LifetimePoints = 500m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act
        var result = await _service.CheckAndUpgradeTierAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.TierChanged.Should().BeFalse();
        member.Tier.Should().Be(MembershipTier.Silver);

        // Verify member was NOT updated
        _memberRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ManualTierUpgradeAsync_WithValidMember_ShouldUpgradeTier()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Bronze,
            IsActive = true
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ManualTierUpgradeAsync(
            memberId: 1,
            newTier: MembershipTier.Gold,
            reason: "VIP Customer promotion",
            upgradedByUserId: 2);

        // Assert
        result.Should().BeTrue();
        member.Tier.Should().Be(MembershipTier.Gold);
    }

    [Fact]
    public async Task ManualTierUpgradeAsync_WithNonExistentMember_ShouldReturnFalse()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.ManualTierUpgradeAsync(
            memberId: 999,
            newTier: MembershipTier.Gold,
            reason: "Test",
            upgradedByUserId: 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTierProgressAsync_WithValidMember_ShouldShowProgress()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Silver,
            LifetimeSpend = 50000m,
            LifetimePoints = 500m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act
        var result = await _service.GetTierProgressAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.NextTier.Should().Be(MembershipTier.Gold);
        result.AmountToNextTier.Should().Be(25000m); // 75000 - 50000 = 25000
        // Progress: 50000 - 25000 = 25000 out of 75000 - 25000 = 50000
        result.NextTierProgress.Should().Be(50m);
    }

    [Fact]
    public async Task PerformAnnualTierReviewAsync_WithMemberBelowThreshold_ShouldDowngradeTier()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Gold,
            LifetimeSpend = 100000m,
            LifetimePoints = 1000m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _memberRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<LoyaltyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act - Period spend/points below Gold threshold
        var result = await _service.PerformAnnualTierReviewAsync(
            memberId: 1,
            periodSpend: 20000m, // Below 75000 threshold
            periodPoints: 200m);

        // Assert
        result.Should().NotBeNull();
        result!.PreviousTier.Should().Be(MembershipTier.Gold);
        result.NewTier.Should().Be(MembershipTier.Bronze);
        result.IsDowngrade.Should().BeTrue();
    }

    [Fact]
    public async Task PerformAnnualTierReviewAsync_WithMemberAboveThreshold_ShouldMaintainTier()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Gold,
            LifetimeSpend = 100000m,
            LifetimePoints = 1000m,
            IsActive = true
        };

        var tierConfigs = CreateDefaultTierConfigs();

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tierConfigs);

        // Act - Period spend/points above Gold threshold
        var result = await _service.PerformAnnualTierReviewAsync(
            memberId: 1,
            periodSpend: 80000m, // Above 75000 threshold
            periodPoints: 800m);

        // Assert
        result.Should().NotBeNull();
        result!.TierChanged.Should().BeFalse();
        result.PreviousTier.Should().Be(MembershipTier.Gold);
        result.NewTier.Should().Be(MembershipTier.Gold);
    }

    #endregion

    #region Customer Analytics Tests

    [Fact]
    public async Task GetCustomerAnalyticsAsync_WithValidMember_ShouldReturnAnalytics()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            PhoneNumber = "254712345678",
            Name = "Test Customer",
            MembershipNumber = "LM-20250101-00001",
            Tier = MembershipTier.Silver,
            PointsBalance = 500,
            LifetimePoints = 750,
            LifetimeSpend = 50000m,
            VisitCount = 20,
            EnrolledAt = DateTime.UtcNow.AddDays(-90),
            LastVisit = DateTime.UtcNow.AddDays(-5),
            IsActive = true
        };

        var silverConfig = new TierConfiguration
        {
            Tier = MembershipTier.Silver,
            Name = "Silver",
            PointsMultiplier = 1.25m,
            IsActive = true
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _tierConfigRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TierConfiguration, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TierConfiguration> { silverConfig });

        // Act
        var result = await _service.GetCustomerAnalyticsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.MemberId.Should().Be(1);
        result.TotalSpend.Should().Be(50000m);
        result.VisitCount.Should().Be(20);
        result.AverageBasket.Should().Be(2500m); // 50000 / 20
        result.Tier.Should().Be(MembershipTier.Silver);
        result.PointsBalance.Should().Be(500);
        result.LifetimePoints.Should().Be(750);
        result.EngagementScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCustomerAnalyticsAsync_WithNonExistentMember_ShouldReturnNull()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.GetCustomerAnalyticsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CalculateEngagementScoreAsync_WithRecentActiveCustomer_ShouldReturnHighScore()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Gold,
            PointsBalance = 1000,
            LifetimeSpend = 100000m,
            VisitCount = 50,
            EnrolledAt = DateTime.UtcNow.AddDays(-180),
            LastVisit = DateTime.UtcNow.AddDays(-3), // Very recent
            IsActive = true
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculateEngagementScoreAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeGreaterOrEqualTo(70); // Should be "Champion" level
    }

    [Fact]
    public async Task CalculateEngagementScoreAsync_WithInactiveCustomer_ShouldReturnLowScore()
    {
        // Arrange
        var member = new LoyaltyMember
        {
            Id = 1,
            Tier = MembershipTier.Bronze,
            PointsBalance = 10,
            LifetimeSpend = 1000m,
            VisitCount = 2,
            EnrolledAt = DateTime.UtcNow.AddDays(-365),
            LastVisit = DateTime.UtcNow.AddDays(-100), // Very old
            IsActive = true
        };

        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        var result = await _service.CalculateEngagementScoreAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeLessThanOrEqualTo(30); // Should be "At Risk" or "Dormant" level
    }

    [Fact]
    public async Task CalculateEngagementScoreAsync_WithNonExistentMember_ShouldReturnNull()
    {
        // Arrange
        _memberRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyMember?)null);

        // Act
        var result = await _service.CalculateEngagementScoreAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportCustomerDataAsync_WithValidFilters_ShouldReturnCsvExport()
    {
        // Arrange
        var members = new List<LoyaltyMember>
        {
            new() { Id = 1, MembershipNumber = "LM-001", Name = "Customer 1", PhoneNumber = "254711111111", Tier = MembershipTier.Bronze, IsActive = true, EnrolledAt = DateTime.UtcNow.AddDays(-30) },
            new() { Id = 2, MembershipNumber = "LM-002", Name = "Customer 2", PhoneNumber = "254722222222", Tier = MembershipTier.Silver, IsActive = true, EnrolledAt = DateTime.UtcNow.AddDays(-60) }
        };

        _memberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LoyaltyMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var filter = new CustomerExportFilterDto { IncludeInactive = false };

        // Act
        var result = await _service.ExportCustomerDataAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RecordCount.Should().Be(2);
        result.FileContent.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().Contain("loyalty_customers");
    }

    [Fact]
    public async Task ExportCustomerDataAsync_WithTierFilter_ShouldFilterByTier()
    {
        // Arrange
        var members = new List<LoyaltyMember>
        {
            new() { Id = 1, MembershipNumber = "LM-001", Tier = MembershipTier.Bronze, IsActive = true, EnrolledAt = DateTime.UtcNow },
            new() { Id = 2, MembershipNumber = "LM-002", Tier = MembershipTier.Gold, IsActive = true, EnrolledAt = DateTime.UtcNow },
            new() { Id = 3, MembershipNumber = "LM-003", Tier = MembershipTier.Gold, IsActive = true, EnrolledAt = DateTime.UtcNow }
        };

        _memberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LoyaltyMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var filter = new CustomerExportFilterDto { Tier = MembershipTier.Gold };

        // Act
        var result = await _service.ExportCustomerDataAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RecordCount.Should().Be(2); // Only Gold members
    }

    [Fact]
    public async Task ExportCustomerDataAsync_WithNoMatchingFilters_ShouldReturnFailure()
    {
        // Arrange
        var members = new List<LoyaltyMember>
        {
            new() { Id = 1, MembershipNumber = "LM-001", Tier = MembershipTier.Bronze, LifetimeSpend = 1000, IsActive = true, EnrolledAt = DateTime.UtcNow }
        };

        _memberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LoyaltyMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var filter = new CustomerExportFilterDto { MinSpend = 1000000 }; // Very high threshold

        // Act
        var result = await _service.ExportCustomerDataAsync(filter);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No customers match");
    }

    [Fact]
    public async Task GetTopCategoriesAsync_ShouldReturnEmptyList()
    {
        // Arrange - This is currently a placeholder implementation

        // Act
        var result = await _service.GetTopCategoriesAsync(1, 5);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Transaction History Tests

    [Fact]
    public async Task GetTransactionHistoryAsync_WithValidMember_ShouldReturnTransactions()
    {
        // Arrange
        var transactions = new List<LoyaltyTransaction>
        {
            new() { Id = 1, LoyaltyMemberId = 1, TransactionType = LoyaltyTransactionType.Earned, Points = 10, TransactionDate = DateTime.UtcNow.AddDays(-1), IsActive = true },
            new() { Id = 2, LoyaltyMemberId = 1, TransactionType = LoyaltyTransactionType.Redeemed, Points = -5, TransactionDate = DateTime.UtcNow, IsActive = true }
        }.AsQueryable();

        _transactionRepositoryMock
            .Setup(r => r.QueryNoTracking())
            .Returns(transactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(1, maxResults: 10);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Points.Should().Be(-5); // Most recent first
        resultList[1].Points.Should().Be(10);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithDateFilters_ShouldFilterCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var transactions = new List<LoyaltyTransaction>
        {
            new() { Id = 1, LoyaltyMemberId = 1, TransactionDate = DateTime.UtcNow.AddDays(-10), IsActive = true }, // Before range
            new() { Id = 2, LoyaltyMemberId = 1, TransactionDate = DateTime.UtcNow.AddDays(-5), IsActive = true },  // In range
            new() { Id = 3, LoyaltyMemberId = 1, TransactionDate = DateTime.UtcNow, IsActive = true }               // After range
        }.AsQueryable();

        _transactionRepositoryMock
            .Setup(r => r.QueryNoTracking())
            .Returns(transactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(1, startDate, endDate);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(1);
        resultList[0].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithMaxResultsLimit_ShouldRespectLimit()
    {
        // Arrange
        var transactions = Enumerable.Range(1, 20)
            .Select(i => new LoyaltyTransaction
            {
                Id = i,
                LoyaltyMemberId = 1,
                TransactionDate = DateTime.UtcNow.AddHours(-i),
                IsActive = true
            }).AsQueryable();

        _transactionRepositoryMock
            .Setup(r => r.QueryNoTracking())
            .Returns(transactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(1, maxResults: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithNoTransactions_ShouldReturnEmptyList()
    {
        // Arrange
        var transactions = new List<LoyaltyTransaction>().AsQueryable();

        _transactionRepositoryMock
            .Setup(r => r.QueryNoTracking())
            .Returns(transactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ShouldExcludeInactiveTransactions()
    {
        // Arrange
        var transactions = new List<LoyaltyTransaction>
        {
            new() { Id = 1, LoyaltyMemberId = 1, TransactionDate = DateTime.UtcNow, IsActive = true },
            new() { Id = 2, LoyaltyMemberId = 1, TransactionDate = DateTime.UtcNow, IsActive = false } // Inactive
        }.AsQueryable();

        _transactionRepositoryMock
            .Setup(r => r.QueryNoTracking())
            .Returns(transactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(1);
    }

    #endregion

    #region Test Helpers

    private static List<TierConfiguration> CreateDefaultTierConfigs()
    {
        return new List<TierConfiguration>
        {
            new() { Tier = MembershipTier.Bronze, Name = "Bronze", SpendThreshold = 0, PointsThreshold = 0, PointsMultiplier = 1.0m, SortOrder = 1, IsActive = true },
            new() { Tier = MembershipTier.Silver, Name = "Silver", SpendThreshold = 25000, PointsThreshold = 250, PointsMultiplier = 1.25m, SortOrder = 2, IsActive = true },
            new() { Tier = MembershipTier.Gold, Name = "Gold", SpendThreshold = 75000, PointsThreshold = 750, PointsMultiplier = 1.5m, SortOrder = 3, IsActive = true },
            new() { Tier = MembershipTier.Platinum, Name = "Platinum", SpendThreshold = 150000, PointsThreshold = 1500, PointsMultiplier = 2.0m, SortOrder = 4, IsActive = true }
        };
    }

    #endregion
}
