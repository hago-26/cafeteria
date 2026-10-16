namespace Pedidos.Api.Pedidos;

public sealed class Pedido
{
    public int Id { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public decimal Total { get; set; }
    public List<PedidoItem> Items { get; set; } = [];
}

/// <summary>Copia nombre y precio al momento del pedido: no depende de que catalogo cambie después.</summary>
public sealed class PedidoItem
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
}

public sealed record CrearPedidoRequest(List<ItemPedidoRequest>? Items);

public sealed record ItemPedidoRequest(int ProductoId, int Cantidad);
