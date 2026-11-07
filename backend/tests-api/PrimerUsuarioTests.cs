using System.Net;
using System.Net.Http.Json;

namespace ClinicaCore.Api.Tests;

/// <summary>
/// Arranque del sistema vacio: el primer registro es anonimo y debe ser Admin.
/// Cada prueba usa su propia ApiFactory (BD InMemory nueva y sin usuarios).
/// </summary>
public class PrimerUsuarioTests
{
    [Fact]
    public async Task SistemaVacio_PrimerRegistroAnonimoComoAdmin_DevuelveCreated()
    {
        await using var factory = new ApiFactory();
        var anonimo = factory.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync("/api/auth/register", new
        {
            correo = "primer-admin@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Admin",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task SistemaVacio_PrimerRegistroConRolNoAdmin_DevuelveBadRequest()
    {
        await using var factory = new ApiFactory();
        var anonimo = factory.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync("/api/auth/register", new
        {
            correo = "primer-medico@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Medico",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task TrasElPrimerAdmin_RegistroAnonimo_Devuelve401()
    {
        await using var factory = new ApiFactory();
        var anonimo = factory.CreateClient();

        await anonimo.PostAsJsonAsync("/api/auth/register", new
        {
            correo = "primer-admin@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Admin",
        });

        var segundo = await anonimo.PostAsJsonAsync("/api/auth/register", new
        {
            correo = "segundo@clinicacore.test",
            password = "ClaveSegura123",
            rol = "Admin",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, segundo.StatusCode);
    }
}
