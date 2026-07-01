namespace GestaoDePedidos.Entities;

public class Produto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public decimal EstoqueDisponivel { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public bool PermiteVendaFracionada { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<PedidoItem> PedidoItens { get; set; } = new List<PedidoItem>();
}
