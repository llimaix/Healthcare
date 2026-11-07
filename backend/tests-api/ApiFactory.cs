using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaCore.Api.Tests;

/// <summary>
/// Factory de pruebas que levanta la Api con una base InMemory compartida por instancia
/// y con la configuracion Jwt necesaria para emitir y validar tokens.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "clinicacore-test",
                ["Jwt:Audience"] = "clinicacore-test",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:Secret"] = "secreto-de-pruebas-con-longitud-suficiente-32",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Reemplaza el DbContext MySQL por InMemory: retira todas las registraciones
            // de EF Core (opciones, contexto y proveedor) para no mezclar proveedores.
            var aRetirar = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ClinicaCoreDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(ClinicaCoreDbContext)
                    || (d.ServiceType.FullName?.StartsWith(
                        "Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var descriptor in aRetirar)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ClinicaCoreDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Ejecuta una accion sobre un DbContext con el ambito resuelto.</summary>
    public async Task ConContextoAsync(Func<ClinicaCoreDbContext, Task> accion)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicaCoreDbContext>();
        await accion(db);
    }

    /// <summary>Garantiza que exista un rol para las pruebas de registro.</summary>
    public async Task AsegurarRolAsync(string nombre)
    {
        await ConContextoAsync(async db =>
        {
            if (!await db.Roles.AnyAsync(r => r.Nombre == nombre))
            {
                db.Roles.Add(new Rol { Nombre = nombre });
                await db.SaveChangesAsync();
            }
        });
    }
}
