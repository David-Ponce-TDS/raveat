using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Controllers;

// Administracion de usuarios: exclusiva de ADMIN, sin excepciones. Entrar y renovar la sesion vive
// en SesionController.
[ApiController]
[Authorize(Roles = RolCodigos.Admin)]
[Route("api/usuarios")]
public sealed class UsuariosController(RavEatDbContext db) : ControllerBase {
    // PasswordHasher aplica PBKDF2 con salt por usuario. Nunca se guarda la contrasena: dos
    // usuarios con la misma clave tienen hashes distintos, y del hash no se vuelve.
    private static readonly PasswordHasher<Usuario> Hasher = new();

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Usuarios.AsNoTracking().Where(x => x.Activo);
        var texto = (busqueda ?? string.Empty).Trim();
        if(texto.Length > 0) consulta = consulta.Where(x => x.Nombre.Contains(texto) || x.Email.Contains(texto));

        var (items, paginaResponse) = await (
            from usuario in consulta
            join rol in db.Roles.AsNoTracking() on usuario.RolId equals rol.Id into roles
            from rol in roles.DefaultIfEmpty()
            orderby usuario.Nombre
            select new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, rol != null ? rol.Id : null, rol != null ? rol.Codigo : null, rol != null ? rol.Nombre : null)
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        // Los roles viajan con el listado: la pantalla los necesita para el desplegable de
        // asignacion y no tiene sentido pedirlos en una segunda consulta.
        var rolesDisponibles = await db.Roles.AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Nombre)
            .Select(x => new RolResponse(x.Id, x.Codigo, x.Nombre))
            .ToListAsync(cancellationToken);

        return Ok(new {usuarios = items, pagina = paginaResponse, roles = rolesDisponibles});
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken cancellationToken) {
        if(string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new {codigo = "usuario_nombre_requerido", mensaje = "El nombre es obligatorio."});
        if(string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return BadRequest(new {codigo = "usuario_email_invalido", mensaje = "El email no tiene un formato válido."});
        if(string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8) return BadRequest(new {codigo = "password_corta", mensaje = "La contraseña necesita al menos 8 caracteres."});

        var email = request.Email.Trim().ToLowerInvariant();
        var duplicado = await db.Usuarios.AnyAsync(x => x.Email == email, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "usuario_duplicado", mensaje = "Ya existe un usuario con ese email."});
        if(request.RolId.HasValue) {
            var rolExiste = await db.Roles.AnyAsync(x => x.Id == request.RolId.Value && x.Activo, cancellationToken);
            if(!rolExiste) return BadRequest(new {codigo = "rol_no_encontrado", mensaje = "El rol indicado no existe."});
        }

        var usuario = new Usuario {
            Nombre = request.Nombre.Trim(),
            Email = email,
            // Sin rol el usuario queda autenticado pero no autorizado, hasta que un admin lo habilite.
            RolId = request.RolId
        };
        usuario.PasswordHash = Hasher.HashPassword(usuario, request.Password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {usuario = await ProyectarAsync(usuario, cancellationToken)});
    }

    // Cambiar el rol invalida el token que el usuario tenga en la mano: `OnTokenValidated` compara
    // el rol del token contra el de la base en cada request.
    [HttpPut("{id:long}/rol")]
    public async Task<IActionResult> CambiarRol(long id, CambiarRolRequest request, CancellationToken cancellationToken) {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(usuario is null) return NotFound(new {codigo = "usuario_no_encontrado", mensaje = "El usuario no existe o fue dado de baja."});
        if(request.RolId.HasValue) {
            var rolExiste = await db.Roles.AnyAsync(x => x.Id == request.RolId.Value && x.Activo, cancellationToken);
            if(!rolExiste) return BadRequest(new {codigo = "rol_no_encontrado", mensaje = "El rol indicado no existe."});
        }
        usuario.RolId = request.RolId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {usuario = await ProyectarAsync(usuario, cancellationToken)});
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken) {
        if(id == User.UsuarioId()) return BadRequest(new {codigo = "autobaja", mensaje = "No podés darte de baja a vos mismo."});
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(usuario is null) return NotFound(new {codigo = "usuario_no_encontrado", mensaje = "El usuario no existe o ya fue dado de baja."});

        usuario.Activo = false;
        // La baja tiene que cortar las sesiones abiertas: si no, el usuario sigue entrando hasta
        // que venza su access token.
        var vigentes = await db.RefreshTokens.Where(x => x.UsuarioId == id && x.RevocadoEn == null).ToListAsync(cancellationToken);
        vigentes.ForEach(token => token.RevocadoEn = DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {eliminado = true});
    }

    private async Task<UsuarioResponse> ProyectarAsync(Usuario usuario, CancellationToken cancellationToken) {
        var rol = usuario.RolId.HasValue
            ? await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == usuario.RolId.Value, cancellationToken)
            : null;
        return new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, rol?.Id, rol?.Codigo, rol?.Nombre);
    }
}

public sealed record UsuarioResponse(long Id, string Nombre, string Email, long? RolId, string? RolCodigo, string? RolNombre);
public sealed record RolResponse(long Id, string Codigo, string Nombre);
public sealed record CrearUsuarioRequest(string Nombre, string Email, string Password, long? RolId);
public sealed record CambiarRolRequest(long? RolId);
