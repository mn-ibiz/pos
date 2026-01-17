// src/HospitalityPOS.Core/Interfaces/ICustomerDisplayService.cs
// Interface for customer display service supporting VFD and secondary monitors
// Story 43-2: Customer Display Integration

using HospitalityPOS.Core.Models.Hardware;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing customer-facing displays including VFD pole displays
/// and secondary monitor displays.
/// </summary>
public interface ICustomerDisplayService
{
    #region Connection Management

    /// <summary>
    /// Gets whether a display is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current display state.
    /// </summary>
    CustomerDisplayState CurrentState { get; }

    /// <summary>
    /// Gets the active display configuration.
    /// </summary>
    CustomerDisplayConfiguration? ActiveConfiguration { get; }

    /// <summary>
    /// Connects to a customer display with the specified configuration.
    /// </summary>
    /// <param name="configuration">Display configuration.</param>
    /// <returns>Connection result.</returns>
    Task<DisplayConnectionResult> ConnectAsync(CustomerDisplayConfiguration configuration);

    /// <summary>
    /// Disconnects from the current display.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Tests the display connection by showing a test message.
    /// </summary>
    /// <param name="testMessage">Optional test message.</param>
    /// <returns>Test result.</returns>
    Task<DisplayTestResult> TestDisplayAsync(string? testMessage = null);

    /// <summary>
    /// Auto-detects available display devices.
    /// </summary>
    /// <returns>List of detected configurations.</returns>
    Task<IReadOnlyList<CustomerDisplayConfiguration>> AutoDetectDisplaysAsync();

    #endregion

    #region Display Content

    /// <summary>
    /// Clears the display.
    /// </summary>
    Task ClearDisplayAsync();

    /// <summary>
    /// Displays the welcome/idle screen.
    /// </summary>
    Task DisplayWelcomeAsync();

    /// <summary>
    /// Displays a scanned/added item.
    /// </summary>
    /// <param name="item">Item information.</param>
    Task DisplayItemAsync(DisplayItemInfo item);

    /// <summary>
    /// Displays the running total.
    /// </summary>
    /// <param name="total">Total information.</param>
    Task DisplayTotalAsync(DisplayTotalInfo total);

    /// <summary>
    /// Displays an item and the running total together.
    /// </summary>
    /// <param name="item">Item information.</param>
    /// <param name="total">Total information.</param>
    Task DisplayItemAndTotalAsync(DisplayItemInfo item, DisplayTotalInfo total);

    /// <summary>
    /// Displays payment information (amount due, tendered, change).
    /// </summary>
    /// <param name="payment">Payment information.</param>
    Task DisplayPaymentAsync(DisplayPaymentInfo payment);

    /// <summary>
    /// Displays the thank you message after transaction completion.
    /// </summary>
    Task DisplayThankYouAsync();

    /// <summary>
    /// Displays a promotional message.
    /// </summary>
    /// <param name="promotion">Promotion information.</param>
    Task DisplayPromotionAsync(DisplayPromotionInfo promotion);

    /// <summary>
    /// Displays custom text on the display.
    /// </summary>
    /// <param name="line1">First line text.</param>
    /// <param name="line2">Second line text (optional).</param>
    Task DisplayTextAsync(string line1, string? line2 = null);

    /// <summary>
    /// Sets display brightness (VFD only).
    /// </summary>
    /// <param name="level">Brightness level (1-4, where 4 is brightest).</param>
    Task SetBrightnessAsync(int level);

    #endregion

    #region Configuration Management

    /// <summary>
    /// Saves a display configuration.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <returns>Saved configuration with ID.</returns>
    Task<CustomerDisplayConfiguration> SaveConfigurationAsync(CustomerDisplayConfiguration configuration);

    /// <summary>
    /// Gets all saved display configurations.
    /// </summary>
    /// <returns>List of configurations.</returns>
    Task<IReadOnlyList<CustomerDisplayConfiguration>> GetConfigurationsAsync();

    /// <summary>
    /// Gets the active display configuration.
    /// </summary>
    /// <returns>Active configuration or null.</returns>
    Task<CustomerDisplayConfiguration?> GetActiveConfigurationAsync();

    /// <summary>
    /// Deletes a display configuration.
    /// </summary>
    /// <param name="configurationId">Configuration ID to delete.</param>
    Task DeleteConfigurationAsync(int configurationId);

    /// <summary>
    /// Sets a configuration as active.
    /// </summary>
    /// <param name="configurationId">Configuration ID to activate.</param>
    Task SetActiveConfigurationAsync(int configurationId);

    #endregion

    #region Hardware Discovery

    /// <summary>
    /// Gets available serial ports for VFD connection.
    /// </summary>
    /// <returns>List of available ports.</returns>
    Task<IReadOnlyList<DisplayPortInfo>> GetAvailablePortsAsync();

    /// <summary>
    /// Gets available monitors for secondary display.
    /// </summary>
    /// <returns>List of available monitors.</returns>
    Task<IReadOnlyList<MonitorInfo>> GetAvailableMonitorsAsync();

    #endregion

    #region Idle/Promotion Management

    /// <summary>
    /// Sets the list of promotions to display during idle.
    /// </summary>
    /// <param name="promotions">Promotions to cycle through.</param>
    Task SetIdlePromotionsAsync(IEnumerable<DisplayPromotionInfo> promotions);

    /// <summary>
    /// Starts the idle promotion cycle.
    /// </summary>
    Task StartIdleCycleAsync();

    /// <summary>
    /// Stops the idle promotion cycle.
    /// </summary>
    Task StopIdleCycleAsync();

    /// <summary>
    /// Gets whether idle cycle is currently running.
    /// </summary>
    bool IsIdleCycleRunning { get; }

    #endregion

    #region Transaction Integration

    /// <summary>
    /// Called when a new transaction starts.
    /// </summary>
    /// <param name="transactionId">Transaction identifier.</param>
    Task OnTransactionStartAsync(string transactionId);

    /// <summary>
    /// Called when an item is added to the transaction.
    /// </summary>
    /// <param name="item">Item information.</param>
    /// <param name="runningTotal">Current running total.</param>
    Task OnItemAddedAsync(DisplayItemInfo item, DisplayTotalInfo runningTotal);

    /// <summary>
    /// Called when an item is removed from the transaction.
    /// </summary>
    /// <param name="runningTotal">Current running total after removal.</param>
    Task OnItemRemovedAsync(DisplayTotalInfo runningTotal);

    /// <summary>
    /// Called when payment is initiated.
    /// </summary>
    /// <param name="amountDue">Amount due.</param>
    Task OnPaymentStartAsync(decimal amountDue);

    /// <summary>
    /// Called when payment is received.
    /// </summary>
    /// <param name="payment">Payment information.</param>
    Task OnPaymentReceivedAsync(DisplayPaymentInfo payment);

    /// <summary>
    /// Called when transaction is completed.
    /// </summary>
    Task OnTransactionCompleteAsync();

    /// <summary>
    /// Called when transaction is voided/cancelled.
    /// </summary>
    Task OnTransactionVoidedAsync();

    #endregion

    #region Events

    /// <summary>
    /// Raised when display state changes.
    /// </summary>
    event EventHandler<DisplayStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raised when display is connected.
    /// </summary>
    event EventHandler<DisplayConnectionResult>? Connected;

    /// <summary>
    /// Raised when display is disconnected.
    /// </summary>
    event EventHandler? Disconnected;

    /// <summary>
    /// Raised when a display error occurs.
    /// </summary>
    event EventHandler<DisplayErrorEventArgs>? Error;

    #endregion
}
