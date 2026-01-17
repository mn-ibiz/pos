using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// DTO for creating or updating a payment method.
/// </summary>
public class PaymentMethodDto
{
    /// <summary>
    /// Gets or sets the payment method name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method type.
    /// </summary>
    public PaymentMethodType Type { get; set; } = PaymentMethodType.Cash;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the method is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a reference is required.
    /// </summary>
    public bool RequiresReference { get; set; }

    /// <summary>
    /// Gets or sets the reference field label.
    /// </summary>
    public string? ReferenceLabel { get; set; }

    /// <summary>
    /// Gets or sets the minimum reference length.
    /// </summary>
    public int? ReferenceMinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum reference length.
    /// </summary>
    public int? ReferenceMaxLength { get; set; }

    /// <summary>
    /// Gets or sets whether this method supports change calculation.
    /// </summary>
    public bool SupportsChange { get; set; }

    /// <summary>
    /// Gets or sets whether this method opens the cash drawer.
    /// </summary>
    public bool OpensDrawer { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the icon path.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the background color (hex).
    /// </summary>
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// DTO for reordering payment methods.
/// </summary>
public class PaymentMethodOrderDto
{
    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the new display order.
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Service interface for managing payment methods.
/// </summary>
public interface IPaymentMethodService
{
    /// <summary>
    /// Gets all payment methods ordered by display order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All payment methods.</returns>
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active payment methods ordered by display order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active payment methods.</returns>
    Task<IReadOnlyList<PaymentMethod>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment method if found; otherwise, null.</returns>
    Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment method by code.
    /// </summary>
    /// <param name="code">The payment method code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment method if found; otherwise, null.</returns>
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    /// <param name="dto">The payment method data.</param>
    /// <param name="createdByUserId">The ID of the user creating the method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created payment method.</returns>
    Task<PaymentMethod> CreateAsync(PaymentMethodDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <param name="dto">The updated payment method data.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated payment method.</returns>
    Task<PaymentMethod> UpdateAsync(int id, PaymentMethodDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the active status of a payment method.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated payment method.</returns>
    Task<PaymentMethod> ToggleActiveAsync(int id, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders payment methods.
    /// </summary>
    /// <param name="orderings">The new orderings.</param>
    /// <param name="modifiedByUserId">The ID of the user reordering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReorderAsync(IEnumerable<PaymentMethodOrderDto> orderings, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payment method.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <param name="deletedByUserId">The ID of the user deleting the method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment method code is unique.
    /// </summary>
    /// <param name="code">The code to check.</param>
    /// <param name="excludeId">Optional ID to exclude from check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is unique.</returns>
    Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment method has any associated payments.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the method has payments.</returns>
    Task<bool> HasPaymentsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when payment methods change.
    /// </summary>
    event EventHandler? PaymentMethodsChanged;
}
