// PRIORITARIA: pantalla de Diagnosticos end-to-end contra el Gateway.
// - Tabla con filtro por paciente.
// - Formulario crear/editar: se elige paciente y de ahi se cargan su historial y
//   sus citas en desplegables (no se escriben ids a mano).
// - Validacion coherente con el backend: idHistorial e idCita > 0, alergias requerido,
//   codigoCie10 <= 10, fecha opcional.
// - Botones de escritura solo para Admin/Medico; Eliminar (con confirmacion) solo Admin.
// - Estados de carga y error visibles; el 403 se muestra como "permiso insuficiente".
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { Confirmacion } from '../components/Confirmacion'
import { useAuth } from '../auth/useAuth'
import {
  actualizarDiagnostico,
  crearDiagnostico,
  eliminarDiagnostico,
  listarDiagnosticos,
} from '../api/diagnosticos'
import { listarCitas, listarHistoriales, listarPacientes } from '../api/referencias'
import type {
  CitaResponse,
  DiagnosticoResponse,
  HistorialResponse,
  PacienteResponse,
} from '../types/contratos'

/** Estado del formulario de crear/editar. Los ids viven como string (valor de <select>). */
interface FormularioDiagnostico {
  idPaciente: string
  idHistorial: string
  idCita: string
  descripcion: string
  codigoCie10: string
  alergias: string
  fecha: string
}

const FORM_VACIO: FormularioDiagnostico = {
  idPaciente: '',
  idHistorial: '',
  idCita: '',
  descripcion: '',
  codigoCie10: '',
  alergias: '',
  fecha: '',
}

function nombrePaciente(p: PacienteResponse): string {
  return `${p.nombre} ${p.apellido} (CUI ${p.cui})`
}

export function DiagnosticosPage() {
  const { puedeEscribir, puedeBorrar } = useAuth()

  // --- Catalogos de referencia ---
  const [pacientes, setPacientes] = useState<PacienteResponse[]>([])

  // --- Datos de la tabla ---
  const [diagnosticos, setDiagnosticos] = useState<DiagnosticoResponse[]>([])
  const [filtroPaciente, setFiltroPaciente] = useState('')
  const [cargandoTabla, setCargandoTabla] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)

  // --- Formulario ---
  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormularioDiagnostico>(FORM_VACIO)
  const [historialesForm, setHistorialesForm] = useState<HistorialResponse[]>([])
  const [citasForm, setCitasForm] = useState<CitaResponse[]>([])
  const [cargandoDependientes, setCargandoDependientes] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  // --- Borrado ---
  const [aBorrar, setABorrar] = useState<DiagnosticoResponse | null>(null)
  const [borrando, setBorrando] = useState(false)

  const cargarTabla = useCallback(async () => {
    setCargandoTabla(true)
    setErrorTabla(null)
    try {
      const idPaciente = filtroPaciente ? Number(filtroPaciente) : undefined
      const datos = await listarDiagnosticos(idPaciente ? { idPaciente } : {})
      setDiagnosticos(datos)
    } catch (e) {
      setErrorTabla(e)
    } finally {
      setCargandoTabla(false)
    }
  }, [filtroPaciente])

  // Carga inicial de pacientes (para filtros y selector del formulario).
  useEffect(() => {
    let activo = true
    listarPacientes()
      .then((p) => {
        if (activo) setPacientes(p)
      })
      .catch(() => {
        // La tabla y su error cubren el fallo general; el selector queda vacio.
      })
    return () => {
      activo = false
    }
  }, [])

  useEffect(() => {
    void cargarTabla()
  }, [cargarTabla])

  /** Carga historial y citas del paciente elegido en el formulario. */
  const cargarDependientes = useCallback(async (idPaciente: number) => {
    setCargandoDependientes(true)
    try {
      const [historiales, citas] = await Promise.all([
        listarHistoriales(idPaciente),
        listarCitas({ idPaciente }),
      ])
      setHistorialesForm(historiales)
      setCitasForm(citas)
    } catch (e) {
      setErrorForm(e)
      setHistorialesForm([])
      setCitasForm([])
    } finally {
      setCargandoDependientes(false)
    }
  }, [])

  function abrirNuevo() {
    setEditandoId(null)
    setForm(FORM_VACIO)
    setHistorialesForm([])
    setCitasForm([])
    setErroresCampo({})
    setErrorForm(null)
    setMostrarForm(true)
  }

  async function abrirEdicion(d: DiagnosticoResponse) {
    setEditandoId(d.idDiagnosticos)
    setErroresCampo({})
    setErrorForm(null)
    // En edicion no se cambia historial/cita (el backend no los modifica), pero se
    // muestran para contexto. Se intenta ubicar el paciente del historial.
    setForm({
      idPaciente: '',
      idHistorial: String(d.idHistorial),
      idCita: String(d.idCita),
      descripcion: d.descripcion ?? '',
      codigoCie10: d.codigoCie10 ?? '',
      alergias: d.alergias,
      fecha: d.fecha ? d.fecha.slice(0, 10) : '',
    })
    setMostrarForm(true)
  }

  function cerrarForm() {
    setMostrarForm(false)
    setEditandoId(null)
    setForm(FORM_VACIO)
    setErroresCampo({})
    setErrorForm(null)
  }

  function cambiarPaciente(valor: string) {
    setForm((f) => ({ ...f, idPaciente: valor, idHistorial: '', idCita: '' }))
    setHistorialesForm([])
    setCitasForm([])
    if (valor) {
      void cargarDependientes(Number(valor))
    }
  }

  /** Valida el formulario de forma coherente con el backend. Devuelve errores por campo. */
  function validar(): Record<string, string> {
    const errores: Record<string, string> = {}
    if (editandoId === null) {
      // Al crear, idHistorial e idCita son obligatorios y > 0.
      if (!form.idHistorial || Number(form.idHistorial) <= 0) {
        errores.idHistorial = 'Selecciona un historial.'
      }
      if (!form.idCita || Number(form.idCita) <= 0) {
        errores.idCita = 'Selecciona una cita.'
      }
    }
    if (!form.alergias.trim()) {
      errores.alergias = 'Las alergias son obligatorias (usa "Ninguna" si no aplica).'
    }
    if (form.codigoCie10.trim().length > 10) {
      errores.codigoCie10 = 'El codigo CIE-10 admite como maximo 10 caracteres.'
    }
    return errores
  }

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setErrorForm(null)
    const errores = validar()
    setErroresCampo(errores)
    if (Object.keys(errores).length > 0) return

    setGuardando(true)
    try {
      const fechaIso = form.fecha ? new Date(form.fecha).toISOString() : undefined
      if (editandoId === null) {
        await crearDiagnostico({
          idHistorial: Number(form.idHistorial),
          idCita: Number(form.idCita),
          descripcion: form.descripcion.trim() || undefined,
          codigoCie10: form.codigoCie10.trim() || undefined,
          alergias: form.alergias.trim(),
          fecha: fechaIso,
        })
      } else {
        await actualizarDiagnostico(editandoId, {
          descripcion: form.descripcion.trim() || undefined,
          codigoCie10: form.codigoCie10.trim() || undefined,
          alergias: form.alergias.trim(),
          fecha: fechaIso,
        })
      }
      cerrarForm()
      await cargarTabla()
    } catch (e) {
      setErrorForm(e)
    } finally {
      setGuardando(false)
    }
  }

  async function confirmarBorrado() {
    if (!aBorrar) return
    setBorrando(true)
    setErrorTabla(null)
    try {
      await eliminarDiagnostico(aBorrar.idDiagnosticos)
      setABorrar(null)
      await cargarTabla()
    } catch (e) {
      setErrorTabla(e)
      setABorrar(null)
    } finally {
      setBorrando(false)
    }
  }

  return (
    <Layout titulo="Diagnosticos">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Diagnosticos</h1>
          <div className="panel-acciones">
            <label className="filtro">
              <span>Filtrar por paciente</span>
              <select
                value={filtroPaciente}
                onChange={(e) => setFiltroPaciente(e.target.value)}
              >
                <option value="">Todos</option>
                {pacientes.map((p) => (
                  <option key={p.idPaciente} value={p.idPaciente}>
                    {nombrePaciente(p)}
                  </option>
                ))}
              </select>
            </label>
            {puedeEscribir && (
              <button className="boton-principal" type="button" onClick={abrirNuevo}>
                Nuevo diagnostico
              </button>
            )}
          </div>
        </div>

        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargarTabla} />}

        {cargandoTabla ? (
          <EstadoCarga mensaje="Cargando diagnosticos…" />
        ) : diagnosticos.length === 0 ? (
          <p className="vacio">No hay diagnosticos para el filtro actual.</p>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Historial</th>
                  <th>Cita</th>
                  <th>CIE-10</th>
                  <th>Descripcion</th>
                  <th>Alergias</th>
                  <th>Fecha</th>
                  {(puedeEscribir || puedeBorrar) && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {diagnosticos.map((d) => (
                  <tr key={d.idDiagnosticos}>
                    <td>{d.idDiagnosticos}</td>
                    <td>{d.idHistorial}</td>
                    <td>{d.idCita}</td>
                    <td>{d.codigoCie10 ?? '—'}</td>
                    <td>{d.descripcion ?? '—'}</td>
                    <td>{d.alergias}</td>
                    <td>{d.fecha ? d.fecha.slice(0, 10) : '—'}</td>
                    {(puedeEscribir || puedeBorrar) && (
                      <td className="celda-acciones">
                        {puedeEscribir && (
                          <button
                            className="boton-secundario"
                            type="button"
                            onClick={() => abrirEdicion(d)}
                          >
                            Editar
                          </button>
                        )}
                        {puedeBorrar && (
                          <button
                            className="boton-peligro"
                            type="button"
                            onClick={() => setABorrar(d)}
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
            {editandoId === null ? 'Nuevo diagnostico' : `Editar diagnostico #${editandoId}`}
          </h2>
          <form className="formulario" onSubmit={manejarSubmit} noValidate>
            {editandoId === null ? (
              <>
                <label className="campo">
                  <span>Paciente</span>
                  <select
                    value={form.idPaciente}
                    onChange={(e) => cambiarPaciente(e.target.value)}
                  >
                    <option value="">Selecciona un paciente…</option>
                    {pacientes.map((p) => (
                      <option key={p.idPaciente} value={p.idPaciente}>
                        {nombrePaciente(p)}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="campo">
                  <span>Historial</span>
                  <select
                    value={form.idHistorial}
                    onChange={(e) => setForm((f) => ({ ...f, idHistorial: e.target.value }))}
                    disabled={!form.idPaciente || cargandoDependientes}
                  >
                    <option value="">
                      {cargandoDependientes ? 'Cargando…' : 'Selecciona un historial…'}
                    </option>
                    {historialesForm.map((h) => (
                      <option key={h.idHistorial} value={h.idHistorial}>
                        #{h.idHistorial} — apertura {h.fechaApertura?.slice(0, 10)}
                      </option>
                    ))}
                  </select>
                  {erroresCampo.idHistorial && (
                    <small className="campo-error">{erroresCampo.idHistorial}</small>
                  )}
                </label>

                <label className="campo">
                  <span>Cita</span>
                  <select
                    value={form.idCita}
                    onChange={(e) => setForm((f) => ({ ...f, idCita: e.target.value }))}
                    disabled={!form.idPaciente || cargandoDependientes}
                  >
                    <option value="">
                      {cargandoDependientes ? 'Cargando…' : 'Selecciona una cita…'}
                    </option>
                    {citasForm.map((c) => (
                      <option key={c.idCita} value={c.idCita}>
                        #{c.idCita} — {c.fechaCita?.slice(0, 10)} {c.motivo ? `(${c.motivo})` : ''}
                      </option>
                    ))}
                  </select>
                  {erroresCampo.idCita && (
                    <small className="campo-error">{erroresCampo.idCita}</small>
                  )}
                </label>
              </>
            ) : (
              <p className="nota">
                Historial #{form.idHistorial} y cita #{form.idCita} no se modifican al editar.
              </p>
            )}

            <label className="campo">
              <span>Codigo CIE-10 (opcional, max 10)</span>
              <input
                type="text"
                maxLength={10}
                value={form.codigoCie10}
                onChange={(e) => setForm((f) => ({ ...f, codigoCie10: e.target.value }))}
              />
              {erroresCampo.codigoCie10 && (
                <small className="campo-error">{erroresCampo.codigoCie10}</small>
              )}
            </label>

            <label className="campo">
              <span>Alergias</span>
              <input
                type="text"
                value={form.alergias}
                onChange={(e) => setForm((f) => ({ ...f, alergias: e.target.value }))}
                required
              />
              {erroresCampo.alergias && (
                <small className="campo-error">{erroresCampo.alergias}</small>
              )}
            </label>

            <label className="campo">
              <span>Descripcion (opcional)</span>
              <textarea
                rows={3}
                value={form.descripcion}
                onChange={(e) => setForm((f) => ({ ...f, descripcion: e.target.value }))}
              />
            </label>

            <label className="campo">
              <span>Fecha (opcional)</span>
              <input
                type="date"
                value={form.fecha}
                onChange={(e) => setForm((f) => ({ ...f, fecha: e.target.value }))}
              />
            </label>

            {errorForm != null && <MensajeError error={errorForm} />}

            <div className="formulario-acciones">
              <button
                className="boton-secundario"
                type="button"
                onClick={cerrarForm}
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
          titulo="Eliminar diagnostico"
          mensaje={`Se eliminara el diagnostico #${aBorrar.idDiagnosticos}. Esta accion no se puede deshacer.`}
          textoConfirmar="Eliminar"
          procesando={borrando}
          onConfirmar={confirmarBorrado}
          onCancelar={() => setABorrar(null)}
        />
      )}
    </Layout>
  )
}
