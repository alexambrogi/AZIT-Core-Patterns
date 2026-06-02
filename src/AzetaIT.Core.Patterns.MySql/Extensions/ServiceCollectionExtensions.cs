using AzetaIT.Core.Patterns.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.MySql.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> and <see cref="AzetaIT.Core.Patterns.Abstractions.IUnitOfWork"/>
    /// (Scoped) backed by MySQL via Pomelo.
    /// </summary>
    /// <param name="serverVersion">
    /// MySQL server version. Use <c>new MySqlServerVersion(new Version(8, 0, 0))</c>
    /// or <c>ServerVersion.AutoDetect(connectionString)</c> for development.
    /// </param>
    /// <example>
    /// services.AddAzetaMySqlUnitOfWork&lt;AppDbContext&gt;(
    ///     connectionString,
    ///     new MySqlServerVersion(new Version(8, 0, 0)));
    /// </example>
    public static IServiceCollection AddAzetaMySqlUnitOfWork<TContext>(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion,
        Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(o =>
        {
            o.UseMySql(connectionString, serverVersion);
            configure?.Invoke(o);
        });
        services.AddAzetaUnitOfWork<TContext>();
        return services;
    }
}
