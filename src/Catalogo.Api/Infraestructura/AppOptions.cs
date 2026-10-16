using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Infraestructura;

/// <summary>Sección "App" de la configuración. Se valida al arrancar (fail fast).</summary>
public sealed class AppOptions
{
    public const string Seccion = "App";

    [Required] public string Nombre { get; set; } = string.Empty;
    [Required] public string Version { get; set; } = string.Empty;
}
