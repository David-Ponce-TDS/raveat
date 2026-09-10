using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController(RavEatDbContext db) : ControllerBase {
    // Las transiciones validas de estado. Un pedido entregado no vuelve a preparacion, y uno
    // cancelado no revive. Tenerlas en un diccionario en vez de en una cadena de ifs deja la regla
    // a la vista y hace que agregar un estado sea tocar un solo lugar.
    private static readonly Dictionary<EstadoPedido, EstadoPedido[]> TransicionesValidas = new() {
        [EstadoPedido.Borrador] = [EstadoPedido.Confirmado, EstadoPedido.Cancelado],
        [EstadoPedido.Confirmado] = [EstadoPedido.EnPreparacion, EstadoPedido.Cancelado],
        [EstadoPedido.EnPreparacion] = [EstadoPedido.Listo, EstadoPedido.Cancelado],
        [EstadoPedido.Listo] = [EstadoPedido.Entregado, EstadoPedido.Cancelado],
        [EstadoPedido.Entregado] = [EstadoPedido.Cerrado],
        [EstadoPedido.Cerrado] = [],
        [EstadoPedido.Cancelado] = []
    };

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estados,
        [FromQuery(Name = "cliente_id")] long? clienteId,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Pedidos.AsNoTracking().AsQueryable();
        var texto = (busqueda ?? string.Empty).Trim();
        if(texto.Length > 0) consulta = consulta.Where(x => x.Codigo.Contains(texto));
        if(clienteId.HasValue && clienteId.Value > 0) consulta = consulta.Where(x => x.ClienteId == clienteId.Value);

        // Los contadores por estado se calculan sobre el total filtrado, no sobre la pagina: son
        // los botones con los que se elige el filtro, asi que no pueden depender de el.
        var conteos = await consulta
            .GroupBy(x => x.Estado)
            .Select(g => new {Estado = g.Key, Cantidad = g.Count()})
            .ToListAsync(cancellationToken);
        var resumen = conteos.ToDictionary(x => x.Estado.ToString(), x => x.Cantidad);
        var total = conteos.Sum(x => x.Cantidad);

        var estadosFiltro = FiltroEnum.Parsear<EstadoPedido>(estados);
        if(estadosFiltro.Count > 0) consulta = consulta.Where(x => estadosFiltro.Contains(x.Estado));

        var (items, paginaResponse) = await (
            from pedido in consulta
            join cliente in db.Clientes.AsNoTracking() on pedido.ClienteId equals cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            orderby pedido.CreadoEn descending
            select new PedidoResumenResponse(
                pedido.Id,
                pedido.Codigo,
                pedido.ClienteId,
                cliente != null ? cliente.Nombre : null,
                pedido.Tipo,
                pedido.Estado,
                pedido.Subtotal,
                pedido.Descuento,
                pedido.Total,
                pedido.EstadoPago,
                pedido.MedioPago,
                pedido.PropinaImporte,
                pedido.Observaciones,
                pedido.CreadoEn
            )
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        return Ok(new {pedidos = items, pagina = paginaResponse, resumen, total});
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken) {
        var pedido = await (
            from p in db.Pedidos.AsNoTracking()
            join cliente in db.Clientes.AsNoTracking() on p.ClienteId equals cliente.Id into clientes
            from cliente in clientes.DefaultIfEmpty()
            where p.Id == id
            select new PedidoResumenResponse(
                p.Id, p.Codigo, p.ClienteId, cliente != null ? cliente.Nombre : null,
                p.Tipo, p.Estado, p.Subtotal, p.Descuento, p.Total,
                p.EstadoPago, p.MedioPago, p.PropinaImporte, p.Observaciones, p.CreadoEn
            )
        ).FirstOrDefaultAsync(cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});

        var items = await db.PedidosItems.AsNoTracking()
            .Where(x => x.PedidoId == id)
            .OrderBy(x => x.Id)
            .Select(x => new PedidoItemResponse(x.Id, x.ProductoId, x.ProductoNombre, x.PrecioUnitario, x.Cantidad, x.Subtotal, x.Observaciones))
            .ToListAsync(cancellationToken);

        return Ok(new {pedido, items});
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearPedidoRequest request, CancellationToken cancellationToken) {
        if(request.Items is null || request.Items.Count == 0) return BadRequest(new {codigo = "pedido_sin_items", mensaje = "El pedido necesita al menos un producto."});
        if(request.Items.Any(x => x.Cantidad <= 0)) return BadRequest(new {codigo = "cantidad_invalida", mensaje = "Las cantidades tienen que ser mayores a cero."});
        if(request.ClienteId.HasValue) {
            var clienteExiste = await db.Clientes.AnyAsync(x => x.Id == request.ClienteId.Value && x.Activo, cancellationToken);
            if(!clienteExiste) return BadRequest(new {codigo = "cliente_no_encontrado", mensaje = "El cliente indicado no existe."});
        }

        // Los precios NO llegan del cliente: se leen de la base. Si el frontend mandara el precio,
        // cualquiera podria pedir una pizza a un peso cambiando el cuerpo de la request.
        var productoIds = request.Items.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await db.Productos.AsNoTracking()
            .Where(x => productoIds.Contains(x.Id) && x.Activo)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var faltante = productoIds.FirstOrDefault(id => !productos.ContainsKey(id));
        if(faltante != 0) return BadRequest(new {codigo = "producto_no_encontrado", mensaje = "Alguno de los productos no existe o fue dado de baja."});
        var noDisponible = productoIds.FirstOrDefault(id => !productos[id].Disponible);
        if(noDisponible != 0) return BadRequest(new {codigo = "producto_no_disponible", mensaje = $"'{productos[noDisponible].Nombre}' no está disponible."});

        var descuento = request.Descuento < 0 ? 0 : request.Descuento;
        var pedido = new Pedido {
            ClienteId = request.ClienteId,
            Codigo = await GenerarCodigoAsync(cancellationToken),
            Tipo = request.Tipo,
            Estado = EstadoPedido.Borrador,
            Descuento = descuento,
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim()
        };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(cancellationToken);

        decimal subtotal = 0;
        foreach(var item in request.Items) {
            var producto = productos[item.ProductoId];
            var subtotalItem = producto.Precio * item.Cantidad;
            subtotal += subtotalItem;
            db.PedidosItems.Add(new PedidoItem {
                PedidoId = pedido.Id,
                ProductoId = producto.Id,
                // Nombre y precio se copian: el pedido tiene que seguir diciendo lo que costo
                // entonces, aunque manana cambien el precio o el nombre del producto.
                ProductoNombre = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = item.Cantidad,
                Subtotal = subtotalItem,
                Observaciones = string.IsNullOrWhiteSpace(item.Observaciones) ? null : item.Observaciones.Trim()
            });
        }

        pedido.Subtotal = subtotal;
        pedido.Total = Math.Max(0, subtotal - descuento);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new {pedido_id = pedido.Id, codigo = pedido.Codigo, total = pedido.Total});
    }

    [HttpPut("{id:long}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, CambiarEstadoRequest request, CancellationToken cancellationToken) {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});

        var permitidos = TransicionesValidas[pedido.Estado];
        if(!permitidos.Contains(request.Estado)) {
            return BadRequest(new {
                codigo = "transicion_invalida",
                mensaje = $"Un pedido en '{Etiqueta(pedido.Estado)}' no puede pasar a '{Etiqueta(request.Estado)}'."
            });
        }

        pedido.Estado = request.Estado;
        if(request.Estado == EstadoPedido.Confirmado) pedido.ConfirmadoEn = DateTimeOffset.UtcNow;
        if(request.Estado == EstadoPedido.Cerrado) pedido.CerradoEn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado = pedido.Estado});
    }

    [HttpPut("{id:long}/pago")]
    public async Task<IActionResult> RegistrarPago(long id, RegistrarPagoRequest request, CancellationToken cancellationToken) {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});
        if(pedido.Estado == EstadoPedido.Cancelado) return BadRequest(new {codigo = "pedido_cancelado", mensaje = "Un pedido cancelado no se puede cobrar."});
        if(request.PropinaImporte < 0) return BadRequest(new {codigo = "propina_invalida", mensaje = "La propina no puede ser negativa."});

        pedido.EstadoPago = EstadoPago.Pagado;
        pedido.MedioPago = request.MedioPago;
        pedido.PropinaImporte = request.PropinaImporte;
        pedido.PagadoEn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {estado_pago = pedido.EstadoPago, total = pedido.Total, propina_importe = pedido.PropinaImporte});
    }

    // Un pedido no se borra: se cancela. El historial de lo que paso en el local es justamente lo
    // que no hay que perder.
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken) {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "El pedido no existe."});
        if(!TransicionesValidas[pedido.Estado].Contains(EstadoPedido.Cancelado)) {
            return BadRequest(new {codigo = "transicion_invalida", mensaje = $"Un pedido en '{Etiqueta(pedido.Estado)}' ya no se puede cancelar."});
        }
        pedido.Estado = EstadoPedido.Cancelado;
        if(pedido.EstadoPago == EstadoPago.Pendiente) pedido.EstadoPago = EstadoPago.Anulado;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {cancelado = true});
    }

    // El codigo lo genera el servidor y no el cliente: es lo que identifica al pedido en el
    // mostrador y no puede depender de dos telefonos que no se hablan entre si.
    // Los mensajes de error nombran el estado como lo serializa la API (snake_case), no como se
    // llama el enum en C#: si no, la pantalla muestra 'EnPreparacion' donde el resto dice
    // 'en_preparacion' y parecen dos cosas distintas.
    private static string Etiqueta(EstadoPedido estado) =>
        System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(estado.ToString());

    private async Task<string> GenerarCodigoAsync(CancellationToken cancellationToken) {
        var ultimo = await db.Pedidos.MaxAsync(x => (long?)x.Id, cancellationToken) ?? 0;
        return $"PED-{ultimo + 1:D4}";
    }
}

public sealed record PedidoResumenResponse(
    long Id,
    string Codigo,
    long? ClienteId,
    string? ClienteNombre,
    TipoPedido Tipo,
    EstadoPedido Estado,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    EstadoPago EstadoPago,
    MedioPago? MedioPago,
    decimal PropinaImporte,
    string? Observaciones,
    DateTimeOffset CreadoEn
);

public sealed record PedidoItemResponse(
    long Id,
    long ProductoId,
    string ProductoNombre,
    decimal PrecioUnitario,
    int Cantidad,
    decimal Subtotal,
    string? Observaciones
);

public sealed record CrearPedidoItemRequest(long ProductoId, int Cantidad, string? Observaciones);
public sealed record CrearPedidoRequest(long? ClienteId, TipoPedido Tipo, decimal Descuento, string? Observaciones, List<CrearPedidoItemRequest> Items);
public sealed record CambiarEstadoRequest(EstadoPedido Estado);
public sealed record RegistrarPagoRequest(MedioPago MedioPago, decimal PropinaImporte);
