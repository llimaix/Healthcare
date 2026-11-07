namespace ClinicaCore.Database.Entities;

/// <summary>
/// Resultado que registra un medico tratante (dominio clinical).
/// Corrige la errata 'resultados_trantes' de la captura: la tabla es 'resultado_tratante'.
/// </summary>
public class ResultadoTratante
{
    public int IdResultado { get; set; }
    public int? IdEstudio { get; set; }
    public int IdMedico { get; set; }
    public int? IdDiagnostico { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string? Conclusion { get; set; }

    public Estudio? Estudio { get; set; }
    public Medico Medico { get; set; } = null!;
    public Diagnostico? Diagnostico { get; set; }
}
