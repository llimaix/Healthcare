using ClinicaCore.Api.Seguridad;
using ClinicaCore.Database;
using ClinicaCore.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaCore.Api.Auth;

/// <summary>Resultado de una operacion de autenticacion o de gestion de usuarios.</summary>
public enum AuthResultado
{
    Exito,
    CorreoDuplicado,
    RolInexistente,
    MedicoInexistente,
    CredencialesInvalidas,
    NoEncontrado,
    AutoBloqueo,
}

/// <summary>
/// Logica de registro, login y gestion de usuarios. Usa PasswordHasher para
/// almacenar y verificar las contrasenas sobre Usuario.PasswordHash.
/// </summary>
public class AuthService
{
    private readonly ClinicaCoreDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthService(ClinicaCoreDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>true si ya existe al menos un usuario (el sistema dejo de estar vacio).</summary>
    public Task<bool> HayUsuariosAsync(CancellationToken ct = default) =>
        _db.Usuarios.AnyAsync(ct);

    /// <summary>Registra un usuario nuevo con la contrasena ya hasheada.</summary>
    public async Task<(AuthResultado Resultado, UsuarioResponse? Usuario)> RegistrarAsync(
        RegistroRequest request, CancellationToken ct = default)
    {
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == request.Rol, ct);
        if (rol is null)
        {
            return (AuthResultado.RolInexistente, null);
        }

        if (request.IdMedico is int idMedico
            && !await _db.Medicos.AnyAsync(m => m.IdMedico == idMedico, ct))
        {
            return (AuthResultado.MedicoInexistente, null);
        }

        var existe = await _db.Usuarios.AnyAsync(u => u.Correo == request.Correo, ct);
        if (existe)
        {
            return (AuthResultado.CorreoDuplicado, null);
        }

        var usuario = new Usuario
        {
            Correo = request.Correo,
            IdRol = rol.IdRol,
            IdMedico = request.IdMedico,
            Activo = true,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, request.Password);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        return (AuthResultado.Exito, MapearUsuario(usuario, rol.Nombre));
    }

    /// <summary>Verifica las credenciales y emite un token si son validas.</summary>
    public async Task<(AuthResultado Resultado, string? Token)> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Correo == request.Correo && u.Activo, ct);

        if (usuario is null)
        {
            return (AuthResultado.CredencialesInvalidas, null);
        }

        var verificacion = _hasher.VerifyHashedPassword(
            usuario, usuario.PasswordHash, request.Password);

        if (verificacion == PasswordVerificationResult.Failed)
        {
            return (AuthResultado.CredencialesInvalidas, null);
        }

        var token = _jwt.GenerarToken(usuario, usuario.Rol.Nombre);
        return (AuthResultado.Exito, token);
    }

    /// <summary>Obtiene la representacion publica de un usuario por su id.</summary>
    public async Task<UsuarioResponse?> ObtenerPorIdAsync(int idUsuario, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, ct);

        return usuario is null ? null : MapearUsuario(usuario, usuario.Rol.Nombre);
    }

    /// <summary>Lista todos los usuarios (activos e inactivos).</summary>
    public async Task<List<UsuarioResponse>> ListarAsync(CancellationToken ct = default)
    {
        var usuarios = await _db.Usuarios.AsNoTracking()
            .Include(u => u.Rol)
            .OrderBy(u => u.IdUsuario)
            .ToListAsync(ct);

        return usuarios.Select(u => MapearUsuario(u, u.Rol.Nombre)).ToList();
    }

    /// <summary>
    /// Cambia rol, estado y medico vinculado de un usuario. Un Admin no puede quitarse
    /// a si mismo el rol Admin ni desactivar su propia cuenta (evita dejar el sistema
    /// sin administrador).
    /// </summary>
    public async Task<(AuthResultado Resultado, UsuarioResponse? Usuario)> ActualizarAsync(
        int idUsuario, ActualizarUsuarioRequest request, int idUsuarioActual, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, ct);
        if (usuario is null)
        {
            return (AuthResultado.NoEncontrado, null);
        }

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == request.Rol, ct);
        if (rol is null)
        {
            return (AuthResultado.RolInexistente, null);
        }

        if (request.IdMedico is int idMedico
            && !await _db.Medicos.AnyAsync(m => m.IdMedico == idMedico, ct))
        {
            return (AuthResultado.MedicoInexistente, null);
        }

        if (idUsuario == idUsuarioActual && (rol.Nombre != "Admin" || !request.Activo))
        {
            return (AuthResultado.AutoBloqueo, null);
        }

        usuario.IdRol = rol.IdRol;
        usuario.Activo = request.Activo;
        usuario.IdMedico = request.IdMedico;
        await _db.SaveChangesAsync(ct);

        return (AuthResultado.Exito, MapearUsuario(usuario, rol.Nombre));
    }

    private static UsuarioResponse MapearUsuario(Usuario usuario, string rol) => new()
    {
        IdUsuario = usuario.IdUsuario,
        Correo = usuario.Correo,
        Rol = rol,
        IdMedico = usuario.IdMedico,
        Activo = usuario.Activo,
    };
}
