namespace ClinicaCore.Database.Entities;

/// <summary>
/// Cabecera de facturacion ligada a una cita (dominio billing).
/// </summary>
public class Facturacion
{
    public int IdFactura { get; set; }
    public int IdPaciente { get; set; }
    public int IdCita { get; set; }
    public int? IdAsegurado { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MontoCobertura { get; set; }
    public decimal Total { get; set; }
    public string? Estado { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public AsociadoSeguro? Asegurado { get; set; }
    public ICollection<FacturacionDetalle> Detalles { get; set; } = new List<FacturacionDetalle>();
}
