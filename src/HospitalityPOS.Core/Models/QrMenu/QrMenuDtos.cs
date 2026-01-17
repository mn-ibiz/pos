// src/HospitalityPOS.Core/Models/QrMenu/QrMenuDtos.cs
// DTOs for QR Menu and Contactless Ordering
// Story 44-1: QR Menu and Contactless Ordering

namespace HospitalityPOS.Core.Models.QrMenu;

/// <summary>
/// Order source type for tracking.
/// </summary>
public enum OrderSource
{
    /// <summary>Order placed at POS terminal.</summary>
    Pos,

    /// <summary>Order placed via QR menu.</summary>
    QrMenu,

    /// <summary>Order placed via online website.</summary>
    Online,

    /// <summary>Order placed via phone call.</summary>
    Phone,

    /// <summary>Order from third-party delivery app.</summary>
    ThirdParty
}

/// <summary>
/// QR menu order status.
/// </summary>
public enum QrOrderStatus
{
    /// <summary>Order just submitted.</summary>
    Pending,

    /// <summary>Order received by kitchen.</summary>
    Received,

    /// <summary>Order being prepared.</summary>
    Preparing,

    /// <summary>Order ready for serving/pickup.</summary>
    Ready,

    /// <summary>Order has been served.</summary>
    Served,

    /// <summary>Order has been paid.</summary>
    Paid,

    /// <summary>Order was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Payment option for QR menu orders.
/// </summary>
public enum QrPaymentOption
{
    /// <summary>Pay at counter after receiving order.</summary>
    PayAtCounter,

    /// <summary>Pay via M-Pesa using QR code.</summary>
    MpesaQr,

    /// <summary>Add to hotel room bill.</summary>
    RoomCharge,

    /// <summary>Pay with card online.</summary>
    CardOnline,

    /// <summary>Pay cash on delivery.</summary>
    CashOnDelivery
}

/// <summary>
/// Configuration for QR menu system.
/// </summary>
public class QrMenuConfiguration
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Whether QR menu ordering is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Base URL for the QR menu web app.</summary>
    public string BaseUrl { get; set; } = "https://menu.example.com";

    /// <summary>Store/restaurant name to display.</summary>
    public string StoreName { get; set; } = "Our Restaurant";

    /// <summary>Logo URL for the menu.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Primary brand color (hex).</summary>
    public string PrimaryColor { get; set; } = "#1a1a2e";

    /// <summary>Secondary brand color (hex).</summary>
    public string SecondaryColor { get; set; } = "#22C55E";

    /// <summary>Welcome message on menu.</summary>
    public string WelcomeMessage { get; set; } = "Welcome! Browse our menu and order from your table.";

    /// <summary>Whether customer name is required.</summary>
    public bool RequireCustomerName { get; set; }

    /// <summary>Whether customer phone is required.</summary>
    public bool RequireCustomerPhone { get; set; }

    /// <summary>Allowed payment options.</summary>
    public List<QrPaymentOption> AllowedPaymentOptions { get; set; } = new() { QrPaymentOption.PayAtCounter };

    /// <summary>Whether to show estimated wait times.</summary>
    public bool ShowEstimatedWaitTime { get; set; } = true;

    /// <summary>Default estimated wait time in minutes.</summary>
    public int DefaultWaitTimeMinutes { get; set; } = 15;

    /// <summary>Whether to allow order modifications after submission.</summary>
    public bool AllowOrderModification { get; set; }

    /// <summary>Minimum order amount (0 = no minimum).</summary>
    public decimal MinimumOrderAmount { get; set; }

    /// <summary>Currency symbol.</summary>
    public string CurrencySymbol { get; set; } = "KSh";

    /// <summary>Currency code.</summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>Opening time (24h format).</summary>
    public TimeOnly? OpeningTime { get; set; }

    /// <summary>Closing time (24h format).</summary>
    public TimeOnly? ClosingTime { get; set; }

    /// <summary>Maximum orders per table per hour (0 = unlimited).</summary>
    public int MaxOrdersPerTablePerHour { get; set; }

    /// <summary>Whether to show allergen information.</summary>
    public bool ShowAllergenInfo { get; set; } = true;

    /// <summary>Whether to show nutritional info.</summary>
    public bool ShowNutritionalInfo { get; set; }

    /// <summary>Footer text for receipts/confirmations.</summary>
    public string? FooterText { get; set; }

    /// <summary>Created date.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last updated date.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// QR code information for a table.
/// </summary>
public class TableQrCode
{
    /// <summary>Table identifier.</summary>
    public int TableId { get; set; }

    /// <summary>Table name/number.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Location/section (e.g., "Outdoor", "Main Hall").</summary>
    public string? Location { get; set; }

    /// <summary>Full URL encoded in QR code.</summary>
    public string MenuUrl { get; set; } = string.Empty;

    /// <summary>QR code image as Base64 PNG.</summary>
    public string QrCodeBase64 { get; set; } = string.Empty;

    /// <summary>QR code image as byte array.</summary>
    public byte[]? QrCodeBytes { get; set; }

    /// <summary>When QR was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Number of times this QR has been scanned.</summary>
    public int ScanCount { get; set; }
}

/// <summary>
/// Category for QR menu display.
/// </summary>
public class QrMenuCategory
{
    /// <summary>Category ID.</summary>
    public int Id { get; set; }

    /// <summary>Category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Category description.</summary>
    public string? Description { get; set; }

    /// <summary>Category image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether category is currently available.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Availability start time.</summary>
    public TimeOnly? AvailableFrom { get; set; }

    /// <summary>Availability end time.</summary>
    public TimeOnly? AvailableUntil { get; set; }

    /// <summary>Number of items in this category.</summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Product item for QR menu display.
/// </summary>
public class QrMenuProduct
{
    /// <summary>Product ID.</summary>
    public int Id { get; set; }

    /// <summary>Category ID.</summary>
    public int CategoryId { get; set; }

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description for QR menu.</summary>
    public string? Description { get; set; }

    /// <summary>Detailed description.</summary>
    public string? DetailedDescription { get; set; }

    /// <summary>Product image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Price.</summary>
    public decimal Price { get; set; }

    /// <summary>Formatted price.</summary>
    public string FormattedPrice { get; set; } = string.Empty;

    /// <summary>Whether product is currently available.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Whether product is in stock.</summary>
    public bool InStock { get; set; } = true;

    /// <summary>Display order within category.</summary>
    public int SortOrder { get; set; }

    /// <summary>Allergen information.</summary>
    public List<string> Allergens { get; set; } = new();

    /// <summary>Dietary tags (Vegetarian, Vegan, Halal, etc.).</summary>
    public List<string> DietaryTags { get; set; } = new();

    /// <summary>Spiciness level (0-5).</summary>
    public int? SpicyLevel { get; set; }

    /// <summary>Preparation time in minutes.</summary>
    public int? PrepTimeMinutes { get; set; }

    /// <summary>Whether this is a featured/popular item.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Whether this item is new.</summary>
    public bool IsNew { get; set; }

    /// <summary>Available modifiers/options.</summary>
    public List<QrMenuModifier> Modifiers { get; set; } = new();
}

/// <summary>
/// Modifier option for products.
/// </summary>
public class QrMenuModifier
{
    /// <summary>Modifier group ID.</summary>
    public int GroupId { get; set; }

    /// <summary>Modifier group name (e.g., "Size", "Extra Toppings").</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Whether selection is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Minimum selections.</summary>
    public int MinSelections { get; set; }

    /// <summary>Maximum selections (0 = unlimited).</summary>
    public int MaxSelections { get; set; } = 1;

    /// <summary>Available options.</summary>
    public List<QrMenuModifierOption> Options { get; set; } = new();
}

/// <summary>
/// Individual modifier option.
/// </summary>
public class QrMenuModifierOption
{
    /// <summary>Option ID.</summary>
    public int Id { get; set; }

    /// <summary>Option name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Additional price for this option.</summary>
    public decimal PriceAdjustment { get; set; }

    /// <summary>Whether this is the default selection.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether option is available.</summary>
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Cart item for QR menu order.
/// </summary>
public class QrCartItem
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Selected modifier IDs.</summary>
    public List<int> SelectedModifiers { get; set; } = new();

    /// <summary>Modifier price adjustments total.</summary>
    public decimal ModifierTotal { get; set; }

    /// <summary>Special instructions/notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Line total (UnitPrice + ModifierTotal) × Quantity.</summary>
    public decimal LineTotal => (UnitPrice + ModifierTotal) * Quantity;
}

/// <summary>
/// Order submission from QR menu.
/// </summary>
public class QrMenuOrderRequest
{
    /// <summary>Table ID.</summary>
    public int TableId { get; set; }

    /// <summary>Table name/number.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Customer name (optional).</summary>
    public string? CustomerName { get; set; }

    /// <summary>Customer phone (optional).</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Customer email (optional).</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>Order items.</summary>
    public List<QrCartItem> Items { get; set; } = new();

    /// <summary>Selected payment option.</summary>
    public QrPaymentOption PaymentOption { get; set; } = QrPaymentOption.PayAtCounter;

    /// <summary>Special instructions for entire order.</summary>
    public string? OrderNotes { get; set; }

    /// <summary>Room number (for hotel room charge).</summary>
    public string? RoomNumber { get; set; }

    /// <summary>Guest name (for hotel room charge).</summary>
    public string? GuestName { get; set; }

    /// <summary>Device/session identifier for tracking.</summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Response after order submission.
/// </summary>
public class QrMenuOrderResponse
{
    /// <summary>Whether order was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Order ID if successful.</summary>
    public int? OrderId { get; set; }

    /// <summary>Order confirmation code.</summary>
    public string? ConfirmationCode { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Current order status.</summary>
    public QrOrderStatus Status { get; set; }

    /// <summary>Estimated wait time in minutes.</summary>
    public int? EstimatedWaitMinutes { get; set; }

    /// <summary>Order total.</summary>
    public decimal OrderTotal { get; set; }

    /// <summary>Position in queue (if applicable).</summary>
    public int? QueuePosition { get; set; }

    /// <summary>Timestamp of order.</summary>
    public DateTime OrderTime { get; set; } = DateTime.UtcNow;

    /// <summary>Create a success response.</summary>
    public static QrMenuOrderResponse Successful(int orderId, string confirmationCode, decimal total, int? waitMinutes = null)
        => new()
        {
            Success = true,
            OrderId = orderId,
            ConfirmationCode = confirmationCode,
            Status = QrOrderStatus.Received,
            OrderTotal = total,
            EstimatedWaitMinutes = waitMinutes
        };

    /// <summary>Create a failure response.</summary>
    public static QrMenuOrderResponse Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Order status update.
/// </summary>
public class QrOrderStatusUpdate
{
    /// <summary>Order ID.</summary>
    public int OrderId { get; set; }

    /// <summary>Confirmation code.</summary>
    public string ConfirmationCode { get; set; } = string.Empty;

    /// <summary>Current status.</summary>
    public QrOrderStatus Status { get; set; }

    /// <summary>Status message.</summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>Updated estimated wait time.</summary>
    public int? EstimatedWaitMinutes { get; set; }

    /// <summary>Timestamp of update.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// QR menu analytics data.
/// </summary>
public class QrMenuAnalytics
{
    /// <summary>Date range start.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Date range end.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Total QR code scans.</summary>
    public int TotalScans { get; set; }

    /// <summary>Unique visitors (by session).</summary>
    public int UniqueVisitors { get; set; }

    /// <summary>Total orders placed via QR.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Total revenue from QR orders.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Average order value.</summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>Conversion rate (orders / scans).</summary>
    public decimal ConversionRate { get; set; }

    /// <summary>Most popular items.</summary>
    public List<QrMenuPopularItem> PopularItems { get; set; } = new();

    /// <summary>Orders by hour of day.</summary>
    public Dictionary<int, int> OrdersByHour { get; set; } = new();

    /// <summary>Orders by day of week.</summary>
    public Dictionary<DayOfWeek, int> OrdersByDayOfWeek { get; set; } = new();

    /// <summary>Average wait time in minutes.</summary>
    public decimal AverageWaitTime { get; set; }

    /// <summary>Comparison to POS orders.</summary>
    public QrVsPosComparison Comparison { get; set; } = new();
}

/// <summary>
/// Popular item in QR menu.
/// </summary>
public class QrMenuPopularItem
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Number of times ordered.</summary>
    public int OrderCount { get; set; }

    /// <summary>Total revenue from this item.</summary>
    public decimal Revenue { get; set; }
}

/// <summary>
/// Comparison between QR and POS orders.
/// </summary>
public class QrVsPosComparison
{
    /// <summary>QR order count.</summary>
    public int QrOrderCount { get; set; }

    /// <summary>POS order count.</summary>
    public int PosOrderCount { get; set; }

    /// <summary>QR order percentage.</summary>
    public decimal QrPercentage => QrOrderCount + PosOrderCount > 0
        ? (decimal)QrOrderCount / (QrOrderCount + PosOrderCount) * 100
        : 0;

    /// <summary>QR average order value.</summary>
    public decimal QrAverageOrderValue { get; set; }

    /// <summary>POS average order value.</summary>
    public decimal PosAverageOrderValue { get; set; }
}

/// <summary>
/// Notification when QR order is received.
/// </summary>
public class QrOrderNotification
{
    /// <summary>Order ID.</summary>
    public int OrderId { get; set; }

    /// <summary>Confirmation code.</summary>
    public string ConfirmationCode { get; set; } = string.Empty;

    /// <summary>Table ID.</summary>
    public int TableId { get; set; }

    /// <summary>Table name.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Customer name if provided.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Number of items.</summary>
    public int ItemCount { get; set; }

    /// <summary>Order total.</summary>
    public decimal OrderTotal { get; set; }

    /// <summary>Order time.</summary>
    public DateTime OrderTime { get; set; }

    /// <summary>Whether order has special instructions.</summary>
    public bool HasSpecialInstructions { get; set; }

    /// <summary>Notification sound type.</summary>
    public string SoundType { get; set; } = "qr_order";
}

/// <summary>
/// QR code print template.
/// </summary>
public class QrCodePrintTemplate
{
    /// <summary>Table name/number.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>QR code as Base64 image.</summary>
    public string QrCodeBase64 { get; set; } = string.Empty;

    /// <summary>Store/restaurant name.</summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>Instruction text.</summary>
    public string InstructionText { get; set; } = "Scan to view menu and order";

    /// <summary>Store logo URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>WiFi information (optional).</summary>
    public string? WifiInfo { get; set; }
}
