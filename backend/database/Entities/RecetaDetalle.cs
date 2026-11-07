namespace ClinicaCore.Database.Entities;

/// <summary>
/// Linea de receta: un insumo o medicamento recetado (dominio clinical).
/// </summary>
public class RecetaDetalle
{
    public int IdRecetaDetalle { get; set; }
    public int IdReceta { get; set; }
    public int IdInsumo { get; set; }
    public string? Dosis { get; set; }
    public string? Frecuencia { get; set; }
    public int DuracionDias { get; set; }

    public Receta Receta { get; set; } = null!;
    public Insumo Insumo { get; set; } = null!;
}
