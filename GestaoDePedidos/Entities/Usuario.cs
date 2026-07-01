using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Entities;

public class Usuario : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool Ativo { get; set; } = true;
}
