using AzetaIT.Core.Patterns.Sample.Products;
using AzetaIT.Core.Patterns.Sample.WorkerService;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddProductServices(
    builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=azeta_sample;Username=postgres;Password=postgres");

builder.Services.AddHostedService<Worker>();

// ========================================================================================================================
// NB => per il deployment come windows service in SCM, aggiungere UseWindowsService() dopo Build() e prima di RunAsync();
// aggiungi il package dotnet add package Microsoft.Extensions.Hosting.WindowsServices
// ========================================================================================================================
// se il file viene bloccato da windows 11 => lanciare Unblock-File -Path "E:\dev\fork\AZIT.Patterns\samples\AzetaIT.Core.Patterns.Sample.WorkerService\bin\Debug\net10.0\AzetaIT.Core.Patterns.Sample.WorkerService.exe"

// ALTRAS per disabilitare SAC:
// Impostazioni → Privacy e sicurezza → Sicurezza di Windows → Controllo app e browser → Impostazioni di Controllo intelligente delle app → imposta su "Disattivato".


// builder.Services.AddWindowsService();

await builder.Build().RunAsync();
