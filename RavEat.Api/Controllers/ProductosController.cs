using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Auth;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Controllers;

// Cerrado por defecto: lo publico se marca endpoint por endpoint. Olvidarse del atributo deja el
// endpoint cerrado de mas, que es el error barato; al reves, deja un agujero.
[ApiController]
[Authorize(Roles = RolCodigos.Admin)]
[Route("api/productos")]
public sealed class ProductosController(RavEatDbContext db) : ControllerBase {
    // La vitrina: la carta entera con sus categorias y los contadores del encabezado.
    // Devuelve todo de una sola vez, que alcanza para una carta de restaurante. El catalogo de
    // gestion, que si puede crecer sin limite, va por "listado" y esta paginado.
    [AllowAnonymous]
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken cancellationToken) {
        // Los contadores se cuentan en la base, no sobre la lista devuelta: son dos preguntas
        // distintas y mezclarlas es lo que despues da numeros que no cierran.
        var total = await db.Productos.CountAsync(x => x.Activo, cancellationToken);
        var disponibles = await db.Productos.CountAsync(x => x.Activo && x.Disponible, cancellationToken);
        var resumen = new ProductosResumenResponse(total, disponibles, total - disponibles);

        // La proyeccion intermedia es a un tipo anonimo y no al record directamente: EF no sabe
        // traducir un Where sobre una propiedad de un record recien construido (falla en tiempo de
        // ejecucion con "could not be translated"). Se filtra y ordena en la base, y el record se
        // arma despues, ya en memoria.
        var categoriasBase = await db.Categorias.AsNoTracking()
            .Where(x => x.Activo)
            .Select(categoria => new {
                categoria.Id,
                categoria.Nombre,
                categoria.Orden,
                Cantidad = db.Productos.Count(producto => producto.CategoriaId == categoria.Id && producto.Activo)
            })
            .Where(x => x.Cantidad > 0)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);
        var categorias = categoriasBase
            .Select(x => new CategoriaProductoResponse(x.Id, x.Nombre, x.Orden, x.Cantidad))
            .ToList();

        // El join se resuelve en la base y baja el nombre de la categoria ya pegado a cada
        // producto: si no, el frontend tendria que cruzar las dos listas a mano.
        var productos = await (
            from producto in db.Productos.AsNoTracking()
            join categoria in db.Categorias.AsNoTracking() on producto.CategoriaId equals categoria.Id
            where producto.Activo
            orderby categoria.Orden, producto.Nombre
            select new ProductoResumenResponse(
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.Precio,
                producto.ImagenUrl,
                producto.Disponible,
                categoria.Id,
                categoria.Nombre
            )
        ).ToListAsync(cancellationToken);

        return Ok(new {resumen, categorias, productos});
    }

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken) {
        var producto = await (
            from p in db.Productos.AsNoTracking()
            join categoria in db.Categorias.AsNoTracking() on p.CategoriaId equals categoria.Id
            where p.Id == id && p.Activo
            select new ProductoResumenResponse(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.ImagenUrl,
                p.Disponible,
                categoria.Id,
                categoria.Nombre
            )
        ).FirstOrDefaultAsync(cancellationToken);
        if(producto is null) return NotFound(new {codigo = "producto_no_encontrado", mensaje = "El producto no existe o fue dado de baja."});
        return Ok(new {producto});
    }

    // El catalogo de gestion: paginado y con los filtros resueltos en la base. A diferencia de la
    // vitrina, muestra tambien los no disponibles, porque desde aca se editan.
    [HttpGet("listado")]
    public async Task<IActionResult> Listado(
        [FromQuery] string? busqueda,
        [FromQuery(Name = "categoria_id")] long? categoriaId,
        [FromQuery] bool? disponible,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Productos.AsNoTracking().Where(x => x.Activo);
        var texto = (busqueda ?? string.Empty).Trim();
        if(texto.Length > 0) consulta = consulta.Where(x => x.Nombre.Contains(texto) || (x.Descripcion != null && x.Descripcion.Contains(texto)));
        if(categoriaId.HasValue && categoriaId.Value > 0) consulta = consulta.Where(x => x.CategoriaId == categoriaId.Value);

        // Los contadores cuentan sobre lo que coincide con la busqueda y la categoria, pero NO con
        // el filtro de disponibilidad: ese filtro se elige tocando esos mismos contadores, y si se
        // contaran a si mismos, al filtrar por "disponibles" el numero de no disponibles daria 0.
        var total = await consulta.CountAsync(cancellationToken);
        var disponibles = await consulta.CountAsync(x => x.Disponible, cancellationToken);
        var resumen = new ProductosResumenResponse(total, disponibles, total - disponibles);

        if(disponible.HasValue) consulta = consulta.Where(x => x.Disponible == disponible.Value);

        var (items, paginaResponse) = await (
            from producto in consulta
            join categoria in db.Categorias.AsNoTracking() on producto.CategoriaId equals categoria.Id
            orderby producto.Nombre
            select new ProductoResumenResponse(
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.Precio,
                producto.ImagenUrl,
                producto.Disponible,
                categoria.Id,
                categoria.Nombre
            )
        ).PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        return Ok(new {resumen, productos = items, pagina = paginaResponse});
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarProductoRequest request, CancellationToken cancellationToken) {
        var error = await ValidarAsync(request, cancellationToken);
        if(error is not null) return BadRequest(error);

        var nombre = request.Nombre.Trim();
        var duplicado = await db.Productos.AnyAsync(x => x.CategoriaId == request.CategoriaId && x.Nombre == nombre && x.Activo, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "producto_duplicado", mensaje = "Ya existe un producto con ese nombre en la categoría."});

        var producto = new Producto();
        Aplicar(producto, request);
        db.Productos.Add(producto);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {producto = await ProyectarAsync(producto, cancellationToken)});
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Actualizar(long id, GuardarProductoRequest request, CancellationToken cancellationToken) {
        var error = await ValidarAsync(request, cancellationToken);
        if(error is not null) return BadRequest(error);

        var producto = await db.Productos.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(producto is null) return NotFound(new {codigo = "producto_no_encontrado", mensaje = "El producto no existe o fue dado de baja."});

        var nombre = request.Nombre.Trim();
        var duplicado = await db.Productos.AnyAsync(x => x.CategoriaId == request.CategoriaId && x.Nombre == nombre && x.Activo && x.Id != id, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "producto_duplicado", mensaje = "Ya existe otro producto con ese nombre en la categoría."});

        Aplicar(producto, request);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {producto = await ProyectarAsync(producto, cancellationToken)});
    }

    // Baja logica: un producto vendido alguna vez no se borra, porque los items de pedidos viejos
    // lo referencian. La FK es Restrict justamente para que un DELETE real falle.
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken) {
        var producto = await db.Productos.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(producto is null) return NotFound(new {codigo = "producto_no_encontrado", mensaje = "El producto no existe o ya fue dado de baja."});
        producto.Activo = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {eliminado = true});
    }

    private async Task<object?> ValidarAsync(GuardarProductoRequest request, CancellationToken cancellationToken) {
        if(string.IsNullOrWhiteSpace(request.Nombre)) return new {codigo = "producto_nombre_requerido", mensaje = "El nombre es obligatorio."};
        if(request.Precio < 0) return new {codigo = "producto_precio_invalido", mensaje = "El precio no puede ser negativo."};
        var categoriaExiste = await db.Categorias.AnyAsync(x => x.Id == request.CategoriaId && x.Activo, cancellationToken);
        if(!categoriaExiste) return new {codigo = "categoria_no_encontrada", mensaje = "La categoría indicada no existe."};
        return null;
    }

    private static void Aplicar(Producto producto, GuardarProductoRequest request) {
        producto.CategoriaId = request.CategoriaId;
        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        producto.Precio = request.Precio;
        producto.ImagenUrl = string.IsNullOrWhiteSpace(request.ImagenUrl) ? null : request.ImagenUrl.Trim();
        producto.Disponible = request.Disponible;
    }

    private async Task<ProductoResumenResponse> ProyectarAsync(Producto x, CancellationToken cancellationToken) {
        var categoriaNombre = await db.Categorias.Where(c => c.Id == x.CategoriaId).Select(c => c.Nombre).FirstAsync(cancellationToken);
        return new ProductoResumenResponse(x.Id, x.Nombre, x.Descripcion, x.Precio, x.ImagenUrl, x.Disponible, x.CategoriaId, categoriaNombre);
    }
}

public sealed record ProductosResumenResponse(int Total, int Disponibles, int NoDisponibles);
public sealed record CategoriaProductoResponse(long Id, string Nombre, int Orden, int Cantidad);
public sealed record ProductoResumenResponse(
    long Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    string? ImagenUrl,
    bool Disponible,
    long CategoriaId,
    string CategoriaNombre
);

public sealed record GuardarProductoRequest(
    long CategoriaId,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    string? ImagenUrl,
    bool Disponible
);
