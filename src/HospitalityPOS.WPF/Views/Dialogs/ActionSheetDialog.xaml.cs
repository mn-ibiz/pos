using System.Windows;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for displaying a list of action options to choose from.
/// </summary>
public partial class ActionSheetDialog : Window
{
    /// <summary>
    /// Gets the selected option.
    /// </summary>
    public string? SelectedOption { get; private set; }

    private readonly string _cancelText;
    private readonly string? _destructiveText;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionSheetDialog"/> class.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">Optional message to display.</param>
    /// <param name="options">List of options to display.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <param name="destructiveText">Optional text for a destructive action.</param>
    public ActionSheetDialog(string title, string? message, IEnumerable<string> options, string cancelText, string? destructiveText)
    {
        InitializeComponent();

        _cancelText = cancelText;
        _destructiveText = destructiveText;

        TitleText.Text = title;
        Title = title;

        if (!string.IsNullOrWhiteSpace(message))
        {
            MessageText.Text = message;
            MessageText.Visibility = Visibility.Visible;
        }

        // Filter out cancel text from options since we have a separate cancel button
        var filteredOptions = options.Where(o => o != cancelText).ToList();
        OptionsPanel.ItemsSource = filteredOptions;

        CancelBtn.Content = cancelText;
    }

    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Content is string option)
        {
            SelectedOption = option;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        SelectedOption = null;
        DialogResult = false;
        Close();
    }
}
