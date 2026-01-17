using CommunityToolkit.Mvvm.ComponentModel;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.WPF.Models;

/// <summary>
/// Display model for a table in the table map view.
/// Contains all information needed to display a table card with status, waiter, bill amount, etc.
/// </summary>
public partial class TableDisplayItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the underlying table entity.
    /// </summary>
    public Table Table { get; set; } = null!;

    /// <summary>
    /// Gets or sets the table number for display.
    /// </summary>
    [ObservableProperty]
    private string _tableNumber = string.Empty;

    /// <summary>
    /// Gets or sets the current table status.
    /// </summary>
    [ObservableProperty]
    private TableStatus _status;

    /// <summary>
    /// Gets or sets the status color (hex).
    /// </summary>
    [ObservableProperty]
    private string _statusColor = string.Empty;

    /// <summary>
    /// Gets or sets the name of the assigned waiter.
    /// </summary>
    [ObservableProperty]
    private string _waiterName = string.Empty;

    /// <summary>
    /// Gets or sets the current bill amount.
    /// </summary>
    [ObservableProperty]
    private decimal _billAmount;

    /// <summary>
    /// Gets or sets the formatted duration since table was occupied.
    /// </summary>
    [ObservableProperty]
    private string _occupiedDuration = string.Empty;

    /// <summary>
    /// Gets or sets the guest count.
    /// </summary>
    [ObservableProperty]
    private int _guestCount;

    /// <summary>
    /// Gets or sets the table capacity.
    /// </summary>
    [ObservableProperty]
    private int _capacity;

    /// <summary>
    /// Gets or sets the X grid position.
    /// </summary>
    [ObservableProperty]
    private int _gridX;

    /// <summary>
    /// Gets or sets the Y grid position.
    /// </summary>
    [ObservableProperty]
    private int _gridY;

    /// <summary>
    /// Gets or sets the width in grid cells.
    /// </summary>
    [ObservableProperty]
    private int _width = 1;

    /// <summary>
    /// Gets or sets the height in grid cells.
    /// </summary>
    [ObservableProperty]
    private int _height = 1;

    /// <summary>
    /// Gets or sets the table shape.
    /// </summary>
    [ObservableProperty]
    private TableShape _shape;

    /// <summary>
    /// Gets or sets the section color (hex).
    /// </summary>
    [ObservableProperty]
    private string _sectionColor = "#4CAF50";

    /// <summary>
    /// Gets or sets whether this table is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    [ObservableProperty]
    private string _sectionName = string.Empty;

    /// <summary>
    /// Gets or sets the reservation time if reserved.
    /// </summary>
    [ObservableProperty]
    private string _reservationTime = string.Empty;

    /// <summary>
    /// Gets or sets the reservation name if reserved.
    /// </summary>
    [ObservableProperty]
    private string _reservationName = string.Empty;

    /// <summary>
    /// Gets the formatted bill amount for display.
    /// </summary>
    public string BillAmountDisplay => BillAmount > 0
        ? $"KSh {BillAmount:N0}"
        : string.Empty;

    /// <summary>
    /// Gets the guest display text.
    /// </summary>
    public string GuestDisplay => Status == TableStatus.Occupied && GuestCount > 0
        ? $"{GuestCount}/{Capacity}"
        : Capacity > 0 ? $"({Capacity})" : string.Empty;

    /// <summary>
    /// Gets the status text for display.
    /// </summary>
    public string StatusText => Status switch
    {
        TableStatus.Available => "Available",
        TableStatus.Occupied => "Occupied",
        TableStatus.Reserved => "Reserved",
        TableStatus.Unavailable => "Unavailable",
        _ => string.Empty
    };

    /// <summary>
    /// Creates a TableDisplayItem from a Table entity.
    /// </summary>
    public static TableDisplayItem FromTable(Table table)
    {
        var item = new TableDisplayItem
        {
            Table = table,
            TableNumber = table.TableNumber,
            Status = table.Status,
            StatusColor = GetStatusColor(table.Status),
            WaiterName = table.AssignedUser?.FullName ?? string.Empty,
            BillAmount = table.CurrentReceipt?.TotalAmount ?? 0,
            OccupiedDuration = table.OccupiedSince.HasValue
                ? FormatDuration(DateTime.Now - table.OccupiedSince.Value)
                : string.Empty,
            GuestCount = table.CurrentReceipt?.GuestCount ?? 0,
            Capacity = table.Capacity,
            GridX = table.GridX,
            GridY = table.GridY,
            Width = table.Width,
            Height = table.Height,
            Shape = table.Shape,
            SectionColor = table.Section?.ColorCode ?? "#4CAF50",
            SectionName = table.Section?.Name ?? string.Empty
        };

        return item;
    }

    /// <summary>
    /// Gets the status color for the given status.
    /// </summary>
    public static string GetStatusColor(TableStatus status)
    {
        return status switch
        {
            TableStatus.Available => "#4CAF50",    // Green
            TableStatus.Occupied => "#F44336",     // Red
            TableStatus.Reserved => "#FFC107",     // Yellow/Amber
            TableStatus.Unavailable => "#9E9E9E", // Gray
            _ => "#FFFFFF"
        };
    }

    /// <summary>
    /// Formats a duration into a human-readable string.
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes} min";
    }

    /// <summary>
    /// Updates the display item from the table entity.
    /// </summary>
    public void UpdateFromTable()
    {
        if (Table == null) return;

        Status = Table.Status;
        StatusColor = GetStatusColor(Table.Status);
        WaiterName = Table.AssignedUser?.FullName ?? string.Empty;
        BillAmount = Table.CurrentReceipt?.TotalAmount ?? 0;
        OccupiedDuration = Table.OccupiedSince.HasValue
            ? FormatDuration(DateTime.Now - Table.OccupiedSince.Value)
            : string.Empty;
        GuestCount = Table.CurrentReceipt?.GuestCount ?? 0;

        OnPropertyChanged(nameof(BillAmountDisplay));
        OnPropertyChanged(nameof(GuestDisplay));
        OnPropertyChanged(nameof(StatusText));
    }
}
