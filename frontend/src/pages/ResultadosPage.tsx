// Placeholder protegido de Resultados tratantes. La pantalla completa la implementa
// FEAT-003; aqui solo se deja la ruta protegida con navegacion y cierre de sesion.
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function ResultadosPage() {
  const { usuario, cerrarSesion } = useAuth()

  return (
    <div className="pagina">
      <header className="barra">
        <strong>Resultados tratantes</strong>
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
        <p>Pantalla de resultados tratantes (pendiente en FEAT-003).</p>
      </main>
    </div>
  )
}
