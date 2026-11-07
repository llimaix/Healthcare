# Fase 4 — Despliegue con Docker y verificacion end-to-end

Este documento describe la capa de contenedores de ClinicaCore (EMR), los pasos
exactos para desplegarla desde cero en un unico nodo con Docker Swarm, y la evidencia
real de la verificacion end-to-end ejecutada contra el servidor Docker 28.5.1.

El objetivo del entregable es un despliegue reproducible con un punto de entrada unico
(nginx), el backend .NET (Api de dominio + Gateway BFF), y MySQL 8 con replicacion
primario -> replica. La alta disponibilidad se demuestra **a nivel de contenedor**
(replicas y reinicio automatico de Swarm), no a nivel de maquina: ver la seccion
[Limitacion de un solo nodo](#limitacion-de-un-solo-nodo).

## Arquitectura de contenedores y topologia

```
                         host:8080
                            |
                     +-------------+
                     |    nginx    |  (SPA + reverse-proxy /api) replicas: 2
                     +-------------+
                            |  proxy_pass /api -> gateway:5000
                     +-------------+
                     |   gateway   |  (BFF, JWT passthrough)     replicas: 2
                     +-------------+
                            |  http://api:5080
                     +-------------+
                     |     api     |  (dominio, EF Core)         replicas: 2
                     +-------------+
                            |  Server=mysql-primary;Port=3306
          +-----------------+------------------+
          |                                    | (replicacion binlog)
   +----------------+                   +----------------+
   | mysql-primary  |  ===== binlog ==> | mysql-replica  |
   | server-id=1 RW |                   | server-id=2 RO |
   +----------------+                   +----------------+
          ^
          | (una sola vez) dotnet ef database update
   +----------------+
   |   migrator     |  servicio replicas: 0, se dispara aparte
   +----------------+

   Red overlay interna: clinicacore_clinicacore-net (internal: true)
```

### Servicios (del `infra/stack.yml`, stack `clinicacore`)

| Servicio        | Imagen                      | Replicas | Puerto           | Rol |
|-----------------|-----------------------------|----------|------------------|-----|
| `nginx`         | `clinicacore-web:local`     | 2        | **8080 -> 80**   | Punto de entrada unico: sirve el SPA (fallback `index.html` para React Router) y hace `proxy_pass` de `/api` al gateway. |
| `gateway`       | `clinicacore-gateway:local` | 2        | 5000 (interno)   | BFF. Reenvia `/api/*` a `api:5080` propagando `Authorization`. El nombre DEBE ser `gateway` (nginx lo resuelve por DNS). |
| `api`           | `clinicacore-api:local`     | 2        | 5080 (interno)   | Servicio de dominio (EF Core). Habla con `mysql-primary`. |
| `mysql-primary` | `mysql:8.0`                 | 1        | 3306 (interno)   | Lectura/escritura (`read_only=OFF`, `server-id=1`, `log_bin`). |
| `mysql-replica` | `mysql:8.0`                 | 1        | 3306 (interno)   | Solo lectura (`read_only=ON`, `server-id=2`), sigue al primario por binlog. |
| `migrator`      | `clinicacore-migrator:local`| 0        | n/a              | Tarea de un solo uso: aplica las migraciones EF. Se dispara con `04-migrar.sh`. |

Unico puerto publicado al host: **8080** (nginx). Todo lo demas vive en la red overlay
interna `clinicacore_clinicacore-net` (`internal: true`), sin exposicion directa.

### Secrets de Docker (nunca en el repo)

Se crean con `infra/scripts/01-crear-secrets.sh` y se declaran `external: true` en el
stack. No se versiona ningun valor:

- `mysql_root_password`, `mysql_app_password`, `mysql_repl_password`, `jwt_secret`.

El entrypoint comun de las imagenes .NET (`backend/docker-entrypoint.sh`) traduce los
secrets a configuracion `CLINICACORE_` (p. ej. compone `ConnectionStrings:ClinicaCore`
con la password de la BD, y exporta `Jwt:Secret`), de modo que las credenciales no
aparecen en `stack.yml` ni en el historial de la imagen.

## Pasos de despliegue desde cero

Todos los scripts son idempotentes y viven en `infra/scripts/` (`set -euo pipefail`).
El orden canonico es 00 -> 05; 06/07 son respaldo/restauracion.

```bash
# 0) Swarm activo (un nodo). Si ya esta activo, no hace nada.
bash infra/scripts/00-init-swarm.sh      # o: docker swarm init

# 1) Secrets (se generan si no se exportan MYSQL_*/JWT_SECRET; nunca se escriben a disco)
bash infra/scripts/01-crear-secrets.sh

# 2) Imagenes (4 imagenes con tag :local, el que usa el stack)
bash infra/scripts/02-build-imagenes.sh

# 3) Deploy del stack
bash infra/scripts/03-deploy.sh          # docker stack deploy -c infra/stack.yml clinicacore

# 4) Migraciones EF (tarea efimera en la red overlay) + siembra de roles de la Api
bash infra/scripts/04-migrar.sh

# 5) Verificacion de salud (espera replicas running + /healthz del nginx)
bash infra/scripts/05-verificar-salud.sh
```

Nota sobre el orden: la Api siembra el catalogo de roles al arrancar, pero en el stack
puede arrancar antes de que existan las tablas. La siembra es **tolerante** (no tumba el
proceso; ver `backend/api/Datos/RolSeeder.cs`), y `04-migrar.sh` fuerza un reinicio suave
de la Api tras aplicar el esquema para que siembre los roles contra la BD ya migrada.

## Verificacion end-to-end (evidencia real)

Servidor: Docker **28.5.1**, Swarm de un solo nodo (`docker-desktop`). Lo que sigue es
el resultado real de un despliegue **desde cero** (volumenes de datos borrados antes).

### 1) Servicios con todas las replicas running

```
$ docker stack services clinicacore
NAME                        MODE         REPLICAS   IMAGE                        PORTS
clinicacore_api             replicated   2/2        clinicacore-api:local
clinicacore_gateway         replicated   2/2        clinicacore-gateway:local
clinicacore_migrator        replicated   0/0        clinicacore-migrator:local
clinicacore_mysql-primary   replicated   1/1        mysql:8.0
clinicacore_mysql-replica   replicated   1/1        mysql:8.0
clinicacore_nginx           replicated   2/2        clinicacore-web:local        *:8080->80/tcp
```

### 2) Migracion aplicada y tablas en el primario

```
$ bash infra/scripts/04-migrar.sh
Applying migration '20261107040344_InicialEsquema'.
Applying migration '20261107065339_AgregarBorradoLogicoPaciente'.
Done.
Migraciones aplicadas.

$ docker exec <mysql-primary> mysql ... clinicacore -e 'SHOW TABLES;'
__EFMigrationsHistory
asociado_seguro
cita
diagnostico
estudio
facturacion
facturacion_detalle
historial_clinico
insumo
kardex
medico
paciente
proveedor
receta
receta_detalle
resultado_tratante
rol
tipo_cobertura
usuario
```

Incluye `paciente`, `diagnostico`, `resultado_tratante` y `__EFMigrationsHistory`.

### 3) Replicacion MySQL

El esquema (y luego los datos del E2E) se replican al nodo `mysql-replica`.

```
$ docker exec <mysql-replica> mysql ... -e 'SHOW REPLICA STATUS\G'
           Replica_IO_Running: Yes
          Replica_SQL_Running: Yes
        Seconds_Behind_Source: 0
                Last_IO_Error:
               Last_SQL_Error:
    Replica_SQL_Running_State: Replica has read all relay log; waiting for more updates

$ docker exec <mysql-replica> mysql ... -e 'SELECT @@read_only AS ro, @@server_id AS sid;'
ro  sid
1   2
```

Demostracion del flujo primario -> replica (dato insertado en el primario via E2E,
leido en la replica de solo lectura):

```
$ docker exec <mysql-replica> mysql ... clinicacore -e 'SELECT * FROM paciente\G'
       id_paciente: 1
            nombre: Ana
          apellido: Lopez
               cui: CUI1794064204

$ docker exec <mysql-replica> mysql ... clinicacore \
    -e 'SELECT COUNT(*) pac, ... FROM ...'
pac  dia  res  rol
1    1    1    4
```

La replica es `read_only=ON` (`server-id=2`); solo el hilo SQL de replicacion escribe.

### 4) E2E por el punto de entrada nginx

El flujo se ejecuta **por nginx** (mismo nginx que publica el 8080). Por la ocupacion
del puerto 8080 del host (ver [nota de puerto](#nota-el-puerto-8080-del-host)), la
evidencia se tomo desde un cliente curl adjunto a la red overlay apuntando a
`http://nginx/`, que atraviesa exactamente nginx -> gateway -> api. Secuencia:
`register(Admin) -> login(JWT) -> paciente -> medico -> historial -> cita ->
diagnostico(crear+listar) -> resultado tratante(crear+listar)`.

```
### 1) REGISTER (Admin) -> POST http://nginx/api/auth/register
HTTP/1.1 201 Created
{"idUsuario":1,"correo":"admin...@clinicacore.test","rol":"Admin","idMedico":null}

### 2) LOGIN -> POST http://nginx/api/auth/login
{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...."}   # JWT

### 3) PACIENTE   -> {"idPaciente":1,...}
### 4) MEDICO     -> {"idMedico":1,...}
### 5) HISTORIAL  -> {"idHistorial":1,"idPaciente":1,...}
### 6) CITA       -> {"idCita":1,"idMedico":1,"idPaciente":1,...}

### 7) DIAGNOSTICO crear (usa idHistorial+idCita)
{"idDiagnosticos":1,"idHistorial":1,"idCita":1,"codigoCie10":"I10","alergias":"Ninguna",...}
### 7b) listar -> [{"idDiagnosticos":1,...}]

### 8) RESULTADO TRATANTE crear (usa idMedico)
{"idResultado":1,"idMedico":1,"idDiagnostico":1,"conclusion":"Estable",...}
### 8b) listar -> [{"idResultado":1,...}]
```

El login devuelve un JWT valido (`iss=clinicacore-api`, `aud=clinicacore-clientes`),
que el gateway propaga a la Api con `Authorization: Bearer`. Se crean y listan al menos
un diagnostico y un resultado tratante, cumpliendo el criterio de aceptacion.

### 5) Tolerancia a fallos a nivel de contenedor

Se mata un contenedor de una replica de `api` y Swarm la reemplaza automaticamente,
volviendo a `2/2`:

```
$ docker kill <contenedor clinicacore_api>
$ docker service ps clinicacore_api
clinicacore_api.1   Running 19 seconds ago
clinicacore_api.1   Failed  30 seconds ago   "task: non-zero exit (137)"   # SIGKILL
clinicacore_api.1   Shutdown about a minute ago
clinicacore_api.2   Running 2 minutes ago
$ docker service ls --filter name=clinicacore_api
clinicacore_api 2/2
```

`restart_policy: condition: any` en el stack garantiza el reinicio. El mismo mecanismo
cubre a `nginx`, `gateway`, `api`, `mysql-primary` y `mysql-replica`.

## Respaldo y restauracion

Respaldo logico del primario con `mysqldump` (volcado a `./backups/`, ignorado por git;
la root password se lee del secret dentro del contenedor, nunca en disco):

```bash
bash infra/scripts/06-backup-mysql.sh                 # genera backups/clinicacore-<sello>.sql
bash infra/scripts/07-restore-mysql.sh backups/clinicacore-<sello>.sql   # restaura en el primario
```

La replica se pone al dia por replicacion normal tras una restauracion en el primario;
no se restaura directamente sobre ella.

## Limpieza del stack

```bash
docker stack rm clinicacore
# (opcional) borrar los volumenes de datos para un arranque totalmente limpio:
#   docker volume rm clinicacore_mysql-primary-data clinicacore_mysql-replica-data
```

Dejar el stack limpio al terminar la demostracion es parte del procedimiento.

## Limitacion de un solo nodo

Solo hay **una maquina**. En consecuencia:

- La replica MySQL corre en el mismo host que el primario: aporta una copia caliente de
  los datos y permite lecturas de solo lectura, pero **no** sobrevive a la caida del
  equipo completo (ambos comparten hardware). No es alta disponibilidad geografica.
- La "alta disponibilidad" demostrada es **a nivel de contenedor**: Swarm reinicia
  contenedores caidos y mantiene las replicas deseadas de nginx/gateway/api. Si el unico
  nodo muere, el servicio cae.
- La promocion de la replica a primario es **manual** y esta documentada en
  `infra/mysql/PROMOCION.md` (`STOP REPLICA; RESET REPLICA ALL; SET GLOBAL read_only=OFF`
  y repunte de la cadena de conexion de la Api). No hay failover automatico.
- Se descartaron explicitamente InnoDB Cluster y configuraciones multinodo por la
  restriccion de un solo nodo.

### Nota: el puerto 8080 del host

Durante la verificacion, el puerto 8080 del host estaba ocupado por un contenedor ajeno
al proyecto (`taskmanager-api`), por lo que `curl http://localhost:8080/healthz` responde
el 404 de ese servicio, no el de nuestro nginx. No se toco ese contenedor (no es del
proyecto). El punto de entrada nginx de ClinicaCore se verifico igualmente **por nginx**
desde la red overlay (`http://nginx/`), que es el mismo servicio que el stack publica en
8080. En un host con el 8080 libre, `http://localhost:8080` sirve el SPA y hace proxy de
`/api` sin cambios.

## Defectos reales encontrados durante el despliegue y correcciones minimas

El despliegue real revelo defectos que impedian la verificacion. Se corrigio lo minimo,
manteniendo verdes `dotnet build`/`dotnet test` (backend) y `npm run build`/`npm run lint`
(frontend). Resumen:

1. **Healthcheck de Api/Gateway con `wget` inexistente.** La imagen `aspnet:10.0`
   (Debian) no trae `wget` ni `curl`, asi que el healthcheck fallaba siempre
   (`/bin/sh: wget: not found`) y Swarm mataba las tareas en bucle. Correccion: instalar
   `curl` en `backend/Dockerfile.api` y `backend/Dockerfile.gateway` y usar `curl -fsS`
   en los healthchecks del `stack.yml`.

2. **Arranque de la Api fragil ante la BD no lista.** `RolSeeder` consultaba la BD al
   arrancar y, ante un fallo transitorio (primario aun levantando o esquema sin migrar),
   lanzaba y tumbaba el proceso antes de exponer `/health`. Correccion: `EnableRetryOnFailure`
   en el `DbContext` y una siembra tolerante con reintentos (`SembrarConReintentosAsync`)
   que no tumba la Api; `04-migrar.sh` reinicia la Api tras migrar para sembrar los roles.

3. **Healthcheck de nginx con `localhost` (IPv6).** En Alpine `localhost` resuelve antes
   a `::1` y nginx solo escucha en IPv4, de modo que el healthcheck daba
   `connection refused`. Correccion: usar `127.0.0.1` en el healthcheck de nginx.

4. **Init del primario dependiente de `envsubst` y de passwords en claro.**
   `00-aplicar-plantilla.sh` usaba `envsubst` (ausente en `mysql:8.0`) y variables
   `MYSQL_*` en claro, pero el stack entrega las credenciales como `*_FILE` (secrets).
   El usuario de replicacion `repl` nunca se creaba. Correccion: leer los secrets desde
   `*_FILE` y sustituir la plantilla con `sed` (sin dependencias).

5. **Password de replicacion demasiado larga.** `mysql_repl_password` se generaba con
   `openssl rand -hex 24` (48 chars) y `CHANGE REPLICATION SOURCE ... SOURCE_PASSWORD`
   tiene un limite de 32 (error 3056), lo que abortaba la configuracion de la replica.
   Correccion: generar la password de `repl` con `openssl rand -hex 15` (30 chars).

6. **Usuario `repl` sin `REPLICATION CLIENT`.** La replica lee las coordenadas del
   primario con `SHOW MASTER STATUS` durante su init, que exige `REPLICATION CLIENT`;
   con solo `REPLICATION SLAVE` fallaba con `ERROR 1227` y la replica no se configuraba.
   Correccion: `GRANT REPLICATION SLAVE, REPLICATION CLIENT` al usuario `repl`.

7. **La BD `clinicacore` no existia en la replica.** El `CREATE DATABASE` del init del
   primario no viaja por el binlog, asi que la primera DDL replicada (el `CREATE TABLE`
   del migrator con default database `clinicacore`) fallaba con
   `Unknown database 'clinicacore'` y detenia el hilo SQL. Correccion: la replica crea
   `clinicacore` localmente (`CREATE DATABASE IF NOT EXISTS`) antes de arrancar la
   replicacion.

Con estas correcciones, un despliegue desde cero (`01` -> `05`) deja el stack con todas
las replicas running, el esquema migrado y replicado, la replicacion activa
(`Replica_IO_Running=Yes`, `Replica_SQL_Running=Yes`), el E2E completo por nginx y la
tolerancia a fallos a nivel de contenedor funcionando.
