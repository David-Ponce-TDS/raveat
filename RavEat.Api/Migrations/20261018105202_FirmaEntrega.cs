using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    /// <inheritdoc />
    public partial class FirmaEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "firma_url",
                table: "entregas",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "firma_url",
                table: "entregas");
        }
    }
}
