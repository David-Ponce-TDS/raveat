using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Etapa 12 - Pedidos de prueba.
    // Hasta v6 la base arrancaba con carta y clientes pero SIN un solo pedido, y las pantallas de
    // v7 —entregas, mapa, bandeja— no se pueden mirar con la lista vacia: habia que cargar seis
    // pedidos a mano en cada maquina antes de empezar. Estos son esos seis, con sus items y sus
    // entregas, copiando lo que hace PedidosController al crearlos.
    // La variedad es a proposito: un pedido de mostrador sin cliente, uno para retirar, dos de
    // entrega esperando repartidor (los que se usan para probar "tomar"), uno ya entregado y uno
    // cancelado. Si todos estuvieran en el mismo estado, la pantalla se disenaria para un solo caso.
    // Los codigos siguen el formato PED-000N que genera la API, asi el proximo pedido real sigue
    // la numeracion sin repetir.
    // Solo inserta datos, no toca el esquema.
    public partial class PedidosDePrueba : Migration
    {
        // Fecha fija: las migraciones deben producir siempre el mismo resultado en cada maquina.
        private static readonly DateTime FechaSemilla = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        // El unico usuario que existe al migrar es el administrador sembrado en Identidad: es el
        // creador de los seis y el repartidor del que ya se entrego.
        private const long AdminId = 1L;

        private static readonly string[] ColumnasPedido = {
            "id", "cliente_id", "usuario_creador_id", "codigo", "tipo", "estado",
            "subtotal", "descuento", "total", "estado_pago", "medio_pago", "propina_importe",
            "pagado_en", "observaciones", "confirmado_en", "cerrado_en", "creado_en", "actualizado_en"
        };

        private static readonly string[] ColumnasItem = {
            "id", "pedido_id", "producto_id", "producto_nombre", "precio_unitario", "cantidad",
            "subtotal", "observaciones", "creado_en", "actualizado_en"
        };

        private static readonly string[] ColumnasEntrega = {
            "id", "pedido_id", "repartidor_id", "direccion_linea", "direccion_referencia",
            "direccion_latitud", "direccion_longitud", "estado", "asignada_en", "entregada_en",
            "observaciones", "creado_en", "actualizado_en"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "pedidos",
                columns: ColumnasPedido,
                values: new object[,] {
                    {1L, null, AdminId, "PED-0001", "Presencial", "Cerrado", 10900m, 0m, 10900m, "Pagado", "Efectivo", 1000m, FechaSemilla, "Mesa 4.", FechaSemilla, FechaSemilla, FechaSemilla, FechaSemilla},
                    {2L, 5L, AdminId, "PED-0002", "Retiro", "Listo", 14000m, 0m, 14000m, "Pendiente", null, 0m, null, "Pasa a buscarlo a las 21.", FechaSemilla, null, FechaSemilla, FechaSemilla},
                    {3L, 1L, AdminId, "PED-0003", "Entregas", "Confirmado", 24000m, 2000m, 22000m, "Pendiente", null, 0m, null, null, FechaSemilla, null, FechaSemilla, FechaSemilla},
                    {4L, 2L, AdminId, "PED-0004", "Entregas", "EnPreparacion", 10300m, 0m, 10300m, "Pagado", "Transferencia", 0m, FechaSemilla, "Sin ajo.", FechaSemilla, null, FechaSemilla, FechaSemilla},
                    {5L, 3L, AdminId, "PED-0005", "Entregas", "Entregado", 8100m, 0m, 8100m, "Pagado", "Tarjeta", 500m, FechaSemilla, null, FechaSemilla, null, FechaSemilla, FechaSemilla},
                    {6L, 4L, AdminId, "PED-0006", "Entregas", "Cancelado", 7800m, 0m, 7800m, "Anulado", null, 0m, null, "El cliente canceló antes de salir.", FechaSemilla, FechaSemilla, FechaSemilla, FechaSemilla}
                });

            // El nombre y el precio se copian del producto, como hace el controller: un pedido
            // viejo tiene que seguir diciendo lo que costo entonces.
            migrationBuilder.InsertData(
                table: "pedidos_items",
                columns: ColumnasItem,
                values: new object[,] {
                    {1L, 1L, 4L, "Hamburguesa clasica", 6500m, 1, 6500m, null, FechaSemilla, FechaSemilla},
                    {2L, 1L, 10L, "Gaseosa", 2200m, 2, 4400m, null, FechaSemilla, FechaSemilla},
                    {3L, 2L, 7L, "Muzzarella", 7000m, 1, 7000m, null, FechaSemilla, FechaSemilla},
                    {4L, 2L, 14L, "Flan casero", 3500m, 2, 7000m, "Uno sin dulce de leche.", FechaSemilla, FechaSemilla},
                    {5L, 3L, 5L, "Doble cheddar", 8200m, 2, 16400m, null, FechaSemilla, FechaSemilla},
                    {6L, 3L, 12L, "Cerveza artesanal", 3800m, 2, 7600m, null, FechaSemilla, FechaSemilla},
                    {7L, 4L, 9L, "Especial", 8500m, 1, 8500m, "Sin aceitunas.", FechaSemilla, FechaSemilla},
                    {8L, 4L, 11L, "Agua mineral", 1800m, 1, 1800m, null, FechaSemilla, FechaSemilla},
                    {9L, 5L, 2L, "Empanadas de carne", 4200m, 1, 4200m, null, FechaSemilla, FechaSemilla},
                    {10L, 5L, 3L, "Provoleta", 3900m, 1, 3900m, null, FechaSemilla, FechaSemilla},
                    {11L, 6L, 8L, "Napolitana", 7800m, 1, 7800m, null, FechaSemilla, FechaSemilla}
                });

            // La entrega copia la direccion y las coordenadas del cliente, igual que el item copia
            // el precio. Las dos Pendientes son las que se toman desde la pantalla de reparto.
            migrationBuilder.InsertData(
                table: "entregas",
                columns: ColumnasEntrega,
                values: new object[,] {
                    {1L, 3L, null, "Av. Corrientes 1368, CABA", "Timbre 3B", -34.6039600m, -58.3860300m, "Pendiente", null, null, null, FechaSemilla, FechaSemilla},
                    {2L, 4L, null, "Av. de Mayo 825, CABA", "Edificio esquina, portón negro", -34.6087400m, -58.3789200m, "Pendiente", null, null, null, FechaSemilla, FechaSemilla},
                    {3L, 5L, AdminId, "Defensa 1179, San Telmo, CABA", "Local a la calle", -34.6207700m, -58.3714500m, "Entregada", FechaSemilla, FechaSemilla, null, FechaSemilla, FechaSemilla},
                    {4L, 6L, null, "Av. Santa Fe 3253, Palermo, CABA", null, -34.5885900m, -58.4110800m, "Cancelada", null, null, "El cliente canceló antes de que saliera.", FechaSemilla, FechaSemilla}
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Al reves que el Up: primero lo que apunta al pedido, despues el pedido.
            migrationBuilder.DeleteData(table: "entregas", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L});
            migrationBuilder.DeleteData(table: "pedidos_items", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L, 11L});
            migrationBuilder.DeleteData(table: "pedidos", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L, 5L, 6L});
        }
    }
}
