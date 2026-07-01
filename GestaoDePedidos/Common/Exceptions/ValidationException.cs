namespace GestaoDePedidos.Common.Exceptions;

/// <summary>
/// Representa violação de uma regra de negócio validada na camada de Service.
/// Não deve ser usada para erros de formato de DTO (isso é responsabilidade do Data Annotations/ModelState).
/// </summary>
public class ValidationException : AppException
{
    public ValidationException(string message) : base(message)
    {
    }
}
