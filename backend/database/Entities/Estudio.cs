namespace ClinicaCore.Database.Entities;

/// <summary>
/// Estudio solicitado en una cita (dominio clinical).
/// </summary>
public class Estudio
{
    public int IdEstudio { get; set; }
    public int IdCita { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaSolicitud { get; set; }
    public string? Estado { get; set; }

    public Cita Cita { get; set; } = null!;
    public ICollection<ResultadoTratante> ResultadosTratantes { get; set; } = new List<ResultadoTratante>();
}
