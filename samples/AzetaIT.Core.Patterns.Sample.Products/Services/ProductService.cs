using AzetaIT.Core.Patterns.Abstractions;
using AzetaIT.Core.Patterns.Dapper.Abstractions;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace AzetaIT.Core.Patterns.Sample.Products;

public class ProductService([FromKeyedServices("postgres")] IUnitOfWork uow) : IProductService
{
    private IDapperRepository<Product> Repo => (IDapperRepository<Product>)uow.GetRepository<Product>();

    private IReadRepository<Product> ReadRepo => uow.GetReadRepository<Product>();

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<IProduct>> GetAllAsync(CancellationToken ct = default) => await ReadRepo.GetAllAsync(ct);

    public async Task<IProduct?> GetByIdAsync(int id, CancellationToken ct = default) => await ReadRepo.GetByIdAsync(id, ct);

    // FindAsync(predicate) non è supportata da Dapper → SQL raw
    public async Task<IReadOnlyList<IProduct>> FindSottoScortaAsync(int sogliaMinima, CancellationToken ct = default)
        => await Repo.QueryAsync(
            "SELECT * FROM products WHERE scorta <= @soglia ORDER BY scorta ASC",
            new { soglia = sogliaMinima }, ct);

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<IProduct> CreateAsync(ProductRequest request, CancellationToken ct = default)
    {
        var id = await Repo.ExecuteScalarAsync<int>(
            @"INSERT INTO products (nome, descrizione, prezzo, scorta)
              VALUES (@nome, @descrizione, @prezzo, @scorta)
              RETURNING id",
            new { nome = request.Nome, descrizione = request.Descrizione, prezzo = request.Prezzo, scorta = request.Scorta }, ct);

        return new Product { Id = id, Nome = request.Nome, Descrizione = request.Descrizione, Prezzo = request.Prezzo, Scorta = request.Scorta };
    }

    public async Task UpdateAsync(int id, ProductUpdateRequest request, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters();
        p.Add("id", id);

        if (request.Nome        is not null) { sets.Add("nome = @nome");               p.Add("nome",        request.Nome); }
        if (request.Descrizione is not null) { sets.Add("descrizione = @descrizione"); p.Add("descrizione", request.Descrizione); }
        if (request.Prezzo      is not null) { sets.Add("prezzo = @prezzo");           p.Add("prezzo",      request.Prezzo); }
        if (request.Scorta      is not null) { sets.Add("scorta = @scorta");           p.Add("scorta",      request.Scorta); }

        if (sets.Count == 0) return;

        var affected = await Repo.ExecuteAsync(
            $"UPDATE products SET {string.Join(", ", sets)} WHERE id = @id", p, ct);

        if (affected == 0)
            throw new KeyNotFoundException($"Product {id} non trovato.");
    }

    public async Task UpdatePrezzoAsync(int id, decimal nuovoPrezzo, CancellationToken ct = default)
    {
        var affected = await Repo.ExecuteAsync(
            "UPDATE products SET prezzo = @prezzo WHERE id = @id",
            new { prezzo = nuovoPrezzo, id }, ct);

        if (affected == 0)
            throw new KeyNotFoundException($"Product {id} non trovato.");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var affected = await Repo.ExecuteAsync(
            "DELETE FROM products WHERE id = @id",
            new { id }, ct);

        if (affected == 0)
            throw new KeyNotFoundException($"Product {id} non trovato.");
    }
}
