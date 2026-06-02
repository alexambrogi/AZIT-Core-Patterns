using System.Linq.Expressions;

namespace AzetaIT.Core.Patterns.Abstractions;

/// <summary>
/// Read-only contract. Use when a consumer must not mutate the aggregate.
/// </summary>
public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate,CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate,CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exposes the raw queryable for callers that need projection or pagination.
    /// Changes tracked by the underlying UoW context.
    /// </summary>
    IQueryable<T> Query();

    /// <summary>
    /// Same as <see cref="Query"/> but with <c>AsNoTracking</c> applied.
    /// Safe for read-only projections.
    /// </summary>
    IQueryable<T> QueryNoTracking();
}
