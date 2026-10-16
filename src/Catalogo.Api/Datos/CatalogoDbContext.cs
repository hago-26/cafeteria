using Catalogo.Api.Productos;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Datos;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("productos");
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            e.Property(p => p.Precio).HasPrecision(10, 2);

            // Datos semilla (EnsureCreated los inserta al crear el esquema)
            e.HasData(
                new Producto { Id = 1, Nombre = "Espresso", Precio = 35m, Disponible = true },
                new Producto { Id = 2, Nombre = "Americano", Precio = 40m, Disponible = true },
                new Producto { Id = 3, Nombre = "Capuchino", Precio = 55m, Disponible = true },
                new Producto { Id = 4, Nombre = "Latte", Precio = 58m, Disponible = true },
                new Producto { Id = 5, Nombre = "Croissant", Precio = 45m, Disponible = true },
                new Producto { Id = 6, Nombre = "Pay de limón", Precio = 60m, Disponible = false });
        });
    }
}
