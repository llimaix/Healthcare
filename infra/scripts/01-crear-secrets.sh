#!/usr/bin/env bash
# Crea los 4 Docker secrets de ClinicaCore si no existen. Idempotente.
# Las passwords se toman de variables de entorno si estan presentes; si no, se
# generan al vuelo. NUNCA se escriben a disco ni se commitean: van directo a
# 'docker secret create' por stdin.
#
# Variables opcionales (si no se definen, se generan):
#   MYSQL_ROOT_PASSWORD, MYSQL_APP_PASSWORD, MYSQL_REPL_PASSWORD, JWT_SECRET
set -euo pipefail

# Genera una cadena aleatoria segura (hex) de ~48 chars.
generar() { openssl rand -hex 24; }

# Password para el usuario de replicacion: MySQL limita SOURCE_PASSWORD en
# CHANGE REPLICATION SOURCE TO a 32 caracteres (error 3056 si se excede). 30 hex
# chars (openssl rand -hex 15) quedan por debajo del limite con margen.
generar_repl() { openssl rand -hex 15; }

# crear_secret <nombre> <valor>
crear_secret() {
  nombre="$1"
  valor="$2"
  if docker secret inspect "${nombre}" >/dev/null 2>&1; then
    echo "secret ${nombre} ya existe; se conserva."
    return 0
  fi
  printf '%s' "${valor}" | docker secret create "${nombre}" - >/dev/null
  echo "secret ${nombre} creado."
}

crear_secret mysql_root_password "${MYSQL_ROOT_PASSWORD:-$(generar)}"
crear_secret mysql_app_password  "${MYSQL_APP_PASSWORD:-$(generar)}"
crear_secret mysql_repl_password "${MYSQL_REPL_PASSWORD:-$(generar_repl)}"
# El Jwt:Secret necesita suficiente entropia para HMAC-SHA256 (>= 32 bytes).
crear_secret jwt_secret "${JWT_SECRET:-$(openssl rand -hex 32)}"

echo "Listo. Secrets disponibles:"
docker secret ls --format '  {{.Name}}'
