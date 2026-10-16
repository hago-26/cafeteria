using Catalogo.Api.Datos;
using Catalogo.Api.Infraestructura;
using Catalogo.Api.Productos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración externa + fail fast ---
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var cadena = builder.Configuration.GetConnectionString("Catalogo");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Catalogo' (variable: ConnectionStrings__Catalogo).");

// --- Servicios ---
builder.Services.AddDbContext<CatalogoDbContext>(o => o.UseNpgsql(cadena));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogoDbContext>("db", tags: ["ready"]);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Pipeline ---
app.UseExceptionHandler();
app.UseServidoPor();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapEndpointsComunes();
app.MapProductos();

await app.InicializarBaseDeDatosAsync<CatalogoDbContext>();

app.Run();
