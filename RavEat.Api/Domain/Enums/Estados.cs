namespace RavEat.Api.Domain.Enums;

public enum TipoPedido {
    Presencial,
    Retiro,
    Entregas
}

// El ciclo de vida de un pedido. Borrador es el estado inicial: el pedido existe pero todavia
// no se confirmo, asi que se puede editar. Desde Confirmado en adelante solo cambia de estado.
public enum EstadoPedido {
    Borrador,
    Confirmado,
    EnPreparacion,
    Listo,
    Entregado,
    Cerrado,
    Cancelado
}

// El pago vive dentro del pedido y no en una tabla aparte: para el alcance del taller, un pedido
// tiene un solo cobro.
public enum EstadoPago {
    Pendiente,
    Pagado,
    Anulado
}

public enum MedioPago {
    Efectivo,
    Transferencia,
    Tarjeta
}

// El ciclo de vida de una entrega, separado del estado del pedido: el pedido puede estar Listo
// y la entrega todavia Pendiente de asignar. Menos estados que un delivery real a proposito.
public enum EstadoEntrega {
    Pendiente,
    Asignada,
    EnCamino,
    Entregada,
    Cancelada
}

// El tipo colorea la bandeja y permite filtrar; no cambia el comportamiento.
public enum TipoNotificacion {
    Informacion,
    Pedido,
    Entrega
}
