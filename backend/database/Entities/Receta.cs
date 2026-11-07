namespace ClinicaCore.Database.Entities;

/// <summary>
/// Cabecera de receta (dominio clinical). El detalle va en RecetaDetalle.
/// </summary>
public class Receta
{
    public int IdReceta { get; set; }
    public int IdCita { get; set; }
    public int? IdDiagnostico { get; set; }
    public DateOnly FechaEmision { get; set; }
    public string? Indicaciones { get; set; }

    public Cita Cita { get; set; } = null!;
    public Diagnostico? Diagnostico { get; set; }
    public ICollection<RecetaDetalle> Detalles { get; set; } = new List<RecetaDetalle>();
}
