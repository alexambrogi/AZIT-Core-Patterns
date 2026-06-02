using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.Dapper.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AzetaIT.Core.Patterns.Sample.Products;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductServices(
        this IServiceCollection services, string connectionString)
    {
        services.AddKeyedScoped<IUnitOfWork>("postgres",(_, _) => new DapperUnitOfWork(new NpgsqlConnection(connectionString)));
        services.AddScoped<IProductService, ProductService>();
        return services;
    }

    public static async Task EnsureProductsSchemaAsync(this IServiceProvider serviceProvider)
    {
        var pgUow = (DapperUnitOfWork)serviceProvider.GetRequiredKeyedService<IUnitOfWork>("postgres");
        await pgUow.GetDapperRepository<Product>().ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS products (
                id          SERIAL PRIMARY KEY,
                nome        VARCHAR(150)    NOT NULL,
                descrizione VARCHAR(500)    NOT NULL DEFAULT '',
                prezzo      DECIMAL(18,2)   NOT NULL,
                scorta      INT             NOT NULL DEFAULT 0
            )");
    }
}
