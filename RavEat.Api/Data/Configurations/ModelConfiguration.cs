using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Data.Configurations;

// Toda la Fluent API vive en este archivo: largos, indices y relaciones en un solo lugar en vez de
// repartidos en atributos sobre las entidades.
internal static class ConfigurationExtensions {
    public static void Base<TEntity>(EntityTypeBuilder<TEntity> builder, string table) where TEntity : EntityBase {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CreadoEn).HasColumnName("creado_en");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
    }
}

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria> {
    public void Configure(EntityTypeBuilder<Categoria> b){ ConfigurationExtensions.Base(b, "categorias"); b.Property(x => x.Nombre).HasMaxLength(100).IsRequired(); b.HasIndex(x => x.Nombre).IsUnique(); }
}

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto> {
    public void Configure(EntityTypeBuilder<Producto> b){ ConfigurationExtensions.Base(b, "productos"); b.Property(x => x.Nombre).HasMaxLength(160).IsRequired(); b.Property(x => x.Descripcion).HasMaxLength(1000); b.Property(x => x.Precio).HasPrecision(12, 2); b.Property(x => x.ImagenUrl).HasMaxLength(500); b.HasIndex(x => new {x.CategoriaId, x.Nombre}).IsUnique(); b.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict); }
}
