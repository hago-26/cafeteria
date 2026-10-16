using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Pedidos.Api.Catalogo;

/// <summary>Sección "Servicios:Catalogo". Variable de entorno: Servicios__Catalogo__BaseUrl.</summary>
public sealed class CatalogoOptions
{
    public const string Seccion = "Servicios:Catalogo";

    [Required, Url] public string BaseUrl { get; set; } = string.Empty;
    [Range(1, 60)] public int TimeoutSegundos { get; set; } = 5;
}

/// <summary>Lo que pedidos necesita saber de un producto. Contrato propio, no la entidad de catalogo.</summary>
public sealed record ProductoCatalogo(int Id, string Nombre, decimal Precio, bool Disponible);

public interface ICatalogoClient
{
    /// <returns>El producto, o null si catalogo responde 404.</returns>
    /// <exception cref="HttpRequestException">Catalogo no responde o responde con error.</exception>
    Task<ProductoCatalogo?> ObtenerProductoAsync(int id, CancellationToken ct);
}

public sealed class CatalogoClient(HttpClient http) : ICatalogoClient
{
    public async Task<ProductoCatalogo?> ObtenerProductoAsync(int id, CancellationToken ct)
    {
        using var respuesta = await http.GetAsync($"productos/{id}", ct);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ProductoCatalogo>(ct);
    }
}
