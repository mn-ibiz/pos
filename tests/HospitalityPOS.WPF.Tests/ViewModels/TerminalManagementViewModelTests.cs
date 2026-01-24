using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.ViewModels;
using Xunit;

namespace HospitalityPOS.WPF.Tests.ViewModels;

/// <summary>
/// Unit tests for TerminalManagementViewModel.
/// Tests cover terminal loading, filtering, CRUD operations, and navigation.
/// </summary>
public class TerminalManagementViewModelTests
{
    private readonly Mock<ITerminalService> _terminalServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TerminalManagementViewModel _viewModel;

    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public TerminalManagementViewModelTests()
    {
        _terminalServiceMock = new Mock<ITerminalService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger>();

        // Setup session service
        _sessionServiceMock.Setup(s => s.CurrentStoreId).Returns(TestStoreId);
        _sessionServiceMock.Setup(s => s.CurrentUser).Returns(new User { Id = TestUserId, Username = "test" });

        _viewModel = new TerminalManagementViewModel(
            _terminalServiceMock.Object,
            _sessionServiceMock.Object,
            _navigationServiceMock.Object,
            _dialogServiceMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeTerminalTypeOptions()
    {
        // Assert
        _viewModel.TerminalTypeOptions.Should().NotBeEmpty();
        _viewModel.TerminalTypeOptions.Should().Contain(o => o.DisplayName == "All Types");
        _viewModel.TerminalTypeOptions.Should().Contain(o => o.DisplayName == "Register");
        _viewModel.TerminalTypeOptions.Should().Contain(o => o.DisplayName == "Till");
        _viewModel.TerminalTypeOptions.Should().Contain(o => o.DisplayName == "Kitchen Display");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyTerminalsList()
    {
        // Assert
        _viewModel.Terminals.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Assert
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.ShowInactiveTerminals.Should().BeFalse();
        _viewModel.SearchText.Should().BeEmpty();
        _viewModel.SelectedTerminal.Should().BeNull();
    }

    #endregion

    #region OnNavigatedTo Tests

    [Fact]
    public async Task OnNavigatedTo_ShouldLoadTerminals()
    {
        // Arrange
        var terminals = new List<Terminal>
        {
            CreateTestTerminal(1, "REG-001"),
            CreateTestTerminal(2, "REG-002")
        };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        // Act
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        _terminalServiceMock.Verify(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task SearchText_WhenChanged_ShouldFilterTerminals()
    {
        // Arrange
        var terminals = new List<Terminal>
        {
            CreateTestTerminal(1, "REG-001", "Main Register"),
            CreateTestTerminal(2, "REG-002", "Secondary Register"),
            CreateTestTerminal(3, "TILL-001", "Till 1")
        };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        // Act
        _viewModel.SearchText = "Main";
        await Task.Delay(50);

        // Assert
        _viewModel.Terminals.Should().HaveCount(1);
        _viewModel.Terminals[0].Name.Should().Be("Main Register");
    }

    [Fact]
    public async Task ShowInactiveTerminals_WhenChanged_ShouldReloadTerminals()
    {
        // Arrange
        var terminals = new List<Terminal>
        {
            CreateTestTerminal(1, "REG-001", isActive: true),
            CreateTestTerminal(2, "REG-002", isActive: false)
        };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        // Initially should only show active
        _viewModel.Terminals.Should().HaveCount(1);

        // Act
        _viewModel.ShowInactiveTerminals = true;
        await Task.Delay(100);

        // Assert - Should reload and show both
        _terminalServiceMock.Verify(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FilterTerminalType_WhenChanged_ShouldFilterByType()
    {
        // Arrange
        var terminals = new List<Terminal>
        {
            CreateTestTerminal(1, "REG-001", terminalType: TerminalType.Register),
            CreateTestTerminal(2, "TILL-001", terminalType: TerminalType.Till),
            CreateTestTerminal(3, "KDS-001", terminalType: TerminalType.KitchenDisplay)
        };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        // Act
        _viewModel.FilterTerminalType = TerminalType.Register;
        await Task.Delay(50);

        // Assert
        _viewModel.Terminals.Should().HaveCount(1);
        _viewModel.Terminals[0].TerminalType.Should().Be(TerminalType.Register);
    }

    #endregion

    #region Navigation Command Tests

    [Fact]
    public void CreateTerminalCommand_ShouldNavigateToEditor()
    {
        // Act
        _viewModel.CreateTerminalCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo<TerminalEditorViewModel>(null), Times.Once);
    }

    [Fact]
    public async Task EditTerminalCommand_WithSelectedTerminal_ShouldNavigateToEditorWithId()
    {
        // Arrange
        var terminals = new List<Terminal> { CreateTestTerminal(42, "REG-001") };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        _viewModel.EditTerminalCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo<TerminalEditorViewModel>(42), Times.Once);
    }

    [Fact]
    public void EditTerminalCommand_WithNoSelection_ShouldNotNavigate()
    {
        // Arrange
        _viewModel.SelectedTerminal = null;

        // Act
        _viewModel.EditTerminalCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo<TerminalEditorViewModel>(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void GoBackCommand_ShouldNavigateBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    #endregion

    #region Terminal Status Command Tests

    [Fact]
    public async Task ViewTerminalStatusCommand_WithSelectedTerminal_ShouldShowStatus()
    {
        // Arrange
        var terminals = new List<Terminal> { CreateTestTerminal(1, "REG-001") };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        var status = new TerminalStatusDto
        {
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            IsOnline = true,
            IpAddress = "192.168.1.100"
        };
        _terminalServiceMock.Setup(s => s.GetTerminalStatusAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.ViewTerminalStatusCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Terminal Status", It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Toggle Active Status Tests

    [Fact]
    public async Task ToggleActiveStatusCommand_WithActiveTerminal_ShouldDeactivate()
    {
        // Arrange
        var terminals = new List<Terminal> { CreateTestTerminal(1, "REG-001", isActive: true) };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _dialogServiceMock.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _terminalServiceMock.Setup(s => s.DeactivateTerminalAsync(1, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.ToggleActiveStatusCommand.ExecuteAsync(null);

        // Assert
        _terminalServiceMock.Verify(s => s.DeactivateTerminalAsync(1, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleActiveStatusCommand_WithInactiveTerminal_ShouldReactivate()
    {
        // Arrange
        var terminals = new List<Terminal> { CreateTestTerminal(1, "REG-001", isActive: false) };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.ShowInactiveTerminals = true;
        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _dialogServiceMock.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _terminalServiceMock.Setup(s => s.ReactivateTerminalAsync(1, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.ToggleActiveStatusCommand.ExecuteAsync(null);

        // Assert
        _terminalServiceMock.Verify(s => s.ReactivateTerminalAsync(1, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleActiveStatusCommand_WhenUserCancels_ShouldNotToggle()
    {
        // Arrange
        var terminals = new List<Terminal> { CreateTestTerminal(1, "REG-001", isActive: true) };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _dialogServiceMock.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // User cancels

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.ToggleActiveStatusCommand.ExecuteAsync(null);

        // Assert
        _terminalServiceMock.Verify(s => s.DeactivateTerminalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Unbind Machine Tests

    [Fact]
    public async Task UnbindMachineCommand_WithAssignedTerminal_ShouldUnbind()
    {
        // Arrange
        var terminal = CreateTestTerminal(1, "REG-001", machineIdentifier: "AA:BB:CC:DD:EE:FF");
        var terminals = new List<Terminal> { terminal };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _dialogServiceMock.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _terminalServiceMock.Setup(s => s.UnbindMachineAsync(1, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.UnbindMachineCommand.ExecuteAsync(null);

        // Assert
        _terminalServiceMock.Verify(s => s.UnbindMachineAsync(1, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Success", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UnbindMachineCommand_WithUnassignedTerminal_ShouldShowInfo()
    {
        // Arrange
        var terminal = CreateTestTerminal(1, "REG-001", machineIdentifier: null);
        var terminals = new List<Terminal> { terminal };
        _terminalServiceMock.Setup(s => s.GetTerminalsByStoreAsync(TestStoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminals);

        _viewModel.OnNavigatedTo(null);
        await Task.Delay(100);

        _viewModel.SelectedTerminal = _viewModel.Terminals.First();

        // Act
        await _viewModel.UnbindMachineCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Info", It.Is<string>(m => m.Contains("not bound"))), Times.Once);
        _terminalServiceMock.Verify(s => s.UnbindMachineAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Generate Code Tests

    [Fact]
    public async Task GenerateTerminalCodeCommand_ShouldShowGeneratedCode()
    {
        // Arrange
        _terminalServiceMock.Setup(s => s.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register, It.IsAny<CancellationToken>()))
            .ReturnsAsync("REG-001");

        // Act
        await _viewModel.GenerateTerminalCodeCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInfoAsync("Generated Code", It.Is<string>(m => m.Contains("REG-001"))), Times.Once);
    }

    #endregion

    #region TerminalDisplayItem Tests

    [Fact]
    public void TerminalDisplayItem_StatusText_ShouldReflectState()
    {
        // Arrange
        var activeOnline = new TerminalDisplayItem { IsActive = true, IsOnline = true };
        var activeOffline = new TerminalDisplayItem { IsActive = true, IsOnline = false };
        var inactive = new TerminalDisplayItem { IsActive = false, IsOnline = false };

        // Assert
        activeOnline.StatusText.Should().Be("Online");
        activeOffline.StatusText.Should().Be("Offline");
        inactive.StatusText.Should().Be("Inactive");
    }

    [Fact]
    public void TerminalDisplayItem_AssignmentText_ShouldReflectState()
    {
        // Arrange
        var assigned = new TerminalDisplayItem { IsAssigned = true };
        var unassigned = new TerminalDisplayItem { IsAssigned = false };

        // Assert
        assigned.AssignmentText.Should().Be("Assigned");
        unassigned.AssignmentText.Should().Be("Unassigned");
    }

    #endregion

    #region Helper Methods

    private Terminal CreateTestTerminal(
        int id = 1,
        string code = "REG-001",
        string name = "Register 1",
        TerminalType terminalType = TerminalType.Register,
        bool isActive = true,
        string? machineIdentifier = null,
        DateTime? lastHeartbeat = null)
    {
        return new Terminal
        {
            Id = id,
            StoreId = TestStoreId,
            Code = code,
            Name = name,
            TerminalType = terminalType,
            BusinessMode = BusinessMode.Supermarket,
            MachineIdentifier = machineIdentifier ?? string.Empty,
            IsActive = isActive,
            LastHeartbeat = lastHeartbeat ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };
    }

    #endregion
}
