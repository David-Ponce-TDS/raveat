using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Push;

// Toda notificacion que entra a la bandeja sale tambien como push. Se engancha al guardado y no a
// cada controller: pedidos y entregas ya escriben la bandeja en tres lugares, y con el envio
// repetido en cada uno, tarde o temprano alguno se olvidaba.
//
// Se envia DESPUES de guardar: si el SaveChanges falla, la accion no paso y no hay nada que avisar.
// Y un FCM caido no tumba la accion: el pedido ya cambio, el aviso queda en la bandeja.
public sealed class PushAlGuardar(EnvioPush envio, ILogger<PushAlGuardar> logger) : SaveChangesInterceptor {
    private List<Notificacion> pendientes = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
        // Se juntan antes de guardar: despues, el estado ya no es Added y no se distinguen de las viejas.
        if(eventData.Context is not null) {
            pendientes = eventData.Context.ChangeTracker.Entries<Notificacion>().Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToList();
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) {
        var nuevas = pendientes;
        pendientes = [];
        if(nuevas.Count > 0 && eventData.Context is RavEatDbContext db) {
            try {
                await envio.EnviarAsync(db, nuevas, cancellationToken);
            }
            catch(Exception error) {
                logger.LogWarning(error, "No se pudo enviar la push; el aviso quedo en la bandeja.");
            }
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
