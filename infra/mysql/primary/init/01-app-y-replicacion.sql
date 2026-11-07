-- Inicializacion del primario ClinicaCore (MySQL 8).
-- PLANTILLA: se ejecuta UNA sola vez cuando el datadir esta vacio, a traves del
-- script hermano 00-aplicar-plantilla.sh, que sustituye las variables ${...} con
-- envsubst y canaliza el resultado al cliente mysql. Las passwords NUNCA van en
-- claro: llegan como variables de entorno que el contenedor oficial de MySQL deriva
-- de Docker secrets (MYSQL_PASSWORD de mysql_app_password, MYSQL_REPL_PASSWORD de
-- mysql_repl_password mediante sus variantes *_FILE).
--
-- Nota: la BD clinicacore y el usuario de aplicacion tambien los crea el entrypoint
-- oficial a partir de MYSQL_DATABASE/MYSQL_USER; aqui se reafirma de forma idempotente
-- y se fijan los privilegios exactos sobre clinicacore, ademas del usuario de replicacion.

-- Base de datos de la aplicacion.
CREATE DATABASE IF NOT EXISTS clinicacore
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- Usuario de aplicacion con permisos acotados a clinicacore.
CREATE USER IF NOT EXISTS '${MYSQL_USER}'@'%' IDENTIFIED BY '${MYSQL_PASSWORD}';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES, DROP
  ON clinicacore.* TO '${MYSQL_USER}'@'%';

-- Usuario dedicado de replicacion (solo el privilegio necesario).
CREATE USER IF NOT EXISTS '${MYSQL_REPL_USER}'@'%' IDENTIFIED BY '${MYSQL_REPL_PASSWORD}';
GRANT REPLICATION SLAVE ON *.* TO '${MYSQL_REPL_USER}'@'%';

FLUSH PRIVILEGES;
