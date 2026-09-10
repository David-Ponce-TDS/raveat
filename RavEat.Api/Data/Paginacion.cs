using Microsoft.EntityFrameworkCore;

namespace RavEat.Api.Data;

// Paginacion comun a los listados. Se resuelve con Skip/Take contra la base: traer la tabla
// entera y cortarla en memoria seria mentirle al cliente sobre cuanto hay, ademas de no escalar.
public sealed record PaginaConsulta {
    public const int TamanoPorDefecto = 20;
    public const int TamanoMaximo = 100;

    private PaginaConsulta(int pagina, int tamano) {
        Pagina = pagina;
        Tamano = tamano;
    }

    public int Pagina { get; }
    public int Tamano { get; }
    public int Saltear => (Pagina - 1) * Tamano;

    // Los valores llegan del cliente, asi que se acotan: una pagina 0 o un tamano de 10.000
    // no pueden tumbar la API.
    public static PaginaConsulta Desde(int? pagina, int? tamano) {
        var paginaSegura = pagina.HasValue && pagina.Value > 0 ? pagina.Value : 1;
        var tamanoSeguro = tamano.HasValue && tamano.Value > 0 ? Math.Min(tamano.Value, TamanoMaximo) : TamanoPorDefecto;
        return new PaginaConsulta(paginaSegura, tamanoSeguro);
    }
}

// Lo que el cliente necesita para saber si pedir la pagina siguiente.
public sealed record PaginaResponse(int Pagina, int Tamano, int Total, bool HayMas);

// Los filtros de estado llegan como CSV en snake_case ("en_preparacion,listo"), igual que los
// serializa la API. Se ignora lo que no corresponda a un valor conocido en vez de romper la
// consulta: el filtro es de la pantalla, no una orden que haya que validar con un 400.
public static class FiltroEnum {
    public static List<TEnum> Parsear<TEnum>(string? valores) where TEnum : struct, Enum {
        if(string.IsNullOrWhiteSpace(valores)) return [];
        var resultado = new List<TEnum>();
        foreach(var texto in valores.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            var normalizado = texto.Replace("_", string.Empty);
            foreach(var candidato in Enum.GetValues<TEnum>()) {
                if(!string.Equals(candidato.ToString(), normalizado, StringComparison.OrdinalIgnoreCase)) continue;
                if(!resultado.Contains(candidato)) resultado.Add(candidato);
                break;
            }
        }
        return resultado;
    }
}

public static class PaginacionExtensions {
    public static async Task<(List<T> Items, PaginaResponse Pagina)> PaginarAsync<T>(this IQueryable<T> consulta, PaginaConsulta pagina, CancellationToken cancellationToken) {
        var total = await consulta.CountAsync(cancellationToken);
        var items = await consulta.Skip(pagina.Saltear).Take(pagina.Tamano).ToListAsync(cancellationToken);
        return (items, new PaginaResponse(pagina.Pagina, pagina.Tamano, total, pagina.Saltear + items.Count < total));
    }
}
