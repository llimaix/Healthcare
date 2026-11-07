// Router de la aplicacion: /login publico y rutas protegidas /diagnosticos y
// /resultados-tratantes. El gancho de 401 del cliente HTTP se cablea en el
// AuthProvider (logout global); RutaProtegida se encarga del redirect a /login.
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RutaProtegida } from './auth/RutaProtegida'
import { LoginPage } from './pages/LoginPage'
import { DiagnosticosPage } from './pages/DiagnosticosPage'
import { ResultadosPage } from './pages/ResultadosPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/diagnosticos"
            element={
              <RutaProtegida>
                <DiagnosticosPage />
              </RutaProtegida>
            }
          />
          <Route
            path="/resultados-tratantes"
            element={
              <RutaProtegida>
                <ResultadosPage />
              </RutaProtegida>
            }
          />
          <Route path="/" element={<Navigate to="/diagnosticos" replace />} />
          <Route path="*" element={<Navigate to="/diagnosticos" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
