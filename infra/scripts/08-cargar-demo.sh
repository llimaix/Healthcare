#!/usr/bin/env bash
# OPCIONAL: carga datos de demostración (infra/mysql/seed-demo.sql) en el primario.
# Ejecutar SOLO cuando el stack ya esté estable y las migraciones aplicadas
# (despues de 04-migrar.sh / 05-verificar-salud.sh).
#
# Idempotente: el SQL usa "WHERE NOT EXISTS", así que repetir la carga no duplica datos.
# Los datos se insertan en el primario y se replican solos al mysql-replica.
set -euo pipefail

raiz="$(cd "$(dirname "$0")/../.." && pwd)"
seed="${raiz}/infra/mysql/seed-demo.sql"

if [ ! -f "${seed}" ]; then
  echo "No se encontro el archivo de datos: ${seed}" >&2
  exit 1
fi

# Contenedor del primario (tarea viva del servicio).
cid="$(docker ps --filter name=clinicacore_mysql-primary -q | head -n1)"
if [ -z "${cid}" ]; then
  echo "mysql-primary no esta corriendo. Levanta el stack primero (03-deploy.sh)." >&2
  exit 1
fi

# La root password vive como secret dentro del contenedor; nunca se escribe a disco.
root_pw="$(docker exec "${cid}" cat /run/secrets/mysql_root_password)"

echo "Cargando datos de demostracion desde ${seed} en el primario ..."
# --default-character-set=utf8mb4: el seed esta en UTF-8. Sin esto, el cliente mysql
# usa latin1 por defecto y los acentos/ñ se corrompen (mojibake tipo "MarÃ­a").
docker exec -i "${cid}" sh -c "exec mysql --default-character-set=utf8mb4 -uroot -p\"${root_pw}\" clinicacore" < "${seed}"

echo "Datos de demostracion cargados. Se replican automaticamente a mysql-replica."
