namespace AzetaIT.Core.Patterns.Sample.Orders;

public interface IOrderService
{
    Task<IReadOnlyList<IOrdine>> GetAllAsync(CancellationToken ct = default);
    Task<IOrdine?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IOrdine> CreateAsync(OrdineRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, OrdineRequest request, CancellationToken ct = default);
    Task ConfermaAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
