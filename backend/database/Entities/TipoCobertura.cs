namespace ClinicaCore.Database.Entities;

/// <summary>
/// Catalogo de planes de cobertura de seguro (dominio billing).
/// </summary>
public class TipoCobertura
{
    public int IdTipoCobertura { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PorcentajeCobertura { get; set; }

    public ICollection<AsociadoSeguro> AsociadosSeguro { get; set; } = new List<AsociadoSeguro>();
}
