namespace GestaoDePedidos.Entities;

public class Cliente : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
