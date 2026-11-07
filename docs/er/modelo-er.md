# Modelo entidad–relación (propuesta v1)

Modelo implementado: coincide tabla por tabla y columna por columna con el esquema que crean las migraciones de EF Core en MySQL. Los atributos marcados con (*) vienen de la captura del enunciado; el resto son decisiones de diseño del equipo. La imagen `docs/er/diagrama-er.png` se genera desde este diagrama con `docs/entregable/generar.mjs`.

## Diagrama

```mermaid
erDiagram
    ROL ||--o{ USUARIO : tiene
    MEDICO |o--o| USUARIO : "cuenta de"
    PACIENTE ||--o{ CITA : agenda
    MEDICO ||--o{ CITA : atiende
    PACIENTE ||--|| HISTORIAL_CLINICO : posee
    HISTORIAL_CLINICO ||--o{ DIAGNOSTICO : registra
    CITA ||--o{ DIAGNOSTICO : genera
    CITA ||--o{ RECETA : emite
    DIAGNOSTICO |o--o{ RECETA : "origina"
    RECETA ||--|{ RECETA_DETALLE : contiene
    INSUMO ||--o{ RECETA_DETALLE : "se receta"
    CITA ||--o{ ESTUDIO : solicita
    ESTUDIO ||--o{ RESULTADO_TRATANTE : produce
    MEDICO ||--o{ RESULTADO_TRATANTE : registra
    DIAGNOSTICO |o--o{ RESULTADO_TRATANTE : "evalua"
    PROVEEDOR ||--o{ INSUMO : suministra
    INSUMO ||--o{ KARDEX : "movimientos"
    PROVEEDOR |o--o{ KARDEX : "entrada desde"
    TIPO_COBERTURA ||--o{ ASOCIADO_SEGURO : define
    PACIENTE ||--o{ ASOCIADO_SEGURO : "asegurado en"
    PACIENTE ||--o{ FACTURACION : paga
    CITA ||--o| FACTURACION : "se cobra en"
    ASOCIADO_SEGURO |o--o{ FACTURACION : "aplica a"
    FACTURACION ||--|{ FACTURACION_DETALLE : contiene

    ROL {
        int id_rol PK
        string nombre UK
    }
    USUARIO {
        int id_usuario PK
        string correo UK
        string password_hash
        int id_rol FK
        int id_medico FK "nullable"
        bool activo
    }
    MEDICO {
        int id_medico PK "*"
        string nombre "*"
        string apellido "*"
        string colegiado UK "*"
        string especialidad "*"
    }
    PACIENTE {
        int id_paciente PK "*"
        string nombre "*"
        string apellido "*"
        string cui UK "*"
        date fecha_nacimiento "*"
        string nit "*"
        string telefono "*"
        string correo_electronico "*"
        char genero "* F o M"
        string tipo_sangre "*"
        bool activo "borrado logico"
    }
    CITA {
        int id_cita PK "*"
        int id_medico FK "*"
        int id_paciente FK "*"
        date fecha_cita "*"
        time hora "*"
        string motivo
        string estado
    }
    HISTORIAL_CLINICO {
        int id_historial PK
        int id_paciente FK,UK
        date fecha_apertura
        string antecedentes
    }
    DIAGNOSTICO {
        int id_diagnosticos PK "*"
        int id_historial FK
        int id_cita FK
        string descripcion
        string codigo_cie10
        string alergias "*"
        datetime fecha
    }
    RECETA {
        int id_receta PK "*"
        int id_cita FK
        int id_diagnostico FK "nullable"
        date fecha_emision
        string indicaciones
    }
    RECETA_DETALLE {
        int id_receta_detalle PK
        int id_receta FK
        int id_insumo FK
        string dosis
        string frecuencia
        int duracion_dias
    }
    ESTUDIO {
        int id_estudio PK
        int id_cita FK
        string tipo
        string descripcion
        date fecha_solicitud
        string estado
    }
    RESULTADO_TRATANTE {
        int id_resultado PK "*"
        int id_estudio FK "nullable"
        int id_medico FK
        int id_diagnostico FK "nullable"
        datetime fecha
        string descripcion
        string conclusion
    }
    PROVEEDOR {
        int id_proveedor PK
        string nombre
        string nit
        string telefono
        string correo
    }
    INSUMO {
        int id_insumo PK
        int id_proveedor FK
        string nombre
        string tipo "medicamento o insumo"
        decimal precio
        int stock_minimo
    }
    KARDEX {
        int id_kardex PK
        int id_insumo FK
        int id_proveedor FK "nullable"
        string tipo_movimiento "ENTRADA o SALIDA"
        int cantidad
        datetime fecha
        string referencia
    }
    TIPO_COBERTURA {
        int id_tipo_cobertura PK
        string nombre
        decimal porcentaje_cobertura
    }
    ASOCIADO_SEGURO {
        int id_asegurado PK "*"
        int id_paciente FK
        int id_tipo_cobertura FK
        string aseguradora
        string numero_poliza
        date vigencia_desde
        date vigencia_hasta
    }
    FACTURACION {
        int id_factura PK
        int id_paciente FK
        int id_cita FK,UK
        int id_asegurado FK "nullable"
        datetime fecha
        decimal subtotal
        decimal monto_cobertura
        decimal total
        string estado
    }
    FACTURACION_DETALLE {
        int id_detalle PK
        int id_factura FK
        string concepto
        int cantidad
        decimal precio_unitario
    }
```

## Supuestos a validar

1. **Kardex:** el enunciado no lo define. Se interpreta como el registro de movimientos (entradas y salidas) de medicamentos e insumos, ligado a proveedores. Por eso aparece la entidad `insumo`, que no está en el enunciado.
2. **Resultados tratantes:** se modela como el resultado que registra un médico, ligado opcionalmente a un estudio y a un diagnóstico. La captura escribe `resultados_trantes`; la tabla se llamará `resultado_tratante`.
3. **Recetas:** se normalizan en cabecera y detalle, porque una receta puede incluir varios medicamentos.
4. **Historial clínico:** uno por paciente; los diagnósticos cuelgan de él y de la cita donde se hicieron.
5. **Tipo de cobertura:** catálogo de planes; `asociado_seguro` une a un paciente con un plan y una póliza.
6. **Seguridad:** `usuario` y `rol` se agregan para JWT. Un usuario puede enlazarse a un médico.
7. **Datos:** una sola base MySQL con llaves foráneas reales. Cada servicio es dueño lógico de sus tablas:
   - `clinical`: paciente, medico, cita, historial_clinico, diagnostico, receta, receta_detalle, estudio, resultado_tratante.
   - `billing`: facturacion, facturacion_detalle, asociado_seguro, tipo_cobertura.
   - `inventory`: proveedor, insumo, kardex.
   - `auth`: usuario, rol.
8. **Tipos:** `genero` acepta solo `F` o `M`, como en la captura. `cui` y `colegiado` son únicos.
