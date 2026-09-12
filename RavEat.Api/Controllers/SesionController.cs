using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Controllers;

// Entrar, renovar y salir. Va separado de UsuariosController a proposito: los atributos
// `[Authorize]` de clase y de metodo se combinan con AND, asi que un controller marcado
// `[Authorize(Roles = ADMIN)]` no puede tener adentro un endpoint abierto a cualquier usuario
// autenticado. Y ademas son dos responsabilidades distintas: gestionar mi sesion no es lo mismo
// que administrar los usuarios del local.
[ApiController]
[Route("api/sesion")]
public sealed class SesionController(RavEatDbContext db, TokenService tokens) : ControllerBase {
    private static readonly PasswordHasher<Usuario> Hasher = new();

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        // Mismo mensaje para "no existe" y "clave incorrecta": decir cual de las dos fallo le
        // confirma a un atacante que ese email tiene cuenta.
        if(usuario is null || !usuario.Activo) return Unauthorized(new {codigo = "credenciales_invalidas", mensaje = "Email o contraseña incorrectos."});
        var verificacion = Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password ?? string.Empty);
        if(verificacion == PasswordVerificationResult.Failed) return Unauthorized(new {codigo = "credenciales_invalidas", mensaje = "Email o contraseña incorrectos."});

        return Ok(new {sesion = await EmitirSesionAsync(usuario, cancellationToken)});
    }

    // Publico a proposito: se llama justo cuando el access token vencio, asi que exigir uno valido
    // haria imposible renovar.
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken) {
        var hash = TokenService.HashRefresh(request.RefreshToken ?? string.Empty);
        var guardado = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if(guardado is null || guardado.RevocadoEn is not null || guardado.ExpiraEn <= DateTimeOffset.UtcNow) {
            return Unauthorized(new {codigo = "refresh_invalido", mensaje = "La sesión expiró. Volvé a iniciar sesión."});
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == guardado.UsuarioId, cancellationToken);
        if(usuario is null || !usuario.Activo) return Unauthorized(new {codigo = "refresh_invalido", mensaje = "La sesión expiró. Volvé a iniciar sesión."});

        // Rotacion: el token presentado queda revocado y se emite uno nuevo. Si alguien robo el
        // refresh, deja de servir en cuanto el duenio legitimo renueva.
        guardado.RevocadoEn = DateTimeOffset.UtcNow;
        return Ok(new {sesion = await EmitirSesionAsync(usuario, cancellationToken)});
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken) {
        var hash = TokenService.HashRefresh(request.RefreshToken ?? string.Empty);
        var guardado = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if(guardado is null) return Ok(new {cerrada = true});

        if(request.Todos) {
            // Cerrar sesion en todos los dispositivos: revoca todo lo vigente del usuario.
            var vigentes = await db.RefreshTokens.Where(x => x.UsuarioId == guardado.UsuarioId && x.RevocadoEn == null).ToListAsync(cancellationToken);
            vigentes.ForEach(token => token.RevocadoEn = DateTimeOffset.UtcNow);
        }
        else {
            guardado.RevocadoEn = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {cerrada = true});
    }

    // Quien soy. `[Authorize]` a secas es correcto aca y es el unico caso del proyecto: un usuario
    // sin rol tiene que poder preguntarlo, porque de esta respuesta la app aprende que todavia no
    // lo habilitaron y muestra la pantalla de espera en vez de un error.
    [Authorize]
    [HttpGet("yo")]
    public async Task<IActionResult> Yo(CancellationToken cancellationToken) {
        var usuarioId = User.UsuarioId();
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == usuarioId, cancellationToken);
        if(usuario is null) return NotFound(new {codigo = "usuario_no_encontrado", mensaje = "El usuario no existe."});
        var rol = usuario.RolId.HasValue
            ? await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == usuario.RolId.Value, cancellationToken)
            : null;
        return Ok(new {usuario = new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, rol?.Id, rol?.Codigo, rol?.Nombre)});
    }

    private async Task<SesionResponse> EmitirSesionAsync(Usuario usuario, CancellationToken cancellationToken) {
        var rol = usuario.RolId.HasValue
            ? await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == usuario.RolId.Value && x.Activo, cancellationToken)
            : null;
        var (token, expiraEn) = tokens.Emitir(usuario, rol);
        var (refresh, refreshHash, refreshExpiraEn) = tokens.EmitirRefresh();

        db.RefreshTokens.Add(new RefreshToken {
            UsuarioId = usuario.Id,
            TokenHash = refreshHash,
            ExpiraEn = refreshExpiraEn
        });
        await db.SaveChangesAsync(cancellationToken);

        return new SesionResponse(
            token,
            expiraEn,
            refresh,
            refreshExpiraEn,
            new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, rol?.Id, rol?.Codigo, rol?.Nombre)
        );
    }
}

public sealed record SesionResponse(string Token, DateTimeOffset ExpiraEn, string RefreshToken, DateTimeOffset RefreshExpiraEn, UsuarioResponse Usuario);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken, bool Todos);
