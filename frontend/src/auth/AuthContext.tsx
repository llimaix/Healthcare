// Provider de autenticacion: mantiene token y usuario (con rol) en estado y en
// localStorage, restaura la sesion al arrancar, y conecta el gancho 401 del cliente
// HTTP (logout global). Expone helpers puedeEscribir / puedeBorrar por rol.
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { AuthContext, type AuthContextValue } from './authContextDef'
import { login as apiLogin, me as apiMe, logout as apiLogout } from '../api/auth'
import { setToken, setOnUnauthorized } from '../api/client'
import type { UsuarioResponse } from '../types/contratos'
import { permisosDe } from './permisos'

const CLAVE_TOKEN = 'clinicacore.token'
const CLAVE_USUARIO = 'clinicacore.usuario'

function leerUsuarioGuardado(): UsuarioResponse | null {
  const crudo = localStorage.getItem(CLAVE_USUARIO)
  if (!crudo) return null
  try {
    return JSON.parse(crudo) as UsuarioResponse
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenEstado] = useState<string | null>(() => localStorage.getItem(CLAVE_TOKEN))
  const [usuario, setUsuario] = useState<UsuarioResponse | null>(() => leerUsuarioGuardado())
  const [cargando, setCargando] = useState<boolean>(() => Boolean(localStorage.getItem(CLAVE_TOKEN)))

  const cerrarSesion = useCallback(() => {
    apiLogout()
    setTokenEstado(null)
    setUsuario(null)
    localStorage.removeItem(CLAVE_TOKEN)
    localStorage.removeItem(CLAVE_USUARIO)
  }, [])

  // Conecta el gancho global de 401 del cliente HTTP -> cerrar sesion.
  // El redirect a /login lo resuelve <RutaProtegida> al quedarse sin sesion.
  useEffect(() => {
    setOnUnauthorized(() => cerrarSesion())
    return () => setOnUnauthorized(null)
  }, [cerrarSesion])

  // Al arrancar con un token persistido, lo fija en el cliente y revalida con /me.
  useEffect(() => {
    const guardado = localStorage.getItem(CLAVE_TOKEN)
    if (!guardado) {
      setCargando(false)
      return
    }
    setToken(guardado)
    let activo = true
    apiMe()
      .then((u) => {
        if (!activo) return
        setUsuario(u)
        localStorage.setItem(CLAVE_USUARIO, JSON.stringify(u))
      })
      .catch(() => {
        // Token invalido/expirado: el gancho 401 ya habra limpiado la sesion.
        if (activo) cerrarSesion()
      })
      .finally(() => {
        if (activo) setCargando(false)
      })
    return () => {
      activo = false
    }
  }, [cerrarSesion])

  const iniciarSesion = useCallback(async (correo: string, password: string) => {
    const nuevoToken = await apiLogin({ correo, password })
    setTokenEstado(nuevoToken)
    localStorage.setItem(CLAVE_TOKEN, nuevoToken)
    const u = await apiMe()
    setUsuario(u)
    localStorage.setItem(CLAVE_USUARIO, JSON.stringify(u))
    return u
  }, [])

  const valor = useMemo<AuthContextValue>(() => {
    const permisos = permisosDe(usuario?.rol)
    return {
      token,
      usuario,
      cargando,
      iniciarSesion,
      cerrarSesion,
      permisos,
      puedeEscribir: permisos.clinicoEscribir,
      puedeBorrar: permisos.borrar,
    }
  }, [token, usuario, cargando, iniciarSesion, cerrarSesion])

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>
}
