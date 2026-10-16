using Pedidos.Api.Catalogo;
using Pedidos.Api.Pedidos;

namespace Pedidos.Api.Tests;

public class PedidoFactoryTests
{
    private static readonly DateTimeOffset Fecha = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Dictionary<int, ProductoCatalogo?> Catalogo = new()
    {
        [1] = new ProductoCatalogo(1, "Espresso", 35m, true),
        [5] = new ProductoCatalogo(5, "Croissant", 45m, true),
        [6] = new ProductoCatalogo(6, "Pay de limón", 60m, false),
        [999] = null // catalogo respondió 404
    };

    [Fact]
    public void Crea_pedido_y_calcula_total()
    {
        var solicitud = new CrearPedidoRequest([new(1, 2), new(5, 1)]);

        var resultado = PedidoFactory.Crear(solicitud, Catalogo, Fecha);

        Assert.True(resultado.EsValido);
        Assert.Equal(115m, resultado.Pedido!.Total);
        Assert.Equal(2, resultado.Pedido.Items.Count);
        Assert.Equal(Fecha, resultado.Pedido.Fecha);
    }

    [Fact]
    public void Copia_nombre_y_precio_del_catalogo()
    {
        var resultado = PedidoFactory.Crear(new CrearPedidoRequest([new(5, 3)]), Catalogo, Fecha);

        var item = Assert.Single(resultado.Pedido!.Items);
        Assert.Equal("Croissant", item.Nombre);
        Assert.Equal(45m, item.PrecioUnitario);
    }

    [Fact]
    public void Rechaza_pedido_sin_items()
    {
        var resultado = PedidoFactory.Crear(new CrearPedidoRequest([]), Catalogo, Fecha);

        Assert.False(resultado.EsValido);
        Assert.Contains("items", resultado.Errores.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rechaza_cantidad_no_positiva(int cantidad)
    {
        var resultado = PedidoFactory.Crear(new CrearPedidoRequest([new(1, cantidad)]), Catalogo, Fecha);

        Assert.False(resultado.EsValido);
        Assert.Contains("items[0].cantidad", resultado.Errores.Keys);
    }

    [Fact]
    public void Rechaza_producto_inexistente()
    {
        var resultado = PedidoFactory.Crear(new CrearPedidoRequest([new(999, 1)]), Catalogo, Fecha);

        Assert.False(resultado.EsValido);
        Assert.Contains("no existe", resultado.Errores["items[0].productoId"][0]);
    }

    [Fact]
    public void Rechaza_producto_no_disponible()
    {
        var resultado = PedidoFactory.Crear(new CrearPedidoRequest([new(1, 1), new(6, 1)]), Catalogo, Fecha);

        Assert.False(resultado.EsValido);
        Assert.Contains("no está disponible", resultado.Errores["items[1].productoId"][0]);
    }
}
