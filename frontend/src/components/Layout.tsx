// Layout compartido de las pantallas protegidas: barra con navegacion minima
// (Diagnosticos -> Resultados tratantes), correo y rol del usuario visibles, y
// boton de cerrar sesion. El contenido de cada pantalla se pasa como children.
import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

interface LayoutProps {
  /** Titulo de la pantalla actual, mostrado en la barra. */
  titulo: string
  /** Contenido de la pantalla. */
  children: ReactNode
}

export function Layout({ titulo, children }: LayoutProps) {
  const { usuario, cerrarSesion } = useAuth()

  return (
    <div className="pagina">
      <header className="barra">
        <strong className="barra-titulo">{titulo}</strong>
        <nav className="barra-nav">
          <NavLink
            to="/diagnosticos"
            className={({ isActive }) => (isActive ? 'barra-link activo' : 'barra-link')}
          >
            Diagnosticos
          </NavLink>
          <NavLink
            to="/resultados-tratantes"
            className={({ isActive }) => (isActive ? 'barra-link activo' : 'barra-link')}
          >
            Resultados tratantes
          </NavLink>
        </nav>
        <span className="barra-usuario">
          {usuario ? (
            <>
              {usuario.correo} <span className="barra-rol">{usuario.rol}</span>
            </>
          ) : (
            'Sin sesion'
          )}
        </span>
        <button className="boton-secundario" type="button" onClick={cerrarSesion}>
          Cerrar sesion
        </button>
      </header>
      <main className="contenido">{children}</main>
    </div>
  )
}
