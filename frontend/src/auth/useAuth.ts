// Hook de acceso al contexto de autenticacion.
import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from './authContextDef'

/** Devuelve el valor del AuthContext. Lanza si se usa fuera del AuthProvider. */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth debe usarse dentro de <AuthProvider>.')
  }
  return ctx
}
