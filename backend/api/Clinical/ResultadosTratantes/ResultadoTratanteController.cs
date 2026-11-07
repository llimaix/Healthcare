using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Clinical.ResultadosTratantes;

/// <summary>
/// Endpoints de resultados tratantes (pantalla prioritaria del entregable). CRUD/consulta
/// protegido por JWT: lectura para cualquier rol autenticado, escritura para Admin y
/// Medico, y borrado solo para Admin.
/// </summary>
[ApiController]
[Route("api/resultados-tratantes")]
[Authorize]
public class ResultadoTratanteController : ControllerBase
{
    private readonly ClinicaCoreDbContext _db;

    public ResultadoTratanteController(ClinicaCoreDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista resultados con filtros opcionales por diagnostico, medico o estudio.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idDiagnostico,
        [FromQuery] int? idMedico,
        [FromQuery] int? idEstudio,
        CancellationToken ct)
    {
        var consulta = _db.ResultadosTratantes.AsNoTracking().AsQueryable();

        if (idDiagnostico is int diagnostico)
        {
            consulta = consulta.Where(r => r.IdDiagnostico == diagnostico);
        }

        if (idMedico is int medico)
        {
            consulta = consulta.Where(r => r.IdMedico == medico);
        }

        if (idEstudio is int estudio)
        {
            consulta = consulta.Where(r => r.IdEstudio == estudio);
        }

        var resultados = await consulta
            .OrderBy(r => r.IdResultado)
            .Select(r => Mapear(r))
            .ToListAsync(ct);

        return Ok(resultados);
    }

    /// <summary>Obtiene un resultado tratante por su id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var resultado = await _db.ResultadosTratantes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdResultado == id, ct);

        return resultado is null
            ? ApiProblems.NoEncontrado($"No existe un resultado tratante con id {id}.")
            : Ok(Mapear(resultado));
    }

    /// <summary>
    /// Crea un resultado tratante. Valida que exista el medico; el estudio y el
    /// diagnostico son opcionales pero, si se indican, deben existir.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearResultadoTratanteRequest request, CancellationToken ct)
    {
        if (!await _db.Medicos.AnyAsync(m => m.IdMedico == request.IdMedico, ct))
        {
            return ApiProblems.Validacion($"No existe un medico con id {request.IdMedico}.");
        }

        if (request.IdEstudio is int idEstudio
            && !await _db.Estudios.AnyAsync(e => e.IdEstudio == idEstudio, ct))
        {
            return ApiProblems.Validacion($"No existe un estudio con id {idEstudio}.");
        }

        if (request.IdDiagnostico is int idDiagnostico
            && !await _db.Diagnosticos.AnyAsync(d => d.IdDiagnosticos == idDiagnostico, ct))
        {
            return ApiProblems.Validacion($"No existe un diagnostico con id {idDiagnostico}.");
        }

        var resultado = new ResultadoTratante
        {
            IdMedico = request.IdMedico,
            IdEstudio = request.IdEstudio,
            IdDiagnostico = request.IdDiagnostico,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            Descripcion = request.Descripcion,
            Conclusion = request.Conclusion,
        };

        _db.ResultadosTratantes.Add(resultado);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(Obtener), new { id = resultado.IdResultado }, Mapear(resultado));
    }

    /// <summary>Actualiza un resultado tratante existente.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarResultadoTratanteRequest request, CancellationToken ct)
    {
        var resultado = await _db.ResultadosTratantes.FirstOrDefaultAsync(r => r.IdResultado == id, ct);
        if (resultado is null)
        {
            return ApiProblems.NoEncontrado($"No existe un resultado tratante con id {id}.");
        }

        if (request.IdEstudio is int idEstudio
            && !await _db.Estudios.AnyAsync(e => e.IdEstudio == idEstudio, ct))
        {
            return ApiProblems.Validacion($"No existe un estudio con id {idEstudio}.");
        }

        if (request.IdDiagnostico is int idDiagnostico
            && !await _db.Diagnosticos.AnyAsync(d => d.IdDiagnosticos == idDiagnostico, ct))
        {
            return ApiProblems.Validacion($"No existe un diagnostico con id {idDiagnostico}.");
        }

        resultado.IdEstudio = request.IdEstudio;
        resultado.IdDiagnostico = request.IdDiagnostico;
        resultado.Descripcion = request.Descripcion;
        resultado.Conclusion = request.Conclusion;
        if (request.Fecha is DateTime fecha)
        {
            resultado.Fecha = fecha;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(Mapear(resultado));
    }

    /// <summary>Elimina un resultado tratante. Operacion reservada al rol Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var resultado = await _db.ResultadosTratantes.FirstOrDefaultAsync(r => r.IdResultado == id, ct);
        if (resultado is null)
        {
            return ApiProblems.NoEncontrado($"No existe un resultado tratante con id {id}.");
        }

        _db.ResultadosTratantes.Remove(resultado);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ResultadoTratanteResponse Mapear(ResultadoTratante r) => new()
    {
        IdResultado = r.IdResultado,
        IdMedico = r.IdMedico,
        IdEstudio = r.IdEstudio,
        IdDiagnostico = r.IdDiagnostico,
        Fecha = r.Fecha,
        Descripcion = r.Descripcion,
        Conclusion = r.Conclusion,
    };
}
