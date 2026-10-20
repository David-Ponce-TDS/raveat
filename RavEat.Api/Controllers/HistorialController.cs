using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;

namespace RavEat.Api.Controllers;

// El historial es de solo lectura: no hay POST, ni PUT, ni DELETE. Se escribe desde las acciones
// que registra (`Auditoria`), nunca desde afuera — un endpoint para agregar movimientos a mano
// convertiria la auditoria en un cuaderno que cualquiera puede rellenar.
[ApiController]
[Authorize(Roles = RolesOperativos)]
[Route("api/historial")]
public sealed class HistorialController(RavEatDbContext db) : ControllerBase {
    private const string RolesOperativos = $"{RolCodigos.Admin},{RolCodigos.Vendedor},{RolCodigos.Proceso},{RolCodigos.Caja},{RolCodigos.Delivery}";

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? busqueda,
        [FromQuery(Name = "solo_mios")] bool? soloMios,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        // Quien ve que: el admin ve todo el movimiento del local; el resto, solo lo suyo. La
        // decision se toma con el token, no con un parametro — con un `?usuario_id=` alcanzaria
        // con cambiar el numero para leer lo que hizo otro.
        var consulta = db.Historiales.AsNoTracking();
        if(!User.EsAdmin() || soloMios == true) {
            var usuarioId = User.UsuarioId();
            consulta = consulta.Where(x => x.UsuarioId == usuarioId);
        }

        // La busqueda va contra la base, igual que en el resto de las listas paginadas: filtrar en
        // la pantalla solo miraria la pagina descargada y diria "sin resultados" teniendo.
        var texto = (busqueda ?? string.Empty).Trim();
        if(texto.Length > 0) {
            consulta = consulta.Where(x =>
                x.EstadoNuevo.Contains(texto) ||
                (x.EstadoAnterior != null && x.EstadoAnterior.Contains(texto)) ||
                (x.Observacion != null && x.Observacion.Contains(texto)) ||
                db.Usuarios.Any(u => u.Id == x.UsuarioId && u.Nombre.Contains(texto))
            );
        }

        // Contadores sobre lo buscado, nunca sobre la pagina ni sobre el tipo: son los botones con los
        // que se elige el tipo, asi que no pueden depender de el. Mismo criterio que pedidos.
        var total = await consulta.CountAsync(cancellationToken);
        var resumen = new Dictionary<string, int> {
            ["pedido"] = await consulta.CountAsync(x => x.PedidoId != null, cancellationToken),
            ["entrega"] = await consulta.CountAsync(x => x.EntregaId != null, cancellationToken)
        };

        if(string.Equals(tipo, "pedido", StringComparison.OrdinalIgnoreCase)) consulta = consulta.Where(x => x.PedidoId != null);
        if(string.Equals(tipo, "entrega", StringComparison.OrdinalIgnoreCase)) consulta = consulta.Where(x => x.EntregaId != null);

        var (movimientos, paginaResponse) = await (
            from historial in consulta
            join usuario in db.Usuarios.AsNoTracking() on historial.UsuarioId equals usuario.Id
            join entrega in db.Entregas.AsNoTracking() on historial.EntregaId equals entrega.Id into entregas
            from entrega in entregas.DefaultIfEmpty()
            // El movimiento de una entrega tambien muestra el codigo del pedido: "entrega 5" no le
            // dice nada a nadie en el local; PED-0008 si.
            join pedido in db.Pedidos.AsNoTracking() on historial.PedidoId ?? entrega.PedidoId equals pedido.Id into pedidos
            from pedido in pedidos.DefaultIfEmpty()
            orderby historial.CreadoEn descending, historial.Id descending
            select new HistorialItemResponse(
                historial.Id,
                historial.PedidoId != null ? "pedido" : "entrega",
                historial.PedidoId ?? historial.EntregaId ?? 0,
                pedido != null ? pedido.Codigo : null,
                usuario.Nombre,
                historial.EstadoAnterior,
                historial.EstadoNuevo,
                historial.Observacion,
                historial.CreadoEn
            )
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        return Ok(new {historial = movimientos, pagina = paginaResponse, resumen, total});
    }
}

public sealed record HistorialItemResponse(
    long Id,
    string TipoEntidad,
    long EntidadId,
    string? PedidoCodigo,
    string UsuarioNombre,
    string? EstadoAnterior,
    string EstadoNuevo,
    string? Observacion,
    DateTimeOffset CreadoEn
);
