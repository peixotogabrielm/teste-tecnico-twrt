using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoDePedidos.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Descricao)
            .HasMaxLength(1000);

        builder.Property(p => p.Preco)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.EstoqueDisponivel)
            .IsRequired()
            .HasColumnType("decimal(18,3)");

        builder.Property(p => p.UnidadeMedida)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.PermiteVendaFracionada)
            .IsRequired();

        builder.Property(p => p.Ativo)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
