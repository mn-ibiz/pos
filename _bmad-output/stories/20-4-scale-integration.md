# Story 20.4: Scale Integration

## Story
**As a** cashier,
**I want to** weigh items directly at checkout,
**So that** loose produce can be priced accurately.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/BarcodeService.cs` - Scale integration with:
  - `ConnectScaleAsync` - Connect to serial port scale
  - `GetWeightAsync` - Read current weight
  - `SetTareAsync` / `ClearTareAsync` - Tare weight handling
  - Real-time weight display support

## Epic
**Epic 20: Barcode, Scale & PLU Management**

## Context
Integrated checkout scales allow cashiers to weigh items at the counter for accurate pricing. The system must communicate with various scale protocols and handle weight readings in real-time.

## Acceptance Criteria

### AC1: Real-Time Weight Display
**Given** an integrated scale is connected
**When** placing item on scale
**Then**:
- Weight is displayed in real-time
- Updates as weight changes
- Shows weight unit (kg or g)
- Stable indicator when weight settles

### AC2: Weight Acceptance
**Given** item requires weighing
**When** weight is stable
**Then**:
- Cashier can accept weight (Enter key or button)
- Price calculated as weight × unit price
- Item added to order with weight displayed

### AC3: Tare Weight
**Given** tare weight is needed (container)
**When** pressing tare button
**Then**:
- Current weight set as tare
- Subsequent readings subtract tare
- Tare can be reset to zero

### AC4: Scale Protocol Support
**Given** different scale models
**When** configuring system
**Then** supports protocols:
- Mettler Toledo
- CAS (common in Kenya)
- Generic serial protocol
- USB HID scales

## Technical Notes

### Scale Communication Interface
```csharp
public interface IScaleService
{
    event EventHandler<WeightReadingEventArgs> WeightChanged;
    event EventHandler<bool> StabilityChanged;

    Task<bool> ConnectAsync();
    void Disconnect();
    Task<decimal> GetCurrentWeightAsync();
    Task SetTareAsync();
    Task ClearTareAsync();
    bool IsConnected { get; }
    bool IsStable { get; }
    decimal CurrentWeight { get; }
}

public class WeightReadingEventArgs : EventArgs
{
    public decimal Weight { get; set; }
    public WeightUnit Unit { get; set; }
    public bool IsStable { get; set; }
}

public enum WeightUnit
{
    Kilograms,
    Grams,
    Pounds,
    Ounces
}
```

### Scale Implementation (CAS Protocol Example)
```csharp
public class CASScaleService : IScaleService, IDisposable
{
    private SerialPort _port;
    private decimal _tare;
    private readonly Timer _pollTimer;

    public async Task<bool> ConnectAsync()
    {
        var config = await _configService.GetScaleConfigAsync();

        _port = new SerialPort(config.PortName, config.BaudRate)
        {
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            ReadTimeout = 500
        };

        try
        {
            _port.Open();
            _pollTimer.Start();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to scale on {Port}", config.PortName);
            return false;
        }
    }

    private void PollWeight(object state)
    {
        try
        {
            // Send weight request command (varies by protocol)
            _port.Write("W\r\n");
            Thread.Sleep(50);

            var response = _port.ReadLine();
            var weight = ParseWeightResponse(response);

            var netWeight = weight - _tare;
            var isStable = response.Contains("ST");

            CurrentWeight = netWeight;
            IsStable = isStable;

            WeightChanged?.Invoke(this, new WeightReadingEventArgs
            {
                Weight = netWeight,
                Unit = WeightUnit.Kilograms,
                IsStable = isStable
            });
        }
        catch (TimeoutException)
        {
            // No response from scale
        }
    }

    public Task SetTareAsync()
    {
        _tare = CurrentWeight + _tare;  // Add to existing tare
        return Task.CompletedTask;
    }
}
```

### ViewModel Integration
```csharp
public partial class WeightEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _displayWeight;

    [ObservableProperty]
    private bool _isStable;

    [ObservableProperty]
    private string _weightDisplay;

    public WeightEntryViewModel(IScaleService scaleService)
    {
        _scaleService = scaleService;
        _scaleService.WeightChanged += OnWeightChanged;
        _scaleService.StabilityChanged += OnStabilityChanged;
    }

    private void OnWeightChanged(object sender, WeightReadingEventArgs e)
    {
        DisplayWeight = e.Weight;
        IsStable = e.IsStable;
        WeightDisplay = $"{e.Weight:F3} {GetUnitString(e.Unit)}";
    }

    [RelayCommand]
    private async Task AcceptWeightAsync()
    {
        if (!IsStable)
        {
            ShowMessage("Please wait for weight to stabilize");
            return;
        }

        await _posViewModel.AddWeighedItemAsync(CurrentProduct, DisplayWeight);
        Close();
    }

    [RelayCommand]
    private async Task TareAsync()
    {
        await _scaleService.SetTareAsync();
    }
}
```

### UI Display
```
┌────────────────────────────────────────┐
│          WEIGH ITEM                     │
├────────────────────────────────────────┤
│                                         │
│  Product: Bananas (PLU 4011)           │
│  Price: KSh 120.00 / kg                │
│                                         │
│         ┌─────────────────────┐        │
│         │                     │        │
│         │      1.250 kg       │  ← Green when stable
│         │         ⚖️          │        │
│         └─────────────────────┘        │
│                                         │
│  Calculated Price: KSh 150.00          │
│                                         │
│  ┌────────────┐  ┌────────────────┐   │
│  │   TARE     │  │  ACCEPT WEIGHT │   │
│  └────────────┘  └────────────────┘   │
│                                         │
│              [CANCEL]                   │
│                                         │
└────────────────────────────────────────┘
```

## Dependencies
- Story 20.3: PLU Code Quick Entry
- Epic 4: Product Management (IsSoldByWeight, UnitPrice)

## Files to Create/Modify
- `HospitalityPOS.Core/Interfaces/IScaleService.cs`
- `HospitalityPOS.Infrastructure/Hardware/CASScaleService.cs`
- `HospitalityPOS.Infrastructure/Hardware/MettlerScaleService.cs`
- `HospitalityPOS.Infrastructure/Hardware/USBScaleService.cs`
- `HospitalityPOS.WPF/ViewModels/POS/WeightEntryViewModel.cs`
- `HospitalityPOS.WPF/Views/POS/WeightEntryDialog.xaml`
- Scale configuration in appsettings.json

## Testing Requirements
- Unit tests with mock scale service
- Integration tests with scale simulator
- Tests for tare functionality
- Tests for multiple scale protocols

## Definition of Done
- [ ] Real-time weight display working
- [ ] Stability indicator accurate
- [ ] Tare functionality working
- [ ] At least one scale protocol implemented
- [ ] Weight acceptance flow complete
- [ ] Price calculation accurate
- [ ] Unit tests passing
- [ ] Code reviewed and approved
