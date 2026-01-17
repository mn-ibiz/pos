# Story 10.5: Report Export

Status: done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/ExportService.cs` (290 lines) - Export functionality with:
  - CSV export for all report types
  - Automatic filename generation with timestamps
  - Proper escaping for special characters

## Story

As a manager,
I want to export reports to common formats,
So that data can be shared or further analyzed.

## Acceptance Criteria

1. **Given** a report is generated
   **When** export is requested
   **Then** report can be exported to CSV format

2. **Given** export options
   **When** PDF is needed
   **Then** optionally PDF format (if time permits)

3. **Given** export is initiated
   **When** saving file
   **Then** export file should be saved to user-selected location

4. **Given** export file is created
   **When** naming the file
   **Then** file naming should include report type and date

## Tasks / Subtasks

- [ ] Task 1: Create Export Service
  - [ ] Create IExportService interface
  - [ ] Implement CSV export
  - [ ] Handle different data types
  - [ ] Proper encoding (UTF-8)

- [ ] Task 2: Implement CSV Export
  - [ ] Generate CSV headers
  - [ ] Format data correctly
  - [ ] Handle special characters
  - [ ] Support large datasets

- [ ] Task 3: Create Export Dialog
  - [ ] File save dialog
  - [ ] Format selection
  - [ ] Default filename
  - [ ] Show progress

- [ ] Task 4: Implement PDF Export (Optional)
  - [ ] Use PDF library
  - [ ] Format report layout
  - [ ] Include headers/footers
  - [ ] Handle pagination

- [ ] Task 5: Add Export to All Reports
  - [ ] Add export button to report screens
  - [ ] Pass report data to export service
  - [ ] Show success message
  - [ ] Handle errors

## Dev Notes

### Export Service Interface

```csharp
public interface IExportService
{
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath);
    Task ExportToCsvAsync(DataTable dataTable, string filePath);
    Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string reportTitle);
    string GenerateFilename(string reportType, DateTime startDate, DateTime? endDate = null, string extension = "csv");
}
```

### CSV Export Service

```csharp
public class ExportService : IExportService
{
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Header row
        var headers = properties.Select(p => EscapeCsvField(GetDisplayName(p)));
        sb.AppendLine(string.Join(",", headers));

        // Data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvField(FormatValue(value));
            });
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Escape quotes and wrap in quotes if needed
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            decimal d => d.ToString("N2"),
            bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? ""
        };
    }

    private string GetDisplayName(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name ?? property.Name;
    }

    public string GenerateFilename(string reportType, DateTime startDate, DateTime? endDate = null, string extension = "csv")
    {
        var sanitizedType = reportType.Replace(" ", "_").Replace("/", "-");
        var dateRange = endDate.HasValue && endDate != startDate
            ? $"{startDate:yyyyMMdd}-{endDate:yyyyMMdd}"
            : $"{startDate:yyyyMMdd}";

        return $"{sanitizedType}_{dateRange}.{extension}";
    }
}
```

### Export Dialog

```
+------------------------------------------+
|      EXPORT REPORT                        |
+------------------------------------------+
|                                           |
|  Report: Daily Sales Summary              |
|  Date Range: 2025-12-20                   |
|                                           |
|  Export Format:                           |
|  (x) CSV (Excel/Spreadsheet)              |
|  ( ) PDF Document                         |
|                                           |
|  Save As:                                 |
|  +------------------------------------+   |
|  | Daily_Sales_20251220.csv          |   |
|  +------------------------------------+   |
|  [Browse...]                              |
|                                           |
|  Location:                                |
|  C:\Users\John\Documents\Reports          |
|                                           |
|  [Cancel]                     [Export]    |
+------------------------------------------+
```

### Export ViewModel

```csharp
public partial class ExportDialogViewModel : BaseViewModel
{
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

    public void Initialize(string reportName, DateTime startDate, DateTime? endDate)
    {
        ReportName = reportName;
        DateRange = endDate.HasValue && endDate != startDate
            ? $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            : startDate.ToString("yyyy-MM-dd");

        FileName = _exportService.GenerateFilename(reportName, startDate, endDate);
        FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Reports",
            FileName);
    }

    [RelayCommand]
    private void Browse()
    {
        var dialog = new SaveFileDialog
        {
            Filter = SelectedFormat == ExportFormat.CSV
                ? "CSV Files (*.csv)|*.csv"
                : "PDF Files (*.pdf)|*.pdf",
            FileName = FileName,
            InitialDirectory = Path.GetDirectoryName(FilePath)
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            FileName = Path.GetFileName(dialog.FileName);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        IsExporting = true;

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (SelectedFormat == ExportFormat.CSV)
            {
                await _exportService.ExportToCsvAsync(ReportData, FilePath);
            }
            else
            {
                await _exportService.ExportToPdfAsync(ReportData, FilePath, ReportName);
            }

            CloseDialog(true);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Export Error", ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }
}

public enum ExportFormat
{
    CSV,
    PDF
}
```

### Integration with Reports

```csharp
// In SalesReportsViewModel
[RelayCommand]
private async Task ExportReportAsync()
{
    if (ReportData == null) return;

    var dialog = new ExportDialog();
    dialog.Initialize(
        GetReportName(SelectedReportType),
        FromDate,
        ToDate);
    dialog.ReportData = ReportData;

    var result = await _dialogService.ShowDialogAsync(dialog);

    if (result == true)
    {
        await _dialogService.ShowMessageAsync(
            "Export Complete",
            $"Report exported successfully to:\n{dialog.FilePath}");

        // Optionally open file location
        Process.Start("explorer.exe", $"/select,\"{dialog.FilePath}\"");
    }
}
```

### CSV Output Example

```csv
Product Name,Category,Quantity Sold,Gross Sales,Discounts,Net Sales
Tusker Lager,Beverages,45,15750.00,200.00,15550.00
Grilled Chicken,Food,32,27200.00,500.00,26700.00
Coca Cola 500ml,Beverages,78,3900.00,0.00,3900.00
Chips Regular,Food,54,10800.00,100.00,10700.00
```

### PDF Export (Optional)

```csharp
public async Task<bool> ExportToPdfAsync<T>(
    IEnumerable<T> data,
    string filePath,
    string reportTitle)
{
    // Using QuestPDF or similar library
    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);

            page.Header()
                .Text(reportTitle)
                .FontSize(20)
                .Bold()
                .AlignCenter();

            page.Content()
                .Table(table =>
                {
                    var properties = typeof(T).GetProperties();

                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var prop in properties)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Header row
                    foreach (var prop in properties)
                    {
                        table.Cell().Background("#E0E0E0").Padding(5)
                            .Text(GetDisplayName(prop)).Bold();
                    }

                    // Data rows
                    foreach (var item in data)
                    {
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(item);
                            table.Cell().Padding(5).Text(FormatValue(value));
                        }
                    }
                });

            page.Footer()
                .AlignRight()
                .Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        });
    })
    .GeneratePdf(filePath);

    return true;
}
```

### Export File Naming Convention
- Format: `{ReportType}_{DateRange}.{ext}`
- Examples:
  - `Daily_Sales_Summary_20251220.csv`
  - `Sales_By_Product_20251201-20251231.csv`
  - `Void_Report_20251220.pdf`

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.7.5-Report-Export]
- [Source: docs/PRD_Hospitality_POS_System.md#RP-045]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
