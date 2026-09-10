using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Etapa 5 — Identidad.
    // Quien usa el sistema: roles, usuarios y refresh tokens. Y el pedido pasa a saber quien lo
    // creo (`usuario_creador_id`).
    //
    // Esta migracion **siembra los roles y el administrador inicial**, cosa que normalmente iria en
    // una migracion de datos aparte. Aca no puede: `pedidos.usuario_creador_id` es NOT NULL con una
    // clave foranea a `usuarios`, asi que los pedidos que ya existen necesitan apuntar a un usuario
    // real. El orden es: crear las tablas, sembrar el admin, y recien entonces agregar la columna
    // con ese admin como valor por defecto. Al reves, la clave foranea falla sobre cualquier base
    // que tenga pedidos.
    //
    // Sin este admin sembrado, ademas, una base recien migrada no tendria a nadie para entrar.
    public partial class Identidad : Migration
    {
        // Fecha fija: las migraciones deben producir siempre el mismo resultado en cada maquina.
        private static readonly DateTime FechaSemilla = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        private const long AdminId = 1L;

        // Hash de la contrasena publica de taller "RavEat123!", generado con el mismo
        // PasswordHasher<Usuario> que usa la API (Identity V3, con el salt incluido en el hash).
        // Es publica a proposito y esta documentada en el README: sirve solo para clase.
        private const string HashClaveTaller = "AQAAAAIAAYagAAAAEKmhIUHBbbjr/OUBkmjzGiK2+MSV0uQeWvFedcLz/kD98Sl1cUPSkgK7WIR0LEVO5Q==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    codigo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    rol_id = table.Column<long>(type: "bigint", nullable: true),
                    nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expira_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    revocado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_roles_codigo",
                table: "roles",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_rol_id",
                table: "usuarios",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_usuario_id_expira_en",
                table: "refresh_tokens",
                columns: new[] { "usuario_id", "expira_en" });

            // Los cinco roles del taller. Son datos y no un enum de C# porque un administrador
            // tiene que poder asignarlos desde la app sin recompilar.
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "nombre", "codigo", "activo", "creado_en", "actualizado_en" },
                values: new object[,] {
                    {1L, "Administrador", "ADMIN", true, FechaSemilla, FechaSemilla},
                    {2L, "Vendedor", "VENDEDOR", true, FechaSemilla, FechaSemilla},
                    {3L, "Proceso", "PROCESO", true, FechaSemilla, FechaSemilla},
                    {4L, "Caja", "CAJA", true, FechaSemilla, FechaSemilla},
                    {5L, "Delivery", "DELIVERY", true, FechaSemilla, FechaSemilla}
                });

            // El unico usuario que existe al migrar. Desde la app, este admin da de alta al resto:
            // el alta de usuarios es exclusiva de ADMIN.
            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "rol_id", "nombre", "email", "password_hash", "activo", "creado_en", "actualizado_en" },
                values: new object[] { AdminId, 1L, "Administrador", "admin@raveat.local", HashClaveTaller, true, FechaSemilla, FechaSemilla });

            // Recien ahora la columna: ya hay un usuario real al que apuntar. Los pedidos que
            // existian de v4 quedan atribuidos al administrador.
            migrationBuilder.AddColumn<long>(
                name: "usuario_creador_id",
                table: "pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: AdminId);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_usuario_creador_id",
                table: "pedidos",
                column: "usuario_creador_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_usuarios_usuario_creador_id",
                table: "pedidos",
                column: "usuario_creador_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_usuarios_usuario_creador_id",
                table: "pedidos");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "ix_pedidos_usuario_creador_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "usuario_creador_id",
                table: "pedidos");
        }
    }
}
