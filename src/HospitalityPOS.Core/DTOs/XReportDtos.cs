namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Data for an X-Report (mid-shift examination report).
/// </summary>
public class XReportData
{
    // Header / Business Info
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty; // KRA PIN

    // Terminal Info
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string ReportNumber { get; set; } = string.Empty; // X-2024-001-0042
    public DateTime GeneratedAt { get; set; }
    public string GeneratedByName { get; set; } = string.Empty;
    public int GeneratedByUserId { get; set; }

    // Shift Info
    public int WorkPeriodId { get; set; }
    public DateTime ShiftStarted { get; set; }
    public DateTime CurrentTime { get; set; }
    public TimeSpan ShiftDuration { get; set; }
    public string ShiftDurationFormatted => FormatDuration(ShiftDuration);

    // Cashier Sessions
    public List<CashierSessionSummary> CashierSessions { get; set; } = new();

    // Sales Summary
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal Voids { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipsCollected { get; set; }
    public decimal GrandTotal { get; set; }

    // Payment Breakdown
    public List<PaymentMethodSummary> PaymentBreakdown { get; set; } = new();
    public List<PaymentTypeBreakdown> PaymentTypeBreakdown { get; set; } = new();
    public decimal TotalPayments { get; set; }

    // Cash Drawer
    public decimal OpeningFloat { get; set; }
    public decimal CashReceived { get; set; }
    public decimal CashRefunds { get; set; }
    public decimal CashPayouts { get; set; }
    public decimal ExpectedCash { get; set; }

    // Statistics
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public int CustomerCount { get; set; }
    public int VoidCount { get; set; }
    public int RefundCount { get; set; }
    public int DiscountCount { get; set; }
    public int DrawerOpenCount { get; set; }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        }
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }
}

/// <summary>
/// Summary data for a cashier session within a work period.
/// </summary>
public class CashierSessionSummary
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; } // null if current
    public bool IsCurrent { get; set; }
    public decimal SalesTotal { get; set; }
    public int TransactionCount { get; set; }
    public decimal CashReceived { get; set; }
    public decimal CardTotal { get; set; }
    public decimal MpesaTotal { get; set; }
    public decimal RefundTotal { get; set; }
    public decimal VoidTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public TimeSpan Duration { get; set; }
    public string DurationFormatted => FormatDuration(Duration);

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes}m";
    }
}

/// <summary>
/// Summary for a payment method in reports.
/// </summary>
public class PaymentMethodSummary
{
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string PaymentMethodCode { get; set; } = string.Empty;
    public HospitalityPOS.Core.Enums.PaymentMethodType PaymentMethodType { get; set; }
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Groups payment methods by their type for enhanced report breakdown.
/// </summary>
public class PaymentTypeBreakdown
{
    public HospitalityPOS.Core.Enums.PaymentMethodType PaymentType { get; set; }
    public string PaymentTypeName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalTransactionCount { get; set; }
    public decimal Percentage { get; set; }
    public List<PaymentMethodSummary> Methods { get; set; } = [];

    /// <summary>
    /// Gets display name for payment type.
    /// </summary>
    public static string GetPaymentTypeName(HospitalityPOS.Core.Enums.PaymentMethodType type) => type switch
    {
        HospitalityPOS.Core.Enums.PaymentMethodType.Cash => "Cash",
        HospitalityPOS.Core.Enums.PaymentMethodType.Card => "Card",
        HospitalityPOS.Core.Enums.PaymentMethodType.MPesa => "M-Pesa",
        HospitalityPOS.Core.Enums.PaymentMethodType.BankTransfer => "Bank Transfer",
        HospitalityPOS.Core.Enums.PaymentMethodType.Credit => "Credit",
        HospitalityPOS.Core.Enums.PaymentMethodType.LoyaltyPoints => "Loyalty Points",
        _ => type.ToString()
    };
}

/// <summary>
/// Persisted record of an X-Report generation.
/// </summary>
public class XReportRecord
{
    public int Id { get; set; }
    public int WorkPeriodId { get; set; }
    public int TerminalId { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string ReportNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int GeneratedByUserId { get; set; }
    public string GeneratedByName { get; set; } = string.Empty;

    // Snapshot of totals at time of generation
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ExpectedCash { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// Export formats for reports.
/// </summary>
public enum ReportExportFormat
{
    Pdf = 1,
    Excel = 2,
    Csv = 3,
    ThermalPrint = 4
}
