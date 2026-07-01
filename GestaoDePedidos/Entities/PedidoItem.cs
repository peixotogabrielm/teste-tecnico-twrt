namespace GestaoDePedidos.Entities;

public class PedidoItem : BaseEntity
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public Guid ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}
