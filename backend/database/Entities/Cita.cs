namespace ClinicaCore.Database.Entities;

/// <summary>
/// Cita o consulta entre paciente y medico (dominio clinical).
/// </summary>
public class Cita
{
    public int IdCita { get; set; }
    public int IdMedico { get; set; }
    public int IdPaciente { get; set; }
    public DateOnly FechaCita { get; set; }
    public TimeOnly Hora { get; set; }
    public string? Motivo { get; set; }
    public string? Estado { get; set; }

    public Medico Medico { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();
    public ICollection<Estudio> Estudios { get; set; } = new List<Estudio>();
    public Facturacion? Facturacion { get; set; }
}
