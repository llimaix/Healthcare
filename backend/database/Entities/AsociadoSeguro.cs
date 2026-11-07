namespace ClinicaCore.Database.Entities;

/// <summary>
/// Vincula un paciente con un plan de cobertura y una poliza (dominio billing).
/// </summary>
public class AsociadoSeguro
{
    public int IdAsegurado { get; set; }
    public int IdPaciente { get; set; }
    public int IdTipoCobertura { get; set; }
    public string? Aseguradora { get; set; }
    public string? NumeroPoliza { get; set; }
    public DateOnly VigenciaDesde { get; set; }
    public DateOnly VigenciaHasta { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public TipoCobertura TipoCobertura { get; set; } = null!;
    public ICollection<Facturacion> Facturaciones { get; set; } = new List<Facturacion>();
}
