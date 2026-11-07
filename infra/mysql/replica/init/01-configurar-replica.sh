#!/usr/bin/env bash
# Configura la replica ClinicaCore apuntando al primario (servicio 'mysql-primary').
# Se ejecuta UNA sola vez durante docker-entrypoint-initdb.d de la replica (datadir vacio).
# Terminologia MySQL 8.0.22+: CHANGE REPLICATION SOURCE TO / START REPLICA.
#
# Replicacion por binlog clasico (SOURCE_AUTO_POSITION=0). Para una instancia NUEVA,
# las coordenadas iniciales son deterministas: se leen del primario en este momento
# (SHOW MASTER STATUS) justo antes de arrancar la replica, de modo que la replica
# empieza a seguir el binlog desde la posicion actual del primario recien inicializado.
set -euo pipefail

PRIMARIO_HOST="${MYSQL_PRIMARY_HOST:-mysql-primary}"
PRIMARIO_PORT="${MYSQL_PRIMARY_PORT:-3306}"
REPL_USER="${MYSQL_REPL_USER:?falta MYSQL_REPL_USER}"
REPL_PASSWORD="${MYSQL_REPL_PASSWORD:?falta MYSQL_REPL_PASSWORD}"

# Cliente root local (socket) para configurar esta replica.
mysql_local() { mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" "$@"; }
# Cliente contra el primario con el usuario de replicacion (red overlay).
mysql_primario() {
  mysql -h "${PRIMARIO_HOST}" -P "${PRIMARIO_PORT}" \
    -u"${REPL_USER}" -p"${REPL_PASSWORD}" "$@"
}

echo "Esperando a que el primario ${PRIMARIO_HOST}:${PRIMARIO_PORT} responda..."
until mysql_primario -e 'SELECT 1' >/dev/null 2>&1; do
  sleep 3
done
echo "Primario disponible."

# Coordenadas actuales del primario (deterministas para una instancia nueva).
estado="$(mysql_primario -N -e 'SHOW MASTER STATUS')"
LOG_FILE="$(echo "${estado}" | awk '{print $1}')"
LOG_POS="$(echo "${estado}" | awk '{print $2}')"
echo "Coordenadas del primario: archivo=${LOG_FILE} posicion=${LOG_POS}"

mysql_local <<SQL
STOP REPLICA;
RESET REPLICA ALL;
CHANGE REPLICATION SOURCE TO
  SOURCE_HOST='${PRIMARIO_HOST}',
  SOURCE_PORT=${PRIMARIO_PORT},
  SOURCE_USER='${REPL_USER}',
  SOURCE_PASSWORD='${REPL_PASSWORD}',
  SOURCE_LOG_FILE='${LOG_FILE}',
  SOURCE_LOG_POS=${LOG_POS},
  SOURCE_AUTO_POSITION=0,
  GET_SOURCE_PUBLIC_KEY=1;
START REPLICA;
SQL

echo "Replica configurada y arrancada. Estado:"
mysql_local -e 'SHOW REPLICA STATUS\G' | grep -E 'Replica_(IO|SQL)_Running|Last_.*Error' || true
