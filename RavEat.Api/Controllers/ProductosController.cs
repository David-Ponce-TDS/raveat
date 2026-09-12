using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;

namespace RavEat.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(RavEatDbContext db) : ControllerBase {
    // La carta entera de una sola vez, que alcanza para un restaurante. Cuando la lista pueda
    // crecer sin limite hay que paginar: eso es v4.
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken cancellationToken) {
        // Los contadores se cuentan en la base, no sobre la lista devuelta: son dos preguntas
        // distintas y mezclarlas es lo que despues da numeros que no cierran.
        var total = await db.Productos.CountAsync(x => x.Activo, cancellationToken);
        var disponibles = await db.Productos.CountAsync(x => x.Activo && x.Disponible, cancellationToken);
        var resumen = new ProductosResumenResponse(total, disponibles, total - disponibles);

        // Proyeccion intermedia a un tipo anonimo y no al record: EF no sabe traducir un Where
        // sobre una propiedad de un record recien construido ("could not be translated"). Se
        // filtra y ordena en la base; el record se arma despues, en memoria.
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
