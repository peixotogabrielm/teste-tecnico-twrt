using GestaoDePedidos.Dtos.Auth;

namespace GestaoDePedidos.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
