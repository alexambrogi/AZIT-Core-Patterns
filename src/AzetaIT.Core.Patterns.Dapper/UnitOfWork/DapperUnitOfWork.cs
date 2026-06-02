using System.Data;
using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.Dapper.Abstractions;
using AzetaIT.Core.Patterns.Dapper.Internal;
using AzetaIT.Core.Patterns.Dapper.Repositories;

namespace AzetaIT.Core.Patterns.Dapper.UnitOfWork;

/// <summary>
/// Dapper implementation of <see cref="IUnitOfWork"/>.
///
/// Key behavioral differences from <c>EFUnitOfWork</c>:
/// - Write operations (Add, Update, Remove) execute SQL immediately on the connection.
/// - <see cref="SaveChangesAsync"/> is a no-op (returns 0). There is no change tracker to flush.
/// - The transaction boundary (BeginTransactionAsync / CommitAsync) is what gives
///   atomicity — wrap multiple writes in a transaction when you need all-or-nothing.
/// - <see cref="GetRepository{T}"/> returns an <see cref="IDapperRepository{T}"/> cast
///   to <see cref="IRepository{T}"/>. Cast back to access raw SQL methods.
/// </summary>
public sealed class DapperUnitOfWork : IUnitOfWork, IDapperContext
{
    private readonly IDbConnection _connection;
    private readonly Dictionary<Type, object> _cache = new();
    private readonly Dictionary<Type, object> _readCache = new();
    private IDbTransaction? _transaction;
    private bool _disposed;

    public DapperUnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }

    // ── IDapperContext ────────────────────────────────────────────────────────

    IDbConnection IDapperContext.Connection => _connection;
    IDbTransaction? IDapperContext.Transaction => _transaction;

    // ── Repository resolution ─────────────────────────────────────────────────

    public IRepository<T> GetRepository<T>() where T : class
    {
        var key = typeof(T);
        if (!_cache.TryGetValue(key, out var repo))
        {
            repo = new DapperRepository<T>(this);
            _cache[key] = repo;
        }
        return (IRepository<T>)repo;
    }

    /// <summary>
    /// Returns the full <see cref="IDapperRepository{T}"/> to access raw SQL methods
    /// (QueryAsync, ExecuteAsync, etc.).
    /// </summary>
    public IDapperRepository<T> GetDapperRepository<T>() where T : class
        => (IDapperRepository<T>)GetRepository<T>();

    public IReadRepository<T> GetReadRepository<T>() where T : class
    {
        var key = typeof(T);
        if (!_readCache.TryGetValue(key, out var repo))
        {
            repo = new DapperReadOnlyRepository<T>(this);
            _readCache[key] = repo;
        }
        return (IReadRepository<T>)repo;
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    /// <summary>
    /// No-op with Dapper — writes are immediate. Returns 0.
    /// Use CommitAsync() to finalise a transaction instead.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    // ── Transaction control ───────────────────────────────────────────────────

    public bool HasActiveTransaction => _transaction is not null;

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or roll it back before starting a new one.");

        _transaction = _connection.BeginTransaction();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            DisposeTransaction();
        }

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to roll back.");

        _transaction.Rollback();
        DisposeTransaction();
        return Task.CompletedTask;
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        DisposeTransaction();
        _connection.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void DisposeTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}
