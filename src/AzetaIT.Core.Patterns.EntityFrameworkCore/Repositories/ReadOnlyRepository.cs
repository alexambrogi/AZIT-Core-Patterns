using System.Linq.Expressions;
using AzetaIT.Core.Patterns.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AzetaIT.Core.Patterns.EntityFrameworkCore.Repositories;

/// <summary>
/// Read-only repository. Every materialized query runs with AsNoTracking —
/// entities returned here are never tracked by the DbContext change tracker.
/// Use this when you need projections, reports, or read-only views.
/// Use <see cref="Repository{T,TContext}"/> when you need to mutate and save.
/// </summary>
public class ReadOnlyRepository<T, TContext> : IReadRepository<T>
    where T : class
    where TContext : DbContext
{
    private readonly IQueryable<T> _set;

    public ReadOnlyRepository(TContext context)
        => _set = context.Set<T>().AsNoTracking();

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await _set.FirstOrDefaultAsync(
            e => EF.Property<object>(e, "Id").Equals(id), cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _set.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _set.Where(predicate).ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _set.AnyAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await _set.CountAsync(cancellationToken);

    public IQueryable<T> Query() => _set;

    public IQueryable<T> QueryNoTracking() => _set;
}
