using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoDePedidos.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexPedidoItemProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidoItens_PedidoId",
                table: "PedidoItens");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_PedidoId_ProdutoId",
                table: "PedidoItens",
                columns: new[] { "PedidoId", "ProdutoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidoItens_PedidoId_ProdutoId",
                table: "PedidoItens");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_PedidoId",
                table: "PedidoItens",
                column: "PedidoId");
        }
    }
}
