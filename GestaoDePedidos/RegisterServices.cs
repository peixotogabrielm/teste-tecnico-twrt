using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Data;
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

        // Repositories e Services específicos entram aqui conforme as entidades forem criadas, exemplo:
        // services.AddScoped<IPedidoRepository, PedidoRepository>();
        // services.AddScoped<IPedidoService, PedidoService>();
    }
}
