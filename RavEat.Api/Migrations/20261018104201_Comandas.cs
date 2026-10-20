using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    /// <inheritdoc />
    public partial class Comandas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El valor por defecto es "Pendiente" y no la cadena vacia que genera EF: los items que
            // ya estaban en la base tienen que quedar en un estado valido del enum. Con "" la
            // comanda no sabria que hacer con ellos y la pantalla mostraria un estado inexistente.
            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "pedidos_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pendiente")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Un pedido que ya estaba listo, entregado o cerrado no puede quedar con sus items sin
            // empezar: la comanda diria "0 de 2" de algo que ya salio. Sus items pasan a Listo.
            migrationBuilder.Sql("UPDATE `pedidos_items` i JOIN `pedidos` p ON p.`id` = i.`pedido_id` SET i.`estado` = 'Listo' WHERE p.`estado` IN ('Listo', 'Entregado', 'Cerrado');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "estado",
                table: "pedidos_items");
        }
    }
}
