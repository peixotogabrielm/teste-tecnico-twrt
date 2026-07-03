namespace GestaoDePedidos.Common.Responses;

public class ApiErrorResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public List<ApiFieldError>? Errors { get; set; }
}
