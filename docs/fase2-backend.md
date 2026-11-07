# Decisiones tecnicas — Fase 2 (backend de microservicios)

La fase 2 agrega la capa ejecutable de backend sobre la capa de datos de la fase 1
([`docs/decisiones.md`](decisiones.md)). Se entregan dos servicios ASP.NET Core Web API
(net10.0): un servicio de dominio (`ClinicaCore.Api`) y una puerta de entrada / BFF
(`ClinicaCore.Gateway`). Fuente de verdad del modelo y del alcance:
[`docs/er/modelo-er.md`](er/modelo-er.md) y [`Requirements.md`](../Requirements.md).

Esta fase no crea Docker, Swarm, nginx ni frontend; eso queda para fases posteriores.
Build y pruebas se corren con el proveedor InMemory, sin necesidad de un MySQL activo.

## 1. Particion final de servicios y justificacion

| Servicio | Proyecto | Rol |
|---|---|---|
| Servicio de dominio | `backend/api/` (`ClinicaCore.Api`) | Expone la logica de negocio y los datos: auth, diagnosticos, resultados tratantes y el resto del dominio clinico/facturacion/inventario. Es el unico que abre conexion a MySQL y valida el JWT por rol. |
| Gateway / BFF | `backend/gateway/` (`ClinicaCore.Gateway`) | Puerta de entrada unica para los clientes. Reenvia `/api/*` al servicio de dominio propagando el header `Authorization`. Deja `/api/auth/*` anonimo y exige JWT valido para el resto. |

**Por que solo dos servicios.** El entregable corre sobre un unico nodo y la consigna pide
mantener la parte simple (descartar lo que exija dos nodos o complejidad innecesaria). Un
servicio de dominio unico sobre un solo `ClinicaCoreDbContext` y una sola base MySQL evita
transacciones distribuidas y bases fragmentadas, que no aportarian valor con un solo nodo.
La particion por dominio (auth / clinical / billing / inventory) se mantiene como division
**logica** en carpetas y controladores, no como procesos separados. El Gateway da el punto
de entrada unico (BFF) que luego podra ubicarse detras de nginx sin cambiar los clientes.

## 2. Diseno de autenticacion y autorizacion

- **Roles.** Catalogo base sembrado al arrancar la Api (`RolSeeder`): `Admin`, `Medico`,
  `Recepcion`, `Lectura`. Se insertan solo si la tabla `rol` esta vacia.
- **Permisos sobre los flujos prioritarios** (diagnosticos y resultados tratantes):
  - Lectura (`GET` listar/obtener): cualquier usuario autenticado.
  - Escritura (`POST`/`PUT`): roles `Admin` y `Medico`.
  - Borrado (`DELETE`): solo `Admin`.
- **Hash de contrasena.** `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>` para
  almacenar y verificar `Usuario.PasswordHash`. No se guardan contrasenas en claro.
- **JWT.** Firmado con HS256 (clave simetrica `Jwt:Secret`). Claims emitidos: `sub` (id de
  usuario), `email` (correo), `role` (nombre del rol) y, si el usuario esta enlazado a un
  medico, `medicoId`. Validacion con issuer, audience, firma y expiracion verificadas y
  `ClockSkew = 0` (sin margen de reloj).
- **Vida del token.** Access token con expiracion `Jwt:AccessTokenMinutes` (por defecto
  60 minutos). **No hay refresh token en esta fase**: cuando expira se vuelve a iniciar sesion.
- **Login.** `POST /api/auth/login` con correo y contrasena devuelve `{ "token": "..." }`.
  El login solo tiene exito para usuarios con `activo = true`.
- **Registro.** `POST /api/auth/register` solo es anonimo mientras la tabla `usuario` esta
  vacia, y en ese caso exige rol `Admin` (arranque del sistema). Con usuarios existentes,
  sin token responde `401` y con un rol distinto de `Admin` responde `403`. El Gateway deja
  `/api/auth/*` sin exigir token pero reenvia el `Authorization`, asi que la Api decide.
- **Gestion de usuarios.** `GET /api/usuarios` y `PUT /api/usuarios/{id}` (rol, `activo`,
  `idMedico`) son solo para `Admin`. Un Admin no puede quitarse su propio rol ni desactivar
  su cuenta (`400`).

El Gateway valida el JWT con los **mismos** parametros (issuer/audience/secret) que la Api,
por lo que ambos servicios deben compartir el valor de `Jwt:Secret`.

## 3. Semantica de borrado

- **Borrado logico de paciente.** La entidad `Paciente` tiene una columna `activo` y un
  filtro global de consulta: los pacientes inactivos quedan fuera de las consultas normales
  sin eliminarse fisicamente. La migracion aditiva `AgregarBorradoLogicoPaciente` introduce
  la columna sin recrear el esquema.
- **FKs clinicas en `Restrict`.** Las relaciones obligatorias y de catalogo usan `Restrict`,
  de modo que no se borra en cascada informacion clinica. Los detalles de receta y factura
  usan `Cascade`; las FKs opcionales usan `SetNull` (ver fase 1). No hay cascada fisica sobre
  los datos clinicos.
- **Borrado de diagnosticos y resultados tratantes.** El `DELETE` de estos recursos es un
  borrado fisico del registro propio (reservado a `Admin`); no arrastra en cascada entidades
  padre por la regla `Restrict`.

## 4. Variables de entorno por servicio

La configuracion se lee de `appsettings*.json`, user-secrets o variables de entorno. Para
variables de entorno el prefijo es **`CLINICACORE_`** y el separador de seccion es **`__`**
(doble guion bajo). No se hardcodean secretos; los `appsettings` versionados traen
`Jwt:Secret` vacio y una cadena de conexion con placeholder.

### Servicio de dominio (`ClinicaCore.Api`)

| Clave de configuracion | Variable de entorno | Descripcion |
|---|---|---|
| `ConnectionStrings:ClinicaCore` | `CLINICACORE_ConnectionStrings__ClinicaCore` | Cadena de conexion a MySQL 8. |
| `Jwt:Secret` | `CLINICACORE_Jwt__Secret` | Clave HS256 para firmar/validar (obligatoria). |
| `Jwt:Issuer` | `CLINICACORE_Jwt__Issuer` | Emisor del token (`clinicacore-api`). |
| `Jwt:Audience` | `CLINICACORE_Jwt__Audience` | Audiencia del token (`clinicacore-clientes`). |
| `Jwt:AccessTokenMinutes` | `CLINICACORE_Jwt__AccessTokenMinutes` | Vida del access token en minutos (60). |

### Gateway / BFF (`ClinicaCore.Gateway`)

| Clave de configuracion | Variable de entorno | Descripcion |
|---|---|---|
| `Jwt:Secret` | `CLINICACORE_Jwt__Secret` | Debe coincidir con la de la Api. |
| `Jwt:Issuer` | `CLINICACORE_Jwt__Issuer` | Igual que en la Api. |
| `Jwt:Audience` | `CLINICACORE_Jwt__Audience` | Igual que en la Api. |
| `Services:ApiBaseUrl` | `CLINICACORE_Services__ApiBaseUrl` | URL base del servicio de dominio (`http://localhost:5080`). |

## 5. Como ejecutar en local (sin Docker)

Requisitos: SDK .NET 10 instalado y una base MySQL 8 accesible para la Api (la cadena de
conexion apunta a ella). Para probar solo el arranque y la logica sin MySQL, usar las
pruebas (`dotnet test`), que corren sobre InMemory.

1. **Proveer el secreto JWT** (no versionarlo). Por user-secrets o variables de entorno.
   El mismo valor debe darse a la Api y al Gateway.

   ```bash
   # user-secrets (por proyecto)
   cd backend/api     && dotnet user-secrets set "Jwt:Secret" "UN_SECRETO_LARGO_Y_ALEATORIO"
   cd backend/gateway && dotnet user-secrets set "Jwt:Secret" "UN_SECRETO_LARGO_Y_ALEATORIO"

   # o por variable de entorno (prefijo CLINICACORE_, separador __)
   export CLINICACORE_Jwt__Secret="UN_SECRETO_LARGO_Y_ALEATORIO"
   export CLINICACORE_ConnectionStrings__ClinicaCore="Server=localhost;Port=3306;Database=clinicacore;User=clinicacore;Password=TU_PASSWORD;"
   ```

2. **Arrancar primero la Api** (el Gateway depende de ella) en el puerto 5080, que es el que
   espera `Services:ApiBaseUrl`:

   ```bash
   cd backend
   dotnet run --project api --urls http://localhost:5080
   ```

3. **Arrancar luego el Gateway** en el puerto 5000:

   ```bash
   cd backend
   dotnet run --project gateway --urls http://localhost:5000
   ```

4. **Probar los flujos** con los archivos `.http` versionados:
   [`backend/api/ClinicaCore.Api.http`](../backend/api/ClinicaCore.Api.http) (directo a la
   Api) y [`backend/gateway/ClinicaCore.Gateway.http`](../backend/gateway/ClinicaCore.Gateway.http)
   (a traves del Gateway). Hacer login, copiar el `token` a la variable del archivo y usarlo
   como `Authorization: Bearer` en diagnosticos y resultados tratantes. En desarrollo tambien
   esta disponible Swagger en cada servicio.

## 6. Verificacion de la fase

| Comando (desde `backend/`) | Resultado esperado |
|---|---|
| `dotnet tool restore` | Restaura `dotnet-ef 9.0.11`. |
| `dotnet build` | Compilacion correcta, 0 advertencias y 0 errores (`TreatWarningsAsErrors=true`). |
| `dotnet test` | Todas las pruebas superadas (las 12 del modelo de fase 1; las de auth, diagnosticos y resultados tratantes de la Api; la unitaria de `DbErrorTraductor`; y las del reenvio del Gateway). Dos pruebas de integracion contra MySQL quedan omitidas (`[Skip]`) por no haber MySQL en el entorno; ver seccion 7. |

## 7. Cierre de los hallazgos no bloqueantes de la revision

La revision de fase 2 dejo dos hallazgos no bloqueantes; ambos quedan cerrados asi:

### 7.1 Reenvio de cuerpos chunked en el Gateway

El reenvio del Gateway (`ClinicaCoreApiClient.ReenviarAsync`) decidia adjuntar el cuerpo de
la peticion solo cuando habia `Content-Length > 0` o un header `Transfer-Encoding`. Una
peticion con cuerpo chunked llega sin `Content-Length` y, segun el cliente, sin que ese
header aparezca en `HttpRequest.Headers`, por lo que el cuerpo se omitia al reenviar.

Ahora el reenvio decide por metodo: si el `Content-Length` es desconocido (nulo) y el metodo
admite cuerpo (`POST`/`PUT`/`PATCH`, no `GET`/`HEAD`/`DELETE`/`OPTIONS`/`TRACE`/`CONNECT`), el
cuerpo se reenvia igualmente. Se preserva el comportamiento previo para peticiones con
`Content-Length` y la propagacion del header `Authorization`.

Cubierto por `backend/tests-gateway/ReenvioTests.cs`: el reenvio de cuerpo sin
`Content-Length` (falla sin el arreglo), el reenvio con `Content-Length`, que un `GET` no
adjunta cuerpo, y la propagacion de `Authorization`.

### 7.2 Cobertura del camino 409/400 sobre MySQL real

`DbErrorTraductor` traduce las violaciones de restriccion que afloran como
`DbUpdateException` al guardar en MySQL (indice unico de `cui`/`colegiado` -> 409; CHECK
`ck_paciente_genero` -> 400). El proveedor InMemory de las pruebas no dispara esas
restricciones, por lo que ese camino no tenia cobertura automatizada.

- **Verificado sin MySQL:** `backend/tests-api/DbErrorTraductorTests.cs` ejercita el
  traductor de forma directa, envolviendo en `DbUpdateException` un mensaje representativo
  del que MySQL produce para cada restriccion, y confirma el 409/400 (y `null` para errores
  desconocidos). Nota de fidelidad: `MySqlConnector` no expone un constructor publico de
  `MySqlException`, asi que no es viable construir aqui una instancia real con sus numeros de
  error (1062 duplicado, 3819 CHECK); el traductor solo inspecciona el texto del mensaje, que
  es lo que estas pruebas reproducen.
- **Pendiente de infraestructura (no verificado):** la prueba de que una `MySqlException`
  real dispara el 409/400 queda escrita y **omitida** en
  `backend/tests-api/ConflictoMySqlIntegrationTests.cs` (`[Fact(Skip = ...)]`), porque en
  este entorno el daemon de Docker esta apagado y no hay MySQL. El encabezado de esa clase
  explica como habilitarla: levantar MySQL 8, aplicar migraciones, exportar
  `CLINICACORE_TEST_MYSQL` con la cadena de conexion y quitar el `Skip`. No se ha ejecutado
  contra MySQL real todavia.
