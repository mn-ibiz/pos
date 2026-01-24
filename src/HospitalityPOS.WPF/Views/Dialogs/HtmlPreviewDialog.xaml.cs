using System.Windows;
using System.Windows.Input;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Reusable HTML preview dialog for payslips, reports, and other HTML content.
/// </summary>
public partial class HtmlPreviewDialog : Window
{
    private readonly string _htmlContent;
    private readonly string? _exportFilename;
    private readonly Func<Task>? _printAction;
    private readonly Func<Task>? _exportAction;

    /// <summary>
    /// Creates an HTML preview dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="subtitle">Optional subtitle.</param>
    /// <param name="htmlContent">HTML content to display.</param>
    /// <param name="exportFilename">Optional filename for PDF export.</param>
    /// <param name="printAction">Optional custom print action.</param>
    /// <param name="exportAction">Optional custom export action.</param>
    public HtmlPreviewDialog(
        string title,
        string? subtitle,
        string htmlContent,
        string? exportFilename = null,
        Func<Task>? printAction = null,
        Func<Task>? exportAction = null)
    {
        InitializeComponent();

        _htmlContent = htmlContent;
        _exportFilename = exportFilename ?? $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        _printAction = printAction;
        _exportAction = exportAction;

        // Set title and subtitle
        Title = title;
        TitleText.Text = title;
        SubtitleText.Text = subtitle ?? string.Empty;
        SubtitleText.Visibility = string.IsNullOrEmpty(subtitle) ? Visibility.Collapsed : Visibility.Visible;

        // Hide buttons if no actions provided
        if (printAction == null && exportAction == null && string.IsNullOrEmpty(exportFilename))
        {
            PrintButton.Visibility = Visibility.Collapsed;
            ExportButton.Visibility = Visibility.Collapsed;
        }

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        // Load HTML when window is ready
        Loaded += HtmlPreviewDialog_Loaded;
    }

    private void HtmlPreviewDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            PreviewBrowser.NavigateToString(_htmlContent);
            InfoText.Text = $"Generated on {DateTime.Now:g}";
        }
        catch (Exception ex)
        {
            InfoText.Text = $"Error loading preview: {ex.Message}";
        }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_printAction != null)
            {
                await _printAction();
            }
            else
            {
                // Default: export to PDF and open
                await ExportToPdfAsync(openAfterExport: true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_exportAction != null)
            {
                await _exportAction();
            }
            else
            {
                await ExportToPdfAsync(openAfterExport: false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportToPdfAsync(bool openAfterExport)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var reportPrintService = scope.ServiceProvider.GetRequiredService<IReportPrintService>();

            await reportPrintService.ExportToPdfAsync(_htmlContent, _exportFilename!);

            var message = openAfterExport
                ? $"Exported and opened: {_exportFilename}"
                : $"Exported to: {_exportFilename}";

            MessageBox.Show(message, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
