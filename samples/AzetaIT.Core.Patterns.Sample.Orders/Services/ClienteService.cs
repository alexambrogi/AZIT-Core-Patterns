using AzetaIT.Core.Patterns.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.Sample.Orders;

public class ClienteService([FromKeyedServices("sqlserver")] IUnitOfWork uow) : IClienteService
{
    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ICliente>> GetAllAsync(CancellationToken ct = default)
        => await uow.GetReadRepository<Cliente>().GetAllAsync(ct);

    public async Task<ICliente?> GetByIdAsync(int id, CancellationToken ct = default)
        => await uow.GetReadRepository<Cliente>().GetByIdAsync(id, ct);

    public async Task<IReadOnlyList<ICliente>> GetByCittaAsync(string citta, CancellationToken ct = default)
        => await uow.GetReadRepository<Cliente>().FindAsync(c => c.Citta == citta, ct);

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<ICliente> CreateAsync(ClienteRequest request, CancellationToken ct = default)
    {
        var cliente = new Cliente
        {
            Nome = request.Nome,
            Citta = request.Citta,
            Provincia = request.Provincia
        };

        await uow.GetRepository<Cliente>().AddAsync(cliente, ct);
        await uow.SaveChangesAsync(ct);
        return cliente;
    }

    public async Task UpdateAsync(int id, ClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await uow.GetRepository<Cliente>().GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Cliente {id} non trovato.");

        cliente.Nome = request.Nome;
        cliente.Citta = request.Citta;
        cliente.Provincia = request.Provincia;

        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<Cliente>();
        var cliente = await repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Cliente {id} non trovato.");

        repo.Remove(cliente);
        await uow.SaveChangesAsync(ct);
    }
}
