using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Pedidos;

namespace Pedidos.Api.Datos;

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(e =>
        {
            e.ToTable("pedidos");
            e.HasKey(p => p.Id);
            e.Property(p => p.Total).HasPrecision(10, 2);
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PedidoItem>(e =>
        {
            e.ToTable("pedido_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Nombre).HasMaxLength(100).IsRequired();
            e.Property(i => i.PrecioUnitario).HasPrecision(10, 2);
        });
    }
}
