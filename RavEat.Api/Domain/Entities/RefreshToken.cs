namespace RavEat.Api.Domain.Entities;

// Token de larga vida que permite renovar el access token sin volver a pedir la contrasena.
// Se guarda **hasheado**: quien lea la tabla no puede usar los tokens que hay adentro, igual que
// con las contrasenas.
// Cada renovacion **rota** el token: revoca el usado y emite uno nuevo. Asi, un token robado deja
// de servir en cuanto el duenio legitimo renueva.
public sealed class RefreshToken : EntityBase {
    public long UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }
}
