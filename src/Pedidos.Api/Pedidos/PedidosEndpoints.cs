using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Catalogo;
using Pedidos.Api.Datos;

namespace Pedidos.Api.Pedidos;

public static class PedidosEndpoints
{
    public static RouteGroupBuilder MapPedidos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos").WithTags("Pedidos");

        grupo.MapGet("/", async (PedidosDbContext db, CancellationToken ct) =>
            TypedResults.Ok(await db.Pedidos.AsNoTracking()
                .Include(p => p.Items)
                .OrderByDescending(p => p.Id)
                .Take(50)
                .ToListAsync(ct)));

        grupo.MapGet("/{id:int}", async Task<Results<Ok<Pedido>, NotFound>> (
            int id, PedidosDbContext db, CancellationToken ct) =>
        {
            var pedido = await db.Pedidos.AsNoTracking()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            return pedido is null ? TypedResults.NotFound() : TypedResults.Ok(pedido);
        });

        grupo.MapPost("/", async (
            CrearPedidoRequest solicitud,
            ICatalogoClient catalogo,
            PedidosDbContext db,
            ILoggerFactory loggers,
            CancellationToken ct) =>
        {
            var errores = PedidoFactory.ValidarSolicitud(solicitud);
            if (errores.Count > 0)
                return Results.ValidationProblem(errores);

            // Validación contra catalogo por HTTP (DNS del servicio, configurado por entorno)
            var productos = new Dictionary<int, ProductoCatalogo?>();
            try
            {
                foreach (var id in solicitud.Items!.Select(i => i.ProductoId).Distinct())
                    productos[id] = await catalogo.ObtenerProductoAsync(id, ct);
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
            {
                loggers.CreateLogger("Pedidos").LogWarning(ex, "Catalogo no disponible");
                return Results.Problem(
                    title: "Catálogo no disponible",
                    detail: "No se pudo validar el pedido. Intenta de nuevo.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var resultado = PedidoFactory.Crear(solicitud, productos, DateTimeOffset.UtcNow);
            if (!resultado.EsValido)
                return Results.ValidationProblem(resultado.Errores);

            db.Pedidos.Add(resultado.Pedido!);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/pedidos/{resultado.Pedido!.Id}", resultado.Pedido);
        });

        return grupo;
    }
}
