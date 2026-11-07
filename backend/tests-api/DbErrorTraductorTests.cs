using System.Net;
using ClinicaCore.Api.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Tests;

/// <summary>
/// Pruebas unitarias directas de <see cref="DbErrorTraductor"/>. Ejercitan el camino de
/// traduccion de violaciones de restriccion que en runtime afloran como
/// <see cref="DbUpdateException"/> al guardar en MySQL, sin necesitar una base real.
///
/// Nota de alcance: MySqlConnector no expone un constructor publico de MySqlException, por
/// lo que no es viable construir aqui una instancia fiel con sus numeros de error de
/// duplicado (1062) o CHECK (3819). El traductor, sin embargo, solo inspecciona el texto
/// del mensaje de la excepcion interna; estas pruebas envuelven en DbUpdateException un
/// mensaje representativo del que produce MySQL para cada restriccion. La cobertura de que
/// una MySqlException real dispara el 409/400 queda para una prueba de integracion contra
/// MySQL (ver <see cref="ConflictoMySqlIntegrationTests"/>), omitida hasta que exista el
/// entorno de infraestructura.
/// </summary>
public class DbErrorTraductorTests
{
    private static DbUpdateException Envolver(string mensajeInterno) =>
        new("An error occurred while saving the entity changes.",
            new Exception(mensajeInterno));

    [Fact]
    public void Traducir_DuplicadoDeCui_DevuelveConflicto()
    {
        // Mensaje representativo de MySQL (error 1062) para el indice unico de CUI.
        var ex = Envolver(
            "Duplicate entry 'CUI-123' for key 'ux_paciente_cui'");

        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.NotNull(resultado);
        Assert.Equal((int)HttpStatusCode.Conflict, resultado!.StatusCode);
    }

    [Fact]
    public void Traducir_DuplicadoDeColegiado_DevuelveConflicto()
    {
        var ex = Envolver(
            "Duplicate entry 'COL-1' for key 'ux_medico_colegiado'");

        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.NotNull(resultado);
        Assert.Equal((int)HttpStatusCode.Conflict, resultado!.StatusCode);
    }

    [Fact]
    public void Traducir_CheckDeGenero_DevuelveValidacion()
    {
        // Mensaje representativo de MySQL (error 3819) al violar el CHECK de genero.
        var ex = Envolver(
            "Check constraint 'ck_paciente_genero' is violated.");

        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.NotNull(resultado);
        Assert.Equal((int)HttpStatusCode.BadRequest, resultado!.StatusCode);
    }

    [Fact]
    public void Traducir_ErrorDesconocido_DevuelveNull()
    {
        var ex = Envolver("Lost connection to MySQL server during query");

        var resultado = DbErrorTraductor.Traducir(ex);

        Assert.Null(resultado);
    }
}
