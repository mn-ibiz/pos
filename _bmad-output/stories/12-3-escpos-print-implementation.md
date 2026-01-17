# Story 12.3: ESC/POS Print Implementation

Status: done

## Story

As a developer,
I want reliable ESC/POS printing,
So that all print jobs complete successfully on thermal printers.

## Acceptance Criteria

1. **Given** printers are configured
   **When** any print job is triggered
   **Then** system should generate proper ESC/POS commands

2. **Given** print formatting
   **When** generating print commands
   **Then** print jobs should support: text styles (bold, large), alignment, cut

3. **Given** logo printing
   **When** receipt requires logo
   **Then** graphics/logo printing should be supported (basic)

4. **Given** multiple print jobs
   **When** queue is active
   **Then** print queue should handle multiple jobs gracefully

5. **Given** print errors
   **When** error occurs
   **Then** print errors should be reported to user with retry option

## Tasks / Subtasks

- [x] Task 1: Create ESC/POS Command Library
  - [x] Implement ESC/POS command constants
  - [x] Create command builder class
  - [x] Support text formatting
  - [x] Support special characters

- [x] Task 2: Implement Print Document Builder
  - [x] Create PrintDocument class
  - [x] Text styling methods
  - [x] Alignment methods
  - [x] Line and separator methods

- [x] Task 3: Implement Printer Communication
  - [x] USB printer communication
  - [x] Network printer communication
  - [x] Serial port communication
  - [x] Windows spooler integration

- [x] Task 4: Create Print Queue Manager
  - [x] Queue print jobs
  - [x] Handle job priorities
  - [x] Retry failed jobs
  - [x] Clear stuck jobs

- [x] Task 5: Implement Image/Logo Printing
  - [x] Convert image to bitmap
  - [x] Generate raster graphics
  - [x] Handle image scaling
  - [x] Cache converted images

## Dev Notes

### ESC/POS Command Constants

```csharp
public static class EscPosCommands
{
    // Initialize printer
    public static readonly byte[] Initialize = { 0x1B, 0x40 };

    // Text formatting
    public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
    public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
    public static readonly byte[] UnderlineOn = { 0x1B, 0x2D, 0x01 };
    public static readonly byte[] UnderlineOff = { 0x1B, 0x2D, 0x00 };
    public static readonly byte[] DoubleStrikeOn = { 0x1B, 0x47, 0x01 };
    public static readonly byte[] DoubleStrikeOff = { 0x1B, 0x47, 0x00 };

    // Font size
    public static readonly byte[] NormalSize = { 0x1D, 0x21, 0x00 };
    public static readonly byte[] DoubleHeight = { 0x1D, 0x21, 0x01 };
    public static readonly byte[] DoubleWidth = { 0x1D, 0x21, 0x10 };
    public static readonly byte[] DoubleSize = { 0x1D, 0x21, 0x11 };

    // Alignment
    public static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };
    public static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };
    public static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };

    // Paper cut
    public static readonly byte[] FullCut = { 0x1D, 0x56, 0x00 };
    public static readonly byte[] PartialCut = { 0x1D, 0x56, 0x01 };
    public static readonly byte[] FeedAndCut = { 0x1D, 0x56, 0x41, 0x03 };

    // Line feed
    public static readonly byte[] LineFeed = { 0x0A };
    public static readonly byte[] CarriageReturn = { 0x0D };

    // Cash drawer
    public static readonly byte[] OpenCashDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA };
    public static readonly byte[] OpenCashDrawer2 = { 0x1B, 0x70, 0x01, 0x19, 0xFA };

    // Beeper
    public static byte[] Beep(int times = 1)
    {
        return new byte[] { 0x1B, 0x42, (byte)times, 0x02 };
    }

    // Print density (0-7, default 7)
    public static byte[] SetDensity(int density)
    {
        return new byte[] { 0x1D, 0x7C, (byte)Math.Clamp(density, 0, 7) };
    }

    // Character code page (for special characters)
    public static readonly byte[] CodePagePC437 = { 0x1B, 0x74, 0x00 };
    public static readonly byte[] CodePageUTF8 = { 0x1B, 0x74, 0xFF };

    // Raster bit image
    public static byte[] RasterBitImage(int width, int height)
    {
        var wL = (byte)(width % 256);
        var wH = (byte)(width / 256);
        var hL = (byte)(height % 256);
        var hH = (byte)(height / 256);
        return new byte[] { 0x1D, 0x76, 0x30, 0x00, wL, wH, hL, hH };
    }
}
```

### PrintDocument Builder

```csharp
public class EscPosPrintDocument
{
    private readonly List<byte> _buffer = new();
    private readonly int _charsPerLine;
    private readonly Encoding _encoding;

    public EscPosPrintDocument(int charsPerLine = 48)
    {
        _charsPerLine = charsPerLine;
        _encoding = Encoding.GetEncoding("IBM437"); // ESC/POS default

        // Initialize printer
        AddCommand(EscPosCommands.Initialize);
    }

    public EscPosPrintDocument AddCommand(byte[] command)
    {
        _buffer.AddRange(command);
        return this;
    }

    public EscPosPrintDocument AddText(string text)
    {
        _buffer.AddRange(_encoding.GetBytes(text));
        return this;
    }

    public EscPosPrintDocument AddLine(string text = "")
    {
        AddText(text);
        AddCommand(EscPosCommands.LineFeed);
        return this;
    }

    public EscPosPrintDocument AddBoldText(string text)
    {
        AddCommand(EscPosCommands.BoldOn);
        AddText(text);
        AddCommand(EscPosCommands.BoldOff);
        return this;
    }

    public EscPosPrintDocument AddLargeText(string text)
    {
        AddCommand(EscPosCommands.DoubleSize);
        AddText(text);
        AddCommand(EscPosCommands.NormalSize);
        return this;
    }

    public EscPosPrintDocument AddCenteredText(string text)
    {
        AddCommand(EscPosCommands.AlignCenter);
        AddText(text);
        AddCommand(EscPosCommands.AlignLeft);
        return this;
    }

    public EscPosPrintDocument AddRightAlignedText(string text)
    {
        AddCommand(EscPosCommands.AlignRight);
        AddText(text);
        AddCommand(EscPosCommands.AlignLeft);
        return this;
    }

    public EscPosPrintDocument AddTwoColumnLine(string left, string right)
    {
        var spaces = _charsPerLine - left.Length - right.Length;
        if (spaces < 1) spaces = 1;

        var line = left + new string(' ', spaces) + right;
        return AddLine(line);
    }

    public EscPosPrintDocument AddThreeColumnLine(string left, string center, string right)
    {
        var totalContentLength = left.Length + center.Length + right.Length;
        var totalSpaces = _charsPerLine - totalContentLength;
        var leftSpaces = totalSpaces / 2;
        var rightSpaces = totalSpaces - leftSpaces;

        var line = left +
                   new string(' ', Math.Max(1, leftSpaces)) +
                   center +
                   new string(' ', Math.Max(1, rightSpaces)) +
                   right;

        return AddLine(line);
    }

    public EscPosPrintDocument AddSeparator(char character = '-')
    {
        return AddLine(new string(character, _charsPerLine));
    }

    public EscPosPrintDocument AddDoubleSeparator()
    {
        return AddLine(new string('=', _charsPerLine));
    }

    public EscPosPrintDocument AddEmptyLine(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            AddCommand(EscPosCommands.LineFeed);
        }
        return this;
    }

    public EscPosPrintDocument AddImage(byte[] imageData, int width)
    {
        AddCommand(EscPosCommands.AlignCenter);
        AddCommand(EscPosCommands.RasterBitImage(width, imageData.Length / (width / 8)));
        _buffer.AddRange(imageData);
        AddCommand(EscPosCommands.AlignLeft);
        return this;
    }

    public EscPosPrintDocument AddLogo(byte[] logoData)
    {
        if (logoData != null && logoData.Length > 0)
        {
            AddCommand(EscPosCommands.AlignCenter);
            _buffer.AddRange(logoData);
            AddCommand(EscPosCommands.AlignLeft);
            AddEmptyLine();
        }
        return this;
    }

    public EscPosPrintDocument Cut(bool partial = true)
    {
        AddEmptyLine(3);
        AddCommand(partial ? EscPosCommands.PartialCut : EscPosCommands.FullCut);
        return this;
    }

    public EscPosPrintDocument OpenCashDrawer()
    {
        AddCommand(EscPosCommands.OpenCashDrawer);
        return this;
    }

    public EscPosPrintDocument Beep(int times = 1)
    {
        AddCommand(EscPosCommands.Beep(times));
        return this;
    }

    public byte[] ToBytes()
    {
        return _buffer.ToArray();
    }
}
```

### Printer Communication Service

```csharp
public interface IPrinterCommunicationService
{
    Task<PrintResult> SendAsync(Printer printer, byte[] data);
    Task<PrinterStatus> GetStatusAsync(Printer printer);
}

public class PrinterCommunicationService : IPrinterCommunicationService
{
    public async Task<PrintResult> SendAsync(Printer printer, byte[] data)
    {
        try
        {
            return printer.ConnectionType switch
            {
                PrinterConnectionType.Network => await SendNetworkAsync(printer, data),
                PrinterConnectionType.USB => await SendUsbAsync(printer, data),
                PrinterConnectionType.Serial => await SendSerialAsync(printer, data),
                PrinterConnectionType.WindowsDriver => await SendWindowsAsync(printer, data),
                _ => PrintResult.Failed("Unknown connection type")
            };
        }
        catch (Exception ex)
        {
            return PrintResult.Failed(ex.Message);
        }
    }

    private async Task<PrintResult> SendNetworkAsync(Printer printer, byte[] data)
    {
        if (string.IsNullOrEmpty(printer.IpAddress))
        {
            return PrintResult.Failed("IP address not configured");
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(printer.IpAddress, printer.Port ?? 9100);

            using var stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            return PrintResult.Success();
        }
        catch (SocketException ex)
        {
            return PrintResult.Failed($"Network error: {ex.Message}");
        }
    }

    private async Task<PrintResult> SendUsbAsync(Printer printer, byte[] data)
    {
        // Use RawPrinterHelper for direct USB printing
        if (string.IsNullOrEmpty(printer.WindowsPrinterName))
        {
            return PrintResult.Failed("Printer name not configured");
        }

        return await Task.Run(() =>
        {
            try
            {
                if (RawPrinterHelper.SendBytesToPrinter(printer.WindowsPrinterName, data))
                {
                    return PrintResult.Success();
                }
                return PrintResult.Failed("Failed to send data to printer");
            }
            catch (Exception ex)
            {
                return PrintResult.Failed(ex.Message);
            }
        });
    }

    private async Task<PrintResult> SendSerialAsync(Printer printer, byte[] data)
    {
        if (string.IsNullOrEmpty(printer.PortName))
        {
            return PrintResult.Failed("Serial port not configured");
        }

        return await Task.Run(() =>
        {
            try
            {
                using var port = new SerialPort(printer.PortName, 9600);
                port.Open();
                port.Write(data, 0, data.Length);
                port.Close();

                return PrintResult.Success();
            }
            catch (Exception ex)
            {
                return PrintResult.Failed($"Serial port error: {ex.Message}");
            }
        });
    }

    private async Task<PrintResult> SendWindowsAsync(Printer printer, byte[] data)
    {
        return await SendUsbAsync(printer, data);  // Same as USB
    }

    public async Task<PrinterStatus> GetStatusAsync(Printer printer)
    {
        try
        {
            var result = await SendAsync(printer, EscPosCommands.Initialize);
            return result.Success ? PrinterStatus.Online : PrinterStatus.Offline;
        }
        catch
        {
            return PrinterStatus.Offline;
        }
    }
}

public class PrintResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public static PrintResult Succeeded() => new() { Success = true };
    public static PrintResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}
```

### Raw Printer Helper (Windows)

```csharp
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private class DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true)]
    private static extern int StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

    public static bool SendBytesToPrinter(string printerName, byte[] bytes)
    {
        var docInfo = new DOCINFO
        {
            pDocName = "ESC/POS Print Job",
            pDataType = "RAW"
        };

        if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            return false;

        try
        {
            if (StartDocPrinter(hPrinter, 1, docInfo) == 0)
                return false;

            if (!StartPagePrinter(hPrinter))
                return false;

            if (!WritePrinter(hPrinter, bytes, bytes.Length, out _))
                return false;

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);

            return true;
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }
}
```

### Print Queue Manager

```csharp
public interface IPrintQueueManager
{
    Task<int> EnqueueAsync(PrintJob job);
    Task<bool> CancelAsync(int jobId);
    Task<bool> RetryAsync(int jobId);
    Task ClearQueueAsync();
    Task<List<PrintJob>> GetQueueAsync();
    event EventHandler<PrintJobEventArgs>? JobCompleted;
    event EventHandler<PrintJobEventArgs>? JobFailed;
}

public class PrintQueueManager : IPrintQueueManager, IDisposable
{
    private readonly ConcurrentQueue<PrintJob> _queue = new();
    private readonly ConcurrentDictionary<int, PrintJob> _jobs = new();
    private readonly IPrinterCommunicationService _printerService;
    private readonly ILogger<PrintQueueManager> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private int _nextJobId = 1;

    public event EventHandler<PrintJobEventArgs>? JobCompleted;
    public event EventHandler<PrintJobEventArgs>? JobFailed;

    public PrintQueueManager(
        IPrinterCommunicationService printerService,
        ILogger<PrintQueueManager> logger)
    {
        _printerService = printerService;
        _logger = logger;

        // Start background processing
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    public Task<int> EnqueueAsync(PrintJob job)
    {
        job.Id = Interlocked.Increment(ref _nextJobId);
        job.Status = PrintJobStatus.Queued;
        job.QueuedAt = DateTime.Now;

        _jobs[job.Id] = job;
        _queue.Enqueue(job);

        _logger.LogInformation("Print job {JobId} queued for {PrinterName}",
            job.Id, job.Printer.Name);

        return Task.FromResult(job.Id);
    }

    public Task<bool> CancelAsync(int jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.Status == PrintJobStatus.Queued)
            {
                job.Status = PrintJobStatus.Cancelled;
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public async Task<bool> RetryAsync(int jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.Status == PrintJobStatus.Failed)
            {
                job.Status = PrintJobStatus.Queued;
                job.RetryCount++;
                job.QueuedAt = DateTime.Now;
                _queue.Enqueue(job);
                return true;
            }
        }
        return false;
    }

    public Task ClearQueueAsync()
    {
        while (_queue.TryDequeue(out var job))
        {
            job.Status = PrintJobStatus.Cancelled;
        }
        return Task.CompletedTask;
    }

    public Task<List<PrintJob>> GetQueueAsync()
    {
        return Task.FromResult(_jobs.Values
            .Where(j => j.Status == PrintJobStatus.Queued ||
                       j.Status == PrintJobStatus.Processing)
            .OrderBy(j => j.QueuedAt)
            .ToList());
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_queue.TryDequeue(out var job))
                {
                    if (job.Status == PrintJobStatus.Cancelled)
                        continue;

                    await ProcessJobAsync(job);
                }
                else
                {
                    await Task.Delay(100, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing print queue");
            }
        }
    }

    private async Task ProcessJobAsync(PrintJob job)
    {
        job.Status = PrintJobStatus.Processing;
        job.StartedAt = DateTime.Now;

        _logger.LogInformation("Processing print job {JobId}", job.Id);

        try
        {
            var result = await _printerService.SendAsync(job.Printer, job.Data);

            if (result.Success)
            {
                job.Status = PrintJobStatus.Completed;
                job.CompletedAt = DateTime.Now;
                JobCompleted?.Invoke(this, new PrintJobEventArgs(job));
            }
            else
            {
                HandleJobFailure(job, result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            HandleJobFailure(job, ex.Message);
        }
    }

    private void HandleJobFailure(PrintJob job, string error)
    {
        job.LastError = error;

        if (job.RetryCount < job.MaxRetries)
        {
            job.RetryCount++;
            job.Status = PrintJobStatus.Queued;
            _queue.Enqueue(job);
            _logger.LogWarning("Print job {JobId} failed, retrying ({Retry}/{Max})",
                job.Id, job.RetryCount, job.MaxRetries);
        }
        else
        {
            job.Status = PrintJobStatus.Failed;
            _logger.LogError("Print job {JobId} failed permanently: {Error}",
                job.Id, error);
            JobFailed?.Invoke(this, new PrintJobEventArgs(job));
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _processingTask.Wait(TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}

public class PrintJob
{
    public int Id { get; set; }
    public Printer Printer { get; set; } = null!;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public PrintJobType Type { get; set; }
    public PrintJobStatus Status { get; set; }
    public PrintJobPriority Priority { get; set; } = PrintJobPriority.Normal;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Reference { get; set; }  // e.g., Receipt number
}

public enum PrintJobType
{
    Receipt,
    KOT,
    Report,
    TestPage
}

public enum PrintJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum PrintJobPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public class PrintJobEventArgs : EventArgs
{
    public PrintJob Job { get; }
    public PrintJobEventArgs(PrintJob job) => Job = job;
}
```

### Image Converter for Logo Printing

```csharp
public interface IImageConverter
{
    byte[] ConvertToEscPosRaster(byte[] imageData, int maxWidth = 384);
    byte[] ConvertBitmapToRaster(Bitmap bitmap);
}

public class ImageConverter : IImageConverter
{
    public byte[] ConvertToEscPosRaster(byte[] imageData, int maxWidth = 384)
    {
        using var ms = new MemoryStream(imageData);
        using var image = Image.FromStream(ms);
        using var bitmap = new Bitmap(image);

        // Scale if needed
        if (bitmap.Width > maxWidth)
        {
            var ratio = (double)maxWidth / bitmap.Width;
            var newHeight = (int)(bitmap.Height * ratio);
            using var scaled = new Bitmap(bitmap, maxWidth, newHeight);
            return ConvertBitmapToRaster(scaled);
        }

        return ConvertBitmapToRaster(bitmap);
    }

    public byte[] ConvertBitmapToRaster(Bitmap bitmap)
    {
        // Ensure width is multiple of 8
        var width = (bitmap.Width + 7) / 8 * 8;
        var height = bitmap.Height;

        var result = new List<byte>();

        // Add raster bit image command
        result.AddRange(EscPosCommands.RasterBitImage(width / 8, height));

        // Convert pixels to bits
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x += 8)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if (x + bit < bitmap.Width)
                    {
                        var pixel = bitmap.GetPixel(x + bit, y);
                        var luminance = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                        if (luminance < 128)
                        {
                            b |= (byte)(0x80 >> bit);
                        }
                    }
                }
                result.Add(b);
            }
        }

        return result.ToArray();
    }
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.10.3-ESC/POS-Implementation]
- [Source: docs/PRD_Hospitality_POS_System.md#PR-021 to PR-030]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5

### Completion Notes List
- Implemented comprehensive ESC/POS command library with all standard thermal printer commands
- Created fluent EscPosPrintDocument builder with receipt and KOT templates
- Implemented PrinterCommunicationService using Windows winspool.drv P/Invoke for raw printing
- Created PrintQueueManager with priority queue, retry logic, and event notifications
- Implemented ImageConverter with Floyd-Steinberg, Atkinson, and ordered dithering algorithms
- Added print models for jobs, status, queue stats, and image conversion results

### File List
- src/HospitalityPOS.Core/Printing/EscPosCommands.cs
- src/HospitalityPOS.Core/Printing/EscPosPrintDocument.cs
- src/HospitalityPOS.Core/Printing/PrintModels.cs
- src/HospitalityPOS.Core/Interfaces/IPrinterCommunicationService.cs
- src/HospitalityPOS.Core/Interfaces/IPrintQueueManager.cs
- src/HospitalityPOS.Core/Interfaces/IImageConverter.cs
- src/HospitalityPOS.Infrastructure/Printing/PrinterCommunicationService.cs
- src/HospitalityPOS.Infrastructure/Printing/PrintQueueManager.cs
- src/HospitalityPOS.Infrastructure/Printing/ImageConverter.cs
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
