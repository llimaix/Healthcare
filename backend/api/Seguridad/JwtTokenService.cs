using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaCore.Database.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaCore.Api.Seguridad;

/// <summary>
/// Emite tokens JWT firmados con HS256 a partir de la configuracion Jwt:*.
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Genera un token de acceso para el usuario con el rol indicado. Incluye los
    /// claims sub (id de usuario), email (correo), role (nombre del rol) y, cuando el
    /// usuario esta enlazado a un medico, medicoId.
    /// </summary>
    public string GenerarToken(Usuario usuario, string rol)
    {
        var secreto = _configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secreto))
        {
            throw new InvalidOperationException(
                "La configuracion Jwt:Secret es obligatoria para emitir tokens.");
        }

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var minutos = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new(ClaimTypes.Role, rol),
        };

        if (usuario.IdMedico is int idMedico)
        {
            claims.Add(new Claim("medicoId", idMedico.ToString()));
        }

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreto));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(minutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
