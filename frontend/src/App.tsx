// Router de la aplicacion: /login publico y rutas protegidas por sesion. /usuarios
// exige el permiso de gestion de usuarios (solo Admin). El gancho de 401 del cliente
// HTTP se cablea en el AuthProvider; RutaProtegida resuelve los redirects.
import type { ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RutaProtegida } from './auth/RutaProtegida'
import { useAuth } from './auth/useAuth'
import { rutaInicio, type Permisos } from './auth/permisos'
import { LoginPage } from './pages/LoginPage'
import { DiagnosticosPage } from './pages/DiagnosticosPage'
import { ResultadosPage } from './pages/ResultadosPage'
import { PacientesPage } from './pages/PacientesPage'
import { CitasPage } from './pages/CitasPage'
import { MedicosPage } from './pages/MedicosPage'
import { UsuariosPage } from './pages/UsuariosPage'

/** Redirige a la pantalla inicial del rol (o a /login sin sesion). */
function Inicio() {
  const { usuario, token, cargando } = useAuth()
  if (cargando) return null
  if (!token) return <Navigate to="/login" replace />
  return <Navigate to={rutaInicio(usuario?.rol)} replace />
}

function protegida(elemento: ReactNode, permiso?: keyof Permisos) {
  return <RutaProtegida permiso={permiso}>{elemento}</RutaProtegida>
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/pacientes" element={protegida(<PacientesPage />)} />
          <Route path="/citas" element={protegida(<CitasPage />)} />
          <Route path="/diagnosticos" element={protegida(<DiagnosticosPage />)} />
          <Route path="/resultados-tratantes" element={protegida(<ResultadosPage />)} />
          <Route path="/medicos" element={protegida(<MedicosPage />)} />
          <Route path="/usuarios" element={protegida(<UsuariosPage />, 'usuarios')} />
          <Route path="*" element={<Inicio />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
