// API de Diagnosticos (/api/diagnosticos). Escritura: Admin/Medico; borrado: solo Admin.
// El frontend habla SOLO con el Gateway; los paths van sin el prefijo /api.
import { httpClient } from './client'
import type {
  ActualizarDiagnosticoRequest,
  CrearDiagnosticoRequest,
  DiagnosticoResponse,
} from '../types/contratos'

/** Construye un query string a partir de pares; omite valores null/undefined/''. */
function query(parametros: Record<string, number | undefined | null>): string {
  const partes: string[] = []
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null) {
      partes.push(`${encodeURIComponent(clave)}=${encodeURIComponent(String(valor))}`)
    }
  }
  return partes.length ? `?${partes.join('&')}` : ''
}

/** GET /api/diagnosticos?idPaciente=&idHistorial=&idCita= */
export function listarDiagnosticos(filtros: {
  idPaciente?: number
  idHistorial?: number
  idCita?: number
} = {}): Promise<DiagnosticoResponse[]> {
  return httpClient.get<DiagnosticoResponse[]>(`/diagnosticos${query(filtros)}`)
}

/** GET /api/diagnosticos/{id} */
export function obtenerDiagnostico(id: number): Promise<DiagnosticoResponse> {
  return httpClient.get<DiagnosticoResponse>(`/diagnosticos/${id}`)
}

/** POST /api/diagnosticos (Admin, Medico). */
export function crearDiagnostico(
  cuerpo: CrearDiagnosticoRequest,
): Promise<DiagnosticoResponse> {
  return httpClient.post<DiagnosticoResponse>('/diagnosticos', cuerpo)
}

/** PUT /api/diagnosticos/{id} (Admin, Medico). */
export function actualizarDiagnostico(
  id: number,
  cuerpo: ActualizarDiagnosticoRequest,
): Promise<DiagnosticoResponse> {
  return httpClient.put<DiagnosticoResponse>(`/diagnosticos/${id}`, cuerpo)
}

/** DELETE /api/diagnosticos/{id} (solo Admin) -> 204. */
export function eliminarDiagnostico(id: number): Promise<void> {
  return httpClient.delete<void>(`/diagnosticos/${id}`)
}
