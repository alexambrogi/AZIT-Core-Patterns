namespace AzetaIT.Core.Patterns.Sample.Orders;

public interface ICliente
{
    int Id { get; }
    string? Nome { get; }
    string? Citta { get; }
    string? Provincia { get; }
}

public record ClienteRequest(string? Nome, string? Citta, string? Provincia);
