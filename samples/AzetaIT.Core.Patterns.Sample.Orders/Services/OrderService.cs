using AzetaIT.Core.Patterns.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.Sample.Orders;

public class OrderService([FromKeyedServices("sqlserver")] IUnitOfWork uow) : IOrderService
{
    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<IOrdine>> GetAllAsync(CancellationToken ct = default)
        => await uow.GetReadRepository<Ordine>().GetAllAsync(ct);

    public async Task<IOrdine?> GetByIdAsync(int id, CancellationToken ct = default)
        => await uow.GetReadRepository<Ordine>().GetByIdAsync(id, ct);

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<IOrdine> CreateAsync(OrdineRequest request, CancellationToken ct = default)
    {
        var ordine = new Ordine
        {
            Numero = request.Numero,
            Cliente = request.Cliente,
            Totale = request.Totale,
            DataOrdine = DateTime.UtcNow,
            Stato = StatoOrdine.Bozza
        };

        await uow.GetRepository<Ordine>().AddAsync(ordine, ct);
        await uow.SaveChangesAsync(ct);
        return ordine;
    }

    public async Task UpdateAsync(int id, OrdineRequest request, CancellationToken ct = default)
    {
        var ordine = await uow.GetRepository<Ordine>().GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ordine {id} non trovato.");

        ordine.Numero = request.Numero;
        ordine.Cliente = request.Cliente;
        ordine.Totale = request.Totale;

        await uow.SaveChangesAsync(ct);
    }

    public async Task ConfermaAsync(int id, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<Ordine>();
        var ordine = await repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ordine {id} non trovato.");

        if (ordine.Stato != StatoOrdine.Bozza)
            throw new InvalidOperationException($"Solo un ordine in Bozza può essere confermato (stato attuale: {ordine.Stato}).");

        await uow.BeginTransactionAsync(ct);
        try
        {
            ordine.Stato = StatoOrdine.Confermato;
            repo.Update(ordine);
            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<Ordine>();
        var ordine = await repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ordine {id} non trovato.");

        repo.Remove(ordine);
        await uow.SaveChangesAsync(ct);
    }
}
