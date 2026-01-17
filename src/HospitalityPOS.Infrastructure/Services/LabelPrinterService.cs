using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for label printer management and operations.
/// </summary>
public class LabelPrinterService : ILabelPrinterService
{
    private readonly IRepository<LabelPrinter> _printerRepository;
    private readonly IRepository<LabelSize> _sizeRepository;
    private readonly IRepository<CategoryPrinterAssignment> _assignmentRepository;
    private readonly IRepository<LabelPrintJob> _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LabelPrinterService> _logger;

    public event EventHandler<LabelPrinterDto>? PrinterStatusChanged;
    public event EventHandler<LabelPrinterDto>? PrinterConnected;
    public event EventHandler<LabelPrinterDto>? PrinterDisconnected;

    public LabelPrinterService(
        IRepository<LabelPrinter> printerRepository,
        IRepository<LabelSize> sizeRepository,
        IRepository<CategoryPrinterAssignment> assignmentRepository,
        IRepository<LabelPrintJob> jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<LabelPrinterService> logger)
    {
        _printerRepository = printerRepository ?? throw new ArgumentNullException(nameof(printerRepository));
        _sizeRepository = sizeRepository ?? throw new ArgumentNullException(nameof(sizeRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Printer CRUD

    public async Task<LabelPrinterDto> CreatePrinterAsync(CreateLabelPrinterDto dto)
    {
        var printer = new LabelPrinter
        {
            Name = dto.Name,
            ConnectionString = dto.ConnectionString,
            StoreId = dto.StoreId,
            PrinterType = (LabelPrinterType)dto.PrinterType,
            PrintLanguage = (LabelPrintLanguage)dto.PrintLanguage,
            DefaultLabelSizeId = dto.DefaultLabelSizeId,
            IsDefault = dto.IsDefault,
            BaudRate = dto.BaudRate,
            DataBits = dto.DataBits,
            Port = dto.Port,
            TimeoutMs = dto.TimeoutMs ?? 5000,
            Status = LabelPrinterStatus.Offline,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.IsDefault)
        {
            await ClearDefaultPrinterAsync(dto.StoreId);
        }

        await _printerRepository.AddAsync(printer);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created label printer {PrinterId} - {Name}", printer.Id, printer.Name);

        return await GetPrinterAsync(printer.Id) ?? throw new InvalidOperationException("Failed to retrieve created printer");
    }

    public async Task<LabelPrinterDto?> GetPrinterAsync(int printerId)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null || !printer.IsActive) return null;

        var assignments = await _assignmentRepository.FindAsync(a => a.LabelPrinterId == printerId && a.IsActive);

        return MapToDto(printer, assignments.Count);
    }

    public async Task<List<LabelPrinterDto>> GetAllPrintersAsync(int storeId)
    {
        var printers = await _printerRepository.FindAsync(p => p.StoreId == storeId && p.IsActive);
        var result = new List<LabelPrinterDto>();

        foreach (var printer in printers.OrderBy(p => p.Name))
        {
            var assignments = await _assignmentRepository.FindAsync(a => a.LabelPrinterId == printer.Id && a.IsActive);
            result.Add(MapToDto(printer, assignments.Count));
        }

        return result;
    }

    public async Task<LabelPrinterDto> UpdatePrinterAsync(int printerId, UpdateLabelPrinterDto dto)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null || !printer.IsActive)
            throw new KeyNotFoundException($"Printer {printerId} not found");

        if (dto.Name != null) printer.Name = dto.Name;
        if (dto.ConnectionString != null) printer.ConnectionString = dto.ConnectionString;
        if (dto.PrinterType.HasValue) printer.PrinterType = (LabelPrinterType)dto.PrinterType.Value;
        if (dto.PrintLanguage.HasValue) printer.PrintLanguage = (LabelPrintLanguage)dto.PrintLanguage.Value;
        if (dto.DefaultLabelSizeId.HasValue) printer.DefaultLabelSizeId = dto.DefaultLabelSizeId;
        if (dto.BaudRate.HasValue) printer.BaudRate = dto.BaudRate;
        if (dto.Port.HasValue) printer.Port = dto.Port;
        if (dto.TimeoutMs.HasValue) printer.TimeoutMs = dto.TimeoutMs;

        if (dto.IsDefault.HasValue && dto.IsDefault.Value && !printer.IsDefault)
        {
            await ClearDefaultPrinterAsync(printer.StoreId);
            printer.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue && !dto.IsDefault.Value)
        {
            printer.IsDefault = false;
        }

        printer.UpdatedAt = DateTime.UtcNow;
        await _printerRepository.UpdateAsync(printer);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated label printer {PrinterId}", printerId);

        return await GetPrinterAsync(printerId) ?? throw new InvalidOperationException("Failed to retrieve updated printer");
    }

    public async Task<bool> DeletePrinterAsync(int printerId)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null) return false;

        // Check for active jobs
        var activeJobs = await _jobRepository.FindAsync(j => j.PrinterId == printerId &&
            (j.Status == LabelPrintJobStatus.Pending || j.Status == LabelPrintJobStatus.InProgress));
        if (activeJobs.Any())
            throw new InvalidOperationException("Cannot delete printer with active print jobs");

        printer.IsActive = false;
        printer.UpdatedAt = DateTime.UtcNow;
        await _printerRepository.UpdateAsync(printer);

        // Deactivate category assignments
        var assignments = await _assignmentRepository.FindAsync(a => a.LabelPrinterId == printerId && a.IsActive);
        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _assignmentRepository.UpdateAsync(assignment);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Deleted label printer {PrinterId}", printerId);

        return true;
    }

    #endregion

    #region Printer Operations

    public async Task<PrinterConnectionTestResultDto> TestPrinterConnectionAsync(int printerId)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null)
            return new PrinterConnectionTestResultDto { Success = false, Message = "Printer not found" };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            bool success = printer.PrinterType switch
            {
                LabelPrinterType.Serial => await TestSerialConnectionAsync(printer),
                LabelPrinterType.Network => await TestNetworkConnectionAsync(printer),
                LabelPrinterType.USB => await TestUsbConnectionAsync(printer),
                LabelPrinterType.Windows => await TestWindowsPrinterAsync(printer),
                _ => false
            };

            stopwatch.Stop();

            if (success)
            {
                printer.Status = LabelPrinterStatus.Online;
                printer.LastConnectedAt = DateTime.UtcNow;
                printer.LastErrorMessage = null;
                await _printerRepository.UpdateAsync(printer);
                await _unitOfWork.SaveChangesAsync();

                PrinterConnected?.Invoke(this, MapToDto(printer, 0));

                return new PrinterConnectionTestResultDto
                {
                    Success = true,
                    Message = "Connection successful",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    PrinterInfo = $"{printer.PrinterType} - {printer.PrintLanguage}"
                };
            }

            return new PrinterConnectionTestResultDto
            {
                Success = false,
                Message = "Connection failed",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Printer connection test failed for {PrinterId}", printerId);

            printer.Status = LabelPrinterStatus.Error;
            printer.LastErrorMessage = ex.Message;
            await _printerRepository.UpdateAsync(printer);
            await _unitOfWork.SaveChangesAsync();

            return new PrinterConnectionTestResultDto
            {
                Success = false,
                Message = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<TestLabelResultDto> PrintTestLabelAsync(int printerId)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null)
            return new TestLabelResultDto { Success = false, Message = "Printer not found" };

        try
        {
            var testLabel = GenerateTestLabel(printer);
            var success = await SendToPrinterAsync(printerId, testLabel);

            return new TestLabelResultDto
            {
                Success = success,
                Message = success ? "Test label printed successfully" : "Failed to print test label",
                LabelContent = testLabel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test label print failed for {PrinterId}", printerId);
            return new TestLabelResultDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task SetDefaultPrinterAsync(int printerId, int storeId)
    {
        await ClearDefaultPrinterAsync(storeId);

        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null || !printer.IsActive)
            throw new KeyNotFoundException($"Printer {printerId} not found");

        printer.IsDefault = true;
        printer.UpdatedAt = DateTime.UtcNow;
        await _printerRepository.UpdateAsync(printer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<LabelPrinterDto?> GetDefaultPrinterAsync(int storeId)
    {
        var printers = await _printerRepository.FindAsync(p => p.StoreId == storeId && p.IsDefault && p.IsActive);
        var printer = printers.FirstOrDefault();
        if (printer == null) return null;

        return MapToDto(printer, 0);
    }

    public async Task<LabelPrinterDto?> GetPrinterForCategoryAsync(int categoryId, int storeId)
    {
        var assignments = await _assignmentRepository.FindAsync(a =>
            a.CategoryId == categoryId && a.StoreId == storeId && a.IsActive);
        var assignment = assignments.FirstOrDefault();

        if (assignment != null)
        {
            return await GetPrinterAsync(assignment.LabelPrinterId);
        }

        // Fall back to default printer
        return await GetDefaultPrinterAsync(storeId);
    }

    public async Task<bool> SendToPrinterAsync(int printerId, string content)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId);
        if (printer == null)
            throw new KeyNotFoundException($"Printer {printerId} not found");

        try
        {
            return printer.PrinterType switch
            {
                LabelPrinterType.Serial => await SendToSerialPrinterAsync(printer, content),
                LabelPrinterType.Network => await SendToNetworkPrinterAsync(printer, content),
                LabelPrinterType.USB => await SendToUsbPrinterAsync(printer, content),
                LabelPrinterType.Windows => await SendToWindowsPrinterAsync(printer, content),
                _ => throw new NotSupportedException($"Printer type {printer.PrinterType} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send to printer {PrinterId}", printerId);
            printer.Status = LabelPrinterStatus.Error;
            printer.LastErrorMessage = ex.Message;
            await _printerRepository.UpdateAsync(printer);
            await _unitOfWork.SaveChangesAsync();
            throw;
        }
    }

    #endregion

    #region Label Sizes

    public async Task<LabelSizeDto> CreateLabelSizeAsync(CreateLabelSizeDto dto)
    {
        var size = new LabelSize
        {
            Name = dto.Name,
            WidthMm = dto.WidthMm,
            HeightMm = dto.HeightMm,
            DotsPerMm = dto.DotsPerMm,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _sizeRepository.AddAsync(size);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(size);
    }

    public async Task<List<LabelSizeDto>> GetAllLabelSizesAsync()
    {
        var sizes = await _sizeRepository.FindAsync(s => s.IsActive);
        return sizes.OrderBy(s => s.Name).Select(MapToDto).ToList();
    }

    public async Task<LabelSizeDto> UpdateLabelSizeAsync(int sizeId, UpdateLabelSizeDto dto)
    {
        var size = await _sizeRepository.GetByIdAsync(sizeId);
        if (size == null || !size.IsActive)
            throw new KeyNotFoundException($"Label size {sizeId} not found");

        if (dto.Name != null) size.Name = dto.Name;
        if (dto.WidthMm.HasValue) size.WidthMm = dto.WidthMm.Value;
        if (dto.HeightMm.HasValue) size.HeightMm = dto.HeightMm.Value;
        if (dto.DotsPerMm.HasValue) size.DotsPerMm = dto.DotsPerMm.Value;
        if (dto.Description != null) size.Description = dto.Description;

        size.UpdatedAt = DateTime.UtcNow;
        await _sizeRepository.UpdateAsync(size);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(size);
    }

    public async Task<bool> DeleteLabelSizeAsync(int sizeId)
    {
        var size = await _sizeRepository.GetByIdAsync(sizeId);
        if (size == null) return false;

        size.IsActive = false;
        size.UpdatedAt = DateTime.UtcNow;
        await _sizeRepository.UpdateAsync(size);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Category Assignments

    public async Task<CategoryPrinterAssignmentDto> AssignCategoryPrinterAsync(AssignCategoryPrinterDto dto, int storeId)
    {
        // Remove existing assignment
        var existingAssignments = await _assignmentRepository.FindAsync(a =>
            a.CategoryId == dto.CategoryId && a.StoreId == storeId && a.IsActive);

        foreach (var existing in existingAssignments)
        {
            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;
            await _assignmentRepository.UpdateAsync(existing);
        }

        var assignment = new CategoryPrinterAssignment
        {
            CategoryId = dto.CategoryId,
            LabelPrinterId = dto.LabelPrinterId,
            LabelTemplateId = dto.LabelTemplateId,
            StoreId = storeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _assignmentRepository.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return await GetAssignmentDtoAsync(assignment);
    }

    public async Task<List<CategoryPrinterAssignmentDto>> GetCategoryAssignmentsAsync(int storeId)
    {
        var assignments = await _assignmentRepository.FindAsync(a => a.StoreId == storeId && a.IsActive);
        var result = new List<CategoryPrinterAssignmentDto>();

        foreach (var assignment in assignments)
        {
            result.Add(await GetAssignmentDtoAsync(assignment));
        }

        return result;
    }

    public async Task<bool> RemoveCategoryAssignmentAsync(int assignmentId)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null) return false;

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepository.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Statistics

    public async Task<Dictionary<int, int>> GetPrinterUsageAsync(int storeId, DateTime from, DateTime to)
    {
        var jobs = await _jobRepository.FindAsync(j =>
            j.StoreId == storeId &&
            j.StartedAt >= from &&
            j.StartedAt <= to);

        return jobs
            .GroupBy(j => j.PrinterId)
            .ToDictionary(g => g.Key, g => g.Sum(j => j.PrintedLabels));
    }

    #endregion

    #region Private Methods

    private async Task ClearDefaultPrinterAsync(int storeId)
    {
        var printers = await _printerRepository.FindAsync(p => p.StoreId == storeId && p.IsDefault);
        foreach (var printer in printers)
        {
            printer.IsDefault = false;
            printer.UpdatedAt = DateTime.UtcNow;
            await _printerRepository.UpdateAsync(printer);
        }
    }

    private async Task<bool> TestSerialConnectionAsync(LabelPrinter printer)
    {
        try
        {
            // Parse connection string for COM port (e.g., "COM3" or "COM3:9600")
            var parts = printer.ConnectionString.Split(':');
            var portName = parts[0];
            var baudRate = parts.Length > 1 && int.TryParse(parts[1], out var rate) ? rate : printer.BaudRate ?? 9600;

            // Use System.IO.Ports.SerialPort to test connection
            // Note: In production, this requires System.IO.Ports NuGet package
            _logger.LogDebug("Testing serial connection to {PortName} at {BaudRate} baud", portName, baudRate);

            // Check if port exists by attempting to get port names
            var availablePorts = System.IO.Ports.SerialPort.GetPortNames();
            if (!availablePorts.Contains(portName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Serial port {PortName} not found. Available ports: {Ports}",
                    portName, string.Join(", ", availablePorts));
                return false;
            }

            // Attempt to open the port briefly
            using var serialPort = new System.IO.Ports.SerialPort(portName, baudRate);
            serialPort.ReadTimeout = printer.TimeoutMs ?? 3000;
            serialPort.WriteTimeout = printer.TimeoutMs ?? 3000;

            await Task.Run(() => serialPort.Open());
            var isOpen = serialPort.IsOpen;
            serialPort.Close();

            _logger.LogInformation("Serial port {PortName} test result: {Result}", portName, isOpen);
            return isOpen;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Serial port {Port} is in use by another application", printer.ConnectionString);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test serial connection to {Port}", printer.ConnectionString);
            return false;
        }
    }

    private async Task<bool> TestNetworkConnectionAsync(LabelPrinter printer)
    {
        try
        {
            // Parse connection string for IP and port
            var parts = printer.ConnectionString.Split(':');
            if (parts.Length < 1) return false;

            var host = parts[0];
            var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : printer.Port ?? 9100;

            _logger.LogDebug("Testing network connection to {Host}:{Port}", host, port);

            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(printer.TimeoutMs ?? 5000);

            try
            {
                await client.ConnectAsync(host, port, cts.Token);
                var connected = client.Connected;
                _logger.LogInformation("Network printer {Host}:{Port} test result: {Result}", host, port, connected);
                return connected;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Network connection to {Host}:{Port} timed out", host, port);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test network connection to {Connection}", printer.ConnectionString);
            return false;
        }
    }

    private async Task<bool> TestUsbConnectionAsync(LabelPrinter printer)
    {
        try
        {
            // USB printers are typically accessed via Windows print spooler
            // or through USB HID/LibUSB for raw access
            // This implementation checks if the printer is registered in Windows
            _logger.LogDebug("Testing USB printer: {PrinterName}", printer.ConnectionString);

            // Use Windows API to check if printer exists
            return await Task.Run(() =>
            {
                try
                {
                    // Try to find the printer in the installed printers list
                    var installedPrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                    foreach (string printerName in installedPrinters)
                    {
                        if (printerName.Equals(printer.ConnectionString, StringComparison.OrdinalIgnoreCase) ||
                            printerName.Contains(printer.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("USB printer {PrinterName} found in system", printerName);
                            return true;
                        }
                    }

                    // Also check if it matches the printer name
                    foreach (string printerName in installedPrinters)
                    {
                        if (printerName.Contains("USB", StringComparison.OrdinalIgnoreCase) &&
                            (printerName.Contains(printer.Name, StringComparison.OrdinalIgnoreCase) ||
                             printer.Name.Contains(printerName, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogInformation("USB printer matched: {PrinterName}", printerName);
                            return true;
                        }
                    }

                    _logger.LogWarning("USB printer {PrinterName} not found in system", printer.ConnectionString);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking USB printer availability");
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test USB connection for {Printer}", printer.Name);
            return false;
        }
    }

    private async Task<bool> TestWindowsPrinterAsync(LabelPrinter printer)
    {
        try
        {
            _logger.LogDebug("Testing Windows printer: {PrinterName}", printer.ConnectionString);

            return await Task.Run(() =>
            {
                try
                {
                    // Check if the printer is in the installed printers list
                    var installedPrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                    foreach (string printerName in installedPrinters)
                    {
                        if (printerName.Equals(printer.ConnectionString, StringComparison.OrdinalIgnoreCase))
                        {
                            // Further check if the printer is ready
                            var settings = new System.Drawing.Printing.PrinterSettings
                            {
                                PrinterName = printerName
                            };

                            if (settings.IsValid)
                            {
                                _logger.LogInformation("Windows printer {PrinterName} is valid and ready", printerName);
                                return true;
                            }
                        }
                    }

                    _logger.LogWarning("Windows printer {PrinterName} not found or not valid", printer.ConnectionString);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking Windows printer availability");
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Windows printer {Printer}", printer.Name);
            return false;
        }
    }

    private async Task<bool> SendToSerialPrinterAsync(LabelPrinter printer, string content)
    {
        // In a real implementation, this would send to serial port
        _logger.LogDebug("Sending to serial printer: {Content}", content);
        await Task.Delay(50);
        return true;
    }

    private async Task<bool> SendToNetworkPrinterAsync(LabelPrinter printer, string content)
    {
        var parts = printer.ConnectionString.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 ? int.Parse(parts[1]) : printer.Port ?? 9100;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        var data = Encoding.ASCII.GetBytes(content);
        await client.GetStream().WriteAsync(data);
        return true;
    }

    private async Task<bool> SendToUsbPrinterAsync(LabelPrinter printer, string content)
    {
        // In a real implementation, this would send to USB printer
        _logger.LogDebug("Sending to USB printer: {Content}", content);
        await Task.Delay(50);
        return true;
    }

    private async Task<bool> SendToWindowsPrinterAsync(LabelPrinter printer, string content)
    {
        // In a real implementation, this would send via Windows print spooler
        _logger.LogDebug("Sending to Windows printer: {Content}", content);
        await Task.Delay(50);
        return true;
    }

    private string GenerateTestLabel(LabelPrinter printer)
    {
        return printer.PrintLanguage switch
        {
            LabelPrintLanguage.ZPL => GenerateZplTestLabel(),
            LabelPrintLanguage.EPL => GenerateEplTestLabel(),
            LabelPrintLanguage.TSPL => GenerateTsplTestLabel(),
            LabelPrintLanguage.Raw => GenerateRawTestLabel(),
            _ => "TEST LABEL"
        };
    }

    private string GenerateZplTestLabel()
    {
        return @"^XA
^FO50,50^A0N,50,50^FDTEST LABEL^FS
^FO50,120^A0N,30,30^FDPrinter Connected^FS
^FO50,170^A0N,25,25^FD" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"^FS
^XZ";
    }

    private string GenerateEplTestLabel()
    {
        return @"N
A50,50,0,4,1,1,N,""TEST LABEL""
A50,100,0,3,1,1,N,""Printer Connected""
A50,130,0,2,1,1,N,""" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"""
P1";
    }

    private string GenerateTsplTestLabel()
    {
        // TSPL (TSC Printer Language) commands
        return @"SIZE 50 mm, 30 mm
GAP 3 mm, 0 mm
DIRECTION 1
CLS
TEXT 50,50,""ROMAN.TTF"",0,12,12,""TEST LABEL""
TEXT 50,100,""ROMAN.TTF"",0,10,10,""Printer Connected""
TEXT 50,140,""ROMAN.TTF"",0,8,8,""" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"""
PRINT 1
";
    }

    private string GenerateRawTestLabel()
    {
        // Simple ESC/POS style raw text
        return "\x1b\x40" +              // Initialize printer
               "\x1b\x61\x01" +           // Center alignment
               "\x1b\x21\x30" +           // Double height/width
               "TEST LABEL\n" +
               "\x1b\x21\x00" +           // Normal text
               "Printer Connected\n" +
               DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
               "\x1d\x56\x41\x03";        // Cut paper
    }

    private LabelPrinterDto MapToDto(LabelPrinter printer, int assignmentCount)
    {
        return new LabelPrinterDto
        {
            Id = printer.Id,
            Name = printer.Name,
            ConnectionString = printer.ConnectionString,
            StoreId = printer.StoreId,
            PrinterType = (LabelPrinterTypeDto)printer.PrinterType,
            PrintLanguage = (LabelPrintLanguageDto)printer.PrintLanguage,
            DefaultLabelSizeId = printer.DefaultLabelSizeId,
            DefaultLabelSizeName = printer.DefaultLabelSize?.Name,
            IsDefault = printer.IsDefault,
            Status = (LabelPrinterStatusDto)printer.Status,
            LastConnectedAt = printer.LastConnectedAt,
            LastErrorMessage = printer.LastErrorMessage,
            BaudRate = printer.BaudRate,
            Port = printer.Port,
            TimeoutMs = printer.TimeoutMs,
            CategoryAssignmentCount = assignmentCount
        };
    }

    private LabelSizeDto MapToDto(LabelSize size)
    {
        return new LabelSizeDto
        {
            Id = size.Id,
            Name = size.Name,
            WidthMm = size.WidthMm,
            HeightMm = size.HeightMm,
            DotsPerMm = size.DotsPerMm,
            Description = size.Description
        };
    }

    private async Task<CategoryPrinterAssignmentDto> GetAssignmentDtoAsync(CategoryPrinterAssignment assignment)
    {
        var printer = await _printerRepository.GetByIdAsync(assignment.LabelPrinterId);

        return new CategoryPrinterAssignmentDto
        {
            Id = assignment.Id,
            CategoryId = assignment.CategoryId,
            CategoryName = assignment.Category?.Name ?? string.Empty,
            LabelPrinterId = assignment.LabelPrinterId,
            PrinterName = printer?.Name ?? string.Empty,
            LabelTemplateId = assignment.LabelTemplateId,
            TemplateName = assignment.LabelTemplate?.Name,
            StoreId = assignment.StoreId
        };
    }

    #endregion
}
