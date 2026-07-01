using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoDePedidos.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Documento)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("[Ativo] = 1")
            .HasDatabaseName("IX_Clientes_Email_Ativo");

        builder.HasIndex(c => c.Documento)
            .IsUnique()
            .HasFilter("[Ativo] = 1")
            .HasDatabaseName("IX_Clientes_Documento_Ativo");
    }
}
