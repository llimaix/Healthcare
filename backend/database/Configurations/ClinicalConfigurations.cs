using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaCore.Database.Configurations;

/// <summary>Mapeo de la tabla medico (dominio clinical).</summary>
public class MedicoConfiguration : IEntityTypeConfiguration<Medico>
{
    public void Configure(EntityTypeBuilder<Medico> builder)
    {
        builder.ToTable("medico");
        builder.HasKey(m => m.IdMedico);
        builder.Property(m => m.IdMedico).HasColumnName("id_medico");
        builder.Property(m => m.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Apellido).HasColumnName("apellido").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Colegiado).HasColumnName("colegiado").HasMaxLength(30).IsRequired();
        builder.Property(m => m.Especialidad).HasColumnName("especialidad").HasMaxLength(100).IsRequired();

        builder.HasIndex(m => m.Colegiado).IsUnique();
    }
}

/// <summary>Mapeo de la tabla paciente (dominio clinical).</summary>
public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("paciente");
        builder.HasKey(p => p.IdPaciente);
        builder.Property(p => p.IdPaciente).HasColumnName("id_paciente");
        builder.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Apellido).HasColumnName("apellido").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Cui).HasColumnName("cui").HasMaxLength(20).IsRequired();
        builder.Property(p => p.FechaNacimiento).HasColumnName("fecha_nacimiento");
        builder.Property(p => p.Nit).HasColumnName("nit").HasMaxLength(20).IsRequired();
        builder.Property(p => p.Telefono).HasColumnName("telefono").HasMaxLength(20).IsRequired();
        builder.Property(p => p.CorreoElectronico).HasColumnName("correo_electronico").HasMaxLength(150).IsRequired();
        // genero acepta solo 'F' o 'M' (como en la captura). Se restringe con CHECK.
        builder.Property(p => p.Genero).HasColumnName("genero").HasColumnType("char(1)").IsRequired();
        builder.Property(p => p.TipoSangre).HasColumnName("tipo_sangre").HasMaxLength(5).IsRequired();

        builder.HasIndex(p => p.Cui).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("ck_paciente_genero", "`genero` IN ('F','M')"));
    }
}

/// <summary>Mapeo de la tabla cita (dominio clinical).</summary>
public class CitaConfiguration : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("cita");
        builder.HasKey(c => c.IdCita);
        builder.Property(c => c.IdCita).HasColumnName("id_cita");
        builder.Property(c => c.IdMedico).HasColumnName("id_medico");
        builder.Property(c => c.IdPaciente).HasColumnName("id_paciente");
        builder.Property(c => c.FechaCita).HasColumnName("fecha_cita");
        builder.Property(c => c.Hora).HasColumnName("hora");
        builder.Property(c => c.Motivo).HasColumnName("motivo").HasMaxLength(255);
        builder.Property(c => c.Estado).HasColumnName("estado").HasMaxLength(30);

        builder.HasOne(c => c.Medico)
            .WithMany(m => m.Citas)
            .HasForeignKey(c => c.IdMedico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Paciente)
            .WithMany(p => p.Citas)
            .HasForeignKey(c => c.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla historial_clinico (dominio clinical).</summary>
public class HistorialClinicoConfiguration : IEntityTypeConfiguration<HistorialClinico>
{
    public void Configure(EntityTypeBuilder<HistorialClinico> builder)
    {
        builder.ToTable("historial_clinico");
        builder.HasKey(h => h.IdHistorial);
        builder.Property(h => h.IdHistorial).HasColumnName("id_historial");
        builder.Property(h => h.IdPaciente).HasColumnName("id_paciente");
        builder.Property(h => h.FechaApertura).HasColumnName("fecha_apertura");
        builder.Property(h => h.Antecedentes).HasColumnName("antecedentes").HasColumnType("text");

        builder.HasIndex(h => h.IdPaciente).IsUnique();

        builder.HasOne(h => h.Paciente)
            .WithOne(p => p.HistorialClinico)
            .HasForeignKey<HistorialClinico>(h => h.IdPaciente)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Mapeo de la tabla diagnostico (dominio clinical).</summary>
public class DiagnosticoConfiguration : IEntityTypeConfiguration<Diagnostico>
{
    public void Configure(EntityTypeBuilder<Diagnostico> builder)
    {
        builder.ToTable("diagnostico");
        builder.HasKey(d => d.IdDiagnosticos);
        builder.Property(d => d.IdDiagnosticos).HasColumnName("id_diagnosticos");
        builder.Property(d => d.IdHistorial).HasColumnName("id_historial");
        builder.Property(d => d.IdCita).HasColumnName("id_cita");
        builder.Property(d => d.Descripcion).HasColumnName("descripcion").HasColumnType("text");
        builder.Property(d => d.CodigoCie10).HasColumnName("codigo_cie10").HasMaxLength(10);
        builder.Property(d => d.Alergias).HasColumnName("alergias").HasColumnType("text").IsRequired();
        builder.Property(d => d.Fecha).HasColumnName("fecha");

        builder.HasOne(d => d.Historial)
            .WithMany(h => h.Diagnosticos)
            .HasForeignKey(d => d.IdHistorial)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Cita)
            .WithMany(c => c.Diagnosticos)
            .HasForeignKey(d => d.IdCita)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla receta (dominio clinical).</summary>
public class RecetaConfiguration : IEntityTypeConfiguration<Receta>
{
    public void Configure(EntityTypeBuilder<Receta> builder)
    {
        builder.ToTable("receta");
        builder.HasKey(r => r.IdReceta);
        builder.Property(r => r.IdReceta).HasColumnName("id_receta");
        builder.Property(r => r.IdCita).HasColumnName("id_cita");
        builder.Property(r => r.IdDiagnostico).HasColumnName("id_diagnostico");
        builder.Property(r => r.FechaEmision).HasColumnName("fecha_emision");
        builder.Property(r => r.Indicaciones).HasColumnName("indicaciones").HasColumnType("text");

        builder.HasOne(r => r.Cita)
            .WithMany(c => c.Recetas)
            .HasForeignKey(r => r.IdCita)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Diagnostico)
            .WithMany(d => d.Recetas)
            .HasForeignKey(r => r.IdDiagnostico)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Mapeo de la tabla receta_detalle (dominio clinical).</summary>
public class RecetaDetalleConfiguration : IEntityTypeConfiguration<RecetaDetalle>
{
    public void Configure(EntityTypeBuilder<RecetaDetalle> builder)
    {
        builder.ToTable("receta_detalle");
        builder.HasKey(rd => rd.IdRecetaDetalle);
        builder.Property(rd => rd.IdRecetaDetalle).HasColumnName("id_receta_detalle");
        builder.Property(rd => rd.IdReceta).HasColumnName("id_receta");
        builder.Property(rd => rd.IdInsumo).HasColumnName("id_insumo");
        builder.Property(rd => rd.Dosis).HasColumnName("dosis").HasMaxLength(100);
        builder.Property(rd => rd.Frecuencia).HasColumnName("frecuencia").HasMaxLength(100);
        builder.Property(rd => rd.DuracionDias).HasColumnName("duracion_dias");

        builder.HasOne(rd => rd.Receta)
            .WithMany(r => r.Detalles)
            .HasForeignKey(rd => rd.IdReceta)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rd => rd.Insumo)
            .WithMany(i => i.RecetaDetalles)
            .HasForeignKey(rd => rd.IdInsumo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla estudio (dominio clinical).</summary>
public class EstudioConfiguration : IEntityTypeConfiguration<Estudio>
{
    public void Configure(EntityTypeBuilder<Estudio> builder)
    {
        builder.ToTable("estudio");
        builder.HasKey(e => e.IdEstudio);
        builder.Property(e => e.IdEstudio).HasColumnName("id_estudio");
        builder.Property(e => e.IdCita).HasColumnName("id_cita");
        builder.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(100);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text");
        builder.Property(e => e.FechaSolicitud).HasColumnName("fecha_solicitud");
        builder.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(30);

        builder.HasOne(e => e.Cita)
            .WithMany(c => c.Estudios)
            .HasForeignKey(e => e.IdCita)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de la tabla resultado_tratante (dominio clinical).</summary>
public class ResultadoTratanteConfiguration : IEntityTypeConfiguration<ResultadoTratante>
{
    public void Configure(EntityTypeBuilder<ResultadoTratante> builder)
    {
        builder.ToTable("resultado_tratante");
        builder.HasKey(rt => rt.IdResultado);
        builder.Property(rt => rt.IdResultado).HasColumnName("id_resultado");
        builder.Property(rt => rt.IdEstudio).HasColumnName("id_estudio");
        builder.Property(rt => rt.IdMedico).HasColumnName("id_medico");
        builder.Property(rt => rt.IdDiagnostico).HasColumnName("id_diagnostico");
        builder.Property(rt => rt.Fecha).HasColumnName("fecha");
        builder.Property(rt => rt.Descripcion).HasColumnName("descripcion").HasColumnType("text");
        builder.Property(rt => rt.Conclusion).HasColumnName("conclusion").HasColumnType("text");

        builder.HasOne(rt => rt.Estudio)
            .WithMany(e => e.ResultadosTratantes)
            .HasForeignKey(rt => rt.IdEstudio)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(rt => rt.Medico)
            .WithMany(m => m.ResultadosTratantes)
            .HasForeignKey(rt => rt.IdMedico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rt => rt.Diagnostico)
            .WithMany(d => d.ResultadosTratantes)
            .HasForeignKey(rt => rt.IdDiagnostico)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
