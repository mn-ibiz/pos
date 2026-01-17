using System.Windows;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Input dialog for collecting text input from the user.
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Gets the prompt text.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// Gets the entered text.
    /// </summary>
    public string? InputText { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is a password input.
    /// </summary>
    public bool IsPassword { get; }

    /// <summary>
    /// Gets the maximum length for input.
    /// </summary>
    public int MaxLength { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDialog"/> class.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <param name="isPassword">Whether to mask input.</param>
    /// <param name="maxLength">Maximum input length.</param>
    public InputDialog(string title, string prompt, string defaultValue = "", bool isPassword = false, int maxLength = 100)
    {
        InitializeComponent();

        Title = title;
        Prompt = prompt;
        IsPassword = isPassword;
        MaxLength = maxLength;

        // Configure visibility based on password mode
        if (isPassword)
        {
            InputTextBox.Visibility = Visibility.Collapsed;
            PasswordInputBox.Visibility = Visibility.Visible;
            PasswordInputBox.MaxLength = maxLength;
            PasswordInputBox.Focus();
        }
        else
        {
            InputTextBox.Visibility = Visibility.Visible;
            PasswordInputBox.Visibility = Visibility.Collapsed;
            InputTextBox.MaxLength = maxLength;
            InputTextBox.Text = defaultValue;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        DataContext = this;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        InputText = IsPassword ? PasswordInputBox.Password : InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
