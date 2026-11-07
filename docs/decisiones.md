# Decisiones tecnicas — Fase 1 (cimiento de datos)

Esta fase construye solo la capa de datos EF Core del EMR ClinicaCore. No incluye
APIs ejecutables, frontend ni infraestructura de Swarm. Fuente de verdad del modelo:
[`docs/er/modelo-er.md`](er/modelo-er.md) y [`Requirements.md`](../Requirements.md).

## 1. Proveedor MySQL de EF Core elegido

| Paquete | Version | Motivo |
|---|---|---|
| `Pomelo.EntityFrameworkCore.MySql` | **9.0.0** | Ultima version estable (publicada 2025-08-17). Es el proveedor MySQL mas usado para EF Core y se apoya en MySqlConnector. |
| `Microsoft.EntityFrameworkCore` | 9.0.11 | EF Core fijado en 9.0.x (ver nota de compatibilidad). |
| `Microsoft.EntityFrameworkCore.Relational` | 9.0.11 | Igual que arriba. |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.11 | Para generar/aplicar migraciones. |
| `MySqlConnector` | 2.6.2 | Se sube explicitamente por encima del 2.4.0 que trae Pomelo. |
| `dotnet-ef` (herramienta local) | 9.0.11 | Declarada en `backend/dotnet-tools.json`. |

### Nota de compatibilidad .NET 10 / EF Core 9

El SDK instalado es .NET 10 (`10.0.100`) y los proyectos tienen como objetivo
`net10.0`. Sin embargo, **Pomelo 9.0.0 depende de EF Core Relational 9.0.x**
(rango `>= 9.0.0 && <= 9.0.999`). A la fecha de esta fase no existe una version de
Pomelo construida para EF Core 10. Mezclar Pomelo 9 con paquetes de EF Core 10
produce el conflicto de version NU1608/NU1107.

**Decision:** fijar todos los paquetes de EF Core (y la herramienta `dotnet-ef`) en
**9.0.x** y mantener el objetivo `net10.0`. El paquete `net10.0` de Pomelo esta
marcado como compatible por NuGet, por lo que el `dotnet build` pasa sin advertencias
nuevas. Cuando Pomelo publique una version para EF Core 10 se podra subir el pin.

### Vulnerabilidad de MySqlConnector (NU1902)

Pomelo 9.0.0 arrastra `MySqlConnector 2.4.0`, que tiene la advertencia de seguridad
de gravedad moderada [GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8)
(posible exposicion de contrasena ante un intermediario en modo TLS de cero
configuracion). Se corrige en 2.6.1+. El proyecto `database` referencia de forma
explicita `MySqlConnector 2.6.2`, que resuelve la advertencia y es compatible con
Pomelo (pide `>= 2.4.0`). Con `TreatWarningsAsErrors` activo, esto mantiene el build
limpio. *Contenido parafraseado para cumplir con restricciones de licencia.*

## 2. Estructura de proyectos creada

```
backend/
├── ClinicaCore.slnx                 # Solucion .NET 10 (formato slnx)
├── dotnet-tools.json                # Manifiesto de herramientas locales (dotnet-ef)
├── database/                        # Biblioteca de clases: capa de datos EF Core
│   ├── ClinicaCore.Database.csproj
│   ├── ClinicaCoreDbContext.cs      # DbContext unico (18 entidades)
│   ├── ClinicaCoreDbContextFactory.cs  # IDesignTimeDbContextFactory (migraciones sin DB)
│   ├── Entities/                    # 18 entidades POCO
│   ├── Configurations/              # Fluent API por dominio (auth/clinical/billing/inventory)
│   └── Migrations/                  # Migracion inicial InicialEsquema + snapshot
└── tests/                           # Pruebas minimas del modelo
    ├── ClinicaCore.Database.Tests.csproj
    └── ModeloTests.cs
```

Se crea un **unico DbContext** sobre una sola base MySQL con llaves foraneas reales,
como indica el modelo ER. La particion por microservicio (clinical, billing,
inventory, auth) es una division **logica** reflejada en los archivos de configuracion
y en los comentarios de las entidades; no se separan contextos ni bases en esta fase.

Las carpetas `gateway/`, servicios de dominio, `shared/`, `frontend/`, `infra/` y
los archivos de Docker/Swarm **no** se crean en esta fase (fuera de alcance).

## 3. Como aplicar la migracion

La migracion inicial ya esta generada y versionada en
`backend/database/Migrations/`. No se necesito una base MySQL corriendo para
generarla: el `IDesignTimeDbContextFactory` fija la version del servidor
(`MySqlServerVersion 8.0.36`) y EF no abre conexion al crear la migracion.

Para **aplicarla** contra una base MySQL 8 real (fase posterior, cuando exista el
contenedor de base de datos):

```bash
cd backend

# 1. Restaurar herramientas locales (incluye dotnet-ef 9.0.11)
dotnet tool restore

# 2. Aplicar la migracion. Ajusta la cadena de conexion al servidor real.
dotnet ef database update \
  --project database/ClinicaCore.Database.csproj \
  --startup-project database/ClinicaCore.Database.csproj \
  --connection "Server=localhost;Port=3306;Database=clinicacore;User=clinicacore;Password=TU_PASSWORD;"
```

Alternativa sin aplicar directamente (util para revisar o para el contenedor
`migrator` de Swarm en fases posteriores): generar el script SQL idempotente.

```bash
cd backend
dotnet ef migrations script --idempotent \
  --project database/ClinicaCore.Database.csproj \
  --startup-project database/ClinicaCore.Database.csproj \
  --output artifacts/migracion-inicial.sql
```

## 4. Supuestos de mapeo adoptados

Se respetan los supuestos del modelo ER. Decisiones adicionales tomadas al mapear,
eligiendo siempre la opcion mas simple:

1. **genero** se mapea como `char(1)` con una restriccion CHECK
   `ck_paciente_genero` que acepta solo `'F'` o `'M'`, como en la captura.
2. **cui** (paciente) y **colegiado** (medico) tienen indice unico.
3. **Nombres de tabla** en snake_case y espanol; `resultado_tratante` corrige la
   errata `resultados_trantes` de la captura.
4. **Comportamiento de borrado:** las relaciones de catalogo y las obligatorias usan
   `Restrict` para no perder datos clinicos por cascada; los detalles de receta y
   factura usan `Cascade`; las FKs opcionales (`id_diagnostico`, `id_estudio`,
   `id_asegurado`, `id_proveedor` en kardex, `id_medico` en usuario) usan `SetNull`.
   Es la opcion mas conservadora y no inventa reglas de negocio.
5. **Tipos temporales:** se usan `DateOnly` para fechas, `TimeOnly` para la hora de la
   cita y `DateTime` para marcas de tiempo, por ser lo mas directo en .NET 10.
6. **Longitudes de cadena** acotadas con valores razonables y `text` para campos
   largos (descripcion, antecedentes, conclusion). No provienen del enunciado; son
   decisiones de mapeo para producir un esquema valido.

No se detectaron contradicciones entre `README.md`, `Requirements.md` y
`docs/er/modelo-er.md` que obligaran a modificar esos documentos.

## 5. Verificacion ejecutada en esta fase

| Comando | Resultado |
|---|---|
| `dotnet build` | Compilacion correcta. 0 advertencias, 0 errores. |
| `dotnet ef migrations add InicialEsquema` | Migracion generada en `database/Migrations/`. |
| `dotnet test` | 12 pruebas superadas, 0 con error. |

Las pruebas validan que el modelo compila, que el DbContext se construye con el
proveedor InMemory, que el modelo expone 18 entidades, que `resultado_tratante` usa
el nombre corregido, que varias tablas usan snake_case en espanol y que `cui` y
`colegiado` tienen unicidad.
