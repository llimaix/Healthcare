namespace ClinicaCore.Database.Entities;

/// <summary>
/// Paciente (dominio clinical). El genero acepta solo 'F' o 'M', como en la captura.
/// </summary>
public class Paciente
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

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public HistorialClinico? HistorialClinico { get; set; }
    public ICollection<AsociadoSeguro> AsociadosSeguro { get; set; } = new List<AsociadoSeguro>();
    public ICollection<Facturacion> Facturaciones { get; set; } = new List<Facturacion>();
}
