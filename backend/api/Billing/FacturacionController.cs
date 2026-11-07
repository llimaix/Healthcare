using System.ComponentModel.DataAnnotations;
using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Billing;

/// <summary>Datos para registrar una factura.</summary>
public class CrearFacturacionRequest
{
    [Required]
    public int IdPaciente { get; set; }

    [Required]
    public int IdCita { get; set; }

    public int? IdAsegurado { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal Subtotal { get; set; }

    public decimal MontoCobertura { get; set; }

    public decimal Total { get; set; }

    [MaxLength(30)]
    public string? Estado { get; set; }
}

/// <summary>Datos para agregar una linea a una factura.</summary>
public class CrearFacturacionDetalleRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(150)]
    public string Concepto { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}

/// <summary>Representacion publica de una factura.</summary>
public class FacturacionResponse
{
    public int IdFactura { get; set; }
    public int IdPaciente { get; set; }
    public int IdCita { get; set; }
    public int? IdAsegurado { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MontoCobertura { get; set; }
    public decimal Total { get; set; }
    public string? Estado { get; set; }
}

/// <summary>Representacion publica de una linea de factura.</summary>
public class FacturacionDetalleResponse
{
    public int IdDetalle { get; set; }
    public int IdFactura { get; set; }
    public string Concepto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

/// <summary>
/// Endpoints de facturacion y su detalle (soporte minimo: consulta + alta). Lectura para
/// cualquier rol autenticado; escritura para Admin y Recepcion.
/// </summary>
[ApiController]
[Route("api/facturaciones")]
[Authorize]
public class FacturacionController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public FacturacionController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista facturas con filtro opcional por paciente.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idPaciente, CancellationToken ct)
    {
        var consulta = _db.Facturaciones.AsNoTracking().AsQueryable();
        if (idPaciente is int paciente)
        {
            consulta = consulta.Where(f => f.IdPaciente == paciente);
        }

        var facturas = await consulta.OrderBy(f => f.IdFactura).Select(f => Mapear(f)).ToListAsync(ct);
        return Ok(facturas);
    }

    /// <summary>Obtiene una factura por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var factura = await _db.Facturaciones.AsNoTracking().FirstOrDefaultAsync(f => f.IdFactura == id, ct);
        return factura is null
            ? ApiProblems.NoEncontrado($"No existe una factura con id {id}.")
            : Ok(Mapear(factura));
    }

    /// <summary>Crea una factura. Valida paciente, cita y, si se indica, el asociado.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Recepcion")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearFacturacionRequest request, CancellationToken ct)
    {
        if (!await _db.Pacientes.AnyAsync(p => p.IdPaciente == request.IdPaciente, ct))
        {
            return ApiProblems.Validacion($"No existe un paciente con id {request.IdPaciente}.");
        }

        if (!await _db.Citas.AnyAsync(c => c.IdCita == request.IdCita, ct))
        {
            return ApiProblems.Validacion($"No existe una cita con id {request.IdCita}.");
        }

        if (request.IdAsegurado is int idAsegurado
            && !await _db.AsociadosSeguro.AnyAsync(a => a.IdAsegurado == idAsegurado, ct))
        {
            return ApiProblems.Validacion($"No existe un asociado de seguro con id {idAsegurado}.");
        }

        var factura = new Facturacion
        {
            IdPaciente = request.IdPaciente,
            IdCita = request.IdCita,
            IdAsegurado = request.IdAsegurado,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            Subtotal = request.Subtotal,
            MontoCobertura = request.MontoCobertura,
            Total = request.Total,
            Estado = request.Estado,
        };

        _db.Facturaciones.Add(factura);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = factura.IdFactura }, Mapear(factura));
    }

    /// <summary>Lista las lineas de una factura.</summary>
    [HttpGet("{id:int}/detalles")]
    public async Task<IActionResult> ListarDetalles(int id, CancellationToken ct)
    {
        if (!await _db.Facturaciones.AnyAsync(f => f.IdFactura == id, ct))
        {
            return ApiProblems.NoEncontrado($"No existe una factura con id {id}.");
        }

        var detalles = await _db.FacturacionDetalles.AsNoTracking()
            .Where(d => d.IdFactura == id)
            .OrderBy(d => d.IdDetalle)
            .Select(d => MapearDetalle(d))
            .ToListAsync(ct);

        return Ok(detalles);
    }

    /// <summary>Agrega una linea a una factura existente.</summary>
    [HttpPost("{id:int}/detalles")]
    [Authorize(Roles = "Admin,Recepcion")]
    public async Task<IActionResult> CrearDetalle(
        int id, [FromBody] CrearFacturacionDetalleRequest request, CancellationToken ct)
    {
        if (!await _db.Facturaciones.AnyAsync(f => f.IdFactura == id, ct))
        {
            return ApiProblems.NoEncontrado($"No existe una factura con id {id}.");
        }

        var detalle = new FacturacionDetalle
        {
            IdFactura = id,
            Concepto = request.Concepto,
            Cantidad = request.Cantidad,
            PrecioUnitario = request.PrecioUnitario,
        };

        _db.FacturacionDetalles.Add(detalle);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListarDetalles), new { id }, MapearDetalle(detalle));
    }

    private static FacturacionResponse Mapear(Facturacion f) => new()
    {
        IdFactura = f.IdFactura,
        IdPaciente = f.IdPaciente,
        IdCita = f.IdCita,
        IdAsegurado = f.IdAsegurado,
        Fecha = f.Fecha,
        Subtotal = f.Subtotal,
        MontoCobertura = f.MontoCobertura,
        Total = f.Total,
        Estado = f.Estado,
    };

    private static FacturacionDetalleResponse MapearDetalle(FacturacionDetalle d) => new()
    {
        IdDetalle = d.IdDetalle,
        IdFactura = d.IdFactura,
        Concepto = d.Concepto,
        Cantidad = d.Cantidad,
        PrecioUnitario = d.PrecioUnitario,
    };
}
