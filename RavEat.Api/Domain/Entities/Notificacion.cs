using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Domain.Entities;

// La bandeja del usuario: la escribe la API cuando pasa algo que le interesa. Es una de las tres
// cosas que se llaman "notificacion": esta persiste, la local vive en el telefono, la push viaja
// por Firebase. LeidaEn es flag y dato a la vez: null = no leida, con fecha = cuando la vio.
public sealed class Notificacion : EntityBase {
    public long UsuarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Informacion;
    public DateTimeOffset? LeidaEn { get; set; }
}
