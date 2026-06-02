using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.Dapper.Internal;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzetaIT.Core.Patterns.Dapper.Repositories;

/// <summary>
/// Read-only Dapper repository. Exposes only <see cref="IReadRepository{T}"/>.
/// Use this for queries where mutation is not needed — mirrors the intent
/// of <c>GetReadRepository&lt;T&gt;()</c> on the UoW.
/// Note: Dapper has no change tracker, so "read-only" is a contract-level
/// guarantee (no write methods are exposed), not an infrastructure-level one.
/// </summary>
internal class DapperReadOnlyRepository<T>(IDapperContext ctx) : IReadRepository<T>
    where T : class
{
    private IDbConnection Conn => ctx.Connection;
    private IDbTransaction? Tx => ctx.Transaction;

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Conn.GetAsync<T>(id, Tx);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await Conn.GetAllAsync<T>(Tx)).ToList();

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Dapper cannot translate LINQ expressions to SQL. " +
            "Use QueryAsync(sql, param) via IDapperRepository<T> instead.");

    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Dapper cannot translate LINQ expressions to SQL. " +
            "Use ExecuteScalarAsync<int>(sql, param) via IDapperRepository<T> instead.");

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var cmd = new CommandDefinition($"SELECT COUNT(*) FROM [{TableName}]", transaction: Tx, cancellationToken: cancellationToken);
        return await Conn.ExecuteScalarAsync<int>(cmd);
    }

    private static string TableName =>
        typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;

    public IQueryable<T> Query()
        => throw new NotSupportedException("IQueryable is not available with Dapper.");

    public IQueryable<T> QueryNoTracking()
        => throw new NotSupportedException("IQueryable is not available with Dapper.");
}
