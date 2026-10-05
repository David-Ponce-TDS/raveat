using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RavEat.Api.Migrations
{
    // Etapa 14 - Usuarios de prueba, uno por rol.
    // Hasta aca la base arrancaba con un solo usuario, el administrador, y todo lo que depende de
    // QUIEN hace la accion no se podia probar sin dar de alta usuarios a mano en cada maquina: la
    // bandeja y la push nunca le avisan al que ejecuta, asi que con un solo usuario no llega
    // ningun aviso. Con uno por rol, el vendedor crea el pedido, cocina lo pasa a listo, y al
    // telefono del vendedor le llega la push.
    // Sin id: la API asigna el siguiente, y una base que ya tenia usuarios cargados a mano no
    // choca con estos. Solo inserta datos, no toca el esquema.
    public partial class UsuariosDePrueba : Migration
    {
        // El mismo hash de Identidad: la contrasena publica de taller "RavEat123!", la del admin.
        // Publicada, la API la reemplaza al arrancar por UsuariosDePrueba:Password (Program.cs).
        private const string HashClaveTaller = "AQAAAAIAAYagAAAAEKmhIUHBbbjr/OUBkmjzGiK2+MSV0uQeWvFedcLz/kD98Sl1cUPSkgK7WIR0LEVO5Q==";

        private static readonly string[] Emails = { "vendedor@raveat.local", "proceso@raveat.local", "caja@raveat.local", "delivery@raveat.local" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los roles son los de Identidad: 2 Vendedor, 3 Proceso, 4 Caja, 5 Delivery.
            // Uno por uno y solo si el email no existe: una base que ya tenia alguno cargado a mano
            // (la publicada tenia delivery@raveat.local) rompia el INSERT por el indice unico del
            // email, la migracion fallaba y la API no arrancaba.
            var usuarios = new (long RolId, string Nombre, string Email)[] {
                (2L, "Vendedor", Emails[0]), (3L, "Proceso", Emails[1]), (4L, "Caja", Emails[2]), (5L, "Delivery", Emails[3])
            };
            foreach(var (rolId, nombre, email) in usuarios) {
                migrationBuilder.Sql($"""
                    INSERT INTO usuarios (rol_id, nombre, email, password_hash, activo, creado_en, actualizado_en)
                    SELECT {rolId}, '{nombre}', '{email}', '{HashClaveTaller}', TRUE, '2026-08-01 00:00:00', '2026-08-01 00:00:00' FROM DUAL
                    WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE email = '{email}');
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "usuarios",
                keyColumn: "email",
                keyValues: Emails);
        }
    }
}
