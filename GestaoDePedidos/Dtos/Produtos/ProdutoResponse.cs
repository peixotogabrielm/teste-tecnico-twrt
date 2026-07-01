namespace GestaoDePedidos.Dtos.Produtos;

public class ProdutoResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public decimal EstoqueDisponivel { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public bool PermiteVendaFracionada { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
