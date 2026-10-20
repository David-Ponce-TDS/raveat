using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    /// <inheritdoc />
    public partial class Historial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historiales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    pedido_id = table.Column<long>(type: "bigint", nullable: true),
                    entrega_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    estado_anterior = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado_nuevo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    observacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    creado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historiales", x => x.id);
                    table.ForeignKey(
                        name: "fk_historiales_entregas_entrega_id",
                        column: x => x.entrega_id,
                        principalTable: "entregas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historiales_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    // Restrict contra el usuario: dar de baja a alguien no puede borrar lo que hizo.
                    table.ForeignKey(
                        name: "fk_historiales_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_historiales_entrega_id",
                table: "historiales",
                column: "entrega_id");

            migrationBuilder.CreateIndex(
                name: "ix_historiales_pedido_id",
                table: "historiales",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_historiales_usuario_id_creado_en",
                table: "historiales",
                columns: new[] { "usuario_id", "creado_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historiales");
        }
    }
}
