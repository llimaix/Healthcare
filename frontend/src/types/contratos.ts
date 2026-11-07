// Tipos TypeScript del contrato HTTP del Gateway de ClinicaCore.
// Serializacion backend = System.Text.Json por defecto -> propiedades camelCase
// tanto en request como en response. El frontend habla SOLO con el Gateway (/api/*).
// Fuente: .agents/tasks/fase3-frontend/plan.md (seccion Contrato HTTP).

// --- Autenticacion ---

/** Roles posibles de un usuario. La UI toma el rol de GET /api/auth/me (campo `rol`). */
export type Rol = 'Admin' | 'Medico' | 'Recepcion' | 'Lectura'

/** POST /api/auth/login (anonimo). OJO: el campo es `correo`, no `email`. */
export interface LoginRequest {
  correo: string
  password: string
}

/** Respuesta 200 de POST /api/auth/login. */
export interface LoginResponse {
  token: string
}

/** GET /api/auth/me y GET /api/usuarios. idMedico es null si el usuario no es un medico. */
export interface UsuarioResponse {
  idUsuario: number
  correo: string
  rol: Rol
  idMedico: number | null
  activo?: boolean
}

/** POST /api/auth/register. Con usuarios existentes, solo un Admin autenticado puede usarlo. */
export interface RegistroRequest {
  correo: string
  password: string
  rol: Rol
  idMedico?: number | null
}

/** PUT /api/usuarios/{id} (solo Admin). */
export interface ActualizarUsuarioRequest {
  rol: Rol
  activo: boolean
  idMedico?: number | null
}

// --- Diagnosticos (/api/diagnosticos) ---

/** POST /api/diagnosticos (Admin, Medico). */
export interface CrearDiagnosticoRequest {
  idHistorial: number
  idCita: number
  descripcion?: string
  /** Codigo CIE-10, maximo 10 caracteres. */
  codigoCie10?: string
  /** Obligatorio, no vacio. */
  alergias: string
  /** ISO-8601. Si se omite, el backend aplica DateTime.UtcNow. */
  fecha?: string
}

/** PUT /api/diagnosticos/{id} (Admin, Medico). No incluye idHistorial/idCita: el backend no los modifica. */
export interface ActualizarDiagnosticoRequest {
  descripcion?: string
  codigoCie10?: string
  alergias: string
  fecha?: string
}

/** Respuesta de GET /api/diagnosticos y GET /api/diagnosticos/{id}. */
export interface DiagnosticoResponse {
  idDiagnosticos: number
  idHistorial: number
  idCita: number
  descripcion?: string
  codigoCie10?: string
  alergias: string
  fecha: string
}

// --- Resultados tratantes (/api/resultados-tratantes) ---

/** POST /api/resultados-tratantes (Admin, Medico). */
export interface CrearResultadoTratanteRequest {
  idMedico: number
  idEstudio?: number | null
  idDiagnostico?: number | null
  /** ISO-8601 opcional. */
  fecha?: string
  descripcion?: string
  conclusion?: string
}

/** PUT /api/resultados-tratantes/{id} (Admin, Medico). No incluye idMedico: el backend no lo modifica en update. */
export interface ActualizarResultadoTratanteRequest {
  idEstudio?: number | null
  idDiagnostico?: number | null
  fecha?: string
  descripcion?: string
  conclusion?: string
}

/** Respuesta de GET /api/resultados-tratantes y GET /api/resultados-tratantes/{id}. */
export interface ResultadoTratanteResponse {
  idResultado: number
  idMedico: number
  idEstudio?: number | null
  idDiagnostico?: number | null
  fecha: string
  descripcion?: string
  conclusion?: string
}

// --- Referencias de apoyo (solo lectura para selectores) ---

/** GET /api/pacientes. */
export interface PacienteResponse {
  idPaciente: number
  nombre: string
  apellido: string
  cui: string
  /** DateOnly en formato yyyy-MM-dd. */
  fechaNacimiento?: string
  nit?: string
  telefono?: string
  correoElectronico?: string
  /** 'F' | 'M' en el backend (char). */
  genero?: string
  tipoSangre?: string
}

/** POST /api/pacientes (Admin, Medico, Recepcion). */
export interface CrearPacienteRequest {
  nombre: string
  apellido: string
  cui: string
  fechaNacimiento: string
  nit: string
  telefono: string
  correoElectronico: string
  genero: 'F' | 'M'
  tipoSangre: string
}

/** PUT /api/pacientes/{id} (Admin, Medico, Recepcion). El CUI y la fecha de nacimiento no se modifican. */
export type ActualizarPacienteRequest = Omit<CrearPacienteRequest, 'cui' | 'fechaNacimiento'>

/** GET /api/historiales?idPaciente= */
export interface HistorialResponse {
  idHistorial: number
  idPaciente: number
  fechaApertura: string
  antecedentes?: string
}

/** POST /api/historiales (Admin, Medico). Un historial por paciente. */
export interface CrearHistorialRequest {
  idPaciente: number
  fechaApertura: string
  antecedentes?: string
}

/** GET /api/citas?idPaciente=&idMedico= */
export interface CitaResponse {
  idCita: number
  idMedico: number
  idPaciente: number
  fechaCita: string
  hora: string
  motivo?: string
  estado?: string
}

/** POST /api/citas (Admin, Medico, Recepcion). */
export interface CrearCitaRequest {
  idMedico: number
  idPaciente: number
  fechaCita: string
  /** TimeOnly en formato HH:mm:ss. */
  hora: string
  motivo?: string
  estado?: string
}

/** PUT /api/citas/{id} (Admin, Medico, Recepcion). Medico y paciente no se modifican. */
export type ActualizarCitaRequest = Omit<CrearCitaRequest, 'idMedico' | 'idPaciente'>

/** GET /api/medicos. */
export interface MedicoResponse {
  idMedico: number
  nombre: string
  apellido: string
  colegiado: string
  especialidad: string
}

/** POST /api/medicos (solo Admin). */
export interface CrearMedicoRequest {
  nombre: string
  apellido: string
  colegiado: string
  especialidad: string
}

/** PUT /api/medicos/{id} (solo Admin). El colegiado no se modifica. */
export type ActualizarMedicoRequest = Omit<CrearMedicoRequest, 'colegiado'>

/** GET /api/estudios?idCita= */
export interface EstudioResponse {
  idEstudio: number
  idCita: number
  tipo?: string
  descripcion?: string
  fechaSolicitud: string
  estado?: string
}

/**
 * Forma de error RFC 7807 (ProblemDetails) que emite ASP.NET Core.
 * Algunos endpoints de auth devuelven en su lugar { error: string }.
 */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  /** Errores de validacion por campo (ValidationProblemDetails). */
  errors?: Record<string, string[]>
  /** Forma alternativa usada por AuthController: { error: "Credenciales invalidas." }. */
  error?: string
}
