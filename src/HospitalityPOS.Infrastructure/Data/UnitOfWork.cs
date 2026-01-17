using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation for managing database transactions.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly POSDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UnitOfWork(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
            // Do NOT dispose _context - it's DI-owned with Scoped lifetime

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="UnitOfWork"/>
    /// and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    /// <remarks>
    /// Note: We do NOT dispose the DbContext here because it is injected by DI
    /// with Scoped lifetime. The DI container owns the DbContext lifecycle.
    /// Disposing it here would cause ObjectDisposedException in other services.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                // Do NOT dispose _context - it's DI-owned with Scoped lifetime
            }

            _disposed = true;
        }
    }
}
