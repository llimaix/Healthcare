# Plan de implementacion — Fase 4 (despliegue en Docker Swarm)

Proyecto ClinicaCore EMR. Se trabaja directo sobre `main` (commit base `7c1ed10`) con
commits atomicos y mensajes en espanol. Docker server 28.5.1 disponible.

Fuentes de verdad leidas: `Requirements.md` (§8), `docs/fase2-backend.md`,
`frontend/README.md`, `docs/er/modelo-er.md`, `docs/decisiones.md`, y el codigo real de
`backend/` y `frontend/`. Decisiones ya tomadas (no se reabren): .NET 10, EF Core 9.0.x +
Pomelo 9.0.0 + MySqlConnector 2.6.2, MySQL 8, replicacion SIMPLE primario-replica con
promocion manual, un solo nodo de Swarm, secrets de Docker, migrator de un solo uso.

## Hallazgos que fijan el diseno

- **nginx = punto de entrada unico.** Una sola imagen nginx (multi-stage: Node construye
  `frontend/dist`, runtime nginx sirve los estaticos y hace reverse-proxy de `/api` al
  Gateway). Esto colapsa los servicios `nginx` y `frontend` del §8.4 en uno solo, que es lo
  mas simple para un nodo y coincide con el modelo de produccion del `frontend/README.md`
  (frontend y `/api` comparten origen -> no hace falta CORS en el Gateway).
- **Gateway** (`backend/gateway`, net10.0) es el unico backend visible; reenvia `/api/*` a
  la Api propagando `Authorization`. `Services:ApiBaseUrl` apuntara a `http://api:5080` en
  la red overlay. Escucha en `:5000`.
- **Api** (`backend/api`, net10.0) es el unico que abre MySQL y valida JWT por rol. Siembra
  roles al arrancar (`RolSeeder`). Escucha en `:5080`. Config por variables de entorno con
  prefijo `CLINICACORE_` y separador `__`.
- **Migrator**: el proyecto `backend/database` tiene `IDesignTimeDbContextFactory` y
  `dotnet-ef 9.0.11` en `.config/dotnet-tools.json`. El migrator sera un contenedor de un
  solo uso sobre la imagen SDK de .NET 10 que corre
  `dotnet ef database update --connection "<cadena-al-primario>"`. El flag `--connection`
  sobreescribe la cadena placeholder del factory. Es el camino ya documentado en
  `docs/decisiones.md` §3; no requiere tocar codigo de la app. (Alternativa descartada:
  generar SQL idempotente y aplicarlo con el cliente mysql — anade un segundo toolchain sin
  aportar; el camino EF ya es reproducible.)
- **Replicacion**: binlog clasico primario->replica (usuario de replicacion +
  `CHANGE REPLICATION SOURCE TO`). MySQL 8 lo soporta; el prompt manda SIMPLE y promocion
  manual, asi que no se usa InnoDB Cluster ni GTID auto-posicionado complejo. Terminologia
  MySQL 8.0.22+: `SHOW REPLICA STATUS`, `START REPLICA`.
- **Secrets**: credenciales MySQL (root, usuario app, usuario replicacion) y `Jwt:Secret`
  como Docker secrets. La Api y el Gateway deben compartir el mismo `Jwt:Secret`. Las
  imagenes .NET leen el secreto de archivo con el patron `*_FILE` o un entrypoint que
  exporta `CLINICACORE_Jwt__Secret` desde `/run/secrets/...`.
- **E2E**: existen endpoints POST para paciente, medico, historial, cita, diagnostico y
  resultado tratante (todos via Gateway con Bearer). No hay seed de usuarios; se crea un
  Admin con `POST /api/auth/register`. La cadena completa de E2E es:
  register Admin -> login -> crear paciente -> crear medico -> crear historial -> crear cita
  -> crear diagnostico (necesita historial+cita) -> crear resultado tratante (necesita
  medico). Esto confirma que el E2E no exige cambios de codigo de la app.
- **Puertos**: Api `:5080`, Gateway `:5000` (de `docs/fase2-backend.md`). nginx publica
  `:8080->80` (o `:80`) en el host.
- **gitignore**: `bin/ obj/ node_modules/ dist/` ya ignorados. No se commiten secretos ni
  volumenes.

## Comandos reales de build y test (verificacion)

- Backend (desde `backend/`): `dotnet tool restore` && `dotnet build` (0 warnings,
  `TreatWarningsAsErrors=true`) && `dotnet test`.
- Frontend (desde `frontend/`): `npm install` && `npm run build` (genera `dist/`) &&
  `npm run lint`.
- Imagenes: `docker build` de cada Dockerfile sin error.
- Stack: `docker swarm init` -> scripts -> `docker stack deploy -c infra/stack.yml clinicacore`
  -> `docker stack services clinicacore` muestra replicas running.

---

# Implementation Plan

- [ ] 1. Dockerfiles de produccion de los tres servicios de backend/frontend y sus `.dockerignore`.
      Crear un Dockerfile multi-stage para el Gateway y otro para la Api (base
      `mcr.microsoft.com/dotnet/sdk:10.0` para build con `dotnet publish -c Release`, runtime
      `mcr.microsoft.com/dotnet/aspnet:10.0`), fijando el pin EF Core 9.0.x ya presente en los
      csproj (no cambiar versiones). Crear un Dockerfile del frontend+nginx multi-stage (build
      con `node:22` corriendo `npm ci` + `npm run build` con `VITE_GATEWAY_URL=/api` como
      ARG/ENV de build; runtime `nginx:1.27-alpine` sirviendo `/usr/share/nginx/html`). Anadir
      `.dockerignore` en `backend/` y `frontend/` que excluyan `bin/ obj/ node_modules/ dist/`.
      El contexto de build del backend debe incluir toda la solucion (Dockerfiles en la raiz
      del repo o con contexto `backend/`) para resolver el `ProjectReference` a `database`.
      Files: `backend/Dockerfile.gateway`, `backend/Dockerfile.api`,
      `frontend/Dockerfile`, `backend/.dockerignore`, `frontend/.dockerignore`
      Verify: `docker build -f backend/Dockerfile.api -t clinicacore-api:local backend` ,
      `docker build -f backend/Dockerfile.gateway -t clinicacore-gateway:local backend` y
      `docker build -f frontend/Dockerfile -t clinicacore-web:local frontend` terminan sin error;
      `docker image ls` muestra las tres imagenes.

- [ ] 2. Configuracion de nginx: servir estaticos y reverse-proxy de `/api` al Gateway.
      Crear `infra/nginx/nginx.conf` (o `default.conf`) que: sirva el SPA de React con
      fallback a `index.html` (rutas de React Router), haga `proxy_pass` de `/api/` a
      `http://gateway:5000`, propague `Authorization`, `Host` y cabeceras `X-Forwarded-*`, y
      exponga `/healthz` para el healthcheck. El Dockerfile del frontend (item 1) copia este
      archivo a `/etc/nginx/conf.d/`. Depende del item 1 (misma imagen).
      Files: `infra/nginx/default.conf`, (ajuste de copia en) `frontend/Dockerfile`
      Verify: reconstruir la imagen web (`docker build -f frontend/Dockerfile -t clinicacore-web:local frontend`)
      sin error; `docker run --rm -p 8080:80 clinicacore-web:local` y `curl -I http://localhost:8080/`
      devuelve 200 con `index.html` (el proxy `/api` se valida en el E2E del item 9).

- [ ] 3. MySQL 8 primario + replica: config, scripts de init, usuarios y promocion manual.
      Crear `infra/mysql/primary/my.cnf` (server-id=1, `log_bin`, `binlog_format=ROW`,
      `read_only=OFF`) y `infra/mysql/replica/my.cnf` (server-id=2, `read_only=ON`,
      `relay_log`). Crear scripts de init montados en `/docker-entrypoint-initdb.d/`: en el
      primario, crear la BD `clinicacore`, el usuario de aplicacion y el usuario de
      replicacion con `REPLICATION SLAVE`; en la replica, un script/entrypoint que ejecute
      `CHANGE REPLICATION SOURCE TO SOURCE_HOST='mysql-primary', SOURCE_USER=..., SOURCE_PASSWORD=..., SOURCE_AUTO_POSITION=0`
      con coordenadas iniciales (log 'mysql-bin.000001', pos 0 sobre instancia nueva) y
      `START REPLICA`. Las credenciales vienen de secrets (ver item 5), nunca en claro en el
      repo. Documentar el procedimiento de promocion manual en `infra/mysql/PROMOCION.md`
      (`STOP REPLICA; RESET REPLICA ALL; SET GLOBAL read_only=OFF;` y repunte de la app).
      Files: `infra/mysql/primary/my.cnf`, `infra/mysql/replica/my.cnf`,
      `infra/mysql/primary/init/01-app-y-replicacion.sql`,
      `infra/mysql/replica/init/01-configurar-replica.sh`, `infra/mysql/PROMOCION.md`
      Verify: se valida al desplegar (item 9): `SHOW REPLICA STATUS\G` en la replica muestra
      `Replica_IO_Running: Yes` y `Replica_SQL_Running: Yes`.

- [ ] 4. Tarea migrator de un solo uso.
      Crear `backend/Dockerfile.migrator` sobre `mcr.microsoft.com/dotnet/sdk:10.0` que haga
      `dotnet tool restore` y, como entrypoint, corra
      `dotnet ef database update --project database/ClinicaCore.Database.csproj --startup-project database/ClinicaCore.Database.csproj --connection "$CLINICACORE_ConnectionStrings__ClinicaCore"`.
      La cadena al primario y la password llegan por env/secret (item 5). Depende del item 1
      (base SDK) e item 3 (primario existe). Documentar como dispararlo y re-ejecutarlo
      (es idempotente: EF no re-aplica migraciones ya registradas en `__EFMigrationsHistory`).
      Files: `backend/Dockerfile.migrator`
      Verify: `docker build -f backend/Dockerfile.migrator -t clinicacore-migrator:local backend`
      sin error; el efecto (tablas creadas en el primario) se comprueba en el item 9.

- [ ] 5. `infra/stack.yml` (compose v3 para `docker stack deploy`).
      Definir servicios: `nginx` (publica `8080:80`, healthcheck a `/healthz`, replicas: 2),
      `gateway` (healthcheck a `/health`, replicas: 2, env `CLINICACORE_Services__ApiBaseUrl=http://api:5080`
      y `Jwt:*` via secret), `api` (healthcheck a `/health`, replicas: 2, env
      `CLINICACORE_ConnectionStrings__ClinicaCore` apuntando a `mysql-primary`, `Jwt:*` via
      secret), `mysql-primary` y `mysql-replica` (replicas: 1 cada uno, volumen propio,
      `placement.constraints` al nodo manager, `restart_policy`), y `migrator`
      (`deploy.mode: replicated`, `replicas: 0` o `restart_policy.condition: none` para que
      sea de un solo uso; se dispara aparte, ver item 6). Red overlay interna `clinicacore-net`.
      Secrets declarados: `mysql_root_password`, `mysql_app_password`, `mysql_repl_password`,
      `jwt_secret`. Cablear `VITE_GATEWAY_URL=/api` ya quedo en build (item 1), no en runtime.
      Depende de items 1-4 (nombres de imagen y servicios). Usar imagenes locales con tag fijo.
      Files: `infra/stack.yml`
      Verify: `docker stack config -c infra/stack.yml` (o `docker compose -f infra/stack.yml config`)
      valida la sintaxis sin error; despliegue real en item 9.

- [ ] 6. Scripts idempotentes de infraestructura y orden de ejecucion.
      Crear en `infra/scripts/` (bash, `set -euo pipefail`, idempotentes): `00-init-swarm.sh`
      (`docker swarm init` si no esta activo), `01-crear-secrets.sh` (crea los 4 secrets si no
      existen; passwords generadas o leidas de env, nunca commiteadas), `02-build-imagenes.sh`
      (construye las 4 imagenes de items 1-4 con tag fijo), `03-deploy.sh`
      (`docker stack deploy -c infra/stack.yml clinicacore`), `04-migrar.sh` (dispara el
      migrator de un solo uso, p. ej. `docker service create --name clinicacore-migrator ... --restart-condition none`
      o `docker run` en la red overlay, y espera su salida), `05-verificar-salud.sh`
      (`docker stack services` + espera a replicas running + curl a `/healthz`),
      `06-backup-mysql.sh` y `07-restore-mysql.sh` (mysqldump/restore contra el primario via
      `docker exec`). Documentar el orden 00->07 en un README de scripts.
      Files: `infra/scripts/00-init-swarm.sh`, `infra/scripts/01-crear-secrets.sh`,
      `infra/scripts/02-build-imagenes.sh`, `infra/scripts/03-deploy.sh`,
      `infra/scripts/04-migrar.sh`, `infra/scripts/05-verificar-salud.sh`,
      `infra/scripts/06-backup-mysql.sh`, `infra/scripts/07-restore-mysql.sh`,
      `infra/scripts/README.md`
      Verify: `bash -n` sobre cada script (sintaxis) y `shellcheck` si esta disponible; la
      ejecucion real end-to-end se hace en el item 9.

- [ ] 7. Cableado de entrypoints que leen secrets en las imagenes .NET.
      Anadir un entrypoint (script sh) en las imagenes Api, Gateway y migrator que exporte
      `CLINICACORE_Jwt__Secret` (y para Api/migrator la password MySQL dentro de la cadena de
      conexion) leyendo de `/run/secrets/*` antes de arrancar el proceso. Esto mantiene los
      secretos fuera de las variables de entorno del `stack.yml` en claro. Ajustar los tres
      Dockerfiles (items 1 y 4) para copiar y usar el entrypoint. Depende de items 1, 4 y 5.
      Files: `backend/docker-entrypoint.sh` (compartido o uno por imagen),
      (ajustes en) `backend/Dockerfile.api`, `backend/Dockerfile.gateway`, `backend/Dockerfile.migrator`
      Verify: reconstruir las imagenes sin error; el arranque correcto con secrets se
      comprueba en el item 9 (servicios running y login JWT funcional).

- [ ] 8. Documentacion de despliegue `docs/fase4-despliegue.md`.
      Escribir: arquitectura de contenedores y topologia (diagrama de servicios y red
      overlay), pasos exactos de despliegue desde cero (orden de scripts 00->05 + migrar),
      como verificar salud, como demostrar la replicacion MySQL (insertar en primario,
      recuperar en replica, `SHOW REPLICA STATUS`), como demostrar la tolerancia a fallos a
      nivel de contenedor (`docker service scale`/matar una replica y ver el reinicio), la
      limitacion explicita de un solo nodo (sin exagerar la HA), el procedimiento de
      respaldo/restauracion, y como dejar el stack limpio (`docker stack rm clinicacore`).
      Depende de items 1-7 (describe lo construido). Se refina con los resultados reales del
      item 9.
      Files: `docs/fase4-despliegue.md`
      Verify: revision de contenido contra los items implementados; no requiere comando.

- [ ] 9. Verificacion REAL con Docker y registro de evidencia.
      Ejecutar el flujo completo: `docker swarm init` -> scripts 01-05 -> migrar (04).
      Comprobar: (a) `docker stack services clinicacore` con replicas running; (b) tablas
      creadas en el primario (`SHOW TABLES` incluye `paciente`, `diagnostico`,
      `resultado_tratante`, `__EFMigrationsHistory`); (c) replicacion: insertar un dato en el
      primario y recuperarlo en la replica + `SHOW REPLICA STATUS` con IO/SQL en Yes; (d) E2E
      por nginx (`http://localhost:8080`): `POST /api/auth/register` (Admin) -> `POST /api/auth/login`
      (JWT) -> con Bearer crear paciente, medico, historial, cita; luego crear+listar un
      diagnostico y crear+listar un resultado tratante. Registrar comandos y resultados en
      `docs/fase4-despliegue.md` (seccion de evidencia). Dejar el stack en estado limpio
      documentado al final. Si el despliegue revela un defecto real de la app, corregir lo
      minimo, justificarlo y mantener verdes `dotnet build`/`dotnet test` y
      `npm run build`/`npm run lint`.
      Files: `docs/fase4-despliegue.md` (seccion de evidencia)
      Verify: todos los comandos (a)-(d) producen el resultado esperado y quedan registrados;
      backend y frontend siguen verdes si se toco codigo.

---

## Digest

Archivos NUEVOS que crea el plan (no hay bloques existentes que reescribir; la Fase 4 es
aditiva y no modifica codigo de fases anteriores salvo que el item 9 revele un defecto):

- `backend/Dockerfile.api` — multi-stage sdk:10.0 -> aspnet:10.0, `dotnet publish -c Release`
  de `api/ClinicaCore.Api.csproj` (contexto `backend/` para resolver el ProjectReference a
  `database`). Verifica: `docker build` OK.
- `backend/Dockerfile.gateway` — idem para `gateway/ClinicaCore.Gateway.csproj`. Verifica:
  `docker build` OK.
- `backend/Dockerfile.migrator` — sdk:10.0, `dotnet tool restore` + entrypoint
  `dotnet ef database update --connection ...`. Reutiliza el factory de
  `backend/database/ClinicaCoreDbContextFactory.cs` (el `--connection` sobreescribe el
  placeholder). Verifica: `docker build` OK; tablas creadas en el item 9.
- `backend/docker-entrypoint.sh` — exporta `CLINICACORE_Jwt__Secret` y la password MySQL
  desde `/run/secrets/*`. Verifica: arranque con secrets en el item 9.
- `backend/.dockerignore`, `frontend/.dockerignore` — excluyen `bin/ obj/ node_modules/ dist/`.
- `frontend/Dockerfile` — node:22 (`npm ci` + `npm run build`, `VITE_GATEWAY_URL=/api`) ->
  nginx:1.27-alpine sirviendo `dist/` + copia de `infra/nginx/default.conf`. Verifica:
  `docker build` OK, `curl /` 200.
- `infra/nginx/default.conf` — estaticos con fallback a `index.html`, `proxy_pass /api/ ->
  http://gateway:5000`, propaga `Authorization`, `/healthz`. Verifica: `curl` en item 2 y 9.
- `infra/mysql/primary/my.cnf`, `infra/mysql/replica/my.cnf` — server-id, log_bin/binlog_format,
  read_only. Verifica: `SHOW REPLICA STATUS` en item 9.
- `infra/mysql/primary/init/01-app-y-replicacion.sql`,
  `infra/mysql/replica/init/01-configurar-replica.sh` — BD, usuarios app/replicacion,
  `CHANGE REPLICATION SOURCE TO` + `START REPLICA`. Verifica: item 9.
- `infra/mysql/PROMOCION.md` — procedimiento de promocion manual.
- `infra/stack.yml` — servicios nginx/gateway/api/mysql-primary/mysql-replica/migrator, red
  overlay, 4 secrets, replicas y healthchecks. Verifica: `docker stack config` OK; deploy en
  item 9.
- `infra/scripts/00..07*.sh` + `infra/scripts/README.md` — init swarm, secrets, build, deploy,
  migrar, verificar, backup/restore. Verifica: `bash -n`/`shellcheck`; ejecucion en item 9.
- `docs/fase4-despliegue.md` — arquitectura, pasos, verificacion, replicacion, tolerancia a
  fallos, limitacion de un nodo, evidencia real del item 9.

Comandos de verificacion (descubiertos en la exploracion, son los reales del proyecto):
- Backend (desde `backend/`): `dotnet tool restore && dotnet build && dotnet test` — verdes hoy
  (12 pruebas de modelo + auth/diagnosticos/resultados/gateway; 2 de integracion MySQL con
  `[Skip]`). `TreatWarningsAsErrors=true`.
- Frontend (desde `frontend/`): `npm install && npm run build && npm run lint` — verdes hoy
  (sin script `test`).
- Infra: `docker build` por imagen; `docker swarm init` + scripts + `docker stack deploy` +
  `docker stack services`; `SHOW REPLICA STATUS`; E2E por nginx con Bearer.

No se commiten secretos reales ni volumenes de datos. No se toca el typo de `README.md` de la
raiz, `docs/review/` ni `.agents/`.
