using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Payments;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the QR payment dialog.
/// Handles QR code display, countdown, and payment polling.
/// </summary>
public partial class QrPaymentDialogViewModel : ViewModelBase
{
    private readonly IQrPaymentService _qrService;
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _countdownTimer;
    private string? _currentQrPaymentId;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the QR code image.
    /// </summary>
    [ObservableProperty]
    private ImageSource? _qrCodeImage;

    /// <summary>
    /// Gets or sets the amount to pay.
    /// </summary>
    [ObservableProperty]
    private decimal _amount;

    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    [ObservableProperty]
    private int _receiptId;

    /// <summary>
    /// Gets or sets the receipt reference.
    /// </summary>
    [ObservableProperty]
    private string _receiptReference = string.Empty;

    /// <summary>
    /// Gets or sets the seconds remaining until QR expires.
    /// </summary>
    [ObservableProperty]
    private int _secondsRemaining;

    /// <summary>
    /// Gets or sets the current status text.
    /// </summary>
    [ObservableProperty]
    private string _status = "Generating QR Code...";

    /// <summary>
    /// Gets or sets whether a payment is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// Gets or sets whether payment was successful.
    /// </summary>
    [ObservableProperty]
    private bool _paymentSuccessful;

    /// <summary>
    /// Gets or sets whether the QR has expired.
    /// </summary>
    [ObservableProperty]
    private bool _isExpired;

    /// <summary>
    /// Gets or sets the M-Pesa receipt number (on success).
    /// </summary>
    [ObservableProperty]
    private string? _mpesaReceiptNumber;

    /// <summary>
    /// Gets or sets the transaction ID (on success).
    /// </summary>
    [ObservableProperty]
    private string? _transactionId;

    /// <summary>
    /// Gets the formatted time remaining.
    /// </summary>
    public string TimeRemainingDisplay => SecondsRemaining > 0
        ? $"{SecondsRemaining / 60}:{SecondsRemaining % 60:D2}"
        : "Expired";

    /// <summary>
    /// Gets the formatted amount.
    /// </summary>
    public string FormattedAmount => $"KSh {Amount:N0}";

    #endregion

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<QrPaymentDialogResult>? DialogClosed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrPaymentDialogViewModel"/> class.
    /// </summary>
    public QrPaymentDialogViewModel(IQrPaymentService qrService, ILogger logger) : base(logger)
    {
        _qrService = qrService ?? throw new ArgumentNullException(nameof(qrService));

        // Set up polling timer (every 3 seconds)
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _pollTimer.Tick += async (s, e) => await PollForPaymentAsync();

        // Set up countdown timer (every second)
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTick;
    }

    #region Public Methods

    /// <summary>
    /// Initializes the dialog with payment details and generates QR code.
    /// </summary>
    public async Task InitializeAsync(int receiptId, decimal amount, string receiptReference)
    {
        ReceiptId = receiptId;
        Amount = amount;
        ReceiptReference = receiptReference;

        await GenerateQrCodeAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to regenerate the QR code.
    /// </summary>
    [RelayCommand]
    private async Task RegenerateQrAsync()
    {
        StopTimers();
        IsExpired = false;
        Status = "Regenerating QR Code...";

        if (!string.IsNullOrEmpty(_currentQrPaymentId))
        {
            await _qrService.CancelQrPaymentAsync(_currentQrPaymentId);
        }

        await GenerateQrCodeAsync();
    }

    /// <summary>
    /// Command to cancel the QR payment.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        StopTimers();

        if (!string.IsNullOrEmpty(_currentQrPaymentId))
        {
            await _qrService.CancelQrPaymentAsync(_currentQrPaymentId);
        }

        DialogClosed?.Invoke(this, new QrPaymentDialogResult
        {
            Success = false,
            Cancelled = true
        });
    }

    /// <summary>
    /// Command to close the dialog after success.
    /// </summary>
    [RelayCommand]
    private void CloseSuccess()
    {
        StopTimers();
        DialogClosed?.Invoke(this, new QrPaymentDialogResult
        {
            Success = true,
            TransactionId = TransactionId,
            MpesaReceiptNumber = MpesaReceiptNumber,
            Amount = Amount
        });
    }

    /// <summary>
    /// Command to simulate payment (for testing).
    /// </summary>
    [RelayCommand]
    private async Task SimulatePaymentAsync()
    {
        if (string.IsNullOrEmpty(_currentQrPaymentId)) return;

        await _qrService.RecordPaymentAsync(
            _currentQrPaymentId,
            $"SIM{DateTime.Now.Ticks}",
            $"SIM{DateTime.Now:HHmmss}",
            "254700000000");

        await HandlePaymentSuccessAsync($"SIM{DateTime.Now.Ticks}", $"SIM{DateTime.Now:HHmmss}");
    }

    #endregion

    #region Private Methods

    private async Task GenerateQrCodeAsync()
    {
        try
        {
            IsProcessing = true;
            Status = "Generating QR Code...";

            var result = await _qrService.GenerateQrForReceiptAsync(ReceiptId, Amount);

            if (!result.Success)
            {
                Status = $"Failed: {result.ErrorMessage}";
                IsProcessing = false;
                return;
            }

            _currentQrPaymentId = result.QrPaymentId;

            // Convert byte array to ImageSource
            if (result.QrCodeBytes != null)
            {
                QrCodeImage = ConvertToImageSource(result.QrCodeBytes);
            }

            SecondsRemaining = result.SecondsRemaining;
            Status = "Scan the QR code with your M-Pesa app";
            IsProcessing = false;

            // Start timers
            _countdownTimer.Start();
            _pollTimer.Start();

            Logger.Information("QR code displayed for receipt {ReceiptId}, amount {Amount}",
                ReceiptId, Amount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate QR code");
            Status = $"Error: {ex.Message}";
            IsProcessing = false;
        }
    }

    private async Task PollForPaymentAsync()
    {
        if (string.IsNullOrEmpty(_currentQrPaymentId)) return;

        try
        {
            var statusResult = await _qrService.CheckPaymentStatusAsync(_currentQrPaymentId);

            switch (statusResult.Status)
            {
                case QrPaymentStatus.Completed:
                    await HandlePaymentSuccessAsync(statusResult.TransactionId!, statusResult.MpesaReceiptNumber!);
                    break;

                case QrPaymentStatus.Expired:
                    HandleExpired();
                    break;

                case QrPaymentStatus.Cancelled:
                case QrPaymentStatus.Failed:
                    StopTimers();
                    Status = "Payment was cancelled or failed";
                    break;

                case QrPaymentStatus.Scanned:
                    Status = "QR Code scanned - Waiting for payment...";
                    break;

                default:
                    // Still pending - continue polling
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error polling for payment status");
        }
    }

    private async Task HandlePaymentSuccessAsync(string transactionId, string receiptNumber)
    {
        StopTimers();
        PaymentSuccessful = true;
        TransactionId = transactionId;
        MpesaReceiptNumber = receiptNumber;
        Status = "Payment Received!";

        Logger.Information("QR payment successful for receipt {ReceiptId}: {TransactionId}",
            ReceiptId, transactionId);

        // Auto-close after a brief delay
        await Task.Delay(2000);
        CloseSuccess();
    }

    private void HandleExpired()
    {
        StopTimers();
        IsExpired = true;
        SecondsRemaining = 0;
        Status = "QR Code has expired. Click 'Regenerate' to try again.";
        OnPropertyChanged(nameof(TimeRemainingDisplay));
    }

    private void CountdownTick(object? sender, EventArgs e)
    {
        if (SecondsRemaining > 0)
        {
            SecondsRemaining--;
            OnPropertyChanged(nameof(TimeRemainingDisplay));

            if (SecondsRemaining <= 0)
            {
                HandleExpired();
            }
            else if (SecondsRemaining <= 30)
            {
                Status = "Hurry! QR Code expiring soon...";
            }
        }
    }

    private void StopTimers()
    {
        _pollTimer.Stop();
        _countdownTimer.Stop();
    }

    private static ImageSource? ConvertToImageSource(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return null;

        try
        {
            var image = new BitmapImage();
            using (var ms = new MemoryStream(imageBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }
        catch
        {
            // If byte array is not a valid image format,
            // create a placeholder
            return CreatePlaceholderQrImage();
        }
    }

    private static ImageSource CreatePlaceholderQrImage()
    {
        // Create a simple placeholder image
        var size = 300;
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            // Background
            context.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, size, size));

            // Border
            context.DrawRectangle(null, new Pen(Brushes.Black, 2),
                new System.Windows.Rect(10, 10, size - 20, size - 20));

            // QR pattern simulation
            var rand = new Random(42);
            var moduleSize = 10;
            var offset = 40;
            var modules = (size - 2 * offset) / moduleSize;

            for (int y = 0; y < modules; y++)
            {
                for (int x = 0; x < modules; x++)
                {
                    if (rand.Next(2) == 1)
                    {
                        context.DrawRectangle(Brushes.Black, null,
                            new System.Windows.Rect(offset + x * moduleSize, offset + y * moduleSize,
                                moduleSize - 1, moduleSize - 1));
                    }
                }
            }

            // Finder patterns (corners)
            DrawFinderPattern(context, 20, 20);
            DrawFinderPattern(context, size - 70, 20);
            DrawFinderPattern(context, 20, size - 70);
        }

        var renderTarget = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);
        renderTarget.Freeze();

        return renderTarget;
    }

    private static void DrawFinderPattern(DrawingContext context, double x, double y)
    {
        var size = 50;
        // Outer black
        context.DrawRectangle(Brushes.Black, null, new System.Windows.Rect(x, y, size, size));
        // White ring
        context.DrawRectangle(Brushes.White, null, new System.Windows.Rect(x + 7, y + 7, size - 14, size - 14));
        // Inner black
        context.DrawRectangle(Brushes.Black, null, new System.Windows.Rect(x + 14, y + 14, size - 28, size - 28));
    }

    #endregion
}

/// <summary>
/// Result of the QR payment dialog.
/// </summary>
public class QrPaymentDialogResult
{
    /// <summary>Whether payment was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Whether user cancelled.</summary>
    public bool Cancelled { get; set; }

    /// <summary>M-Pesa transaction ID.</summary>
    public string? TransactionId { get; set; }

    /// <summary>M-Pesa receipt number.</summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>Amount paid.</summary>
    public decimal Amount { get; set; }
}
