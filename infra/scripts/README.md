# Scripts de infraestructura ClinicaCore

Scripts idempotentes para construir y operar el stack en Docker Swarm (un solo nodo).
Todos usan `set -euo pipefail`. Ejecutar desde la raiz del repo o indicando la ruta;
los scripts resuelven sus propias rutas relativas a `infra/`.

## Orden de ejecucion (00 -> 07)

| Script | Que hace |
| --- | --- |
| `00-init-swarm.sh` | Inicializa Docker Swarm si no esta activo. |
| `01-crear-secrets.sh` | Crea los 4 secrets (`mysql_root_password`, `mysql_app_password`, `mysql_repl_password`, `jwt_secret`) si no existen. Passwords de env o generadas; nunca se commitean. |
| `02-build-imagenes.sh` | Construye las 4 imagenes con tag fijo `:local` (web, gateway, api, migrator). |
| `03-deploy.sh` | `docker stack deploy -c infra/stack.yml clinicacore`. |
| `04-migrar.sh` | Dispara el migrator de un solo uso en la red overlay y espera su salida (aplica migraciones EF al primario). |
| `05-verificar-salud.sh` | Lista servicios, espera a que las replicas esten running y comprueba `/healthz` por nginx (`:8090`, override con `PUERTO_WEB`). |
| `06-backup-mysql.sh` | `mysqldump` del primario via `docker exec` a `./backups/` (ignorado por git). |
| `07-restore-mysql.sh <archivo.sql>` | Restaura un volcado en el primario via `docker exec`. |
| `08-cargar-demo.sh` | **Opcional.** Carga `infra/mysql/seed-demo.sql` (datos ficticios) en el primario para demostración. Idempotente; se replican solos a la réplica. |

## Flujo tipico de despliegue

```bash
bash infra/scripts/00-init-swarm.sh
bash infra/scripts/01-crear-secrets.sh
bash infra/scripts/02-build-imagenes.sh
bash infra/scripts/03-deploy.sh
bash infra/scripts/04-migrar.sh
bash infra/scripts/05-verificar-salud.sh
```

## Notas

- **Secrets:** se gestionan como Docker secrets. Para fijar passwords concretas,
  exportar `MYSQL_ROOT_PASSWORD`, `MYSQL_APP_PASSWORD`, `MYSQL_REPL_PASSWORD`, `JWT_SECRET`
  antes de `01-crear-secrets.sh`. Nunca se escriben al repo.
- **Un solo nodo:** la alta disponibilidad es a nivel de contenedor (replicas + reinicio).
  La promocion manual de la replica esta en `infra/mysql/PROMOCION.md`.
- **Migrator:** declarado con `replicas: 0` en el stack; `04-migrar.sh` lo corre aparte
  como contenedor efimero sobre la red overlay `attachable`.
- **Limpieza:** `docker stack rm clinicacore` elimina los servicios (los volumenes de
  datos persisten hasta `docker volume rm`).
