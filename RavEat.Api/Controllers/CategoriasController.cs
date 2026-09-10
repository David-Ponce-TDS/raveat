using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController(RavEatDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) {
        // Proyeccion a un record en vez de devolver la entidad: la respuesta no arrastra las fechas
        // de auditoria ni el flag Activo, que son cosa de la base y no del frontend.
        var categorias = await db.Categorias.AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.Nombre)
            .Select(x => new CategoriaResponse(x.Id, x.Nombre, x.Orden))
            .ToListAsync(cancellationToken);
        return Ok(new {categorias});
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarCategoriaRequest request, CancellationToken cancellationToken) {
        if(string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new {codigo = "categoria_nombre_requerido", mensaje = "El nombre de la categoría es obligatorio."});
        var nombre = request.Nombre.Trim();
        var duplicado = await db.Categorias.AnyAsync(x => x.Nombre == nombre, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "categoria_duplicada", mensaje = "Ya existe una categoría con ese nombre."});

        // Sin orden explicito va al final: es lo que espera quien agrega una categoria nueva.
        var orden = request.Orden ?? (await db.Categorias.MaxAsync(x => (int?)x.Orden, cancellationToken) ?? 0) + 1;
        var categoria = new Categoria {Nombre = nombre, Orden = orden, Activo = true};
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {categoria = new CategoriaResponse(categoria.Id, categoria.Nombre, categoria.Orden)});
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Actualizar(long id, GuardarCategoriaRequest request, CancellationToken cancellationToken) {
        if(string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new {codigo = "categoria_nombre_requerido", mensaje = "El nombre de la categoría es obligatorio."});
        var categoria = await db.Categorias.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(categoria is null) return NotFound(new {codigo = "categoria_no_encontrada", mensaje = "La categoría no existe o fue dada de baja."});

        var nombre = request.Nombre.Trim();
        var duplicado = await db.Categorias.AnyAsync(x => x.Nombre == nombre && x.Id != id, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "categoria_duplicada", mensaje = "Ya existe otra categoría con ese nombre."});

        categoria.Nombre = nombre;
        if(request.Orden.HasValue) categoria.Orden = request.Orden.Value;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {categoria = new CategoriaResponse(categoria.Id, categoria.Nombre, categoria.Orden)});
    }

    // Una categoria con productos activos no se da de baja: dejaria productos colgando de algo que
    // ya no se lista. Primero hay que mover o dar de baja esos productos.
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken) {
        var categoria = await db.Categorias.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(categoria is null) return NotFound(new {codigo = "categoria_no_encontrada", mensaje = "La categoría no existe o ya fue dada de baja."});

        var conProductos = await db.Productos.AnyAsync(x => x.CategoriaId == id && x.Activo, cancellationToken);
        if(conProductos) return BadRequest(new {codigo = "categoria_con_productos", mensaje = "La categoría tiene productos activos. Movelos o dalos de baja primero."});

        categoria.Activo = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {eliminado = true});
    }
}

public sealed record CategoriaResponse(long Id, string Nombre, int Orden);
public sealed record GuardarCategoriaRequest(string Nombre, int? Orden);
