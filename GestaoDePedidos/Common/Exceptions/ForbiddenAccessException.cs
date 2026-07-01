namespace GestaoDePedidos.Common.Exceptions;

public class ForbiddenAccessException : AppException
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
