using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Database.Tests;

/// <summary>
/// Pruebas minimas de fase 1: el modelo compila, el DbContext se construye con el
/// proveedor InMemory y se valida el modelo (conteo de entidades y nombres de tabla
/// clave). No hay reglas de negocio todavia.
/// </summary>
public class ModeloTests
{
    private static ClinicaCoreDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ClinicaCoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicaCoreDbContext(options);
    }

    [Fact]
    public void DbContext_SeConstruyeConInMemory()
    {
        using var ctx = CrearContexto();
        Assert.NotNull(ctx);
        Assert.NotNull(ctx.Model);
    }

    [Fact]
    public void Modelo_Tiene18Entidades()
    {
        using var ctx = CrearContexto();
        var entidades = ctx.Model.GetEntityTypes().Count();
        Assert.Equal(18, entidades);
    }

    [Fact]
    public void ResultadoTratante_UsaNombreDeTablaCorregido()
    {
        using var ctx = CrearContexto();
        var tabla = ctx.Model.FindEntityType(typeof(ResultadoTratante))!.GetTableName();
        Assert.Equal("resultado_tratante", tabla);
    }

    [Theory]
    [InlineData(typeof(Rol), "rol")]
    [InlineData(typeof(Usuario), "usuario")]
    [InlineData(typeof(Paciente), "paciente")]
    [InlineData(typeof(HistorialClinico), "historial_clinico")]
    [InlineData(typeof(RecetaDetalle), "receta_detalle")]
    [InlineData(typeof(Insumo), "insumo")]
    [InlineData(typeof(FacturacionDetalle), "facturacion_detalle")]
    public void Entidades_UsanNombresSnakeCaseEnEspanol(Type tipo, string tablaEsperada)
    {
        using var ctx = CrearContexto();
        var tabla = ctx.Model.FindEntityType(tipo)!.GetTableName();
        Assert.Equal(tablaEsperada, tabla);
    }

    [Fact]
    public void Paciente_TieneUnicidadDeCui()
    {
        using var ctx = CrearContexto();
        var entidad = ctx.Model.FindEntityType(typeof(Paciente))!;
        var cui = entidad.FindProperty(nameof(Paciente.Cui))!;
        var indiceUnico = entidad.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Contains(cui));
        Assert.NotNull(indiceUnico);
    }

    [Fact]
    public void Medico_TieneUnicidadDeColegiado()
    {
        using var ctx = CrearContexto();
        var entidad = ctx.Model.FindEntityType(typeof(Medico))!;
        var colegiado = entidad.FindProperty(nameof(Medico.Colegiado))!;
        var indiceUnico = entidad.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Contains(colegiado));
        Assert.NotNull(indiceUnico);
    }
}
