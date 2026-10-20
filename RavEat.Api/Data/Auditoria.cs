using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Data;

// El historial se escribe desde varios controllers, y siempre igual: quien, sobre que, de que
// estado a cual. Se centraliza aca por el mismo motivo que las notificaciones no se arman a mano en
// cada endpoint — para que la forma del registro no dependa de quien lo escribio.
//
// Ojo con lo que NO hace: no llama a SaveChanges. El registro se agrega al mismo cambio que la
// accion que lo produjo, asi que o se guardan los dos o ninguno. Un historial que dice que el
// pedido paso a "listo" cuando el pedido no cambio es peor que no tener historial.
public static class Auditoria {
    public static void RegistrarPedido(this RavEatDbContext db, long usuarioId, long pedidoId, string? estadoAnterior, string estadoNuevo, string? observacion = null) {
        db.Historiales.Add(new Historial {
            PedidoId = pedidoId,
            UsuarioId = usuarioId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            Observacion = observacion
        });
    }

    public static void RegistrarEntrega(this RavEatDbContext db, long usuarioId, long entregaId, string? estadoAnterior, string estadoNuevo, string? observacion = null) {
        db.Historiales.Add(new Historial {
            EntregaId = entregaId,
            UsuarioId = usuarioId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            Observacion = observacion
        });
    }
}
