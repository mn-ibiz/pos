using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog for adding special instructions/notes to an order item.
/// </summary>
public partial class OrderItemNoteDialog : Window
{
    /// <summary>
    /// Gets or sets the note text.
    /// </summary>
    public string NoteText
    {
        get => NotesTextBox.Text;
        set => NotesTextBox.Text = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderItemNoteDialog"/> class.
    /// </summary>
    /// <param name="productName">The product name to display.</param>
    /// <param name="existingNote">Existing note text, if any.</param>
    public OrderItemNoteDialog(string productName, string? existingNote = null)
    {
        InitializeComponent();
        ProductNameText.Text = productName;
        NotesTextBox.Text = existingNote ?? string.Empty;

        Loaded += (s, e) =>
        {
            NotesTextBox.Focus();
            NotesTextBox.CaretIndex = NotesTextBox.Text.Length;
        };

        // Allow drag to move window
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        };
    }

    private void QuickNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string quickNote)
        {
            var currentText = NotesTextBox.Text.Trim();
            if (string.IsNullOrEmpty(currentText))
            {
                NotesTextBox.Text = quickNote;
            }
            else
            {
                NotesTextBox.Text = currentText + ", " + quickNote;
            }
            NotesTextBox.CaretIndex = NotesTextBox.Text.Length;
            NotesTextBox.Focus();
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        NotesTextBox.Text = string.Empty;
        NotesTextBox.Focus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
