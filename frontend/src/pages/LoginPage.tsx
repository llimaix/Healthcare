// Pantalla de login: formulario con correo y password, estados de carga y error
// visibles. Al exito guarda el token, carga /me (rol) y navega a /diagnosticos.
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../api/client'

export function LoginPage() {
  const { iniciarSesion } = useAuth()
  const navigate = useNavigate()

  const [correo, setCorreo] = useState('')
  const [password, setPassword] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setError(null)
    setEnviando(true)
    try {
      await iniciarSesion(correo.trim(), password)
      navigate('/diagnosticos', { replace: true })
    } catch (e) {
      if (e instanceof ApiError) {
        setError(
          e.status === 401
            ? 'Credenciales invalidas. Revisa el correo y la contrasena.'
            : e.message,
        )
      } else {
        setError('No se pudo conectar con el servidor. Intenta de nuevo.')
      }
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="login-contenedor">
      <form className="login-tarjeta" onSubmit={manejarSubmit} noValidate>
        <h1 className="login-titulo">ClinicaCore EMR</h1>
        <p className="login-subtitulo">Inicia sesion para continuar</p>

        <label className="campo">
          <span>Correo</span>
          <input
            type="email"
            name="correo"
            autoComplete="username"
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            required
            disabled={enviando}
          />
        </label>

        <label className="campo">
          <span>Contrasena</span>
          <input
            type="password"
            name="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            disabled={enviando}
          />
        </label>

        {error && (
          <p className="login-error" role="alert">
            {error}
          </p>
        )}

        <button className="boton-principal" type="submit" disabled={enviando}>
          {enviando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  )
}
