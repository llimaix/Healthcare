using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Billing;

/// <summary>Datos para asociar un paciente a un plan de cobertura.</summary>
public class CrearAsociadoSeguroRequest
{
    [Required]
    public int IdPaciente { get; set; }

    [Required]
    public int IdTipoCobertura { get; set; }

    [MaxLength(100)]
    public string? Aseguradora { get; set; }

    [MaxLength(50)]
    public string? NumeroPoliza { get; set; }

    [Required]
    public DateOnly VigenciaDesde { get; set; }

    [Required]
    public DateOnly VigenciaHasta { get; set; }
}

/// <summary>Representacion publica de un asociado de seguro.</summary>
public class AsociadoSeguroResponse
{
    public int IdAsegurado { get; set; }
    public int IdPaciente { get; set; }
    public int IdTipoCobertura { get; set; }
    public string? Aseguradora { get; set; }
    public string? NumeroPoliza { get; set; }
    public DateOnly VigenciaDesde { get; set; }
    public DateOnly VigenciaHasta { get; set; }
}

/// <summary>
/// Endpoints de asociados de seguro (soporte minimo: consulta + alta). Lectura para
/// cualquier rol autenticado; escritura para Admin y Recepcion.
/// </summary>
[ApiController]
[Route("api/asociados-seguro")]
[Authorize]
public class AsociadoSeguroController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public AsociadoSeguroController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista asociados con filtro opcional por paciente.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idPaciente, CancellationToken ct)
    {
        var consulta = _db.AsociadosSeguro.AsNoTracking().AsQueryable();
        if (idPaciente is int paciente)
        {
            consulta = consulta.Where(a => a.IdPaciente == paciente);
        }

        var asociados = await consulta.OrderBy(a => a.IdAsegurado).Select(a => Mapear(a)).ToListAsync(ct);
        return Ok(asociados);
    }

    /// <summary>Obtiene un asociado de seguro por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var asociado = await _db.AsociadosSeguro.AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdAsegurado == id, ct);

        return asociado is null
            ? ApiProblems.NoEncontrado($"No existe un asociado de seguro con id {id}.")
            : Ok(Mapear(asociado));
    }

    /// <summary>Crea un asociado de seguro. Valida paciente y tipo de cobertura.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Recepcion")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearAsociadoSeguroRequest request, CancellationToken ct)
    {
        if (!await _db.Pacientes.AnyAsync(p => p.IdPaciente == request.IdPaciente, ct))
        {
            return ApiProblems.Validacion($"No existe un paciente con id {request.IdPaciente}.");
        }

        if (!await _db.TiposCobertura.AnyAsync(t => t.IdTipoCobertura == request.IdTipoCobertura, ct))
        {
            return ApiProblems.Validacion($"No existe un tipo de cobertura con id {request.IdTipoCobertura}.");
        }

        var asociado = new AsociadoSeguro
        {
            IdPaciente = request.IdPaciente,
            IdTipoCobertura = request.IdTipoCobertura,
            Aseguradora = request.Aseguradora,
            NumeroPoliza = request.NumeroPoliza,
            VigenciaDesde = request.VigenciaDesde,
            VigenciaHasta = request.VigenciaHasta,
        };

        _db.AsociadosSeguro.Add(asociado);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = asociado.IdAsegurado }, Mapear(asociado));
    }

    private static AsociadoSeguroResponse Mapear(AsociadoSeguro a) => new()
    {
        IdAsegurado = a.IdAsegurado,
        IdPaciente = a.IdPaciente,
        IdTipoCobertura = a.IdTipoCobertura,
        Aseguradora = a.Aseguradora,
        NumeroPoliza = a.NumeroPoliza,
        VigenciaDesde = a.VigenciaDesde,
        VigenciaHasta = a.VigenciaHasta,
    };
}
