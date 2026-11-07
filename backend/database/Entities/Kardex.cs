namespace ClinicaCore.Database.Entities;

/// <summary>
/// Movimiento de inventario (entrada o salida) de un insumo (dominio inventory).
/// </summary>
public class Kardex
{
    public int IdKardex { get; set; }
    public int IdInsumo { get; set; }
    public int? IdProveedor { get; set; }
    public string? TipoMovimiento { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }

    public Insumo Insumo { get; set; } = null!;
    public Proveedor? Proveedor { get; set; }
}
