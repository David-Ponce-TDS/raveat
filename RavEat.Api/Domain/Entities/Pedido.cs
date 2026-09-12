using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Domain.Entities;

// El pago (estado, medio, propina) son campos del pedido y no una tabla de cobros: un pedido del
// taller tiene un solo cobro.
// ClienteId es opcional: un pedido de mostrador no siempre tiene a quien asociarse.
// Todavia no hay UsuarioCreadorId porque no existen los usuarios: entra en v5 con la sesion.
public sealed class Pedido : EntityBase {
    public long? ClienteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public TipoPedido Tipo { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.Borrador;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
    public MedioPago? MedioPago { get; set; }
    public decimal PropinaImporte { get; set; }
    public DateTimeOffset? PagadoEn { get; set; }
    public string? Observaciones { get; set; }
    public DateTimeOffset? ConfirmadoEn { get; set; }
    public DateTimeOffset? CerradoEn { get; set; }
}
