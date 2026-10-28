using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Data.Configurations;

// Toda la Fluent API vive en este archivo: largos, indices y relaciones en un solo lugar en vez de
// repartidos en atributos sobre las entidades.
internal static class ConfigurationExtensions {
    // Los enums se guardan como texto y no como numero: una fila de la base se lee sola, y agregar
    // un valor en el medio del enum no reinterpreta los datos existentes.
    public static PropertyBuilder<TEnum> EnumTexto<TEnum>(this PropertyBuilder<TEnum> property, int length = 30) where TEnum : struct, Enum => property.HasConversion<string>().HasMaxLength(length);

    public static void Base<TEntity>(EntityTypeBuilder<TEntity> builder, string table) where TEntity : EntityBase {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CreadoEn).HasColumnName("creado_en");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
    }
}

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol> {
    public void Configure(EntityTypeBuilder<Rol> b){ ConfigurationExtensions.Base(b, "roles"); b.Property(x => x.Nombre).HasMaxLength(80).IsRequired(); b.Property(x => x.Codigo).HasMaxLength(40).IsRequired(); b.HasIndex(x => x.Codigo).IsUnique(); }
}

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario> {
    // SetNull en el rol: dar de baja un rol no borra a sus usuarios, los deja sin rol. Vuelven a
    // estar autenticados y no autorizados, que es un estado valido del modelo.
    public void Configure(EntityTypeBuilder<Usuario> b){ ConfigurationExtensions.Base(b, "usuarios"); b.Property(x => x.Nombre).HasMaxLength(120).IsRequired(); b.Property(x => x.Email).HasMaxLength(180).IsRequired(); b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired(); b.HasIndex(x => x.Email).IsUnique(); b.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.SetNull); }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken> {
    public void Configure(EntityTypeBuilder<RefreshToken> b){ ConfigurationExtensions.Base(b, "refresh_tokens"); b.Property(x => x.TokenHash).HasMaxLength(120).IsRequired(); b.HasIndex(x => x.TokenHash).IsUnique(); b.HasIndex(x => new {x.UsuarioId, x.ExpiraEn}); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); }
}

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria> {
    public void Configure(EntityTypeBuilder<Categoria> b){ ConfigurationExtensions.Base(b, "categorias"); b.Property(x => x.Nombre).HasMaxLength(100).IsRequired(); b.HasIndex(x => x.Nombre).IsUnique(); }
}

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto> {
    public void Configure(EntityTypeBuilder<Producto> b){ ConfigurationExtensions.Base(b, "productos"); b.Property(x => x.Nombre).HasMaxLength(160).IsRequired(); b.Property(x => x.Descripcion).HasMaxLength(1000); b.Property(x => x.Precio).HasPrecision(12, 2); b.Property(x => x.ImagenUrl).HasMaxLength(500); b.HasIndex(x => new {x.CategoriaId, x.Nombre}).IsUnique(); b.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente> {
    public void Configure(EntityTypeBuilder<Cliente> b){ ConfigurationExtensions.Base(b, "clientes"); b.Property(x => x.Nombre).HasMaxLength(120).IsRequired(); b.Property(x => x.Telefono).HasMaxLength(40).IsRequired(); b.Property(x => x.Email).HasMaxLength(180); b.Property(x => x.DireccionLinea).HasMaxLength(255); b.Property(x => x.DireccionReferencia).HasMaxLength(500); b.Property(x => x.DireccionLatitud).HasPrecision(10, 7); b.Property(x => x.DireccionLongitud).HasPrecision(10, 7); b.HasIndex(x => x.Telefono); }
}

public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido> {
    // El indice por (estado, creado_en) es el que usa el listado: filtra por estado y ordena por
    // fecha. Sin el, cada pagina recorre la tabla entera. El del token es unico: la pagina publica
    // busca el pedido por el token completo, nunca por un pedazo.
    public void Configure(EntityTypeBuilder<Pedido> b){ ConfigurationExtensions.Base(b, "pedidos"); b.Property(x => x.Codigo).HasMaxLength(40).IsRequired(); b.Property(x => x.TokenPublico).HasMaxLength(32).IsRequired(); b.Property(x => x.Tipo).EnumTexto(); b.Property(x => x.Estado).EnumTexto(); b.Property(x => x.Subtotal).HasPrecision(12, 2); b.Property(x => x.Descuento).HasPrecision(12, 2); b.Property(x => x.Total).HasPrecision(12, 2); b.Property(x => x.EstadoPago).EnumTexto(); b.Property(x => x.MedioPago).HasConversion<string>().HasMaxLength(30); b.Property(x => x.PropinaImporte).HasPrecision(12, 2); b.Property(x => x.Observaciones).HasMaxLength(1000); b.HasIndex(x => x.Codigo).IsUnique(); b.HasIndex(x => x.TokenPublico).IsUnique(); b.HasIndex(x => new {x.Estado, x.CreadoEn}); b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioCreadorId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class PedidoItemConfiguration : IEntityTypeConfiguration<PedidoItem> {
    // Cascade contra el pedido: borrar un pedido se lleva sus items, que no existen sin el.
    // Restrict contra el producto: un producto usado en un pedido no se puede borrar.
    public void Configure(EntityTypeBuilder<PedidoItem> b){ ConfigurationExtensions.Base(b, "pedidos_items"); b.Property(x => x.ProductoNombre).HasMaxLength(160).IsRequired(); b.Property(x => x.PrecioUnitario).HasPrecision(12, 2); b.Property(x => x.Subtotal).HasPrecision(12, 2); b.Property(x => x.Estado).EnumTexto(); b.Property(x => x.Observaciones).HasMaxLength(500); b.HasIndex(x => x.PedidoId); b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class EntregaConfiguration : IEntityTypeConfiguration<Entrega> {
    // Una entrega por pedido (indice unico): el pedido es el centro y la entrega, su apendice.
    // SetNull en el repartidor: dar de baja al repartidor no borra la entrega, la deja sin asignar.
    public void Configure(EntityTypeBuilder<Entrega> b){ ConfigurationExtensions.Base(b, "entregas"); b.Property(x => x.DireccionLinea).HasMaxLength(255).IsRequired(); b.Property(x => x.DireccionReferencia).HasMaxLength(500); b.Property(x => x.DireccionLatitud).HasPrecision(10, 7); b.Property(x => x.DireccionLongitud).HasPrecision(10, 7); b.Property(x => x.Estado).EnumTexto(); b.Property(x => x.FirmaUrl).HasMaxLength(255); b.Property(x => x.Observaciones).HasMaxLength(1000); b.HasIndex(x => x.PedidoId).IsUnique(); b.HasIndex(x => new {x.RepartidorId, x.Estado}); b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RepartidorId).OnDelete(DeleteBehavior.SetNull); }
}

public sealed class DispositivoPushConfiguration : IEntityTypeConfiguration<DispositivoPush> {
    // El token es unico: si el telefono cambia de usuario, la fila se reasigna en vez de duplicarse
    // y la push nunca le llega al usuario anterior. Cascade: los telefonos no sobreviven al usuario.
    public void Configure(EntityTypeBuilder<DispositivoPush> b){ ConfigurationExtensions.Base(b, "dispositivos_push"); b.Property(x => x.Token).HasMaxLength(512).IsRequired(); b.HasIndex(x => x.Token).IsUnique(); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); }
}

public sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion> {
    // El indice por (usuario, leida_en) sirve a las dos consultas de la bandeja: listar las del
    // usuario y contar las no leidas. Cascade: la bandeja no sobrevive al usuario.
    public void Configure(EntityTypeBuilder<Notificacion> b){ ConfigurationExtensions.Base(b, "notificaciones"); b.Property(x => x.Titulo).HasMaxLength(160).IsRequired(); b.Property(x => x.Mensaje).HasMaxLength(500).IsRequired(); b.Property(x => x.Tipo).EnumTexto(); b.HasIndex(x => new {x.UsuarioId, x.LeidaEn}); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); }
}

public sealed class HistorialConfiguration : IEntityTypeConfiguration<Historial> {
    // Restrict contra el usuario, al reves que la bandeja: dar de baja a alguien no puede borrar
    // lo que hizo. El historial vale justamente por eso.
    public void Configure(EntityTypeBuilder<Historial> b){ ConfigurationExtensions.Base(b, "historiales"); b.Property(x => x.EstadoAnterior).HasMaxLength(40); b.Property(x => x.EstadoNuevo).HasMaxLength(40).IsRequired(); b.Property(x => x.Observacion).HasMaxLength(500); b.HasIndex(x => new {x.UsuarioId, x.CreadoEn}); b.HasIndex(x => x.PedidoId); b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Entrega>().WithMany().HasForeignKey(x => x.EntregaId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict); }
}
