using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for cash drawer configuration and management.
/// </summary>
public partial class CashDrawerSettingsViewModel : ObservableObject
{
    private readonly ICashDrawerService _cashDrawerService;
    private readonly IPrinterService _printerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<CashDrawer> _cashDrawers = new();

    [ObservableProperty]
    private CashDrawer? _selectedDrawer;

    [ObservableProperty]
    private ObservableCollection<Printer> _availablePrinters = new();

    [ObservableProperty]
    private Printer? _selectedPrinter;

    [ObservableProperty]
    private string _drawerName = "Main Drawer";

    [ObservableProperty]
    private CashDrawerPin _drawerPin = CashDrawerPin.Pin2;

    [ObservableProperty]
    private bool _autoOpenOnCashPayment = true;

    [ObservableProperty]
    private bool _autoOpenOnCashRefund = true;

    [ObservableProperty]
    private bool _autoOpenOnDrawerCount = true;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private CashDrawerStatus _drawerStatus = CashDrawerStatus.Unknown;

    [ObservableProperty]
    private ObservableCollection<CashDrawerLog> _todayLogs = new();

    [ObservableProperty]
    private DateTime _selectedLogDate = DateTime.Today;

    /// <summary>
    /// Gets the available drawer pin options.
    /// </summary>
    public CashDrawerPin[] DrawerPinOptions { get; } = Enum.GetValues<CashDrawerPin>();

    public CashDrawerSettingsViewModel(
        ICashDrawerService cashDrawerService,
        IPrinterService printerService,
        IDialogService dialogService)
    {
        _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
        _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Initializes the ViewModel by loading data.
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading cash drawer settings...";

        try
        {
            await LoadPrintersAsync();
            await LoadDrawersAsync();

            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
            await _dialogService.ShowErrorAsync("Load Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPrintersAsync()
    {
        var printers = await _printerService.GetAllPrintersAsync();
        AvailablePrinters = new ObservableCollection<Printer>(
            printers.Where(p => p.IsActive));
    }

    private async Task LoadDrawersAsync()
    {
        var drawers = await _cashDrawerService.GetAllDrawersAsync();
        CashDrawers = new ObservableCollection<CashDrawer>(drawers);

        if (CashDrawers.Count > 0 && SelectedDrawer == null)
        {
            SelectedDrawer = CashDrawers[0];
        }
    }

    partial void OnSelectedDrawerChanged(CashDrawer? value)
    {
        if (value != null)
        {
            LoadDrawerDetails(value);
            _ = LoadDrawerLogsAsync();
        }
        else
        {
            ClearForm();
        }
    }

    private void LoadDrawerDetails(CashDrawer drawer)
    {
        DrawerName = drawer.Name;
        DrawerPin = drawer.DrawerPin;
        AutoOpenOnCashPayment = drawer.AutoOpenOnCashPayment;
        AutoOpenOnCashRefund = drawer.AutoOpenOnCashRefund;
        AutoOpenOnDrawerCount = drawer.AutoOpenOnDrawerCount;
        SelectedPrinter = AvailablePrinters.FirstOrDefault(p => p.Id == drawer.LinkedPrinterId);
        DrawerStatus = drawer.Status;
        IsEditing = true;
    }

    private void ClearForm()
    {
        DrawerName = "Main Drawer";
        DrawerPin = CashDrawerPin.Pin2;
        AutoOpenOnCashPayment = true;
        AutoOpenOnCashRefund = true;
        AutoOpenOnDrawerCount = true;
        SelectedPrinter = null;
        DrawerStatus = CashDrawerStatus.Unknown;
        IsEditing = false;
    }

    partial void OnSelectedLogDateChanged(DateTime value)
    {
        _ = LoadDrawerLogsAsync();
    }

    private async Task LoadDrawerLogsAsync()
    {
        if (SelectedDrawer == null)
        {
            TodayLogs.Clear();
            return;
        }

        try
        {
            var logs = await _cashDrawerService.GetLogsAsync(SelectedDrawer.Id, SelectedLogDate);
            TodayLogs = new ObservableCollection<CashDrawerLog>(logs);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to load logs: {ex.Message}");
        }
    }

    [RelayCommand]
    private void NewDrawer()
    {
        SelectedDrawer = null;
        ClearForm();
        IsEditing = false;
    }

    [RelayCommand]
    private async Task SaveDrawerAsync()
    {
        if (string.IsNullOrWhiteSpace(DrawerName))
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Please enter a drawer name.");
            return;
        }

        if (SelectedPrinter == null)
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Please select a linked printer.");
            return;
        }

        IsLoading = true;
        StatusMessage = "Saving cash drawer...";

        try
        {
            if (IsEditing && SelectedDrawer != null)
            {
                // Update existing drawer
                SelectedDrawer.Name = DrawerName;
                SelectedDrawer.LinkedPrinterId = SelectedPrinter.Id;
                SelectedDrawer.DrawerPin = DrawerPin;
                SelectedDrawer.AutoOpenOnCashPayment = AutoOpenOnCashPayment;
                SelectedDrawer.AutoOpenOnCashRefund = AutoOpenOnCashRefund;
                SelectedDrawer.AutoOpenOnDrawerCount = AutoOpenOnDrawerCount;

                await _cashDrawerService.UpdateDrawerAsync(SelectedDrawer);
                StatusMessage = "Cash drawer updated successfully.";
            }
            else
            {
                // Create new drawer
                var drawer = new CashDrawer
                {
                    Name = DrawerName,
                    LinkedPrinterId = SelectedPrinter.Id,
                    DrawerPin = DrawerPin,
                    AutoOpenOnCashPayment = AutoOpenOnCashPayment,
                    AutoOpenOnCashRefund = AutoOpenOnCashRefund,
                    AutoOpenOnDrawerCount = AutoOpenOnDrawerCount
                };

                var created = await _cashDrawerService.CreateDrawerAsync(drawer);
                CashDrawers.Add(created);
                SelectedDrawer = created;
                StatusMessage = "Cash drawer created successfully.";
            }

            await LoadDrawersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving drawer: {ex.Message}";
            await _dialogService.ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDrawerAsync()
    {
        if (SelectedDrawer == null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Delete Cash Drawer",
            $"Are you sure you want to delete '{SelectedDrawer.Name}'?");

        if (!confirmed)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Deleting cash drawer...";

        try
        {
            await _cashDrawerService.DeleteDrawerAsync(SelectedDrawer.Id);
            CashDrawers.Remove(SelectedDrawer);
            SelectedDrawer = CashDrawers.FirstOrDefault();
            StatusMessage = "Cash drawer deleted successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting drawer: {ex.Message}";
            await _dialogService.ShowErrorAsync("Delete Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestDrawerAsync()
    {
        if (SelectedDrawer == null)
        {
            await _dialogService.ShowErrorAsync("Error", "Please select a cash drawer to test.");
            return;
        }

        IsLoading = true;
        StatusMessage = "Testing cash drawer...";

        try
        {
            var success = await _cashDrawerService.TestDrawerAsync(SelectedDrawer.Id);

            if (success)
            {
                DrawerStatus = CashDrawerStatus.Open;
                StatusMessage = "Cash drawer opened successfully!";
                await LoadDrawerLogsAsync();
            }
            else
            {
                StatusMessage = "Failed to open cash drawer. Check printer connection.";
                await _dialogService.ShowErrorAsync("Test Failed", "Could not open the cash drawer. Please check the printer connection.");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error testing drawer: {ex.Message}";
            await _dialogService.ShowErrorAsync("Test Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenDrawerManualAsync()
    {
        if (SelectedDrawer == null)
        {
            await _dialogService.ShowErrorAsync("Error", "Please select a cash drawer.");
            return;
        }

        // Show manual open dialog
        var notes = await _dialogService.ShowInputAsync(
            "Manual Drawer Open",
            "Enter reason for opening drawer:");

        if (string.IsNullOrEmpty(notes))
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Opening cash drawer...";

        try
        {
            var success = await _cashDrawerService.OpenDrawerAsync(
                SelectedDrawer.Id,
                CashDrawerOpenReason.ManualOpen,
                notes: notes);

            if (success)
            {
                DrawerStatus = CashDrawerStatus.Open;
                StatusMessage = "Cash drawer opened.";
                await LoadDrawerLogsAsync();
            }
            else
            {
                StatusMessage = "Failed to open cash drawer.";
                await _dialogService.ShowErrorAsync("Error", "Could not open the cash drawer.");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowErrorAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        await LoadDrawerLogsAsync();
    }

    [RelayCommand]
    private void PreviousDay()
    {
        SelectedLogDate = SelectedLogDate.AddDays(-1);
    }

    [RelayCommand]
    private void NextDay()
    {
        if (SelectedLogDate.Date < DateTime.Today)
        {
            SelectedLogDate = SelectedLogDate.AddDays(1);
        }
    }
}
