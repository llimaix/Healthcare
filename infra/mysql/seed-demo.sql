-- =============================================================================
-- ClinicaCore — Datos de demostración (dummy)
-- =============================================================================
-- Carga OPCIONAL de datos genéricos para demostración y capturas de pantalla.
-- NO es parte del arranque del sistema: se aplica a mano cuando el stack ya está
-- estable y las migraciones aplicadas (ver infra/scripts/08-cargar-demo.sh).
--
-- Es idempotente: cada INSERT usa "WHERE NOT EXISTS", así que volver a correrlo
-- no duplica filas. Respeta las llaves foráneas y el CHECK de genero (F/M).
-- Los datos son ficticios; cualquier parecido con personas reales es coincidencia.
-- =============================================================================

USE clinicacore;

-- --- Catálogo de roles (puede que el seeder de la Api ya los haya creado) --------
INSERT INTO rol (id_rol, nombre)
SELECT 1, 'Admin'      WHERE NOT EXISTS (SELECT 1 FROM rol WHERE nombre = 'Admin');
INSERT INTO rol (id_rol, nombre)
SELECT 2, 'Medico'     WHERE NOT EXISTS (SELECT 1 FROM rol WHERE nombre = 'Medico');
INSERT INTO rol (id_rol, nombre)
SELECT 3, 'Recepcion'  WHERE NOT EXISTS (SELECT 1 FROM rol WHERE nombre = 'Recepcion');
INSERT INTO rol (id_rol, nombre)
SELECT 4, 'Lectura'    WHERE NOT EXISTS (SELECT 1 FROM rol WHERE nombre = 'Lectura');

-- --- Médicos ---------------------------------------------------------------------
INSERT INTO medico (id_medico, nombre, apellido, colegiado, especialidad)
SELECT 1, 'Ana', 'García', 'COL-1001', 'Medicina General'
WHERE NOT EXISTS (SELECT 1 FROM medico WHERE colegiado = 'COL-1001');
INSERT INTO medico (id_medico, nombre, apellido, colegiado, especialidad)
SELECT 2, 'Carlos', 'Méndez', 'COL-1002', 'Cardiología'
WHERE NOT EXISTS (SELECT 1 FROM medico WHERE colegiado = 'COL-1002');
INSERT INTO medico (id_medico, nombre, apellido, colegiado, especialidad)
SELECT 3, 'Lucía', 'Ramírez', 'COL-1003', 'Pediatría'
WHERE NOT EXISTS (SELECT 1 FROM medico WHERE colegiado = 'COL-1003');

-- --- Pacientes (genero en {F, M}) ------------------------------------------------
INSERT INTO paciente (id_paciente, nombre, apellido, cui, fecha_nacimiento, nit, telefono, correo_electronico, genero, tipo_sangre, activo)
SELECT 1, 'María', 'López', 'CUI-2001', '1990-04-12', 'NIT-3001', '5551-0001', 'maria.lopez@demo.test', 'F', 'O+', 1
WHERE NOT EXISTS (SELECT 1 FROM paciente WHERE cui = 'CUI-2001');
INSERT INTO paciente (id_paciente, nombre, apellido, cui, fecha_nacimiento, nit, telefono, correo_electronico, genero, tipo_sangre, activo)
SELECT 2, 'José', 'Pérez', 'CUI-2002', '1985-11-30', 'NIT-3002', '5551-0002', 'jose.perez@demo.test', 'M', 'A+', 1
WHERE NOT EXISTS (SELECT 1 FROM paciente WHERE cui = 'CUI-2002');
INSERT INTO paciente (id_paciente, nombre, apellido, cui, fecha_nacimiento, nit, telefono, correo_electronico, genero, tipo_sangre, activo)
SELECT 3, 'Sofía', 'Hernández', 'CUI-2003', '2001-07-08', 'NIT-3003', '5551-0003', 'sofia.hernandez@demo.test', 'F', 'B-', 1
WHERE NOT EXISTS (SELECT 1 FROM paciente WHERE cui = 'CUI-2003');
INSERT INTO paciente (id_paciente, nombre, apellido, cui, fecha_nacimiento, nit, telefono, correo_electronico, genero, tipo_sangre, activo)
SELECT 4, 'Diego', 'Morales', 'CUI-2004', '1978-02-20', 'NIT-3004', '5551-0004', 'diego.morales@demo.test', 'M', 'AB+', 1
WHERE NOT EXISTS (SELECT 1 FROM paciente WHERE cui = 'CUI-2004');

-- --- Historial clínico (uno por paciente; id_paciente es UNIQUE) -----------------
INSERT INTO historial_clinico (id_historial, id_paciente, fecha_apertura, antecedentes)
SELECT 1, 1, '2023-01-15', 'Hipertensión controlada. Sin alergias conocidas.'
WHERE NOT EXISTS (SELECT 1 FROM historial_clinico WHERE id_paciente = 1);
INSERT INTO historial_clinico (id_historial, id_paciente, fecha_apertura, antecedentes)
SELECT 2, 2, '2023-03-02', 'Antecedente de fractura en 2019. Fumador.'
WHERE NOT EXISTS (SELECT 1 FROM historial_clinico WHERE id_paciente = 2);
INSERT INTO historial_clinico (id_historial, id_paciente, fecha_apertura, antecedentes)
SELECT 3, 3, '2024-05-21', 'Sin antecedentes relevantes.'
WHERE NOT EXISTS (SELECT 1 FROM historial_clinico WHERE id_paciente = 3);
INSERT INTO historial_clinico (id_historial, id_paciente, fecha_apertura, antecedentes)
SELECT 4, 4, '2024-09-10', 'Diabetes tipo 2 en tratamiento.'
WHERE NOT EXISTS (SELECT 1 FROM historial_clinico WHERE id_paciente = 4);

-- --- Citas -----------------------------------------------------------------------
INSERT INTO cita (id_cita, id_medico, id_paciente, fecha_cita, hora, motivo, estado)
SELECT 1, 1, 1, '2025-01-10', '09:00:00', 'Control de presión arterial', 'Atendida'
WHERE NOT EXISTS (SELECT 1 FROM cita WHERE id_cita = 1);
INSERT INTO cita (id_cita, id_medico, id_paciente, fecha_cita, hora, motivo, estado)
SELECT 2, 2, 2, '2025-01-12', '10:30:00', 'Dolor torácico', 'Atendida'
WHERE NOT EXISTS (SELECT 1 FROM cita WHERE id_cita = 2);
INSERT INTO cita (id_cita, id_medico, id_paciente, fecha_cita, hora, motivo, estado)
SELECT 3, 3, 3, '2025-02-01', '08:15:00', 'Chequeo general', 'Atendida'
WHERE NOT EXISTS (SELECT 1 FROM cita WHERE id_cita = 3);
INSERT INTO cita (id_cita, id_medico, id_paciente, fecha_cita, hora, motivo, estado)
SELECT 4, 1, 4, '2025-02-05', '11:45:00', 'Seguimiento de diabetes', 'Atendida'
WHERE NOT EXISTS (SELECT 1 FROM cita WHERE id_cita = 4);

-- --- Diagnósticos ----------------------------------------------------------------
INSERT INTO diagnostico (id_diagnosticos, id_historial, id_cita, descripcion, codigo_cie10, alergias, fecha)
SELECT 1, 1, 1, 'Hipertensión esencial (primaria)', 'I10', 'Ninguna', '2025-01-10 09:20:00'
WHERE NOT EXISTS (SELECT 1 FROM diagnostico WHERE id_diagnosticos = 1);
INSERT INTO diagnostico (id_diagnosticos, id_historial, id_cita, descripcion, codigo_cie10, alergias, fecha)
SELECT 2, 2, 2, 'Angina de pecho estable', 'I20.9', 'Penicilina', '2025-01-12 10:50:00'
WHERE NOT EXISTS (SELECT 1 FROM diagnostico WHERE id_diagnosticos = 2);
INSERT INTO diagnostico (id_diagnosticos, id_historial, id_cita, descripcion, codigo_cie10, alergias, fecha)
SELECT 3, 3, 3, 'Examen médico general sin hallazgos', 'Z00.0', 'Ninguna', '2025-02-01 08:35:00'
WHERE NOT EXISTS (SELECT 1 FROM diagnostico WHERE id_diagnosticos = 3);
INSERT INTO diagnostico (id_diagnosticos, id_historial, id_cita, descripcion, codigo_cie10, alergias, fecha)
SELECT 4, 4, 4, 'Diabetes mellitus tipo 2 sin complicaciones', 'E11.9', 'Sulfas', '2025-02-05 12:00:00'
WHERE NOT EXISTS (SELECT 1 FROM diagnostico WHERE id_diagnosticos = 4);

-- --- Resultados tratantes --------------------------------------------------------
INSERT INTO resultado_tratante (id_resultado, id_estudio, id_medico, id_diagnostico, fecha, descripcion, conclusion)
SELECT 1, NULL, 1, 1, '2025-01-10 09:40:00', 'Presión arterial 150/95 mmHg en consulta.', 'Ajustar dosis de antihipertensivo; control en 30 días.'
WHERE NOT EXISTS (SELECT 1 FROM resultado_tratante WHERE id_resultado = 1);
INSERT INTO resultado_tratante (id_resultado, id_estudio, id_medico, id_diagnostico, fecha, descripcion, conclusion)
SELECT 2, NULL, 2, 2, '2025-01-12 11:10:00', 'Electrocardiograma con cambios leves del segmento ST.', 'Iniciar tratamiento y prueba de esfuerzo programada.'
WHERE NOT EXISTS (SELECT 1 FROM resultado_tratante WHERE id_resultado = 2);
INSERT INTO resultado_tratante (id_resultado, id_estudio, id_medico, id_diagnostico, fecha, descripcion, conclusion)
SELECT 3, NULL, 3, 3, '2025-02-01 08:50:00', 'Signos vitales normales; laboratorios dentro de rango.', 'Paciente sano; control anual.'
WHERE NOT EXISTS (SELECT 1 FROM resultado_tratante WHERE id_resultado = 3);
INSERT INTO resultado_tratante (id_resultado, id_estudio, id_medico, id_diagnostico, fecha, descripcion, conclusion)
SELECT 4, NULL, 1, 4, '2025-02-05 12:20:00', 'Hemoglobina glicosilada (HbA1c) 7.8%.', 'Reforzar dieta y adherencia; control en 3 meses.'
WHERE NOT EXISTS (SELECT 1 FROM resultado_tratante WHERE id_resultado = 4);

-- Resumen de carga
SELECT
  (SELECT COUNT(*) FROM medico)             AS medicos,
  (SELECT COUNT(*) FROM paciente)           AS pacientes,
  (SELECT COUNT(*) FROM cita)               AS citas,
  (SELECT COUNT(*) FROM diagnostico)        AS diagnosticos,
  (SELECT COUNT(*) FROM resultado_tratante) AS resultados_tratantes;
