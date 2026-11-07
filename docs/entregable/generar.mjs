// Genera el PDF del entregable del proyecto final (ClinicaCore EMR):
//   1. Renderiza el diagrama E-R desde docs/er/modelo-er.md (Mermaid) a PNG.
//   2. Toma capturas de los formularios de Diagnosticos y Resultados tratantes contra
//      el sistema en ejecucion (http://localhost:8090 por defecto), con Chrome headless.
//   3. Arma el PDF con portada, las tres evidencias y el enlace al codigo fuente.
//
// Requisitos: stack levantado con los datos de demostracion (infra/scripts/08-cargar-demo.sh),
// Google Chrome instalado y mermaid-cli (mmdc) en el PATH.
//
// Uso (desde docs/entregable/):  npm install && npm run generar
// Variables opcionales: BASE_URL, CHROME_PATH, ADMIN_CORREO, ADMIN_PASSWORD, REPO_URL.

import { execFileSync } from 'node:child_process'
import { mkdirSync, readFileSync, writeFileSync, existsSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { tmpdir } from 'node:os'
import puppeteer from 'puppeteer-core'

const aqui = dirname(fileURLToPath(import.meta.url))
const raiz = resolve(aqui, '..', '..')

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:8090'
const CHROME_PATH =
  process.env.CHROME_PATH ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
const ADMIN_CORREO = process.env.ADMIN_CORREO ?? 'admin@clinicacore.test'
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin123!'
const REPO_URL = process.env.REPO_URL ?? 'https://github.com/llimaix/Healthcare'

const dirEvidencias = join(raiz, 'docs', 'evidencias')
const pngEr = join(raiz, 'docs', 'er', 'diagrama-er.png')
const pdfSalida = join(aqui, 'ProyectoFinal-ClinicaCore.pdf')
const portada = JSON.parse(readFileSync(join(aqui, 'portada.json'), 'utf8'))

const capturas = {
  diagLista: join(dirEvidencias, 'diagnosticos-lista.png'),
  diagForm: join(dirEvidencias, 'diagnosticos-formulario.png'),
  resLista: join(dirEvidencias, 'resultados-tratantes-lista.png'),
  resForm: join(dirEvidencias, 'resultados-tratantes-formulario.png'),
}

function paso(texto) {
  console.log(`\n==> ${texto}`)
}

function git(...args) {
  return execFileSync('git', ['-C', raiz, ...args], { encoding: 'utf8' }).trim()
}

// --- 1. Diagrama E-R -------------------------------------------------------------
function renderizarDiagrama() {
  paso('Renderizando el diagrama E-R desde docs/er/modelo-er.md')
  const md = readFileSync(join(raiz, 'docs', 'er', 'modelo-er.md'), 'utf8')
  const bloque = md.match(/```mermaid\n([\s\S]*?)```/)
  if (!bloque) throw new Error('No se encontro el bloque ```mermaid en modelo-er.md')
  const mmd = join(tmpdir(), 'clinicacore-er.mmd')
  writeFileSync(mmd, bloque[1])
  execFileSync('mmdc', ['-i', mmd, '-o', pngEr, '-s', '5', '-b', 'white'], { stdio: 'inherit' })
  console.log(`    ${pngEr}`)
}

// --- 2. Capturas de pantalla -----------------------------------------------------

/** Fija el valor de un input/textarea controlado por React y dispara el evento. */
async function escribir(pagina, selector, valor) {
  await pagina.$eval(
    selector,
    (el, v) => {
      const proto = el instanceof HTMLTextAreaElement ? HTMLTextAreaElement : HTMLInputElement
      Object.getOwnPropertyDescriptor(proto.prototype, 'value').set.call(el, v)
      el.dispatchEvent(new Event('input', { bubbles: true }))
    },
    valor,
  )
}

/** Selecciona en el n-esimo <select> del formulario y espera a que tenga opciones. */
async function elegir(pagina, indice, valor) {
  await pagina.waitForFunction(
    (i) => {
      const s = document.querySelectorAll('.form-panel select')[i]
      return s && !s.disabled && s.options.length > 1
    },
    {},
    indice,
  )
  const selects = await pagina.$$('.form-panel select')
  await selects[indice].select(valor)
}

async function esperarTabla(pagina) {
  await pagina.waitForSelector('.tabla tbody tr')
  await pagina.waitForNetworkIdle({ idleTime: 400 })
}

/** Captura la pantalla completa (barra incluida) recortada al final del contenido. */
async function capturarPantalla(pagina, ruta) {
  const alto = await pagina.$eval('.contenido', (el) => Math.ceil(el.getBoundingClientRect().bottom))
  const ancho = await pagina.evaluate(() => document.documentElement.clientWidth)
  await pagina.screenshot({ path: ruta, clip: { x: 0, y: 0, width: ancho, height: alto } })
}

async function capturarPantallas(navegador) {
  paso(`Tomando capturas contra ${BASE_URL}`)
  mkdirSync(dirEvidencias, { recursive: true })
  const pagina = await navegador.newPage()
  await pagina.setViewport({ width: 1366, height: 860, deviceScaleFactor: 2 })

  // Login como Admin (ve todas las acciones: Nuevo, Editar, Eliminar).
  await pagina.goto(`${BASE_URL}/login`, { waitUntil: 'networkidle0' })
  await pagina.type('input[name=correo]', ADMIN_CORREO)
  await pagina.type('input[name=password]', ADMIN_PASSWORD)
  await Promise.all([pagina.waitForNavigation(), pagina.click('button[type=submit]')])

  // Diagnosticos: listado
  await pagina.goto(`${BASE_URL}/diagnosticos`, { waitUntil: 'networkidle0' })
  await esperarTabla(pagina)
  await capturarPantalla(pagina, capturas.diagLista)
  console.log(`    ${capturas.diagLista}`)

  // Diagnosticos: formulario de registro lleno (no se guarda)
  await pagina.click('.panel-acciones .boton-principal')
  await pagina.waitForSelector('.form-panel')
  await elegir(pagina, 0, '1') // paciente: Maria Lopez
  await elegir(pagina, 1, '1') // su historial
  await elegir(pagina, 2, '1') // su cita
  await escribir(pagina, '.form-panel input[maxlength="10"]', 'I10')
  const textos = await pagina.$$('.form-panel input[type=text]')
  await textos[1].evaluate((el) => el.setAttribute('data-alergias', '1'))
  await escribir(pagina, '.form-panel input[data-alergias]', 'Ninguna')
  await escribir(pagina, '.form-panel textarea', 'Control de hipertensión: presión arterial 140/90 mmHg.')
  await escribir(pagina, '.form-panel input[type=date]', '2025-03-10')
  await (await pagina.$('.form-panel')).screenshot({ path: capturas.diagForm })
  console.log(`    ${capturas.diagForm}`)

  // Resultados tratantes: listado
  await pagina.goto(`${BASE_URL}/resultados-tratantes`, { waitUntil: 'networkidle0' })
  await esperarTabla(pagina)
  await capturarPantalla(pagina, capturas.resLista)
  console.log(`    ${capturas.resLista}`)

  // Resultados tratantes: formulario de registro lleno (no se guarda)
  await pagina.click('.panel-acciones .boton-principal')
  await pagina.waitForSelector('.form-panel')
  await elegir(pagina, 0, '1') // medico: Ana Garcia
  await elegir(pagina, 1, '1') // diagnostico #1 (I10)
  await escribir(pagina, '.form-panel input[type=date]', '2025-03-10')
  const areas = await pagina.$$('.form-panel textarea')
  await areas[0].evaluate((el) => el.setAttribute('data-desc', '1'))
  await areas[1].evaluate((el) => el.setAttribute('data-concl', '1'))
  await escribir(pagina, '.form-panel textarea[data-desc]', 'Presión arterial 140/90 mmHg tras ajuste de dosis.')
  await escribir(pagina, '.form-panel textarea[data-concl]', 'Respuesta favorable; mantener tratamiento y control en 60 días.')
  await (await pagina.$('.form-panel')).screenshot({ path: capturas.resForm })
  console.log(`    ${capturas.resForm}`)

  await pagina.close()
}

// --- 3. PDF --------------------------------------------------------------------
function escaparHtml(texto) {
  return String(texto).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c])
}

function construirHtml(version) {
  const img = (ruta) => pathToFileURL(ruta).href
  const fecha = new Date().toLocaleDateString('es-GT', { day: 'numeric', month: 'long', year: 'numeric' })
  const filaPortada = (etiqueta, valor) =>
    valor ? `<tr><th>${etiqueta}</th><td>${escaparHtml(valor)}</td></tr>` : ''
  const urlCommit = `${REPO_URL}/tree/${version.commit}`
  const urlZip = `${REPO_URL}/archive/${version.commit}.zip`

  return `<!doctype html>
<html lang="es"><head><meta charset="utf-8"><title>ClinicaCore — Proyecto final</title>
<style>
  @page { size: A4; margin: 18mm 16mm 18mm 16mm; }
  @page horizontal { size: A4 landscape; margin: 12mm; }
  * { box-sizing: border-box; }
  body { font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1f2933; font-size: 11pt; line-height: 1.45; margin: 0; }
  h1 { font-size: 20pt; margin: 0 0 4mm; }
  h2 { font-size: 15pt; margin: 0 0 3mm; color: #1e3a8a; border-bottom: 2px solid #c7d7ff; padding-bottom: 2mm; }
  h3 { font-size: 11.5pt; margin: 6mm 0 2mm; }
  p { margin: 0 0 3mm; }
  a { color: #1d4ed8; }
  .pagina { page-break-after: always; }
  .pagina:last-child { page-break-after: auto; }
  .portada { display: flex; flex-direction: column; justify-content: center; min-height: 255mm; }
  .portada .uni { font-size: 13pt; color: #52606d; margin-bottom: 18mm; }
  .portada h1 { font-size: 26pt; line-height: 1.2; }
  .portada .sub { font-size: 13pt; color: #334e68; margin-bottom: 14mm; }
  table.datos { border-collapse: collapse; font-size: 11pt; margin-bottom: 12mm; }
  table.datos th { text-align: left; color: #52606d; font-weight: 600; padding: 1.5mm 8mm 1.5mm 0; }
  table.datos td { padding: 1.5mm 0; }
  .indice li { margin-bottom: 1mm; }
  .horizontal { page: horizontal; }
  .er { width: 100%; height: 150mm; object-fit: contain; display: block; margin: 0 auto; }
  .captura { width: 100%; border: 1px solid #d9e2ec; border-radius: 2mm; display: block; }
  .captura.form { max-height: 150mm; width: auto; max-width: 100%; margin: 0 auto; }
  .pie { font-size: 9pt; color: #52606d; margin-top: 2mm; }
  .enlace { font-size: 12.5pt; font-weight: 600; word-break: break-all; }
  .caja { border: 1px solid #c7d7ff; background: #f5f8ff; border-radius: 2mm; padding: 4mm 5mm; margin: 3mm 0 5mm; }
  pre { background: #f5f7fa; border: 1px solid #e4e7eb; border-radius: 2mm; padding: 3mm 4mm; font-size: 9pt; white-space: pre-wrap; }
  code { font-family: Menlo, Consolas, monospace; font-size: 9.5pt; }
</style></head><body>

<section class="pagina portada">
  <div class="uni">${escaparHtml(portada.universidad)}</div>
  <h1>ClinicaCore<br>Sistema de gestión de atención médica (EMR)</h1>
  <div class="sub">${escaparHtml(portada.curso)} · ${escaparHtml(portada.actividad)}</div>
  <table class="datos">
    ${filaPortada('Estudiante', portada.estudiante)}
    ${filaPortada('Carné', portada.carne)}
    ${filaPortada('Sección', portada.seccion)}
    ${filaPortada('Catedrático', portada.catedratico)}
    <tr><th>Fecha</th><td>${escaparHtml(fecha)}</td></tr>
    <tr><th>Código fuente</th><td><a href="${REPO_URL}">${REPO_URL}</a></td></tr>
  </table>
  <h3>Contenido</h3>
  <ol class="indice">
    <li>Diagrama entidad–relación</li>
    <li>Formularios de diagnósticos</li>
    <li>Formularios de resultados tratantes</li>
    <li>Enlace para descargar el código del proyecto</li>
  </ol>
  <p class="pie">Stack: .NET 10 (ASP.NET Core, EF Core) · React + TypeScript · MySQL 8 con réplica · nginx · Docker Swarm.</p>
</section>

<section class="pagina horizontal">
  <h2>1. Diagrama entidad–relación</h2>
  <img class="er" src="${img(pngEr)}" alt="Diagrama entidad-relacion">
  <p class="pie">18 tablas implementadas en MySQL 8 mediante migraciones de EF Core. PK = llave primaria, FK = llave foránea, UK = valor único. Los atributos marcados con * provienen del enunciado. La imagen en alta resolución está en el repositorio: <code>docs/er/diagrama-er.png</code>.</p>
</section>

<section class="pagina">
  <h2>2. Formularios de diagnósticos</h2>
  <h3>2.1 Listado de diagnósticos</h3>
  <img class="captura" src="${img(capturas.diagLista)}" alt="Listado de diagnosticos">
  <p class="pie">Pantalla <code>/diagnosticos</code> con sesión de Administrador: filtro por paciente y acciones de crear, editar y eliminar.</p>
</section>
<section class="pagina">
  <h3>2.2 Formulario de registro de diagnóstico</h3>
  <img class="captura form" src="${img(capturas.diagForm)}" alt="Formulario de diagnostico">
  <p class="pie">Al elegir el paciente se cargan su historial clínico y sus citas. El código CIE-10 admite hasta 10 caracteres y las alergias son obligatorias.</p>
</section>

<section class="pagina">
  <h2>3. Formularios de resultados tratantes</h2>
  <h3>3.1 Listado de resultados tratantes</h3>
  <img class="captura" src="${img(capturas.resLista)}" alt="Listado de resultados tratantes">
  <p class="pie">Pantalla <code>/resultados-tratantes</code>: filtros por médico y por diagnóstico, y acciones de crear, editar y eliminar.</p>
</section>
<section class="pagina">
  <h3>3.2 Formulario de registro de resultado tratante</h3>
  <img class="captura form" src="${img(capturas.resForm)}" alt="Formulario de resultado tratante">
  <p class="pie">El médico tratante es obligatorio; el diagnóstico y el estudio relacionados son opcionales.</p>
</section>

<section class="pagina">
  <h2>4. Enlace para descargar el código del proyecto</h2>
  <div class="caja">
    <p>Repositorio público en GitHub:</p>
    <p class="enlace"><a href="${REPO_URL}">${REPO_URL}</a></p>
    <p>Descarga directa (ZIP) de la versión documentada en este PDF:</p>
    <p class="enlace"><a href="${urlZip}">${urlZip}</a></p>
  </div>
  <p>Las capturas de este documento se tomaron sobre el commit <code>${version.corto}</code> de la rama <code>main</code>
  (<a href="${urlCommit}">ver en GitHub</a>). No se requiere cuenta ni permisos para descargarlo.</p>
  <h3>Cómo ejecutarlo</h3>
  <p>Requiere Docker con Swarm. Desde la raíz del repositorio:</p>
  <pre>bash infra/scripts/00-init-swarm.sh
bash infra/scripts/01-crear-secrets.sh
bash infra/scripts/02-build-imagenes.sh
bash infra/scripts/03-deploy.sh
bash infra/scripts/04-migrar.sh
bash infra/scripts/05-verificar-salud.sh
bash infra/scripts/08-cargar-demo.sh   # opcional: datos y usuarios de demostración</pre>
  <p>La aplicación queda en <code>http://localhost:8090</code>. Usuarios de demostración: <code>admin@clinicacore.test</code> / <code>Admin123!</code>; <code>medico@</code>, <code>recepcion@</code> y <code>lectura@clinicacore.test</code> / <code>Demo123!</code>. El detalle completo está en el <code>README.md</code> del repositorio.</p>
</section>

</body></html>`
}

async function generarPdf(navegador, version) {
  paso('Armando el PDF')
  const html = join(tmpdir(), 'clinicacore-entregable.html')
  writeFileSync(html, construirHtml(version))
  const pagina = await navegador.newPage()
  await pagina.goto(pathToFileURL(html).href, { waitUntil: 'load' })
  await pagina.pdf({ path: pdfSalida, printBackground: true, preferCSSPageSize: true })
  await pagina.close()
  console.log(`    ${pdfSalida}`)
}

// --- main ----------------------------------------------------------------------
async function main() {
  if (!existsSync(CHROME_PATH)) {
    throw new Error(`No se encontro Chrome en ${CHROME_PATH}. Define CHROME_PATH.`)
  }
  const salud = await fetch(`${BASE_URL}/healthz`).catch(() => null)
  if (!salud?.ok) {
    throw new Error(`El sistema no responde en ${BASE_URL}. Levanta el stack (ver README).`)
  }

  // Version documentada = ultimo commit que cambio el codigo de la aplicacion. Los
  // commits posteriores de solo documentacion (incluido este PDF) no la alteran.
  if (git('status', '--porcelain', '--', 'backend', 'frontend', 'infra')) {
    throw new Error('Hay cambios sin commit en backend/frontend/infra. Haz commit antes de generar el PDF.')
  }
  const commit = git('log', '-1', '--format=%H', '--', 'backend', 'frontend', 'infra')
  const version = { commit, corto: commit.slice(0, 7) }
  // El enlace del PDF solo funciona si ese commit ya esta publicado en origin/main.
  try {
    git('fetch', '-q', 'origin', 'main')
    git('merge-base', '--is-ancestor', commit, 'origin/main')
  } catch {
    console.warn(`AVISO: el commit ${version.corto} no esta en origin/main; haz push o el enlace de descarga fallara.`)
  }

  renderizarDiagrama()
  const navegador = await puppeteer.launch({ executablePath: CHROME_PATH, headless: true })
  try {
    await capturarPantallas(navegador)
    await generarPdf(navegador, version)
  } finally {
    await navegador.close()
  }
  console.log(`\nListo. Commit documentado: ${version.corto}`)
}

main().catch((e) => {
  console.error(`\nERROR: ${e.message}`)
  process.exit(1)
})
