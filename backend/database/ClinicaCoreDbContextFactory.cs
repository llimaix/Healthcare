using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicaCore.Database;

/// <summary>
/// Factory de tiempo de diseno que usa `dotnet ef` para crear el DbContext sin una
/// base MySQL corriendo. La cadena de conexion aqui solo describe el servidor
/// objetivo (MySQL 8); no se abre ninguna conexion al generar o programar migraciones.
/// En tiempo de ejecucion, la cadena real llega por configuracion (fases posteriores).
/// </summary>
public class ClinicaCoreDbContextFactory : IDesignTimeDbContextFactory<ClinicaCoreDbContext>
{
    public ClinicaCoreDbContext CreateDbContext(string[] args)
    {
        // Cadena de marcador de posicion; EF no se conecta para generar migraciones.
        const string connectionString =
            "Server=localhost;Port=3306;Database=clinicacore;User=clinicacore;Password=changeme;";

        // Version del servidor fijada de forma explicita para no requerir una conexion.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        var optionsBuilder = new DbContextOptionsBuilder<ClinicaCoreDbContext>()
            .UseMySql(connectionString, serverVersion);

        return new ClinicaCoreDbContext(optionsBuilder.Options);
    }
}
