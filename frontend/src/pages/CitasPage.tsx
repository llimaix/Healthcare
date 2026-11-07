// Pantalla de Citas: lectura para cualquier rol; agendar y editar para Admin, Medico
// y Recepcion; eliminar solo Admin. Una cita es requisito para registrar diagnosticos.
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { Confirmacion } from '../components/Confirmacion'
import { useAuth } from '../auth/useAuth'
import { listarCitas, listarMedicos, listarPacientes } from '../api/referencias'
import { actualizarCita, crearCita, eliminarCita } from '../api/gestion'
import type { CitaResponse, MedicoResponse, PacienteResponse } from '../types/contratos'

interface FormularioCita {
  idPaciente: string
  idMedico: string
  fechaCita: string
  hora: string
  motivo: string
  estado: string
}

const FORM_VACIO: FormularioCita = {
  idPaciente: '',
  idMedico: '',
  fechaCita: '',
  hora: '',
  motivo: '',
  estado: 'Programada',
}

const ESTADOS = ['Programada', 'Atendida', 'Cancelada']

/** El backend espera TimeOnly como HH:mm:ss; el input time entrega HH:mm. */
function aHoraCompleta(hora: string): string {
  return hora.length === 5 ? `${hora}:00` : hora
}

export function CitasPage() {
  const { permisos } = useAuth()

  const [citas, setCitas] = useState<CitaResponse[]>([])
  const [pacientes, setPacientes] = useState<PacienteResponse[]>([])
  const [medicos, setMedicos] = useState<MedicoResponse[]>([])
  const [cargando, setCargando] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)
  const [aviso, setAviso] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormularioCita>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  const [aBorrar, setABorrar] = useState<CitaResponse | null>(null)
  const [borrando, setBorrando] = useState(false)

  const cargar = useCallback(async () => {
    setCargando(true)
    setErrorTabla(null)
    try {
      const [c, p, m] = await Promise.all([listarCitas(), listarPacientes(), listarMedicos()])
      setCitas(c)
      setPacientes(p)
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

  const nombrePaciente = useMemo(() => {
    const mapa = new Map(pacientes.map((p) => [p.idPaciente, `${p.nombre} ${p.apellido}`]))
    return (id: number) => mapa.get(id) ?? `Paciente #${id}`
  }, [pacientes])

  const nombreMedico = useMemo(() => {
    const mapa = new Map(medicos.map((m) => [m.idMedico, `${m.nombre} ${m.apellido}`]))
    return (id: number) => mapa.get(id) ?? `Medico #${id}`
  }, [medicos])

  function abrirNuevo() {
    setEditandoId(null)
    setForm(FORM_VACIO)
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function abrirEdicion(c: CitaResponse) {
    setEditandoId(c.idCita)
    setForm({
      idPaciente: String(c.idPaciente),
      idMedico: String(c.idMedico),
      fechaCita: c.fechaCita,
      hora: c.hora.slice(0, 5),
      motivo: c.motivo ?? '',
      estado: c.estado ?? '',
    })
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function validar(): Record<string, string> {
    const e: Record<string, string> = {}
    if (editandoId === null) {
      if (!form.idPaciente) e.idPaciente = 'Selecciona un paciente.'
      if (!form.idMedico) e.idMedico = 'Selecciona un medico.'
    }
    if (!form.fechaCita) e.fechaCita = 'La fecha es obligatoria.'
    if (!form.hora) e.hora = 'La hora es obligatoria.'
    return e
  }

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setErrorForm(null)
    const errores = validar()
    setErroresCampo(errores)
    if (Object.keys(errores).length > 0) return

    const datos = {
      fechaCita: form.fechaCita,
      hora: aHoraCompleta(form.hora),
      motivo: form.motivo.trim() || undefined,
      estado: form.estado || undefined,
    }

    setGuardando(true)
    try {
      if (editandoId === null) {
        await crearCita({
          ...datos,
          idPaciente: Number(form.idPaciente),
          idMedico: Number(form.idMedico),
        })
        setAviso('Cita agendada.')
      } else {
        await actualizarCita(editandoId, datos)
        setAviso('Cita actualizada.')
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

  async function confirmarBorrado() {
    if (!aBorrar) return
    setBorrando(true)
    try {
      await eliminarCita(aBorrar.idCita)
      setAviso(`Cita #${aBorrar.idCita} eliminada.`)
      setABorrar(null)
      await cargar()
    } catch (e) {
      // 409/400 si la cita ya tiene diagnosticos asociados (FK Restrict).
      setErrorTabla(e)
      setABorrar(null)
    } finally {
      setBorrando(false)
    }
  }

  const campo = (clave: keyof FormularioCita) => (valor: string) =>
    setForm((f) => ({ ...f, [clave]: valor }))

  const hayAcciones = permisos.citasEscribir || permisos.borrar

  return (
    <Layout titulo="Citas">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Citas</h1>
          <div className="panel-acciones">
            {permisos.citasEscribir && (
              <button className="boton-principal" type="button" onClick={abrirNuevo}>
                Agendar cita
              </button>
            )}
          </div>
        </div>

        {aviso && <p className="aviso-exito">{aviso}</p>}
        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargar} />}

        {cargando ? (
          <EstadoCarga mensaje="Cargando citas…" />
        ) : citas.length === 0 ? (
          <p className="vacio">No hay citas registradas.</p>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Paciente</th>
                  <th>Medico</th>
                  <th>Fecha</th>
                  <th>Hora</th>
                  <th>Motivo</th>
                  <th>Estado</th>
                  {hayAcciones && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {citas.map((c) => (
                  <tr key={c.idCita}>
                    <td>{c.idCita}</td>
                    <td>{nombrePaciente(c.idPaciente)}</td>
                    <td>{nombreMedico(c.idMedico)}</td>
                    <td>{c.fechaCita}</td>
                    <td>{c.hora.slice(0, 5)}</td>
                    <td>{c.motivo ?? '—'}</td>
                    <td>{c.estado ?? '—'}</td>
                    {hayAcciones && (
                      <td className="celda-acciones">
                        {permisos.citasEscribir && (
                          <button
                            className="boton-secundario"
                            type="button"
                            onClick={() => abrirEdicion(c)}
                          >
                            Editar
                          </button>
                        )}
                        {permisos.borrar && (
                          <button
                            className="boton-peligro"
                            type="button"
                            onClick={() => setABorrar(c)}
                          >
                            Eliminar
                          </button>
                        )}
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
            {editandoId === null ? 'Agendar cita' : `Editar cita #${editandoId}`}
          </h2>
          <form className="formulario" onSubmit={manejarSubmit} noValidate>
            <label className="campo">
              <span>Paciente</span>
              <select
                value={form.idPaciente}
                disabled={editandoId !== null}
                onChange={(e) => campo('idPaciente')(e.target.value)}
              >
                <option value="">Selecciona un paciente…</option>
                {pacientes.map((p) => (
                  <option key={p.idPaciente} value={p.idPaciente}>
                    {p.nombre} {p.apellido} (CUI {p.cui})
                  </option>
                ))}
              </select>
              {erroresCampo.idPaciente && (
                <small className="campo-error">{erroresCampo.idPaciente}</small>
              )}
            </label>
            <label className="campo">
              <span>Medico</span>
              <select
                value={form.idMedico}
                disabled={editandoId !== null}
                onChange={(e) => campo('idMedico')(e.target.value)}
              >
                <option value="">Selecciona un medico…</option>
                {medicos.map((m) => (
                  <option key={m.idMedico} value={m.idMedico}>
                    {m.nombre} {m.apellido} — {m.especialidad}
                  </option>
                ))}
              </select>
              {erroresCampo.idMedico && (
                <small className="campo-error">{erroresCampo.idMedico}</small>
              )}
            </label>
            <label className="campo">
              <span>Fecha</span>
              <input
                type="date"
                value={form.fechaCita}
                onChange={(e) => campo('fechaCita')(e.target.value)}
              />
              {erroresCampo.fechaCita && (
                <small className="campo-error">{erroresCampo.fechaCita}</small>
              )}
            </label>
            <label className="campo">
              <span>Hora</span>
              <input type="time" value={form.hora} onChange={(e) => campo('hora')(e.target.value)} />
              {erroresCampo.hora && <small className="campo-error">{erroresCampo.hora}</small>}
            </label>
            <label className="campo">
              <span>Motivo (opcional)</span>
              <input
                value={form.motivo}
                maxLength={255}
                onChange={(e) => campo('motivo')(e.target.value)}
              />
            </label>
            <label className="campo">
              <span>Estado</span>
              <select value={form.estado} onChange={(e) => campo('estado')(e.target.value)}>
                {ESTADOS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </label>
            {editandoId !== null && (
              <p className="nota campo-ancho">El paciente y el medico de la cita no se modifican.</p>
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

      {aBorrar && (
        <Confirmacion
          titulo="Eliminar cita"
          mensaje={`Se eliminara la cita #${aBorrar.idCita}. Si ya tiene diagnosticos asociados, el sistema no lo permitira.`}
          textoConfirmar="Eliminar"
          procesando={borrando}
          onConfirmar={confirmarBorrado}
          onCancelar={() => setABorrar(null)}
        />
      )}
    </Layout>
  )
}
