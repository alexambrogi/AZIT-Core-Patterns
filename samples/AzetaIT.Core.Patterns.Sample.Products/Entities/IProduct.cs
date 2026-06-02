namespace AzetaIT.Core.Patterns.Sample.Products;

public interface IProduct
{
    int Id { get; }
    string Nome { get; }
    string Descrizione { get; }
    decimal Prezzo { get; }
    int Scorta { get; }
}

public record ProductRequest(string Nome, string Descrizione, decimal Prezzo, int Scorta);

public record ProductUpdateRequest(string? Nome, string? Descrizione, decimal? Prezzo, int? Scorta);
