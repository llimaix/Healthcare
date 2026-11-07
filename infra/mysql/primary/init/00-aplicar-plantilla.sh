#!/usr/bin/env bash
# Aplica la plantilla 01-app-y-replicacion.sql sustituyendo las variables de entorno
# (usuarios/passwords derivados por el contenedor oficial de MySQL desde Docker secrets).
# Se ejecuta UNA sola vez durante docker-entrypoint-initdb.d, cuando el datadir esta vacio.
# No deja passwords en disco: envsubst canaliza el SQL resuelto directo al cliente mysql.
set -euo pipefail

dir="$(dirname "$0")"
plantilla="${dir}/01-app-y-replicacion.sql"

# El cliente mysql local corre como root sobre el socket; MYSQL_* vienen del entorno.
# Solo se exportan las variables que la plantilla usa, para no filtrar otras al SQL.
envsubst '${MYSQL_USER} ${MYSQL_PASSWORD} ${MYSQL_REPL_USER} ${MYSQL_REPL_PASSWORD}' \
  < "${plantilla}" \
  | mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}"
