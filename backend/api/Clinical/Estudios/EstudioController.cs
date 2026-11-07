using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Estudios;

/// <summary>Datos para registrar un estudio.</summary>
public class CrearEstudioRequest
{
    [Required]
    public int IdCita { get; set; }

    [MaxLength(100)]
    public string? Tipo { get; set; }

    public string? Descripcion { get; set; }

    [Required]
    public DateOnly FechaSolicitud { get; set; }

    [MaxLength(30)]
    public string? Estado { get; set; }
}

/// <summary>Representacion publica de un estudio.</summary>
public class EstudioResponse
{
    public int IdEstudio { get; set; }
    public int IdCita { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaSolicitud { get; set; }
    public string? Estado { get; set; }
}

/// <summary>
/// Endpoints de estudios (soporte minimo: consulta + alta). Lectura para cualquier rol
/// autenticado; escritura para Admin y Medico.
/// </summary>
[ApiController]
[Route("api/estudios")]
[Authorize]
public class EstudioController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public EstudioController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista estudios con filtro opcional por cita.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idCita, CancellationToken ct)
    {
        var consulta = _db.Estudios.AsNoTracking().AsQueryable();
        if (idCita is int cita)
        {
            consulta = consulta.Where(e => e.IdCita == cita);
        }

        var estudios = await consulta.OrderBy(e => e.IdEstudio).Select(e => Mapear(e)).ToListAsync(ct);
        return Ok(estudios);
    }

    /// <summary>Obtiene un estudio por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var estudio = await _db.Estudios.AsNoTracking().FirstOrDefaultAsync(e => e.IdEstudio == id, ct);
        return estudio is null
            ? ApiProblems.NoEncontrado($"No existe un estudio con id {id}.")
            : Ok(Mapear(estudio));
    }

    /// <summary>Crea un estudio. Valida que exista la cita referida.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearEstudioRequest request, CancellationToken ct)
    {
        if (!await _db.Citas.AnyAsync(c => c.IdCita == request.IdCita, ct))
        {
            return ApiProblems.Validacion($"No existe una cita con id {request.IdCita}.");
        }

        var estudio = new Estudio
        {
            IdCita = request.IdCita,
            Tipo = request.Tipo,
            Descripcion = request.Descripcion,
            FechaSolicitud = request.FechaSolicitud,
            Estado = request.Estado,
        };

        _db.Estudios.Add(estudio);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = estudio.IdEstudio }, Mapear(estudio));
    }

    private static EstudioResponse Mapear(Estudio e) => new()
    {
        IdEstudio = e.IdEstudio,
        IdCita = e.IdCita,
        Tipo = e.Tipo,
        Descripcion = e.Descripcion,
        FechaSolicitud = e.FechaSolicitud,
        Estado = e.Estado,
    };
}
