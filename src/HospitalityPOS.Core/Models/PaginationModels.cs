namespace HospitalityPOS.Core.Models;

/// <summary>
/// Base class for paginated requests.
/// </summary>
public class PaginationParameters
{
    private int _pageNumber = 1;
    private int _pageSize = 50;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the page size. Default is 50, max is 500.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 50 : (value > 500 ? 500 : value);
    }

    /// <summary>
    /// Gets the number of records to skip.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of records to take.
    /// </summary>
    public int Take => PageSize;
}

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates an empty paginated result.
    /// </summary>
    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 50) => new()
    {
        Items = [],
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = 0
    };

    /// <summary>
    /// Creates a paginated result from items and pagination info.
    /// </summary>
    public static PaginatedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount) => new()
    {
        Items = items,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
