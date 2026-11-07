#!/usr/bin/env bash
# Respaldo logico de la BD clinicacore contra el PRIMARIO via 'docker exec' + mysqldump.
# El volcado se escribe a ./backups/ (ignorado por git). No deja passwords en disco:
# la root password se lee dentro del contenedor desde el secret montado.
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
destino="${raiz}/backups"
mkdir -p "${destino}"

cid="$(docker ps --filter name=clinicacore_mysql-primary -q | head -n1)"
if [ -z "${cid}" ]; then
  echo "No encuentro el contenedor mysql-primary en ejecucion." >&2
  exit 1
fi

sello="$(date +%Y%m%d-%H%M%S)"
archivo="${destino}/clinicacore-${sello}.sql"

echo "Respaldando clinicacore del primario a ${archivo} ..."
docker exec "${cid}" sh -lc \
  'mysqldump -uroot -p"$(cat /run/secrets/mysql_root_password)" --single-transaction --routines --triggers clinicacore' \
  > "${archivo}"

echo "Backup completado: ${archivo}"
