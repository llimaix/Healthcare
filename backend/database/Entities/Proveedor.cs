namespace ClinicaCore.Database.Entities;

/// <summary>
/// Proveedor de insumos y medicamentos (dominio inventory).
/// </summary>
public class Proveedor
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Nit { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
    public ICollection<Kardex> MovimientosKardex { get; set; } = new List<Kardex>();
}
