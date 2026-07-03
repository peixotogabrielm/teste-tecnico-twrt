namespace GestaoDePedidos.Common.Validation;

public static class EmailNormalizer
{
    public static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
