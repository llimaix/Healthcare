using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Pacientes;

/// <summary>
/// Endpoints de pacientes. Alta/consulta/actualizacion para Admin, Medico y Recepcion;
/// lectura para cualquier rol autenticado. "Eliminar" es borrado logico (Activo=false),
/// reservado al rol Admin; nunca hay DELETE fisico.
/// </summary>
[ApiController]
[Route("api/pacientes")]
[Authorize]
public class PacienteController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public PacienteController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista los pacientes activos.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var pacientes = await _db.Pacientes.AsNoTracking()
            .OrderBy(p => p.IdPaciente)
            .Select(p => Mapear(p))
            .ToListAsync(ct);

        return Ok(pacientes);
    }

    /// <summary>Obtiene un paciente activo por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var paciente = await _db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPaciente == id, ct);

        return paciente is null
            ? ApiProblems.NoEncontrado($"No existe un paciente con id {id}.")
            : Ok(Mapear(paciente));
    }

    /// <summary>Crea un paciente. El genero solo acepta 'F' o 'M' y el CUI es unico.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico,Recepcion")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearPacienteRequest request, CancellationToken ct)
    {
        if (!GeneroValido(request.Genero))
        {
            return ApiProblems.Validacion("El genero solo acepta los valores 'F' o 'M'.");
        }

        if (await _db.Pacientes.IgnoreQueryFilters().AnyAsync(p => p.Cui == request.Cui, ct))
        {
            return ApiProblems.Conflicto("Ya existe un paciente con ese CUI.");
        }

        var paciente = new Paciente
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Cui = request.Cui,
            FechaNacimiento = request.FechaNacimiento,
            Nit = request.Nit,
            Telefono = request.Telefono,
            CorreoElectronico = request.CorreoElectronico,
            Genero = request.Genero,
            TipoSangre = request.TipoSangre,
            Activo = true,
        };

        _db.Pacientes.Add(paciente);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrorTraductor.Traducir(ex) is { } problema)
        {
            return problema;
        }

        return CreatedAtAction(nameof(Obtener), new { id = paciente.IdPaciente }, Mapear(paciente));
    }

    /// <summary>Actualiza los datos de un paciente activo.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Medico,Recepcion")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarPacienteRequest request, CancellationToken ct)
    {
        if (!GeneroValido(request.Genero))
        {
            return ApiProblems.Validacion("El genero solo acepta los valores 'F' o 'M'.");
        }

        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.IdPaciente == id, ct);
        if (paciente is null)
        {
            return ApiProblems.NoEncontrado($"No existe un paciente con id {id}.");
        }

        paciente.Nombre = request.Nombre;
        paciente.Apellido = request.Apellido;
        paciente.Nit = request.Nit;
        paciente.Telefono = request.Telefono;
        paciente.CorreoElectronico = request.CorreoElectronico;
        paciente.Genero = request.Genero;
        paciente.TipoSangre = request.TipoSangre;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrorTraductor.Traducir(ex) is { } problema)
        {
            return problema;
        }

        return Ok(Mapear(paciente));
    }

    /// <summary>Borrado logico: marca el paciente como inactivo. Reservado al rol Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.IdPaciente == id, ct);
        if (paciente is null)
        {
            return ApiProblems.NoEncontrado($"No existe un paciente con id {id}.");
        }

        paciente.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static bool GeneroValido(char genero) => genero is 'F' or 'M';

    private static PacienteResponse Mapear(Paciente p) => new()
    {
        IdPaciente = p.IdPaciente,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        Cui = p.Cui,
        FechaNacimiento = p.FechaNacimiento,
        Nit = p.Nit,
        Telefono = p.Telefono,
        CorreoElectronico = p.CorreoElectronico,
        Genero = p.Genero,
        TipoSangre = p.TipoSangre,
    };
}
