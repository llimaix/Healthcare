using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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

    /// <summary>Correo del administrador semilla que crean las pruebas.</summary>
    public const string CorreoAdminSemilla = "admin-semilla@clinicacore.test";

    /// <summary>Contrasena comun de los usuarios creados en las pruebas.</summary>
    public const string ClavePruebas = "ClaveSegura123";

    /// <summary>
    /// Garantiza que exista el Admin semilla. Lo inserta directo en la BD (con hash)
    /// porque, una vez hay usuarios, el registro exige un Admin autenticado.
    /// </summary>
    public async Task AsegurarAdminSemillaAsync()
    {
        await ConContextoAsync(async db =>
        {
            if (await db.Usuarios.AnyAsync(u => u.Correo == CorreoAdminSemilla))
            {
                return;
            }

            var rolAdmin = await db.Roles.SingleAsync(r => r.Nombre == "Admin");
            var admin = new Usuario
            {
                Correo = CorreoAdminSemilla,
                IdRol = rolAdmin.IdRol,
                Activo = true,
            };
            admin.PasswordHash = new PasswordHasher<Usuario>().HashPassword(admin, ClavePruebas);
            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();
        });
    }

    /// <summary>HttpClient autenticado como el Admin semilla.</summary>
    public async Task<HttpClient> ClienteAdminSemillaAsync()
    {
        await AsegurarAdminSemillaAsync();
        return await ClienteConLoginAsync(CorreoAdminSemilla, ClavePruebas);
    }

    /// <summary>Inicia sesion y devuelve un HttpClient con el token como Bearer.</summary>
    public async Task<HttpClient> ClienteConLoginAsync(string correo, string password)
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { correo, password });
        login.EnsureSuccessStatusCode();
        var cuerpo = await login.Content.ReadFromJsonAsync<TokenDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
        return client;
    }

    /// <summary>
    /// Crea un usuario nuevo del rol indicado (registrado por el Admin semilla) y
    /// devuelve un HttpClient autenticado con el.
    /// </summary>
    public async Task<HttpClient> ClienteAutenticadoAsync(string rol)
    {
        var correo = await CrearUsuarioAsync(rol);
        return await ClienteConLoginAsync(correo, ClavePruebas);
    }

    /// <summary>Registra (como Admin semilla) un usuario del rol dado y devuelve su correo.</summary>
    public async Task<string> CrearUsuarioAsync(string rol)
    {
        var admin = await ClienteAdminSemillaAsync();
        var correo = $"{rol.ToLowerInvariant()}-{Guid.NewGuid():N}@clinicacore.test";

        var registro = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = ClavePruebas,
            rol,
        });
        registro.EnsureSuccessStatusCode();
        return correo;
    }

    private sealed class TokenDto
    {
        public string Token { get; set; } = null!;
    }
}
