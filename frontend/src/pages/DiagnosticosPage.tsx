// Placeholder protegido de Diagnosticos. La pantalla completa (lista + formulario
// crear/editar/borrar) la implementa FEAT-002; aqui solo se deja la ruta protegida
// funcionando con la navegacion y el cierre de sesion.
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function DiagnosticosPage() {
  const { usuario, cerrarSesion } = useAuth()

  return (
    <div className="pagina">
      <header className="barra">
        <strong>Diagnosticos</strong>
        <nav className="barra-nav">
          <Link to="/diagnosticos">Diagnosticos</Link>
          <Link to="/resultados-tratantes">Resultados tratantes</Link>
        </nav>
        <span className="barra-usuario">
          {usuario?.correo} ({usuario?.rol})
        </span>
        <button className="boton-secundario" type="button" onClick={cerrarSesion}>
          Cerrar sesion
        </button>
      </header>
      <main className="contenido">
        <p>Pantalla de diagnosticos (pendiente en FEAT-002).</p>
      </main>
    </div>
  )
}
