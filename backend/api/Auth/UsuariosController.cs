using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaCore.Api.Auth;

/// <summary>
/// Gestion de usuarios, reservada al rol Admin: listar y cambiar rol, estado
/// (activo/inactivo) y medico vinculado. El alta se hace con POST /api/auth/register.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Admin")]
public class UsuariosController : ControllerBase
{
    private readonly AuthService _auth;

    public UsuariosController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Lista todos los usuarios.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _auth.ListarAsync(ct));

    /// <summary>Actualiza rol, estado y medico vinculado de un usuario.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarUsuarioRequest request, CancellationToken ct)
    {
        if (!int.TryParse(AuthController.IdUsuarioDelToken(User), out var idActual))
        {
            return Unauthorized();
        }

        var (resultado, usuario) = await _auth.ActualizarAsync(id, request, idActual, ct);
        return resultado switch
        {
            AuthResultado.Exito => Ok(usuario),
            AuthResultado.NoEncontrado => NotFound(new { error = $"No existe un usuario con id {id}." }),
            AuthResultado.RolInexistente => BadRequest(new { error = "El rol indicado no existe." }),
            AuthResultado.MedicoInexistente => BadRequest(new { error = "El medico indicado no existe." }),
            AuthResultado.AutoBloqueo => BadRequest(new
            {
                error = "No puedes quitarte el rol Admin ni desactivar tu propia cuenta.",
            }),
            _ => BadRequest(),
        };
    }
}
