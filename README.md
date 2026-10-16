# ☕ Cafetería — microservicios .NET 10 + Docker

Proyecto práctico del curso de Docker avanzado. Se construye por etapas (0 → 18).

## Arquitectura

```
navegador ──► gateway (NGINX, :8080)  ─┬─► catalogo-api (:8080) ──► db-catalogo (PostgreSQL)
              UI estática + /api/*      └─► pedidos-api  (:8080) ──► db-pedidos  (PostgreSQL)
                                               └── HTTP ──► catalogo-api
```

| Servicio | Ruta en gateway | Endpoints propios |
|---|---|---|
| catalogo-api | `/api/catalogo/*` | `GET /productos`, `GET /productos/{id}` |
| pedidos-api | `/api/pedidos/*` | `GET /pedidos`, `GET /pedidos/{id}`, `POST /pedidos` |
| ambos | | `GET /info`, `GET /health/live`, `GET /health/ready` |

Todas las respuestas incluyen la cabecera `X-Servido-Por` con el host que respondió.

## Estructura

```
cafeteria/
├── global.json                 # fija el SDK de .NET 10
├── Cafeteria.slnx              # solución
├── Directory.Build.props       # net10.0, nullable, etc. para todos
├── Directory.Packages.props    # versiones de NuGet centralizadas
├── .env.example                # plantilla de variables (versionada)
├── src/
│   ├── Catalogo.Api/
│   └── Pedidos.Api/
├── tests/
│   └── Pedidos.Api.Tests/      # xUnit
├── gateway/
│   └── ui/                     # HTML + JS (el NGINX llega en la etapa 10)
└── scripts/                    # db-up / db-down (desarrollo local)
```

## Configuración

Orden de precedencia (el último gana):
`appsettings.json` → `appsettings.{Entorno}.json` → user-secrets (solo Development) → variables de entorno → argumentos.

| Clave | Variable de entorno |
|---|---|
| `ConnectionStrings:Catalogo` | `ConnectionStrings__Catalogo` |
| `ConnectionStrings:Pedidos` | `ConnectionStrings__Pedidos` |
| `Servicios:Catalogo:BaseUrl` | `Servicios__Catalogo__BaseUrl` |
| `App:Version` | `App__Version` |
| entorno | `ASPNETCORE_ENVIRONMENT` (`Development` / `Staging` / `Production`) |

Si falta una clave obligatoria, la API **no arranca** (fail fast).

## Desarrollo local (etapa 0)

Requisitos: .NET SDK 10, Docker Desktop, Git.

```bash
cp .env.example .env            # Windows: Copy-Item .env.example .env
# edita .env y cambia las contraseñas

./scripts/db-up.sh              # Windows: ./scripts/db-up.ps1
dotnet restore
dotnet build
dotnet test

# dos terminales
dotnet run --project src/Catalogo.Api   # http://localhost:5001
dotnet run --project src/Pedidos.Api    # http://localhost:5002
```

Peticiones de ejemplo: `src/*/*.http` (VS / VS Code con REST Client / Rider).

Apagar BD: `./scripts/db-down.sh` (conserva datos) · `./scripts/db-down.sh --borrar-datos`.
