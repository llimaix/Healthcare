using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.Medicos;

/// <summary>
/// Endpoints de medicos. Lectura para cualquier rol autenticado; escritura y borrado
/// reservados al rol Admin. El numero de colegiado es unico.
/// </summary>
[ApiController]
[Route("api/medicos")]
[Authorize]
public class MedicoController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public MedicoController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista los medicos.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var medicos = await _db.Medicos.AsNoTracking()
            .OrderBy(m => m.IdMedico)
            .Select(m => Mapear(m))
            .ToListAsync(ct);

        return Ok(medicos);
    }

    /// <summary>Obtiene un medico por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var medico = await _db.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.IdMedico == id, ct);
        return medico is null
            ? ApiProblems.NoEncontrado($"No existe un medico con id {id}.")
            : Ok(Mapear(medico));
    }

    /// <summary>Crea un medico. El numero de colegiado debe ser unico.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearMedicoRequest request, CancellationToken ct)
    {
        if (await _db.Medicos.AnyAsync(m => m.Colegiado == request.Colegiado, ct))
        {
            return ApiProblems.Conflicto("Ya existe un medico con ese numero de colegiado.");
        }

        var medico = new Medico
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Colegiado = request.Colegiado,
            Especialidad = request.Especialidad,
        };

        _db.Medicos.Add(medico);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrorTraductor.Traducir(ex) is { } problema)
        {
            return problema;
        }

        return CreatedAtAction(nameof(Obtener), new { id = medico.IdMedico }, Mapear(medico));
    }

    /// <summary>Actualiza los datos de un medico (el colegiado no se modifica).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarMedicoRequest request, CancellationToken ct)
    {
        var medico = await _db.Medicos.FirstOrDefaultAsync(m => m.IdMedico == id, ct);
        if (medico is null)
        {
            return ApiProblems.NoEncontrado($"No existe un medico con id {id}.");
        }

        medico.Nombre = request.Nombre;
        medico.Apellido = request.Apellido;
        medico.Especialidad = request.Especialidad;

        await _db.SaveChangesAsync(ct);
        return Ok(Mapear(medico));
    }

    private static MedicoResponse Mapear(Medico m) => new()
    {
        IdMedico = m.IdMedico,
        Nombre = m.Nombre,
        Apellido = m.Apellido,
        Colegiado = m.Colegiado,
        Especialidad = m.Especialidad,
    };
}
