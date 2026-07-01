using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Common.Validation;

public static class PedidoStatusTransicaoValidator
{
    private static readonly Dictionary<PedidoStatus, PedidoStatus[]> TransicoesPermitidas = new()
    {
        [PedidoStatus.Criado] = new[] { PedidoStatus.Pago, PedidoStatus.Cancelado },
        [PedidoStatus.Pago] = new[] { PedidoStatus.Enviado, PedidoStatus.Cancelado },
        [PedidoStatus.Enviado] = Array.Empty<PedidoStatus>(),
        [PedidoStatus.Cancelado] = Array.Empty<PedidoStatus>()
    };

    public static bool IsValid(PedidoStatus statusAtual, PedidoStatus novoStatus)
    {
        return TransicoesPermitidas.TryGetValue(statusAtual, out var permitidos) && permitidos.Contains(novoStatus);
    }
}
