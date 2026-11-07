// Matriz de permisos de la UI por rol. Refleja los [Authorize(Roles=...)] de la Api;
// el backend sigue siendo la autoridad (responde 403), esto solo decide que se muestra.
import type { Rol } from '../types/contratos'

export interface Permisos {
  /** Crear/editar diagnosticos, resultados tratantes e historiales. */
  clinicoEscribir: boolean
  /** Crear/editar pacientes. */
  pacientesEscribir: boolean
  /** Crear/editar citas. */
  citasEscribir: boolean
  /** Crear/editar medicos. */
  medicosEscribir: boolean
  /** Borrar registros (diagnosticos, resultados, pacientes, citas). */
  borrar: boolean
  /** Ver y gestionar usuarios. */
  usuarios: boolean
}

const SIN_PERMISOS: Permisos = {
  clinicoEscribir: false,
  pacientesEscribir: false,
  citasEscribir: false,
  medicosEscribir: false,
  borrar: false,
  usuarios: false,
}

const POR_ROL: Record<Rol, Permisos> = {
  Admin: {
    clinicoEscribir: true,
    pacientesEscribir: true,
    citasEscribir: true,
    medicosEscribir: true,
    borrar: true,
    usuarios: true,
  },
  Medico: { ...SIN_PERMISOS, clinicoEscribir: true, pacientesEscribir: true, citasEscribir: true },
  Recepcion: { ...SIN_PERMISOS, pacientesEscribir: true, citasEscribir: true },
  Lectura: SIN_PERMISOS,
}

/** Descripcion corta de lo que puede hacer cada rol, mostrada en la barra. */
export const DESCRIPCION_ROL: Record<Rol, string> = {
  Admin: 'Acceso total: registra, edita y elimina; gestiona médicos y usuarios.',
  Medico: 'Registra y edita datos clínicos, pacientes y citas. No elimina.',
  Recepcion: 'Registra y edita pacientes y citas. Datos clínicos solo en consulta.',
  Lectura: 'Solo consulta. No puede registrar, editar ni eliminar.',
}

export function permisosDe(rol: Rol | undefined): Permisos {
  return rol ? POR_ROL[rol] : SIN_PERMISOS
}

/** Pantalla inicial segun el rol: Recepcion trabaja sobre pacientes y citas. */
export function rutaInicio(rol: Rol | undefined): string {
  return rol === 'Recepcion' ? '/pacientes' : '/diagnosticos'
}
