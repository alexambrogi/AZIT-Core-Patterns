namespace AzetaIT.Core.Patterns.Sample.Orders;

public class Ordine : IOrdine
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DataOrdine { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal Totale { get; set; }
    public StatoOrdine Stato { get; set; }
}

public enum StatoOrdine
{
    Bozza,
    Confermato,
    Spedito,
    Annullato
}
