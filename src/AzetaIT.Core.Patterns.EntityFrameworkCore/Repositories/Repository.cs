using System.Linq.Expressions;
using AzetaIT.Core.Patterns.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AzetaIT.Core.Patterns.EntityFrameworkCore.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>.
/// Shares the <typeparamref name="TContext"/> injected by <see cref="EFUnitOfWork{TContext}"/>.
/// </summary>
public class Repository<T, TContext> : IRepository<T>
    where T : class
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<T> Set;

    public Repository(TContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Set.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.Where(predicate).ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.AnyAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await Set.CountAsync(cancellationToken);

    public IQueryable<T> Query() => Set.AsQueryable();

    public IQueryable<T> QueryNoTracking() => Set.AsNoTracking();

    // ── Write ────────────────────────────────────────────────────────────────

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await Set.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity)
        => Set.Update(entity);

    public void UpdateRange(IEnumerable<T> entities)
        => Set.UpdateRange(entities);

    public void Remove(T entity)
        => Set.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities)
        => Set.RemoveRange(entities);
}
