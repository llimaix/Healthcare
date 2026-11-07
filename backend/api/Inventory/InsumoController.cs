using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Inventory;

/// <summary>Datos para registrar un insumo.</summary>
public class CrearInsumoRequest
{
    [Required]
    public int IdProveedor { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(150)]
    public string Nombre { get; set; } = null!;

    [MaxLength(50)]
    public string? Tipo { get; set; }

    public decimal Precio { get; set; }

    public int StockMinimo { get; set; }
}

/// <summary>Representacion publica de un insumo.</summary>
public class InsumoResponse
{
    public int IdInsumo { get; set; }
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Tipo { get; set; }
    public decimal Precio { get; set; }
    public int StockMinimo { get; set; }
}

/// <summary>
/// Endpoints de insumos (soporte minimo: consulta + alta). Lectura para cualquier rol
/// autenticado; escritura reservada al rol Admin.
/// </summary>
[ApiController]
[Route("api/insumos")]
[Authorize]
public class InsumoController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public InsumoController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista insumos con filtro opcional por proveedor.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idProveedor, CancellationToken ct)
    {
        var consulta = _db.Insumos.AsNoTracking().AsQueryable();
        if (idProveedor is int proveedor)
        {
            consulta = consulta.Where(i => i.IdProveedor == proveedor);
        }

        var insumos = await consulta.OrderBy(i => i.IdInsumo).Select(i => Mapear(i)).ToListAsync(ct);
        return Ok(insumos);
    }

    /// <summary>Obtiene un insumo por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var insumo = await _db.Insumos.AsNoTracking().FirstOrDefaultAsync(i => i.IdInsumo == id, ct);
        return insumo is null
            ? ApiProblems.NoEncontrado($"No existe un insumo con id {id}.")
            : Ok(Mapear(insumo));
    }

    /// <summary>Crea un insumo. Valida que exista el proveedor referido.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearInsumoRequest request, CancellationToken ct)
    {
        if (!await _db.Proveedores.AnyAsync(p => p.IdProveedor == request.IdProveedor, ct))
        {
            return ApiProblems.Validacion($"No existe un proveedor con id {request.IdProveedor}.");
        }

        var insumo = new Insumo
        {
            IdProveedor = request.IdProveedor,
            Nombre = request.Nombre,
            Tipo = request.Tipo,
            Precio = request.Precio,
            StockMinimo = request.StockMinimo,
        };

        _db.Insumos.Add(insumo);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = insumo.IdInsumo }, Mapear(insumo));
    }

    private static InsumoResponse Mapear(Insumo i) => new()
    {
        IdInsumo = i.IdInsumo,
        IdProveedor = i.IdProveedor,
        Nombre = i.Nombre,
        Tipo = i.Tipo,
        Precio = i.Precio,
        StockMinimo = i.StockMinimo,
    };
}
