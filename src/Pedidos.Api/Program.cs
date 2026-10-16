using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pedidos.Api.Catalogo;
using Pedidos.Api.Datos;
using Pedidos.Api.Infraestructura;
using Pedidos.Api.Pedidos;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración externa + fail fast ---
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CatalogoOptions>()
    .Bind(builder.Configuration.GetSection(CatalogoOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var cadena = builder.Configuration.GetConnectionString("Pedidos");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Pedidos' (variable: ConnectionStrings__Pedidos).");

// --- Servicios ---
builder.Services.AddDbContext<PedidosDbContext>(o => o.UseNpgsql(cadena));

// Cliente HTTP tipado hacia catalogo-api. La URL viene de configuración, nunca del código.
builder.Services.AddHttpClient<ICatalogoClient, CatalogoClient>((sp, http) =>
{
    var opciones = sp.GetRequiredService<IOptions<CatalogoOptions>>().Value;
    http.BaseAddress = new Uri(opciones.BaseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
});

// ready = su propia BD. No se encadena a catalogo para no propagar caídas.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PedidosDbContext>("db", tags: ["ready"]);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Pipeline ---
app.UseExceptionHandler();
app.UseServidoPor();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapEndpointsComunes();
app.MapPedidos();

await app.InicializarBaseDeDatosAsync<PedidosDbContext>();

app.Run();
