using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.WPF.ViewModels;
using Xunit;

namespace HospitalityPOS.WPF.Tests.ViewModels;

/// <summary>
/// Unit tests for the ViewModelBase class.
/// </summary>
public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        public TestViewModel(ILogger logger) : base(logger)
        {
        }

        public new Task ExecuteAsync(Func<Task> operation, string busyMessage = "Please wait...")
            => base.ExecuteAsync(operation, busyMessage);

        public new Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string busyMessage = "Please wait...")
            => base.ExecuteAsync(operation, busyMessage);
    }

    private readonly Mock<ILogger> _loggerMock;
    private readonly TestViewModel _viewModel;

    public ViewModelBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _viewModel = new TestViewModel(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestViewModel(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void InitialState_ShouldHaveDefaultValues()
    {
        // Assert
        _viewModel.IsBusy.Should().BeFalse();
        _viewModel.BusyMessage.Should().BeEmpty();
        _viewModel.ErrorMessage.Should().BeNull();
        _viewModel.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetIsBusyDuringOperation()
    {
        // Arrange
        bool wasBusyDuringOperation = false;
        string busyMessageDuringOperation = string.Empty;

        // Act
        await _viewModel.ExecuteAsync(async () =>
        {
            wasBusyDuringOperation = _viewModel.IsBusy;
            busyMessageDuringOperation = _viewModel.BusyMessage;
            await Task.Delay(10);
        }, "Loading...");

        // Assert
        wasBusyDuringOperation.Should().BeTrue();
        busyMessageDuringOperation.Should().Be("Loading...");
        _viewModel.IsBusy.Should().BeFalse();
        _viewModel.BusyMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ShouldSetErrorMessage()
    {
        // Arrange
        const string errorMessage = "Test error occurred";

        // Act
        await _viewModel.ExecuteAsync(() => throw new InvalidOperationException(errorMessage));

        // Assert
        _viewModel.ErrorMessage.Should().Be(errorMessage);
        _viewModel.HasError.Should().BeTrue();
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ShouldReturnResult()
    {
        // Arrange & Act
        var result = await _viewModel.ExecuteAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
        _viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Generic_WithException_ShouldReturnDefault()
    {
        // Act
        var result = await _viewModel.ExecuteAsync<int>(() =>
            throw new InvalidOperationException("Error"));

        // Assert
        result.Should().Be(default);
        _viewModel.HasError.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyBusy_ShouldNotExecute()
    {
        // Arrange
        var executionCount = 0;
        var tcs = new TaskCompletionSource();

        // Start first operation but don't complete it
        var firstTask = _viewModel.ExecuteAsync(async () =>
        {
            executionCount++;
            await tcs.Task;
        });

        // Act - Try to start second operation while first is still running
        var secondTask = _viewModel.ExecuteAsync(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        });

        await secondTask;
        tcs.SetResult();
        await firstTask;

        // Assert
        executionCount.Should().Be(1);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        // Arrange
        _viewModel.GetType().GetProperty("ErrorMessage")!
            .SetValue(_viewModel, "Some error");

        // Act
        _viewModel.ClearErrorCommand.Execute(null);

        // Assert
        _viewModel.ErrorMessage.Should().BeNull();
        _viewModel.HasError.Should().BeFalse();
    }
}
