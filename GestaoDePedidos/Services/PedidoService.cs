using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Validation;
using GestaoDePedidos.Data;
using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = GestaoDePedidos.Common.Exceptions.ValidationException;

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
            throw new ValidationException("Cliente inativo não pode criar pedidos.");
        }

        if (request.Itens.Count == 0)
        {
            throw new ValidationException("O pedido deve possuir pelo menos um item.");
        }

        var produtoIds = request.Itens.Select(i => i.ProdutoId).ToList();
        if (produtoIds.Distinct().Count() != produtoIds.Count)
        {
            throw new ValidationException("Produto duplicado no pedido: cada produto deve aparecer no máximo uma vez.");
        }

        foreach (var itemRequest in request.Itens)
        {
            if (itemRequest.Quantidade <= 0)
            {
                throw new ValidationException("A quantidade deve ser maior que zero.");
            }
        }

        var produtos = await _context.Produtos
            .AsNoTracking()
            .Where(p => produtoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Status = PedidoStatus.Criado
        };

        foreach (var itemRequest in request.Itens)
        {
            if (!produtos.TryGetValue(itemRequest.ProdutoId, out var produto))
            {
                throw new NotFoundException("Produto não encontrado.");
            }

            if (!produto.Ativo)
            {
                throw new ValidationException("Produto inativo não pode ser vendido.");
            }

            if (!QuantidadeValidator.IsValid(itemRequest.Quantidade, produto.PermiteVendaFracionada))
            {
                throw new ValidationException(produto.PermiteVendaFracionada
                    ? "Produto fracionado aceita no máximo 3 casas decimais."
                    : "Este produto não permite venda fracionada.");
            }

            if (produto.EstoqueDisponivel < itemRequest.Quantidade)
            {
                throw new ValidationException("Estoque insuficiente para o produto informado.");
            }

            var valorTotalItem = Math.Round(itemRequest.Quantidade * produto.Preco, 2, MidpointRounding.AwayFromZero);

            pedido.Itens.Add(new PedidoItem
            {
                ProdutoId = produto.Id,
                Quantidade = itemRequest.Quantidade,
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

    public async Task<PedidoResponse> ObterPorIdAsync(Guid id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Itens)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Pedido não encontrado.");

        return pedido.ToResponse();
    }
}
