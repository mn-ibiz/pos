using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// DTO for creating or updating a category.
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent category ID.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for reordering categories.
/// </summary>
public class CategoryOrderDto
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the new display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the new parent category ID.
    /// </summary>
    public int? ParentCategoryId { get; set; }
}

/// <summary>
/// Service interface for managing product categories.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All categories.</returns>
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active categories.</returns>
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets root-level categories (those without a parent).
    /// </summary>
    /// <param name="activeOnly">If true, only return active categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Root categories.</returns>
    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by ID with its subcategories.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category if found; otherwise, null.</returns>
    Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category tree (hierarchical structure) starting from root categories.
    /// </summary>
    /// <param name="activeOnly">If true, only include active categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Root categories with subcategories populated.</returns>
    Task<IReadOnlyList<Category>> GetCategoryTreeAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="dto">The category data.</param>
    /// <param name="createdByUserId">The ID of the user creating the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category.</returns>
    Task<Category> CreateCategoryAsync(CategoryDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="dto">The updated category data.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    Task<Category> UpdateCategoryAsync(int id, CategoryDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="deletedByUserId">The ID of the user deleting the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the category has products or subcategories.</exception>
    Task<bool> DeleteCategoryAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="isActive">The new active status.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    Task<Category> SetCategoryActiveAsync(int id, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders categories.
    /// </summary>
    /// <param name="orderings">The new orderings.</param>
    /// <param name="modifiedByUserId">The ID of the user reordering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReorderCategoriesAsync(IEnumerable<CategoryOrderDto> orderings, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category has any products.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the category has products.</returns>
    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category has any subcategories.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the category has subcategories.</returns>
    Task<bool> HasSubcategoriesAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category name is unique within the parent.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="parentCategoryId">The parent category ID.</param>
    /// <param name="excludeCategoryId">Category ID to exclude from check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the name is unique.</returns>
    Task<bool> IsNameUniqueAsync(string name, int? parentCategoryId, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
}
