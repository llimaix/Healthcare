// Pantalla de Medicos: lectura para cualquier rol; registrar y editar solo Admin
// (el backend no expone borrado de medicos).
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { useAuth } from '../auth/useAuth'
import { listarMedicos } from '../api/referencias'
import { actualizarMedico, crearMedico } from '../api/gestion'
import type { MedicoResponse } from '../types/contratos'

interface FormularioMedico {
  nombre: string
  apellido: string
  colegiado: string
  especialidad: string
}

const FORM_VACIO: FormularioMedico = { nombre: '', apellido: '', colegiado: '', especialidad: '' }

export function MedicosPage() {
  const { permisos } = useAuth()

  const [medicos, setMedicos] = useState<MedicoResponse[]>([])
  const [cargando, setCargando] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)
  const [aviso, setAviso] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormularioMedico>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  const cargar = useCallback(async () => {
    setCargando(true)
    setErrorTabla(null)
    try {
      setMedicos(await listarMedicos())
    } catch (e) {
      setErrorTabla(e)
    } finally {
      setCargando(false)
    }
  }, [])

  useEffect(() => {
    void cargar()
  }, [cargar])

  function abrirNuevo() {
    setEditandoId(null)
    setForm(FORM_VACIO)
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function abrirEdicion(m: MedicoResponse) {
    setEditandoId(m.idMedico)
    setForm({
      nombre: m.nombre,
      apellido: m.apellido,
      colegiado: m.colegiado,
      especialidad: m.especialidad,
    })
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function validar(): Record<string, string> {
    const e: Record<string, string> = {}
    if (!form.nombre.trim()) e.nombre = 'El nombre es obligatorio.'
    if (!form.apellido.trim()) e.apellido = 'El apellido es obligatorio.'
    if (editandoId === null && !form.colegiado.trim()) e.colegiado = 'El colegiado es obligatorio.'
    if (!form.especialidad.trim()) e.especialidad = 'La especialidad es obligatoria.'
    return e
  }

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setErrorForm(null)
    const errores = validar()
    setErroresCampo(errores)
    if (Object.keys(errores).length > 0) return

    setGuardando(true)
    try {
      const datos = {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        especialidad: form.especialidad.trim(),
      }
      if (editandoId === null) {
        const creado = await crearMedico({ ...datos, colegiado: form.colegiado.trim() })
        setAviso(`Medico ${creado.nombre} ${creado.apellido} registrado.`)
      } else {
        await actualizarMedico(editandoId, datos)
        setAviso('Medico actualizado.')
      }
      setMostrarForm(false)
      setEditandoId(null)
      await cargar()
    } catch (e) {
      setErrorForm(e)
    } finally {
      setGuardando(false)
    }
  }

  const campo = (clave: keyof FormularioMedico) => (valor: string) =>
    setForm((f) => ({ ...f, [clave]: valor }))

  return (
    <Layout titulo="Medicos">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Medicos</h1>
          <div className="panel-acciones">
            {permisos.medicosEscribir && (
              <button className="boton-principal" type="button" onClick={abrirNuevo}>
                Registrar medico
              </button>
            )}
          </div>
        </div>

        {aviso && <p className="aviso-exito">{aviso}</p>}
        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargar} />}

        {cargando ? (
          <EstadoCarga mensaje="Cargando medicos…" />
        ) : medicos.length === 0 ? (
          <p className="vacio">No hay medicos registrados.</p>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nombre</th>
                  <th>Colegiado</th>
                  <th>Especialidad</th>
                  {permisos.medicosEscribir && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {medicos.map((m) => (
                  <tr key={m.idMedico}>
                    <td>{m.idMedico}</td>
                    <td>
                      {m.nombre} {m.apellido}
                    </td>
                    <td>{m.colegiado}</td>
                    <td>{m.especialidad}</td>
                    {permisos.medicosEscribir && (
                      <td className="celda-acciones">
                        <button
                          className="boton-secundario"
                          type="button"
                          onClick={() => abrirEdicion(m)}
                        >
                          Editar
                        </button>
                      </td>
                    )}
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
            {editandoId === null ? 'Registrar medico' : `Editar medico #${editandoId}`}
          </h2>
          <form className="formulario" onSubmit={manejarSubmit} noValidate>
            <label className="campo">
              <span>Nombre</span>
              <input value={form.nombre} onChange={(e) => campo('nombre')(e.target.value)} />
              {erroresCampo.nombre && <small className="campo-error">{erroresCampo.nombre}</small>}
            </label>
            <label className="campo">
              <span>Apellido</span>
              <input value={form.apellido} onChange={(e) => campo('apellido')(e.target.value)} />
              {erroresCampo.apellido && (
                <small className="campo-error">{erroresCampo.apellido}</small>
              )}
            </label>
            <label className="campo">
              <span>Numero de colegiado</span>
              <input
                value={form.colegiado}
                maxLength={30}
                disabled={editandoId !== null}
                onChange={(e) => campo('colegiado')(e.target.value)}
              />
              {erroresCampo.colegiado && (
                <small className="campo-error">{erroresCampo.colegiado}</small>
              )}
            </label>
            <label className="campo">
              <span>Especialidad</span>
              <input
                value={form.especialidad}
                onChange={(e) => campo('especialidad')(e.target.value)}
              />
              {erroresCampo.especialidad && (
                <small className="campo-error">{erroresCampo.especialidad}</small>
              )}
            </label>

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
