using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for password hashing, verification, and validation.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password using BCrypt with the configured work factor.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The BCrypt password hash.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The BCrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Generates a secure temporary password that meets complexity requirements.
    /// </summary>
    /// <param name="length">The length of the password to generate (default: 12).</param>
    /// <returns>A randomly generated temporary password.</returns>
    string GenerateTemporaryPassword(int length = 12);

    /// <summary>
    /// Validates password complexity against the configured requirements.
    /// Requirements: 8+ characters, at least one uppercase, one lowercase, one digit.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>A PasswordValidationResult containing validation status and any errors.</returns>
    PasswordValidationResult ValidatePasswordComplexity(string password);

    /// <summary>
    /// Hashes a PIN using BCrypt with the configured work factor.
    /// </summary>
    /// <param name="pin">The plain-text PIN to hash.</param>
    /// <returns>The BCrypt PIN hash.</returns>
    string HashPin(string pin);

    /// <summary>
    /// Verifies a PIN against a BCrypt hash.
    /// </summary>
    /// <param name="pin">The plain-text PIN to verify.</param>
    /// <param name="hash">The BCrypt hash to verify against.</param>
    /// <returns>True if the PIN matches the hash; otherwise, false.</returns>
    bool VerifyPin(string pin, string hash);
}
