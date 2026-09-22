using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Domain.Entities;

// La entrega copia la direccion y las coordenadas del cliente AL CREARSE, igual que el item copia
// el precio del producto: si el cliente se muda manana, esta entrega sigue diciendo a donde fue.
// RepartidorId es opcional: la entrega existe desde que el pedido es de tipo "entregas", pero se
// asigna despues. No hay firma en pantalla del receptor: es el modulo extra X10.
public sealed class Entrega : EntityBase {
    public long PedidoId { get; set; }
    public long? RepartidorId { get; set; }
    public string DireccionLinea { get; set; } = string.Empty;
    public string? DireccionReferencia { get; set; }
    public decimal? DireccionLatitud { get; set; }
    public decimal? DireccionLongitud { get; set; }
    public EstadoEntrega Estado { get; set; } = EstadoEntrega.Pendiente;
    public DateTimeOffset? AsignadaEn { get; set; }
    public DateTimeOffset? EntregadaEn { get; set; }
    public string? Observaciones { get; set; }
}
