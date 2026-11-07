using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Historiales;

/// <summary>
/// Endpoints de historial clinico (uno por paciente). Lectura para cualquier rol
/// autenticado; escritura para Admin y Medico.
/// </summary>
[ApiController]
[Route("api/historiales")]
[Authorize]
public class HistorialClinicoController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public HistorialClinicoController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista historiales con filtro opcional por paciente.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idPaciente, CancellationToken ct)
    {
        var consulta = _db.HistorialesClinicos.AsNoTracking().AsQueryable();

        if (idPaciente is int paciente)
        {
            consulta = consulta.Where(h => h.IdPaciente == paciente);
        }

        var historiales = await consulta
            .OrderBy(h => h.IdHistorial)
            .Select(h => Mapear(h))
            .ToListAsync(ct);

        return Ok(historiales);
    }

    /// <summary>Obtiene un historial por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var historial = await _db.HistorialesClinicos.AsNoTracking()
            .FirstOrDefaultAsync(h => h.IdHistorial == id, ct);

        return historial is null
            ? ApiProblems.NoEncontrado($"No existe un historial con id {id}.")
            : Ok(Mapear(historial));
    }

    /// <summary>Abre un historial para un paciente (uno por paciente).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearHistorialRequest request, CancellationToken ct)
    {
        if (!await _db.Pacientes.AnyAsync(p => p.IdPaciente == request.IdPaciente, ct))
        {
            return ApiProblems.Validacion($"No existe un paciente con id {request.IdPaciente}.");
        }

        if (await _db.HistorialesClinicos.AnyAsync(h => h.IdPaciente == request.IdPaciente, ct))
        {
            return ApiProblems.Conflicto("El paciente ya tiene un historial clinico.");
        }

        var historial = new HistorialClinico
        {
            IdPaciente = request.IdPaciente,
            FechaApertura = request.FechaApertura,
            Antecedentes = request.Antecedentes,
        };

        _db.HistorialesClinicos.Add(historial);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = historial.IdHistorial }, Mapear(historial));
    }

    /// <summary>Actualiza la fecha de apertura o los antecedentes del historial.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarHistorialRequest request, CancellationToken ct)
    {
        var historial = await _db.HistorialesClinicos.FirstOrDefaultAsync(h => h.IdHistorial == id, ct);
        if (historial is null)
        {
            return ApiProblems.NoEncontrado($"No existe un historial con id {id}.");
        }

        historial.FechaApertura = request.FechaApertura;
        historial.Antecedentes = request.Antecedentes;

        await _db.SaveChangesAsync(ct);
        return Ok(Mapear(historial));
    }

    private static HistorialResponse Mapear(HistorialClinico h) => new()
    {
        IdHistorial = h.IdHistorial,
        IdPaciente = h.IdPaciente,
        FechaApertura = h.FechaApertura,
        Antecedentes = h.Antecedentes,
    };
}
