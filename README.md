# ClinicaCore — Healthcare (EMR)

Plataforma web de registros médicos electrónicos (EMR) para clínicas y hospitales, construida con **.NET 10**, **React** y **MySQL 8**, y desplegada en contenedores sobre **Docker Swarm** con base de datos replicada (primario–réplica).

El sistema administra pacientes, médicos, citas, historial clínico, diagnósticos, recetas, estudios, resultados tratantes, kardex, facturación, proveedores y cobertura de seguros. La autenticación es con **JWT** y autorización por roles.

## Stack

| Capa | Tecnología |
| --- | --- |
| Frontend | React 19 + TypeScript (Vite), servido como estáticos por nginx |
| Punto de entrada | nginx (sirve el SPA y hace proxy de `/api`) |
| Backend | ASP.NET Core en .NET 10: Gateway/BFF + Api de dominio |
| Datos | EF Core 9.0.x (Pomelo + MySqlConnector) sobre MySQL 8 |
| Orquestación | Docker Swarm (un solo nodo), `docker stack deploy` |

## Arquitectura (resumen)

```
host:8080 → nginx → gateway:5000 → api:5080 → mysql-primary
                                                     └── binlog ──→ mysql-replica (solo lectura)
```

- **nginx** es el único puerto publicado al host (`8080`). Todo lo demás vive en una red overlay interna.
- **gateway** es el único backend visible; propaga el `Authorization: Bearer` a la Api de dominio.
- **mysql-primary** acepta escrituras; **mysql-replica** las recibe por replicación y queda en solo lectura.
- **migrator** es una tarea de un solo uso que aplica las migraciones de EF Core al primario.

Diagrama y topología detallados en [`docs/fase4-despliegue.md`](docs/fase4-despliegue.md).

## Requisitos

- Docker Engine con Swarm (se inicializa solo con el paso 0). Verificado en Docker 28.5.1.
- Para desarrollo fuera de contenedores: .NET SDK 10, Node 20+ y npm.

## Cómo levantarlo (despliegue completo en contenedores)

Desde la raíz del repo, en orden. Los scripts son idempotentes.

```bash
bash infra/scripts/00-init-swarm.sh        # inicializa Swarm si no está activo
bash infra/scripts/01-crear-secrets.sh     # crea los 4 secrets (no se commitean)
bash infra/scripts/02-build-imagenes.sh    # construye las 4 imágenes :local
bash infra/scripts/03-deploy.sh            # docker stack deploy clinicacore
bash infra/scripts/04-migrar.sh            # aplica migraciones EF al primario
bash infra/scripts/05-verificar-salud.sh   # espera réplicas running y /healthz
```

### Dónde queda corriendo

- **Aplicación web:** http://localhost:8090 (el SPA de React servido por nginx).
- **API vía el gateway:** http://localhost:8090/api (p. ej. `POST /api/auth/login`).
- Los servicios `gateway` (5000), `api` (5080) y MySQL (3306) **no** se publican al host; solo son accesibles dentro de la red overlay.

> Nota: el stack publica nginx en el puerto `8090` del host (el `8080` suele estar ocupado por otro proyecto). Para cambiarlo, edita el mapeo de `nginx` en `infra/stack.yml` (`8090:80`) y vuelve a desplegar.

### Primer uso

El sistema arranca sin usuarios. Registra un administrador y haz login:

```bash
curl -X POST http://localhost:8090/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"correo":"admin@clinicacore.test","password":"Admin123!","rol":"Admin"}'

curl -X POST http://localhost:8090/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"correo":"admin@clinicacore.test","password":"Admin123!"}'
# → devuelve { "token": "<JWT>" }, úsalo como  Authorization: Bearer <JWT>
```

Hay ejemplos de peticiones listos en `backend/gateway/ClinicaCore.Gateway.http` y `backend/api/ClinicaCore.Api.http`.

### Limpieza

```bash
docker stack rm clinicacore
# para borrar también los datos:
# docker volume rm clinicacore_mysql-primary-data clinicacore_mysql-replica-data
```

## Desarrollo local sin contenedores

- **Backend:** ver [`docs/fase2-backend.md`](docs/fase2-backend.md) (cómo correr la Api en `:5080` y el Gateway en `:5000`, variables de entorno, secreto JWT).
- **Frontend:** ver [`frontend/README.md`](frontend/README.md) (Vite dev server, proxy `/api` hacia el Gateway, variable `VITE_GATEWAY_URL`).

## Estructura del repositorio

```
├── backend/          # Solución .NET 10: Api de dominio, Gateway/BFF, capa de datos (EF Core) y tests
├── frontend/         # App React + TypeScript (Vite)
├── infra/            # nginx, MySQL (primario/réplica), stack.yml de Swarm y scripts 00–07
├── docs/             # Modelo ER, decisiones y guías por fase
├── Requirements.md   # Requerimientos del proyecto
└── README.md
```

## Documentación

- [`Requirements.md`](Requirements.md) — alcance, requerimientos y criterios de aceptación.
- [`docs/er/modelo-er.md`](docs/er/modelo-er.md) — modelo entidad–relación (18 entidades).
- [`docs/decisiones.md`](docs/decisiones.md) — decisiones técnicas de la capa de datos.
- [`docs/fase2-backend.md`](docs/fase2-backend.md) — diseño del backend, auth JWT y roles.
- [`docs/fase4-despliegue.md`](docs/fase4-despliegue.md) — despliegue en Swarm, replicación y verificación end-to-end.
- [`infra/scripts/README.md`](infra/scripts/README.md) — detalle de cada script de infraestructura.
- [`infra/mysql/PROMOCION.md`](infra/mysql/PROMOCION.md) — promoción manual de la réplica.

## Alta disponibilidad y limitación de un solo nodo

La réplica de MySQL y las réplicas de nginx/gateway/api dan tolerancia **a nivel de contenedor**: Swarm reinicia contenedores caídos y mantiene el número de réplicas deseado. Como todo corre en **una sola máquina**, esto no protege contra la caída del equipo completo, y la promoción de la réplica a primario es **manual** (documentada en `infra/mysql/PROMOCION.md`). Se descartaron InnoDB Cluster y configuraciones multinodo por la restricción de un solo nodo.
