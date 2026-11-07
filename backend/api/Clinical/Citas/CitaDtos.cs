using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.Citas;

/// <summary>Datos para registrar una cita nueva.</summary>
public class CrearCitaRequest
{
    [Required]
    public int IdMedico { get; set; }

    [Required]
    public int IdPaciente { get; set; }

    [Required]
    public DateOnly FechaCita { get; set; }

    [Required]
    public TimeOnly Hora { get; set; }

    [MaxLength(255)]
    public string? Motivo { get; set; }

    [MaxLength(30)]
    public string? Estado { get; set; }
}

/// <summary>Datos para actualizar una cita existente.</summary>
public class ActualizarCitaRequest
{
    [Required]
    public DateOnly FechaCita { get; set; }

    [Required]
    public TimeOnly Hora { get; set; }

    [MaxLength(255)]
    public string? Motivo { get; set; }

    [MaxLength(30)]
    public string? Estado { get; set; }
}

/// <summary>Representacion publica de una cita.</summary>
public class CitaResponse
{
    public int IdCita { get; set; }
    public int IdMedico { get; set; }
    public int IdPaciente { get; set; }
    public DateOnly FechaCita { get; set; }
    public TimeOnly Hora { get; set; }
    public string? Motivo { get; set; }
    public string? Estado { get; set; }
}
