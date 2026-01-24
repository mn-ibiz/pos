using System.Windows;
using System.Windows.Controls;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models.HR;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Result from the manual attendance entry dialog.
/// </summary>
public class ManualAttendanceEntryResult
{
    /// <summary>
    /// Gets or sets the entry type.
    /// </summary>
    public AttendanceEventType EntryType { get; set; }

    /// <summary>
    /// Gets or sets the punch time.
    /// </summary>
    public DateTime PunchTime { get; set; }

    /// <summary>
    /// Gets or sets the reason for the manual entry.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the manager PIN.
    /// </summary>
    public string ManagerPin { get; set; } = string.Empty;
}

/// <summary>
/// Interaction logic for ManualAttendanceEntryDialog.xaml
/// </summary>
public partial class ManualAttendanceEntryDialog : Window
{
    private readonly Employee? _employee;

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public ManualAttendanceEntryResult? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualAttendanceEntryDialog"/> class.
    /// </summary>
    /// <param name="employee">The employee for the manual entry.</param>
    public ManualAttendanceEntryDialog(Employee? employee = null)
    {
        InitializeComponent();
        _employee = employee;

        SetupDialog();
    }

    private void SetupDialog()
    {
        // Populate hours (00-23)
        for (int i = 0; i < 24; i++)
        {
            var item = new ComboBoxItem { Content = i.ToString("D2"), Tag = i };
            HourComboBox.Items.Add(item);
            if (i == DateTime.Now.Hour)
            {
                HourComboBox.SelectedItem = item;
            }
        }

        // Populate minutes (00, 05, 10, ..., 55)
        for (int i = 0; i < 60; i += 5)
        {
            var item = new ComboBoxItem { Content = i.ToString("D2"), Tag = i };
            MinuteComboBox.Items.Add(item);
            // Select nearest 5-minute interval
            if (i <= DateTime.Now.Minute && (i + 5) > DateTime.Now.Minute)
            {
                MinuteComboBox.SelectedItem = item;
            }
        }

        // Set default date to today
        DatePicker.SelectedDate = DateTime.Today;
    }

    private AttendanceEventType GetSelectedEntryType()
    {
        if (EntryTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "ClockIn" => AttendanceEventType.ClockIn,
                "ClockOut" => AttendanceEventType.ClockOut,
                "BreakStart" => AttendanceEventType.BreakStart,
                "BreakEnd" => AttendanceEventType.BreakEnd,
                _ => AttendanceEventType.ClockIn
            };
        }
        return AttendanceEventType.ClockIn;
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        // Validate date
        if (!DatePicker.SelectedDate.HasValue)
        {
            ShowError("Please select a date.");
            return;
        }

        // Validate time
        if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
        {
            ShowError("Please select a time.");
            return;
        }

        // Validate reason
        if (ReasonComboBox.SelectedItem == null)
        {
            ShowError("Please select a reason.");
            return;
        }

        // Validate manager PIN
        var managerPin = ManagerPinBox.Password;
        if (string.IsNullOrWhiteSpace(managerPin))
        {
            ShowError("Manager PIN is required for authorization.");
            ManagerPinBox.Focus();
            return;
        }

        if (managerPin.Length < 4)
        {
            ShowError("Manager PIN must be at least 4 digits.");
            ManagerPinBox.Focus();
            return;
        }

        // Build punch time
        var selectedDate = DatePicker.SelectedDate.Value.Date;
        var hour = (int)((ComboBoxItem)HourComboBox.SelectedItem).Tag;
        var minute = (int)((ComboBoxItem)MinuteComboBox.SelectedItem).Tag;
        var punchTime = selectedDate.AddHours(hour).AddMinutes(minute);

        // Validate that punch time is not in the future
        if (punchTime > DateTime.Now)
        {
            ShowError("Punch time cannot be in the future.");
            return;
        }

        // Get reason
        var reason = ((ComboBoxItem)ReasonComboBox.SelectedItem).Content?.ToString() ?? "Manual entry";
        var notes = NotesTextBox.Text.Trim();

        Result = new ManualAttendanceEntryResult
        {
            EntryType = GetSelectedEntryType(),
            PunchTime = punchTime,
            Reason = reason,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            ManagerPin = managerPin
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
    }
}
