using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Recetas;

/// <summary>Datos para registrar una receta.</summary>
public class CrearRecetaRequest
{
    [Required]
    public int IdCita { get; set; }

    public int? IdDiagnostico { get; set; }

    [Required]
    public DateOnly FechaEmision { get; set; }

    public string? Indicaciones { get; set; }
}

/// <summary>Datos para agregar una linea de receta.</summary>
public class CrearRecetaDetalleRequest
{
    [Required]
    public int IdInsumo { get; set; }

    [MaxLength(100)]
    public string? Dosis { get; set; }

    [MaxLength(100)]
    public string? Frecuencia { get; set; }

    public int DuracionDias { get; set; }
}

/// <summary>Representacion publica de una receta.</summary>
public class RecetaResponse
{
    public int IdReceta { get; set; }
    public int IdCita { get; set; }
    public int? IdDiagnostico { get; set; }
    public DateOnly FechaEmision { get; set; }
    public string? Indicaciones { get; set; }
}

/// <summary>Representacion publica de una linea de receta.</summary>
public class RecetaDetalleResponse
{
    public int IdRecetaDetalle { get; set; }
    public int IdReceta { get; set; }
    public int IdInsumo { get; set; }
    public string? Dosis { get; set; }
    public string? Frecuencia { get; set; }
    public int DuracionDias { get; set; }
}

/// <summary>
/// Endpoints de recetas y su detalle (soporte minimo: consulta + alta). Lectura para
/// cualquier rol autenticado; escritura para Admin y Medico.
/// </summary>
[ApiController]
[Route("api/recetas")]
[Authorize]
public class RecetaController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public RecetaController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista recetas con filtro opcional por cita.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idCita, CancellationToken ct)
    {
        var consulta = _db.Recetas.AsNoTracking().AsQueryable();
        if (idCita is int cita)
        {
            consulta = consulta.Where(r => r.IdCita == cita);
        }

        var recetas = await consulta.OrderBy(r => r.IdReceta).Select(r => Mapear(r)).ToListAsync(ct);
        return Ok(recetas);
    }

    /// <summary>Obtiene una receta por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var receta = await _db.Recetas.AsNoTracking().FirstOrDefaultAsync(r => r.IdReceta == id, ct);
        return receta is null
            ? ApiProblems.NoEncontrado($"No existe una receta con id {id}.")
            : Ok(Mapear(receta));
    }

    /// <summary>Crea una receta. Valida que exista la cita; el diagnostico es opcional.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearRecetaRequest request, CancellationToken ct)
    {
        if (!await _db.Citas.AnyAsync(c => c.IdCita == request.IdCita, ct))
        {
            return ApiProblems.Validacion($"No existe una cita con id {request.IdCita}.");
        }

        if (request.IdDiagnostico is int idDiagnostico
            && !await _db.Diagnosticos.AnyAsync(d => d.IdDiagnosticos == idDiagnostico, ct))
        {
            return ApiProblems.Validacion($"No existe un diagnostico con id {idDiagnostico}.");
        }

        var receta = new Receta
        {
            IdCita = request.IdCita,
            IdDiagnostico = request.IdDiagnostico,
            FechaEmision = request.FechaEmision,
            Indicaciones = request.Indicaciones,
        };

        _db.Recetas.Add(receta);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = receta.IdReceta }, Mapear(receta));
    }

    /// <summary>Lista las lineas de una receta.</summary>
    [HttpGet("{id:int}/detalles")]
    public async Task<IActionResult> ListarDetalles(int id, CancellationToken ct)
    {
        if (!await _db.Recetas.AnyAsync(r => r.IdReceta == id, ct))
        {
            return ApiProblems.NoEncontrado($"No existe una receta con id {id}.");
        }

        var detalles = await _db.RecetaDetalles.AsNoTracking()
            .Where(d => d.IdReceta == id)
            .OrderBy(d => d.IdRecetaDetalle)
            .Select(d => MapearDetalle(d))
            .ToListAsync(ct);

        return Ok(detalles);
    }

    /// <summary>Agrega una linea a una receta. Valida que existan la receta y el insumo.</summary>
    [HttpPost("{id:int}/detalles")]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> CrearDetalle(
        int id, [FromBody] CrearRecetaDetalleRequest request, CancellationToken ct)
    {
        if (!await _db.Recetas.AnyAsync(r => r.IdReceta == id, ct))
        {
            return ApiProblems.NoEncontrado($"No existe una receta con id {id}.");
        }

        if (!await _db.Insumos.AnyAsync(i => i.IdInsumo == request.IdInsumo, ct))
        {
            return ApiProblems.Validacion($"No existe un insumo con id {request.IdInsumo}.");
        }

        var detalle = new RecetaDetalle
        {
            IdReceta = id,
            IdInsumo = request.IdInsumo,
            Dosis = request.Dosis,
            Frecuencia = request.Frecuencia,
            DuracionDias = request.DuracionDias,
        };

        _db.RecetaDetalles.Add(detalle);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListarDetalles), new { id }, MapearDetalle(detalle));
    }

    private static RecetaResponse Mapear(Receta r) => new()
    {
        IdReceta = r.IdReceta,
        IdCita = r.IdCita,
        IdDiagnostico = r.IdDiagnostico,
        FechaEmision = r.FechaEmision,
        Indicaciones = r.Indicaciones,
    };

    private static RecetaDetalleResponse MapearDetalle(RecetaDetalle d) => new()
    {
        IdRecetaDetalle = d.IdRecetaDetalle,
        IdReceta = d.IdReceta,
        IdInsumo = d.IdInsumo,
        Dosis = d.Dosis,
        Frecuencia = d.Frecuencia,
        DuracionDias = d.DuracionDias,
    };
}
