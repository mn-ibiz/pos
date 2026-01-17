using HospitalityPOS.Core.Models.Hardware;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for weight scale integration.
/// Supports USB HID and RS-232 serial scales for supermarket weighed products.
/// </summary>
public interface IScaleService : IDisposable
{
    #region Connection

    /// <summary>
    /// Gets whether the scale is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current scale status.
    /// </summary>
    ScaleStatus Status { get; }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    ScaleConfiguration? CurrentConfiguration { get; }

    /// <summary>
    /// Connects to a scale using the specified configuration.
    /// </summary>
    /// <param name="config">The scale configuration.</param>
    Task<ScaleConnectionResult> ConnectAsync(ScaleConfiguration config);

    /// <summary>
    /// Connects to a scale using the active configuration.
    /// </summary>
    Task<ScaleConnectionResult> ConnectAsync();

    /// <summary>
    /// Disconnects from the currently connected scale.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Tests the connection to a scale with the given configuration.
    /// </summary>
    /// <param name="config">The configuration to test.</param>
    Task<ScaleTestResult> TestConnectionAsync(ScaleConfiguration config);

    #endregion

    #region Weight Reading

    /// <summary>
    /// Reads the current weight from the scale.
    /// </summary>
    Task<WeightReading> ReadWeightAsync();

    /// <summary>
    /// Waits for a stable weight reading.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    Task<WeightReading?> WaitForStableWeightAsync(int timeoutMs = 5000);

    /// <summary>
    /// Gets the last weight reading (cached).
    /// </summary>
    WeightReading? LastReading { get; }

    /// <summary>
    /// Starts continuous weight monitoring.
    /// </summary>
    /// <param name="intervalMs">Reading interval in milliseconds.</param>
    Task StartContinuousReadingAsync(int intervalMs = 200);

    /// <summary>
    /// Stops continuous weight monitoring.
    /// </summary>
    Task StopContinuousReadingAsync();

    /// <summary>
    /// Gets whether continuous reading is active.
    /// </summary>
    bool IsContinuousReadingActive { get; }

    #endregion

    #region Tare and Zero

    /// <summary>
    /// Tares the scale (sets current weight as zero).
    /// </summary>
    Task<bool> TareAsync();

    /// <summary>
    /// Zeros the scale.
    /// </summary>
    Task<bool> ZeroAsync();

    /// <summary>
    /// Sets a manual tare weight.
    /// </summary>
    /// <param name="tareWeight">The tare weight to apply.</param>
    Task<bool> SetTareWeightAsync(decimal tareWeight);

    /// <summary>
    /// Clears the current tare.
    /// </summary>
    Task<bool> ClearTareAsync();

    /// <summary>
    /// Gets the current tare weight.
    /// </summary>
    decimal CurrentTareWeight { get; }

    #endregion

    #region Configuration

    /// <summary>
    /// Gets all scale configurations.
    /// </summary>
    Task<List<ScaleConfiguration>> GetConfigurationsAsync();

    /// <summary>
    /// Gets the active scale configuration.
    /// </summary>
    Task<ScaleConfiguration?> GetActiveConfigurationAsync();

    /// <summary>
    /// Saves a scale configuration.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    Task<ScaleConfiguration> SaveConfigurationAsync(ScaleConfiguration config);

    /// <summary>
    /// Deletes a scale configuration.
    /// </summary>
    /// <param name="configId">The configuration ID to delete.</param>
    Task<bool> DeleteConfigurationAsync(int configId);

    /// <summary>
    /// Sets a configuration as active.
    /// </summary>
    /// <param name="configId">The configuration ID to activate.</param>
    Task<bool> SetActiveConfigurationAsync(int configId);

    /// <summary>
    /// Gets available serial ports.
    /// </summary>
    Task<List<AvailablePort>> GetAvailablePortsAsync();

    /// <summary>
    /// Attempts to auto-detect connected scales.
    /// </summary>
    Task<List<ScaleConfiguration>> AutoDetectScalesAsync();

    #endregion

    #region Product Configuration

    /// <summary>
    /// Gets weighed product configuration.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    Task<WeighedProductConfig?> GetWeighedProductConfigAsync(int productId);

    /// <summary>
    /// Sets weighed product configuration.
    /// </summary>
    /// <param name="config">The product weight configuration.</param>
    Task<WeighedProductConfig> SetWeighedProductConfigAsync(WeighedProductConfig config);

    /// <summary>
    /// Gets all weighed products.
    /// </summary>
    Task<List<WeighedProductConfig>> GetAllWeighedProductsAsync();

    /// <summary>
    /// Checks if a product is sold by weight.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    Task<bool> IsProductWeighedAsync(int productId);

    #endregion

    #region Order Integration

    /// <summary>
    /// Creates a weighed order item from current scale reading.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    Task<WeighedOrderItem> CreateWeighedOrderItemAsync(int productId);

    /// <summary>
    /// Creates a weighed order item with specified weight.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="weight">The weight.</param>
    /// <param name="unit">The weight unit.</param>
    Task<WeighedOrderItem> CreateWeighedOrderItemAsync(int productId, decimal weight, WeightUnit unit);

    /// <summary>
    /// Calculates the price for a weighed product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="weight">The weight.</param>
    /// <param name="unit">The weight unit.</param>
    Task<decimal> CalculatePriceAsync(int productId, decimal weight, WeightUnit unit);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when weight changes.
    /// </summary>
    event EventHandler<WeightChangedEventArgs>? WeightChanged;

    /// <summary>
    /// Event raised when a stable weight is detected.
    /// </summary>
    event EventHandler<WeightChangedEventArgs>? StableWeightDetected;

    /// <summary>
    /// Event raised when scale status changes.
    /// </summary>
    event EventHandler<ScaleStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Event raised when scale is disconnected unexpectedly.
    /// </summary>
    event EventHandler? Disconnected;

    /// <summary>
    /// Event raised when scale detects overload.
    /// </summary>
    event EventHandler? Overload;

    #endregion

    #region Utility

    /// <summary>
    /// Converts weight between units.
    /// </summary>
    /// <param name="weight">The weight to convert.</param>
    /// <param name="fromUnit">The source unit.</param>
    /// <param name="toUnit">The target unit.</param>
    decimal ConvertWeight(decimal weight, WeightUnit fromUnit, WeightUnit toUnit);

    /// <summary>
    /// Formats a weight for display.
    /// </summary>
    /// <param name="weight">The weight.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="decimalPlaces">Number of decimal places.</param>
    string FormatWeight(decimal weight, WeightUnit unit, int decimalPlaces = 3);

    /// <summary>
    /// Formats a receipt line for a weighed item.
    /// </summary>
    /// <param name="item">The weighed order item.</param>
    string FormatReceiptLine(WeighedOrderItem item);

    #endregion
}
