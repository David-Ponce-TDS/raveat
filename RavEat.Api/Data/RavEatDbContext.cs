using Microsoft.EntityFrameworkCore;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Data;

public sealed class RavEatDbContext(DbContextOptions<RavEatDbContext> options) : DbContext(options) {
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder){
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RavEatDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess){ ActualizarAuditoria(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default){ ActualizarAuditoria(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

    private void ActualizarAuditoria(){
        var ahora = DateTimeOffset.UtcNow;
        foreach(var entry in ChangeTracker.Entries<EntityBase>()){
            if(entry.State == EntityState.Added){ entry.Entity.CreadoEn = ahora; entry.Entity.ActualizadoEn = ahora; }
            if(entry.State == EntityState.Modified) entry.Entity.ActualizadoEn = ahora;
        }
    }
}
