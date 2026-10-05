using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

// La app avisa en que telefono esta abierta la sesion. El usuario sale del token, como en la
// bandeja: con un usuario_id en el body, cualquiera se anotaria para recibir las push de otro.
// La baja no tiene endpoint propio: viaja con el logout, que es cuando el telefono deja de ser
// de ese usuario.
[ApiController]
[Authorize(Roles = RolesOperativos)]
[Route("api/dispositivos")]
public sealed class DispositivosController(RavEatDbContext db) : ControllerBase {
    private const string RolesOperativos = $"{RolCodigos.Admin},{RolCodigos.Vendedor},{RolCodigos.Proceso},{RolCodigos.Caja},{RolCodigos.Delivery}";

    // Alta o reasignacion: si el token ya estaba —el mismo telefono, otro usuario—, cambia de
    // dueño. Asi la push del usuario anterior no le llega al que entro despues.
    [HttpPut]
    public async Task<IActionResult> Registrar(RegistrarDispositivoRequest request, CancellationToken cancellationToken) {
        if(string.IsNullOrWhiteSpace(request.Token)) return BadRequest(new {codigo = "token_requerido", mensaje = "Falta el token del dispositivo."});
        var dispositivo = await db.DispositivosPush.FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);
        if(dispositivo is null) {
            db.DispositivosPush.Add(new DispositivoPush {UsuarioId = User.UsuarioId(), Token = request.Token});
        }
        else {
            dispositivo.UsuarioId = User.UsuarioId();
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {registrado = true});
    }
}

public sealed record RegistrarDispositivoRequest(string Token);
