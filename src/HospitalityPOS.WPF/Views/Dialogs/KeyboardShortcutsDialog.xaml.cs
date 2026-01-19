using System.Windows;
using System.Windows.Input;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Dialog that displays all available keyboard shortcuts for the POS system.
/// </summary>
public partial class KeyboardShortcutsDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardShortcutsDialog"/> class.
    /// </summary>
    public KeyboardShortcutsDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }
}
