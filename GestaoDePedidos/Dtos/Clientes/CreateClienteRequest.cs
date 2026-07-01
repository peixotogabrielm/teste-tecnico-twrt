using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Clientes;

public class CreateClienteRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O documento é obrigatório.")]
    [StringLength(20)]
    public string Documento { get; set; } = string.Empty;
}
