using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.Historiales;

/// <summary>Datos para abrir un historial clinico (uno por paciente).</summary>
public class CrearHistorialRequest
{
    [Required]
    public int IdPaciente { get; set; }

    [Required]
    public DateOnly FechaApertura { get; set; }

    public string? Antecedentes { get; set; }
}

/// <summary>Datos para actualizar un historial clinico.</summary>
public class ActualizarHistorialRequest
{
    [Required]
    public DateOnly FechaApertura { get; set; }

    public string? Antecedentes { get; set; }
}

/// <summary>Representacion publica de un historial clinico.</summary>
public class HistorialResponse
{
    public int IdHistorial { get; set; }
    public int IdPaciente { get; set; }
    public DateOnly FechaApertura { get; set; }
    public string? Antecedentes { get; set; }
}
