using Microsoft.AspNetCore.Mvc;

namespace ClinicaCore.Api.Common;

/// <summary>
/// Fabrica de respuestas de error homogeneas en formato ProblemDetails (RFC 7807).
/// Centraliza los codigos que el dominio devuelve: 400 validacion, 404 no encontrado,
/// 409 conflicto. Los controllers la usan para no repetir la forma de cada error.
/// </summary>
public static class ApiProblems
{
    /// <summary>400: datos de entrada invalidos (regla de negocio o referencia).</summary>
    public static ObjectResult Validacion(string detalle) =>
        Crear(StatusCodes.Status400BadRequest, "Solicitud invalida", detalle);

    /// <summary>404: el recurso solicitado no existe.</summary>
    public static ObjectResult NoEncontrado(string detalle) =>
        Crear(StatusCodes.Status404NotFound, "Recurso no encontrado", detalle);

    /// <summary>409: conflicto, por ejemplo una unicidad o un CHECK violado.</summary>
    public static ObjectResult Conflicto(string detalle) =>
        Crear(StatusCodes.Status409Conflict, "Conflicto", detalle);

    private static ObjectResult Crear(int status, string titulo, string detalle)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle,
        };
        return new ObjectResult(problema) { StatusCode = status };
    }
}
