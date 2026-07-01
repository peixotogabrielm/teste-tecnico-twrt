using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Security;
using GestaoDePedidos.Data;
using GestaoDePedidos.Repository;
using GestaoDePedidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.DependencyInjection;

public class RegisterServices
{
    public RegisterServices(IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton(sp => JwtSettings.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProdutoService, ProdutoService>();

        // Repositories e Services específicos entram aqui conforme as entidades forem criadas, exemplo:
        // services.AddScoped<IPedidoRepository, PedidoRepository>();
        // services.AddScoped<IPedidoService, PedidoService>();
    }
}
