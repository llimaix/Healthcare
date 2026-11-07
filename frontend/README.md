# Frontend — ClinicaCore EMR

SPA en React + TypeScript (Vite 7) que consume el **Gateway** del backend
(`/api/*`). El frontend nunca habla directo con la Api de dominio.

## Requisitos

- Node 20.19+ / 22.12+ (minimo de Vite 7). El entorno de desarrollo usa
  **Node v26.8.2 / npm 11.19.1**: funciona, pero al instalar puede aparecer un aviso
  `EBADENGINE` de alguna dependencia transitiva porque Node 26 es una linea non-LTS.
  Es un **aviso, no un error**, y no bloquea build ni dev.

## Instalacion y uso

```bash
cd frontend
npm install        # instala dependencias (versiones exactas, ver package.json)
npm run dev        # dev server en http://localhost:5173
npm run build      # build de produccion: tsc -b + vite build
npm run lint       # ESLint
npm run preview    # sirve el build de produccion
```

## Variable de entorno

| Variable            | Default (dev) | Descripcion                                              |
| ------------------- | ------------- | -------------------------------------------------------- |
| `VITE_GATEWAY_URL`  | `/api`        | URL base contra la que el cliente HTTP llama al Gateway. |

En desarrollo el valor es `/api` (relativo). El dev server de Vita actua como proxy
(`vite.config.ts` -> `server.proxy['/api']` -> `http://localhost:5000`): el navegador
ve siempre el mismo origen y Vite reenvia al Gateway. Esto evita CORS en dev **sin
tocar el backend** (el Gateway no tiene CORS configurado). Ver `.env.example`.

Para levantar el backend (Gateway en `:5000` + Api + MySQL) remitirse a
`../docs/fase2-backend.md`.

## Estado de pantallas

- **COMPLETAS en esta fase (FEAT-001):** login (`/login`), flujo de autenticacion
  (contexto, token en `localStorage`, ruta protegida, logout, manejo global de 401),
  cliente HTTP con Bearer y manejo de 401/403, tipos del contrato.
- **Placeholders protegidos:** `/diagnosticos` y `/resultados-tratantes` existen como
  rutas protegidas; sus pantallas completas (lista + formularios CRUD) las implementan
  **FEAT-002** y **FEAT-003**.
- **Fuera de alcance:** CRUD completo del resto de entidades; registro de usuarios.

## Verificacion realizada

Se verifico: `npm install`, `npm run build` (tsc + vite build) y `npm run lint`, todos
sin errores (ver el commit de esta fase para el resultado resumido).

**No verificado (Docker apagado):** el flujo en vivo contra Gateway + MySQL. La prueba
end-to-end real queda para la fase de integracion y **no** se afirma haberla ejecutado.
