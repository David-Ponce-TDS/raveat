using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(RavEatDbContext db) : ControllerBase {
    // La busqueda va contra la base y no contra la lista devuelta. Con el listado paginado, filtrar
    // en la pantalla solo miraria la pagina descargada y diria "no hay resultados" teniendo
    // resultados. Es la regla que acompana a la paginacion.
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano,
        CancellationToken cancellationToken
    ) {
        var consulta = db.Clientes.AsNoTracking().Where(x => x.Activo);
        var texto = (busqueda ?? string.Empty).Trim();
        if(texto.Length > 0) consulta = consulta.Where(x => x.Nombre.Contains(texto) || x.Telefono.Contains(texto) || (x.Email != null && x.Email.Contains(texto)));

        var (items, paginaResponse) = await consulta
            .OrderBy(x => x.Nombre)
            .Select(x => new ClienteResponse(x.Id, x.Nombre, x.Telefono, x.Email, x.DireccionLinea, x.DireccionReferencia, x.DireccionLatitud, x.DireccionLongitud))
            .PaginarAsync(PaginaConsulta.Desde(pagina, tamano), cancellationToken);

        return Ok(new {clientes = items, pagina = paginaResponse});
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken cancellationToken) {
        var cliente = await db.Clientes.AsNoTracking()
            .Where(x => x.Id == id && x.Activo)
            .Select(x => new ClienteResponse(x.Id, x.Nombre, x.Telefono, x.Email, x.DireccionLinea, x.DireccionReferencia, x.DireccionLatitud, x.DireccionLongitud))
            .FirstOrDefaultAsync(cancellationToken);
        if(cliente is null) return NotFound(new {codigo = "cliente_no_encontrado", mensaje = "El cliente no existe o fue dado de baja."});
        return Ok(new {cliente});
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarClienteRequest request, CancellationToken cancellationToken) {
        var error = Validar(request);
        if(error is not null) return BadRequest(error);

        var telefono = request.Telefono.Trim();
        // El telefono identifica al cliente en la practica: dos fichas con el mismo numero son la
        // misma persona cargada dos veces.
        var duplicado = await db.Clientes.AnyAsync(x => x.Telefono == telefono && x.Activo, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "cliente_duplicado", mensaje = "Ya existe un cliente con ese teléfono."});

        var cliente = new Cliente();
        Aplicar(cliente, request);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {cliente = Proyectar(cliente)});
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Actualizar(long id, GuardarClienteRequest request, CancellationToken cancellationToken) {
        var error = Validar(request);
        if(error is not null) return BadRequest(error);

        var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(cliente is null) return NotFound(new {codigo = "cliente_no_encontrado", mensaje = "El cliente no existe o fue dado de baja."});

        var telefono = request.Telefono.Trim();
        var duplicado = await db.Clientes.AnyAsync(x => x.Telefono == telefono && x.Activo && x.Id != id, cancellationToken);
        if(duplicado) return BadRequest(new {codigo = "cliente_duplicado", mensaje = "Ya existe otro cliente con ese teléfono."});

        Aplicar(cliente, request);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {cliente = Proyectar(cliente)});
    }

    // Baja logica: un cliente con pedidos viejos no se borra, porque esos pedidos lo referencian.
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken) {
        var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.Activo, cancellationToken);
        if(cliente is null) return NotFound(new {codigo = "cliente_no_encontrado", mensaje = "El cliente no existe o ya fue dado de baja."});
        cliente.Activo = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new {eliminado = true});
    }

    // La validacion vive en el servidor y no solo en el formulario: el frontend puede saltearse,
    // y el mismo endpoint lo puede llamar cualquier otro cliente.
    private static object? Validar(GuardarClienteRequest request) {
        if(string.IsNullOrWhiteSpace(request.Nombre)) return new {codigo = "cliente_nombre_requerido", mensaje = "El nombre es obligatorio."};
        if(string.IsNullOrWhiteSpace(request.Telefono)) return new {codigo = "cliente_telefono_requerido", mensaje = "El teléfono es obligatorio."};
        if(!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@')) return new {codigo = "cliente_email_invalido", mensaje = "El email no tiene un formato válido."};
        return null;
    }

    private static void Aplicar(Cliente cliente, GuardarClienteRequest request) {
        cliente.Nombre = request.Nombre.Trim();
        cliente.Telefono = request.Telefono.Trim();
        cliente.Email = Normalizar(request.Email);
        cliente.DireccionLinea = Normalizar(request.DireccionLinea);
        cliente.DireccionReferencia = Normalizar(request.DireccionReferencia);
        cliente.DireccionLatitud = request.DireccionLatitud;
        cliente.DireccionLongitud = request.DireccionLongitud;
    }

    // Un campo opcional vacio se guarda como NULL y no como cadena vacia: si no, "sin email" y
    // "email en blanco" serian dos estados distintos para la misma cosa.
    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ClienteResponse Proyectar(Cliente x) =>
        new(x.Id, x.Nombre, x.Telefono, x.Email, x.DireccionLinea, x.DireccionReferencia, x.DireccionLatitud, x.DireccionLongitud);
}

public sealed record ClienteResponse(
    long Id,
    string Nombre,
    string Telefono,
    string? Email,
    string? DireccionLinea,
    string? DireccionReferencia,
    decimal? DireccionLatitud,
    decimal? DireccionLongitud
);

public sealed record GuardarClienteRequest(
    string Nombre,
    string Telefono,
    string? Email,
    string? DireccionLinea,
    string? DireccionReferencia,
    decimal? DireccionLatitud,
    decimal? DireccionLongitud
);
