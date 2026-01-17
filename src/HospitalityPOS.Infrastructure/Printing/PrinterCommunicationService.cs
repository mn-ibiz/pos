using System.Runtime.InteropServices;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Printing;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Printing;

/// <summary>
/// Service for low-level printer communication using Windows API.
/// </summary>
public class PrinterCommunicationService : IPrinterCommunicationService
{
    private readonly ILogger<PrinterCommunicationService> _logger;

    public PrinterCommunicationService(ILogger<PrinterCommunicationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendRawDataAsync(string printerName, byte[] data, string documentName = "POS Document")
    {
        return await Task.Run(() => SendRawData(printerName, data, documentName));
    }

    /// <inheritdoc />
    public bool SendRawData(string printerName, byte[] data, string documentName = "POS Document")
    {
        if (string.IsNullOrEmpty(printerName))
        {
            _logger.LogError("Printer name is null or empty");
            return false;
        }

        if (data == null || data.Length == 0)
        {
            _logger.LogWarning("No data to send to printer {PrinterName}", printerName);
            return true;
        }

        try
        {
            return RawPrinterHelper.SendBytesToPrinter(printerName, data, documentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data to printer {PrinterName}", printerName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<PrinterStatus> GetPrinterStatusAsync(string printerName)
    {
        return await Task.Run(() => GetPrinterStatus(printerName));
    }

    /// <inheritdoc />
    public PrinterStatus GetPrinterStatus(string printerName)
    {
        var status = new PrinterStatus { PrinterName = printerName };

        try
        {
            // Open printer to get status
            if (!RawPrinterHelper.OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                status.IsOnline = false;
                status.HasError = true;
                status.ErrorDescription = "Cannot open printer";
                return status;
            }

            try
            {
                // Get printer info
                var info = GetPrinterInfo(hPrinter);
                if (info.HasValue)
                {
                    var printerInfo = info.Value;
                    status.RawStatus = (int)printerInfo.Status;
                    status.JobsInQueue = (int)printerInfo.cJobs;

                    // Parse status flags
                    status.IsOnline = (printerInfo.Status & PrinterStatusFlags.Offline) == 0;
                    status.HasPaper = (printerInfo.Status & PrinterStatusFlags.PaperOut) == 0;
                    status.IsCoverOpen = (printerInfo.Status & PrinterStatusFlags.DoorOpen) != 0;
                    status.HasPaperJam = (printerInfo.Status & PrinterStatusFlags.PaperJam) != 0;

                    status.HasError = (printerInfo.Status & (
                        PrinterStatusFlags.Error |
                        PrinterStatusFlags.NotAvailable |
                        PrinterStatusFlags.NoToner)) != 0;

                    status.IsReady = status.IsOnline && !status.HasError && status.HasPaper && !status.IsCoverOpen;

                    // Build error description
                    if (status.HasError)
                    {
                        var errors = new List<string>();
                        if ((printerInfo.Status & PrinterStatusFlags.Error) != 0) errors.Add("Error");
                        if ((printerInfo.Status & PrinterStatusFlags.NotAvailable) != 0) errors.Add("Not Available");
                        if ((printerInfo.Status & PrinterStatusFlags.NoToner) != 0) errors.Add("No Toner");
                        status.ErrorDescription = string.Join(", ", errors);
                    }
                }
                else
                {
                    // Assume online if we can't get status
                    status.IsOnline = true;
                    status.IsReady = true;
                }
            }
            finally
            {
                RawPrinterHelper.ClosePrinter(hPrinter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting printer status for {PrinterName}", printerName);
            status.HasError = true;
            status.ErrorDescription = ex.Message;
        }

        return status;
    }

    /// <inheritdoc />
    public async Task<bool> IsPrinterReadyAsync(string printerName)
    {
        var status = await GetPrinterStatusAsync(printerName);
        return status.IsReady;
    }

    /// <inheritdoc />
    public async Task<PrintResult> SendTestPrintAsync(string printerName)
    {
        var jobId = Guid.NewGuid();

        try
        {
            var doc = EscPosPrintDocument.Create80mm()
                .AlignCenter()
                .DoubleSize().Bold()
                .TextLine("PRINTER TEST")
                .NoBold().NormalSize()
                .EmptyLine()
                .TextLine("Hospitality POS")
                .TextLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
                .EmptyLine()
                .AlignLeft()
                .Separator()
                .TextLine("Normal text")
                .Bold().TextLine("Bold text").NoBold()
                .Underline().TextLine("Underlined text").NoUnderline()
                .DoubleHeight().TextLine("Double height").NormalSize()
                .DoubleWidth().TextLine("Double width").NormalSize()
                .DoubleSize().TextLine("Double size").NormalSize()
                .Separator()
                .AlignCenter()
                .TextLine("Test completed successfully!")
                .EmptyLine()
                .FeedAndPartialCut();

            var success = await SendRawDataAsync(printerName, doc.Build(), "Test Print");

            if (success)
            {
                _logger.LogInformation("Test print successful on {PrinterName}", printerName);
                return PrintResult.Succeeded(jobId);
            }
            else
            {
                _logger.LogWarning("Test print failed on {PrinterName}", printerName);
                return PrintResult.Failed(jobId, "Failed to send data to printer");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test print error on {PrinterName}", printerName);
            return PrintResult.Failed(jobId, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenCashDrawerAsync(string printerName, int pin = 0)
    {
        var command = pin == 0 ? EscPosCommands.OpenCashDrawer : EscPosCommands.OpenCashDrawer2;
        return await SendRawDataAsync(printerName, command, "Cash Drawer");
    }

    /// <inheritdoc />
    public async Task<bool> BeepAsync(string printerName, int times = 1, int duration = 2)
    {
        var command = EscPosCommands.Beep(times, duration);
        return await SendRawDataAsync(printerName, command, "Beep");
    }

    private PRINTER_INFO_2? GetPrinterInfo(IntPtr hPrinter)
    {
        int needed = 0;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, ref needed);

        if (needed <= 0)
            return null;

        IntPtr buffer = Marshal.AllocHGlobal(needed);
        try
        {
            if (GetPrinter(hPrinter, 2, buffer, needed, ref needed))
            {
                return Marshal.PtrToStructure<PRINTER_INFO_2>(buffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return null;
    }

    #region Native Interop

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetPrinter(IntPtr hPrinter, int Level, IntPtr pPrinter, int cbBuf, ref int pcbNeeded);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PRINTER_INFO_2
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public PrinterStatusFlags Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [Flags]
    private enum PrinterStatusFlags : uint
    {
        Paused = 0x00000001,
        Error = 0x00000002,
        PendingDeletion = 0x00000004,
        PaperJam = 0x00000008,
        PaperOut = 0x00000010,
        ManualFeed = 0x00000020,
        PaperProblem = 0x00000040,
        Offline = 0x00000080,
        IOActive = 0x00000100,
        Busy = 0x00000200,
        Printing = 0x00000400,
        OutputBinFull = 0x00000800,
        NotAvailable = 0x00001000,
        Waiting = 0x00002000,
        Processing = 0x00004000,
        Initializing = 0x00008000,
        WarmingUp = 0x00010000,
        TonerLow = 0x00020000,
        NoToner = 0x00040000,
        PagePunt = 0x00080000,
        UserIntervention = 0x00100000,
        OutOfMemory = 0x00200000,
        DoorOpen = 0x00400000,
        ServerUnknown = 0x00800000,
        PowerSave = 0x01000000
    }

    #endregion
}

/// <summary>
/// Helper class for sending raw data to Windows printers.
/// </summary>
internal static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDataType;
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>
    /// Sends raw bytes to a printer.
    /// </summary>
    public static bool SendBytesToPrinter(string printerName, byte[] bytes, string documentName = "POS Document")
    {
        IntPtr hPrinter = IntPtr.Zero;
        IntPtr pBytes = IntPtr.Zero;
        bool success = false;

        try
        {
            // Open printer
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                throw new Exception($"Cannot open printer: {printerName}. Error: {Marshal.GetLastWin32Error()}");
            }

            // Start document
            var docInfo = new DOCINFO
            {
                pDocName = documentName,
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                throw new Exception($"StartDocPrinter failed. Error: {Marshal.GetLastWin32Error()}");
            }

            try
            {
                // Start page
                if (!StartPagePrinter(hPrinter))
                {
                    throw new Exception($"StartPagePrinter failed. Error: {Marshal.GetLastWin32Error()}");
                }

                try
                {
                    // Allocate unmanaged memory and copy bytes
                    pBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, pBytes, bytes.Length);

                    // Write data
                    if (!WritePrinter(hPrinter, pBytes, bytes.Length, out int written))
                    {
                        throw new Exception($"WritePrinter failed. Error: {Marshal.GetLastWin32Error()}");
                    }

                    success = written == bytes.Length;
                }
                finally
                {
                    EndPagePrinter(hPrinter);
                }
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            if (pBytes != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }

        return success;
    }
}
