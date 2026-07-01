using GestaoDePedidos.Common.Security;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["AdminCredentials:Email"]
            ?? throw new InvalidOperationException("Variável de ambiente AdminCredentials:Email não configurada.");
        var adminPassword = configuration["AdminCredentials:Password"]
            ?? throw new InvalidOperationException("Variável de ambiente AdminCredentials:Password não configurada.");

        var jaExiste = await context.Usuarios.AnyAsync(u => u.Email == adminEmail);
        if (jaExiste)
        {
            return;
        }

        var admin = new Usuario
        {
            Email = adminEmail,
            PasswordHash = PasswordHasher.Hash(adminPassword),
            Role = Role.Admin,
            Ativo = true
        };

        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();
    }
}
