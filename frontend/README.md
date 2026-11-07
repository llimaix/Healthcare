# Frontend — ClinicaCore EMR

SPA en React + TypeScript (Vite 7) que consume el **Gateway** del backend
(`/api/*`). El frontend nunca habla directo con la Api de dominio: todo el trafico
pasa por el Gateway (`http://localhost:5000`), que reenvia a la Api propagando el
`Authorization: Bearer`.

## Requisitos

- Node 20.19+ / 22.12+ (minimo que pide Vite 7). El entorno de desarrollo usa
  **Node v26.8.2 / npm 11.19.1**: funciona, pero al instalar puede aparecer un aviso
  `EBADENGINE` de alguna dependencia transitiva porque Node 26 es una linea non-LTS por
  encima del rango declarado. Es un **aviso, no un error**, y no bloquea `install`,
  `build` ni `dev`.
- Las dependencias estan fijadas con versiones **exactas** (sin `^`/`~`) en
  `package.json`; usa `npm install` tal cual para reproducir el mismo arbol.

## Instalacion y uso

```bash
cd frontend
npm install        # instala dependencias (versiones exactas, ver package.json)
npm run dev        # dev server en http://localhost:5173, apunta al Gateway via proxy
npm run build      # build de produccion: tsc -b + vite build
npm run lint       # ESLint de la plantilla
npm run preview    # sirve localmente el build de produccion
```

> No existe script `npm run test`: esta fase no agrego pruebas de componente (eran
> opcionales y no se incorporaron). Si en una iteracion futura se agregan (Vitest +
> @testing-library/react), se fijaran sus versiones exactas y se documentara aqui.

### Como apunta el dev server al Gateway

`npm run dev` levanta Vite en `http://localhost:5173`. El cliente HTTP llama a rutas
relativas bajo `/api` y el **proxy del dev server de Vite** las reenvia al Gateway:

```
navegador (http://localhost:5173)  ->  /api/...  ->  (proxy Vite)  ->  http://localhost:5000  (Gateway)
```

Esto hace que el navegador vea siempre el **mismo origen** en desarrollo y evita el
problema de CORS (el Gateway no tiene CORS configurado) **sin tocar el backend**. La
regla vive en `vite.config.ts` (`server.proxy['/api']` -> `http://localhost:5000`,
`changeOrigin: true`).

## Variable de entorno

| Variable           | Default (dev) | Descripcion                                              |
| ------------------ | ------------- | -------------------------------------------------------- |
| `VITE_GATEWAY_URL` | `/api`        | URL base contra la que el cliente HTTP llama al Gateway. |

- **Desarrollo local:** `VITE_GATEWAY_URL=/api` (valor relativo, definido en
  `.env.development`). Combinado con el proxy de Vite, resuelve a
  `http://localhost:5000` sin cross-origin.
- **Como cambiarla:** copia `.env.example` a `.env.local` (o edita `.env.development`)
  y ajusta el valor. En produccion puede ser un origen absoluto (p. ej.
  `https://clinica.example.com/api`) cuando un proxy inverso sirva el frontend y enrute
  `/api`; esa integracion (nginx/contenedores) es **fase 4** y no forma parte de este
  frontend.

Para levantar el backend que consume este frontend —la **Api** de dominio en `:5080`,
el **Gateway** en `:5000` y **MySQL**— remitirse a
[`../docs/fase2-backend.md`](../docs/fase2-backend.md).

## Estado de pantallas

| Pantalla / area                        | Ruta                   | Estado                        |
| -------------------------------------- | ---------------------- | ----------------------------- |
| Login + flujo de autenticacion         | `/login`               | **Completa**                  |
| Diagnosticos (listar/crear/editar/borrar) | `/diagnosticos`     | **Completa**                  |
| Resultados tratantes (listar/crear/editar/borrar) | `/resultados-tratantes` | **Completa**         |
| Resto de las 18 entidades del EMR      | —                      | **Pendiente / fuera de alcance** |

- **COMPLETAS:** `Login`, `Diagnosticos` y `Resultados tratantes`. Las dos pantallas
  clinicas tienen acabado presentable (tablas con filtros, formularios de alta/edicion,
  borrado con confirmacion, estados de carga y mensajes de error) apto para capturas de
  evidencia.
  - **Diagnosticos:** tabla con filtro por paciente; al crear se elige **paciente** y de
    ahi se cargan su **historial** (`/api/historiales?idPaciente=`) y sus **citas**
    (`/api/citas?idPaciente=`) en desplegables, de modo que no se escriben ids a mano.
    En edicion no se piden historial/cita (el backend no los modifica).
  - **Resultados tratantes:** tabla con filtro por medico y por diagnostico; al crear se
    elige **medico** (requerido, via `/api/medicos`) y, opcionalmente, un diagnostico
    (`/api/diagnosticos`). En edicion el medico queda bloqueado (el backend no lo
    modifica). El estudio se captura como id numerico opcional (ver Supuestos).
- **PENDIENTE / stub:** el CRUD del resto de las entidades del EMR queda **fuera de
  alcance de la fase 3**. Las pantallas de apoyo (pacientes, historiales, citas,
  medicos, estudios) existen solo como **lectura** para poblar selectores, no como CRUD.
- **Fuera de alcance de la UI:** el registro/alta de usuarios (`/api/auth/register`); los
  usuarios se crean por backend/seed.

### Mapa de roles -> permisos reflejado en la UI

| Accion                                      | Roles permitidos         | Reflejo en la UI                                                         |
| ------------------------------------------- | ------------------------ | ----------------------------------------------------------------------- |
| Login / ver propio perfil                   | cualquier cuenta activa  | —                                                                       |
| Listar / ver diagnosticos y resultados      | cualquier autenticado    | tablas visibles para todos los roles (Admin, Medico, Recepcion, Lectura) |
| Crear / editar diagnosticos y resultados    | **Admin, Medico**        | botones `Nuevo` / `Editar` / `Guardar` solo si el rol puede escribir    |
| Borrar diagnosticos y resultados            | **solo Admin**           | boton `Eliminar` (y su columna) solo si el rol es Admin                  |

El rol se obtiene de `GET /api/auth/me` (campo `rol`) tras el login; no se decodifica el
JWT en el cliente. **La guarda de la UI es cosmetica, no la frontera de seguridad:** el
backend decide. Ante **403** se muestra "Permiso insuficiente" sin romper la pantalla;
ante **401** (token ausente o expirado) la app cierra sesion y redirige a `/login`. No
hay refresh token: al expirar el token (60 min) hay que volver a iniciar sesion.

## Verificacion realizada

En esta fase se ejecutaron, con Docker apagado, los siguientes comandos del frontend:

- `npm install` — instalacion de dependencias. **OK** (aparecen avisos `EBADENGINE` por
  Node 26 y mensajes de `postinstall`/`npm audit`; son avisos, no errores).
- `npm run build` — `tsc -b` (typecheck) + `vite build` (build de produccion). **OK**,
  `dist/` generado sin errores de tipo ni de build.
- `npm run lint` — ESLint de la plantilla. **OK**, sin errores.
- `npm run test` — **no se ejecuto**: el script no existe (esta fase no agrego pruebas de
  componente).

El resultado resumido de estos comandos queda tambien registrado en el mensaje del commit
de esta fase, para que la revision pueda leerlo sin re-ejecutarlos.

> **No verificado (Docker apagado):** el flujo **en vivo** contra el Gateway + MySQL. No
> hay backend ni base de datos corriendo, por lo que la validacion end-to-end real (login
> real, lecturas y escrituras contra la Api a traves del Gateway) **queda pendiente para
> la fase de integracion**. Este README **no** afirma haber probado contra el backend
> real; la UI asume las formas `camelCase` del contrato documentado en la fase 2.

## Supuestos y desajustes detectados contra el Gateway

1. **CORS ausente en el Gateway (desajuste real).** `backend/gateway/Program.cs` no
   registra CORS, asi que una SPA en otro origen seria bloqueada por el navegador.
   **Resolucion sin tocar el backend:** el proxy del dev server de Vite hace que `/api`
   salga del mismo origen que la SPA. En produccion (fase 4, detras de un proxy inverso)
   el frontend y `/api` comparten origen, por lo que tampoco hace falta CORS.
2. **El rol se toma de `GET /api/auth/me`** (campo `rol`), no del JWT. El token lleva el
   rol en el claim estandar `role`, pero la UI prefiere leerlo de `/me` tras el login:
   mas simple y fiable que decodificar el token en el cliente.
3. **Sin refresh token.** El token expira a los 60 min; al recibir 401 la UI cierra
   sesion y redirige a `/login` para re-autenticar.
4. **Registro de usuarios fuera de alcance de la UI.** `POST /api/auth/register` existe
   en el backend pero no se expone en el frontend de esta fase; los usuarios se crean por
   backend/seed.
5. **`idEstudio` en Resultados tratantes se captura como input numerico opcional**, no
   como selector `/api/estudios?idCita=`: ese endpoint exige `idCita` y en el flujo de un
   resultado (que referencia medico/diagnostico) no hay una cita seleccionada de forma
   natural. El helper de lectura de estudios queda implementado por si una iteracion
   futura ata el estudio a la cita del diagnostico elegido. Es una simplificacion
   consciente.
6. **Formato de `fecha`.** Los `<input type="date">` envian solo el dia; la UI lo
   convierte a ISO-8601 antes de enviarlo. Si se omite `fecha` al crear, el backend aplica
   `DateTime.UtcNow`.
7. **Las pantallas de apoyo son solo lectura.** Pacientes, historiales, citas, medicos y
   estudios se consultan unicamente para poblar selectores; no se construyo su CRUD. El
   resto de entidades del EMR queda fuera de alcance / stub.
