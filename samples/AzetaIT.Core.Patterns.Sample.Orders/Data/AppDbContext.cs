using Microsoft.EntityFrameworkCore;

namespace AzetaIT.Core.Patterns.Sample.Orders;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ordine> Ordini => Set<Ordine>();
    public DbSet<Cliente> Clienti => Set<Cliente>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ordine>(e =>
        {
            e.ToTable("Ordini");
            e.HasKey(o => o.Id);
            e.Property(o => o.Numero).IsRequired().HasMaxLength(20);
            e.Property(o => o.Cliente).IsRequired().HasMaxLength(150);
            e.Property(o => o.Totale).HasColumnType("decimal(18,2)");
            e.Property(o => o.Stato).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clienti");
            e.HasKey(c => c.Id);
            e.Property(c => c.Nome).HasColumnName("Cliente").HasMaxLength(255);
            e.Property(c => c.Citta).HasMaxLength(255);
            e.Property(c => c.Provincia).HasMaxLength(2);
        });
    }
}
