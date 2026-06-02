using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IUnitOfWork"/> as Scoped, backed by <typeparamref name="TContext"/>.
    /// Call this after <c>AddDbContext&lt;TContext&gt;</c>.
    /// </summary>
    /// <example>
    /// services.AddDbContext&lt;AppDbContext&gt;(o => o.UseSqlServer(connectionString));
    /// services.AddAzetaUnitOfWork&lt;AppDbContext&gt;();
    /// </example>
    public static IServiceCollection AddAzetaUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, EFUnitOfWork<TContext>>();
        return services;
    }
}
