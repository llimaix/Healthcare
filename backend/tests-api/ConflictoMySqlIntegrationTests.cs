using ClinicaCore.Api.Common;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Tests;

/// <summary>
/// Prueba de integracion del camino 409/400 que <see cref="DbErrorTraductor"/> cubre solo
/// en runtime MySQL: el proveedor InMemory no dispara el indice unico de CUI/colegiado ni
/// el CHECK ck_paciente_genero, asi que la traduccion de la DbUpdateException real no puede
/// ejercitarse sin una base MySQL. Esta prueba inserta directamente por el DbContext para
/// saltar la validacion adelantada de los controllers y forzar la violacion a nivel de base.
///
/// Esta OMITIDA (Skip) porque en este entorno el daemon de Docker esta apagado y no hay
/// MySQL disponible. Pertenece a la fase de infraestructura.
///
/// Como ejecutarla cuando exista MySQL:
///   1. Levanta un MySQL 8 con la base clinicacore y aplica las migraciones:
///        dotnet ef database update --project backend/database --startup-project backend/api
///   2. Exporta la cadena de conexion de pruebas antes de correr:
///        export CLINICACORE_TEST_MYSQL="Server=localhost;Port=3306;Database=clinicacore;User=root;Password=TU_CLAVE"
///   3. Quita el atributo Skip de los [Fact] de esta clase (o cambialo por un gate que lea
///      la variable) y ejecuta: cd backend && dotnet test
/// </summary>
public class ConflictoMySqlIntegrationTests
{
    private const string SkipMotivo =
        "Requiere MySQL real (fase de infraestructura). Docker apagado en este entorno. " +
        "Ver instrucciones en el encabezado de la clase para habilitarla.";

    private static ClinicaCoreDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable("CLINICACORE_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "Define CLINICACORE_TEST_MYSQL con la cadena de conexion de pruebas.");

        var opciones = new DbContextOptionsBuilder<ClinicaCoreDbContext>()
            .UseMySql(cadena, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new ClinicaCoreDbContext(opciones);
    }

    private static Paciente NuevoPaciente(string cui, char genero) => new()
    {
        Nombre = "Integracion",
        Apellido = "MySQL",
        Cui = cui,
        FechaNacimiento = new DateOnly(1990, 1, 1),
        Nit = "123",
        Telefono = "555",
        CorreoElectronico = $"int-{Guid.NewGuid():N}@clinicacore.test",
        Genero = genero,
        TipoSangre = "O+",
        Activo = true,
    };

    [Fact(Skip = SkipMotivo)]
    public async Task InsertarCuiDuplicado_TraduceA409()
    {
        await using var db = CrearContexto();
        var cui = $"CUI-{Guid.NewGuid():N}".Substring(0, 18);

        db.Pacientes.Add(NuevoPaciente(cui, 'F'));
        await db.SaveChangesAsync();

        db.Pacientes.Add(NuevoPaciente(cui, 'M'));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.NotNull(resultado);
        Assert.Equal(409, resultado!.StatusCode);
    }

    [Fact(Skip = SkipMotivo)]
    public async Task InsertarGeneroInvalido_TraduceA400()
    {
        await using var db = CrearContexto();
        var cui = $"CUI-{Guid.NewGuid():N}".Substring(0, 18);

        // 'X' viola el CHECK ck_paciente_genero (solo 'F' o 'M') en MySQL.
        db.Pacientes.Add(NuevoPaciente(cui, 'X'));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.NotNull(resultado);
        Assert.Equal(400, resultado!.StatusCode);
    }
}
