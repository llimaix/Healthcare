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

/** GET /api/auth/me (Bearer). idMedico es null si el usuario no es un medico. */
export interface UsuarioResponse {
  idUsuario: number
  correo: string
  rol: Rol
  idMedico: number | null
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
  /** 'F' | 'M' en el backend (char). */
  genero?: string
}

/** GET /api/historiales?idPaciente= */
export interface HistorialResponse {
  idHistorial: number
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

/** GET /api/medicos. */
export interface MedicoResponse {
  idMedico: number
  nombre: string
  apellido: string
  colegiado: string
  especialidad: string
}

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
