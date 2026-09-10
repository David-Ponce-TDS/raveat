namespace RavEat.Api.Domain.Entities;

public sealed class Categoria : EntityBase {
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
