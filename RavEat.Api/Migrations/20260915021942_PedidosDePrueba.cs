using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Pedidos de prueba: ocho pedidos con sus items, uno por cada estado de la maquina y con los
    // tres estados de pago, para que la lista, los filtros, el comprobante y el QR tengan algo que
    // mostrar desde el primer arranque. Cada item copia el nombre y el precio del producto tal
    // como estaban al crearse: es la regla del endpoint, tambien en la semilla.
    // Escrita a mano sobre una migracion generada vacia: solo inserta datos, no toca el esquema.
    public partial class PedidosDePrueba : Migration
    {
        // Fecha fija: las migraciones deben producir siempre el mismo resultado en cada maquina.
        private static readonly DateTime FechaSemilla = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

        // Todos los pedidos los creo el administrador sembrado en Identidad.
        private const long AdminId = 1L;

        private static readonly string[] ColumnasPedido = {
            "id", "cliente_id", "usuario_creador_id", "codigo", "tipo", "estado",
            "subtotal", "descuento", "total", "estado_pago", "medio_pago", "propina_importe", "pagado_en",
            "observaciones", "confirmado_en", "cerrado_en", "creado_en", "actualizado_en"
        };

        private static readonly string[] ColumnasItem = {
            "id", "pedido_id", "producto_id", "producto_nombre", "precio_unitario", "cantidad",
            "subtotal", "observaciones", "creado_en", "actualizado_en"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los enums van con el nombre en C# ("EnPreparacion"): asi los guarda la conversion a
            // texto. En el JSON salen en snake_case, pero eso es cosa de Program.cs.
            // creado_en escalonado: el listado ordena por fecha descendente y el orden se nota.
            migrationBuilder.InsertData(
                table: "pedidos",
                columns: ColumnasPedido,
                values: new object[,] {
                    {1L, 1L,   AdminId, "PED-0001", "Presencial", "Borrador",      9200m,  0m,    9200m,  "Pendiente", null,            0m,   null,                          "Mesa 4.",                       null,                        null,                        FechaSemilla.AddHours(1),  FechaSemilla.AddHours(1)},
                    {2L, 2L,   AdminId, "PED-0002", "Retiro",     "Confirmado",    14600m, 0m,    14600m, "Pendiente", null,            0m,   null,                          null,                            FechaSemilla.AddHours(2),    null,                        FechaSemilla.AddHours(2),  FechaSemilla.AddHours(2)},
                    {3L, null, AdminId, "PED-0003", "Presencial", "EnPreparacion", 10400m, 0m,    10400m, "Pendiente", null,            0m,   null,                          "Venta de mostrador.",           FechaSemilla.AddHours(3),    null,                        FechaSemilla.AddHours(3),  FechaSemilla.AddHours(3)},
                    {4L, 3L,   AdminId, "PED-0004", "Entregas",   "Listo",         15600m, 0m,    15600m, "Pagado",    "Transferencia", 0m,   FechaSemilla.AddHours(4),      "Sin ajo en una.",               FechaSemilla.AddHours(4),    null,                        FechaSemilla.AddHours(4),  FechaSemilla.AddHours(4)},
                    {5L, 4L,   AdminId, "PED-0005", "Retiro",     "Entregado",     12000m, 1000m, 11000m, "Pagado",    "Efectivo",      500m, FechaSemilla.AddHours(5),      null,                            FechaSemilla.AddHours(5),    null,                        FechaSemilla.AddHours(5),  FechaSemilla.AddHours(5)},
                    {6L, 5L,   AdminId, "PED-0006", "Entregas",   "Cerrado",       14400m, 0m,    14400m, "Pagado",    "Tarjeta",       0m,   FechaSemilla.AddHours(6),      null,                            FechaSemilla.AddHours(6),    FechaSemilla.AddHours(8),    FechaSemilla.AddHours(6),  FechaSemilla.AddHours(8)},
                    {7L, 1L,   AdminId, "PED-0007", "Presencial", "Cancelado",     3900m,  0m,    3900m,  "Anulado",   null,            0m,   null,                          "Se fue sin esperar.",           null,                        null,                        FechaSemilla.AddHours(7),  FechaSemilla.AddHours(7)},
                    {8L, 2L,   AdminId, "PED-0008", "Retiro",     "Confirmado",    10400m, 0m,    10400m, "Pendiente", null,            0m,   null,                          null,                            FechaSemilla.AddHours(9),    null,                        FechaSemilla.AddHours(9),  FechaSemilla.AddHours(9)}
                });

            migrationBuilder.InsertData(
                table: "pedidos_items",
                columns: ColumnasItem,
                values: new object[,] {
                    { 1L, 1L, 1L,  "Papas fritas",        3500m, 2, 7000m,  null,                FechaSemilla.AddHours(1), FechaSemilla.AddHours(1)},
                    { 2L, 1L, 10L, "Gaseosa",             2200m, 1, 2200m,  null,                FechaSemilla.AddHours(1), FechaSemilla.AddHours(1)},
                    { 3L, 2L, 7L,  "Muzzarella",          7000m, 1, 7000m,  null,                FechaSemilla.AddHours(2), FechaSemilla.AddHours(2)},
                    { 4L, 2L, 12L, "Cerveza artesanal",   3800m, 2, 7600m,  null,                FechaSemilla.AddHours(2), FechaSemilla.AddHours(2)},
                    { 5L, 3L, 5L,  "Doble cheddar",       8200m, 1, 8200m,  "Sin panceta.",      FechaSemilla.AddHours(3), FechaSemilla.AddHours(3)},
                    { 6L, 3L, 10L, "Gaseosa",             2200m, 1, 2200m,  null,                FechaSemilla.AddHours(3), FechaSemilla.AddHours(3)},
                    { 7L, 4L, 8L,  "Napolitana",          7800m, 2, 15600m, null,                FechaSemilla.AddHours(4), FechaSemilla.AddHours(4)},
                    { 8L, 5L, 9L,  "Especial",            8500m, 1, 8500m,  null,                FechaSemilla.AddHours(5), FechaSemilla.AddHours(5)},
                    { 9L, 5L, 14L, "Flan casero",         3500m, 1, 3500m,  null,                FechaSemilla.AddHours(5), FechaSemilla.AddHours(5)},
                    {10L, 6L, 2L,  "Empanadas de carne",  4200m, 3, 12600m, null,                FechaSemilla.AddHours(6), FechaSemilla.AddHours(6)},
                    {11L, 6L, 11L, "Agua mineral",        1800m, 1, 1800m,  null,                FechaSemilla.AddHours(6), FechaSemilla.AddHours(6)},
                    {12L, 7L, 3L,  "Provoleta",           3900m, 1, 3900m,  null,                FechaSemilla.AddHours(7), FechaSemilla.AddHours(7)},
                    {13L, 8L, 13L, "Brownie con helado",  4200m, 1, 4200m,  null,                FechaSemilla.AddHours(9), FechaSemilla.AddHours(9)},
                    {14L, 8L, 6L,  "Veggie",              6200m, 1, 6200m,  null,                FechaSemilla.AddHours(9), FechaSemilla.AddHours(9)}
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Primero los items y despues los pedidos: el orden inverso a la clave foranea.
            for(long id = 1; id <= 14; id++) migrationBuilder.DeleteData(table: "pedidos_items", keyColumn: "id", keyValue: id);
            for(long id = 1; id <= 8; id++) migrationBuilder.DeleteData(table: "pedidos", keyColumn: "id", keyValue: id);
        }
    }
}
