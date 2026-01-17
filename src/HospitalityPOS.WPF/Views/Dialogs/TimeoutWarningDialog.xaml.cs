using System.ComponentModel;
using System.Windows;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for TimeoutWarningDialog.xaml
/// </summary>
public partial class TimeoutWarningDialog : Window, INotifyPropertyChanged
{
    private int _secondsRemaining;

    /// <summary>
    /// Gets or sets the number of seconds remaining until logout.
    /// </summary>
    public int SecondsRemaining
    {
        get => _secondsRemaining;
        set
        {
            if (_secondsRemaining != value)
            {
                _secondsRemaining = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecondsRemaining)));
            }
        }
    }

    /// <summary>
    /// Gets whether the user chose to stay logged in.
    /// </summary>
    public bool StayLoggedIn { get; private set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutWarningDialog"/> class.
    /// </summary>
    public TimeoutWarningDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Initializes a new instance with initial countdown value.
    /// </summary>
    /// <param name="initialSeconds">The initial countdown seconds.</param>
    public TimeoutWarningDialog(int initialSeconds) : this()
    {
        SecondsRemaining = initialSeconds;
    }

    /// <summary>
    /// Updates the countdown display.
    /// </summary>
    /// <param name="seconds">The remaining seconds.</param>
    public void UpdateCountdown(int seconds)
    {
        SecondsRemaining = seconds;

        if (seconds <= 0)
        {
            StayLoggedIn = false;
            DialogResult = false;
            Close();
        }
    }

    private void StayLoggedInButton_Click(object sender, RoutedEventArgs e)
    {
        StayLoggedIn = true;
        DialogResult = true;
        Close();
    }

    private void LogoutNowButton_Click(object sender, RoutedEventArgs e)
    {
        StayLoggedIn = false;
        DialogResult = false;
        Close();
    }
}
