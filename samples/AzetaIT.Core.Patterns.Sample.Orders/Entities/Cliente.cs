namespace AzetaIT.Core.Patterns.Sample.Orders;

public class Cliente : ICliente
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? Citta { get; set; }
    public string? Provincia { get; set; }
}
