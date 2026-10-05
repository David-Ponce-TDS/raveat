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

// Como se NOMBRA cada valor en los textos que lee una persona: el aviso de la bandeja (y su push),
// los errores de transicion y el comprobante. Son los mismos textos de utils/estados.js en la app:
// antes salia el codigo de la API ('en_preparacion') y el aviso no se parecia a la pantalla.
public static class TextosEstados {
    public static string Texto(this EstadoPedido estado) => estado switch {
        EstadoPedido.Borrador => "Borrador",
        EstadoPedido.Confirmado => "Confirmado",
        EstadoPedido.EnPreparacion => "En preparación",
        EstadoPedido.Listo => "Listo",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cerrado => "Cerrado",
        _ => "Cancelado"
    };

    public static string Texto(this EstadoPago estado) => estado switch {
        EstadoPago.Pendiente => "Pendiente",
        EstadoPago.Pagado => "Pagado",
        _ => "Anulado"
    };

    public static string Texto(this MedioPago medio) => medio switch {
        MedioPago.Efectivo => "Efectivo",
        MedioPago.Transferencia => "Transferencia",
        _ => "Tarjeta"
    };

    public static string Texto(this EstadoEntrega estado) => estado switch {
        EstadoEntrega.Pendiente => "Pendiente",
        EstadoEntrega.Asignada => "Asignada",
        EstadoEntrega.EnCamino => "En camino",
        EstadoEntrega.Entregada => "Entregada",
        _ => "Cancelada"
    };
}
