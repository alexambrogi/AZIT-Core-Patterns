using Dapper.Contrib.Extensions;

namespace AzetaIT.Core.Patterns.Sample.Products;

[Table("products")]
public class Product : IProduct
{
    [Key]
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public int Scorta { get; set; }
}
