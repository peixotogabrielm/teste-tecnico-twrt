namespace GestaoDePedidos.Common.Responses;

public class ApiFieldError
{
    public string Field { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = [];
}
