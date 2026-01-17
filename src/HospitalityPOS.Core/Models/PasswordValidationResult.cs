namespace HospitalityPOS.Core.Models;

/// <summary>
/// Represents the result of password complexity validation.
/// </summary>
public class PasswordValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the password meets all complexity requirements.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid PasswordValidationResult.</returns>
    public static PasswordValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>An invalid PasswordValidationResult.</returns>
    public static PasswordValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = [.. errors]
    };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>An invalid PasswordValidationResult.</returns>
    public static PasswordValidationResult Failure(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
