// Definicion del contexto de autenticacion (sin componentes, para que el lint de
// react-refresh no se queje de exports mixtos en el .tsx del provider).
import { createContext } from 'react'
import type { UsuarioResponse } from '../types/contratos'

export interface AuthContextValue {
  /** Token Bearer actual (null si no hay sesion). */
  token: string | null
  /** Usuario autenticado (null mientras carga o sin sesion). */
  usuario: UsuarioResponse | null
  /** true mientras se restaura la sesion desde localStorage al arrancar. */
  cargando: boolean
  /** Inicia sesion: login + carga de /me. Lanza ApiError si falla. */
  iniciarSesion: (correo: string, password: string) => Promise<void>
  /** Cierra sesion: limpia token y usuario de memoria y localStorage. */
  cerrarSesion: () => void
  /** Permiso de escritura (crear/editar diagnosticos y resultados): rol in {Admin, Medico}. */
  puedeEscribir: boolean
  /** Permiso de borrado: rol === 'Admin'. */
  puedeBorrar: boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)
