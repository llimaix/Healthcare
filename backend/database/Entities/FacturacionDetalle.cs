namespace ClinicaCore.Database.Entities;

/// <summary>
/// Linea de una factura (dominio billing).
/// </summary>
public class FacturacionDetalle
{
    public int IdDetalle { get; set; }
    public int IdFactura { get; set; }
    public string Concepto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Facturacion Factura { get; set; } = null!;
}
