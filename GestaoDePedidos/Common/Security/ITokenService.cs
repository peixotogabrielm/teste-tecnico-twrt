using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Common.Security;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GerarToken(Usuario usuario);
}
