using System.ComponentModel.DataAnnotations;

namespace ClinicaCore.Api.Auth;

/// <summary>Datos para registrar un nuevo usuario.</summary>
public class RegistroRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public string Password { get; set; } = null!;

    [Required]
    public string Rol { get; set; } = null!;

    public int? IdMedico { get; set; }
}

/// <summary>Credenciales para iniciar sesion.</summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public string Password { get; set; } = null!;
}

/// <summary>Respuesta con el token de acceso emitido tras un login valido.</summary>
public class LoginResponse
{
    public string Token { get; set; } = null!;
}

/// <summary>Representacion publica de un usuario (sin el hash de contrasena).</summary>
public class UsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public int? IdMedico { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Datos que un Admin puede cambiar de un usuario existente.</summary>
public class ActualizarUsuarioRequest
{
    [Required]
    public string Rol { get; set; } = null!;

    [Required]
    public bool Activo { get; set; }

    public int? IdMedico { get; set; }
}
