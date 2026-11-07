#!/usr/bin/env bash
# Aplica la plantilla 01-app-y-replicacion.sql sustituyendo las variables de entorno
# (usuarios/passwords derivados por el contenedor oficial de MySQL desde Docker secrets).
# Se ejecuta UNA sola vez durante docker-entrypoint-initdb.d, cuando el datadir esta vacio.
# No deja passwords en disco: la sustitucion se canaliza directo al cliente mysql.
#
# IMPORTANTE: el stack entrega las credenciales como *_FILE (Docker secrets), no como
# variables en claro. Aqui se resuelven leyendo el archivo del secret si existe, con
# respaldo a la variable directa. Se evita 'envsubst' (gettext) porque la imagen
# oficial de MySQL no lo incluye; la sustitucion se hace con 'sed', siempre disponible.
set -euo pipefail

dir="$(dirname "$0")"
plantilla="${dir}/01-app-y-replicacion.sql"

# leer_secreto <NOMBRE_VAR>: imprime el valor del secret tomandolo de <VAR>_FILE si
# existe, o de <VAR> en su defecto.
leer_secreto() {
  nombre="$1"
  archivo_var="${nombre}_FILE"
  archivo="$(eval "printf '%s' \"\${${archivo_var}:-}\"")"
  if [ -n "${archivo}" ] && [ -f "${archivo}" ]; then
    cat "${archivo}"
  else
    eval "printf '%s' \"\${${nombre}:-}\""
  fi
}

ROOT_PASSWORD="$(leer_secreto MYSQL_ROOT_PASSWORD)"
APP_PASSWORD="$(leer_secreto MYSQL_PASSWORD)"
REPL_PASSWORD="$(leer_secreto MYSQL_REPL_PASSWORD)"
APP_USER="${MYSQL_USER:?falta MYSQL_USER}"
REPL_USER="${MYSQL_REPL_USER:?falta MYSQL_REPL_USER}"

# Sustituye los marcadores de la plantilla por sus valores y canaliza al cliente mysql
# local (socket, como root). Se usan delimitadores alternos en sed por si el valor
# contiene '/'. El SQL resuelto nunca toca el disco.
sed \
  -e "s|\${MYSQL_USER}|${APP_USER}|g" \
  -e "s|\${MYSQL_PASSWORD}|${APP_PASSWORD}|g" \
  -e "s|\${MYSQL_REPL_USER}|${REPL_USER}|g" \
  -e "s|\${MYSQL_REPL_PASSWORD}|${REPL_PASSWORD}|g" \
  "${plantilla}" \
  | mysql --protocol=socket -uroot -p"${ROOT_PASSWORD}"
