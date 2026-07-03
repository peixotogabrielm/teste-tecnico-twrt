using System.Text.Json.Serialization;

namespace GestaoDePedidos.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoMovimentacaoEstoque
{
    Entrada,
    Saida
}
