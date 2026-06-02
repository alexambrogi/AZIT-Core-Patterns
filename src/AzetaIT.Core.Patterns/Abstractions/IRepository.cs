namespace AzetaIT.Core.Patterns.Abstractions;

/// <summary>
/// Full read/write contract. Composed from <see cref="IReadRepository{T}"/> and <see cref="IWriteRepository{T}"/>.
/// </summary>
public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : class
{
}
