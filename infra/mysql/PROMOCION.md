# Promocion manual de la replica MySQL

Este documento describe como promover la replica (`mysql-replica`) a primario cuando el
primario (`mysql-primary`) deja de estar disponible. La promocion es **manual y deliberada**:
no hay failover automatico.

## Limitacion de un solo nodo (leer primero)

El stack corre sobre **una sola maquina** (un unico nodo de Docker Swarm). Primario y
replica comparten el mismo equipo. Por lo tanto:

- La replica aporta **redundancia de datos** (una segunda copia al dia) y permite lecturas
  de respaldo, pero **no** aporta alta disponibilidad frente a la caida del equipo: si la
  maquina cae, caen ambos.
- La alta disponibilidad que este proyecto demuestra es **a nivel de contenedor** (reinicio
  automatico y replicas de los servicios sin estado), no a nivel de maquina.
- La promocion de aqui cubre el caso de **perdida del contenedor/datadir del primario**
  estando el equipo sano, o un mantenimiento planificado del primario.

No se usa InnoDB Cluster ni ninguna topologia multinodo (descartadas por diseño).

## Procedimiento de promocion

Supone que el contenedor del servicio `mysql-replica` esta sano. Se abre una sesion de root
dentro de la tarea de la replica:

```bash
# Localizar el contenedor de la replica y abrir el cliente mysql como root.
cid=$(docker ps --filter name=clinicacore_mysql-replica -q | head -n1)
docker exec -it "$cid" sh -lc 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD"'
```

Dentro del cliente MySQL:

```sql
-- 1. Detener la replicacion y olvidar la configuracion de origen.
STOP REPLICA;
RESET REPLICA ALL;

-- 2. Habilitar escrituras (la replica estaba en solo lectura).
SET GLOBAL read_only = OFF;
SET GLOBAL super_read_only = OFF;

-- 3. Verificar que ya no sigue a ningun origen.
SHOW REPLICA STATUS\G   -- debe salir vacio
```

A partir de este punto, `mysql-replica` es un primario autonomo de lectura/escritura.

## Repunte de la cadena de conexion de la aplicacion

La Api y el migrator apuntan a `mysql-primary` mediante la cadena de conexion inyectada por
variable de entorno (`CLINICACORE_ConnectionStrings__ClinicaCore`, con la password leida del
secret `mysql_app_password`). Para que la aplicacion use el nuevo primario:

- **Opcion A (recomendada, sin cambiar el nombre de servicio):** hacer que el servicio
  `api` resuelva el nuevo primario cambiando el host de la cadena a `mysql-replica` y
  redesplegando solo ese servicio:

  ```bash
  docker service update \
    --env-add 'CLINICACORE_ConnectionStrings__ClinicaCore=Server=mysql-replica;Port=3306;Database=clinicacore;User=clinicacore;' \
    clinicacore_api
  ```

  (La password sigue llegando por el secret; el entrypoint la vuelve a componer.)

- **Opcion B:** editar `infra/stack.yml` dejando el host de la cadena del servicio `api`
  en `mysql-replica` y volver a desplegar el stack (`bash infra/scripts/03-deploy.sh`).

Una vez estable el nuevo primario, reconstruir una replica nueva (datadir vacio) apuntandola
a el con el mismo procedimiento de `infra/mysql/replica/init/01-configurar-replica.sh`,
ajustando `MYSQL_PRIMARY_HOST` al nuevo primario.
