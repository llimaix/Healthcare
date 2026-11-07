// Definicion del contexto de autenticacion (sin componentes, para que el lint de
// react-refresh no se queje de exports mixtos en el .tsx del provider).
import { createContext } from 'react'
import type { UsuarioResponse } from '../types/contratos'
import type { Permisos } from './permisos'

export interface AuthContextValue {
  /** Token Bearer actual (null si no hay sesion). */
  token: string | null
  /** Usuario autenticado (null mientras carga o sin sesion). */
  usuario: UsuarioResponse | null
  /** true mientras se restaura la sesion desde localStorage al arrancar. */
  cargando: boolean
  /** Inicia sesion: login + carga de /me. Devuelve el usuario. Lanza ApiError si falla. */
  iniciarSesion: (correo: string, password: string) => Promise<UsuarioResponse>
  /** Cierra sesion: limpia token y usuario de memoria y localStorage. */
  cerrarSesion: () => void
  /** Permisos de la UI segun el rol del usuario (ver auth/permisos.ts). */
  permisos: Permisos
  /** Atajo: crear/editar diagnosticos y resultados (Admin, Medico). */
  puedeEscribir: boolean
  /** Atajo: borrar registros (solo Admin). */
  puedeBorrar: boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)
