using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    /// <inheritdoc />
    public partial class TokenPublicoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "token_publico",
                table: "pedidos",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Los pedidos que ya existian nacen con la cadena vacia, y el indice unico de abajo los
            // rechazaria a todos menos a uno. Este UPDATE es la mitad que EF no escribe sola: sabe
            // agregar la columna, pero no que valor le toca a cada fila vieja. RANDOM_BYTES es de
            // MySQL: rellenar desde C# obligaria a leer y guardar la tabla entera antes del indice.
            migrationBuilder.Sql("UPDATE pedidos SET token_publico = LOWER(HEX(RANDOM_BYTES(16))) WHERE token_publico = '';");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_token_publico",
                table: "pedidos",
                column: "token_publico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pedidos_token_publico",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "token_publico",
                table: "pedidos");
        }
    }
}
