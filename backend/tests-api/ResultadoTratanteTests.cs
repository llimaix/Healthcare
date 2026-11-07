using System.Net;
using System.Net.Http.Json;
using ClinicaCore.Database.Entities;

namespace ClinicaCore.Api.Tests;

/// <summary>Pruebas de integracion de los endpoints de resultados tratantes.</summary>
public class ResultadoTratanteTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultadoTratanteTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Crear_ConMedico_DevuelveCreated()
    {
        var idMedico = await SembrarMedicoAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var respuesta = await client.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            descripcion = "Radiografia sin hallazgos",
            conclusion = "Normal",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<ResultadoDto>();
        Assert.NotNull(creado);
        Assert.Equal(idMedico, creado!.IdMedico);
    }

    [Fact]
    public async Task Crear_ConMedicoInexistente_DevuelveBadRequest()
    {
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var respuesta = await client.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico = 999999,
            conclusion = "Invalido",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listar_ConFiltroPorMedico_DevuelveSoloLosDeEseMedico()
    {
        var idMedico = await SembrarMedicoAsync();
        var client = await _factory.ClienteAutenticadoAsync("Admin");

        await client.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            conclusion = "Control",
        });

        var respuesta = await client.GetAsync($"/api/resultados-tratantes?idMedico={idMedico}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var lista = await respuesta.Content.ReadFromJsonAsync<List<ResultadoDto>>();
        Assert.NotNull(lista);
        Assert.NotEmpty(lista!);
        Assert.All(lista!, r => Assert.Equal(idMedico, r.IdMedico));
    }

    [Fact]
    public async Task Obtener_DevuelveElResultado()
    {
        var idMedico = await SembrarMedicoAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var creado = await (await client.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            conclusion = "Seguimiento",
        })).Content.ReadFromJsonAsync<ResultadoDto>();

        var respuesta = await client.GetAsync($"/api/resultados-tratantes/{creado!.IdResultado}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_ComoLectura_Devuelve403()
    {
        var idMedico = await SembrarMedicoAsync();
        var admin = await _factory.ClienteAutenticadoAsync("Admin");
        var creado = await (await admin.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            conclusion = "Para borrar",
        })).Content.ReadFromJsonAsync<ResultadoDto>();

        var lectura = await _factory.ClienteAutenticadoAsync("Lectura");
        var respuesta = await lectura.DeleteAsync($"/api/resultados-tratantes/{creado!.IdResultado}");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_ComoAdmin_DevuelveNoContent()
    {
        var idMedico = await SembrarMedicoAsync();
        var admin = await _factory.ClienteAutenticadoAsync("Admin");
        var creado = await (await admin.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            conclusion = "Para borrar",
        })).Content.ReadFromJsonAsync<ResultadoDto>();

        var respuesta = await admin.DeleteAsync($"/api/resultados-tratantes/{creado!.IdResultado}");
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_ComoLectura_Devuelve403()
    {
        var idMedico = await SembrarMedicoAsync();
        var lectura = await _factory.ClienteAutenticadoAsync("Lectura");

        var respuesta = await lectura.PostAsJsonAsync("/api/resultados-tratantes", new
        {
            idMedico,
            conclusion = "Sin permiso",
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    private async Task<int> SembrarMedicoAsync()
    {
        int idMedico = 0;
        await _factory.ConContextoAsync(async db =>
        {
            var medico = new Medico
            {
                Nombre = "Marta",
                Apellido = "Diaz",
                Colegiado = $"COL-{Guid.NewGuid():N}".Substring(0, 20),
                Especialidad = "Radiologia",
            };
            db.Medicos.Add(medico);
            await db.SaveChangesAsync();
            idMedico = medico.IdMedico;
        });
        return idMedico;
    }

    private sealed class ResultadoDto
    {
        public int IdResultado { get; set; }
        public int IdMedico { get; set; }
        public string? Conclusion { get; set; }
    }
}
