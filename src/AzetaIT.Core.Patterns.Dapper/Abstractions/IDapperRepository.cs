using AzetaIT.Core.Patterns.Abstractions;

namespace AzetaIT.Core.Patterns.Dapper.Abstractions;

/// <summary>
/// Extends <see cref="IRepository{T}"/> with raw-SQL methods specific to Dapper.
/// Use these when the generic CRUD interface is not expressive enough
/// (filtered queries, projections, stored procedures).
/// </summary>
public interface IDapperRepository<T> : IRepository<T> where T : class
{
    /// <summary>Executes a SELECT and maps results to <typeparamref name="T"/>.</summary>
    Task<IReadOnlyList<T>> QueryAsync(string sql, object? param = null, CancellationToken ct = default);

    /// <summary>Executes a SELECT and returns the first row or null.</summary>
    Task<T?> QueryFirstOrDefaultAsync(string sql, object? param = null, CancellationToken ct = default);

    /// <summary>Executes a non-query (INSERT / UPDATE / DELETE) and returns affected rows.</summary>
    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default);

    /// <summary>Executes a scalar query and returns the single value.</summary>
    Task<TResult?> ExecuteScalarAsync<TResult>(string sql, object? param = null, CancellationToken ct = default);
}
