using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;

namespace RavEat.Api.Controllers;

// Esto NO es una operacion del local: es una herramienta de prueba para volver a arrancar un
// ensayo desde cero. Vive en su propio controller y no en PedidosController a proposito: ahi un
// pedido no se borra, se cancela, y mezclar el borrado fisico con las acciones del mostrador
// invitaria a usarlo como una mas. Cerrado a ADMIN; el resto de los roles ni ve que existe.
[ApiController]
[Authorize(Roles = RolCodigos.Admin)]
[Route("api/mantenimiento")]
public sealed class MantenimientoController(RavEatDbContext db) : ControllerBase {
    // DELETE no reinicia el AUTO_INCREMENT de MySQL, y el codigo del pedido sale de MAX(id) + 1:
    // sin esto el proximo pedido naceria PED-0001 y el siguiente saltaria al id viejo. Las
    // sentencias van escritas enteras: un nombre de tabla no puede viajar como parametro.
    private static readonly string[] SentenciasReinicio = [
        "ALTER TABLE `pedidos` AUTO_INCREMENT = 1",
        "ALTER TABLE `pedidos_items` AUTO_INCREMENT = 1",
        "ALTER TABLE `entregas` AUTO_INCREMENT = 1",
        "ALTER TABLE `notificaciones` AUTO_INCREMENT = 1"
    ];

    // Deja la base como antes del primer pedido: sin pedidos, sin items, sin entregas y sin
    // bandeja. Productos, categorias, clientes y usuarios no se tocan: la carta y las cuentas son
    // lo que se quiere conservar entre prueba y prueba.
    [HttpDelete("pedidos")]
    public async Task<IActionResult> VaciarPedidos(CancellationToken cancellationToken) {
        var pedidos = await db.Pedidos.CountAsync(cancellationToken);
        var entregas = await db.Entregas.CountAsync(cancellationToken);
        var notificaciones = await db.Notificaciones.CountAsync(cancellationToken);

        // De hijo a padre y explicito, aunque las FK ya sean Cascade: el borrado dice lo que hace.
        // La bandeja se vacia entera: todas las notificaciones nacen de un pedido o de su entrega.
        await using(var transaccion = await db.Database.BeginTransactionAsync(cancellationToken)) {
            await db.Notificaciones.ExecuteDeleteAsync(cancellationToken);
            await db.Entregas.ExecuteDeleteAsync(cancellationToken);
            await db.PedidosItems.ExecuteDeleteAsync(cancellationToken);
            await db.Pedidos.ExecuteDeleteAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }

        // Fuera de la transaccion a proposito: ALTER TABLE hace commit implicito en InnoDB.
        foreach(var sentencia in SentenciasReinicio) {
            await db.Database.ExecuteSqlRawAsync(sentencia, cancellationToken);
        }

        return Ok(new {
            pedidos,
            entregas,
            notificaciones,
            mensaje = $"Se borraron {pedidos} pedidos y todo lo que colgaba de ellos."
        });
    }
}
