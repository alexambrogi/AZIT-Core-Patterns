using System.Data;

namespace AzetaIT.Core.Patterns.Dapper.Internal;

/// <summary>
/// Shared connection + transaction state passed from UoW to repositories.
/// </summary>
internal interface IDapperContext
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}
