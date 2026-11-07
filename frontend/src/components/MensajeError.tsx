// Muestra errores de red o del backend sin dejar la pantalla en blanco. Traduce el
// caso 403 a "permiso insuficiente". Acepta un ApiError, un Error generico o un texto.
import { ApiError } from '../api/client'

interface MensajeErrorProps {
  /** Error a mostrar: ApiError, Error generico o texto ya formateado. */
  error: unknown
  /** Callback opcional para reintentar la operacion que fallo. */
  onReintentar?: () => void
}

/** Convierte cualquier error en un texto legible para el usuario. */
function textoDeError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.esPermisoInsuficiente) {
      return 'Permiso insuficiente: tu rol no autoriza esta accion.'
    }
    if (error.status === 401) {
      return 'Tu sesion expiro. Vuelve a iniciar sesion.'
    }
    // Errores de validacion por campo del backend.
    if (error.errors) {
      const detalles = Object.values(error.errors).flat()
      if (detalles.length > 0) {
        return detalles.join(' ')
      }
    }
    return error.message
  }
  if (error instanceof Error) {
    return error.message || 'Ocurrio un error inesperado.'
  }
  if (typeof error === 'string') {
    return error
  }
  return 'No se pudo completar la operacion. Intenta de nuevo.'
}

export function MensajeError({ error, onReintentar }: MensajeErrorProps) {
  return (
    <div className="mensaje-error" role="alert">
      <span>{textoDeError(error)}</span>
      {onReintentar && (
        <button className="boton-secundario" type="button" onClick={onReintentar}>
          Reintentar
        </button>
      )}
    </div>
  )
}
