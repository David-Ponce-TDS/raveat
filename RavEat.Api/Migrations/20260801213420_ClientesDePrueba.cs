using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Etapa 4 — Clientes de prueba.
    // Los mismos cinco clientes que en v2 y v3 vivian en src/datos/clientes.js, ahora en la base.
    // Las direcciones son reales de CABA y con coordenadas verdaderas, porque a partir de v7 se
    // dibujan en un mapa: con datos inventados los marcadores caen en el oceano.
    // Dos de los cinco tienen campos vacios a proposito (sin email, sin direccion): si todos los
    // datos de prueba estuvieran completos, la vista se disenaria para el caso ideal.
    // Solo inserta datos, no toca el esquema.
    public partial class ClientesDePrueba : Migration
    {
        // Fecha fija: las migraciones deben producir siempre el mismo resultado en cada maquina.
        private static readonly DateTime FechaSemilla = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] ColumnasCliente = {
            "id", "nombre", "telefono", "email",
            "direccion_linea", "direccion_referencia", "direccion_latitud", "direccion_longitud",
            "activo", "creado_en", "actualizado_en"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "clientes",
                columns: ColumnasCliente,
                values: new object[,] {
                    {1L, "María López", "11-5555-1001", "maria.lopez@example.com", "Av. Corrientes 1368, CABA", "Timbre 3B", -34.6039600m, -58.3860300m, true, FechaSemilla, FechaSemilla},
                    {2L, "Julián Pérez", "11-5555-1002", "julian.perez@example.com", "Av. de Mayo 825, CABA", "Edificio esquina, portón negro", -34.6087400m, -58.3789200m, true, FechaSemilla, FechaSemilla},
                    {3L, "Carla Fernández", "11-5555-1003", "carla.fernandez@example.com", "Defensa 1179, San Telmo, CABA", "Local a la calle", -34.6207700m, -58.3714500m, true, FechaSemilla, FechaSemilla},
                    {4L, "Roberto Díaz", "11-5555-1004", null, "Av. Santa Fe 3253, Palermo, CABA", null, -34.5885900m, -58.4110800m, true, FechaSemilla, FechaSemilla},
                    {5L, "Lucía Giménez", "11-5555-1005", "lucia.gimenez@example.com", null, null, null, null, true, FechaSemilla, FechaSemilla}
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "clientes", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L, 5L});
        }
    }
}
