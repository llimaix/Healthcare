// Indicador de carga reutilizable. Evita pantallas en blanco mientras se resuelven
// las peticiones al Gateway.

interface EstadoCargaProps {
  /** Texto a mostrar junto al indicador. */
  mensaje?: string
}

export function EstadoCarga({ mensaje = 'Cargando…' }: EstadoCargaProps) {
  return (
    <div className="estado-carga" role="status" aria-live="polite">
      <span className="estado-carga-spinner" aria-hidden="true" />
      <span>{mensaje}</span>
    </div>
  )
}
