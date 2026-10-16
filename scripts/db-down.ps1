# Detiene y elimina los contenedores de BD. Los volúmenes (datos) se conservan.
# Para borrar también los datos:  ./scripts/db-down.ps1 -BorrarDatos
param([switch]$BorrarDatos)

docker rm -f db-catalogo db-pedidos 2>$null | Out-Null
Write-Host "Contenedores eliminados."

if ($BorrarDatos) {
    docker volume rm db-catalogo-data db-pedidos-data 2>$null | Out-Null
    Write-Host "Volúmenes eliminados (datos borrados)."
}
