namespace ClinicaCore.Database.Entities;

/// <summary>
/// Catalogo de roles para el control de acceso (dominio auth).
/// </summary>
public class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
