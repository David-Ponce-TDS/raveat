using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

// La entrega no se crea por este controller: nace con el pedido de tipo "entregas" (en
// PedidosController) copiando la direccion del cliente. Aca se lista, se asigna y se mueve.
[ApiController]
[Authorize(Roles = RolesOperativos)]
[Route("api/entregas")]
public sealed class EntregasController(RavEatDbContext db) : ControllerBase {
    private const string RolesOperativos = $"{RolCodigos.Admin},{RolCodigos.Vendedor},{RolCodigos.Proceso},{RolCodigos.Caja},{RolCodigos.Delivery}";

    // Mismo patron que el pedido: las transiciones en un diccionario, no en ifs. Asignada no
    // aparece como destino porque a ese estado se llega solo por /asignar, que valida repartidor.
    private static readonly Dictionary<EstadoEntrega, EstadoEntrega[]> TransicionesValidas = new() {
        [EstadoEntrega.Pendiente] = [EstadoEntrega.Cancelada],
        [EstadoEntrega.Asignada] = [EstadoEntrega.EnCamino, EstadoEntrega.Cancelada],
        [EstadoEntrega.EnCamino] = [EstadoEntrega.Entregada, EstadoEntrega.Cancelada],
        [EstadoEntrega.Entregada] = [],
        [EstadoEntrega.Cancelada] = []
    };

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estados,
        [FromQuery(Name = "solo_mias")] bool? soloMias,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Entregas.AsNoTracking().AsQueryable();

        // El repartidor filtra "las mias" con un flag, no mandando su id: la identidad sale del
        // token. Ademas de las suyas ve las pendientes, que son las que puede tomar.
        if(soloMias == true) {
            var usuarioId = User.UsuarioId();
            consulta = consulta.Where(x => x.RepartidorId == usuarioId || (x.RepartidorId == null && x.Estado == EstadoEntrega.Pendiente));
        }

        // Contadores por estado sobre el total filtrado, nunca sobre la pagina: son los botones
        // con los que se elige el filtro de estado, asi que no pueden depender de el.
        var conteos = await consulta
            .GroupBy(x => x.Estado)
            .Select(g => new {Estado = g.Key, Cantidad = g.Count()})
            .ToListAsync(cancellationToken);
        var resumen = conteos.ToDictionary(x => x.Estado.ToString(), x => x.Cantidad);
        var total = conteos.Sum(x => x.Cantidad);

        var estadosFiltro = FiltroEnum.Parsear<EstadoEntrega>(estados);
        if(estadosFiltro.Count > 0) consulta = consulta.Where(x => estadosFiltro.Contains(x.Estado));

        // El telefono del cliente baja pegado a la entrega: el repartidor llama o escribe desde la
        // misma fila, sin abrir otra pantalla para averiguar a quien le lleva.
        var (items, paginaResponse) = await (
            from entrega in consulta
            join pedido in db.Pedidos.AsNoTracking() on entrega.PedidoId equals pedido.Id
            join repartidor in db.Usuarios.AsNoTracking() on entrega.RepartidorId equals repartidor.Id into repartidores
            from repartidor in repartidores.DefaultIfEmpty()
            join cliente in db.Clientes.AsNoTracking() on pedido.ClienteId equals cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby entrega.CreadoEn descending
            select new EntregaResumenResponse(
                entrega.Id,
                entrega.PedidoId,
                pedido.Codigo,
                pedido.Total,
                entrega.RepartidorId,
                repartidor != null ? repartidor.Nombre : null,
                cliente != null ? cliente.Nombre : null,
                cliente != null ? cliente.Telefono : null,
                entrega.DireccionLinea,
                entrega.DireccionReferencia,
                entrega.DireccionLatitud,
                entrega.DireccionLongitud,
                entrega.Estado,
                entrega.Observaciones,
                entrega.CreadoEn
            )
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        return Ok(new {entregas = items, pagina = paginaResponse, resumen, total});
    }

    // Asignar: los que venden asignan a cualquier repartidor; un DELIVERY solo puede tomarla para
    // si mismo. La regla vive aca y no en el frontend, porque el frontend se puede editar.
    [HttpPut("{id:long}/asignar")]
    public async Task<IActionResult> Asignar(long id, AsignarEntregaRequest request, CancellationToken cancellationToken) {
        var entrega = await db.Entregas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(entrega is null) return NotFound(new {codigo = "entrega_no_encontrada", mensaje = "La entrega no existe."});
        if(entrega.Estado != EstadoEntrega.Pendiente) return BadRequest(new {codigo = "entrega_ya_asignada", mensaje = "La entrega ya no está pendiente."});

        var repartidorId = request.RepartidorId ?? User.UsuarioId();
        var esParaOtro = repartidorId != User.UsuarioId();
        if(esParaOtro && !User.IsInRole(RolCodigos.Admin) && !User.IsInRole(RolCodigos.Vendedor) && !User.IsInRole(RolCodigos.Caja)) {
            return StatusCode(403, new {codigo = "sin_permiso", mensaje = "Solo se puede tomar la entrega para uno mismo."});
        }

        // El asignado tiene que ser un usuario activo con rol de delivery (o un admin cubriendo).
        var repartidorValido = await (
            from usuario in db.Usuarios.AsNoTracking()
            join rol in db.Roles.AsNoTracking() on usuario.RolId equals rol.Id
            where usuario.Id == repartidorId && usuario.Activo && (rol.Codigo == RolCodigos.Delivery || rol.Codigo == RolCodigos.Admin)
            select usuario.Id
        ).AnyAsync(cancellationToken);
        if(!repartidorValido) return BadRequest(new {codigo = "repartidor_invalido", mensaje = "El repartidor indicado no existe o su rol no es de delivery."});

        entrega.RepartidorId = repartidorId;
        entrega.Estado = EstadoEntrega.Asignada;
        entrega.AsignadaEn = DateTimeOffset.UtcNow;
        // Se avisa al repartidor solo si lo asigno otro: tomarla uno mismo no necesita aviso.
        if(esParaOtro) {
            var codigoPedido = await db.Pedidos.AsNoTracking().Where(x => x.Id == entrega.PedidoId).Select(x => x.Codigo).FirstAsync(cancellationToken);
            db.Notificaciones.Add(new Notificacion {
                UsuarioId = repartidorId,
                Titulo = $"Entrega del pedido {codigoPedido}",
                // Con nombre y apellido: "te asignaron" no dice a quien preguntarle por la direccion.
                Mensaje = $"{await NombreDeQuienEjecuta(cancellationToken)} te asignó la entrega a {entrega.DireccionLinea}.",
                Tipo = TipoNotificacion.Entrega
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado = entrega.Estado, repartidor_id = entrega.RepartidorId});
    }

    [HttpPut("{id:long}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, CambiarEstadoEntregaRequest request, CancellationToken cancellationToken) {
        var entrega = await db.Entregas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(entrega is null) return NotFound(new {codigo = "entrega_no_encontrada", mensaje = "La entrega no existe."});

        // La mueve su repartidor; admin y los que venden pueden intervenir. Otro delivery, no.
        var esDuenio = entrega.RepartidorId == User.UsuarioId();
        if(!esDuenio && !User.IsInRole(RolCodigos.Admin) && !User.IsInRole(RolCodigos.Vendedor) && !User.IsInRole(RolCodigos.Caja)) {
            return StatusCode(403, new {codigo = "sin_permiso", mensaje = "La entrega la mueve el repartidor asignado."});
        }

        var permitidos = TransicionesValidas[entrega.Estado];
        if(!permitidos.Contains(request.Estado)) {
            return BadRequest(new {codigo = "transicion_invalida", mensaje = $"Una entrega en '{Etiqueta(entrega.Estado)}' no puede pasar a '{Etiqueta(request.Estado)}'."});
        }

        entrega.Estado = request.Estado;
        if(request.Estado == EstadoEntrega.Entregada) entrega.EntregadaEn = DateTimeOffset.UtcNow;

        // Al entregar o cancelar se avisa al creador del pedido, que es quien responde ante el
        // cliente. Mismo criterio que en pedidos: no se le avisa al que ejecuta.
        if(request.Estado is EstadoEntrega.Entregada or EstadoEntrega.Cancelada) {
            var pedido = await db.Pedidos.AsNoTracking().Where(x => x.Id == entrega.PedidoId).Select(x => new {x.Codigo, x.UsuarioCreadorId}).FirstAsync(cancellationToken);
            if(pedido.UsuarioCreadorId != User.UsuarioId()) {
                var quien = await NombreDeQuienEjecuta(cancellationToken);
                db.Notificaciones.Add(new Notificacion {
                    UsuarioId = pedido.UsuarioCreadorId,
                    Titulo = $"Entrega del pedido {pedido.Codigo}",
                    Mensaje = request.Estado == EstadoEntrega.Entregada ? $"{quien} entregó el pedido." : $"{quien} canceló la entrega.",
                    Tipo = TipoNotificacion.Entrega
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado = entrega.Estado});
    }

    // El nombre del que ejecuta sale de la base: el token lleva id, email y rol, no el nombre.
    private Task<string> NombreDeQuienEjecuta(CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking().Where(x => x.Id == User.UsuarioId()).Select(x => x.Nombre).FirstAsync(cancellationToken);

    private static string Etiqueta(EstadoEntrega estado) =>
        System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(estado.ToString());
}

public sealed record EntregaResumenResponse(
    long Id,
    long PedidoId,
    string PedidoCodigo,
    decimal PedidoTotal,
    long? RepartidorId,
    string? RepartidorNombre,
    string? ClienteNombre,
    string? ClienteTelefono,
    string DireccionLinea,
    string? DireccionReferencia,
    decimal? DireccionLatitud,
    decimal? DireccionLongitud,
    EstadoEntrega Estado,
    string? Observaciones,
    DateTimeOffset CreadoEn
);

public sealed record AsignarEntregaRequest(long? RepartidorId);
public sealed record CambiarEstadoEntregaRequest(EstadoEntrega Estado);
