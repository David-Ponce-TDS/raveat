namespace RavEat.Api.Domain.Entities;

// La direccion vive embebida en el cliente y no en una tabla aparte: para el alcance del taller,
// un cliente tiene una sola direccion. Los tres campos de direccion son opcionales porque un
// cliente que solo retira en el local no necesita ninguno.
public sealed class Cliente : EntityBase {
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DireccionLinea { get; set; }
    public string? DireccionReferencia { get; set; }
    public decimal? DireccionLatitud { get; set; }
    public decimal? DireccionLongitud { get; set; }
    public bool Activo { get; set; } = true;
}
