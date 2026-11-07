using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Database;

/// <summary>
/// DbContext unico de ClinicaCore. Mapea las 18 entidades del modelo ER aprobado
/// a una sola base MySQL con llaves foraneas reales. Cada microservicio es dueno
/// logico de un subconjunto de tablas (clinical, billing, inventory, auth); esa
/// particion es organizativa, no fisica. Ver docs/er/modelo-er.md.
/// </summary>
public class ClinicaCoreDbContext : DbContext
{
    public ClinicaCoreDbContext(DbContextOptions<ClinicaCoreDbContext> options)
        : base(options)
    {
    }

    // Auth
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // Clinical
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaDetalle> RecetaDetalles => Set<RecetaDetalle>();
    public DbSet<Estudio> Estudios => Set<Estudio>();
    public DbSet<ResultadoTratante> ResultadosTratantes => Set<ResultadoTratante>();

    // Inventory
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Kardex> Kardex => Set<Kardex>();

    // Billing
    public DbSet<TipoCobertura> TiposCobertura => Set<TipoCobertura>();
    public DbSet<AsociadoSeguro> AsociadosSeguro => Set<AsociadoSeguro>();
    public DbSet<Facturacion> Facturaciones => Set<Facturacion>();
    public DbSet<FacturacionDetalle> FacturacionDetalles => Set<FacturacionDetalle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases IEntityTypeConfiguration del ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaCoreDbContext).Assembly);
    }
}
