using AzetaIT.Core.Patterns.Sample.Products;

namespace AzetaIT.Core.Patterns.Sample.WorkerService;

public class Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IProductService>();

            var products = await svc.GetAllAsync(stoppingToken);
            logger.LogInformation("Products in stock: {Count}", products.Count);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
