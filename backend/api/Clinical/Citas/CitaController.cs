using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Citas;

/// <summary>
/// Endpoints de citas. Lectura para cualquier rol autenticado; alta/actualizacion para
/// Admin, Medico y Recepcion; borrado reservado al rol Admin.
/// </summary>
[ApiController]
[Route("api/citas")]
[Authorize]
public class CitaController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public CitaController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista citas con filtros opcionales por paciente o medico.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idPaciente, [FromQuery] int? idMedico, CancellationToken ct)
    {
        var consulta = _db.Citas.AsNoTracking().AsQueryable();

        if (idPaciente is int paciente)
        {
            consulta = consulta.Where(c => c.IdPaciente == paciente);
        }

        if (idMedico is int medico)
        {
            consulta = consulta.Where(c => c.IdMedico == medico);
        }

        var citas = await consulta
            .OrderBy(c => c.IdCita)
            .Select(c => Mapear(c))
            .ToListAsync(ct);

        return Ok(citas);
    }

    /// <summary>Obtiene una cita por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var cita = await _db.Citas.AsNoTracking().FirstOrDefaultAsync(c => c.IdCita == id, ct);
        return cita is null
            ? ApiProblems.NoEncontrado($"No existe una cita con id {id}.")
            : Ok(Mapear(cita));
    }

    /// <summary>Crea una cita. Valida que existan el medico y el paciente referidos.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico,Recepcion")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearCitaRequest request, CancellationToken ct)
    {
        if (!await _db.Medicos.AnyAsync(m => m.IdMedico == request.IdMedico, ct))
        {
            return ApiProblems.Validacion($"No existe un medico con id {request.IdMedico}.");
        }

        if (!await _db.Pacientes.AnyAsync(p => p.IdPaciente == request.IdPaciente, ct))
        {
            return ApiProblems.Validacion($"No existe un paciente con id {request.IdPaciente}.");
        }

        var cita = new Cita
        {
            IdMedico = request.IdMedico,
            IdPaciente = request.IdPaciente,
            FechaCita = request.FechaCita,
            Hora = request.Hora,
            Motivo = request.Motivo,
            Estado = request.Estado,
        };

        _db.Citas.Add(cita);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = cita.IdCita }, Mapear(cita));
    }

    /// <summary>Actualiza fecha, hora, motivo o estado de una cita existente.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Medico,Recepcion")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarCitaRequest request, CancellationToken ct)
    {
        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.IdCita == id, ct);
        if (cita is null)
        {
            return ApiProblems.NoEncontrado($"No existe una cita con id {id}.");
        }

        cita.FechaCita = request.FechaCita;
        cita.Hora = request.Hora;
        cita.Motivo = request.Motivo;
        cita.Estado = request.Estado;

        await _db.SaveChangesAsync(ct);
        return Ok(Mapear(cita));
    }

    /// <summary>Elimina una cita. Operacion reservada al rol Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.IdCita == id, ct);
        if (cita is null)
        {
            return ApiProblems.NoEncontrado($"No existe una cita con id {id}.");
        }

        _db.Citas.Remove(cita);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CitaResponse Mapear(Cita c) => new()
    {
        IdCita = c.IdCita,
        IdMedico = c.IdMedico,
        IdPaciente = c.IdPaciente,
        FechaCita = c.FechaCita,
        Hora = c.Hora,
        Motivo = c.Motivo,
        Estado = c.Estado,
    };
}
