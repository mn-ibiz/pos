using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for cash denomination counting dialog.
/// </summary>
public partial class CashDenominationCountViewModel : ViewModelBase
{
    private readonly IWorkPeriodService _workPeriodService;

    /// <summary>
    /// Gets or sets the collection of denomination entries.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DenominationEntryViewModel> _denominationEntries = new();

    /// <summary>
    /// Gets or sets the total notes amount.
    /// </summary>
    [ObservableProperty]
    private decimal _totalNotes;

    /// <summary>
    /// Gets or sets the total coins amount.
    /// </summary>
    [ObservableProperty]
    private decimal _totalCoins;

    /// <summary>
    /// Gets or sets the grand total amount.
    /// </summary>
    [ObservableProperty]
    private decimal _grandTotal;

    /// <summary>
    /// Gets or sets the expected cash (for closing counts).
    /// </summary>
    [ObservableProperty]
    private decimal? _expectedCash;

    /// <summary>
    /// Gets or sets the variance (for closing counts).
    /// </summary>
    [ObservableProperty]
    private decimal _variance;

    /// <summary>
    /// Gets or sets whether this is a closing count.
    /// </summary>
    [ObservableProperty]
    private bool _isClosingCount;

    /// <summary>
    /// Gets or sets the variance explanation.
    /// </summary>
    [ObservableProperty]
    private string _varianceExplanation = string.Empty;

    /// <summary>
    /// Gets or sets optional notes.
    /// </summary>
    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>
    /// Gets or sets the dialog result.
    /// </summary>
    public bool? DialogResult { get; set; }

    /// <summary>
    /// Gets the variance display text.
    /// </summary>
    public string VarianceDisplay => Variance >= 0
        ? $"+{Variance:N2} (OVER)"
        : $"{Variance:N2} (SHORT)";

    /// <summary>
    /// Gets whether there is a significant variance requiring explanation.
    /// </summary>
    public bool RequiresExplanation => IsClosingCount && Math.Abs(Variance) > 100;

    /// <summary>
    /// Gets the variance status color.
    /// </summary>
    public string VarianceColor => Math.Abs(Variance) < 1 ? "#4CAF50" : (Variance > 0 ? "#FFA500" : "#FF6B6B");

    /// <summary>
    /// Initializes a new instance of the <see cref="CashDenominationCountViewModel"/> class.
    /// </summary>
    public CashDenominationCountViewModel(
        IWorkPeriodService workPeriodService,
        ILogger logger)
        : base(logger)
    {
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        Title = "Cash Count";
    }

    /// <summary>
    /// Loads the denominations for counting.
    /// </summary>
    [RelayCommand]
    public async Task LoadDenominationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var denominations = await _workPeriodService.GetActiveDenominationsAsync();

            DenominationEntries.Clear();
            foreach (var denom in denominations.OrderBy(d => d.Type).ThenByDescending(d => d.Value))
            {
                var entry = new DenominationEntryViewModel(denom);
                entry.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DenominationEntryViewModel.Quantity) ||
                        e.PropertyName == nameof(DenominationEntryViewModel.LineTotal))
                    {
                        RecalculateTotals();
                    }
                };
                DenominationEntries.Add(entry);
            }

            // Load recommended float if opening
            if (!IsClosingCount)
            {
                var recommendation = await _workPeriodService.GetRecommendedFloatAsync();
                foreach (var entry in DenominationEntries)
                {
                    if (recommendation.RecommendedDenominations.TryGetValue(entry.Value, out var qty))
                    {
                        entry.RecommendedQuantity = qty;
                    }
                }
            }

            RecalculateTotals();
        }, "Loading denominations...");
    }

    /// <summary>
    /// Applies the recommended float quantities.
    /// </summary>
    [RelayCommand]
    private void ApplyRecommendedFloat()
    {
        foreach (var entry in DenominationEntries)
        {
            if (entry.RecommendedQuantity > 0)
            {
                entry.Quantity = entry.RecommendedQuantity;
            }
        }
        RecalculateTotals();
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        foreach (var entry in DenominationEntries)
        {
            entry.Quantity = 0;
        }
        RecalculateTotals();
    }

    /// <summary>
    /// Confirms the count.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        if (RequiresExplanation && string.IsNullOrWhiteSpace(VarianceExplanation))
        {
            ErrorMessage = "Please provide an explanation for the cash variance.";
            return;
        }

        DialogResult = true;
    }

    /// <summary>
    /// Cancels the count.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }

    /// <summary>
    /// Gets the denomination count DTO from the current entries.
    /// </summary>
    public CashDenominationCountDto GetCountDto()
    {
        var dto = new CashDenominationCountDto
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        foreach (var entry in DenominationEntries.Where(e => e.Quantity > 0))
        {
            dto.Denominations[entry.Value] = entry.Quantity;
        }

        return dto;
    }

    private void RecalculateTotals()
    {
        TotalNotes = DenominationEntries
            .Where(e => e.Type == DenominationType.Note)
            .Sum(e => e.LineTotal);

        TotalCoins = DenominationEntries
            .Where(e => e.Type == DenominationType.Coin)
            .Sum(e => e.LineTotal);

        GrandTotal = TotalNotes + TotalCoins;

        if (ExpectedCash.HasValue)
        {
            Variance = GrandTotal - ExpectedCash.Value;
            OnPropertyChanged(nameof(VarianceDisplay));
            OnPropertyChanged(nameof(VarianceColor));
            OnPropertyChanged(nameof(RequiresExplanation));
        }
    }
}

/// <summary>
/// ViewModel for a single denomination entry.
/// </summary>
public partial class DenominationEntryViewModel : ObservableObject
{
    /// <summary>
    /// Gets the denomination ID.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the denomination type.
    /// </summary>
    public DenominationType Type { get; }

    /// <summary>
    /// Gets the denomination value.
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets or sets the quantity counted.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int _quantity;

    /// <summary>
    /// Gets the line total (Value x Quantity).
    /// </summary>
    public decimal LineTotal => Value * Quantity;

    /// <summary>
    /// Gets or sets the recommended quantity.
    /// </summary>
    [ObservableProperty]
    private int _recommendedQuantity;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenominationEntryViewModel"/> class.
    /// </summary>
    public DenominationEntryViewModel(CashDenominationDto dto)
    {
        Id = dto.Id;
        Type = dto.Type;
        Value = dto.Value;
        DisplayName = dto.DisplayName;
    }
}
