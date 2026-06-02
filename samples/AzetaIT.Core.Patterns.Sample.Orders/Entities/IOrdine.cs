namespace AzetaIT.Core.Patterns.Sample.Orders;

public interface IOrdine
{
    int Id { get; }
    string Numero { get; }
    DateTime DataOrdine { get; }
    string Cliente { get; }
    decimal Totale { get; }
    StatoOrdine Stato { get; }
}

public record OrdineRequest(string Numero, string Cliente, decimal Totale);
