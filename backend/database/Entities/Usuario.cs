namespace ClinicaCore.Database.Entities;

/// <summary>
/// Cuenta de acceso con autenticacion JWT (dominio auth).
/// Puede enlazarse opcionalmente a un medico.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int IdRol { get; set; }
    public int? IdMedico { get; set; }
    public bool Activo { get; set; }

    public Rol Rol { get; set; } = null!;
    public Medico? Medico { get; set; }
}
