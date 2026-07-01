using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoDePedidos.Data.Configurations;

public class PedidoStatusHistoricoConfiguration : IEntityTypeConfiguration<PedidoStatusHistorico>
{
    public void Configure(EntityTypeBuilder<PedidoStatusHistorico> builder)
    {
        builder.ToTable("PedidoStatusHistoricos");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.StatusAnterior)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.NovoStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.DataAlteracao)
            .IsRequired();

        builder.Property(h => h.Motivo)
            .HasMaxLength(500);
    }
}
