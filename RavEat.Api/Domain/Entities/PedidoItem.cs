namespace RavEat.Api.Domain.Entities;

// El nombre y el precio se copian al crear el item, no se leen del producto al mostrar el pedido.
// Es a proposito: si manana sube el precio de la pizza, un pedido de la semana pasada tiene que
// seguir diciendo lo que costo entonces. Es el mismo motivo por el que las bajas son logicas.
public sealed class PedidoItem : EntityBase {
    public long PedidoId { get; set; }
    public long ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public string? Observaciones { get; set; }
}
