using Catalogo.Api.Datos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Productos;

public static class ProductosEndpoints
{
    public static RouteGroupBuilder MapProductos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/productos").WithTags("Productos");

        grupo.MapGet("/", async (CatalogoDbContext db, CancellationToken ct) =>
            TypedResults.Ok(await db.Productos.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct)));

        grupo.MapGet("/{id:int}", async Task<Results<Ok<Producto>, NotFound>> (
            int id, CatalogoDbContext db, CancellationToken ct) =>
        {
            var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return producto is null ? TypedResults.NotFound() : TypedResults.Ok(producto);
        });

        return grupo;
    }
}
