// Guarda de ruta: redirige a /login si no hay sesion. Mientras se restaura la
// sesion desde localStorage muestra un indicador de carga para evitar un parpadeo
// que expulse a un usuario con token valido. Si se indica `permiso`, un usuario sin
// ese permiso vuelve a su pantalla de inicio.
import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './useAuth'
import { rutaInicio, type Permisos } from './permisos'

interface RutaProtegidaProps {
  children: ReactNode
  /** Permiso requerido para ver la ruta (opcional). */
  permiso?: keyof Permisos
}

export function RutaProtegida({ children, permiso }: RutaProtegidaProps) {
  const { token, cargando, usuario, permisos } = useAuth()

  if (cargando) {
    return <div style={{ padding: '2rem' }}>Cargando sesion…</div>
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (permiso && !permisos[permiso]) {
    return <Navigate to={rutaInicio(usuario?.rol)} replace />
  }

  return <>{children}</>
}
