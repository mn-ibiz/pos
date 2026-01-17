namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Generic result wrapper for service operations.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the data returned by the operation.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string errorMessage, string? errorCode = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}

/// <summary>
/// Non-generic result for operations that don't return data.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string errorMessage, string? errorCode = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}
