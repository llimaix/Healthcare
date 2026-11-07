using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaCore.Api.Auth;

/// <summary>Endpoints de autenticacion: registro, login y perfil propio.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Registra un usuario nuevo.
    /// - Sistema vacio (sin usuarios): se permite sin token, solo para crear el primer
    ///   usuario, que debe tener rol Admin.
    /// - Con usuarios existentes: solo un Admin autenticado puede registrar usuarios.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegistroRequest request, CancellationToken ct)
    {
        if (await _auth.HayUsuariosAsync(ct))
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { error = "Solo un administrador puede registrar usuarios." });
            }

            if (!User.IsInRole("Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Solo un administrador puede registrar usuarios." });
            }
        }
        else if (request.Rol != "Admin")
        {
            return BadRequest(new { error = "El primer usuario del sistema debe tener rol Admin." });
        }

        var (resultado, usuario) = await _auth.RegistrarAsync(request, ct);
        return resultado switch
        {
            AuthResultado.Exito => CreatedAtAction(nameof(Me), null, usuario),
            AuthResultado.CorreoDuplicado => Conflict(new { error = "El correo ya esta registrado." }),
            AuthResultado.RolInexistente => BadRequest(new { error = "El rol indicado no existe." }),
            AuthResultado.MedicoInexistente => BadRequest(new { error = "El medico indicado no existe." }),
            _ => BadRequest(),
        };
    }

    /// <summary>Inicia sesion y devuelve un token de acceso.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var (resultado, token) = await _auth.LoginAsync(request, ct);
        if (resultado != AuthResultado.Exito)
        {
            return Unauthorized(new { error = "Credenciales invalidas." });
        }

        return Ok(new LoginResponse { Token = token! });
    }

    /// <summary>Devuelve el usuario autenticado.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!int.TryParse(IdUsuarioDelToken(User), out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await _auth.ObtenerPorIdAsync(idUsuario, ct);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    /// <summary>Extrae el id de usuario (claim sub) del token.</summary>
    internal static string? IdUsuarioDelToken(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
}
