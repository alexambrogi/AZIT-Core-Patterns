namespace AzetaIT.Core.Patterns.Abstractions;

/// <summary>
/// Write-only contract. Does NOT call SaveChanges — that responsibility belongs to IUnitOfWork.
/// </summary>
public interface IWriteRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void UpdateRange(IEnumerable<T> entities);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}
