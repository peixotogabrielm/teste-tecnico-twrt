namespace GestaoDePedidos.Common.Security;

public class JwtSettings
{
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int ExpiresInMinutes { get; init; }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Variável de ambiente Jwt:SecretKey não configurada.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Variável de ambiente Jwt:Issuer não configurada.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Variável de ambiente Jwt:Audience não configurada.");
        var expiresInMinutesRaw = configuration["Jwt:ExpirationMinutes"]
            ?? throw new InvalidOperationException("Variável de ambiente Jwt:ExpirationMinutes não configurada.");

        if (!int.TryParse(expiresInMinutesRaw, out var expiresInMinutes))
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes deve ser um número inteiro.");
        }

        return new JwtSettings
        {
            SecretKey = secretKey,
            Issuer = issuer,
            Audience = audience,
            ExpiresInMinutes = expiresInMinutes
        };
    }
}
