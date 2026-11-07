// Pantalla de Pacientes: listar, registrar y editar (Admin, Medico, Recepcion),
// eliminar (borrado logico, solo Admin) y abrir historial clinico (Admin, Medico).
// Un paciente necesita historial para poder recibir diagnosticos.
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Layout } from '../components/Layout'
import { EstadoCarga } from '../components/EstadoCarga'
import { MensajeError } from '../components/MensajeError'
import { Confirmacion } from '../components/Confirmacion'
import { useAuth } from '../auth/useAuth'
import { listarHistoriales, listarPacientes } from '../api/referencias'
import {
  actualizarPaciente,
  crearHistorial,
  crearPaciente,
  eliminarPaciente,
} from '../api/gestion'
import type { HistorialResponse, PacienteResponse } from '../types/contratos'

interface FormularioPaciente {
  nombre: string
  apellido: string
  cui: string
  fechaNacimiento: string
  nit: string
  telefono: string
  correoElectronico: string
  genero: '' | 'F' | 'M'
  tipoSangre: string
  abrirHistorial: boolean
  antecedentes: string
}

const FORM_VACIO: FormularioPaciente = {
  nombre: '',
  apellido: '',
  cui: '',
  fechaNacimiento: '',
  nit: '',
  telefono: '',
  correoElectronico: '',
  genero: '',
  tipoSangre: '',
  abrirHistorial: true,
  antecedentes: '',
}

const TIPOS_SANGRE = ['O+', 'O-', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-']

function hoy(): string {
  return new Date().toISOString().slice(0, 10)
}

export function PacientesPage() {
  const { permisos } = useAuth()

  const [pacientes, setPacientes] = useState<PacienteResponse[]>([])
  const [historiales, setHistoriales] = useState<HistorialResponse[]>([])
  const [cargando, setCargando] = useState(true)
  const [errorTabla, setErrorTabla] = useState<unknown>(null)
  const [aviso, setAviso] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormularioPaciente>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [errorForm, setErrorForm] = useState<unknown>(null)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})

  const [aBorrar, setABorrar] = useState<PacienteResponse | null>(null)
  const [borrando, setBorrando] = useState(false)

  const cargar = useCallback(async () => {
    setCargando(true)
    setErrorTabla(null)
    try {
      const [p, h] = await Promise.all([listarPacientes(), listarHistoriales()])
      setPacientes(p)
      setHistoriales(h)
    } catch (e) {
      setErrorTabla(e)
    } finally {
      setCargando(false)
    }
  }, [])

  useEffect(() => {
    void cargar()
  }, [cargar])

  function tieneHistorial(idPaciente: number): boolean {
    return historiales.some((h) => h.idPaciente === idPaciente)
  }

  function abrirNuevo() {
    setEditandoId(null)
    setForm({ ...FORM_VACIO, abrirHistorial: permisos.clinicoEscribir })
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function abrirEdicion(p: PacienteResponse) {
    setEditandoId(p.idPaciente)
    setForm({
      nombre: p.nombre,
      apellido: p.apellido,
      cui: p.cui,
      fechaNacimiento: p.fechaNacimiento ?? '',
      nit: p.nit ?? '',
      telefono: p.telefono ?? '',
      correoElectronico: p.correoElectronico ?? '',
      genero: p.genero === 'F' || p.genero === 'M' ? p.genero : '',
      tipoSangre: p.tipoSangre ?? '',
      abrirHistorial: false,
      antecedentes: '',
    })
    setErroresCampo({})
    setErrorForm(null)
    setAviso(null)
    setMostrarForm(true)
  }

  function cerrarForm() {
    setMostrarForm(false)
    setEditandoId(null)
    setForm(FORM_VACIO)
  }

  function validar(): Record<string, string> {
    const e: Record<string, string> = {}
    if (!form.nombre.trim()) e.nombre = 'El nombre es obligatorio.'
    if (!form.apellido.trim()) e.apellido = 'El apellido es obligatorio.'
    if (editandoId === null) {
      if (!form.cui.trim()) e.cui = 'El CUI es obligatorio.'
      if (!form.fechaNacimiento) e.fechaNacimiento = 'La fecha de nacimiento es obligatoria.'
    }
    if (!form.nit.trim()) e.nit = 'El NIT es obligatorio (usa "CF" si no tiene).'
    if (!form.telefono.trim()) e.telefono = 'El telefono es obligatorio.'
    if (!/^\S+@\S+\.\S+$/.test(form.correoElectronico.trim())) {
      e.correoElectronico = 'Ingresa un correo valido.'
    }
    if (!form.genero) e.genero = 'Selecciona el genero.'
    if (!form.tipoSangre) e.tipoSangre = 'Selecciona el tipo de sangre.'
    return e
  }

  async function manejarSubmit(evento: FormEvent) {
    evento.preventDefault()
    setErrorForm(null)
    const errores = validar()
    setErroresCampo(errores)
    if (Object.keys(errores).length > 0) return

    const comunes = {
      nombre: form.nombre.trim(),
      apellido: form.apellido.trim(),
      nit: form.nit.trim(),
      telefono: form.telefono.trim(),
      correoElectronico: form.correoElectronico.trim(),
      genero: form.genero as 'F' | 'M',
      tipoSangre: form.tipoSangre,
    }

    setGuardando(true)
    try {
      if (editandoId === null) {
        const creado = await crearPaciente({
          ...comunes,
          cui: form.cui.trim(),
          fechaNacimiento: form.fechaNacimiento,
        })
        if (form.abrirHistorial && permisos.clinicoEscribir) {
          await crearHistorial({
            idPaciente: creado.idPaciente,
            fechaApertura: hoy(),
            antecedentes: form.antecedentes.trim() || undefined,
          })
          setAviso(`Paciente ${creado.nombre} ${creado.apellido} registrado con su historial clinico.`)
        } else {
          setAviso(`Paciente ${creado.nombre} ${creado.apellido} registrado.`)
        }
      } else {
        await actualizarPaciente(editandoId, comunes)
        setAviso('Paciente actualizado.')
      }
      cerrarForm()
      await cargar()
    } catch (e) {
      setErrorForm(e)
    } finally {
      setGuardando(false)
    }
  }

  async function abrirHistorialDe(p: PacienteResponse) {
    setErrorTabla(null)
    setAviso(null)
    try {
      await crearHistorial({ idPaciente: p.idPaciente, fechaApertura: hoy() })
      setAviso(`Historial clinico abierto para ${p.nombre} ${p.apellido}.`)
      await cargar()
    } catch (e) {
      setErrorTabla(e)
    }
  }

  async function confirmarBorrado() {
    if (!aBorrar) return
    setBorrando(true)
    try {
      await eliminarPaciente(aBorrar.idPaciente)
      setAviso(`Paciente ${aBorrar.nombre} ${aBorrar.apellido} dado de baja.`)
      setABorrar(null)
      await cargar()
    } catch (e) {
      setErrorTabla(e)
      setABorrar(null)
    } finally {
      setBorrando(false)
    }
  }

  const campo = (clave: keyof FormularioPaciente) => (valor: string) =>
    setForm((f) => ({ ...f, [clave]: valor }))

  const hayAcciones = permisos.pacientesEscribir || permisos.clinicoEscribir || permisos.borrar

  return (
    <Layout titulo="Pacientes">
      <section className="panel">
        <div className="panel-encabezado">
          <h1 className="panel-titulo">Pacientes</h1>
          <div className="panel-acciones">
            {permisos.pacientesEscribir && (
              <button className="boton-principal" type="button" onClick={abrirNuevo}>
                Registrar paciente
              </button>
            )}
          </div>
        </div>

        {aviso && <p className="aviso-exito">{aviso}</p>}
        {errorTabla != null && <MensajeError error={errorTabla} onReintentar={cargar} />}

        {cargando ? (
          <EstadoCarga mensaje="Cargando pacientes…" />
        ) : pacientes.length === 0 ? (
          <p className="vacio">No hay pacientes registrados.</p>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nombre</th>
                  <th>CUI</th>
                  <th>Nacimiento</th>
                  <th>Genero</th>
                  <th>Sangre</th>
                  <th>Telefono</th>
                  <th>Historial</th>
                  {hayAcciones && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {pacientes.map((p) => (
                  <tr key={p.idPaciente}>
                    <td>{p.idPaciente}</td>
                    <td>
                      {p.nombre} {p.apellido}
                    </td>
                    <td>{p.cui}</td>
                    <td>{p.fechaNacimiento ?? '—'}</td>
                    <td>{p.genero ?? '—'}</td>
                    <td>{p.tipoSangre ?? '—'}</td>
                    <td>{p.telefono ?? '—'}</td>
                    <td>
                      {tieneHistorial(p.idPaciente) ? (
                        <span className="etiqueta etiqueta-ok">Abierto</span>
                      ) : (
                        <span className="etiqueta">Sin historial</span>
                      )}
                    </td>
                    {hayAcciones && (
                      <td className="celda-acciones">
                        {permisos.pacientesEscribir && (
                          <button
                            className="boton-secundario"
                            type="button"
                            onClick={() => abrirEdicion(p)}
                          >
                            Editar
                          </button>
                        )}
                        {permisos.clinicoEscribir && !tieneHistorial(p.idPaciente) && (
                          <button
                            className="boton-secundario"
                            type="button"
                            onClick={() => void abrirHistorialDe(p)}
                          >
                            Abrir historial
                          </button>
                        )}
                        {permisos.borrar && (
                          <button
                            className="boton-peligro"
                            type="button"
                            onClick={() => setABorrar(p)}
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
            {editandoId === null ? 'Registrar paciente' : `Editar paciente #${editandoId}`}
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
              <span>CUI</span>
              <input
                value={form.cui}
                maxLength={20}
                disabled={editandoId !== null}
                onChange={(e) => campo('cui')(e.target.value)}
              />
              {erroresCampo.cui && <small className="campo-error">{erroresCampo.cui}</small>}
            </label>
            <label className="campo">
              <span>Fecha de nacimiento</span>
              <input
                type="date"
                value={form.fechaNacimiento}
                max={hoy()}
                disabled={editandoId !== null}
                onChange={(e) => campo('fechaNacimiento')(e.target.value)}
              />
              {erroresCampo.fechaNacimiento && (
                <small className="campo-error">{erroresCampo.fechaNacimiento}</small>
              )}
            </label>
            <label className="campo">
              <span>NIT</span>
              <input value={form.nit} maxLength={20} onChange={(e) => campo('nit')(e.target.value)} />
              {erroresCampo.nit && <small className="campo-error">{erroresCampo.nit}</small>}
            </label>
            <label className="campo">
              <span>Telefono</span>
              <input
                value={form.telefono}
                maxLength={20}
                onChange={(e) => campo('telefono')(e.target.value)}
              />
              {erroresCampo.telefono && (
                <small className="campo-error">{erroresCampo.telefono}</small>
              )}
            </label>
            <label className="campo">
              <span>Correo electronico</span>
              <input
                type="email"
                value={form.correoElectronico}
                onChange={(e) => campo('correoElectronico')(e.target.value)}
              />
              {erroresCampo.correoElectronico && (
                <small className="campo-error">{erroresCampo.correoElectronico}</small>
              )}
            </label>
            <label className="campo">
              <span>Genero</span>
              <select value={form.genero} onChange={(e) => campo('genero')(e.target.value)}>
                <option value="">Selecciona…</option>
                <option value="F">F (femenino)</option>
                <option value="M">M (masculino)</option>
              </select>
              {erroresCampo.genero && <small className="campo-error">{erroresCampo.genero}</small>}
            </label>
            <label className="campo">
              <span>Tipo de sangre</span>
              <select value={form.tipoSangre} onChange={(e) => campo('tipoSangre')(e.target.value)}>
                <option value="">Selecciona…</option>
                {TIPOS_SANGRE.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
              {erroresCampo.tipoSangre && (
                <small className="campo-error">{erroresCampo.tipoSangre}</small>
              )}
            </label>

            {editandoId === null && permisos.clinicoEscribir && (
              <>
                <label className="campo campo-check">
                  <input
                    type="checkbox"
                    checked={form.abrirHistorial}
                    onChange={(e) => setForm((f) => ({ ...f, abrirHistorial: e.target.checked }))}
                  />
                  <span>Abrir historial clinico al registrar (necesario para diagnosticos)</span>
                </label>
                {form.abrirHistorial && (
                  <label className="campo campo-ancho">
                    <span>Antecedentes (opcional)</span>
                    <textarea
                      rows={2}
                      value={form.antecedentes}
                      onChange={(e) => campo('antecedentes')(e.target.value)}
                    />
                  </label>
                )}
              </>
            )}
            {editandoId === null && !permisos.clinicoEscribir && (
              <p className="nota campo-ancho">
                El historial clinico lo abre un medico o un administrador desde esta pantalla.
              </p>
            )}
            {editandoId !== null && (
              <p className="nota campo-ancho">El CUI y la fecha de nacimiento no se modifican.</p>
            )}

            {errorForm != null && (
              <div className="campo-ancho">
                <MensajeError error={errorForm} />
              </div>
            )}

            <div className="formulario-acciones">
              <button className="boton-secundario" type="button" onClick={cerrarForm} disabled={guardando}>
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
          titulo="Dar de baja al paciente"
          mensaje={`${aBorrar.nombre} ${aBorrar.apellido} dejara de aparecer en el sistema (borrado logico). Sus registros clinicos se conservan.`}
          textoConfirmar="Dar de baja"
          procesando={borrando}
          onConfirmar={confirmarBorrado}
          onCancelar={() => setABorrar(null)}
        />
      )}
    </Layout>
  )
}
