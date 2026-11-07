using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Tests;

/// <summary>Pruebas de integracion del modulo de autenticacion JWT.</summary>
public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Registro_CreaUsuarioConHash()
    {
        var client = _factory.CreateClient();
        var correo = $"nuevo-{Guid.NewGuid():N}@clinicacore.test";

        var respuesta = await client.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = "ClaveSegura123",
            rol = "Admin",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        await _factory.ConContextoAsync(async db =>
        {
            var usuario = await db.Usuarios.SingleAsync(u => u.Correo == correo);
            Assert.NotEqual("ClaveSegura123", usuario.PasswordHash);
            Assert.NotEmpty(usuario.PasswordHash);
        });
    }

    [Fact]
    public async Task Login_CredencialesValidas_DevuelveTokenConClaimRole()
    {
        var client = _factory.CreateClient();
        var correo = $"login-{Guid.NewGuid():N}@clinicacore.test";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = "ClaveSegura123",
            rol = "Medico",
        });

        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new
        {
            correo,
            password = "ClaveSegura123",
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(cuerpo);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo!.Token));

        // El token debe incluir el claim de rol.
        var payload = DecodificarPayload(cuerpo.Token);
        Assert.Contains("Medico", payload);
    }

    [Fact]
    public async Task Login_CredencialesInvalidas_Falla()
    {
        var client = _factory.CreateClient();
        var correo = $"malo-{Guid.NewGuid():N}@clinicacore.test";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = "ClaveSegura123",
            rol = "Lectura",
        });

        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new
        {
            correo,
            password = "ClaveIncorrecta",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Me_SinToken_Devuelve401()
    {
        var client = _factory.CreateClient();
        var respuesta = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Me_ConTokenValido_DevuelveElUsuario()
    {
        var client = _factory.CreateClient();
        var correo = $"me-{Guid.NewGuid():N}@clinicacore.test";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = "ClaveSegura123",
            rol = "Recepcion",
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            correo,
            password = "ClaveSegura123",
        });
        var cuerpo = await login.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", cuerpo!.Token);

        var respuesta = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioResponseDto>();
        Assert.NotNull(usuario);
        Assert.Equal(correo, usuario!.Correo);
        Assert.Equal("Recepcion", usuario.Rol);
    }

    [Fact]
    public async Task Paciente_Inactivo_QuedaFueraDeLasConsultas()
    {
        await _factory.ConContextoAsync(async db =>
        {
            db.Pacientes.Add(new Paciente
            {
                Nombre = "Ana",
                Apellido = "Lopez",
                Cui = $"CUI-{Guid.NewGuid():N}".Substring(0, 18),
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Nit = "123",
                Telefono = "555",
                CorreoElectronico = "ana@clinicacore.test",
                Genero = 'F',
                TipoSangre = "O+",
                Activo = false,
            });
            await db.SaveChangesAsync();

            // El filtro global excluye a los pacientes inactivos.
            var visibles = await db.Pacientes.CountAsync();
            Assert.Equal(0, visibles);

            var todos = await db.Pacientes.IgnoreQueryFilters().CountAsync();
            Assert.Equal(1, todos);
        });
    }

    private static string DecodificarPayload(string token)
    {
        var partes = token.Split('.');
        var payload = partes[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }

    private sealed class LoginResponseDto
    {
        public string Token { get; set; } = null!;
    }

    private sealed class UsuarioResponseDto
    {
        public int IdUsuario { get; set; }
        public string Correo { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public int? IdMedico { get; set; }
    }
}
