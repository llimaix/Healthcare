// API de gestion: pacientes, medicos, citas, historiales y usuarios.
// Los permisos los aplica el backend (403); la UI solo oculta lo que el rol no puede usar.
// Las lecturas (listar) estan en referencias.ts; aqui van las escrituras.
import { httpClient } from './client'
import type {
  ActualizarCitaRequest,
  ActualizarMedicoRequest,
  ActualizarPacienteRequest,
  ActualizarUsuarioRequest,
  CitaResponse,
  CrearCitaRequest,
  CrearHistorialRequest,
  CrearMedicoRequest,
  CrearPacienteRequest,
  HistorialResponse,
  MedicoResponse,
  PacienteResponse,
  RegistroRequest,
  UsuarioResponse,
} from '../types/contratos'

// --- Pacientes (escritura: Admin, Medico, Recepcion; borrado logico: Admin) ---

export function crearPaciente(cuerpo: CrearPacienteRequest): Promise<PacienteResponse> {
  return httpClient.post<PacienteResponse>('/pacientes', cuerpo)
}

export function actualizarPaciente(
  id: number,
  cuerpo: ActualizarPacienteRequest,
): Promise<PacienteResponse> {
  return httpClient.put<PacienteResponse>(`/pacientes/${id}`, cuerpo)
}

export function eliminarPaciente(id: number): Promise<void> {
  return httpClient.delete<void>(`/pacientes/${id}`)
}

// --- Historial clinico (escritura: Admin, Medico) ---

export function crearHistorial(cuerpo: CrearHistorialRequest): Promise<HistorialResponse> {
  return httpClient.post<HistorialResponse>('/historiales', cuerpo)
}

// --- Medicos (escritura: solo Admin) ---

export function crearMedico(cuerpo: CrearMedicoRequest): Promise<MedicoResponse> {
  return httpClient.post<MedicoResponse>('/medicos', cuerpo)
}

export function actualizarMedico(
  id: number,
  cuerpo: ActualizarMedicoRequest,
): Promise<MedicoResponse> {
  return httpClient.put<MedicoResponse>(`/medicos/${id}`, cuerpo)
}

// --- Citas (escritura: Admin, Medico, Recepcion; borrado: Admin) ---

export function crearCita(cuerpo: CrearCitaRequest): Promise<CitaResponse> {
  return httpClient.post<CitaResponse>('/citas', cuerpo)
}

export function actualizarCita(id: number, cuerpo: ActualizarCitaRequest): Promise<CitaResponse> {
  return httpClient.put<CitaResponse>(`/citas/${id}`, cuerpo)
}

export function eliminarCita(id: number): Promise<void> {
  return httpClient.delete<void>(`/citas/${id}`)
}

// --- Usuarios (solo Admin) ---

export function listarUsuarios(): Promise<UsuarioResponse[]> {
  return httpClient.get<UsuarioResponse[]>('/usuarios')
}

/** POST /api/auth/register con el token del Admin. */
export function registrarUsuario(cuerpo: RegistroRequest): Promise<UsuarioResponse> {
  return httpClient.post<UsuarioResponse>('/auth/register', cuerpo)
}

export function actualizarUsuario(
  id: number,
  cuerpo: ActualizarUsuarioRequest,
): Promise<UsuarioResponse> {
  return httpClient.put<UsuarioResponse>(`/usuarios/${id}`, cuerpo)
}
