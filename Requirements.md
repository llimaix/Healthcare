# Requerimientos del proyecto — Sistema de gestión de atención médica

**Valor:** 10 pts del Examen Final.

## 1. Objetivo

Desarrollar un sistema de gestión de atención médica utilizando **.NET**, con persistencia en una **base de datos relacional**, para administrar información de pacientes, profesionales médicos y procesos asociados a la atención clínica.

El sistema deberá contemplar el almacenamiento, la administración y la consulta de registros médicos electrónicos (EMR), tales como diagnósticos, recetas, resultados de laboratorio y otros datos clínicos. La información deberá organizarse de manera que facilite su consulta y el cumplimiento de las disposiciones aplicables de protección de datos de salud.

## 2. Alcance funcional

El modelo de datos y el desarrollo del proyecto deberán considerar las siguientes entidades o módulos identificados en el enunciado:

1. Pacientes.
2. Médicos.
3. Citas o consultas.
4. Historial clínico.
5. Recetas.
6. Estudios.
7. Facturación.
8. Diagnósticos.
9. Proveedores.
10. Kardex (*«cardex» en la imagen original*).
11. Tipos de cobertura.
12. Resultados tratantes (*`resultados_trantes` en la imagen, con posible error tipográfico; denominación por confirmar*).
13. Asociación con seguro.

> **Nota sobre el alcance:** la imagen presenta una lista inicial de entidades y un bosquejo parcial de campos. Las entidades no detalladas deberán modelarse de manera coherente, sin atribuir al enunciado atributos o reglas de negocio que no aparecen en él.

## 3. Modelo de datos: campos identificados

Los siguientes atributos se distinguen en la captura proporcionada. Constituyen la referencia inicial para el diseño del modelo entidad–relación; los tipos de datos, llaves, restricciones y cardinalidades se establecerán durante el modelado.

| Entidad | Atributos visibles en la imagen | Observaciones |
|---|---|---|
| Médico | `id_medico`, `nombre`, `apellido`, `colegiado`, `especialidad` | Identificador del profesional y datos profesionales. |
| Paciente | `id_paciente`, `nombre`, `apellido`, `cui`, `fecha_nacimiento`, `nit`, `telefono`, `correo_electronico`, `genero (F, M)`, `tipo_sangre` | Los valores de género se transcriben como aparecen en el borrador. |
| Cita del paciente | `id_cita`, `id_medico`, `fecha_cita`, `hora`, `id_paciente` | La relación médico–paciente se infiere de los identificadores listados. |
| Diagnósticos | `id_diagnosticos`, `alergias` | La línea de la captura está incompleta; se requiere validar el diseño. |
| Recetas | `id_receta` | No se visualizan otros atributos. |
| Resultados tratantes (`resultados_trantes`) | `id_resultado` | El nombre se transcribe como aparece; la línea queda incompleta. |
| Asociación con seguro | `id_asegurado` | El resto de la línea queda fuera de la captura. |

**Revisión obligatoria antes de implementar:** confirmar los nombres de las entidades truncadas, completar los atributos faltantes y determinar las relaciones y las cardinalidades. La captura no define por completo el esquema relacional.

## 4. Requerimientos de implementación

- **Tecnología:** utilizar .NET para desarrollar el proyecto.
- **Persistencia:** diseñar e implementar una base de datos relacional alineada con las entidades identificadas.
- **Modelo entidad–relación:** representar las entidades, sus atributos, las claves primarias y foráneas, y las relaciones con sus cardinalidades.
- **Interfaz:** desarrollar los formularios necesarios para mostrar y gestionar diagnósticos y resultados tratantes, de forma que puedan documentarse mediante capturas de pantalla.
- **Trazabilidad:** mantener correspondencia entre el modelo de datos, la implementación y los formularios presentados como evidencia.

> El enunciado no especifica versión de .NET, motor de base de datos, framework de interfaz ni arquitectura. Estas decisiones las define el equipo y se documentan en la sección 8; no son imposiciones del enunciado y pueden cambiar si algo limita el resultado, sin dejar fuera el alcance funcional, los requerimientos de implementación ni los criterios de aceptación.

## 5. Entregables obligatorios

Se deberá entregar **un archivo PDF** que incluya, como mínimo, los siguientes elementos:

1. **Imagen del diagrama entidad–relación:** representación legible del modelo de datos implementado.
2. **Imagen de los formularios de diagnósticos:** capturas claras de las pantallas correspondientes.
3. **Imagen de los resultados tratantes (formularios):** capturas claras de las pantallas correspondientes.
4. **Enlace para descargar el código fuente del proyecto:** URL funcional y accesible para obtener el código completo.

### 5.1. Criterios de presentación del PDF

- Incluir un título y secciones claramente identificadas para cada evidencia solicitada.
- Insertar las imágenes con una resolución que permita leer nombres, campos y relaciones.
- Asegurar que las capturas correspondan a la implementación entregada.
- Presentar el enlace al código fuente como un hipervínculo verificable; especificar el repositorio o archivo de descarga y, cuando corresponda, las instrucciones de acceso.

## 6. Criterios de aceptación

El trabajo se considerará completo cuando:

- [ ] El proyecto se haya desarrollado en .NET.
- [ ] Exista un modelo relacional que cubra el alcance acordado y resuelva los datos incompletos de la captura.
- [ ] El diagrama entidad–relación refleje la estructura de datos realmente implementada.
- [ ] Los formularios de diagnósticos estén implementados y documentados mediante imágenes.
- [ ] Los formularios de resultados tratantes estén implementados y documentados mediante imágenes.
- [ ] El PDF contenga las tres categorías de imágenes exigidas.
- [ ] El PDF incluya un enlace válido para descargar el código fuente.
- [ ] El contenido del PDF y el código fuente entregado correspondan a la misma versión del proyecto.

## 7. Información pendiente de confirmar

La imagen no permite determinar con certeza:

- Los campos completos de `diagnosticos`, `recetas`, `resultados_trantes` y `asociado_seguro`.
- El significado de las marcas «N1» (lista de tablas) y «n2, n3» (definiciones de campos) de la captura; podrían indicar niveles o fases de entrega.
- El significado exacto de «Kardex» y su relación con los demás registros.
- Si «resultados tratantes» se refiere a resultados de tratamiento, resultados del médico tratante u otra entidad específica. En este documento se usa **resultados tratantes**, tal como lo indica el enunciado.
- Las reglas de negocio de estudios, facturación, proveedores y tipos de cobertura.
- Las cardinalidades definitivas, las validaciones de formularios y los permisos de acceso.

Estas definiciones deberán validarse antes de cerrar el diseño de la base de datos y la implementación.

---

**Fuente del requerimiento:** captura de un borrador en Notepad++ y las instrucciones adicionales proporcionadas para el desarrollo en .NET y la entrega de un PDF con evidencias y enlace al código.

---

## 8. Propuesta técnica del equipo (modificable)

### 8.1. Descripción del proyecto para el repositorio

**Nombre de trabajo:** `clinicacore`

**Descripción:** ClinicaCore es una plataforma web de registros médicos electrónicos (EMR) para clínicas y hospitales. Permite administrar pacientes, médicos, citas, historial clínico, diagnósticos, recetas, estudios, resultados tratantes, kardex, facturación, proveedores y cobertura de seguros. Está construida con .NET, React y MySQL, y se despliega en contenedores sobre Docker Swarm, con una base de datos replicada para mantener la disponibilidad de la información clínica.

**Tema sugerido para GitHub:** `emr`, `dotnet`, `react`, `mysql`, `docker-swarm`, `nginx`, `microservices`.

### 8.2. Stack

| Capa | Tecnología | Nota |
|---|---|---|
| Frontend | React | Formularios de diagnósticos y resultados tratantes, que sirven como evidencia para el PDF. |
| Backend | .NET 10 con ASP.NET Core | Servicios HTTP. Autenticación con JWT. |
| Acceso a datos | EF Core con proveedor para MySQL (por ejemplo Pomelo) | Migraciones versionadas. |
| Base de datos | MySQL 8 con replicación primario–réplica | Ver 8.5. |
| Proxy | nginx | Punto de entrada único; sirve el frontend y enruta hacia el backend. |
| Contenedores | Docker | Un contenedor por servicio. |
| Orquestación | Docker Swarm | Despliegue con `docker stack deploy`. |

### 8.3. Estructura del repositorio (monorepo)

La estructura es una base de partida; los nombres de carpetas y proyectos pueden ajustarse.

```
clinicacore/
├── frontend/            # Aplicación React
├── backend/
│   ├── gateway/         # API de entrada para el frontend (BFF)
│   ├── clinical/        # Servicio clínico: pacientes, médicos, citas, historial,
│   │                    # diagnósticos, recetas, estudios, resultados tratantes, kardex
│   ├── billing/         # Servicio de facturación, seguros, cobertura y proveedores
│   ├── shared/          # Modelos y utilidades compartidas
│   ├── database/        # Entidades, migraciones y herramienta de migración
│   └── tests/           # Pruebas por servicio
├── infra/
│   ├── nginx/           # Configuración del proxy
│   ├── mysql/           # Configuración de replicación y scripts de inicio
│   ├── stack.yml        # Definición del stack de Swarm
│   └── scripts/         # Inicialización de Swarm, despliegue, respaldo
├── docs/
│   ├── er/              # Diagrama entidad–relación
│   ├── evidencias/      # Capturas de formularios para el PDF
│   └── decisiones.md    # Decisiones técnicas y pendientes
├── .github/             # Flujos de CI
└── README.md
```

La partición en `clinical` y `billing` es una propuesta. Si resulta excesiva para el tiempo disponible, se empieza con un único servicio de negocio y se separa después, sin afectar el alcance.

### 8.4. Servicios y despliegue en Swarm

- `nginx`: expone los puertos públicos y enruta hacia el frontend y el gateway.
- `frontend`: archivos estáticos de React.
- `gateway`: único servicio de backend visible para el frontend; delega en `clinical` y `billing`.
- `clinical` y `billing`: solo accesibles dentro de la red interna.
- `migrator`: tarea de un solo uso que aplica las migraciones; las réplicas de las APIs no migran.
- Red overlay interna para la comunicación entre servicios.
- Credenciales de MySQL y cadenas de conexión mediante Docker secrets.
- Réplicas múltiples para `nginx`, `gateway`, `clinical` y `billing`, con verificaciones de salud.

### 8.5. Alta disponibilidad de MySQL

Opción propuesta: **replicación primario–réplica con GTID**.

- Un nodo primario recibe escrituras y una o más réplicas las reciben por replicación.
- Cada nodo usa su propio volumen y se fija a un nodo de Swarm mediante restricciones de ubicación.
- Un script de inicio configura la replicación.
- Las lecturas pesadas (reportes) pueden dirigirse a la réplica.
- La promoción de la réplica ante la caída del primario es **manual** y queda documentada en un procedimiento.

**Decisión:** se usa la replicación simple. MySQL InnoDB Cluster queda descartado, igual que cualquier configuración que exija más de un nodo.

**Limitaciones a tener en cuenta:**
- El entorno es una sola máquina con un solo nodo de Swarm. Primario y réplica comparten equipo, así que la réplica protege ante la falla o corrupción de un contenedor o volumen de MySQL, pero no ante la caída de la máquina.
- La replicación no sustituye a los respaldos; se incluye un procedimiento de respaldo y restauración.
- Con un solo nodo, las restricciones de ubicación y la tolerancia a fallos de Swarm se demuestran a nivel de contenedor (reinicio automático y réplicas), no de máquina.

### 8.6. Tratamiento de datos de salud

Por tratarse de datos clínicos: autenticación y roles, comunicación cifrada entre cliente y proxy, secretos fuera del repositorio, y registro de accesos a información sensible. El alcance exacto se define según el tiempo disponible.

### 8.7. Decisiones tomadas

- Servicios: los necesarios según el dominio; la partición propuesta en 8.3 se ajusta durante el diseño.
- Base de datos: replicación simple primario–réplica; InnoDB Cluster descartado.
- Infraestructura: una sola máquina, un solo nodo de Swarm.
- Versiones: .NET 10 y MySQL 8.
- Autenticación: JWT.

### 8.8. Pendientes menores

- Versión exacta de MySQL 8 (por ejemplo 8.4 LTS).
- Roles de usuario y sus permisos sobre los datos clínicos.
- Estrategia de emisión y vencimiento de los JWT.
