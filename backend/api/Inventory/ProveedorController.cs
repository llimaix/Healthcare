using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Inventory;

/// <summary>Datos para registrar un proveedor.</summary>
public class CrearProveedorRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(150)]
    public string Nombre { get; set; } = null!;

    [MaxLength(20)]
    public string? Nit { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(150)]
    public string? Correo { get; set; }
}

/// <summary>Representacion publica de un proveedor.</summary>
public class ProveedorResponse
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Nit { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}

/// <summary>
/// Endpoints de proveedores (soporte minimo: consulta + alta). Lectura para cualquier
/// rol autenticado; escritura reservada al rol Admin.
/// </summary>
[ApiController]
[Route("api/proveedores")]
[Authorize]
public class ProveedorController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public ProveedorController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista los proveedores.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var proveedores = await _db.Proveedores.AsNoTracking()
            .OrderBy(p => p.IdProveedor)
            .Select(p => Mapear(p))
            .ToListAsync(ct);

        return Ok(proveedores);
    }

    /// <summary>Obtiene un proveedor por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var proveedor = await _db.Proveedores.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProveedor == id, ct);

        return proveedor is null
            ? ApiProblems.NoEncontrado($"No existe un proveedor con id {id}.")
            : Ok(Mapear(proveedor));
    }

    /// <summary>Crea un proveedor.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearProveedorRequest request, CancellationToken ct)
    {
        var proveedor = new Proveedor
        {
            Nombre = request.Nombre,
            Nit = request.Nit,
            Telefono = request.Telefono,
            Correo = request.Correo,
        };

        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = proveedor.IdProveedor }, Mapear(proveedor));
    }

    private static ProveedorResponse Mapear(Proveedor p) => new()
    {
        IdProveedor = p.IdProveedor,
        Nombre = p.Nombre,
        Nit = p.Nit,
        Telefono = p.Telefono,
        Correo = p.Correo,
    };
}
