namespace ClinicaCore.Database.Entities;

/// <summary>
/// Historial clinico: uno por paciente (dominio clinical).
/// </summary>
public class HistorialClinico
{
    public int IdHistorial { get; set; }
    public int IdPaciente { get; set; }
    public DateOnly FechaApertura { get; set; }
    public string? Antecedentes { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
}
