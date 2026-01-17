namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a system configuration setting.
/// </summary>
public class SystemSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string? SettingType { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}
