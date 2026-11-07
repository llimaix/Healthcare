using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.ResultadosTratantes;

/// <summary>Datos para registrar un resultado tratante nuevo.</summary>
public class CrearResultadoTratanteRequest
{
    [Required]
    public int IdMedico { get; set; }

    public int? IdEstudio { get; set; }

    public int? IdDiagnostico { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Descripcion { get; set; }

    public string? Conclusion { get; set; }
}

/// <summary>Datos para actualizar un resultado tratante existente.</summary>
public class ActualizarResultadoTratanteRequest
{
    public int? IdEstudio { get; set; }

    public int? IdDiagnostico { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Descripcion { get; set; }

    public string? Conclusion { get; set; }
}

/// <summary>Representacion publica de un resultado tratante.</summary>
public class ResultadoTratanteResponse
{
    public int IdResultado { get; set; }
    public int IdMedico { get; set; }
    public int? IdEstudio { get; set; }
    public int? IdDiagnostico { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string? Conclusion { get; set; }
}
