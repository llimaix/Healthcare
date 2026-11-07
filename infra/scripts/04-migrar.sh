#!/usr/bin/env bash
# Dispara el migrator de un solo uso: aplica las migraciones EF al primario y espera
# su salida. La red overlay del stack es 'attachable', asi que se corre un contenedor
# efimero adjunto a ella con 'docker run --rm' (bloquea hasta que el migrator termina).
#
# La password de la BD la inyecta el entrypoint desde el secret mysql_app_password.
# Como 'docker run' no monta Docker secrets de servicio, aqui se pasa el secret como
# archivo temporal montado en /run/secrets (se borra al salir).
set -euo pipefail

red="clinicacore_clinicacore-net"
secret_tmp="$(mktemp -d)"
trap 'rm -rf "${secret_tmp}"' EXIT

# El secret de servicio no es accesible desde 'docker run'; se obtiene el valor del
# secret mysql_app_password tomandolo de la variable de entorno si el operador la
# exporta, o leyendolo de una tarea viva (api) que ya lo tiene montado.
if [ -n "${MYSQL_APP_PASSWORD:-}" ]; then
  printf '%s' "${MYSQL_APP_PASSWORD}" > "${secret_tmp}/mysql_app_password"
else
  # Lee el secret desde una tarea viva que ya lo tiene montado (api o mysql-primary).
  cid="$(docker ps --filter name=clinicacore_api -q | head -n1)"
  if [ -z "${cid}" ]; then
    echo "No puedo obtener mysql_app_password: exporta MYSQL_APP_PASSWORD o levanta el servicio api primero." >&2
    exit 1
  fi
  docker exec "${cid}" cat /run/secrets/mysql_app_password > "${secret_tmp}/mysql_app_password"
fi

echo "Aplicando migraciones con clinicacore-migrator:local sobre la red ${red} ..."
docker run --rm \
  --network "${red}" \
  -e CLINICACORE_ConnectionStrings__ClinicaCore="Server=mysql-primary;Port=3306;Database=clinicacore;User=clinicacore;" \
  -e MYSQL_APP_PASSWORD_FILE=/run/secrets/mysql_app_password \
  --mount type=bind,src="${secret_tmp}/mysql_app_password",dst=/run/secrets/mysql_app_password,ro \
  clinicacore-migrator:local

echo "Migraciones aplicadas."

# La Api siembra el catalogo de roles al arrancar, pero en el stack puede haber
# arrancado ANTES de que existieran las tablas (la siembra es tolerante y no tumba el
# proceso; ver api/Datos/RolSeeder.cs). Ya con el esquema aplicado, se fuerza un
# reinicio suave de la Api para que siembre los roles contra la BD migrada. Idempotente:
# si los roles ya existen, la siembra no hace nada.
if docker service inspect clinicacore_api >/dev/null 2>&1; then
  echo "Reiniciando la Api para sembrar roles contra la BD migrada..."
  docker service update --force clinicacore_api >/dev/null
  echo "Api reiniciada."
fi
