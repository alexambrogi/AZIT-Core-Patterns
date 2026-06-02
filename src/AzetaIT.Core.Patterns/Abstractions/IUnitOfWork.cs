namespace AzetaIT.Core.Patterns.Abstractions;

/// <summary>
/// Coordinates a set of repositories and their persistence within a single transaction boundary.
/// Repositories are resolved lazily and cached per UoW instance.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Returns a full read/write repository for <typeparamref name="T"/>.
    /// Instances are cached — calling this twice for the same type returns the same object.
    /// </summary>
    IRepository<T> GetRepository<T>() where T : class;

    /// <summary>
    /// Returns a read-only view of the repository for <typeparamref name="T"/>.
    /// Useful to enforce read-only semantics at the call site without a separate registration.
    /// </summary>
    IReadRepository<T> GetReadRepository<T>() where T : class;

    /// <summary>Persists all pending changes tracked by this unit of work.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // ── Transaction control ──────────────────────────────────────────────────

    /// <summary>Starts an explicit database transaction. Throws if one is already active.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls <see cref="SaveChangesAsync"/> and commits the active transaction.
    /// Throws if no transaction is active.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the active transaction without saving. Throws if none is active.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether a transaction started via <see cref="BeginTransactionAsync"/> is currently open.</summary>
    bool HasActiveTransaction { get; }
}
