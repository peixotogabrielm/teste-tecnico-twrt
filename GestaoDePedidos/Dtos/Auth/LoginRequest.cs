using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "O login é obrigatório.")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}
