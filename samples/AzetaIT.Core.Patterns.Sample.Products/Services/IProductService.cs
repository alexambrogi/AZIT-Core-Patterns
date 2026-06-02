namespace AzetaIT.Core.Patterns.Sample.Products;

public interface IProductService
{
    Task<IReadOnlyList<IProduct>> GetAllAsync(CancellationToken ct = default);
    Task<IProduct?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<IProduct>> FindSottoScortaAsync(int sogliaMinima, CancellationToken ct = default);
    Task<IProduct> CreateAsync(ProductRequest request, CancellationToken ct = default);
    Task UpdatePrezzoAsync(int id, decimal nuovoPrezzo, CancellationToken ct = default);
    Task UpdateAsync(int id, ProductUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
