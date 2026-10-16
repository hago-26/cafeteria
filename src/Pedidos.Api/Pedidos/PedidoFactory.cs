using Pedidos.Api.Catalogo;

namespace Pedidos.Api.Pedidos;

public sealed record ResultadoPedido(Pedido? Pedido, Dictionary<string, string[]> Errores)
{
    public bool EsValido => Pedido is not null;

    public static ResultadoPedido Exito(Pedido pedido) => new(pedido, []);
    public static ResultadoPedido Fallo(Dictionary<string, string[]> errores) => new(null, errores);
}

/// <summary>Reglas de negocio puras (sin HTTP ni BD): fáciles de probar.</summary>
public static class PedidoFactory
{
    public static Dictionary<string, string[]> ValidarSolicitud(CrearPedidoRequest? solicitud)
    {
        var errores = new Dictionary<string, string[]>();

        if (solicitud?.Items is not { Count: > 0 } items)
        {
            errores["items"] = ["El pedido debe tener al menos un producto."];
            return errores;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Cantidad <= 0)
                errores[$"items[{i}].cantidad"] = ["La cantidad debe ser mayor a 0."];
        }

        return errores;
    }

    public static ResultadoPedido Crear(
        CrearPedidoRequest solicitud,
        IReadOnlyDictionary<int, ProductoCatalogo?> productos,
        DateTimeOffset fecha)
    {
        var errores = ValidarSolicitud(solicitud);
        if (errores.Count > 0)
            return ResultadoPedido.Fallo(errores);

        var items = solicitud.Items!;
        var pedido = new Pedido { Fecha = fecha };

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (!productos.TryGetValue(item.ProductoId, out var producto) || producto is null)
            {
                errores[$"items[{i}].productoId"] = [$"El producto {item.ProductoId} no existe."];
                continue;
            }

            if (!producto.Disponible)
            {
                errores[$"items[{i}].productoId"] = [$"El producto '{producto.Nombre}' no está disponible."];
                continue;
            }

            pedido.Items.Add(new PedidoItem
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = item.Cantidad
            });
        }

        if (errores.Count > 0)
            return ResultadoPedido.Fallo(errores);

        pedido.Total = pedido.Items.Sum(x => x.PrecioUnitario * x.Cantidad);
        return ResultadoPedido.Exito(pedido);
    }
}
