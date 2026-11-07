using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Common;

/// <summary>
/// Traduce violaciones de restricciones de la base (indice unico, CHECK) que afloran
/// como DbUpdateException al guardar en MySQL hacia respuestas ProblemDetails claras.
/// El proveedor InMemory de las pruebas no las dispara; por eso el dominio tambien
/// valida unicidad y genero por adelantado. Este traductor cubre el camino de runtime.
/// </summary>
public static class DbErrorTraductor
{
    /// <summary>
    /// Devuelve una respuesta de conflicto/validacion si la excepcion corresponde a una
    /// restriccion conocida; en otro caso devuelve null para que la excepcion se propague.
    /// </summary>
    public static ObjectResult? Traducir(DbUpdateException ex)
    {
        var mensaje = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();

        if (Contiene(mensaje, "cui"))
        {
            return ApiProblems.Conflicto("Ya existe un paciente con ese CUI.");
        }

        if (Contiene(mensaje, "colegiado"))
        {
            return ApiProblems.Conflicto("Ya existe un medico con ese numero de colegiado.");
        }

        if (Contiene(mensaje, "correo"))
        {
            return ApiProblems.Conflicto("El correo ya esta registrado.");
        }

        if (Contiene(mensaje, "ck_paciente_genero"))
        {
            return ApiProblems.Validacion("El genero solo acepta los valores 'F' o 'M'.");
        }

        return null;
    }

    private static bool Contiene(string texto, string fragmento) =>
        texto.Contains(fragmento, StringComparison.Ordinal);
}
