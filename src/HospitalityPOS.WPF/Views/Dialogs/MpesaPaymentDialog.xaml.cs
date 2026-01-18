using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using Serilog;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// M-Pesa payment dialog for STK Push payments.
/// </summary>
public partial class MpesaPaymentDialog : Window
{
    private readonly IMpesaService? _mpesaService;
    private readonly ILogger _logger;
    private readonly decimal _amount;
    private readonly string _accountReference;
    private readonly DispatcherTimer _statusPollTimer;

    private string? _checkoutRequestId;
    private bool _paymentCompleted;
    private bool _useCash;

    /// <summary>
    /// Gets the M-Pesa receipt number if payment was successful.
    /// </summary>
    public string? MpesaReceiptNumber { get; private set; }

    /// <summary>
    /// Gets whether the user chose to use cash instead.
    /// </summary>
    public bool UsedCash => _useCash;

    /// <summary>
    /// Gets whether the payment was completed successfully.
    /// </summary>
    public bool PaymentSuccessful => _paymentCompleted;

    /// <summary>
    /// Gets the phone number used for payment.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MpesaPaymentDialog"/> class.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="accountReference">The account/receipt reference.</param>
    /// <param name="mpesaService">The M-Pesa service.</param>
    /// <param name="logger">The logger.</param>
    public MpesaPaymentDialog(decimal amount, string accountReference, IMpesaService? mpesaService, ILogger logger)
    {
        InitializeComponent();

        _amount = amount;
        _accountReference = accountReference;
        _mpesaService = mpesaService;
        _logger = logger;

        // Set up status polling timer
        _statusPollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _statusPollTimer.Tick += StatusPollTimer_Tick;

        // Initialize UI
        AmountDisplay.Text = $"KES {_amount:N2}";

        // Check if M-Pesa service is available
        if (_mpesaService == null)
        {
            StatusSection.Visibility = Visibility.Visible;
            StatusMessage.Text = "M-Pesa service is not configured. Please set up M-Pesa in Settings.";
            StatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")!);
            SendStkPushButton.IsEnabled = false;
        }
    }

    private void NumpadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && PhoneNumberInput.Text.Length < 9)
        {
            PhoneNumberInput.Text += button.Content.ToString();
            PhoneNumberInput.CaretIndex = PhoneNumberInput.Text.Length;
            ValidatePhoneNumber();
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        PhoneNumberInput.Text = string.Empty;
        PhoneValidationMessage.Visibility = Visibility.Collapsed;
    }

    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (PhoneNumberInput.Text.Length > 0)
        {
            PhoneNumberInput.Text = PhoneNumberInput.Text[..^1];
            ValidatePhoneNumber();
        }
    }

    private void PhoneNumberInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidatePhoneNumber();
    }

    private bool ValidatePhoneNumber()
    {
        var phone = PhoneNumberInput.Text.Trim();

        if (string.IsNullOrEmpty(phone))
        {
            PhoneValidationMessage.Visibility = Visibility.Collapsed;
            SendStkPushButton.IsEnabled = _mpesaService != null;
            return false;
        }

        // Check if it starts with valid prefixes (7 or 1 for Kenya)
        if (!phone.StartsWith("7") && !phone.StartsWith("1"))
        {
            PhoneValidationMessage.Text = "Phone number should start with 7 or 1";
            PhoneValidationMessage.Visibility = Visibility.Visible;
            SendStkPushButton.IsEnabled = false;
            return false;
        }

        // Check length (should be 9 digits after 254)
        if (phone.Length != 9)
        {
            PhoneValidationMessage.Text = $"Enter {9 - phone.Length} more digit(s)";
            PhoneValidationMessage.Visibility = Visibility.Visible;
            SendStkPushButton.IsEnabled = false;
            return false;
        }

        // Check if all characters are digits
        if (!phone.All(char.IsDigit))
        {
            PhoneValidationMessage.Text = "Phone number should contain only digits";
            PhoneValidationMessage.Visibility = Visibility.Visible;
            SendStkPushButton.IsEnabled = false;
            return false;
        }

        PhoneValidationMessage.Visibility = Visibility.Collapsed;
        SendStkPushButton.IsEnabled = _mpesaService != null;
        return true;
    }

    private async void SendStkPushButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidatePhoneNumber())
        {
            MessageBox.Show("Please enter a valid phone number.", "Invalid Phone", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_mpesaService == null)
        {
            MessageBox.Show("M-Pesa service is not configured.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var fullPhoneNumber = "254" + PhoneNumberInput.Text.Trim();
        PhoneNumber = fullPhoneNumber;

        // Disable input during processing
        SendStkPushButton.IsEnabled = false;
        PhoneNumberInput.IsEnabled = false;
        StatusSection.Visibility = Visibility.Visible;

        try
        {
            // Update UI to show sending
            UpdateStatus(MpesaStkStatus.Pending, "Sending STK Push request...");
            SetStepActive(1);

            _logger.Information("Initiating M-Pesa STK Push for {Phone}, Amount: {Amount}", fullPhoneNumber, _amount);

            // Initiate STK Push
            var result = await _mpesaService.InitiateStkPushAsync(
                fullPhoneNumber,
                _amount,
                _accountReference,
                "Payment for goods");

            if (result.Success && !string.IsNullOrEmpty(result.CheckoutRequestId))
            {
                _checkoutRequestId = result.CheckoutRequestId;

                // Update UI to show sent
                UpdateStatus(MpesaStkStatus.Processing, "STK Push sent! Please check your phone and enter your M-Pesa PIN.");
                SetStepComplete(1);
                SetStepActive(2);

                // Start polling for status
                _statusPollTimer.Start();

                _logger.Information("STK Push sent successfully. CheckoutRequestId: {CheckoutId}", _checkoutRequestId);
            }
            else
            {
                // Failed to initiate
                var errorMessage = result.ErrorMessage ?? result.ResponseDescription ?? "Failed to send STK Push";
                UpdateStatus(MpesaStkStatus.Failed, $"Error: {errorMessage}");
                SetStepFailed(1);

                SendStkPushButton.IsEnabled = true;
                PhoneNumberInput.IsEnabled = true;

                _logger.Warning("STK Push failed: {Error}", errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error initiating M-Pesa STK Push");
            UpdateStatus(MpesaStkStatus.Failed, $"Error: {ex.Message}");
            SetStepFailed(1);

            SendStkPushButton.IsEnabled = true;
            PhoneNumberInput.IsEnabled = true;
        }
    }

    private async void StatusPollTimer_Tick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_checkoutRequestId) || _mpesaService == null)
        {
            _statusPollTimer.Stop();
            return;
        }

        try
        {
            var status = await _mpesaService.QueryTransactionStatusAsync(_checkoutRequestId);

            switch (status.Status)
            {
                case MpesaStkStatus.Pending:
                    // Still waiting - keep polling
                    break;

                case MpesaStkStatus.Processing:
                    // Customer is entering PIN
                    UpdateStatus(MpesaStkStatus.Processing, "Customer is entering PIN...");
                    SetStepComplete(2);
                    SetStepActive(3);
                    break;

                case MpesaStkStatus.Success:
                    _statusPollTimer.Stop();
                    _paymentCompleted = true;
                    MpesaReceiptNumber = status.MpesaReceiptNumber;

                    UpdateStatus(MpesaStkStatus.Success, "Payment received successfully!");
                    SetStepComplete(3);
                    SetStepComplete(4);

                    // Show receipt number
                    if (!string.IsNullOrEmpty(MpesaReceiptNumber))
                    {
                        ReceiptSection.Visibility = Visibility.Visible;
                        MpesaReceiptText.Text = MpesaReceiptNumber;
                    }

                    _logger.Information("M-Pesa payment completed. Receipt: {Receipt}", MpesaReceiptNumber);

                    // Auto-close after short delay
                    await Task.Delay(2000);
                    DialogResult = true;
                    Close();
                    break;

                case MpesaStkStatus.Failed:
                    _statusPollTimer.Stop();
                    var failReason = status.ResultDescription ?? "Payment failed";
                    UpdateStatus(MpesaStkStatus.Failed, $"Payment failed: {failReason}");
                    SetStepFailed(GetCurrentStep());

                    SendStkPushButton.IsEnabled = true;
                    SendStkPushButton.Content = CreateRetryContent();
                    PhoneNumberInput.IsEnabled = true;

                    _logger.Warning("M-Pesa payment failed: {Reason}", failReason);
                    break;

                case MpesaStkStatus.Cancelled:
                    _statusPollTimer.Stop();
                    UpdateStatus(MpesaStkStatus.Cancelled, "Payment was cancelled by customer.");
                    SetStepFailed(GetCurrentStep());

                    SendStkPushButton.IsEnabled = true;
                    SendStkPushButton.Content = CreateRetryContent();
                    PhoneNumberInput.IsEnabled = true;

                    _logger.Information("M-Pesa payment cancelled by customer");
                    break;

                case MpesaStkStatus.Timeout:
                    _statusPollTimer.Stop();
                    UpdateStatus(MpesaStkStatus.Timeout, "Request timed out. Please try again.");
                    SetStepFailed(GetCurrentStep());

                    SendStkPushButton.IsEnabled = true;
                    SendStkPushButton.Content = CreateRetryContent();
                    PhoneNumberInput.IsEnabled = true;

                    _logger.Warning("M-Pesa STK Push timed out");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error polling M-Pesa status");
            // Continue polling - might be a temporary error
        }
    }

    private object CreateRetryContent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock
        {
            Text = "\uE72C",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "RETRY",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        return panel;
    }

    private void UpdateStatus(MpesaStkStatus status, string message)
    {
        StatusMessage.Text = message;

        var color = status switch
        {
            MpesaStkStatus.Pending => "#F59E0B",    // Yellow/Orange - waiting
            MpesaStkStatus.Processing => "#3B82F6", // Blue - processing
            MpesaStkStatus.Success => "#22C55E",    // Green - success
            MpesaStkStatus.Failed => "#EF4444",     // Red - failed
            MpesaStkStatus.Cancelled => "#6B7280",  // Grey - cancelled
            MpesaStkStatus.Timeout => "#F59E0B",    // Orange - timeout
            _ => "#9CA3AF"
        };

        StatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    private void SetStepActive(int step)
    {
        var indicator = step switch
        {
            1 => Step1Indicator,
            2 => Step2Indicator,
            3 => Step3Indicator,
            4 => Step4Indicator,
            _ => null
        };

        if (indicator != null)
        {
            indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")!);
        }
    }

    private void SetStepComplete(int step)
    {
        var indicator = step switch
        {
            1 => Step1Indicator,
            2 => Step2Indicator,
            3 => Step3Indicator,
            4 => Step4Indicator,
            _ => null
        };

        if (indicator != null)
        {
            indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")!);
        }
    }

    private void SetStepFailed(int step)
    {
        var indicator = step switch
        {
            1 => Step1Indicator,
            2 => Step2Indicator,
            3 => Step3Indicator,
            4 => Step4Indicator,
            _ => null
        };

        if (indicator != null)
        {
            indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")!);
        }
    }

    private int GetCurrentStep()
    {
        // Determine current step based on indicator colors
        if (Step4Indicator.Fill is SolidColorBrush b4 && b4.Color.ToString() == "#FF3B82F6") return 4;
        if (Step3Indicator.Fill is SolidColorBrush b3 && b3.Color.ToString() == "#FF3B82F6") return 3;
        if (Step2Indicator.Fill is SolidColorBrush b2 && b2.Color.ToString() == "#FF3B82F6") return 2;
        return 1;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _statusPollTimer.Stop();
        DialogResult = false;
        Close();
    }

    private void UseCashButton_Click(object sender, RoutedEventArgs e)
    {
        _statusPollTimer.Stop();
        _useCash = true;
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _statusPollTimer.Stop();
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _statusPollTimer.Stop();
        base.OnClosed(e);
    }
}
