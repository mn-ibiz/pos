using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the Export Dialog.
/// </summary>
public partial class ExportDialogViewModel : ViewModelBase
{
    private readonly IExportService _exportService;
    private readonly ILogger _logger;

    /// <summary>
    /// Gets or sets the report data to export.
    /// </summary>
    public object? ReportData { get; set; }

    /// <summary>
    /// Gets or sets the export function to be called.
    /// </summary>
    public Func<string, CancellationToken, Task>? ExportAction { get; set; }

    /// <summary>
    /// Gets or sets the action to close the dialog.
    /// </summary>
    public Action<bool>? CloseDialog { get; set; }

    [ObservableProperty]
    private string _reportName = string.Empty;

    [ObservableProperty]
    private string _dateRange = string.Empty;

    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.CSV;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isPdfSupported;

    /// <summary>
    /// Gets the available export formats.
    /// </summary>
    public ExportFormat[] AvailableFormats { get; } = [ExportFormat.CSV, ExportFormat.PDF];

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDialogViewModel"/> class.
    /// </summary>
    public ExportDialogViewModel(
        IExportService exportService,
        ILogger logger)
    {
        _exportService = exportService;
        _logger = logger;
        IsPdfSupported = false; // PDF not implemented yet
    }

    /// <summary>
    /// Initializes the dialog with report details.
    /// </summary>
    /// <param name="reportName">The name of the report.</param>
    /// <param name="startDate">The start date of the report period.</param>
    /// <param name="endDate">Optional end date of the report period.</param>
    public void Initialize(string reportName, DateTime startDate, DateTime? endDate = null)
    {
        ReportName = reportName;
        DateRange = endDate.HasValue && endDate.Value.Date != startDate.Date
            ? $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            : startDate.ToString("yyyy-MM-dd");

        FileName = _exportService.GenerateFilename(reportName, startDate, endDate, "csv");
        FilePath = Path.Combine(
            _exportService.GetDefaultExportDirectory(),
            FileName);

        ErrorMessage = null;
    }

    /// <summary>
    /// Updates the filename when the format changes.
    /// </summary>
    partial void OnSelectedFormatChanged(ExportFormat value)
    {
        if (string.IsNullOrEmpty(FileName))
        {
            return;
        }

        var extension = value == ExportFormat.CSV ? "csv" : "pdf";
        var currentExtension = Path.GetExtension(FileName);

        if (!string.IsNullOrEmpty(currentExtension))
        {
            FileName = Path.ChangeExtension(FileName, extension);
            if (!string.IsNullOrEmpty(FilePath))
            {
                FilePath = Path.ChangeExtension(FilePath, extension);
            }
        }
    }

    /// <summary>
    /// Opens a file save dialog to select the export location.
    /// </summary>
    [RelayCommand]
    private void Browse()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = SelectedFormat == ExportFormat.CSV
                ? "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
                : "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
            FileName = FileName,
            DefaultExt = SelectedFormat == ExportFormat.CSV ? ".csv" : ".pdf",
            Title = "Export Report"
        };

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            FileName = Path.GetFileName(dialog.FileName);
        }
    }

    /// <summary>
    /// Exports the report to the selected format and location.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            ErrorMessage = "Please select a file location.";
            return;
        }

        if (SelectedFormat == ExportFormat.PDF && !IsPdfSupported)
        {
            ErrorMessage = "PDF export is not available. Please use CSV format.";
            return;
        }

        if (ExportAction == null)
        {
            ErrorMessage = "No export action configured.";
            return;
        }

        IsExporting = true;
        ErrorMessage = null;

        await ExecuteAsync(async () =>
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await ExportAction(FilePath, CancellationToken.None);

                _logger.Information("Export completed successfully: {FilePath}", FilePath);

                CloseDialog?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Export failed: {FilePath}", FilePath);
                ErrorMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
            }
        });
    }

    /// <summary>
    /// Cancels the export dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CloseDialog?.Invoke(false);
    }

    /// <summary>
    /// Gets the filter string for the file dialog.
    /// </summary>
    public string GetDialogFilter()
    {
        return SelectedFormat == ExportFormat.CSV
            ? "CSV Files (*.csv)|*.csv"
            : "PDF Files (*.pdf)|*.pdf";
    }
}
