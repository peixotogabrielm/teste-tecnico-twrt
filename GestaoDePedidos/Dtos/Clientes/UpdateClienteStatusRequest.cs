using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Clientes;

public class UpdateClienteStatusRequest
{
    [Required(ErrorMessage = "O campo Ativo é obrigatório.")]
    public bool Ativo { get; set; }
}
