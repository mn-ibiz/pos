namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Business mode options for the POS system.
/// </summary>
public enum BusinessMode
{
    /// <summary>
    /// Restaurant/hospitality mode with table management, kitchen display, and waiter assignment.
    /// </summary>
    Restaurant = 1,

    /// <summary>
    /// Supermarket/retail mode with barcode scanning, product offers, and payroll.
    /// </summary>
    Supermarket = 2,

    /// <summary>
    /// Hybrid mode with all features enabled.
    /// </summary>
    Hybrid = 3
}

/// <summary>
/// System configuration entity storing business mode and feature flags.
/// </summary>
public class SystemConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode Mode { get; set; } = BusinessMode.Restaurant;

    /// <summary>
    /// Gets or sets the business name.
    /// </summary>
    public string BusinessName { get; set; } = "My Business";

    /// <summary>
    /// Gets or sets the business address.
    /// </summary>
    public string? BusinessAddress { get; set; }

    /// <summary>
    /// Gets or sets the business phone number.
    /// </summary>
    public string? BusinessPhone { get; set; }

    /// <summary>
    /// Gets or sets the business email.
    /// </summary>
    public string? BusinessEmail { get; set; }

    /// <summary>
    /// Gets or sets the business tax registration number (legacy field).
    /// </summary>
    public string? TaxRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the KRA PIN number (Kenya-specific).
    /// Format: A followed by 9 digits and ending with a letter (e.g., A123456789Z).
    /// </summary>
    public string? KraPinNumber { get; set; }

    /// <summary>
    /// Gets or sets the VAT registration number (Kenya-specific).
    /// </summary>
    public string? VatRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the default tax rate (VAT rate in Kenya is 16%).
    /// </summary>
    public decimal DefaultTaxRate { get; set; } = 16m;

    /// <summary>
    /// Gets or sets the currency code (e.g., KES, USD).
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Gets or sets the currency symbol.
    /// </summary>
    public string CurrencySymbol { get; set; } = "Ksh";

    #region Restaurant/Hospitality Features

    /// <summary>
    /// Gets or sets whether table management is enabled.
    /// </summary>
    public bool EnableTableManagement { get; set; } = true;

    /// <summary>
    /// Gets or sets whether kitchen display/printing is enabled.
    /// </summary>
    public bool EnableKitchenDisplay { get; set; } = true;

    /// <summary>
    /// Gets or sets whether waiter assignment is enabled.
    /// </summary>
    public bool EnableWaiterAssignment { get; set; } = true;

    /// <summary>
    /// Gets or sets whether course sequencing is enabled.
    /// </summary>
    public bool EnableCourseSequencing { get; set; } = false;

    /// <summary>
    /// Gets or sets whether reservations are enabled.
    /// </summary>
    public bool EnableReservations { get; set; } = false;

    #endregion

    #region Retail/Supermarket Features

    /// <summary>
    /// Gets or sets whether barcode auto-focus is enabled.
    /// </summary>
    public bool EnableBarcodeAutoFocus { get; set; } = false;

    /// <summary>
    /// Gets or sets whether product offers/promotions are enabled.
    /// </summary>
    public bool EnableProductOffers { get; set; } = false;

    /// <summary>
    /// Gets or sets whether supplier credit management is enabled.
    /// </summary>
    public bool EnableSupplierCredit { get; set; } = false;

    /// <summary>
    /// Gets or sets whether loyalty program is enabled.
    /// </summary>
    public bool EnableLoyaltyProgram { get; set; } = false;

    /// <summary>
    /// Gets or sets whether batch/expiry tracking is enabled.
    /// </summary>
    public bool EnableBatchExpiry { get; set; } = false;

    /// <summary>
    /// Gets or sets whether scale integration is enabled.
    /// </summary>
    public bool EnableScaleIntegration { get; set; } = false;

    #endregion

    #region Shared/Enterprise Features

    /// <summary>
    /// Gets or sets whether payroll is enabled.
    /// </summary>
    public bool EnablePayroll { get; set; } = false;

    /// <summary>
    /// Gets or sets whether accounting module is enabled.
    /// </summary>
    public bool EnableAccounting { get; set; } = false;

    /// <summary>
    /// Gets or sets whether multi-store is enabled.
    /// </summary>
    public bool EnableMultiStore { get; set; } = false;

    /// <summary>
    /// Gets or sets whether cloud sync is enabled.
    /// </summary>
    public bool EnableCloudSync { get; set; } = false;

    #endregion

    #region Kenya-Specific Features

    /// <summary>
    /// Gets or sets whether Kenya eTIMS compliance is enabled.
    /// </summary>
    public bool EnableKenyaETims { get; set; } = true;

    /// <summary>
    /// Gets or sets whether M-Pesa integration is enabled.
    /// </summary>
    public bool EnableMpesa { get; set; } = true;

    #endregion

    /// <summary>
    /// Gets or sets whether setup has been completed.
    /// </summary>
    public bool SetupCompleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the setup completion date.
    /// </summary>
    public DateTime? SetupCompletedAt { get; set; }

    /// <summary>
    /// Gets or sets when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the configuration was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Applies default feature flags based on the current mode.
    /// </summary>
    public void ApplyModeDefaults()
    {
        switch (Mode)
        {
            case BusinessMode.Restaurant:
                ApplyRestaurantDefaults();
                break;

            case BusinessMode.Supermarket:
                ApplySupermarketDefaults();
                break;

            case BusinessMode.Hybrid:
                ApplyHybridDefaults();
                break;
        }
    }

    private void ApplyRestaurantDefaults()
    {
        // Restaurant features enabled
        EnableTableManagement = true;
        EnableKitchenDisplay = true;
        EnableWaiterAssignment = true;
        EnableCourseSequencing = false;
        EnableReservations = false;

        // Retail features disabled by default
        EnableBarcodeAutoFocus = false;
        EnableProductOffers = false;
        EnableSupplierCredit = false;
        EnableLoyaltyProgram = false;
        EnableBatchExpiry = false;
        EnableScaleIntegration = false;

        // Enterprise features disabled by default
        EnablePayroll = false;
        EnableAccounting = false;
        EnableMultiStore = false;
        EnableCloudSync = false;
    }

    private void ApplySupermarketDefaults()
    {
        // Restaurant features disabled
        EnableTableManagement = false;
        EnableKitchenDisplay = false;
        EnableWaiterAssignment = false;
        EnableCourseSequencing = false;
        EnableReservations = false;

        // Retail features enabled
        EnableBarcodeAutoFocus = true;
        EnableProductOffers = true;
        EnableSupplierCredit = true;
        EnableLoyaltyProgram = true;
        EnableBatchExpiry = true;
        EnableScaleIntegration = true;

        // Enterprise features available
        EnablePayroll = true;
        EnableAccounting = false;
        EnableMultiStore = false;
        EnableCloudSync = false;
    }

    private void ApplyHybridDefaults()
    {
        // All features enabled
        EnableTableManagement = true;
        EnableKitchenDisplay = true;
        EnableWaiterAssignment = true;
        EnableCourseSequencing = true;
        EnableReservations = true;

        EnableBarcodeAutoFocus = true;
        EnableProductOffers = true;
        EnableSupplierCredit = true;
        EnableLoyaltyProgram = true;
        EnableBatchExpiry = true;
        EnableScaleIntegration = true;

        EnablePayroll = true;
        EnableAccounting = true;
        EnableMultiStore = false;
        EnableCloudSync = false;
    }

    /// <summary>
    /// Gets a display-friendly name for the current mode.
    /// </summary>
    public string GetModeDisplayName()
    {
        return Mode switch
        {
            BusinessMode.Restaurant => "Restaurant / Hospitality",
            BusinessMode.Supermarket => "Supermarket / Retail",
            BusinessMode.Hybrid => "Hybrid (All Features)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets a description for the current mode.
    /// </summary>
    public string GetModeDescription()
    {
        return Mode switch
        {
            BusinessMode.Restaurant => "Table management, kitchen display, waiter assignment, and hospitality features.",
            BusinessMode.Supermarket => "Barcode scanning, product offers, loyalty program, and retail features.",
            BusinessMode.Hybrid => "All features enabled for businesses that need both hospitality and retail capabilities.",
            _ => ""
        };
    }
}
