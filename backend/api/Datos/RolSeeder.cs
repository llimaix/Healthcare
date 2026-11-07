using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicaCore.Api.Datos;

/// <summary>
/// Siembra el catalogo de roles base cuando la tabla rol esta vacia.
/// </summary>
public static class RolSeeder
{
    private static readonly string[] RolesBase = ["Admin", "Medico", "Recepcion", "Lectura"];

    public static async Task SembrarAsync(ClinicaCoreDbContext db, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(ct))
        {
            return;
        }

        db.Roles.AddRange(RolesBase.Select(nombre => new Rol { Nombre = nombre }));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Siembra reintentando ante la BD aun no lista (primario arrancando o esquema sin
    /// migrar en el despliegue en contenedores). Si tras los reintentos no lo logra, NO
    /// lanza: registra el aviso y deja arrancar la Api para que /health responda; los
    /// roles se sembraran en un arranque posterior, ya con la BD migrada.
    /// </summary>
    public static async Task SembrarConReintentosAsync(
        ClinicaCoreDbContext db,
        ILogger logger,
        int intentos = 10,
        CancellationToken ct = default)
    {
        for (var intento = 1; intento <= intentos; intento++)
        {
            try
            {
                await SembrarAsync(db, ct);
                return;
            }
            catch (Exception ex) when (intento < intentos)
            {
                logger.LogWarning(
                    ex,
                    "La siembra de roles fallo (intento {Intento}/{Intentos}); la BD podria no estar lista todavia. Reintentando...",
                    intento,
                    intentos);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "La siembra de roles no se completo tras {Intentos} intentos; la Api arranca igual y se reintentara en el proximo arranque.",
                    intentos);
                return;
            }
        }
    }
}
