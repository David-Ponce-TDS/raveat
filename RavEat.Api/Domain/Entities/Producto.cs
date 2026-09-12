namespace RavEat.Api.Domain.Entities;

// La baja es siempre logica (Activo = false): un producto que alguna vez se vendio no se borra,
// porque los pedidos viejos lo referencian. Disponible es otra cosa: el producto sigue en la
// carta pero hoy no se puede pedir.
public sealed class Producto : EntityBase {
    public long CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Disponible { get; set; } = true;
    public bool Activo { get; set; } = true;
}
