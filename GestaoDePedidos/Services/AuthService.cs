using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Security;
using GestaoDePedidos.Dtos.Auth;
using GestaoDePedidos.Repository;

namespace GestaoDePedidos.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUsuarioRepository usuarioRepository, ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var usuario = await _usuarioRepository.GetByEmailAsync(request.Login);

        if (usuario is null || !usuario.Ativo || !PasswordHasher.Verify(request.Senha, usuario.PasswordHash))
        {
            throw new UnauthorizedException("Login ou senha inválidos.");
        }

        var (accessToken, expiresAt) = _tokenService.GerarToken(usuario);

        return new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            UserId = usuario.Id,
            Role = usuario.Role.ToString()
        };
    }
}
