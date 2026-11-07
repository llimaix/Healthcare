#!/usr/bin/env bash
# Restaura un volcado logico de clinicacore en el PRIMARIO via 'docker exec' + mysql.
# Uso: 07-restore-mysql.sh <archivo.sql>
# La replica se pone al dia por replicacion normal; no se restaura directamente en ella.
set -euo pipefail

archivo="${1:-}"
if [ -z "${archivo}" ] || [ ! -f "${archivo}" ]; then
  echo "Uso: $0 <archivo.sql>  (archivo de backup existente)" >&2
  exit 1
fi

cid="$(docker ps --filter name=clinicacore_mysql-primary -q | head -n1)"
if [ -z "${cid}" ]; then
  echo "No encuentro el contenedor mysql-primary en ejecucion." >&2
  exit 1
fi

echo "Restaurando ${archivo} en clinicacore (primario) ..."
docker exec -i "${cid}" sh -lc \
  'mysql -uroot -p"$(cat /run/secrets/mysql_root_password)" clinicacore' \
  < "${archivo}"

echo "Restauracion completada."
