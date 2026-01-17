using System.Windows;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for DatePickerDialog.xaml
/// </summary>
public partial class DatePickerDialog : Window
{
    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string DialogTitle { get; }

    /// <summary>
    /// Gets the prompt text.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// Gets or sets the selected date.
    /// </summary>
    public DateTime SelectedDate { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatePickerDialog"/> class.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="defaultDate">The default date.</param>
    public DatePickerDialog(string title, string prompt, DateTime defaultDate)
    {
        InitializeComponent();

        DialogTitle = title;
        Title = title;
        Prompt = prompt;
        SelectedDate = defaultDate;
        DataContext = this;
    }

    private void OnSelectClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
