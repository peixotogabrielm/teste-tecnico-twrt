using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Produtos;

public class UpdateProdutoStatusRequest
{
    [Required(ErrorMessage = "O campo Ativo é obrigatório.")]
    public bool Ativo { get; set; }
}
