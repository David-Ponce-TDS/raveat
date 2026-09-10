namespace RavEat.Api.Domain.Entities;

// Id y fechas de auditoria, comunes a todas las entidades. Las fechas las mantiene el DbContext
// en SaveChanges y no cada controller: si dependiera de acordarse, alguna queda sin actualizar.
public abstract class EntityBase {
    public long Id { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActualizadoEn { get; set; } = DateTimeOffset.UtcNow;
}
