using System.Text.Json.Serialization;

namespace GestaoDePedidos.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PedidoStatus
{
    Criado,
    Pago,
    Enviado,
    Cancelado
}
