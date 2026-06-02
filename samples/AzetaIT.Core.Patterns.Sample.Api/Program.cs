using AzetaIT.Core.Patterns.Sample.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProductServices(
    builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=azeta_sample;Username=postgres;Password=postgres");

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
