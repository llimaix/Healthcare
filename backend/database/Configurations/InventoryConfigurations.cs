using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaCore.Database.Configurations;

/// <summary>Mapeo de la tabla proveedor (dominio inventory).</summary>
public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("proveedor");
        builder.HasKey(p => p.IdProveedor);
        builder.Property(p => p.IdProveedor).HasColumnName("id_proveedor");
        builder.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(p => p.Nit).HasColumnName("nit").HasMaxLength(20);
        builder.Property(p => p.Telefono).HasColumnName("telefono").HasMaxLength(20);
        builder.Property(p => p.Correo).HasColumnName("correo").HasMaxLength(150);
    }
}

/// <summary>Mapeo de la tabla insumo (dominio inventory).</summary>
public class InsumoConfiguration : IEntityTypeConfiguration<Insumo>
{
    public void Configure(EntityTypeBuilder<Insumo> builder)
    {
        builder.ToTable("insumo");
        builder.HasKey(i => i.IdInsumo);
        builder.Property(i => i.IdInsumo).HasColumnName("id_insumo");
        builder.Property(i => i.IdProveedor).HasColumnName("id_proveedor");
        builder.Property(i => i.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(i => i.Tipo).HasColumnName("tipo").HasMaxLength(50);
        builder.Property(i => i.Precio).HasColumnName("precio").HasColumnType("decimal(12,2)");
        builder.Property(i => i.StockMinimo).HasColumnName("stock_minimo");

        builder.HasOne(i => i.Proveedor)
            .WithMany(p => p.Insumos)
            .HasForeignKey(i => i.IdProveedor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla kardex (dominio inventory).</summary>
public class KardexConfiguration : IEntityTypeConfiguration<Kardex>
{
    public void Configure(EntityTypeBuilder<Kardex> builder)
    {
        builder.ToTable("kardex");
        builder.HasKey(k => k.IdKardex);
        builder.Property(k => k.IdKardex).HasColumnName("id_kardex");
        builder.Property(k => k.IdInsumo).HasColumnName("id_insumo");
        builder.Property(k => k.IdProveedor).HasColumnName("id_proveedor");
        builder.Property(k => k.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(10);
        builder.Property(k => k.Cantidad).HasColumnName("cantidad");
        builder.Property(k => k.Fecha).HasColumnName("fecha");
        builder.Property(k => k.Referencia).HasColumnName("referencia").HasMaxLength(100);

        builder.HasOne(k => k.Insumo)
            .WithMany(i => i.MovimientosKardex)
            .HasForeignKey(k => k.IdInsumo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Proveedor)
            .WithMany(p => p.MovimientosKardex)
            .HasForeignKey(k => k.IdProveedor)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
