#!/usr/bin/env bash
# Levanta db-catalogo y db-pedidos (PostgreSQL) y configura user-secrets de las APIs.
# Uso (desde la raíz del repo):  ./scripts/db-up.sh
set -euo pipefail
cd "$(dirname "$0")/.."

[[ -f .env ]] || { echo "No existe .env. Ejecuta: cp .env.example .env  (y cambia las contraseñas)"; exit 1; }
set -a; source .env; set +a

PG_IMAGE="postgres:17-alpine"

start_db() {
  local nombre=$1 db=$2 user=$3 pass=$4 puerto=$5 volumen=$6
  if docker ps -a --format '{{.Names}}' | grep -qx "$nombre"; then
    echo "• $nombre ya existe: iniciando"
    docker start "$nombre" >/dev/null
  else
    echo "• Creando $nombre en localhost:$puerto"
    docker run -d --name "$nombre" \
      -e POSTGRES_DB="$db" -e POSTGRES_USER="$user" -e POSTGRES_PASSWORD="$pass" \
      -p "$puerto:5432" \
      -v "$volumen:/var/lib/postgresql/data" \
      "$PG_IMAGE" >/dev/null
  fi
  printf "  esperando a que acepte conexiones"
  for _ in $(seq 1 30); do
    if docker exec "$nombre" pg_isready -U "$user" -d "$db" >/dev/null 2>&1; then echo " ✔"; return; fi
    printf "."; sleep 1
  done
  echo; echo "$nombre no respondió a tiempo. Revisa: docker logs $nombre"; exit 1
}

start_db db-catalogo "$CATALOGO_DB_NAME" "$CATALOGO_DB_USER" "$CATALOGO_DB_PASSWORD" "$CATALOGO_DB_PORT" db-catalogo-data
start_db db-pedidos  "$PEDIDOS_DB_NAME"  "$PEDIDOS_DB_USER"  "$PEDIDOS_DB_PASSWORD"  "$PEDIDOS_DB_PORT"  db-pedidos-data

echo "• Guardando cadenas de conexión en user-secrets (fuera del repo)"
dotnet user-secrets set "ConnectionStrings:Catalogo" \
  "Host=localhost;Port=$CATALOGO_DB_PORT;Database=$CATALOGO_DB_NAME;Username=$CATALOGO_DB_USER;Password=$CATALOGO_DB_PASSWORD" \
  --project src/Catalogo.Api >/dev/null
dotnet user-secrets set "ConnectionStrings:Pedidos" \
  "Host=localhost;Port=$PEDIDOS_DB_PORT;Database=$PEDIDOS_DB_NAME;Username=$PEDIDOS_DB_USER;Password=$PEDIDOS_DB_PASSWORD" \
  --project src/Pedidos.Api >/dev/null

echo "Listo. Bases de datos arriba y user-secrets configurados."
