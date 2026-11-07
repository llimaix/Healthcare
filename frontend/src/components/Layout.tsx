// Layout compartido de las pantallas protegidas: barra con la navegacion que permite
// el rol, correo y rol del usuario, y boton de cerrar sesion. Debajo, un aviso con lo
// que el rol puede hacer, para que la diferencia entre roles sea visible.
import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { DESCRIPCION_ROL, type Permisos } from '../auth/permisos'

interface LayoutProps {
  /** Titulo de la pantalla actual, mostrado en la barra. */
  titulo: string
  /** Contenido de la pantalla. */
  children: ReactNode
}

interface Enlace {
  ruta: string
  texto: string
  /** Si se indica, el enlace solo aparece para roles con ese permiso. */
  permiso?: keyof Permisos
}

const ENLACES: Enlace[] = [
  { ruta: '/pacientes', texto: 'Pacientes' },
  { ruta: '/citas', texto: 'Citas' },
  { ruta: '/diagnosticos', texto: 'Diagnosticos' },
  { ruta: '/resultados-tratantes', texto: 'Resultados tratantes' },
  { ruta: '/medicos', texto: 'Medicos' },
  { ruta: '/usuarios', texto: 'Usuarios', permiso: 'usuarios' },
]

export function Layout({ titulo, children }: LayoutProps) {
  const { usuario, permisos, cerrarSesion } = useAuth()

  return (
    <div className="pagina">
      <header className="barra">
        <strong className="barra-titulo">{titulo}</strong>
        <nav className="barra-nav">
          {ENLACES.filter((e) => !e.permiso || permisos[e.permiso]).map((e) => (
            <NavLink
              key={e.ruta}
              to={e.ruta}
              className={({ isActive }) => (isActive ? 'barra-link activo' : 'barra-link')}
            >
              {e.texto}
            </NavLink>
          ))}
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
      {usuario && (
        <div className="aviso-rol">
          <strong>Rol {usuario.rol}:</strong> {DESCRIPCION_ROL[usuario.rol]}
        </div>
      )}
      <main className="contenido">{children}</main>
    </div>
  )
}
