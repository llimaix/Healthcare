// Dialogo modal de confirmacion, usado antes de borrar. Minimalista y accesible.
interface ConfirmacionProps {
  /** Titulo del dialogo. */
  titulo: string
  /** Mensaje descriptivo de la accion a confirmar. */
  mensaje: string
  /** Texto del boton que confirma (por defecto "Confirmar"). */
  textoConfirmar?: string
  /** Si true, el boton de confirmar queda deshabilitado (p. ej. durante el borrado). */
  procesando?: boolean
  /** Se invoca al confirmar. */
  onConfirmar: () => void
  /** Se invoca al cancelar o cerrar. */
  onCancelar: () => void
}

export function Confirmacion({
  titulo,
  mensaje,
  textoConfirmar = 'Confirmar',
  procesando = false,
  onConfirmar,
  onCancelar,
}: ConfirmacionProps) {
  return (
    <div className="modal-fondo" role="presentation" onClick={onCancelar}>
      <div
        className="modal-tarjeta"
        role="dialog"
        aria-modal="true"
        aria-label={titulo}
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="modal-titulo">{titulo}</h2>
        <p className="modal-mensaje">{mensaje}</p>
        <div className="modal-acciones">
          <button
            className="boton-secundario"
            type="button"
            onClick={onCancelar}
            disabled={procesando}
          >
            Cancelar
          </button>
          <button
            className="boton-peligro"
            type="button"
            onClick={onConfirmar}
            disabled={procesando}
          >
            {procesando ? 'Procesando…' : textoConfirmar}
          </button>
        </div>
      </div>
    </div>
  )
}
