// Cliente HTTP minimo sobre fetch para hablar con el Gateway de ClinicaCore.
// - Lee la URL base de import.meta.env.VITE_GATEWAY_URL (default /api via proxy de Vite).
// - Adjunta Authorization: Bearer <token> cuando hay token.
// - Serializa/parsea JSON e interpreta ProblemDetails de error.
// - Expone errores tipados (ApiError) con status, title y detail.
// - Ganchos: un 401 dispara el gancho onUnauthorized (logout + redirect a /login);
//   un 403 produce un ApiError de 'permiso insuficiente' sin romper la UI.

import type { ProblemDetails } from '../types/contratos'

/** Base del Gateway. En dev es /api (proxy de Vite). */
const BASE_URL = import.meta.env.VITE_GATEWAY_URL ?? '/api'

/** Error tipado de la API. Siempre lleva un status HTTP y un mensaje legible. */
export class ApiError extends Error {
  readonly status: number
  readonly title?: string
  readonly detail?: string
  /** Errores de validacion por campo, si el backend los envio. */
  readonly errors?: Record<string, string[]>

  constructor(
    status: number,
    message: string,
    options?: { title?: string; detail?: string; errors?: Record<string, string[]> },
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = options?.title
    this.detail = options?.detail
    this.errors = options?.errors
  }

  /** true si el error es un 403 (permiso insuficiente). */
  get esPermisoInsuficiente(): boolean {
    return this.status === 403
  }
}

// --- Estado del token y ganchos globales ---

let tokenActual: string | null = null
let onUnauthorized: (() => void) | null = null

/** Fija (o limpia con null) el token Bearer que se adjunta a cada peticion. */
export function setToken(token: string | null): void {
  tokenActual = token
}

/** Devuelve el token Bearer en memoria (o null). */
export function getToken(): string | null {
  return tokenActual
}

/**
 * Registra el gancho que se dispara ante un 401 (token ausente/expirado).
 * Tipicamente: logout + redirect a /login. Se invoca ANTES de lanzar el ApiError.
 */
export function setOnUnauthorized(handler: (() => void) | null): void {
  onUnauthorized = handler
}

interface RequestOptions {
  /** Cuerpo a serializar como JSON (objetos) o a enviar tal cual. */
  body?: unknown
  /** Cabeceras adicionales. */
  headers?: Record<string, string>
  /** AbortSignal opcional. */
  signal?: AbortSignal
}

type Metodo = 'GET' | 'POST' | 'PUT' | 'DELETE'

/** Construye la URL final combinando la base con el path (que ya debe empezar por /). */
function construirUrl(path: string): string {
  const base = BASE_URL.replace(/\/$/, '')
  const sufijo = path.startsWith('/') ? path : `/${path}`
  return `${base}${sufijo}`
}

/** Intenta extraer un ProblemDetails (o { error }) del cuerpo de la respuesta. */
async function parsearError(respuesta: Response): Promise<ApiError> {
  let problema: ProblemDetails | null = null
  try {
    const texto = await respuesta.text()
    if (texto) {
      problema = JSON.parse(texto) as ProblemDetails
    }
  } catch {
    // Cuerpo no JSON o vacio: se ignora y se usa el status.
  }

  const detail = problema?.detail
  const title = problema?.title
  // AuthController usa { error: "..." }; ProblemDetails usa title/detail.
  const mensaje =
    problema?.error ??
    detail ??
    title ??
    `Error ${respuesta.status} ${respuesta.statusText}`.trim()

  return new ApiError(respuesta.status, mensaje, {
    title,
    detail,
    errors: problema?.errors,
  })
}

/** Nucleo de la peticion: adjunta Bearer, maneja 401/403 y parsea JSON. */
async function request<T>(metodo: Metodo, path: string, opciones: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { ...opciones.headers }

  let cuerpo: BodyInit | undefined
  if (opciones.body !== undefined) {
    headers['Content-Type'] = 'application/json'
    cuerpo = JSON.stringify(opciones.body)
  }

  if (tokenActual) {
    headers['Authorization'] = `Bearer ${tokenActual}`
  }

  const respuesta = await fetch(construirUrl(path), {
    method: metodo,
    headers,
    body: cuerpo,
    signal: opciones.signal,
  })

  if (!respuesta.ok) {
    // 401: token ausente/expirado. Dispara el gancho global (logout + redirect) y lanza.
    if (respuesta.status === 401) {
      onUnauthorized?.()
    }
    const error = await parsearError(respuesta)
    // 403: la UI debe mostrar "permiso insuficiente" sin romperse; el error ya lo indica.
    if (respuesta.status === 403 && !error.detail && !error.title) {
      throw new ApiError(403, 'Permiso insuficiente para realizar esta accion.', {
        title: error.title,
        detail: error.detail,
        errors: error.errors,
      })
    }
    throw error
  }

  // 204 No Content (p. ej. DELETE) o cuerpo vacio.
  if (respuesta.status === 204) {
    return undefined as T
  }
  const texto = await respuesta.text()
  if (!texto) {
    return undefined as T
  }
  return JSON.parse(texto) as T
}

/** Cliente HTTP tipado contra el Gateway. */
export const httpClient = {
  get: <T>(path: string, opciones?: RequestOptions) => request<T>('GET', path, opciones),
  post: <T>(path: string, body?: unknown, opciones?: RequestOptions) =>
    request<T>('POST', path, { ...opciones, body }),
  put: <T>(path: string, body?: unknown, opciones?: RequestOptions) =>
    request<T>('PUT', path, { ...opciones, body }),
  delete: <T>(path: string, opciones?: RequestOptions) => request<T>('DELETE', path, opciones),
}
