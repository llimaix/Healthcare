using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Inventory;

/// <summary>Datos para registrar un movimiento de kardex.</summary>
public class CrearKardexRequest
{
    [Required]
    public int IdInsumo { get; set; }

    public int? IdProveedor { get; set; }

    [MaxLength(20)]
    public string? TipoMovimiento { get; set; }

    public int Cantidad { get; set; }

    public DateTime? Fecha { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }
}

/// <summary>Representacion publica de un movimiento de kardex.</summary>
public class KardexResponse
{
    public int IdKardex { get; set; }
    public int IdInsumo { get; set; }
    public int? IdProveedor { get; set; }
    public string? TipoMovimiento { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }
}

/// <summary>
/// Endpoints de kardex (soporte minimo: consulta + alta). Lectura para cualquier rol
/// autenticado; escritura reservada al rol Admin.
/// </summary>
[ApiController]
[Route("api/kardex")]
[Authorize]
public class KardexController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public KardexController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista movimientos con filtro opcional por insumo.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idInsumo, CancellationToken ct)
    {
        var consulta = _db.Kardex.AsNoTracking().AsQueryable();
        if (idInsumo is int insumo)
        {
            consulta = consulta.Where(k => k.IdInsumo == insumo);
        }

        var movimientos = await consulta.OrderBy(k => k.IdKardex).Select(k => Mapear(k)).ToListAsync(ct);
        return Ok(movimientos);
    }

    /// <summary>Obtiene un movimiento por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var movimiento = await _db.Kardex.AsNoTracking().FirstOrDefaultAsync(k => k.IdKardex == id, ct);
        return movimiento is null
            ? ApiProblems.NoEncontrado($"No existe un movimiento de kardex con id {id}.")
            : Ok(Mapear(movimiento));
    }

    /// <summary>Registra un movimiento. Valida insumo y, si se indica, proveedor.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearKardexRequest request, CancellationToken ct)
    {
        if (!await _db.Insumos.AnyAsync(i => i.IdInsumo == request.IdInsumo, ct))
        {
            return ApiProblems.Validacion($"No existe un insumo con id {request.IdInsumo}.");
        }

        if (request.IdProveedor is int idProveedor
            && !await _db.Proveedores.AnyAsync(p => p.IdProveedor == idProveedor, ct))
        {
            return ApiProblems.Validacion($"No existe un proveedor con id {idProveedor}.");
        }

        var movimiento = new Kardex
        {
            IdInsumo = request.IdInsumo,
            IdProveedor = request.IdProveedor,
            TipoMovimiento = request.TipoMovimiento,
            Cantidad = request.Cantidad,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            Referencia = request.Referencia,
        };

        _db.Kardex.Add(movimiento);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = movimiento.IdKardex }, Mapear(movimiento));
    }

    private static KardexResponse Mapear(Kardex k) => new()
    {
        IdKardex = k.IdKardex,
        IdInsumo = k.IdInsumo,
        IdProveedor = k.IdProveedor,
        TipoMovimiento = k.TipoMovimiento,
        Cantidad = k.Cantidad,
        Fecha = k.Fecha,
        Referencia = k.Referencia,
    };
}
