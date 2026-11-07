namespace ClinicaCore.Database.Entities;

/// <summary>
/// Profesional medico (dominio clinical).
/// </summary>
public class Medico
{
    public int IdMedico { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Colegiado { get; set; } = null!;
    public string Especialidad { get; set; } = null!;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<ResultadoTratante> ResultadosTratantes { get; set; } = new List<ResultadoTratante>();
    public Usuario? Usuario { get; set; }
}
