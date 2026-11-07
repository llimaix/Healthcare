using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaCore.Database.Configurations;

/// <summary>Mapeo de la tabla tipo_cobertura (dominio billing).</summary>
public class TipoCoberturaConfiguration : IEntityTypeConfiguration<TipoCobertura>
{
    public void Configure(EntityTypeBuilder<TipoCobertura> builder)
    {
        builder.ToTable("tipo_cobertura");
        builder.HasKey(t => t.IdTipoCobertura);
        builder.Property(t => t.IdTipoCobertura).HasColumnName("id_tipo_cobertura");
        builder.Property(t => t.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(t => t.PorcentajeCobertura).HasColumnName("porcentaje_cobertura").HasColumnType("decimal(5,2)");
    }
}

/// <summary>Mapeo de la tabla asociado_seguro (dominio billing).</summary>
public class AsociadoSeguroConfiguration : IEntityTypeConfiguration<AsociadoSeguro>
{
    public void Configure(EntityTypeBuilder<AsociadoSeguro> builder)
    {
        builder.ToTable("asociado_seguro");
        builder.HasKey(a => a.IdAsegurado);
        builder.Property(a => a.IdAsegurado).HasColumnName("id_asegurado");
        builder.Property(a => a.IdPaciente).HasColumnName("id_paciente");
        builder.Property(a => a.IdTipoCobertura).HasColumnName("id_tipo_cobertura");
        builder.Property(a => a.Aseguradora).HasColumnName("aseguradora").HasMaxLength(150);
        builder.Property(a => a.NumeroPoliza).HasColumnName("numero_poliza").HasMaxLength(50);
        builder.Property(a => a.VigenciaDesde).HasColumnName("vigencia_desde");
        builder.Property(a => a.VigenciaHasta).HasColumnName("vigencia_hasta");

        builder.HasOne(a => a.Paciente)
            .WithMany(p => p.AsociadosSeguro)
            .HasForeignKey(a => a.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TipoCobertura)
            .WithMany(t => t.AsociadosSeguro)
            .HasForeignKey(a => a.IdTipoCobertura)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla facturacion (dominio billing).</summary>
public class FacturacionConfiguration : IEntityTypeConfiguration<Facturacion>
{
    public void Configure(EntityTypeBuilder<Facturacion> builder)
    {
        builder.ToTable("facturacion");
        builder.HasKey(f => f.IdFactura);
        builder.Property(f => f.IdFactura).HasColumnName("id_factura");
        builder.Property(f => f.IdPaciente).HasColumnName("id_paciente");
        builder.Property(f => f.IdCita).HasColumnName("id_cita");
        builder.Property(f => f.IdAsegurado).HasColumnName("id_asegurado");
        builder.Property(f => f.Fecha).HasColumnName("fecha");
        builder.Property(f => f.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(12,2)");
        builder.Property(f => f.MontoCobertura).HasColumnName("monto_cobertura").HasColumnType("decimal(12,2)");
        builder.Property(f => f.Total).HasColumnName("total").HasColumnType("decimal(12,2)");
        builder.Property(f => f.Estado).HasColumnName("estado").HasMaxLength(30);

        builder.HasIndex(f => f.IdCita).IsUnique();

        builder.HasOne(f => f.Paciente)
            .WithMany(p => p.Facturaciones)
            .HasForeignKey(f => f.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Cita)
            .WithOne(c => c.Facturacion)
            .HasForeignKey<Facturacion>(f => f.IdCita)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Asegurado)
            .WithMany(a => a.Facturaciones)
            .HasForeignKey(f => f.IdAsegurado)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Mapeo de la tabla facturacion_detalle (dominio billing).</summary>
public class FacturacionDetalleConfiguration : IEntityTypeConfiguration<FacturacionDetalle>
{
    public void Configure(EntityTypeBuilder<FacturacionDetalle> builder)
    {
        builder.ToTable("facturacion_detalle");
        builder.HasKey(fd => fd.IdDetalle);
        builder.Property(fd => fd.IdDetalle).HasColumnName("id_detalle");
        builder.Property(fd => fd.IdFactura).HasColumnName("id_factura");
        builder.Property(fd => fd.Concepto).HasColumnName("concepto").HasMaxLength(255).IsRequired();
        builder.Property(fd => fd.Cantidad).HasColumnName("cantidad");
        builder.Property(fd => fd.PrecioUnitario).HasColumnName("precio_unitario").HasColumnType("decimal(12,2)");

        builder.HasOne(fd => fd.Factura)
            .WithMany(f => f.Detalles)
            .HasForeignKey(fd => fd.IdFactura)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
