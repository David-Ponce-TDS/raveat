using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

// La bandeja es SIEMPRE la del token: el usuario no se manda por query ni por body, porque con un
// usuario_id en la URL cualquiera leeria la bandeja de cualquiera. Es la misma regla de identidad
// de los pedidos, aplicada a la lectura.
[ApiController]
[Authorize(Roles = RolesOperativos)]
[Route("api/notificaciones")]
public sealed class NotificacionesController(RavEatDbContext db) : ControllerBase {
    private const string RolesOperativos = $"{RolCodigos.Admin},{RolCodigos.Vendedor},{RolCodigos.Proceso},{RolCodigos.Caja},{RolCodigos.Delivery}";

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken cancellationToken) {
        var consulta = db.Notificaciones.AsNoTracking().Where(x => x.UsuarioId == User.UsuarioId());
        // El contador de no leidas se calcula sobre la bandeja COMPLETA, no sobre la pagina: si
        // contara lo descargado, la campanita mentiria apenas se junten mas de una pagina.
        var resumen = new NotificacionesResumenResponse(
            await consulta.CountAsync(cancellationToken),
            await consulta.CountAsync(x => x.LeidaEn == null, cancellationToken)
        );
        var (notificaciones, paginaResponse) = await consulta
            .OrderByDescending(x => x.CreadoEn)
            .ThenByDescending(x => x.Id)
            .Select(x => new NotificacionResponse(x.Id, x.Titulo, x.Mensaje, x.Tipo, x.LeidaEn, x.CreadoEn))
            .PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);
        return Ok(new {resumen, notificaciones, pagina = paginaResponse});
    }

    // Marca como leidas las del usuario. Con id marca una sola; sin id, todas las pendientes.
    [HttpPut("leer")]
    public async Task<IActionResult> MarcarLeidas(MarcarLeidasRequest request, CancellationToken cancellationToken) {
        var ahora = DateTimeOffset.UtcNow;
        var consulta = db.Notificaciones.Where(x => x.UsuarioId == User.UsuarioId() && x.LeidaEn == null);
        if(request.NotificacionId.HasValue) consulta = consulta.Where(x => x.Id == request.NotificacionId.Value);
        var marcadas = await consulta.ExecuteUpdateAsync(x => x.SetProperty(n => n.LeidaEn, ahora), cancellationToken);
        return Ok(new {marcadas});
    }
}

public sealed record MarcarLeidasRequest(long? NotificacionId);
public sealed record NotificacionesResumenResponse(int Total, int NoLeidas);
public sealed record NotificacionResponse(long Id, string Titulo, string Mensaje, TipoNotificacion Tipo, DateTimeOffset? LeidaEn, DateTimeOffset CreadoEn);
