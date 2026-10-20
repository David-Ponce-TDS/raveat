namespace RavEat.Api.Domain.Entities;

// Quien movio que, y cuando. Es la unica tabla del modelo que no se edita ni se borra: un historial
// que se puede corregir no sirve para lo que existe. Por eso tampoco tiene baja logica.
//
// PedidoId y EntregaId son los dos objetos que cambian de estado en la app, y siempre viene uno de
// los dos. El usuario NO llega del cliente: sale del token, igual que el creador del pedido — si
// se pudiera mandar por el body, cualquiera firmaria un movimiento con el nombre de otro.
public sealed class Historial : EntityBase {
    public long? PedidoId { get; set; }
    public long? EntregaId { get; set; }
    public long UsuarioId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}
