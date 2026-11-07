using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Billing;

/// <summary>Datos para registrar un tipo de cobertura.</summary>
public class CrearTipoCoberturaRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    public decimal PorcentajeCobertura { get; set; }
}

/// <summary>Representacion publica de un tipo de cobertura.</summary>
public class TipoCoberturaResponse
{
    public int IdTipoCobertura { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PorcentajeCobertura { get; set; }
}

/// <summary>
/// Endpoints de tipos de cobertura (soporte minimo: consulta + alta). Lectura para
/// cualquier rol autenticado; escritura reservada al rol Admin.
/// </summary>
[ApiController]
[Route("api/tipos-cobertura")]
[Authorize]
public class TipoCoberturaController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public TipoCoberturaController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista los tipos de cobertura.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var tipos = await _db.TiposCobertura.AsNoTracking()
            .OrderBy(t => t.IdTipoCobertura)
            .Select(t => Mapear(t))
            .ToListAsync(ct);

        return Ok(tipos);
    }

    /// <summary>Obtiene un tipo de cobertura por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var tipo = await _db.TiposCobertura.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTipoCobertura == id, ct);

        return tipo is null
            ? ApiProblems.NoEncontrado($"No existe un tipo de cobertura con id {id}.")
            : Ok(Mapear(tipo));
    }

    /// <summary>Crea un tipo de cobertura.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearTipoCoberturaRequest request, CancellationToken ct)
    {
        var tipo = new TipoCobertura
        {
            Nombre = request.Nombre,
            PorcentajeCobertura = request.PorcentajeCobertura,
        };

        _db.TiposCobertura.Add(tipo);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = tipo.IdTipoCobertura }, Mapear(tipo));
    }

    private static TipoCoberturaResponse Mapear(TipoCobertura t) => new()
    {
        IdTipoCobertura = t.IdTipoCobertura,
        Nombre = t.Nombre,
        PorcentajeCobertura = t.PorcentajeCobertura,
    };
}
