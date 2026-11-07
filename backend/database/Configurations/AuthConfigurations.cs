using ClinicaCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaCore.Database.Configurations;

/// <summary>Mapeo de la tabla rol (dominio auth).</summary>
public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");
        builder.HasKey(r => r.IdRol);
        builder.Property(r => r.IdRol).HasColumnName("id_rol");
        builder.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Nombre).IsUnique();
    }
}

/// <summary>Mapeo de la tabla usuario (dominio auth).</summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(u => u.IdUsuario);
        builder.Property(u => u.IdUsuario).HasColumnName("id_usuario");
        builder.Property(u => u.Correo).HasColumnName("correo").HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(u => u.IdRol).HasColumnName("id_rol");
        builder.Property(u => u.IdMedico).HasColumnName("id_medico");
        builder.Property(u => u.Activo).HasColumnName("activo");

        builder.HasIndex(u => u.Correo).IsUnique();

        builder.HasOne(u => u.Rol)
            .WithMany(r => r.Usuarios)
            .HasForeignKey(u => u.IdRol)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Medico)
            .WithOne(m => m.Usuario)
            .HasForeignKey<Usuario>(u => u.IdMedico)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
