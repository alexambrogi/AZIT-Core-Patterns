using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using AzetaIT.Core.Patterns.Dapper.Abstractions;
using AzetaIT.Core.Patterns.Dapper.Internal;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzetaIT.Core.Patterns.Dapper.Repositories;

/// <summary>
/// Full read/write Dapper repository.
///
/// Important differences from the EF Core counterpart:
/// - There is NO change tracker. Write methods (Update, Remove) execute SQL immediately.
/// - SaveChangesAsync on the UoW is a no-op — it returns 0 and does nothing.
///   The transaction boundary (BeginTransaction / CommitAsync) is the only
///   meaningful unit of work when using Dapper.
/// - FindAsync(predicate) and ExistsAsync(predicate) are NOT supported because
///   LINQ expressions cannot be translated to SQL without an ORM.
///   Use QueryAsync(sql, param) or ExistsAsync(sql, param) on IDapperRepository instead.
/// </summary>
internal class DapperRepository<T>(IDapperContext ctx) : IDapperRepository<T>
    where T : class
{
    private IDbConnection Conn => ctx.Connection;
    private IDbTransaction? Tx => ctx.Transaction;

    // ── Read (Dapper.Contrib) ─────────────────────────────────────────────────

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Conn.GetAsync<T>(id, Tx);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await Conn.GetAllAsync<T>(Tx)).ToList();

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Dapper cannot translate LINQ expressions to SQL. " +
            "Cast to IDapperRepository<T> and use QueryAsync(sql, param) instead.");

    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Dapper cannot translate LINQ expressions to SQL. " +
            "Cast to IDapperRepository<T> and use ExecuteScalarAsync<int>(sql, param) instead.");

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var cmd = new CommandDefinition($"SELECT COUNT(*) FROM [{TableName}]", transaction: Tx, cancellationToken: cancellationToken);
        return await Conn.ExecuteScalarAsync<int>(cmd);
    }

    private static string TableName =>
        typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;

    public IQueryable<T> Query()
        => throw new NotSupportedException("IQueryable is not available with Dapper. Use QueryAsync(sql, param) instead.");

    public IQueryable<T> QueryNoTracking()
        => throw new NotSupportedException("IQueryable is not available with Dapper. Use QueryAsync(sql, param) instead.");

    // ── Write (Dapper.Contrib — immediate, no staging) ───────────────────────

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Conn.InsertAsync(entity, Tx);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            await Conn.InsertAsync(entity, Tx);
    }

    public void Update(T entity) => Conn.Update(entity, Tx);

    public void UpdateRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            Conn.Update(entity, Tx);
    }

    public void Remove(T entity) => Conn.Delete(entity, Tx);

    public void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            Conn.Delete(entity, Tx);
    }

    // ── Dapper-specific raw SQL ───────────────────────────────────────────────

    public async Task<IReadOnlyList<T>> QueryAsync(string sql, object? param = null, CancellationToken ct = default)
    {
        var cmd = new CommandDefinition(sql, param, Tx, cancellationToken: ct);
        return (await Conn.QueryAsync<T>(cmd)).ToList();
    }

    public async Task<T?> QueryFirstOrDefaultAsync(string sql, object? param = null, CancellationToken ct = default)
    {
        var cmd = new CommandDefinition(sql, param, Tx, cancellationToken: ct);
        return await Conn.QueryFirstOrDefaultAsync<T>(cmd);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default)
    {
        var cmd = new CommandDefinition(sql, param, Tx, cancellationToken: ct);
        return await Conn.ExecuteAsync(cmd);
    }

    public async Task<TResult?> ExecuteScalarAsync<TResult>(string sql, object? param = null, CancellationToken ct = default)
    {
        var cmd = new CommandDefinition(sql, param, Tx, cancellationToken: ct);
        return await Conn.ExecuteScalarAsync<TResult>(cmd);
    }
}
