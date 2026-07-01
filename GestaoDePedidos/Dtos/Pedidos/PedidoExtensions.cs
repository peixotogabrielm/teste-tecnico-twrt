using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Dtos.Pedidos;

public static class PedidoExtensions
{
    private static readonly TimeZoneInfo SaoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static PedidoResponse ToResponse(this Pedido pedido) => new()
    {
        Id = pedido.Id,
        ClienteId = pedido.ClienteId,
        Status = pedido.Status,
        DataCriacao = ParaHorarioLocal(pedido.DataCriacao),
        ValorTotal = pedido.ValorTotal,
        Itens = pedido.Itens.Select(i => i.ToResponse()).ToList(),
        HistoricoStatus = pedido.HistoricoStatus
            .OrderBy(h => h.DataAlteracao)
            .Select(h => h.ToResponse())
            .ToList()
    };

    public static PedidoItemResponse ToResponse(this PedidoItem item) => new()
    {
        Id = item.Id,
        ProdutoId = item.ProdutoId,
        Quantidade = item.Quantidade,
        PrecoUnitario = item.PrecoUnitario,
        ValorTotal = item.ValorTotal
    };

    public static PedidoStatusHistoricoResponse ToResponse(this PedidoStatusHistorico historico) => new()
    {
        StatusAnterior = historico.StatusAnterior,
        NovoStatus = historico.NovoStatus,
        DataAlteracao = ParaHorarioLocal(historico.DataAlteracao),
        Motivo = historico.Motivo
    };

    private static DateTimeOffset ParaHorarioLocal(DateTime dataUtc)
    {
        var utc = new DateTimeOffset(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc));
        return TimeZoneInfo.ConvertTime(utc, SaoPauloTimeZone);
    }
}
