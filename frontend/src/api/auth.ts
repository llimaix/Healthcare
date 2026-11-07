// Operaciones de autenticacion contra el Gateway (/api/auth/*, anonimo salvo /me).
import { httpClient, setToken } from './client'
import type { LoginRequest, LoginResponse, UsuarioResponse } from '../types/contratos'

/**
 * POST /api/auth/login. Envia {correo,password}, recibe {token} y lo fija como
 * Bearer para las siguientes peticiones. Devuelve el token.
 */
export async function login(credenciales: LoginRequest): Promise<string> {
  const respuesta = await httpClient.post<LoginResponse>('/auth/login', credenciales)
  setToken(respuesta.token)
  return respuesta.token
}

/**
 * GET /api/auth/me. Devuelve el usuario autenticado (incluye el campo `rol`).
 * Requiere que el token ya este fijado (via login o setToken).
 */
export function me(): Promise<UsuarioResponse> {
  return httpClient.get<UsuarioResponse>('/auth/me')
}

/** Limpia el token en memoria. El borrado de localStorage lo hace el AuthContext. */
export function logout(): void {
  setToken(null)
}
