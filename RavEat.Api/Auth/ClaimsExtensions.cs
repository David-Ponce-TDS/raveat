using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RavEat.Api.Auth;

// Reemplazan al `usuario_id` que hasta v4 podria haber mandado el cliente: la identidad sale del
// token firmado y de ningun otro lado. Un endpoint que necesite saber quien es el usuario usa
// `User.UsuarioId()`, nunca un campo del body o del query.
public static class ClaimsExtensions {
    public static long UsuarioId(this ClaimsPrincipal usuario) {
        var valor = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(valor, out var id) ? id : 0;
    }

    public static string? RolCodigo(this ClaimsPrincipal usuario) {
        return usuario.FindFirstValue(TokenService.ClaimRolCodigo);
    }

    public static bool EsAdmin(this ClaimsPrincipal usuario) {
        return usuario.IsInRole(RolCodigos.Admin);
    }
}

public static class RolCodigos {
    public const string Admin = "ADMIN";
    public const string Vendedor = "VENDEDOR";
    public const string Proceso = "PROCESO";
    public const string Caja = "CAJA";
    public const string Delivery = "DELIVERY";
}
