// API de Resultados tratantes (/api/resultados-tratantes). Escritura: Admin/Medico;
// borrado: solo Admin. El frontend habla SOLO con el Gateway; paths sin prefijo /api.
import { httpClient } from './client'
import type {
  ActualizarResultadoTratanteRequest,
  CrearResultadoTratanteRequest,
  ResultadoTratanteResponse,
} from '../types/contratos'

/** Construye un query string a partir de pares; omite valores null/undefined. */
function query(parametros: Record<string, number | undefined | null>): string {
  const partes: string[] = []
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null) {
      partes.push(`${encodeURIComponent(clave)}=${encodeURIComponent(String(valor))}`)
    }
  }
  return partes.length ? `?${partes.join('&')}` : ''
}

/** GET /api/resultados-tratantes?idDiagnostico=&idMedico=&idEstudio= */
export function listarResultados(filtros: {
  idDiagnostico?: number
  idMedico?: number
  idEstudio?: number
} = {}): Promise<ResultadoTratanteResponse[]> {
  return httpClient.get<ResultadoTratanteResponse[]>(`/resultados-tratantes${query(filtros)}`)
}

/** GET /api/resultados-tratantes/{id} */
export function obtenerResultado(id: number): Promise<ResultadoTratanteResponse> {
  return httpClient.get<ResultadoTratanteResponse>(`/resultados-tratantes/${id}`)
}

/** POST /api/resultados-tratantes (Admin, Medico). */
export function crearResultado(
  cuerpo: CrearResultadoTratanteRequest,
): Promise<ResultadoTratanteResponse> {
  return httpClient.post<ResultadoTratanteResponse>('/resultados-tratantes', cuerpo)
}

/** PUT /api/resultados-tratantes/{id} (Admin, Medico). */
export function actualizarResultado(
  id: number,
  cuerpo: ActualizarResultadoTratanteRequest,
): Promise<ResultadoTratanteResponse> {
  return httpClient.put<ResultadoTratanteResponse>(`/resultados-tratantes/${id}`, cuerpo)
}

/** DELETE /api/resultados-tratantes/{id} (solo Admin) -> 204. */
export function eliminarResultado(id: number): Promise<void> {
  return httpClient.delete<void>(`/resultados-tratantes/${id}`)
}
