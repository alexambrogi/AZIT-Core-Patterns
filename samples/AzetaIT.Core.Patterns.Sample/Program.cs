using AzetaIT.Core.Patterns.Sample.Orders;
using AzetaIT.Core.Patterns.Sample.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ── Bootstrap ─────────────────────────────────────────────────────────────────

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var sqlServerConnectionString = ctx.Configuration["ConnectionStrings:SqlServer"]
            ?? "Server=localhost;Database=AzetaIT_Sample;Trusted_Connection=True;TrustServerCertificate=True;";

        var postgresqlConnectionString = ctx.Configuration["ConnectionStrings:Postgres"]
            ?? "Host=localhost;Port=5432;Database=azeta_sample;Username=postgres;Password=postgres";

        services.AddOrderServices(sqlServerConnectionString);
        services.AddProductServices(postgresqlConnectionString);
    })
    .Build();

// ── Setup DB (solo sample: crea tabelle se non esistono) ──────────────────────

await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.EnsureOrdersSchemaAsync();
    await scope.ServiceProvider.EnsureProductsSchemaAsync();
}

// ── Demo: Ordini (SQL Server / EF Core) ──────────────────────────────────────

Console.WriteLine("════════════════════════════════════════");
Console.WriteLine("  ORDINI  —  SQL Server + EF Core");
Console.WriteLine("════════════════════════════════════════");

await using (var scope = host.Services.CreateAsyncScope())
{
    var svc = scope.ServiceProvider.GetRequiredService<IOrderService>();

    Console.WriteLine("\n── CREATE ──");
    var o1 = await svc.CreateAsync(new OrdineRequest("ORD-001", "Mario Rossi", 250.00m));
    var o2 = await svc.CreateAsync(new OrdineRequest("ORD-002", "Luigi Bianchi", 99.90m));
    Console.WriteLine($"  {o1.Id} | {o1.Numero} | {o1.Cliente} | {o1.Totale:C} | {o1.Stato}");
    Console.WriteLine($"  {o2.Id} | {o2.Numero} | {o2.Cliente} | {o2.Totale:C} | {o2.Stato}");

    Console.WriteLine("\n── UPDATE (tracking — no Update() esplicito) ──");
    await svc.UpdateAsync(o1.Id, new OrdineRequest(o1.Numero, o1.Cliente, 320.00m));
    Console.WriteLine($"  ORD-001 nuovo totale: {(await svc.GetByIdAsync(o1.Id))!.Totale:C}");

    Console.WriteLine("\n── CONFERMA (transazione esplicita) ──");
    await svc.ConfermaAsync(o1.Id);
    Console.WriteLine($"  ORD-001 stato: {(await svc.GetByIdAsync(o1.Id))!.Stato}");

    Console.WriteLine("\n── DELETE ──");
    await svc.DeleteAsync(o2.Id);
    Console.WriteLine($"  ORD-002 eliminato.");

    Console.WriteLine("\n── READ ALL ──");
    foreach (var o in await svc.GetAllAsync())
        Console.WriteLine($"  [{o.Id}] {o.Numero} | {o.Cliente} | {o.Totale:C} | {o.Stato}");
}

// ── Demo: Products (PostgreSQL / Dapper) ─────────────────────────────────────

Console.WriteLine("\n════════════════════════════════════════");
Console.WriteLine("  PRODUCTS  —  PostgreSQL + Dapper");
Console.WriteLine("════════════════════════════════════════");

await using (var scope = host.Services.CreateAsyncScope())
{
    var svc = scope.ServiceProvider.GetRequiredService<IProductService>();

    //Console.WriteLine("\n── CREATE (INSERT ... RETURNING id) ──");
    //var p1 = await svc.CreateAsync(new ProductRequest("Notebook Pro", "Laptop 16GB RAM", 1299.99m, 5));
    //var p2 = await svc.CreateAsync(new ProductRequest("Mouse Wireless", "Mouse ergonomico", 39.90m, 2));
    //var p3 = await svc.CreateAsync(new ProductRequest("Tastiera Mec.", "Switch Cherry MX", 89.00m, 0));
    //Console.WriteLine($"  {p1.Id} | {p1.Nome} | {p1.Prezzo:C} | scorta: {p1.Scorta}");
    //Console.WriteLine($"  {p2.Id} | {p2.Nome} | {p2.Prezzo:C} | scorta: {p2.Scorta}");
    //Console.WriteLine($"  {p3.Id} | {p3.Nome} | {p3.Prezzo:C} | scorta: {p3.Scorta}");

    Console.WriteLine("\n── READ ALL (GetReadRepository → DapperReadOnlyRepository) ──");
    foreach (var p in await svc.GetAllAsync())
        Console.WriteLine($"  [{p.Id}] {p.Nome} | {p.Prezzo:C} | scorta: {p.Scorta}");

    Console.WriteLine("\n── FIND sotto-scorta ≤ 2 (QueryAsync SQL raw) ──");
    foreach (var p in await svc.FindSottoScortaAsync(sogliaMinima: 2))
        Console.WriteLine($"  [{p.Id}] {p.Nome} | scorta: {p.Scorta}  ← attenzione");

    //Console.WriteLine("\n── UPDATE prezzo (ExecuteAsync SQL raw) ──");
    //await svc.UpdatePrezzoAsync(p1.Id, 1199.00m);
    //Console.WriteLine($"  Notebook Pro nuovo prezzo: {(await svc.GetByIdAsync(p1.Id))!.Prezzo:C}");
    //
    //Console.WriteLine("\n── DELETE ──");
    //await svc.DeleteAsync(p3.Id);
    //Console.WriteLine($"  Tastiera Mec. eliminata.");
    //
    //Console.WriteLine("\n── READ ALL (post-delete) ──");
    //foreach (var p in await svc.GetAllAsync())
    //    Console.WriteLine($"  [{p.Id}] {p.Nome} | {p.Prezzo:C} | scorta: {p.Scorta}");


    Console.WriteLine("\n-- Update all data for PRODUCTS --");
    await svc.UpdateAsync(2, new ProductUpdateRequest("Mouse Wireless-2", "Mouse ergonomico con batteria ricaricabile-2", 46.90m, 12));
}

Console.WriteLine("\nFine demo.");
