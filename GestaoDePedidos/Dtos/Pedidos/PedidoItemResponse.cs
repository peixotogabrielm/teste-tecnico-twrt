namespace GestaoDePedidos.Dtos.Pedidos;

public class PedidoItemResponse
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}
