using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AzetaIT.Core.Patterns.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// Registers as Scoped — one instance per HTTP request (or per DI scope).
/// Repositories are created lazily and cached for the lifetime of this instance.
/// </summary>
public sealed class EFUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _cache = new();
    private readonly Dictionary<Type, object> _readCache = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public EFUnitOfWork(TContext context) => _context = context;

    // ── Repository resolution ─────────────────────────────────────────────

    public IRepository<T> GetRepository<T>() where T : class
    {
        var key = typeof(T);
        if (!_cache.TryGetValue(key, out var repo))
        {
            repo = new Repository<T, TContext>(_context);
            _cache[key] = repo;
        }
        return (IRepository<T>)repo;
    }

    public IReadRepository<T> GetReadRepository<T>() where T : class
    {
        var key = typeof(T);
        if (!_readCache.TryGetValue(key, out var repo))
        {
            repo = new ReadOnlyRepository<T, TContext>(_context);
            _readCache[key] = repo;
        }
        return (IReadRepository<T>)repo;
    }

    // ── Persistence ───────────────────────────────────────────────────────

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    // ── Transaction control ───────────────────────────────────────────────

    public bool HasActiveTransaction => _transaction is not null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or roll it back before starting a new one.");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to roll back.");

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    // ── Disposal ─────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_transaction is not null)
            await DisposeTransactionAsync();

        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null) return;
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
