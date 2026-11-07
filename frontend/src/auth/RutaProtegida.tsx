// Guarda de ruta: redirige a /login si no hay sesion. Mientras se restaura la
// sesion desde localStorage muestra un indicador de carga para evitar un parpadeo
// que expulse a un usuario con token valido.
import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './useAuth'

export function RutaProtegida({ children }: { children: ReactNode }) {
  const { token, cargando } = useAuth()

  if (cargando) {
    return <div style={{ padding: '2rem' }}>Cargando sesion…</div>
  }

  if (!token) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}
