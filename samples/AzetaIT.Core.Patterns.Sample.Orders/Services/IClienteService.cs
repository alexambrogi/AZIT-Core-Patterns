namespace AzetaIT.Core.Patterns.Sample.Orders;

public interface IClienteService
{
    Task<IReadOnlyList<ICliente>> GetAllAsync(CancellationToken ct = default);
    Task<ICliente?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ICliente>> GetByCittaAsync(string citta, CancellationToken ct = default);
    Task<ICliente> CreateAsync(ClienteRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, ClienteRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
