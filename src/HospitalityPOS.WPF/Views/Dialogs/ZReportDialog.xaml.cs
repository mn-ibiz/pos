using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models.Reports;
using HospitalityPOS.WPF.Converters;

namespace HospitalityPOS.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for ZReportDialog.xaml
/// </summary>
public partial class ZReportDialog : Window
{
    private readonly ZReport _report;
    private readonly bool _autoPrint;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZReportDialog"/> class.
    /// </summary>
    /// <param name="report">The Z-Report to display.</param>
    /// <param name="autoPrint">If true, automatically prints the report when the dialog loads.</param>
    public ZReportDialog(ZReport report, bool autoPrint = false)
    {
        InitializeComponent();
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _autoPrint = autoPrint;
        PopulateReport();

        // Auto-print if requested (e.g., when closing day)
        if (_autoPrint)
        {
            Loaded += ZReportDialog_Loaded;
        }
    }

    private void ZReportDialog_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-print after dialog is loaded
        PrintReportSilently();
    }

    /// <summary>
    /// Prints the Z-Report without showing the print dialog (uses default printer).
    /// </summary>
    public void PrintReportSilently()
    {
        try
        {
            var printContent = GeneratePrintContent();

            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                PageWidth = 280,
                PagePadding = new Thickness(10),
                ColumnWidth = 280
            };

            var lines = printContent.Split('\n');
            foreach (var line in lines)
            {
                var paragraph = new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    LineHeight = 1
                };
                document.Blocks.Add(paragraph);
            }

            var printDialog = new System.Windows.Controls.PrintDialog();
            // Use default printer without showing dialog
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            printDialog.PrintDocument(paginator, $"Z Report #{_report.ZReportNumber:D4}");

            MessageBox.Show(
                $"Z-Report #{_report.ZReportNumber:D4} sent to printer.",
                "Print Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to print Z-Report: {ex.Message}",
                "Print Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void PopulateReport()
    {
        // Header
        BusinessNameText.Text = _report.BusinessName;
        if (!string.IsNullOrEmpty(_report.BusinessAddress))
        {
            BusinessAddressText.Text = _report.BusinessAddress;
            BusinessAddressText.Visibility = Visibility.Visible;
        }

        // Z-Report Number
        ZReportNumberText.Text = !string.IsNullOrEmpty(_report.ReportNumberFormatted)
            ? $"Z-Report #: {_report.ReportNumberFormatted}"
            : $"Z-Report #: Z-{_report.ZReportNumber:D4}";

        // Terminal Info
        if (!string.IsNullOrEmpty(_report.TerminalCode))
        {
            TerminalInfoText.Text = $"Terminal: {_report.TerminalCode}" +
                (!string.IsNullOrEmpty(_report.TerminalName) ? $" ({_report.TerminalName})" : "");
            TerminalInfoText.Visibility = Visibility.Visible;
        }

        // Work Period Info
        OpenedAtText.Text = _report.WorkPeriodOpenedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        OpenedByText.Text = _report.WorkPeriodOpenedBy;
        ClosedAtText.Text = _report.WorkPeriodClosedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        ClosedByText.Text = _report.WorkPeriodClosedBy;
        DurationText.Text = $"Duration: {FormatDuration(_report.Duration)}";

        // Sales Summary
        GrossSalesText.Text = $"KSh {_report.GrossSales:N2}";
        DiscountsText.Text = $"-KSh {_report.TotalDiscounts:N2}";
        NetSalesText.Text = $"KSh {_report.NetSales:N2}";
        TaxText.Text = $"KSh {_report.TaxCollected:N2}";
        GrandTotalText.Text = $"KSh {_report.GrandTotal:N2}";

        // Cashier Sessions (Enhanced Breakdown)
        if (_report.CashierSessions?.Count > 0)
        {
            // Convert session times to local time for display
            var localSessions = _report.CashierSessions.Select(s => new
            {
                s.CashierName,
                SessionStart = s.SessionStart.ToLocalTime(),
                SessionEnd = s.SessionEnd?.ToLocalTime(),
                s.DurationFormatted,
                s.TransactionCount,
                s.GrossSales,
                s.NetSales,
                s.CashPayments,
                s.CardPayments,
                s.OtherPayments,
                s.VoidCount,
                s.VoidAmount,
                s.RefundCount,
                s.RefundAmount
            }).ToList();
            CashierSessionItems.ItemsSource = localSessions;
        }
        else
        {
            NoCashierSessionsText.Visibility = Visibility.Visible;
        }

        // Sales by User
        UserSalesItems.ItemsSource = _report.SalesByUser;

        // Payment Methods (Grouped by Type)
        if (_report.SalesByPaymentMethod?.Count > 0)
        {
            var totalPayments = _report.SalesByPaymentMethod.Sum(p => p.TotalAmount);

            // Group by payment type based on method name patterns
            var typeBreakdown = _report.SalesByPaymentMethod
                .Select(p => new
                {
                    Method = p,
                    Type = InferPaymentType(p.PaymentMethod)
                })
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    PaymentTypeName = PaymentTypeBreakdown.GetPaymentTypeName(g.Key),
                    TotalAmount = g.Sum(m => m.Method.TotalAmount),
                    TotalTransactionCount = g.Sum(m => m.Method.TransactionCount),
                    Percentage = totalPayments > 0
                        ? Math.Round(g.Sum(m => m.Method.TotalAmount) / totalPayments * 100, 1)
                        : 0m,
                    Methods = g.Select(m => new
                    {
                        PaymentMethodName = m.Method.PaymentMethod,
                        TotalAmount = m.Method.TotalAmount,
                        TransactionCount = m.Method.TransactionCount
                    }).ToList()
                })
                .OrderBy(t => t.PaymentTypeName)
                .ToList();

            PaymentTypeItems.ItemsSource = typeBreakdown;
        }
        else
        {
            NoPaymentMethodsText.Visibility = Visibility.Visible;
        }

        // Receipts Summary
        SettledCountRun.Text = _report.SettledReceiptsCount.ToString();
        SettledTotalText.Text = $"KSh {_report.SettledReceiptsTotal:N2}";
        PendingCountRun.Text = _report.PendingReceiptsCount.ToString();
        PendingTotalText.Text = $"KSh {_report.PendingReceiptsTotal:N2}";
        VoidedCountRun.Text = _report.VoidCount.ToString();
        VoidedTotalText.Text = $"KSh {_report.VoidTotal:N2}";

        // Cash Drawer
        OpeningFloatText.Text = $"KSh {_report.OpeningFloat:N2}";
        CashSalesText.Text = $"KSh {_report.CashSales:N2}";
        CashPayoutsText.Text = $"-KSh {_report.CashPayouts:N2}";
        ExpectedCashText.Text = $"KSh {_report.ExpectedCash:N2}";
        ActualCashText.Text = $"KSh {_report.ActualCash:N2}";

        // Variance
        VarianceText.Text = _report.Variance >= 0
            ? $"KSh {_report.Variance:N2}"
            : $"-KSh {Math.Abs(_report.Variance):N2}";

        if (_report.IsShort)
        {
            VarianceText.Foreground = VarianceStatusColors.ShortForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.ShortBackground;
            VarianceStatusText.Text = "*** SHORT ***";
            VarianceStatusText.Foreground = VarianceStatusColors.ShortForeground;
        }
        else if (_report.IsOver)
        {
            VarianceText.Foreground = VarianceStatusColors.OverForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.OverBackground;
            VarianceStatusText.Text = "*** OVER ***";
            VarianceStatusText.Foreground = VarianceStatusColors.OverForeground;
        }
        else
        {
            VarianceText.Foreground = VarianceStatusColors.ExactForeground;
            VarianceStatusBorder.Background = VarianceStatusColors.ExactBackground;
            VarianceStatusText.Text = "*** EXACT ***";
            VarianceStatusText.Foreground = VarianceStatusColors.ExactForeground;
        }

        // Top Selling Items
        var indexedItems = _report.TopSellingItems.Select((item, index) => new
        {
            Index = $"{index + 1}.",
            item.ProductName,
            item.QuantitySold,
            item.TotalValue
        }).ToList();
        TopSellingItems.ItemsSource = indexedItems;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return $"{hours}h {minutes:D2}m";
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var printContent = GeneratePrintContent();

        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            PageWidth = 280,
            PagePadding = new Thickness(10),
            ColumnWidth = 280
        };

        var lines = printContent.Split('\n');
        foreach (var line in lines)
        {
            var paragraph = new Paragraph(new Run(line))
            {
                Margin = new Thickness(0, 0, 0, 0),
                LineHeight = 1
            };
            document.Blocks.Add(paragraph);
        }

        var printDialog = new System.Windows.Controls.PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            printDialog.PrintDocument(paginator, "Z Report");
            MessageBox.Show("Z-Report sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private string GeneratePrintContent()
    {
        const int lineWidth = 48;
        var sb = new System.Text.StringBuilder();

        // Double separator
        sb.AppendLine(new string('=', lineWidth));

        // Header
        sb.AppendLine(CenterText(_report.BusinessName, lineWidth));
        if (!string.IsNullOrEmpty(_report.BusinessAddress))
        {
            sb.AppendLine(CenterText(_report.BusinessAddress, lineWidth));
        }

        sb.AppendLine(new string('=', lineWidth));
        sb.AppendLine(CenterText("*** Z REPORT ***", lineWidth));
        var reportNum = !string.IsNullOrEmpty(_report.ReportNumberFormatted)
            ? _report.ReportNumberFormatted
            : $"Z-{_report.ZReportNumber:D4}";
        sb.AppendLine(CenterText($"Z-Report #: {reportNum}", lineWidth));
        if (!string.IsNullOrEmpty(_report.TerminalCode))
        {
            sb.AppendLine(CenterText($"Terminal: {_report.TerminalCode}", lineWidth));
        }
        sb.AppendLine(new string('=', lineWidth));

        // Work Period
        sb.AppendLine($"Date: {_report.WorkPeriodClosedAt.ToLocalTime():yyyy-MM-dd}");
        sb.AppendLine($"Period Open:  {_report.WorkPeriodOpenedAt.ToLocalTime():HH:mm} by {_report.WorkPeriodOpenedBy}");
        sb.AppendLine($"Period Close: {_report.WorkPeriodClosedAt.ToLocalTime():HH:mm} by {_report.WorkPeriodClosedBy}");
        sb.AppendLine($"Duration: {FormatDuration(_report.Duration)}");

        sb.AppendLine(new string('=', lineWidth));

        // Sales Summary
        sb.AppendLine("SALES SUMMARY");
        sb.AppendLine(new string('-', lineWidth));
        sb.AppendLine(FormatLine("Gross Sales:", $"KSh {_report.GrossSales:N2}", lineWidth));
        sb.AppendLine(FormatLine("Discounts:", $"-KSh {_report.TotalDiscounts:N2}", lineWidth));
        sb.AppendLine(FormatLine("Net Sales:", $"KSh {_report.NetSales:N2}", lineWidth));
        sb.AppendLine(FormatLine("Tax:", $"KSh {_report.TaxCollected:N2}", lineWidth));
        sb.AppendLine(new string(' ', lineWidth - 18) + new string('-', 18));
        sb.AppendLine(FormatLine("GRAND TOTAL:", $"KSh {_report.GrandTotal:N2}", lineWidth));

        sb.AppendLine(new string('=', lineWidth));

        // Cashier Sessions (Enhanced)
        if (_report.CashierSessions?.Count > 0)
        {
            sb.AppendLine("CASHIER SESSIONS");
            sb.AppendLine(new string('-', lineWidth));
            foreach (var session in _report.CashierSessions)
            {
                sb.AppendLine($"{session.CashierName}");
                var endTime = session.SessionEnd?.ToLocalTime().ToString("HH:mm") ?? "Active";
                sb.AppendLine($"  {session.SessionStart.ToLocalTime():HH:mm} - {endTime} ({session.DurationFormatted})");
                sb.AppendLine(FormatLine($"  Transactions:", session.TransactionCount.ToString(), lineWidth));
                sb.AppendLine(FormatLine($"  Net Sales:", $"KSh {session.NetSales:N2}", lineWidth));
                sb.AppendLine(FormatLine($"    Cash:", $"KSh {session.CashPayments:N2}", lineWidth));
                sb.AppendLine(FormatLine($"    Card:", $"KSh {session.CardPayments:N2}", lineWidth));
                sb.AppendLine(FormatLine($"    Other:", $"KSh {session.OtherPayments:N2}", lineWidth));
                if (session.VoidCount > 0)
                {
                    sb.AppendLine(FormatLine($"  Voids ({session.VoidCount}):", $"-KSh {session.VoidAmount:N2}", lineWidth));
                }
                sb.AppendLine();
            }
            sb.AppendLine(new string('=', lineWidth));
        }

        // Sales by Cashier (Summary)
        sb.AppendLine("SALES BY CASHIER");
        sb.AppendLine(new string('-', lineWidth));
        foreach (var user in _report.SalesByUser)
        {
            sb.AppendLine($"{user.UserName}:");
            sb.AppendLine($"  Transactions: {user.TransactionCount}");
            sb.AppendLine($"  Total: KSh {user.TotalAmount:N2}");
            sb.AppendLine($"  Average: KSh {user.AverageTransaction:N2}");
            sb.AppendLine();
        }

        sb.AppendLine(new string('=', lineWidth));

        // Payment Methods (Grouped by Type)
        sb.AppendLine("PAYMENT BREAKDOWN");
        sb.AppendLine(new string('-', lineWidth));

        if (_report.SalesByPaymentMethod?.Count > 0)
        {
            var totalPayments = _report.SalesByPaymentMethod.Sum(p => p.TotalAmount);
            var typeBreakdown = _report.SalesByPaymentMethod
                .Select(p => new { Method = p, Type = InferPaymentType(p.PaymentMethod) })
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    TypeName = PaymentTypeBreakdown.GetPaymentTypeName(g.Key),
                    TotalAmount = g.Sum(m => m.Method.TotalAmount),
                    TotalCount = g.Sum(m => m.Method.TransactionCount),
                    Methods = g.Select(m => m.Method).ToList()
                })
                .OrderBy(t => t.TypeName)
                .ToList();

            foreach (var pt in typeBreakdown)
            {
                // Type header
                sb.AppendLine(FormatLine($"[{pt.TypeName}] ({pt.TotalCount}):", $"KSh {pt.TotalAmount:N2}", lineWidth));
                // Individual methods
                foreach (var pm in pt.Methods)
                {
                    sb.AppendLine(FormatLine($"  {pm.PaymentMethod} ({pm.TransactionCount}):", $"KSh {pm.TotalAmount:N2}", lineWidth));
                }
            }
        }
        else
        {
            sb.AppendLine("No payments recorded");
        }

        sb.AppendLine(new string('=', lineWidth));

        // Receipts
        sb.AppendLine("RECEIPTS");
        sb.AppendLine(new string('-', lineWidth));
        sb.AppendLine(FormatLine($"Settled: {_report.SettledReceiptsCount}", $"KSh {_report.SettledReceiptsTotal:N2}", lineWidth));
        sb.AppendLine(FormatLine($"Pending: {_report.PendingReceiptsCount}", $"KSh {_report.PendingReceiptsTotal:N2}", lineWidth));
        sb.AppendLine(FormatLine($"Voided: {_report.VoidCount}", $"KSh {_report.VoidTotal:N2}", lineWidth));

        // Voids Detail
        if (_report.Voids.Count > 0)
        {
            sb.AppendLine(new string('=', lineWidth));
            sb.AppendLine("VOIDS DETAIL");
            sb.AppendLine(new string('-', lineWidth));
            foreach (var v in _report.Voids)
            {
                sb.AppendLine($"{v.ReceiptNumber}: KSh {v.Amount:N2}");
                if (!string.IsNullOrEmpty(v.Reason))
                {
                    var reason = v.Reason.Length > lineWidth - 4 ? v.Reason[..(lineWidth - 7)] + "..." : v.Reason;
                    sb.AppendLine($"  Reason: {reason}");
                }
                if (!string.IsNullOrEmpty(v.VoidedBy))
                {
                    sb.AppendLine($"  Voided by: {v.VoidedBy}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine(new string('=', lineWidth));

        // Cash Drawer
        sb.AppendLine("CASH DRAWER");
        sb.AppendLine(new string('-', lineWidth));
        sb.AppendLine(FormatLine("Opening Float:", $"KSh {_report.OpeningFloat:N2}", lineWidth));
        sb.AppendLine(FormatLine("+ Cash Sales:", $"KSh {_report.CashSales:N2}", lineWidth));
        sb.AppendLine(FormatLine("- Cash Payouts:", $"-KSh {_report.CashPayouts:N2}", lineWidth));
        sb.AppendLine(new string(' ', lineWidth - 18) + new string('-', 18));
        sb.AppendLine(FormatLine("EXPECTED:", $"KSh {_report.ExpectedCash:N2}", lineWidth));
        sb.AppendLine();
        sb.AppendLine(FormatLine("Actual Count:", $"KSh {_report.ActualCash:N2}", lineWidth));
        sb.AppendLine(new string(' ', lineWidth - 18) + new string('-', 18));

        var varianceStr = _report.Variance >= 0
            ? $"KSh {_report.Variance:N2}"
            : $"-KSh {Math.Abs(_report.Variance):N2}";
        sb.AppendLine(FormatLine("VARIANCE:", varianceStr, lineWidth));
        sb.AppendLine(CenterText($"*** {_report.VarianceStatus} ***", lineWidth));

        sb.AppendLine(new string('=', lineWidth));

        // Top Selling Items
        if (_report.TopSellingItems.Count > 0)
        {
            sb.AppendLine("TOP SELLING ITEMS");
            sb.AppendLine(new string('-', lineWidth));
            var index = 1;
            foreach (var item in _report.TopSellingItems.Take(5))
            {
                sb.AppendLine(FormatLine($"{index}. {item.ProductName} ({item.QuantitySold})", $"KSh {item.TotalValue:N2}", lineWidth));
                index++;
            }
            sb.AppendLine(new string('=', lineWidth));
        }

        // Footer
        sb.AppendLine(CenterText("*** END OF Z REPORT ***", lineWidth));
        sb.AppendLine(CenterText("This is an official document", lineWidth));
        sb.AppendLine(CenterText("Do not discard", lineWidth));
        sb.AppendLine(new string('=', lineWidth));

        return sb.ToString();
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        var padding = (width - text.Length) / 2;
        return new string(' ', padding) + text;
    }

    private static string FormatLine(string label, string value, int width)
    {
        var spaces = width - label.Length - value.Length;
        if (spaces < 1) spaces = 1;
        return label + new string(' ', spaces) + value;
    }

    /// <summary>
    /// Infers the payment method type from the method name.
    /// </summary>
    private static PaymentMethodType InferPaymentType(string methodName)
    {
        var name = methodName.ToUpperInvariant();
        return name switch
        {
            _ when name.Contains("CASH") => PaymentMethodType.Cash,
            _ when name.Contains("CARD") || name.Contains("VISA") || name.Contains("MASTER") || name.Contains("DEBIT") || name.Contains("CREDIT") => PaymentMethodType.Card,
            _ when name.Contains("MPESA") || name.Contains("M-PESA") => PaymentMethodType.MPesa,
            _ when name.Contains("BANK") || name.Contains("TRANSFER") || name.Contains("EFT") => PaymentMethodType.BankTransfer,
            _ when name.Contains("CREDIT") && !name.Contains("CARD") => PaymentMethodType.Credit,
            _ when name.Contains("LOYALTY") || name.Contains("POINTS") => PaymentMethodType.LoyaltyPoints,
            _ => PaymentMethodType.Cash // Default to Cash for unknown methods
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
