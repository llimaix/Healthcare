using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.Pacientes;

/// <summary>Datos para registrar un paciente nuevo.</summary>
public class CrearPacienteRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Apellido { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(20)]
    public string Cui { get; set; } = null!;

    [Required]
    public DateOnly FechaNacimiento { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(20)]
    public string Nit { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(20)]
    public string Telefono { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [MaxLength(150)]
    public string CorreoElectronico { get; set; } = null!;

    /// <summary>Genero del paciente; solo se aceptan 'F' o 'M'.</summary>
    [Required]
    public char Genero { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(5)]
    public string TipoSangre { get; set; } = null!;
}

/// <summary>Datos para actualizar un paciente existente.</summary>
public class ActualizarPacienteRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Apellido { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(20)]
    public string Nit { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(20)]
    public string Telefono { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [MaxLength(150)]
    public string CorreoElectronico { get; set; } = null!;

    [Required]
    public char Genero { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(5)]
    public string TipoSangre { get; set; } = null!;
}

/// <summary>Representacion publica de un paciente.</summary>
public class PacienteResponse
{
    public int IdPaciente { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Cui { get; set; } = null!;
    public DateOnly FechaNacimiento { get; set; }
    public string Nit { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string CorreoElectronico { get; set; } = null!;
    public char Genero { get; set; }
    public string TipoSangre { get; set; } = null!;
}
