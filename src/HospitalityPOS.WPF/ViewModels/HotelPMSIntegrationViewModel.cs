using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Hotel PMS Integration - handles Opera, Marsha connections, guest sync, and folio posting.
/// </summary>
public partial class HotelPMSIntegrationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<PMSConnection> _connections = new();

    [ObservableProperty]
    private ObservableCollection<GuestFolio> _activeFolios = new();

    [ObservableProperty]
    private ObservableCollection<PMSTransaction> _recentTransactions = new();

    [ObservableProperty]
    private ObservableCollection<SyncLog> _syncLogs = new();

    [ObservableProperty]
    private PMSConnection? _selectedConnection;

    [ObservableProperty]
    private GuestFolio? _selectedFolio;

    [ObservableProperty]
    private PMSSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Connection status
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    // Summary stats
    [ObservableProperty]
    private int _activeGuestCount;

    [ObservableProperty]
    private decimal _pendingPostings;

    [ObservableProperty]
    private int _todayCheckIns;

    [ObservableProperty]
    private int _todayCheckOuts;

    // Connection Editor
    [ObservableProperty]
    private bool _isConnectionEditorOpen;

    [ObservableProperty]
    private PMSConnectionRequest _editingConnection = new();

    [ObservableProperty]
    private bool _isNewConnection;

    #endregion

    public List<string> PMSTypes { get; } = new() { "Opera", "Marsha", "Protel", "RoomMaster", "Custom" };

    public HotelPMSIntegrationViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Hotel PMS Integration";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;

            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            // Load connections
            var connections = await pmsService.GetConnectionsAsync();
            Connections = new ObservableCollection<PMSConnection>(connections);

            var activeConnection = connections.FirstOrDefault(c => c.IsActive);
            if (activeConnection != null)
            {
                IsConnected = activeConnection.Status == "Connected";
                ConnectionStatus = activeConnection.Status;
                LastSyncTime = activeConnection.LastSyncTime;
            }

            // Load active folios
            var folios = await pmsService.GetActiveFoliosAsync();
            ActiveFolios = new ObservableCollection<GuestFolio>(folios);
            ActiveGuestCount = folios.Count;

            // Load recent transactions
            var transactions = await pmsService.GetRecentTransactionsAsync(50);
            RecentTransactions = new ObservableCollection<PMSTransaction>(transactions);
            PendingPostings = transactions.Where(t => t.Status == "Pending").Sum(t => t.Amount);

            // Load today's activity
            var today = DateOnly.FromDateTime(DateTime.Today);
            TodayCheckIns = await pmsService.GetCheckInCountAsync(today);
            TodayCheckOuts = await pmsService.GetCheckOutCountAsync(today);

            // Load sync logs
            var logs = await pmsService.GetSyncLogsAsync(20);
            SyncLogs = new ObservableCollection<SyncLog>(logs);

            // Load settings
            Settings = await pmsService.GetSettingsAsync();

            IsLoading = false;
        }, "Loading PMS data...");
    }

    [RelayCommand]
    private void CreateConnection()
    {
        EditingConnection = new PMSConnectionRequest
        {
            PMSType = "Opera",
            Port = 443,
            UseSSL = true
        };
        IsNewConnection = true;
        IsConnectionEditorOpen = true;
    }

    [RelayCommand]
    private void EditConnection(PMSConnection? connection)
    {
        if (connection is null) return;

        EditingConnection = new PMSConnectionRequest
        {
            Id = connection.Id,
            Name = connection.Name,
            PMSType = connection.PMSType,
            HostUrl = connection.HostUrl,
            Port = connection.Port,
            Username = connection.Username,
            UseSSL = connection.UseSSL,
            PropertyCode = connection.PropertyCode
        };
        IsNewConnection = false;
        IsConnectionEditorOpen = true;
    }

    [RelayCommand]
    private async Task SaveConnectionAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            if (IsNewConnection)
            {
                await pmsService.CreateConnectionAsync(EditingConnection);
            }
            else
            {
                await pmsService.UpdateConnectionAsync(EditingConnection);
            }

            IsConnectionEditorOpen = false;
            await LoadDataAsync();
        }, "Saving connection...");
    }

    [RelayCommand]
    private void CancelEditConnection()
    {
        IsConnectionEditorOpen = false;
    }

    [RelayCommand]
    private async Task TestConnectionAsync(PMSConnection? connection)
    {
        if (connection is null) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            var result = await pmsService.TestConnectionAsync(connection.Id);

            if (result.Success)
            {
                await DialogService.ShowMessageAsync("Connection Test", $"Successfully connected to {connection.Name}!\nPMS Version: {result.PMSVersion}");
            }
            else
            {
                await DialogService.ShowErrorAsync("Connection Failed", result.ErrorMessage ?? "Failed to connect to PMS");
            }

            await LoadDataAsync();
        }, "Testing connection...");
    }

    [RelayCommand]
    private async Task SyncGuestsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            var result = await pmsService.SyncGuestsAsync();
            await DialogService.ShowMessageAsync(
                "Sync Complete",
                $"Guests synced: {result.GuestsSynced}\nNew guests: {result.NewGuests}\nUpdated: {result.UpdatedGuests}");
            await LoadDataAsync();
        }, "Syncing guests...");
    }

    [RelayCommand]
    private async Task PostChargeAsync(GuestFolio? folio)
    {
        if (folio is null) return;

        var amountStr = await DialogService.ShowInputAsync(
            "Post Charge",
            $"Enter charge amount for room {folio.RoomNumber}:");

        if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
        {
            ErrorMessage = "Invalid amount";
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            var result = await pmsService.PostChargeAsync(folio.Id, amount, "POS Charge", SessionService.CurrentUserId);

            if (result.Success)
            {
                await DialogService.ShowMessageAsync("Success", $"Charge of KSh {amount:N0} posted to room {folio.RoomNumber}");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to post charge";
            }
        }, "Posting charge...");
    }

    [RelayCommand]
    private async Task ViewFolioDetailsAsync(GuestFolio? folio)
    {
        if (folio is null) return;

        await DialogService.ShowMessageAsync(
            $"Room {folio.RoomNumber}",
            $"Guest: {folio.GuestName}\n" +
            $"Check-In: {folio.CheckInDate:d}\n" +
            $"Check-Out: {folio.CheckOutDate:d}\n" +
            $"Folio Balance: KSh {folio.FolioBalance:N0}\n" +
            $"Credit Limit: KSh {folio.CreditLimit:N0}");
    }

    [RelayCommand]
    private async Task RefreshTokenAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            var activeConnection = Connections.FirstOrDefault(c => c.IsActive);
            if (activeConnection is null)
            {
                ErrorMessage = "No active connection";
                return;
            }

            await pmsService.RefreshTokenAsync(activeConnection.Id);
            await DialogService.ShowMessageAsync("Success", "OAuth token refreshed successfully.");
            await LoadDataAsync();
        }, "Refreshing token...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pmsService = scope.ServiceProvider.GetService<IHotelPMSService>();

            if (pmsService is null)
            {
                ErrorMessage = "PMS Integration service not available";
                return;
            }

            await pmsService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "PMS settings saved.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class PMSConnection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PMSType { get; set; } = string.Empty;
    public string HostUrl { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public bool UseSSL { get; set; }
    public string? PropertyCode { get; set; }
    public string Status { get; set; } = "Disconnected";
    public bool IsActive { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PMSConnectionRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PMSType { get; set; } = string.Empty;
    public string HostUrl { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSSL { get; set; } = true;
    public string? PropertyCode { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class GuestFolio
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public decimal FolioBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit => CreditLimit - FolioBalance;
    public string Status { get; set; } = "Active";
    public string? PMSFolioId { get; set; }
}

public class PMSTransaction
{
    public int Id { get; set; }
    public int? FolioId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? PMSReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SyncLog
{
    public int Id { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int Errors { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class PMSSettings
{
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
    public bool PostChargesImmediately { get; set; } = true;
    public bool RequireRoomVerification { get; set; } = true;
    public decimal MaxChargeWithoutVerification { get; set; } = 10000m;
    public bool SendCheckoutNotifications { get; set; } = true;
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string? PMSVersion { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GuestSyncResult
{
    public int GuestsSynced { get; set; }
    public int NewGuests { get; set; }
    public int UpdatedGuests { get; set; }
}

public class ChargePostResult
{
    public bool Success { get; set; }
    public string? PMSReference { get; set; }
    public string? ErrorMessage { get; set; }
}
