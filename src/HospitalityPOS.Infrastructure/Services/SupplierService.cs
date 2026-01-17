using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing suppliers.
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public SupplierService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Supplier>> GetAllSuppliersAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await _context.Suppliers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Code == code.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Supplier>> SearchSuppliersAsync(string searchTerm, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            query = query.Where(s =>
                s.Code.ToLower().Contains(searchLower) ||
                s.Name.ToLower().Contains(searchLower) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(searchLower)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower)));
        }

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        // Ensure code is uppercase
        supplier.Code = supplier.Code.ToUpperInvariant();

        // Check for duplicate code
        if (!await IsCodeUniqueAsync(supplier.Code, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A supplier with code '{supplier.Code}' already exists.");
        }

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Created supplier {SupplierCode} - {SupplierName}", supplier.Code, supplier.Name);

        return supplier;
    }

    /// <inheritdoc />
    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var existingSupplier = await _context.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == supplier.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existingSupplier is null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplier.Id} not found.");
        }

        // Ensure code is uppercase
        supplier.Code = supplier.Code.ToUpperInvariant();

        // Check for duplicate code (excluding current supplier)
        if (!await IsCodeUniqueAsync(supplier.Code, supplier.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A supplier with code '{supplier.Code}' already exists.");
        }

        // Update properties
        existingSupplier.Code = supplier.Code;
        existingSupplier.Name = supplier.Name;
        existingSupplier.ContactPerson = supplier.ContactPerson;
        existingSupplier.Phone = supplier.Phone;
        existingSupplier.Email = supplier.Email;
        existingSupplier.Address = supplier.Address;
        existingSupplier.City = supplier.City;
        existingSupplier.Country = supplier.Country;
        existingSupplier.TaxId = supplier.TaxId;
        existingSupplier.BankAccount = supplier.BankAccount;
        existingSupplier.BankName = supplier.BankName;
        existingSupplier.PaymentTermDays = supplier.PaymentTermDays;
        existingSupplier.CreditLimit = supplier.CreditLimit;
        existingSupplier.Notes = supplier.Notes;
        existingSupplier.IsActive = supplier.IsActive;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Updated supplier {SupplierCode} - {SupplierName}", existingSupplier.Code, existingSupplier.Name);

        return existingSupplier;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            return false;
        }

        supplier.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Activated supplier {SupplierCode}", supplier.Code);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            return false;
        }

        supplier.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Deactivated supplier {SupplierCode}", supplier.Code);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeSupplierId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.ToUpperInvariant();

        var query = _context.Suppliers
            .IgnoreQueryFilters()
            .Where(s => s.Code == normalizedCode);

        if (excludeSupplierId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSupplierId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default)
    {
        // Pattern: SUP-XXXX
        var prefix = "SUP-";

        var lastSupplier = await _context.Suppliers
            .IgnoreQueryFilters()
            .Where(s => s.Code.StartsWith(prefix))
            .OrderByDescending(s => s.Code)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lastSupplier is null)
        {
            return $"{prefix}0001";
        }

        var lastNumber = lastSupplier.Code.Replace(prefix, "");
        if (int.TryParse(lastNumber, out var number))
        {
            return $"{prefix}{(number + 1):D4}";
        }

        // Fallback: count existing suppliers + 1
        var count = await _context.Suppliers
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return $"{prefix}{(count + 1):D4}";
    }

    /// <inheritdoc />
    public async Task<int> GetSupplierCountAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsQueryable();

        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Supplier>> GetSuppliersWithBalanceAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.CurrentBalance > 0)
            .OrderByDescending(s => s.CurrentBalance)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
