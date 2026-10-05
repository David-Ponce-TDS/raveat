namespace RavEat.Api.Domain.Entities;

// El telefono donde el usuario tiene la sesion abierta, identificado por su token de FCM. Es la
// direccion a la que la API le manda la push: sin esta tabla, la API sabe A QUIEN avisar (la
// bandeja) pero no A DONDE. Un usuario puede tener varios telefonos; un token, un solo usuario.
public sealed class DispositivoPush : EntityBase {
    public long UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
}
