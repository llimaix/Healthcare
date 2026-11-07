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

# -----------------------------------------------------------------------------
# Usuarios de demostracion: uno por rol.
# -----------------------------------------------------------------------------
# NO se insertan por SQL: la password se guarda con hash (PasswordHasher de .NET),
# asi que un INSERT plano no permitiria iniciar sesion. Se crean con
# /api/auth/register a traves de nginx, que hashea correctamente.
#
# Reglas del registro: con el sistema vacio, el primer usuario se crea sin token y
# debe ser Admin. Despues, solo un Admin autenticado puede registrar usuarios. Por eso
# el script crea (o reutiliza) el Admin, inicia sesion con el y registra a los demas
# con su token. Idempotente: HTTP 201 = creado, 409 = ya existe.
base_url="${DEMO_BASE_URL:-http://localhost:8090}"
admin_correo="${DEMO_ADMIN_CORREO:-admin@clinicacore.test}"
admin_password="${DEMO_ADMIN_PASSWORD:-Admin123!}"
password="${DEMO_PASSWORD:-Demo123!}"

# POST /api/auth/register e imprime el codigo HTTP.
#   $1 correo  $2 rol  $3 password  $4 token (vacio = sin token)  $5 JSON extra opcional
# Compatible con el bash 3.2 de macOS (sin arreglos vacios bajo set -u).
registrar() {
  cuerpo="{\"correo\":\"$1\",\"password\":\"$3\",\"rol\":\"$2\"${5:-}}"
  if [ -n "${4:-}" ]; then
    curl -s -o /dev/null -w '%{http_code}' -X POST "${base_url}/api/auth/register" \
      -H 'Content-Type: application/json' -H "Authorization: Bearer $4" -d "${cuerpo}" || true
  else
    curl -s -o /dev/null -w '%{http_code}' -X POST "${base_url}/api/auth/register" \
      -H 'Content-Type: application/json' -d "${cuerpo}" || true
  fi
}

echo "Creando usuarios de demostracion (uno por rol) en ${base_url} ..."

# 1) Admin: solo se puede crear sin token si el sistema aun no tiene usuarios.
code="$(registrar "${admin_correo}" "Admin" "${admin_password}")"
case "${code}" in
  201) echo "  usuario Admin creado: ${admin_correo}" ;;
  409|401) echo "  usuario Admin ya existe o el sistema ya tiene usuarios; se reutiliza ${admin_correo}" ;;
  "")  echo "No se pudo contactar ${base_url} (¿nginx arriba? revisa 05-verificar-salud.sh)." >&2; exit 1 ;;
  *)   echo "  AVISO: register de ${admin_correo} devolvio HTTP ${code}." >&2 ;;
esac

# 2) Login del Admin para registrar al resto.
token="$(curl -s -X POST "${base_url}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"correo\":\"${admin_correo}\",\"password\":\"${admin_password}\"}" \
  | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')"
if [ -z "${token}" ]; then
  echo "No se pudo iniciar sesion como ${admin_correo}." >&2
  echo "Si el primer Admin se creo con otras credenciales, exporta DEMO_ADMIN_CORREO y DEMO_ADMIN_PASSWORD." >&2
  exit 1
fi

# 3) Resto de roles, registrados por el Admin. El usuario medico se vincula al
#    medico 1 del seed (Ana Garcia).
crear_usuario() {
  correo="$1"; rol="$2"; extra_json="${3:-}"
  code="$(registrar "${correo}" "${rol}" "${password}" "${token}" "${extra_json}")"
  case "${code}" in
    201) echo "  usuario ${rol} creado: ${correo}" ;;
    409) echo "  usuario ${rol} ya existia: ${correo}" ;;
    *)   echo "  AVISO: register de ${correo} devolvio HTTP ${code:-sin respuesta}." >&2 ;;
  esac
}

crear_usuario "medico@clinicacore.test"    "Medico" ',"idMedico":1'
crear_usuario "recepcion@clinicacore.test" "Recepcion"
crear_usuario "lectura@clinicacore.test"   "Lectura"

echo "Usuarios de demostracion listos."
echo "  ${admin_correo}     -> ${admin_password}  (rol Admin)"
echo "  medico@clinicacore.test    -> ${password}  (rol Medico)"
echo "  recepcion@clinicacore.test -> ${password}  (rol Recepcion)"
echo "  lectura@clinicacore.test   -> ${password}  (rol Lectura)"
