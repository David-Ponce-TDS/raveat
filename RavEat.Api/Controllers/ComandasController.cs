using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

// La comanda es el pedido mirado desde la cocina. No hay entidad "Comanda" ni tabla nueva: seria
// duplicar el pedido y sus items para agregarles una columna. Lo unico que hacia falta es el estado
// POR ITEM, que es lo que la cocina realmente maneja — el guiso todavia en el fuego mientras la
// ensalada ya salio.
//
// El estado del PEDIDO no se toca a mano desde aca: se recalcula a partir de sus items. Si se
// pudieran mover los dos por separado, tarde o temprano dirian cosas distintas.
[ApiController]
[Authorize(Roles = $"{RolCodigos.Proceso},{RolCodigos.Admin}")]
[Route("api/comandas")]
public sealed class ComandasController(RavEatDbContext db) : ControllerBase {
    // Lo que cocina tiene que ver: lo confirmado (todavia sin empezar), lo que esta preparando y lo
    // que acaba de terminar. Un borrador no se cocina y un pedido entregado ya no es asunto suyo.
    private static readonly EstadoPedido[] EstadosDeCocina = [
        EstadoPedido.Confirmado,
        EstadoPedido.EnPreparacion,
        EstadoPedido.Listo
    ];

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estados,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Pedidos.AsNoTracking().Where(x => EstadosDeCocina.Contains(x.Estado));

        // Contadores sobre el total, no sobre la pagina ni sobre el filtro: son los botones con los
        // que se elige el filtro, asi que no pueden depender de el.
        var conteos = await consulta
            .GroupBy(x => x.Estado)
            .Select(g => new {Estado = g.Key, Cantidad = g.Count()})
            .ToListAsync(cancellationToken);
        var resumen = conteos.ToDictionary(x => x.Estado.ToString(), x => x.Cantidad);
        var total = conteos.Sum(x => x.Cantidad);

        var estadosFiltro = FiltroEnum.Parsear<EstadoPedido>(estados);
        if(estadosFiltro.Count > 0) consulta = consulta.Where(x => estadosFiltro.Contains(x.Estado));

        // Se ordena por la hora de confirmacion ascendente: en una cocina se atiende por orden de
        // llegada, al reves que en el listado de pedidos, donde interesa lo ultimo que entro.
        var (comandas, paginaResponse) = await (
            from pedido in consulta
            join cliente in db.Clientes.AsNoTracking() on pedido.ClienteId equals cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby pedido.ConfirmadoEn ?? pedido.CreadoEn, pedido.Id
            select new ComandaResumenResponse(
                pedido.Id,
                pedido.Codigo,
                pedido.Tipo,
                pedido.Estado,
                cliente != null ? cliente.Nombre : null,
                pedido.Observaciones,
                pedido.ConfirmadoEn ?? pedido.CreadoEn,
                db.PedidosItems.Count(x => x.PedidoId == pedido.Id),
                db.PedidosItems.Count(x => x.PedidoId == pedido.Id && x.Estado == EstadoItemPedido.Listo)
            )
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        // Los items de la pagina se traen en una sola consulta y se agrupan en memoria: una por
        // comanda serian veinte viajes a la base para dibujar una pantalla.
        var pedidosIds = comandas.Select(x => x.PedidoId).ToList();
        var items = await db.PedidosItems.AsNoTracking()
            .Where(x => pedidosIds.Contains(x.PedidoId))
            .OrderBy(x => x.Id)
            .Select(x => new ComandaItemResponse(x.Id, x.PedidoId, x.ProductoNombre, x.Cantidad, x.Estado, x.Observaciones))
            .ToListAsync(cancellationToken);

        return Ok(new {comandas, items, pagina = paginaResponse, resumen, total});
    }

    // Mover un item. Es la accion natural de la cocina: se marca lo que se empieza y lo que sale.
    [HttpPut("items/{id:long}/estado")]
    public async Task<IActionResult> CambiarEstadoItem(long id, CambiarEstadoItemRequest request, CancellationToken cancellationToken) {
        var item = await db.PedidosItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(item is null) return NotFound(new {codigo = "item_no_encontrado", mensaje = "El ítem no existe."});

        var pedido = await db.Pedidos.FirstOrDefaultAsync(x => x.Id == item.PedidoId, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});
        if(!EstadosDeCocina.Contains(pedido.Estado)) {
            return BadRequest(new {codigo = "pedido_fuera_de_cocina", mensaje = $"El pedido está en '{pedido.Estado.Texto()}' y ya no pasa por preparación."});
        }

        item.Estado = request.Estado;
        await RecalcularEstadoPedidoAsync(pedido, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado_item = item.Estado, estado_pedido = pedido.Estado});
    }

    // Atajo de la cocina real: "salió todo". Evita ir tocando item por item cuando el pedido sale
    // completo, que es lo mas frecuente.
    [HttpPut("{id:long}/listo")]
    public async Task<IActionResult> MarcarTodoListo(long id, CancellationToken cancellationToken) {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});
        if(!EstadosDeCocina.Contains(pedido.Estado)) {
            return BadRequest(new {codigo = "pedido_fuera_de_cocina", mensaje = $"El pedido está en '{pedido.Estado.Texto()}' y ya no pasa por preparación."});
        }

        var items = await db.PedidosItems.Where(x => x.PedidoId == pedido.Id).ToListAsync(cancellationToken);
        if(items.Count == 0) return BadRequest(new {codigo = "pedido_sin_items", mensaje = "El pedido no tiene ítems para preparar."});
        foreach(var item in items) item.Estado = EstadoItemPedido.Listo;

        await RecalcularEstadoPedidoAsync(pedido, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado_pedido = pedido.Estado});
    }

    // La regla completa, escrita una sola vez: el estado del pedido es un resumen de sus items.
    // La notificacion se agrega en el mismo SaveChanges de quien llama: o pasan las dos cosas, o
    // ninguna. Avisar que un pedido esta listo y que el estado no se haya guardado seria peor que
    // no avisar.
    private async Task RecalcularEstadoPedidoAsync(Pedido pedido, CancellationToken cancellationToken) {
        // Consulta de ENTIDADES, no proyeccion. La diferencia importa: el item que se acaba de
        // marcar todavia no se guardo (el SaveChanges viene despues), y una proyeccion
        // (`Select(x => x.Estado)`) lee los valores de la base — el recalculo miraba el estado
        // VIEJO y el pedido quedaba siempre un paso atras de su cocina. La consulta de entidades
        // pasa por el change tracker: la fila ya trackeada vuelve con su valor modificado.
        var itemsPedido = await db.PedidosItems
            .Where(x => x.PedidoId == pedido.Id)
            .ToListAsync(cancellationToken);
        if(itemsPedido.Count == 0) return;
        var estadosItems = itemsPedido.Select(x => x.Estado).ToList();

        var nuevoEstado = estadosItems.All(x => x == EstadoItemPedido.Listo) ? EstadoPedido.Listo
            : estadosItems.All(x => x == EstadoItemPedido.Pendiente) ? EstadoPedido.Confirmado
            : EstadoPedido.EnPreparacion;
        if(nuevoEstado == pedido.Estado) return;

        db.RegistrarPedido(User.UsuarioId(), pedido.Id, pedido.Estado.ToString(), nuevoEstado.ToString(), "Movimiento de preparación.");
        pedido.Estado = nuevoEstado;
        if(nuevoEstado == EstadoPedido.Listo && pedido.UsuarioCreadorId != User.UsuarioId()) {
            db.Notificaciones.Add(new Notificacion {
                UsuarioId = pedido.UsuarioCreadorId,
                Titulo = $"Pedido {pedido.Codigo} listo",
                Mensaje = "Terminó la preparación: ya se puede entregar o retirar.",
                Tipo = TipoNotificacion.Pedido
            });
        }
    }
}

public sealed record ComandaResumenResponse(
    long PedidoId,
    string Codigo,
    TipoPedido Tipo,
    EstadoPedido Estado,
    string? ClienteNombre,
    string? Observaciones,
    DateTimeOffset DesdeEn,
    int TotalItems,
    int ItemsListos
);
public sealed record ComandaItemResponse(long Id, long PedidoId, string ProductoNombre, int Cantidad, EstadoItemPedido Estado, string? Observaciones);
public sealed record CambiarEstadoItemRequest(EstadoItemPedido Estado);
