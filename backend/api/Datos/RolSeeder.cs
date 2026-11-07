using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;

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
}
