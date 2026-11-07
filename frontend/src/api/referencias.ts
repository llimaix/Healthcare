// API de referencias (solo lectura) para poblar los selectores de los formularios
// prioritarios. El frontend habla SOLO con el Gateway; estos GET son accesibles a
// cualquier usuario autenticado. Los paths van sin el prefijo /api (la base ya es /api).
import { httpClient } from './client'
import type {
  CitaResponse,
  EstudioResponse,
  HistorialResponse,
  MedicoResponse,
  PacienteResponse,
} from '../types/contratos'

/** Construye un query string a partir de pares; omite valores null/undefined. */
function query(parametros: Record<string, number | string | undefined | null>): string {
  const partes: string[] = []
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null && valor !== '') {
      partes.push(`${encodeURIComponent(clave)}=${encodeURIComponent(String(valor))}`)
    }
  }
  return partes.length ? `?${partes.join('&')}` : ''
}

/** GET /api/pacientes. */
export function listarPacientes(): Promise<PacienteResponse[]> {
  return httpClient.get<PacienteResponse[]>('/pacientes')
}

/** GET /api/historiales?idPaciente= */
export function listarHistoriales(idPaciente?: number): Promise<HistorialResponse[]> {
  return httpClient.get<HistorialResponse[]>(`/historiales${query({ idPaciente })}`)
}

/** GET /api/citas?idPaciente=&idMedico= */
export function listarCitas(filtros: {
  idPaciente?: number
  idMedico?: number
} = {}): Promise<CitaResponse[]> {
  return httpClient.get<CitaResponse[]>(`/citas${query(filtros)}`)
}

/** GET /api/medicos. */
export function listarMedicos(): Promise<MedicoResponse[]> {
  return httpClient.get<MedicoResponse[]>('/medicos')
}

/** GET /api/estudios?idCita= */
export function listarEstudios(idCita?: number): Promise<EstudioResponse[]> {
  return httpClient.get<EstudioResponse[]>(`/estudios${query({ idCita })}`)
}
