using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using Xunit;

namespace HospitalityPOS.Core.Tests.DTOs;

/// <summary>
/// Unit tests for the Result classes.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<string>.Success("test data");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("test data");
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = Result<string>.Failure("Something went wrong", "ERR001");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorMessage.Should().Be("Something went wrong");
        result.ErrorCode.Should().Be("ERR001");
    }

    [Fact]
    public void NonGenericSuccess_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void NonGenericFailure_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = Result.Failure("Error occurred");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error occurred");
    }

    [Fact]
    public void Failure_WithoutErrorCode_ShouldHaveNullErrorCode()
    {
        // Arrange & Act
        var result = Result<int>.Failure("Error message");

        // Assert
        result.ErrorCode.Should().BeNull();
    }
}
