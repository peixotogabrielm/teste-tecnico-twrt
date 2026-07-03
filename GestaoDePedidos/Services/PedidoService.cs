using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Common.Validation;
using GestaoDePedidos.Data;
using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Services;

public class PedidoService : IPedidoService
{
    private readonly ApplicationDbContext _context;

    public PedidoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PedidoResponse> CriarAsync(CriarPedidoRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClienteId)
            ?? throw new NotFoundException("Cliente não encontrado.");

        if (!cliente.Ativo)
        {
            throw new BadRequestException("Cliente inativo não pode criar pedidos.");
        }

        if (request.Itens.Count == 0)
        {
            throw new BadRequestException("O pedido deve possuir pelo menos um item.");
        }

        foreach (var itemRequest in request.Itens)
        {
            if (itemRequest.Quantidade <= 0)
            {
                throw new BadRequestException("A quantidade deve ser maior que zero.");
            }
        }

        var itensMesclados = request.Itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(i => i.Quantidade) })
            .ToList();

        var produtoIds = itensMesclados.Select(i => i.ProdutoId).ToList();

        var produtos = await _context.Produtos
            .AsNoTracking()
            .Where(p => produtoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Status = PedidoStatus.Criado
        };

        foreach (var itemMesclado in itensMesclados)
        {
            if (!produtos.TryGetValue(itemMesclado.ProdutoId, out var produto))
            {
                throw new NotFoundException("Produto não encontrado.");
            }

            if (!produto.Ativo)
            {
                throw new BadRequestException("Produto inativo não pode ser vendido.");
            }

            if (!QuantidadeValidator.IsValid(itemMesclado.Quantidade, produto.PermiteVendaFracionada))
            {
                throw new BadRequestException(produto.PermiteVendaFracionada
                    ? "Produto fracionado aceita no máximo 3 casas decimais."
                    : "Este produto não permite venda fracionada.");
            }

            if (produto.EstoqueDisponivel < itemMesclado.Quantidade)
            {
                throw new BadRequestException("Estoque insuficiente para o produto informado.");
            }

            var valorTotalItem = Math.Round(itemMesclado.Quantidade * produto.Preco, 2, MidpointRounding.AwayFromZero);

            pedido.Itens.Add(new PedidoItem
            {
                ProdutoId = produto.Id,
                Quantidade = itemMesclado.Quantidade,
                PrecoUnitario = produto.Preco,
                ValorTotal = valorTotalItem
            });
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.ValorTotal);
        pedido.HistoricoStatus.Add(new PedidoStatusHistorico
        {
            PedidoId = pedido.Id,
            StatusAnterior = null,
            NovoStatus = PedidoStatus.Criado
        });

        _context.Pedidos.Add(pedido);

        foreach (var item in pedido.Itens)
        {
            var linhasAfetadas = await _context.Produtos
                .Where(p => p.Id == item.ProdutoId && p.EstoqueDisponivel >= item.Quantidade)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.EstoqueDisponivel, p => p.EstoqueDisponivel - item.Quantidade));

            if (linhasAfetadas == 0)
            {
                throw new ConflictException("Não foi possível concluir o pedido devido a conflito de estoque. Tente novamente.");
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return pedido.ToResponse();
    }

    public async Task<PagedResult<PedidoResponse>> ObterPaginadoAsync(PagedRequest request, Guid? clienteId)
    {
        var query = _context.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.HistoricoStatus.OrderBy(h => h.DataAlteracao))
            .AsNoTracking()
            .Where(p => clienteId == null || p.ClienteId == clienteId)
            .OrderByDescending(p => p.DataCriacao);

        var totalCount = await query.CountAsync();

        var pedidos = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var items = pedidos.Select(p => p.ToResponse()).ToList();

        return new PagedResult<PedidoResponse>(items, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task<PedidoResponse> ObterPorIdAsync(Guid id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.HistoricoStatus.OrderBy(h => h.DataAlteracao))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Pedido não encontrado.");

        return pedido.ToResponse();
    }

    public async Task AtualizarStatusAsync(Guid id, AtualizarPedidoStatusRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var pedido = await _context.Pedidos
            .Include(p => p.Itens)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Pedido não encontrado.");

        var statusAtual = pedido.Status;

        if (!PedidoStatusTransicaoValidator.IsValid(statusAtual, request.NovoStatus))
        {
            throw new BadRequestException($"Transição de status inválida de {statusAtual} para {request.NovoStatus}.");
        }

        var linhasAfetadas = await _context.Pedidos
            .Where(p => p.Id == id && p.Status == statusAtual)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, request.NovoStatus));

        if (linhasAfetadas == 0)
        {
            throw new ConflictException("O status do pedido foi alterado por outra requisição. Tente novamente.");
        }

        if (request.NovoStatus == PedidoStatus.Cancelado)
        {
            foreach (var item in pedido.Itens)
            {
                await _context.Produtos
                    .Where(p => p.Id == item.ProdutoId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.EstoqueDisponivel, p => p.EstoqueDisponivel + item.Quantidade));
            }
        }

        _context.PedidoStatusHistoricos.Add(new PedidoStatusHistorico
        {
            PedidoId = id,
            StatusAnterior = statusAtual,
            NovoStatus = request.NovoStatus,
            Motivo = request.Motivo
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
