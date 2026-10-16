using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Pedidos.Api.Infraestructura;

// Código duplicado a propósito en cada servicio (en lugar de una librería compartida):
// cada microservicio se construye y despliega de forma independiente.
public static class EndpointsComunes
{
    /// <summary>/info, /health/live y /health/ready.</summary>
    public static WebApplication MapEndpointsComunes(this WebApplication app)
    {
        app.MapGet("/info", (IOptions<AppOptions> opciones, IHostEnvironment entorno) =>
                TypedResults.Ok(new InfoResponse(
                    opciones.Value.Nombre,
                    Environment.MachineName,
                    entorno.EnvironmentName,
                    opciones.Value.Version)))
            .WithTags("Infra");

        // live: el proceso responde. No revisa dependencias.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        // ready: puede atender tráfico (su base de datos responde).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains("ready")
        });

        return app;
    }

    /// <summary>Agrega X-Servido-Por con el host que respondió (para ver el balanceo).</summary>
    public static WebApplication UseServidoPor(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Servido-Por"] = Environment.MachineName;
            await next();
        });
        return app;
    }

    /// <summary>Crea el esquema si Database:InicializarAlArrancar = true. Si la BD no responde, falla al arrancar.</summary>
    public static async Task InicializarBaseDeDatosAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        if (!app.Configuration.GetValue("Database:InicializarAlArrancar", false))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Base de datos lista ({Contexto})", typeof(TContext).Name);
    }
}

public sealed record InfoResponse(string Servicio, string Host, string Entorno, string Version);
