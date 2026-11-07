namespace ClinicaCore.Database.Entities;

/// <summary>
/// Diagnostico registrado en el historial y la cita (dominio clinical).
/// </summary>
public class Diagnostico
{
    public int IdDiagnosticos { get; set; }
    public int IdHistorial { get; set; }
    public int IdCita { get; set; }
    public string? Descripcion { get; set; }
    public string? CodigoCie10 { get; set; }
    public string Alergias { get; set; } = null!;
    public DateTime Fecha { get; set; }

    public HistorialClinico Historial { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();
    public ICollection<ResultadoTratante> ResultadosTratantes { get; set; } = new List<ResultadoTratante>();
}
