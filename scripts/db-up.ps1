# Levanta db-catalogo y db-pedidos (PostgreSQL) y configura user-secrets de las APIs.
# Uso (desde la raíz del repo):  ./scripts/db-up.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

if (-not (Test-Path .env)) { throw "No existe .env. Ejecuta: Copy-Item .env.example .env  (y cambia las contraseñas)" }

# Carga .env en variables de entorno de esta sesión
Get-Content .env | Where-Object { $_ -match '^\s*([^#=\s]+)\s*=\s*(.*)$' } | ForEach-Object {
    Set-Item -Path "env:$($Matches[1])" -Value $Matches[2].Trim()
}

$PG_IMAGE = 'postgres:17-alpine'

function Start-Db($nombre, $db, $user, $pass, $puerto, $volumen) {
    $existe = docker ps -a --filter "name=^$nombre$" --format '{{.Names}}'
    if ($existe) {
        Write-Host "• $nombre ya existe: iniciando"
        docker start $nombre | Out-Null
    } else {
        Write-Host "• Creando $nombre en localhost:$puerto"
        docker run -d --name $nombre `
            -e POSTGRES_DB=$db -e POSTGRES_USER=$user -e POSTGRES_PASSWORD=$pass `
            -p "${puerto}:5432" `
            -v "${volumen}:/var/lib/postgresql/data" `
            $PG_IMAGE | Out-Null
    }
    Write-Host -NoNewline "  esperando a que acepte conexiones"
    for ($i = 0; $i -lt 30; $i++) {
        docker exec $nombre pg_isready -U $user -d $db *> $null
        if ($LASTEXITCODE -eq 0) { Write-Host " ✔"; return }
        Write-Host -NoNewline "."; Start-Sleep -Seconds 1
    }
    throw "$nombre no respondió a tiempo. Revisa: docker logs $nombre"
}

Start-Db 'db-catalogo' $env:CATALOGO_DB_NAME $env:CATALOGO_DB_USER $env:CATALOGO_DB_PASSWORD $env:CATALOGO_DB_PORT 'db-catalogo-data'
Start-Db 'db-pedidos'  $env:PEDIDOS_DB_NAME  $env:PEDIDOS_DB_USER  $env:PEDIDOS_DB_PASSWORD  $env:PEDIDOS_DB_PORT  'db-pedidos-data'

Write-Host "• Guardando cadenas de conexión en user-secrets (fuera del repo)"
dotnet user-secrets set "ConnectionStrings:Catalogo" "Host=localhost;Port=$($env:CATALOGO_DB_PORT);Database=$($env:CATALOGO_DB_NAME);Username=$($env:CATALOGO_DB_USER);Password=$($env:CATALOGO_DB_PASSWORD)" --project src/Catalogo.Api | Out-Null
dotnet user-secrets set "ConnectionStrings:Pedidos"  "Host=localhost;Port=$($env:PEDIDOS_DB_PORT);Database=$($env:PEDIDOS_DB_NAME);Username=$($env:PEDIDOS_DB_USER);Password=$($env:PEDIDOS_DB_PASSWORD)" --project src/Pedidos.Api | Out-Null

Write-Host "Listo. Bases de datos arriba y user-secrets configurados."
