namespace ClinicaCore.Database.Entities;

/// <summary>
/// Insumo o medicamento (dominio inventory).
/// </summary>
public class Insumo
{
    public int IdInsumo { get; set; }
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Tipo { get; set; }
    public decimal Precio { get; set; }
    public int StockMinimo { get; set; }

    public Proveedor Proveedor { get; set; } = null!;
    public ICollection<RecetaDetalle> RecetaDetalles { get; set; } = new List<RecetaDetalle>();
    public ICollection<Kardex> MovimientosKardex { get; set; } = new List<Kardex>();
}
