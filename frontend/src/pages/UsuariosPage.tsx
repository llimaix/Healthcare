// Pantalla de Usuarios (solo Admin): listar, crear usuarios con su rol, cambiar el
// rol, vincular un medico y activar/desactivar cuentas. Un usuario inactivo no puede
// iniciar sesion. El Admin no puede quitarse su propio rol ni desactivarse.
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { useAuth } from '../auth/useAuth'
import { listarMedicos } from '../api/referencias'
import { actualizarUsuario, listarUsuarios, registrarUsuario } from '../api/gestion'
import type { MedicoResponse, Rol, UsuarioResponse } from '../types/contratos'

const ROLES: Rol[] = ['Admin', 'Medico', 'Recepcion', 'Lectura']

interface FormularioUsuario {
  correo: string
  password: string
  rol: Rol
  idMedico: string
  activo: boolean
}

const FORM_VACIO: FormularioUsuario = {
  correo: '',
  password: '',
  rol: 'Lectura',
  idMedico: '',
  activo: true,
}

export function UsuariosPage() {
  const { usuario: yo } = useAuth()

  const [usuarios, setUsuarios] = useState<UsuarioResponse[]>([])
  const [medicos, setMedicos] = useState<MedicoResponse[]>([])
  const [cargando, setCargando] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)
  const [aviso, setAviso] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [editando, setEditando] = useState<UsuarioResponse | null>(null)
  const [form, setForm] = useState<FormularioUsuario>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  const cargar = useCallback(async () => {
    setCargando(true)
    setErrorTabla(null)
    try {
      const [u, m] = await Promise.all([listarUsuarios(), listarMedicos()])
      setUsuarios(u)
      setMedicos(m)
    } catch (e) {
      setErrorTabla(e)
    } finally {
      setCargando(false)
    }
  }, [])

  useEffect(() => {
    void cargar()
  }, [cargar])

  const nombreMedico = useMemo(() => {
    const mapa = new Map(medicos.map((m) => [m.idMedico, `${m.nombre} ${m.apellido}`]))
    return (id: number | null) => (id == null ? '—' : (mapa.get(id) ?? `Medico #${id}`))
  }, [medicos])

  function abrirNuevo() {
    setEditando(null)
    setForm(FORM_VACIO)
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function abrirEdicion(u: UsuarioResponse) {
    setEditando(u)
    setForm({
      correo: u.correo,
      password: '',
      rol: u.rol,
      idMedico: u.idMedico == null ? '' : String(u.idMedico),
      activo: u.activo ?? true,
    })
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function validar(): Record<string, string> {
    const e: Record<string, string> = {}
    if (editando === null) {
      if (!/^\S+@\S+\.\S+$/.test(form.correo.trim())) e.correo = 'Ingresa un correo valido.'
      if (form.password.length < 8) e.password = 'La contrasena debe tener al menos 8 caracteres.'
    }
    return e
  }

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setErrorForm(null)
    const errores = validar()
    setErroresCampo(errores)
    if (Object.keys(errores).length > 0) return

    const idMedico = form.rol === 'Medico' && form.idMedico ? Number(form.idMedico) : null

    setGuardando(true)
    try {
      if (editando === null) {
        const creado = await registrarUsuario({
          correo: form.correo.trim(),
          password: form.password,
          rol: form.rol,
          idMedico,
        })
        setAviso(`Usuario ${creado.correo} creado con rol ${creado.rol}.`)
      } else {
        await actualizarUsuario(editando.idUsuario, {
          rol: form.rol,
          activo: form.activo,
          idMedico,
        })
        setAviso(`Usuario ${editando.correo} actualizado.`)
      }
      setMostrarForm(false)
      setEditando(null)
      await cargar()
    } catch (e) {
      setErrorForm(e)
    } finally {
      setGuardando(false)
    }
  }

  async function alternarActivo(u: UsuarioResponse) {
    setErrorTabla(null)
    setAviso(null)
    try {
      const nuevo = !(u.activo ?? true)
      await actualizarUsuario(u.idUsuario, { rol: u.rol, activo: nuevo, idMedico: u.idMedico })
      setAviso(`Usuario ${u.correo} ${nuevo ? 'activado' : 'desactivado'}.`)
      await cargar()
    } catch (e) {
      setErrorTabla(e)
    }
  }

  const esYo = (u: UsuarioResponse) => yo?.idUsuario === u.idUsuario

  return (
    <Layout titulo="Usuarios">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Usuarios</h1>
          <div className="panel-acciones">
            <button className="boton-principal" type="button" onClick={abrirNuevo}>
              Crear usuario
            </button>
          </div>
        </div>

        {aviso && <p className="aviso-exito">{aviso}</p>}
        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargar} />}

        {cargando ? (
          <EstadoCarga mensaje="Cargando usuarios…" />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Correo</th>
                  <th>Rol</th>
                  <th>Medico vinculado</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {usuarios.map((u) => (
                  <tr key={u.idUsuario}>
                    <td>{u.idUsuario}</td>
                    <td>
                      {u.correo} {esYo(u) && <span className="nota">(tú)</span>}
                    </td>
                    <td>
                      <span className="barra-rol">{u.rol}</span>
                    </td>
                    <td>{nombreMedico(u.idMedico)}</td>
                    <td>
                      {u.activo ?? true ? (
                        <span className="etiqueta etiqueta-ok">Activo</span>
                      ) : (
                        <span className="etiqueta etiqueta-off">Inactivo</span>
                      )}
                    </td>
                    <td className="celda-acciones">
                      <button
                        className="boton-secundario"
                        type="button"
                        onClick={() => abrirEdicion(u)}
                      >
                        Editar
                      </button>
                      {!esYo(u) && (
                        <button
                          className={(u.activo ?? true) ? 'boton-peligro' : 'boton-secundario'}
                          type="button"
                          onClick={() => void alternarActivo(u)}
                        >
                          {(u.activo ?? true) ? 'Desactivar' : 'Activar'}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {mostrarForm && (
        <section className="panel form-panel">
          <h2 className="panel-titulo">
            {editando === null ? 'Crear usuario' : `Editar usuario ${editando.correo}`}
          </h2>
          <form className="formulario" onSubmit={manejarSubmit} noValidate>
            <label className="campo">
              <span>Correo</span>
              <input
                type="email"
                value={form.correo}
                disabled={editando !== null}
                onChange={(e) => setForm((f) => ({ ...f, correo: e.target.value }))}
              />
              {erroresCampo.correo && <small className="campo-error">{erroresCampo.correo}</small>}
            </label>
            {editando === null && (
              <label className="campo">
                <span>Contrasena inicial</span>
                <input
                  type="password"
                  autoComplete="new-password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                />
                {erroresCampo.password && (
                  <small className="campo-error">{erroresCampo.password}</small>
                )}
              </label>
            )}
            <label className="campo">
              <span>Rol</span>
              <select
                value={form.rol}
                disabled={editando !== null && esYo(editando)}
                onChange={(e) => setForm((f) => ({ ...f, rol: e.target.value as Rol }))}
              >
                {ROLES.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </label>
            {form.rol === 'Medico' && (
              <label className="campo">
                <span>Medico vinculado (opcional)</span>
                <select
                  value={form.idMedico}
                  onChange={(e) => setForm((f) => ({ ...f, idMedico: e.target.value }))}
                >
                  <option value="">Sin vincular</option>
                  {medicos.map((m) => (
                    <option key={m.idMedico} value={m.idMedico}>
                      {m.nombre} {m.apellido} — {m.especialidad}
                    </option>
                  ))}
                </select>
              </label>
            )}
            {editando !== null && !esYo(editando) && (
              <label className="campo campo-check">
                <input
                  type="checkbox"
                  checked={form.activo}
                  onChange={(e) => setForm((f) => ({ ...f, activo: e.target.checked }))}
                />
                <span>Cuenta activa (si se desactiva, no puede iniciar sesion)</span>
              </label>
            )}
            {editando !== null && esYo(editando) && (
              <p className="nota campo-ancho">
                No puedes cambiar tu propio rol ni desactivar tu cuenta.
              </p>
            )}

            {errorForm != null && (
              <div className="campo-ancho">
                <MensajeError error={errorForm} />
              </div>
            )}

            <div className="formulario-acciones">
              <button
                className="boton-secundario"
                type="button"
                onClick={() => setMostrarForm(false)}
                disabled={guardando}
              >
                Cancelar
              </button>
              <button className="boton-principal" type="submit" disabled={guardando}>
                {guardando ? 'Guardando…' : 'Guardar'}
              </button>
            </div>
          </form>
        </section>
      )}
    </Layout>
  )
}
