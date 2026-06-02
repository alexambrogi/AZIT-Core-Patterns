using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.Sample.Orders;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServices(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
        services.AddKeyedScoped<IUnitOfWork>("sqlserver", (sp, _) => new EFUnitOfWork<AppDbContext>(sp.GetRequiredService<AppDbContext>()));
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IClienteService, ClienteService>();
        return services;
    }

    public static async Task EnsureOrdersSchemaAsync(this IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
