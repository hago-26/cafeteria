using System.Net;
using System.Text;
using Pedidos.Api.Catalogo;

namespace Pedidos.Api.Tests;

public class CatalogoClientTests
{
    private static (CatalogoClient cliente, StubHandler handler) Crear(HttpStatusCode codigo, string? json = null)
    {
        var handler = new StubHandler(codigo, json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://catalogo-api:8080/") };
        return (new CatalogoClient(http), handler);
    }

    [Fact]
    public async Task Devuelve_producto_cuando_catalogo_responde_200()
    {
        var (cliente, handler) = Crear(HttpStatusCode.OK,
            """{"id":3,"nombre":"Capuchino","precio":55.00,"disponible":true}""");

        var producto = await cliente.ObtenerProductoAsync(3, CancellationToken.None);

        Assert.Equal(new ProductoCatalogo(3, "Capuchino", 55m, true), producto);
        Assert.Equal("http://catalogo-api:8080/productos/3", handler.UltimaUri?.ToString());
    }

    [Fact]
    public async Task Devuelve_null_cuando_catalogo_responde_404()
    {
        var (cliente, _) = Crear(HttpStatusCode.NotFound);

        Assert.Null(await cliente.ObtenerProductoAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task Lanza_excepcion_cuando_catalogo_falla()
    {
        var (cliente, _) = Crear(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => cliente.ObtenerProductoAsync(1, CancellationToken.None));
    }

    private sealed class StubHandler(HttpStatusCode codigo, string? json) : HttpMessageHandler
    {
        public Uri? UltimaUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            UltimaUri = request.RequestUri;
            var respuesta = new HttpResponseMessage(codigo);
            if (json is not null)
                respuesta.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return Task.FromResult(respuesta);
        }
    }
}
