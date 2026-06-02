using System.Data;
using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.Dapper.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.Dapper.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IUnitOfWork"/> as Scoped, backed by a Dapper <see cref="DapperUnitOfWork"/>.
    /// The caller is responsible for registering <see cref="IDbConnection"/> before calling this.
    /// </summary>
    /// <example>
    /// services.AddScoped&lt;IDbConnection&gt;(_ => new SqlConnection(connectionString));
    /// services.AddAzetaDapperUnitOfWork();
    /// </example>
    public static IServiceCollection AddAzetaDapperUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<DapperUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DapperUnitOfWork>());
        return services;
    }
}
