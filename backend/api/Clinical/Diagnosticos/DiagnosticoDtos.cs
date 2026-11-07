using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.Diagnosticos;

/// <summary>Datos para registrar un diagnostico nuevo.</summary>
public class CrearDiagnosticoRequest
{
    [Required]
    public int IdHistorial { get; set; }

    [Required]
    public int IdCita { get; set; }

    public string? Descripcion { get; set; }

    [MaxLength(10)]
    public string? CodigoCie10 { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Alergias { get; set; } = null!;

    public DateTime? Fecha { get; set; }
}

/// <summary>Datos para actualizar un diagnostico existente.</summary>
public class ActualizarDiagnosticoRequest
{
    public string? Descripcion { get; set; }

    [MaxLength(10)]
    public string? CodigoCie10 { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Alergias { get; set; } = null!;

    public DateTime? Fecha { get; set; }
}

/// <summary>Representacion publica de un diagnostico.</summary>
public class DiagnosticoResponse
{
    public int IdDiagnosticos { get; set; }
    public int IdHistorial { get; set; }
    public int IdCita { get; set; }
    public string? Descripcion { get; set; }
    public string? CodigoCie10 { get; set; }
    public string Alergias { get; set; } = null!;
    public DateTime Fecha { get; set; }
}
