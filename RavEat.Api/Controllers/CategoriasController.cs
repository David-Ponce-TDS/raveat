using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;

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
}

public sealed record CategoriaResponse(long Id, string Nombre, int Orden);
