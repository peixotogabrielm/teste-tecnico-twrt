using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoDePedidos.Data.Configurations;

public class PedidoItemConfiguration : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> builder)
    {
        builder.ToTable("PedidoItens");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantidade)
            .IsRequired()
            .HasColumnType("decimal(18,3)");

        builder.Property(i => i.PrecoUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.ValorTotal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Produto)
            .WithMany(p => p.PedidoItens)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
