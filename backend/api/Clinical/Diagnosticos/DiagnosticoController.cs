using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Diagnosticos;

/// <summary>
/// Endpoints de diagnosticos (pantalla prioritaria del entregable). CRUD/consulta
/// protegido por JWT: lectura para cualquier rol autenticado, escritura para Admin y
/// Medico, y borrado solo para Admin.
/// </summary>
[ApiController]
[Route("api/diagnosticos")]
[Authorize]
public class DiagnosticoController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public DiagnosticoController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista diagnosticos con filtros opcionales por paciente, historial o cita.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idPaciente,
        [FromQuery] int? idHistorial,
        [FromQuery] int? idCita,
        CancellationToken ct)
    {
        var consulta = _db.Diagnosticos.AsNoTracking().AsQueryable();

        if (idHistorial is int historial)
        {
            consulta = consulta.Where(d => d.IdHistorial == historial);
        }

        if (idCita is int cita)
        {
            consulta = consulta.Where(d => d.IdCita == cita);
        }

        if (idPaciente is int paciente)
        {
            consulta = consulta.Where(d => d.Historial.IdPaciente == paciente);
        }

        var diagnosticos = await consulta
            .OrderBy(d => d.IdDiagnosticos)
            .Select(d => Mapear(d))
            .ToListAsync(ct);

        return Ok(diagnosticos);
    }

    /// <summary>Obtiene un diagnostico por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var diagnostico = await _db.Diagnosticos.AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdDiagnosticos == id, ct);

        return diagnostico is null
            ? ApiProblems.NoEncontrado($"No existe un diagnostico con id {id}.")
            : Ok(Mapear(diagnostico));
    }

    /// <summary>Crea un diagnostico. Valida que existan el historial y la cita referidos.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearDiagnosticoRequest request, CancellationToken ct)
    {
        if (!await _db.HistorialesClinicos.AnyAsync(h => h.IdHistorial == request.IdHistorial, ct))
        {
            return ApiProblems.Validacion($"No existe un historial con id {request.IdHistorial}.");
        }

        if (!await _db.Citas.AnyAsync(c => c.IdCita == request.IdCita, ct))
        {
            return ApiProblems.Validacion($"No existe una cita con id {request.IdCita}.");
        }

        var diagnostico = new Diagnostico
        {
            IdHistorial = request.IdHistorial,
            IdCita = request.IdCita,
            Descripcion = request.Descripcion,
            CodigoCie10 = request.CodigoCie10,
            Alergias = request.Alergias,
            Fecha = request.Fecha ?? DateTime.UtcNow,
        };

        _db.Diagnosticos.Add(diagnostico);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrorTraductor.Traducir(ex) is { } problema)
        {
            return problema;
        }

        return CreatedAtAction(
            nameof(Obtener), new { id = diagnostico.IdDiagnosticos }, Mapear(diagnostico));
    }

    /// <summary>Actualiza los datos de un diagnostico existente.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarDiagnosticoRequest request, CancellationToken ct)
    {
        var diagnostico = await _db.Diagnosticos.FirstOrDefaultAsync(d => d.IdDiagnosticos == id, ct);
        if (diagnostico is null)
        {
            return ApiProblems.NoEncontrado($"No existe un diagnostico con id {id}.");
        }

        diagnostico.Descripcion = request.Descripcion;
        diagnostico.CodigoCie10 = request.CodigoCie10;
        diagnostico.Alergias = request.Alergias;
        if (request.Fecha is DateTime fecha)
        {
            diagnostico.Fecha = fecha;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(Mapear(diagnostico));
    }

    /// <summary>Elimina un diagnostico. Operacion reservada al rol Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var diagnostico = await _db.Diagnosticos.FirstOrDefaultAsync(d => d.IdDiagnosticos == id, ct);
        if (diagnostico is null)
        {
            return ApiProblems.NoEncontrado($"No existe un diagnostico con id {id}.");
        }

        _db.Diagnosticos.Remove(diagnostico);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static DiagnosticoResponse Mapear(Diagnostico d) => new()
    {
        IdDiagnosticos = d.IdDiagnosticos,
        IdHistorial = d.IdHistorial,
        IdCita = d.IdCita,
        Descripcion = d.Descripcion,
        CodigoCie10 = d.CodigoCie10,
        Alergias = d.Alergias,
        Fecha = d.Fecha,
    };
}
