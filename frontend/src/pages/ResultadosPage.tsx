// PRIORITARIA: pantalla de Resultados tratantes end-to-end contra el Gateway.
// - Tabla con filtro por diagnostico o por medico.
// - Formulario crear/editar: medico requerido (via /api/medicos) y selectores
//   opcionales de diagnostico (/api/diagnosticos) y estudio (/api/estudios?idCita=).
// - Validacion: idMedico requerido > 0 al crear; descripcion/conclusion opcionales.
// - Botones de escritura solo Admin/Medico; Eliminar (con confirmacion) solo Admin.
// - Estados de carga y error visibles; el 403 se muestra como "permiso insuficiente".
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { Confirmacion } from '../components/Confirmacion'
import { useAuth } from '../auth/useAuth'
import {
  actualizarResultado,
  crearResultado,
  eliminarResultado,
  listarResultados,
} from '../api/resultados'
import { listarDiagnosticos } from '../api/diagnosticos'
import { listarMedicos } from '../api/referencias'
import type {
  DiagnosticoResponse,
  MedicoResponse,
  ResultadoTratanteResponse,
} from '../types/contratos'

/** Estado del formulario de crear/editar. Los ids viven como string (valor de <select>). */
interface FormularioResultado {
  idMedico: string
  idDiagnostico: string
  idEstudio: string
  fecha: string
  descripcion: string
  conclusion: string
}

const FORM_VACIO: FormularioResultado = {
  idMedico: '',
  idDiagnostico: '',
  idEstudio: '',
  fecha: '',
  descripcion: '',
  conclusion: '',
}

function nombreMedico(m: MedicoResponse): string {
  return `${m.nombre} ${m.apellido} — ${m.especialidad} (col. ${m.colegiado})`
}

export function ResultadosPage() {
  const { puedeEscribir, puedeBorrar } = useAuth()

  // --- Catalogos de referencia ---
  const [medicos, setMedicos] = useState<MedicoResponse[]>([])
  const [diagnosticos, setDiagnosticos] = useState<DiagnosticoResponse[]>([])

  // --- Datos de la tabla ---
  const [resultados, setResultados] = useState<ResultadoTratanteResponse[]>([])
  const [filtroMedico, setFiltroMedico] = useState('')
  const [filtroDiagnostico, setFiltroDiagnostico] = useState('')
  const [cargandoTabla, setCargandoTabla] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)

  // --- Formulario ---
  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormularioResultado>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  // --- Borrado ---
  const [aBorrar, setABorrar] = useState<ResultadoTratanteResponse | null>(null)
  const [borrando, setBorrando] = useState(false)

  const cargarTabla = useCallback(async () => {
    setCargandoTabla(true)
    setErrorTabla(null)
    try {
      const filtros: { idMedico?: number; idDiagnostico?: number } = {}
      if (filtroMedico) filtros.idMedico = Number(filtroMedico)
      if (filtroDiagnostico) filtros.idDiagnostico = Number(filtroDiagnostico)
      const datos = await listarResultados(filtros)
      setResultados(datos)
    } catch (e) {
      setErrorTabla(e)
    } finally {
      setCargandoTabla(false)
    }
  }, [filtroMedico, filtroDiagnostico])

  // Carga inicial de medicos y diagnosticos (para filtros y selectores del formulario).
  useEffect(() => {
    let activo = true
    listarMedicos()
      .then((m) => {
        if (activo) setMedicos(m)
      })
      .catch(() => {
        // El error general de la tabla cubre el fallo; el selector queda vacio.
      })
    listarDiagnosticos()
      .then((d) => {
        if (activo) setDiagnosticos(d)
      })
      .catch(() => {
        // Idem.
      })
    return () => {
      activo = false
    }
  }, [])

  useEffect(() => {
    void cargarTabla()
  }, [cargarTabla])

  const medicosPorId = useMemo(() => {
    const mapa = new Map<number, MedicoResponse>()
    for (const m of medicos) mapa.set(m.idMedico, m)
    return mapa
  }, [medicos])

  function abrirNuevo() {
    setEditandoId(null)
    setForm(FORM_VACIO)
    setErroresCampo({})
    setErrorForm(null)
    setMostrarForm(true)
  }

  function abrirEdicion(r: ResultadoTratanteResponse) {
    setEditandoId(r.idResultado)
    setErroresCampo({})
    setErrorForm(null)
    setForm({
      idMedico: String(r.idMedico),
      idDiagnostico: r.idDiagnostico != null ? String(r.idDiagnostico) : '',
      idEstudio: r.idEstudio != null ? String(r.idEstudio) : '',
      fecha: r.fecha ? r.fecha.slice(0, 10) : '',
      descripcion: r.descripcion ?? '',
      conclusion: r.conclusion ?? '',
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

  /** Valida el formulario de forma coherente con el backend. */
  function validar(): Record<string, string> {
    const errores: Record<string, string> = {}
    if (editandoId === null) {
      // idMedico solo se fija al crear (el backend no lo modifica en update).
      if (!form.idMedico || Number(form.idMedico) <= 0) {
        errores.idMedico = 'Selecciona un medico.'
      }
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
      const idDiagnostico = form.idDiagnostico ? Number(form.idDiagnostico) : null
      const idEstudio = form.idEstudio ? Number(form.idEstudio) : null
      if (editandoId === null) {
        await crearResultado({
          idMedico: Number(form.idMedico),
          idDiagnostico,
          idEstudio,
          fecha: fechaIso,
          descripcion: form.descripcion.trim() || undefined,
          conclusion: form.conclusion.trim() || undefined,
        })
      } else {
        await actualizarResultado(editandoId, {
          idDiagnostico,
          idEstudio,
          fecha: fechaIso,
          descripcion: form.descripcion.trim() || undefined,
          conclusion: form.conclusion.trim() || undefined,
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
      await eliminarResultado(aBorrar.idResultado)
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
    <Layout titulo="Resultados tratantes">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Resultados tratantes</h1>
          <div className="panel-acciones">
            <label className="filtro">
              <span>Filtrar por medico</span>
              <select value={filtroMedico} onChange={(e) => setFiltroMedico(e.target.value)}>
                <option value="">Todos</option>
                {medicos.map((m) => (
                  <option key={m.idMedico} value={m.idMedico}>
                    {nombreMedico(m)}
                  </option>
                ))}
              </select>
            </label>
            <label className="filtro">
              <span>Filtrar por diagnostico</span>
              <select
                value={filtroDiagnostico}
                onChange={(e) => setFiltroDiagnostico(e.target.value)}
              >
                <option value="">Todos</option>
                {diagnosticos.map((d) => (
                  <option key={d.idDiagnosticos} value={d.idDiagnosticos}>
                    #{d.idDiagnosticos} — {d.codigoCie10 ?? 'sin CIE-10'}
                  </option>
                ))}
              </select>
            </label>
            {puedeEscribir && (
              <button className="boton-principal" type="button" onClick={abrirNuevo}>
                Nuevo resultado
              </button>
            )}
          </div>
        </div>

        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargarTabla} />}

        {cargandoTabla ? (
          <EstadoCarga mensaje="Cargando resultados…" />
        ) : resultados.length === 0 ? (
          <p className="vacio">No hay resultados para el filtro actual.</p>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Medico</th>
                  <th>Diagnostico</th>
                  <th>Estudio</th>
                  <th>Descripcion</th>
                  <th>Conclusion</th>
                  <th>Fecha</th>
                  {(puedeEscribir || puedeBorrar) && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {resultados.map((r) => {
                  const medico = medicosPorId.get(r.idMedico)
                  return (
                    <tr key={r.idResultado}>
                      <td>{r.idResultado}</td>
                      <td>
                        {medico ? `${medico.nombre} ${medico.apellido}` : `#${r.idMedico}`}
                      </td>
                      <td>{r.idDiagnostico ?? '—'}</td>
                      <td>{r.idEstudio ?? '—'}</td>
                      <td>{r.descripcion ?? '—'}</td>
                      <td>{r.conclusion ?? '—'}</td>
                      <td>{r.fecha ? r.fecha.slice(0, 10) : '—'}</td>
                      {(puedeEscribir || puedeBorrar) && (
                        <td className="celda-acciones">
                          {puedeEscribir && (
                            <button
                              className="boton-secundario"
                              type="button"
                              onClick={() => abrirEdicion(r)}
                            >
                              Editar
                            </button>
                          )}
                          {puedeBorrar && (
                            <button
                              className="boton-peligro"
                              type="button"
                              onClick={() => setABorrar(r)}
                            >
                              Eliminar
                            </button>
                          )}
                        </td>
                      )}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {mostrarForm && (
        <section className="panel form-panel">
          <h2 className="panel-titulo">
            {editandoId === null ? 'Nuevo resultado' : `Editar resultado #${editandoId}`}
          </h2>
          <form className="formulario" onSubmit={manejarSubmit} noValidate>
            <label className="campo">
              <span>Medico</span>
              <select
                value={form.idMedico}
                onChange={(e) => setForm((f) => ({ ...f, idMedico: e.target.value }))}
                disabled={editandoId !== null}
              >
                <option value="">Selecciona un medico…</option>
                {medicos.map((m) => (
                  <option key={m.idMedico} value={m.idMedico}>
                    {nombreMedico(m)}
                  </option>
                ))}
              </select>
              {editandoId !== null && (
                <small className="nota">El medico no se modifica al editar.</small>
              )}
              {erroresCampo.idMedico && (
                <small className="campo-error">{erroresCampo.idMedico}</small>
              )}
            </label>

            <label className="campo">
              <span>Diagnostico (opcional)</span>
              <select
                value={form.idDiagnostico}
                onChange={(e) => setForm((f) => ({ ...f, idDiagnostico: e.target.value }))}
              >
                <option value="">Sin diagnostico</option>
                {diagnosticos.map((d) => (
                  <option key={d.idDiagnosticos} value={d.idDiagnosticos}>
                    #{d.idDiagnosticos} — {d.codigoCie10 ?? 'sin CIE-10'}
                  </option>
                ))}
              </select>
            </label>

            <label className="campo">
              <span>ID de estudio (opcional)</span>
              <input
                type="number"
                min={1}
                value={form.idEstudio}
                onChange={(e) => setForm((f) => ({ ...f, idEstudio: e.target.value }))}
              />
              <small className="nota">
                Opcional. El estudio depende de la cita; dejalo vacio si no aplica.
              </small>
            </label>

            <label className="campo">
              <span>Fecha (opcional)</span>
              <input
                type="date"
                value={form.fecha}
                onChange={(e) => setForm((f) => ({ ...f, fecha: e.target.value }))}
              />
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
              <span>Conclusion (opcional)</span>
              <textarea
                rows={3}
                value={form.conclusion}
                onChange={(e) => setForm((f) => ({ ...f, conclusion: e.target.value }))}
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
          titulo="Eliminar resultado"
          mensaje={`Se eliminara el resultado #${aBorrar.idResultado}. Esta accion no se puede deshacer.`}
          textoConfirmar="Eliminar"
          procesando={borrando}
          onConfirmar={confirmarBorrado}
          onCancelar={() => setABorrar(null)}
        />
      )}
    </Layout>
  )
}
