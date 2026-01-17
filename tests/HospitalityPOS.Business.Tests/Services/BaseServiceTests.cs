using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Business.Services;
using HospitalityPOS.Core.Interfaces;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the BaseService class.
/// </summary>
public class BaseServiceTests
{
    private class TestService : BaseService
    {
        public TestService(IUnitOfWork unitOfWork, ILogger logger)
            : base(unitOfWork, logger)
        {
        }

        public Task<T> TestExecuteInTransactionAsync<T>(Func<Task<T>> operation)
            => ExecuteInTransactionAsync(operation);

        public Task TestExecuteInTransactionAsync(Func<Task> operation)
            => ExecuteInTransactionAsync(operation);
    }

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestService _service;

    public BaseServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger>();
        _service = new TestService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestService(null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestService(_unitOfWorkMock.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithSuccessfulOperation_ShouldCommitTransaction()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TestExecuteInTransactionAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithFailedOperation_ShouldRollbackTransaction()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var action = () => _service.TestExecuteInTransactionAsync<int>(() =>
            throw new InvalidOperationException("Test error"));

        await action.Should().ThrowAsync<InvalidOperationException>();

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_VoidVersion_ShouldCommitOnSuccess()
    {
        // Arrange
        var operationExecuted = false;
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.TestExecuteInTransactionAsync(() =>
        {
            operationExecuted = true;
            return Task.CompletedTask;
        });

        // Assert
        operationExecuted.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
