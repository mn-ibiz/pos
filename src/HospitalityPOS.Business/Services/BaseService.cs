using Serilog;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Business.Services;

/// <summary>
/// Base class for all business services providing common functionality.
/// </summary>
public abstract class BaseService
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected BaseService(IUnitOfWork unitOfWork, ILogger logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation within a transaction.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await operation().ConfigureAwait(false);
            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Transaction failed, rolling back");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within a transaction without return value.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await operation().ConfigureAwait(false);
            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Transaction failed, rolling back");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
