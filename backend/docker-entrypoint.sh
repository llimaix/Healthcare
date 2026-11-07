#!/usr/bin/env sh
# Entrypoint comun de las imagenes .NET (Api, Gateway y migrator).
# Lee los Docker secrets montados en /run/secrets y los traduce a las variables de
# configuracion con prefijo CLINICACORE_ (separador __) que espera el backend, SIN que
# las credenciales aparezcan nunca en el stack.yml ni en el historial de la imagen.
#
#   - jwt_secret          -> CLINICACORE_Jwt__Secret            (Api y Gateway)
#   - mysql_app_password  -> se inyecta en la cadena de conexion (Api y migrator)
#
# La cadena base (sin password) llega por CLINICACORE_ConnectionStrings__ClinicaCore;
# aqui se le anade "Password=<secret>;". Si no hay secret de BD montado (p. ej. el
# Gateway, que no habla con MySQL), la cadena se deja tal cual.
#
# sh (POSIX): la imagen runtime de aspnet no trae bash.
set -eu

JWT_SECRET_FILE="${JWT_SECRET_FILE:-/run/secrets/jwt_secret}"
MYSQL_APP_PASSWORD_FILE="${MYSQL_APP_PASSWORD_FILE:-/run/secrets/mysql_app_password}"

# Jwt:Secret compartido por Api y Gateway (misma clave para firmar y validar).
if [ -f "${JWT_SECRET_FILE}" ]; then
  CLINICACORE_Jwt__Secret="$(cat "${JWT_SECRET_FILE}")"
  export CLINICACORE_Jwt__Secret
fi

# Password de la BD: se concatena a la cadena base solo si hay secret montado.
if [ -f "${MYSQL_APP_PASSWORD_FILE}" ] && [ -n "${CLINICACORE_ConnectionStrings__ClinicaCore:-}" ]; then
  _db_pass="$(cat "${MYSQL_APP_PASSWORD_FILE}")"
  case "${CLINICACORE_ConnectionStrings__ClinicaCore}" in
    *[!\;]) _sep=";" ;;  # la cadena no termina en ';'
    *)      _sep=""  ;;
  esac
  CLINICACORE_ConnectionStrings__ClinicaCore="${CLINICACORE_ConnectionStrings__ClinicaCore}${_sep}Password=${_db_pass};"
  export CLINICACORE_ConnectionStrings__ClinicaCore
  unset _db_pass _sep
fi

# Cede el control al proceso real (la Api, el Gateway o 'dotnet ef ...').
exec "$@"
