#!/usr/bin/env bash
# Detiene y elimina los contenedores de BD. Los volúmenes (datos) se conservan.
# Para borrar también los datos:  ./scripts/db-down.sh --borrar-datos
set -uo pipefail

docker rm -f db-catalogo db-pedidos >/dev/null 2>&1
echo "Contenedores eliminados."

if [[ "${1:-}" == "--borrar-datos" ]]; then
  docker volume rm db-catalogo-data db-pedidos-data >/dev/null 2>&1
  echo "Volúmenes eliminados (datos borrados)."
fi
