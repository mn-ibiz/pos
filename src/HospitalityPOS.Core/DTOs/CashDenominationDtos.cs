using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO for cash denomination configuration.
/// </summary>
public class CashDenominationDto
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = "KES";
    public DenominationType Type { get; set; }
    public decimal Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for submitting a cash count with denominations.
/// </summary>
public class CashDenominationCountDto
{
    /// <summary>
    /// Dictionary of denomination values to quantities counted.
    /// Key: denomination value (1000, 500, 0.50, etc.)
    /// Value: quantity counted
    /// </summary>
    public Dictionary<decimal, int> Denominations { get; set; } = new();

    /// <summary>
    /// Optional notes about the count.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for a cash count result with full details.
/// </summary>
public class CashCountResultDto
{
    public int Id { get; set; }
    public int WorkPeriodId { get; set; }
    public CashCountType CountType { get; set; }
    public int CountedByUserId { get; set; }
    public string CountedByUserName { get; set; } = string.Empty;
    public DateTime CountedAt { get; set; }
    public int? VerifiedByUserId { get; set; }
    public string? VerifiedByUserName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public decimal TotalNotes { get; set; }
    public decimal TotalCoins { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Individual line items in the count.
    /// </summary>
    public List<CashCountLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for a single denomination line in a cash count.
/// </summary>
public class CashCountLineDto
{
    public int DenominationId { get; set; }
    public DenominationType Type { get; set; }
    public decimal DenominationValue { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// DTO for recommended float composition.
/// </summary>
public class FloatRecommendationDto
{
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Recommended quantities by denomination value.
    /// </summary>
    public Dictionary<decimal, int> RecommendedDenominations { get; set; } = new();

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for cash variance breakdown.
/// </summary>
public class CashVarianceDto
{
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal TotalVariance { get; set; }
    public bool IsOver => TotalVariance > 0;
    public bool IsShort => TotalVariance < 0;
    public bool IsExact => TotalVariance == 0;

    /// <summary>
    /// Formatted variance display.
    /// </summary>
    public string VarianceDisplay => TotalVariance >= 0
        ? $"+{TotalVariance:N2} (OVER)"
        : $"{TotalVariance:N2} (SHORT)";
}
