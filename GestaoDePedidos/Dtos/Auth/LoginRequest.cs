using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}
