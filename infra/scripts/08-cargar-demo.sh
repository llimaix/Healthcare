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
# asi que un INSERT plano no permitiria iniciar sesion. Se crean por el endpoint
# publico /api/auth/register a traves de nginx, que hashea correctamente.
# Idempotente: HTTP 201 = creado, 409 = ya existe (ambos se consideran OK).
base_url="${DEMO_BASE_URL:-http://localhost:8090}"
password="${DEMO_PASSWORD:-Demo123!}"

crear_usuario() {
  correo="$1"; rol="$2"
  code="$(curl -s -o /dev/null -w '%{http_code}' \
    -X POST "${base_url}/api/auth/register" \
    -H 'Content-Type: application/json' \
    -d "{\"correo\":\"${correo}\",\"password\":\"${password}\",\"rol\":\"${rol}\"}" || true)"
  case "${code}" in
    201) echo "  usuario ${rol} creado: ${correo}" ;;
    409) echo "  usuario ${rol} ya existia: ${correo}" ;;
    "")  echo "  AVISO: no se pudo contactar ${base_url} para crear ${correo} (¿nginx arriba?)." >&2 ;;
    *)   echo "  AVISO: register de ${correo} devolvio HTTP ${code}." >&2 ;;
  esac
}

echo "Creando usuarios de demostracion (uno por rol) en ${base_url} ..."
# El Admin conserva la contrasena del README (Admin123!); los demas usan DEMO_PASSWORD.
admin_password="${DEMO_ADMIN_PASSWORD:-Admin123!}"
code="$(curl -s -o /dev/null -w '%{http_code}' \
  -X POST "${base_url}/api/auth/register" \
  -H 'Content-Type: application/json' \
  -d "{\"correo\":\"admin@clinicacore.test\",\"password\":\"${admin_password}\",\"rol\":\"Admin\"}" || true)"
case "${code}" in
  201) echo "  usuario Admin creado: admin@clinicacore.test" ;;
  409) echo "  usuario Admin ya existia: admin@clinicacore.test" ;;
  *)   echo "  AVISO: register de admin@clinicacore.test devolvio HTTP ${code:-sin respuesta}." >&2 ;;
esac
crear_usuario "medico@clinicacore.test"    "Medico"
crear_usuario "recepcion@clinicacore.test" "Recepcion"
crear_usuario "lectura@clinicacore.test"   "Lectura"
echo "Usuarios de demostracion listos."
echo "  admin@clinicacore.test     -> ${admin_password}  (rol Admin)"
echo "  medico@clinicacore.test    -> ${password}  (rol Medico)"
echo "  recepcion@clinicacore.test -> ${password}  (rol Recepcion)"
echo "  lectura@clinicacore.test   -> ${password}  (rol Lectura)"
