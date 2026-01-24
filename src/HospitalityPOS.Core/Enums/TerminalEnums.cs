namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Defines the type/purpose of a POS terminal.
/// </summary>
public enum TerminalType
{
    /// <summary>
    /// Supermarket checkout register.
    /// </summary>
    Register = 1,

    /// <summary>
    /// Hotel/Restaurant service point.
    /// </summary>
    Till = 2,

    /// <summary>
    /// Back office admin terminal.
    /// </summary>
    AdminWorkstation = 3,

    /// <summary>
    /// Kitchen display station (KDS).
    /// </summary>
    KitchenDisplay = 4,

    /// <summary>
    /// Handheld mobile device (future).
    /// </summary>
    MobileTerminal = 5,

    /// <summary>
    /// Self-service checkout kiosk (future).
    /// </summary>
    SelfCheckout = 6
}

/// <summary>
/// Defines the business mode/context for a terminal.
/// </summary>
public enum BusinessMode
{
    /// <summary>
    /// Supermarket/retail mode.
    /// </summary>
    Supermarket = 1,

    /// <summary>
    /// Restaurant/hospitality mode.
    /// </summary>
    Restaurant = 2,

    /// <summary>
    /// Administrative/back office mode.
    /// </summary>
    Admin = 3
}

/// <summary>
/// Defines the type of machine identifier used.
/// </summary>
public enum MachineIdentifierType
{
    /// <summary>
    /// MAC address from network adapter.
    /// </summary>
    MacAddress = 1,

    /// <summary>
    /// Windows Machine GUID from registry.
    /// </summary>
    WindowsMachineGuid = 2,

    /// <summary>
    /// Generated GUID fallback.
    /// </summary>
    GeneratedGuid = 3
}

/// <summary>
/// Defines the connection status of a terminal.
/// </summary>
public enum TerminalStatus
{
    /// <summary>
    /// Terminal status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Terminal is online and connected.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Terminal is offline or disconnected.
    /// </summary>
    Offline = 2,

    /// <summary>
    /// Terminal is in maintenance mode.
    /// </summary>
    Maintenance = 3,

    /// <summary>
    /// Terminal has an error condition.
    /// </summary>
    Error = 4
}
