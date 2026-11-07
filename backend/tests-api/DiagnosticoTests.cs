using System.Net;
using System.Net.Http.Json;
using ClinicaCore.Database.Entities;

namespace ClinicaCore.Api.Tests;

/// <summary>Pruebas de integracion de los endpoints de diagnosticos.</summary>
public class DiagnosticoTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DiagnosticoTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Crear_ConHistorialYCita_DevuelveCreated()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var respuesta = await client.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            descripcion = "Faringitis",
            alergias = "Ninguna",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<DiagnosticoDto>();
        Assert.NotNull(creado);
        Assert.Equal("Ninguna", creado!.Alergias);
    }

    [Fact]
    public async Task Crear_ConHistorialInexistente_DevuelveBadRequest()
    {
        var (_, idCita) = await SembrarHistorialYCitaAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var respuesta = await client.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial = 999999,
            idCita,
            alergias = "Ninguna",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_SinAlergias_DevuelveBadRequestPorValidacion()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var respuesta = await client.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            descripcion = "Sin alergias declaradas",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listar_ConFiltroPorCita_DevuelveSoloLosDeEsaCita()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var client = await _factory.ClienteAutenticadoAsync("Admin");

        await client.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            alergias = "Polen",
        });

        var respuesta = await client.GetAsync($"/api/diagnosticos?idCita={idCita}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var lista = await respuesta.Content.ReadFromJsonAsync<List<DiagnosticoDto>>();
        Assert.NotNull(lista);
        Assert.NotEmpty(lista!);
        Assert.All(lista!, d => Assert.Equal(idCita, d.IdCita));
    }

    [Fact]
    public async Task Obtener_YActualizar_FuncionanParaMedico()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var client = await _factory.ClienteAutenticadoAsync("Medico");

        var creado = await (await client.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            alergias = "Mani",
        })).Content.ReadFromJsonAsync<DiagnosticoDto>();

        var obtener = await client.GetAsync($"/api/diagnosticos/{creado!.IdDiagnosticos}");
        Assert.Equal(HttpStatusCode.OK, obtener.StatusCode);

        var actualizar = await client.PutAsJsonAsync($"/api/diagnosticos/{creado.IdDiagnosticos}", new
        {
            alergias = "Mani, polvo",
            descripcion = "Actualizado",
        });
        Assert.Equal(HttpStatusCode.OK, actualizar.StatusCode);
        var editado = await actualizar.Content.ReadFromJsonAsync<DiagnosticoDto>();
        Assert.Equal("Mani, polvo", editado!.Alergias);
    }

    [Fact]
    public async Task Eliminar_ComoMedico_Devuelve403()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var admin = await _factory.ClienteAutenticadoAsync("Admin");
        var creado = await (await admin.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            alergias = "Ninguna",
        })).Content.ReadFromJsonAsync<DiagnosticoDto>();

        var medico = await _factory.ClienteAutenticadoAsync("Medico");
        var respuesta = await medico.DeleteAsync($"/api/diagnosticos/{creado!.IdDiagnosticos}");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_ComoAdmin_DevuelveNoContent()
    {
        var (idHistorial, idCita) = await SembrarHistorialYCitaAsync();
        var admin = await _factory.ClienteAutenticadoAsync("Admin");
        var creado = await (await admin.PostAsJsonAsync("/api/diagnosticos", new
        {
            idHistorial,
            idCita,
            alergias = "Ninguna",
        })).Content.ReadFromJsonAsync<DiagnosticoDto>();

        var respuesta = await admin.DeleteAsync($"/api/diagnosticos/{creado!.IdDiagnosticos}");
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listar_SinToken_Devuelve401()
    {
        var client = _factory.CreateClient();
        var respuesta = await client.GetAsync("/api/diagnosticos");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private async Task<(int IdHistorial, int IdCita)> SembrarHistorialYCitaAsync()
    {
        int idHistorial = 0;
        int idCita = 0;

        await _factory.ConContextoAsync(async db =>
        {
            var medico = new Medico
            {
                Nombre = "Carlos",
                Apellido = "Ruiz",
                Colegiado = $"COL-{Guid.NewGuid():N}".Substring(0, 20),
                Especialidad = "General",
            };
            var paciente = new Paciente
            {
                Nombre = "Lucia",
                Apellido = "Mota",
                Cui = $"CUI-{Guid.NewGuid():N}".Substring(0, 18),
                FechaNacimiento = new DateOnly(1985, 5, 20),
                Nit = "456",
                Telefono = "777",
                CorreoElectronico = "lucia@clinicacore.test",
                Genero = 'F',
                TipoSangre = "A+",
                Activo = true,
            };
            db.Medicos.Add(medico);
            db.Pacientes.Add(paciente);
            await db.SaveChangesAsync();

            var historial = new HistorialClinico
            {
                IdPaciente = paciente.IdPaciente,
                FechaApertura = new DateOnly(2024, 1, 1),
            };
            var cita = new Cita
            {
                IdMedico = medico.IdMedico,
                IdPaciente = paciente.IdPaciente,
                FechaCita = new DateOnly(2024, 2, 1),
                Hora = new TimeOnly(10, 0),
            };
            db.HistorialesClinicos.Add(historial);
            db.Citas.Add(cita);
            await db.SaveChangesAsync();

            idHistorial = historial.IdHistorial;
            idCita = cita.IdCita;
        });

        return (idHistorial, idCita);
    }

    private sealed class DiagnosticoDto
    {
        public int IdDiagnosticos { get; set; }
        public int IdHistorial { get; set; }
        public int IdCita { get; set; }
        public string? Descripcion { get; set; }
        public string Alergias { get; set; } = null!;
    }
}
