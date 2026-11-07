using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Clinical.Medicos;

/// <summary>Datos para registrar un medico nuevo.</summary>
public class CrearMedicoRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Apellido { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(30)]
    public string Colegiado { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Especialidad { get; set; } = null!;
}

/// <summary>Datos para actualizar un medico existente.</summary>
public class ActualizarMedicoRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Apellido { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Especialidad { get; set; } = null!;
}

/// <summary>Representacion publica de un medico.</summary>
public class MedicoResponse
{
    public int IdMedico { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Colegiado { get; set; } = null!;
    public string Especialidad { get; set; } = null!;
}
