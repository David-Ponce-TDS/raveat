namespace RavEat.Api.Domain.Entities;

// Todas las entidades comparten id y fechas de auditoria. Las fechas las mantiene el DbContext
// en SaveChanges, no cada controller: si dependiera de acordarse, tarde o temprano queda una sin
// actualizar.
public abstract class EntityBase {
    public long Id { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActualizadoEn { get; set; } = DateTimeOffset.UtcNow;
}
