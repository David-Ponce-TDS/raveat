using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Etapa 2 — Datos de prueba: cinco categorias y catorce productos, uno por cada imagen de
    // wwwroot/seed/productos. Va por migracion y no por un endpoint de siembra porque el lanzador
    // borra y reconstruye la base en cada arranque: lo que no este aca no existe al levantar.
    // Es la unica migracion escrita a mano: solo inserta datos, no toca el esquema.
    public partial class DatosDePrueba : Migration
    {
        // Fecha fija: las migraciones deben producir siempre el mismo resultado en cada maquina.
        private static readonly DateTime FechaSemilla = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] ColumnasCategoria = {
            "id", "nombre", "orden", "activo", "creado_en", "actualizado_en"
        };

        private static readonly string[] ColumnasProducto = {
            "id", "categoria_id", "nombre", "descripcion", "precio", "imagen_url",
            "disponible", "activo", "creado_en", "actualizado_en"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categorias",
                columns: ColumnasCategoria,
                values: new object[,] {
                    {1L, "Entradas", 1, true, FechaSemilla, FechaSemilla},
                    {2L, "Hamburguesas", 2, true, FechaSemilla, FechaSemilla},
                    {3L, "Pizzas", 3, true, FechaSemilla, FechaSemilla},
                    {4L, "Bebidas", 4, true, FechaSemilla, FechaSemilla},
                    {5L, "Postres", 5, true, FechaSemilla, FechaSemilla}
                });

            // imagen_url guarda una ruta relativa y nunca el host: guardarlo invalidaria todas las
            // imagenes al mover la API de maquina. El frontend la completa en utils/imagenes.js.
            migrationBuilder.InsertData(
                table: "productos",
                columns: ColumnasProducto,
                values: new object[,] {
                    {1L, 1L, "Papas fritas", "Porcion de papas crocantes con cheddar.", 3500m, "/seed/productos/papas_fritas.jpg", true, true, FechaSemilla, FechaSemilla},
                    {2L, 1L, "Empanadas de carne", "Seis empanadas caseras al horno.", 4200m, "/seed/productos/empanadas.jpg", true, true, FechaSemilla, FechaSemilla},
                    {3L, 1L, "Provoleta", "Provolone grillado con oregano y aceite de oliva.", 3900m, "/seed/productos/provoleta.jpg", true, true, FechaSemilla, FechaSemilla},
                    {4L, 2L, "Hamburguesa clasica", "Medallon de carne, lechuga, tomate y salsa de la casa.", 6500m, "/seed/productos/hamburguesa_clasica.jpg", true, true, FechaSemilla, FechaSemilla},
                    {5L, 2L, "Doble cheddar", "Doble medallon, doble cheddar y panceta.", 8200m, "/seed/productos/hamburguesa_cheddar.jpg", true, true, FechaSemilla, FechaSemilla},
                    // El unico no disponible de la carta: sirve para ver en pantalla que "dado de
                    // baja" y "hoy no se puede pedir" son dos cosas distintas.
                    {6L, 2L, "Veggie", "Medallon de garbanzos y vegetales grillados.", 6200m, "/seed/productos/hamburguesa_veggie.jpg", false, true, FechaSemilla, FechaSemilla},
                    {7L, 3L, "Muzzarella", "Salsa de tomate y abundante muzzarella.", 7000m, "/seed/productos/pizza_muzzarella.jpg", true, true, FechaSemilla, FechaSemilla},
                    {8L, 3L, "Napolitana", "Muzzarella, tomate fresco y ajo.", 7800m, "/seed/productos/pizza_napolitana.jpg", true, true, FechaSemilla, FechaSemilla},
                    {9L, 3L, "Especial", "Muzzarella, jamon, morrones y aceitunas.", 8500m, "/seed/productos/pizza_especial.jpg", true, true, FechaSemilla, FechaSemilla},
                    {10L, 4L, "Gaseosa", "Botella de gaseosa 500ml.", 2200m, "/seed/productos/gaseosa.jpg", true, true, FechaSemilla, FechaSemilla},
                    {11L, 4L, "Agua mineral", "Botella 500ml con o sin gas.", 1800m, "/seed/productos/agua.jpg", true, true, FechaSemilla, FechaSemilla},
                    {12L, 4L, "Cerveza artesanal", "Pinta de cerveza rubia artesanal.", 3800m, "/seed/productos/cerveza.jpg", true, true, FechaSemilla, FechaSemilla},
                    {13L, 5L, "Brownie con helado", "Brownie tibio con helado de crema.", 4200m, "/seed/productos/brownie.jpg", true, true, FechaSemilla, FechaSemilla},
                    {14L, 5L, "Flan casero", "Flan con dulce de leche y crema.", 3500m, "/seed/productos/flan.jpg", true, true, FechaSemilla, FechaSemilla}
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los productos primero: la FK a categorias es Restrict.
            migrationBuilder.DeleteData(table: "productos", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L, 11L, 12L, 13L, 14L});
            migrationBuilder.DeleteData(table: "categorias", keyColumn: "id", keyValues: new object[] {1L, 2L, 3L, 4L, 5L});
        }
    }
}
