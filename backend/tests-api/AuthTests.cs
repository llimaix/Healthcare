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
    public async Task Registro_PorAdmin_CreaUsuarioConHash()
    {
        var admin = await _factory.ClienteAdminSemillaAsync();
        var correo = $"nuevo-{Guid.NewGuid():N}@clinicacore.test";

        var respuesta = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            correo,
            password = "ClaveSegura123",
            rol = "Medico",
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
    public async Task Registro_SinToken_ConUsuariosExistentes_Devuelve401()
    {
        await _factory.AsegurarAdminSemillaAsync();
        var anonimo = _factory.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync("/api/auth/register", new
        {
            correo = $"intruso-{Guid.NewGuid():N}@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Admin",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("Medico")]
    [InlineData("Recepcion")]
    [InlineData("Lectura")]
    public async Task Registro_PorRolNoAdmin_Devuelve403(string rol)
    {
        var client = await _factory.ClienteAutenticadoAsync(rol);

        var respuesta = await client.PostAsJsonAsync("/api/auth/register", new
        {
            correo = $"otro-{Guid.NewGuid():N}@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Lectura",
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_CredencialesValidas_DevuelveTokenConClaimRole()
    {
        var correo = await _factory.CrearUsuarioAsync("Medico");
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new
        {
            correo,
            password = ApiFactory.ClavePruebas,
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
        var correo = await _factory.CrearUsuarioAsync("Lectura");
        var client = _factory.CreateClient();

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
        var correo = await _factory.CrearUsuarioAsync("Recepcion");
        var client = await _factory.ClienteConLoginAsync(correo, ApiFactory.ClavePruebas);

        var respuesta = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioResponseDto>();
        Assert.NotNull(usuario);
        Assert.Equal(correo, usuario!.Correo);
        Assert.Equal("Recepcion", usuario.Rol);
    }

    [Fact]
    public async Task Usuarios_Listar_ComoAdmin_DevuelveLista()
    {
        var admin = await _factory.ClienteAdminSemillaAsync();
        var respuesta = await admin.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<UsuarioResponseDto>>();
        Assert.Contains(lista!, u => u.Correo == ApiFactory.CorreoAdminSemilla);
    }

    [Theory]
    [InlineData("Medico")]
    [InlineData("Recepcion")]
    [InlineData("Lectura")]
    public async Task Usuarios_Listar_ComoNoAdmin_Devuelve403(string rol)
    {
        var client = await _factory.ClienteAutenticadoAsync(rol);
        var respuesta = await client.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Usuarios_Desactivar_ImpideElLogin()
    {
        var correo = await _factory.CrearUsuarioAsync("Lectura");
        var admin = await _factory.ClienteAdminSemillaAsync();
        var lista = await admin.GetFromJsonAsync<List<UsuarioResponseDto>>("/api/usuarios");
        var usuario = lista!.Single(u => u.Correo == correo);

        var actualizar = await admin.PutAsJsonAsync($"/api/usuarios/{usuario.IdUsuario}", new
        {
            rol = "Lectura",
            activo = false,
        });
        Assert.Equal(HttpStatusCode.OK, actualizar.StatusCode);

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            correo,
            password = ApiFactory.ClavePruebas,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Usuarios_AdminNoPuedeQuitarseSuRol()
    {
        var admin = await _factory.ClienteAdminSemillaAsync();
        var yo = await admin.GetFromJsonAsync<UsuarioResponseDto>("/api/auth/me");

        var respuesta = await admin.PutAsJsonAsync($"/api/usuarios/{yo!.IdUsuario}", new
        {
            rol = "Lectura",
            activo = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
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
        public bool Activo { get; set; }
    }
}
