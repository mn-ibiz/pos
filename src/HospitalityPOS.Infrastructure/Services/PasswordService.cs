using System.Security.Cryptography;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Password service implementation using BCrypt for secure hashing.
/// </summary>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// BCrypt work factor (cost). Higher = more secure but slower.
    /// 12 is the recommended minimum for production.
    /// </summary>
    private const int WorkFactor = 12;

    /// <summary>
    /// Minimum password length requirement.
    /// </summary>
    private const int MinPasswordLength = 8;

    /// <summary>
    /// Characters used for temporary password generation.
    /// Excludes ambiguous characters (0, O, l, 1, I) for readability.
    /// </summary>
    private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Invalid hash format
            return false;
        }
    }

    /// <inheritdoc />
    public string GenerateTemporaryPassword(int length = 12)
    {
        if (length < MinPasswordLength)
        {
            length = MinPasswordLength;
        }

        // Use cryptographically secure random number generator
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        var chars = new char[length];

        // Ensure at least one of each required character type
        // Use first 4 positions for guaranteed character types
        chars[0] = GetRandomUppercase(randomBytes[0]);
        chars[1] = GetRandomLowercase(randomBytes[1]);
        chars[2] = GetRandomDigit(randomBytes[2]);
        chars[3] = GetRandomSpecial(randomBytes[3]);

        // Fill remaining positions with random characters from the full set
        for (int i = 4; i < length; i++)
        {
            chars[i] = PasswordChars[randomBytes[i] % PasswordChars.Length];
        }

        // Shuffle the array to randomize position of guaranteed characters
        ShuffleArray(chars);

        return new string(chars);
    }

    /// <inheritdoc />
    public PasswordValidationResult ValidatePasswordComplexity(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            return PasswordValidationResult.Failure("Password is required");
        }

        if (password.Length < MinPasswordLength)
        {
            errors.Add($"Password must be at least {MinPasswordLength} characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number");
        }

        return errors.Count == 0
            ? PasswordValidationResult.Success()
            : PasswordValidationResult.Failure(errors);
    }

    /// <inheritdoc />
    public string HashPin(string pin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);
        return BCrypt.Net.BCrypt.HashPassword(pin, WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPin(string pin, string hash)
    {
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static char GetRandomUppercase(byte randomByte)
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        return uppercase[randomByte % uppercase.Length];
    }

    private static char GetRandomLowercase(byte randomByte)
    {
        const string lowercase = "abcdefghjkmnpqrstuvwxyz";
        return lowercase[randomByte % lowercase.Length];
    }

    private static char GetRandomDigit(byte randomByte)
    {
        const string digits = "23456789";
        return digits[randomByte % digits.Length];
    }

    private static char GetRandomSpecial(byte randomByte)
    {
        const string special = "!@#$%";
        return special[randomByte % special.Length];
    }

    private static void ShuffleArray(char[] array)
    {
        Span<byte> randomBytes = stackalloc byte[array.Length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = randomBytes[i] % (i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
